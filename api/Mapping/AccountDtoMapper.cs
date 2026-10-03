using System.Linq.Expressions;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class AccountDtoMapper
{
    public static readonly Expression<Func<Account, AccountDto>> Projection = x => new AccountDto
    {
        Id = x.Id,
        PlaidItemId = x.PlaidItemId,
        Name = x.Name,
        OfficialName = x.OfficialName,
        Type = x.Type,
        Subtype = x.Subtype,
        Mask = x.Mask,
        CurrentBalance = x.CurrentBalance,
        AvailableBalance = x.AvailableBalance,
        IsoCurrencyCode = x.IsoCurrencyCode,
        IsActive = x.IsActive,
        Source = x.Source,
        Provenance = x.Provenance,
        OpeningBalance = x.OpeningBalance,
        OpeningBalanceDate = x.OpeningBalanceDate,
        ArchivedAt = x.ArchivedAt
    };
}
