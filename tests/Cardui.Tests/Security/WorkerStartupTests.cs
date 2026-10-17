using Cardui.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Cardui.Tests.Security;

public class WorkerStartupTests
{
    /// <summary>
    /// Builds the worker's service list with blank planning-model settings.
    /// The host must start even though the worker never registers a planning model.
    /// </summary>
    [Fact]
    public void Host_BuildsWithoutAPlanningModel()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=127.0.0.1;Port=1;Database=cardui;Username=cardui_user;Password=test",
            ["DataProtection:KeysPath"] = Path.Combine(Path.GetTempPath(), "cardui-test-keys"),
        });

        builder.Services.AddCarduiDatabase(builder.Configuration);
        builder.Services.AddCarduiPlaid(builder.Configuration);
        builder.Services.AddCarduiApplicationServices(builder.Configuration, builder.Environment);

        using var host = builder.Build();
    }
}
