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
    private ObservableCollection<ElementThemeWrapper> themes =
    [
        new (ElementTheme.Default, "Settings_Themes_Default"), new (ElementTheme.Light, "Settings_Themes_Light"), new (ElementTheme.Dark, "Settings_Themes_Dark"),
    ];

    [ObservableProperty]
    private string version;
    private ElementThemeWrapper selectedTheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="themesService">A service for working with app themes.</param>
    /// <param name="commonDataService">A service for transferring app data between layers of DI.</param>
    /// <param name="uriService">A service for working with URI.</param>
    /// <param name="shellViewModel">Implements the <see cref="ShellViewModel"/> class.</param>
    public SettingsViewModel(IThemesService themesService, ICommonDataService commonDataService, IUriService uriService, ShellViewModel shellViewModel)
    {
        build = commonDataService.GetBuildName();
        delimiter = commonDataService.GetDelimiter();
        FontOptions = shellViewModel.FontOptions;
        NavigationViewHitTestVisible = shellViewModel.NavigationViewHitTestVisible;
        LogPageVisible = shellViewModel.LogPageVisible;
        LogPageVisibleCommand = shellViewModel.SetLogPageVisibility_Command;
        OpenLinkCommand = new AsyncRelayCommand<string>(url => uriService.OpenUrlAsync(url!));
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
    public ElementThemeWrapper SelectedTheme
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
