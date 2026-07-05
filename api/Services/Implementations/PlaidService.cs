using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Models;
using Cardui.Api.Exceptions;
using Cardui.Api.Services.Interfaces;
using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Going.Plaid.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Account = Cardui.Api.Models.Account;
using Environment = Going.Plaid.Environment;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;
using Transaction = Cardui.Api.Models.Transaction;

namespace Cardui.Api.Services.Implementations;

public class PlaidService : IPlaidService
{
    private readonly CarduiDBContext _dbContext;
    private readonly PlaidClient _plaidClient;
    private readonly PlaidConfig _plaidOptions;
    private readonly ITransactionCategorizationService _transactionCategorizationService;

    public PlaidService(CarduiDBContext dbContext, IOptions<PlaidConfig> plaidOptions,
        ITransactionCategorizationService transactionCategorizationService)
    {
        _dbContext = dbContext;
        _plaidOptions = plaidOptions.Value;
        _plaidClient = new PlaidClient(
            GetPlaidEnvironment(_plaidOptions.Environment),
            _plaidOptions.ClientId,
            _plaidOptions.Secret);
        _transactionCategorizationService = transactionCategorizationService;
    }

    public async Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync()
    {
        var request = new LinkTokenCreateRequest
        {
            ClientId = _plaidOptions.ClientId,
            Secret = _plaidOptions.Secret,
            ClientName = _plaidOptions.ClientName,
            Language = Language.English,
            CountryCodes = new List<CountryCode>
            {
                CountryCode.Us
            },
            Products = new List<Products>
            {
                Products.Transactions
            },
            User = new LinkTokenCreateRequestUser
            {
                ClientUserId = "dev-user"
            }
        };

        var response = await _plaidClient.LinkTokenCreateAsync(request);

        return new CreateLinkTokenResponseDto
        {
            LinkToken = response.LinkToken
        };
    }

    public async Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(
        ExchangePublicTokenRequestDto dto)
    {
        var request = new ItemPublicTokenExchangeRequest
        {
            ClientId = _plaidOptions.ClientId,
            Secret = _plaidOptions.Secret,
            PublicToken = dto.PublicToken
        };

        var response = await _plaidClient.ItemPublicTokenExchangeAsync(request);

        var now = DateTimeOffset.UtcNow;

        var plaidItem = new PlaidItem
        {
            Id = Guid.NewGuid(),
            PlaidItemId = response.ItemId,
            AccessToken = response.AccessToken,
            InstitutionId = dto.InstitutionId,
            InstitutionName = dto.InstitutionName,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.PlaidItems.Add(plaidItem);
        await _dbContext.SaveChangesAsync();

        await SyncAccountsForPlaidItemAsync(plaidItem);
        await SyncTransactionsForPlaidItemAsync(plaidItem);

        return new ExchangePublicTokenResponseDto
        {
            PlaidItemId = plaidItem.Id
        };
    }

    public async Task SyncAccountsAsync(Guid plaidItemId)
    {
        var plaidItem = await _dbContext.PlaidItems
            .FirstOrDefaultAsync(x => x.Id == plaidItemId);

        if (plaidItem is null) throw new NotFoundException($"Plaid item '{plaidItemId}' was not found.");

        await SyncAccountsForPlaidItemAsync(plaidItem);
    }

    public async Task<SyncTransactionsResponseDto> SyncTransactionsAsync(Guid plaidItemId)
    {
        var plaidItem = await _dbContext.PlaidItems
            .FirstOrDefaultAsync(x => x.Id == plaidItemId);

        if (plaidItem is null) throw new NotFoundException($"Plaid item '{plaidItemId}' was not found.");

        return await SyncTransactionsForPlaidItemAsync(plaidItem);
    }

    public async Task<IReadOnlyList<PlaidItemDto>> GetPlaidItemsAsync()
    {
        return await _dbContext.PlaidItems
            .AsNoTracking()
            .OrderBy(x => x.InstitutionName)
            .Select(x => new PlaidItemDto
            {
                Id = x.Id,
                InstitutionId = x.InstitutionId,
                InstitutionName = x.InstitutionName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                LastTransactionsSyncedAt = x.LastTransactionsSyncedAt
            })
            .ToListAsync();
    }

    public async Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(Guid plaidItemId)
    {
        var plaidItem = await _dbContext.PlaidItems
            .FirstOrDefaultAsync(x => x.Id == plaidItemId);

        if (plaidItem is null) throw new NotFoundException($"Plaid item '{plaidItemId}' was not found.");

        await SyncAccountsForPlaidItemAsync(plaidItem);
        var transactionsResult = await SyncTransactionsForPlaidItemAsync(plaidItem);

        return new SyncPlaidItemResponseDto
        {
            PlaidItemId = plaidItem.Id,
            Transactions = transactionsResult
        };
    }

    #region PrivateMethods

    private static Environment GetPlaidEnvironment(string environment)
    {
        return Enum.Parse<Environment>(
            environment,
            true);
    }

    private async Task SyncAccountsForPlaidItemAsync(PlaidItem plaidItem)
    {
        var request = new AccountsGetRequest
        {
            ClientId = _plaidOptions.ClientId,
            Secret = _plaidOptions.Secret,
            AccessToken = plaidItem.AccessToken
        };

        var response = await _plaidClient.AccountsGetAsync(request);

        var now = DateTimeOffset.UtcNow;

        foreach (var plaidAccount in response.Accounts)
        {
            var existingAccount = await _dbContext.Accounts
                .FirstOrDefaultAsync(x => x.PlaidAccountId == plaidAccount.AccountId);

            if (existingAccount is null)
            {
                var account = new Account
                {
                    Id = Guid.NewGuid(),
                    PlaidItemId = plaidItem.Id,
                    PlaidAccountId = plaidAccount.AccountId,
                    Name = plaidAccount.Name,
                    OfficialName = plaidAccount.OfficialName,
                    Type = plaidAccount.Type.ToString().ToLowerInvariant(),
                    Subtype = plaidAccount.Subtype?.ToString().ToLowerInvariant(),
                    Mask = plaidAccount.Mask,
                    CurrentBalance = plaidAccount.Balances.Current.HasValue
                        ? Convert.ToDecimal(plaidAccount.Balances.Current.Value)
                        : 0m,
                    AvailableBalance = plaidAccount.Balances.Available.HasValue
                        ? Convert.ToDecimal(plaidAccount.Balances.Available.Value)
                        : null,
                    IsoCurrencyCode = plaidAccount.Balances.IsoCurrencyCode,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.Accounts.Add(account);
            }
            else
            {
                existingAccount.Name = plaidAccount.Name;
                existingAccount.OfficialName = plaidAccount.OfficialName;
                existingAccount.Type = plaidAccount.Type.ToString().ToLowerInvariant();
                existingAccount.Subtype = plaidAccount.Subtype?.ToString().ToLowerInvariant();
                existingAccount.Mask = plaidAccount.Mask;
                existingAccount.CurrentBalance = plaidAccount.Balances.Current.HasValue
                    ? Convert.ToDecimal(plaidAccount.Balances.Current.Value)
                    : 0m;
                existingAccount.AvailableBalance = plaidAccount.Balances.Available.HasValue
                    ? Convert.ToDecimal(plaidAccount.Balances.Available.Value)
                    : null;
                existingAccount.IsoCurrencyCode = plaidAccount.Balances.IsoCurrencyCode;
                existingAccount.IsActive = true;
                existingAccount.UpdatedAt = now;
            }
        }

        plaidItem.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();
    }

    private async Task UpsertPlaidTransactionAsync(
        Going.Plaid.Entity.Transaction plaidTransaction)
    {
        if (string.IsNullOrWhiteSpace(plaidTransaction.TransactionId))
            throw new InvalidOperationException("Plaid transaction is missing transaction id.");

        if (plaidTransaction.Date is null)
            throw new InvalidOperationException(
                $"Plaid transaction {plaidTransaction.TransactionId} is missing date.");

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(x => x.PlaidAccountId == plaidTransaction.AccountId);

        if (account is null) return;

        var existingTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(x =>
                x.PlaidTransactionId == plaidTransaction.TransactionId);

        var now = DateTimeOffset.UtcNow;
        var name = plaidTransaction.MerchantName
                   ?? "Unknown transaction";

        if (existingTransaction is null)
        {
            var categoryId = await _transactionCategorizationService
                .GetCategoryIdForPlaidTransactionAsync(plaidTransaction);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                PlaidTransactionId = plaidTransaction.TransactionId,
                Date = plaidTransaction.Date.Value,
                AuthorizedDate = plaidTransaction.AuthorizedDate,
                Name = name,
                MerchantName = plaidTransaction.MerchantName,
                Amount = Convert.ToDecimal(plaidTransaction.Amount),
                IsoCurrencyCode = plaidTransaction.IsoCurrencyCode,
                Pending = plaidTransaction.Pending ?? false,
                CategoryId = categoryId,
                Notes = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Transactions.Add(transaction);
            return;
        }

        existingTransaction.AccountId = account.Id;
        existingTransaction.Date = plaidTransaction.Date.Value;
        existingTransaction.AuthorizedDate = plaidTransaction.AuthorizedDate;
        existingTransaction.Name = name;
        existingTransaction.MerchantName = plaidTransaction.MerchantName;
        existingTransaction.Amount = Convert.ToDecimal(plaidTransaction.Amount);
        existingTransaction.IsoCurrencyCode = plaidTransaction.IsoCurrencyCode;
        existingTransaction.Pending = plaidTransaction.Pending ?? false;
        existingTransaction.UpdatedAt = now;
    }

    private async Task<SyncTransactionsResponseDto> SyncTransactionsForPlaidItemAsync(
        PlaidItem plaidItem)
    {
        var addedCount = 0;
        var modifiedCount = 0;
        var removedCount = 0;
        var hasMore = true;
        var cursor = plaidItem.TransactionsCursor;

        while (hasMore)
        {
            var request = new TransactionsSyncRequest
            {
                ClientId = _plaidOptions.ClientId,
                Secret = _plaidOptions.Secret,
                AccessToken = plaidItem.AccessToken,
                Cursor = cursor,
                Count = 100
            };

            var response = await _plaidClient.TransactionsSyncAsync(request);

            foreach (var plaidTransaction in response.Added)
            {
                await UpsertPlaidTransactionAsync(plaidTransaction);
                addedCount++;
            }

            foreach (var plaidTransaction in response.Modified)
            {
                await UpsertPlaidTransactionAsync(plaidTransaction);
                modifiedCount++;
            }

            foreach (var removedTransaction in response.Removed)
            {
                var existingTransaction = await _dbContext.Transactions
                    .FirstOrDefaultAsync(x =>
                        x.PlaidTransactionId == removedTransaction.TransactionId);

                if (existingTransaction is not null)
                {
                    _dbContext.Transactions.Remove(existingTransaction);
                    removedCount++;
                }
            }

            cursor = response.NextCursor;
            hasMore = response.HasMore;
        }

        plaidItem.TransactionsCursor = cursor;
        plaidItem.LastTransactionsSyncedAt = DateTimeOffset.UtcNow;
        plaidItem.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return new SyncTransactionsResponseDto
        {
            Added = addedCount,
            Modified = modifiedCount,
            Removed = removedCount,
            NextCursor = cursor
        };
    }

    #endregion
}