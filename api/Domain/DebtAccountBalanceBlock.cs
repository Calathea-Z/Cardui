namespace Cardui.Api.Domain;

/// <summary>
/// Why a linked account balance is shown but not copied onto the debt.
/// None means the person can choose that balance. The plan does not copy it on its own.
/// </summary>
public enum DebtAccountBalanceBlock
{
    None,
    DateUnknown,
    NegativeBalance,
    CurrencyDiffers,
    AmountTooLarge
}
