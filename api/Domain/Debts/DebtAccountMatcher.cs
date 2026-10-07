using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Ranks connected accounts that look like a debt.
/// The caller passes only accounts the debt is allowed to follow.
/// </summary>
public static class DebtAccountMatcher
{
    private const int SuggestionLimit = 3;
    private const int MinimumWordLength = 3;
    private const decimal CloseFloor = 50m;
    private const decimal CloseShare = 0.05m;

    private static readonly HashSet<string> IgnoredWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and",
        "the",
        "for",
        "card",
        "cards",
        "credit",
        "loan",
        "loans",
        "account",
        "accounts",
        "bank"
    };

    /// <summary>
    /// Returns up to three accounts that share a signal with the debt, strongest first.
    /// A revolving debt matches a credit card. An installment debt matches a loan.
    /// A signal is the account's last digits in the debt name, a distinctive word from the
    /// account name, official name, or institution, or a latest dated balance that is close.
    /// Close means the amounts differ by at most $50, or by 5 percent of the larger amount when that is more.
    /// The last digits rank first, then more shared words, then a smaller balance gap.
    /// An account with no signal is left out.
    /// </summary>
    public static IReadOnlyList<DebtAccountSuggestion> Suggest(
        DebtKind kind,
        string debtName,
        decimal? debtBalance,
        IReadOnlyList<DebtMatchCandidate> accounts)
    {
        var ranked = new List<DebtRankedMatch>();
        foreach (var account in accounts)
        {
            if (!KindMatches(kind, account.AccountType))
            {
                continue;
            }

            var reasons = ReasonsFor(debtName, debtBalance, account);
            if (reasons.Count == 0)
            {
                continue;
            }

            ranked.Add(new DebtRankedMatch(account, reasons));
        }

        return ranked
            .OrderByDescending(match => HasMask(match.Reasons))
            .ThenByDescending(match => WordCount(match.Reasons))
            .ThenBy(match => Gap(match.Reasons))
            .ThenBy(match => match.Account.Name, StringComparer.OrdinalIgnoreCase)
            .Take(SuggestionLimit)
            .Select((match, index) => new DebtAccountSuggestion(
                match.Account.AccountId,
                index + 1,
                match.Reasons))
            .ToList();
    }

    #region Private Methods

    /// <summary>
    /// True when the debt's kind is the same kind of account.
    /// A card is not suggested for a loan, and a loan is not suggested for a card.
    /// </summary>
    private static bool KindMatches(DebtKind kind, string accountType)
    {
        return kind == DebtKind.Revolving
            ? AccountTypes.IsCreditCard(accountType)
            : AccountTypes.IsLoan(accountType);
    }

    /// <summary>
    /// Collects the signals for one account, in the order they are shown.
    /// Digits, then shared words, then the balance gap.
    /// </summary>
    private static List<DebtMatchReason> ReasonsFor(
        string debtName,
        decimal? debtBalance,
        DebtMatchCandidate account)
    {
        var reasons = new List<DebtMatchReason>();
        var mask = MaskReason(debtName, account.Mask);
        if (mask is not null)
        {
            reasons.Add(mask);
        }

        var name = NameReason(debtName, account);
        if (name is not null)
        {
            reasons.Add(name);
        }

        var balance = BalanceReason(debtBalance, account.DatedBalance);
        if (balance is not null)
        {
            reasons.Add(balance);
        }

        return reasons;
    }

    /// <summary>
    /// The last digits when they appear in the debt name as their own number.
    /// A mask inside a longer run of digits does not count. Fewer than two digits does not count.
    /// </summary>
    private static DebtMatchReason? MaskReason(string debtName, string? mask)
    {
        var digits = Digits(mask);
        if (digits.Length < 2 || !ContainsDigitRun(debtName, digits))
        {
            return null;
        }

        return new DebtMatchReason(DebtMatchReasonKind.Mask, digits, [], null);
    }

    /// <summary>
    /// The distinctive words the debt name shares with the account, its official name, or its institution.
    /// Short words and generic words such as "card" are skipped. The spelling is the debt's.
    /// </summary>
    private static DebtMatchReason? NameReason(string debtName, DebtMatchCandidate account)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var words = new List<string>();
        AddWords(words, seen, debtName, account.Name);
        AddWords(words, seen, debtName, account.OfficialName);
        AddWords(words, seen, debtName, account.InstitutionName);
        if (words.Count == 0)
        {
            return null;
        }

        return new DebtMatchReason(DebtMatchReasonKind.Name, null, words, null);
    }

    /// <summary>
    /// The balance gap when both amounts are known and close.
    /// The gap is absolute and rounded to cents. A missing dated balance is not a signal.
    /// </summary>
    private static DebtMatchReason? BalanceReason(decimal? debtBalance, decimal? datedBalance)
    {
        if (debtBalance is not decimal owed || datedBalance is not decimal latest)
        {
            return null;
        }

        var gap = AccountLedger.Round(Math.Abs(owed - latest));
        var larger = Math.Max(Math.Abs(AccountLedger.Round(owed)), Math.Abs(AccountLedger.Round(latest)));
        var limit = Math.Max(CloseFloor, AccountLedger.Round(larger * CloseShare));
        if (gap > limit)
        {
            return null;
        }

        return new DebtMatchReason(DebtMatchReasonKind.Balance, null, [], gap);
    }

    /// <summary>
    /// Adds each distinctive word from a source that appears as its own word in the debt name.
    /// A word already added from an earlier source is skipped.
    /// </summary>
    private static void AddWords(
        List<string> words,
        HashSet<string> seen,
        string debtName,
        string? source)
    {
        foreach (var word in LetterWords(source))
        {
            if (IgnoredWords.Contains(word) || !seen.Add(word))
            {
                continue;
            }

            var spelled = WordInDebt(debtName, word);
            if (spelled is null)
            {
                seen.Remove(word);
                continue;
            }

            words.Add(spelled);
        }
    }

    /// <summary>
    /// The letter runs in a name that are long enough to be distinctive.
    /// A run shorter than three letters is skipped.
    /// </summary>
    private static IEnumerable<string> LetterWords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        var start = -1;
        for (var index = 0; index <= value.Length; index++)
        {
            var letter = index < value.Length && char.IsLetter(value[index]);
            if (letter && start < 0)
            {
                start = index;
            }
            else if (!letter && start >= 0)
            {
                if (index - start >= MinimumWordLength)
                {
                    yield return value.Substring(start, index - start);
                }

                start = -1;
            }
        }
    }

    /// <summary>
    /// The debt's spelling of a word, when that word stands alone in the name.
    /// A match inside a longer word does not count.
    /// </summary>
    private static string? WordInDebt(string debtName, string word)
    {
        for (var index = 0; index <= debtName.Length - word.Length; index++)
        {
            if (index > 0 && char.IsLetter(debtName[index - 1]))
            {
                continue;
            }

            var end = index + word.Length;
            if (end < debtName.Length && char.IsLetter(debtName[end]))
            {
                continue;
            }

            if (string.Compare(
                    debtName,
                    index,
                    word,
                    0,
                    word.Length,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return debtName.Substring(index, word.Length);
            }
        }

        return null;
    }

    /// <summary>
    /// The digits in a mask, in order. Letters and punctuation are dropped.
    /// </summary>
    private static string Digits(string? mask)
    {
        if (string.IsNullOrWhiteSpace(mask))
        {
            return "";
        }

        return new string(mask.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// True when the digits appear in the name bounded by something that is not a digit.
    /// </summary>
    private static bool ContainsDigitRun(string debtName, string digits)
    {
        var index = 0;
        while ((index = debtName.IndexOf(digits, index, StringComparison.Ordinal)) >= 0)
        {
            var before = index == 0 || !char.IsDigit(debtName[index - 1]);
            var after = index + digits.Length >= debtName.Length
                || !char.IsDigit(debtName[index + digits.Length]);
            if (before && after)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    /// <summary>
    /// True when the reasons include the account's last digits.
    /// </summary>
    private static bool HasMask(IReadOnlyList<DebtMatchReason> reasons)
    {
        return reasons.Any(reason => reason.Kind == DebtMatchReasonKind.Mask);
    }

    /// <summary>
    /// How many distinctive words the reasons share with the debt name.
    /// </summary>
    private static int WordCount(IReadOnlyList<DebtMatchReason> reasons)
    {
        return reasons
            .Where(reason => reason.Kind == DebtMatchReasonKind.Name)
            .Sum(reason => reason.Words.Count);
    }

    /// <summary>
    /// The balance gap, or a value past every real gap when the balance was not a signal.
    /// A smaller gap ranks first.
    /// </summary>
    private static decimal Gap(IReadOnlyList<DebtMatchReason> reasons)
    {
        return reasons
            .Where(reason => reason.Kind == DebtMatchReasonKind.Balance)
            .Select(reason => reason.Difference ?? decimal.MaxValue)
            .DefaultIfEmpty(decimal.MaxValue)
            .First();
    }

    #endregion
}
