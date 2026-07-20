using Cardui.Api.Data;
using Cardui.Api.Dtos.Common;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class TransactionsService : ITransactionsService
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public TransactionsService(CarduiDBContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(
        TransactionQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = NormalizePagination(query);

        var transactionsQuery = ApplyFilters(
            _dbContext.Transactions.AsNoTracking(),
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

    public async Task<TransactionDto> UpdateTransactionCategoryAsync(
        Guid transactionId,
        UpdateTransactionCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(x => x.Id == dto.CategoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                throw new BadRequestException($"Category '{dto.CategoryId}' was not found.");
            }
        }

        transaction.CategoryId = dto.CategoryId;
        transaction.UpdatedAt = _timeProvider.GetUtcNow();

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

        return transactionsQuery;
    }

    private async Task<TransactionDto> ProjectTransactionByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(TransactionDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{id}' was not found.");
        }

        return transaction;
    }
}
