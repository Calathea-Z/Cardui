using Cardui.Api.Configuration;
using Cardui.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCarduiDatabase(builder.Configuration);
builder.Services.AddCarduiPlaid(builder.Configuration);
builder.Services.AddCarduiApplicationServices(builder.Configuration, builder.Environment);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        CarduiJsonSerializer.UseEnumMemberNames(options.JsonSerializerOptions);
    });
builder.Services.AddCarduiCors(builder.Configuration);
builder.Services.AddCarduiClerkAuthentication(builder.Configuration);
builder.Services.AddCarduiRateLimiter();

var app = builder.Build();

await app.UseCarduiApiAsync();
app.Run();
