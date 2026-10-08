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
    public async Task<CashFlowRecoveryReportDto> GetRecoveryAsync(
        CancellationToken cancellationToken = default)
    {
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        var report = ProjectRecovery(debts);
        return CashFlowRecoveryDtoMapper.Map(report, debts.Count > 0);
    }

    #region Private Methods

    /// <summary>
    /// Runs the approved recovery rules for these debts.
    /// The planning currency is the household's. Extra and reclaim stay at the baseline.
    /// </summary>
    private CashFlowRecoveryReport ProjectRecovery(IReadOnlyList<DebtDto> debts)
    {
        var input = HouseholdRecovery.Prepare(
            _householdScope.PlanningCurrency,
            debts.Select(ToDebt).ToList());
        return CashFlowRecovery.Track(PayoffRollover.Compare(input));
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
