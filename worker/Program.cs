using Cardui.Api.Configuration;
using Cardui.Api.Data;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCarduiDatabase(builder.Configuration);
builder.Services.AddCarduiPlaid(builder.Configuration);
builder.Services.AddCarduiApplicationServices(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("PlaidDailySyncWorker");
var dbContext = scope.ServiceProvider.GetRequiredService<CarduiDBContext>();
var householdScope = scope.ServiceProvider.GetRequiredService<HouseholdScope>();

var plaidItems = await dbContext.PlaidItems
    .AsNoTracking()
    .OrderBy(x => x.InstitutionName)
    .Select(x => new { x.Id, x.HouseholdId })
    .ToListAsync();

logger.LogInformation("Starting daily Plaid sync for {Count} item(s).", plaidItems.Count);

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
    if (plaidItem.HouseholdId is Guid householdId)
    {
        householdScope.Bind(householdId);
    }
    else
    {
        householdScope.BindUnassigned();
    }

    try
    {
        var result = await plaidService.SyncPlaidItemAsync(plaidItem.Id, cancellationToken);

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
        logger.LogError(ex, "Failed to sync Plaid item {PlaidItemId}.", plaidItem.Id);
    }
}

logger.LogInformation(
    "Daily Plaid sync finished. Successes: {SuccessCount}. Failures: {FailureCount}.",
    successCount,
    failureCount);

Environment.ExitCode = failureCount > 0 ? 1 : 0;
