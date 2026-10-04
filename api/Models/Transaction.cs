namespace Cardui.Api.Models;

public class Transaction
{
    public Guid Id { get; init; }

    public Guid AccountId { get; set; }
    public Account Account { get; init; } = null!;

    public string? PlaidTransactionId { get; set; }

    public FinancialRecordSource Source { get; set; } = FinancialRecordSource.Plaid;

    public FinancialRecordProvenance Provenance { get; set; } = FinancialRecordProvenance.PlaidSync;

    public Guid? ImportId { get; set; }

    public DateOnly Date { get; set; }
    public bool IsDateUserEdited { get; set; }
    public DateOnly? AuthorizedDate { get; set; }

    public required string Name { get; set; }
    public string? MerchantName { get; set; }

    public decimal Amount { get; set; }

    public string? IsoCurrencyCode { get; set; }
    public bool Pending { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; init; }
    public bool IsCategoryUserEdited { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}