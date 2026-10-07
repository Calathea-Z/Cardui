using Cardui.Api.Domain.Categories;

namespace Cardui.Api.Domain.Transactions;

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

    private static readonly (string CategoryKey, string[] Keywords)[] MerchantKeywordRules =
    [
        (SystemCategoryKeys.FoodDining,
        [
            "whole foods",
            "trader joe",
            "kroger",
            "safeway",
            "grocery",
            "market",
            "restaurant",
            "cafe",
            "coffee",
            "starbucks",
            "doordash",
            "uber eats",
            "chipotle"
        ]),
        (SystemCategoryKeys.AutoTransport,
        [
            "shell",
            "chevron",
            "exxon",
            "gas",
            "uber",
            "lyft",
            "parking"
        ]),
        (SystemCategoryKeys.TravelLifestyle,
        [
            "netflix",
            "spotify",
            "hulu",
            "disney",
            "amc",
            "cinema",
            "ticket"
        ]),
        (SystemCategoryKeys.Shopping,
        [
            "target",
            "amazon",
            "walmart",
            "costco",
            "best buy"
        ]),
        (SystemCategoryKeys.BillsUtilities,
        [
            "electric",
            "utility",
            "internet",
            "phone",
            "insurance",
            "rent",
            "mortgage",
            "t-mobile",
            "tmobile",
            "verizon",
            "at&t",
            "att "
        ])
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

        foreach (var (categoryKey, keywords) in MerchantKeywordRules)
        {
            if (ContainsAny(text, keywords))
            {
                return categoryKey;
            }
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
