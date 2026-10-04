using Cardui.Api.Domain;

namespace Cardui.Api.Services;

public static class TransactionCategoryClassifier
{
    private static readonly string[] IncomeHints =
    [
        "payroll",
        "paycheck",
        "direct deposit",
        "salary",
        "wages",
        "social security",
        "pension",
        "interest earned",
        "dividend"
    ];

    /// <summary>
    /// Chooses a system category key. Bank transfers win first, then income
    /// hints on money coming in, then merchant keywords. Anything else is Other.
    /// </summary>
    public static string GetCategoryKey(
        string? merchantName,
        string? description,
        decimal? amount)
    {
        var text = TransferTextClassifier.BuildText(merchantName, description);

        if (TransferTextClassifier.LooksLikeBankTransfer(merchantName, description))
        {
            return SystemCategoryKeys.Transfers;
        }

        if (amount < 0 && ContainsAny(text, IncomeHints))
        {
            return SystemCategoryKeys.Income;
        }

        if (ContainsAny(text, "whole foods", "trader joe", "kroger", "safeway", "grocery", "market"))
        {
            return SystemCategoryKeys.FoodDining;
        }

        if (ContainsAny(text, "restaurant", "cafe", "coffee", "starbucks", "doordash", "uber eats", "chipotle"))
        {
            return SystemCategoryKeys.FoodDining;
        }

        if (ContainsAny(text, "shell", "chevron", "exxon", "gas", "uber", "lyft", "parking"))
        {
            return SystemCategoryKeys.AutoTransport;
        }

        if (ContainsAny(text, "netflix", "spotify", "hulu", "disney", "amc", "cinema", "ticket"))
        {
            return SystemCategoryKeys.TravelLifestyle;
        }

        if (ContainsAny(text, "target", "amazon", "walmart", "costco", "best buy"))
        {
            return SystemCategoryKeys.Shopping;
        }

        if (ContainsAny(text, "electric", "utility", "internet", "phone", "insurance", "rent", "mortgage", "t-mobile", "tmobile", "verizon", "at&t", "att "))
        {
            return SystemCategoryKeys.BillsUtilities;
        }

        return SystemCategoryKeys.Other;
    }

    #region Private Methods

    /// <summary>
    /// True when the text contains any of the keywords.
    /// </summary>
    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }

    #endregion
}
