using System.Linq.Expressions;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class TransactionDtoMapper
{
    public static readonly Expression<Func<Transaction, TransactionDto>> Projection = x => new TransactionDto
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
                Key = x.Category.Key,
                Color = x.Category.Color,
                Icon = x.Category.Icon
            },
        Notes = x.Notes
    };
}
