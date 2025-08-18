// <copyright file="Mutators.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Customizations
{
    using Microsoft.Win32;
    using Microsoft.Win32.TaskScheduler;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using System;
    using System.Collections.Generic;
    using System.ServiceProcess;
    using System.Text;

    /// <summary>
    /// Set the OS settings.
    /// </summary>
    public static class Mutators
    {
        private static readonly IAppNotificationService AppNotificationService = App.GetService<IAppNotificationService>();
        private static readonly IAppxPackagesService AppxPackagesService = App.GetService<IAppxPackagesService>();
        private static readonly ICommonDataService CommonDataService = App.GetService<ICommonDataService>();
        private static readonly ICursorsService CursorsService = App.GetService<ICursorsService>();
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
            var diagTrackService = new ServiceController("DiagTrack");
            var firewallRule = FirewallService.GetGroupRules("DiagTrack").First();

            if (enable)
            {
                OsService.SetServiceStartMode(diagTrackService, ServiceStartMode.Automatic);
                diagTrackService.TryStart();
                firewallRule.Enabled = true;
                firewallRule.Action = NetFwTypeLib.NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                return;
            }

            diagTrackService.TryStop();
            OsService.SetServiceStartMode(diagTrackService, ServiceStartMode.Disabled);
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
                var osEdition = CommonDataService.OsProperties.Edition;
                var isEnterpriseOrEducation = osEdition.Contains("Enterprise") || osEdition.Contains("Education");
                Registry.LocalMachine.OpenOrCreateSubKey("Software\\Policies\\Microsoft\\Windows\\DataCollection")
                    .SetValue("AllowTelemetry", isEnterpriseOrEducation ? 0 : 1, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection")
                    .SetValue("MaxTelemetryAllowed", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack")
                    .SetValue("ShowedToastAtLevel", 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection")
                .SetValue("MaxTelemetryAllowed", 3, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenOrCreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack")
                .SetValue("ShowedToastAtLevel", 3, RegistryValueKind.DWord);
            Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\DataCollection", true)
                ?.DeleteValue("AllowTelemetry", false);
        }

        /// <summary>
        /// Set Windows feature "Error reporting" state.
        /// </summary>
        /// <param name="enable">Feature state.</param>
        public static void ErrorReporting(bool enable)
        {
            var policyReportingPath = "Software\\Policies\\Microsoft\\Windows\\Windows Error Reporting";
            var errorReportingPath = "Software\\Microsoft\\Windows\\Windows Error Reporting";
            var reportingTask = ScheduledTaskService.GetTaskOrDefault("Microsoft\\Windows\\Windows Error Reporting\\QueueReporting");
            using var werService = new ServiceController("WerSvc");
            GroupPolicyService.ClearPolicyCache(policyReportingPath, "Disabled", Registry.LocalMachine, Registry.CurrentUser);
            ScheduledTaskService.SetState(reportingTask, enable);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(errorReportingPath, true)?.DeleteValue("Disabled", false);
                OsService.SetServiceStartMode(werService, ServiceStartMode.Manual);
                werService.TryStart();
                return;
            }

            Registry.CurrentUser.OpenSubKey(errorReportingPath, true)?.SetValue("Disabled", 1, RegistryValueKind.DWord);
            OsService.SetServiceStartMode(werService, ServiceStartMode.Disabled);
            werService.TryStop();
        }

        /// <summary>
        /// Set Windows feature "Feedback frequency" state.
        /// </summary>
        /// <param name="state">Feedback frequency state.</param>
        public static void FeedbackFrequency(int state)
        {
            var rulesPath = "Software\\Microsoft\\Siuf\\Rules";
            var policyCollectionPath = "Software\\Policies\\Microsoft\\Windows\\DataCollection";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyCollectionPath, "DoNotShowFeedbackNotifications");

            if (state.Equals(2))
            {
                Registry.CurrentUser.OpenOrCreateSubKey(rulesPath).SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.DeleteSubKey(rulesPath, false);
        }

        /// <summary>
        /// Set telemetry scheduled tasks state.
        /// </summary>
        /// <param name="enable">Scheduled tasks state.</param>
        public static void ScheduledTasks(bool enable)
        {
            new List<Task?>()
             {
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\MareBackup"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\StartupAppTask"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Application Experience\\ProgramDataUpdater"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Autochk\\Proxy"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Maps\\MapsToastTask"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Maps\\MapsUpdateTask"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\Windows\\Shell\\FamilySafetyRefreshTask"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\XblGameSave\\XblGameSaveTask"),
                ScheduledTaskService.GetTaskOrDefault("\\Microsoft\\XblGameSave\\XblGameSaveTask1"),
             }
            .ForEach(task => ScheduledTaskService.SetState(task, enable));
        }

        /// <summary>
        /// Set Windows feature "Sign-in info" state.
        /// </summary>
        /// <param name="enable">Sign-in info state.</param>
        public static void SigninInfo(bool enable)
        {
            var policySystemPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            var userSid = InstrumentationService.GetUserSid(Environment.UserName);
            var userArsoPath = $"Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\\UserARSO\\{userSid}";
            var optOut = "OptOut";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySystemPath, "DisableAutomaticRestartSignOn");

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(userArsoPath, true)?.DeleteValue(optOut, false);
                return;
            }

            Registry.LocalMachine.OpenOrCreateSubKey(userArsoPath).SetValue(optOut, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set language list access state.
        /// </summary>
        /// <param name="enable">Language list state.</param>
        public static void LanguageListAccess(bool enable)
        {
            var userProfilePath = "Control Panel\\International\\User Profile";
            var httpOptOut = "HttpAcceptLanguageOptOut";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(userProfilePath, true)?.DeleteValue(httpOptOut, false);
                return;
            }

            Registry.CurrentUser.OpenSubKey(userProfilePath, true)?.SetValue(httpOptOut, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the permission for apps to use advertising ID state.
        /// </summary>
        /// <param name="enable">Advertising ID state.</param>
        public static void AdvertisingID(bool enable)
        {
            var advertisingPath = "Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo";
            var advertisingPolicyPath = "Software\\Policies\\Microsoft\\Windows\\AdvertisingInfo";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, advertisingPolicyPath, "DisabledByGroupPolicy");
            Registry.CurrentUser.OpenOrCreateSubKey(advertisingPath).SetValue("enable", enable ? 1 : 0, RegistryValueKind.DWord);
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
            var contentDeliveryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager";
            var policyCloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyCloudPath, "DisableSoftLanding");
            Registry.CurrentUser.OpenSubKey(contentDeliveryPath, true)?.SetValue("SubscribedContent-338389enable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the suggested content in the Settings app state.
        /// </summary>
        /// <param name="enable">Suggested content state.</param>
        public static void SettingsSuggestedContent(bool enable)
        {
            new List<string> { "SubscribedContent-353694enable", "SubscribedContent-353696enable", "SubscribedContent-338393enable" }
            .ForEach(content => Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", true)
                ?.SetValue(content, enable ? 1 : 0, RegistryValueKind.DWord));
        }

        /// <summary>
        /// Set the automatic installing suggested apps state.
        /// </summary>
        /// <param name="enable">Suggested apps state.</param>
        public static void AppsSilentInstalling(bool enable)
        {
            var cloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            var contentPath = "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, cloudPath, "DisableWindowsConsumerFeatures");
            Registry.CurrentUser.OpenSubKey(contentPath, true)?.SetValue("SilentInstalledAppsenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the Windows feature "Whats New" state.
        /// </summary>
        /// <param name="enable">Whats New state.</param>
        public static void WhatsNewInWindows(bool enable)
        {
            var profilePath = "Software\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement";
            Registry.CurrentUser.OpenOrCreateSubKey(profilePath).SetValue("ScoobeSystemSettingenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows feature "Tailored experiences" state.
        /// </summary>
        /// <param name="enable">Tailored experiences state.</param>
        public static void TailoredExperiences(bool enable)
        {
            var policyCloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            var privacyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Privacy";
            GroupPolicyService.ClearPolicyCache(Registry.CurrentUser, policyCloudPath, "DisableTailoredExperiencesWithDiagnosticData");
            Registry.CurrentUser.OpenSubKey(privacyPath, true)?.SetValue("TailoredExperiencesWithDiagnosticDataenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows feature "Bing search" state.
        /// </summary>
        /// <param name="enable">Bing search state.</param>
        public static void BingSearch(bool enable)
        {
            var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var disableSuggestions = "DisableSearchBoxSuggestions";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(explorerPath, true)?.DeleteValue(disableSuggestions, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(explorerPath).SetValue(disableSuggestions, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu recommendations state.
        /// </summary>
        /// <param name="enable">Start menu recommendations state.</param>
        public static void StartRecommendationsTips(bool enable)
        {
            var irisPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var startIris = "Start_IrisRecommendations";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(irisPath, true)?.DeleteValue(irisPath, false);
                return;
            }

            Registry.CurrentUser.OpenSubKey(irisPath, true)?.SetValue(startIris, 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start Menu notifications state.
        /// </summary>
        /// <param name="enable">Start Menu notifications state.</param>
        public static void StartAccountNotifications(bool enable)
        {
            var notificationsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var startNotifications = "Start_AccountNotifications";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(notificationsPath, true)?.DeleteValue(startNotifications, false);
                return;
            }

            Registry.CurrentUser.OpenSubKey(notificationsPath, true)?.SetValue(startNotifications, 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the "This PC" icon on Desktop state.
        /// </summary>
        /// <param name="enable">"This PC" icon state.</param>
        public static void ThisPC(bool enable)
        {
            var pcPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel";
            var pcGuid = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

            if (enable)
            {
                Registry.CurrentUser.OpenOrCreateSubKey(pcPath).SetValue(pcGuid, 0, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(pcPath, true)?.DeleteValue(pcGuid, false);
        }

        /// <summary>
        /// Set item check boxes state.
        /// </summary>
        /// <param name="enable">Item check boxes state.</param>
        public static void CheckBoxes(bool enable)
        {
            var boxesPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(boxesPath, true)?.SetValue("AutoCheckSelect", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set hidden files, folders, and drives state.
        /// </summary>
        /// <param name="enable">Hidden items state.</param>
        public static void HiddenItems(bool enable)
        {
            var itemsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(itemsPath, true)?.SetValue("Hidden", enable ? 1 : 2, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set file name extensions visibility state.
        /// </summary>
        /// <param name="enable">File extensions visibility state.</param>
        public static void FileExtensions(bool enable)
        {
            var extensionsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(extensionsPath, true)?.SetValue("HideFileExt", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set folder merge conflicts state.
        /// </summary>
        /// <param name="enable">Folder merge conflicts state.</param>
        public static void MergeConflicts(bool enable)
        {
            var mergePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(mergePath, true)?.SetValue("HideMergeConflicts", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set how to open File Explorer.
        /// </summary>
        /// <param name="state">File Explorer open state.</param>
        public static void OpenFileExplorerTo(int state)
        {
            var filePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(filePath, true)?.SetValue("LaunchTo", state, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer ribbon state.
        /// </summary>
        /// <param name="state">File Explorer ribbon state.</param>
        public static void FileExplorerRibbon(int state)
        {
            var policyExplorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var explorerRibbonPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Ribbon";
            GroupPolicyService.ClearPolicyCache(policyExplorerPath, "ExplorerRibbonStartsMinimized", Registry.LocalMachine, Registry.CurrentUser);
            Registry.CurrentUser.OpenOrCreateSubKey(explorerRibbonPath).SetValue("MinimizedStateTabletModeOff", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer compact mode state.
        /// </summary>
        /// <param name="enable">File Explorer compact mode state.</param>
        public static void FileExplorerCompactMode(bool enable)
        {
            var compactPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(compactPath, true)?.SetValue("UseCompactMode", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer provider notification visibility state.
        /// </summary>
        /// <param name="enable">File Explorer provider notification visibility state.</param>
        public static void OneDriveFileExplorerAd(bool enable)
        {
            var drivePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(drivePath, true)?.SetValue("ShowSyncProviderNotifications", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set snap a window state.
        /// </summary>
        /// <param name="enable">Snap Assist state.</param>
        public static void SnapAssist(bool enable)
        {
            var desktopPath = "Control Panel\\Desktop";
            var snapPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(desktopPath, true)?.SetValue("WindowArrangementActive", "1", RegistryValueKind.String);
            Registry.CurrentUser.OpenSubKey(snapPath, true)?.SetValue("SnapAssist", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set file transfer dialog box mode.
        /// </summary>
        /// <param name="state">File transfer dialog box state.</param>
        public static void FileTransferDialog(int state)
        {
            var statusPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager";
            Registry.CurrentUser.OpenOrCreateSubKey(statusPath).SetValue("EnthusiastMode", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set recycle bin confirmation dialog state.
        /// </summary>
        /// <param name="enable">Recycle bin dialog state.</param>
        public static void RecycleBinDeleteConfirmation(bool enable)
        {
            var policyExplorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            var shellStateValue = "ShellState";
            GroupPolicyService.ClearPolicyCache(policyExplorerPath, "ConfirmFileDelete", Registry.LocalMachine, Registry.CurrentUser);
            var confirmation = Registry.CurrentUser.OpenSubKey(shellPath)?.GetValue(shellStateValue) as byte[] ?? new byte[5];
            confirmation[4] = enable ? (byte)51 : (byte)55;
            Registry.CurrentUser.OpenSubKey(shellPath, true)?.SetValue(shellStateValue, confirmation, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set recently used Quick access files state.
        /// </summary>
        /// <param name="enable">Quick access files state.</param>
        public static void QuickAccessRecentFiles(bool enable)
        {
            var explorerMachinePath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var explorerUserPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var explorerSoftwarePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, explorerMachinePath, "NoRecentDocsHistory");
            GroupPolicyService.ClearPolicyCache(Registry.CurrentUser, explorerUserPath, "NoRecentDocsHistory");
            Registry.CurrentUser.OpenSubKey(explorerSoftwarePath, true)?.SetValue("ShowRecent", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set frequently used Quick access folders state.
        /// </summary>
        /// <param name="enable">Quick access folders state.</param>
        public static void QuickAccessFrequentFolders(bool enable)
        {
            var frequentPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            Registry.CurrentUser.OpenSubKey(frequentPath, true)?.SetValue("ShowFrequent", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar alignment state.
        /// </summary>
        /// <param name="state">Taskbar alignment state.</param>
        public static void TaskbarAlignment(int state)
        {
            var alignmentPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(alignmentPath, true)?.SetValue("TaskbarAl", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar widgets icon state.
        /// </summary>
        /// <param name="enable">Taskbar widgets icon state.</param>
        public static void TaskbarWidgets(bool enable)
        {
            var allowNewsPath = "Software\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests";
            var msDshPath = "Software\\Policies\\Microsoft\\Dsh";
            var explorerAdvancedPath = "HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, allowNewsPath, "value");
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, msDshPath, "AllowNewsAndInterests");
            var command = $"-Command \"& {{New-ItemProperty -Path {explorerAdvancedPath} -Name TaskbarDa -PropertyType DWord -Value {(enable ? 1 : 0)} -Force}}\"";
            PowerShellService.InvokeCommandBypassUCPD(command);
        }

        /// <summary>
        /// Set Search on the taskbar state.
        /// </summary>
        /// <param name="state">Taskbar search state.</param>
        public static void TaskbarSearchWindows10(int state)
        {
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Search";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySearchPath, "DisableSearch", "SearchOnTaskbarMode");
            Registry.CurrentUser.OpenSubKey(searchPath, true)?.SetValue("SearchboxTaskbarMode", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Search on the taskbar state.
        /// </summary>
        /// <param name="state">Taskbar search state.</param>
        public static void TaskbarSearchWindows11(int state)
        {
            var policyDisablePath = "Software\\Microsoft\\PolicyManager\\default\\Search\\DisableSearch";
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Search";
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policyDisablePath, "value", 0, RegistryValueKind.DWord);
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySearchPath, "DisableSearch", "SearchOnTaskbarMode");
            var searchMode = state switch
            {
                3 => 3,
                4 => 2,
                _ => state - 1,
            };
            Registry.CurrentUser.OpenSubKey(searchPath, true)?.SetValue("SearchboxTaskbarMode", searchMode, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set search highlights state.
        /// </summary>
        /// <param name="enable">Search highlights state.</param>
        public static void SearchHighlightsWindows10(bool enable)
        {
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var feedsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Feeds\\DSB";
            var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySearchPath, "EnableDynamicContentInWSB");
            Registry.CurrentUser.OpenSubKey(feedsPath, true)?.SetValue("ShowDynamicContent", enable ? 1 : 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey(searchPath, true)?.SetValue("IsDynamicSearchBoxenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set search highlights state.
        /// </summary>
        /// <param name="enable">Search highlights state.</param>
        public static void SearchHighlightsWindows11(bool enable)
        {
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var searchSettingsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySearchPath, "EnableDynamicContentInWSB");

            if (enable)
            {
                var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Search";
                var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
                Registry.CurrentUser.OpenSubKey(searchPath, true)?.DeleteValue("BingSearchenable", false);
                Registry.CurrentUser.OpenSubKey(explorerPath, true)?.DeleteValue("DisableSearchBoxSuggestions", false);
            }

            Registry.CurrentUser.OpenSubKey(searchSettingsPath, true)?.SetValue("IsDynamicSearchBoxenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Cortana button taskbar state.
        /// </summary>
        /// <param name="enable">Cortana button state.</param>
        public static void CortanaButton(bool enable)
        {
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var explorerAdvancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySearchPath, "AllowCortana");
            Registry.CurrentUser.OpenSubKey(explorerAdvancedPath, true)?.SetValue("ShowCortanaButton", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar task view button state.
        /// </summary>
        /// <param name="enable">Taskbar task view button state.</param>
        public static void TaskViewButton(bool enable)
        {
            var explorerAdvancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";

            if (CommonDataService.IsWindows11)
            {
                var policyExplorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
                GroupPolicyService.ClearPolicyCache(policyExplorerPath, "HideTaskViewButton", Registry.CurrentUser, Registry.LocalMachine);
            }

            Registry.CurrentUser.OpenSubKey(explorerAdvancedPath, true)?.SetValue("ShowTaskViewButton", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set News and Interests state.
        /// </summary>
        /// <param name="enable">News and Interests state.</param>
        public static void NewsInterests(bool enable)
        {
            var policyFeedsPath = "Software\\Policies\\Microsoft\\Windows\\Windows Feeds";
            var policyNewsPath = "Software\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyFeedsPath, "EnableFeeds");
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyNewsPath, "value");
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
            var policyExplorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var explorerPeoplePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People";
            GroupPolicyService.ClearPolicyCache(policyExplorerPath, "HidePeopleBar", Registry.CurrentUser, Registry.LocalMachine);
            Registry.CurrentUser.OpenOrCreateSubKey(explorerPeoplePath)?.SetValue("PeopleBand", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Meet Now icon state.
        /// </summary>
        /// <param name="enable">Meet Now icon state.</param>
        public static void MeetNow(bool enable)
        {
            var policyExplorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var explorerSettingsValue = "Settings";
            var explorerStuckPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3";
            GroupPolicyService.ClearPolicyCache(policyExplorerPath, "HideSCAMeetNow", Registry.CurrentUser, Registry.LocalMachine);
            var settings = Registry.CurrentUser.OpenSubKey(explorerStuckPath)?.GetValue(explorerSettingsValue) as byte[] ?? new byte[10];
            settings[9] = enable ? (byte)0 : (byte)128;
            Registry.CurrentUser.OpenSubKey(explorerStuckPath, true)?.SetValue(explorerSettingsValue, settings, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set Windows Ink Workspace button state.
        /// </summary>
        /// <param name="enable">Windows Ink Workspace button state.</param>
        public static void WindowsInkWorkspace(bool enable)
        {
            var policyWorkspacePath = "Software\\Policies\\Microsoft\\WindowsInkWorkspace";
            var penWorkspacePath = "Software\\Microsoft\\Windows\\CurrentVersion\\PenWorkspace";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyWorkspacePath, "AllowWindowsInkWorkspace");
            Registry.CurrentUser.OpenSubKey(penWorkspacePath, true)?.SetValue("PenWorkspaceButtonDesiredVisibility", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set notification area icons state.
        /// </summary>
        /// <param name="enable">Notification area icons state.</param>
        public static void NotificationAreaIcons(bool enable)
        {
            var policyExplorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var explorerTrayPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            GroupPolicyService.ClearPolicyCache(policyExplorerPath, "NoAutoTrayNotify", Registry.CurrentUser, Registry.LocalMachine);
            Registry.CurrentUser.OpenSubKey(explorerTrayPath, true)?.SetValue("EnableAutoTray", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set seconds on the taskbar clock state.
        /// </summary>
        /// <param name="enable">Seconds on the taskbar clock state.</param>
        public static void SecondsInSystemClock(bool enable)
        {
            var clockPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(clockPath, true)?.SetValue("ShowSecondsInSystemClock", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar combine state.
        /// </summary>
        /// <param name="state">Taskbar combine state.</param>
        public static void TaskbarCombine(int state)
        {
            var policyExplorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var advancedTaskbarPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            GroupPolicyService.ClearPolicyCache(policyExplorerPath, "NoTaskGrouping", Registry.LocalMachine, Registry.CurrentUser);
            Registry.CurrentUser.OpenSubKey(advancedTaskbarPath, true)?.SetValue("TaskbarGlomLevel", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set end task in taskbar by click state.
        /// </summary>
        /// <param name="enable">Taskbar end task state.</param>
        public static void TaskbarEndTask(bool enable)
        {
            var taskbarPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings";
            var taskbarTask = "TaskbarEndTask";

            if (enable)
            {
                Registry.CurrentUser.OpenOrCreateSubKey(taskbarPath).SetValue(taskbarTask, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(taskbarPath, true)?.DeleteValue(taskbarTask, false);
        }

        /// <summary>
        /// Set Control Panel icons view state.
        /// </summary>
        /// <param name="state">Control Panel icons view state.</param>
        public static void ControlPanelView(int state)
        {
            var policyExplorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var controlPanelPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel";
            GroupPolicyService.ClearPolicyCache(Registry.CurrentUser, policyExplorerPath, "ForceClassicControlPanel");

            switch (state)
            {
                case 1:
                    Registry.CurrentUser.OpenOrCreateSubKey(controlPanelPath).SetValue("AllItemsIconView", 0, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey(controlPanelPath, true)?.SetValue("StartupPage", 0, RegistryValueKind.DWord);
                    break;
                case 2:
                    Registry.CurrentUser.OpenOrCreateSubKey(controlPanelPath).SetValue("AllItemsIconView", 0, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey(controlPanelPath, true)?.SetValue("StartupPage", 1, RegistryValueKind.DWord);
                    break;
                default:
                    Registry.CurrentUser.OpenOrCreateSubKey(controlPanelPath).SetValue("AllItemsIconView", 1, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey(controlPanelPath, true)?.SetValue("StartupPage", 1, RegistryValueKind.DWord);
                    break;
            }
        }

        /// <summary>
        /// Set Windows color mode state.
        /// </summary>
        /// <param name="state">Windows color mode state.</param>
        public static void WindowsColorMode(int state)
        {
            var personalizePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
            Registry.CurrentUser.OpenSubKey(personalizePath, true)?.SetValue("SystemUsesLightTheme", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set apps color mode state.
        /// </summary>
        /// <param name="state">Apps color mode state.</param>
        public static void AppColorMode(int state)
        {
            var personalizePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
            Registry.CurrentUser.OpenSubKey(personalizePath, true)?.SetValue("AppsUseLightTheme", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set "New App Installed" indicator state.
        /// </summary>
        /// <param name="enable">New App Installed" indicator state.</param>
        public static void NewAppInstalledNotification(bool enable)
        {
            var alertPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var appAlert = "NoNewAppAlert";

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(alertPath, true)?.DeleteValue(appAlert, false);
                return;
            }

            Registry.LocalMachine.OpenOrCreateSubKey(alertPath).SetValue(appAlert, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set first sign-in animation state.
        /// </summary>
        /// <param name="enable">First sign-in animation state.</param>
        public static void FirstLogonAnimation(bool enable)
        {
            var policySystemPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            var logonPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";
            var firstLogonAnimation = "EnableFirstLogonAnimation";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySystemPath, firstLogonAnimation);
            Registry.LocalMachine.OpenSubKey(logonPath, true)?.SetValue(firstLogonAnimation, enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set JPEG wallpapers quality state.
        /// </summary>
        /// <param name="state">JPEG wallpapers quality state.</param>
        public static void JPEGWallpapersQuality(int state)
        {
            var desktopPath = "Control Panel\\Desktop";
            var jpegQuality = "JPEGImportQuality";

            if (state.Equals(1))
            {
                Registry.CurrentUser.OpenSubKey(desktopPath, true)?.SetValue(jpegQuality, 100, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(desktopPath, true)?.DeleteValue(jpegQuality, false);
        }

        /// <summary>
        /// Set "- Shortcut" suffix state.
        /// </summary>
        /// <param name="enable">"- Shortcut" suffix state.</param>
        public static void ShortcutsSuffix(bool enable)
        {
            var linkPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            var templatesPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates";
            var shortcutTemplate = "ShortcutNameTemplate";
            Registry.CurrentUser.OpenSubKey(linkPath, true)?.DeleteValue("link", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(templatesPath, true)?.DeleteValue(shortcutTemplate, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(templatesPath)?.SetValue(shortcutTemplate, "%s.lnk", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Print screen button state.
        /// </summary>
        /// <param name="enable">Print screen button state.</param>
        public static void PrtScnSnippingTool(bool enable)
        {
            var keyboardPath = "Control Panel\\Keyboard";
            Registry.CurrentUser.OpenSubKey(keyboardPath, true)?.SetValue("PrintScreenKeyForSnippingenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set input method for app window state.
        /// </summary>
        /// <param name="enable">Input method for app window state.</param>
        public static void AppsLanguageSwitch(bool enable)
        {
            var command = enable ? "Set-WinLanguageBarOption -UseLegacySwitchMode" : "Set-WinLanguageBarOption";
            _ = PowerShellService.Invoke(command);
        }

        /// <summary>
        /// Set Aero Shake state.
        /// </summary>
        /// <param name="enable">Aero Shake state.</param>
        public static void AeroShaking(bool enable)
        {
            var policyPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            GroupPolicyService.ClearPolicyCache(policyPath, "NoWindowMinimizingShortcuts", Registry.CurrentUser, Registry.LocalMachine);
            Registry.CurrentUser.OpenSubKey(explorerPath, true)?.SetValue("DisallowShaking", enable ? 0 : 1, RegistryValueKind.DWord);
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
                    CursorsService.SetJepriCreationsDarkCursors();
                    break;

                case 2:
                    CursorsService.SetJepriCreationsLightCursors();
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
            #pragma warning disable SA1003 // Symbols should be spaced correctly
            var folderPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FolderTypes\\{885a186e-a440-4ada-812b-db871b942259}";

            if (state.Equals(1))
            {
                PowerShellService.ClearCommonDialogViews();
                var groupPath = folderPath.Insert(folderPath.Length, "\\TopViews\\{00000000-0000-0000-0000-000000000000}");
                using var groupKey = Registry.CurrentUser.OpenOrCreateSubKey(groupPath);
                groupKey.SetValue("ColumnList", "System.Null", RegistryValueKind.String);
                groupKey.SetValue("GroupBy", "System.Null", RegistryValueKind.String);
                groupKey.SetValue("LogicalViewMode", 1, RegistryValueKind.DWord);
                groupKey.SetValue("Name", "NoName", RegistryValueKind.String);
                groupKey.SetValue("Order", 0, RegistryValueKind.DWord);
                groupKey.SetValue("PrimaryProperty", "System.ItemNameDisplay", RegistryValueKind.String);
                groupKey.SetValue("SortByList", "prop:System.ItemNameDisplay", RegistryValueKind.String);
                return;
            }

            Registry.CurrentUser.DeleteSubKeyTree(folderPath, false);
            #pragma warning restore SA1003 // Symbols should be spaced correctly
        }

        /// <summary>
        /// Set navigation pane expand state.
        /// </summary>
        /// <param name="enable">Navigation pane expand state.</param>
        public static void NavigationPaneExpand(bool enable)
        {
            var panePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(panePath, true)?.SetValue("NavPaneExpandToCurrentFolder", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu recently added apps state.
        /// </summary>
        /// <param name="enable">Start menu recently added apps state.</param>
        public static void RecentlyAddedApps(bool enable)
        {
            var addedAppsPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var addedAppsValue = "HideRecentlyAddedApps";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, addedAppsPath, addedAppsValue);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(addedAppsPath, true)?.DeleteValue(addedAppsValue, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(addedAppsPath).SetValue(addedAppsValue, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu app suggestions state.
        /// </summary>
        /// <param name="enable">Start menu app suggestions state.</param>
        public static void AppSuggestions(bool enable)
        {
            var policyCloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            var contentDeliveryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyCloudPath, "DisableWindowsConsumerFeatures");
            Registry.CurrentUser.OpenSubKey(contentDeliveryPath, true)?.SetValue("SubscribedContent-338388enable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu layout state.
        /// </summary>
        /// <param name="state">Start menu layout state.</param>
        public static void StartLayout(int state)
        {
            var layoutPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(layoutPath, true)?.SetValue("Start_Layout", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set recommended section state.
        /// </summary>
        /// <param name="enable">Recommended section state.</param>
        public static void StartRecommendedSection(bool enable)
        {
            var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var policyExplorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var policyEducationPath = "Software\\Microsoft\\PolicyManager\\current\\device\\Education";
            var hideRecommended = "HideRecommendedSection";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyExplorerPath, "HideRecommendedSection");
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyEducationPath, "IsEducationEnvironment");

            if (enable)
            {
                var startPath = "Software\\Microsoft\\PolicyManager\\Current\\Device\\Start";
                Registry.CurrentUser.OpenSubKey(explorerPath, true)?.DeleteValue(hideRecommended, false);
                Registry.LocalMachine.OpenSubKey(startPath, true)?.DeleteValue(hideRecommended, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(explorerPath).SetValue(hideRecommended, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set One Drive state.
        /// </summary>
        /// <param name="enable">One Drive state.</param>
        public static void OneDrive(bool enable)
        {
            var oneDrivePath = "Policies\\Microsoft\\Windows\\OneDrive";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, oneDrivePath, "DisableFileSyncNGSC");

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
            var sensePath = "Software\\Policies\\Microsoft\\Windows\\StorageSense";
            var storagePath = "Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, sensePath, "AllowStorageSenseGlobal");

            if (enable)
            {
                Registry.CurrentUser.OpenOrCreateSubKey(storagePath).SetValue("01", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenSubKey(storagePath, true)?.SetValue("04", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenSubKey(storagePath, true)?.SetValue("2048", 30, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(storagePath).SetValue("01", 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey(storagePath, true)?.SetValue("04", 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey(storagePath, true)?.SetValue("2048", 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set hibernation state.
        /// </summary>
        /// <param name="enable">Hibernation state.</param>
        public static void Hibernation(bool enable)
        {
            _ = ProcessService.WaitForExit(name: "POWERCFG.EXE", arguments: $"/HIBERNATE {(enable ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Set long path limit state.
        /// </summary>
        /// <param name="enable">Long path limit state.</param>
        public static void Win32LongPathLimit(bool enable)
        {
            var systemPath = "System\\CurrentControlSet\\Control\\FileSystem";
            Registry.LocalMachine.OpenSubKey(systemPath, true)?.SetValue("LongPathsenable", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set BSOD error state.
        /// </summary>
        /// <param name="enable">BSOD error state.</param>
        public static void BSoDStopError(bool enable)
        {
            var crashPath = "System\\CurrentControlSet\\Control\\CrashControl";
            Registry.LocalMachine.OpenSubKey(crashPath, true)?.SetValue("DisplayParameters", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set administrator approval mode state.
        /// </summary>
        /// <param name="enable">Administrator approval mode state.</param>
        public static void AdminApprovalMode(bool enable)
        {
            var policySystemPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            var systemPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "ConsentPromptBehaviorUser", 3, RegistryValueKind.DWord);
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "EnableInstallerDetection", 1, RegistryValueKind.DWord);
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "ValidateAdminCodeSignatures", 0, RegistryValueKind.DWord);
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "EnableSecureUIAPaths", 1, RegistryValueKind.DWord);
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "EnableLUA", 1, RegistryValueKind.DWord);
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "PromptOnSecureDesktop", 1, RegistryValueKind.DWord);
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "EnableVirtualization", 1, RegistryValueKind.DWord);
            GroupPolicyService.SetPolicyValue(Registry.LocalMachine, policySystemPath, "EnableUIADesktopToggle", 1, RegistryValueKind.DWord);
            Registry.LocalMachine.OpenSubKey(systemPath, true)?.SetValue("ConsentPromptBehaviorAdmin", enable ? 0 : 5, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set delivery optimization state.
        /// </summary>
        /// <param name="enable">Delivery optimization state.</param>
        public static void DeliveryOptimization(bool enable)
        {
            var deliveryPath = "Software\\Policies\\Microsoft\\Windows\\DeliveryOptimization";
            var settingsPath = "S-1-5-20\\Software\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Settings";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, deliveryPath, "DODownloadMode");
            Registry.Users.OpenSubKey(settingsPath, true)?.SetValue("DownloadMode", enable ? 1 : 0, RegistryValueKind.DWord);

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
            var windowsPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows";
            Registry.CurrentUser.OpenSubKey(windowsPath, true)?.SetValue("LegacyDefaultPrinterMode", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set update Microsoft products state.
        /// </summary>
        /// <param name="enable">Update Microsoft products state.</param>
        public static void UpdateMicrosoftProducts(bool enable)
        {
            var updatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, updatePath, "AllowMUUpdateService");

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
        /// <param name="state">Restart notification state.</param>
        public static void RestartNotification(int state)
        {
            var updatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            var settingsPath = "Software\\Microsoft\\WindowsUpdate\\UX\\Settings";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, updatePath, "SetAutoRestartNotificationDisable");
            Registry.LocalMachine.OpenSubKey(settingsPath, true)?.SetValue("RestartNotificationsAllowed2", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set restart device after update state.
        /// </summary>
        /// <param name="enable">Restart device after update state.</param>
        public static void RestartDeviceAfterUpdate(bool enable)
        {
            var updatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            var settingsPath = "Software\\Microsoft\\WindowsUpdate\\UX\\Settings";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, updatePath, "ActiveHoursStart", "ActiveHoursEnd", "SetActiveHours");
            Registry.LocalMachine.OpenSubKey(settingsPath, true)?.SetValue("IsExpedited", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set active hours restart state.
        /// </summary>
        /// <param name="state">Active hours restart state.</param>
        public static void ActiveHours(int state)
        {
            var autoUpdatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
            var windowsUpdatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            var settingsPath = "Software\\Microsoft\\WindowsUpdate\\UX\\Settings";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, autoUpdatePath, "NoAutoRebootWithLoggedOnUsers", "AlwaysAutoRebootAtScheduledTime");
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, windowsUpdatePath, "ActiveHoursStart", "ActiveHoursEnd", "SetActiveHours");
            Registry.LocalMachine.OpenSubKey(settingsPath, true)?.SetValue("SmartActiveHoursState", state.Equals(1) ? 1 : 2, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows latest update state.
        /// </summary>
        /// <param name="enable">Latest update state.</param>
        public static void WindowsLatestUpdate(bool enable)
        {
            var policyUpdatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            var settingsPath = "Software\\Microsoft\\WindowsUpdate\\UX\\Settings";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyUpdatePath, "AllowOptionalContent", "SetAllowOptionalContent");
            Registry.LocalMachine.OpenSubKey(settingsPath, true)?.SetValue("IsContinuousInnovationOptedIn", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set network adapters power state.
        /// </summary>
        /// <param name="enable">Network adapters power state.</param>
        public static void NetworkAdaptersSavePower(bool enable)
        {
            // Method intentionally left empty.
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

            var profilePath = "Control Panel\\International\\User Profile";
            Registry.CurrentUser.OpenSubKey(profilePath, true)?.DeleteValue("InputMethodOverride", false);
        }

        /// <summary>
        /// Set installed .NET state.
        /// </summary>
        /// <param name="enable">Installed .NET state.</param>
        public static void LatestInstalledNET(bool enable)
        {
            var useLatestClr = "OnlyUseLatestCLR";
            var clrPath = "Software\\Microsoft\\.NETFramework";
            var clrWowPath = "Software\\Wow6432Node\\Microsoft\\.NETFramework";

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(clrPath, true)?.SetValue(useLatestClr, 1, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(clrWowPath, true)?.SetValue(useLatestClr, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(clrPath, true)?.DeleteValue(useLatestClr, false);
            Registry.LocalMachine.OpenSubKey(clrWowPath, true)?.DeleteValue(useLatestClr, false);
        }

        // TODO: Set description
        public static void WinPrtScrFolder(bool enable)
        {
            // Method intentionally left empty.
        }

        /// <summary>
        /// Set recommended troubleshooting state.
        /// </summary>
        /// <param name="state">Recommended troubleshooting state.</param>
        public static void RecommendedTroubleshooting(int state)
        {
            var policyDataPath = "Software\\Policies\\Microsoft\\Windows\\DataCollection";
            var dataCollectionPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection";
            var diagTrackPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack";
            var errorReportingPath = "Software\\Microsoft\\Windows\\Windows Error Reporting";
            var windowsMitigationPath = "Software\\Microsoft\\WindowsMitigation";
            Registry.LocalMachine.OpenSubKey(policyDataPath, true)?.DeleteValue("AllowTelemetry", false);
            Registry.LocalMachine.OpenSubKey(dataCollectionPath, true)?.DeleteValue("MaxTelemetryAllowed", false);
            Registry.CurrentUser.OpenSubKey(diagTrackPath, true)?.DeleteValue("ShowedToastAtLevel", false);
            using var queueReportingTask = ScheduledTaskService.GetTaskOrDefault("Microsoft\\Windows\\Windows Error Reporting\\QueueReporting");
            ScheduledTaskService.SetState(queueReportingTask, true);
            Registry.CurrentUser.OpenSubKey(errorReportingPath, true)?.DeleteValue("Disabled", false);
            using var werService = new ServiceController("WerSvc");
            OsService.SetServiceStartMode(werService, ServiceStartMode.Manual);
            werService.TryStart();
            Registry.LocalMachine.OpenOrCreateSubKey(windowsMitigationPath).SetValue("UserPreference", state.Equals(1) ? 3 : 2, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set folders launch separate process state.
        /// </summary>
        /// <param name="enable">Folders launch separate process state.</param>
        public static void FoldersLaunchSeparateProcess(bool enable)
        {
            var explorerAdvancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(explorerAdvancedPath, true)?.SetValue("SeparateProcess", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set reserved storage state.
        /// </summary>
        /// <param name="state">Reserved storage state.</param>
        public static void ReservedStorage(int state)
        {
            _ = PowerShellService.Invoke($"Set-WindowsReservedStorageState -State {(state.Equals(1) ? "Disabled" : "Enabled")}");
        }

        /// <summary>
        /// Set help page state.
        /// </summary>
        /// <param name="enable">Help page state.</param>
        public static void F1HelpPage(bool enable)
        {
            if (enable)
            {
                var helpPagePath = "Software\\Classes\\Typelib\\{8cec5860-07a1-11d9-b15e-000d56bfe6ee}";
                Registry.CurrentUser.DeleteSubKeyTree(helpPagePath, false);
                return;
            }

            var helpPage64Path = "Software\\Classes\\Typelib\\{8cec5860-07a1-11d9-b15e-000d56bfe6ee}\\1.0\\0\\win64";
            Registry.CurrentUser.OpenOrCreateSubKey(helpPage64Path).SetValue(string.Empty, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set Num Lock state.
        /// </summary>
        /// <param name="enable">Num Lock state.</param>
        public static void NumLock(bool enable)
        {
            var keyboardIndicatorsPath = ".DEFAULT\\Control Panel\\Keyboard";
            Registry.Users.OpenSubKey(keyboardIndicatorsPath, true)?.SetValue("InitialKeyboardIndicators", $"{(enable ? "2147483650" : "2147483648")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Caps Lock state.
        /// </summary>
        /// <param name="enable">Caps Lock state.</param>
        public static void CapsLock(bool enable)
        {
            Registry.CurrentUser.OpenSubKey("Keyboard Layout", true)?.DeleteValue("Attributes", false);
            var keyboardPath = "System\\CurrentControlSet\\Control\\Keyboard Layout";
            var scancodeValue = "Scancode Map";

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(keyboardPath, true)?.DeleteValue(scancodeValue, false);
                return;
            }

            Registry.LocalMachine.OpenSubKey(keyboardPath, true)?.SetValue(scancodeValue, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 58, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set sticky shift state.
        /// </summary>
        /// <param name="enable">Sticky shift state.</param>
        public static void StickyShift(bool enable)
        {
            var stickyKeysPath = "Control Panel\\Accessibility\\StickyKeys";
            Registry.CurrentUser.OpenSubKey(stickyKeysPath, true)?.SetValue("Flags", $"{(enable ? "510" : "506")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set autoplay state.
        /// </summary>
        /// <param name="enable">Autoplay state.</param>
        public static void Autoplay(bool enable)
        {
            var policyExplorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var autoplayHandlersPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers";
            GroupPolicyService.ClearPolicyCache(policyExplorerPath, "NoDriveTypeAutoRun", Registry.LocalMachine, Registry.CurrentUser);
            Registry.CurrentUser.OpenSubKey(autoplayHandlersPath, true)?.SetValue("DisableAutoplay", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set thumbnail cache state.
        /// </summary>
        /// <param name="enable">Thumbnail cache state.</param>
        public static void ThumbnailCacheRemoval(bool enable)
        {
            var autorun = "Autorun";
            var thumbnailCachePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache";
            var thumbnailWowCachePath = "Software\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache";
            Registry.LocalMachine.OpenSubKey(thumbnailCachePath, true)?.SetValue(autorun, enable ? 3 : 0, RegistryValueKind.DWord);
            Registry.LocalMachine.OpenSubKey(thumbnailWowCachePath, true)?.SetValue(autorun, enable ? 3 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set save restartable apps state.
        /// </summary>
        /// <param name="enable">Restartable apps state.</param>
        public static void SaveRestartableApps(bool enable)
        {
            var logonPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";
            Registry.CurrentUser.OpenSubKey(logonPath, true)?.SetValue("RestartApps", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set network discovery state.
        /// </summary>
        /// <param name="enable">Network discovery state.</param>
        public static void NetworkDiscovery(bool enable)
        {
            if (enable)
            {
                FirewallService.SetGroupRules(name: "@FirewallAPI.dll,-32752", enable: true, profileID: 2);
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
            var policyPowerPath = "Software\\Policies\\Microsoft\\Power\\PowerSettings";
            var args = $"/SETACTIVE {(state.Equals(1) ? "SCHEME_MIN" : "SCHEME_BALANCED")}";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyPowerPath, "ActivePowerScheme");
            _ = ProcessService.WaitForExit("POWERCFG.EXE", args);
        }

        /// <summary>
        /// Set RKN bypass state.
        /// </summary>
        /// <param name="enable">RKN bypass state.</param>
        public static void RKNBypass(bool enable)
        {
            var settingsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            var autoConfigUrl = "AutoConfigURL";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(settingsPath, true)?.SetValue(autoConfigUrl, "https://p.thenewone.lol:8443/proxy.pac", RegistryValueKind.String);
                return;
            }

            Registry.CurrentUser.OpenSubKey(settingsPath, true)?.DeleteValue(autoConfigUrl, false);
        }

        /// <summary>
        /// Set registry backup state.
        /// </summary>
        /// <param name="enable">Registry backup state.</param>
        public static void RegistryBackup(bool enable)
        {
            var configurationPath = "System\\CurrentControlSet\\Control\\Session Manager\\Configuration Manager";
            var enablePeriodicBackup = "EnablePeriodicBackup";

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(configurationPath, true)?.SetValue(enablePeriodicBackup, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(configurationPath, true)?.DeleteValue(enablePeriodicBackup, false);
        }

        /// <summary>
        /// Set HEVC state.
        /// </summary>
        /// <param name="enable">HEVC state.</param>
        public static void HEVC(bool enable)
        {
            if (enable)
            {
                var downloadFolder = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string ?? Environment.GetEnvironmentVariable("TEMP");
                var appxFile = $"{downloadFolder}\\Microsoft.HEVCVideoExtension_8wekyb3d8bbwe.appx";
                HttpService.DownloadHEVCAppxAsync(appxFile).Wait();
                AppxPackagesService.InstallFromFileAsync(appxFile).Wait();
                File.Delete(appxFile);
                return;
            }

            AppxPackagesService.RemovePackage(packageName: "Microsoft.HEVCVideoExtension", forAllUsers: false);
        }

        /// <summary>
        /// Set Cortana auto start state.
        /// </summary>
        /// <param name="enable">Cortana auto start state.</param>
        public static void CortanaAutostart(bool enable)
        {
            var startupIdPath = "Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\SystemAppData\\Microsoft.549981C3F5F10_8wekyb3d8bbwe\\CortanaStartupId";
            Registry.ClassesRoot.OpenSubKey(startupIdPath, true)?.SetValue("State", enable ? 2 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Xbox game bar state.
        /// </summary>
        /// <param name="enable">Xbox game bar state.</param>
        public static void XboxGameBar(bool enable)
        {
            var barValue = enable ? 1 : 0;
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", true)?.SetValue("AppCaptureenable", barValue, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey("System\\GameConfigStore", true)?.SetValue("GameDVR_enable", barValue, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Xbox game tips state.
        /// </summary>
        /// <param name="enable">Xbox game tips state.</param>
        public static void XboxGameTips(bool enable)
        {
            var barValue = enable ? 1 : 0;
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\GameBar", true)?.SetValue("ShowStartupPanel", barValue, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set GPU scheduling state.
        /// </summary>
        /// <param name="enable">GPU scheduling state.</param>
        public static void GPUScheduling(bool enable)
        {
            var hwSchValue = enable ? 2 : 1;
            Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers")?.SetValue("HwSchMode", hwSchValue, RegistryValueKind.DWord);
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
            var protectionScript = $"Set-MpPreference -EnableNetworkProtection {(enable ? "enable" : "Disabled")}";
            _ = PowerShellService.Invoke(protectionScript);
        }

        /// <summary>
        /// Set Windows PUApps detection state.
        /// </summary>
        /// <param name="enable">PUApps detection state.</param>
        public static void PUAppsDetection(bool enable)
        {
            var detectionScript = $"Set-MpPreference -PUAProtection {(enable ? "enable" : "Disabled")}";
            _ = PowerShellService.Invoke(detectionScript);
        }

        /// <summary>
        /// Set Microsoft Defender sandbox state.
        /// </summary>
        /// <param name="enable">Microsoft Defender sandbox state.</param>
        public static void DefenderSandbox(bool enable)
        {
            var sandboxScript = $"setx /M MP_FORCE_USE_SANDBOX {(enable ? "1" : "0")}";
            _ = PowerShellService.Invoke(sandboxScript);
        }

        /// <summary>
        /// Set Windows Event Viewer custom view state.
        /// </summary>
        /// <param name="enable">Event Viewer custom view state.</param>
        public static void EventViewerCustomView(bool enable)
        {
            var auditValueName = "ProcessCreationIncludeCmdLine_enable";
            var viewerXmlPath = $"{Environment.GetEnvironmentVariable("ALLUSERSPROFILE")}\\Microsoft\\Event Viewer\\Views\\ProcessCreation.xml";
            var viewerAuditPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit";
            var viewerGuid = "{0CCE922B-69AE-11D9-BED3-505054503030}";
            var viewerXml = @$"<ViewerConfig>
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

            if (enable)
            {
                _ = PowerShellService.Invoke($"auditpol /set /subcategory:\"{viewerGuid}\" /success:enable /failure:enable");
                Registry.LocalMachine.OpenSubKey(viewerAuditPath, true)?.SetValue(auditValueName, 1, RegistryValueKind.DWord);
                FileService.Save(file: viewerXmlPath, content: viewerXml, encoding: Encoding.Default);
                return;
            }

            if (!CommonDataService.IsWindows11)
            {
                _ = PowerShellService.Invoke($"auditpol / set / subcategory:\"{viewerGuid}\" / success:disable / failure:disable");
            }

            Registry.LocalMachine.OpenSubKey(viewerAuditPath, true)?.DeleteValue(auditValueName, false);
            File.Delete(viewerXmlPath);
        }

        /// <summary>
        /// Set Windows PowerShell modules logging state.
        /// </summary>
        /// <param name="enable">PowerShell modules logging state.</param>
        public static void PowerShellModulesLogging(bool enable)
        {
            var moduleLoggingPath = "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging";
            var moduleNamesPath = $"{moduleLoggingPath}\\ModuleNames";
            var moduleLoggingValueName = "EnableModuleLogging";

            if (enable)
            {
                Registry.LocalMachine.OpenOrCreateSubKey(moduleNamesPath).SetValue("*", "*");
                Registry.LocalMachine.OpenSubKey(moduleLoggingPath, true)?.SetValue(moduleLoggingValueName, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(moduleLoggingPath, true)?.DeleteValue(moduleLoggingValueName, false);
            Registry.LocalMachine.OpenSubKey(moduleNamesPath, true)?.DeleteValue("*", false);
        }

        /// <summary>
        /// Set Windows PowerShell scripts logging state.
        /// </summary>
        /// <param name="enable">PowerShell scripts logging state.</param>
        public static void PowerShellScriptsLogging(bool enable)
        {
            var scriptLoggingPath = "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging";
            var scriptLoggingValueName = "EnableScriptBlockLogging";

            if (enable)
            {
                Registry.LocalMachine.OpenOrCreateSubKey(scriptLoggingPath).SetValue(scriptLoggingValueName, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(scriptLoggingPath, true)?.DeleteValue(scriptLoggingValueName, false);
        }

        /// <summary>
        /// Set Windows SmartScreen state.
        /// </summary>
        /// <param name="enable">Windows SmartScreen state.</param>
        public static void AppsSmartScreen(bool enable)
        {
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)
                ?.SetValue("SmartScreenenable", $"{(enable ? "Warn" : "Off")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Windows save zone state.
        /// </summary>
        /// <param name="enable">Windows save zone state.</param>
        public static void SaveZoneInformation(bool enable)
        {
            var policyAttachmentsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments";
            var saveZoneInformation = "SaveZoneInformation";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policyAttachmentsPath, saveZoneInformation);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(policyAttachmentsPath, true)?.DeleteValue(saveZoneInformation, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(policyAttachmentsPath).SetValue(saveZoneInformation, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows script host state.
        /// </summary>
        /// <param name="enable">Windows script host state.</param>
        public static void WindowsScriptHost(bool enable)
        {
            var scriptHostPath = "Software\\Microsoft\\Windows Script Host\\Settings";
            var scriptHostValueName = "enable";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(scriptHostPath, true)?.DeleteValue(scriptHostValueName, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(scriptHostPath).SetValue(scriptHostValueName, 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows Sandbox state.
        /// </summary>
        /// <param name="enable">Windows Sandbox state.</param>
        public static void WindowsSandbox(bool enable)
        {
            var enableSandboxScript = "Enable-WindowsOptionalFeature -FeatureName Containers-DisposableClientVM -All -Online -NoRestart";
            var disableSandboxScript = "Disable-WindowsOptionalFeature -FeatureName Containers-DisposableClientVM -All -Online -NoRestart";
            _ = PowerShellService.Invoke($"{(enable ? enableSandboxScript : disableSandboxScript)}");
        }

        /// <summary>
        /// Set Local Security Authority state.
        /// </summary>
        /// <param name="enable">ocal Security Authority state.</param>
        public static void LocalSecurityAuthority(bool enable)
        {
            var policySystemPath = "SOFTWARE\\Policies\\Microsoft\\Windows\\System";
            var lsaControlPath = "System\\CurrentControlSet\\Control\\Lsa";
            var runPPLValue = "RunAsPPL";
            var runPPLBootValue = "RunAsPPLBoot";
            GroupPolicyService.ClearPolicyCache(Registry.LocalMachine, policySystemPath, "RunAsPPL");

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(lsaControlPath, true)?.SetValue(runPPLValue, 2, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(lsaControlPath, true)?.SetValue(runPPLBootValue, 2, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(lsaControlPath, true)?.DeleteValue(runPPLValue, false);
            Registry.LocalMachine.OpenSubKey(lsaControlPath, true)?.DeleteValue(runPPLBootValue, false);
        }

        /// <summary>
        /// Set "Extract all" item in the Windows Installer (.msi) context menu state.
        /// </summary>
        /// <param name="enable">"Extract all" item state.</param>
        public static void MSIExtractContext(bool enable)
        {
            var msiExtractPath = "Msi.Package\\shell\\Extract";

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey($"{msiExtractPath}\\Command").SetValue(string.Empty, "msiexec.exe /a \"%1\" /qb TARGETDIR=\"%1 extracted\"", RegistryValueKind.String);

                Registry.ClassesRoot.OpenSubKey(msiExtractPath, true)?.SetValue("MUIVerb", "@shell32.dll,-37514", RegistryValueKind.String);

                Registry.ClassesRoot.OpenSubKey(msiExtractPath, true)?.SetValue("Icon", "shell32.dll,-16817", RegistryValueKind.String);

                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(msiExtractPath, false);
        }

        /// <summary>
        /// Set "Install" item in the Cabinet archives (.cab) context menu state.
        /// </summary>
        /// <param name="enable">"Install" item state.</param>
        public static void CABInstallContext(bool enable)
        {
            var runAsPath = "CABFolder\\Shell\\runas";

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey($"{runAsPath}\\Command")
                    .SetValue(string.Empty, "cmd /c DISM.exe /Online /Add-Package /PackagePath:\"%1\" /NoRestart & pause", RegistryValueKind.String);

                Registry.ClassesRoot.OpenSubKey(runAsPath, true)
                    ?.SetValue("MUIVerb", "@shell32.dll,-10210", RegistryValueKind.String);

                Registry.ClassesRoot.OpenSubKey(runAsPath, true)
                    ?.SetValue("HasLUAShield", string.Empty, RegistryValueKind.String);

                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(runAsPath, false);
        }

        /// <summary>
        /// Set "Cast to Device" item in the media files and folders context menu state.
        /// </summary>
        /// <param name="enable">"Cast to Device" item state.</param>
        public static void CastToDeviceContext(bool enable)
        {
            var shellBlockedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            var castToDeviceGuid = "{7AD84985-87B4-4a16-BE58-8B72A5B390F7}";

            Registry.LocalMachine.OpenSubKey(shellBlockedPath, true)?.DeleteValue(castToDeviceGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(shellBlockedPath, true)?.DeleteValue(castToDeviceGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(shellBlockedPath).SetValue(castToDeviceGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Share" context menu item state.
        /// </summary>
        /// <param name="enable">"Share" item state.</param>
        public static void ShareContext(bool enable)
        {
            var shellBlockedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            var shareContextGuid = "{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}";

            Registry.LocalMachine.OpenSubKey(shellBlockedPath, true)?.DeleteValue(shareContextGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(shellBlockedPath, true)?.DeleteValue(shareContextGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(shellBlockedPath).SetValue(shareContextGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Clipchamp" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Clipchamp" item state.</param>
        public static void EditWithClipchampContext(bool enable)
        {
            var clipChampPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            var clipChampGuid = "{8AB635F8-9A67-4698-AB99-784AD929F3B4}";

            Registry.LocalMachine.OpenSubKey(clipChampPath, true)?.DeleteValue(clipChampGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(clipChampPath, true)?.DeleteValue(clipChampGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(clipChampPath).SetValue(clipChampGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Photos" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Photos" item state.</param>
        public static void EditWithPhotosContext(bool enable)
        {
            var photosPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            var photosGuid = "{BFE0E2A4-C70C-4AD7-AC3D-10D1ECEBB5B4}";

            Registry.LocalMachine.OpenSubKey(photosPath, true)?.DeleteValue(photosGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(photosPath, true)?.DeleteValue(photosPath, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(photosPath).SetValue(photosGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Paint Context" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Paint Context" item state.</param>
        public static void EditWithPaintContext(bool enable)
        {
            var paintPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            var paintGuid = "{2430F218-B743-4FD6-97BF-5C76541B4AE9}";

            Registry.LocalMachine.OpenSubKey(paintPath, true)?.DeleteValue(paintGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(paintPath, true)?.DeleteValue(paintGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(paintPath).SetValue(paintGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit with Paint 3D" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit with Paint 3D" item state.</param>
        public static void EditWithPaint3DContext(bool enable)
        {
            var paintContextValue = "ProgrammaticAccessOnly";
            new List<string>()
            {
                ".bmp", ".gif", ".jpe", ".jpeg", ".jpg", ".png", ".tif", ".tiff",
            }
            .ForEach(fileType =>
            {
                var fileTypePath = $"SystemFileAssociations\\{fileType}\\Shell\\3D Edit";

                if (enable)
                {
                    Registry.ClassesRoot.OpenSubKey(fileTypePath, true)?.DeleteValue(paintContextValue, false);
                    return;
                }

                Registry.ClassesRoot.OpenSubKey(fileTypePath, true)?.SetValue(paintContextValue, string.Empty, RegistryValueKind.String);
            });
        }

        /// <summary>
        /// Set "Print" item in the .bat and .cmd files context menu state.
        /// </summary>
        /// <param name="enable">"Print" item state.</param>
        public static void PrintCMDContext(bool enable)
        {
            var batPrintPath = "batfile\\shell\\print";
            var cmdPrintPath = "cmdfile\\shell\\print";
            var printContextValue = "ProgrammaticAccessOnly";

            if (enable)
            {
                Registry.ClassesRoot.OpenSubKey(batPrintPath, true)?.DeleteValue(printContextValue, false);
                Registry.ClassesRoot.OpenSubKey(cmdPrintPath, true)?.DeleteValue(printContextValue, false);
                return;
            }

            Registry.ClassesRoot.OpenSubKey(batPrintPath, true)?.SetValue(printContextValue, string.Empty, RegistryValueKind.String);
            Registry.ClassesRoot.OpenSubKey(cmdPrintPath, true)?.SetValue(printContextValue, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Include in Library" item in the folders and drives context menu state.
        /// </summary>
        /// <param name="enable">"Include in Library" item state.</param>
        public static void IncludeInLibraryContext(bool enable)
        {
            var libraryContextPath = "Folder\\ShellEx\\ContextMenuHandlers\\Library Location";
            var enableValue = "{3dad6c5d-2167-4cae-9914-f99e41c12cfa}";
            var disableValue = "-{3dad6c5d-2167-4cae-9914-f99e41c12cfa}";
            var contextValue = enable ? enableValue : disableValue;
            Registry.ClassesRoot.OpenSubKey(libraryContextPath, true)?.SetValue(string.Empty, contextValue, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Send to" item in the folders context menu state.
        /// </summary>
        /// <param name="enable">"Send to" item state.</param>
        public static void SendToContext(bool enable)
        {
            var sendToPath = "AllFilesystemObjects\\shellex\\ContextMenuHandlers\\SendTo";
            var enableValue = "{7BA4C740-9E81-11CF-99D3-00AA004AE837}";
            var disableValue = "-{7BA4C740-9E81-11CF-99D3-00AA004AE837}";
            var contextValue = enable ? enableValue : disableValue;
            Registry.ClassesRoot.OpenSubKey(sendToPath, true)?.SetValue(string.Empty, contextValue, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Bitmap image" item in the "New" context menu state.
        /// </summary>
        /// <param name="enable">"Bitmap image" item state.</param>
        public static void BitmapImageNewContext(bool enable)
        {
            var bmpShellPath = ".bmp\\ShellNew";

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(bmpShellPath).SetValue("ItemName", "@%SystemRoot%\\System32\\mspaint.exe,-59414", RegistryValueKind.ExpandString);
                Registry.ClassesRoot.OpenSubKey(bmpShellPath, true)?.SetValue("NullFile", string.Empty, RegistryValueKind.String);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(bmpShellPath, false);
        }

        /// <summary>
        /// Set "Rich Text Document" item in the "New" context menu state.
        /// </summary>
        /// <param name="enable">"Rich Text Document" item state.</param>
        public static void RichTextDocumentNewContext(bool enable)
        {
            var rtfShellPath = ".rtf\\ShellNew";

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(rtfShellPath).SetValue("Data", @"{\rtf1}", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey(rtfShellPath, true)?.SetValue("ItemName", "@%ProgramFiles%\\Windows NT\\Accessories\\WORDPAD.EXE,-213", RegistryValueKind.ExpandString);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(rtfShellPath, false);
        }

        /// <summary>
        /// Set "Compressed (zipped) Folder" item in the "New" context menu state.
        /// </summary>
        /// <param name="enable">"Compressed (zipped) Folder" item state.</param>
        public static void CompressedFolderNewContext(bool enable)
        {
            var zipShellPath = ".zip\\CompressedFolder\\ShellNew";
            var zipContextValue = new byte[] { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(zipShellPath).SetValue("Data", zipContextValue, RegistryValueKind.Binary);
                Registry.ClassesRoot.OpenSubKey(zipShellPath, true)?.SetValue("ItemName", "@%SystemRoot%\\System32\\zipfldr.dll,-10194", RegistryValueKind.ExpandString);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(zipShellPath, false);
        }

        /// <summary>
        /// Set "Open", "Print", and "Edit" context menu items available when selecting more than 15 files state.
        /// </summary>
        /// <param name="enable">"Open", "Print", and "Edit" context menu items state.</param>
        public static void MultipleInvokeContext(bool enable)
        {
            var multipleContextPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            var multipleContextValue = "MultipleInvokePromptMinimum";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(multipleContextPath, true)?.SetValue(multipleContextValue, 300, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(multipleContextPath, true)?.DeleteValue(multipleContextValue, false);
        }

        /// <summary>
        /// Set "Look for an app in the Microsoft Store" items in the "Open with" dialog state.
        /// </summary>
        /// <param name="enable">"Look for an app in the Microsoft Store" items state.</param>
        public static void UseStoreOpenWith(bool enable)
        {
            var storeContextPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var storeContextValue = "NoUseStoreOpenWith";

            Registry.LocalMachine.OpenSubKey(storeContextPath, true)?.DeleteValue(storeContextValue, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(storeContextPath, true)?.DeleteValue(storeContextValue, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(storeContextPath).SetValue(storeContextValue, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set "Open in Windows Terminal" item in the folders context menu state.
        /// </summary>
        /// <param name="enable">"Open in Windows Terminal" item state.</param>
        public static void OpenWindowsTerminalContext(bool enable)
        {
            var extensionsBlockPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            var terminalGuid = "{9F156763-7844-4DC4-B2B1-901F640F5155}";

            Registry.LocalMachine.OpenSubKey(extensionsBlockPath, true)?.DeleteValue(terminalGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(extensionsBlockPath, true)?.DeleteValue(terminalGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(extensionsBlockPath).SetValue(terminalGuid, string.Empty, RegistryValueKind.String);
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
                var deserializedSettings = JsonConvert.DeserializeObject(File.ReadAllText(terminalSettings, Encoding.UTF8)) as JObject;
                var elevateSetting = deserializedSettings?.SelectToken("profiles.defaults.elevate");

                if (elevateSetting is null)
                {
                    var defaultsSetting = deserializedSettings!.SelectToken("profiles.defaults") as JObject;
                    defaultsSetting!.Add(new JProperty("elevate", string.Empty));
                    elevateSetting = deserializedSettings!.SelectToken("profiles.defaults.elevate");
                }

                elevateSetting!.Replace(enable);
                File.WriteAllText(terminalSettings, deserializedSettings!.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed write data to configuration file", ex);
            }
        }

        /// <summary>
        /// Set images edit from context menu state.
        /// </summary>
        /// <param name="enable">Images edit from context menu state.</param>
        public static void ImagesEditContext(bool enable)
        {
            var accessPath = "SystemFileAssociations\\image\\shell\\edit";
            var accessName = "ProgrammaticAccessOnly";

            if (enable)
            {
                Registry.ClassesRoot.OpenSubKey(accessPath, true)?.DeleteValue(accessName, false);
                return;
            }

            Registry.ClassesRoot.OpenOrCreateSubKey(accessPath).SetValue(accessName, string.Empty, RegistryValueKind.String);
        }
    }
}
