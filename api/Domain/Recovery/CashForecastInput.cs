namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The facts for one forecast.
/// StartingCash is on hand at the start of AsOf, before that day's events.
/// StartingReserve is the part already protected. It can be larger than cash, and available cash is then negative.
/// Income, bill, and debt amounts are one payment, not a monthly average.
/// Savings events reserve cash. Each debt keeps its own extra. A shared extra pool is not applied here.
/// IncomeBasis records whether those income amounts are conservative or typical.
/// OmitIncomeBefore drops included pay from AsOf until that date. Null keeps every paycheck. The date itself is paid.
/// Windfall is one cash inflow. Null adds nothing. It does not change the reserve.
/// </summary>
public sealed record CashForecastInput(
    string PlanningCurrency,
    ForecastIncomeBasis IncomeBasis,
    DateOnly AsOf,
    decimal StartingCash,
    decimal StartingReserve,
    IReadOnlyList<DatedIncome> Incomes,
    IReadOnlyList<DatedBill> Bills,
    IReadOnlyList<DebtAmortizationInput> Debts,
    IReadOnlyList<CashFlowEvent> Savings,
    DateOnly? OmitIncomeBefore = null,
    CashFlowEvent? Windfall = null);
