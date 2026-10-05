using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Obligations;

public class UpsertObligationDto
{
    public string? Name { get; set; }

    public decimal Amount { get; set; }

    public ObligationCadence? Cadence { get; set; }

    public DateOnly NextDueDate { get; set; }

    public Guid? AccountId { get; set; }

    public ObligationFlexibility? Flexibility { get; set; }
}
