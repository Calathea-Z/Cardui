namespace Cardui.Api.Dtos.Debts;

/// <summary>
/// A balance the person is keeping while a debt follows an account.
/// A null date means today in the household time zone. Update balance omits the date.
/// </summary>
public class SetDebtBalanceOverrideDto
{
    public decimal? Balance { get; set; }

    /// <summary>
    /// The date this balance was true. Null means today in the household time zone.
    /// </summary>
    public DateOnly? BalanceAsOf { get; set; }
}
