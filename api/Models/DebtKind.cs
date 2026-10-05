namespace Cardui.Api.Models;

/// <summary>
/// Whether a debt revolves or is paid down on a set term.
/// Revolving is a card or line of credit. Installment is a loan with a remaining term.
/// The stored column and the API use the member name.
/// </summary>
public enum DebtKind
{
    Revolving,
    Installment
}
