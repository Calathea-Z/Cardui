namespace Cardui.Api.Exceptions;

using Microsoft.AspNetCore.Http;

public sealed class PlanningModelNotConfiguredException : AppException
{
    public PlanningModelNotConfiguredException()
        : base("Guided conversation is not configured.")
    {
    }

    /// <inheritdoc />
    public override int StatusCode => StatusCodes.Status503ServiceUnavailable;

    /// <inheritdoc />
    public override string Title => "Planning Model Not Configured";
}
