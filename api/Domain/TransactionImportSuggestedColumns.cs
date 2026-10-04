namespace Cardui.Api.Domain;

public sealed record TransactionImportSuggestedColumns(
    int? DateColumn,
    int? NameColumn,
    int? AmountColumn,
    int? DebitColumn,
    int? CreditColumn,
    int? CategoryColumn,
    int? NotesColumn);
