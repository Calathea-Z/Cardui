using Cardui.Api.Data;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class TransactionsService : ITransactionsService
{
    private readonly CarduiDBContext _dbContext;

    public TransactionsService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }

public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(TransactionQueryDto query)
{
    var transactionsQuery = _dbContext.Transactions
        .AsNoTracking()
        .AsQueryable();

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

    return await transactionsQuery
        .OrderByDescending(x => x.Date)
        .Select(x => new TransactionDto
        {
            Id = x.Id,
            Date = x.Date,
            AuthorizedDate = x.AuthorizedDate,
            Name = x.Name,
            MerchantName = x.MerchantName,
            Amount = x.Amount,
            IsoCurrencyCode = x.IsoCurrencyCode,
            Pending = x.Pending,
            Account = new TransactionAccountDto
            {
                Id = x.Account.Id,
                Name = x.Account.Name,
                Type = x.Account.Type,
                Subtype = x.Account.Subtype
            },
            Category = x.Category == null
                ? null
                : new TransactionCategoryDto
                {
                    Id = x.Category.Id,
                    Name = x.Category.Name,
                    Color = x.Category.Color,
                    Icon = x.Category.Icon
                }
        })
        .ToListAsync();
}

public async Task<TransactionDto?> GetTransactionByIdAsync(Guid id)
{
    return await _dbContext.Transactions
        .AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new TransactionDto
        {
            Id = x.Id,
            Date = x.Date,
            AuthorizedDate = x.AuthorizedDate,
            Name = x.Name,
            MerchantName = x.MerchantName,
            Amount = x.Amount,
            IsoCurrencyCode = x.IsoCurrencyCode,
            Pending = x.Pending,
            Account = new TransactionAccountDto
            {
                Id = x.Account.Id,
                Name = x.Account.Name,
                Type = x.Account.Type,
                Subtype = x.Account.Subtype
            },
            Category = x.Category == null
                ? null
                : new TransactionCategoryDto
                {
                    Id = x.Category.Id,
                    Name = x.Category.Name,
                    Color = x.Category.Color,
                    Icon = x.Category.Icon
                }
        })
        .FirstOrDefaultAsync();
}

public async Task<TransactionDto?> UpdateTransactionCategoryAsync(
    Guid transactionId,
    UpdateTransactionCategoryDto dto)
{
    var transaction = await _dbContext.Transactions
        .FirstOrDefaultAsync(x => x.Id == transactionId);

    if (transaction is null)
    {
        return null;
    }

    if (dto.CategoryId.HasValue)
    {
        var categoryExists = await _dbContext.Categories
            .AnyAsync(x => x.Id == dto.CategoryId.Value);

        if (!categoryExists)
        {
            return null;
        }
    }

    transaction.CategoryId = dto.CategoryId;
    transaction.UpdatedAt = DateTimeOffset.UtcNow;

    await _dbContext.SaveChangesAsync();

    return await GetTransactionByIdAsync(transaction.Id);
}
}