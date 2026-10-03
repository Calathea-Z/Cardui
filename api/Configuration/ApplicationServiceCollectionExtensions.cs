using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Microsoft.AspNetCore.DataProtection;

namespace Cardui.Api.Configuration;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCarduiApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? "Tortoise");
        var keysPath = configuration["DataProtection:KeysPath"];

        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPlaidAccessTokenProtector, DataProtectionPlaidAccessTokenProtector>();

        services.AddScoped<IHouseholdsService, HouseholdsService>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<ITransactionsService, TransactionsService>();
        services.AddScoped<IGroupsService, GroupsService>();
        services.AddScoped<ISubGroupsService, SubGroupsService>();
        services.AddScoped<ICategoriesService, CategoriesService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPlaidService, PlaidService>();
        services.AddScoped<ITransactionCategorizationService, TransactionCategorizationService>();
        services.AddScoped<ITransferPairingService, TransferPairingService>();

        services.AddScoped<IPlaidRequestExecutor, PlaidRequestExecutor>();
        services.AddScoped<IPlaidAccountSyncService, PlaidAccountSyncService>();
        services.AddScoped<IPlaidTransactionPageClient, PlaidTransactionPageClient>();
        services.AddScoped<IPlaidTransactionReconciler, PlaidTransactionReconciler>();
        services.AddScoped<IPlaidTransactionSyncService, PlaidTransactionSyncService>();

        return services;
    }
}
