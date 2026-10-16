using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Allocation;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Dtos.Savings;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningTools
{
    /// <summary>
    /// Reads one household summary.
    /// Cash, debts, income, bills, savings, living, allocation, and saved plans are the sections.
    /// Bills are the saved bills. A suggestion is left out. Another household is not read.
    /// </summary>
    Task<PlanningSummaryDto> ReadSummaryAsync(
        PlanningSummaryKind kind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a proposed income source with the same rules as saving one.
    /// The source is not saved. A contributor from another household is rejected.
    /// </summary>
    Task<IncomeFactProposalDto> ProposeIncomeAsync(
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a proposed bill with the same rules as saving one.
    /// The bill is not saved. A suggestion key is rejected, so a noticed payment does not become a bill.
    /// An account from another household is rejected.
    /// </summary>
    Task<BillFactProposalDto> ProposeBillAsync(
        UpsertObligationDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a proposed debt with the same rules as saving one.
    /// The debt is not saved. An account from another household is rejected.
    /// </summary>
    Task<DebtFactProposalDto> ProposeDebtAsync(
        UpsertDebtDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a proposed savings row with the same rules as saving one.
    /// The goal is not saved. The account has to be cash in this household, and not already followed.
    /// </summary>
    Task<SavingsFactProposalDto> ProposeSavingsAsync(
        UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a proposed monthly contribution with the same rules as saving one.
    /// The contribution is not saved. A person from another household is not found.
    /// </summary>
    Task<LivingContributionProposalDto> ProposeLivingContributionAsync(
        Guid contributorId,
        UpsertLivingContributionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a proposed allocation with the same rules as saving one.
    /// The percents are not saved, and no money is moved.
    /// </summary>
    Task<AllocationFactProposalDto> ProposeAllocationAsync(
        UpsertAllocationPreferenceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs one temporary scenario through the plan calculation.
    /// Extra, income loss, a windfall, flexible spending, cash to keep, priority, and reclaim are the supported amounts.
    /// Nothing is saved. The numbers come from that calculation.
    /// </summary>
    Task<PlanRecoveryDto> RequestCalculationAsync(
        PlanScenarioQuery scenario,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates two temporary scenarios and returns both.
    /// The supported amounts are the same ones a single calculation accepts. The two runs happen one after another.
    /// Neither scenario is saved.
    /// </summary>
    Task<PlanningScenarioComparisonDto> CompareScenariosAsync(
        PlanScenarioQuery baseline,
        PlanScenarioQuery alternative,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares one consolidation loan, balance transfer, or replacement loan.
    /// The offer is not saved and is not treated as approved. A debt from another household is rejected.
    /// </summary>
    Task<RestructuringComparisonDto> CompareRestructuringAsync(
        CompareRestructuringDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drafts the decision plan for one scenario and one freed-cash choice.
    /// The draft is not saved and not accepted. No money is moved.
    /// </summary>
    Task<PlanChangeDraftDto> DraftPlanChangeAsync(
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        CancellationToken cancellationToken = default);
}
