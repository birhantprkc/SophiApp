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
        private readonly ICommonDataService dataService;

        [ObservableProperty]
        private string titleText = string.Empty;

        [ObservableProperty]
        private string descriptionText = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsFailureViewModel"/> class.
        /// </summary>
        /// <param name="dataService">A service for transferring app data between layers of DI.</param>
        public RequirementsFailureViewModel(ICommonDataService dataService)
        {
            this.dataService = dataService;
        }

        /// <summary>
        /// Localize <see cref="RequirementsFailure"/> reason text.
        /// </summary>
        /// <param name="reason">Reason for failure requirements.</param>
        public void LocalizeFailureReason(RequirementsFailure reason)
        {
            switch (reason)
            {
                case RequirementsFailure.DefenderControlledFolderEnable:
                    TitleText = "OsRequirementsFailure_DefenderControlledFolderEnable".GetLocalized();
                    break;
                case RequirementsFailure.DefenderFileMissing:
                    TitleText = string.Format("OsRequirementsFailure_DefenderFilesMissing".GetLocalized(), dataService.DefenderFileMissing);
                    break;
                case RequirementsFailure.DefenderIsBroken:
                    TitleText = "OsRequirementsFailure_DefenderIsBroken".GetLocalized();
                    break;
                case RequirementsFailure.DefenderSecurityHealthFailure:
                    TitleText = "OsRequirementsFailure_DefenderSecurityHealthFailure".GetLocalized();
                    break;
                case RequirementsFailure.DefenderServiceFailure:
                    TitleText = string.Format("OsRequirementsFailure_DefenderServiceBroken".GetLocalized(), dataService.DefenderServiceBroken);
                    break;
                case RequirementsFailure.DefenderSettingsPageHidden:
                    TitleText = "OsRequirementsFailure_DefenderSettingsPageHidden".GetLocalized();
                    break;
                case RequirementsFailure.EventLogBroken:
                    TitleText = "OsRequirementsFailure_EventLogStopped".GetLocalized();
                    break;
                case RequirementsFailure.FeatureExperiencePackRemoved:
                    TitleText = "OsRequirementsFailure_FeatureExperiencePackRemoved".GetLocalized();
                    break;
                case RequirementsFailure.Is32BitOs:
                    TitleText = "OsRequirementsFailure_Is32BitOs".GetLocalized();
                    break;
                case RequirementsFailure.MalwareDetected:
                    TitleText = string.Format("OsRequirementsFailure_MalwareDetected".GetLocalized(), dataService.DetectedMalware);
                    DescriptionText = "OsRequirementsFailure_ReinstallWindows".GetLocalized();
                    break;
                case RequirementsFailure.MsStoreRemoved:
                    TitleText = "OsRequirementsFailure_MsStoreRemoved".GetLocalized();
                    break;
                case RequirementsFailure.RebootRequired:
                    TitleText = "OsRequirementsFailure_RebootRequired".GetLocalized();
                    break;
                case RequirementsFailure.RunByNotLoggedUser:
                    TitleText = "OsRequirementsFailure_RunByNotLoggedUser".GetLocalized();
                    DescriptionText = "OsRequirementsFailure_RunByAdmin".GetLocalized();
                    break;
                case RequirementsFailure.WinUnsupportedBuild:
                    TitleText = "OsRequirementsFailure_UnsupportedBuild".GetLocalized();
                    DescriptionText = string.Format("OsRequirementsFailure_UsingBuild".GetLocalized(), dataService.OsProperties.Caption, dataService.OsProperties.DisplayVersion);
                    break;
                case RequirementsFailure.WinUnsupportedUBR:
                    var supportedUBR = dataService.OsProperties.IsLTSC ? dataService.SupportedUBR.Win11LTSC : dataService.SupportedUBR.Win11;
                    TitleText = string.Format("OsRequirementsFailure_UnsupportedUBR".GetLocalized(), dataService.OsProperties.Build, dataService.OsProperties.UBR, supportedUBR);
                    DescriptionText = "OsRequirementsFailure_RunWinUpdate".GetLocalized();
                    break;
                case RequirementsFailure.WMIBroken:
                    TitleText = "OsRequirementsFailure_WmiBroken".GetLocalized();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(paramName: nameof(reason), message: $"Value: {reason} is not found in {typeof(RequirementsFailure).FullName} enumeration.");
            }
        }
    }
}
