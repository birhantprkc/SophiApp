// <copyright file="DefenderFileMissingPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="DefenderFileMissingPage"/>.
    /// </summary>
    public sealed partial class DefenderFileMissingPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderFileMissingPage"/> class.
        /// </summary>
        public DefenderFileMissingPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<DefenderFileMissingViewModel>();
        }

        /// <summary>
        /// Gets <see cref="DefenderFileMissingViewModel"/>.
        /// </summary>
        public DefenderFileMissingViewModel ViewModel { get; }
    }
}
