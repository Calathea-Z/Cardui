using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Labels a planning input and refuses to treat anything else as a saved bill.
/// A recurring pattern stays a suggestion. A monthly average stays an estimate with no date.
/// A note stays words. The label is not stored, and nothing is saved.
/// </summary>
public static class PlanningFactBasisRules
{
    /// <summary>
    /// Labels one input.
    /// A missing source, a blank name, or an unknown kind is rejected. The amount on a note, a date, or a match is dropped.
    /// A monthly average loses any date it was given. Confirmed is set only for a saved bill.
    /// </summary>
    public static bool TryDescribe(
        PlanningFactSubject subject,
        out PlanningFact? fact,
        out string error)
    {
        var rejection = Reject(subject);
        if (rejection is not null)
        {
            fact = null;
            error = rejection;
            return false;
        }

        var kind = subject.Kind;
        var basis = BasisFor(subject);
        fact = new PlanningFact(
            kind,
            basis,
            UnitFor(kind),
            IsConfirmedObligation(basis, kind),
            subject.SubjectId,
            subject.RelatedId,
            subject.Name.Trim(),
            Detail(subject.Detail),
            AmountFor(subject),
            subject.Currency,
            AsOfFor(subject),
            Explanation(kind, basis));
        error = "";
        return true;
    }

    /// <summary>
    /// True only when the person saved a bill.
    /// A suggestion, an estimate, an imported amount, and every other statement stay false.
    /// </summary>
    public static bool IsConfirmedObligation(PlanningFactBasis basis, PlanningFactKind kind)
    {
        return basis == PlanningFactBasis.UserStatement && kind == PlanningFactKind.Bill;
    }

    /// <summary>
    /// Rejects treating this fact as a new confirmed bill.
    /// A bill the person already saved is allowed through, and this method does not save another one.
    /// </summary>
    public static string? RejectAsObligation(PlanningFactBasis basis, PlanningFactKind kind)
    {
        if (!Enum.IsDefined(basis) || !Enum.IsDefined(kind))
        {
            return "Choose a fact.";
        }

        if (IsConfirmedObligation(basis, kind))
        {
            return null;
        }

        if (kind == PlanningFactKind.RecurringExpense)
        {
            return "An inferred expense is not a bill until it is saved.";
        }

        if (kind == PlanningFactKind.DebtMatch)
        {
            return "A suggested account is not followed until the person chooses.";
        }

        return basis switch
        {
            PlanningFactBasis.Estimate => "An estimate is not a confirmed bill.",
            PlanningFactBasis.ImportedFact => "An imported amount is not a bill until it is saved.",
            PlanningFactBasis.UnverifiedSuggestion =>
                "An inferred expense is not a bill until it is saved.",
            _ => "That fact is not a bill."
        };
    }

    #region Private Methods

    /// <summary>
    /// Rejects an input that cannot be labeled.
    /// Cash needs the account source. A debt balance or credit limit needs the field source.
    /// </summary>
    private static string? Reject(PlanningFactSubject subject)
    {
        if (!Enum.IsDefined(subject.Kind))
        {
            return "Choose a fact.";
        }

        if (string.IsNullOrWhiteSpace(subject.Name))
        {
            return "Name this fact.";
        }

        if (subject.Kind == PlanningFactKind.CashBalance && !Known(subject.RecordSource))
        {
            return "Choose where this amount came from.";
        }

        if (NeedsFieldSource(subject.Kind) && !Known(subject.FieldSource))
        {
            return "Choose where this amount came from.";
        }

        return null;
    }

    /// <summary>
    /// True when the account source is one Cardui records.
    /// A missing source cannot be labeled as the person's figure or as an import.
    /// </summary>
    private static bool Known(FinancialRecordSource? source)
    {
        return source is FinancialRecordSource value && Enum.IsDefined(value);
    }

    /// <summary>
    /// True when the debt field source is one Cardui records.
    /// A missing source cannot be labeled as the person's figure or as an import.
    /// </summary>
    private static bool Known(DebtFieldSource? source)
    {
        return source is DebtFieldSource value && Enum.IsDefined(value);
    }

    /// <summary>
    /// True for a debt balance or credit limit, which can come from the person or from a connection.
    /// </summary>
    private static bool NeedsFieldSource(PlanningFactKind kind)
    {
        return kind is PlanningFactKind.DebtBalance or PlanningFactKind.DebtCreditLimit;
    }

    /// <summary>
    /// The basis for an input that has already been checked.
    /// A suggestion stays unverified even when the charges were imported or typed.
    /// </summary>
    private static PlanningFactBasis BasisFor(PlanningFactSubject subject)
    {
        return subject.Kind switch
        {
            PlanningFactKind.RecurringExpense or PlanningFactKind.DebtMatch =>
                PlanningFactBasis.UnverifiedSuggestion,
            PlanningFactKind.IncomeMonthlyAverage => PlanningFactBasis.Estimate,
            PlanningFactKind.CashBalance => BasisForRecord(subject.RecordSource!.Value),
            PlanningFactKind.DebtBalance or PlanningFactKind.DebtCreditLimit =>
                BasisForField(subject.FieldSource!.Value),
            PlanningFactKind.IncomePayment
                or PlanningFactKind.Bill
                or PlanningFactKind.DebtMinimum
                or PlanningFactKind.DebtDueDate
                or PlanningFactKind.DebtApr
                or PlanningFactKind.LivingSpending
                or PlanningFactKind.ConversationNote => PlanningFactBasis.UserStatement,
            _ => throw new InvalidOperationException("Choose a fact.")
        };
    }

    /// <summary>
    /// A file or a connected account is imported. An account the person created is their statement.
    /// </summary>
    private static PlanningFactBasis BasisForRecord(FinancialRecordSource source)
    {
        return source == FinancialRecordSource.Manual
            ? PlanningFactBasis.UserStatement
            : PlanningFactBasis.ImportedFact;
    }

    /// <summary>
    /// A synced debt amount is imported. The person's own amount, including an override, is their statement.
    /// </summary>
    private static PlanningFactBasis BasisForField(DebtFieldSource source)
    {
        return source == DebtFieldSource.Synced
            ? PlanningFactBasis.ImportedFact
            : PlanningFactBasis.UserStatement;
    }

    /// <summary>
    /// Percent is only the interest rate. A date, a note, and a match have no amount.
    /// </summary>
    private static PlanningFactUnit UnitFor(PlanningFactKind kind)
    {
        return kind switch
        {
            PlanningFactKind.DebtApr => PlanningFactUnit.Percent,
            PlanningFactKind.DebtDueDate
                or PlanningFactKind.DebtMatch
                or PlanningFactKind.ConversationNote => PlanningFactUnit.None,
            _ => PlanningFactUnit.Money
        };
    }

    /// <summary>
    /// Drops an amount that would turn a date, a note, or a match into money.
    /// A zero the person entered stays zero. A suggestion keeps its typical payment.
    /// </summary>
    private static decimal? AmountFor(PlanningFactSubject subject)
    {
        return subject.Kind is PlanningFactKind.DebtDueDate
            or PlanningFactKind.DebtMatch
            or PlanningFactKind.ConversationNote
            ? null
            : subject.Amount;
    }

    /// <summary>
    /// Drops a date on a monthly average so it cannot be read as cash on that date.
    /// A note and a match have no date either.
    /// </summary>
    private static DateOnly? AsOfFor(PlanningFactSubject subject)
    {
        return subject.Kind is PlanningFactKind.IncomeMonthlyAverage
            or PlanningFactKind.DebtMatch
            or PlanningFactKind.ConversationNote
            ? null
            : subject.AsOf;
    }

    /// <summary>
    /// The note, suggestion key, or account name, with surrounding space removed.
    /// Blank stays empty so it cannot be read as a value.
    /// </summary>
    private static string? Detail(string? detail)
    {
        var text = detail?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// Says how the fact may be used. A suggestion is not a bill, and a note is not an amount.
    /// </summary>
    private static string Explanation(PlanningFactKind kind, PlanningFactBasis basis)
    {
        if (kind == PlanningFactKind.RecurringExpense)
        {
            return "This is a payment pattern noticed in activity. The amount is one typical payment. It is not a bill until the person saves it.";
        }

        if (kind == PlanningFactKind.DebtMatch)
        {
            return "This account looks like the debt. It is not followed until the person chooses.";
        }

        if (kind == PlanningFactKind.IncomeMonthlyAverage)
        {
            return "This is a yearly average spread across months. It is not cash on a date.";
        }

        if (kind == PlanningFactKind.ConversationNote)
        {
            return "The person wrote this. It is not an amount, and unknown does not mean zero.";
        }

        if (basis == PlanningFactBasis.ImportedFact)
        {
            return "This amount came from a connected account or a file.";
        }

        if (kind == PlanningFactKind.Bill)
        {
            return "The person saved this bill.";
        }

        return "The person entered this.";
    }

    #endregion
}
