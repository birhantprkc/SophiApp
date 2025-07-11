// <copyright file="GroupPolicyService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;

    /// <inheritdoc/>
    public class GroupPolicyService : IGroupPolicyService
    {
        /// <inheritdoc/>
        public void ClearPolicyCache(RegistryKey registryKey, string subKey, string value)
        {
            registryKey.OpenSubKey(subKey, true)?.DeleteValue(value, false);
            registryKey.Dispose();
        }

        /// <inheritdoc/>
        public void ClearPolicyCache(RegistryKey registryKey, string subKey, params string[] values)
        {
            var regKey = registryKey.OpenSubKey(subKey, true);

            foreach (var value in values)
            {
                regKey?.DeleteValue(value, false);
            }

            regKey?.Dispose();
            registryKey.Dispose();
        }

        /// <inheritdoc/>
        public void ClearPolicyCache(string subKey, string value, params RegistryKey[] registryKeys)
        {
            foreach (var key in registryKeys)
            {
                key.OpenSubKey(subKey, true)?.DeleteValue(value, false);
                key.Dispose();
            }
        }

        /// <inheritdoc/>
        public void SetPolicyValue<T>(RegistryKey registryKey, string subKey, string name, T value, RegistryValueKind kind)
            where T : struct
        {
            registryKey.OpenSubKey(subKey)?.SetValue(name, value, kind);
            registryKey.Dispose();
        }
    }
}
