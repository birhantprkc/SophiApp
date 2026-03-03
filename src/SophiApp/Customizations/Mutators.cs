// <copyright file="Mutators.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Customizations
{
    using Microsoft.Win32;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.ServiceProcess;
    using System.Text;
    using TaskScheduler = Microsoft.Win32.TaskScheduler;

    /// <summary>
    /// Set the OS settings.
    /// </summary>
    public static class Mutators
    {
        private static readonly IAppNotificationService AppNotificationService = App.GetService<IAppNotificationService>();
        private static readonly IAppxPackagesService AppxPackagesService = App.GetService<IAppxPackagesService>();
        private static readonly ICommonDataService DataService = App.GetService<ICommonDataService>();
        private static readonly ICursorsService CursorsService = App.GetService<ICursorsService>();
        private static readonly IRedistributablePackageService RedistributablePackageService = App.GetService<IRedistributablePackageService>();
        private static readonly IFileService FileService = App.GetService<IFileService>();
        private static readonly IFirewallService FirewallService = App.GetService<IFirewallService>();
        private static readonly IGroupPolicyService GroupPolicyService = App.GetService<IGroupPolicyService>();
        private static readonly IHttpService HttpService = App.GetService<IHttpService>();
        private static readonly IInstrumentationService InstrumentationService = App.GetService<IInstrumentationService>();
        private static readonly IOneDriveService OneDriveService = App.GetService<IOneDriveService>();
        private static readonly IOsService OsService = App.GetService<IOsService>();
        private static readonly IPowerShellService PowerShellService = App.GetService<IPowerShellService>();
        private static readonly IProcessService ProcessService = App.GetService<IProcessService>();
        private static readonly IRegistryService RegistryService = App.GetService<IRegistryService>();
        private static readonly IScheduledTaskService ScheduledTaskService = App.GetService<IScheduledTaskService>();
        private static readonly IUpdateService UpdateService = App.GetService<IUpdateService>();

        /// <summary>
        /// Set DiagTrack service state.
        /// </summary>
        /// <param name="enable">DiagTrack service state.</param>
        public static void DiagTrackService(bool enable)
        {
            // Connected User Experiences and Telemetry
            // Disabling the "Connected User Experiences and Telemetry" service (DiagTrack) can cause you not being able to get Xbox achievements anymore and affects Feedback Hub
            var diagTrackService = new System.ServiceProcess.ServiceController("DiagTrack");
            var firewallRule = FirewallService.GetGroupRules("DiagTrack").First();

            if (enable)
            {
                OsService.SetStartMode(diagTrackService, ServiceStartMode.Automatic);
                diagTrackService.TryStart();

                // Allow connection for the Unified Telemetry Client Outbound Traffic
                firewallRule.Enabled = true;
                firewallRule.Action = NetFwTypeLib.NET_FW_ACTION_.NET_FW_ACTION_ALLOW;

                return;
            }

            diagTrackService.TryStop();
            OsService.SetStartMode(diagTrackService, ServiceStartMode.Disabled);

            // Block connection for the Unified Telemetry Client Outbound Traffic
            firewallRule.Enabled = true;
            firewallRule.Action = NetFwTypeLib.NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
        }

        /// <summary>
        /// Set Windows feature "Diagnostic data level" state.
        /// </summary>
        /// <param name="state">Diagnostic data level state.</param>
        public static void DiagnosticDataLevel(int state)
        {
            if (state.Equals(2))
            {
                var osEdition = DataService.OsProperties.Edition;
                var isEnterpriseOrEducation = osEdition.Contains("Enterprise") || osEdition.Contains("Education");

                // 0 — Diagnostic data off
                // 1 — Send required diagnostic data
                // 2 — Optional diagnostic data
                Registry.LocalMachine.OpenOrCreateSubKey("Software\\Policies\\Microsoft\\Windows\\DataCollection").SetValue("AllowTelemetry", isEnterpriseOrEducation ? 0 : 1, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection").SetValue("MaxTelemetryAllowed", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack").SetValue("ShowedToastAtLevel", 1, RegistryValueKind.DWord);

                // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
                GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry", "DWORD", isEnterpriseOrEducation ? "0" : "1");

                return;
            }

            Registry.LocalMachine.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection").SetValue("MaxTelemetryAllowed", 3, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack").SetValue("ShowedToastAtLevel", 3, RegistryValueKind.DWord);
            Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\DataCollection", true)?.DeleteValue("AllowTelemetry", false);

            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry");
        }

        /// <summary>
        /// Set Windows feature "Error reporting" state.
        /// </summary>
        /// <param name="enable">Feature state.</param>
        public static void ErrorReporting(bool enable)
        {
            using var werService = new System.ServiceProcess.ServiceController("WerSvc");

            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", Registry.LocalMachine, Registry.CurrentUser);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", LGPOScope.Computer, LGPOScope.User);

            var reportingTask = ScheduledTaskService.GetTaskOrDefault("Microsoft\\Windows\\Windows Error Reporting\\QueueReporting");
            ScheduledTaskService.SetState(reportingTask, enable);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Windows Error Reporting", true)?.DeleteValue("Disabled", false);

                OsService.SetStartMode(werService, ServiceStartMode.Manual);
                werService.TryStart();

                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Windows Error Reporting", true)?.SetValue("Disabled", 1, RegistryValueKind.DWord);

            OsService.SetStartMode(werService, ServiceStartMode.Disabled);
            werService.TryStop();
        }

        /// <summary>
        /// Set Windows feature "Feedback frequency" state.
        /// </summary>
        /// <param name="state">Feedback frequency state.</param>
        public static void FeedbackFrequency(int state)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\DataCollection", "DoNotShowFeedbackNotifications");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\DataCollection", "DoNotShowFeedbackNotifications");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Siuf\\Rules", true)?.DeleteValue("PeriodInNanoSeconds", false);

            if (state.Equals(2))
            {
                Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Siuf\\Rules").SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Siuf\\Rules", true)?.DeleteValue("NumberOfSIUFInPeriod", false);
        }

        /// <summary>
        /// Set telemetry scheduled tasks state.
        /// </summary>
        /// <param name="enable">Scheduled tasks state.</param>
        public static void ScheduledTasks(bool enable)
        {
            new List<TaskScheduler.Task?>()
             {
                // Gathers Win32 application data for App Backup scenario
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\MareBackup"),
                // Collects program telemetry information if opted-in to the Microsoft Customer Experience Improvement Program
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser"),
                // Collects program telemetry information if opted-in to the Microsoft Customer Experience Improvement Program
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser Exp"),
                // Updates compatibility database
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\StartupAppTask"),
                // This task collects and uploads autochk SQM data if opted-in to the Microsoft Customer Experience Improvement Program
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\ProgramDataUpdater"),
                // If the user has consented to participate in the Windows Customer Experience Improvement Program, this job collects and sends usage data to Microsoft
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Autochk\\Proxy"),
                // The USB CEIP (Customer Experience Improvement Program) task collects Universal Serial Bus related statistics and information about your machine and sends it to the Windows Device Connectivity engineering group at Microsoft
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator"),
                // The USB CEIP (Customer Experience Improvement Program) task collects Universal Serial Bus related statistics and information about your machine and sends it to the Windows Device Connectivity engineering group at Microsoft
                // The information received is used to help improve the reliability, stability, and overall functionality of USB in Windows
                // If the user has not consented to participate in Windows CEIP, this task does not do anything
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip"),
                // The Windows Disk Diagnostic reports general disk and system information to Microsoft for users participating in the Customer Experience Program
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector"),
                // This task shows various Map related toasts
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Maps\\MapsToastTask"),
                // This task checks for updates to maps which you have downloaded for offline use
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Maps\\MapsUpdateTask"),
             }
            .ForEach(task => ScheduledTaskService.SetState(task, enable));
        }

        /// <summary>
        /// Set Windows feature "Sign-in info" state.
        /// </summary>
        /// <param name="enable">Sign-in info state.</param>
        public static void SigninInfo(bool enable)
        {
            var userSid = InstrumentationService.GetUserSid(Environment.UserName);
            var userArsoPath = $"Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\\UserARSO\\{userSid}";

            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableAutomaticRestartSignOn");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableAutomaticRestartSignOn");

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(userArsoPath, true)?.DeleteValue("OptOut", false);
                return;
            }

            Registry.LocalMachine.OpenOrCreateSubKey(userArsoPath).SetValue("OptOut", 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set language list access state.
        /// </summary>
        /// <param name="enable">Language list state.</param>
        public static void LanguageListAccess(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Control Panel\\International\\User Profile", true)?.DeleteValue("HttpAcceptLanguageOptOut", false);
                return;
            }

            Registry.CurrentUser.OpenSubKey("Control Panel\\International\\User Profile", true)?.SetValue("HttpAcceptLanguageOptOut", 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the permission for apps to use advertising ID state.
        /// </summary>
        /// <param name="enable">Advertising ID state.</param>
        public static void AdvertisingID(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\AdvertisingInfo", "DisabledByGroupPolicy");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\DataCollection", "DisabledByGroupPolicy");
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo").SetValue("Enabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the Windows welcome experiences state.
        /// </summary>
        /// <param name="enable">Windows welcome experiences state.</param>
        public static void WindowsWelcomeExperience(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", true)
                ?.SetValue("SubscribedContent-310093enable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows tips state.
        /// </summary>
        /// <param name="enable">Windows tips state.</param>
        public static void WindowsTips(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableSoftLanding");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableSoftLanding");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", true)
                ?.SetValue("SubscribedContent-338389enable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the suggested content in the Settings app state.
        /// </summary>
        /// <param name="enable">Suggested content state.</param>
        public static void SettingsSuggestedContent(bool enable)
        {
            new List<string> { "SubscribedContent-353694Enable", "SubscribedContent-353696Enable", "SubscribedContent-338393Enable" }
            .ForEach(content => Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", true)
                ?.SetValue(content, enable ? 1 : 0, RegistryValueKind.DWord));
        }

        /// <summary>
        /// Set the automatic installing suggested apps state.
        /// </summary>
        /// <param name="enable">Suggested apps state.</param>
        public static void AppsSilentInstalling(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", true)
                ?.SetValue("SilentInstalledAppsenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the Windows feature "Whats New" state.
        /// </summary>
        /// <param name="enable">Whats New state.</param>
        public static void WhatsNewInWindows(bool enable)
        {
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement")
                .SetValue("ScoobeSystemSettingEnable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows feature "Tailored experiences" state.
        /// </summary>
        /// <param name="enable">Tailored experiences state.</param>
        public static void TailoredExperiences(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableTailoredExperiencesWithDiagnosticData");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.User, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableTailoredExperiencesWithDiagnosticData");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", true)
                ?.SetValue("TailoredExperiencesWithDiagnosticDataEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows feature "Bing search" state.
        /// </summary>
        /// <param name="enable">Bing search state.</param>
        public static void BingSearch(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer", true)?.DeleteValue("DisableSearchBoxSuggestions", false);

                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer").SetValue("DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(scope: LGPOScope.User, path: "Software\\Policies\\Microsoft\\Windows\\Explorer", name: "DisableSearchBoxSuggestions", type: "DWORD", value: "1");
        }

        /// <summary>
        /// Set Start menu recommendations state.
        /// </summary>
        /// <param name="enable">Start menu recommendations state.</param>
        public static void StartRecommendationsTips(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.DeleteValue("Start_IrisRecommendations", false);
                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("Start_IrisRecommendations", 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start Menu notifications state.
        /// </summary>
        /// <param name="enable">Start Menu notifications state.</param>
        public static void StartAccountNotifications(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.DeleteValue("Start_AccountNotifications", false);
                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("Start_AccountNotifications", 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the "This PC" icon on Desktop state.
        /// </summary>
        /// <param name="enable">"This PC" icon state.</param>
        public static void ThisPC(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel")
                    .SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0, RegistryValueKind.DWord);

                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", true)
                ?.DeleteValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", false);
        }

        /// <summary>
        /// Set item check boxes state.
        /// </summary>
        /// <param name="enable">Item check boxes state.</param>
        public static void CheckBoxes(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("AutoCheckSelect", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set hidden files, folders, and drives state.
        /// </summary>
        /// <param name="enable">Hidden items state.</param>
        public static void HiddenItems(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("Hidden", enable ? 1 : 2, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set file name extensions visibility state.
        /// </summary>
        /// <param name="enable">File extensions visibility state.</param>
        public static void FileExtensions(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("HideFileExt", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set folder merge conflicts state.
        /// </summary>
        /// <param name="enable">Folder merge conflicts state.</param>
        public static void MergeConflicts(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("HideMergeConflicts", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set how to open File Explorer.
        /// </summary>
        /// <param name="state">File Explorer open state.</param>
        public static void OpenFileExplorerTo(int state)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("LaunchTo", state, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer ribbon state.
        /// </summary>
        /// <param name="state">File Explorer ribbon state.</param>
        public static void FileExplorerRibbon(int state)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "ExplorerRibbonStartsMinimized", Registry.LocalMachine, Registry.CurrentUser);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "ExplorerRibbonStartsMinimized", LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Ribbon")
                .SetValue("MinimizedStateTabletModeOff", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer compact mode state.
        /// </summary>
        /// <param name="enable">File Explorer compact mode state.</param>
        public static void FileExplorerCompactMode(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("UseCompactMode", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer provider notification visibility state.
        /// </summary>
        /// <param name="enable">File Explorer provider notification visibility state.</param>
        public static void OneDriveFileExplorerAd(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("ShowSyncProviderNotifications", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set snap a window state.
        /// </summary>
        /// <param name="enable">Snap Assist state.</param>
        public static void SnapAssist(bool enable)
        {
            // Property type is string
            Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true)?.SetValue("WindowArrangementActive", "1", RegistryValueKind.String);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("SnapAssist", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set file transfer dialog box mode.
        /// </summary>
        /// <param name="state">File transfer dialog box state.</param>
        public static void FileTransferDialog(int state)
        {
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager")
                .SetValue("EnthusiastMode", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set recycle bin confirmation dialog state.
        /// </summary>
        /// <param name="enable">Recycle bin dialog state.</param>
        public static void RecycleBinDeleteConfirmation(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "ConfirmFileDelete", Registry.LocalMachine, Registry.CurrentUser);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "ConfirmFileDelete", LGPOScope.Computer, LGPOScope.User);

            var confirmation = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer")?.GetValue("ShellState") as byte[] ?? new byte[5];
            confirmation[4] = enable ? (byte)51 : (byte)55;
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)?.SetValue("ShellState", confirmation, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set recently used Quick access files state.
        /// </summary>
        /// <param name="enable">Quick access files state.</param>
        public static void QuickAccessRecentFiles(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Explorer", "NoRecentDocsHistory");
            GroupPolicyService.DeleteRegistryValue(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "NoRecentDocsHistory", LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)?.SetValue("ShowRecent", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set frequently used Quick access folders state.
        /// </summary>
        /// <param name="enable">Quick access folders state.</param>
        public static void QuickAccessFrequentFolders(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)?.SetValue("ShowFrequent", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar alignment state.
        /// </summary>
        /// <param name="state">Taskbar alignment state.</param>
        public static void TaskbarAlignment(int state)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("TaskbarAl", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar widgets icon state.
        /// </summary>
        /// <param name="enable">Taskbar widgets icon state.</param>
        public static void TaskbarWidgets(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests", "value");
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Dsh", "AllowNewsAndInterests");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Dsh", "AllowNewsAndInterests");
            // We cannot set a value to TaskbarDa, having called any of APIs, except of copying powershell.exe (or any other tricks) with a different name,
            // due to a UCPD driver tracks all executables to block the access to the registry
            var command = $"-Command \"& {{New-ItemProperty -Path HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced -Name TaskbarDa -PropertyType DWord -Value {(enable ? 1 : 0)} -Force}}\"";
            PowerShellService.InvokeCommandBypassUCPD(command);
        }

        /// <summary>
        /// Set Search on the taskbar state.
        /// </summary>
        /// <param name="state">Taskbar search state.</param>
        public static void TaskbarSearchWindows10(int state)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "DisableSearch", "SearchOnTaskbarMode");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "DisableSearch", "SearchOnTaskbarMode");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search", true)
                ?.SetValue("SearchboxTaskbarMode", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Search on the taskbar state.
        /// </summary>
        /// <param name="state">Taskbar search state.</param>
        public static void TaskbarSearchWindows11(int state)
        {
            GroupPolicyService.SetRegistryValue(Registry.LocalMachine, "Software\\Microsoft\\PolicyManager\\default\\Search\\DisableSearch", "value", 0, RegistryValueKind.DWord);
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "DisableSearch", "SearchOnTaskbarMode");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "DisableSearch", "SearchOnTaskbarMode");

            var searchMode = state switch
            {
                3 => 3,
                4 => 2,
                _ => state - 1,
            };

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search", true)?.SetValue("SearchboxTaskbarMode", searchMode, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set search highlights state.
        /// </summary>
        /// <param name="enable">Search highlights state.</param>
        public static void SearchHighlightsWindows10(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "EnableDynamicContentInWSB");
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "EnableDynamicContentInWSB");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Feeds\\DSB", true)
                ?.SetValue("ShowDynamicContent", enable ? 1 : 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", true)
                ?.SetValue("IsDynamicSearchBoxEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set search highlights state.
        /// </summary>
        /// <param name="enable">Search highlights state.</param>
        public static void SearchHighlightsWindows11(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "EnableDynamicContentInWSB");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "EnableDynamicContentInWSB");

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search", true)?.DeleteValue("BingSearchEnabled", false);
                Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer", true)?.DeleteValue("DisableSearchBoxSuggestions", false);
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", true)
                ?.SetValue("IsDynamicSearchBoxEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Cortana button taskbar state.
        /// </summary>
        /// <param name="enable">Cortana button state.</param>
        public static void CortanaButton(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "AllowCortana");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "AllowCortana");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("ShowCortanaButton", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar task view button state.
        /// </summary>
        /// <param name="enable">Taskbar task view button state.</param>
        public static void TaskViewButton(bool enable)
        {
            if (DataService.IsWindows11)
            {
                GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideTaskViewButton", Registry.CurrentUser, Registry.LocalMachine);
                GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideTaskViewButton", LGPOScope.User, LGPOScope.Computer);
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("ShowTaskViewButton", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set News and Interests state.
        /// </summary>
        /// <param name="enable">News and Interests state.</param>
        public static void NewsInterests(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Windows Feeds", "EnableFeeds");
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Windows Feeds", "value");

            // https://forums.mydigitallife.net/threads/taskbarda-widgets-registry-change-is-now-blocked.88547/#post-1849006
            var hashData = OsService.GetNewsAndInterestsHash(enable);
            var feedsCommand = $"-Command \"& {{New-ItemProperty -Path HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds -Name ShellFeedsTaskbarViewMode -PropertyType DWord -Value {(enable ? 0 : 2)} -Force}}\"";
            var hashCommand = $"-Command \"& {{New-ItemProperty -Path HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds -Name EnShellFeedsTaskbarViewMode -PropertyType DWord -Value {hashData} -Force}}\"";
            PowerShellService.InvokeCommandBypassUCPD(feedsCommand);
            PowerShellService.InvokeCommandBypassUCPD(hashCommand);
        }

        /// <summary>
        /// Set taskbar people icon state.
        /// </summary>
        /// <param name="enable">Taskbar people icon state.</param>
        public static void PeopleTaskbar(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "HidePeopleBar", Registry.CurrentUser, Registry.LocalMachine);
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People")
                ?.SetValue("PeopleBand", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Meet Now icon state.
        /// </summary>
        /// <param name="enable">Meet Now icon state.</param>
        public static void MeetNow(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HideSCAMeetNow", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HideSCAMeetNow", LGPOScope.User, LGPOScope.Computer);

            var stuckSettings = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3")?.GetValue("Settings") as byte[] ?? new byte[10];
            stuckSettings[9] = enable ? (byte)0 : (byte)128;
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3", true)
                ?.SetValue("Settings", stuckSettings, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set Windows Ink Workspace button state.
        /// </summary>
        /// <param name="enable">Windows Ink Workspace button state.</param>
        public static void WindowsInkWorkspace(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\WindowsInkWorkspace", "AllowWindowsInkWorkspace");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HideSCAMeetNow");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\PenWorkspace", true)
                ?.SetValue("PenWorkspaceButtonDesiredVisibility", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set notification area icons state.
        /// </summary>
        /// <param name="enable">Notification area icons state.</param>
        public static void NotificationAreaIcons(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoAutoTrayNotify", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoAutoTrayNotify", LGPOScope.User, LGPOScope.Computer);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)
                ?.SetValue("EnableAutoTray", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set seconds on the taskbar clock state.
        /// </summary>
        /// <param name="enable">Seconds on the taskbar clock state.</param>
        public static void SecondsInSystemClock(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("ShowSecondsInSystemClock", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar combine state.
        /// </summary>
        /// <param name="state">Taskbar combine state.</param>
        public static void TaskbarCombine(int state)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoTaskGrouping", Registry.LocalMachine, Registry.CurrentUser);
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoTaskGrouping", LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("TaskbarGlomLevel", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set end task in taskbar by click state.
        /// </summary>
        /// <param name="enable">Taskbar end task state.</param>
        public static void TaskbarEndTask(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings")
                    .SetValue("TaskbarEndTask", 1, RegistryValueKind.DWord);

                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings", true)
                ?.DeleteValue("TaskbarEndTask", false);
        }

        /// <summary>
        /// Set Control Panel icons view state.
        /// </summary>
        /// <param name="state">Control Panel icons view state.</param>
        public static void ControlPanelView(int state)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "ForceClassicControlPanel");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.User, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "ForceClassicControlPanel");

            switch (state)
            {
                case 1:
                    Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel")
                        .SetValue("AllItemsIconView", 0, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel", true)
                        ?.SetValue("StartupPage", 0, RegistryValueKind.DWord);

                    break;
                case 2:
                    Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel")
                        .SetValue("AllItemsIconView", 0, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel", true)
                        ?.SetValue("StartupPage", 1, RegistryValueKind.DWord);

                    break;
                default:
                    Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel")
                        .SetValue("AllItemsIconView", 1, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel", true)
                        ?.SetValue("StartupPage", 1, RegistryValueKind.DWord);

                    break;
            }
        }

        /// <summary>
        /// Set Windows color mode state.
        /// </summary>
        /// <param name="state">Windows color mode state.</param>
        public static void WindowsColorMode(int state)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true)
                ?.SetValue("SystemUsesLightTheme", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set apps color mode state.
        /// </summary>
        /// <param name="state">Apps color mode state.</param>
        public static void AppColorMode(int state)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true)
                ?.SetValue("AppsUseLightTheme", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set "New App Installed" indicator state.
        /// </summary>
        /// <param name="enable">New App Installed" indicator state.</param>
        public static void NewAppInstalledNotification(bool enable)
        {
            if (enable)
            {
                Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer", true)
                    ?.DeleteValue("NoNewAppAlert", false);
                return;
            }

            Registry.LocalMachine.OpenOrCreateSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer")
                .SetValue("NoNewAppAlert", 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set first sign-in animation state.
        /// </summary>
        /// <param name="enable">First sign-in animation state.</param>
        public static void FirstLogonAnimation(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "EnableFirstLogonAnimation");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "EnableFirstLogonAnimation");
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", true)
                ?.SetValue("EnableFirstLogonAnimation", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set JPEG wallpapers quality state.
        /// </summary>
        /// <param name="state">JPEG wallpapers quality state.</param>
        public static void JPEGWallpapersQuality(int state)
        {
            if (state.Equals(1))
            {
                Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true)
                    ?.SetValue("JPEGImportQuality", 100, RegistryValueKind.DWord);

                return;
            }

            Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true)
                ?.DeleteValue("JPEGImportQuality", false);
        }

        /// <summary>
        /// Set "- Shortcut" suffix state.
        /// </summary>
        /// <param name="enable">"- Shortcut" suffix state.</param>
        public static void ShortcutsSuffix(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)?.DeleteValue("link", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates", true)?.DeleteValue("ShortcutNameTemplate", false);

                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates")
                ?.SetValue("ShortcutNameTemplate", "%s.lnk", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Print screen button state.
        /// </summary>
        /// <param name="enable">Print screen button state.</param>
        public static void PrtScnSnippingTool(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Control Panel\\Keyboard", true)?.SetValue("PrintScreenKeyForSnippingEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set input method for app window state.
        /// </summary>
        /// <param name="enable">Input method for app window state.</param>
        public static void AppsLanguageSwitch(bool enable)
        {
            _ = PowerShellService.Invoke(enable ? "Set-WinLanguageBarOption -UseLegacySwitchMode" : "Set-WinLanguageBarOption");
        }

        /// <summary>
        /// Set Aero Shake state.
        /// </summary>
        /// <param name="enable">Aero Shake state.</param>
        public static void AeroShaking(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "NoWindowMinimizingShortcuts", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "NoWindowMinimizingShortcuts", LGPOScope.User, LGPOScope.Computer);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("DisallowShaking", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set "Windows 11 Cursors Concept" from Jepri Creations state.
        /// </summary>
        /// <param name="state">Cursors state.</param>
        public static void Cursors(int state)
        {
            switch (state)
            {
                case 1:
                    CursorsService.SetJepriCreationsCursors(JepriCursorsTheme.Dark);
                    break;

                case 2:
                    CursorsService.SetJepriCreationsCursors(JepriCursorsTheme.Light);
                    break;

                default:
                    CursorsService.SetDefaultCursors();
                    break;
            }

            CursorsService.ReloadCursors();
        }

        /// <summary>
        /// Set files and folders grouping state.
        /// </summary>
        /// <param name="state">Files and folders grouping state.</param>
        public static void FolderGroupBy(int state)
        {
            var folderPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FolderTypes\\{885a186e-a440-4ada-812b-db871b942259}\\TopViews\\{00000000-0000-0000-0000-000000000000}";
            if (state.Equals(1))
            {
                // Clear any Common Dialog views
                PowerShellService.ClearCommonDialogViews();
                // https://learn.microsoft.com/en-us/windows/win32/properties/props-system-null
                Registry.CurrentUser.OpenOrCreateSubKey(folderPath).SetValue("ColumnList", "System.Null", RegistryValueKind.String);
                Registry.CurrentUser.OpenOrCreateSubKey(folderPath).SetValue("GroupBy", "System.Null", RegistryValueKind.String);
                Registry.CurrentUser.OpenOrCreateSubKey(folderPath).SetValue("LogicalViewMode", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenOrCreateSubKey(folderPath).SetValue("Name", "NoName", RegistryValueKind.String);
                Registry.CurrentUser.OpenOrCreateSubKey(folderPath).SetValue("Order", 0, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenOrCreateSubKey(folderPath).SetValue("PrimaryProperty", "System.ItemNameDisplay", RegistryValueKind.String);
                Registry.CurrentUser.OpenOrCreateSubKey(folderPath).SetValue("SortByList", "prop:System.ItemNameDisplay", RegistryValueKind.String);
                return;
            }

            Registry.CurrentUser.DeleteSubKeyTree(folderPath, false);
        }

        /// <summary>
        /// Set navigation pane expand state.
        /// </summary>
        /// <param name="enable">Navigation pane expand state.</param>
        public static void NavigationPaneExpand(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("NavPaneExpandToCurrentFolder", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu recently added apps state.
        /// </summary>
        /// <param name="enable">Start menu recently added apps state.</param>
        public static void RecentlyAddedStartApps(bool enable)
        {
            // Remove all policies in order to make changes visible in UI
            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideRecentlyAddedApps", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideRecentlyAddedApps", LGPOScope.User, LGPOScope.Computer);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.DeleteValue("ShowRecentList", false);
                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.SetValue("ShowRecentList", 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set most used apps in Start.
        /// </summary>
        /// <param name="enable">Start menu used apps state.</param>
        public static void MostUsedStartApps(bool enable)
        {
            if (DataService.IsWindows11)
            {
                GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "ShowOrHideMostUsedApps", Registry.CurrentUser, Registry.LocalMachine);
                // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
                GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "ShowOrHideMostUsedApps", LGPOScope.User, LGPOScope.Computer);
                GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoStartMenuMFUprogramsList", Registry.CurrentUser, Registry.LocalMachine);
                GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoInstrumentation", Registry.CurrentUser, Registry.LocalMachine);
                // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
                GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoStartMenuMFUprogramsList", LGPOScope.User, LGPOScope.Computer);
                GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoInstrumentation", LGPOScope.User, LGPOScope.Computer);

                if (enable)
                {
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.DeleteValue("ShowFrequentList", false);
                    return;
                }

                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.SetValue("ShowFrequentList", 0, RegistryValueKind.DWord);
                return;
            }

            // Remove all policies in order to make changes visible in UI
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoStartMenuMFUprogramsList", Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoStartMenuMFUprogramsList", LGPOScope.Computer);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", true)?.DeleteValue("NoStartMenuMFUprogramsList", false);
                GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoStartMenuMFUprogramsList", LGPOScope.User, LGPOScope.Computer);
                GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoInstrumentation");
                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", true)?.SetValue("NoStartMenuMFUprogramsList", 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu app suggestions state.
        /// </summary>
        /// <param name="enable">Start menu app suggestions state.</param>
        public static void AppSuggestions(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures");
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", true)
                ?.SetValue("SubscribedContent-338388enable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu layout state.
        /// </summary>
        /// <param name="state">Start menu layout state.</param>
        public static void StartLayout(int state)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("Start_Layout", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set recommended section state.
        /// </summary>
        /// <param name="enable">Recommended section state.</param>
        public static void StartRecommendedSection(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideRecommendedSection", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideRecommendedSection", LGPOScope.User, LGPOScope.Computer);
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Microsoft\\PolicyManager\\current\\device\\Education", "IsEducationEnvironment");
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Microsoft\\PolicyManager\\current\\device\\Start", "HideRecommendedSection");
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory", LGPOScope.User, LGPOScope.Computer);
            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideRecentlyAddedApps", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "HideRecentlyAddedApps", LGPOScope.User, LGPOScope.Computer);
            GroupPolicyService.DeleteRegistryValue("Software\\Policies\\Microsoft\\Windows\\Explorer", "ShowOrHideMostUsedApps", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "ShowOrHideMostUsedApps", LGPOScope.User, LGPOScope.Computer);
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoStartMenuMFUprogramsList", Registry.CurrentUser, Registry.LocalMachine);
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoInstrumentation", Registry.CurrentUser, Registry.LocalMachine);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoStartMenuMFUprogramsList", LGPOScope.User, LGPOScope.Computer);
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoInstrumentation", LGPOScope.User, LGPOScope.Computer);

            if (enable)
            {
                // Show recently added apps in Start
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.DeleteValue("ShowRecentList", false);
                // Show most used Apps in Start
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.DeleteValue("ShowFrequentList", false);
                // Show recommendations for tips, shortcuts, new apps, and more in Start
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.DeleteValue("Start_IrisRecommendations", false);
                // Show recommended files in Start, recent files in File Explorer, and items in jump lists
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.DeleteValue("Start_TrackDocs", false);
                return;
            }

            // Hide recently added apps in Start
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.SetValue("ShowRecentList", 0, RegistryValueKind.DWord);
            // Hide most used Apps in Start
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Start", true)?.SetValue("ShowFrequentList", 0, RegistryValueKind.DWord);
            // Hide recommendations for tips, shortcuts, new apps, and more in Start
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("Start_IrisRecommendations", 0, RegistryValueKind.DWord);
            // Hide recommended files in Start, recent files in File Explorer, and items in jump lists
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("Start_TrackDocs", 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set One Drive state.
        /// </summary>
        /// <param name="enable">One Drive state.</param>
        public static void OneDrive(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSyncNGSC");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSyncNGSC");

            if (enable)
            {
                OneDriveService.Install();
                return;
            }

            OneDriveService.Uninstall();
        }

        /// <summary>
        /// Set storage sense state.
        /// </summary>
        /// <param name="enable">Storage sense state.</param>
        public static void StorageSense(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\StorageSense", "AllowStorageSenseGlobal");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\StorageSense", "AllowStorageSenseGlobal");

            if (enable)
            {
                // Turn on Storage Sense
                Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy")
                    .SetValue("01", 1, RegistryValueKind.DWord);
                // Turn on automatic cleaning up temporary system and app files
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", true)
                    ?.SetValue("04", 1, RegistryValueKind.DWord);
                // Run Storage Sense every month
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", true)
                    ?.SetValue("2048", 30, RegistryValueKind.DWord);
                return;
            }

            // Turn off Storage Sense
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy")
                .SetValue("01", 0, RegistryValueKind.DWord);
            // Turn off automatic cleaning up temporary system and app files
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", true)
                ?.SetValue("04", 0, RegistryValueKind.DWord);
            // Run Storage Sense during low free disk space
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", true)
                ?.SetValue("2048", 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set hibernation state.
        /// </summary>
        /// <param name="enable">Hibernation state.</param>
        public static void Hibernation(bool enable)
        {
            var powerConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe");
            _ = ProcessService.WaitForExit(name: powerConfig, arguments: $"/HIBERNATE {(enable ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Set long path limit state.
        /// </summary>
        /// <param name="enable">Long path limit state.</param>
        public static void Win32LongPathsSupport(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\FileSystem", true)?.SetValue("LongPathsEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
            GroupPolicyService.SetRegistryValue(Registry.LocalMachine, "System\\CurrentControlSet\\Control\\FileSystem", "LongPathsEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
            GroupPolicyService.ClearPolicyCache(scope: LGPOScope.Computer, path: "System\\CurrentControlSet\\Control\\FileSystem", name: "LongPathsEnabled", type: "DWORD", value: enable ? "1" : "0");
        }

        /// <summary>
        /// Set BSOD error state.
        /// </summary>
        /// <param name="enable">BSOD error state.</param>
        public static void BSoDStopError(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\CrashControl", true)?.SetValue("DisplayParameters", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set administrator approval mode state.
        /// </summary>
        /// <param name="state">Administrator approval mode state.</param>
        public static void AdminApprovalMode(int state)
        {
            using var approvalKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", true);
            approvalKey?.DeleteValue("FilterAdministratorToken", false);
            approvalKey?.SetValue("ConsentPromptBehaviorUser", 3, RegistryValueKind.DWord);
            approvalKey?.SetValue("EnableInstallerDetection", 1, RegistryValueKind.DWord);
            approvalKey?.SetValue("ValidateAdminCodeSignatures", 0, RegistryValueKind.DWord);
            approvalKey?.SetValue("EnableSecureUIAPaths", 1, RegistryValueKind.DWord);
            approvalKey?.SetValue("EnableLUA", 1, RegistryValueKind.DWord);
            approvalKey?.SetValue("PromptOnSecureDesktop", 1, RegistryValueKind.DWord);
            approvalKey?.SetValue("EnableVirtualization", 1, RegistryValueKind.DWord);
            approvalKey?.SetValue("EnableUIADesktopToggle", 1, RegistryValueKind.DWord);
            approvalKey?.SetValue("ConsentPromptBehaviorAdmin", state.Equals(1) ? 5 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set delivery optimization state.
        /// </summary>
        /// <param name="enable">Delivery optimization state.</param>
        public static void DeliveryOptimization(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\DeliveryOptimization", "DODownloadMode");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\DeliveryOptimization", "DODownloadMode");
            Registry.Users.OpenSubKey("S-1-5-20\\Software\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Settings", true)
                ?.SetValue("DownloadMode", enable ? 1 : 0, RegistryValueKind.DWord);

            if (!enable)
            {
                _ = PowerShellService.Invoke("Delete-DeliveryOptimizationCache -Force");
            }
        }

        /// <summary>
        /// Set Windows manage default printer state.
        /// </summary>
        /// <param name="enable">Default printer manage state.</param>
        public static void WindowsManageDefaultPrinter(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows", true)?.SetValue("LegacyDefaultPrinterMode", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set update Microsoft products state.
        /// </summary>
        /// <param name="enable">Update Microsoft products state.</param>
        public static void UpdateMicrosoftProducts(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AllowMUUpdateService");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AllowMUUpdateService");

            if (enable)
            {
                UpdateService.RunMicrosoftProductsUpdate();
                return;
            }

            UpdateService.StopMicrosoftProductsUpdate();
        }

        /// <summary>
        /// Set restart notification state.
        /// </summary>
        /// <param name="enable">Restart notification state.</param>
        public static void RestartNotification(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetAutoRestartNotificationDisable");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetAutoRestartNotificationDisable");
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings", true)
                ?.SetValue("RestartNotificationsAllowed2", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set restart device after update state.
        /// </summary>
        /// <param name="enable">Restart device after update state.</param>
        public static void RestartDeviceAfterUpdate(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ActiveHoursStart", "ActiveHoursEnd", "SetActiveHours");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ActiveHoursStart", "ActiveHoursEnd", "SetActiveHours");
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings", true)
                ?.SetValue("IsExpedited", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set active hours restart state.
        /// </summary>
        /// <param name="state">Active hours restart state.</param>
        public static void ActiveHours(int state)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoRebootWithLoggedOnUsers", "AlwaysAutoRebootAtScheduledTime");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoRebootWithLoggedOnUsers", "AlwaysAutoRebootAtScheduledTime");
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ActiveHoursStart", "ActiveHoursEnd", "SetActiveHours");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ActiveHoursStart", "ActiveHoursEnd", "SetActiveHours");
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings", true)?.SetValue("SmartActiveHoursState", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows latest update state.
        /// </summary>
        /// <param name="enable">Latest update state.</param>
        public static void WindowsLatestUpdate(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "AllowOptionalContent", "SetAllowOptionalContent");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate", "AllowOptionalContent", "SetAllowOptionalContent");
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings", true)?.SetValue("IsContinuousInnovationOptedIn", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set network adapters power state.
        /// </summary>
        /// <param name="enable">Network adapters power state.</param>
        public static void NetworkAdaptersSavePower(bool enable)
        {
            PowerShellService.SetTurnOffDeviceNetworkAdapterState(enable);
        }

        /// <summary>
        /// Set input method state.
        /// </summary>
        /// <param name="state">Input method state.</param>
        public static void InputMethod(int state)
        {
            if (state.Equals(1))
            {
                _ = PowerShellService.Invoke("Set-WinDefaultInputMethodOverride -InputTip \"0409:00000409\"");
                return;
            }

            Registry.CurrentUser.OpenSubKey("Control Panel\\International\\User Profile", true)?.DeleteValue("InputMethodOverride", false);
        }

        /// <summary>
        /// Set Print Screen folder state.
        /// </summary>
        /// <param name="state">Print Screen folder state.</param>
        public static void WinPrtScrFolder(int state)
        {
            if (state.Equals(1))
            {
                var desktopPath = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")?.GetValue("Desktop") as string;
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true)
                    ?.SetValue("{B7BEDE81-DF94-4682-A7D8-57A52620B86F}", desktopPath!, RegistryValueKind.ExpandString);

                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", true)
                ?.DeleteValue("{B7BEDE81-DF94-4682-A7D8-57A52620B86F}", false);
        }

        /// <summary>
        /// Set recommended troubleshooting state.
        /// </summary>
        /// <param name="state">Recommended troubleshooting state.</param>
        public static void RecommendedTroubleshooting(int state)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\DataCollection", true)?.DeleteValue("AllowTelemetry", false);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry");
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", true)?.DeleteValue("MaxTelemetryAllowed", false);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack", true)?.DeleteValue("ShowedToastAtLevel", false);

            // Turn on Windows Error Reporting
            using var queueReportingTask = ScheduledTaskService.GetTaskOrDefault("Microsoft\\Windows\\Windows Error Reporting\\QueueReporting");
            ScheduledTaskService.SetState(queueReportingTask, true);

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Windows Error Reporting", true)?.DeleteValue("Disabled", false);
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\Windows Error Reporting", true)?.DeleteValue("Disabled", false);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", LGPOScope.User, LGPOScope.Computer);

            using var werService = new System.ServiceProcess.ServiceController("WerSvc");
            OsService.SetStartMode(werService, ServiceStartMode.Manual);
            werService.TryStart();

            Registry.LocalMachine.OpenOrCreateSubKey("Software\\Microsoft\\WindowsMitigation").SetValue("UserPreference", state.Equals(1) ? 3 : 2, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set folders launch separate process state.
        /// </summary>
        /// <param name="enable">Folders launch separate process state.</param>
        public static void FoldersLaunchSeparateProcess(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)?.SetValue("SeparateProcess", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set reserved storage state.
        /// </summary>
        /// <param name="enable">Reserved storage state.</param>
        public static void ReservedStorage(bool enable)
        {
            var command = enable ? "Set-WindowsReservedStorageState -State Enabled" : "Set-WindowsReservedStorageState -State Disabled";
            _ = PowerShellService.Invoke(command);
        }

        /// <summary>
        /// Set help page state.
        /// </summary>
        /// <param name="enable">Help page state.</param>
        public static void F1HelpPage(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\Typelib\\{8cec5860-07a1-11d9-b15e-000d56bfe6ee}", false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Classes\\Typelib\\{8cec5860-07a1-11d9-b15e-000d56bfe6ee}\\1.0\\0\\win64")
                .SetValue(string.Empty, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set Num Lock state.
        /// </summary>
        /// <param name="enable">Num Lock state.</param>
        public static void NumLock(bool enable)
        {
            Registry.Users.OpenSubKey(".DEFAULT\\Control Panel\\Keyboard", true)?.SetValue("InitialKeyboardIndicators", $"{(enable ? "2147483650" : "2147483648")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Caps Lock state.
        /// </summary>
        /// <param name="enable">Caps Lock state.</param>
        public static void CapsLock(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Keyboard Layout", true)?.DeleteValue("Attributes", false);

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Keyboard Layout", true)?.DeleteValue("Scancode Map", false);
                return;
            }

            Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Keyboard Layout", true)
            ?.SetValue("Scancode Map", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 58, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set sticky shift state.
        /// </summary>
        /// <param name="enable">Sticky shift state.</param>
        public static void StickyShift(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Control Panel\\Accessibility\\StickyKeys", true)?.SetValue("Flags", $"{(enable ? "510" : "506")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set autoplay state.
        /// </summary>
        /// <param name="enable">Autoplay state.</param>
        public static void Autoplay(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDriveTypeAutoRun", Registry.LocalMachine, Registry.CurrentUser);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDriveTypeAutoRun", LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers", true)?.SetValue("DisableAutoplay", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set thumbnail cache state.
        /// </summary>
        /// <param name="enable">Thumbnail cache state.</param>
        public static void ThumbnailCacheRemoval(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache", true)
                ?.SetValue("Autorun", enable ? 3 : 0, RegistryValueKind.DWord);
            Registry.LocalMachine.OpenSubKey("Software\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache", true)
                ?.SetValue("Autorun", enable ? 3 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set save restartable apps state.
        /// </summary>
        /// <param name="enable">Restartable apps state.</param>
        public static void SaveRestartableApps(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", true)
                ?.SetValue("RestartApps", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set network discovery state.
        /// </summary>
        /// <param name="enable">Network discovery state.</param>
        public static void NetworkDiscovery(bool enable)
        {
            if (enable)
            {
                // File and printer sharing
                FirewallService.SetGroupRules(name: "@FirewallAPI.dll,-32752", enable: true, profileID: 2);
                // Network discovery
                FirewallService.SetGroupRules(name: "@FirewallAPI.dll,-28502", enable: true, profileID: 2);
                _ = PowerShellService.Invoke("Set-NetConnectionProfile -NetworkCategory Private");

                return;
            }

            FirewallService.SetGroupRules(name: "@FirewallAPI.dll,-32752", enable: false, profileID: 2);
            FirewallService.SetGroupRules(name: "@FirewallAPI.dll,-28502", enable: false, profileID: 2);
        }

        /// <summary>
        /// Set power plan state.
        /// </summary>
        /// <param name="state">Power plan state.</param>
        public static void PowerPlan(int state)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Power\\PowerSettings", "ActivePowerScheme");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Power\\PowerSettings", "ActivePowerScheme");

            var arguments = $"/SETACTIVE {(state.Equals(1) ? "SCHEME_MIN" : "SCHEME_BALANCED")}";
            var powerConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe");
            _ = ProcessService.WaitForExit(powerConfig, arguments);
        }

        /// <summary>
        /// Set RKN bypass state.
        /// </summary>
        /// <param name="enable">RKN bypass state.</param>
        public static void RKNBypass(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true)?.SetValue("AutoConfigURL", "https://p.thenewone.lol:8443/proxy.pac", RegistryValueKind.String);
            }
            else
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true)?.DeleteValue("AutoConfigURL", false);
            }

            // Apply changed proxy settings
            // https://learn.microsoft.com/en-us/windows/win32/wininet/option-flags
            // INTERNET_OPTION_SETTINGS_CHANGED = 39
            // INTERNET_OPTION_REFRESH = 37
            InternetSetOption(0, 39, 0, 0);
            InternetSetOption(0, 37, 0, 0);
        }

        /// <summary>
        /// Set registry backup state.
        /// </summary>
        /// <param name="enable">Registry backup state.</param>
        public static void RegistryBackup(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", true)?.DeleteValue("MaintenanceDisabled", false);
            ScheduledTaskService.SetState(ScheduledTaskService.FindTaskOrDefault("RegIdleBackup"), true);

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Session Manager\\Configuration Manager", true)
                    ?.SetValue("EnablePeriodicBackup", 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Session Manager\\Configuration Manager", true)
                ?.DeleteValue("EnablePeriodicBackup", false);
        }

        /// <summary>
        /// Set restore previous folders state.
        /// </summary>
        /// <param name="enable">Previous folders state.</param>
        public static void RestorePreviousFolders(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("PersistBrowsers", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows Terminal default app state.
        /// </summary>
        /// <param name="state">Windows Terminal state.</param>
        public static void DefaultTerminalApp(int state)
        {
            if (state.Equals(1))
            {
                var appxPackage = AppxPackagesService.GetPackages().First(package => package.Id.Name.Equals("Microsoft.WindowsTerminal"));
                var appxPath = $"Software\\Classes\\PackagedCom\\Package\\{appxPackage.Id.FullName}\\Class";

                Registry.LocalMachine.OpenSubKey(appxPath)?.GetSubKeyNames()
                    .ForEach(key =>
                    {
                        switch (Registry.LocalMachine.OpenSubKey(Path.Combine(appxPath, key))?.GetValue("ServerId") ?? -1)
                        {
                            case 0:
                                Registry.CurrentUser.OpenOrCreateSubKey("Console\\%%Startup").SetValue("DelegationConsole", key, RegistryValueKind.String);

                                break;
                            case 1:
                                Registry.CurrentUser.OpenOrCreateSubKey("Console\\%%Startup").SetValue("DelegationTerminal", key, RegistryValueKind.String);

                                break;
                            default:
                                break;
                        }
                    });

                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Console\\%%Startup").SetValue("DelegationConsole", "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}", RegistryValueKind.String);
            Registry.CurrentUser.OpenSubKey("Console\\%%Startup", true)?.SetValue("DelegationTerminal", "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set clock in notification center state.
        /// </summary>
        /// <param name="enable">Clock state.</param>
        public static void ShowClockInNotificationCenter(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", true)
                ?.SetValue("ShowClockInNotificationCenter", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Download and install latest version .NET 8 desktop runtime from the Microsoft resources.
        /// </summary>
        /// <param name="enable">.NET Desktop install state.</param>
        public static void InstallDotNetRuntime_8(bool enable)
        {
            if (enable)
            {
                var latestRelease = DataService.LatestReleaseNET8!;
                var releaseVersion = $"windowsdesktop-runtime-{latestRelease.Version}-win-x64.exe";
                var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
                var downloadFolder = Registry.CurrentUser.OpenSubKey(shellPath)?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, releaseVersion);
                var downloadUrl = $"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/{latestRelease.Version}/{releaseVersion}";
                HttpService.DownloadFile(downloadUrl, offlineInstaller);
                ProcessService.WaitForExit(offlineInstaller, "/install /passive /norestart");
                File.Delete(offlineInstaller);
                RedistributablePackageService.DeleteInstallerLogs("Microsoft_Windows_Desktop_Runtime*.log");
            }
        }

        /// <summary>
        /// Download and install latest version .NET 9 desktop runtime from the Microsoft resources.
        /// </summary>
        /// <param name="enable">.NET Desktop install state.</param>
        public static void InstallDotNetRuntime_9(bool enable)
        {
            if (enable)
            {
                var latestRelease = DataService.LatestReleaseNET9!;
                var releaseVersion = $"windowsdesktop-runtime-{latestRelease.Version}-win-x64.exe";
                var downloadFolder = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, releaseVersion);
                var downloadUrl = $"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/{latestRelease.Version}/{releaseVersion}";
                HttpService.DownloadFile(downloadUrl, offlineInstaller);
                ProcessService.WaitForExit(offlineInstaller, "/install /passive /norestart");
                File.Delete(offlineInstaller);
                RedistributablePackageService.DeleteInstallerLogs("Microsoft_Windows_Desktop_Runtime*.log");
            }
        }

        /// <summary>
        /// Download and install latest version Visual C++ x86 from the Microsoft resources.
        /// </summary>
        /// <param name="enable">Visual C++ install state.</param>
        public static void InstallVisualC_x86(bool enable)
        {
            if (enable)
            {
                var downloadFolder = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, "VC_redist.x86.exe");
                HttpService.DownloadFile("https://aka.ms/vc14/vc_redist.x86.exe", offlineInstaller);
                ProcessService.WaitForExit(offlineInstaller, "/install /passive /norestart");
                File.Delete(offlineInstaller);
                RedistributablePackageService.DeleteInstallerLogs("dd_vcredist_x86_*.log");
            }
        }

        /// <summary>
        /// Download and install latest version Visual C++ x64 from the Microsoft resources.
        /// </summary>
        /// <param name="enable">Visual C++ install state.</param>
        public static void InstallVisualC_x64(bool enable)
        {
            if (enable)
            {
                var downloadFolder = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, "VC_redist.x64.exe");
                HttpService.DownloadFile("https://aka.ms/vc14/vc_redist.x64.exe", offlineInstaller);
                ProcessService.WaitForExit(offlineInstaller, "/install /passive /norestart");
                File.Delete(offlineInstaller);
                RedistributablePackageService.DeleteInstallerLogs("dd_vcredist_amd64_*.log");
            }
        }

        /// <summary>
        /// Set Windows AI state.
        /// </summary>
        /// <param name="enable">Windows AI state.</param>
        public static void RemoveWindowsAI(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows", true)?.DeleteSubKeyTree("WindowsAI", false);
            Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows", true)?.DeleteSubKeyTree("WindowsCopilot", false);
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows", true)?.DeleteSubKeyTree("WindowsAI", false);
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows", true)?.DeleteSubKeyTree("WindowsCopilot", false);

            if (enable)
            {
                _ = PowerShellService.Invoke("Enable-WindowsOptionalFeature -Online -FeatureName Recall -All -NoRestart; Start-Process -FilePath 'ms-windows-store://pdp/?ProductId=9NHT9RB2F4HD'");
                return;
            }

            _ = PowerShellService.Invoke("Disable-WindowsOptionalFeature -Online -FeatureName Recall -NoRestart");
            AppxPackagesService.RemovePackage("Microsoft.Copilot");
        }

        /// <summary>
        /// Set HEVC state.
        /// </summary>
        /// <param name="enable">HEVC state.</param>
        public static void HEVC(bool enable)
        {
            if (enable)
            {
                var downloadFolder = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")
                    ?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string ?? Environment.GetEnvironmentVariable("TEMP");
                var appxFile = $"{downloadFolder}\\Microsoft.HEVCVideoExtension_8wekyb3d8bbwe.appx";
                _ = Task.Run(async () =>
                {
                    await HttpService.DownloadHEVCAppxAsync(appxFile);
                    await AppxPackagesService.InstallFromFileAsync(appxFile);
                });
                File.Delete(appxFile);
                return;
            }

            AppxPackagesService.RemovePackage(packageId: "Microsoft.HEVCVideoExtension", allUsers: false);
        }

        /// <summary>
        /// Set Cortana auto start state.
        /// </summary>
        /// <param name="enable">Cortana auto start state.</param>
        public static void CortanaAutostart(bool enable)
        {
            Registry.ClassesRoot.OpenSubKey("Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\SystemAppData\\Microsoft.549981C3F5F10_8wekyb3d8bbwe\\CortanaStartupId", true)
                ?.SetValue("State", enable ? 2 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Xbox game bar state.
        /// </summary>
        /// <param name="enable">Xbox game bar state.</param>
        public static void XboxGameBar(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", true)?.SetValue("AppCaptureEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey("System\\GameConfigStore", true)?.SetValue("GameDVR_Enabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Xbox game tips state.
        /// </summary>
        /// <param name="enable">Xbox game tips state.</param>
        public static void XboxGameTips(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\GameBar", true)?.SetValue("ShowStartupPanel", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set GPU scheduling state.
        /// </summary>
        /// <param name="enable">GPU scheduling state.</param>
        public static void GPUScheduling(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers")?.SetValue("HwSchMode", enable ? 2 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set "Windows Cleanup" scheduled task state.
        /// </summary>
        /// <param name="enable">Task state.</param>
        public static void CleanupTask(bool enable)
        {
            ScheduledTaskService.DeleteTaskFolders(["Sophia Script", "SophiApp"]);
            RegistryService.RemoveVolumeCachesStateFlags();

            if (enable)
            {
                AppNotificationService.EnableToastNotification();
                RegistryService.SetVolumeCachesStateFlags();
                AppNotificationService.RegisterAsToastSender("SophiApp");
                AppNotificationService.RegisterCleanupProtocolAsToastSender();
                ScheduledTaskService.RegisterCleanupTask();
                ScheduledTaskService.RegisterCleanupNotificationTask();

                return;
            }

            ScheduledTaskService.UnregisterCleanupTask();
            ScheduledTaskService.UnregisterCleanupNotificationTask();
            ScheduledTaskService.TryDeleteTaskFolder("Sophia");
            AppNotificationService.UnregisterCleanupProtocol();
        }

        /// <summary>
        /// Set scheduled task "SoftwareDistribution" state.
        /// </summary>
        /// <param name="enable">Task state.</param>
        public static void SoftwareDistributionTask(bool enable)
        {
            ScheduledTaskService.DeleteTaskFolders(["Sophia Script", "SophiApp"]);

            if (enable)
            {
                AppNotificationService.EnableToastNotification();
                AppNotificationService.RegisterAsToastSender("SophiApp");
                ScheduledTaskService.RegisterSoftwareDistributionTask();
                return;
            }

            ScheduledTaskService.UnregisterSoftwareDistributionTask();
            ScheduledTaskService.TryDeleteTaskFolder("Sophia");
        }

        /// <summary>
        /// Set scheduled task "Temp" state.
        /// </summary>
        /// <param name="enable">Task state.</param>
        public static void TempTask(bool enable)
        {
            ScheduledTaskService.DeleteTaskFolders(["Sophia Script", "SophiApp"]);

            if (enable)
            {
                AppNotificationService.EnableToastNotification();
                AppNotificationService.RegisterAsToastSender("SophiApp");
                ScheduledTaskService.RegisterTempTask();
                return;
            }

            ScheduledTaskService.UnregisterTempTask();
            ScheduledTaskService.TryDeleteTaskFolder("Sophia");
        }

        /// <summary>
        /// Set Windows network protection state.
        /// </summary>
        /// <param name="enable">Network protection state.</param>
        public static void NetworkProtection(bool enable)
        {
            _ = PowerShellService.Invoke($"Set-MpPreference -EnableNetworkProtection {(enable ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Set Windows PUApps detection state.
        /// </summary>
        /// <param name="enable">PUApps detection state.</param>
        public static void PUAppsDetection(bool enable)
        {
            _ = PowerShellService.Invoke($"Set-MpPreference -PUAProtection {(enable ? "enable" : "Disabled")}");
        }

        /// <summary>
        /// Set Microsoft Defender sandbox state.
        /// </summary>
        /// <param name="enable">Microsoft Defender sandbox state.</param>
        public static void DefenderSandbox(bool enable)
        {
            _ = PowerShellService.Invoke($"setx /M MP_FORCE_USE_SANDBOX {(enable ? "1" : "0")}");
        }

        /// <summary>
        /// Set Windows Event Viewer custom view state.
        /// </summary>
        /// <param name="enable">Event Viewer custom view state.</param>
        public static void EventViewerCustomView(bool enable)
        {
            var viewerXml = $"{Environment.GetEnvironmentVariable("ALLUSERSPROFILE")}\\Microsoft\\Event Viewer\\Views\\ProcessCreation.xml";
            var viewerGuid = "{0CCE922B-69AE-11D9-BED3-505054503030}";
            var xml = @$"<ViewerConfig>
  <QueryConfig>
    <QueryParams>
      <UserQuery />
    </QueryParams>
    <QueryNode>
      <Name>{"EventViewerCustomView_ProcessCreationXml_Name".GetLocalized()}</Name>
      <Description>{"EventViewerCustomView_ProcessCreationXml_Description".GetLocalized()}</Description>
      <QueryList>
        <Query Id=""0"" Path=""Security"">
          <Select Path=""Security"">*[System[(EventID=4688)]]</Select>
        </Query>
      </QueryList>
    </QueryNode>
  </QueryConfig>
</ViewerConfig>";

            // Enable events auditing generated when a process is created (starts)
            if (enable)
            {
                _ = PowerShellService.Invoke($"auditpol /set /subcategory:\"{viewerGuid}\" /success:enable /failure:enable");
                // Include command line in process creation events
                Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit", true)?.SetValue("ProcessCreationIncludeCmdLine_Enabled", 1, RegistryValueKind.DWord);
                FileService.Save(file: viewerXml, content: xml, encoding: Encoding.UTF8);
                // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
                GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit", "ProcessCreationIncludeCmdLine_Enabled", "DWORD", "1");
                return;
            }

            if (!DataService.IsWindows11)
            {
                _ = PowerShellService.Invoke($"auditpol / set / subcategory:\"{viewerGuid}\" / success:disable / failure:disable");
            }

            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit", true)?.DeleteValue("ProcessCreationIncludeCmdLine_Enabled", false);
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit", "ProcessCreationIncludeCmdLine_Enabled");
            File.Delete(viewerXml);
        }

        /// <summary>
        /// Set Windows PowerShell modules logging state.
        /// </summary>
        /// <param name="enable">PowerShell modules logging state.</param>
        public static void PowerShellModulesLogging(bool enable)
        {
            var loggingPath = "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging";
            var namesPath = $"{loggingPath}\\ModuleNames";

            if (enable)
            {
                Registry.LocalMachine.OpenOrCreateSubKey(namesPath).SetValue("*", "*");
                Registry.LocalMachine.OpenSubKey(loggingPath, true)?.SetValue("EnableModuleLogging", 1, RegistryValueKind.DWord);
                // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
                GroupPolicyService.ClearPolicyCache(scope: LGPOScope.Computer, path: loggingPath, name: "EnableModuleLogging", type: "DWORD", value: "1");
                GroupPolicyService.ClearPolicyCache(scope: LGPOScope.Computer, path: namesPath, name: "*", type: "SZ", value: "*");
                return;
            }

            Registry.LocalMachine.OpenSubKey(loggingPath, true)?.DeleteValue("EnableModuleLogging", false);
            Registry.LocalMachine.OpenSubKey(namesPath, true)?.DeleteValue("*", false);
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, loggingPath, "EnableModuleLogging");
        }

        /// <summary>
        /// Set Windows PowerShell scripts logging state.
        /// </summary>
        /// <param name="enable">PowerShell scripts logging state.</param>
        public static void PowerShellScriptsLogging(bool enable)
        {
            if (enable)
            {
                Registry.LocalMachine.OpenOrCreateSubKey("Software\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging").SetValue("EnableScriptBlockLogging", 1, RegistryValueKind.DWord);
                GroupPolicyService.ClearPolicyCache(scope: LGPOScope.Computer, path: "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging", name: "EnableScriptBlockLogging", type: "DWORD", value: "1");
                return;
            }

            Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging", true)?.DeleteValue("EnableScriptBlockLogging", false);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging", "EnableScriptBlockLogging");
        }

        /// <summary>
        /// Set Windows SmartScreen state.
        /// </summary>
        /// <param name="enable">Windows SmartScreen state.</param>
        public static void AppsSmartScreen(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)?.SetValue("SmartScreenEnabled", $"{(enable ? "Warn" : "Off")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Windows save zone state.
        /// </summary>
        /// <param name="enable">Windows save zone state.</param>
        public static void SaveZoneInformation(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments", "SaveZoneInformation");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments", "SaveZoneInformation");

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments", true)?.DeleteValue("SaveZoneInformation", false);
                // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
                GroupPolicyService.ClearPolicyCache(scope: LGPOScope.User, path: "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments", name: "SaveZoneInformation", type: "DWORD", value: "1");

                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments").SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.User, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments", "SaveZoneInformation");
        }

        /// <summary>
        /// Set Windows Sandbox state.
        /// </summary>
        /// <param name="enable">Windows Sandbox state.</param>
        public static void WindowsSandbox(bool enable)
        {
            var enableCommand = "Enable-WindowsOptionalFeature -FeatureName Containers-DisposableClientVM -All -Online -NoRestart";
            var disableCommand = "Disable-WindowsOptionalFeature -FeatureName Containers-DisposableClientVM -All -Online -NoRestart";
            _ = PowerShellService.Invoke($"{(enable ? enableCommand : disableCommand)}");
        }

        /// <summary>
        /// Set Local Security Authority state.
        /// </summary>
        /// <param name="enable">ocal Security Authority state.</param>
        public static void LocalSecurityAuthority(bool enable)
        {
            GroupPolicyService.DeleteRegistryValue(Registry.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "RunAsPPL");
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(LGPOScope.Computer, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "RunAsPPL");

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Lsa", true)?.SetValue("RunAsPPL", 2, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Lsa", true)?.SetValue("RunAsPPLBoot", 2, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Lsa", true)?.DeleteValue("RunAsPPL", false);
            Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Lsa", true)?.DeleteValue("RunAsPPLBoot", false);
        }

        /// <summary>
        /// Set "Extract all" item in the Windows Installer (.msi) context menu state.
        /// </summary>
        /// <param name="enable">"Extract all" item state.</param>
        public static void MSIExtractContext(bool enable)
        {
            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey("Msi.Package\\shell\\Extract\\Command").SetValue(string.Empty, "msiexec.exe /a \"%1\" /qb TARGETDIR=\"%1 extracted\"", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey("Msi.Package\\shell\\Extract", true)?.SetValue("MUIVerb", "@shell32.dll,-37514", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey("Msi.Package\\shell\\Extract", true)?.SetValue("Icon", "shell32.dll,-16817", RegistryValueKind.String);

                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree("Msi.Package\\shell\\Extract", false);
        }

        /// <summary>
        /// Set "Install" item in the Cabinet archives (.cab) context menu state.
        /// </summary>
        /// <param name="enable">"Install" item state.</param>
        public static void CABInstallContext(bool enable)
        {
            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey("CABFolder\\Shell\\runas\\Command")
                    .SetValue(string.Empty, "cmd /c DISM.exe /Online /Add-Package /PackagePath:\"%1\" /NoRestart & pause", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey("CABFolder\\Shell\\runas", true)?.SetValue("MUIVerb", "@shell32.dll,-10210", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey("CABFolder\\Shell\\runas", true)?.SetValue("HasLUAShield", string.Empty, RegistryValueKind.String);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree("CABFolder\\Shell\\runas", false);
        }

        /// <summary>
        /// Set "Cast to Device" item in the media files and folders context menu state.
        /// </summary>
        /// <param name="enable">"Cast to Device" item state.</param>
        public static void CastToDeviceContext(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)?.DeleteValue("{7AD84985-87B4-4a16-BE58-8B72A5B390F7}", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)?.DeleteValue("{7AD84985-87B4-4a16-BE58-8B72A5B390F7}", false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")
                .SetValue("{7AD84985-87B4-4a16-BE58-8B72A5B390F7}", string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Share" context menu item state.
        /// </summary>
        /// <param name="enable">"Share" item state.</param>
        public static void ShareContext(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                ?.DeleteValue("{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                    ?.DeleteValue("{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}", false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")
                .SetValue("{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}", string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Clipchamp" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Clipchamp" item state.</param>
        public static void EditWithClipchampContext(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                ?.DeleteValue("{8AB635F8-9A67-4698-AB99-784AD929F3B4}", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                    ?.DeleteValue("{8AB635F8-9A67-4698-AB99-784AD929F3B4}", false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")
                .SetValue("{8AB635F8-9A67-4698-AB99-784AD929F3B4}", string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Photos" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Photos" item state.</param>
        public static void EditWithPhotosContext(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                ?.DeleteValue("{BFE0E2A4-C70C-4AD7-AC3D-10D1ECEBB5B4}", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                    ?.DeleteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")
                .SetValue("{BFE0E2A4-C70C-4AD7-AC3D-10D1ECEBB5B4}", string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Paint Context" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Paint Context" item state.</param>
        public static void EditWithPaintContext(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                ?.DeleteValue("{2430F218-B743-4FD6-97BF-5C76541B4AE9}", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                    ?.DeleteValue("{2430F218-B743-4FD6-97BF-5C76541B4AE9}", false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")
                .SetValue("{2430F218-B743-4FD6-97BF-5C76541B4AE9}", string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit with Paint 3D" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit with Paint 3D" item state.</param>
        public static void EditWithPaint3DContext(bool enable)
        {
            new List<string>()
            {
                ".bmp", ".gif", ".jpe", ".jpeg", ".jpg", ".png", ".tif", ".tiff",
            }
            .ForEach(file =>
            {
                var filePath = $"SystemFileAssociations\\{file}\\Shell\\3D Edit";

                if (enable)
                {
                    Registry.ClassesRoot.OpenSubKey(filePath, true)?.DeleteValue("ProgrammaticAccessOnly", false);
                    return;
                }

                Registry.ClassesRoot.OpenSubKey(filePath, true)?.SetValue("ProgrammaticAccessOnly", string.Empty, RegistryValueKind.String);
            });
        }

        /// <summary>
        /// Set "Print" item in the .bat and .cmd files context menu state.
        /// </summary>
        /// <param name="enable">"Print" item state.</param>
        public static void PrintCMDContext(bool enable)
        {
            if (enable)
            {
                Registry.ClassesRoot.OpenSubKey("batfile\\shell\\print", true)?.DeleteValue("ProgrammaticAccessOnly", false);
                Registry.ClassesRoot.OpenSubKey("cmdfile\\shell\\print", true)?.DeleteValue("ProgrammaticAccessOnly", false);
                return;
            }

            Registry.ClassesRoot.OpenSubKey("batfile\\shell\\print", true)?.SetValue("ProgrammaticAccessOnly", string.Empty, RegistryValueKind.String);
            Registry.ClassesRoot.OpenSubKey("cmdfile\\shell\\print", true)?.SetValue("ProgrammaticAccessOnly", string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Include in Library" item in the folders and drives context menu state.
        /// </summary>
        /// <param name="enable">"Include in Library" item state.</param>
        public static void IncludeInLibraryContext(bool enable)
        {
            Registry.ClassesRoot.OpenSubKey("Folder\\ShellEx\\ContextMenuHandlers\\Library Location", true)
                ?.SetValue(string.Empty, enable ? "{3dad6c5d-2167-4cae-9914-f99e41c12cfa}" : "-{3dad6c5d-2167-4cae-9914-f99e41c12cfa}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Send to" item in the folders context menu state.
        /// </summary>
        /// <param name="enable">"Send to" item state.</param>
        public static void SendToContext(bool enable)
        {
            Registry.ClassesRoot.OpenSubKey("AllFilesystemObjects\\shellex\\ContextMenuHandlers\\SendTo", true)
                ?.SetValue(string.Empty, enable ? "{7BA4C740-9E81-11CF-99D3-00AA004AE837}" : "-{7BA4C740-9E81-11CF-99D3-00AA004AE837}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Bitmap image" item in the "New" context menu state.
        /// </summary>
        /// <param name="enable">"Bitmap image" item state.</param>
        public static void BitmapImageNewContext(bool enable)
        {
            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(".bmp\\ShellNew").SetValue("ItemName", "@%SystemRoot%\\System32\\mspaint.exe,-59414", RegistryValueKind.ExpandString);
                Registry.ClassesRoot.OpenSubKey(".bmp\\ShellNew", true)?.SetValue("NullFile", string.Empty, RegistryValueKind.String);

                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(".bmp\\ShellNew", false);
        }

        /// <summary>
        /// Set "Rich Text Document" item in the "New" context menu state.
        /// </summary>
        /// <param name="enable">"Rich Text Document" item state.</param>
        public static void RichTextDocumentNewContext(bool enable)
        {
            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(".rtf\\ShellNew").SetValue("Data", @"{\rtf1}", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey(".rtf\\ShellNew", true)?.SetValue("ItemName", "@%ProgramFiles%\\Windows NT\\Accessories\\WORDPAD.EXE,-213", RegistryValueKind.ExpandString);

                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(".rtf\\ShellNew", false);
        }

        /// <summary>
        /// Set "Compressed (zipped) Folder" item in the "New" context menu state.
        /// </summary>
        /// <param name="enable">"Compressed (zipped) Folder" item state.</param>
        public static void CompressedFolderNewContext(bool enable)
        {
            var zipContext = new byte[] { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(".zip\\CompressedFolder\\ShellNew").SetValue("Data", zipContext, RegistryValueKind.Binary);
                Registry.ClassesRoot.OpenSubKey(".zip\\CompressedFolder\\ShellNew", true)?.SetValue("ItemName", "@%SystemRoot%\\System32\\zipfldr.dll,-10194", RegistryValueKind.ExpandString);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(".zip\\CompressedFolder\\ShellNew", false);
        }

        /// <summary>
        /// Set "Open", "Print", and "Edit" context menu items available when selecting more than 15 files state.
        /// </summary>
        /// <param name="enable">"Open", "Print", and "Edit" context menu items state.</param>
        public static void MultipleInvokeContext(bool enable)
        {
            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)
                    ?.SetValue("MultipleInvokePromptMinimum", 300, RegistryValueKind.DWord);

                return;
            }

            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)?.DeleteValue("MultipleInvokePromptMinimum", false);
        }

        /// <summary>
        /// Set "Look for an app in the Microsoft Store" items in the "Open with" dialog state.
        /// </summary>
        /// <param name="enable">"Look for an app in the Microsoft Store" items state.</param>
        public static void UseStoreOpenWith(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer", true)?.DeleteValue("NoUseStoreOpenWith", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer", true)?.DeleteValue("NoUseStoreOpenWith", false);
                // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
                GroupPolicyService.ClearPolicyCache(LGPOScope.User, "Software\\Policies\\Microsoft\\Windows\\Explorer", "NoUseStoreOpenWith");

                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer").SetValue("NoUseStoreOpenWith", 1, RegistryValueKind.DWord);
            // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
            GroupPolicyService.ClearPolicyCache(scope: LGPOScope.User, path: "Software\\Policies\\Microsoft\\Windows\\Explorer", name: "NoUseStoreOpenWith", type: "DWORD", value: "1");
        }

        /// <summary>
        /// Set "Open in Windows Terminal" item in the folders context menu state.
        /// </summary>
        /// <param name="enable">"Open in Windows Terminal" item state.</param>
        public static void OpenWindowsTerminalContext(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)?.DeleteValue("{9F156763-7844-4DC4-B2B1-901F640F5155}", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", true)
                    ?.DeleteValue("{9F156763-7844-4DC4-B2B1-901F640F5155}", false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")
                .SetValue("{9F156763-7844-4DC4-B2B1-901F640F5155}", string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set Open Windows Terminal from context menu as administrator by default state.
        /// </summary>
        /// <param name="enable">"Open in Windows Terminal as Administrator" item state.</param>
        public static void OpenWindowsTerminalAdminContext(bool enable)
        {
            try
            {
                var terminalSettings = $"{Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%")}\\Packages\\Microsoft.WindowsTerminal_8wekyb3d8bbwe\\LocalState\\settings.json";
                var deserializeSettings = JsonConvert.DeserializeObject(File.ReadAllText(terminalSettings, Encoding.UTF8)) as JObject;
                var elevateSetting = deserializeSettings?.SelectToken("profiles.defaults.elevate");

                if (elevateSetting is null)
                {
                    var defaultsSetting = deserializeSettings!.SelectToken("profiles.defaults") as JObject;
                    defaultsSetting!.Add(new JProperty("elevate", string.Empty));
                    elevateSetting = deserializeSettings!.SelectToken("profiles.defaults.elevate");
                }

                elevateSetting!.Replace(enable);
                File.WriteAllText(terminalSettings, deserializeSettings!.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed write data to terminal configuration file", ex);
            }
        }

        /// <summary>
        /// Set images edit from context menu state.
        /// </summary>
        /// <param name="enable">Images edit from context menu state.</param>
        public static void ImagesEditContext(bool enable)
        {
            if (enable)
            {
                Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\image\\shell\\edit", true)?.DeleteValue("ProgrammaticAccessOnly", false);
                return;
            }

            Registry.ClassesRoot.OpenOrCreateSubKey("SystemFileAssociations\\image\\shell\\edit").SetValue("ProgrammaticAccessOnly", string.Empty, RegistryValueKind.String);
        }

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    }
}
