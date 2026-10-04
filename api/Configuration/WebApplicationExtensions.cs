using Cardui.Api.Data;
using Cardui.Api.Middleware;
using Cardui.Api.Services.Plaid;

namespace Cardui.Api.Configuration;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the API pipeline: security headers, exception handling, CORS,
    /// Clerk authentication, rate limits, household scope, GET /api/health, and controllers.
    /// It rewraps stored Plaid tokens before serving. In development it also seeds
    /// system categories when none exist and maps OpenAPI.
    /// </summary>
    public static async Task UseCarduiApiAsync(this WebApplication app)
    {
        ProductionSecurityGuard.Ensure(app.Environment, app.Configuration);

        if (!app.Environment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();
            var migrator = scope.ServiceProvider
                .GetRequiredService<PlaidAccessTokenStoreMigrator>();
            await migrator.MigrateAsync();

            if (app.Environment.IsDevelopment())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CarduiDBContext>();
                var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
                await DataSeeder.SeedAsync(dbContext, timeProvider);
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        app.UseMiddleware<SecurityHeadersMiddleware>();
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseExceptionHandler();
        app.UseCors(CorsPolicyNames.Frontend);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseMiddleware<HouseholdScopeMiddleware>();
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
            .AllowAnonymous();
        app.MapControllers();
    }
}
