// <copyright file="IFirewallService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Contracts.Services
{
    using NetFwTypeLib;

    /// <summary>
    /// A service for working with Windows firewall API.
    /// </summary>
    public interface IFirewallService
    {
        /// <summary>
        /// Gets firewall group rules using the group name.
        /// </summary>
        /// <param name="name">The name of group to search rules.</param>
        IEnumerable<INetFwRule> GetGroupRules(string name);

        /// <summary>
        /// Gets firewall group rules using the group name.
        /// </summary>
        /// <param name="names">The name of groups to search rules.</param>
        List<INetFwRule> GetGroupRules(params string[] names);

        /// <summary>
        /// Sets firewall group rules state and profile.
        /// </summary>
        /// <param name="name">The name of rules group.</param>
        /// <param name="enable">Set rule is enabled.</param>
        /// <param name="profileID">Windows firewall profile ID: 1 - DOMAIN, 2 - PRIVATE, 4 - PUBLIC, int.MaxValue - ALL.</param>
        void SetGroupRules(string name, bool enable, int profileID);
    }
}
