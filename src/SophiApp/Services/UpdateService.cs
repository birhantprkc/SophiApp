// <copyright file="UpdateService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using System.Diagnostics;

    /// <inheritdoc/>
    public class UpdateService : IUpdateService
    {
        private readonly IInstrumentationService instrumentationService;
        private readonly ICommonDataService commonDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateService"/> class.
        /// </summary>
        /// <param name="instrumentationService">Service for working with WMI.</param>
        /// <param name="commonDataService">Service for transferring app data between layers of DI.</param>
        public UpdateService(IInstrumentationService instrumentationService, ICommonDataService commonDataService)
        {
            this.instrumentationService = instrumentationService;
            this.commonDataService = commonDataService;
        }

        /// <inheritdoc/>
        public bool HasMicrosoftProductsUpdate()
        {
            if (commonDataService.IsWindows11)
            {
                var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings")
                    ?.GetValue("AllowMUUpdateService") as int? ?? -1;
                return isEnabled.Equals(1);
            }

            var serviceManager = GetServiceManager() !;

            for (int i = 0; i < serviceManager.Services.Count; i++)
            {
                if (serviceManager.Services[i].ServiceID == "7971f918-a847-4430-9279-4a52d1efe18d")
                {
                    return serviceManager.Services[i].IsDefaultAUService;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void RunOsUpdate()
        {
            try
            {
                RunMicrosoftProductsUpdate();
                RunUwpAppsUpdate();
                RunOsUpdates();
            }
            catch (Exception ex)
            {
                App.Logger.LogOsUpdateException(ex);
            }
        }

        /// <inheritdoc/>
        public void RunMicrosoftProductsUpdate()
        {
            if (commonDataService.IsWindows11)
            {
                Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings")
                    ?.SetValue("AllowMUUpdateService", 1, RegistryValueKind.DWord);
                return;
            }

            var serviceManager = GetServiceManager() !;
            serviceManager.AddService2("7971f918-a847-4430-9279-4a52d1efe18d", 7, string.Empty);
        }

        /// <inheritdoc/>
        public void StopMicrosoftProductsUpdate()
        {
            if (commonDataService.IsWindows11)
            {
                Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings")
                    ?.DeleteValue("AllowMUUpdateService", false);
                return;
            }

            var serviceManager = GetServiceManager() !;
            var productsServiceId = "7971f918-a847-4430-9279-4a52d1efe18d";

            for (int i = 0; i < serviceManager.Services.Count; i++)
            {
                if (serviceManager.Services[i].ServiceID == productsServiceId && serviceManager.Services[i].IsDefaultAUService)
                {
                    serviceManager.RemoveService(productsServiceId);
                }
            }
        }

        private dynamic? GetServiceManager()
        {
            Type type = Type.GetTypeFromProgID("Microsoft.Update.ServiceManager") !;
            return Activator.CreateInstance(type);
        }

        private void RunUwpAppsUpdate()
        {
            _ = instrumentationService.GetUwpAppsManagementOrDefault()?.InvokeMethod("UpdateScanMethod", Array.Empty<object>());
        }

        private void RunOsUpdates()
        {
            _ = Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = "UsoClient.exe",
                        Arguments = "StartInteractiveScan",
                    });

            _ = Process.Start(
                new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = "ms-settings:windowsupdate",
                });
        }
    }
}
