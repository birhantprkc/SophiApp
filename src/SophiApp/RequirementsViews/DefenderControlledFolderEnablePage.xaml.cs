// <copyright file="DefenderControlledFolderEnablePage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="DefenderControlledFolderEnablePage"/>.
    /// </summary>
    public sealed partial class DefenderControlledFolderEnablePage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderControlledFolderEnablePage"/> class.
        /// </summary>
        public DefenderControlledFolderEnablePage()
        {
            InitializeComponent();
            ViewModel = App.GetService<DefenderControlledFolderEnableViewModel>();
        }

        /// <summary>
        /// Gets <see cref="DefenderControlledFolderEnableViewModel"/>.
        /// </summary>
        public DefenderControlledFolderEnableViewModel ViewModel { get; }
    }
}
