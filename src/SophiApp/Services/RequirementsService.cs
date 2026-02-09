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
        private readonly IInstrumentationService instrumentationService;
        private readonly IProcessService processService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsService"/> class.
        /// </summary>
        /// <param name="notificationService">A service for working with toast notifications API.</param>
        /// <param name="packagesService">A service for working with appx packages API.</param>
        /// <param name="dataService">A service for transferring app data between DI layers.</param>
        /// <param name="instrumentationService">A service for working with WMI API.</param>
        /// <param name="processService">A service for working with Windows process API.</param>
        public RequirementsService(
            IAppNotificationService notificationService,
            IAppxPackagesService packagesService,
            ICommonDataService dataService,
            IInstrumentationService instrumentationService,
            IProcessService processService)
        {
            this.notificationService = notificationService;
            this.packagesService = packagesService;
            this.dataService = dataService;
            this.instrumentationService = instrumentationService;
            this.processService = processService;
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
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var malwares = new Dictionary<string, Func<bool>>()
            {
                // https://www.youtube.com/GHOSTSPECTRE
                { "OsRequirements_Malware_GhostToolbox", () => File.Exists($"{system32}\\migwiz\\dlmanifests\\run.ghost.cmd") },
                // https://win10tweaker.ru
                { "OsRequirements_Malware_Win10Tweaker", () => Registry.CurrentUser.OpenSubKey("Software\\Win 10 Tweaker") is not null },
                // https://revi.cc
                { "OsRequirements_Malware_RevisionTool", () => Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Revision Tool")) },
                // https://github.com/Atlas-OS/Atlas
                { "OsRequirements_Malware_AtlasOS", () => Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "AtlasModules")) },
                // https://boosterx.ru
                { "OsRequirements_Malware_BoosterX", () => File.Exists($"{programFiles}\\GameModeX\\GameModeX.exe") },
                // https://www.youtube.com/watch?v=5NBqbUUB1Pk
                { "OsRequirements_Malware_WinClean", () => Directory.Exists($"{programFiles}\\WinClean Plus Apps") },
                // https://pc-np.com
                { "OsRequirements_Malware_PCNP", () => Registry.CurrentUser.OpenSubKey("Software\\PCNP") is not null },
                // https://www.reddit.com/r/TronScript
                { "OsRequirements_Malware_Tron", () => Directory.Exists(Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%"), "\\logs\\tron")) },
                // https://crystalcry.ru
                { "OsRequirements_Malware_CrystalCry", () => Registry.LocalMachine.OpenSubKey("Software\\CrystalCry") is not null },
                // https://github.com/es3n1n/defendnot
                { "OsRequirements_Malware_Defendnot", () => Directory.Exists($"{system32}\\Tasks\\defendnot") },
                // https://github.com/zoicware/RemoveWindowsAI
                { "OsRequirements_Malware_RemoveWindowsAI", () => Directory.GetDirectories(path: $"{system32}\\CatRoot", searchPattern: "ZoicwareRemoveWindowsAI*", searchOption: SearchOption.AllDirectories).Length > 0 },
                // https://forum.ru-board.com/topic.cgi?forum=62&topic=30617&start=1600#14
                {
                    "OsRequirements_Malware_AutoSettingsPS", () =>
                    {
                        var exclusions = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows Defender\\Exclusions\\Paths")?.GetValueNames() ?? [];
                        return Array.Exists(exclusions, key => key.Contains("AutoSettingsPS"));
                    }
                },
                // https://forum.ru-board.com/topic.cgi?forum=5&topic=50519
                {
                    "OsRequirements_Malware_ModernTweaker", () =>
                    {
                        var shellCache = Registry.CurrentUser.OpenSubKey("Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache")?.GetValueNames() ?? [];
                        return Array.Exists(shellCache, key => key.Contains("ModernTweaker"));
                    }
                },
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
            })
                ? Result.Failure(nameof(RequirementsFailure.MalwareDetected)) : Result.Success();
        }

        /// <inheritdoc/>
        public Result GetFeatureExperiencePackState() => packagesService.PackageExist("MicrosoftWindows.Client.CBS") ? Result.Success() : Result.Failure(nameof(RequirementsFailure.FeatureExperiencePackRemoved));

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
        public Result GetMicrosoftStoreState() => packagesService.PackageExist("Microsoft.WindowsStore") ? Result.Success() : Result.Failure(nameof(RequirementsFailure.MsStoreRemoved));

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
            var latestVersion = dataService.LatestAppRelease?.SophiApp_release ?? new Version(0, 0, 0);

            if (latestVersion > dataService.AppVersion)
            {
                App.Logger.LogAppUpdate(latestVersion);
                var payload = string.Format("AppUpdateNotification".GetLocalized(), latestVersion.ToString(3), "https://github.com/Sophia-Community/SophiApp/releases");
                notificationService.Show(payload);
            }

            return Result.Success();
        }
    }
}
