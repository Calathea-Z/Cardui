namespace Cardui.Api.Services;

/// <summary>
/// Shared text heuristics for transfer detection.
/// Bank-transfer phrases are safe to categorize as Transfers on a single leg.
/// Broader payment-rail hints (Venmo/Zelle/etc.) are only used when pairing
/// opposite amounts across owned accounts.
/// </summary>
public static class TransferTextClassifier
{
    private static readonly string[] BankTransferHints =
    [
        "online transfer",
        "internal transfer",
        "account transfer",
        "funds transfer",
        "transfer from",
        "transfer to",
        "xfer from",
        "xfer to",
        "x-fer",
        "from savings",
        "to savings",
        "from checking",
        "to checking",
        "from account",
        "to account",
        "move money",
    ];

    private static readonly string[] PairingHints =
    [
        ..BankTransferHints,
        "transfer",
        "xfer",
        "tfr ",
        " tfr",
        "zelle",
        "venmo",
        "paypal",
        "cash app",
        "cashapp",
        "wire",
        "credit to",
        "debit to",
    ];

    public static string BuildText(string? merchantName, string? name)
    {
        return string.Join(
            " ",
            new[] { merchantName, name }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
            .ToLowerInvariant();
    }

    public static bool LooksLikeBankTransfer(string? merchantName, string? name)
    {
        var text = BuildText(merchantName, name);
        return BankTransferHints.Any(hint => text.Contains(hint, StringComparison.Ordinal));
    }

    public static bool LooksLikeTransferPairSignal(string? merchantName, string? name)
    {
        var text = BuildText(merchantName, name);
        return PairingHints.Any(hint => text.Contains(hint, StringComparison.Ordinal));
    }
}
