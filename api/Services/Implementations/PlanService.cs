using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Mapping;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class PlanService : IPlanService
{
    private readonly IDebtsService _debtsService;
    private readonly HouseholdScope _householdScope;

    public PlanService(IDebtsService debtsService, HouseholdScope householdScope)
    {
        _debtsService = debtsService;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PlanRecoveryDto> GetRecoveryAsync(
        CancellationToken cancellationToken = default)
    {
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        return ProjectRecovery(debts);
    }

    #region Private Methods

    /// <summary>
    /// Runs the approved rollover and recovery rules for these debts, then shapes the result for the page.
    /// The planning currency is the household's. Extra and reclaim stay at the baseline.
    /// </summary>
    private PlanRecoveryDto ProjectRecovery(IReadOnlyList<DebtDto> debts)
    {
        var prepared = HouseholdRecovery.Prepare(
            _householdScope.PlanningCurrency,
            debts.Select(ToDebt).ToList());
        var comparison = PayoffRollover.Compare(prepared.Rollover);
        var report = CashFlowRecovery.Track(comparison);
        return PlanRecoveryDtoMapper.Map(comparison, report, prepared.MissingBalance, debts.Count > 0);
    }

    /// <summary>
    /// Copies the balance and limit already in use. A stored balance that following replaced is not the amount owed.
    /// </summary>
    private static HouseholdRecoveryDebt ToDebt(DebtDto debt)
    {
        return new HouseholdRecoveryDebt(
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
            debt.NextDueDate,
            debt.CreditLimitInUse);
    }

    #endregion
}
