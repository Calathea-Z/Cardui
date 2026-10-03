namespace Cardui.Api.Dtos.Account;

public class ReconcileAccountBalanceDto
{
    public DateOnly AsOfDate { get; set; }

    public decimal StatementBalance { get; set; }
}
