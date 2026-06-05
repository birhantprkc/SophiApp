// <copyright file="WinUnsupportedBuildPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="WinUnsupportedBuildPage"/>.
    /// </summary>
    public sealed partial class WinUnsupportedBuildPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WinUnsupportedBuildPage"/> class.
        /// </summary>
        public WinUnsupportedBuildPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<WinUnsupportedBuildViewModel>();
        }

        /// <summary>
        /// Gets <see cref="WinUnsupportedBuildViewModel"/>.
        /// </summary>
        public WinUnsupportedBuildViewModel ViewModel { get; }
    }
}
