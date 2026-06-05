// <copyright file="BitLockerProtectionStatusViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using SophiApp.ViewModels;

    /// <summary>
    /// Implements the <see cref="BitLockerProtectionStatusViewModel"/> class.
    /// </summary>
    public partial class BitLockerProtectionStatusViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private IRelayCommand bitLockerProtectionStatusCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitLockerProtectionStatusViewModel"/> class.
        /// </summary>
        public BitLockerProtectionStatusViewModel()
        {
            var shellViewModel = App.GetService<ShellViewModel>();
            BitLockerProtectionStatusCommand = shellViewModel.BitLockerProtectionStatus_Command;
        }
    }
}
