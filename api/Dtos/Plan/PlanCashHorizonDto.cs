namespace Cardui.Api.Dtos.Plan;

public class PlanCashHorizonDto
{
    public int Months { get; set; }

    public required PlanCashWindowDto Window { get; set; }

    /// <summary>
    /// Known monthly minimums still due at the end of the horizon. Null when every remaining debt is missing a term.
    /// </summary>
    public decimal? MinimumObligation { get; set; }

    /// <summary>
    /// Remaining debts whose minimum could not be included.
    /// </summary>
    public int UnknownMinimumCount { get; set; }
}
