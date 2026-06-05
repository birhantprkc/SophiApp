// <copyright file="RebootRequiredPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="RebootRequiredPage"/>.
    /// </summary>
    public sealed partial class RebootRequiredPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RebootRequiredPage"/> class.
        /// </summary>
        public RebootRequiredPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<RebootRequiredViewModel>();
        }

        /// <summary>
        /// Gets <see cref="RebootRequiredViewModel"/>.
        /// </summary>
        public RebootRequiredViewModel ViewModel { get; }
    }
}
