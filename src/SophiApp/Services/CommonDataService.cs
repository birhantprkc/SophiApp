// <copyright file="CommonDataService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using CSharpFunctionalExtensions;
    using Microsoft.UI.Input;
    using SophiApp.Contracts.Services;
    using SophiApp.Helpers;
    using System;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <inheritdoc/>
    public class CommonDataService : ICommonDataService
    {
        private static readonly InputCursor HandCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        private static InputCursor userCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        private readonly AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
        private readonly IHttpService httpService;
        private readonly IInstrumentationService instrumentationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonDataService"/> class.
        /// </summary>
        /// <param name="instrumentationService">Service for working with WMI.</param>
        /// <param name="httpService">Service for working with HTTP API.</param>
        public CommonDataService(IInstrumentationService instrumentationService, IHttpService httpService)
        {
            this.instrumentationService = instrumentationService;
            this.httpService = httpService;
            OsProperties = new ();
        }

        /// <summary>
        /// Gets or sets app user cursor.
        /// </summary>
        public static InputCursor UserCursor
        {
            get => userCursor;
            set
            {
                if (userCursor != value)
                {
                    userCursor = value;
                }
            }
        }

        /// <summary>
        /// Gets url hovering cursor.
        /// </summary>
        public static InputCursor UrlCursor => HandCursor;

        /// <inheritdoc/>
        public bool IsWindows11 { get => OsProperties.Caption.Contains("11"); }

        /// <inheritdoc/>
        public OsProperties OsProperties { get; private set; }

        /// <inheritdoc/>
        public string DetectedMalware { get; set; } = string.Empty;

        /// <inheritdoc/>
        public bool DefenderControlledFolderAccess { get; set; } = false;

        /// <inheritdoc/>
        public string DefenderFileMissing { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string DefenderServiceBroken { get; set; } = string.Empty;

        /// <inheritdoc/>
        public bool DefenderMpPreferenceBroken { get; set; } = false;

        /// <inheritdoc/>
        public bool DefenderEnabled { get; set; } = false;

        /// <inheritdoc/>
        public Version AppVersion => assembly.Version ?? new Version(0, 0, 0);

        /// <inheritdoc/>
        public AppVersion? LatestAppRelease { get; private set; }

        /// <inheritdoc/>
        public NetRelease? LatestReleaseNET8 { get; private set; }

        /// <inheritdoc/>
        public NetRelease? LatestReleaseNET9 { get; private set; }

        /// <inheritdoc/>
        public VCRelease? LatestReleaseVC { get; private set; }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                OsProperties = instrumentationService.GetOsPropertiesOrDefault();
                App.Logger.LogAppProperties(version: assembly.Version!, directory: AppContext.BaseDirectory);
            });
        }

        /// <inheritdoc/>
        public async Task<Result> GetExternalServicesDataAsync()
        {
            await Task.WhenAll(SetLatestAppReleaseAsync(), SetNet8ReleaseAsync(), SetNet9ReleaseAsync(), SetVCReleaseAsync());
            return Result.Success();
        }

        /// <inheritdoc/>
        public string GetBuildName() => "Daria";

        /// <inheritdoc/>
        public string GetDelimiter() => "|";

        /// <inheritdoc/>
        public string GetFullName() => $"{assembly.Name} {assembly.Version!.Major}.{assembly.Version.Minor}.{assembly.Version.Build}";

        private async Task SetLatestAppReleaseAsync()
        {
            try
            {
                LatestAppRelease = await httpService.GetFromJsonAsync<AppVersion>("https://raw.githubusercontent.com/Sophia-Community/SophiApp/master/sophiapp_versions.json", 5);
            }
            catch
            {
                await Task.CompletedTask;
            }
        }

        private async Task SetNet8ReleaseAsync()
        {
            try
            {
                LatestReleaseNET8 = await httpService.GetFromJsonAsync<NetRelease>("https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json", 5);
            }
            catch (Exception)
            {
                await Task.CompletedTask;
            }
        }

        private async Task SetNet9ReleaseAsync()
        {
            try
            {
                LatestReleaseNET9 = await httpService.GetFromJsonAsync<NetRelease>("https://builds.dotnet.microsoft.com/dotnet/release-metadata/9.0/releases.json", 5);
            }
            catch (Exception)
            {
                await Task.CompletedTask;
            }
        }

        private async Task SetVCReleaseAsync()
        {
            try
            {
                LatestReleaseVC = await httpService.GetFromJsonAsync<VCRelease>("https://raw.githubusercontent.com/ScoopInstaller/Extras/refs/heads/master/bucket/vcredist2022.json", 5);
            }
            catch (Exception)
            {
                await Task.CompletedTask;
            }
        }
    }
}
