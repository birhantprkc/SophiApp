// <copyright file="IInitializeService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services;

/// <summary>
/// Initializes with app services data.
/// </summary>
public interface IInitializeService
{
    /// <summary>
    /// Initializes the app services data.
    /// </summary>
    /// <param name="args">App launch arguments.</param>
    Task InitializeServicesAsync(object args);

    /// <summary>
    /// Initializes the <see cref="MainWindow"/> data.
    /// </summary>
    Task InitializeMainWindowAsync();
}
