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

    public TransactionsService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(TransactionQueryDto query)
    {
        var (page, pageSize) = NormalizePagination(query);

        var transactionsQuery = ApplyFilters(
            _dbContext.Transactions.AsNoTracking(),
            query);

        var totalCount = await transactionsQuery.CountAsync();

        var items = await transactionsQuery
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(TransactionDtoMapper.Projection)
            .ToListAsync();

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

    public Task<TransactionDto> GetTransactionByIdAsync(Guid id) =>
        ProjectTransactionByIdAsync(id);

    public async Task<TransactionDto> UpdateTransactionCategoryAsync(
        Guid transactionId,
        UpdateTransactionCategoryDto dto)
    {
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(x => x.Id == transactionId);

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{transactionId}' was not found.");
        }

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(x => x.Id == dto.CategoryId.Value);

            if (!categoryExists)
            {
                throw new BadRequestException($"Category '{dto.CategoryId}' was not found.");
            }
        }

        transaction.CategoryId = dto.CategoryId;
        transaction.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await ProjectTransactionByIdAsync(transaction.Id);
    }

    #region Private Methods

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
            var search = query.Search.Trim().ToLower();

            transactionsQuery = transactionsQuery.Where(x =>
                x.Name.ToLower().Contains(search) ||
                (x.MerchantName != null && x.MerchantName.ToLower().Contains(search)) ||
                (x.Notes != null && x.Notes.ToLower().Contains(search)));
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

    private async Task<TransactionDto> ProjectTransactionByIdAsync(Guid id)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(TransactionDtoMapper.Projection)
            .FirstOrDefaultAsync();

        if (transaction is null)
        {
            throw new NotFoundException($"Transaction '{id}' was not found.");
        }

        return transaction;
    }

    #endregion
}
