using Cardui.Api.Configuration;
using Cardui.Api.Data;
using Cardui.Api.Options;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCarduiDatabase(builder.Configuration);
builder.Services.AddCarduiPlaid(builder.Configuration);
builder.Services.AddCarduiApplicationServices(builder.Configuration, builder.Environment);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("PlaidDailySyncWorker");
var migrator = scope.ServiceProvider.GetRequiredService<PlaidAccessTokenStoreMigrator>();
await migrator.MigrateAsync();

var dbContext = scope.ServiceProvider.GetRequiredService<CarduiDBContext>();
var householdScope = scope.ServiceProvider.GetRequiredService<HouseholdScope>();

var plaidItems = await dbContext.PlaidItems
    .AsNoTracking()
    .OrderBy(x => x.InstitutionName)
    .Select(x => new { x.Id, x.HouseholdId })
    .ToListAsync();

logger.LogInformation("Starting daily Plaid sync for {Count} item(s).", plaidItems.Count);

var plaidOptions = scope.ServiceProvider.GetRequiredService<IOptions<PlaidOptions>>().Value;
if (!PlaidConfiguration.IsConfigured(plaidOptions))
{
    logger.LogInformation("Plaid credentials are not configured. Bank linking is off.");
    if (plaidItems.Count > 0)
    {
        logger.LogError(
            "Daily Plaid sync cannot run for {Count} connected item(s) without Plaid credentials.",
            plaidItems.Count);
        Environment.ExitCode = 1;
    }

    return;
}

if (plaidItems.Count == 0)
{
    logger.LogInformation("No connected Plaid items found. Daily sync finished.");
    return;
}

var plaidService = scope.ServiceProvider.GetRequiredService<IPlaidService>();
var successCount = 0;
var failureCount = 0;
var cancellationToken = CancellationToken.None;

foreach (var plaidItem in plaidItems)
{
    if (plaidItem.HouseholdId is not Guid householdId)
    {
        logger.LogWarning(
            "Skipping Plaid item {PlaidItemId} because it has no household.",
            plaidItem.Id);
        continue;
    }

    householdScope.Bind(householdId);

    try
    {
        var result = await plaidService.SyncPlaidItemAsync(plaidItem.Id, cancellationToken);
        if (result.AlreadyRunning)
        {
            logger.LogInformation(
                "Skipped Plaid item {PlaidItemId} because a sync is already running.",
                plaidItem.Id);
            continue;
        }

        logger.LogInformation(
            "Synced Plaid item {PlaidItemId}. Added {Added}, modified {Modified}, removed {Removed}.",
            result.PlaidItemId,
            result.Transactions.Added,
            result.Transactions.Modified,
            result.Transactions.Removed);

        successCount++;
    }
    catch (Exception ex)
    {
        failureCount++;
        logger.LogError(
            "Failed to sync Plaid item {PlaidItemId}. {ErrorType}",
            plaidItem.Id,
            ex.GetType().Name);
    }
}

logger.LogInformation(
    "Daily Plaid sync finished. Successes: {SuccessCount}. Failures: {FailureCount}.",
    successCount,
    failureCount);

Environment.ExitCode = failureCount > 0 ? 1 : 0;
