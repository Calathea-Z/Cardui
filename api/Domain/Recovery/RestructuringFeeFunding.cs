namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Where the offer's fee is paid.
/// Financed adds it to the new balance. Cash pays it from cash and does not add it to the balance.
/// </summary>
public enum RestructuringFeeFunding
{
    Financed,
    Cash
}
