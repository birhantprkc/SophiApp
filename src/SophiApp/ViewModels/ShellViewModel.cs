// <copyright file="ShellViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSharpFunctionalExtensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SophiApp.Contracts.Services;
using SophiApp.ControlTemplates;
using SophiApp.Extensions;
using SophiApp.Helpers;
using SophiApp.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

/// <summary>
/// Implements the <see cref="ShellViewModel"/> class.
/// </summary>
public partial class ShellViewModel : ObservableRecipient
{
    private readonly IAppNotificationService notificationService;
    private readonly IAppxPackagesService packagesService;
    private readonly ICommonDataService dataService;
    private readonly IDefenderService defenderService;
    private readonly IGroupPolicyService groupPolicyService;
    private readonly IModelService modelService;
    private readonly IProcessService processService;
    private readonly IRequirementsService requirementsService;
    private readonly ISettingsService settingsService;
    private readonly RequirementsFailureViewModel failureViewModel;
    private readonly StartupViewModel startupModel;
    private bool logPageVisible;

    [ObservableProperty]
    private bool isBackEnabled;
    [ObservableProperty]
    private bool navigationViewHitTestVisible = false;
    [ObservableProperty]
    private bool setUpCustomizationsPanelIsVisible = false;
    [ObservableProperty]
    private bool uwpForAllUsersState = true;
    [ObservableProperty]
    private int progressBarValue = 0;
    [ObservableProperty]
    private List<string> loggedActions = [];
    private List<UIModel> uwpAllUsersModels = [];
    private List<UIModel> uwpCurrentUserModels = [];
    [ObservableProperty]
    private object? selectedNavigationViewItem;
    [ObservableProperty]
    private ObservableCollection<UIModel> applicableModels = [];
    [ObservableProperty]
    private ObservableCollection<UIModel> uwpAppsModels = [];
    [ObservableProperty]
    private string delimiter;
    [ObservableProperty]
    private string setUpCustomizationsPanelText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel"/> class.
    /// </summary>
    /// <param name="notificationService">A service for working with toast notifications API.</param>
    /// <param name="packagesService">A service for working with appx packages API.</param>
    /// <param name="dataService">A service for working with common app data.</param>
    /// <param name="defenderService">A service for working with Microsoft Defender API.</param>
    /// <param name="modelService">A service for working with UI models using MVVM pattern.</param>
    /// <param name="navigationService">Page navigation service.</param>
    /// <param name="navigationViewService">A service for navigating to View.</param>
    /// <param name="processService">A service for working with Windows process API.</param>
    /// <param name="requirementsService">Service for working with OS requirements.</param>
    /// <param name="requirementsFailureViewModel">Implements the <see cref="RequirementsFailureViewModel"/> class.</param>
    /// <param name="startupViewModel">Implements the <see cref="StartupViewModel"/> class.</param>
    /// <param name="groupPolicyService">A service for working with group policy API.</param>
    /// <param name="settingsService">A service for working with app settings.</param>
    public ShellViewModel(
        IAppNotificationService notificationService,
        IAppxPackagesService packagesService,
        ICommonDataService dataService,
        IDefenderService defenderService,
        IModelService modelService,
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IProcessService processService,
        IRequirementsService requirementsService,
        RequirementsFailureViewModel requirementsFailureViewModel,
        StartupViewModel startupViewModel,
        IGroupPolicyService groupPolicyService,
        ISettingsService settingsService)
    {
        this.notificationService = notificationService;
        this.dataService = dataService;
        this.defenderService = defenderService;
        this.modelService = modelService;
        this.packagesService = packagesService;
        this.processService = processService;
        this.requirementsService = requirementsService;
        this.groupPolicyService = groupPolicyService;
        this.settingsService = settingsService;
        startupModel = startupViewModel;
        NavigationViewService = navigationViewService;
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        failureViewModel = requirementsFailureViewModel;
        delimiter = this.dataService.GetDelimiter();

        ApplicableModelsApply_Command = new AsyncRelayCommand(ApplicableModelsApplyAsync);
        ApplicableModelsClear_Command = new AsyncRelayCommand(ApplicableModelsClearAsync);
        RadioButtonsGroup2Clicked_Command = new RelayCommand<UIRadioButtonsGroup2Model>(group => RadioButtonsGroup2Clicked(group!));
        RadioButtonsGroup3Clicked_Command = new RelayCommand<UIRadioButtonsGroup3Model>(group => RadioButtonsGroup3Clicked(group!));
        RadioButtonsGroup4Clicked_Command = new RelayCommand<UIRadioButtonsGroup4Model>(group => RadioButtonsGroup4Clicked(group!));
        SetLogPageVisibility_Command = new RelayCommand<bool>(SetLogPageVisibility);
        OpenTaskScheduler_Command = new AsyncRelayCommand(OpenTaskSchedulerAsync);
        SearchBoxQuerySubmitted_Command = new AsyncRelayCommand<AutoSuggestBoxQuerySubmittedEventArgs>(args => SearchBoxQuerySubmittedAsync(args!));
        UIModelClicked_Command = new RelayCommand<UIModel>(model => UIModelClicked(model!));
        UIUwpAppModelClicked_Command = new RelayCommand<UIUwpAppModel>(model => UIUwpAppModelClicked(model!));
        UwpForAllUsersClicked_Command = new RelayCommand(UwpForAllUsersClicked);
        LogPageVisible = settingsService.ReadLogPageVisibility();
    }

    /// <summary>
    /// Gets <see cref="IAsyncRelayCommand"/> to click an "Apply" button in the Apply Customizations Panel.
    /// </summary>
    public IAsyncRelayCommand ApplicableModelsApply_Command { get; }

    /// <summary>
    /// Gets <see cref="IAsyncRelayCommand"/> to click an "Cancel" button in the Apply Customizations Panel.
    /// </summary>
    public IAsyncRelayCommand ApplicableModelsClear_Command { get; }

    /// <summary>
    /// Gets or sets a value indicating whether log page visibility.
    /// </summary>
    public bool LogPageVisible
    {
        get => logPageVisible;
        set
        {
            if (logPageVisible != value)
            {
                logPageVisible = value;
                settingsService.SaveLogPageVisibility(value);
                OnPropertyChanged(nameof(LogPageVisible));
            }
        }
    }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to <see cref="RadioButtonsGroup2"/> clicked.
    /// </summary>
    public IRelayCommand<UIRadioButtonsGroup2Model> RadioButtonsGroup2Clicked_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to <see cref="RadioButtonsGroup3"/> clicked.
    /// </summary>
    public IRelayCommand<UIRadioButtonsGroup3Model> RadioButtonsGroup3Clicked_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to <see cref="RadioButtonsGroup4"/> clicked.
    /// </summary>
    public IRelayCommand<UIRadioButtonsGroup4Model> RadioButtonsGroup4Clicked_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an "Show log page in navigation menu" CheckBox in Settings page.
    /// </summary>
    public IRelayCommand<bool> SetLogPageVisibility_Command { get; }

    /// <summary>
    /// Gets <see cref="IAsyncRelayCommand"/> to click "Search" in AutoSuggestBox.
    /// </summary>
    public IAsyncRelayCommand<AutoSuggestBoxQuerySubmittedEventArgs> SearchBoxQuerySubmitted_Command { get; }

    /// <summary>
    /// Gets <see cref="IAsyncRelayCommand"/> to click an "Open" button in the Task Scheduler page.
    /// </summary>
    public IAsyncRelayCommand OpenTaskScheduler_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an element in the interface.
    /// </summary>
    public IRelayCommand<UIModel> UIModelClicked_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an <see cref="UIUwpAppModel"/> in the interface.
    /// </summary>
    public IRelayCommand<UIUwpAppModel> UIUwpAppModelClicked_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an "For all users" checkbox in the UWP page.
    /// </summary>
    public IRelayCommand UwpForAllUsersClicked_Command { get; }

    /// <summary>
    /// Gets and saves the app font sizes to a setting file.
    /// </summary>
    public FontOptions FontOptions { get; } = new ();

    /// <summary>
    /// Gets <see cref="INavigationService"/>.
    /// </summary>
    public INavigationService NavigationService { get; }

    /// <summary>
    /// Gets <see cref="INavigationViewService"/>.
    /// </summary>
    public INavigationViewService NavigationViewService { get; }

    /// <summary>
    /// Gets <see cref="UIModel"/> collection from "UIMarkup.json" file.
    /// </summary>
    public ObservableCollection<UIModel> JsonModels { get; private set; } = [];

    /// <summary>
    /// Gets <see cref="UIModel"/> collection founded by AutoSuggestBox query.
    /// </summary>
    public ObservableCollection<UIModel> FoundModels { get; private set; } = [];

    /// <summary>
    /// Executes the ViewModel logic of the MVVM pattern.
    /// </summary>
    public async Task ExecuteAsync()
    {
        var numberOfRequirements = 12;
        await Task.Run(() =>
        {
            var timer = Stopwatch.StartNew();
            _ = Result.Try(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.StatusText = "OsRequirements_GetOsBitness".GetLocalized();
                _ = NavigationService.NavigateTo(typeof(StartupViewModel).FullName!);
            }))
            .Bind(_ => requirementsService.GetOsBitness())
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GetWmiState".GetLocalized();
            }))
            .Bind(requirementsService.GetWmiState)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GetOsVersion".GetLocalized();
            }))
            .Bind(requirementsService.GetOsVersion)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_AppRunFromLoggedUser".GetLocalized();
            }))
            .Bind(requirementsService.AppRunFromLoggedUser)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_MalwareDetection".GetLocalized();
            }))
            .Bind(requirementsService.MalwareDetection)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GetFeatureExperiencePackState".GetLocalized();
            }))
            .Bind(requirementsService.GetFeatureExperiencePackState)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GetEventLogState".GetLocalized();
            }))
            .Bind(requirementsService.GetEventLogState)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GetMicrosoftStoreState".GetLocalized();
            }))
            .Bind(requirementsService.GetMicrosoftStoreState)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GetPendingRebootState".GetLocalized();
            }))
            .Bind(requirementsService.GetPendingRebootState)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_UpdateDetection".GetLocalized();
            }))
            .Bind(requirementsService.AppUpdateDetection)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GetMsDefenderState".GetLocalized();
            }))
            .Bind(defenderService.GetState)
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_ReadWindowsSettings".GetLocalized();
            }))
            .Tap(async () =>
            {
                JsonModels = await modelService.BuildJsonModelsAsync();
                modelService.GetModelsState(JsonModels);
            })
            .Tap(() => App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                startupModel.ProgressBarValue = startupModel.ProgressBarValue.Increase(numberOfRequirements);
                startupModel.StatusText = "OsRequirements_GeneratingUserInterface".GetLocalized();
            }))
            .Tap(async () =>
            {
                uwpAllUsersModels = await modelService.BuildUwpAppModelsAsync(forAllUsers: true);
                uwpCurrentUserModels = await modelService.BuildUwpAppModelsAsync(forAllUsers: false);
                UwpAppsModels = new (uwpAllUsersModels);
            })
            .Match(
                onSuccess: () =>
                {
                    timer.Stop();
                    App.Logger.LogViewModelExecute(timer);
                    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        NavigationViewHitTestVisible = true;
                        _ = NavigationService.NavigateTo(pageKey: typeof(PrivacyViewModel).FullName!, clearNavigation: true);
                    });
                },
                onFailure: failure =>
                {
                    timer.Stop();
                    App.Logger.LogViewModelExecute(timer);
                    var failureReason = failure.ToEnum<RequirementsFailure>();
                    App.Logger.LogNavigateToRequirementsFailure(failureReason);
                    failureViewModel.PrepareForNavigation(failureReason);
                    App.MainWindow.DispatcherQueue.TryEnqueue(() => NavigationService.NavigateTo(typeof(RequirementsFailureViewModel).FullName!));
                });
        });
    }

    /// <summary>
    /// Show a page with a message about a fatal error.
    /// </summary>
    public void FatalErrorHandling()
    {
        NavigationViewHitTestVisible = false;
        _ = NavigationService.NavigateTo(typeof(FatalErrorViewModel).FullName!);
    }

    private void RadioButtonsGroup2Clicked(UIRadioButtonsGroup2Model group)
    {
        var selectedId = int.Parse(group.SelectedId);

        if (ApplicableModels.Contains(group) && group.DefaultId == selectedId)
        {
            ApplicableModels.Remove(group);
            App.Logger.LogApplicableModelRemoved(group.Name);
            return;
        }

        ApplicableModels.Add(group);
        App.Logger.LogApplicableModelAdded(group.Name, selectedId);
    }

    private void RadioButtonsGroup3Clicked(UIRadioButtonsGroup3Model group)
    {
        var selectedId = int.Parse(group.SelectedId);

        if (ApplicableModels.Contains(group))
        {
            if (group.DefaultId == selectedId)
            {
                ApplicableModels.Remove(group);
                App.Logger.LogApplicableModelRemoved(group.Name);
                return;
            }
            else
            {
                App.Logger.LogApplicableModelChanged(group.Name, selectedId);
                return;
            }
        }

        ApplicableModels.Add(group);
        App.Logger.LogApplicableModelAdded(group.Name, selectedId);
    }

    private void RadioButtonsGroup4Clicked(UIRadioButtonsGroup4Model group)
    {
        var selectedId = int.Parse(group.SelectedId);

        if (ApplicableModels.Contains(group))
        {
            if (group.DefaultId == selectedId)
            {
                ApplicableModels.Remove(group);
                App.Logger.LogApplicableModelRemoved(group.Name);
                return;
            }
            else
            {
                App.Logger.LogApplicableModelChanged(group.Name, selectedId);
                return;
            }
        }

        ApplicableModels.Add(group);
        App.Logger.LogApplicableModelAdded(group.Name, selectedId);
    }

    private void SetLogPageVisibility(bool isVisible)
    {
        LogPageVisible = isVisible;
        App.Logger.LogPageVisibility(isVisible);
    }

    /// <summary>
    /// Handles the navigation event of a menu item.
    /// </summary>
    /// <param name="sender">An object is the source of an event.</param>
    /// <param name="e">Provides data for navigation methods and event handlers that cannot cancel the navigation request.</param>
    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;
        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            SelectedNavigationViewItem = selectedItem;
        }
    }

    private async Task ApplicableModelsApplyAsync()
    {
        NavigationViewHitTestVisible = false;
        ProgressBarValue = 0;
        SetUpCustomizationsPanelText = ApplicableModels.Count == 1 ? "Panel_SetupCustomization_Applying".GetLocalized() : "Panel_SetupCustomizations_Applying".GetLocalized();
        SetUpCustomizationsPanelIsVisible = true;
        var callback = new Action(() => App.MainWindow.DispatcherQueue.TryEnqueue(() => ProgressBarValue = ProgressBarValue.Increase(ApplicableModels.Count)));
        App.Logger.LogStartApplicableModelsSetState();
        await modelService.SetModelsStateAsync(ApplicableModels, callback);
        ProgressBarValue = 0;
        SetUpCustomizationsPanelText = "OsRequirements_ReadWindowsSettings".GetLocalized();
        await modelService.GetModelsStateAsync(ApplicableModels, callback);
        ApplicableModels.Clear();
        App.Logger.LogApplicableModelsClear();
        groupPolicyService.UpdateLocalPolicy();
        EnvironmentHelper.RefreshUserDesktop();
        EnvironmentHelper.ForcedRefresh();
        processService.KillProcessByName("StartMenuExperienceHost");
        processService.KillProcessByName("explorer");
        notificationService.EnableToastNotification();
        defenderService.EnableControlledFolder();
        SetUpCustomizationsPanelIsVisible = false;
        NavigationViewHitTestVisible = true;
    }

    private async Task ApplicableModelsClearAsync()
    {
        NavigationViewHitTestVisible = false;
        App.Logger.LogApplicableModelsCanceled();
        ProgressBarValue = 0;
        SetUpCustomizationsPanelText = "OsRequirements_ReadWindowsSettings".GetLocalized();
        SetUpCustomizationsPanelIsVisible = true;
        var callback = new Action(() => App.MainWindow.DispatcherQueue.TryEnqueue(() => ProgressBarValue = ProgressBarValue.Increase(ApplicableModels.Count)));
        await modelService.GetModelsStateAsync(ApplicableModels, callback);
        ApplicableModels.Clear();
        App.Logger.LogApplicableModelsClear();
        SetUpCustomizationsPanelIsVisible = false;
        NavigationViewHitTestVisible = true;
    }

    private async Task SearchBoxQuerySubmittedAsync(AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            NavigationViewHitTestVisible = false;
            FoundModels = await modelService.GetModelsContainsTextAsync(JsonModels, args.QueryText);
            _ = NavigationService.NavigateTo(pageKey: typeof(SearchViewModel).FullName!, ignorePageType: true);
            NavigationViewHitTestVisible = true;
        }
    }

    private async Task OpenTaskSchedulerAsync()
    {
        await Task.Run(() => processService.StartProcessByName(name: "control.exe", arguments: "schedtasks"));
    }

    private void UIModelClicked(UIModel model)
    {
        if (ApplicableModels.Contains(model))
        {
            ApplicableModels.Remove(model);
            App.Logger.LogApplicableModelRemoved(model.Name);
            return;
        }

        ApplicableModels.Add(model);
        App.Logger.LogApplicableModelAdded(model.Name);
    }

    private void UwpForAllUsersClicked()
    {
        NavigationViewHitTestVisible = false;
        App.Logger.LogUwpForAllUsersState(UwpForAllUsersState);
        UwpForAllUsersState = !UwpForAllUsersState;
        UwpAppsModels = new ObservableCollection<UIModel>(UwpForAllUsersState ? uwpAllUsersModels : uwpCurrentUserModels);
        NavigationViewHitTestVisible = true;
    }

    private void UIUwpAppModelClicked(UIUwpAppModel model)
    {
        model.ForAllUsers = UwpForAllUsersState;
        model.Mutator = (packageFullName, removeForAll) => packagesService.RemovePackage(packageName: model.Title, forAllUsers: model.ForAllUsers);
        UIModelClicked(model);
    }
}
