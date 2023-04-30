using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Extensions.DependencyInjection;

namespace BloodPressureRecorder;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets("69b2ae72-b3a9-4075-bd09-b1e0dfd0880a")
            .Build();

        var services = new ServiceCollection()
            .AddSenseNetClient()
            .ConfigureSenseNetRepository(
                configure: options => { config.Bind("sensenet", options); },
                registerContentTypes: types =>
                {
                    types.Add<AppFolder>(AppFolder.ContentTypeName);
                    types.Add<BloodPressure>(BloodPressure.ContentTypeName);
                })
            .AddLogging(logging => logging.AddConsole())
            .AddSingleton<IDataHandler, DataHandler>()
            .BuildServiceProvider();

        /*
        var repos = services.GetRequiredService<IRepositoryCollection>();
        var repo = await repos.GetRepositoryAsync(CancellationToken.None);
        var children = await repo.LoadCollectionAsync(
            new LoadCollectionRequest{Path = "/Root"}, CancellationToken.None);
        await using var writer = new StringWriter();
        foreach (var content in children)
            writer.WriteLine($"  {content.Name,-20} {content["Type"]}");
        var initInfo = writer.GetStringBuilder().ToString();
        */

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(services, null));
    }
}