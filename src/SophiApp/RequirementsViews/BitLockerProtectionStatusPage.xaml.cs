// <copyright file="BitLockerProtectionStatusPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="BitLockerProtectionStatusPage"/>.
    /// </summary>
    public sealed partial class BitLockerProtectionStatusPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitLockerProtectionStatusPage"/> class.
        /// </summary>
        public BitLockerProtectionStatusPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<BitLockerProtectionStatusViewModel>();
        }

        /// <summary>
        /// Gets <see cref="BitLockerProtectionStatusViewModel"/>.
        /// </summary>
        public BitLockerProtectionStatusViewModel ViewModel { get; }
    }
}
