// <copyright file="OsProperties.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Helpers
{
    using System.Management;
    using Microsoft.Win32;

    /// <summary>
    /// Data transfer object for os properties.
    /// </summary>
    public class OsProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OsProperties"/> class.
        /// </summary>
        public OsProperties()
        {
            Caption = "n/a";
            BuildNumber = -1;
            UpdateBuildRevision = -1;
            Edition = "n/a";
            CSName = "n/a";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OsProperties"/> class.
        /// </summary>
        /// <param name="properties">A collection of os properties.</param>
        public OsProperties(PropertyDataCollection properties)
        {
            Caption = (string?)properties[nameof(Caption)]?.Value ?? "n/a";
            BuildNumber = int.Parse((string?)properties[nameof(BuildNumber)]?.Value ?? "-1");
            UpdateBuildRevision = (int?)RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion")?.GetValue("UBR") ?? -1;
            Edition = (string?)RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion")?.GetValue("EditionID") ?? "n/a";
            CSName = (string?)properties[nameof(CSName)]?.Value ?? "n/a";
        }

        /// <summary>
        /// Gets os caption version.
        /// </summary>
        public string Caption { get; init; }

        /// <summary>
        /// Gets os build version.
        /// </summary>
        public int BuildNumber { get; init; }

        /// <summary>
        /// Gets os UBR version.
        /// </summary>
        public int UpdateBuildRevision { get; init; }

        /// <summary>
        /// Gets os edition.
        /// </summary>
        public string Edition { get; init; }

        /// <summary>
        /// Gets PC name.
        /// </summary>
        public string CSName { get; init; }
    }
}
