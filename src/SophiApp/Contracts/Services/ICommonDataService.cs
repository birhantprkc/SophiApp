// <copyright file="ICommonDataService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    using SophiApp.Helpers;

    /// <summary>
    /// A service for transferring app data between DI layers.
    /// </summary>
    public interface ICommonDataService
    {
        /// <summary>
        /// Gets a values of OS properties.
        /// </summary>
        OsProperties OsProperties { get; }

        /// <summary>
        /// Gets or sets malware name detected by <see cref="IRequirementsService"/>.
        /// </summary>
        string DetectedMalware { get; set; }

        /// <summary>
        /// Gets or sets Microsoft Defender missing files name.
        /// </summary>
        string DefenderFileMissing { get; set; }

        /// <summary>
        /// Gets or sets Microsoft Defender broken service name.
        /// </summary>
        string DefenderServiceBroken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Microsoft Defender preference state.
        /// </summary>
        bool DefenderMpPreferenceBroken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Microsoft Defender enabled state.
        /// </summary>
        bool DefenderEnabled { get; set; }

        /// <summary>
        /// Gets app version.
        /// </summary>
        Version AppVersion { get; }

        /// <summary>
        /// Gets app latest release from GitHub repository.
        /// </summary>
        AppVersion? LatestAppRelease { get; }

        /// <summary>
        /// Gets .NET 8 latest release.
        /// </summary>
        DotNetRelease? LatestReleaseNET8 { get; }

        /// <summary>
        /// Gets .NET 9 latest release.
        /// </summary>
        DotNetRelease? LatestReleaseNET9 { get; }

        /// <summary>
        /// Gets .NET 10 latest release.
        /// </summary>
        DotNetRelease? LatestReleaseNET10 { get; }

        /// <summary>
        /// Gets Visual C++ latest release.
        /// </summary>
        VCRelease? LatestReleaseVC { get; }

        /// <summary>
        /// Gets app supported OS builds version.
        /// </summary>
        SupportedUBR SupportedUBR { get; }

        /// <summary>
        /// Initialize <see cref="ICommonDataService"/> data.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Receiving data from external services.
        /// </summary>
        Task GetExternalServicesDataAsync();

        /// <summary>
        /// Gets app name and version.
        /// </summary>
        string GetFullName();

        /// <summary>
        /// Gets the code name of the application build.
        /// </summary>
        string GetBuildName();

        /// <summary>
        /// Gets app name and version delimiter.
        /// </summary>
        string GetDelimiter();
    }
}
