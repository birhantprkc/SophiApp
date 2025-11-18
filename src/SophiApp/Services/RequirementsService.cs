// <copyright file="RequirementsService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using System;
    using System.Security.Principal;
    using System.ServiceProcess;
    using CSharpFunctionalExtensions;
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;

    /// <inheritdoc/>
    public class RequirementsService : IRequirementsService
    {
        private readonly IAppNotificationService notificationService;
        private readonly IAppxPackagesService packagesService;
        private readonly ICommonDataService dataService;
        private readonly IDiskService diskService;
        private readonly IInstrumentationService instrumentationService;
        private readonly IProcessService processService;
        private readonly IHttpService httpService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsService"/> class.
        /// </summary>
        /// <param name="notificationService">A service for working with toast notifications API.</param>
        /// <param name="packagesService">A service for working with appx packages API.</param>
        /// <param name="dataService">A service for transferring app data between DI layers.</param>
        /// <param name="diskService">A service for working with disk API.</param>
        /// <param name="instrumentationService">A service for working with WMI API.</param>
        /// <param name="processService">A service for working with Windows process API.</param>
        /// <param name="httpService">A service for working with HTTP API.</param>
        public RequirementsService(
            IAppNotificationService notificationService,
            IAppxPackagesService packagesService,
            ICommonDataService dataService,
            IDiskService diskService,
            IInstrumentationService instrumentationService,
            IProcessService processService,
            IHttpService httpService)
        {
            this.notificationService = notificationService;
            this.packagesService = packagesService;
            this.dataService = dataService;
            this.diskService = diskService;
            this.instrumentationService = instrumentationService;
            this.processService = processService;
            this.httpService = httpService;
        }

        /// <inheritdoc/>
        public Result GetOsBitness()
        {
            App.Logger.LogOsBitness(Environment.Is64BitOperatingSystem);
            return Environment.Is64BitOperatingSystem ? Result.Success() : Result.Failure(nameof(RequirementsFailure.Is32BitOs));
        }

        /// <inheritdoc/>
        public Result GetWmiState()
        {
            try
            {
                var wmiService = new System.ServiceProcess.ServiceController("Winmgmt");
                using var verifyRepository = processService.WaitForExit(name: "cmd.exe", arguments: "/c winmgmt /verifyrepository");
                var serviceIsRun = wmiService.Status == ServiceControllerStatus.Running;
                var repoIsConsistent = verifyRepository.ExitCode.Equals(0);
                var osPropertiesIsCorrect = dataService.OsProperties.BuildNumber != -1;
                App.Logger.LogWMIState(wmiService.Status, verifyRepository.ExitCode, repoIsConsistent);
                return osPropertiesIsCorrect && serviceIsRun && repoIsConsistent ? Result.Success() : Result.Failure(nameof(RequirementsFailure.WMIBroken));
            }
            catch (Exception ex)
            {
                App.Logger.LogWMIStateException(ex);
                return Result.Failure(nameof(RequirementsFailure.WMIBroken));
            }
        }

        /// <inheritdoc/>
        public Result GetOsVersion()
        {
            return dataService.OsProperties.BuildNumber switch
            {
                var build when dataService.IsWindows11 && build < 22631 => Result.Failure(nameof(RequirementsFailure.Win11BuildLess22631)),
                var build when dataService.IsWindows11 && build.Equals(22631) && dataService.OsProperties.UpdateBuildRevision < 2283 => Result.Failure(nameof(RequirementsFailure.Win11UbrLess2283)),
                var build when !dataService.IsWindows11 && !build.Equals(19045) => Result.Failure(nameof(RequirementsFailure.Win10UnsupportedBuild)),
                var build when !dataService.IsWindows11 && !build.Equals(19045) && dataService.OsProperties.Edition.Contains("EnterpriseS", StringComparison.InvariantCultureIgnoreCase) => Result.Failure(nameof(RequirementsFailure.Win10EnterpriseSVersion)),
                var build when !dataService.IsWindows11 && build.Equals(19045) && dataService.OsProperties.UpdateBuildRevision < 3448 => Result.Failure(nameof(RequirementsFailure.Win10UpdateBuildRevisionLess3448)),
                _ => Result.Success()
            };
        }

        /// <inheritdoc/>
        public Result AppRunFromLoggedUser()
        {
            var currentUserName = WindowsIdentity.GetCurrent().Name.Split('\\')[1];
            var loggedUserProcess = Array.Find(array: System.Diagnostics.Process.GetProcesses(), match: p => p.ProcessName.Equals("explorer") && p.SessionId.Equals(System.Diagnostics.Process.GetCurrentProcess().SessionId));
            return instrumentationService.GetProcessOwnerOrDefault(loggedUserProcess).Equals(currentUserName) ? Result.Success() : Result.Failure(nameof(RequirementsFailure.RunByNotLoggedUser));
        }

        /// <inheritdoc/>
        public Result MalwareDetection()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var programData = Environment.ExpandEnvironmentVariables("%ProgramData%");
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var systemDrive = Environment.ExpandEnvironmentVariables("%SystemDrive%");
            var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var temp = Environment.ExpandEnvironmentVariables("%TEMP%");
            var malwares = new Dictionary<string, Func<bool>>()
            {
                { "OsRequirements_Malware_Windows10Debloater", () => Directory.Exists($"{systemDrive}\\Temp\\Windows10Debloater") },
                { "OsRequirements_Malware_Win10BloatRemover", () => Directory.Exists($"{temp}\\.net\\Win10BloatRemover") },
                {
                    "OsRequirements_Malware_BloatwareRemoval", () =>
                    {
                        try
                        {
                            return Directory.GetFileSystemEntries(path: $"{systemDrive}\\BRU", searchPattern: "Bloatware-Removal*.log").Length > 0;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                },
                { "OsRequirements_Malware_GhostToolbox", () => File.Exists($"{system32}\\migwiz\\dlmanifests\\run.ghost.cmd") },
                { "OsRequirements_Malware_Win10Tweaker", () => Registry.CurrentUser.OpenSubKey("Software\\Win 10 Tweaker") is not null },
                { "OsRequirements_Malware_BoosterX", () => File.Exists($"{programFiles}\\GameModeX\\GameModeX.exe") },
                { "OsRequirements_Malware_DefenderControl", () => Directory.Exists($"{appData}\\Defender Control") },
                { "OsRequirements_Malware_DefenderSwitch", () => Directory.Exists($"{programData}\\DSW") },
                { "OsRequirements_Malware_RevisionTool", () => Directory.Exists($"{programFilesX86}\\Revision Tool") },
                {
                    "OsRequirements_Malware_WinterOsTweaker", () =>
                    {
                        try
                        {
                            return Directory.GetFileSystemEntries(path: $"{systemRoot}", searchPattern: "WinterOS*").Length > 0;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                },
                { "OsRequirements_Malware_WinCry", () => File.Exists($"{systemRoot}\\TempCleaner.exe") },
                { "OsRequirements_Malware_WinClean", () => Directory.Exists($"{programFiles}\\WinClean Plus Apps") },
                { "OsRequirements_Malware_AtlasOS", () => Directory.Exists($"{systemRoot}\\AtlasModules") },
                { "OsRequirements_Malware_KirbyOS", () => Directory.Exists($"{programData}\\KirbyOS") },
                {
                    "OsRequirements_Malware_AutoSettingsPS", () =>
                    {
                        var exclusions = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows Defender\\Exclusions\\Paths")?.GetValueNames() ?? [];
                        return Array.Exists(exclusions!, key => key.Contains("AutoSettingsPS"));
                    }
                },
                {
                    "OsRequirements_Malware_FlibustierWindowsImage", () =>
                    {
                        var values = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\.NETFramework\\Performance")?.GetValueNames() ?? [];
                        return Array.Exists(values, key => key.Contains("flibustier"));
                    }
                },
                { "OsRequirements_Malware_Winpilot", () => Registry.CurrentUser.OpenSubKey("Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache")?.ValueExist("Winpilot") ?? false },
                { "OsRequirements_Malware_xd-AntiSpy", () => Registry.CurrentUser.OpenSubKey("Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache")?.ValueExist("xd-AntiSpy") ?? false },
                { "OsRequirements_Malware_ModernTweaker", () => Registry.ClassesRoot.OpenSubKey("CLSID\\{645FF040-5081-101B-9F08-00AA002F954E}\\shell\\Modern Cleaner") is not null },
                { "OsRequirements_Malware_Optimizer", () => Registry.CurrentUser.OpenSubKey("Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache")?.ValueExist("optimizer") ?? false },
                { "OsRequirements_Malware_PCNP", () => Registry.CurrentUser.OpenSubKey("Software\\PCNP") is not null },
                { "OsRequirements_Malware_Tron", () => Directory.Exists($"{systemDrive}\\logs\\tron") },
                { "OsRequirements_Malware_ChlorideOS", () => diskService.GetVolumeLabels().Any(label => label.Equals("ChlorideOS")) },
                { "OsRequirements_Malware_KernelOS", () => instrumentationService.GetPowerPlanNames().Any(name => name.Contains("KernelOS")) },
                { "OsRequirements_Malware_WinUtil", () => instrumentationService.GetPowerPlanNames().Any(name => name.Contains("ChrisTitus")) },
                { "OsRequirements_Malware_Defendnot", () => Directory.Exists($"{system32}\\Tasks\\defendnot") },
            };

            return malwares.Any(malware =>
            {
                if (malware.Value())
                {
                    dataService.DetectedMalware = malware.Key.GetLocalized();
                    App.Logger.LogMalwareDetected(dataService.DetectedMalware);
                    return true;
                }

                return false;
            }) ? Result.Failure(nameof(RequirementsFailure.MalwareDetected)) : Result.Success();
        }

        /// <inheritdoc/>
        public Result GetFeatureExperiencePackState()
        {
            return packagesService.PackageExist("MicrosoftWindows.Client.CBS") ? Result.Success() : Result.Failure(nameof(RequirementsFailure.FeatureExperiencePackRemoved));
        }

        /// <inheritdoc/>
        public Result GetEventLogState()
        {
            try
            {
                return new System.ServiceProcess.ServiceController("EventLog").Status == ServiceControllerStatus.Running ? Result.Success() : Result.Failure(nameof(RequirementsFailure.EventLogBroken));
            }
            catch (Exception e)
            {
                App.Logger.LogEventLogException(e);
                return Result.Failure(nameof(RequirementsFailure.EventLogBroken));
            }
        }

        /// <inheritdoc/>
        public Result GetMicrosoftStoreState()
        {
            return packagesService.PackageExist("Microsoft.WindowsStore") ? Result.Success() : Result.Failure(nameof(RequirementsFailure.MsStoreRemoved));
        }

        /// <inheritdoc/>
        public Result GetPendingRebootState()
        {
            var rebootParameters = new List<Func<bool>>()
            {
                () => Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootPending") is not null,
                () => Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootInProgress") is not null,
                () => Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\PackagesPending") is not null,
                () => Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\PostRebootReporting") is not null,
                () => Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RebootRequired") is not null,
            };

            return rebootParameters.Exists(parameter => parameter()) ? Result.Failure(nameof(RequirementsFailure.RebootRequired)) : Result.Success();
        }

        /// <inheritdoc/>
        public Result AppUpdateDetection()
        {
            try
            {
                var version = httpService.GetAppVersionOrDefault();

                if (version > dataService.AppVersion)
                {
                    App.Logger.LogAppUpdate(version);
                    var payload = string.Format("AppUpdateNotification".GetLocalized(), version.ToString(3), "https://github.com/Sophia-Community/SophiApp/releases");
                    notificationService.Show(payload);
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogAppUpdateException(ex);
            }

            return Result.Success();
        }
    }
}
