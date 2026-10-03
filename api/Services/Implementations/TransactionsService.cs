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

    public Task<TransactionDto> GetTransactionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        ProjectTransactionByIdAsync(id, cancellationToken);

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

        var merchantKey = source.MerchantName?.Trim();
        var hasMerchantName = !string.IsNullOrWhiteSpace(merchantKey);
        var matchKey = hasMerchantName
            ? merchantKey!
            : source.Name.Trim();
        var matchKeyLower = matchKey.ToLowerInvariant();

        var historyQuery = _dbContext.Transactions
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.ArchivedAt == null);

        if (hasMerchantName)
        {
            historyQuery = historyQuery.Where(x =>
                x.MerchantName != null &&
                x.MerchantName.ToLower() == matchKeyLower);
        }
        else
        {
            historyQuery = historyQuery.Where(x =>
                x.Name.ToLower() == matchKeyLower);
        }

        var transactions = await historyQuery
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(TransactionDtoMapper.Projection)
            .ToListAsync(cancellationToken);

        var normalizedGranularity = NormalizeGranularity(granularity);
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var selectedKey = GetPeriodKey(today, normalizedGranularity);

        var earliestDate = transactions.Count == 0
            ? today
            : transactions.Min(x => x.Date);

        var periods = BuildPeriods(
            earliestDate,
            today,
            normalizedGranularity,
            transactions);

        if (periods.All(period => period.Key != selectedKey))
        {
            periods.Add(CreateEmptyPeriod(selectedKey, normalizedGranularity));
            periods = OrderPeriods(periods);
        }

        return new MerchantHistoryDto
        {
            DisplayName = matchKey,
            TotalTransactionCount = transactions.Count,
            Granularity = normalizedGranularity,
            SelectedPeriodKey = selectedKey,
            Periods = periods,
            Transactions = transactions
        };
    }

    private static string NormalizeGranularity(string? granularity)
    {
        return granularity?.Trim().ToLowerInvariant() switch
        {
            "quarterly" => "quarterly",
            "yearly" => "yearly",
            _ => "monthly"
        };
    }

    private static string GetPeriodKey(DateOnly date, string granularity)
    {
        return granularity switch
        {
            "quarterly" => $"{date.Year}-Q{(date.Month - 1) / 3 + 1}",
            "yearly" => date.Year.ToString(),
            _ => $"{date.Year:D4}-{date.Month:D2}"
        };
    }

    private static (string Label, string ShortLabel) GetPeriodLabels(
        string periodKey,
        string granularity)
    {
        if (granularity == "yearly" && int.TryParse(periodKey, out var year))
        {
            return (year.ToString(), year.ToString());
        }

        if (granularity == "quarterly")
        {
            var parts = periodKey.Split("-Q");
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var quarterYear) &&
                int.TryParse(parts[1], out var quarter))
            {
                return ($"Q{quarter} {quarterYear}", $"Q{quarter}");
            }
        }

        if (DateOnly.TryParseExact(
                $"{periodKey}-01",
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var monthDate))
        {
            return (
                monthDate.ToString("MMMM yyyy"),
                monthDate.ToString("MMM"));
        }

        return (periodKey, periodKey);
    }

    private static MerchantHistoryPeriodDto CreateEmptyPeriod(
        string periodKey,
        string granularity)
    {
        var (label, shortLabel) = GetPeriodLabels(periodKey, granularity);
        return new MerchantHistoryPeriodDto
        {
            Key = periodKey,
            Label = label,
            ShortLabel = shortLabel,
            TotalAmount = 0,
            TransactionCount = 0
        };
    }

    private static List<MerchantHistoryPeriodDto> BuildPeriods(
        DateOnly startDate,
        DateOnly endDate,
        string granularity,
        IReadOnlyList<TransactionDto> transactions)
    {
        var totals = transactions
            .GroupBy(x => GetPeriodKey(x.Date, granularity))
            .ToDictionary(
                group => group.Key,
                group => (
                    TotalAmount: group.Sum(x => x.Amount),
                    TransactionCount: group.Count()));

        var periods = new List<MerchantHistoryPeriodDto>();
        var cursor = AlignPeriodStart(startDate, granularity);
        var end = AlignPeriodStart(endDate, granularity);

        while (cursor <= end)
        {
            var key = GetPeriodKey(cursor, granularity);
            totals.TryGetValue(key, out var stats);
            var (label, shortLabel) = GetPeriodLabels(key, granularity);

            periods.Add(new MerchantHistoryPeriodDto
            {
                Key = key,
                Label = label,
                ShortLabel = shortLabel,
                TotalAmount = stats.TotalAmount,
                TransactionCount = stats.TransactionCount
            });

            cursor = AdvancePeriod(cursor, granularity);
        }

        return periods;
    }

    private static List<MerchantHistoryPeriodDto> OrderPeriods(
        List<MerchantHistoryPeriodDto> periods)
    {
        return periods
            .DistinctBy(period => period.Key)
            .OrderBy(period => period.Key, StringComparer.Ordinal)
            .ToList();
    }

    private static DateOnly AlignPeriodStart(DateOnly date, string granularity)
    {
        return granularity switch
        {
            "quarterly" => new DateOnly(date.Year, (date.Month - 1) / 3 * 3 + 1, 1),
            "yearly" => new DateOnly(date.Year, 1, 1),
            _ => new DateOnly(date.Year, date.Month, 1)
        };
    }

    private static DateOnly AdvancePeriod(DateOnly date, string granularity)
    {
        return granularity switch
        {
            "quarterly" => date.AddMonths(3),
            "yearly" => date.AddYears(1),
            _ => date.AddMonths(1)
        };
    }

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
        if (dto.Date > today)
        {
            throw new BadRequestException("The transaction date cannot be in the future.");
        }

        var account = await LoadAccountAsync(transaction.AccountId, cancellationToken);
        if (ManualAccountBalance.UsesLedger(account)
            && account.OpeningBalanceDate is DateOnly openingDate
            && dto.Date < openingDate)
        {
            throw new BadRequestException(
                "The transaction date cannot be before the account opening date.");
        }

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
        if (dto.Date > today)
        {
            throw new BadRequestException("The transaction date cannot be in the future.");
        }

        if (ManualAccountBalance.UsesLedger(account)
            && account.OpeningBalanceDate is DateOnly openingDate
            && dto.Date < openingDate)
        {
            throw new BadRequestException(
                "The transaction date cannot be before the account opening date.");
        }

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

    private static (int Page, int PageSize) NormalizePagination(TransactionQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        return (page, pageSize);
    }

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

    private static bool IsManualEntry(Transaction transaction) =>
        transaction.Source == FinancialRecordSource.Manual
        && transaction.Provenance == FinancialRecordProvenance.ManualEntry;

    private static string RequireName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > 300)
        {
            throw new BadRequestException("A name is required.");
        }

        return trimmed;
    }

    private static string? EmptyToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private Task<bool> CategoryIsVisibleAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .VisibleToHousehold(_householdScope)
            .AnyAsync(x => x.Id == categoryId, cancellationToken);
    }
}
