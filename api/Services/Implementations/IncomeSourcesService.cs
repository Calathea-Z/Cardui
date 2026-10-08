using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Domain.Income;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class IncomeSourcesService : IIncomeSourcesService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public IncomeSourcesService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IncomeSourceDto>> GetIncomeSourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await LoadIncomeSourcesAsync(cancellationToken);
        return AddSchedules(sources);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HouseholdIncome>> GetOutlookIncomesAsync(
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await _dbContext.IncomeSources
            .AsNoTracking()
            .Where(source => source.HouseholdId == householdId)
            .OrderBy(source => source.Name)
            .ThenBy(source => source.Id)
            .Select(source => new HouseholdIncome(
                source.Id,
                source.Name,
                source.Currency,
                source.TakeHomeAmount,
                source.LowTakeHomeAmount,
                source.Cadence,
                source.NextPaymentDate,
                source.Raises
                    .OrderBy(raise => raise.EffectiveDate)
                    .Select(raise => new DatedIncomeRaise(raise.EffectiveDate, raise.TakeHomeAmount))
                    .ToList(),
                source.ContributorId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IncomeSourceDto> CreateAsync(
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var draft = RequireDraft(dto);
        var currency = await RequirePlanningCurrencyAsync(householdId, cancellationToken);
        await RequireContributorAsync(householdId, draft.ContributorId, cancellationToken);
        await RequireUniqueNameAsync(householdId, draft.Name, null, cancellationToken);
        var incomeSourceId = await SaveNewIncomeSourceAsync(
            householdId,
            currency,
            draft,
            cancellationToken);
        return await ProjectIncomeSourceAsync(incomeSourceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IncomeSourceDto> UpdateAsync(
        Guid incomeSourceId,
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default)
    {
        var source = await FindIncomeSourceAsync(incomeSourceId, cancellationToken);
        var draft = RequireDraft(dto);
        await RequireContributorAsync(source.HouseholdId, draft.ContributorId, cancellationToken);
        await RequireUniqueNameAsync(
            source.HouseholdId,
            draft.Name,
            source.Id,
            cancellationToken);
        await SaveIncomeSourceAsync(source, draft, cancellationToken);
        return await ProjectIncomeSourceAsync(source.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        Guid incomeSourceId,
        CancellationToken cancellationToken = default)
    {
        var source = await FindIncomeSourceAsync(incomeSourceId, cancellationToken);
        await DeleteIncomeSourceAsync(source, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Reads the household planning currency so a new source is labeled in that currency.
    /// </summary>
    private async Task<string> RequirePlanningCurrencyAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var currency = await _dbContext.Households
            .AsNoTracking()
            .Where(household => household.Id == householdId)
            .Select(household => household.PlanningCurrency)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new NotFoundException("No household exists for the signed-in owner.");
        }

        return currency;
    }

    /// <summary>
    /// Loads one income source in the signed-in household, including its raises.
    /// A source from another household is not found.
    /// </summary>
    private async Task<IncomeSource> FindIncomeSourceAsync(
        Guid incomeSourceId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var source = await _dbContext.IncomeSources
            .Include(x => x.Raises)
            .FirstOrDefaultAsync(
                x => x.Id == incomeSourceId && x.HouseholdId == householdId,
                cancellationToken);

        if (source is null)
        {
            throw new NotFoundException("That income source was not found.");
        }

        return source;
    }

    /// <summary>
    /// Rejects a contributor that is not in this household.
    /// A missing contributor means the source is unassigned.
    /// </summary>
    private async Task RequireContributorAsync(
        Guid householdId,
        Guid? contributorId,
        CancellationToken cancellationToken)
    {
        if (contributorId is not Guid id)
        {
            return;
        }

        var exists = await _dbContext.HouseholdContributors
            .AsNoTracking()
            .AnyAsync(
                contributor => contributor.Id == id && contributor.HouseholdId == householdId,
                cancellationToken);

        if (!exists)
        {
            throw new BadRequestException("Choose a contributor from this household.");
        }
    }

    /// <summary>
    /// Rejects a name another source in the household already uses.
    /// The comparison ignores letter case.
    /// </summary>
    private async Task RequireUniqueNameAsync(
        Guid householdId,
        string name,
        Guid? exceptIncomeSourceId,
        CancellationToken cancellationToken)
    {
        var sources = _dbContext.IncomeSources
            .AsNoTracking()
            .Where(source => source.HouseholdId == householdId);

        if (exceptIncomeSourceId is Guid exceptId)
        {
            sources = sources.Where(source => source.Id != exceptId);
        }

        var names = await sources
            .Select(source => source.Name)
            .ToListAsync(cancellationToken);

        if (names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("That income source is already in the household.");
        }
    }

    /// <summary>
    /// Inserts one income source and returns its id.
    /// Currency is the household planning currency at creation.
    /// Low, strong, gross pay, and raises are stored with it when the draft includes them.
    /// </summary>
    private async Task<Guid> SaveNewIncomeSourceAsync(
        Guid householdId,
        string currency,
        IncomeSourceDraft draft,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var source = new IncomeSource
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = draft.Name,
            TakeHomeAmount = draft.TakeHomeAmount,
            LowTakeHomeAmount = draft.LowTakeHomeAmount,
            StrongTakeHomeAmount = draft.StrongTakeHomeAmount,
            GrossPayAmount = draft.GrossPayAmount,
            Currency = currency,
            Cadence = draft.Cadence,
            NextPaymentDate = draft.NextPaymentDate,
            ContributorId = draft.ContributorId,
            Reliability = draft.Reliability,
            CreatedAt = now,
            UpdatedAt = now
        };

        AssignRaises(source, draft.Raises);
        _dbContext.IncomeSources.Add(source);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return source.Id;
    }

    /// <summary>
    /// Writes the payment facts, scenarios, gross pay, and raises onto an existing source.
    /// Currency is left as it was when the source was created.
    /// </summary>
    private async Task SaveIncomeSourceAsync(
        IncomeSource source,
        IncomeSourceDraft draft,
        CancellationToken cancellationToken)
    {
        source.Name = draft.Name;
        source.TakeHomeAmount = draft.TakeHomeAmount;
        source.LowTakeHomeAmount = draft.LowTakeHomeAmount;
        source.StrongTakeHomeAmount = draft.StrongTakeHomeAmount;
        source.GrossPayAmount = draft.GrossPayAmount;
        source.Cadence = draft.Cadence;
        source.NextPaymentDate = draft.NextPaymentDate;
        source.ContributorId = draft.ContributorId;
        source.Reliability = draft.Reliability;
        source.UpdatedAt = _timeProvider.GetUtcNow();
        AssignRaises(source, draft.Raises);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes the income source and its expected raises.
    /// </summary>
    private async Task DeleteIncomeSourceAsync(
        IncomeSource source,
        CancellationToken cancellationToken)
    {
        _dbContext.IncomeRaises.RemoveRange(source.Raises);
        _dbContext.IncomeSources.Remove(source);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the API shape of one household income source, including the contributor name.
    /// Upcoming dates and the monthly average are added after the row is read.
    /// </summary>
    private async Task<IncomeSourceDto> ProjectIncomeSourceAsync(
        Guid incomeSourceId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var source = await _dbContext.IncomeSources
            .AsNoTracking()
            .Where(x => x.Id == incomeSourceId && x.HouseholdId == householdId)
            .Select(IncomeSourceDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            throw new NotFoundException("That income source was not found.");
        }

        return AddSchedules([source])[0];
    }

    /// <summary>
    /// Lists the household's income sources, ordered by name, without derived dates.
    /// </summary>
    private async Task<List<IncomeSourceDto>> LoadIncomeSourcesAsync(
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await _dbContext.IncomeSources
            .AsNoTracking()
            .Where(source => source.HouseholdId == householdId)
            .OrderBy(source => source.Name)
            .ThenBy(source => source.NextPaymentDate)
            .Select(IncomeSourceDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds the upcoming pay dates and the monthly average to each source.
    /// The average is not stored and is not a payment on a date. Today uses the household time zone.
    /// </summary>
    private IReadOnlyList<IncomeSourceDto> AddSchedules(IReadOnlyList<IncomeSourceDto> sources)
    {
        var today = FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
        foreach (var source in sources)
        {
            source.UpcomingPaymentDates = PaycheckSchedule.UpcomingDates(
                source.Cadence,
                source.NextPaymentDate,
                today);
            source.AverageMonthlyAmount = PaycheckSchedule.AverageMonthlyAmount(
                source.TakeHomeAmount,
                source.Cadence);
        }

        return sources;
    }

    /// <summary>
    /// Turns the request into stored payment facts, or rejects it.
    /// A missing raise list means no raise is expected.
    /// </summary>
    private static IncomeSourceDraft RequireDraft(UpsertIncomeSourceDto dto)
    {
        var raises = (dto.Raises ?? [])
            .Select(raise => new IncomeRaiseDraft(raise.EffectiveDate, raise.TakeHomeAmount))
            .ToList();

        if (!IncomeSourceRules.TryNormalize(
                dto.Name,
                dto.TakeHomeAmount,
                dto.Cadence,
                dto.NextPaymentDate,
                dto.ContributorId,
                dto.Reliability,
                out var draft,
                out var error,
                dto.LowTakeHomeAmount,
                dto.StrongTakeHomeAmount,
                raises,
                dto.GrossPayAmount))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Makes the source's raises match the accepted list.
    /// A date that is no longer present is deleted. A new date is inserted.
    /// The current typical amount is left alone.
    /// </summary>
    private void AssignRaises(
        IncomeSource source,
        IReadOnlyList<IncomeRaiseDraft> raises)
    {
        var dates = raises.Select(raise => raise.EffectiveDate).ToHashSet();
        var removed = source.Raises
            .Where(raise => !dates.Contains(raise.EffectiveDate))
            .ToList();

        foreach (var raise in removed)
        {
            source.Raises.Remove(raise);
            _dbContext.IncomeRaises.Remove(raise);
        }

        var sourceIsStored = _dbContext.ChangeTracker
            .Entries<IncomeSource>()
            .Any(entry => ReferenceEquals(entry.Entity, source));

        foreach (var draft in raises)
        {
            var match = source.Raises
                .FirstOrDefault(raise => raise.EffectiveDate == draft.EffectiveDate);

            if (match is null)
            {
                var raise = new IncomeRaise
                {
                    Id = Guid.NewGuid(),
                    IncomeSourceId = source.Id,
                    EffectiveDate = draft.EffectiveDate,
                    TakeHomeAmount = draft.TakeHomeAmount
                };
                source.Raises.Add(raise);

                // A preset id on a stored source is tracked as an update of a row
                // that does not exist yet. Mark the insert explicitly.
                if (sourceIsStored)
                {
                    _dbContext.Entry(raise).State = EntityState.Added;
                }

                continue;
            }

            match.TakeHomeAmount = draft.TakeHomeAmount;
        }
    }

    #endregion
}
