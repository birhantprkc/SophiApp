// <copyright file="DefenderService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using CSharpFunctionalExtensions;
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Helpers;

    /// <inheritdoc/>
    public class DefenderService : IDefenderService
    {
        private readonly ICommonDataService commonDataService;
        private readonly IInstrumentationService instrumentationService;
        private readonly IOsService osService;
        private readonly IPowerShellService powerShellService;
        private readonly IProcessService processService;

        private readonly List<string> servicesName = ["Windefend", "SecurityHealthService", "wscsvc"];
        private bool servicesStatus = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderService"/> class.
        /// </summary>
        /// <param name="commonDataService">A service for transferring app data between DI layers.</param>
        /// <param name="instrumentationService">A service for working with WMI API.</param>
        /// <param name="osService">A service for working with Windows services API.</param>
        /// <param name="powerShellService">A service for working with Windows PowerShell API.</param>
        /// <param name="processService">A service for working with Windows <see cref="Process"/> API.</param>
        public DefenderService(
            ICommonDataService commonDataService,
            IInstrumentationService instrumentationService,
            IOsService osService,
            IPowerShellService powerShellService,
            IProcessService processService)
        {
            this.commonDataService = commonDataService;
            this.instrumentationService = instrumentationService;
            this.osService = osService;
            this.powerShellService = powerShellService;
            this.processService = processService;
        }

        /// <inheritdoc/>
        public Result GetState() => FilesExist()
            .Bind(GetSettingsPageVisibility)
            .Bind(GetAntivirusProducts)
            .BindIf(IsDefaultAntivirus, GetAntiSpywareEnabled)
            .Bind(GetServiceState)
            .Tap(GetControlledFolderAccess)
            .Tap(GetProductState)
            .TapIf(!commonDataService.DefenderMpPreferenceBroken, DisableControlledFolder);

        /// <inheritdoc/>
        public void EnableControlledFolder()
        {
            if (commonDataService.DefenderEnabled && !commonDataService.DefenderMpPreferenceBroken && commonDataService.DefenderControlledFolderAccess)
            {
                _ = powerShellService.Invoke("Set-MpPreference -EnableControlledFolderAccess Enabled");
            }
        }

        private Result FilesExist()
        {
            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var defenderFiles = new List<string>()
            {
                Path.Combine(systemFolder, "smartscreen.exe"),
                Path.Combine(systemFolder, "SecurityHealthSystray.exe"),
                Path.Combine(systemFolder, "CompatTelRunner.exe"),
            };

            return defenderFiles.TrueForAll(file =>
            {
                if (File.Exists(file))
                {
                    return true;
                }

                App.Logger.LogDefenderFileMissing(file);
                commonDataService.DefenderFileMissing = file;
                return false;
            }) ? Result.Success() : Result.Failure(nameof(RequirementsFailure.DefenderFileMissing));
        }

        private Result GetSettingsPageVisibility()
        {
            var settingsPagePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var settingsPageVisibility = Registry.LocalMachine.OpenSubKey(settingsPagePath)
                    ?.GetValue("SettingsPageVisibility") as string ?? string.Empty;
            return settingsPageVisibility.Contains("hide:windowsdefender")
                ? Result.Failure(nameof(RequirementsFailure.DefenderSettingsPageHidden))
                : Result.Success();
        }

        private Result GetAntivirusProducts()
        {
            var antivirusProducts = instrumentationService.GetAntivirusProductsOrDefault();
            return antivirusProducts.Count > 0 ? Result.Success() : Result.Failure(nameof(RequirementsFailure.DefenderIsBroken));
        }

        private bool IsDefaultAntivirus()
        {
            var productState = instrumentationService.GetAntivirusProductsOrDefault()
                .Find(product => product.GetPropertyValue("instanceGuid")
                .Equals("{D68DDC3A-831F-4fae-9E44-DA132C1ACF46}"))
                ?.GetPropertyValue("productState");
            var defenderState = productState is null ? "00" : string.Format("0x{0:x}", productState).Substring(3, 2);
            var isDefaultAntivirus = !(defenderState.Equals("00") || defenderState.Equals("01"));
            return isDefaultAntivirus;
        }

        private Result GetAntiSpywareEnabled()
        {
            try
            {
                _ = instrumentationService.GetAntiSpywareEnabled();
                return Result.Success();
            }
            catch (Exception ex)
            {
                App.Logger.LogDefenderAntiSpywareEnabledException(ex);
                return Result.Failure(nameof(RequirementsFailure.DefenderIsBroken));
            }
        }

        private Result GetServiceState()
        {
            servicesStatus = servicesName.TrueForAll(service =>
            {
                if (osService.Exist(service))
                {
                    App.Logger.LogDefenderServiceStatus(service, true);
                    return true;
                }

                App.Logger.LogDefenderServiceBroken(service);
                commonDataService.DefenderServiceBroken = service;
                return false;
            });

            return servicesStatus ? Result.Success() : Result.Failure(nameof(RequirementsFailure.DefenderServiceBroken));
        }

        private void GetControlledFolderAccess()
        {
            var folderAccessIsEnabled = powerShellService.Invoke("(Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess")[0];

            if (folderAccessIsEnabled is null)
            {
                commonDataService.DefenderMpPreferenceBroken = true;
                App.Logger.LogDefenderMpPreferenceIsNull();
            }
        }

        private void GetProductState()
        {
            var productStatus = IsDefaultAntivirus();
            var antiSpyware = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows Defender")?.GetValue("DisableAntiSpyware") as int? ?? -1;
            var realtimeMonitoring = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection")?.GetValue("DisableRealtimeMonitoring") as int? ?? -1;
            var behaviorMonitoring = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection")?.GetValue("DisableBehaviorMonitoring") as int? ?? -1;

            var antiSpywareEnabled = !antiSpyware.Equals(1);
            var realtimeMonitoringEnabled = !realtimeMonitoring.Equals(1);
            var behaviorMonitoringEnabled = !behaviorMonitoring.Equals(1);

            commonDataService.DefenderEnabled = servicesStatus && productStatus && antiSpywareEnabled && realtimeMonitoringEnabled && behaviorMonitoringEnabled;
        }

        private void DisableControlledFolder()
        {
            commonDataService.DefenderControlledFolderAccess = powerShellService.Invoke("(Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess")[0].BaseObject.Equals(1);

            if (commonDataService.DefenderControlledFolderAccess)
            {
                _ = powerShellService.Invoke("Set-MpPreference -EnableControlledFolderAccess Disabled");
                _ = processService.StartProcessByName("explorer.exe", "windowsdefender://RansomwareProtection");
            }
        }
    }
}
