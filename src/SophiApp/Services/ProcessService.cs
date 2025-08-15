// <copyright file="ProcessService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using System.Diagnostics;

    /// <inheritdoc/>
    public class ProcessService : IProcessService
    {
        /// <inheritdoc/>
        public bool IsExist(string name)
        {
            return Array.Exists(Process.GetProcessesByName(name), process => process.ProcessName.Equals(name));
        }

        /// <inheritdoc/>
        public void KillProcessByName(int timeout, params string[] processes)
        {
            foreach (var process in processes)
            {
                KillProcessByName(process, timeout);
            }
        }

        /// <inheritdoc/>
        public void KillProcessByName(string name, int timeout = 1000)
        {
            Process.GetProcessesByName(name)
                .ForEach(process =>
                {
                    process.Kill();
                    process.WaitForExit(timeout);
                    process.Dispose();
                });
        }

        /// <inheritdoc/>
        public Process WaitForExit(string name, string arguments)
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = name;
            process.StartInfo.Arguments = arguments;
            _ = process.Start();
            process.WaitForExit();
            return process;
        }

        /// <inheritdoc/>
        public void SetAutoRestartShell(bool allow)
        {
            var winlogonPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";
            Registry.LocalMachine.OpenSubKey(winlogonPath, true)?.SetValue("AutoRestartShell", allow ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <inheritdoc/>
        public Process? StartProcessByName(string name, string arguments = "", ProcessWindowStyle style = ProcessWindowStyle.Normal)
        {
            return Process.Start(new ProcessStartInfo()
            {
                FileName = name,
                Arguments = arguments,
                WindowStyle = style,
            });
        }
    }
}
