// <copyright file="Is32BitOsPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="Is32BitOsPage"/>.
    /// </summary>
    public sealed partial class Is32BitOsPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Is32BitOsPage"/> class.
        /// </summary>
        public Is32BitOsPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<Is32BitOsViewModel>();
        }

        /// <summary>
        /// Gets <see cref="Is32BitOsViewModel"/>.
        /// </summary>
        public Is32BitOsViewModel ViewModel { get; }
    }
}
