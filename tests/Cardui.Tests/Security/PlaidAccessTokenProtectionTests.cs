using System.Security.Cryptography;
using Cardui.Api.Data;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Plaid;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cardui.Tests.Security;

public class PlaidAccessTokenProtectionTests
{
    [Fact]
    public void Unprotect_RejectsPlaintext()
    {
        var protector = new DataProtectionPlaidAccessTokenProtector(
            new EphemeralDataProtectionProvider());

        var stored = protector.Protect("sandbox-token");

        Assert.StartsWith(PlaidAccessTokenCipher.Prefix, stored);
        Assert.Equal("sandbox-token", protector.Unprotect(stored));
        Assert.Throws<CryptographicException>(() => protector.Unprotect("sandbox-token"));
    }

    [Fact]
    public async Task Migrate_RewrapsPlaintext()
    {
        await using var dbContext = CreateDbContext();
        var current = new DataProtectionPlaidAccessTokenProtector(
            new EphemeralDataProtectionProvider());
        var plaintextId = Guid.NewGuid();
        dbContext.PlaidItems.Add(CreateItem(plaintextId, "plain-token"));
        await dbContext.SaveChangesAsync();

        var migrator = new PlaidAccessTokenStoreMigrator(
            dbContext,
            current,
            NullLogger<PlaidAccessTokenStoreMigrator>.Instance);

        await migrator.MigrateAsync();

        var plaintext = await dbContext.PlaidItems.SingleAsync(item => item.Id == plaintextId);
        Assert.Equal("plain-token", current.Unprotect(plaintext.AccessToken));
    }

    [Fact]
    public async Task Migrate_RejectsATokenFromAnotherKeyRing()
    {
        await using var dbContext = CreateDbContext();
        var otherKey = new EphemeralDataProtectionProvider()
            .CreateProtector(PlaidAccessTokenCipher.Purpose);
        var current = new DataProtectionPlaidAccessTokenProtector(
            new EphemeralDataProtectionProvider());
        dbContext.PlaidItems.Add(CreateItem(
            Guid.NewGuid(),
            PlaidAccessTokenCipher.Protect(otherKey, "other-ring-token")));
        await dbContext.SaveChangesAsync();

        var migrator = new PlaidAccessTokenStoreMigrator(
            dbContext,
            current,
            NullLogger<PlaidAccessTokenStoreMigrator>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            migrator.MigrateAsync());

        Assert.Contains("Reconnect that bank", exception.Message, StringComparison.Ordinal);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }

    private static PlaidItem CreateItem(Guid id, string accessToken)
    {
        return new PlaidItem
        {
            Id = id,
            HouseholdId = Guid.NewGuid(),
            PlaidItemId = id.ToString("N"),
            AccessToken = accessToken,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };
    }
}
