// <copyright file="PageService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using SophiApp.Contracts.Services;
using SophiApp.RequirementsViewModels;
using SophiApp.RequirementsViews;
using SophiApp.ViewModels;
using SophiApp.Views;

/// <inheritdoc/>
public class PageService : IPageService
{
    private readonly Dictionary<string, Type> pages = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PageService"/> class.
    /// </summary>
    public PageService()
    {
        Configure<AntiSpywareDisabledViewModel, AntiSpywareDisabledPage>();
        Configure<BitLockerEncryptOrDecryptViewModel, BitLockerEncryptOrDecryptPage>();
        Configure<BitLockerProtectionStatusViewModel, BitLockerProtectionStatusPage>();
        Configure<ContextMenuViewModel, ContextMenuPage>();
        Configure<DefenderControlledFolderEnableViewModel, DefenderControlledFolderEnablePage>();
        Configure<DefenderFileMissingViewModel, DefenderFileMissingPage>();
        Configure<DefenderSecurityHealthFailureViewModel, DefenderSecurityHealthFailurePage>();
        Configure<DefenderServiceFailureViewModel, DefenderServiceFailurePage>();
        Configure<DefenderSettingsPageHiddenViewModel, DefenderSettingsPageHiddenPage>();
        Configure<DetectHostFileEntriesViewModel, DetectHostFileEntriesPage>();
        Configure<EventLogBrokenViewModel, EventLogBrokenPage>();
        Configure<FatalErrorViewModel, FatalErrorPage>();
        Configure<FeatureExperiencePackRemovedViewModel, FeatureExperiencePackRemovedPage>();
        Configure<Is32BitOsViewModel, Is32BitOsPage>();
        Configure<LogViewModel, LogPage>();
        Configure<MalwareDetectedViewModel, MalwareDetectedPage>();
        Configure<MsStoreRemovedViewModel, MsStoreRemovedPage>();
        Configure<PersonalizationViewModel, PersonalizationPage>();
        Configure<PrivacyViewModel, PrivacyPage>();
        Configure<ProVersionViewModel, ProVersionPage>();
        Configure<RebootRequiredViewModel, RebootRequiredPage>();
        Configure<RunByNotLoggedUserViewModel, RunByNotLoggedUserPage>();
        Configure<SearchViewModel, SearchPage>();
        Configure<SecurityViewModel, SecurityPage>();
        Configure<SettingsViewModel, SettingsPage>();
        Configure<StartupViewModel, StartupPage>();
        Configure<SystemViewModel, SystemPage>();
        Configure<TaskSchedulerViewModel, TaskSchedulerPage>();
        Configure<UwpViewModel, UwpPage>();
        Configure<WinUnsupportedBuildViewModel, WinUnsupportedBuildPage>();
        Configure<WinUnsupportedUbrViewModel, WinUnsupportedUbrPage>();
        Configure<WmiBrokenViewModel, WmiBrokenPage>();
    }

    /// <inheritdoc/>
    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (pages)
        {
            if (!pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (pages)
        {
            var key = typeof(VM).FullName!;
            if (pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (pages.ContainsValue(type))
            {
                throw new ArgumentException($"This type is already configured with key {pages.First(p => p.Value == type).Key}");
            }

            pages.Add(key, type);
        }
    }
}
