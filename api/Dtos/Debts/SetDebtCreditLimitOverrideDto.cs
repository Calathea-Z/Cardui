namespace Cardui.Api.Dtos.Debts;

/// <summary>
/// A credit limit the person is keeping while a revolving debt follows an account.
/// The amount is required. An amount equal to the synced limit is still an override.
/// </summary>
public class SetDebtCreditLimitOverrideDto
{
    public decimal? CreditLimit { get; set; }
}
