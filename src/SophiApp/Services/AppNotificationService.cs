// <copyright file="AppNotificationService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services;

using Microsoft.Win32;
using SophiApp.Contracts.Services;
using SophiApp.Extensions;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

/// <inheritdoc/>
public class AppNotificationService : IAppNotificationService
{
    /// <inheritdoc/>
    public void EnableToastNotification()
    {
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", true)?.DeleteValue("ToastEnabled", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Windows.ActionCenter.SmartOptOut", true)?.DeleteValue("Enable", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Sophia", true)?.DeleteValue("ShowBanner", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Sophia", true)?.DeleteValue("ShowInActionCenter", false);
        Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Sophia", true)?.DeleteValue("Enabled", false);
        Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\Explorer", true)?.DeleteValue("DisableNotificationCenter", false);
        Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", true)?.DeleteValue("NoToastApplicationNotification", false);
    }

    /// <inheritdoc/>
    public void RegisterAsToastSender(string name)
    {
        try
        {
            var appId = $"AppUserModelId\\{name}";
            var actionCenterSetting = $"Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\{name}";
            Registry.CurrentUser.OpenOrCreateSubKey(actionCenterSetting).SetValue("ShowInActionCenter", 1, RegistryValueKind.DWord);
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
        var cleanupCommandPath = "WindowsCleanup\\shell\\open\\command";
        var cleanupCommand = @"powershell.exe -Command ""& {Start-ScheduledTask -TaskPath '\Sophia\' -TaskName 'Windows Cleanup'}""";
        Registry.ClassesRoot.OpenOrCreateSubKey(cleanupCommandPath).SetValue(string.Empty, cleanupCommand, RegistryValueKind.String);
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
