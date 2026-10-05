namespace Cardui.Api.Models;

/// <summary>
/// Whether a bill has to be paid.
/// Essential has to be paid. Flexible can be reduced or skipped.
/// The stored column and the API use the member name.
/// </summary>
public enum ObligationFlexibility
{
    Essential,
    Flexible
}
