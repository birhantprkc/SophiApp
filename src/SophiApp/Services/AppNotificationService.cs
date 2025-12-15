// <copyright file="AppNotificationService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;

using Microsoft.Win32;
using SophiApp.Contracts.Services;
using SophiApp.Extensions;
using SophiApp.Helpers;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

/// <inheritdoc/>
public class AppNotificationService : IAppNotificationService
{
    private readonly IGroupPolicyService groupPolicyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppNotificationService"/> class.
    /// </summary>
    /// <param name="groupPolicyService">A service for working with group policy API.</param>
    public AppNotificationService(IGroupPolicyService groupPolicyService)
    {
        this.groupPolicyService = groupPolicyService;
    }

    /// <inheritdoc/>
    public void EnableToastNotification()
    {
        // Enable notifications
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", true)?.DeleteValue("ToastEnabled", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Windows.ActionCenter.SmartOptOut", true)?.DeleteValue("Enable", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\SophiApp", true)?.DeleteValue("ShowBanner", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\SophiApp", true)?.DeleteValue("ShowInActionCenter", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\SophiApp", true)?.DeleteValue("Enabled", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\SystemSettings\\AccountNotifications", true)?.DeleteValue("EnableAccountNotifications", false);
        Registry.LocalMachine.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer", true)?.DeleteValue("DisableNotificationCenter", false);
        Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", true)?.DeleteValue("NoToastApplicationNotification", false);
        // Remove registry keys if Windows Script Host is disabled
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows Script Host\\Settings", true)?.DeleteValue("Enabled", false);
        Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows Script Host\\Settings", true)?.DeleteValue("Enabled", false);
        // // Call LGPO.exe to make changes in "C:\Windows\System32\GroupPolicy\Machine\Registry.pol" or "C:\Windows\System32\GroupPolicy\User\Registry.pol" database
        groupPolicyService.ClearLocalCache("Software\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter", LGPOScope.Computer, LGPOScope.User);
    }

    /// <inheritdoc/>
    public void RegisterAsToastSender(string name)
    {
        try
        {
            var appId = $"AppUserModelId\\{name}";
            var actionCenterSetting = $"Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\{name}";
            // Determines whether the app can be seen in Settings where the user can turn notifications on or off
            Registry.CurrentUser.OpenOrCreateSubKey(actionCenterSetting).SetValue("ShowInActionCenter", 0, RegistryValueKind.DWord);
            // Register app
            Registry.ClassesRoot.OpenOrCreateSubKey(appId).SetValue("DisplayName", name, RegistryValueKind.String);
            Registry.ClassesRoot.OpenSubKey(appId, true)?.SetValue("ShowInSettings", 0, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            App.Logger.LogRegisterNotificationSenderException(ex);
        }
    }

    /// <inheritdoc/>
    public void RegisterCleanupProtocolAsToastSender()
    {
        // Start the "Windows Cleanup" task if the "Run" button clicked
        var cleanupCommand = @"powershell.exe -Command ""& {Start-ScheduledTask -TaskPath '\Sophia\' -TaskName 'Windows Cleanup'}""";
        Registry.ClassesRoot.OpenOrCreateSubKey("WindowsCleanup\\shell\\open\\command").SetValue(string.Empty, cleanupCommand, RegistryValueKind.String);
        // Register the "WindowsCleanup" protocol to be able to run the scheduled task by clicking the "Run" button in a toast
        Registry.ClassesRoot.OpenSubKey("WindowsCleanup", true)?.SetValue(string.Empty, "URL:WindowsCleanup", RegistryValueKind.String);
        Registry.ClassesRoot.OpenSubKey("WindowsCleanup", true)?.SetValue("URL Protocol", string.Empty, RegistryValueKind.String);
        Registry.ClassesRoot.OpenSubKey("WindowsCleanup", true)?.SetValue("EditFlags", 2162688, RegistryValueKind.DWord);
    }

    /// <inheritdoc/>
    public void Show(string payload)
    {
        var xml = new XmlDocument();
        xml.LoadXml(payload);
        var toast = new ToastNotification(xml);
        ToastNotificationManager.CreateToastNotifier("SophiApp")
            .Show(toast);
    }

    /// <inheritdoc/>
    public void UnregisterCleanupProtocol()
    {
        Registry.ClassesRoot.DeleteSubKeyTree("WindowsCleanup", false);
    }
}
