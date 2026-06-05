// <copyright file="BitLockerEncryptOrDecryptPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="BitLockerEncryptOrDecryptPage"/>.
    /// </summary>
    public sealed partial class BitLockerEncryptOrDecryptPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitLockerEncryptOrDecryptPage"/> class.
        /// </summary>
        public BitLockerEncryptOrDecryptPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<BitLockerEncryptOrDecryptViewModel>();
        }

        /// <summary>
        /// Gets <see cref="BitLockerEncryptOrDecryptViewModel"/>.
        /// </summary>
        public BitLockerEncryptOrDecryptViewModel ViewModel { get; }
    }
}
