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
    private Microsoft.UI.Xaml.ElementTheme elementTheme;

    [ObservableProperty]
    private string delimiter;

    [ObservableProperty]
    private bool navigationViewHitTestVisible;

    [ObservableProperty]
    private ObservableCollection<Helpers.AppTheme> themes =
    [
        new (Microsoft.UI.Xaml.ElementTheme.Default, "Settings_Themes_Default"), new (Microsoft.UI.Xaml.ElementTheme.Light, "Settings_Themes_Light"), new (Microsoft.UI.Xaml.ElementTheme.Dark, "Settings_Themes_Dark"),
    ];

    [ObservableProperty]
    private string version;
    private Helpers.AppTheme selectedTheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="themesService">A service for working with app themes.</param>
    /// <param name="commonDataService">A service for transferring app data between layers of DI.</param>
    /// <param name="httpService">A service for working with HTTP.</param>
    /// <param name="shellViewModel">Implements the <see cref="ShellViewModel"/> class.</param>
    public SettingsViewModel(IThemesService themesService, ICommonDataService commonDataService, IHttpService httpService, ShellViewModel shellViewModel)
    {
        build = commonDataService.GetBuildName();
        delimiter = commonDataService.GetDelimiter();
        FontOptions = shellViewModel.FontOptions;
        NavigationViewHitTestVisible = shellViewModel.NavigationViewHitTestVisible;
        LogPageVisible = shellViewModel.LogPageVisible;
        LogPageVisibleCommand = shellViewModel.SetLogPageVisibility_Command;
        OpenLinkCommand = new AsyncRelayCommand<string>(url => httpService.OpenUrlAsync(url));
        selectedTheme = themes.First(wrapper => wrapper.ElementTheme.Equals(themesService.Theme));
        this.themesService = themesService;
        version = commonDataService.GetFullName();
    }

    /// <summary>
    /// Gets or saves the app font sizes to a setting file.
    /// </summary>
    public FontOptions FontOptions { get; }

    /// <summary>
    /// Gets or sets app selected theme.
    /// </summary>
    public Helpers.AppTheme SelectedTheme
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
    /// Gets a resource using an identifier.
    /// </summary>
    public IRelayCommand OpenLinkCommand { get; }

    /// <summary>
    /// Gets <see cref="IRelayCommand"/> to click an "Show log page in navigation menu" CheckBox in Settings page.
    /// </summary>
    public IRelayCommand<bool> LogPageVisibleCommand { get; }
}
