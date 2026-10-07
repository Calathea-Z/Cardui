namespace Cardui.Api.Domain.TransactionImport;

public static class TransactionImportLimits
{
    public const int MaxFileBytes = 1_048_576;

    public const int MaxDataRows = 2_000;

    public const int MaxFileNameLength = 200;

    public const int MaxNameLength = 300;

    public const int MaxNotesLength = 1_000;

    public const int MaxOpenImports = 10;
}
