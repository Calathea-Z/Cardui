using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// Separates a credit-card purchase from the payment that settles it.
/// A purchase counts in its category. A payment changes cash or card debt and is not spending.
/// </summary>
public static class CardActivityRules
{
    private static readonly string[] CardPaymentPhrases =
    [
        "credit card payment",
        "credit card pymt",
        "credit crd",
        "crcardpmt",
        "crcard pmt",
        "card payment",
        "card pymt",
        "card pmt",
        "card autopay",
        "card online payment",
        "cc payment",
        "cc pymt",
        "cc pmt",
        "payment thank you",
        "pymt thank you",
        "pmt thank you",
        "thank you for your payment",
        "e-payment"
    ];

    private static readonly string[] CardMarkers = ["credit card", "credit crd", "crcard"];

    private static readonly string[] PaymentMarkers = ["payment", "pymt", "pmt"];

    private static readonly string[] PaymentPhrases = ["autopay", "auto pay", "auto-pay", "e-payment"];

    /// <summary>
    /// Classifies one transaction.
    /// Pending rows and statement adjustments do nothing. A card payment is recognized from its
    /// description, or from a credit on a credit card that reads as a payment. Other transfers
    /// and income are left out. A purchase or refund on a card changes debt and leaves cash as it is.
    /// The same purchase on a cash account changes cash and leaves debt as it is.
    /// </summary>
    public static CardActivityEffect Classify(CardActivityInput input)
    {
        if (input.Pending
            || input.Amount == 0
            || input.Provenance == FinancialRecordProvenance.BalanceReconciliation)
        {
            return None();
        }

        if (IsCardPayment(input))
        {
            return Payment(input);
        }

        if (IsTransfer(input) || IsIncome(input))
        {
            return None();
        }

        return PurchaseOrRefund(input);
    }

    /// <summary>
    /// True when the description is a card payment on any account.
    /// A bill autopay without card language stays spending. A mortgage payment stays spending.
    /// </summary>
    public static bool LooksLikeCardPayment(string? merchantName, string? name)
    {
        var text = TransferTextClassifier.BuildText(merchantName, name);
        if (text.Length == 0)
        {
            return false;
        }

        if (CardPaymentPhrases.Any(phrase => text.Contains(phrase, StringComparison.Ordinal))
            || ContainsTerm(text, "epayment"))
        {
            return true;
        }

        return HasCardMarker(text) && HasPaymentMarker(text);
    }

    #region Private Methods

    /// <summary>
    /// True for a described card payment, or a credit on a card that reads as a payment.
    /// A refund on the card stays a refund.
    /// </summary>
    private static bool IsCardPayment(CardActivityInput input)
    {
        if (LooksLikeCardPayment(input.MerchantName, input.Name))
        {
            return true;
        }

        return AccountTypes.IsCreditCard(input.AccountType ?? "")
            && input.Amount < 0
            && (IsTransfer(input) || LooksLikePaymentReceived(input.MerchantName, input.Name));
    }

    /// <summary>
    /// True when a credit on a card uses payment language.
    /// The match is a whole word, so a repayment is not treated as a payment.
    /// </summary>
    private static bool LooksLikePaymentReceived(string? merchantName, string? name)
    {
        var text = TransferTextClassifier.BuildText(merchantName, name);
        if (text.Length == 0)
        {
            return false;
        }

        return HasPaymentMarker(text)
            || text.Contains("thank you", StringComparison.Ordinal);
    }

    /// <summary>
    /// True when the text names a card rather than a generic purchase.
    /// </summary>
    private static bool HasCardMarker(string text)
    {
        return CardMarkers.Any(marker => text.Contains(marker, StringComparison.Ordinal))
            || ContainsTerm(text, "crd");
    }

    /// <summary>
    /// True when the text names a payment rather than a fee or a purchase.
    /// </summary>
    private static bool HasPaymentMarker(string text)
    {
        return PaymentMarkers.Any(marker => ContainsTerm(text, marker))
            || ContainsTerm(text, "epayment")
            || PaymentPhrases.Any(phrase => text.Contains(phrase, StringComparison.Ordinal));
    }

    /// <summary>
    /// True when the term stands alone, not as part of a longer word.
    /// </summary>
    private static bool ContainsTerm(string text, string term)
    {
        var index = 0;
        while ((index = text.IndexOf(term, index, StringComparison.Ordinal)) >= 0)
        {
            var before = index == 0 || !char.IsLetter(text[index - 1]);
            var after = index + term.Length >= text.Length
                || !char.IsLetter(text[index + term.Length]);
            if (before && after)
            {
                return true;
            }

            index += term.Length;
        }

        return false;
    }

    /// <summary>
    /// A payment does not count as spending.
    /// Cash falls on a depository account. Debt falls when the amount on a card is negative.
    /// </summary>
    private static CardActivityEffect Payment(CardActivityInput input)
    {
        decimal cash = 0;
        decimal debt = 0;
        if (AccountTypes.IsCreditCard(input.AccountType ?? ""))
        {
            debt = input.Amount;
        }
        else if (AccountTypes.IsCash(input.AccountType ?? ""))
        {
            cash = -input.Amount;
        }

        return new CardActivityEffect(CardActivityKind.CardPayment, 0, cash, debt);
    }

    /// <summary>
    /// A purchase or refund counts once in its category.
    /// On a card it changes debt. On a cash account it changes cash.
    /// </summary>
    private static CardActivityEffect PurchaseOrRefund(CardActivityInput input)
    {
        var kind = input.Amount < 0 ? CardActivityKind.Refund : CardActivityKind.Purchase;
        decimal cash = 0;
        decimal debt = 0;
        if (AccountTypes.IsCreditCard(input.AccountType ?? ""))
        {
            debt = input.Amount;
        }
        else if (AccountTypes.IsCash(input.AccountType ?? ""))
        {
            cash = -input.Amount;
        }

        return new CardActivityEffect(kind, input.Amount, cash, debt);
    }

    /// <summary>
    /// An empty effect. Pending rows, adjustments, income, and non-card transfers use it.
    /// </summary>
    private static CardActivityEffect None()
    {
        return new CardActivityEffect(CardActivityKind.None, 0, 0, 0);
    }

    /// <summary>
    /// True when the category or its group is Transfers.
    /// </summary>
    private static bool IsTransfer(CardActivityInput input)
    {
        return HasKey(input.GroupKey, SystemGroupKeys.Transfers)
            || HasKey(input.CategoryKey, SystemCategoryKeys.Transfers);
    }

    /// <summary>
    /// True when the category or its group is Income.
    /// </summary>
    private static bool IsIncome(CardActivityInput input)
    {
        return HasKey(input.GroupKey, SystemGroupKeys.Income)
            || HasKey(input.CategoryKey, SystemCategoryKeys.Income);
    }

    /// <summary>
    /// Compares a category or group key without regard to case.
    /// </summary>
    private static bool HasKey(string? actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
