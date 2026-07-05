using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Models;
using CardUI.Api.Services.Interfaces;
using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Account = Cardui.Api.Models.Account;
using Environment = Going.Plaid.Environment;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Api.Services.Implementations;

public class PlaidService : IPlaidService
{
    private readonly CarduiDBContext _dbContext;
    private readonly PlaidClient _plaidClient;
    private readonly PlaidConfig _plaidOptions;

    public PlaidService(CarduiDBContext dbContext, IOptions<PlaidConfig> plaidOptions)
    {
        _dbContext = dbContext;
        _plaidOptions = plaidOptions.Value;
        _plaidClient = new PlaidClient(
            GetPlaidEnvironment(_plaidOptions.Environment),
            _plaidOptions.ClientId,
            _plaidOptions.Secret);
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

        return new ExchangePublicTokenResponseDto
        {
            PlaidItemId = plaidItem.Id
        };
    }

    public async Task SyncAccountsAsync(Guid plaidItemId)
    {
        var plaidItem = await _dbContext.PlaidItems
            .FirstOrDefaultAsync(x => x.Id == plaidItemId);

        if (plaidItem is null) throw new InvalidOperationException("Plaid item was not found.");

        await SyncAccountsForPlaidItemAsync(plaidItem);
    }

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
}