namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Which household summary a planning tool may read.
/// Cash is the Accounts cash total. Debts are the debt summary. Bills are saved bills.
/// Plan is the saved plan versions. The member name is the tool's name for that summary.
/// </summary>
public enum PlanningSummaryKind
{
    Cash,
    Debts,
    Income,
    Bills,
    Savings,
    Living,
    Allocation,
    Plan
}
