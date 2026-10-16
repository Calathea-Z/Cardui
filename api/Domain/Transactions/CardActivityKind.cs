namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// How one transaction affects category spending, cash, and card debt.
/// The member name is the text a later response would store and return.
/// </summary>
public enum CardActivityKind
{
    None,
    Purchase,
    Refund,
    CardPayment
}
