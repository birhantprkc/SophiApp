// <copyright file="RedistributablePackageService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using System;
    using System.Diagnostics;

    /// <inheritdoc/>
    public class RedistributablePackageService : IRedistributablePackageService
    {
        private readonly IHttpService httpService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedistributablePackageService"/> class.
        /// </summary>
        /// <param name="httpService">A service for working with HTTP API.</param>
        public RedistributablePackageService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        /// <inheritdoc/>
        public void DeleteInstallerLogs(string logPattern)
        {
            Directory.GetFileSystemEntries(Path.GetTempPath(), logPattern, SearchOption.TopDirectoryOnly)
                .ForEach(File.Delete);
        }

        /// <inheritdoc/>
        public T GetPackageRelease<T>(string url)
            where T : class
        {
            var releasedJson = httpService.ReadAsJson(url);
            return Json.ToObject<T>(releasedJson);
        }

        /// <inheritdoc/>
        public Version GetInstalledPackageVersionOrDefault(string name)
        {
            var packageCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Package Cache");
            var installer = Directory.GetFileSystemEntries(packageCache, name, SearchOption.AllDirectories).FirstOrDefault();
            return installer is null ? new Version("0.0.0") : Version.Parse(FileVersionInfo.GetVersionInfo(installer).FileVersion!);
        }
    }
}
