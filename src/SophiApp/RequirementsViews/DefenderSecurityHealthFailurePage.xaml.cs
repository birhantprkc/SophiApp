// <copyright file="DefenderSecurityHealthFailurePage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="DefenderSecurityHealthFailurePage"/>.
    /// </summary>
    public sealed partial class DefenderSecurityHealthFailurePage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderSecurityHealthFailurePage"/> class.
        /// </summary>
        public DefenderSecurityHealthFailurePage()
        {
            InitializeComponent();
            ViewModel = App.GetService<DefenderSecurityHealthFailureViewModel>();
        }

        /// <summary>
        /// Gets <see cref="DefenderSecurityHealthFailureViewModel"/>.
        /// </summary>
        public DefenderSecurityHealthFailureViewModel ViewModel { get; }
    }
}
