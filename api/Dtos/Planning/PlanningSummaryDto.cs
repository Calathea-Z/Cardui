using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Allocation;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Savings;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// One household summary. The section named by Kind is set. The other sections stay null.
/// Bills are saved bills. A suggestion is not included. This is not a transaction list or a bank token.
/// </summary>
public sealed class PlanningSummaryDto
{
    public PlanningSummaryKind Kind { get; init; }

    public CashPosition? Cash { get; init; }

    public DebtSummaryReportDto? Debts { get; init; }

    public IReadOnlyList<IncomeSourceDto>? Income { get; init; }

    public IReadOnlyList<ObligationDto>? Bills { get; init; }

    public IReadOnlyList<SavingsGoalDto>? Savings { get; init; }

    public LivingPageDto? Living { get; init; }

    public AllocationPageDto? Allocation { get; init; }

    public IReadOnlyList<PlanVersionDto>? Plan { get; init; }
}
