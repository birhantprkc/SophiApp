// <copyright file="LoggerService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.UI.Xaml;
    using Serilog;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;
    using SophiApp.ViewModels;
    using System.Diagnostics;
    using System.Globalization;
    using System.ServiceProcess;

    /// <inheritdoc/>
    public class LoggerService : ILoggerService
    {
        private readonly ShellViewModel shellViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerService"/> class.
        /// </summary>
        public LoggerService()
        {
            LogFolder = Path.Combine(AppContext.BaseDirectory, "Log");
            LogFile = Path.Combine(LogFolder, $"SophiApp-{Environment.MachineName.ToUpper()}.log");
            LogFile.TryDelete();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    LogFile,
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: "{Message}{NewLine}")
                .CreateLogger();

            shellViewModel = App.GetService<ShellViewModel>();
        }

        /// <inheritdoc/>
        public string LogFolder { get; init; }

        /// <inheritdoc/>
        public string LogFile { get; init; }

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
                $"Windows version: {properties.Caption}", $"Windows edition: {properties.Edition}", $"Windows build: {properties.BuildNumber}.{properties.UpdateBuildRevision}",
                $"Computer name: {properties.CSName}", $"User name: {Environment.UserName}", $"User culture: {CultureInfo.CurrentCulture.EnglishName}",
                $"User region: {RegionInfo.CurrentRegion.EnglishName}", $"User date and time: {dateTime}", $"User time zone: {TimeZoneInfo.Local.DisplayName}",
            ];
        }

        /// <inheritdoc/>
        public void LogAppProperties(Version version, string directory)
        {
            Log.Information("App version: {Version:l}", version);
            Log.Information("App directory: {Directory}", directory);
            shellViewModel.LoggedActions.AddRange([$"App version: {version}", $"App directory: {directory}"]);
        }

        /// <inheritdoc/>
        public void LogNavigateToPage(string name)
        {
            Log.Information("Navigate to: {Name:l}", name);
            shellViewModel.LoggedActions.Add($"Navigate to: {name}");
        }

        /// <inheritdoc/>
        public void LogChangeTheme(ElementTheme theme)
        {
            Log.Information("Change theme to: {Theme}", theme);
            shellViewModel.LoggedActions.Add($"Change theme to: {theme}");
        }

        /// <inheritdoc/>
        public void LogOpenedUrl(string url)
        {
            Log.Information("Opened url: {Url}", url);
            shellViewModel.LoggedActions.Add($"Opened url: {url}");
        }

        /// <inheritdoc/>
        public void LogOneDriveSetupFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Information("OneDrive setup file not found in PC");
                shellViewModel.LoggedActions.Add("OneDrive setup file not found in PC");
                return;
            }

            Log.Information("OneDrive setup file found: {Path}", path);
            shellViewModel.LoggedActions.Add($"OneDrive setup file found: \"{path}\"");
        }

        /// <inheritdoc/>
        public void LogOneDriveUserFilesExist(string path, int count)
        {
            Log.Information("After uninstalling OneDrive, {Count:l} file(s) left in the {Path} folder", count, path);
            shellViewModel.LoggedActions.Add($"After uninstalling OneDrive, {count} file(s) left in the \"{path}\" folder");
        }

        /// <inheritdoc/>
        public void LogOsBitness(bool is64BitOs)
        {
            Log.Information("Is x64: {Is64BitOs}", is64BitOs);
            shellViewModel.LoggedActions.Add($"Is x64: {is64BitOs}");
        }

        /// <inheritdoc/>
        public void LogWMIState(ServiceControllerStatus serviceState, int repositoryExitCode, bool repositoryIsConsistent)
        {
            Log.Information("WMI service state: {ServiceState}", serviceState);
            Log.Information("WMI verify repository exit code: {ExitCode}", repositoryExitCode);
            Log.Information("WMI repository is consistent: {RepositoryIsConsistent}", repositoryIsConsistent);
            shellViewModel.LoggedActions.AddRange([$"WMI service state: {serviceState}", $"WMI verify repository exit code: {repositoryExitCode}", $"WMI repository is consistent: {repositoryIsConsistent}"]);
        }

        /// <inheritdoc/>
        public void LogMalwareDetected(string name)
        {
            Log.Warning("Malware detected: {Malware:l} in the {Service:l}", name, nameof(IRequirementsService));
            shellViewModel.LoggedActions.Add($"Malware detected: {name} in the {nameof(IRequirementsService)}");
        }

        /// <inheritdoc/>
        public void LogAppUpdate(Version version)
        {
            Log.Information("App version available in the repository: {Version:l}", version);
            shellViewModel.LoggedActions.Add($"App version available in the repository: {version}");
        }

        /// <inheritdoc/>
        public void LogAllModelsBuilt(int count)
        {
            Log.Information("{Service:l} built {Count} models", nameof(IModelService), count);
            shellViewModel.LoggedActions.Add($"{nameof(IModelService)} built {count} models");
        }

        /// <inheritdoc/>
        public void LogStartModelsGetState()
        {
            Log.Information("{Service:l} has started initialization of models", nameof(IModelService));
            shellViewModel.LoggedActions.Add($"{nameof(IModelService)} has started initialization of models");
        }

        /// <inheritdoc/>
        public void LogStartApplicableModelsSetState()
        {
            Log.Information("{Service:l} has started set customizations in the applicable collection", nameof(IModelService));
            shellViewModel.LoggedActions.Add($"{nameof(IModelService)} has started set customizations state in the applicable collection");
        }

        /// <inheritdoc/>
        public void LogAllModelsGetState(Stopwatch timer, int count)
        {
            Log.Information("{Service:l} took time to get {Count} models state: {TimeSpent}", nameof(IModelService), count, timer.Elapsed);
            shellViewModel.LoggedActions.Add($"{nameof(IModelService)} took time to get {count} models state: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogModelGetState(string name, Stopwatch timer)
        {
            Log.Information("{Name:l} took time to get state: {TimeSpent}", name, timer.Elapsed);
            shellViewModel.LoggedActions.Add($"{name} took time to get state: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogModelSetState(string name, Stopwatch timer)
        {
            Log.Information("{Name:l} took time to set state: {TimeSpent}", name, timer.Elapsed);
            shellViewModel.LoggedActions.Add($"{name} took time to set state: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogModelState<T>(string name, T state)
            where T : struct
        {
            Log.Information("{Name:l} has state: {State}", name, state);
            shellViewModel.LoggedActions.Add($"{name} has state: {state}");
        }

        /// <inheritdoc/>
        public void LogApplicableModelsCanceled()
        {
            Log.Information("The applying of customizations has been canceled by user");
            shellViewModel.LoggedActions.Add($"The applying of customizations has been canceled by user");
        }

        /// <inheritdoc/>
        public void LogApplicableModelsClear()
        {
            Log.Information("Applicable collection has been cleaned up");
            shellViewModel.LoggedActions.Add($"Applicable collection has been cleaned up");
        }

        /// <inheritdoc/>
        public void LogApplicableModelChanged<T>(string name, T value)
            where T : struct
        {
            Log.Information("The parameter value of customization {Name:l} has been changed to {Value} in applicable collection", name, value);
            shellViewModel.LoggedActions.Add($"The parameter value of the customization {name} has been changed to {value} in applicable collection");
        }

        /// <inheritdoc/>
        public void LogApplicableModelRemoved(string name)
        {
            Log.Information("{Name:l} has been removed from applicable collection", name);
            shellViewModel.LoggedActions.Add($"{name} has been removed from applicable collection");
        }

        /// <inheritdoc/>
        public void LogApplicableModelAdded(string name)
        {
            Log.Information("{Name:l} has been added to applicable customization collection", name);
            shellViewModel.LoggedActions.Add($"{name} has been added to applicable customization collection");
        }

        /// <inheritdoc/>
        public void LogApplicableModelAdded<T>(string name, T parameter)
            where T : struct
        {
            Log.Information("{Name:l} with parameter {Parameter} has been added to applicable customization collection", name, parameter);
            shellViewModel.LoggedActions.Add($"{name} with parameter {parameter} has been added to applicable customization collection");
        }

        /// <inheritdoc/>
        public void LogDefenderServiceState(string service, bool exists)
        {
            Log.Information("Service {Service:l} exists: {Exists:l}", service, exists);
            shellViewModel.LoggedActions.Add($"Service {service} exists: {exists}");
        }

        /// <inheritdoc/>
        public void LogUwpForAllUsersState(bool state)
        {
            Log.Information("The UWP For All Users checkbox state has been changed to: {State}", state);
            shellViewModel.LoggedActions.Add($"The UWP For All Users checkbox state has been changed to: {state}");
        }

        /// <inheritdoc/>
        public void LogDescriptionTextSizeChanged(int size)
        {
            Log.Information("The text size of UI element descriptions set to: {Size}", size);
            shellViewModel.LoggedActions.Add($"The text size of UI element descriptions set to: {size}");
        }

        /// <inheritdoc/>
        public void LogPageVisibility(bool isVisible)
        {
            Log.Information("The log page visibility set to: {IsVisible}", isVisible);
            shellViewModel.LoggedActions.Add($"The log page visibility set to: {isVisible}");
        }

        /// <inheritdoc/>
        public void LogTitleTextSizeChanged(int size)
        {
            Log.Information("The text size of UI element headers set to: {Size}", size);
            shellViewModel.LoggedActions.Add($"The text size of UI element headers set to: {size}");
        }

        /// <inheritdoc/>
        public void LogStopTextSearch(string text, Stopwatch timer, int count)
        {
            Log.Information("A search for the text {Text} took {Seconds} seconds and return {Count} customization(s)", text, timer.Elapsed.TotalSeconds, count);
            shellViewModel.LoggedActions.Add($"A search for the text \"{text}\" took {timer.Elapsed.TotalSeconds} seconds and return {count} customization(s)");
        }

        /// <inheritdoc/>
        public void LogViewModelExecute(Stopwatch timer)
        {
            Log.Information("{ViewModel:l} took time to execute: {TimeSpent}", nameof(ShellViewModel), timer.Elapsed);
            shellViewModel.LoggedActions.Add($"{nameof(ShellViewModel)} took time to execute: {timer.Elapsed}");
        }

        /// <inheritdoc/>
        public void LogUrlIsAvailable(string url, bool state)
        {
            Log.Information("The url {Url} is available: {State}", url, state);
            shellViewModel.LoggedActions.Add($"The url \"{url}\" is available: {state}");
        }

        /// <inheritdoc/>
        public void LogUwpModelsBuilt(Stopwatch timer, int count, bool forAllUsers)
        {
            if (forAllUsers)
            {
                Log.Information("{Service:l} took {TimeSpent} to built {Count} UWP models for all users", nameof(IModelService), timer.Elapsed, count);
            }
            else
            {
                Log.Information("{Service:l} took {TimeSpent} to built {Count} UWP models for current user", nameof(IModelService), timer.Elapsed, count);
            }

            shellViewModel.LoggedActions.Add($"{nameof(IModelService)} took {timer.Elapsed} to built {count} UWP models for {(forAllUsers ? "all users" : "current user")}");
        }

        /// <inheritdoc/>
        public void LogNavigateToRequirementsFailure(RequirementsFailure failure)
        {
            Log.Information("Failure to meet {Service:l} requirements due to {Name}", nameof(IRequirementsService), failure);
            shellViewModel.LoggedActions.Add($"Failure to meet {nameof(IRequirementsService)} requirements due to {failure}");
        }

        /// <inheritdoc/>
        public void LogOsPropertiesException(Exception exception)
        {
            Log.Error(exception, "[WRN] Failed to obtain the {Property:l} in the {Service:l}: {Message}", nameof(OsProperties), nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain the {nameof(OsProperties)} in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogUwpAppsManagementException(Exception exception)
        {
            Log.Error(exception, "[WRN] Failed to obtain UWP apps API in the {Service:l}: {Message}", nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain UWP apps API in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogProcessOwnerException(Exception exception)
        {
            Log.Error(exception, "[WRN] Failed to obtain process owner API in the {Service:l}: {Message}", nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain process owner API in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogAntivirusProductsException(Exception exception)
        {
            Log.Error(exception, "[WRN] Failed to obtain AntiVirusProduct API in the {Service:l}: {Message}", nameof(IInstrumentationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain AntiVirusProduct API in the {nameof(IInstrumentationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogUnhandledException(Exception exception) => Log.Fatal(exception, "[ERR] AN UNHANDLED EXCEPTION OCCURED: {Message}", exception.Message);

        /// <inheritdoc/>
        public void LogRegisterNotificationSenderException(Exception exception)
        {
            Log.Error(exception, "[WRN] Failed to obtain register as sender API in the {Service:l}: {Message}", nameof(IAppNotificationService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain register as sender API in the {nameof(IAppNotificationService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogOsUpdateException(Exception exception)
        {
            Log.Error(exception, "[WRN] Failed to obtain os update API in the {Service:l}: {Message}", nameof(IUpdateService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain os update API in the {nameof(IUpdateService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogWMIStateException(Exception exception)
        {
            Log.Error(exception, "[WRN] Failed to obtain WMI state requirements in the {Service:l}: {Message}", nameof(IRequirementsService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain WMI state requirements in the {nameof(IRequirementsService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogEventLogException(Exception exception)
        {
            Log.Error(exception, "[WRN] The EventLog broken or removed: {Message}", exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] The EventLog broken or removed: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogDefenderFileMissing(string file)
        {
            Log.Error("[WRN] Microsoft Defender file missing: {File}", file);
            shellViewModel.LoggedActions.Add($"[WRN] Microsoft Defender file missing: {file}");
        }

        /// <inheritdoc/>
        public void LogDefenderMpPreferenceIsNull()
        {
            Log.Error("[WRN] Executing cmdlet (Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess return null");
            shellViewModel.LoggedActions.Add($"[WRN] Executing cmdlet (Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess return null");
        }

        /// <inheritdoc/>
        public void LogDefenderAntivirusProductsIsNull()
        {
            Log.Error("[WRN] Class AntiVirusProduct from root/SecurityCenter2 namespace return null");
            shellViewModel.LoggedActions.Add($"[WRN] Class AntiVirusProduct from root/SecurityCenter2 namespace return null");
        }

        /// <inheritdoc/>
        public void LogDefenderServiceBroken(string service)
        {
            Log.Error("[WRN] Microsoft Defender service broken: {Service:l}", service);
            shellViewModel.LoggedActions.Add($"[WRN] Microsoft Defender service broken: {service}");
        }

        /// <inheritdoc/>
        public void LogDefenderControlledFolderState(byte state)
        {
            Log.Information("Microsoft Defender controlled folder access state: {State}", state);
            shellViewModel.LoggedActions.Add($"Microsoft Defender controlled folder access state: {state}");
        }

        /// <inheritdoc/>
        public void LogDefenderIsDefault(bool isDefault)
        {
            Log.Information("Microsoft Defender is default AV: {IsDefault}", isDefault);
            shellViewModel.LoggedActions.Add($"Microsoft Defender is default AV: {isDefault}");
        }

        /// <inheritdoc/>
        public void LogDefenderSecurityHealthStatus(ServiceControllerStatus status)
        {
            Log.Information("Microsoft Defender Security Health service status: {Status}", status);
            shellViewModel.LoggedActions.Add($"Microsoft Defender Security Health service status: {status}");
        }

        /// <inheritdoc/>
        public void LogDefenderSecurityHealthException(Exception exception)
        {
            Log.Error("[WRN] Failed to obtain Microsoft Defender Security Health service status in the {Service:l}: {Message}", nameof(IDefenderService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain Microsoft Defender Security Health service status in the {nameof(IDefenderService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogDefenderAntiSpywareEnabledException(Exception exception)
        {
            Log.Error("[WRN] Failed to obtain Microsoft Defender AntiSpywareEnabled value in the {Service:l}: {Message}", nameof(IDefenderService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain Microsoft Defender AntiSpywareEnabled value in the {nameof(IDefenderService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogDefenderControlledFolderException(Exception exception)
        {
            Log.Error("[WRN] Failed to obtain Microsoft Defender EnableControlledFolderAccess value in the {Service:l}: {Message}", nameof(IDefenderService), exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] Failed to obtain Microsoft Defender EnableControlledFolderAccess value in the {nameof(IDefenderService)}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogModelGetStateException(string name, Exception exception)
        {
            Log.Error("[WRN] An error occurred while getting state in {Model:l}: {Message}", name, exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] An error occurred while getting state in {name}: {exception.Message}");
        }

        /// <inheritdoc/>
        public void LogModelSetStateException<T>(Exception exception, string name, T parameter)
            where T : struct
        {
            Log.Error("[WRN] An error occurred while set state in {Model:l} with parameter {Parameter}: {Message}", name, parameter, exception.Message);
            shellViewModel.LoggedActions.Add($"[WRN] An error occurred while set state in {name} with parameter {parameter}: {exception.Message}");
        }
    }
}
