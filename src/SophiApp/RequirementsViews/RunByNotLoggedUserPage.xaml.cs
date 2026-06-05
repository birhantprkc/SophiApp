// <copyright file="RunByNotLoggedUserPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="RunByNotLoggedUserPage"/>.
    /// </summary>
    public sealed partial class RunByNotLoggedUserPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunByNotLoggedUserPage"/> class.
        /// </summary>
        public RunByNotLoggedUserPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<RunByNotLoggedUserViewModel>();
        }

        /// <summary>
        /// Gets <see cref="RunByNotLoggedUserViewModel"/>.
        /// </summary>
        public RunByNotLoggedUserViewModel ViewModel { get; }
    }
}
