namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Which income amounts a forecast was given.
/// Conservative is the low payment. Typical is the usual take-home payment.
/// The forecast records the basis. It does not choose one.
/// </summary>
public enum ForecastIncomeBasis
{
    Conservative,
    Typical
}
