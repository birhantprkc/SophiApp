// <copyright file="DefenderServiceFailurePage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="DefenderServiceFailurePage"/>.
    /// </summary>
    public sealed partial class DefenderServiceFailurePage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderServiceFailurePage"/> class.
        /// </summary>
        public DefenderServiceFailurePage()
        {
            InitializeComponent();
            ViewModel = App.GetService<DefenderServiceFailureViewModel>();
        }

        /// <summary>
        /// Gets <see cref="DefenderServiceFailureViewModel"/>.
        /// </summary>
        public DefenderServiceFailureViewModel ViewModel { get; }
    }
}
