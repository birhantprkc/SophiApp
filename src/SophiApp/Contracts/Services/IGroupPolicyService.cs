// <copyright file="IGroupPolicyService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    using Microsoft.Win32;

    /// <summary>
    /// A service for working with group policy API.
    /// </summary>
    public interface IGroupPolicyService
    {
        /// <summary>
        /// Clear the registry value cache to make changes visible in UI.
        /// </summary>
        /// <param name="registryKey">Represents a key-level node in the Windows registry.</param>
        /// <param name="subKey">Registry subkey path to open.</param>
        /// <param name="value">The name of the value to delete.</param>
        public void ClearPolicyCache(RegistryKey registryKey, string subKey, string value);

        /// <summary>
        /// Clear the registry value cache to make changes visible in UI.
        /// </summary>
        /// <param name="registryKey">Represents a key-level node in the Windows registry.</param>
        /// <param name="subKey">Registry subkey path to open.</param>
        /// <param name="values">The name of the values to delete.</param>
        public void ClearPolicyCache(RegistryKey registryKey, string subKey, params string[] values);

        /// <summary>
        /// Clear the registry value cache to make changes visible in UI.
        /// </summary>
        /// <param name="subKey">Registry subkey path to open.</param>
        /// <param name="value">The name of the values to delete.</param>
        /// <param name="registryKeys">Represents a key-level nodes in the Windows registry.</param>
        public void ClearPolicyCache(string subKey, string value, params RegistryKey[] registryKeys);

        /// <summary>
        /// Set registry value to clear policy cache and make changes visible in UI.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="value"/>.</typeparam>
        /// <param name="registryKey">Represents a key-level nodes in the Windows registry.</param>
        /// <param name="subKey">Registry subkey path to open.</param>
        /// <param name="name">The name of the value to be stored.</param>
        /// <param name="value">The data to be stored.</param>
        /// <param name="kind">Specifies the data types to use when storing values in the registry.</param>
        public void SetPolicyValue<T>(RegistryKey registryKey, string subKey, string name, T value, RegistryValueKind kind)
            where T : struct;
    }
}
