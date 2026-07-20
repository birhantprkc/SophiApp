// <copyright file="ShellViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SophiApp.Contracts.Services;
using SophiApp.ControlTemplates;
using SophiApp.Extensions;
using SophiApp.Helpers;
using SophiApp.Models;
using SophiApp.RequirementsViews;
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
    private readonly IGroupPolicyService groupPolicyService;
    private readonly IModelService modelService;
    private readonly IPowerShellService powerShellService;
    private readonly IProcessService processService;
    private readonly IRegistryService registryService;
    private readonly IRequirementsService requirementsService;
    private readonly ISettingsService settingsService;
    private readonly IUpdateService updateService;
    private readonly StartupViewModel startupModel;
    private bool logPageVisibility;
    private bool navigationViewHitTestVisible = false;
    private TaskCompletionSource<bool>? taskContinueSource;

    [ObservableProperty]
    private bool isBackEnabled;
    [ObservableProperty]
    private bool setUpCustomizationsPanelIsVisible = false;
    [ObservableProperty]
    private bool uwpForAllUsersState = true;
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
    /// <param name="dataService">A service for working with common app data.</param>
    /// <param name="groupPolicyService">A service for working with group policy API.</param>
    /// <param name="modelService">A service for working with UI models using MVVM pattern.</param>
    /// <param name="navigationService">Page navigation service.</param>
    /// <param name="navigationViewService">A service for navigating to View.</param>
    /// <param name="powerShellService">A service for working with Windows PowerShell API.</param>
    /// <param name="notificationService">A service for working with toast notifications API.</param>
    /// <param name="packagesService">A service for working with appx packages API.</param>
    /// <param name="processService">A service for working with Windows process API.</param>
    /// <param name="registryService">A service for working with Windows registry API.</param>
    /// <param name="requirementsService">Service for working with OS requirements.</param>
    /// <param name="settingsService">A service for working with app settings.</param>
    /// <param name="updateService">Determines whether the Windows Update API is used to obtain updates for other Microsoft products.</param>
    /// <param name="startupViewModel">Implements the <see cref="StartupViewModel"/> class.</param>
    public ShellViewModel(
        IAppNotificationService notificationService,
        IAppxPackagesService packagesService,
        ICommonDataService dataService,
        IGroupPolicyService groupPolicyService,
        IModelService modelService,
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IPowerShellService powerShellService,
        IProcessService processService,
        IRegistryService registryService,
        IRequirementsService requirementsService,
        ISettingsService settingsService,
        IUpdateService updateService,
        StartupViewModel startupViewModel)
    {
        this.dataService = dataService;
        this.groupPolicyService = groupPolicyService;
        this.modelService = modelService;
        this.notificationService = notificationService;
        this.packagesService = packagesService;
        this.powerShellService = powerShellService;
        this.processService = processService;
        this.registryService = registryService;
        this.requirementsService = requirementsService;
        this.settingsService = settingsService;
        this.updateService = updateService;
        startupModel = startupViewModel;
        NavigationViewService = navigationViewService;
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        delimiter = this.dataService.GetDelimiter();

        ApplicableModelsApply_Command = new AsyncRelayCommand(ApplicableModelsApplyAsync);
        ApplicableModelsClear_Command = new AsyncRelayCommand(ApplicableModelsClearAsync);
        BitLockerProtectionStatus_Command = new RelayCommand<bool>(SetBitLockerProtectionStatus);
        ContinueRequirementActionsExecute_Command = new RelayCommand(ContinueRequirementActionsExecute);
        DeleteLGPOFile_Command = new RelayCommand(() => DebugOptions.DeleteLGPOFile = !DebugOptions.DeleteLGPOFile);
        OpenBitLockerSettings_Command = new RelayCommand(() => processService.StartProcessByName("control.exe", "/name Microsoft.BitLockerDriveEncryption"));
        OpenDefenderControlledFolder_Command = new RelayCommand(() => processService.StartProcessByName("explorer.exe", "windowsdefender://RansomwareProtection"));
        OpenHostsFolder_Command = new RelayCommand(() => processService.StartProcessByName("explorer.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers\\etc")));
        OpenTaskScheduler_Command = new AsyncRelayCommand(OpenTaskSchedulerAsync);
        RadioButtonsGroup2Clicked_Command = new RelayCommand<UIRadioButtonsGroup2Model>(group => RadioButtonsGroup2Clicked(group!));
        RadioButtonsGroup3Clicked_Command = new RelayCommand<UIRadioButtonsGroup3Model>(group => RadioButtonsGroup3Clicked(group!));
        RadioButtonsGroup4Clicked_Command = new RelayCommand<UIRadioButtonsGroup4Model>(group => RadioButtonsGroup4Clicked(group!));
        SetLogPageVisibility_Command = new RelayCommand<bool>(SetLogPageVisibility);
        SetShowFunctionsInfo_Command = new RelayCommand(() => DebugOptions.ShowFunctionsInfo = !DebugOptions.ShowFunctionsInfo);
        SearchBoxQuerySubmitted_Command = new AsyncRelayCommand<AutoSuggestBoxQuerySubmittedEventArgs>(args => SearchBoxQuerySubmittedAsync(args!));
        UIModelClicked_Command = new RelayCommand<UIModel>(model => UIModelClicked(model!));
        UIUwpAppModelClicked_Command = new RelayCommand<UIUwpAppModel>(model => UIUwpAppModelClicked(model!));
        UwpForAllUsersClicked_Command = new RelayCommand(UwpForAllUsersClicked);
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
    /// Gets <see cref="IRelayCommand"/> to click <see cref="BitLockerProtectionStatusPage"/> button.
    /// </summary>
    public IRelayCommand<bool> BitLockerProtectionStatus_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to continue performing <see cref="RequirementAction"/> after fail result.
    /// </summary>
    public IRelayCommand ContinueRequirementActionsExecute_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an "Delete LGPO.txt file" CheckBox in Settings page.
    /// </summary>
    public IRelayCommand DeleteLGPOFile_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to open BitLocker settings.
    /// </summary>
    public IRelayCommand OpenBitLockerSettings_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to open Microsoft Defender controlled folder settings.
    /// </summary>
    public IRelayCommand OpenDefenderControlledFolder_Command { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to open hosts file folder in explorer.
    /// </summary>
    public IRelayCommand OpenHostsFolder_Command { get; }

    /// <summary>
    /// Gets <see cref="IAsyncRelayCommand"/> to click an "Open" button in the Task Scheduler page.
    /// </summary>
    public IAsyncRelayCommand OpenTaskScheduler_Command { get; }

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
    /// Gets <see cref="IRelayCommand"/> to click an "Show functions name and ID" CheckBox in Settings page.
    /// </summary>
    public IRelayCommand SetShowFunctionsInfo_Command { get; }

    /// <summary>
    /// Gets <see cref="IAsyncRelayCommand"/> to click "Search" in AutoSuggestBox.
    /// </summary>
    public IAsyncRelayCommand<AutoSuggestBoxQuerySubmittedEventArgs> SearchBoxQuerySubmitted_Command { get; }

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
    /// Gets app debug mode options.
    /// </summary>
    public DebugOptions DebugOptions { get; } = new ();

    /// <summary>
    /// Gets and saves the app font sizes to a setting file.
    /// </summary>
    public FontOptions FontOptions { get; } = new ();

    /// <summary>
    /// Gets the info badges counters state by <see cref="UICategoryTag"/>.
    /// </summary>
    public InfoBadgeCounters InfoBadgeCounters { get; } = new ();

    /// <summary>
    /// Gets or sets a value indicating whether log page visibility.
    /// </summary>
    public bool LogPageVisibility
    {
        get => logPageVisibility;
        set
        {
            if (logPageVisibility != value)
            {
                logPageVisibility = value;
                App.Logger.LogPageVisibility(value);
                settingsService.SaveLogPageVisibility(value);
                OnPropertyChanged(nameof(LogPageVisibility));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether NavigationView items hit test visible property.
    /// </summary>
    public bool NavigationViewHitTestVisible
    {
        get => navigationViewHitTestVisible;
        private set => SetProperty(ref navigationViewHitTestVisible, value, nameof(NavigationViewHitTestVisible));
    }

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
        requirementsService.ActionForDebug = settingsService.ReadDebugRequirementAction();
        await RunRequirementActionsAsync();
        await GenerateUIAsync();
    }

    /// <summary>
    /// Show a page with a message about a fatal error.
    /// </summary>
    public void FatalErrorHandling()
    {
        NavigationViewHitTestVisible = false;
        _ = NavigationService.NavigateTo(typeof(FatalErrorViewModel).FullName!);
    }

    private async Task ApplicableModelsApplyAsync()
    {
        NavigationViewHitTestVisible = false;
        SetUpCustomizationsPanelText = ApplicableModels.Count == 1 ? "Panel_SetupCustomization_Applying".GetLocalized() : "Panel_SetupCustomizations_Applying".GetLocalized();
        SetUpCustomizationsPanelIsVisible = true;
        App.Logger.LogStartApplicableModelsSetState();
        await modelService.SetModelsStateAsync(ApplicableModels);
        SetUpCustomizationsPanelText = "OsRequirements_ReadWindowsSettings".GetLocalized();
        await modelService.GetModelsStateAsync(ApplicableModels);
        ApplicableModels.Clear();
        InfoBadgeCounters.ResetAll();
        App.Logger.LogApplicableModelsClear();
        groupPolicyService.UpdatePolicy(deleteConfig: DebugOptions.DeleteLGPOFile);
        EnvironmentHelper.RefreshUserDesktop();
        EnvironmentHelper.ForcedRefresh();
        processService.KillProcessByName("StartMenuExperienceHost");
        processService.KillProcessByName("explorer");
        notificationService.EnableToastNotification();
        SetUpCustomizationsPanelIsVisible = false;
        NavigationViewHitTestVisible = true;
    }

    private async Task ApplicableModelsClearAsync()
    {
        NavigationViewHitTestVisible = false;
        App.Logger.LogApplicableModelsCanceled();
        SetUpCustomizationsPanelText = "OsRequirements_ReadWindowsSettings".GetLocalized();
        SetUpCustomizationsPanelIsVisible = true;
        await modelService.GetModelsStateAsync(ApplicableModels);
        ApplicableModels.Clear();
        InfoBadgeCounters.ResetAll();
        App.Logger.LogApplicableModelsClear();
        SetUpCustomizationsPanelIsVisible = false;
        NavigationViewHitTestVisible = true;
    }

    private void ContinueRequirementActionsExecute()
    {
        Task.Delay(80).Wait();
        taskContinueSource?.SetResult(true);
        App.MainWindow.DispatcherQueue.TryEnqueue(() => NavigationService.NavigateTo(page: typeof(StartupViewModel).FullName!, clearHistory: true, disablePageAnimation: true));
    }

    private async Task GenerateUIAsync()
    {
        _ = App.MainWindow.DispatcherQueue.TryEnqueue(() => startupModel.StatusText = "OsRequirements_ReadWindowsSettings".GetLocalized());
        JsonModels = await modelService.BuildJsonModelsAsync();
        await modelService.GetModelsState(JsonModels);
        _ = App.MainWindow.DispatcherQueue.TryEnqueue(() => startupModel.StatusText = "OsRequirements_GeneratingUserInterface".GetLocalized());
        uwpAllUsersModels = await modelService.BuildUwpAppModelsAsync(forAllUsers: true);
        uwpCurrentUserModels = await modelService.BuildUwpAppModelsAsync(forAllUsers: false);
        UwpAppsModels = new (uwpAllUsersModels);
        registryService.UseLatestCLR();
        _ = App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            NavigationViewHitTestVisible = true;
            NavigationService.NavigateTo(page: typeof(PrivacyViewModel).FullName!, clearHistory: true);
        });
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

    private async Task OpenTaskSchedulerAsync() => await Task.Run(() => processService.StartProcessByName(name: "control.exe", arguments: "schedtasks"));

    private void RadioButtonsGroup2Clicked(UIRadioButtonsGroup2Model group)
    {
        var selectedId = int.Parse(group.SelectedId);

        if (ApplicableModels.Contains(group) && group.DefaultId == selectedId)
        {
            ApplicableModels.Remove(group);
            InfoBadgeCounters.DecrementCategory(group.Tag);
            App.Logger.LogApplicableModelRemoved(group.Name);
            return;
        }

        ApplicableModels.Add(group);
        InfoBadgeCounters.IncrementCategory(group.Tag);
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
                InfoBadgeCounters.DecrementCategory(group.Tag);
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
        InfoBadgeCounters.IncrementCategory(group.Tag);
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
                InfoBadgeCounters.DecrementCategory(group.Tag);
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
        InfoBadgeCounters.IncrementCategory(group.Tag);
        App.Logger.LogApplicableModelAdded(group.Name, selectedId);
    }

    private async Task RunRequirementActionsAsync()
    {
        foreach (var action in requirementsService.Actions)
        {
            _ = App.MainWindow.DispatcherQueue.TryEnqueue(() => startupModel.StatusText = action.DisplayText!);
            await Task.Delay(100);
            var timer = Stopwatch.StartNew();
            var result = action.Execute();
            timer.Stop();
            App.Logger.LogRequirementsActionExecute(action.Execute.Method.Name, timer);

            if (result != RequirementsResult.AllCorrect)
            {
                App.Logger.LogRequirementsFailureResult(result);
                _ = App.MainWindow.DispatcherQueue.TryEnqueue(() => NavigationService.NavigateTo(result));
                updateService.RunUpdateByReason(result);
                taskContinueSource = new ();
                await taskContinueSource.Task;
            }
        }
    }

    private void SetBitLockerProtectionStatus(bool runDecrypt)
    {
        if (runDecrypt)
        {
            powerShellService.Invoke("Disable-BitLocker -MountPoint $env:SystemDrive");
        }

        ContinueRequirementActionsExecute();
    }

    private void SetLogPageVisibility(bool visible) => LogPageVisibility = visible;

    private async Task SearchBoxQuerySubmittedAsync(AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            NavigationViewHitTestVisible = false;
            FoundModels = await modelService.GetModelsContainsTextAsync(JsonModels, args.QueryText);
            _ = NavigationService.NavigateTo(page: typeof(SearchViewModel).FullName!, disablePageAnimation: true);
            NavigationViewHitTestVisible = true;
        }
    }

    private void UIModelClicked(UIModel model)
    {
        if (ApplicableModels.Contains(model))
        {
            ApplicableModels.Remove(model);
            InfoBadgeCounters.DecrementCategory(model.Tag);
            App.Logger.LogApplicableModelRemoved(model.Name);
            return;
        }

        ApplicableModels.Add(model);
        InfoBadgeCounters.IncrementCategory(model.Tag);
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
        model.Mutator = (packageFullName, removeForAll) => packagesService.RemovePackage(packageId: model.Title, allUsers: model.ForAllUsers);
        UIModelClicked(model);
    }
}
