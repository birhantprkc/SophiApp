// <copyright file="DetectHostFileEntriesViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using SophiApp.ViewModels;

    /// <summary>
    /// Implements the <see cref="DetectHostFileEntriesViewModel"/> class.
    /// </summary>
    public partial class DetectHostFileEntriesViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private IRelayCommand continueRequirementActionsExecuteCommand;

        [ObservableProperty]
        private IRelayCommand openHostsFolderCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetectHostFileEntriesViewModel"/> class.
        /// </summary>
        public DetectHostFileEntriesViewModel()
        {
            var viewModel = App.GetService<ShellViewModel>();
            continueRequirementActionsExecuteCommand = viewModel.ContinueRequirementActionsExecute_Command;
            openHostsFolderCommand = viewModel.OpenHostsFolder_Command;
        }
    }
}
