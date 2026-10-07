namespace Cardui.Api.Dtos.Debts;

/// <summary>
/// The account a debt should follow, and whether the person's balance stays as their own value.
/// KeepOwnBalance is used only when that balance differs from the connected one.
/// </summary>
public class FollowDebtAccountDto
{
    public Guid AccountId { get; set; }

    public bool KeepOwnBalance { get; set; }
}
