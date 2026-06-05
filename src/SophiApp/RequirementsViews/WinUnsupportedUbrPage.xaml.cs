// <copyright file="WinUnsupportedUbrPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="WinUnsupportedUbrPage"/> class.
    /// </summary>
    public sealed partial class WinUnsupportedUbrPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WinUnsupportedUbrPage"/> class.
        /// </summary>
        public WinUnsupportedUbrPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<WinUnsupportedUbrViewModel>();
        }

        /// <summary>
        /// Gets <see cref="WinUnsupportedUbrViewModel"/>.
        /// </summary>
        public WinUnsupportedUbrViewModel ViewModel { get; }
    }
}
