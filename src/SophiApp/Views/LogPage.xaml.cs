// <copyright file="LogPage.xaml.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Views
{
    using Microsoft.UI.Xaml.Controls;
    using SophiApp.Helpers;
    using SophiApp.ViewModels;

    /// <summary>
    /// Implements the <see cref="LogPage"/> class.
    /// </summary>
    public sealed partial class LogPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogPage"/> class.
        /// </summary>
        public LogPage()
        {
            InitializeComponent();
            ViewModel = App.GetService<ShellViewModel>();
        }

        /// <summary>
        /// Gets view model for log page.
        /// </summary>
        public ShellViewModel ViewModel { get; }

        private void LogListView_ContextRequested(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
            => ContextMenuHelper.ShowContextMenu(sender, LogPageItemsCommandsFlyout, args);

        private void LogPageItemsCommandsFlyout_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
            => ContextMenuHelper.CopyToClipboard(ViewModel.LoggedActions);
    }
}
