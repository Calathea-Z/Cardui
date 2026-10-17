using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningFacts
{
    /// <summary>
    /// Labels the signed-in household's planning inputs.
    /// A user statement is something the person entered or saved. An imported fact came from a connected account or a file.
    /// A monthly average is an estimate with no date. A recurring pattern and a suggested debt account stay unverified.
    /// Nothing is saved. A suggestion does not become a bill, and a note does not become an amount.
    /// </summary>
    Task<IReadOnlyList<PlanningFactDto>> DescribeAsync(
        CancellationToken cancellationToken = default);
}
