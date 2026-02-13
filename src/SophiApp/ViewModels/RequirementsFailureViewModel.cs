// <copyright file="RequirementsFailureViewModel.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using SophiApp.Helpers;

    /// <summary>
    /// Implements the <see cref="RequirementsFailureViewModel"/> class.
    /// </summary>
    public partial class RequirementsFailureViewModel : ObservableRecipient
    {
        private readonly IUpdateService updateService;
        private readonly ICommonDataService dataService;

        [ObservableProperty]
        private string titleText = string.Empty;

        [ObservableProperty]
        private string descriptionText = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsFailureViewModel"/> class.
        /// </summary>
        /// <param name="updateService">A service for dealing with OS updates.</param>
        /// <param name="dataService">A service for transferring app data between layers of DI.</param>
        public RequirementsFailureViewModel(IUpdateService updateService, ICommonDataService dataService)
        {
            this.updateService = updateService;
            this.dataService = dataService;
        }

        /// <summary>
        /// Prepares the ViewModel for display in UI.
        /// </summary>
        /// <param name="reason">Reason for failure requirements.</param>
        public void PrepareForNavigation(RequirementsFailure reason)
        {
            TitleText = LocalizeTitleText(reason);
            DescriptionText = LocalizeDescriptionText(reason);
        }

        /// <summary>
        /// Start receiving OS updates.
        /// </summary>
        /// <param name="reason">Reasons for failure requirements.</param>
        public void RunOsUpdate(RequirementsFailure reason)
        {
            switch (reason)
            {
                case RequirementsFailure.Win11BuildLess22631:
                case RequirementsFailure.Win11UbrLess2283:
                case RequirementsFailure.Win10UnsupportedBuild:
                    updateService.RunOsUpdate();
                    break;

                default:
                    break;
            }
        }

        private string LocalizeTitleText(RequirementsFailure reason)
        {
            return reason switch
            {
                RequirementsFailure.DefenderControlledFolderEnable => "OsRequirementsFailure_DefenderControlledFolderEnable".GetLocalized(),
                RequirementsFailure.DefenderFileMissing => string.Format("OsRequirementsFailure_DefenderFilesMissing".GetLocalized(), dataService.DefenderFileMissing),
                RequirementsFailure.DefenderIsBroken => "OsRequirementsFailure_DefenderIsBroken".GetLocalized(),
                RequirementsFailure.DefenderSecurityHealthFailure => "OsRequirementsFailure_DefenderSecurityHealthFailure".GetLocalized(),
                RequirementsFailure.DefenderServiceFailure => string.Format("OsRequirementsFailure_DefenderServiceBroken".GetLocalized(), dataService.DefenderServiceBroken),
                RequirementsFailure.DefenderSettingsPageHidden => "OsRequirementsFailure_DefenderSettingsPageHidden".GetLocalized(),
                RequirementsFailure.EventLogBroken => "OsRequirementsFailure_EventLogStopped".GetLocalized(),
                RequirementsFailure.FeatureExperiencePackRemoved => "OsRequirementsFailure_FeatureExperiencePackRemoved".GetLocalized(),
                RequirementsFailure.Is32BitOs => "OsRequirementsFailure_Is32BitOs".GetLocalized(),
                RequirementsFailure.MalwareDetected => string.Format("OsRequirementsFailure_MalwareDetected".GetLocalized(), dataService.DetectedMalware),
                RequirementsFailure.MsStoreRemoved => "OsRequirementsFailure_MsStoreRemoved".GetLocalized(),
                RequirementsFailure.RebootRequired => "OsRequirementsFailure_RebootRequired".GetLocalized(),
                RequirementsFailure.RunByNotLoggedUser => "OsRequirementsFailure_RunByNotLoggedUser".GetLocalized(),
                RequirementsFailure.Win10EnterpriseSVersion => "OsRequirementsFailure_Win10EnterpriseSVersion".GetLocalized(),
                RequirementsFailure.Win10UnsupportedBuild => string.Format("OsRequirementsFailure_Win10UnsupportedBuild".GetLocalized(), dataService.OsProperties.BuildNumber, dataService.OsProperties.UpdateBuildRevision),
                RequirementsFailure.Win10UpdateBuildRevisionLess3448 => string.Format("OsRequirementsFailure_Win10UnsupportedBuild".GetLocalized(), dataService.OsProperties.BuildNumber, dataService.OsProperties.UpdateBuildRevision),
                RequirementsFailure.Win11BuildLess22631 => string.Format("OsRequirementsFailure_Win11UnsupportedBuild".GetLocalized(), dataService.OsProperties.BuildNumber, dataService.OsProperties.UpdateBuildRevision),
                RequirementsFailure.Win11UbrLess2283 => string.Format("OsRequirementsFailure_Win11UnsupportedBuild".GetLocalized(), dataService.OsProperties.BuildNumber, dataService.OsProperties.UpdateBuildRevision),
                RequirementsFailure.WMIBroken => "OsRequirementsFailure_WmiBroken".GetLocalized(),
                _ => throw new ArgumentOutOfRangeException(paramName: nameof(reason), message: $"Value: {reason} is not found in {typeof(RequirementsFailure).FullName} enumeration.")
            };
        }

        private string LocalizeDescriptionText(RequirementsFailure reason)
        {
            switch (reason)
            {
                case RequirementsFailure.MalwareDetected:
                    return "OsRequirementsFailure_ReinstallWindows".GetLocalized();

                default:
                    return string.Empty;
            }
        }
    }
}
