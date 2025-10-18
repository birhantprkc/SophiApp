// <copyright file="GroupPolicyService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Helpers;

    /// <inheritdoc/>
    public class GroupPolicyService : IGroupPolicyService
    {
        private readonly IProcessService processService;
        private readonly bool gpeditExists = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "gpedit.msc"));
        private readonly string lgpoSettingsFile = Environment.ExpandEnvironmentVariables("%TEMP%\\LGPO.txt");
        private readonly string lgpoExeFile = Path.Combine(AppContext.BaseDirectory, "Binaries", "LGPO.exe");

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupPolicyService"/> class.
        /// </summary>
        /// <param name="processService">A service for working with Windows <see cref="Process"/> API.</param>
        public GroupPolicyService(IProcessService processService)
        {
            this.processService = processService;
        }

        /// <inheritdoc/>
        public void ClearRegistryCache(RegistryKey registryKey, string subKey, string value)
        {
            registryKey.OpenSubKey(subKey, true)?.DeleteValue(value, false);
            registryKey.Dispose();
        }

        /// <inheritdoc/>
        public void ClearRegistryCache(RegistryKey registryKey, string subKey, params string[] values)
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
        public void ClearRegistryCache(string subKey, string value, params RegistryKey[] registryKeys)
        {
            foreach (var key in registryKeys)
            {
                key.OpenSubKey(subKey, true)?.DeleteValue(value, false);
                key.Dispose();
            }
        }

        /// <inheritdoc/>
        public void ClearRegistryCache<T>(RegistryKey registryKey, string subKey, string name, T value, RegistryValueKind kind)
            where T : struct
        {
            registryKey.OpenSubKey(subKey, true)?.SetValue(name, value, kind);
            registryKey.Dispose();
        }

        /// <inheritdoc/>
        public void ClearLocalCache(LGPOScope scope, string path, string name, string type = "", string value = "")
        {
            if (gpeditExists)
            {
                var settingValues = string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(value)
                    ? [scope.ToString(), path, name, "DELETE", string.Empty]
                    : new string[5] { scope.ToString(), path, name, $"{type}:{value}", string.Empty };
                File.AppendAllLines(lgpoSettingsFile, settingValues, System.Text.Encoding.UTF8);
            }
        }

        /// <inheritdoc/>
        public void ClearLocalCache(LGPOScope scope, string path, params string[] names)
        {
            if (gpeditExists)
            {
                var nameValues = new List<string>();

                foreach (var name in names)
                {
                    nameValues.AddRange([scope.ToString(), path, name, "DELETE", string.Empty]);
                }

                File.AppendAllLines(lgpoSettingsFile, nameValues, System.Text.Encoding.UTF8);
            }
        }

        /// <inheritdoc/>
        public void ClearLocalCache(string path, string name, params LGPOScope[] scopes)
        {
            if (gpeditExists)
            {
                var scopeValues = new List<string>();

                foreach (var scope in scopes)
                {
                    scopeValues.AddRange([scope.ToString(), path, name, "DELETE", string.Empty]);
                }

                File.AppendAllLines(lgpoSettingsFile, scopeValues, System.Text.Encoding.UTF8);
            }
        }

        /// <inheritdoc/>
        public void UpdateLocalPolicy()
        {
            if (File.Exists(lgpoSettingsFile) && File.Exists(lgpoExeFile))
            {
                _ = processService.WaitForExit(name: lgpoExeFile, arguments: $"/t {lgpoSettingsFile}");
                _ = processService.WaitForExit(name: "gpupdate.exe", arguments: "/force");
                File.Delete(lgpoSettingsFile);
            }
        }
    }
}
