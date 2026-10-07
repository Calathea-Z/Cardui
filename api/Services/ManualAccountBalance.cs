using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services;

public static class ManualAccountBalance
{
    /// <summary>
    /// True when the account is manual and has an opening date, so its balance comes from the ledger.
    /// </summary>
    public static bool UsesLedger(Account account) =>
        account.PlaidItemId is null && account.OpeningBalanceDate is not null;

    /// <summary>
    /// Recalculates a manual account's current balance through today and stores today's snapshot.
    /// Linked accounts and accounts without an opening date are skipped.
    /// </summary>
    public static async Task RefreshAsync(
        CarduiDBContext dbContext,
        Account account,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!UsesLedger(account) || account.OpeningBalanceDate is not DateOnly openingDate)
        {
            return;
        }

        var transactions = await LoadTransactionsAsync(
            dbContext,
            account.Id,
            cancellationToken);

        account.CurrentBalance = AccountLedger.BalanceAsOf(
            account.Type,
            account.OpeningBalance,
            openingDate,
            transactions,
            today);
        account.UpdatedAt = now;

        await UpsertSnapshotAsync(dbContext, account, today, now, cancellationToken);
    }

    /// <summary>
    /// Replaces the balance snapshot for this account and date, including a
    /// snapshot that is only tracked and not saved yet.
    /// </summary>
    public static async Task UpsertSnapshotAsync(
        CarduiDBContext dbContext,
        Account account,
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.AccountBalanceSnapshots
            .Where(x => x.AccountId == account.Id && x.Date == date)
            .ToListAsync(cancellationToken);

        foreach (var local in dbContext.AccountBalanceSnapshots.Local)
        {
            if (local.AccountId == account.Id
                && local.Date == date
                && !existing.Contains(local))
            {
                existing.Add(local);
            }
        }

        if (existing.Count > 0)
        {
            dbContext.AccountBalanceSnapshots.RemoveRange(existing);
        }

        dbContext.AccountBalanceSnapshots.Add(new AccountBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Date = date,
            CurrentBalance = account.CurrentBalance,
            AvailableBalance = account.AvailableBalance,
            IsoCurrencyCode = account.IsoCurrencyCode,
            CreatedAt = now
        });
    }

    #region Private Methods

    /// <summary>
    /// Loads the account's ledger rows without tracking them, then applies unsaved changes.
    /// </summary>
    private static async Task<List<LedgerTransaction>> LoadTransactionsAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var stored = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new
            {
                x.Id,
                x.Date,
                x.Amount,
                x.Pending,
                IsArchived = x.ArchivedAt != null
            })
            .ToListAsync(cancellationToken);

        var byId = stored.ToDictionary(
            x => x.Id,
            x => new LedgerTransaction(x.Date, x.Amount, x.Pending, x.IsArchived));

        ApplyTrackedTransactions(dbContext, accountId, byId);
        return byId.Values.ToList();
    }

    /// <summary>
    /// Replaces stored ledger rows with unsaved tracked transactions for this account.
    /// A tracked deletion drops the row so the balance does not keep it.
    /// </summary>
    private static void ApplyTrackedTransactions(
        CarduiDBContext dbContext,
        Guid accountId,
        Dictionary<Guid, LedgerTransaction> byId)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<Transaction>())
        {
            if (entry.Entity.AccountId != accountId)
            {
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                byId.Remove(entry.Entity.Id);
                continue;
            }

            var transaction = entry.Entity;
            byId[transaction.Id] = new LedgerTransaction(
                transaction.Date,
                transaction.Amount,
                transaction.Pending,
                transaction.ArchivedAt != null);
        }
    }

    #endregion
}
