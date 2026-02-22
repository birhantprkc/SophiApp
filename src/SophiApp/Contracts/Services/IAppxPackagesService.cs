// <copyright file="IAppxPackagesService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    using Windows.ApplicationModel;

    /// <summary>
    /// A service for working with appx packages API.
    /// </summary>
    public interface IAppxPackagesService
    {
        /// <summary>
        /// Checks by ID that the package is installed.
        /// </summary>
        /// <param name="packageId">The ID of the package being checked, not to be confused with the package Display name.</param>
        /// <param name="allUsers">Search in installed packages for all users or only for the current user.</param>
        bool PackageExist(string packageId, bool allUsers = false);

        /// <summary>
        /// Get appx package or default.
        /// </summary>
        /// <param name="packageId">The ID of the package being checked, not to be confused with the package Display name.</param>
        /// <param name="allUsers">Search in installed packages for all users or only for the current user.</param>
        Package? GetPackageOrDefault(string packageId, bool allUsers = false);

        /// <summary>
        /// Retrieves information about a appx packages.
        /// </summary>
        /// <param name="allUsers">Search in installed packages for all users or only for the current user.</param>
        List<Package> GetPackages(bool allUsers = false);

        /// <summary>
        /// Removes appx package.
        /// </summary>
        /// <param name="packageId">The appx package identity name.</param>
        /// <param name="allUsers">Remove a package for all users or current user only.</param>
        void RemovePackage(string packageId, bool allUsers = false);

        /// <summary>
        /// Installs the appx application using the file.
        /// </summary>
        /// <param name="appxPath">Path to the appx installation file.</param>
        Task InstallFromFileAsync(string appxPath);
    }
}
