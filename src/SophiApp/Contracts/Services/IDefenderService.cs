// <copyright file="IDefenderService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    using CSharpFunctionalExtensions;

    /// <summary>
    /// A service for working with Microsoft Defender API.
    /// </summary>
    public interface IDefenderService
    {
        /// <summary>
        /// Get a Microsoft Defender state.
        /// </summary>
        public Result GetState();

        /// <summary>
        /// Turn on Microsoft Defender controlled folder.
        /// </summary>
        public void EnableControlledFolder();
    }
}
