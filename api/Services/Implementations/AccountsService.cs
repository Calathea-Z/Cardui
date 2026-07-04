using Cardui.Api.Data;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class AccountsService : IAccountsService
{
    private readonly CarduiDBContext _dbContext;

    public AccountsService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync()
    {
        return await _dbContext.Accounts
            .OrderBy(x => x.Name)
            .Select(x => new AccountDto
            {
                Id = x.Id,
                Name = x.Name,
                OfficialName = x.OfficialName,
                Type = x.Type,
                Subtype = x.Subtype,
                Mask = x.Mask,
                CurrentBalance = x.CurrentBalance,
                AvailableBalance = x.AvailableBalance,
                IsoCurrencyCode = x.IsoCurrencyCode,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }
}