using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Obligations;

public class ObligationSuggestionDto
{
    public required string Key { get; set; }

    public required string Name { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public ObligationCadence Cadence { get; set; }

    public DateOnly NextDueDate { get; set; }

    public Guid? AccountId { get; set; }

    public string? AccountName { get; set; }
}
