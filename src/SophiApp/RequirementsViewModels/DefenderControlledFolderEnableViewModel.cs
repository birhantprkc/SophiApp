// <copyright file="DefenderControlledFolderEnableViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using SophiApp.ViewModels;

    /// <summary>
    /// Implements the <see cref="DefenderControlledFolderEnableViewModel"/> class.
    /// </summary>
    public partial class DefenderControlledFolderEnableViewModel : ObservableRecipient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderControlledFolderEnableViewModel"/> class.
        /// </summary>
        public DefenderControlledFolderEnableViewModel()
        {
            var shellViewModel = App.GetService<ShellViewModel>();
            OpenDefenderControlledFolderCommand = shellViewModel.OpenDefenderControlledFolder_Command;
        }

        /// <summary>
        /// Gets <see cref="IRelayCommand"/> to open Microsoft Defender controlled folder settings.
        /// </summary>
        public IRelayCommand OpenDefenderControlledFolderCommand { get; }
    }
}
