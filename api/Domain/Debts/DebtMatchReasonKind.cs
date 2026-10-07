namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Why an account was suggested for a debt.
/// The stored value and the API use the member name. No score is shown.
/// </summary>
public enum DebtMatchReasonKind
{
    Mask,
    Name,
    Balance
}
