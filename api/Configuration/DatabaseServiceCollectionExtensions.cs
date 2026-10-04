using Cardui.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Configuration;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Postgres database using the configured connection string.
    /// </summary>
    public static IServiceCollection AddCarduiDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CarduiDBContext>(options =>
            options.UseNpgsql(DatabaseConnectionString.Get(configuration)));

        return services;
    }
}
