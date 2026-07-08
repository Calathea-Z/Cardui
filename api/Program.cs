using Cardui.Api.Configuration;
using Cardui.Api.Data;
using Cardui.Api.Middleware;
using Cardui.Api.Options;
using Cardui.Api.Services.Implementations;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<CarduiDBContext>(options =>
    options.UseNpgsql(DatabaseConnectionString.Get(builder.Configuration)));

builder.Services.AddOptions<PlaidOptions>()
    .Bind(builder.Configuration.GetSection("Plaid"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PlaidOptions>, PlaidOptionsValidator>();
builder.Services.AddCarduiPlaid();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IAccountsService, AccountsService>();
builder.Services.AddScoped<ITransactionsService, TransactionsService>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPlaidService, PlaidService>();
builder.Services.AddScoped<ITransactionCategorizationService, TransactionCategorizationService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CarduiDBContext>();
    await DataSeeder.SeedAsync(dbContext);

    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.MapControllers();
app.Run();
