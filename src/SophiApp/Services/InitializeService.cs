// <copyright file="InitializeService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SophiApp.Contracts.Services;
using SophiApp.Views;

/// <inheritdoc/>
public class InitializeService : IInitializeService
{
    private readonly IThemesService themesService;
    private readonly ICommonDataService commonDataService;
    private readonly ISettingsService settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializeService"/> class.
    /// </summary>
    /// <param name="themesService">A service for working with app themes.</param>
    /// <param name="commonDataService">A service for working with common app data.</param>
    /// <param name="settingsService">A service for working with app settings.</param>
    public InitializeService(
        IThemesService themesService,
        ICommonDataService commonDataService,
        ISettingsService settingsService)
    {
        this.themesService = themesService;
        this.commonDataService = commonDataService;
        this.settingsService = settingsService;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(object args)
    {
        await settingsService.InitializeAsync();
        InitializeAppWindow();
        await themesService.InitializeAsync();
        await themesService.SetRequestedThemeAsync();
    }

    private void InitializeAppWindow()
    {
        App.MainWindow.Title = commonDataService.GetFullName();

        if (App.MainWindow.Content == null)
        {
            var shell = App.GetService<ShellPage>() as UIElement;
            App.MainWindow.Content = shell ?? new Frame();
        }

        App.MainWindow.CenterOnScreen();
        App.MainWindow.Activate();
    }
}
