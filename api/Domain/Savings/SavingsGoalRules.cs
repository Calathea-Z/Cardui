using Cardui.Api.Models;

namespace Cardui.Api.Domain.Savings;

public static class SavingsGoalRules
{
    public const decimal MaxAmount = 100_000_000m;

    public static readonly DateOnly EarliestDate = new(2000, 1, 1);

    public static readonly DateOnly LatestDate = new(2100, 12, 31);

    /// <summary>
    /// Checks a proposed row and returns the values to store.
    /// Everyday spending needs a monthly amount and a day of the month. Cash to keep needs a floor. A goal that finishes needs a target and a date.
    /// A blank available-now or amount-set-aside arrives as zero. Fixed names stay fixed.
    /// </summary>
    public static bool TryNormalize(
        SavingsGoalKind? kind,
        string? name,
        decimal? targetAmount,
        DateOnly? targetDate,
        decimal reservedAmount,
        Guid? accountId,
        bool useAccountBalance,
        decimal? monthlyAmount,
        int? readyDay,
        decimal? floorAmount,
        out SavingsGoalDraft draft,
        out string error)
    {
        draft = default;
        if (kind is not SavingsGoalKind goalKind)
        {
            error = "Choose everyday spending, cash to keep, an emergency goal, or something to save for.";
            return false;
        }

        if (!TryName(goalKind, name, out var storedName, out error))
        {
            return false;
        }

        if (!TryMoney(reservedAmount, AvailableLabel(goalKind), requirePositive: false, out var reserved, out error))
        {
            return false;
        }

        if (useAccountBalance && accountId is null)
        {
            error = "Choose an account to use its balance.";
            return false;
        }

        if (goalKind == SavingsGoalKind.Operating)
        {
            if (monthlyAmount is not decimal monthly || !TryMoney(monthly, "monthly amount", requirePositive: true, out var storedMonthly, out error))
            {
                error = string.IsNullOrEmpty(error) ? "Enter a monthly amount greater than zero." : error;
                return false;
            }

            if (readyDay is not int day || day < 1 || day > 31)
            {
                error = "Choose the day of the month this spending counts.";
                return false;
            }

            draft = new SavingsGoalDraft(
                goalKind, storedName, null, null, reserved, accountId, useAccountBalance, storedMonthly, day, null);
            error = "";
            return true;
        }

        if (goalKind == SavingsGoalKind.Floor)
        {
            if (floorAmount is not decimal floor || !TryMoney(floor, "amount to keep", requirePositive: true, out var storedFloor, out error))
            {
                error = string.IsNullOrEmpty(error) ? "Enter an amount to keep greater than zero." : error;
                return false;
            }

            draft = new SavingsGoalDraft(
                goalKind, storedName, null, null, reserved, accountId, useAccountBalance, null, null, storedFloor);
            error = "";
            return true;
        }

        if (targetAmount is not decimal targetInput || !TryMoney(targetInput, "target", requirePositive: true, out var target, out error))
        {
            error = string.IsNullOrEmpty(error) ? "Enter a target greater than zero." : error;
            return false;
        }

        if (targetDate is not DateOnly date || date < EarliestDate || date > LatestDate)
        {
            error = "Choose a target date.";
            return false;
        }

        draft = new SavingsGoalDraft(
            goalKind, storedName, target, date, reserved, accountId, useAccountBalance, null, null, null);
        error = "";
        return true;
    }

    #region Private Methods

    /// <summary>
    /// The stored name for a kind.
    /// Operating cash and the emergency goal are fixed. A named goal needs a name.
    /// </summary>
    private static bool TryName(
        SavingsGoalKind kind,
        string? name,
        out string storedName,
        out string error)
    {
        if (kind == SavingsGoalKind.Operating)
        {
            storedName = SavingsGoal.OperatingName;
            error = "";
            return true;
        }

        if (kind == SavingsGoalKind.Floor)
        {
            storedName = SavingsGoal.FloorName;
            error = "";
            return true;
        }

        if (kind == SavingsGoalKind.Emergency)
        {
            storedName = SavingsGoal.EmergencyName;
            error = "";
            return true;
        }

        storedName = name?.Trim() ?? "";
        if (storedName.Length == 0)
        {
            error = "Name what you are saving for.";
            return false;
        }

        if (storedName.Length > SavingsGoal.NameMaxLength)
        {
            error = "Use 80 characters or fewer.";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>
    /// The label in an amount error for this kind.
    /// Everyday spending and cash to keep call it available now. A goal that finishes calls it an amount set aside.
    /// </summary>
    private static string AvailableLabel(SavingsGoalKind kind)
    {
        return kind is SavingsGoalKind.Operating or SavingsGoalKind.Floor
            ? "available amount"
            : "amount set aside";
    }

    /// <summary>
    /// Rounds one money field to cents.
    /// A target has to be greater than zero. An amount set aside may be zero. A negative amount is rejected.
    /// </summary>
    private static bool TryMoney(
        decimal amount,
        string label,
        bool requirePositive,
        out decimal rounded,
        out string error)
    {
        rounded = 0;
        if (amount < 0)
        {
            error = $"The {label} cannot be negative.";
            return false;
        }

        if (requirePositive && amount == 0)
        {
            error = $"Enter a {label} greater than zero.";
            return false;
        }

        if (amount > MaxAmount)
        {
            error = "That amount is too large.";
            return false;
        }

        rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (requirePositive && rounded == 0)
        {
            error = $"Enter a {label} greater than zero.";
            return false;
        }

        error = "";
        return true;
    }

    #endregion
}
