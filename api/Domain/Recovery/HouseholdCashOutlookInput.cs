namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The household cash facts behind the Plan cash outlook.
/// StartingCash is the Cash total on Accounts at the start of AsOf, before that day's payments.
/// StartingReserve is the part already set aside. Savings raise that reserve later and do not reduce cash.
/// </summary>
public sealed record HouseholdCashOutlookInput(
    string PlanningCurrency,
    DateOnly AsOf,
    decimal StartingCash,
    IReadOnlyList<HouseholdIncome> Incomes,
    IReadOnlyList<DatedBill> Bills,
    decimal StartingReserve,
    IReadOnlyList<CashFlowEvent> Savings);
