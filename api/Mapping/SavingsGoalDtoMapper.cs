using Cardui.Api.Domain.Recovery;
using Cardui.Api.Domain.Savings;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class SavingsGoalDtoMapper
{
    /// <summary>
    /// Copies one goal into the shape the Savings page renders.
    /// The amount in use is the account balance while following without an override, and the typed amount otherwise.
    /// The monthly amount is calculated through the target date. It is not stored.
    /// </summary>
    public static SavingsGoalDto Map(
        SavingsGoal goal,
        SavingsAccountBalance? account,
        DateOnly today,
        string planningCurrency)
    {
        var following = IsFollowing(goal, account, planningCurrency);
        var overridden = following && goal.ReservedOverriddenAt is not null;
        var balance = following ? account!.CurrentBalance : (decimal?)null;
        var amountInUse = AmountInUse(goal, account, planningCurrency);
        var plan = goal.Kind is SavingsGoalKind.Emergency or SavingsGoalKind.Sinking
            ? SavingsTarget.Plan(
                amountInUse,
                goal.TargetAmount ?? 0,
                today,
                goal.TargetDate ?? today,
                null)
            : null;
        var floor = goal.FloorAmount ?? 0;
        var floorGap = amountInUse >= floor ? 0 : floor - amountInUse;
        return new SavingsGoalDto
        {
            Id = goal.Id,
            Kind = goal.Kind,
            Name = FixedName(goal),
            TargetAmount = goal.TargetAmount,
            TargetDate = goal.TargetDate,
            MonthlyAmount = goal.MonthlyAmount,
            ReadyDay = goal.ReadyDay,
            FloorAmount = goal.FloorAmount,
            ReservedAmount = goal.ReservedAmount,
            AmountInUse = amountInUse,
            Currency = goal.Currency,
            AccountId = goal.AccountId,
            AccountName = account?.Name,
            AccountMask = account?.Mask,
            AccountBalance = balance,
            Following = following,
            ReservedOverridden = overridden,
            AccountUnavailable = goal.AccountFollowedSince is not null && goal.AccountId is not null && !following,
            NegativeBalance = following && !overridden && balance < 0,
            Remaining = plan?.Remaining ?? (goal.Kind == SavingsGoalKind.Floor ? floorGap : 0),
            AlreadyMet = plan?.AlreadyMet ?? (goal.Kind == SavingsGoalKind.Floor && floor > 0 && amountInUse >= floor),
            DatePassed = plan?.DatePassed ?? false,
            BeyondHorizon = plan?.TargetDateBeyondHorizon ?? false,
            AmountNeededPerMonth = plan?.AmountNeededPerMonth,
            FinalAmountNeeded = plan?.FinalAmountNeeded
        };
    }

    /// <summary>
    /// The amount the plan protects for one goal.
    /// A followed account with no override uses its balance. Anything else uses the typed amount.
    /// </summary>
    public static decimal AmountInUse(
        SavingsGoal goal,
        SavingsAccountBalance? account,
        string planningCurrency)
    {
        var following = IsFollowing(goal, account, planningCurrency);
        var overridden = following && goal.ReservedOverriddenAt is not null;
        var balance = following ? account!.CurrentBalance : (decimal?)null;
        return SavingsAmount.InUse(following, overridden, goal.ReservedAmount, balance);
    }

    #region Private Methods

    /// <summary>
    /// The name on the screen.
    /// Everyday spending and Emergency use their fixed names, including a row saved under an older label.
    /// </summary>
    private static string FixedName(SavingsGoal goal)
    {
        if (goal.Kind == SavingsGoalKind.Operating)
        {
            return SavingsGoal.OperatingName;
        }

        if (goal.Kind == SavingsGoalKind.Floor)
        {
            return SavingsGoal.FloorName;
        }

        if (goal.Kind == SavingsGoalKind.Emergency)
        {
            return SavingsGoal.EmergencyName;
        }

        return goal.Name;
    }

    /// <summary>
    /// True when the stored account is still a cash account the goal may follow.
    /// A missing, archived, or ineligible account is not followed.
    /// </summary>
    private static bool IsFollowing(
        SavingsGoal goal,
        SavingsAccountBalance? account,
        string planningCurrency)
    {
        return goal.AccountFollowedSince is not null
            && account is not null
            && goal.AccountId == account.Id
            && SavingsAccounts.CanFollow(
                account.Type,
                account.Currency,
                account.IsActive,
                account.ArchivedAt,
                planningCurrency);
    }

    #endregion
}
