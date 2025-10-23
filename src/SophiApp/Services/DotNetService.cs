// <copyright file="DotNetService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;
    using System;
    using System.Diagnostics;

    /// <inheritdoc/>
    public class DotNetService : IDotNetService
    {
        private readonly IHttpService httpService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetService"/> class.
        /// </summary>
        /// <param name="httpService">A service for working with HTTP API.</param>
        public DotNetService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        /// <inheritdoc/>
        public void DeleteInstallerLogs()
        {
            Directory.GetFileSystemEntries(Path.GetTempPath(), "Microsoft_Windows_Desktop_Runtime*.log", SearchOption.TopDirectoryOnly)
                .ForEach(log => File.Delete(log));
        }

        /// <inheritdoc/>
        public DotNetReleases GetReleasesInfo(string url)
        {
            if (httpService.UrlIsAvailable(url))
            {
                var releasedJson = httpService.ReadAsJson(url);
                return JsonExtensions.ToObject<DotNetReleases>(releasedJson);
            }

            throw new InvalidOperationException($"Releases url is unavailable: {url}");
        }

        /// <inheritdoc/>
        public Version GetInstallerVersionOrDefault(string name)
        {
            var packageCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Package Cache");
            var installer = Directory.GetFileSystemEntries(packageCache, name, SearchOption.AllDirectories).FirstOrDefault();
            return installer is null ? new Version("0.0.0") : Version.Parse(FileVersionInfo.GetVersionInfo(installer).FileVersion!);
        }
    }
}
