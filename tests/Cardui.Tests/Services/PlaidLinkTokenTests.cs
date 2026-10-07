using Cardui.Api.Data;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Link;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Tests.Services;

public class PlaidLinkTokenTests
{
    private const string StoredAccessToken = "stored-token";
    private const string AccessToken = "access-sandbox-repair";
    private const string LinkToken = "link-sandbox-test";

    private static readonly DateTimeOffset Now =
        new(2026, 10, 7, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateLinkToken_RequestsTransactionsAndNoAccessToken()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await SeedHouseholdAsync(dbContext);
        var link = new RecordingLinkClient();
        var service = CreateService(dbContext, Bind(householdId), link);

        var response = await service.CreateLinkTokenAsync();

        Assert.Equal(LinkToken, response.LinkToken);
        var request = link.Request;
        Assert.NotNull(request);
        Assert.Null(request.AccessToken);
        Assert.NotNull(request.Products);
        Assert.Equal(Products.Transactions, Assert.Single(request.Products));
        Assert.Equal(householdId.ToString("D"), request.User!.ClientUserId);
        Assert.Equal("https://example.test/plaid", request.Webhook);
    }

    [Fact]
    public async Task CreateUpdateLinkToken_RepairsTheHouseholdItemWithoutReturningTheAccessToken()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await SeedHouseholdAsync(dbContext);
        var item = await SeedItemAsync(dbContext, householdId);
        var link = new RecordingLinkClient();
        var service = CreateService(dbContext, Bind(householdId), link);

        var response = await service.CreateUpdateLinkTokenAsync(item.Id);

        Assert.Equal(LinkToken, response.LinkToken);
        Assert.NotNull(response.LinkToken);
        Assert.DoesNotContain(AccessToken, response.LinkToken, StringComparison.Ordinal);
        var request = link.Request;
        Assert.NotNull(request);
        Assert.Equal(AccessToken, request.AccessToken);
        Assert.Null(request.Products);
        Assert.Equal(householdId.ToString("D"), request.User!.ClientUserId);
    }

    [Fact]
    public async Task CreateUpdateLinkToken_OtherHousehold_IsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var ownerId = await SeedHouseholdAsync(dbContext);
        var otherId = await SeedHouseholdAsync(dbContext);
        var item = await SeedItemAsync(dbContext, ownerId);
        var link = new RecordingLinkClient();
        var service = CreateService(dbContext, Bind(otherId), link);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateUpdateLinkTokenAsync(item.Id));

        Assert.Equal(0, link.Calls);
    }

    [Fact]
    public async Task GetPlaidItems_NeedsRepairWhenTheLatestAttemptFailed()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await SeedHouseholdAsync(dbContext);
        await SeedItemAsync(dbContext, householdId, "Broken", Now.AddHours(-2), Now);
        await SeedItemAsync(dbContext, householdId, "Healthy", Now, Now.AddHours(-2));
        var service = CreateService(dbContext, Bind(householdId), new RecordingLinkClient());

        var items = await service.GetPlaidItemsAsync();

        Assert.Equal(2, items.Count);
        Assert.True(items.Single(item => item.InstitutionName == "Broken").NeedsRepair);
        Assert.False(items.Single(item => item.InstitutionName == "Healthy").NeedsRepair);
    }

    #region Private Methods

    /// <summary>
    /// Builds a service whose Link calls are recorded instead of sent.
    /// </summary>
    private static PlaidService CreateService(
        CarduiDBContext dbContext,
        HouseholdScope scope,
        RecordingLinkClient link)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new PlaidConfig
        {
            ClientId = "test-client",
            Secret = "test-secret",
            Environment = "sandbox",
            WebhookUrl = "https://example.test/plaid"
        });

        return new PlaidService(
            dbContext,
            null!,
            options,
            new PlaidRequestExecutor(options, NullLogger<PlaidRequestExecutor>.Instance),
            null!,
            null!,
            new MappingAccessTokenProtector(),
            new PlaidItemRemoval(dbContext, TimeProvider.System),
            scope,
            NullLogger<PlaidService>.Instance,
            TimeProvider.System,
            link);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    /// <summary>
    /// Adds a household and returns its id.
    /// </summary>
    private static async Task<Guid> SeedHouseholdAsync(CarduiDBContext dbContext)
    {
        var householdId = Guid.NewGuid();
        dbContext.Households.Add(new Household
        {
            Id = householdId,
            OwnerClerkUserId = "owner-" + householdId.ToString("N"),
            DisplayName = "Household",
            TimeZoneId = "America/Denver",
            CreatedAt = Now.AddDays(-30),
            UpdatedAt = Now.AddDays(-30)
        });
        await dbContext.SaveChangesAsync();
        return householdId;
    }

    /// <summary>
    /// Adds one bank connection for the household.
    /// </summary>
    private static async Task<PlaidItem> SeedItemAsync(
        CarduiDBContext dbContext,
        Guid householdId,
        string? institutionName = null,
        DateTimeOffset? completedAt = null,
        DateTimeOffset? failedAt = null)
    {
        var item = new PlaidItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = "item-" + Guid.NewGuid().ToString("N"),
            AccessToken = StoredAccessToken,
            InstitutionName = institutionName,
            LastSyncCompletedAt = completedAt,
            LastSyncFailedAt = failedAt,
            CreatedAt = Now.AddDays(-10),
            UpdatedAt = Now.AddDays(-10)
        };
        dbContext.PlaidItems.Add(item);
        await dbContext.SaveChangesAsync();
        return item;
    }

    #endregion

    private sealed class RecordingLinkClient : IPlaidLinkClient
    {
        public LinkTokenCreateRequest? Request { get; private set; }

        public int Calls { get; private set; }

        /// <summary>
        /// Records the request Plaid would receive and returns a link token.
        /// </summary>
        public Task<string?> CreateAsync(
            LinkTokenCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Request = request;
            return Task.FromResult<string?>(LinkToken);
        }
    }

    private sealed class MappingAccessTokenProtector : IPlaidAccessTokenProtector
    {
        public string Protect(string accessToken) => accessToken;

        /// <summary>
        /// Returns the plain token for the stored test value.
        /// </summary>
        public string Unprotect(string storedAccessToken)
        {
            return storedAccessToken == StoredAccessToken
                ? AccessToken
                : storedAccessToken;
        }
    }
}
