using Cardui.Api.Data;
using Cardui.Api.Domain;
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
        var householdId = _householdScope.RequireHouseholdId();
        var sources = _dbContext.IncomeSources
            .AsNoTracking()
            .Where(source => source.HouseholdId == householdId);

        return await sources
            .OrderBy(source => source.Name)
            .ThenBy(source => source.NextPaymentDate)
            .Select(IncomeSourceDtoMapper.Projection)
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
    /// Loads one income source in the signed-in household.
    /// A source from another household is not found.
    /// </summary>
    private async Task<IncomeSource> FindIncomeSourceAsync(
        Guid incomeSourceId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var source = await _dbContext.IncomeSources
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
            Currency = currency,
            Cadence = draft.Cadence,
            NextPaymentDate = draft.NextPaymentDate,
            ContributorId = draft.ContributorId,
            Reliability = draft.Reliability,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.IncomeSources.Add(source);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return source.Id;
    }

    /// <summary>
    /// Writes the payment facts onto an existing source.
    /// Currency is left as it was when the source was created.
    /// </summary>
    private async Task SaveIncomeSourceAsync(
        IncomeSource source,
        IncomeSourceDraft draft,
        CancellationToken cancellationToken)
    {
        source.Name = draft.Name;
        source.TakeHomeAmount = draft.TakeHomeAmount;
        source.Cadence = draft.Cadence;
        source.NextPaymentDate = draft.NextPaymentDate;
        source.ContributorId = draft.ContributorId;
        source.Reliability = draft.Reliability;
        source.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes the income source row.
    /// </summary>
    private async Task DeleteIncomeSourceAsync(
        IncomeSource source,
        CancellationToken cancellationToken)
    {
        _dbContext.IncomeSources.Remove(source);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the API shape of one household income source, including the contributor name.
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

        return source;
    }

    /// <summary>
    /// Turns the request into stored payment facts, or rejects it.
    /// </summary>
    private static IncomeSourceDraft RequireDraft(UpsertIncomeSourceDto dto)
    {
        if (!IncomeSourceRules.TryNormalize(
                dto.Name,
                dto.TakeHomeAmount,
                dto.Cadence,
                dto.NextPaymentDate,
                dto.ContributorId,
                dto.Reliability,
                out var draft,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    #endregion
}
