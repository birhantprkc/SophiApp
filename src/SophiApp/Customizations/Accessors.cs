// <copyright file="Accessors.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Customizations
{
    using Microsoft.Win32;
    using Microsoft.Win32.TaskScheduler;
    using NetFwTypeLib;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;
    using System;
    using System.Globalization;
    using System.ServiceProcess;
    using System.Text;
    using Windows.ApplicationModel;

    /// <summary>
    /// Get the OS settings.
    /// </summary>
    public static class Accessors
    {
        private static readonly IAppxPackagesService AppxPackagesService = App.GetService<IAppxPackagesService>();
        private static readonly ICommonDataService CommonDataService = App.GetService<ICommonDataService>();
        private static readonly IRedistributablePackageService RedistributablePackageService = App.GetService<IRedistributablePackageService>();
        private static readonly IFirewallService FirewallService = App.GetService<IFirewallService>();
        private static readonly IHttpService HttpService = App.GetService<IHttpService>();
        private static readonly IInstrumentationService InstrumentationService = App.GetService<IInstrumentationService>();
        private static readonly IOneDriveService OneDriveService = App.GetService<IOneDriveService>();
        private static readonly IOsService OsService = App.GetService<IOsService>();
        private static readonly IPowerShellService PowerShellService = App.GetService<IPowerShellService>();
        private static readonly IProcessService ProcessService = App.GetService<IProcessService>();
        private static readonly IScheduledTaskService ScheduledTaskService = App.GetService<IScheduledTaskService>();
        private static readonly IUpdateService UpdateService = App.GetService<IUpdateService>();
        private static readonly IXmlService XmlService = App.GetService<IXmlService>();

        /// <summary>
        /// Get DiagTrack service state.
        /// </summary>
        public static bool DiagTrackService()
        {
            var diagTrackService = new System.ServiceProcess.ServiceController("DiagTrack");
            var firewallRule = FirewallService.GetGroupRules("DiagTrack").First();

            if (diagTrackService.StartType == ServiceStartMode.Disabled && firewallRule.Enabled && firewallRule.Action == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get Windows feature "Diagnostic data level" state.
        /// </summary>
        public static int DiagnosticDataLevel()
        {
            var allowTelemetry = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\DataCollection")?.GetValue("AllowTelemetry") as int? ?? -1;
            var maxTelemetryAllowed = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection")?.GetValue("MaxTelemetryAllowed") as int? ?? -1;
            var showedToastLevel = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack")?.GetValue("ShowedToastAtLevel") as int? ?? -1;
            return allowTelemetry.Equals(1) && maxTelemetryAllowed.Equals(1) && showedToastLevel.Equals(1) ? 2 : 1;
        }

        /// <summary>
        /// Get Windows feature "Error reporting" state.
        /// </summary>
        public static bool ErrorReporting()
        {
            var queueReportingTask = ScheduledTaskService.GetTaskOrDefault("Microsoft\\Windows\\Windows Error Reporting\\QueueReporting") ?? throw new InvalidOperationException("Failed to find a QueueReporting scheduled task");
            var disableErrorReporting = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Windows Error Reporting")?.GetValue("Disabled") as int? ?? -1;
            return !(queueReportingTask.State == TaskState.Disabled && disableErrorReporting.Equals(1) && new System.ServiceProcess.ServiceController("WerSvc").StartType == ServiceStartMode.Disabled);
        }

        /// <summary>
        /// Get Windows feature "Feedback frequency" state.
        /// </summary>
        public static int FeedbackFrequency()
        {
            var siufPeriod = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Siuf\\Rules")?.GetValue("NumberOfSIUFInPeriod") as int? ?? -1;
            return siufPeriod.Equals(0) ? 2 : 1;
        }

        /// <summary>
        /// Get telemetry scheduled tasks state.
        /// </summary>
        public static bool ScheduledTasks()
        {
            var telemetryTasks = new List<Task?>()
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
            };

            return telemetryTasks.TrueForAll(task => task is null)
                ? throw new InvalidOperationException("No scheduled telemetry tasks were found")
                : telemetryTasks.Exists(task => task?.State == TaskState.Ready);
        }

        /// <summary>
        /// Get Windows feature "Sign-in info" state.
        /// </summary>
        public static bool SigninInfo()
        {
            var userSid = InstrumentationService.GetUserSid(Environment.UserName);
            var userArso = Registry.LocalMachine.OpenSubKey($"Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\\UserARSO\\{userSid}")?.GetValue("OptOut") ?? -1;
            return !userArso.Equals(1);
        }

        /// <summary>
        /// Get language list access state.
        /// </summary>
        public static bool LanguageListAccess()
        {
            var httpAcceptLanguage = Registry.CurrentUser.OpenSubKey("Control Panel\\International\\User Profile")?.GetValue("HttpAcceptLanguageOptOut") as int? ?? -1;
            return !httpAcceptLanguage.Equals(1);
        }

        /// <summary>
        /// Get the permission for apps to use advertising ID state.
        /// </summary>
        public static bool AdvertisingID()
        {
            var advertisingInfo = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo")?.GetValue("Enabled") as int? ?? -1;
            return !advertisingInfo.Equals(0);
        }

        /// <summary>
        /// Get the Windows welcome experiences state.
        /// </summary>
        public static bool WindowsWelcomeExperience()
        {
            var subscribedContent = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager")?.GetValue("SubscribedContent-310093Enabled") as int? ?? -1;
            return !subscribedContent.Equals(0);
        }

        /// <summary>
        /// Get Windows tips state.
        /// </summary>
        public static bool WindowsTips()
        {
            var subscribedContent = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager")?.GetValue("SubscribedContent-338389Enabled") as int? ?? -1;
            return !subscribedContent.Equals(0);
        }

        /// <summary>
        /// Get the suggested content in the Settings app state.
        /// </summary>
        public static bool SettingsSuggestedContent()
        {
            var subscribedContent = new List<int>()
            {
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager")?.GetValue("SubscribedContent-338393Enabled") as int? ?? -1,
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager")?.GetValue("SubscribedContent-353694Enabled") as int? ?? -1,
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager")?.GetValue("SubscribedContent-353696Enabled") as int? ?? -1,
            }
            .TrueForAll(subscribed => subscribed.Equals(0));
            return !subscribedContent;
        }

        /// <summary>
        /// Get the automatic installing suggested apps state.
        /// </summary>
        public static bool AppsSilentInstalling()
        {
            var appsIsSilentInstalled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager")?.GetValue("SilentInstalledAppsEnabled") as int? ?? -1;
            return !appsIsSilentInstalled.Equals(0);
        }

        /// <summary>
        /// Get the Windows feature "Whats New" state.
        /// </summary>
        public static bool WhatsNewInWindows()
        {
            var scoobeSettingIsEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement")?.GetValue("ScoobeSystemSettingEnabled") as int? ?? -1;
            return !scoobeSettingIsEnabled.Equals(0);
        }

        /// <summary>
        /// Get Windows feature "Tailored experiences" state.
        /// </summary>
        public static bool TailoredExperiences()
        {
            var tailoredExperiencesIsEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Privacy")?.GetValue("TailoredExperiencesWithDiagnosticDataEnabled") as int? ?? -1;
            return !tailoredExperiencesIsEnabled.Equals(0);
        }

        /// <summary>
        /// Get Windows feature "Bing search" state.
        /// </summary>
        public static bool BingSearch()
        {
            var searchBoxIsDisabled = Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer")?.GetValue("DisableSearchBoxSuggestions") as int? ?? -1;
            return !searchBoxIsDisabled.Equals(1);
        }

        /// <summary>
        /// Get Start menu recommendations state.
        /// </summary>
        public static bool StartRecommendationsTips()
        {
            var irisRecommendations = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("Start_IrisRecommendations") as int? ?? -1;
            return !irisRecommendations.Equals(0);
        }

        /// <summary>
        /// Get Start Menu notifications state.
        /// </summary>
        public static bool StartAccountNotifications()
        {
            var accountNotifications = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("Start_AccountNotifications") as int? ?? -1;
            return !accountNotifications.Equals(0);
        }

        /// <summary>
        /// Get the "This PC" icon on Desktop state.
        /// </summary>
        public static bool ThisPC()
        {
            var panelValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel")?.GetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}") as int? ?? -1;
            return panelValue.Equals(0);
        }

        /// <summary>
        /// Get item check boxes state.
        /// </summary>
        public static bool CheckBoxes()
        {
            var checkValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("AutoCheckSelect") as int? ?? -1;
            return checkValue.Equals(1);
        }

        /// <summary>
        /// Get hidden files, folders, and drives state.
        /// </summary>
        public static bool HiddenItems()
        {
            var itemsValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("Hidden") as int? ?? -1;
            return itemsValue.Equals(1);
        }

        /// <summary>
        /// Get file name extensions visibility state.
        /// </summary>
        public static bool FileExtensions()
        {
            var extensionsValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("HideFileExt") as int? ?? -1;
            return extensionsValue.Equals(0);
        }

        /// <summary>
        /// Get folder merge conflicts state.
        /// </summary>
        public static bool MergeConflicts()
        {
            var mergeValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("HideMergeConflicts") as int? ?? -1;
            return mergeValue.Equals(0);
        }

        /// <summary>
        /// Get how to open File Explorer.
        /// </summary>
        public static int OpenFileExplorerTo()
        {
            var fileValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("LaunchTo") as int? ?? -1;
            return fileValue.Equals(1) ? 1 : 2;
        }

        /// <summary>
        /// Get File Explorer ribbon state.
        /// </summary>
        public static int FileExplorerRibbon()
        {
            var ribbonValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Ribbon")?.GetValue("MinimizedStateTabletModeOff") as int? ?? -1;
            return ribbonValue.Equals(0) ? 1 : 2;
        }

        /// <summary>
        /// Get File Explorer compact mode state.
        /// </summary>
        public static bool FileExplorerCompactMode()
        {
            var compactModeValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("UseCompactMode") as int? ?? -1;
            return !compactModeValue.Equals(0);
        }

        /// <summary>
        /// Get File Explorer provider notification visibility state.
        /// </summary>
        public static bool OneDriveFileExplorerAd()
        {
            var notificationsValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("ShowSyncProviderNotifications") as int? ?? -1;
            return !notificationsValue.Equals(0);
        }

        /// <summary>
        /// Get snap a window state.
        /// </summary>
        public static bool SnapAssist()
        {
            var snapValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("SnapAssist") as int? ?? -1;
            return !snapValue.Equals(0);
        }

        /// <summary>
        /// Get file transfer dialog box mode.
        /// </summary>
        public static int FileTransferDialog()
        {
            var modeValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\OperationStatusManager")?.GetValue("EnthusiastMode") as int? ?? -1;
            return modeValue.Equals(1) ? 1 : 2;
        }

        /// <summary>
        /// Get recycle bin confirmation dialog state.
        /// </summary>
        public static bool RecycleBinDeleteConfirmation()
        {
            var shellValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer")?.GetValue("ShellState") as byte[] ?? new byte[5];
            return shellValue[4].Equals(51);
        }

        /// <summary>
        /// Get recently used Quick access files state.
        /// </summary>
        public static bool QuickAccessRecentFiles()
        {
            var recentValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer")?.GetValue("ShowRecent") as int? ?? -1;
            return !recentValue.Equals(0);
        }

        /// <summary>
        /// Get frequently used Quick access folders state.
        /// </summary>
        public static bool QuickAccessFrequentFolders()
        {
            var frequentValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer")?.GetValue("ShowFrequent") as int? ?? -1;
            return !frequentValue.Equals(0);
        }

        /// <summary>
        /// Get taskbar alignment state.
        /// </summary>
        public static int TaskbarAlignment()
        {
            var taskbarValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("TaskbarAl") as int? ?? -1;
            return taskbarValue.Equals(1) ? 1 : 2;
        }

        /// <summary>
        /// Get taskbar widgets icon state.
        /// </summary>
        public static bool TaskbarWidgets()
        {
            if (AppxPackagesService.PackageExist("MicrosoftWindows.Client.WebExperience"))
            {
                var taskbarValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("TaskbarDa") as int? ?? -1;
                return !taskbarValue.Equals(0);
            }

            throw new InvalidOperationException("AppX package MicrosoftWindows.Client.WebExperience is not installed");
        }

        /// <summary>
        /// Get Search on the taskbar state.
        /// </summary>
        public static int TaskbarSearchWindows10()
        {
            var smallIconsValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("TaskbarSmallIcons") as int? ?? -1;
            var searchModeValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search")?.GetValue("SearchboxTaskbarMode") as int? ?? -1;

            if (smallIconsValue.Equals(1))
            {
                throw new InvalidOperationException("Small taskbar icons mode is enabled");
            }

            return searchModeValue + 1;
        }

        /// <summary>
        /// Get Search on the taskbar state.
        /// </summary>
        public static int TaskbarSearchWindows11()
        {
            var smallIconsValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("TaskbarSmallIcons") as int? ?? -1;
            var searchModeValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search")?.GetValue("SearchboxTaskbarMode") as int? ?? -1;

            if (smallIconsValue.Equals(1))
            {
                throw new InvalidOperationException("Small taskbar icons mode is enabled");
            }

            return searchModeValue switch
            {
                0 => 1,
                1 => 2,
                2 => 4,
                _ => 3,
            };
        }

        /// <summary>
        /// Get search highlights state.
        /// </summary>
        public static bool SearchHighlightsWindows10()
        {
            var dynamicContent = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Feeds\\DSB")?.GetValue("ShowDynamicContent") as int? ?? -1;
            var dynamicSearch = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings")?.GetValue("IsDynamicSearchBoxEnabled") as int? ?? -1;
            return !(dynamicContent.Equals(0) && dynamicSearch.Equals(0));
        }

        /// <summary>
        /// Get search highlights state.
        /// </summary>
        public static bool SearchHighlightsWindows11()
        {
            var searchEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Search")?.GetValue("BingSearchEnabled") as int? ?? -1;
            var searchSuggestions = Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer")?.GetValue("DisableSearchBoxSuggestions") as int? ?? -1;

            // Checking whether "Ask Copilot" and "Find results in Web" were disabled. They also disable Search Highlights automatically
            if (searchEnabled.Equals(1) || searchSuggestions.Equals(1))
            {
                var blockedKey = searchEnabled.Equals(1) ? "BingSearchEnabled" : "DisableSearchBoxSuggestions";
                throw new InvalidOperationException($"SearchHighlights is already blocked within {blockedKey} registry keys. No need to block search highlights again.");
            }

            var dynamicSearch = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings")?.GetValue("IsDynamicSearchBoxEnabled") as int? ?? -1;
            return !dynamicSearch.Equals(0);
        }

        /// <summary>
        /// Get Cortana button taskbar state.
        /// </summary>
        public static bool CortanaButton()
        {
            if (AppxPackagesService.PackageExist("Microsoft.549981C3F5F10"))
            {
                var buttonValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("ShowCortanaButton") as int? ?? -1;
                return !buttonValue.Equals(0);
            }

            throw new InvalidOperationException("AppX package Microsoft.549981C3F5F10 is not installed");
        }

        /// <summary>
        /// Get taskbar task view button state.
        /// </summary>
        public static bool TaskViewButton()
        {
            var buttonValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("ShowTaskViewButton") as int? ?? -1;
            return !buttonValue.Equals(0);
        }

        /// <summary>
        /// Get News and Interests state.
        /// </summary>
        public static bool NewsInterests()
        {
            var feedsValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Feeds")?.GetValue("ShellFeedsTaskbarViewMode") as int? ?? -1;
            return !feedsValue.Equals(2);
        }

        /// <summary>
        /// Get taskbar people icon state.
        /// </summary>
        public static bool PeopleTaskbar()
        {
            var peopleValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\People")?.GetValue("PeopleBand") as int? ?? -1;
            return !peopleValue.Equals(0);
        }

        /// <summary>
        /// Get Meet Now icon state.
        /// </summary>
        public static bool MeetNow()
        {
            var meetValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3")?.GetValue("Settings") as byte[] ?? new byte[10];
            return !meetValue[9].Equals(128);
        }

        /// <summary>
        /// Get Windows Ink Workspace button state.
        /// </summary>
        public static bool WindowsInkWorkspace()
        {
            var workspaceValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\PenWorkspace")?.GetValue("PenWorkspaceButtonDesiredVisibility") as int? ?? -1;
            return workspaceValue.Equals(1);
        }

        /// <summary>
        /// Get notification area icons state.
        /// </summary>
        public static bool NotificationAreaIcons()
        {
            var trayValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer")?.GetValue("EnableAutoTray") as int? ?? -1;
            return trayValue.Equals(0);
        }

        /// <summary>
        /// Get seconds on the taskbar clock state.
        /// </summary>
        public static bool SecondsInSystemClock()
        {
            var clockValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("ShowSecondsInSystemClock") as int? ?? -1;
            return clockValue.Equals(1);
        }

        /// <summary>
        /// Get taskbar combine state.
        /// </summary>
        public static int TaskbarCombine()
        {
            var levelValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("TaskbarGlomLevel") as int? ?? -1;
            return levelValue.Equals(-1) ? 1 : levelValue + 1;
        }

        /// <summary>
        /// Get end task in taskbar by click state.
        /// </summary>
        public static bool TaskbarEndTask()
        {
            var taskValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings")?.GetValue("TaskbarEndTask") as int? ?? -1;
            return taskValue.Equals(1);
        }

        /// <summary>
        /// Get Control Panel icons view state.
        /// </summary>
        public static int ControlPanelView()
        {
            var viewValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel")?.GetValue("AllItemsIconView") as int? ?? 0;
            var pageValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel")?.GetValue("StartupPage") as int? ?? 0;

            if (viewValue.Equals(0) && pageValue.Equals(0))
            {
                return 1;
            }

            return viewValue.Equals(0) && pageValue.Equals(1) ? 2 : 3;
        }

        /// <summary>
        /// Get Windows color mode state.
        /// </summary>
        public static int WindowsColorMode()
        {
            var themeValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")?.GetValue("SystemUsesLightTheme") as int? ?? -1;
            return themeValue.Equals(0) ? 1 : 2;
        }

        /// <summary>
        /// Get apps color mode state.
        /// </summary>
        public static int AppColorMode()
        {
            var themeValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Feeds")?.GetValue("AppsUseLightTheme") as int? ?? -1;
            return themeValue.Equals(0) ? 1 : 2;
        }

        /// <summary>
        /// Get "New App Installed" indicator state.
        /// </summary>
        public static bool NewAppInstalledNotification()
        {
            var alertValue = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer")?.GetValue("NoNewAppAlert") as int? ?? -1;
            return !alertValue.Equals(1);
        }

        /// <summary>
        /// Get first sign-in animation state.
        /// </summary>
        public static bool FirstLogonAnimation()
        {
            var logonValue = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon")?.GetValue("EnableFirstLogonAnimation") as int? ?? -1;
            return !logonValue.Equals(0);
        }

        /// <summary>
        /// Get JPEG wallpapers quality state.
        /// </summary>
        public static int JPEGWallpapersQuality()
        {
            var qualityValue = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop")?.GetValue("JPEGImportQuality") as int? ?? -1;
            return qualityValue.Equals(100) ? 1 : 2;
        }

        /// <summary>
        /// Get "- Shortcut" suffix state.
        /// </summary>
        public static bool ShortcutsSuffix()
        {
            var shortcutsValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates")?.GetValue("ShortcutNameTemplate") as string ?? string.Empty;
            return !shortcutsValue.Equals("%s.lnk");
        }

        /// <summary>
        /// Get Print screen button state.
        /// </summary>
        public static bool PrtScnSnippingTool()
        {
            var snippingValue = Registry.CurrentUser.OpenSubKey("Control Panel\\Keyboard")?.GetValue("PrintScreenKeyForSnippingEnabled") as int? ?? -1;
            return snippingValue.Equals(1);
        }

        /// <summary>
        /// Get input method for app window state.
        /// </summary>
        public static bool AppsLanguageSwitch()
        {
            return PowerShellService.Invoke<bool>("$((Get-WinLanguageBarOption).IsLegacySwitchingMode)");
        }

        /// <summary>
        /// Get Aero Shake state.
        /// </summary>
        public static bool AeroShaking()
        {
            var shakingValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("DisallowShaking") as int? ?? -1;
            return shakingValue.Equals(0);
        }

        /// <summary>
        /// Get "Windows 11 Cursors Concept" from Jepri Creations state.
        /// </summary>
        public static int Cursors()
        {
            var cursorsScheme = Registry.CurrentUser.OpenSubKey("Control Panel\\Cursors")?.GetValue(string.Empty) as string ?? string.Empty;

            if (cursorsScheme.Equals("W11 Cursor Dark Free by Jepri Creations"))
            {
                return 1;
            }

            if (cursorsScheme.Equals("W11 Cursor Light Free by Jepri Creations"))
            {
                return 2;
            }

            return 3;
        }

        /// <summary>
        /// Get files and folders grouping state.
        /// </summary>
        public static int FolderGroupBy()
        {
            var groupByPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FolderTypes\\{885a186e-a440-4ada-812b-db871b942259}\\TopViews\\{00000000-0000-0000-0000-000000000000}";
            var groupByValue = Registry.CurrentUser.OpenSubKey(groupByPath)?.GetValue("GroupBy") as string ?? string.Empty;
            return groupByValue.Equals("System.Null") ? 1 : 2;
        }

        /// <summary>
        /// Get navigation pane expand state.
        /// </summary>
        public static bool NavigationPaneExpand()
        {
            var paneValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("NavPaneExpandToCurrentFolder") as int? ?? -1;
            return !paneValue.Equals(0);
        }

        /// <summary>
        /// Get Start menu recently added apps state.
        /// </summary>
        public static bool RecentlyAddedApps()
        {
            var appsValue = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer")?.GetValue("HideRecentlyAddedApps") as int? ?? -1;
            return !appsValue.Equals(1);
        }

        /// <summary>
        /// Get Start menu app suggestions state.
        /// </summary>
        public static bool AppSuggestions()
        {
            var contentValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager")?.GetValue("SubscribedContent-338388Enabled") as int? ?? -1;
            return !contentValue.Equals(0);
        }

        /// <summary>
        /// Get Start menu layout state.
        /// </summary>
        public static int StartLayout()
        {
            // Default — 0
            // Show More Pins — 1
            // Show More Recommendations — 2
            var layoutValue = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")?.GetValue("Start_Layout") as int? ?? 0;

            return layoutValue + 1;
        }

        /// <summary>
        /// Get recommended section state.
        /// </summary>
        public static bool StartRecommendedSection()
        {
            var os = CommonDataService.OsProperties;

            if (os.Edition.Contains("Home", StringComparison.InvariantCultureIgnoreCase) || os.Edition.Contains("Core", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("This version Windows is not supported");
            }

            var sectionValue = Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer")?.GetValue("HideRecommendedSection") as int? ?? -1;
            var environmentValue = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\PolicyManager\\Current\\Device\\Education")?.GetValue("IsEducationEnvironment") as int? ?? -1;
            return !sectionValue.Equals(1) && !environmentValue.Equals(1);
        }

        /// <summary>
        /// Get One Drive state.
        /// </summary>
        public static bool OneDrive()
        {
            if (OneDriveService.IsInstalled())
            {
                if (Path.Exists(OneDriveService.GetUserDataFolderOrDefault()))
                {
                    if (OneDriveService.UserIsLogged())
                    {
                        throw new InvalidOperationException("Please log out from OneDrive account before uninstalling the application");
                    }

                    return true;
                }

                throw new InvalidOperationException("A user data folder does not exist");
            }
            else
            {
                if (OneDriveService.SetupFileExist() || HttpService.UrlIsAvailable("https://g.live.com/1rewlive5skydrive/OneDriveProductionV2"))
                {
                    return false;
                }

                throw new InvalidOperationException("OneDriveSetup.exe was not found and there is no Internet access to download OneDrive installer");
            }
        }

        /// <summary>
        /// Get storage sense state.
        /// </summary>
        public static bool StorageSense()
        {
            var storagePolicy01 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy")?.GetValue("01") as int? ?? -1;
            var storagePolicy04 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy")?.GetValue("04") as int? ?? -1;
            var storagePolicy2048 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy")?.GetValue("2048") as int? ?? -1;
            return storagePolicy01.Equals(1) && storagePolicy04.Equals(1) && storagePolicy2048.Equals(30);
        }

        /// <summary>
        /// Get hibernation state.
        /// </summary>
        public static bool Hibernation()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Power")
                ?.GetValue("HibernateEnabled") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get long path limit state.
        /// </summary>
        public static bool Win32LongPathsSupport()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\FileSystem")
                ?.GetValue("LongPathsEnabled") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get BSOD error state.
        /// </summary>
        public static bool BSoDStopError()
        {
            var crashPath = "System\\CurrentControlSet\\Control\\CrashControl";
            var isEnabled = Registry.LocalMachine.OpenSubKey(crashPath)?.GetValue("DisplayParameters") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get administrator approval mode state.
        /// </summary>
        public static int AdminApprovalMode()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System")
                ?.GetValue("ConsentPromptBehaviorAdmin") as int? ?? -1;
            return isEnabled.Equals(0) ? 2 : 1;
        }

        /// <summary>
        /// Get delivery optimization state.
        /// </summary>
        public static bool DeliveryOptimization()
        {
            var isEnabled = Registry.Users.OpenSubKey("S-1-5-20\\Software\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Settings")?.GetValue("DownloadMode") as int? ?? -1;
            return !isEnabled.Equals(0);
        }

        /// <summary>
        /// Get Windows manage default printer state.
        /// </summary>
        public static bool WindowsManageDefaultPrinter()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows")?.GetValue("LegacyDefaultPrinterMode") as int? ?? -1;
            return !isEnabled.Equals(1);
        }

        /// <summary>
        /// Get update Microsoft products state.
        /// </summary>
        public static bool UpdateMicrosoftProducts()
        {
            return UpdateService.HasMicrosoftProductsUpdate();
        }

        /// <summary>
        /// Get restart notification state.
        /// </summary>
        public static bool RestartNotification()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings")?.GetValue("RestartNotificationsAllowed2") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get device restart after update state.
        /// </summary>
        public static bool RestartDeviceAfterUpdate()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings")?.GetValue("IsExpedited") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get active hours restart state.
        /// </summary>
        public static int ActiveHours()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings")
                ?.GetValue("SmartActiveHoursState") as int? ?? -1;
            return isEnabled.Equals(0) ? 2 : 1;
        }

        /// <summary>
        /// Get Windows latest update state.
        /// </summary>
        public static bool WindowsLatestUpdate()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsUpdate\\UX\\Settings")
                ?.GetValue("IsContinuousInnovationOptedIn") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get network adapters power state.
        /// </summary>
        public static bool NetworkAdaptersSavePower()
        {
            return PowerShellService.TurnOffDeviceNetworkAdapterExist()
                ?? throw new InvalidOperationException("There is no network adapter which has an AllowComputerToTurnOffDevice property");
        }

        /// <summary>
        /// Get input method state.
        /// </summary>
        public static int InputMethod()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Control Panel\\International\\User Profile")?.GetValue("InputMethodOverride") as string ?? string.Empty;
            return isEnabled.Equals("0409:00000409") ? 1 : 2;
        }

        /// <summary>
        /// Get installed .NET state.
        /// </summary>
        public static bool LatestInstalledNET()
        {
            var latesClr = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\.NETFramework")?.GetValue("OnlyUseLatestCLR") as int? ?? -1;
            var latesWowClr = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\.NETFramework")?.GetValue("OnlyUseLatestCLR") as int? ?? -1;
            return latesClr.Equals(1) && latesWowClr.Equals(1);
        }

        /// <summary>
        /// Get Print Screen folder state.
        /// </summary>
        public static int WinPrtScrFolder()
        {
            if (OneDriveService.UserIsLogged())
            {
                throw new InvalidOperationException("Please log out from OneDrive account");
            }

            var prtScrPath = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")
                ?.GetValue("{B7BEDE81-DF94-4682-A7D8-57A52620B86F}") as string ?? string.Empty;
            var desktopPath = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")
                ?.GetValue("Desktop") as string;
            return prtScrPath.Equals(desktopPath) ? 1 : 2;
        }

        /// <summary>
        /// Get recommended troubleshooting state.
        /// </summary>
        public static int RecommendedTroubleshooting()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\WindowsMitigation")?.GetValue("UserPreference") as int? ?? -1;
            return isEnabled.Equals(3) ? 1 : 2;
        }

        /// <summary>
        /// Get folders launch separate process state.
        /// </summary>
        public static bool FoldersLaunchSeparateProcess()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")
                ?.GetValue("SeparateProcess") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get reserved storage state.
        /// </summary>
        public static bool ReservedStorage()
        {
            using var reserveKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ReserveManager");
            var miscPolicy = reserveKey?.GetValue("MiscPolicyInfo") as int? ?? -1;
            var passedPolicy = reserveKey?.GetValue("PassedPolicy") as int? ?? -1;
            var shippedReserves = reserveKey?.GetValue("ShippedWithReserves") as int? ?? -1;
            return !(miscPolicy.Equals(2) && passedPolicy.Equals(0) && shippedReserves.Equals(1));
        }

        /// <summary>
        /// Get help page state.
        /// </summary>
        public static bool F1HelpPage()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Software\\Classes\\Typelib\\{8cec5860-07a1-11d9-b15e-000d56bfe6ee}\\1.0\\0\\win64")?.GetValue(string.Empty) as string;
            return isEnabled is null;
        }

        /// <summary>
        /// Get Num Lock state.
        /// </summary>
        public static bool NumLock()
        {
            var isEnabled = Registry.Users.OpenSubKey(".DEFAULT\\Control Panel\\Keyboard")?.GetValue("InitialKeyboardIndicators") as string ?? string.Empty;
            return isEnabled.Equals("2147483650");
        }

        /// <summary>
        /// Get Caps Lock state.
        /// </summary>
        public static bool CapsLock()
        {
            var scancodeMap = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 58, 0, 0, 0, 0, 0 };
            var isEnabled = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Keyboard Layout")?.GetValue("Scancode Map") as byte[] ?? [];
            return !isEnabled.SequenceEqual(scancodeMap);
        }

        /// <summary>
        /// Get sticky shift state.
        /// </summary>
        public static bool StickyShift()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Control Panel\\Accessibility\\StickyKeys")?.GetValue("Flags") as string ?? string.Empty;
            return !isEnabled.Equals("506");
        }

        /// <summary>
        /// Get autoplay state.
        /// </summary>
        public static bool Autoplay()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers")?.GetValue("DisableAutoplay") as int? ?? -1;
            return !isEnabled.Equals(1);
        }

        /// <summary>
        /// Get thumbnail cache state.
        /// </summary>
        public static bool ThumbnailCacheRemoval()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache")?.GetValue("Autorun") as int? ?? -1;
            var isEnabledWow = Registry.LocalMachine.OpenSubKey("Software\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VolumeCaches\\Thumbnail Cache")?.GetValue("Autorun") as int? ?? -1;
            return !(isEnabled.Equals(0) && isEnabledWow.Equals(0));
        }

        /// <summary>
        /// Get save restartable apps state.
        /// </summary>
        public static bool SaveRestartableApps()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon")?.GetValue("RestartApps") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get network discovery state.
        /// </summary>
        public static bool NetworkDiscovery()
        {
            var discoveryGroupRules = FirewallService.GetGroupRules("@FirewallAPI.dll,-32752", "@FirewallAPI.dll,-28502");
            return discoveryGroupRules.TrueForAll(rule => rule.Enabled);
        }

        /// <summary>
        /// Get power plan state.
        /// </summary>
        public static int PowerPlan()
        {
            var activePlan = InstrumentationService.GetPowerPlans()
                .Select(plan => (IsActive: (bool)plan.GetPropertyValue("IsActive"), InstanceID: (string)plan.GetPropertyValue("InstanceID")))
                .First(plan => plan.IsActive);

            return activePlan.InstanceID.Contains("{8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c}") ? 1 : 2;
        }

        /// <summary>
        /// Get RKN bypass state.
        /// </summary>
        public static bool RKNBypass()
        {
            // If current region is Russia
            if (RegionInfo.CurrentRegion.GeoId.Equals(203))
            {
                var isEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings")?.GetValue("AutoConfigURL") as string ?? string.Empty;
                return isEnabled.Equals("https://p.thenewone.lol:8443/proxy.pac");
            }

            throw new InvalidOperationException("Unsupported GEO ID");
        }

        /// <summary>
        /// Get registry backup state.
        /// </summary>
        public static bool RegistryBackup()
        {
            var isEnabled = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Session Manager\\Configuration Manager")
                ?.GetValue("EnablePeriodicBackup") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get restore previous folders state.
        /// </summary>
        public static bool RestorePreviousFolders()
        {
            var isEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")
                ?.GetValue("PersistBrowsers") as int? ?? -1;
            return isEnabled.Equals(1);
        }

        /// <summary>
        /// Get Windows Terminal default app state.
        /// </summary>
        public static int DefaultTerminalApp()
        {
            var appxPackage = AppxPackagesService.GetPackages()
                .FirstOrDefault(package => package.Id.Name.Equals("Microsoft.WindowsTerminal"));

            if (appxPackage is not null)
            {
                var requiredVersion = new PackageVersion(1, 11, 0, 0);

                if (appxPackage.Id.Version.Major >= requiredVersion.Major
                    && appxPackage.Id.Version.Minor >= requiredVersion.Minor)
                {
                    var appxPath = $"Software\\Classes\\PackagedCom\\Package\\{appxPackage.Id.FullName}\\Class";
                    var consoleId = string.Empty;
                    var consolePath = "Console\\%%Startup";
                    var terminalId = string.Empty;
                    Registry.LocalMachine.OpenSubKey(appxPath)?.GetSubKeyNames()
                        .ForEach(key =>
                        {
                            switch (Registry.LocalMachine.OpenSubKey(Path.Combine(appxPath, key))?.GetValue("ServerId") ?? -1)
                            {
                                case 0:
                                    consoleId = key;
                                    break;
                                case 1:
                                    terminalId = key;
                                    break;
                                default:
                                    break;
                            }
                        });
                    var delegationConsole = Registry.CurrentUser.OpenSubKey(consolePath)?.GetValue("DelegationConsole") as string ?? string.Empty;
                    var delegationTerminal = Registry.CurrentUser.OpenSubKey(consolePath)?.GetValue("DelegationTerminal") as string ?? string.Empty;
                    return delegationConsole.Equals(consoleId) && delegationTerminal.Equals(terminalId) ? 1 : 2;
                }

                throw new InvalidOperationException($"Unsupported Windows Terminal version: {appxPackage.Id.Version.Major}.{appxPackage.Id.Version.Minor} required version {requiredVersion.Major}.{requiredVersion.Minor} or above");
            }

            throw new InvalidOperationException("AppX package Windows Terminal is not installed");
        }

        /// <summary>
        /// Get clock in notification center state.
        /// </summary>
        public static bool ShowClockInNotificationCenter()
        {
            var showClock = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced")
                ?.GetValue("ShowClockInNotificationCenter", false) as int? ?? -1;
            return showClock.Equals(1);
        }

        /// <summary>
        /// Get .NET 8 desktop runtime version.
        /// </summary>
        public static bool InstallDotNetRuntime_8()
        {
            var latestVersion = CommonDataService.LatestReleaseNET8?.Version ?? throw new InvalidOperationException("Internet connection is not available");
            var installedVersion = RedistributablePackageService.GetInstalledPackageVersionOrDefault($"windowsdesktop-runtime-{latestVersion}-win-x64.exe");
            return latestVersion > installedVersion ? false : throw new InvalidOperationException("Latest .NET version is installed");
        }

        /// <summary>
        /// Get .NET 9 desktop runtime version.
        /// </summary>
        public static bool InstallDotNetRuntime_9()
        {
            var latestVersion = CommonDataService.LatestReleaseNET9?.Version ?? throw new InvalidOperationException("Internet connection is not available");
            var installedVersion = RedistributablePackageService.GetInstalledPackageVersionOrDefault($"windowsdesktop-runtime-{latestVersion}-win-x64.exe");
            return latestVersion > installedVersion ? false : throw new InvalidOperationException("Latest .NET version is installed");
        }

        /// <summary>
        /// Get Microsoft Visual C++ x86 redistributable package version.
        /// </summary>
        public static bool InstallVisualC_x86()
        {
            var latestVersion = CommonDataService.LatestReleaseVC?.Version ?? throw new InvalidOperationException("Internet connection is not available");
            var installedVersion = RedistributablePackageService.GetInstalledPackageVersionOrDefault("VC_redist.x86.exe");
            return latestVersion > installedVersion ? false : throw new InvalidOperationException("Latest Visual C++ x86 version is installed");
        }

        /// <summary>
        /// Get Microsoft Visual C++ x64 redistributable package version.
        /// </summary>
        public static bool InstallVisualC_x64()
        {
            var latestVersion = CommonDataService.LatestReleaseVC?.Version ?? throw new InvalidOperationException("Internet connection is not available");
            var installedVersion = RedistributablePackageService.GetInstalledPackageVersionOrDefault("VC_redist.x64.exe");
            return latestVersion > installedVersion ? false : throw new InvalidOperationException("Latest Visual C++ x64 version is installed");
        }

        /// <summary>
        /// Gets HEVC state.
        /// </summary>
        public static bool HEVC()
        {
            var appxVideoExists = AppxPackagesService.PackageExist("Microsoft.HEVCVideoExtension");
            var appxPhotosExists = AppxPackagesService.PackageExist("Microsoft.Windows.Photos");

            if (!appxPhotosExists)
            {
                throw new InvalidOperationException("AppX package Microsoft.Windows.Photos is not installed");
            }

            return appxVideoExists && appxPhotosExists;
        }

        /// <summary>
        /// Gets Cortana auto start state.
        /// </summary>
        public static bool CortanaAutostart()
        {
            if (AppxPackagesService.PackageExist("Microsoft.549981C3F5F10"))
            {
                var pathCortana = "Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\SystemAppData\\Microsoft.549981C3F5F10_8wekyb3d8bbwe\\CortanaStartupId";
                var stateCortana = Registry.ClassesRoot.OpenSubKey(pathCortana)?.GetValue("State") as int? ?? -1;
                return stateCortana != 1;
            }

            throw new InvalidOperationException("AppX package Cortana is not installed");
        }

        /// <summary>
        /// Gets Xbox game bar state.
        /// </summary>
        public static bool XboxGameBar()
        {
            var appCaptureIsEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR")
                ?.GetValue("AppCaptureEnabled") as int? ?? -1;
            var dvrIsEnabled = Registry.CurrentUser.OpenSubKey("System\\GameConfigStore")
                ?.GetValue("GameDVR_Enabled") as int? ?? -1;

            if (appCaptureIsEnabled == 0 && dvrIsEnabled == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets Xbox game tips state.
        /// </summary>
        public static bool XboxGameTips()
        {
            if (AppxPackagesService.PackageExist("Microsoft.GamingApp"))
            {
                var startupPanelIsEnabled = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\GameBar")?.GetValue("ShowStartupPanel") as int? ?? -1;
                return startupPanelIsEnabled == 1;
            }

            throw new InvalidOperationException("AppX package Microsoft.GamingApp is not installed");
        }

        /// <summary>
        /// Get GPU scheduling state.
        /// </summary>
        public static bool GPUScheduling()
        {
            const int WDDMMinimalVersion = 2700;
            // Determining whether PC has an external graphics card
            var isExternalDACType = InstrumentationService.IsExternalDACType();
            var isVirtualMachine = InstrumentationService.IsVirtualMachine();
            var wddmVersion = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\GraphicsDrivers\\FeatureSetUsage")?.GetValue("WddmVersion_Min") as int? ?? -1;

            // Checking whether a WDDM verion is 2.7 or higher
            if (isExternalDACType && !isVirtualMachine && wddmVersion >= WDDMMinimalVersion)
            {
                var hwSchMode = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\GraphicsDrivers")?.GetValue("HwSchMode") as int? ?? -1;
                return hwSchMode == 2;
            }

            throw new InvalidOperationException($"DAC type is external: {isExternalDACType}. PC is a VM: {isVirtualMachine}. WDDM version (minimal {WDDMMinimalVersion}): {wddmVersion}");
        }

        /// <summary>
        /// Get scheduled task "Windows Cleanup" state.
        /// </summary>
        public static bool CleanupTask()
        {
            if (CommonDataService.IsWindows11 && !OsService.VBSIsInstalled())
            {
                throw new InvalidOperationException("VBSCRIPT component is not installed");
            }

            var cleanupTask = ScheduledTaskService.GetTaskOrDefault("Sophia\\Windows Cleanup");

            if (cleanupTask is not null && cleanupTask.Definition.Principal.UserId != Environment.UserName)
            {
                throw new InvalidOperationException($"The Windows Cleanup scheduled task was already created as {cleanupTask.Definition.Principal.UserId}");
            }

            return cleanupTask is not null && cleanupTask.State != TaskState.Disabled && cleanupTask.State != TaskState.Unknown;
        }

        /// <summary>
        /// Get scheduled task "SoftwareDistribution" state.
        /// </summary>
        public static bool SoftwareDistributionTask()
        {
            if (CommonDataService.IsWindows11 && !OsService.VBSIsInstalled())
            {
                throw new InvalidOperationException("VBSCRIPT component is not installed");
            }

            var distributionTask = ScheduledTaskService.GetTaskOrDefault("Sophia\\SoftwareDistribution");

            if (distributionTask is not null && distributionTask.Definition.Principal.UserId != Environment.UserName)
            {
                throw new InvalidOperationException($"The SoftwareDistribution scheduled task was already created as {distributionTask.Definition.Principal.UserId}");
            }

            return distributionTask is not null && distributionTask.State != TaskState.Disabled && distributionTask.State != TaskState.Unknown;
        }

        /// <summary>
        /// Get scheduled task "Temp" state.
        /// </summary>
        public static bool TempTask()
        {
            if (CommonDataService.IsWindows11 && !OsService.VBSIsInstalled())
            {
                throw new InvalidOperationException("VBSCRIPT component is not installed");
            }

            var tempTask = ScheduledTaskService.GetTaskOrDefault("Sophia\\Temp");

            if (tempTask is not null && tempTask.Definition.Principal.UserId != Environment.UserName)
            {
                throw new InvalidOperationException($"The Temp scheduled task was already created as {tempTask.Definition.Principal.UserId}");
            }

            return tempTask is not null && tempTask.State != TaskState.Disabled && tempTask.State != TaskState.Unknown;
        }

        /// <summary>
        /// Get Windows network protection state.
        /// </summary>
        public static bool NetworkProtection()
        {
            var defenderIsEnabled = CommonDataService.DefenderEnabled;
            var defenderMpPreferenceBroken = CommonDataService.DefenderMpPreferenceBroken;
            var antiSpywareEnabled = InstrumentationService.GetAntiSpywareEnabled();

            if (defenderIsEnabled && !defenderMpPreferenceBroken && antiSpywareEnabled)
            {
                var networkProtection = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows Defender\\Windows Defender Exploit Guard\\Network Protection")?.GetValue("EnableNetworkProtection") as int? ?? -1;
                return networkProtection.Equals(1);
            }

            throw new InvalidOperationException("Microsoft Defender antispyware protection is disabled");
        }

        /// <summary>
        /// Get Windows PUApps detection state.
        /// </summary>
        public static bool PUAppsDetection()
        {
            var defenderIsEnabled = CommonDataService.DefenderEnabled;
            var defenderMpPreferenceBroken = CommonDataService.DefenderMpPreferenceBroken;
            var antiSpywareEnabled = InstrumentationService.GetAntiSpywareEnabled();

            if (defenderIsEnabled && !defenderMpPreferenceBroken && antiSpywareEnabled)
            {
                var puaProtection = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows Defender")?.GetValue("PUAProtection") as int? ?? -1;
                return puaProtection.Equals(1);
            }

            throw new InvalidOperationException("Microsoft Defender antispyware protection is disabled");
        }

        /// <summary>
        /// Get Microsoft Defender sandbox state.
        /// </summary>
        public static bool DefenderSandbox()
        {
            var defenderIsEnabled = CommonDataService.DefenderEnabled;
            var defenderMpPreferenceBroken = CommonDataService.DefenderMpPreferenceBroken;
            var antiSpywareEnabled = InstrumentationService.GetAntiSpywareEnabled();

            if (defenderIsEnabled && !defenderMpPreferenceBroken && antiSpywareEnabled)
            {
                return ProcessService.Exists("MsMpEngCP");
            }

            throw new InvalidOperationException("Microsoft Defender antispyware protection is disabled");
        }

        /// <summary>
        /// Get Windows event viewer custom view state.
        /// </summary>
        public static bool EventViewerCustomView()
        {
            var processXmlPath = $"{Environment.GetEnvironmentVariable("ALLUSERSPROFILE")}\\Microsoft\\Event Viewer\\Views\\ProcessCreation.xml";
            var auditPolicyScript = @"$OutputEncoding = [System.Console]::OutputEncoding = [System.Console]::InputEncoding = [System.Text.Encoding]::UTF8
$Enabled = auditpol /get /Subcategory:'{0CCE922B-69AE-11D9-BED3-505054503030}' /r | ConvertFrom-Csv | Select-Object -ExpandProperty 'Inclusion Setting'
if ($Enabled -eq 'Success and Failure')
{
    $true
}
else
{
    $false
}";

            var auditPolicyIsEnabled = PowerShellService.Invoke<bool>(auditPolicyScript);
            var processAuditIsEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit")?.GetValue("ProcessCreationIncludeCmdLine_Enabled") as int? ?? -1;
            var xmlAuditIsEnabled = XmlService.TryLoad(processXmlPath)?.SelectSingleNode("//Select[@Path=\"Security\"]")?.InnerText ?? string.Empty;

            return auditPolicyIsEnabled && processAuditIsEnabled.Equals(1) && xmlAuditIsEnabled.Equals("*[System[(EventID=4688)]]");
        }

        /// <summary>
        /// Get Windows PowerShell modules logging state.
        /// </summary>
        public static bool PowerShellModulesLogging()
        {
            var moduleLoggingPath = "Software\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging";
            var moduleNamePath = $"{moduleLoggingPath}\\ModuleNames";

            var moduleLoggingIsEnabled = Registry.LocalMachine.OpenSubKey(moduleLoggingPath)?.GetValue("EnableModuleLogging") as int? ?? -1;
            var moduleNamesIsAny = Registry.LocalMachine.OpenSubKey(moduleNamePath)?.GetValue("*") as string ?? string.Empty;
            return moduleLoggingIsEnabled.Equals(1) && moduleNamesIsAny.Equals("*");
        }

        /// <summary>
        /// Get Windows PowerShell scripts logging state.
        /// </summary>
        public static bool PowerShellScriptsLogging()
        {
            var scriptLogging = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging")?.GetValue("EnableScriptBlockLogging") as int? ?? -1;
            return scriptLogging.Equals(1);
        }

        /// <summary>
        /// Get Windows SmartScreen state.
        /// </summary>
        public static bool AppsSmartScreen()
        {
            var defenderIsEnabled = CommonDataService.DefenderEnabled;
            var defenderMpPreferenceBroken = CommonDataService.DefenderMpPreferenceBroken;
            var antiSpywareEnabled = InstrumentationService.GetAntiSpywareEnabled();

            if (defenderIsEnabled && !defenderMpPreferenceBroken && antiSpywareEnabled)
            {
                var smartScreenIsEnabled = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer")?.GetValue("SmartScreenEnabled") as string ?? string.Empty;
                return !smartScreenIsEnabled.Equals("Off");
            }

            throw new InvalidOperationException("Microsoft Defender antispyware protection is disabled");
        }

        /// <summary>
        /// Get Windows save zone state.
        /// </summary>
        public static bool SaveZoneInformation()
        {
            var saveZoneInformation = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Attachments")?.GetValue("SaveZoneInformation") as int? ?? -1;
            return saveZoneInformation.Equals(1);
        }

        /// <summary>
        /// Get Windows Sandbox state.
        /// </summary>
        public static bool WindowsSandbox()
        {
            bool WindowsSandboxIsEnabled()
            {
                // Checking whether x86 virtualization is enabled in the firmware
                var sandboxScript = "Get-WindowsOptionalFeature -FeatureName Containers-DisposableClientVM -Online";
                var sandboxState = PowerShellService.Invoke(sandboxScript).FirstOrDefault();
                return !sandboxState?.Properties["State"]?.Value.Equals("Disabled") ?? throw new InvalidOperationException("Windows Sandbox state undefined");
            }

            if (CommonDataService.OsProperties.Edition.Equals("Professional") || CommonDataService.OsProperties.Edition.Equals("Enterprise"))
            {
                // Determining whether Hyper-V is enabled
                var virtualizationIsEnabled = InstrumentationService.CpuVirtualizationFirmwareIsEnabled() ?? throw new InvalidOperationException("This CPU does not support virtualization");
                var hypervisorPresent = InstrumentationService.HypervisorIsPresent() ?? throw new InvalidOperationException("Enable virtualization in UEFI");

                if (virtualizationIsEnabled)
                {
                    return WindowsSandboxIsEnabled();
                }
                else if (hypervisorPresent)
                {
                    return WindowsSandboxIsEnabled();
                }

                throw new InvalidOperationException("This PC does not support Windows Sandbox feature");
            }

            throw new InvalidOperationException("Unsupported Windows edition");
        }

        /// <summary>
        /// Get Local Security Authority state.
        /// </summary>
        public static bool LocalSecurityAuthority()
        {
            var virtualizationIsEnabled = InstrumentationService.CpuVirtualizationFirmwareIsEnabled() ?? throw new InvalidOperationException("This CPU does not support virtualization");
            var hypervisorPresent = InstrumentationService.HypervisorIsPresent() ?? throw new InvalidOperationException("Enable virtualization in UEFI");
            var runAsPPL = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Lsa")?.GetValue("RunAsPPL") ?? -1;
            var runAsPPLBoot = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Lsa")?.GetValue("RunAsPPLBoot") ?? -1;
            var runAsPPLPolicy = Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\System")?.GetValue("RunAsPPL") ?? -1;

            if (virtualizationIsEnabled)
            {
                return (runAsPPL.Equals(2) && runAsPPLBoot.Equals(2)) || runAsPPLPolicy.Equals(2);
            }
            else if (hypervisorPresent)
            {
                return (runAsPPL.Equals(2) && runAsPPLBoot.Equals(2)) || runAsPPLPolicy.Equals(2);
            }

            throw new InvalidOperationException("This PC does not support Local Security Authority feature");
        }

        /// <summary>
        /// Get "Extract all" item in the Windows Installer (.msi) context menu state.
        /// </summary>
        public static bool MSIExtractContext()
        {
            var muiVerb = Registry.ClassesRoot.OpenSubKey("Msi.Package\\shell\\Extract")?.GetValue("MUIVerb") as string;
            return muiVerb?.Equals("@shell32.dll,-37514") ?? false;
        }

        /// <summary>
        /// Get "Install" item in the Cabinet archives (.cab) context menu state.
        /// </summary>
        public static bool CABInstallContext()
        {
            var isDefault = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.cab\\UserChoice")
                ?.GetValue("ProgId") ?? string.Empty;

            if (isDefault.Equals("CABFolder"))
            {
                var muiVerb = Registry.ClassesRoot.OpenSubKey("CABFolder\\Shell\\runas")?.GetValue("MUIVerb") as string;
                return muiVerb?.Equals("@shell32.dll,-10210") ?? false;
            }

            throw new InvalidOperationException("A third-party archiver is set as the default archiver");
        }

        /// <summary>
        /// Get "Cast to Device" item in the media files and folders context menu state.
        /// </summary>
        public static bool CastToDeviceContext()
        {
            var userCastToDevice = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{7AD84985-87B4-4a16-BE58-8B72A5B390F7}") as string;
            var machineCastToDevice = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{7AD84985-87B4-4a16-BE58-8B72A5B390F7}") as string;
            return userCastToDevice is null && machineCastToDevice is null;
        }

        /// <summary>
        /// Get "Share" context menu item state.
        /// </summary>
        public static bool ShareContext()
        {
            var userShareContext = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}") as string;
            var machineShareContext = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{E2BF9676-5F8F-435C-97EB-11607A5BEDF7}") as string;
            return userShareContext is null && machineShareContext is null;
        }

        /// <summary>
        /// Get "Edit With Clipchamp" item in the media files context menu state.
        /// </summary>
        public static bool EditWithClipchampContext()
        {
            if (AppxPackagesService.PackageExist("Clipchamp.Clipchamp"))
            {
                var userClipchamp = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{8AB635F8-9A67-4698-AB99-784AD929F3B4}");
                var machineClipchamp = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{8AB635F8-9A67-4698-AB99-784AD929F3B4}");
                return userClipchamp is null && machineClipchamp is null;
            }

            throw new InvalidOperationException("AppX package Clipchamp.Clipchamp is not installed");
        }

        /// <summary>
        /// Get "Edit With Photos" item in the media files context menu state.
        /// </summary>
        public static bool EditWithPhotosContext()
        {
            if (AppxPackagesService.PackageExist("Microsoft.Windows.Photos"))
            {
                var userPhotosContext = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{8AB635F8-9A67-4698-AB99-784AD929F3B4}");
                var machinePhotosContext = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{8AB635F8-9A67-4698-AB99-784AD929F3B4}");
                return userPhotosContext is null && machinePhotosContext is null;
            }

            throw new InvalidOperationException("AppX package Microsoft.Windows.Photos is not installed");
        }

        /// <summary>
        /// Get "Edit With Paint Context" item in the media files context menu state.
        /// </summary>
        public static bool EditWithPaintContext()
        {
            if (AppxPackagesService.PackageExist("Microsoft.Paint"))
            {
                var paintContext = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{2430F218-B743-4FD6-97BF-5C76541B4AE9}");
                return paintContext is null;
            }

            throw new InvalidOperationException("AppX package Microsoft.Paint is not installed");
        }

        /// <summary>
        /// Get "Edit with Paint 3D" item in the media files context menu state.
        /// </summary>
        public static bool EditWithPaint3DContext()
        {
            if (AppxPackagesService.PackageExist("Microsoft.MSPaint"))
            {
                var accessValues = new List<object?>()
                {
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.bmp\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.gif\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.jpe\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.jpeg\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.jpg\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.png\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.tif\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                    Registry.ClassesRoot.OpenSubKey("SystemFileAssociations\\.tiff\\Shell\\3D Edit")?.GetValue("ProgrammaticAccessOnly"),
                };

                return !accessValues.TrueForAll(value => value is not null);
            }

            throw new InvalidOperationException("AppX package Microsoft.MSPaint is not installed");
        }

        /// <summary>
        /// Get "Print" item in the .bat and .cmd files context menu state.
        /// </summary>
        public static bool PrintCMDContext()
        {
            var accessOnlyValues = new List<object?>()
            {
                Registry.ClassesRoot.OpenSubKey("batfile\\shell\\print")?.GetValue("ProgrammaticAccessOnly"),
                Registry.ClassesRoot.OpenSubKey("cmdfile\\shell\\print")?.GetValue("ProgrammaticAccessOnly"),
            };

            return !accessOnlyValues.TrueForAll(value => value is not null);
        }

        /// <summary>
        /// Get "Include in Library" item in the folders and drives context menu state.
        /// </summary>
        public static bool IncludeInLibraryContext()
        {
            var libraryContextValue = Registry.ClassesRoot.OpenSubKey("Folder\\ShellEx\\ContextMenuHandlers\\Library Location")?.GetValue(string.Empty) as string;
            return !libraryContextValue?.Equals("-{3dad6c5d-2167-4cae-9914-f99e41c12cfa}") ?? true;
        }

        /// <summary>
        /// Get Send to" item in the folders context menu state.
        /// </summary>
        public static bool SendToContext()
        {
            var sendToContext = Registry.ClassesRoot.OpenSubKey("AllFilesystemObjects\\shellex\\ContextMenuHandlers\\SendTo")?.GetValue(string.Empty) as string;
            return !sendToContext?.Equals("-{7BA4C740-9E81-11CF-99D3-00AA004AE837}") ?? true;
        }

        /// <summary>
        /// Get "Bitmap image" item in the "New" context menu state.
        /// </summary>
        public static bool BitmapImageNewContext()
        {
            var paintPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\mspaint.exe";

            if (File.Exists(paintPath))
            {
                var bmpShellNew = Registry.ClassesRoot.OpenSubKey(".bmp\\ShellNew");
                return !(bmpShellNew is null);
            }

            throw new InvalidOperationException($"File {paintPath} does not exist");
        }

        /// <summary>
        /// Get "Rich Text Document" item in the "New" context menu state.
        /// </summary>
        public static bool RichTextDocumentNewContext()
        {
            var wordpadPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Windows NT\\Accessories\\wordpad.exe";

            if (File.Exists(wordpadPath))
            {
                var rtfShellNew = Registry.ClassesRoot.OpenSubKey(".rtf\\ShellNew");
                return !(rtfShellNew is null);
            }

            throw new InvalidOperationException($"File {wordpadPath} does not exist");
        }

        /// <summary>
        /// Get "Compressed (zipped) Folder" item in the "New" context menu state.
        /// </summary>
        public static bool CompressedFolderNewContext()
        {
            var zipShellNew = Registry.ClassesRoot.OpenSubKey(".zip\\CompressedFolder\\ShellNew");
            return !(zipShellNew is null);
        }

        /// <summary>
        /// Get "Open", "Print", and "Edit" context menu items available when selecting more than 15 files state.
        /// </summary>
        public static bool MultipleInvokeContext()
        {
            var multipleInvokePrompt = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer")?.GetValue("MultipleInvokePromptMinimum") as int?;
            return multipleInvokePrompt?.Equals(300) ?? false;
        }

        /// <summary>
        /// Get "Look for an app in the Microsoft Store" items in the "Open with" dialog state.
        /// </summary>
        public static bool UseStoreOpenWith()
        {
            var storeOpenWith = Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer")?.GetValue("NoUseStoreOpenWith") as int?;
            return !storeOpenWith?.Equals(1) ?? true;
        }

        /// <summary>
        /// Get "Open in Windows Terminal" item in the folders context menu state.
        /// </summary>
        public static bool OpenWindowsTerminalContext()
        {
            var appxTerminal = "Microsoft.WindowsTerminal";

            if (AppxPackagesService.PackageExist(appxTerminal))
            {
                var userBlockedGuid = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{9F156763-7844-4DC4-B2B1-901F640F5155}");
                var machineBlockedGuid = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{9F156763-7844-4DC4-B2B1-901F640F5155}");
                return userBlockedGuid is null && machineBlockedGuid is null;
            }

            throw new InvalidOperationException($"AppX package {appxTerminal} is not installed");
        }

        /// <summary>
        /// Get Open Windows Terminal from context menu as administrator by default state.
        /// </summary>
        public static bool OpenWindowsTerminalAdminContext()
        {
            var appxTerminal = "Microsoft.WindowsTerminal";

            if (AppxPackagesService.PackageExist(appxTerminal))
            {
                var userBlockedGuid = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{9F156763-7844-4DC4-B2B1-901F640F5155}");
                var machineBlockedGuid = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked")?.GetValue("{9F156763-7844-4DC4-B2B1-901F640F5155}");

                if (userBlockedGuid is null && machineBlockedGuid is null)
                {
                    try
                    {
                        var terminalSettings = $@"{Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%")}\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json";
                        var jsonSettings = File.ReadAllText(terminalSettings, Encoding.UTF8);
                        var jsonProfile = Json.ToObject<MsTerminalSettingsDto>(jsonSettings);
                        return jsonProfile?.Profiles?.Defaults?.Elevate ?? false;
                    }
                    catch (ArgumentException)
                    {
                        throw new InvalidOperationException($"{appxTerminal} configuration file is not valid");
                    }
                }

                return true;
            }

            throw new InvalidOperationException($"AppX package {appxTerminal} is not installed");
        }

        /// <summary>
        /// Get images edit from context menu state.
        /// </summary>
        public static bool ImagesEditContext()
        {
            var paintPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\mspaint.exe";

            if (File.Exists(paintPath))
            {
                var accessPath = "SystemFileAssociations\\image\\shell\\edit";
                var accessValue = Registry.ClassesRoot.OpenSubKey(accessPath)?.GetValue("ProgrammaticAccessOnly") as string;
                return accessValue is null;
            }

            throw new InvalidOperationException($"File {paintPath} does not exist");
        }
    }
}
