// <copyright file="DefenderService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using CSharpFunctionalExtensions;
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Helpers;
    using System.ServiceProcess;

    /// <inheritdoc/>
    public class DefenderService : IDefenderService
    {
        private readonly ICommonDataService dataService;
        private readonly IInstrumentationService instrumentationService;
        private readonly IOsService osService;
        private readonly IPowerShellService powerShellService;
        private readonly List<string> defenderServices = ["Windefend", "SecurityHealthService", "wscsvc", "wdFilter"];

        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderService"/> class.
        /// </summary>
        /// <param name="dataService">A service for transferring app data between DI layers.</param>
        /// <param name="instrumentationService">A service for working with WMI API.</param>
        /// <param name="osService">A service for working with Windows services API.</param>
        /// <param name="powerShellService">A service for working with Windows PowerShell API.</param>
        public DefenderService(
            ICommonDataService dataService,
            IInstrumentationService instrumentationService,
            IOsService osService,
            IPowerShellService powerShellService)
        {
            this.dataService = dataService;
            this.instrumentationService = instrumentationService;
            this.osService = osService;
            this.powerShellService = powerShellService;
        }

        /// <inheritdoc/>
        public Result GetState() => FilesExist()
            .Bind(GetSettingsPageVisibility)
            .Bind(GetServiceState)
            .Bind(GetMpComputerStatus)
            .Bind(GetAntivirusProducts)
            .Bind(GetMpPreference)
            .Bind(GetSecurityHealthState)
            .TapIf(IsDefaultAntivirus(), SetDataServiceDefenderEnable)
            .Bind(GetControlledFolderState);

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
                dataService.DefenderFileMissing = file;
                return false;
            })
                ? Result.Success() : Result.Failure(nameof(RequirementsFailure.DefenderFileMissing));
        }

        private Result GetSettingsPageVisibility()
        {
            var pageVisibility = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer")?.GetValue("SettingsPageVisibility") as string ?? string.Empty;
            return pageVisibility.Contains("hide:windowsdefender") ? Result.Failure(nameof(RequirementsFailure.DefenderSettingsPageHidden)) : Result.Success();
        }

        private Result GetServiceState()
        {
            return defenderServices.TrueForAll(service =>
            {
                if (osService.Exist(service))
                {
                    App.Logger.LogDefenderServiceState(service, true);
                    return true;
                }

                App.Logger.LogDefenderServiceBroken(service);
                dataService.DefenderServiceBroken = service;
                return false;
            })
                ? Result.Success() : Result.Failure(nameof(RequirementsFailure.DefenderServiceFailure));
        }

        private Result GetMpComputerStatus()
        {
            try
            {
                _ = instrumentationService.GetAntiSpywareEnabled();
                return Result.Success();
            }
            catch (Exception e)
            {
                App.Logger.LogDefenderAntiSpywareEnabledException(e);
                return Result.Failure(nameof(RequirementsFailure.DefenderIsBroken));
            }
        }

        private Result GetAntivirusProducts()
        {
            var antivirusProducts = instrumentationService.GetAntivirusProductsOrDefault();

            if (antivirusProducts.Count > 0)
            {
                return Result.Success();
            }

            App.Logger.LogDefenderAntivirusProductsIsNull();
            return Result.Failure(nameof(RequirementsFailure.DefenderIsBroken));
        }

        private Result GetMpPreference()
        {
            var script = "(Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess";

            if (powerShellService.InvokeOrDefault<byte>(script) is null)
            {
                dataService.DefenderMpPreferenceBroken = true;
                App.Logger.LogDefenderMpPreferenceIsNull();
                return Result.Failure(nameof(RequirementsFailure.DefenderIsBroken));
            }

            return Result.Success();
        }

        private Result GetSecurityHealthState()
        {
            try
            {
                osService.TryStart("SecurityHealthService");
                var healthState = osService.GetStatus("SecurityHealthService");
                App.Logger.LogDefenderSecurityHealthStatus(healthState);
                return healthState == ServiceControllerStatus.Running ? Result.Success() : Result.Failure(nameof(RequirementsFailure.DefenderSecurityHealthFailure));
            }
            catch (Exception e)
            {
                App.Logger.LogDefenderSecurityHealthException(e);
                return Result.Failure(nameof(RequirementsFailure.DefenderSecurityHealthFailure));
            }
        }

        private bool IsDefaultAntivirus()
        {
            var productState = instrumentationService.GetAntivirusProductsOrDefault()
                .Find(product => product.GetPropertyValue("instanceGuid")
                .Equals("{D68DDC3A-831F-4fae-9E44-DA132C1ACF46}"))
                ?.GetPropertyValue("productState");
            var defenderState = productState is null ? "00" : string.Format("0x{0:x}", productState).Substring(3, 2);
            var isDefault = !(defenderState.Equals("00") || defenderState.Equals("01"));
            App.Logger.LogDefenderIsDefault(isDefault);
            return isDefault;
        }

        private void SetDataServiceDefenderEnable() => dataService.DefenderEnabled = true;

        private Result GetControlledFolderState()
        {
            try
            {
                var folderState = powerShellService.Invoke<byte>("(Get-MpPreference -ErrorAction Stop).EnableControlledFolderAccess");
                App.Logger.LogDefenderControlledFolderState(folderState);
                return folderState.Equals(1) ? Result.Failure(nameof(RequirementsFailure.DefenderControlledFolderEnable)) : Result.Success();
            }
            catch (Exception e)
            {
                App.Logger.LogDefenderControlledFolderException(e);
                return Result.Failure(nameof(RequirementsFailure.DefenderControlledFolderEnable));
            }
        }
    }
}
