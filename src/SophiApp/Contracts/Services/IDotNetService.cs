// <copyright file="IDotNetService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    using SophiApp.Helpers;

    /// <summary>
    /// A service for working .NET API.
    /// </summary>
    public interface IDotNetService
    {
        /// <summary>
        /// Delete .NET installer log files.
        /// </summary>
        void DeleteInstallerLogs();

        /// <summary>
        /// Get .NET releases information from <paramref name="url"/>.
        /// </summary>
        /// <param name="url">.NET releases url.</param>
        DotNetReleases GetReleasesInfo(string url);

        /// <summary>
        /// Get .NET offline installer version.
        /// </summary>
        /// <param name="name">.NET installer file name.</param>
        Version GetInstallerVersionOrDefault(string name);
    }
}
