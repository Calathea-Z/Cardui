namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Terms the person entered for one offer.
/// A blank field stays null so the comparison can ask for it. Nothing here is saved or treated as approved.
/// </summary>
public sealed record RestructuringOffer(
    RestructuringOfferKind? Kind,
    decimal? Apr,
    int? TermMonths,
    decimal? Fee,
    RestructuringFeeFunding? FeeFunding,
    decimal? UpfrontCash);
