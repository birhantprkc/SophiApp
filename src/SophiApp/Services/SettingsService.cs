// <copyright file="SettingsService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;

using Microsoft.UI.Xaml;
using SophiApp.Contracts.Services;
using SophiApp.Extensions;
using SophiApp.Helpers;
using Windows.Storage;

/// <inheritdoc/>
public class SettingsService : ISettingsService
{
    private const string AppThemeSettingKey = "AppTheme";
    private const string SettingsFile = "Settings.json";
    private const string TextDescriptionSizeSettingKey = "TextDescriptionSize";
    private const string TextTitleSizeSettingKey = "TextTitleSize";

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
    public int TextDescriptionMinSize { get; } = 14;

    /// <inheritdoc/>
    public int TextDescriptionMaxSize { get; } = 24;

    /// <inheritdoc/>
    public int TextTitleMinSize { get; } = 16;

    /// <inheritdoc/>
    public int TextTitleMaxSize { get; } = 26;

    /// <inheritdoc/>
    public async Task InitializeAsync() => settings = await Task.Run(() => fileService.ReadFromJson<IDictionary<string, object>>(settingsFolder, SettingsFile)) ?? new Dictionary<string, object>();

    /// <inheritdoc/>
    public async Task<int> ReadTextDescriptionSizeAsync()
    {
        var textDescriptionSize = await ReadSettingAsync<int>(TextDescriptionSizeSettingKey);
        return textDescriptionSize > 0 && textDescriptionSize <= TextDescriptionMaxSize ? textDescriptionSize : TextDescriptionMinSize;
    }

    /// <inheritdoc/>
    public async Task<int> ReadTextTitleSizeAsync()
    {
        var textTitleSize = await ReadSettingAsync<int>(TextTitleSizeSettingKey);
        return textTitleSize > 0 && textTitleSize <= TextTitleMaxSize ? textTitleSize : TextTitleMinSize;
    }

    /// <inheritdoc/>
    public async Task<ElementTheme> ReadThemeAsync() => await ReadSettingAsync<ElementTheme>(AppThemeSettingKey);

    /// <inheritdoc/>
    public async Task SaveTextDescriptionSizeAsync(int size) => await SaveSettingAsync(TextDescriptionSizeSettingKey, size);

    /// <inheritdoc/>
    public async Task SaveTextTitleSizeAsync(int size) => await SaveSettingAsync(TextTitleSizeSettingKey, size);

    /// <inheritdoc/>
    public async Task SaveThemeAsync(ElementTheme theme) => await SaveSettingAsync(AppThemeSettingKey, theme);

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
