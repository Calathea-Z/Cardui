using System.Security.Cryptography.X509Certificates;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Microsoft.AspNetCore.DataProtection;

namespace Cardui.Api.Configuration;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers application services, data protection for Plaid tokens, and the household scope.
    /// </summary>
    public static IServiceCollection AddCarduiApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        AddPlaidTokenProtection(services, configuration, environment);

        services.AddScoped<HouseholdScope>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPlaidAccessTokenProtector, DataProtectionPlaidAccessTokenProtector>();
        services.AddSingleton<IPlaidWebhookKeySource, PlaidWebhookKeySource>();

        services.AddScoped<IHouseholdsService, HouseholdsService>();
        services.AddScoped<IFinancialProfileService, FinancialProfileService>();
        services.AddScoped<IIncomeSourcesService, IncomeSourcesService>();
        services.AddScoped<IObligationsService, ObligationsService>();
        services.AddScoped<IDebtsService, DebtsService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ICategoryTargetsService, CategoryTargetsService>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<ITransactionsService, TransactionsService>();
        services.AddScoped<ITransactionImportService, TransactionImportService>();
        services.AddScoped<IGroupsService, GroupsService>();
        services.AddScoped<ISubGroupsService, SubGroupsService>();
        services.AddScoped<ICategoriesService, CategoriesService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPlaidService, PlaidService>();
        services.AddScoped<ITransactionCategorizationService, TransactionCategorizationService>();
        services.AddScoped<ITransferPairingService, TransferPairingService>();

        services.AddSingleton<IPlaidRequestExecutor, PlaidRequestExecutor>();
        services.AddScoped<IPlaidLinkClient, PlaidLinkClient>();
        services.AddScoped<IPlaidAccountsClient, PlaidAccountsClient>();
        services.AddScoped<IPlaidAccountSyncService, PlaidAccountSyncService>();
        services.AddScoped<IPlaidTransactionPageClient, PlaidTransactionPageClient>();
        services.AddScoped<IPlaidTransactionReconciler, PlaidTransactionReconciler>();
        services.AddScoped<IPlaidTransactionSyncService, PlaidTransactionSyncService>();
        services.AddScoped<PlaidItemRemoval>();
        services.AddScoped<PlaidAccessTokenStoreMigrator>();
        services.AddScoped<IPlaidWebhookService, PlaidWebhookService>();

        return services;
    }

    #region Private Methods

    /// <summary>
    /// Protects Plaid access tokens with a Cardui key ring stored in a shared directory.
    /// Production requires that directory and a certificate so the keys are encrypted at rest.
    /// </summary>
    private static void AddPlaidTokenProtection(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var applicationName = configuration["DataProtection:ApplicationName"];
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            applicationName = "Cardui";
        }

        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "DataProtection:KeysPath is required in production.");
            }

            keysPath = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath,
                "..",
                ".data-protection-keys"));
        }

        Directory.CreateDirectory(keysPath);
        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName(applicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        var certificatePath = configuration["DataProtection:CertificatePath"];
        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                configuration["DataProtection:CertificatePassword"]);
            dataProtectionBuilder.ProtectKeysWithCertificate(certificate);
        }
        else if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "DataProtection:CertificatePath is required in production.");
        }
    }

    #endregion
}
