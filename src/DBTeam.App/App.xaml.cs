using System;
using System.Windows;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Infrastructure;
using DBTeam.Data.Security;
using DBTeam.Data.Sql;
using DBTeam.Data.Stores;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DBTeam.App;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBTeam", "logs");
        System.IO.Directory.CreateDirectory(logDir);
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(System.IO.Path.Combine(logDir, "app-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
        DBTeam.Core.Infrastructure.ServiceLocator.Services = Services;

        Services.GetRequiredService<Services.LocalizationService>().LoadAndApply();
        Services.GetRequiredService<Services.ThemeService>().LoadAndApply();

        var main = Services.GetRequiredService<Shell.MainWindow>();
        MainWindow = main;
        main.Show();
    }

    private static void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton<ISecretProtector, DpapiProtector>();
        s.AddSingleton<IConnectionStore, JsonConnectionStore>();
        s.AddSingleton<IConnectionService, SqlServerConnectionService>();
        s.AddSingleton<IDatabaseMetadataService, SqlServerMetadataService>();
        s.AddSingleton<IQueryExecutionService, SqlServerQueryExecutionService>();
        s.AddSingleton<IEventBus, EventBus>();

        s.AddSingleton<Services.ThemeService>();
        s.AddSingleton<Services.LocalizationService>();
        s.AddSingleton<Shell.MainWindow>();
        s.AddSingleton<ViewModels.MainViewModel>();

        DBTeam.Modules.ConnectionManager.ModuleRegistration.Register(s);
        DBTeam.Modules.ObjectExplorer.ModuleRegistration.Register(s);
        DBTeam.Modules.QueryEditor.ModuleRegistration.Register(s);
        DBTeam.Modules.ResultsGrid.ModuleRegistration.Register(s);
        DBTeam.Modules.SchemaCompare.ModuleRegistration.Register(s);
        DBTeam.Modules.DataCompare.ModuleRegistration.Register(s);
        DBTeam.Modules.TableDesigner.ModuleRegistration.Register(s);
        DBTeam.Modules.Profiler.ModuleRegistration.Register(s);
        DBTeam.Modules.DataGenerator.ModuleRegistration.Register(s);
        DBTeam.Modules.Documenter.ModuleRegistration.Register(s);
        DBTeam.Modules.Diagram.ModuleRegistration.Register(s);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
