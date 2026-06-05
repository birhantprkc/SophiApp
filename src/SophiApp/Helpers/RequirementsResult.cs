// <copyright file="RequirementsResult.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Helpers
{
    /// <summary>
    /// Result of requirements execution.
    /// </summary>
    public enum RequirementsResult
    {
        AllCorrect,
        BitLockerEncryptOrDecryptState,
        BitLockerProtectionStatus,
        DefenderControlledFolderEnable,
        DefenderFileMissing,
        AntiSpywareDisabled,
        DefenderSecurityHealthFailure,
        DefenderServiceFailure,
        DefenderSettingsPageHidden,
        DetectHostFileEntries,
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
