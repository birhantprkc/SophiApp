// <copyright file="OneDriveService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using Microsoft.Win32;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using static System.Environment;

    /// <inheritdoc/>
    public class OneDriveService : IOneDriveService
    {
        private readonly IHttpService httpService;
        private readonly IPowerShellService powerShellService;
        private readonly IProcessService processService;
        private readonly IScheduledTaskService scheduledTaskService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneDriveService"/> class.
        /// </summary>
        /// <param name="powerShellService">A service for working with Windows PowerShell API.</param>
        /// <param name="processService">A service for working with Windows <see cref="System.Diagnostics.Process"/> API.</param>
        /// <param name="scheduledTaskService">A service for working with Scheduled Task API.</param>
        /// <param name="httpService">A service for working with HTTP API.</param>
        public OneDriveService(IPowerShellService powerShellService, IProcessService processService, IScheduledTaskService scheduledTaskService, IHttpService httpService)
        {
            this.powerShellService = powerShellService;
            this.processService = processService;
            this.scheduledTaskService = scheduledTaskService;
            this.httpService = httpService;
        }

        /// <inheritdoc/>
        public string GetUninstallStringOrDefault()
        {
            var uninstallPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\OneDriveSetup.exe";
            var uninstallString = "UninstallString";
            var uninstallValue = Registry.CurrentUser.OpenSubKey(uninstallPath)?.GetValue(uninstallString) as string
                ?? Registry.LocalMachine.OpenSubKey(uninstallPath)?.GetValue(uninstallString) as string;
            return uninstallValue?.Replace("\"", null) ?? string.Empty;
        }

        /// <inheritdoc/>
        public string GetUserDataFolderOrDefault()
        {
            return Registry.CurrentUser.OpenSubKey("Environment")
                ?.GetValue("OneDrive") as string ?? string.Empty;
        }

        /// <inheritdoc/>
        public void Install()
        {
            if (SetupFileExist())
            {
                var uninstallString = GetUninstallStringOrDefault();
                var process = uninstallString.Substring(0, uninstallString.IndexOf(".exe") + 4);
                var arguments = uninstallString.Substring(uninstallString.IndexOf("/"));
                _ = processService.WaitForExit(process, arguments);
            }
            else
            {
                var downloadPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
                var downloadFolder = Registry.CurrentUser.OpenSubKey(downloadPath)?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string ?? string.Empty;
                var downloadFile = Path.Combine(downloadFolder, "OneDriveSetup.exe");
                httpService.DownloadOneDrive(downloadFile);
                processService.WaitForExit(name: downloadFile);
                Thread.Sleep(3000);
                File.Delete(downloadFile);
            }
        }

        /// <inheritdoc/>
        public bool SetupFileExist()
        {
            var uninstallString = GetUninstallStringOrDefault();

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
            var uninstallString = GetUninstallStringOrDefault();
            var setupFolder = GetSetupFolder();
            var processPath = uninstallString.Substring(0, uninstallString.IndexOf(".exe") + 4);
            var processArguments = uninstallString.Substring(uninstallString.IndexOf("/"));
            var userDataFolder = GetUserDataFolderOrDefault();

            // PowerShell is used to avoid going through all the files and folders, of which there may be many, to filter user and system files.
            var userFilesCount = powerShellService.Invoke<int>("(Get-ChildItem -Path $env:OneDrive -ErrorAction Ignore | Measure-Object).Count");
            processService.KillProcessByName(timeout: 1000, "OneDrive", "OneDriveSetup", "FileCoAuth");
            _ = processService.WaitForExit(processPath, processArguments);

            if (userFilesCount.Equals(0))
            {
                // Uses PowerShell to avoid the "Access Denied" error, not to go through all the files and folders, of which there may be many, to set the Normal attribute.
                // See https://stackoverflow.com/questions/1701457/directory-delete-doesnt-work-access-denied-error-but-under-windows-explorer-it
                _ = powerShellService.Invoke("Remove-Item -Path $env:OneDrive -Recurse -Force -ErrorAction Ignore");
                processService.SetAutoRestartShell(allow: false);
                processService.KillProcessByName("explorer");
                Thread.Sleep(3000);
                processService.SetAutoRestartShell(allow: true);
                processService.KillProcessByName("UserOOBEBroker");
                UnregisterFileSyncShell(setupFolder);

                // Uses PowerShell to avoid the "Access Denied" error, not to go through all the files and folders, of which there may be many, to set the Normal attribute.
                // See https://stackoverflow.com/questions/1701457/directory-delete-doesnt-work-access-denied-error-but-under-windows-explorer-it
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
                App.Logger.LogOneDriveUserFilesExist(userDataFolder, userFilesCount);
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
            var uninstallString = GetUninstallStringOrDefault();
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
