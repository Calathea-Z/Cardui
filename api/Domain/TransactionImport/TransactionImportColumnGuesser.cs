using System.Text;

namespace Cardui.Api.Domain.TransactionImport;

public static class TransactionImportColumnGuesser
{
    /// <summary>
    /// Picks likely columns from header text. The first matching header wins
    /// for each field. A lone Memo header is treated as the name.
    /// </summary>
    public static TransactionImportSuggestedColumns Guess(IReadOnlyList<string> headers)
    {
        int? date = null;
        int? name = null;
        int? amount = null;
        int? debit = null;
        int? credit = null;
        int? category = null;
        int? notes = null;

        for (var index = 0; index < headers.Count; index++)
        {
            var header = Normalize(headers[index]);
            if (header.Length == 0)
            {
                continue;
            }

            if (date is null && IsDate(header))
            {
                date = index;
            }
            else if (amount is null && IsAmount(header))
            {
                amount = index;
            }
            else if (debit is null && IsDebit(header))
            {
                debit = index;
            }
            else if (credit is null && IsCredit(header))
            {
                credit = index;
            }
            else if (category is null && IsCategory(header))
            {
                category = index;
            }
            else if (name is null && IsName(header))
            {
                name = index;
            }
            else if (notes is null && IsNotes(header))
            {
                notes = index;
            }
        }

        if (name is null && notes is int notesIndex && Normalize(headers[notesIndex]) == "memo")
        {
            name = notesIndex;
            notes = null;
        }

        return new TransactionImportSuggestedColumns(
            date,
            name,
            amount,
            debit,
            credit,
            category,
            notes);
    }

    #region Private Methods

    /// <summary>
    /// Lowercases a header and keeps letters as words.
    /// </summary>
    private static string Normalize(string header)
    {
        var builder = new StringBuilder();
        var previousSpace = false;
        foreach (var character in header.Trim().ToLowerInvariant())
        {
            if (char.IsLetter(character))
            {
                builder.Append(character);
                previousSpace = false;
                continue;
            }

            if ((char.IsWhiteSpace(character) || character is '_' or '-' or '/')
                && !previousSpace
                && builder.Length > 0)
            {
                builder.Append(' ');
                previousSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// True for a posted-date header.
    /// </summary>
    private static bool IsDate(string header) =>
        header is "date" or "posted" or "posted date" or "post date"
            or "posting date" or "transaction date" or "trans date";

    /// <summary>
    /// True for a description header.
    /// </summary>
    private static bool IsName(string header) =>
        header is "description" or "name" or "merchant" or "payee"
            or "transaction description" or "original description";

    /// <summary>
    /// True for a single amount header.
    /// </summary>
    private static bool IsAmount(string header) =>
        header is "amount" or "transaction amount" or "amt";

    /// <summary>
    /// True for a money-out header.
    /// </summary>
    private static bool IsDebit(string header) =>
        header is "debit" or "debit amount" or "withdrawal" or "withdrawals";

    /// <summary>
    /// True for a money-in header. "Credit card" does not match.
    /// </summary>
    private static bool IsCredit(string header) =>
        header is "credit" or "credit amount" or "deposit" or "deposits";

    /// <summary>
    /// True for a category header.
    /// </summary>
    private static bool IsCategory(string header) =>
        header is "category" or "category name";

    /// <summary>
    /// True for a notes header.
    /// </summary>
    private static bool IsNotes(string header) =>
        header is "notes" or "note" or "memo" or "transaction memo";

    #endregion
}
