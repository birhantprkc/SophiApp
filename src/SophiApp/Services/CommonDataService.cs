// <copyright file="CommonDataService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using System;
    using System.Reflection;
    using Microsoft.UI.Input;
    using SophiApp.Contracts.Services;
    using SophiApp.Helpers;

    /// <inheritdoc/>
    public class CommonDataService : ICommonDataService
    {
        private static readonly InputCursor HandCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        private static InputCursor userCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        private readonly AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
        private readonly IInstrumentationService instrumentationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonDataService"/> class.
        /// </summary>
        /// <param name="instrumentationService">Service for working with WMI.</param>
        public CommonDataService(IInstrumentationService instrumentationService)
        {
            this.instrumentationService = instrumentationService;
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
        public Version AppVersion => assembly.Version!;

        /// <inheritdoc/>
        public void Initialize()
        {
            OsProperties = instrumentationService.GetOsPropertiesOrDefault();
            App.Logger.LogAppProperties(version: assembly.Version!, directory: AppContext.BaseDirectory);
        }

        /// <inheritdoc/>
        public string GetBuildName() => "Daria";

        /// <inheritdoc/>
        public string GetDelimiter() => "|";

        /// <inheritdoc/>
        public string GetFullName() => $"{assembly.Name} {assembly.Version!.Major}.{assembly.Version.Minor}.{assembly.Version.Build}";
    }
}
