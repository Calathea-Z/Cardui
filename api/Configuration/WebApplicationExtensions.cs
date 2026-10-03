using Cardui.Api.Data;

namespace Cardui.Api.Configuration;

public static class WebApplicationExtensions
{
    public static async Task UseCarduiApiAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CarduiDBContext>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            await DataSeeder.SeedAsync(dbContext, timeProvider);

            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseExceptionHandler();
        app.UseCors(CorsPolicyNames.Frontend);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
        app.MapControllers();
    }
}
