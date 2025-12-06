// <copyright file="Mutators.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Customizations
{
    using CSharpFunctionalExtensions;
    using Microsoft.Win32;
    using Microsoft.Win32.TaskScheduler;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ServiceProcess;
    using System.Text;
    using System.Xml.Linq;
    using static System.Formats.Asn1.AsnWriter;

    /// <summary>
    /// Set the OS settings.
    /// </summary>
    public static class Mutators
    {
        private static readonly IAppNotificationService AppNotificationService = App.GetService<IAppNotificationService>();
        private static readonly IAppxPackagesService AppxPackagesService = App.GetService<IAppxPackagesService>();
        private static readonly ICommonDataService CommonDataService = App.GetService<ICommonDataService>();
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
            var diagTrackService = new System.ServiceProcess.ServiceController("DiagTrack");
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
            using var werService = new System.ServiceProcess.ServiceController("WerSvc");
            GroupPolicyService.ClearRegistryCache(policyReportingPath, "Disabled", Registry.LocalMachine, Registry.CurrentUser);
            GroupPolicyService.ClearLocalCache(policyReportingPath, "Disabled", LGPOScope.Computer, LGPOScope.User);
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
            var notShowFeedback = "DoNotShowFeedbackNotifications";
            var policyCollectionPath = "Software\\Policies\\Microsoft\\Windows\\DataCollection";
            var rulesPath = "Software\\Microsoft\\Siuf\\Rules";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policyCollectionPath, notShowFeedback);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policyCollectionPath, notShowFeedback);

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
            var disableRestart = "DisableAutomaticRestartSignOn";
            var optOut = "OptOut";
            var userSid = InstrumentationService.GetUserSid(Environment.UserName);
            var policySystemPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            var userArsoPath = $"Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\\UserARSO\\{userSid}";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policySystemPath, disableRestart);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policySystemPath, disableRestart);

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
            var disabledByPolicy = "DisabledByGroupPolicy";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, advertisingPolicyPath, disabledByPolicy);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, "Software\\Policies\\Microsoft\\Windows\\DataCollection", disabledByPolicy);
            Registry.CurrentUser.OpenOrCreateSubKey(advertisingPath)
                .SetValue("enable", enable ? 1 : 0, RegistryValueKind.DWord);
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
            var disableLanding = "DisableSoftLanding";
            var policyCloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policyCloudPath, disableLanding);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policyCloudPath, disableLanding);
            Registry.CurrentUser.OpenSubKey(contentDeliveryPath, true)
                ?.SetValue("SubscribedContent-338389enable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the suggested content in the Settings app state.
        /// </summary>
        /// <param name="enable">Suggested content state.</param>
        public static void SettingsSuggestedContent(bool enable)
        {
            var contentManager = "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager";
            new List<string> { "SubscribedContent-353694Enable", "SubscribedContent-353696Enable", "SubscribedContent-338393Enable" }
            .ForEach(content => Registry.CurrentUser.OpenSubKey(contentManager, true)
                ?.SetValue(content, enable ? 1 : 0, RegistryValueKind.DWord));
        }

        /// <summary>
        /// Set the automatic installing suggested apps state.
        /// </summary>
        /// <param name="enable">Suggested apps state.</param>
        public static void AppsSilentInstalling(bool enable)
        {
            var disableFeatures = "DisableWindowsConsumerFeatures";
            var contentManager = "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager";
            var cloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, cloudPath, disableFeatures);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, cloudPath, disableFeatures);
            Registry.CurrentUser.OpenSubKey(contentManager, true)
                ?.SetValue("SilentInstalledAppsenable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set the Windows feature "Whats New" state.
        /// </summary>
        /// <param name="enable">Whats New state.</param>
        public static void WhatsNewInWindows(bool enable)
        {
            var profilePath = "Software\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement";
            Registry.CurrentUser.OpenOrCreateSubKey(profilePath)
                .SetValue("ScoobeSystemSettingEnable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows feature "Tailored experiences" state.
        /// </summary>
        /// <param name="enable">Tailored experiences state.</param>
        public static void TailoredExperiences(bool enable)
        {
            var disableDiagnostic = "DisableTailoredExperiencesWithDiagnosticData";
            var policyCloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            var privacyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Privacy";
            GroupPolicyService.ClearRegistryCache(Registry.CurrentUser, policyCloudPath, disableDiagnostic);
            GroupPolicyService.ClearLocalCache(LGPOScope.User, policyCloudPath, disableDiagnostic);
            Registry.CurrentUser.OpenSubKey(privacyPath, true)
                ?.SetValue("TailoredExperiencesWithDiagnosticDataEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
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
                Registry.CurrentUser.OpenSubKey(explorerPath, true)
                    ?.DeleteValue(disableSuggestions, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(explorerPath)
                .SetValue(disableSuggestions, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu recommendations state.
        /// </summary>
        /// <param name="enable">Start menu recommendations state.</param>
        public static void StartRecommendationsTips(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var startRecommendations = "Start_IrisRecommendations";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(advancedPath, true)
                    ?.DeleteValue(startRecommendations, false);
                return;
            }

            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue(startRecommendations, 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start Menu notifications state.
        /// </summary>
        /// <param name="enable">Start Menu notifications state.</param>
        public static void StartAccountNotifications(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var startNotifications = "Start_AccountNotifications";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(advancedPath, true)
                    ?.DeleteValue(startNotifications, false);
                return;
            }

            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue(startNotifications, 0, RegistryValueKind.DWord);
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
                Registry.CurrentUser.OpenOrCreateSubKey(pcPath)
                    .SetValue(pcGuid, 0, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(pcPath, true)
                ?.DeleteValue(pcGuid, false);
        }

        /// <summary>
        /// Set item check boxes state.
        /// </summary>
        /// <param name="enable">Item check boxes state.</param>
        public static void CheckBoxes(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("AutoCheckSelect", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set hidden files, folders, and drives state.
        /// </summary>
        /// <param name="enable">Hidden items state.</param>
        public static void HiddenItems(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("Hidden", enable ? 1 : 2, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set file name extensions visibility state.
        /// </summary>
        /// <param name="enable">File extensions visibility state.</param>
        public static void FileExtensions(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("HideFileExt", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set folder merge conflicts state.
        /// </summary>
        /// <param name="enable">Folder merge conflicts state.</param>
        public static void MergeConflicts(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("HideMergeConflicts", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set how to open File Explorer.
        /// </summary>
        /// <param name="state">File Explorer open state.</param>
        public static void OpenFileExplorerTo(int state)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("LaunchTo", state, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer ribbon state.
        /// </summary>
        /// <param name="state">File Explorer ribbon state.</param>
        public static void FileExplorerRibbon(int state)
        {
            var explorerRibbonPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Ribbon";
            var policyExplorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var ribbonMinimized = "ExplorerRibbonStartsMinimized";
            GroupPolicyService.ClearRegistryCache(policyExplorerPath, ribbonMinimized, Registry.LocalMachine, Registry.CurrentUser);
            GroupPolicyService.ClearLocalCache(policyExplorerPath, ribbonMinimized, LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenOrCreateSubKey(explorerRibbonPath)
                .SetValue("MinimizedStateTabletModeOff", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer compact mode state.
        /// </summary>
        /// <param name="enable">File Explorer compact mode state.</param>
        public static void FileExplorerCompactMode(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("UseCompactMode", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set File Explorer provider notification visibility state.
        /// </summary>
        /// <param name="enable">File Explorer provider notification visibility state.</param>
        public static void OneDriveFileExplorerAd(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("ShowSyncProviderNotifications", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set snap a window state.
        /// </summary>
        /// <param name="enable">Snap Assist state.</param>
        public static void SnapAssist(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true)
                ?.SetValue("WindowArrangementActive", "1", RegistryValueKind.String);
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("SnapAssist", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set file transfer dialog box mode.
        /// </summary>
        /// <param name="state">File transfer dialog box state.</param>
        public static void FileTransferDialog(int state)
        {
            var statusPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager";
            Registry.CurrentUser.OpenOrCreateSubKey(statusPath)
                .SetValue("EnthusiastMode", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set recycle bin confirmation dialog state.
        /// </summary>
        /// <param name="enable">Recycle bin dialog state.</param>
        public static void RecycleBinDeleteConfirmation(bool enable)
        {
            GroupPolicyService.ClearRegistryCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "ConfirmFileDelete", Registry.LocalMachine, Registry.CurrentUser);
            GroupPolicyService.ClearLocalCache("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "ConfirmFileDelete", LGPOScope.Computer, LGPOScope.User);
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
            var noRecent = "NoRecentDocsHistory";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\Explorer", noRecent);
            GroupPolicyService.ClearRegistryCache(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", noRecent);
            GroupPolicyService.ClearLocalCache("Software\\Policies\\Microsoft\\Windows\\Explorer", noRecent, LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", true)
                ?.SetValue("ShowRecent", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set frequently used Quick access folders state.
        /// </summary>
        /// <param name="enable">Quick access folders state.</param>
        public static void QuickAccessFrequentFolders(bool enable)
        {
            var frequentPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            Registry.CurrentUser.OpenSubKey(frequentPath, true)
                ?.SetValue("ShowFrequent", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar alignment state.
        /// </summary>
        /// <param name="state">Taskbar alignment state.</param>
        public static void TaskbarAlignment(int state)
        {
            var alignmentPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(alignmentPath, true)
                ?.SetValue("TaskbarAl", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar widgets icon state.
        /// </summary>
        /// <param name="enable">Taskbar widgets icon state.</param>
        public static void TaskbarWidgets(bool enable)
        {
            var advancedPath = "HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var allowNews = "AllowNewsAndInterests";
            var dshPath = "Software\\Policies\\Microsoft\\Dsh";
            var newsPath = "Software\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, newsPath, "value");
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, dshPath, allowNews);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, dshPath, allowNews);
            var command = $"-Command \"& {{New-ItemProperty -Path {advancedPath} -Name TaskbarDa -PropertyType DWord -Value {(enable ? 1 : 0)} -Force}}\"";
            PowerShellService.InvokeCommandBypassUCPD(command);
        }

        /// <summary>
        /// Set Search on the taskbar state.
        /// </summary>
        /// <param name="state">Taskbar search state.</param>
        public static void TaskbarSearchWindows10(int state)
        {
            var disableSearch = "DisableSearch";
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var searchMode = "SearchOnTaskbarMode";
            var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Search";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policySearchPath, disableSearch, searchMode);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policySearchPath, disableSearch, searchMode);
            Registry.CurrentUser.OpenSubKey(searchPath, true)
                ?.SetValue("SearchboxTaskbarMode", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Search on the taskbar state.
        /// </summary>
        /// <param name="state">Taskbar search state.</param>
        public static void TaskbarSearchWindows11(int state)
        {
            var disableSearch = "DisableSearch";
            var policyDisablePath = "Software\\Microsoft\\PolicyManager\\default\\Search\\DisableSearch";
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Search";
            var taskbarSearchMode = "SearchOnTaskbarMode";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policyDisablePath, "value", 0, RegistryValueKind.DWord);
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policySearchPath, disableSearch, taskbarSearchMode);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policySearchPath, disableSearch, taskbarSearchMode);

            var searchMode = state switch
            {
                3 => 3,
                4 => 2,
                _ => state - 1,
            };

            Registry.CurrentUser.OpenSubKey(searchPath, true)
                ?.SetValue("SearchboxTaskbarMode", searchMode, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set search highlights state.
        /// </summary>
        /// <param name="enable">Search highlights state.</param>
        public static void SearchHighlightsWindows10(bool enable)
        {
            var enableContent = "EnableDynamicContentInWSB";
            var feedsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Feeds\\DSB";
            var policySearch = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policySearch, enableContent);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policySearch, enableContent);
            Registry.CurrentUser.OpenSubKey(feedsPath, true)
                ?.SetValue("ShowDynamicContent", enable ? 1 : 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey(searchPath, true)
                ?.SetValue("IsDynamicSearchBoxEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set search highlights state.
        /// </summary>
        /// <param name="enable">Search highlights state.</param>
        public static void SearchHighlightsWindows11(bool enable)
        {
            var policySearchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            var searchSettingsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings";
            var enableContent = "EnableDynamicContentInWSB";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policySearchPath, enableContent);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policySearchPath, enableContent);

            if (enable)
            {
                var searchPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Search";
                var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
                Registry.CurrentUser.OpenSubKey(searchPath, true)
                    ?.DeleteValue("BingSearchEnabled", false);
                Registry.CurrentUser.OpenSubKey(explorerPath, true)
                    ?.DeleteValue("DisableSearchBoxSuggestions", false);
            }

            Registry.CurrentUser.OpenSubKey(searchSettingsPath, true)
                ?.SetValue("IsDynamicSearchBoxEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Cortana button taskbar state.
        /// </summary>
        /// <param name="enable">Cortana button state.</param>
        public static void CortanaButton(bool enable)
        {
            var allowCortana = "AllowCortana";
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var searchPath = "Software\\Policies\\Microsoft\\Windows\\Windows Search";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, searchPath, allowCortana);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, searchPath, allowCortana);
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("ShowCortanaButton", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar task view button state.
        /// </summary>
        /// <param name="enable">Taskbar task view button state.</param>
        public static void TaskViewButton(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";

            if (CommonDataService.IsWindows11)
            {
                var hideView = "HideTaskViewButton";
                var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
                GroupPolicyService.ClearRegistryCache(explorerPath, hideView, Registry.CurrentUser, Registry.LocalMachine);
                GroupPolicyService.ClearLocalCache(explorerPath, hideView, LGPOScope.User, LGPOScope.Computer);
            }

            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("ShowTaskViewButton", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set News and Interests state.
        /// </summary>
        /// <param name="enable">News and Interests state.</param>
        public static void NewsInterests(bool enable)
        {
            var feedsPath = "Software\\Policies\\Microsoft\\Windows\\Windows Feeds";
            var newsPath = "Software\\Microsoft\\PolicyManager\\default\\NewsAndInterests\\AllowNewsAndInterests";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, feedsPath, "EnableFeeds");
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, newsPath, "value");
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
            var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var peoplePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People";
            GroupPolicyService.ClearRegistryCache(explorerPath, "HidePeopleBar", Registry.CurrentUser, Registry.LocalMachine);
            Registry.CurrentUser.OpenOrCreateSubKey(peoplePath)
                ?.SetValue("PeopleBand", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Meet Now icon state.
        /// </summary>
        /// <param name="enable">Meet Now icon state.</param>
        public static void MeetNow(bool enable)
        {
            var settings = "Settings";
            var stuckPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3";
            var hideMeet = "HideSCAMeetNow";
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            GroupPolicyService.ClearRegistryCache(explorerPath, hideMeet, Registry.CurrentUser, Registry.LocalMachine);
            GroupPolicyService.ClearLocalCache(explorerPath, hideMeet, LGPOScope.User, LGPOScope.Computer);
            var stuckSettings = Registry.CurrentUser.OpenSubKey(stuckPath)?.GetValue(settings) as byte[] ?? new byte[10];
            stuckSettings[9] = enable ? (byte)0 : (byte)128;
            Registry.CurrentUser.OpenSubKey(stuckPath, true)
                ?.SetValue(settings, stuckSettings, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set Windows Ink Workspace button state.
        /// </summary>
        /// <param name="enable">Windows Ink Workspace button state.</param>
        public static void WindowsInkWorkspace(bool enable)
        {
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var penPath = "Software\\Microsoft\\Windows\\CurrentVersion\\PenWorkspace";
            var workspacePath = "Software\\Policies\\Microsoft\\WindowsInkWorkspace";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, workspacePath, "AllowWindowsInkWorkspace");
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, explorerPath, "HideSCAMeetNow");
            Registry.CurrentUser.OpenSubKey(penPath, true)
                ?.SetValue("PenWorkspaceButtonDesiredVisibility", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set notification area icons state.
        /// </summary>
        /// <param name="enable">Notification area icons state.</param>
        public static void NotificationAreaIcons(bool enable)
        {
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            var noNotify = "NoAutoTrayNotify";
            var policyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            GroupPolicyService.ClearRegistryCache(policyPath, noNotify, Registry.CurrentUser, Registry.LocalMachine);
            GroupPolicyService.ClearLocalCache(policyPath, noNotify, LGPOScope.User, LGPOScope.Computer);
            Registry.CurrentUser.OpenSubKey(explorerPath, true)
                ?.SetValue("EnableAutoTray", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set seconds on the taskbar clock state.
        /// </summary>
        /// <param name="enable">Seconds on the taskbar clock state.</param>
        public static void SecondsInSystemClock(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("ShowSecondsInSystemClock", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set taskbar combine state.
        /// </summary>
        /// <param name="state">Taskbar combine state.</param>
        public static void TaskbarCombine(int state)
        {
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var noGrouping = "NoTaskGrouping";
            GroupPolicyService.ClearRegistryCache(explorerPath, noGrouping, Registry.LocalMachine, Registry.CurrentUser);
            GroupPolicyService.ClearLocalCache(explorerPath, noGrouping, LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("TaskbarGlomLevel", state - 1, RegistryValueKind.DWord);
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
                Registry.CurrentUser.OpenOrCreateSubKey(taskbarPath)
                    .SetValue(taskbarTask, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(taskbarPath, true)
                ?.DeleteValue(taskbarTask, false);
        }

        /// <summary>
        /// Set Control Panel icons view state.
        /// </summary>
        /// <param name="state">Control Panel icons view state.</param>
        public static void ControlPanelView(int state)
        {
            var allView = "AllItemsIconView";
            var controlPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel";
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var forcePanel = "ForceClassicControlPanel";
            var startupPage = "StartupPage";
            GroupPolicyService.ClearRegistryCache(Registry.CurrentUser, explorerPath, forcePanel);
            GroupPolicyService.ClearLocalCache(LGPOScope.User, explorerPath, forcePanel);

            switch (state)
            {
                case 1:
                    Registry.CurrentUser.OpenOrCreateSubKey(controlPath)
                        .SetValue(allView, 0, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey(controlPath, true)
                        ?.SetValue(startupPage, 0, RegistryValueKind.DWord);
                    break;
                case 2:
                    Registry.CurrentUser.OpenOrCreateSubKey(controlPath)
                        .SetValue(allView, 0, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey(controlPath, true)
                        ?.SetValue(startupPage, 1, RegistryValueKind.DWord);
                    break;
                default:
                    Registry.CurrentUser.OpenOrCreateSubKey(controlPath)
                        .SetValue(allView, 1, RegistryValueKind.DWord);
                    Registry.CurrentUser.OpenSubKey(controlPath, true)
                        ?.SetValue(startupPage, 1, RegistryValueKind.DWord);
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
            Registry.CurrentUser.OpenSubKey(personalizePath, true)
                ?.SetValue("SystemUsesLightTheme", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set apps color mode state.
        /// </summary>
        /// <param name="state">Apps color mode state.</param>
        public static void AppColorMode(int state)
        {
            var personalizePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
            Registry.CurrentUser.OpenSubKey(personalizePath, true)
                ?.SetValue("AppsUseLightTheme", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set "New App Installed" indicator state.
        /// </summary>
        /// <param name="enable">New App Installed" indicator state.</param>
        public static void NewAppInstalledNotification(bool enable)
        {
            var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var noAlert = "NoNewAppAlert";

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(explorerPath, true)
                    ?.DeleteValue(noAlert, false);
                return;
            }

            Registry.LocalMachine.OpenOrCreateSubKey(explorerPath)
                .SetValue(noAlert, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set first sign-in animation state.
        /// </summary>
        /// <param name="enable">First sign-in animation state.</param>
        public static void FirstLogonAnimation(bool enable)
        {
            var systemPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            var logonPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";
            var enableAnimation = "EnableFirstLogonAnimation";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, systemPath, enableAnimation);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, systemPath, enableAnimation);
            Registry.LocalMachine.OpenSubKey(logonPath, true)
                ?.SetValue(enableAnimation, enable ? 1 : 0, RegistryValueKind.DWord);
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
                Registry.CurrentUser.OpenSubKey(desktopPath, true)
                    ?.SetValue(jpegQuality, 100, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(desktopPath, true)
                ?.DeleteValue(jpegQuality, false);
        }

        /// <summary>
        /// Set "- Shortcut" suffix state.
        /// </summary>
        /// <param name="enable">"- Shortcut" suffix state.</param>
        public static void ShortcutsSuffix(bool enable)
        {
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            var templatesPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates";
            var shortcutTemplate = "ShortcutNameTemplate";
            Registry.CurrentUser.OpenSubKey(explorerPath, true)
                ?.DeleteValue("link", false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(templatesPath, true)
                    ?.DeleteValue(shortcutTemplate, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(templatesPath)
                ?.SetValue(shortcutTemplate, "%s.lnk", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Print screen button state.
        /// </summary>
        /// <param name="enable">Print screen button state.</param>
        public static void PrtScnSnippingTool(bool enable)
        {
            var keyboardPath = "Control Panel\\Keyboard";
            Registry.CurrentUser.OpenSubKey(keyboardPath, true)
                ?.SetValue("PrintScreenKeyForSnippingEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
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
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var noShortcuts = "NoWindowMinimizingShortcuts";
            GroupPolicyService.ClearRegistryCache(explorerPath, noShortcuts, Registry.CurrentUser, Registry.LocalMachine);
            GroupPolicyService.ClearLocalCache(explorerPath, noShortcuts, LGPOScope.User, LGPOScope.Computer);
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
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
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("NavPaneExpandToCurrentFolder", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu recently added apps state.
        /// </summary>
        /// <param name="enable">Start menu recently added apps state.</param>
        public static void RecentlyAddedApps(bool enable)
        {
            var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var hideApps = "HideRecentlyAddedApps";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, explorerPath, hideApps);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(explorerPath, true)
                    ?.DeleteValue(hideApps, false);
                GroupPolicyService.ClearLocalCache(LGPOScope.User, explorerPath, hideApps);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(explorerPath)
                .SetValue(hideApps, 1, RegistryValueKind.DWord);
            GroupPolicyService.ClearLocalCache(scope: LGPOScope.User, path: explorerPath, name: hideApps, type: "DWORD", value: "1");
        }

        /// <summary>
        /// Set Start menu app suggestions state.
        /// </summary>
        /// <param name="enable">Start menu app suggestions state.</param>
        public static void AppSuggestions(bool enable)
        {
            var disableFeatures = "DisableWindowsConsumerFeatures";
            var contentPath = "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager";
            var cloudPath = "Software\\Policies\\Microsoft\\Windows\\CloudContent";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, cloudPath, disableFeatures);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, cloudPath, disableFeatures);
            Registry.CurrentUser.OpenSubKey(contentPath, true)
                ?.SetValue("SubscribedContent-338388enable", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Start menu layout state.
        /// </summary>
        /// <param name="state">Start menu layout state.</param>
        public static void StartLayout(int state)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("Start_Layout", state - 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set recommended section state.
        /// </summary>
        /// <param name="enable">Recommended section state.</param>
        public static void StartRecommendedSection(bool enable)
        {
            var educationPath = "Software\\Microsoft\\PolicyManager\\current\\device\\Education";
            var explorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var hideRecommended = "HideRecommendedSection";
            var policyExplorerPath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, policyExplorerPath, hideRecommended);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policyExplorerPath, hideRecommended);
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, educationPath, "IsEducationEnvironment");

            if (enable)
            {
                var startPath = "Software\\Microsoft\\PolicyManager\\Current\\Device\\Start";
                Registry.CurrentUser.OpenSubKey(explorerPath, true)
                    ?.DeleteValue(hideRecommended, false);
                Registry.LocalMachine.OpenSubKey(startPath, true)
                    ?.DeleteValue(hideRecommended, false);
                GroupPolicyService.ClearLocalCache(LGPOScope.User, explorerPath, hideRecommended);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(explorerPath)
                .SetValue(hideRecommended, 1, RegistryValueKind.DWord);
            GroupPolicyService.ClearLocalCache(scope: LGPOScope.User, path: explorerPath, name: hideRecommended, type: "DWORD", value: "1");
        }

        /// <summary>
        /// Set One Drive state.
        /// </summary>
        /// <param name="enable">One Drive state.</param>
        public static void OneDrive(bool enable)
        {
            var disableNgsc = "DisableFileSyncNGSC";
            var oneDrivePath = "Policies\\Microsoft\\Windows\\OneDrive";
            var policyOneDrivePath = "Software\\Policies\\Microsoft\\Windows\\OneDrive";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, oneDrivePath, disableNgsc);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, policyOneDrivePath, disableNgsc);

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
            var allowGlobal = "AllowStorageSenseGlobal";
            var sensePath = "Software\\Policies\\Microsoft\\Windows\\StorageSense";
            var storagePath = "Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, sensePath, allowGlobal);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, sensePath, allowGlobal);

            if (enable)
            {
                Registry.CurrentUser.OpenOrCreateSubKey(storagePath)
                    .SetValue("01", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenSubKey(storagePath, true)
                    ?.SetValue("04", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.OpenSubKey(storagePath, true)
                    ?.SetValue("2048", 30, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(storagePath)
                .SetValue("01", 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey(storagePath, true)
                ?.SetValue("04", 0, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey(storagePath, true)
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
            var supportPath = "System\\CurrentControlSet\\Control\\FileSystem";
            var longPathEnabled = "LongPathsEnabled";
            Registry.LocalMachine.OpenSubKey(supportPath, true)
                ?.SetValue(longPathEnabled, enable ? 1 : 0, RegistryValueKind.DWord);
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, supportPath, longPathEnabled, enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set BSOD error state.
        /// </summary>
        /// <param name="enable">BSOD error state.</param>
        public static void BSoDStopError(bool enable)
        {
            var crashPath = "System\\CurrentControlSet\\Control\\CrashControl";
            Registry.LocalMachine.OpenSubKey(crashPath, true)
                ?.SetValue("DisplayParameters", enable ? 1 : 0, RegistryValueKind.DWord);
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
            var deliveryPath = "Software\\Policies\\Microsoft\\Windows\\DeliveryOptimization";
            var downloadMode = "DODownloadMode";
            var settingsPath = "S-1-5-20\\Software\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Settings";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, deliveryPath, downloadMode);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, deliveryPath, downloadMode);
            Registry.Users.OpenSubKey(settingsPath, true)
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
            var windowsPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows";
            Registry.CurrentUser.OpenSubKey(windowsPath, true)
                ?.SetValue("LegacyDefaultPrinterMode", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set update Microsoft products state.
        /// </summary>
        /// <param name="enable">Update Microsoft products state.</param>
        public static void UpdateMicrosoftProducts(bool enable)
        {
            var allowService = "AllowMUUpdateService";
            var updatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, updatePath, allowService);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, updatePath, allowService);

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
            var setDisable = "SetAutoRestartNotificationDisable";
            var updatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, updatePath, setDisable);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, updatePath, setDisable);
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings", true)
                ?.SetValue("RestartNotificationsAllowed2", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set restart device after update state.
        /// </summary>
        /// <param name="enable">Restart device after update state.</param>
        public static void RestartDeviceAfterUpdate(bool enable)
        {
            var activeEnd = "ActiveHoursEnd";
            var activeStart = "ActiveHoursStart";
            var setHours = "SetActiveHours";
            var settingsPath = "Software\\Microsoft\\WindowsUpdate\\UX\\Settings";
            var updatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, updatePath, activeStart, activeEnd, setHours);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, updatePath, activeStart, activeEnd, setHours);
            Registry.LocalMachine.OpenSubKey(settingsPath, true)
                ?.SetValue("IsExpedited", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set active hours restart state.
        /// </summary>
        /// <param name="state">Active hours restart state.</param>
        public static void ActiveHours(int state)
        {
            var activeEnd = "ActiveHoursEnd";
            var activeStart = "ActiveHoursStart";
            var alwaysAuto = "AlwaysAutoRebootAtScheduledTime";
            var autoPath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
            var noAuto = "NoAutoRebootWithLoggedOnUsers";
            var setHours = "SetActiveHours";
            var windowsUpdatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, autoPath, noAuto, alwaysAuto);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, autoPath, noAuto, alwaysAuto);
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, windowsUpdatePath, activeStart, activeEnd, setHours);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, windowsUpdatePath, activeStart, activeEnd, setHours);
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings", true)
                ?.SetValue("SmartActiveHoursState", state.Equals(1) ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Windows latest update state.
        /// </summary>
        /// <param name="enable">Latest update state.</param>
        public static void WindowsLatestUpdate(bool enable)
        {
            var allowContent = "AllowOptionalContent";
            var setContent = "SetAllowOptionalContent";
            var updatePath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, updatePath, allowContent, setContent);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, updatePath, allowContent, setContent);
            Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings", true)
                ?.SetValue("IsContinuousInnovationOptedIn", enable ? 1 : 0, RegistryValueKind.DWord);
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

            var profilePath = "Control Panel\\International\\User Profile";
            Registry.CurrentUser.OpenSubKey(profilePath, true)
                ?.DeleteValue("InputMethodOverride", false);
        }

        /// <summary>
        /// Set installed .NET state.
        /// </summary>
        /// <param name="enable">Installed .NET state.</param>
        public static void LatestInstalledNET(bool enable)
        {
            var clrPath = "Software\\Microsoft\\.NETFramework";
            var clrWowPath = "Software\\Wow6432Node\\Microsoft\\.NETFramework";
            var latestClr = "OnlyUseLatestCLR";

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(clrPath, true)
                    ?.SetValue(latestClr, 1, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(clrWowPath, true)
                    ?.SetValue(latestClr, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(clrPath, true)
                ?.DeleteValue(latestClr, false);
            Registry.LocalMachine.OpenSubKey(clrWowPath, true)
                ?.DeleteValue(latestClr, false);
        }

        /// <summary>
        /// Set Print Screen folder state.
        /// </summary>
        /// <param name="state">Print Screen folder state.</param>
        public static void WinPrtScrFolder(int state)
        {
            var userShellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
            var prtScrGuid = "{B7BEDE81-DF94-4682-A7D8-57A52620B86F}";

            if (state.Equals(1))
            {
                var desktopPath = Registry.CurrentUser.OpenSubKey(userShellPath)
                    ?.GetValue("Desktop") as string;
                Registry.CurrentUser.OpenSubKey(userShellPath, true)
                ?.SetValue(prtScrGuid, desktopPath!, RegistryValueKind.ExpandString);
                return;
            }

            Registry.CurrentUser.OpenSubKey(userShellPath, true)
                ?.DeleteValue(prtScrGuid, false);
        }

        /// <summary>
        /// Set recommended troubleshooting state.
        /// </summary>
        /// <param name="state">Recommended troubleshooting state.</param>
        public static void RecommendedTroubleshooting(int state)
        {
            var collectionPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection";
            var dataPath = "Software\\Policies\\Microsoft\\Windows\\DataCollection";
            var diagPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack";
            var mitigationPath = "Software\\Microsoft\\WindowsMitigation";
            var reportingPath = "Software\\Microsoft\\Windows\\Windows Error Reporting";
            Registry.LocalMachine.OpenSubKey(dataPath, true)
                ?.DeleteValue("AllowTelemetry", false);
            Registry.LocalMachine.OpenSubKey(collectionPath, true)
                ?.DeleteValue("MaxTelemetryAllowed", false);
            Registry.CurrentUser.OpenSubKey(diagPath, true)
                ?.DeleteValue("ShowedToastAtLevel", false);
            using var queueReportingTask = ScheduledTaskService.GetTaskOrDefault("Microsoft\\Windows\\Windows Error Reporting\\QueueReporting");
            ScheduledTaskService.SetState(queueReportingTask, true);
            Registry.CurrentUser.OpenSubKey(reportingPath, true)
                ?.DeleteValue("Disabled", false);
            using var werService = new System.ServiceProcess.ServiceController("WerSvc");
            OsService.SetServiceStartMode(werService, ServiceStartMode.Manual);
            werService.TryStart();
            Registry.LocalMachine.OpenOrCreateSubKey(mitigationPath)
                .SetValue("UserPreference", state.Equals(1) ? 3 : 2, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set folders launch separate process state.
        /// </summary>
        /// <param name="enable">Folders launch separate process state.</param>
        public static void FoldersLaunchSeparateProcess(bool enable)
        {
            var advancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
            Registry.CurrentUser.OpenSubKey(advancedPath, true)
                ?.SetValue("SeparateProcess", enable ? 1 : 0, RegistryValueKind.DWord);
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
                var pageX32Path = "Software\\Classes\\Typelib\\{8cec5860-07a1-11d9-b15e-000d56bfe6ee}";
                Registry.CurrentUser.DeleteSubKeyTree(pageX32Path, false);
                return;
            }

            var pageX64Path = "Software\\Classes\\Typelib\\{8cec5860-07a1-11d9-b15e-000d56bfe6ee}\\1.0\\0\\win64";
            Registry.CurrentUser.OpenOrCreateSubKey(pageX64Path)
                .SetValue(string.Empty, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set Num Lock state.
        /// </summary>
        /// <param name="enable">Num Lock state.</param>
        public static void NumLock(bool enable)
        {
            var keyboardPath = ".DEFAULT\\Control Panel\\Keyboard";
            Registry.Users.OpenSubKey(keyboardPath, true)
                ?.SetValue("InitialKeyboardIndicators", $"{(enable ? "2147483650" : "2147483648")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Caps Lock state.
        /// </summary>
        /// <param name="enable">Caps Lock state.</param>
        public static void CapsLock(bool enable)
        {
            var keyboardPath = "System\\CurrentControlSet\\Control\\Keyboard Layout";
            var scanMap = "Scancode Map";
            Registry.CurrentUser.OpenSubKey("Keyboard Layout", true)
                ?.DeleteValue("Attributes", false);

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(keyboardPath, true)?.DeleteValue(scanMap, false);
                return;
            }

            Registry.LocalMachine.OpenSubKey(keyboardPath, true)?.SetValue(scanMap, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 58, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Set sticky shift state.
        /// </summary>
        /// <param name="enable">Sticky shift state.</param>
        public static void StickyShift(bool enable)
        {
            var stickyPath = "Control Panel\\Accessibility\\StickyKeys";
            Registry.CurrentUser.OpenSubKey(stickyPath, true)
                ?.SetValue("Flags", $"{(enable ? "510" : "506")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set autoplay state.
        /// </summary>
        /// <param name="enable">Autoplay state.</param>
        public static void Autoplay(bool enable)
        {
            var autoplayPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers";
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
            var noAutoRun = "NoDriveTypeAutoRun";
            GroupPolicyService.ClearRegistryCache(explorerPath, noAutoRun, Registry.LocalMachine, Registry.CurrentUser);
            GroupPolicyService.ClearLocalCache(explorerPath, noAutoRun, LGPOScope.Computer, LGPOScope.User);
            Registry.CurrentUser.OpenSubKey(autoplayPath, true)
                ?.SetValue("DisableAutoplay", enable ? 0 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set thumbnail cache state.
        /// </summary>
        /// <param name="enable">Thumbnail cache state.</param>
        public static void ThumbnailCacheRemoval(bool enable)
        {
            var autorun = "Autorun";
            var cacheX32Path = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache";
            var cacheX64Path = "Software\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache";
            Registry.LocalMachine.OpenSubKey(cacheX32Path, true)
                ?.SetValue(autorun, enable ? 3 : 0, RegistryValueKind.DWord);
            Registry.LocalMachine.OpenSubKey(cacheX64Path, true)
                ?.SetValue(autorun, enable ? 3 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set save restartable apps state.
        /// </summary>
        /// <param name="enable">Restartable apps state.</param>
        public static void SaveRestartableApps(bool enable)
        {
            var logonPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";
            Registry.CurrentUser.OpenSubKey(logonPath, true)
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
            var activeScheme = "ActivePowerScheme";
            var arguments = $"/SETACTIVE {(state.Equals(1) ? "SCHEME_MIN" : "SCHEME_BALANCED")}";
            var powerConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe");
            var settingsPath = "Software\\Policies\\Microsoft\\Power\\PowerSettings";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, settingsPath, activeScheme);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, settingsPath, activeScheme);
            _ = ProcessService.WaitForExit(powerConfig, arguments);
        }

        /// <summary>
        /// Set RKN bypass state.
        /// </summary>
        /// <param name="enable">RKN bypass state.</param>
        public static void RKNBypass(bool enable)
        {
            var settingsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            var autoUrl = "AutoConfigURL";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(settingsPath, true)
                    ?.SetValue(autoUrl, "https://p.thenewone.lol:8443/proxy.pac", RegistryValueKind.String);
                return;
            }

            Registry.CurrentUser.OpenSubKey(settingsPath, true)
                ?.DeleteValue(autoUrl, false);
        }

        /// <summary>
        /// Set registry backup state.
        /// </summary>
        /// <param name="enable">Registry backup state.</param>
        public static void RegistryBackup(bool enable)
        {
            var configurationPath = "System\\CurrentControlSet\\Control\\Session Manager\\Configuration Manager";
            var enableBackup = "EnablePeriodicBackup";

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(configurationPath, true)
                    ?.SetValue(enableBackup, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(configurationPath, true)
                ?.DeleteValue(enableBackup, false);
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
            var consolePath = "Console\\%%Startup";
            var consoleGuid = "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}";

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
                                Registry.CurrentUser.OpenOrCreateSubKey(consolePath).SetValue("DelegationConsole", key, RegistryValueKind.String);
                                break;
                            case 1:
                                Registry.CurrentUser.OpenOrCreateSubKey(consolePath).SetValue("DelegationTerminal", key, RegistryValueKind.String);
                                break;
                            default:
                                break;
                        }
                    });
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(consolePath).SetValue("DelegationConsole", consoleGuid, RegistryValueKind.String);
            Registry.CurrentUser.OpenSubKey(consolePath, true)?.SetValue("DelegationTerminal", consoleGuid, RegistryValueKind.String);
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
                var latestRelease = RedistributablePackageService.GetPackageRelease<NetRelease>("https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json");
                var releaseName = $"windowsdesktop-runtime-{latestRelease.Version}-win-x64.exe";
                var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
                var downloadFolder = Registry.CurrentUser.OpenSubKey(shellPath)?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, releaseName);
                var downloadUrl = $"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/{latestRelease.Version}/{releaseName}";
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
                var latestRelease = RedistributablePackageService.GetPackageRelease<NetRelease>("https://builds.dotnet.microsoft.com/dotnet/release-metadata/9.0/releases.json");
                var releaseName = $"windowsdesktop-runtime-{latestRelease.Version}-win-x64.exe";
                var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
                var downloadFolder = Registry.CurrentUser.OpenSubKey(shellPath)?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, releaseName);
                var downloadUrl = $"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/{latestRelease.Version}/{releaseName}";
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
                var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
                var downloadFolder = Registry.CurrentUser.OpenSubKey(shellPath)?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, "VC_redist.x86.exe");
                HttpService.DownloadFile("https://aka.ms/vs/17/release/VC_redist.x86.exe", offlineInstaller);
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
                var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
                var downloadFolder = Registry.CurrentUser.OpenSubKey(shellPath)?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
                var offlineInstaller = Path.Combine(downloadFolder!, "VC_redist.x64.exe");
                HttpService.DownloadFile("https://aka.ms/vs/17/release/VC_redist.x64.exe", offlineInstaller);
                ProcessService.WaitForExit(offlineInstaller, "/install /passive /norestart");
                File.Delete(offlineInstaller);
                RedistributablePackageService.DeleteInstallerLogs("dd_vcredist_amd64_*.log");
            }
        }

        /// <summary>
        /// Set HEVC state.
        /// </summary>
        /// <param name="enable">HEVC state.</param>
        public static void HEVC(bool enable)
        {
            if (enable)
            {
                var foldersPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders";
                var downloadFolder = Registry.CurrentUser.OpenSubKey(foldersPath)
                    ?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string ?? Environment.GetEnvironmentVariable("TEMP");
                var appxFile = $"{downloadFolder}\\Microsoft.HEVCVideoExtension_8wekyb3d8bbwe.appx";
                HttpService.DownloadHEVCAppxAsync(appxFile)
                    .Wait();
                AppxPackagesService.InstallFromFileAsync(appxFile)
                    .Wait();
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
            var startupPath = "Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\SystemAppData\\Microsoft.549981C3F5F10_8wekyb3d8bbwe\\CortanaStartupId";
            Registry.ClassesRoot.OpenSubKey(startupPath, true)
                ?.SetValue("State", enable ? 2 : 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Xbox game bar state.
        /// </summary>
        /// <param name="enable">Xbox game bar state.</param>
        public static void XboxGameBar(bool enable)
        {
            var gameBarMode = enable ? 1 : 0;
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", true)
                ?.SetValue("AppCaptureEnabled", gameBarMode, RegistryValueKind.DWord);
            Registry.CurrentUser.OpenSubKey("System\\GameConfigStore", true)
                ?.SetValue("GameDVR_Enabled", gameBarMode, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set Xbox game tips state.
        /// </summary>
        /// <param name="enable">Xbox game tips state.</param>
        public static void XboxGameTips(bool enable)
        {
            var startupPanelMode = enable ? 1 : 0;
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\GameBar", true)
                ?.SetValue("ShowStartupPanel", startupPanelMode, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set GPU scheduling state.
        /// </summary>
        /// <param name="enable">GPU scheduling state.</param>
        public static void GPUScheduling(bool enable)
        {
            var schedulingMode = enable ? 2 : 1;
            Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers")
                ?.SetValue("HwSchMode", schedulingMode, RegistryValueKind.DWord);
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
            _ = PowerShellService.Invoke($"Set-MpPreference -EnableNetworkProtection {(enable ? "enable" : "Disabled")}");
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
            var processCreationEnabled = "ProcessCreationIncludeCmdLine_Enabled";
            var viewerXml = $"{Environment.GetEnvironmentVariable("ALLUSERSPROFILE")}\\Microsoft\\Event Viewer\\Views\\ProcessCreation.xml";
            var viewerAudit = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit";
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

            if (enable)
            {
                _ = PowerShellService.Invoke($"auditpol /set /subcategory:\"{viewerGuid}\" /success:enable /failure:enable");
                Registry.LocalMachine.OpenSubKey(viewerAudit, true)?.SetValue(processCreationEnabled, 1, RegistryValueKind.DWord);
                FileService.Save(file: viewerXml, content: xml, encoding: Encoding.Default);
                GroupPolicyService.ClearLocalCache(LGPOScope.Computer, viewerAudit, processCreationEnabled, "DWORD", "1");
                return;
            }

            if (!CommonDataService.IsWindows11)
            {
                _ = PowerShellService.Invoke($"auditpol / set / subcategory:\"{viewerGuid}\" / success:disable / failure:disable");
            }

            Registry.LocalMachine.OpenSubKey(viewerAudit, true)?.DeleteValue(processCreationEnabled, false);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, viewerAudit, processCreationEnabled);
            File.Delete(viewerXml);
        }

        /// <summary>
        /// Set Windows PowerShell modules logging state.
        /// </summary>
        /// <param name="enable">PowerShell modules logging state.</param>
        public static void PowerShellModulesLogging(bool enable)
        {
            var enableLogging = "EnableModuleLogging";
            var loggingPath = "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging";
            var namesPath = $"{loggingPath}\\ModuleNames";

            if (enable)
            {
                Registry.LocalMachine.OpenOrCreateSubKey(namesPath)
                    .SetValue("*", "*");
                Registry.LocalMachine.OpenSubKey(loggingPath, true)
                    ?.SetValue(enableLogging, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(loggingPath, true)
                ?.DeleteValue(enableLogging, false);
            Registry.LocalMachine.OpenSubKey(namesPath, true)
                ?.DeleteValue("*", false);
        }

        /// <summary>
        /// Set Windows PowerShell scripts logging state.
        /// </summary>
        /// <param name="enable">PowerShell scripts logging state.</param>
        public static void PowerShellScriptsLogging(bool enable)
        {
            var loggingPath = "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging";
            var enableLogging = "EnableScriptBlockLogging";

            if (enable)
            {
                Registry.LocalMachine.OpenOrCreateSubKey(loggingPath)
                    .SetValue(enableLogging, 1, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(loggingPath, true)
                ?.DeleteValue(enableLogging, false);
        }

        /// <summary>
        /// Set Windows SmartScreen state.
        /// </summary>
        /// <param name="enable">Windows SmartScreen state.</param>
        public static void AppsSmartScreen(bool enable)
        {
            var explorerPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            Registry.LocalMachine.OpenSubKey(explorerPath, true)
                ?.SetValue("SmartScreenEnabled", $"{(enable ? "Warn" : "Off")}", RegistryValueKind.String);
        }

        /// <summary>
        /// Set Windows save zone state.
        /// </summary>
        /// <param name="enable">Windows save zone state.</param>
        public static void SaveZoneInformation(bool enable)
        {
            var attachmentsPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments";
            var zoneInformation = "SaveZoneInformation";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, attachmentsPath, zoneInformation);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, attachmentsPath, zoneInformation);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(attachmentsPath, true)
                    ?.DeleteValue(zoneInformation, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(attachmentsPath)
                .SetValue(zoneInformation, 1, RegistryValueKind.DWord);
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
            var lsaPath = "System\\CurrentControlSet\\Control\\Lsa";
            var runPPL = "RunAsPPL";
            var runPPLBoot = "RunAsPPLBoot";
            var systemPath = "SOFTWARE\\Policies\\Microsoft\\Windows\\System";
            GroupPolicyService.ClearRegistryCache(Registry.LocalMachine, systemPath, runPPL);
            GroupPolicyService.ClearLocalCache(LGPOScope.Computer, systemPath, runPPL);

            if (enable)
            {
                Registry.LocalMachine.OpenSubKey(lsaPath, true)
                    ?.SetValue(runPPL, 2, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(lsaPath, true)
                    ?.SetValue(runPPLBoot, 2, RegistryValueKind.DWord);
                return;
            }

            Registry.LocalMachine.OpenSubKey(lsaPath, true)
                ?.DeleteValue(runPPL, false);
            Registry.LocalMachine.OpenSubKey(lsaPath, true)
                ?.DeleteValue(runPPLBoot, false);
        }

        /// <summary>
        /// Set "Extract all" item in the Windows Installer (.msi) context menu state.
        /// </summary>
        /// <param name="enable">"Extract all" item state.</param>
        public static void MSIExtractContext(bool enable)
        {
            var extractPath = "Msi.Package\\shell\\Extract";

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey($"{extractPath}\\Command")
                    .SetValue(string.Empty, "msiexec.exe /a \"%1\" /qb TARGETDIR=\"%1 extracted\"", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey(extractPath, true)
                    ?.SetValue("MUIVerb", "@shell32.dll,-37514", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey(extractPath, true)
                    ?.SetValue("Icon", "shell32.dll,-16817", RegistryValueKind.String);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(extractPath, false);
        }

        /// <summary>
        /// Set "Install" item in the Cabinet archives (.cab) context menu state.
        /// </summary>
        /// <param name="enable">"Install" item state.</param>
        public static void CABInstallContext(bool enable)
        {
            var runasPath = "CABFolder\\Shell\\runas";

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey($"{runasPath}\\Command")
                    .SetValue(string.Empty, "cmd /c DISM.exe /Online /Add-Package /PackagePath:\"%1\" /NoRestart & pause", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey(runasPath, true)
                    ?.SetValue("MUIVerb", "@shell32.dll,-10210", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey(runasPath, true)
                    ?.SetValue("HasLUAShield", string.Empty, RegistryValueKind.String);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(runasPath, false);
        }

        /// <summary>
        /// Set "Cast to Device" item in the media files and folders context menu state.
        /// </summary>
        /// <param name="enable">"Cast to Device" item state.</param>
        public static void CastToDeviceContext(bool enable)
        {
            var castGuid = "{7AD84985-87B4-4a16-BE58-8B72A5B390F7}";
            var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            Registry.LocalMachine.OpenSubKey(shellPath, true)
                ?.DeleteValue(castGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(shellPath, true)
                    ?.DeleteValue(castGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(shellPath)
                .SetValue(castGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Share" context menu item state.
        /// </summary>
        /// <param name="enable">"Share" item state.</param>
        public static void ShareContext(bool enable)
        {
            var shareGuid = "{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}";
            var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            Registry.LocalMachine.OpenSubKey(shellPath, true)
                ?.DeleteValue(shareGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(shellPath, true)
                    ?.DeleteValue(shareGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(shellPath)
                .SetValue(shareGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Clipchamp" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Clipchamp" item state.</param>
        public static void EditWithClipchampContext(bool enable)
        {
            var champGuid = "{8AB635F8-9A67-4698-AB99-784AD929F3B4}";
            var champPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            Registry.LocalMachine.OpenSubKey(champPath, true)
                ?.DeleteValue(champGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(champPath, true)
                    ?.DeleteValue(champGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(champPath)
                .SetValue(champGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Photos" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Photos" item state.</param>
        public static void EditWithPhotosContext(bool enable)
        {
            var photosGuid = "{BFE0E2A4-C70C-4AD7-AC3D-10D1ECEBB5B4}";
            var photosPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            Registry.LocalMachine.OpenSubKey(photosPath, true)
                ?.DeleteValue(photosGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(photosPath, true)
                    ?.DeleteValue(photosPath, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(photosPath)
                .SetValue(photosGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit With Paint Context" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit With Paint Context" item state.</param>
        public static void EditWithPaintContext(bool enable)
        {
            var paintGuid = "{2430F218-B743-4FD6-97BF-5C76541B4AE9}";
            var paintPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            Registry.LocalMachine.OpenSubKey(paintPath, true)
                ?.DeleteValue(paintGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(paintPath, true)
                    ?.DeleteValue(paintGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(paintPath)
                .SetValue(paintGuid, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Edit with Paint 3D" item in the media files context menu state.
        /// </summary>
        /// <param name="enable">"Edit with Paint 3D" item state.</param>
        public static void EditWithPaint3DContext(bool enable)
        {
            var paintAccess = "ProgrammaticAccessOnly";
            new List<string>()
            {
                ".bmp", ".gif", ".jpe", ".jpeg", ".jpg", ".png", ".tif", ".tiff",
            }
            .ForEach(file =>
            {
                var filePath = $"SystemFileAssociations\\{file}\\Shell\\3D Edit";

                if (enable)
                {
                    Registry.ClassesRoot.OpenSubKey(filePath, true)
                        ?.DeleteValue(paintAccess, false);
                    return;
                }

                Registry.ClassesRoot.OpenSubKey(filePath, true)
                    ?.SetValue(paintAccess, string.Empty, RegistryValueKind.String);
            });
        }

        /// <summary>
        /// Set "Print" item in the .bat and .cmd files context menu state.
        /// </summary>
        /// <param name="enable">"Print" item state.</param>
        public static void PrintCMDContext(bool enable)
        {
            var accessOnly = "ProgrammaticAccessOnly";
            var batPrint = "batfile\\shell\\print";
            var cmdPrint = "cmdfile\\shell\\print";

            if (enable)
            {
                Registry.ClassesRoot.OpenSubKey(batPrint, true)
                    ?.DeleteValue(accessOnly, false);
                Registry.ClassesRoot.OpenSubKey(cmdPrint, true)
                    ?.DeleteValue(accessOnly, false);
                return;
            }

            Registry.ClassesRoot.OpenSubKey(batPrint, true)
                ?.SetValue(accessOnly, string.Empty, RegistryValueKind.String);
            Registry.ClassesRoot.OpenSubKey(cmdPrint, true)
                ?.SetValue(accessOnly, string.Empty, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Include in Library" item in the folders and drives context menu state.
        /// </summary>
        /// <param name="enable">"Include in Library" item state.</param>
        public static void IncludeInLibraryContext(bool enable)
        {
            var disableGuid = "-{3dad6c5d-2167-4cae-9914-f99e41c12cfa}";
            var enableGuid = "{3dad6c5d-2167-4cae-9914-f99e41c12cfa}";
            var libraryPath = "Folder\\ShellEx\\ContextMenuHandlers\\Library Location";
            Registry.ClassesRoot.OpenSubKey(libraryPath, true)
                ?.SetValue(string.Empty, enable ? enableGuid : disableGuid, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Send to" item in the folders context menu state.
        /// </summary>
        /// <param name="enable">"Send to" item state.</param>
        public static void SendToContext(bool enable)
        {
            var disableGuid = "-{7BA4C740-9E81-11CF-99D3-00AA004AE837}";
            var enableGuid = "{7BA4C740-9E81-11CF-99D3-00AA004AE837}";
            var sendPath = "AllFilesystemObjects\\shellex\\ContextMenuHandlers\\SendTo";
            Registry.ClassesRoot.OpenSubKey(sendPath, true)
                ?.SetValue(string.Empty, enable ? enableGuid : disableGuid, RegistryValueKind.String);
        }

        /// <summary>
        /// Set "Bitmap image" item in the "New" context menu state.
        /// </summary>
        /// <param name="enable">"Bitmap image" item state.</param>
        public static void BitmapImageNewContext(bool enable)
        {
            var shellPath = ".bmp\\ShellNew";

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(shellPath)
                    .SetValue("ItemName", "@%SystemRoot%\\System32\\mspaint.exe,-59414", RegistryValueKind.ExpandString);
                Registry.ClassesRoot.OpenSubKey(shellPath, true)
                    ?.SetValue("NullFile", string.Empty, RegistryValueKind.String);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(shellPath, false);
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
                Registry.ClassesRoot.OpenOrCreateSubKey(rtfShellPath)
                    .SetValue("Data", @"{\rtf1}", RegistryValueKind.String);
                Registry.ClassesRoot.OpenSubKey(rtfShellPath, true)
                    ?.SetValue("ItemName", "@%ProgramFiles%\\Windows NT\\Accessories\\WORDPAD.EXE,-213", RegistryValueKind.ExpandString);
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
            var zipPath = ".zip\\CompressedFolder\\ShellNew";
            var zipContext = new byte[] { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            if (enable)
            {
                Registry.ClassesRoot.OpenOrCreateSubKey(zipPath)
                    .SetValue("Data", zipContext, RegistryValueKind.Binary);
                Registry.ClassesRoot.OpenSubKey(zipPath, true)
                    ?.SetValue("ItemName", "@%SystemRoot%\\System32\\zipfldr.dll,-10194", RegistryValueKind.ExpandString);
                return;
            }

            Registry.ClassesRoot.DeleteSubKeyTree(zipPath, false);
        }

        /// <summary>
        /// Set "Open", "Print", and "Edit" context menu items available when selecting more than 15 files state.
        /// </summary>
        /// <param name="enable">"Open", "Print", and "Edit" context menu items state.</param>
        public static void MultipleInvokeContext(bool enable)
        {
            var multiplePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";
            var multipleContext = "MultipleInvokePromptMinimum";

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(multiplePath, true)
                    ?.SetValue(multipleContext, 300, RegistryValueKind.DWord);
                return;
            }

            Registry.CurrentUser.OpenSubKey(multiplePath, true)
                ?.DeleteValue(multipleContext, false);
        }

        /// <summary>
        /// Set "Look for an app in the Microsoft Store" items in the "Open with" dialog state.
        /// </summary>
        /// <param name="enable">"Look for an app in the Microsoft Store" items state.</param>
        public static void UseStoreOpenWith(bool enable)
        {
            var storePath = "Software\\Policies\\Microsoft\\Windows\\Explorer";
            var noStore = "NoUseStoreOpenWith";

            Registry.LocalMachine.OpenSubKey(storePath, true)
                ?.DeleteValue(noStore, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(storePath, true)
                    ?.DeleteValue(noStore, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(storePath)
                .SetValue(noStore, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Set "Open in Windows Terminal" item in the folders context menu state.
        /// </summary>
        /// <param name="enable">"Open in Windows Terminal" item state.</param>
        public static void OpenWindowsTerminalContext(bool enable)
        {
            var shellPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked";
            var shellGuid = "{9F156763-7844-4DC4-B2B1-901F640F5155}";

            Registry.LocalMachine.OpenSubKey(shellPath, true)
                ?.DeleteValue(shellGuid, false);

            if (enable)
            {
                Registry.CurrentUser.OpenSubKey(shellPath, true)
                    ?.DeleteValue(shellGuid, false);
                return;
            }

            Registry.CurrentUser.OpenOrCreateSubKey(shellPath)
                .SetValue(shellGuid, string.Empty, RegistryValueKind.String);
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
            var shellPath = "SystemFileAssociations\\image\\shell\\edit";
            var accessOnly = "ProgrammaticAccessOnly";

            if (enable)
            {
                Registry.ClassesRoot.OpenSubKey(shellPath, true)
                    ?.DeleteValue(accessOnly, false);
                return;
            }

            Registry.ClassesRoot.OpenOrCreateSubKey(shellPath)
                .SetValue(accessOnly, string.Empty, RegistryValueKind.String);
        }
    }
}
