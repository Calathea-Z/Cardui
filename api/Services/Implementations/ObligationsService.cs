using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class ObligationsService : IObligationsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public ObligationsService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ObligationDto>> GetObligationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await LoadObligationsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ObligationDto> CreateAsync(
        UpsertObligationDto dto,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var draft = RequireDraft(dto);
        var currency = await RequirePlanningCurrencyAsync(householdId, cancellationToken);
        await RequireAccountAsync(householdId, draft.AccountId, cancellationToken);
        await RequireUniqueNameAsync(householdId, draft.Name, null, cancellationToken);
        var obligationId = await SaveNewObligationAsync(
            householdId,
            currency,
            draft,
            cancellationToken);
        return await ProjectObligationAsync(obligationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ObligationDto> UpdateAsync(
        Guid obligationId,
        UpsertObligationDto dto,
        CancellationToken cancellationToken = default)
    {
        var obligation = await FindObligationAsync(obligationId, cancellationToken);
        var draft = RequireDraft(dto);
        await RequireAccountAsync(obligation.HouseholdId, draft.AccountId, cancellationToken);
        await RequireUniqueNameAsync(
            obligation.HouseholdId,
            draft.Name,
            obligation.Id,
            cancellationToken);
        await SaveObligationAsync(obligation, draft, cancellationToken);
        return await ProjectObligationAsync(obligation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        Guid obligationId,
        CancellationToken cancellationToken = default)
    {
        var obligation = await FindObligationAsync(obligationId, cancellationToken);
        await DeleteObligationAsync(obligation, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Reads the household planning currency so a new bill is labeled in that currency.
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
    /// Loads one bill in the signed-in household.
    /// A bill from another household is not found.
    /// </summary>
    private async Task<Obligation> FindObligationAsync(
        Guid obligationId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var obligation = await _dbContext.Obligations
            .FirstOrDefaultAsync(
                x => x.Id == obligationId && x.HouseholdId == householdId,
                cancellationToken);

        if (obligation is null)
        {
            throw new NotFoundException("That bill was not found.");
        }

        return obligation;
    }

    /// <summary>
    /// Rejects an account that is not in this household.
    /// A missing account means the bill is not tied to one. An archived account may stay linked.
    /// </summary>
    private async Task RequireAccountAsync(
        Guid householdId,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (accountId is not Guid id)
        {
            return;
        }

        var exists = await _dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(
                account => account.Id == id && account.HouseholdId == householdId,
                cancellationToken);

        if (!exists)
        {
            throw new BadRequestException("Choose an account from this household.");
        }
    }

    /// <summary>
    /// Rejects a name another bill in the household already uses.
    /// The comparison ignores letter case.
    /// </summary>
    private async Task RequireUniqueNameAsync(
        Guid householdId,
        string name,
        Guid? exceptObligationId,
        CancellationToken cancellationToken)
    {
        var obligations = _dbContext.Obligations
            .AsNoTracking()
            .Where(obligation => obligation.HouseholdId == householdId);

        if (exceptObligationId is Guid exceptId)
        {
            obligations = obligations.Where(obligation => obligation.Id != exceptId);
        }

        var names = await obligations
            .Select(obligation => obligation.Name)
            .ToListAsync(cancellationToken);

        if (names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("That bill is already in the household.");
        }
    }

    /// <summary>
    /// Inserts one bill and returns its id.
    /// Currency is the household planning currency at creation. The amount stays one payment.
    /// </summary>
    private async Task<Guid> SaveNewObligationAsync(
        Guid householdId,
        string currency,
        ObligationDraft draft,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var obligation = new Obligation
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = draft.Name,
            Amount = draft.Amount,
            Currency = currency,
            Cadence = draft.Cadence,
            NextDueDate = draft.NextDueDate,
            AccountId = draft.AccountId,
            Flexibility = draft.Flexibility,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Obligations.Add(obligation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return obligation.Id;
    }

    /// <summary>
    /// Writes the accepted bill facts onto an existing row.
    /// Currency and the created time stay.
    /// </summary>
    private async Task SaveObligationAsync(
        Obligation obligation,
        ObligationDraft draft,
        CancellationToken cancellationToken)
    {
        obligation.Name = draft.Name;
        obligation.Amount = draft.Amount;
        obligation.Cadence = draft.Cadence;
        obligation.NextDueDate = draft.NextDueDate;
        obligation.AccountId = draft.AccountId;
        obligation.Flexibility = draft.Flexibility;
        obligation.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes one bill. The source account stays.
    /// </summary>
    private async Task DeleteObligationAsync(
        Obligation obligation,
        CancellationToken cancellationToken)
    {
        _dbContext.Obligations.Remove(obligation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the API shape of one household bill, including the source account name.
    /// </summary>
    private async Task<ObligationDto> ProjectObligationAsync(
        Guid obligationId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var obligation = await _dbContext.Obligations
            .AsNoTracking()
            .Where(x => x.Id == obligationId && x.HouseholdId == householdId)
            .Select(ObligationDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (obligation is null)
        {
            throw new NotFoundException("That bill was not found.");
        }

        return obligation;
    }

    /// <summary>
    /// Lists the household's bills, ordered by name, then by the next due date.
    /// </summary>
    private async Task<List<ObligationDto>> LoadObligationsAsync(
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await _dbContext.Obligations
            .AsNoTracking()
            .Where(obligation => obligation.HouseholdId == householdId)
            .OrderBy(obligation => obligation.Name)
            .ThenBy(obligation => obligation.NextDueDate)
            .Select(ObligationDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Turns the request into stored bill facts, or rejects it.
    /// </summary>
    private static ObligationDraft RequireDraft(UpsertObligationDto dto)
    {
        if (!ObligationRules.TryNormalize(
                dto.Name,
                dto.Amount,
                dto.Cadence,
                dto.NextDueDate,
                dto.AccountId,
                dto.Flexibility,
                out var draft,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    #endregion
}
