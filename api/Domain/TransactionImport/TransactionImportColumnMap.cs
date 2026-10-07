namespace Cardui.Api.Domain.TransactionImport;

public sealed record TransactionImportColumnMap(
    int DateColumn,
    int NameColumn,
    int? AmountColumn,
    int? DebitColumn,
    int? CreditColumn,
    int? CategoryColumn,
    int? NotesColumn,
    CsvAmountSign AmountSign,
    CsvDateOrder DateOrder);
