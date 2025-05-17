// <copyright file="IOsService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    using System.ServiceProcess;

    /// <summary>
    /// A service for working with Windows services API.
    /// </summary>
    public interface IOsService
    {
        /// <summary>
        /// Get the hash for the change of state "News and Interests" widget on the taskbar.
        /// </summary>
        /// <param name="enable">Enable or disable widget.</param>
        uint GetNewsInterestsHashData(bool enable);

        /// <summary>
        /// Gets the status of the service.
        /// </summary>
        /// <param name="name">Service name.</param>
        ServiceControllerStatus? GetStatus(string name);

        /// <summary>
        /// Sets the startup mode of the Windows service.
        /// </summary>
        /// <param name="service">Represents a Windows service and allows you to connect to a running or stopped.</param>
        /// <param name="mode">Indicates the start mode of the service.</param>
        void SetServiceStartMode(ServiceController service, ServiceStartMode mode);

        /// <summary>
        /// Returns true if the service exists.
        /// </summary>
        /// <param name="name">Service name.</param>
        bool Exist(string name);

        /// <summary>
        /// Determines that the VBSCRIPT component is installed.
        /// </summary>
        bool VBSIsInstalled();
    }
}
