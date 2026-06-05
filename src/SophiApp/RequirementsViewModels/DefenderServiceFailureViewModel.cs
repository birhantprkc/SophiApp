// <copyright file="DefenderServiceFailureViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;

    /// <summary>
    /// Implements the <see cref="DefenderSecurityHealthFailureViewModel"/> class.
    /// </summary>
    public partial class DefenderServiceFailureViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string text;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderServiceFailureViewModel"/> class.
        /// </summary>
        public DefenderServiceFailureViewModel()
        {
            var dataService = App.GetService<ICommonDataService>();
            Text = string.Format("OsRequirementsFailure_DefenderServiceBroken".GetLocalized(), dataService.DefenderServiceBroken);
        }
    }
}
