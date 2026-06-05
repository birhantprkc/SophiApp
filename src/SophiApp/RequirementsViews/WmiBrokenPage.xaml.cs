// <copyright file="WmiBrokenPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="WmiBrokenPage"/>.
    /// </summary>
    public sealed partial class WmiBrokenPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WmiBrokenPage"/> class.
        /// </summary>
        public WmiBrokenPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<WmiBrokenViewModel>();
        }

        /// <summary>
        /// Gets <see cref="WmiBrokenViewModel"/>.
        /// </summary>
        public WmiBrokenViewModel ViewModel { get; }
    }
}
