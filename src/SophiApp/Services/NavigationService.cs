// <copyright file="NavigationService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SophiApp.Contracts.Services;
using SophiApp.Contracts.ViewModels;
using SophiApp.Extensions;
using SophiApp.Helpers;
using SophiApp.RequirementsViewModels;
using SophiApp.Views;
using System.Diagnostics.CodeAnalysis;

/// <inheritdoc/>
public class NavigationService : INavigationService
{
    private readonly IPageService pageService;
    private object? lastParameterUsed;
    private Frame? frame;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="pageService">A service for working with app page.</param>
    public NavigationService(IPageService pageService)
    {
        this.pageService = pageService;
    }

    /// <inheritdoc/>
    public event NavigatedEventHandler? Navigated;

    /// <inheritdoc/>
    public Frame? Frame
    {
        get
        {
            if (frame == null)
            {
                frame = App.MainWindow.Content as Frame;
                RegisterFrameEvents();
            }

            return frame;
        }

        set
        {
            UnregisterFrameEvents();
            frame = value;
            RegisterFrameEvents();
        }
    }

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(Frame), nameof(frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    /// <inheritdoc/>
    public bool GoBack()
    {
        if (CanGoBack)
        {
            var vmBeforeNavigation = frame.GetPageViewModel();
            frame.GoBack();
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool NavigateTo(string page, object? parameter = null, bool clearHistory = false, bool disablePageAnimation = false)
    {
        var pageType = pageService.GetPageType(page);

        if (frame != null && (disablePageAnimation || frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(lastParameterUsed))))
        {
            frame.Tag = clearHistory;
            var vmBeforeNavigation = frame.GetPageViewModel();
            var navigateAnimation = disablePageAnimation ? new SuppressNavigationTransitionInfo() : null;
            var navigated = frame.Navigate(pageType, parameter, navigateAnimation);
            if (navigated)
            {
                lastParameterUsed = parameter;
                if (vmBeforeNavigation is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedFrom();
                }
            }

            return navigated;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool NavigateTo(RequirementsResult result, bool clearNavigation = true)
    {
        var viewModel = result switch
        {
            RequirementsResult.BitLockerEncryptOrDecryptState => typeof(BitLockerEncryptOrDecryptViewModel).FullName,
            RequirementsResult.BitLockerProtectionStatus => typeof(BitLockerProtectionStatusViewModel).FullName,
            RequirementsResult.DefenderControlledFolderEnable => typeof(DefenderControlledFolderEnableViewModel).FullName,
            RequirementsResult.DefenderFileMissing => typeof(DefenderFileMissingViewModel).FullName,
            RequirementsResult.AntiSpywareDisabled => typeof(AntiSpywareDisabledViewModel).FullName,
            RequirementsResult.DefenderSecurityHealthFailure => typeof(DefenderSecurityHealthFailureViewModel).FullName,
            RequirementsResult.DefenderServiceFailure => typeof(DefenderServiceFailureViewModel).FullName,
            RequirementsResult.DefenderSettingsPageHidden => typeof(DefenderSettingsPageHiddenViewModel).FullName,
            RequirementsResult.DetectHostFileEntries => typeof(DetectHostFileEntriesViewModel).FullName,
            RequirementsResult.EventLogBroken => typeof(EventLogBrokenViewModel).FullName,
            RequirementsResult.FeatureExperiencePackRemoved => typeof(FeatureExperiencePackRemovedViewModel).FullName,
            RequirementsResult.Is32BitOs => typeof(Is32BitOsViewModel).FullName,
            RequirementsResult.MalwareDetected => typeof(MalwareDetectedViewModel).FullName,
            RequirementsResult.MsStoreRemoved => typeof(MsStoreRemovedViewModel).FullName,
            RequirementsResult.RebootRequired => typeof(RebootRequiredViewModel).FullName,
            RequirementsResult.RunByNotLoggedUser => typeof(RunByNotLoggedUserViewModel).FullName,
            RequirementsResult.WinUnsupportedBuild => typeof(WinUnsupportedBuildViewModel).FullName,
            RequirementsResult.WinUnsupportedUBR => typeof(WinUnsupportedUbrViewModel).FullName,
            RequirementsResult.WMIBroken => typeof(WmiBrokenViewModel).FullName,
            _ => throw new TypeAccessException($"Not defined enum constant \"{nameof(result)}\" in {nameof(RequirementsResult)}")
        };

        return NavigateTo(page: viewModel!, clearHistory: clearNavigation);
    }

    private void RegisterFrameEvents()
    {
        if (frame != null)
        {
            frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (frame != null)
        {
            frame.Navigated -= OnNavigated;
        }
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame page)
        {
            var clearNavigation = (bool)page.Tag;

            if (clearNavigation)
            {
                page.BackStack.Clear();
            }

            if (FrameNavigation.GetPageViewModel(page) is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.Parameter);
            }

            if (e.SourcePageType != typeof(StartupPage))
            {
                App.Logger.LogNavigateToPage(e.SourcePageType.Name);
            }

            Navigated?.Invoke(sender, e);
        }
    }
}
