// <copyright file="App.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SophiApp.Contracts.Services;
using SophiApp.RequirementsViewModels;
using SophiApp.RequirementsViews;
using SophiApp.Services;
using SophiApp.ViewModels;
using SophiApp.Views;

/// <summary>
/// <inheritdoc/>
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        InitializeComponent();
        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                // Services
                _ = services.AddSingleton<ICommonDataService, CommonDataService>();
                _ = services.AddSingleton<IFileService, FileService>();
                _ = services.AddSingleton<IFirewallService, FirewallService>();
                _ = services.AddSingleton<IInitializeService, InitializeService>();
                _ = services.AddSingleton<IInstrumentationService, InstrumentationService>();
                _ = services.AddSingleton<ILoggerService, LoggerService>();
                _ = services.AddSingleton<IModelService, ModelService>();
                _ = services.AddSingleton<INavigationService, NavigationService>();
                _ = services.AddSingleton<IPageService, PageService>();
                _ = services.AddSingleton<ISettingsService, SettingsService>();
                _ = services.AddSingleton<IThemesService, ThemesService>();
                _ = services.AddTransient<IAppNotificationService, AppNotificationService>();
                _ = services.AddTransient<IAppxPackagesService, AppxPackagesService>();
                _ = services.AddTransient<ICursorsService, CursorsService>();
                _ = services.AddTransient<IDisplayService, DisplayService>();
                _ = services.AddTransient<IGroupPolicyService, GroupPolicyService>();
                _ = services.AddTransient<IHttpService, HttpService>();
                _ = services.AddTransient<INavigationViewService, NavigationViewService>();
                _ = services.AddTransient<IOneDriveService, OneDriveService>();
                _ = services.AddTransient<IOsService, OsService>();
                _ = services.AddTransient<IPowerShellService, PowerShellService>();
                _ = services.AddTransient<IProcessService, ProcessService>();
                _ = services.AddTransient<IRedistributablePackageService, RedistributablePackageService>();
                _ = services.AddTransient<IRegistryService, RegistryService>();
                _ = services.AddTransient<IRequirementsService, RequirementsService>();
                _ = services.AddTransient<IScheduledTaskService, ScheduledTaskService>();
                _ = services.AddTransient<IUpdateService, UpdateService>();
                _ = services.AddTransient<IXmlService, XmlService>();

                // ViewModels
                _ = services.AddSingleton<ShellViewModel>();
                _ = services.AddScoped<StartupViewModel>();
                _ = services.AddTransient<AntiSpywareDisabledViewModel>();
                _ = services.AddTransient<BitLockerEncryptOrDecryptViewModel>();
                _ = services.AddTransient<BitLockerProtectionStatusViewModel>();
                _ = services.AddTransient<ContextMenuViewModel>();
                _ = services.AddTransient<DefenderControlledFolderEnableViewModel>();
                _ = services.AddTransient<DefenderFileMissingViewModel>();
                _ = services.AddTransient<DefenderSecurityHealthFailureViewModel>();
                _ = services.AddTransient<DefenderServiceFailureViewModel>();
                _ = services.AddTransient<DefenderSettingsPageHiddenViewModel>();
                _ = services.AddTransient<DetectHostFileEntriesViewModel>();
                _ = services.AddTransient<EventLogBrokenViewModel>();
                _ = services.AddTransient<FatalErrorViewModel>();
                _ = services.AddTransient<FeatureExperiencePackRemovedViewModel>();
                _ = services.AddTransient<Is32BitOsViewModel>();
                _ = services.AddTransient<LogViewModel>();
                _ = services.AddTransient<MalwareDetectedViewModel>();
                _ = services.AddTransient<MsStoreRemovedViewModel>();
                _ = services.AddTransient<PersonalizationViewModel>();
                _ = services.AddTransient<PrivacyViewModel>();
                _ = services.AddTransient<ProVersionViewModel>();
                _ = services.AddTransient<RebootRequiredViewModel>();
                _ = services.AddTransient<RunByNotLoggedUserViewModel>();
                _ = services.AddTransient<SearchViewModel>();
                _ = services.AddTransient<SecurityViewModel>();
                _ = services.AddTransient<SettingsViewModel>();
                _ = services.AddTransient<SystemViewModel>();
                _ = services.AddTransient<TaskSchedulerViewModel>();
                _ = services.AddTransient<UwpViewModel>();
                _ = services.AddTransient<WinUnsupportedBuildViewModel>();
                _ = services.AddTransient<WinUnsupportedUbrViewModel>();
                _ = services.AddTransient<WmiBrokenViewModel>();

                // Views
                _ = services.AddTransient<AntiSpywareDisabledPage>();
                _ = services.AddTransient<BitLockerEncryptOrDecryptPage>();
                _ = services.AddTransient<BitLockerProtectionStatusPage>();
                _ = services.AddTransient<ContextMenuPage>();
                _ = services.AddTransient<DefenderControlledFolderEnablePage>();
                _ = services.AddTransient<DefenderFileMissingPage>();
                _ = services.AddTransient<DefenderSecurityHealthFailurePage>();
                _ = services.AddTransient<DefenderServiceFailurePage>();
                _ = services.AddTransient<DefenderSettingsPageHiddenPage>();
                _ = services.AddTransient<DetectHostFileEntriesPage>();
                _ = services.AddTransient<EventLogBrokenPage>();
                _ = services.AddTransient<FatalErrorPage>();
                _ = services.AddTransient<FeatureExperiencePackRemovedPage>();
                _ = services.AddTransient<Is32BitOsPage>();
                _ = services.AddTransient<LogPage>();
                _ = services.AddTransient<MalwareDetectedPage>();
                _ = services.AddTransient<MsStoreRemovedPage>();
                _ = services.AddTransient<PersonalizationPage>();
                _ = services.AddTransient<PrivacyPage>();
                _ = services.AddTransient<ProVersionPage>();
                _ = services.AddTransient<RebootRequiredPage>();
                _ = services.AddTransient<RunByNotLoggedUserPage>();
                _ = services.AddTransient<SearchPage>();
                _ = services.AddTransient<SecurityPage>();
                _ = services.AddTransient<SettingsPage>();
                _ = services.AddTransient<ShellPage>();
                _ = services.AddTransient<StartupPage>();
                _ = services.AddTransient<SystemPage>();
                _ = services.AddTransient<TaskSchedulerPage>();
                _ = services.AddTransient<UwpPage>();
                _ = services.AddTransient<WinUnsupportedBuildPage>();
                _ = services.AddTransient<WinUnsupportedUbrPage>();
                _ = services.AddTransient<WmiBrokenPage>();
            })
            .Build();

        UnhandledException += App_UnhandledException;
    }

    /// <summary>
    /// Gets app main window.
    /// </summary>
    public static WindowEx MainWindow { get; } = new MainWindow();

    /// <summary>
    /// Gets or sets app title bar.
    /// </summary>
    public static UIElement? AppTitlebar { get; set; }

    /// <summary>
    /// Gets <see cref="ILoggerService"/>.
    /// </summary>
    public static ILoggerService Logger { get; } = GetService<ILoggerService>();

    /// <summary>
    /// Gets <see cref="IHost"/>.
    /// </summary>
    public IHost Host { get; init; }

    /// <summary>
    /// Gets app service.
    /// </summary>
    /// <typeparam name="T">Service type.</typeparam>
    public static T GetService<T>()
        where T : class
    {
        if ((Current as App) !.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    /// <summary>
    /// Allows only one copy of the app to run.
    /// </summary>
    public static void SetSingleInstance()
    {
        var keyInstance = AppInstance.FindOrRegisterForKey("2e340960-5e58-4e2d-b0c1-0a1b54145345");

        if (!keyInstance.IsCurrent)
        {
            Current.Exit();
        }
    }

    /// <inheritdoc/>
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        GetService<IAppNotificationService>().RegisterAsToastSender("SophiApp");
        var initializeService = GetService<IInitializeService>();
        await initializeService.InitializeServicesDataAsync(args);
        await initializeService.InitializeMainWindowAsync();
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Logger.LogUnhandledException(e.Exception);
        GetService<ShellViewModel>().FatalErrorHandling();
    }
}
