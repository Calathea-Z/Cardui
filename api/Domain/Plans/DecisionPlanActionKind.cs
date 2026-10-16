namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One of the actions a decision plan can rank.
/// The member name is the stored text. At most three actions are kept, in the order the rule ranks them.
/// </summary>
public enum DecisionPlanActionKind
{
    CoverCashShortfall,
    LowerExtra,
    AddStartingCash,
    AddDebt,
    AddIncome,
    FinishDebtTerms,
    RefreshBalances,
    HoldIncomeLoss,
    HoldWindfall,
    PayFirstDebt,
    UseFreedPayment,
    ConfirmBills,
    SetFlexibleSpending,
    ChooseCashToKeep
}
