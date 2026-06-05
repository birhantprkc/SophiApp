// <copyright file="AntiSpywareDisabledPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="AntiSpywareDisabledPage"/>.
    /// </summary>
    public sealed partial class AntiSpywareDisabledPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AntiSpywareDisabledPage"/> class.
        /// </summary>
        public AntiSpywareDisabledPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<AntiSpywareDisabledViewModel>();
        }

        /// <summary>
        /// Gets <see cref="AntiSpywareDisabledViewModel"/>.
        /// </summary>
        public AntiSpywareDisabledViewModel ViewModel { get; }
    }
}
