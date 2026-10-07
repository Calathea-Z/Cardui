using Cardui.Api.Domain;

namespace Cardui.Api.Models;

public class Household
{
    public const int OwnerClerkUserIdMaxLength = 128;
    public const int DisplayNameMaxLength = 200;

    public Guid Id { get; init; }

    public required string OwnerClerkUserId { get; init; }

    public required string DisplayName { get; set; }

    public string PlanningCurrency { get; set; } = PlanningCurrencyRules.DefaultCode;

    public string TimeZoneId { get; set; } = HouseholdTime.DefaultTimeZoneId;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<HouseholdContributor> Contributors { get; init; } =
        new List<HouseholdContributor>();

    public ICollection<IncomeSource> IncomeSources { get; init; } =
        new List<IncomeSource>();

    public ICollection<Obligation> Obligations { get; init; } =
        new List<Obligation>();

    public ICollection<Debt> Debts { get; init; } =
        new List<Debt>();

    public ICollection<CategoryTargetMonth> CategoryTargetMonths { get; init; } =
        new List<CategoryTargetMonth>();
}
