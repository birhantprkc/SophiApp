// <copyright file="MsStoreRemovedPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="MsStoreRemovedPage"/>.
    /// </summary>
    public sealed partial class MsStoreRemovedPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsStoreRemovedPage"/> class.
        /// </summary>
        public MsStoreRemovedPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<MsStoreRemovedViewModel>();
        }

        /// <summary>
        /// Gets <see cref="MsStoreRemovedViewModel"/>.
        /// </summary>
        public MsStoreRemovedViewModel ViewModel { get; }
    }
}
