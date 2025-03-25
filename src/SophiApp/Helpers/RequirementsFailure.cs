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
        DefenderFileMissing,
        DefenderIsBroken,
        DefenderServiceBroken,
        DefenderSettingsPageHidden,
        EventLogBroken,
        FeatureExperiencePackRemoved,
        Is32BitOs,
        MalwareDetected,
        MsStoreRemoved,
        RebootRequired,
        RunByNotLoggedUser,
        Win10EnterpriseSVersion,
        Win10UnsupportedBuild,
        Win10UpdateBuildRevisionLess3448,
        Win11BuildLess22631,
        Win11UbrLess2283,
        WMIBroken,
    }
}
