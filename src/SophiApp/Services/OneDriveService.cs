// <copyright file="OneDriveService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using System.Diagnostics;
    using static System.Environment;

    /// <inheritdoc/>
    public class OneDriveService : IOneDriveService
    {
        private readonly IPowerShellService powerShellService;
        private readonly IProcessService processService;
        private readonly IScheduledTaskService scheduledTaskService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneDriveService"/> class.
        /// </summary>
        /// <param name="powerShellService">A service for working with Windows PowerShell API.</param>
        /// <param name="processService">A service for working with Windows <see cref="Process"/> API.</param>
        /// <param name="scheduledTaskService">A service for working with Scheduled Task API.</param>
        public OneDriveService(IPowerShellService powerShellService, IProcessService processService, IScheduledTaskService scheduledTaskService)
        {
            this.powerShellService = powerShellService;
            this.processService = processService;
            this.scheduledTaskService = scheduledTaskService;
        }

        /// <inheritdoc/>
        public string GetUninstallString()
        {
            var command = @"Get-Package -Name ""Microsoft OneDrive"" -ProviderName Programs -ErrorAction Ignore | ForEach-Object -Process {$_.Meta.Attributes[""UninstallString""]}";
            return powerShellService.Invoke(command).FirstOrDefault()?.BaseObject?.ToString()?.Replace("\"", null) ?? string.Empty;
        }

        /// <inheritdoc/>
        public string GetUserDataFolderOrDefault()
        {
            return Registry.CurrentUser.OpenSubKey("Environment")?.GetValue("OneDrive") as string ?? string.Empty;
        }

        /// <inheritdoc/>
        public bool SetupFileExist()
        {
            var uninstallString = GetUninstallString();

            if (string.IsNullOrWhiteSpace(uninstallString))
            {
                return false;
            }

            var filePath = uninstallString[.. (uninstallString.IndexOf(".exe") + 4)];
            return Path.Exists(filePath);
        }

        /// <inheritdoc/>
        public bool IsInstalled()
        {
            var package = powerShellService.Invoke(@"Get-Package -Name ""Microsoft OneDrive"" -ProviderName Programs").FirstOrDefault();
            return package is not null;
        }

        /// <inheritdoc/>
        public void Uninstall()
        {
            var uninstallString = GetUninstallString();
            var setupFolder = GetSetupFolder();
            var processPath = uninstallString.Substring(0, uninstallString.IndexOf(".exe") + 4);
            var processArguments = uninstallString.Substring(uninstallString.IndexOf("/"));
            var userDataFolder = GetUserDataFolderOrDefault();
            var userFilesCount = powerShellService.Invoke<int>("(Get-ChildItem -Path $env:OneDrive -ErrorAction Ignore | Measure-Object).Count");
            processService.KillProcessByName(timeout: 1000, "OneDrive", "OneDriveSetup", "FileCoAuth");
            _ = processService.WaitForExit(processPath, processArguments);

            if (userFilesCount.Equals(0))
            {
                _ = powerShellService.Invoke("Remove-Item -Path $env:OneDrive -Recurse -Force -ErrorAction Ignore");
                processService.SetAutoRestartShell(allow: false);
                processService.KillProcessByName("explorer");
                Thread.Sleep(3000);
                processService.SetAutoRestartShell(allow: true);
                processService.KillProcessByName("UserOOBEBroker");
                UnregisterFileSyncShell(setupFolder);
                _ = powerShellService.Invoke($"Remove-Item -Path \"{setupFolder}\" -Force -Recurse -ErrorAction Ignore");
                _ = processService.StartProcessByName("explorer");
                Thread.Sleep(3000);
                Registry.CurrentUser.OpenSubKey("Environment", true)?.DeleteValue("OneDrive", false);
                Registry.CurrentUser.OpenSubKey("Environment", true)?.DeleteValue("OneDriveConsumer", false);
                scheduledTaskService.UnregisterOneDriveTasks();
                DeleteResources();
            }
            else
            {
                processService.StartProcessByName("explorer.exe", userDataFolder);
            }
        }

        /// <inheritdoc/>
        public bool UserIsLogged()
        {
            var personalPath = "Software\\Microsoft\\OneDrive\\Accounts\\Personal";
            var userEmail = Registry.CurrentUser.OpenSubKey(personalPath)?.GetValue("UserEmail") as string ?? string.Empty;
            return !string.IsNullOrWhiteSpace(userEmail);
        }

        private void DeleteResources()
        {
            var localAppData = GetFolderPath(SpecialFolder.LocalApplicationData);
            var startMenu = GetFolderPath(SpecialFolder.StartMenu);
            var programData = GetFolderPath(SpecialFolder.CommonApplicationData);
            var programFiles = GetFolderPath(SpecialFolder.ProgramFiles);
            var systemDrive = Path.GetPathRoot(SystemDirectory) !;

            var deletedResources = new List<string>()
            {
                Path.Combine(localAppData, "OneDrive"), Path.Combine(localAppData, "Microsoft\\OneDrive"), Path.Combine(localAppData, "Microsoft\\OneAuth"),
                Path.Combine(startMenu, "Programs\\OneDrive.lnk"), Path.Combine(programData, "Microsoft OneDrive"), Path.Combine(programFiles, "Microsoft OneDrive"),
                Path.Combine(systemDrive, "OneDriveTemp"),
            };

            deletedResources.ForEach(r =>
            {
                if (Directory.Exists(r))
                {
                    Directory.Delete(r, true);
                }
                else if (File.Exists(r))
                {
                    File.Delete(r);
                }
            });

            Registry.CurrentUser.DeleteSubKeyTree("Software\\Microsoft\\OneDrive", false);
        }

        private string GetSetupFolder()
        {
            var uninstallString = GetUninstallString();
            return uninstallString[..uninstallString.LastIndexOf('\\')];
        }

        private void UnregisterFileSyncShell(string setupFolder)
        {
            var regSvr = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\regsvr32.exe";
            Directory.GetFileSystemEntries(path: setupFolder, searchPattern: "FileSyncShell64.dll", SearchOption.AllDirectories)
                .ForEach(dll => processService.WaitForExit(name: regSvr, arguments: $"/u /s {dll}"));
        }
    }
}
