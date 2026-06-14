// <copyright file="RequirementsService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;
    using System;
    using System.Security.Principal;
    using System.ServiceProcess;
    using RegistryKey = Microsoft.Win32.RegistryKey;
    using ServiceController = System.ServiceProcess.ServiceController;

    /// <inheritdoc/>
    public class RequirementsService : IRequirementsService
    {
        private readonly ICommonDataService dataService;
        private readonly IInstrumentationService instrumentationService;
        private readonly IOsService osService;
        private readonly IAppxPackagesService packagesService;
        private readonly IPowerShellService powerShellService;
        private readonly IProcessService processService;
        private readonly ISettingsService settingsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsService"/> class.
        /// </summary>
        /// <param name="dataService">A service for transferring app data between DI layers.</param>
        /// <param name="instrumentationService">A service for working with WMI API.</param>
        /// <param name="osService">A service for working with Windows services API.</param>
        /// <param name="packagesService">A service for working with appx packages API.</param>
        /// <param name="powerShellService">A service for working with Windows PowerShell API.</param>
        /// <param name="processService">A service for working with Windows process API.</param>
        /// <param name="settingsService">A service for working with app settings.</param>
        public RequirementsService(
            ICommonDataService dataService,
            IInstrumentationService instrumentationService,
            IOsService osService,
            IAppxPackagesService packagesService,
            IPowerShellService powerShellService,
            IProcessService processService,
            ISettingsService settingsService)
        {
            this.dataService = dataService;
            this.instrumentationService = instrumentationService;
            this.osService = osService;
            this.packagesService = packagesService;
            this.powerShellService = powerShellService;
            this.processService = processService;
            this.settingsService = settingsService;
        }

        /// <inheritdoc/>
        public List<RequirementAction> Actions => [
            new (action: GetOsBitness, displayText: "OsRequirements_GetOsBitness".GetLocalized()), new (action: GetWMIState, displayText: "OsRequirements_GetWmiState".GetLocalized()),
            new (action: GetExternalServicesData, displayText: "OsRequirements_GetExternalServicesData".GetLocalized()), new (action: GetOsVersion, displayText: "OsRequirements_GetOsVersion".GetLocalized()),
            new (action: AppRunFromLoggedUser, displayText: "OsRequirements_AppRunFromLoggedUser".GetLocalized()), new (action: DetectMalware, displayText: "OsRequirements_MalwareDetection".GetLocalized()),
            new (action: GetFeatureExperiencePackState, displayText: "OsRequirements_GetFeatureExperiencePackState".GetLocalized()), new (action: GetEventLogState, displayText: "OsRequirements_GetEventLogState".GetLocalized()),
            new (action: GetMicrosoftStoreState, displayText: "OsRequirements_GetMicrosoftStoreState".GetLocalized()), new (action: GetPendingRebootState, displayText: "OsRequirements_GetPendingRebootState".GetLocalized()),
            new (action: GetAppUpdate, displayText: "OsRequirements_UpdateDetection".GetLocalized()), new (action: GetDefenderFilesExist, displayText: "OsRequirements_GetMsDefenderState".GetLocalized()),
            new (action: GetDefenderSettingsPageVisibility), new (action: GetDefenderServiceState), new (action: GetAntiSpywareEnabled), new (action: GetAntivirusProducts), new (action: GetSecurityHealthState),
            new (action: SetCommonDataServiceDefenderState), new (action: GetDefenderControlledFolderState), new (action: DetectHostFileEntries, displayText: "OsRequirements_DetectHostFileEntries".GetLocalized()),
            new (action: GetBitLockerEncryptOrDecryptState, displayText: "OsRequirements_GetBitLockerState".GetLocalized()), new (action: GetBitLockerProtectionState)];

        /// <inheritdoc/>
        public string? ActionForDebug { get; set; }

        private RequirementsResult GetOsBitness()
        {
            App.Logger.LogOsBitness(Environment.Is64BitOperatingSystem);

            if (ActionForDebug?.Equals(nameof(GetOsBitness)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.Is32BitOs;
            }

            return Environment.Is64BitOperatingSystem ? RequirementsResult.AllCorrect : RequirementsResult.Is32BitOs;
        }

        private RequirementsResult GetWMIState()
        {
            if (ActionForDebug?.Equals(nameof(GetWMIState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.WMIBroken;
            }

            try
            {
                var service = new ServiceController("Winmgmt");
                using var verifyRepository = processService.WaitForExit(name: "cmd.exe", arguments: "/c winmgmt /verifyrepository");
                var serviceRunning = service.Status == ServiceControllerStatus.Running;
                var repositoryConsistent = verifyRepository.ExitCode.Equals(0);
                var buildCorrect = dataService.OsProperties.Build != -1;
                App.Logger.LogWMIState(service.Status, verifyRepository.ExitCode, repositoryConsistent);
                return buildCorrect && serviceRunning && repositoryConsistent ? RequirementsResult.AllCorrect : RequirementsResult.WMIBroken;
            }
            catch (Exception ex)
            {
                App.Logger.LogWMIStateException(ex);
                return RequirementsResult.WMIBroken;
            }
        }

        private RequirementsResult GetOsVersion()
        {
            if (ActionForDebug?.Equals(nameof(GetOsVersion)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.WinUnsupportedBuild;
            }

            return dataService.OsProperties.Build switch
            {
                var build when dataService.OsProperties.IsLTSC && build < 26100 => RequirementsResult.WinUnsupportedBuild,
                var build when !dataService.OsProperties.IsLTSC && build < 26200 => RequirementsResult.WinUnsupportedBuild,
                var _ when dataService.OsProperties.IsLTSC && dataService.OsProperties.UBR < dataService.SupportedUBR.Win11LTSC => RequirementsResult.WinUnsupportedUBR,
                var _ when !dataService.OsProperties.IsLTSC && dataService.OsProperties.UBR < dataService.SupportedUBR.Win11 => RequirementsResult.WinUnsupportedUBR,
                _ => RequirementsResult.AllCorrect
            };
        }

        private RequirementsResult AppRunFromLoggedUser()
        {
            if (ActionForDebug?.Equals(nameof(AppRunFromLoggedUser)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.RunByNotLoggedUser;
            }

            var currentUserName = WindowsIdentity.GetCurrent().Name.Split('\\')[1];
            var loggedUserProcess = Array.Find(array: System.Diagnostics.Process.GetProcesses(), match: p => p.ProcessName.Equals("explorer") && p.SessionId.Equals(System.Diagnostics.Process.GetCurrentProcess().SessionId));
            return instrumentationService.GetProcessOwnerOrDefault(loggedUserProcess).Equals(currentUserName) ? RequirementsResult.AllCorrect : RequirementsResult.RunByNotLoggedUser;
        }

        private RequirementsResult GetExternalServicesData()
        {
            Task.Run(async () => await dataService.GetExternalServicesDataAsync()).Wait();
            return RequirementsResult.AllCorrect;
        }

        private RequirementsResult DetectMalware()
        {
            if (ActionForDebug?.Equals(nameof(DetectMalware)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                dataService.DetectedMalware = "OsRequirements_Malware_Win10Tweaker".GetLocalized();
                return RequirementsResult.MalwareDetected;
            }

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
                        var exclusions = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Microsoft Defender\\Exclusions\\Paths")?.GetValueNames() ?? [];
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
            return malwares.Any(m =>
            {
                if (m.Value())
                {
                    dataService.DetectedMalware = m.Key.GetLocalized();
                    App.Logger.LogMalwareDetected(dataService.DetectedMalware);
                    return true;
                }

                return false;
            }) ? RequirementsResult.MalwareDetected : RequirementsResult.AllCorrect;
        }

        private RequirementsResult GetFeatureExperiencePackState()
        {
            if (ActionForDebug?.Equals(nameof(GetFeatureExperiencePackState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.FeatureExperiencePackRemoved;
            }

            return packagesService.PackageExist("MicrosoftWindows.Client.CBS") ? RequirementsResult.AllCorrect : RequirementsResult.FeatureExperiencePackRemoved;
        }

        private RequirementsResult GetEventLogState()
        {
            if (ActionForDebug?.Equals(nameof(GetEventLogState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.EventLogBroken;
            }

            try
            {
                var service = new ServiceController("EventLog");
                return service.Status == ServiceControllerStatus.Running ? RequirementsResult.AllCorrect : RequirementsResult.EventLogBroken;
            }
            catch (Exception e)
            {
                App.Logger.LogEventLogException(e);
                return RequirementsResult.EventLogBroken;
            }
        }

        private RequirementsResult GetMicrosoftStoreState()
        {
            if (ActionForDebug?.Equals(nameof(GetMicrosoftStoreState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.MsStoreRemoved;
            }

            if (dataService.OsProperties.IsLTSC)
            {
                return packagesService.PackageExist("MicrosoftWindows.Client.CBS") ? RequirementsResult.AllCorrect : RequirementsResult.MsStoreRemoved;
            }

            return packagesService.PackageExist("Microsoft.WindowsStore") && packagesService.PackageExist("MicrosoftWindows.Client.CBS") ? RequirementsResult.AllCorrect : RequirementsResult.MsStoreRemoved;
        }

        private RequirementsResult GetPendingRebootState()
        {
            if (ActionForDebug?.Equals(nameof(GetPendingRebootState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.RebootRequired;
            }

            var parameters = new List<RegistryKey?>()
            {
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootPending"),
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootInProgress"),
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\PackagesPending"),
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\PostRebootReporting"),
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RebootRequired"),
            };

            return parameters.Exists(k => k is not null) ? RequirementsResult.RebootRequired : RequirementsResult.AllCorrect;
        }

        private RequirementsResult GetAppUpdate()
        {
            // TODO: Refactoring app update.
            // var latestVersion = dataService.LatestAppRelease?.SophiApp_release ?? new Version(0, 0, 0);
            // if (latestVersion > dataService.AppVersion)
            // {
            //    App.Logger.LogAppUpdate(latestVersion);
            //    var payload = string.Format("AppUpdateNotification".GetLocalized(), latestVersion.ToString(3), "https://github.com/Sophia-Community/SophiApp/releases");
            //    notificationService.Show(payload);
            // }
            return RequirementsResult.AllCorrect;
        }

        private RequirementsResult GetDefenderFilesExist()
        {
            if (ActionForDebug?.Equals(nameof(GetDefenderFilesExist)) ?? false)
            {
                dataService.DefenderFileMissing = "SecurityHealthSystray.exe";
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.DefenderFileMissing;
            }

            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var files = new List<string>()
            {
                Path.Combine(systemFolder, "smartscreen.exe"),
                Path.Combine(systemFolder, "SecurityHealthSystray.exe"),
                Path.Combine(systemFolder, "CompatTelRunner.exe"),
            };
            return files.TrueForAll(f =>
            {
                if (File.Exists(f))
                {
                    return true;
                }

                App.Logger.LogDefenderFileMissing(f);
                dataService.DefenderFileMissing = f;
                return false;
            }) ? RequirementsResult.AllCorrect : RequirementsResult.DefenderFileMissing;
        }

        private RequirementsResult GetDefenderSettingsPageVisibility()
        {
            if (ActionForDebug?.Equals(nameof(GetDefenderSettingsPageVisibility)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.DefenderSettingsPageHidden;
            }

            var visibility = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer")?.GetValue("SettingsPageVisibility") as string ?? string.Empty;
            return visibility.Contains("hide:windowsdefender") ? RequirementsResult.DefenderSettingsPageHidden : RequirementsResult.AllCorrect;
        }

        private RequirementsResult GetDefenderServiceState()
        {
            if (ActionForDebug?.Equals(nameof(GetDefenderServiceState)) ?? false)
            {
                dataService.DefenderServiceBroken = "SecurityHealthService";
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.DefenderServiceFailure;
            }

            var logEntry = string.Empty;
            var services = new List<string>() { "Windefend", "SecurityHealthService", "wscsvc", "wdFilter" };
            var servicesState = services.TrueForAll(s =>
            {
                if (osService.Exist(s))
                {
                    logEntry = logEntry.Insert(logEntry.Length, $"{s}:True, ");
                    return true;
                }

                App.Logger.LogDefenderServiceNotFound(s);
                dataService.DefenderServiceBroken = s;
                return false;
            });

            if (servicesState)
            {
                App.Logger.LogDefenderServiceState(logEntry);
                return RequirementsResult.AllCorrect;
            }

            return RequirementsResult.DefenderServiceFailure;
        }

        private RequirementsResult GetAntiSpywareEnabled()
        {
            if (ActionForDebug?.Equals(nameof(GetAntiSpywareEnabled)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.AntiSpywareDisabled;
            }

            try
            {
                _ = instrumentationService.GetAntiSpywareEnabled();
                return RequirementsResult.AllCorrect;
            }
            catch (Exception e)
            {
                App.Logger.LogDefenderAntiSpywareEnabledException(e);
                return RequirementsResult.AntiSpywareDisabled;
            }
        }

        private RequirementsResult GetAntivirusProducts()
        {
            if (ActionForDebug?.Equals(nameof(GetAntivirusProducts)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.AntiSpywareDisabled;
            }

            var antivirusProducts = instrumentationService.GetAntivirusProductsOrDefault();

            if (antivirusProducts.Count > 0)
            {
                return RequirementsResult.AllCorrect;
            }

            App.Logger.LogDefenderAntivirusProductsIsNull();
            return RequirementsResult.AntiSpywareDisabled;
        }

        private RequirementsResult GetSecurityHealthState()
        {
            if (ActionForDebug?.Equals(nameof(GetSecurityHealthState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.DefenderSecurityHealthFailure;
            }

            try
            {
                osService.TryStart("SecurityHealthService");
                var healthState = osService.GetStatus("SecurityHealthService");
                App.Logger.LogDefenderSecurityHealthStatus(healthState);
                return healthState == ServiceControllerStatus.Running ? RequirementsResult.AllCorrect : RequirementsResult.DefenderSecurityHealthFailure;
            }
            catch (Exception e)
            {
                App.Logger.LogDefenderSecurityHealthException(e);
                return RequirementsResult.DefenderSecurityHealthFailure;
            }
        }

        private RequirementsResult SetCommonDataServiceDefenderState()
        {
            var productState = instrumentationService.GetAntivirusProductsOrDefault()
                .Find(product => product.GetPropertyValue("instanceGuid")
                .Equals("{D68DDC3A-831F-4fae-9E44-DA132C1ACF46}"))
                ?.GetPropertyValue("productState");
            var defenderState = productState is null ? "00" : string.Format("0x{0:x}", productState).Substring(3, 2);
            var defenderDefaultAV = !(defenderState.Equals("00") || defenderState.Equals("01"));
            App.Logger.LogDefenderIsDefault(defenderDefaultAV);
            dataService.DefenderEnabled = defenderDefaultAV;
            return RequirementsResult.AllCorrect;
        }

        private RequirementsResult GetDefenderControlledFolderState()
        {
            if (ActionForDebug?.Equals(nameof(GetDefenderControlledFolderState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.DefenderControlledFolderEnable;
            }

            try
            {
                var folderState = powerShellService.Invoke<byte>("(Get-MpPreference).EnableControlledFolderAccess");
                App.Logger.LogDefenderControlledFolderState(folderState);
                return folderState.Equals(1) ? RequirementsResult.DefenderControlledFolderEnable : RequirementsResult.AllCorrect;
            }
            catch (Exception e)
            {
                App.Logger.LogDefenderControlledFolderException(e);
                return RequirementsResult.DefenderControlledFolderEnable;
            }
        }

        private RequirementsResult DetectHostFileEntries()
        {
            if (ActionForDebug?.Equals(nameof(DetectHostFileEntries)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.DetectHostFileEntries;
            }

            var host = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers\\etc\\hosts");
            var entries = File.ReadAllLines(host);
            return entries.Any(e => !(string.IsNullOrEmpty(e) || e.StartsWith('#'))) ? RequirementsResult.DetectHostFileEntries : RequirementsResult.AllCorrect;
        }

        private RequirementsResult GetBitLockerEncryptOrDecryptState()
        {
            if (ActionForDebug?.Equals(nameof(GetBitLockerEncryptOrDecryptState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.BitLockerEncryptOrDecryptState;
            }

            var systemDriveState = powerShellService.Invoke("Get-BitLockerVolume -MountPoint $env:SystemDrive | Where-Object -FilterScript {$_.VolumeStatus -notin @(\"FullyEncrypted\", \"FullyDecrypted\")}");
            return systemDriveState.Count == 0 ? RequirementsResult.AllCorrect : RequirementsResult.BitLockerEncryptOrDecryptState;
        }

        private RequirementsResult GetBitLockerProtectionState()
        {
            if (ActionForDebug?.Equals(nameof(GetBitLockerProtectionState)) ?? false)
            {
                settingsService.SaveDebugRequirementActionAsync(string.Empty);
                return RequirementsResult.BitLockerProtectionStatus;
            }

            var protectionState = powerShellService.Invoke("Get-BitLockerVolume -MountPoint $env:SystemDrive | Where-Object -FilterScript {($_.ProtectionStatus -eq \"Off\") -and ($_.VolumeStatus -eq \"FullyEncrypted\")}");
            return protectionState.Count == 0 ? RequirementsResult.AllCorrect : RequirementsResult.BitLockerProtectionStatus;
        }
    }
}
