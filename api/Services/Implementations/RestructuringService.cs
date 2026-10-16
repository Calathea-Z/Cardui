using Cardui.Api.Domain;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class RestructuringService : IRestructuringService
{
    private readonly IDebtsService _debtsService;
    private readonly IAccountsService _accountsService;
    private readonly ISavingsGoalsService _savingsGoalsService;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public RestructuringService(
        IDebtsService debtsService,
        IAccountsService accountsService,
        ISavingsGoalsService savingsGoalsService,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _debtsService = debtsService;
        _accountsService = accountsService;
        _savingsGoalsService = savingsGoalsService;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<RestructuringComparisonDto> CompareAsync(
        CompareRestructuringDto dto,
        CancellationToken cancellationToken = default)
    {
        var today = FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
        var selected = await SelectDebtsAsync(dto.DebtIds, cancellationToken);
        var cash = await LoadCashAsync(today, cancellationToken);
        return RestructuringComparisonDtoMapper.Map(Compare(today, selected, cash, dto));
    }

    #region Private Methods

    /// <summary>
    /// Loads the selected debts in the order given.
    /// An unknown id is rejected. The loads stay in this household.
    /// </summary>
    private async Task<List<RestructuringDebt>> SelectDebtsAsync(
        IReadOnlyList<Guid>? debtIds,
        CancellationToken cancellationToken)
    {
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        if (debtIds is null || debtIds.Count == 0)
        {
            return [];
        }

        var byId = debts.ToDictionary(debt => debt.Id);
        var selected = new List<RestructuringDebt>();
        foreach (var debtId in debtIds)
        {
            if (!byId.TryGetValue(debtId, out var debt))
            {
                throw new BadRequestException("Choose debts from this household.");
            }

            selected.Add(ToDebt(debt));
        }

        return selected;
    }

    /// <summary>
    /// Loads cash on hand and the reserve for the opportunity-cost comparison.
    /// The loads run one after another because they share one database context.
    /// </summary>
    private async Task<RestructuringCash> LoadCashAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var cashPosition = await _accountsService.GetCashPositionAsync(cancellationToken);
        var savings = await _savingsGoalsService.GetOutlookAsync(today, cancellationToken);
        return new RestructuringCash(
            cashPosition.Total,
            savings.StartingReserve,
            savings.CashFloor,
            savings.HasCashFloor);
    }

    /// <summary>
    /// Runs the comparison. A rejected offer becomes a bad request and is not saved.
    /// </summary>
    private RestructuringComparison Compare(
        DateOnly today,
        IReadOnlyList<RestructuringDebt> debts,
        RestructuringCash cash,
        CompareRestructuringDto dto)
    {
        var offer = new RestructuringOffer(
            dto.Kind,
            dto.Apr,
            dto.TermMonths,
            dto.Fee,
            dto.FeeFunding,
            dto.UpfrontCash);
        if (!RestructuringComparisonRules.TryCompare(
                _householdScope.PlanningCurrency,
                today,
                debts,
                offer,
                cash,
                out var comparison,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return comparison!;
    }

    /// <summary>
    /// Copies the balance in use. A stored balance that following replaced is not the amount owed.
    /// </summary>
    private static RestructuringDebt ToDebt(DebtDto debt)
    {
        return new RestructuringDebt(
            debt.Id,
            debt.Name,
            debt.Currency,
            debt.Kind,
            debt.BalanceInUse,
            debt.Apr,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            debt.MinimumPayment,
            debt.RemainingTermMonths,
            debt.NextDueDate);
    }

    #endregion
}
