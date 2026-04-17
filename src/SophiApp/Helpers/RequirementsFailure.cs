// <copyright file="RequirementsFailure.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Helpers
{
    /// <summary>
    /// Reasons for failure requirements.
    /// </summary>
    public enum RequirementsFailure
    {
        DefenderControlledFolderEnable,
        DefenderFileMissing,
        DefenderIsBroken,
        DefenderSecurityHealthFailure,
        DefenderServiceFailure,
        DefenderSettingsPageHidden,
        EventLogBroken,
        FeatureExperiencePackRemoved,
        Is32BitOs,
        MalwareDetected,
        MsStoreRemoved,
        RebootRequired,
        RunByNotLoggedUser,
        WinUnsupportedBuild,
        WinUnsupportedUBR,
        WMIBroken,
    }
}
