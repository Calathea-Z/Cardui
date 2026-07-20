using Cardui.Api.Configuration;
using Cardui.Api.Data;
using Cardui.Api.Options;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<CarduiDBContext>(options =>
{
    options.UseNpgsql(DatabaseConnectionString.Get(builder.Configuration));
});

builder.Services.AddOptions<PlaidOptions>()
    .Bind(builder.Configuration.GetSection("Plaid"))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<PlaidOptions>, PlaidOptionsValidator>();
builder.Services.AddCarduiPlaid();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IPlaidService, PlaidService>();
builder.Services.AddScoped<ITransactionCategorizationService, TransactionCategorizationService>();
builder.Services.AddScoped<ITransferPairingService, TransferPairingService>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("PlaidDailySyncWorker");
var dbContext = scope.ServiceProvider.GetRequiredService<CarduiDBContext>();

var plaidItemIds = await dbContext.PlaidItems
    .AsNoTracking()
    .OrderBy(x => x.InstitutionName)
    .Select(x => x.Id)
    .ToListAsync();

logger.LogInformation("Starting daily Plaid sync for {Count} item(s).", plaidItemIds.Count);

if (plaidItemIds.Count == 0)
{
    logger.LogInformation("No connected Plaid items found. Daily sync finished.");
    return;
}

var plaidService = scope.ServiceProvider.GetRequiredService<IPlaidService>();
var successCount = 0;
var failureCount = 0;

foreach (var plaidItemId in plaidItemIds)
    try
    {
        var result = await plaidService.SyncPlaidItemAsync(plaidItemId);

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
        logger.LogError(ex, "Failed to sync Plaid item {PlaidItemId}.", plaidItemId);
    }

logger.LogInformation(
    "Daily Plaid sync finished. Successes: {SuccessCount}. Failures: {FailureCount}.",
    successCount,
    failureCount);

Environment.ExitCode = failureCount > 0 ? 1 : 0;