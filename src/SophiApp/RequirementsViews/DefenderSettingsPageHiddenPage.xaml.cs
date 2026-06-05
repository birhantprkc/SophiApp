// <copyright file="DefenderSettingsPageHiddenPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="DefenderSettingsPageHiddenPage"/>.
    /// </summary>
    public sealed partial class DefenderSettingsPageHiddenPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderSettingsPageHiddenPage"/> class.
        /// </summary>
        public DefenderSettingsPageHiddenPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<DefenderSettingsPageHiddenViewModel>();
        }

        /// <summary>
        /// Gets <see cref="DefenderSettingsPageHiddenViewModel"/>.
        /// </summary>
        public DefenderSettingsPageHiddenViewModel ViewModel { get; }
    }
}
