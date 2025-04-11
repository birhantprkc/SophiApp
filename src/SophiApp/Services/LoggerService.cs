// <copyright file="LoggerService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using System.Diagnostics;
    using System.Globalization;
    using System.ServiceProcess;
    using Microsoft.UI.Xaml;
    using Serilog;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;
    using SophiApp.ViewModels;

    /// <inheritdoc/>
    public class LoggerService : ILoggerService
    {
        private readonly string logFile = $"{AppContext.BaseDirectory}\\Log\\SophiApp-{Environment.MachineName.ToUpper()}.log";
        private readonly ShellViewModel shellViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerService"/> class.
        /// </summary>
        public LoggerService()
        {
            logFile.TryDelete();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    logFile,
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: "[{Level:u3}] {Message}{NewLine}")
                .CreateLogger();

            shellViewModel = App.GetService<ShellViewModel>();
        }

        /// <inheritdoc/>
        public void LogOsProperties(OsProperties properties)
        {
            var dateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            Log.Information("Windows version: {Caption:l}", properties.Caption);
            Log.Information("Windows edition: {Edition:l}", properties.Edition);
            Log.Information("Windows build: {Build}.{Ubr}", properties.BuildNumber, properties.UpdateBuildRevision);
            Log.Information("Computer name: {ComputerName:l}", properties.CSName);
            Log.Information("User name: {UserName:l}", Environment.UserName);
            Log.Information("User culture: {Culture:l}", CultureInfo.CurrentCulture.EnglishName);
            Log.Information("User region: {Region:l}", RegionInfo.CurrentRegion.EnglishName);
            Log.Information("User date and time: {DateTime:l}", dateTime);
            Log.Information("User time zone: {TimeZone:l}", TimeZoneInfo.Local.DisplayName);

            shellViewModel.LoggedActions =
            [
                $"[INF] Windows version: {properties.Caption}", $"[INF] Windows edition: {properties.Edition}", $"[INF] Windows build: {properties.BuildNumber}.{properties.UpdateBuildRevision}",
                $"[INF] Computer name: {properties.CSName}", $"[INF] User name: {Environment.UserName}", $"[INF] User culture: {CultureInfo.CurrentCulture.EnglishName}",
                $"[INF] User region: {RegionInfo.CurrentRegion.EnglishName}", $"[INF] User date and time: {dateTime}", $"[INF] User time zone: {TimeZoneInfo.Local.DisplayName}",
            ];
        }

        /// <inheritdoc/>
        public void LogAppProperties(Version version, string directory)
        {
            Log.Information("App version: {Version:l}", version);
            Log.Information("App directory: {Directory}", directory);
            shellViewModel.LoggedActions.AddRange([$"[INF] App version: {version}", $"[INF] App directory: {directory}"]);
        }

        /// <inheritdoc/>
        public void LogNavigateToPage(string name)
        {
            Log.Information("Navigate to page: {Name:l}", name);
            shellViewModel.LoggedActions.Add($"[INF] Navigate to page: {name}");
        }

        /// <inheritdoc/>
        public void LogChangeTheme(ElementTheme theme)
        {
            Log.Information("Change theme to: {Theme}", theme);
            shellViewModel.LoggedActions.Add($"[INF] Change theme to: {theme}");
        }

        /// <inheritdoc/>
        public void LogOpenedUrl(string url)
        {
            Log.Information("Opened url: {Url}", url);
            shellViewModel.LoggedActions.Add($"[INF] Opened url: {url}");
        }

        /// <inheritdoc/>
        public void LogOsBitness(bool is64BitOs)
        {
            Log.Information("Is x64: {Is64BitOs}", is64BitOs);
            shellViewModel.LoggedActions.Add($"[INF] Is x64: {is64BitOs}");
        }

        /// <inheritdoc/>
        public void LogWMIState(ServiceControllerStatus serviceState, int repositoryExitCode, bool repositoryIsConsistent)
        {
            Log.Information("WMI service state: {ServiceState}", serviceState);
            Log.Information("WMI verify repository exit code: {ExitCode}", repositoryExitCode);
            Log.Information("WMI repository is consistent: {RepositoryIsConsistent}", repositoryIsConsistent);
            shellViewModel.LoggedActions.AddRange([$"[INF] WMI service state: {serviceState}", $"[INF] WMI verify repository exit code: {repositoryExitCode}", $"[INF] WMI repository is consistent: {repositoryIsConsistent}"]);
        }

        /// <inheritdoc/>
        public void LogMalwareDetected(string name)
        {
            Log.Warning("Malware detected: {Malware:l} in the {Service:l}", name, nameof(IRequirementsService));
            shellViewModel.LoggedActions.Add($"[INF] Malware detected: {name} in the {nameof(IRequirementsService)}");
        }

        /// <inheritdoc/>
        public void LogAppUpdate(Version version)
        {
            Log.Information("App version available in the repository: {Version:l}", version);
            shellViewModel.LoggedActions.Add($"[INF] App version available in the repository: {version}");
        }

        /// <inheritdoc/>
        public void LogAllModelsBuilt(int count)
        {
            Log.Information("Service {Service:l} built {Count} UI models", nameof(IModelService), count);
            shellViewModel.LoggedActions.Add($"[INF] Service {nameof(IModelService)} built {count} UI models");
        }

        /// <inheritdoc/>
        public void LogStartModelsGetState()
        {
            Log.Warning("Service {Service:l} has started initialization of UI models", nameof(IModelService));
            shellViewModel.LoggedActions.Add($"[WRN] Service {nameof(IModelService)} has started initialization of UI models");
        }

        /// <inheritdoc/>
        public void LogStartApplicableModelsSetState()
        {
            Log.Warning("Service {Service:l} has started set UI models state in the applicable models collection", nameof(IModelService));
            shellViewModel.LoggedActions.Add($"[WRN] Service {nameof(IModelService)} has started set UI models state in the applicable models collection");
        }

        /// <inheritdoc/>
        public void LogAllModelsGetState(Stopwatch timer, int count)
        {
            Log.Warning("Service {Service:l} took time to get {Count} UI models state: {TimeSpent}", nameof(IModelService), count, timer.Elapsed);
            shellViewModel.LoggedActions.Add($"[WRN] Service {nameof(IModelService)} took time to get {count} UI models state: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogAllModelsSetState(Stopwatch timer, int count)
        {
            Log.Warning("Service {Service:l} took time to set {Count} UI model(s) state: {TimeSpent}", nameof(IModelService), count, timer.Elapsed);
            shellViewModel.LoggedActions.Add($"[WRN] Service {nameof(IModelService)} took time to set {count} UI model(s) state: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogAllModelsSetStateCanceled()
        {
            Log.Warning("Service {Service:l} has cancel set UI model(s) state in the applicable models collection", nameof(IModelService));
            shellViewModel.LoggedActions.Add($"[WRN] Service {nameof(IModelService)} has cancel set UI model(s) state in the applicable models collection");
        }

        /// <inheritdoc/>
        public void LogModelGetState(string name, Stopwatch timer)
        {
            Log.Information("UI model {Name:l} took time to get state: {TimeSpent}", name, timer.Elapsed);
            shellViewModel.LoggedActions.Add($"[INF] UI model {name} took time to get state: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogModelSetState(string name, Stopwatch timer)
        {
            Log.Information("UI model {Name:l} took time to set state: {TimeSpent}", name, timer.Elapsed);
            shellViewModel.LoggedActions.Add($"[INF] UI model {name} took time to set state: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogModelState<T>(string name, T state)
            where T : struct
        {
            Log.Information("UI model {Name:l} has state: {State}", name, state);
            shellViewModel.LoggedActions.Add($"[INF] UI model {name} has state: {state}");
        }

        /// <inheritdoc/>
        public void LogApplicableModelsCanceled()
        {
            Log.Warning("The applying of customizations has been canceled");
            shellViewModel.LoggedActions.Add($"[WRN] The applying of customizations has been canceled");
        }

        /// <inheritdoc/>
        public void LogApplicableModelsClear()
        {
            Log.Warning("Applicable models collection has been cleaned up");
            shellViewModel.LoggedActions.Add($"[WRN] Applicable models collection has been cleaned up");
        }

        /// <inheritdoc/>
        public void LogApplicableModelChanged<T>(string name, T previous, T current)
            where T : struct
        {
            Log.Information("The value of the UI model {Name:l} parameter has been changed from {Previous} to {Current} in applicable models collection", name, previous, current);
            shellViewModel.LoggedActions.Add($"[INF] The value of the UI model {name} parameter has been changed from {previous} to {current} in applicable models collection");
        }

        /// <inheritdoc/>
        public void LogApplicableModelRemoved(string name)
        {
            Log.Information("UI model {Name:l} has been removed from applicable models collection", name);
            shellViewModel.LoggedActions.Add($"[INF] UI model {name} has been removed from applicable models collection");
        }

        /// <inheritdoc/>
        public void LogApplicableModelAdded(string name)
        {
            Log.Information("UI model {Name:l} has been added to applicable models collection", name);
            shellViewModel.LoggedActions.Add($"[INF] UI model {name} has been added to applicable models collection");
        }

        /// <inheritdoc/>
        public void LogApplicableModelAdded<T>(string name, T parameter)
            where T : struct
        {
            Log.Information("UI model {Name:l} with parameter {Parameter} has been added to applicable models collection", name, parameter);
            shellViewModel.LoggedActions.Add($"[INF] UI model {name} with parameter {parameter} has been added to applicable models collection");
        }

        /// <inheritdoc/>
        public void LogUwpForAllUsersState(bool state)
        {
            Log.Information("The UWP For All Users checkbox state has been changed to {State}", state);
            shellViewModel.LoggedActions.Add($"[INF] The UWP For All Users checkbox state has been changed to {state}");
        }

        /// <inheritdoc/>
        public void LogDescriptionTextSizeChanged(int size)
        {
            Log.Information("The text size of UI element descriptions set to {Size}", size);
            shellViewModel.LoggedActions.Add($"[INF] The text size of UI element descriptions set to {size}");
        }

        /// <inheritdoc/>
        public void LogTitleTextSizeChanged(int size)
        {
            Log.Information("The text size of UI element headers set to {Size}", size);
            shellViewModel.LoggedActions.Add($"[INF] The text size of UI element headers set to {size}");
        }

        /// <inheritdoc/>
        public void LogStartTextSearch(string text)
        {
            Log.Information("A search for the text {Text} has been launched", text);
            shellViewModel.LoggedActions.Add($"[INF] A search for the text \"{text}\" has been launched");
        }

        /// <inheritdoc/>
        public void LogStopTextSearch(Stopwatch timer, int count)
        {
            Log.Information("The text search took {Seconds} second(s) and return {Count} UI model(s)", timer.Elapsed.TotalSeconds, count);
            shellViewModel.LoggedActions.Add($"[INF] The text search took {timer.Elapsed.TotalSeconds} second(s) and return {count} UI model(s)");
        }

        /// <inheritdoc/>
        public void LogNavigateToRequirementsFailure(RequirementsFailure failure)
        {
            Log.Information("Failure to meet {Service:l} requirements due to {Name}", nameof(IRequirementsService), failure);
            shellViewModel.LoggedActions.Add($"[INF] Failure to meet {nameof(IRequirementsService)} requirements due to {failure}");
        }

        /// <inheritdoc/>
        public void LogOsPropertiesException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain the {Property:l} in the {Service:l}: {Message}", nameof(OsProperties), nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain the {nameof(OsProperties)} in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogUwpAppsManagementException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain UWP apps API in the {Service:l}: {Message}", nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain UWP apps API in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogProcessOwnerException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain process owner API in the {Service:l}: {Message}", nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain process owner API in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogAntivirusProductsException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain AntiVirusProduct API in the {Service:l}: {Message}", nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain AntiVirusProduct API in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogUnhandledException(Exception exception) => Log.Fatal(exception, "AN UNHANDLED EXCEPTION OCCURED: {Message}", exception.Message);

        /// <inheritdoc/>
        public void LogRegisterNotificationSenderException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain register as sender API in the {Service:l}: {Message}", nameof(IAppNotificationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain register as sender API in the {nameof(IAppNotificationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogOsUpdateException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain os update API in the {Service:l}: {Message}", nameof(IUpdateService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain os update API in the {nameof(IUpdateService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogWMIStateException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain WMI state requirements in the {Service:l}: {Message}", nameof(IRequirementsService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain WMI state requirements in the {nameof(IRequirementsService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogEventLogException(Exception exception)
        {
            Log.Error(exception, "The EventLog broken or removed: {Message}", exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] The EventLog broken or removed: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogAppUpdateException(Exception exception)
        {
            Log.Error(exception, "Failed to obtain app update requirements in the {Service:l}: {Message}", nameof(IRequirementsService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain app update requirements in the {nameof(IRequirementsService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogDefenderFileMissing(string file)
        {
            Log.Error("Microsoft Defender file missing: {File}", file);
            shellViewModel.LoggedActions.Add($"[ERR] Microsoft Defender file missing: {file}");
        }

        /// <inheritdoc/>
        public void LogDefenderServiceBroken(string service)
        {
            Log.Error("Microsoft Defender service broken or not running: {Service:l}", service);
            shellViewModel.LoggedActions.Add($"[ERR] Microsoft Defender service broken or not running: {service}");
        }

        /// <inheritdoc/>
        public void LogDefenderMpPreferenceIsNull()
        {
            Log.Error("Executing cmdlet “(Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess” return null");
            shellViewModel.LoggedActions.Add($"[ERR] Executing cmdlet “(Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess” return null");
        }

        /// <inheritdoc/>
        public void LogDefenderAntiSpywareEnabledException(Exception exception)
        {
            Log.Error("Failed to obtain AntiSpywareEnabled value in the {Service:l}: {Message}", nameof(IDefenderService), exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] Failed to obtain AntiSpywareEnabled value in the {nameof(IDefenderService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogModelGetStateException(string name, Exception exception)
        {
            Log.Error("An error occurred while get state in the UI model {Model:l}: {Message}", name, exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] An error occurred while get state in the UI model {name}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogModelSetStateException<T>(Exception exception, string name, T parameter)
            where T : struct
        {
            Log.Error("An error occurred while set state in the UI model {Model:l} with parameter {Parameter}: {Message}", name, parameter, exception.Message);
            shellViewModel.LoggedActions.Add($"[ERR] An error occurred while set state in the UI model {name} with parameter {parameter}: {exception.Message}");
        }
    }
}
