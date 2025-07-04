// <copyright file="IUpdateService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    /// <summary>
    /// A service for working with Windows and app updates.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Determines whether the Windows Update API is used to obtain updates for other Microsoft products.
        /// </summary>
        bool AllowedOtherProductsUpdate();

        /// <summary>
        /// Start receiving OS updates.
        /// </summary>
        void RunOsUpdate();
    }
}
