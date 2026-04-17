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
            SupportedUBR = new ();
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
        public OsProperties OsProperties { get; private set; }

        /// <inheritdoc/>
        public string DetectedMalware { get; set; } = string.Empty;

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
        public DotNetRelease? LatestReleaseNET8 { get; private set; }

        /// <inheritdoc/>
        public DotNetRelease? LatestReleaseNET9 { get; private set; }

        /// <inheritdoc/>
        public DotNetRelease? LatestReleaseNET10 { get; private set; }

        /// <inheritdoc/>
        public VCRelease? LatestReleaseVC { get; private set; }

        /// <inheritdoc/>
        public SupportedUBR SupportedUBR { get; private set; }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                OsProperties = instrumentationService.GetOsPropertiesOrDefault();
                OsProperties.IsLTSC = OsProperties.Caption.Contains("LTSC");
                App.Logger.LogAppProperties(version: assembly.Version!, directory: AppContext.BaseDirectory);
            });
        }

        /// <inheritdoc/>
        public async Task<Result> GetExternalServicesDataAsync()
        {
            try
            {
                await Task.WhenAll(
                        SetLatestAppReleaseAsync(),
                        SetNet8ReleaseAsync(),
                        SetNet9ReleaseAsync(),
                        SetNet10ReleaseAsync(),
                        SetVCReleaseAsync(),
                        SetSupportedBuildsAsync());

                return Result.Success();
            }
            catch
            {
                return Result.Success();
            }
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
                LatestReleaseNET8 = await httpService.GetFromJsonAsync<DotNetRelease>("https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json", 5);
            }
            catch
            {
                await Task.CompletedTask;
            }
        }

        private async Task SetNet9ReleaseAsync()
        {
            try
            {
                LatestReleaseNET9 = await httpService.GetFromJsonAsync<DotNetRelease>("https://builds.dotnet.microsoft.com/dotnet/release-metadata/9.0/releases.json", 5);
            }
            catch
            {
                await Task.CompletedTask;
            }
        }

        private async Task SetNet10ReleaseAsync()
        {
            try
            {
                LatestReleaseNET10 = await httpService.GetFromJsonAsync<DotNetRelease>("https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json", 5);
            }
            catch
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
            catch
            {
                await Task.CompletedTask;
            }
        }

        private async Task SetSupportedBuildsAsync()
        {
            try
            {
                SupportedUBR = await httpService.GetFromJsonAsync<SupportedUBR>("https://raw.githubusercontent.com/farag2/Sophia-Script-for-Windows/refs/heads/main/supported_windows_builds.json", 5);
            }
            catch
            {
                await Task.CompletedTask;
            }
        }
    }
}
