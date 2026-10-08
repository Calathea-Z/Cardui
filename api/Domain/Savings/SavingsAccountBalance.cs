namespace Cardui.Api.Domain.Savings;

/// <summary>
/// The account facts a savings goal needs to decide whether it can follow that balance.
/// </summary>
public sealed record SavingsAccountBalance(
    Guid Id,
    string Name,
    string? Mask,
    string Type,
    string? Currency,
    decimal CurrentBalance,
    bool IsActive,
    DateTimeOffset? ArchivedAt);
