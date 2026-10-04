using System.Text.Json;
using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Plaid;

public class PlaidWebhookService : IPlaidWebhookService
{
    private readonly CarduiDBContext _dbContext;
    private readonly IPlaidWebhookKeySource _keys;
    private readonly PlaidItemRemoval _itemRemoval;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlaidWebhookService> _logger;

    public PlaidWebhookService(
        CarduiDBContext dbContext,
        IPlaidWebhookKeySource keys,
        PlaidItemRemoval itemRemoval,
        TimeProvider timeProvider,
        ILogger<PlaidWebhookService> logger)
    {
        _dbContext = dbContext;
        _keys = keys;
        _itemRemoval = itemRemoval;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AcceptAsync(
        string verificationJwt,
        ReadOnlyMemory<byte> rawBody,
        CancellationToken cancellationToken = default)
    {
        var keyId = PlaidWebhookVerifier.ReadKeyId(verificationJwt);
        var key = await _keys.GetKeyAsync(keyId, cancellationToken);
        await PlaidWebhookVerifier.Verify(
            verificationJwt,
            rawBody,
            key,
            _timeProvider.GetUtcNow());

        var notice = ReadNotice(rawBody);
        await ApplyAsync(notice, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Reads the webhook type, code, item id, and error code. Other fields are ignored.
    /// </summary>
    private static PlaidWebhookNotice ReadNotice(ReadOnlyMemory<byte> rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        string? errorCode = null;
        if (root.TryGetProperty("error", out var error)
            && error.ValueKind == JsonValueKind.Object
            && error.TryGetProperty("error_code", out var code))
        {
            errorCode = code.GetString();
        }

        return new PlaidWebhookNotice(
            ReadString(root, "webhook_type"),
            ReadString(root, "webhook_code"),
            ReadString(root, "item_id"),
            errorCode);
    }

    /// <summary>
    /// Reads a string property, or null when it is missing.
    /// </summary>
    private static string? ReadString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    /// <summary>
    /// Removes a revoked item, or records a short connection warning.
    /// Unknown items and unrelated webhook codes are ignored.
    /// </summary>
    private async Task ApplyAsync(
        PlaidWebhookNotice notice,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(notice.WebhookType, "ITEM", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(notice.ItemId))
        {
            return;
        }

        var plaidItem = await _dbContext.PlaidItems
            .FirstOrDefaultAsync(item => item.PlaidItemId == notice.ItemId, cancellationToken);
        if (plaidItem is null)
        {
            return;
        }

        if (string.Equals(notice.WebhookCode, "USER_PERMISSION_REVOKED", StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Removing Plaid item {PlaidItemId} after revoked permission.",
                plaidItem.Id);
            await _itemRemoval.RemoveStoredItemAsync(plaidItem, cancellationToken);
            return;
        }

        var message = ConnectionWarning(notice.WebhookCode, notice.ErrorCode);
        if (message is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        plaidItem.LastSyncError = message;
        plaidItem.LastSyncFailedAt = now;
        plaidItem.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Maps a connection webhook to a short message, or null when Cardui does not act on it.
    /// </summary>
    private static string? ConnectionWarning(string? webhookCode, string? errorCode)
    {
        if (string.Equals(webhookCode, "PENDING_DISCONNECT", StringComparison.Ordinal))
        {
            return "This bank connection will stop soon. Reconnect it to keep syncing.";
        }

        if (string.Equals(webhookCode, "USER_ACCOUNT_REVOKED", StringComparison.Ordinal))
        {
            return "The bank revoked access to an account.";
        }

        if (!string.Equals(webhookCode, "ERROR", StringComparison.Ordinal))
        {
            return null;
        }

        return string.Equals(errorCode, "ITEM_LOGIN_REQUIRED", StringComparison.Ordinal)
            ? "This bank connection needs to be signed in again."
            : "The bank reported a connection error.";
    }

    #endregion
}
