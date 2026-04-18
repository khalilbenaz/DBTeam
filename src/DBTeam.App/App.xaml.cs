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
            .Enrich.WithProperty("AppVersion", typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown")
            .Enrich.WithProperty("OSVersion", Environment.OSVersion.ToString())
            .WriteTo.File(System.IO.Path.Combine(logDir, "app-.log"), rollingInterval: RollingInterval.Day,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("App starting · version {AppVersion}", typeof(App).Assembly.GetName().Version);

        AppDomain.CurrentDomain.UnhandledException += (_, a) => HandleFatal(a.ExceptionObject as Exception, "AppDomain.UnhandledException");
        DispatcherUnhandledException += (_, a) => { HandleNonFatal(a.Exception, "Dispatcher.UnhandledException"); a.Handled = true; };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, a) => { HandleNonFatal(a.Exception, "TaskScheduler.UnobservedTaskException"); a.SetObserved(); };

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
        s.AddSingleton<IQueryHistoryStore, JsonLinesQueryHistoryStore>();
        s.AddSingleton<IEventBus, EventBus>();

        s.AddSingleton<Services.ThemeService>();
        s.AddSingleton<Services.LocalizationService>();
        s.AddSingleton<Services.SessionService>();
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
        DBTeam.Modules.Debugger.ModuleRegistration.Register(s);
        DBTeam.Modules.Admin.ModuleRegistration.Register(s);
        DBTeam.Modules.Terminal.ModuleRegistration.Register(s);
        DBTeam.Modules.AiAssistant.ModuleRegistration.Register(s);
        DBTeam.Modules.Monitoring.ModuleRegistration.Register(s);
        DBTeam.Modules.Import.ModuleRegistration.Register(s);
        DBTeam.Modules.QueryBuilder.ModuleRegistration.Register(s);
        DBTeam.Modules.Git.ModuleRegistration.Register(s);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void HandleNonFatal(Exception? ex, string source)
    {
        if (ex is null) return;
        Log.Error(ex, "Non-fatal exception from {Source}", source);
        try
        {
            Current?.Dispatcher?.Invoke(() =>
            {
                var dlg = new Shell.ErrorDialog(ex, source);
                if (Current.MainWindow is { IsLoaded: true } w) dlg.Owner = w;
                dlg.ShowDialog();
            });
        }
        catch (Exception inner) { Log.Error(inner, "Failed to show error dialog"); }
    }

    private static void HandleFatal(Exception? ex, string source)
    {
        Log.Fatal(ex, "Fatal exception from {Source}", source);
        Log.CloseAndFlush();
    }
}
