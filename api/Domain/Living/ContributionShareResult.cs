using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Living;

/// <summary>
/// Paychecks after contribution shares, plus the monthly picture of what is shared.
/// Incomes keep their real dates. A cap scales a scheduled paycheck. It does not add a deposit on the 1st.
/// UnassignedNames are paychecks with no person. SharedMonthly includes those scheduled amounts and each person's shared amount.
/// </summary>
public sealed record ContributionShareResult(
    IReadOnlyList<HouseholdIncome> Incomes,
    IReadOnlyList<ContributionPerson> People,
    IReadOnlyList<string> UnassignedNames,
    decimal SharedMonthly);
