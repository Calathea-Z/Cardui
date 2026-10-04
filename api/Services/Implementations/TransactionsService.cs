using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Common;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class TransactionsService : ITransactionsService
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public TransactionsService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(
        TransactionQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = NormalizePagination(query);

        var transactionsQuery = ApplyFilters(
            _dbContext.Transactions.AsNoTracking().InHousehold(_householdScope),
            query);

        var totalCount = await transactionsQuery.CountAsync(cancellationToken);

        var items = await transactionsQuery
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(TransactionDtoMapper.Projection)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultDto<TransactionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1 && totalPages > 0
        };
    }

    /// <inheritdoc />
    public Task<TransactionDto> GetTransactionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        ProjectTransactionByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<MerchantHistoryDto> GetMerchantHistoryAsync(
        Guid transactionId,
        string granularity = "monthly",
        CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.Transactions
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.Id == transactionId)
            .Select(x => new { x.Name, x.MerchantName })
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        var match = MerchantMatchKey.Create(source.Name, source.MerchantName);
        var transactions = await LoadMerchantTransactionsAsync(match, cancellationToken);
        var today = FinancialDate.Today(_timeProvider);
        var series = MerchantHistoryPeriods.Build(
            today,
            granularity,
            transactions.Select(transaction =>
                new MerchantHistoryActivity(transaction.Date, transaction.Amount)));

        return new MerchantHistoryDto
        {
            DisplayName = match.DisplayName,
            TotalTransactionCount = transactions.Count,
            Granularity = series.Granularity,
            SelectedPeriodKey = series.SelectedPeriodKey,
            Periods = series.Periods
                .Select(period => new MerchantHistoryPeriodDto
                {
                    Key = period.Key,
                    Label = period.Label,
                    ShortLabel = period.ShortLabel,
                    TotalAmount = period.TotalAmount,
                    TransactionCount = period.TransactionCount
                })
                .ToList(),
            Transactions = transactions
        };
    }

    /// <inheritdoc />
    public async Task<TransactionDto> UpdateTransactionCategoryAsync(
        Guid transactionId,
        UpdateTransactionCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Transactions
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await CategoryIsVisibleAsync(
                dto.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                throw new BadRequestException($"Category '{dto.CategoryId}' was not found.");
            }
        }

        transaction.CategoryId = dto.CategoryId;
        transaction.IsCategoryUserEdited = true;
        transaction.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectTransactionByIdAsync(transaction.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionDto> UpdateTransactionDetailsAsync(
        Guid transactionId,
        UpdateTransactionDetailsDto dto,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Transactions
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await CategoryIsVisibleAsync(
                dto.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                throw new BadRequestException($"Category '{dto.CategoryId}' was not found.");
            }
        }

        var today = FinancialDate.Today(_timeProvider);
        RequireTransactionDate(dto.Date, today);

        var account = await LoadAccountAsync(transaction.AccountId, cancellationToken);
        RequireDateOnOrAfterOpening(account, dto.Date);

        var manualEntry = IsManualEntry(transaction);
        if (manualEntry)
        {
            if (dto.Name is not null)
            {
                transaction.Name = RequireName(dto.Name);
                transaction.MerchantName = transaction.Name;
            }

            if (dto.Amount is decimal amount)
            {
                transaction.Amount = AccountLedger.Round(amount);
            }

            if (dto.Pending is bool pending)
            {
                transaction.Pending = pending;
            }
        }

        transaction.Date = dto.Date;
        transaction.IsDateUserEdited = true;
        transaction.CategoryId = dto.CategoryId;
        transaction.IsCategoryUserEdited = true;
        transaction.Notes = EmptyToNull(dto.Notes);
        transaction.UpdatedAt = _timeProvider.GetUtcNow();

        if (ManualAccountBalance.UsesLedger(account))
        {
            await ManualAccountBalance.RefreshAsync(
                _dbContext,
                account,
                today,
                transaction.UpdatedAt,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectTransactionByIdAsync(transaction.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionDto> CreateManualTransactionAsync(
        CreateManualTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(dto.AccountId, cancellationToken);
        if (account.ArchivedAt is not null)
        {
            throw new BadRequestException("Restore this account before adding a transaction.");
        }

        var today = FinancialDate.Today(_timeProvider);
        RequireTransactionDate(dto.Date, today);
        RequireDateOnOrAfterOpening(account, dto.Date);

        if (dto.CategoryId is Guid categoryId)
        {
            var categoryExists = await CategoryIsVisibleAsync(categoryId, cancellationToken);
            if (!categoryExists)
            {
                throw new BadRequestException($"Category '{dto.CategoryId}' was not found.");
            }
        }

        var now = _timeProvider.GetUtcNow();
        var name = RequireName(dto.Name);
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PlaidTransactionId = null,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Date = dto.Date,
            IsDateUserEdited = true,
            Name = name,
            MerchantName = name,
            Amount = AccountLedger.Round(dto.Amount),
            IsoCurrencyCode = account.IsoCurrencyCode,
            Pending = dto.Pending,
            CategoryId = dto.CategoryId,
            IsCategoryUserEdited = dto.CategoryId.HasValue,
            Notes = EmptyToNull(dto.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Transactions.Add(transaction);

        if (ManualAccountBalance.UsesLedger(account))
        {
            await ManualAccountBalance.RefreshAsync(
                _dbContext,
                account,
                today,
                now,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectTransactionByIdAsync(transaction.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionDto> ArchiveTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await FindTransactionAsync(transactionId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        transaction.ArchivedAt ??= now;
        transaction.UpdatedAt = now;
        await RefreshAccountBalanceAsync(transaction.AccountId, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectTransactionByIdAsync(transaction.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionDto> RestoreTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await FindTransactionAsync(transactionId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        transaction.ArchivedAt = null;
        transaction.UpdatedAt = now;
        await RefreshAccountBalanceAsync(transaction.AccountId, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectTransactionByIdAsync(transaction.Id, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Loads non-archived transactions for the same merchant, newest first.
    /// </summary>
    private async Task<List<TransactionDto>> LoadMerchantTransactionsAsync(
        MerchantMatchKey match,
        CancellationToken cancellationToken)
    {
        var matchKeyLower = match.DisplayName.ToLowerInvariant();
        var historyQuery = _dbContext.Transactions
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(transaction => transaction.ArchivedAt == null);

        if (match.MatchesMerchantName)
        {
            historyQuery = historyQuery.Where(transaction =>
                transaction.MerchantName != null &&
                transaction.MerchantName.ToLower() == matchKeyLower);
        }
        else
        {
            historyQuery = historyQuery.Where(transaction =>
                transaction.Name.ToLower() == matchKeyLower);
        }

        return await historyQuery
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .Select(TransactionDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Rejects a transaction date in the future.
    /// </summary>
    private static void RequireTransactionDate(DateOnly date, DateOnly today)
    {
        if (date > today)
        {
            throw new BadRequestException("The transaction date cannot be in the future.");
        }
    }

    /// <summary>
    /// Rejects a date before the opening date of an account whose balance
    /// comes from its transactions.
    /// </summary>
    private static void RequireDateOnOrAfterOpening(Account account, DateOnly date)
    {
        if (ManualAccountBalance.UsesLedger(account)
            && account.OpeningBalanceDate is DateOnly openingDate
            && date < openingDate)
        {
            throw new BadRequestException(
                "The transaction date cannot be before the account opening date.");
        }
    }

    /// <summary>
    /// Keeps the page at 1 or higher and limits the page size to 100.
    /// </summary>
    private static (int Page, int PageSize) NormalizePagination(TransactionQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        return (page, pageSize);
    }

    /// <summary>
    /// Applies search, account, category, date, pending, and archived filters.
    /// Archived transactions are excluded unless the query asks for them.
    /// </summary>
    private static IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> transactionsQuery,
        TransactionQueryDto query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{query.Search.Trim()}%";

            transactionsQuery = transactionsQuery.Where(x =>
                EF.Functions.ILike(x.Name, searchPattern) ||
                (x.MerchantName != null && EF.Functions.ILike(x.MerchantName, searchPattern)) ||
                (x.Notes != null && EF.Functions.ILike(x.Notes, searchPattern)));
        }

        if (query.AccountId.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(x => x.AccountId == query.AccountId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(x => x.CategoryId == query.CategoryId.Value);
        }

        if (query.From.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(x => x.Date >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(x => x.Date <= query.To.Value);
        }

        if (query.Pending.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(x => x.Pending == query.Pending.Value);
        }

        transactionsQuery = query.Archived == true
            ? transactionsQuery.Where(x => x.ArchivedAt != null)
            : transactionsQuery.Where(x => x.ArchivedAt == null);

        return transactionsQuery;
    }

    /// <summary>
    /// Loads the API shape of one household transaction, or throws when it is missing.
    /// </summary>
    private async Task<TransactionDto> ProjectTransactionByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.Id == id)
            .Select(TransactionDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{id}' was not found.");
        }

        return transaction;
    }

    /// <summary>
    /// Loads a tracked household account, or throws when it is missing.
    /// </summary>
    private async Task<Account> LoadAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Account '{accountId}' was not found.");
        }

        return account;
    }

    /// <summary>
    /// Loads a tracked household transaction, or throws when it is missing.
    /// </summary>
    private async Task<Transaction> FindTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        return transaction;
    }

    /// <summary>
    /// Recalculates today's balance when the account uses the manual ledger.
    /// Linked accounts are left unchanged.
    /// </summary>
    private async Task RefreshAccountBalanceAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);

        if (account is null || !ManualAccountBalance.UsesLedger(account))
        {
            return;
        }

        await ManualAccountBalance.RefreshAsync(
            _dbContext,
            account,
            FinancialDate.Today(_timeProvider),
            now,
            cancellationToken);
    }

    /// <summary>
    /// True when the transaction was entered in Cardui and is not a reconciliation.
    /// </summary>
    private static bool IsManualEntry(Transaction transaction) =>
        transaction.Source == FinancialRecordSource.Manual
        && transaction.Provenance == FinancialRecordProvenance.ManualEntry;

    /// <summary>
    /// Trims a required name and rejects an empty value or one longer than 300 characters.
    /// </summary>
    private static string RequireName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > 300)
        {
            throw new BadRequestException("A name is required.");
        }

        return trimmed;
    }

    /// <summary>
    /// Trims optional text and stores blank input as null.
    /// </summary>
    private static string? EmptyToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// True when the category is a system category or belongs to this household.
    /// </summary>
    private Task<bool> CategoryIsVisibleAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .VisibleToHousehold(_householdScope)
            .AnyAsync(x => x.Id == categoryId, cancellationToken);
    }

    #endregion
}
