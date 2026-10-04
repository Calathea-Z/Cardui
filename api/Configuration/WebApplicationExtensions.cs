using Cardui.Api.Data;
using Cardui.Api.Middleware;
using Cardui.Api.Options;
using Cardui.Api.Services.Plaid;
using Microsoft.Extensions.Options;

namespace Cardui.Api.Configuration;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the API pipeline: security headers, exception handling, CORS,
    /// Clerk authentication, rate limits, household scope, GET /api/health, and controllers.
    /// It logs whether Plaid credentials are configured, then rewraps stored Plaid
    /// tokens before serving. In development it also seeds
    /// system categories when none exist and maps OpenAPI.
    /// </summary>
    public static async Task UseCarduiApiAsync(this WebApplication app)
    {
        ProductionSecurityGuard.Ensure(app.Environment, app.Configuration);
        LogPlaidMode(app);

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

    #region Private Methods

    /// <summary>
    /// Logs whether bank linking is available. The client id and secret are not logged.
    /// Reading the options validates them, so a partial configuration fails here.
    /// </summary>
    private static void LogPlaidMode(WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<PlaidOptions>>().Value;
        if (PlaidConfiguration.IsConfigured(options))
        {
            app.Logger.LogInformation(
                "Plaid is configured for environment {PlaidEnvironment}.",
                options.Environment);
            return;
        }

        app.Logger.LogInformation(
            "Plaid credentials are not configured. Bank linking is off.");
    }

    #endregion
}
