// <copyright file="EventLogBrokenPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViews
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.RequirementsViewModels;

    /// <summary>
    /// Implements the <see cref="EventLogBrokenPage"/>.
    /// </summary>
    public sealed partial class EventLogBrokenPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogBrokenPage"/> class.
        /// </summary>
        public EventLogBrokenPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<EventLogBrokenViewModel>();
        }

        /// <summary>
        /// Gets <see cref="EventLogBrokenViewModel"/>.
        /// </summary>
        public EventLogBrokenViewModel ViewModel { get; }
    }
}
