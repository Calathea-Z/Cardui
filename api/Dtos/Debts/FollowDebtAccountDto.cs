namespace Cardui.Api.Dtos.Debts;

/// <summary>
/// The account a debt should follow, and whether a different balance or credit limit stays as the person's value.
/// Each flag is used only when that amount differs from the connected one.
/// </summary>
public class FollowDebtAccountDto
{
    public Guid AccountId { get; set; }

    public bool KeepOwnBalance { get; set; }

    public bool KeepOwnCreditLimit { get; set; }
}
