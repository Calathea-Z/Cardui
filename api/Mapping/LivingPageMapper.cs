using Cardui.Api.Domain.Living;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Savings;

namespace Cardui.Api.Mapping;

public static class LivingPageMapper
{
    /// <summary>
    /// Copies the living calculation into the page the screen renders.
    /// The sentences stay on the page. This sends the amounts, the limit, and the dates.
    /// </summary>
    public static LivingPageDto Map(
        string planningCurrency,
        ContributionShareResult share,
        SavingsGoalDto? livingSpending,
        IReadOnlyList<SavingsAccountDto> accounts,
        LivingGap gap)
    {
        return new LivingPageDto
        {
            PlanningCurrency = planningCurrency,
            Contributions = share.People.Select(MapPerson).ToList(),
            UnassignedIncome = share.UnassignedNames,
            LivingSpending = livingSpending,
            Accounts = accounts,
            Gap = MapGap(gap)
        };
    }

    #region Private Methods

    /// <summary>
    /// Copies one person's contribution. A null monthly amount means it is not set.
    /// </summary>
    private static LivingContributionDto MapPerson(ContributionPerson person)
    {
        return new LivingContributionDto
        {
            ContributorId = person.ContributorId,
            Name = person.Name,
            MonthlyAmount = person.MonthlyAmount,
            RecordedMonthly = person.RecordedMonthly,
            SharedMonthly = person.SharedMonthly,
            KeptMonthly = person.KeptMonthly,
            Limit = person.Limit,
            HasUnscheduledPay = person.HasUnscheduledPay
        };
    }

    /// <summary>
    /// Copies the monthly picture. Shortfall is zero when the shared pay covers the listed amounts.
    /// </summary>
    private static LivingGapDto MapGap(LivingGap gap)
    {
        return new LivingGapDto
        {
            SharedMonthly = gap.SharedMonthly,
            BillsMonthly = gap.BillsMonthly,
            MinimumsMonthly = gap.MinimumsMonthly,
            LivingSpendingMonthly = gap.LivingSpendingMonthly,
            Shortfall = gap.Shortfall,
            LeftOut = gap.LeftOut
        };
    }

    #endregion
}
