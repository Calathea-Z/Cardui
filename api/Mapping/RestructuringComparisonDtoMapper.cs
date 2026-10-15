using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Mapping;

public static class RestructuringComparisonDtoMapper
{
    /// <summary>
    /// Copies one comparison into the shape Plan renders.
    /// The page writes its own sentences. This does not add a rank or an approval.
    /// </summary>
    public static RestructuringComparisonDto Map(RestructuringComparison comparison)
    {
        return new RestructuringComparisonDto
        {
            Kind = comparison.Kind,
            Debts = comparison.Debts.Select(MapDebt).ToList(),
            CoveredBalance = comparison.CoveredBalance,
            Fee = comparison.Fee,
            FeeFunding = comparison.FeeFunding,
            UpfrontCash = comparison.UpfrontCash,
            CashDueNow = comparison.CashDueNow,
            NewPrincipal = comparison.NewPrincipal,
            OfferApr = comparison.OfferApr,
            TermMonths = comparison.TermMonths,
            OfferPaymentCount = comparison.OfferPaymentCount,
            OfferMonthlyPayment = comparison.OfferMonthlyPayment,
            KeepMonthlyPayment = comparison.KeepMonthlyPayment,
            MonthlyCashFlow = comparison.MonthlyCashFlow,
            KeepTotalInterest = comparison.KeepTotalInterest,
            OfferTotalInterest = comparison.OfferTotalInterest,
            KeepTotalCost = comparison.KeepTotalCost,
            OfferTotalCost = comparison.OfferTotalCost,
            KeepPaidOffOn = comparison.KeepPaidOffOn,
            OfferPaidOffOn = comparison.OfferPaidOffOn,
            OfferStop = comparison.OfferStop,
            BreakEven = comparison.BreakEven,
            BreakEvenOn = comparison.BreakEvenOn,
            Cash = MapCash(comparison.Cash)
        };
    }

    #region Private Methods

    /// <summary>
    /// Copies one replaced debt, including a promotional rate that stays separate from the stored APR.
    /// </summary>
    private static RestructuringDebtOutcomeDto MapDebt(RestructuringDebtOutcome debt)
    {
        return new RestructuringDebtOutcomeDto
        {
            DebtId = debt.DebtId,
            Name = debt.Name,
            Kind = debt.Kind,
            Balance = debt.Balance,
            Apr = debt.Apr,
            PromotionalApr = debt.PromotionalApr,
            PromotionalEndsOn = debt.PromotionalEndsOn,
            Stop = debt.Stop,
            PaidOffOn = debt.PaidOffOn
        };
    }

    /// <summary>
    /// Copies the cash effect. A financed fee stays out of cash due.
    /// </summary>
    private static RestructuringCashEffectDto MapCash(RestructuringCashEffect cash)
    {
        return new RestructuringCashEffectDto
        {
            CashOnHand = cash.CashOnHand,
            Reserve = cash.Reserve,
            CashToKeep = cash.CashToKeep,
            HasCashFloor = cash.HasCashFloor,
            ExceedsCashOnHand = cash.ExceedsCashOnHand,
            UsesReserve = cash.UsesReserve,
            LeavesLessThanCashToKeep = cash.LeavesLessThanCashToKeep
        };
    }

    #endregion
}
