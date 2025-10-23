// <copyright file="SettingsService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;

using Microsoft.UI.Xaml;
using SophiApp.Contracts.Services;
using SophiApp.Extensions;
using SophiApp.Helpers;
using Windows.Graphics;
using Windows.Storage;

/// <inheritdoc/>
public class SettingsService : ISettingsService
{
    private const string AppThemeKey = "AppTheme";
    private const string AppWindowPositionXKey = "AppWindowPositionX";
    private const string AppWindowPositionYKey = "AppWindowPositionY";
    private const string AppWindowHeightKey = "AppWindowHeight";
    private const string AppWindowWidthKey = "AppWindowWidth";
    private const string AppWindowStateKey = "AppWindowState";
    private const string SettingsFile = "Settings.json";
    private const string TextDescriptionSizeKey = "TextDescriptionSize";
    private const string TextTitleSizeKey = "TextTitleSize";

    private readonly IFileService fileService;
    private readonly string settingsFolder = AppContext.BaseDirectory;
    private IDictionary<string, object>? settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="fileService"><inheritdoc/></param>
    public SettingsService(IFileService fileService)
    {
        this.fileService = fileService;
    }

    /// <inheritdoc/>
    public double AppWindowMinHeight => 858;

    /// <inheritdoc/>
    public double AppWindowMinWidth => 1185;

    /// <inheritdoc/>
    public int DescriptionTextMinSize => 14;

    /// <inheritdoc/>
    public int DescriptionTextMaxSize => 24;

    /// <inheritdoc/>
    public int TitleTextMinSize => 16;

    /// <inheritdoc/>
    public int TitleTextMaxSize => 26;

    /// <inheritdoc/>
    public async Task InitializeAsync() => settings = await Task.Run(() => fileService.ReadFromJson<IDictionary<string, object>>(settingsFolder, SettingsFile)) ?? new Dictionary<string, object>();

    /// <inheritdoc/>
    public async Task<PointInt32> ReadAppWindowPositionAsync()
    {
        var x = await ReadSettingAsync<int>(AppWindowPositionXKey);
        var y = await ReadSettingAsync<int>(AppWindowPositionYKey);
        return new PointInt32(x, y);
    }

    /// <inheritdoc/>
    public async Task<double> ReadAppWindowHeightAsync()
    {
        var height = await ReadSettingAsync<double>(AppWindowHeightKey);
        return height > AppWindowMinHeight ? height : AppWindowMinHeight;
    }

    /// <inheritdoc/>
    public async Task<WindowState> ReadAppWindowStateAsync() => await ReadSettingAsync<WindowState>(AppWindowStateKey);

    /// <inheritdoc/>
    public async Task<double> ReadAppWindowWidthAsync()
    {
        var width = await ReadSettingAsync<double>(AppWindowWidthKey);
        return width > AppWindowMinWidth ? width : AppWindowMinWidth;
    }

    /// <inheritdoc/>
    public async Task<int> ReadTextDescriptionSizeAsync()
    {
        var descriptionTextSize = await ReadSettingAsync<int>(TextDescriptionSizeKey);
        return descriptionTextSize > DescriptionTextMinSize && descriptionTextSize <= DescriptionTextMaxSize ? descriptionTextSize : DescriptionTextMinSize;
    }

    /// <inheritdoc/>
    public async Task<int> ReadTextTitleSizeAsync()
    {
        var titleTextSize = await ReadSettingAsync<int>(TextTitleSizeKey);
        return titleTextSize > TitleTextMinSize && titleTextSize <= TitleTextMaxSize ? titleTextSize : TitleTextMinSize;
    }

    /// <inheritdoc/>
    public async Task<ElementTheme> ReadThemeAsync() => await ReadSettingAsync<ElementTheme>(AppThemeKey);

    /// <inheritdoc/>
    public async Task SaveAppWindowPositionAsync(PointInt32 point)
    {
        await SaveSettingAsync(AppWindowPositionXKey, point.X);
        await SaveSettingAsync(AppWindowPositionYKey, point.Y);
    }

    /// <inheritdoc/>
    public async Task SaveAppWindowSizeAsync(double height, double width)
    {
        await SaveSettingAsync(AppWindowHeightKey, height);
        await SaveSettingAsync(AppWindowWidthKey, width);
    }

    /// <inheritdoc/>
    public async Task SaveAppWindowStateAsync(WindowState state) => await SaveSettingAsync(AppWindowStateKey, state);

    /// <inheritdoc/>
    public async Task SaveTextDescriptionSizeAsync(int size) => await SaveSettingAsync(TextDescriptionSizeKey, size);

    /// <inheritdoc/>
    public async Task SaveTextTitleSizeAsync(int size) => await SaveSettingAsync(TextTitleSizeKey, size);

    /// <inheritdoc/>
    public async Task SaveThemeAsync(ElementTheme theme) => await SaveSettingAsync(AppThemeKey, theme);

    private async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await JsonExtensions.ToObjectAsync<T>((string)obj);
            }
        }
        else
        {
            if (settings != null && settings.TryGetValue(key, out var obj))
            {
                return await JsonExtensions.ToObjectAsync<T>((string)obj);
            }
        }

        return default;
    }

    private async Task SaveSettingAsync<T>(string key, T value)
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = await JsonExtensions.StringifyAsync(value!);
        }
        else
        {
            settings![key] = await JsonExtensions.StringifyAsync(value!);
            fileService.SaveToJson(settingsFolder, SettingsFile, settings);
        }
    }
}
