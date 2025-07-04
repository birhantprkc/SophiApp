// <copyright file="UpdateService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using System.Diagnostics;
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;

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
        public bool AllowedOtherProductsUpdate()
        {
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

        private dynamic? GetServiceManager()
        {
            Type type = Type.GetTypeFromProgID("Microsoft.Update.ServiceManager") !;
            return Activator.CreateInstance(type);
        }

        private void RunMicrosoftProductsUpdate()
        {
            if (commonDataService.IsWindows11)
            {
                var settingsPath = "Software\\Microsoft\\WindowsUpdate\\UX\\Settings";
                Registry.LocalMachine.OpenSubKey(settingsPath)?.SetValue("AllowMUUpdateService", 1, RegistryValueKind.DWord);
                return;
            }

            dynamic? service = GetServiceManager();
            _ = service?.AddService2("7971f918-a847-4430-9279-4a52d1efe18d", 7, string.Empty);
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
