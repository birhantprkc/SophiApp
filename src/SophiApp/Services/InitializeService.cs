// <copyright file="InitializeService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;
using Microsoft.UI.Xaml.Controls;
using SophiApp.Contracts.Services;
using SophiApp.Helpers;
using SophiApp.ViewModels;
using SophiApp.Views;
using System.Threading.Tasks;

/// <inheritdoc/>
public class InitializeService : IInitializeService
{
    private readonly IThemesService themesService;
    private readonly ICommonDataService commonDataService;
    private readonly ISettingsService settingsService;
    private readonly ShellViewModel shellViewModel;
    private readonly IDisplayService displayService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializeService"/> class.
    /// </summary>
    /// <param name="themesService">A service for working with app themes.</param>
    /// <param name="commonDataService">A service for working with common app data.</param>
    /// <param name="settingsService">A service for working with app settings.</param>
    /// <param name="shellViewModel">Implements the <see cref="ShellViewModel"/> class.</param>
    /// <param name="displayService">A service for working with display API.</param>
    public InitializeService(
        IThemesService themesService,
        ICommonDataService commonDataService,
        ISettingsService settingsService,
        ShellViewModel shellViewModel,
        IDisplayService displayService)
    {
        this.themesService = themesService;
        this.commonDataService = commonDataService;
        this.settingsService = settingsService;
        this.shellViewModel = shellViewModel;
        this.displayService = displayService;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(object args)
    {
        commonDataService.Initialize();
        await settingsService.InitializeAsync();
        await InitializeAppWindow();
        await themesService.InitializeAsync();
        await themesService.SetRequestedThemeAsync();
        await shellViewModel.FontOptions.InitializeAsync();
    }

    private async Task InitializeAppWindow()
    {
        App.MainWindow.Title = commonDataService.GetFullName();
        App.MainWindow.Content = App.MainWindow.Content is null ? App.GetService<ShellPage>() : new Frame();
        App.MainWindow.MinHeight = settingsService.AppWindowMinHeight;
        App.MainWindow.MinWidth = settingsService.AppWindowMinWidth;

        var windowState = await settingsService.ReadAppWindowStateAsync();
        var windowPosition = await settingsService.ReadAppWindowPositionAsync();
        var windowHeight = await settingsService.ReadAppWindowHeightAsync();
        var windowWidth = await settingsService.ReadAppWindowWidthAsync();

        var displayArea = await displayService.GetDisplayAreaAsync();
        var positionIsValid = windowPosition.X > 0 && windowPosition.Y > 0;
        var heightIsValid = windowPosition.Y + windowHeight <= (displayArea?.WorkArea.Height ?? -1);
        var widthIsValid = windowPosition.X + windowWidth <= (displayArea?.WorkArea.Width ?? -1);

        if (windowState == WindowState.Maximized)
        {
            App.MainWindow.WindowState = WindowState.Maximized;
        }
        else if (positionIsValid && heightIsValid && widthIsValid)
        {
            App.MainWindow.MoveAndResize(x: windowPosition.X, y: windowPosition.Y, width: windowWidth, height: windowHeight);
        }
        else
        {
            App.MainWindow.Height = settingsService.AppWindowMinHeight;
            App.MainWindow.Width = settingsService.AppWindowMinWidth;
            App.MainWindow.CenterOnScreen();
        }

        TitleBarHelper.ApplySystemThemeToCaptionButtons();
        App.MainWindow.Activate();
    }
}
