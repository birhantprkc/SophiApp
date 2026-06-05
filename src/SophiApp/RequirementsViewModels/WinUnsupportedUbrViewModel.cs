// <copyright file="WinUnsupportedUbrViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;

    /// <summary>
    /// Implements the <see cref="WinUnsupportedUbrViewModel"/> class.
    /// </summary>
    public partial class WinUnsupportedUbrViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string text;

        /// <summary>
        /// Initializes a new instance of the <see cref="WinUnsupportedUbrViewModel"/> class.
        /// </summary>
        public WinUnsupportedUbrViewModel()
        {
            var dataService = App.GetService<ICommonDataService>();
            var supportedUBR = dataService.OsProperties.IsLTSC ? dataService.SupportedUBR.Win11LTSC : dataService.SupportedUBR.Win11;
            Text = string.Format("OsRequirementsFailure_UnsupportedUBR".GetLocalized(), dataService.OsProperties.Build, dataService.OsProperties.UBR, supportedUBR);
        }
    }
}
