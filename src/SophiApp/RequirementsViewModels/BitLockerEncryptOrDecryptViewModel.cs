// <copyright file="BitLockerEncryptOrDecryptViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.ViewModels;

    /// <summary>
    /// Implements the <see cref="BitLockerEncryptOrDecryptViewModel"/> class.
    /// </summary>
    public partial class BitLockerEncryptOrDecryptViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private bool isHomeEdition;
        [ObservableProperty]
        private IRelayCommand openBitLockerSettingsCommand;
        [ObservableProperty]
        private string text;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitLockerEncryptOrDecryptViewModel"/> class.
        /// </summary>
        public BitLockerEncryptOrDecryptViewModel()
        {
            var dataService = App.GetService<ICommonDataService>();
            var powerShellService = App.GetService<IPowerShellService>();
            var shellViewModel = App.GetService<ShellViewModel>();
            var command = "[int](Get-BitLockerVolume -MountPoint $env:SystemDrive | Where-Object -FilterScript {$_.VolumeStatus -notin @(\"FullyEncrypted\", \"FullyDecrypted\")}).EncryptionPercentage";
            Text = string.Format("OsRequirementsFailure_BitLockerEncryptOrDecryptState".GetLocalized(), powerShellService.Invoke<int>(command));
            IsHomeEdition = dataService.OsProperties.Edition.Contains("Core");
            OpenBitLockerSettingsCommand = shellViewModel.OpenBitLockerSettingsCommand;
        }
    }
}
