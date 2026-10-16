using Cardui.Api.Data;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cardui.Api.Services.Implementations;

public class SavedPlanService : ISavedPlanService
{
    private const string StaleDraft = "This draft changed. Review the plan and save again.";

    private const string StaleAccept =
        "This version is no longer the draft. Accept the current draft, or save a new one.";

    private readonly CarduiDBContext _dbContext;
    private readonly IPlanService _planService;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public SavedPlanService(
        CarduiDBContext dbContext,
        IPlanService planService,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _planService = planService;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<DecisionPlanDto> GetPreviewAsync(
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        CancellationToken cancellationToken = default)
    {
        RequireKind(kind);
        return ToDto(await ComposeAsync(scenario, kind, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanVersionDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var versions = await LoadUntrackedAsync(cancellationToken);
        return versions.Select(ToVersionDto).ToList();
    }

    /// <inheritdoc />
    public async Task<PlanVersionDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var version = await FindUntrackedAsync(id, cancellationToken);
        return ToVersionDto(version);
    }

    /// <inheritdoc />
    public async Task<PlanVersionDto> SaveDraftAsync(
        SavePlanVersionDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireKind(dto.Kind);
        var plan = await ComposeAsync(ToScenario(dto), dto.Kind, cancellationToken);
        var householdId = _householdScope.RequireHouseholdId();
        var now = _timeProvider.GetUtcNow();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var versions = await LoadTrackedAsync(householdId, cancellationToken);
        var draft = versions.SingleOrDefault(version => version.Status == PlanVersionStatus.Draft);
        var same = draft is not null && DecisionPlanRules.Same(plan, Read(draft));
        var decision = PlanVersionRules.Save(draft?.Id, dto.ReplacesDraftId, same);

        if (decision == PlanVersionSaveResult.Unchanged)
        {
            return ToVersionDto(draft!);
        }

        if (decision == PlanVersionSaveResult.Stale)
        {
            throw new ConflictException(StaleDraft);
        }

        if (decision == PlanVersionSaveResult.ReplaceDraft)
        {
            draft!.Status = PlanVersionStatus.Superseded;
            draft.SupersededAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var created = NewVersion(householdId, versions, plan, now);
        _dbContext.PlanVersions.Add(created);
        await SaveVersionAsync(transaction, StaleDraft, cancellationToken);
        return ToVersionDto(created);
    }

    /// <inheritdoc />
    public async Task<PlanVersionDto> AcceptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var now = _timeProvider.GetUtcNow();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var versions = await LoadTrackedAsync(householdId, cancellationToken);
        var version = versions.SingleOrDefault(item => item.Id == id);
        if (version is null)
        {
            throw new NotFoundException("That saved plan was not found.");
        }

        var draft = versions.SingleOrDefault(item => item.Status == PlanVersionStatus.Draft);
        var decision = PlanVersionRules.Accept(
            version.Id == draft?.Id,
            version.Status == PlanVersionStatus.Accepted);
        if (decision == PlanVersionAcceptResult.AlreadyAccepted)
        {
            return ToVersionDto(version);
        }

        if (decision == PlanVersionAcceptResult.Stale)
        {
            throw new ConflictException(StaleAccept);
        }

        var previous = versions.SingleOrDefault(item => item.Status == PlanVersionStatus.Accepted);
        if (previous is not null)
        {
            previous.Status = PlanVersionStatus.Superseded;
            previous.SupersededAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        version.Status = PlanVersionStatus.Accepted;
        version.AcceptedAt = now;
        await SaveVersionAsync(transaction, StaleAccept, cancellationToken);
        return ToVersionDto(version);
    }

    #region Private Methods

    /// <summary>
    /// Calculates the recovery report and writes the decision plan from the selected path.
    /// </summary>
    private async Task<DecisionPlan> ComposeAsync(
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        CancellationToken cancellationToken)
    {
        var report = await _planService.GetRecoveryAsync(scenario, cancellationToken);
        return SavedPlanComposer.Compose(report, kind);
    }

    /// <summary>
    /// Rejects a payoff choice this plan does not store.
    /// </summary>
    private static void RequireKind(PayoffRolloverKind kind)
    {
        if (kind is not (PayoffRolloverKind.Rollover or PayoffRolloverKind.ReclaimAll))
        {
            throw new BadRequestException("Choose roll payments forward or free up cash.");
        }
    }

    /// <summary>
    /// The temporary scenario stored with a draft. Null spending and floor amounts keep the saved figures.
    /// </summary>
    private static PlanScenarioQuery ToScenario(SavePlanVersionDto dto)
    {
        return new PlanScenarioQuery
        {
            MonthlyExtra = dto.MonthlyExtra,
            IncomeLossMonths = dto.IncomeLossMonths,
            WindfallAmount = dto.WindfallAmount,
            WindfallOn = dto.WindfallOn,
            LivingSpendingMonthly = dto.LivingSpendingMonthly,
            CashFloor = dto.CashFloor
        };
    }

    /// <summary>
    /// Loads every version for the household so a save can see the open draft.
    /// The rows are tracked because a save may supersede one of them.
    /// </summary>
    private async Task<List<PlanVersion>> LoadTrackedAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PlanVersions
            .Where(version => version.HouseholdId == householdId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the household's versions without tracking them, newest first.
    /// </summary>
    private async Task<List<PlanVersion>> LoadUntrackedAsync(CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await _dbContext.PlanVersions
            .AsNoTracking()
            .Where(version => version.HouseholdId == householdId)
            .OrderByDescending(version => version.Number)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Loads one version in this household.
    /// </summary>
    private async Task<PlanVersion> FindUntrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var version = await _dbContext.PlanVersions
            .AsNoTracking()
            .Where(item => item.HouseholdId == householdId && item.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            throw new NotFoundException("That saved plan was not found.");
        }

        return version;
    }

    /// <summary>
    /// The next version row. Its document is the plan just calculated.
    /// </summary>
    private static PlanVersion NewVersion(
        Guid householdId,
        IReadOnlyList<PlanVersion> versions,
        DecisionPlan plan,
        DateTimeOffset now)
    {
        var number = versions.Count == 0 ? 1 : versions.Max(version => version.Number) + 1;
        return new PlanVersion
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Number = number,
            Status = PlanVersionStatus.Draft,
            CreatedAt = now,
            CalculationVersion = plan.CalculationVersion,
            AsOf = plan.AsOf,
            PlanningCurrency = plan.PlanningCurrency,
            Document = DecisionPlanJson.Serialize(plan)
        };
    }

    /// <summary>
    /// Saves the version change and commits it.
    /// A second draft or a second accepted plan is rejected so the open one stays the only one.
    /// </summary>
    private async Task SaveVersionAsync(
        IDbContextTransaction transaction,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsPlanVersionConstraint(exception))
        {
            throw new ConflictException(conflictMessage);
        }
    }

    /// <summary>
    /// True when the database rejected a second draft, a second accepted plan, or a repeated version number.
    /// </summary>
    private static bool IsPlanVersionConstraint(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains(
            "IX_PlanVersions",
            StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// Reads the decision plan stored on a version.
    /// </summary>
    private static DecisionPlan Read(PlanVersion version)
    {
        return DecisionPlanJson.Deserialize(version.Document);
    }

    /// <summary>
    /// The API shape of one version, including the plan it captured.
    /// </summary>
    private static PlanVersionDto ToVersionDto(PlanVersion version)
    {
        return new PlanVersionDto
        {
            Id = version.Id,
            Number = version.Number,
            Status = version.Status,
            CreatedAt = version.CreatedAt,
            AcceptedAt = version.AcceptedAt,
            SupersededAt = version.SupersededAt,
            Plan = ToDto(Read(version))
        };
    }

    /// <summary>
    /// The API shape of a decision plan.
    /// </summary>
    private static DecisionPlanDto ToDto(DecisionPlan plan)
    {
        return new DecisionPlanDto
        {
            CalculationVersion = plan.CalculationVersion,
            AsOf = plan.AsOf,
            PlanningCurrency = plan.PlanningCurrency,
            Kind = plan.Kind,
            Facts = ToDto(plan.Facts),
            Scenario = ToDto(plan.Scenario),
            Debts = plan.Debts.Select(ToDto).ToList(),
            Steps = plan.Steps.Select(ToDto).ToList(),
            MissingBalanceNames = plan.MissingBalanceNames.ToList(),
            ExcludedCurrencies = plan.ExcludedCurrencies.ToList(),
            Actions = plan.Actions.Select(ToDto).ToList(),
            Milestones = plan.Milestones.Select(ToDto).ToList(),
            Assumptions = plan.Assumptions.Select(ToDto).ToList(),
            Risks = plan.Risks.Select(ToDto).ToList(),
            Missing = plan.Missing.Select(ToDto).ToList(),
            ReviewConditions = plan.ReviewConditions.Select(ToDto).ToList()
        };
    }

    /// <summary>
    /// The API shape of the captured cash and payoff figures.
    /// </summary>
    private static DecisionPlanFactsDto ToDto(DecisionPlanFacts facts)
    {
        return new DecisionPlanFactsDto
        {
            StartingCash = facts.StartingCash,
            StartingReserve = facts.StartingReserve,
            StartingAvailable = facts.StartingAvailable,
            StartingCashAccountCount = facts.StartingCashAccountCount,
            StartingCashOldestAsOf = facts.StartingCashOldestAsOf,
            StaleCashCount = facts.StaleCashCount,
            HasIncome = facts.HasIncome,
            HasBills = facts.HasBills,
            LivingSpendingMonthly = facts.LivingSpendingMonthly,
            CashFloor = facts.CashFloor,
            HasEmergencyGoal = facts.HasEmergencyGoal,
            NamedSavingsGoalCount = facts.NamedSavingsGoalCount,
            StaleDebtCount = facts.StaleDebtCount,
            PaymentReviewCount = facts.PaymentReviewCount,
            Spendable = facts.Spendable,
            AlreadyShort = facts.AlreadyShort,
            Guardrail = new DecisionPlanGuardrailDto
            {
                Active = facts.Guardrail.Active,
                OrdinaryExpenses = facts.Guardrail.OrdinaryExpenses,
                NearTermObligations = facts.Guardrail.NearTermObligations,
                On = facts.Guardrail.On,
                Shortfall = facts.Guardrail.Shortfall,
                AlreadyShortWithoutExtra = facts.Guardrail.AlreadyShortWithoutExtra
            },
            ShortfallOn = facts.ShortfallOn,
            RecoveredOn = facts.RecoveredOn,
            ReserveShortfallOn = facts.ReserveShortfallOn,
            StartingObligation = facts.StartingObligation,
            RecurringRoom = facts.RecurringRoom,
            PaidOffOn = facts.PaidOffOn,
            TotalInterest = facts.TotalInterest
        };
    }

    /// <summary>
    /// The API shape of the selected scenario.
    /// </summary>
    private static DecisionPlanScenarioDto ToDto(DecisionPlanScenario scenario)
    {
        return new DecisionPlanScenarioDto
        {
            MonthlyExtra = scenario.MonthlyExtra,
            IncomeLossMonths = scenario.IncomeLossMonths,
            WindfallAmount = scenario.WindfallAmount,
            WindfallOn = scenario.WindfallOn,
            LivingSpendingMonthly = scenario.LivingSpendingMonthly,
            CashFloor = scenario.CashFloor
        };
    }

    /// <summary>
    /// The API shape of one captured debt.
    /// </summary>
    private static DecisionPlanDebtDto ToDto(DecisionPlanDebt debt)
    {
        return new DecisionPlanDebtDto
        {
            DebtId = debt.DebtId,
            Name = debt.Name,
            Balance = debt.Balance,
            Minimum = debt.Minimum,
            Apr = debt.Apr,
            Stop = debt.Stop,
            PaidOffOn = debt.PaidOffOn
        };
    }

    /// <summary>
    /// The API shape of one captured payoff.
    /// </summary>
    private static DecisionPlanStepDto ToDto(DecisionPlanStep step)
    {
        return new DecisionPlanStepDto
        {
            DebtId = step.DebtId,
            Name = step.Name,
            EndedOn = step.EndedOn,
            StartsOn = step.StartsOn,
            Minimum = step.Minimum,
            BreathingRoom = step.BreathingRoom
        };
    }

    /// <summary>
    /// The API shape of one ranked action.
    /// </summary>
    private static DecisionPlanActionDto ToDto(DecisionPlanAction action)
    {
        return new DecisionPlanActionDto
        {
            Order = action.Order,
            Kind = action.Kind,
            Name = action.Name,
            NextName = action.NextName,
            Amount = action.Amount,
            Extra = action.Extra,
            On = action.On,
            Count = action.Count,
            AlreadyShortWithoutExtra = action.AlreadyShortWithoutExtra
        };
    }

    /// <summary>
    /// The API shape of one assumption, risk, gap, or review condition.
    /// </summary>
    private static DecisionPlanDetailDto ToDto(DecisionPlanDetail detail)
    {
        return new DecisionPlanDetailDto
        {
            Kind = detail.Kind,
            Name = detail.Name,
            Amount = detail.Amount,
            On = detail.On,
            Count = detail.Count
        };
    }

    /// <summary>
    /// The API shape of one milestone.
    /// </summary>
    private static DecisionPlanMilestoneDto ToDto(DecisionPlanMilestone milestone)
    {
        return new DecisionPlanMilestoneDto
        {
            Kind = milestone.Kind,
            Name = milestone.Name,
            Amount = milestone.Amount,
            On = milestone.On
        };
    }

    #endregion
}
