using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Income;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Living;

public static class ContributionShare
{
    /// <summary>
    /// Scales scheduled paychecks so the shared plan uses each person's monthly amount.
    /// A blank amount leaves that person's paychecks whole. A paycheck with no person stays whole.
    /// The scale is a share of today's scheduled pay, applied to each real paycheck, including a later raise and low pay.
    /// An amount with no scheduled paycheck is not turned into a deposit.
    /// </summary>
    public static ContributionShareResult Apply(
        IReadOnlyList<HouseholdIncome> incomes,
        IReadOnlyList<ContributionShareInput> inputs,
        string planningCurrency)
    {
        var people = new List<ContributionPerson>();
        var scaledIds = new Dictionary<Guid, HouseholdIncome>();
        foreach (var input in inputs.OrderBy(input => input.Name, StringComparer.OrdinalIgnoreCase).ThenBy(input => input.ContributorId))
        {
            var own = incomes.Where(income => income.ContributorId == input.ContributorId).ToList();
            var person = ApplyShare(input, own, planningCurrency, scaledIds);
            people.Add(person);
        }

        var result = new List<HouseholdIncome>(incomes.Count);
        foreach (var income in incomes)
        {
            result.Add(scaledIds.TryGetValue(income.Id, out var scaled) ? scaled : income);
        }

        var unassigned = incomes
            .Where(income => income.ContributorId is null)
            .Select(income => income.Name)
            .ToList();
        var shared = AccountLedger.Round(
            people.Sum(person => person.SharedMonthly)
            + UnassignedMonthly(incomes, planningCurrency));
        return new ContributionShareResult(result, people, unassigned, shared);
    }

    #region Private Methods

    /// <summary>
    /// Decides one person's shared pay and scales their scheduled paychecks when the cap is below that pay.
    /// Pay in another currency stays as recorded and is not part of the monthly average.
    /// </summary>
    private static ContributionPerson ApplyShare(
        ContributionShareInput input,
        IReadOnlyList<HouseholdIncome> incomes,
        string planningCurrency,
        Dictionary<Guid, HouseholdIncome> scaledIds)
    {
        var scheduled = incomes
            .Where(income => Scheduled(income, planningCurrency))
            .ToList();
        var recorded = AccountLedger.Round(scheduled.Sum(income => Monthly(income.TypicalAmount, income.Cadence)));
        var unscheduled = incomes.Any(income => !Scheduled(income, planningCurrency) && Included(income.Currency, planningCurrency));
        if (input.MonthlyAmount is not decimal amount)
        {
            return Person(input, recorded == 0 ? null : recorded, recorded, 0m, ContributionLimit.FullPay, unscheduled);
        }

        var capAmount = AccountLedger.Round(amount);
        if (scheduled.Count == 0)
        {
            return Person(input, null, 0m, 0m, ContributionLimit.Unplaced, unscheduled);
        }

        if (capAmount >= recorded)
        {
            return Person(input, recorded, recorded, 0m, ContributionLimit.AllRecordedPay, unscheduled);
        }

        var factor = recorded == 0 ? 0m : capAmount / recorded;
        foreach (var income in scheduled)
        {
            scaledIds[income.Id] = Scale(income, factor);
        }

        var shared = AccountLedger.Round(scheduled.Sum(income =>
            Monthly(AccountLedger.Round(income.TypicalAmount * factor), income.Cadence)));
        var kept = AccountLedger.Round(recorded - shared);
        return Person(input, recorded, shared, kept, ContributionLimit.Shared, unscheduled);
    }

    /// <summary>
    /// True when the paycheck has a yearly average and counts in the planning currency.
    /// A blank currency counts. Irregular pay has no average.
    /// </summary>
    private static bool Scheduled(HouseholdIncome income, string planningCurrency)
    {
        return Included(income.Currency, planningCurrency)
            && PaycheckSchedule.AverageMonthlyAmount(income.TypicalAmount, income.Cadence) is not null;
    }

    /// <summary>
    /// The average month for one payment. A cadence with no average contributes zero.
    /// </summary>
    private static decimal Monthly(decimal payment, IncomeCadence cadence)
    {
        return PaycheckSchedule.AverageMonthlyAmount(payment, cadence) ?? 0m;
    }

    /// <summary>
    /// True when a row counts in the planning currency. A blank currency counts.
    /// </summary>
    private static bool Included(string? currency, string planningCurrency)
    {
        return PlanningCurrencyRules.IsIncluded(currency, planningCurrency);
    }

    /// <summary>
    /// Multiplies one paycheck, its low pay, and each raise by the same share.
    /// The dates stay. A later raise keeps that share of the larger check.
    /// </summary>
    private static HouseholdIncome Scale(HouseholdIncome income, decimal factor)
    {
        return income with
        {
            TypicalAmount = AccountLedger.Round(income.TypicalAmount * factor),
            LowAmount = income.LowAmount is decimal low ? AccountLedger.Round(low * factor) : null,
            Raises = income.Raises
                .Select(raise => raise with { Amount = AccountLedger.Round(raise.Amount * factor) })
                .ToList()
        };
    }

    /// <summary>
    /// The scheduled monthly pay of checks that name no person, in the planning currency.
    /// </summary>
    private static decimal UnassignedMonthly(IReadOnlyList<HouseholdIncome> incomes, string planningCurrency)
    {
        return incomes
            .Where(income => income.ContributorId is null && Scheduled(income, planningCurrency))
            .Sum(income => Monthly(income.TypicalAmount, income.Cadence));
    }

    /// <summary>
    /// One person's result. A recorded monthly of zero is stored as null when they have no scheduled pay.
    /// </summary>
    private static ContributionPerson Person(
        ContributionShareInput input,
        decimal? recorded,
        decimal shared,
        decimal kept,
        ContributionLimit limit,
        bool unscheduled)
    {
        return new ContributionPerson(
            input.ContributorId,
            input.Name,
            input.MonthlyAmount is decimal amount ? AccountLedger.Round(amount) : null,
            recorded,
            shared,
            kept,
            limit,
            unscheduled);
    }

    #endregion
}
