namespace Cardui.Api.Dtos.Plaid;

public sealed record PlaidWebhookNotice(
    string? WebhookType,
    string? WebhookCode,
    string? ItemId,
    string? ErrorCode);
