using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Models;
using CardUI.Api.Services.Interfaces;
using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Microsoft.Extensions.Options;
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

        return new ExchangePublicTokenResponseDto
        {
            PlaidItemId = plaidItem.Id
        };
    }

    private static Environment GetPlaidEnvironment(string environment)
    {
        return Enum.Parse<Environment>(
            environment,
            true);
    }
}