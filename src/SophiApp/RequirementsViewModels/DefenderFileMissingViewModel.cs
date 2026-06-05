// <copyright file="DefenderFileMissingViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.RequirementsViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;

    /// <summary>
    /// Implements the <see cref="DefenderFileMissingViewModel"/> class.
    /// </summary>
    public partial class DefenderFileMissingViewModel : ObservableRecipient
    {
        private readonly ICommonDataService dataService;

        [ObservableProperty]
        private string text;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefenderFileMissingViewModel"/> class.
        /// </summary>
        public DefenderFileMissingViewModel()
        {
            dataService = App.GetService<ICommonDataService>();
            Text = string.Format("OsRequirementsFailure_DefenderFilesMissing".GetLocalized(), dataService.DefenderFileMissing);
        }
    }
}
