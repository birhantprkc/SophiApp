// <copyright file="SettingsViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using SophiApp.Contracts.Services;
using SophiApp.Helpers;
using Views;

/// <summary>
/// Implements the <see cref="SettingsViewModel"/> class.
/// </summary>
public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemesService themesService;

    [ObservableProperty]
    private string build;

    [ObservableProperty]
    private ElementTheme elementTheme;

    [ObservableProperty]
    private string delimiter;

    [ObservableProperty]
    private bool navigationViewHitTestVisible;

    [ObservableProperty]
    private ObservableCollection<AppTheme> themes =
    [
        new (ElementTheme.Default, "Settings_Themes_Default"), new (ElementTheme.Light, "Settings_Themes_Light"), new (ElementTheme.Dark, "Settings_Themes_Dark"),
    ];

    [ObservableProperty]
    private string version;
    private AppTheme selectedTheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="themesService">A service for working with app themes.</param>
    /// <param name="dataService">A service for transferring app data between layers of DI.</param>
    /// <param name="httpService">A service for working with HTTP.</param>
    /// <param name="shellViewModel">Implements the <see cref="ShellViewModel"/> class.</param>
    public SettingsViewModel(IThemesService themesService, ICommonDataService dataService, IHttpService httpService, ShellViewModel shellViewModel)
    {
        build = dataService.GetBuildName();
        delimiter = dataService.GetDelimiter();
        DebugOptions = shellViewModel.DebugOptions;
        FontOptions = shellViewModel.FontOptions;
        NavigationViewHitTestVisible = shellViewModel.NavigationViewHitTestVisible;
        LogPageVisible = shellViewModel.LogPageVisible;
        DeleteLGPOFileCommand = shellViewModel.DeleteLGPOFile_Command;
        LogPageVisibleCommand = shellViewModel.SetLogPageVisibility_Command;
        OpenLinkCommand = new AsyncRelayCommand<string>(httpService.OpenUrlAsync);
        SetShowFunctionsInfoCommand = shellViewModel.SetShowFunctionsInfo_Command;
        selectedTheme = themes.First(wrapper => wrapper.ElementTheme.Equals(themesService.Theme));
        this.themesService = themesService;
        version = dataService.GetFullName();
    }

    /// <summary>
    /// Gets app debug mode options.
    /// </summary>
    public DebugOptions DebugOptions { get; }

    /// <summary>
    /// Gets or saves the app font sizes to a setting file.
    /// </summary>
    public FontOptions FontOptions { get; }

    /// <summary>
    /// Gets or sets app selected theme.
    /// </summary>
    public AppTheme SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (value != selectedTheme)
            {
                selectedTheme = value;
                _ = themesService.SetThemeAsync(selectedTheme.ElementTheme);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="LogPage"/> is visible.
    /// </summary>
    public bool LogPageVisible { get; set; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an "Delete LGPO.txt file" CheckBox in Settings page.
    /// </summary>
    public IRelayCommand DeleteLGPOFileCommand { get; }

    /// <summary>
    /// Gets a resource using an identifier.
    /// </summary>
    public IRelayCommand OpenLinkCommand { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an "Show functions name and ID" CheckBox in Settings page.
    /// </summary>
    public IRelayCommand SetShowFunctionsInfoCommand { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an "Show log page in navigation menu" CheckBox in Settings page.
    /// </summary>
    public IRelayCommand<bool> LogPageVisibleCommand { get; }
}
