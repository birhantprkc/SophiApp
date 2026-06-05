// <copyright file="DetectHostFileEntriesPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="DetectHostFileEntriesPage"/>.
    /// </summary>
    public sealed partial class DetectHostFileEntriesPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DetectHostFileEntriesPage"/> class.
        /// </summary>
        public DetectHostFileEntriesPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<DetectHostFileEntriesViewModel>();
        }

        /// <summary>
        /// Gets <see cref="DetectHostFileEntriesViewModel"/>.
        /// </summary>
        public DetectHostFileEntriesViewModel ViewModel { get; }
    }
}
