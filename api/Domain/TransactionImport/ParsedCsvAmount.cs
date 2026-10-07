namespace Cardui.Api.Domain.TransactionImport;

public readonly record struct ParsedCsvAmount(decimal Amount, bool ExplicitDirection);
