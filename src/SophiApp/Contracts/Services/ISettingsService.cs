// <copyright file="ISettingsService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services;

using Microsoft.UI.Xaml;

/// <summary>
/// A service for working with app settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a minimum font size for UI elements description.
    /// </summary>
    public int TextDescriptionMinSize { get; }

    /// <summary>
    /// Gets a maximum font size for UI elements description.
    /// </summary>
    public int TextDescriptionMaxSize { get; }

    /// <summary>
    /// Gets a minimum font size for UI elements title.
    /// </summary>
    public int TextTitleMinSize { get; }

    /// <summary>
    /// Gets a maximum font size for UI elements title.
    /// </summary>
    public int TextTitleMaxSize { get; }

    /// <summary>
    /// Initialize <see cref="ISettingsService"/> data.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Read UI elements descriptions size from a file.
    /// </summary>
    Task<int> ReadTextDescriptionSizeAsync();

    /// <summary>
    /// Read UI elements title size from a file.
    /// </summary>
    Task<int> ReadTextTitleSizeAsync();

    /// <summary>
    /// Reads app theme from a file.
    /// </summary>
    Task<ElementTheme> ReadThemeAsync();

    /// <summary>
    /// Write UI elements description size to a file.
    /// </summary>
    /// <param name="size">UI elements description size.</param>
    Task SaveTextDescriptionSizeAsync(int size);

    /// <summary>
    /// Write UI elements title size to a file.
    /// </summary>
    /// <param name="size">UI elements title size.</param>
    Task SaveTextTitleSizeAsync(int size);

    /// <summary>
    /// Write app theme to a file.
    /// </summary>
    /// <param name="theme">Specifies a UI theme that should be used for UI elements.</param>
    Task SaveThemeAsync(ElementTheme theme);
}
