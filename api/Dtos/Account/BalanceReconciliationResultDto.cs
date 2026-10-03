namespace Cardui.Api.Dtos.Account;

public class BalanceReconciliationResultDto
{
    public required AccountDto Account { get; set; }

    public decimal CalculatedBalance { get; set; }

    public decimal StatementBalance { get; set; }

    public decimal Adjustment { get; set; }

    public Guid? AdjustmentTransactionId { get; set; }
}
