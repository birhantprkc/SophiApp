// <copyright file="ScheduledTaskService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Win32.TaskScheduler;
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;

    /// <inheritdoc/>
    public class ScheduledTaskService : IScheduledTaskService
    {
        private readonly TaskService taskScheduler;
        private readonly FileInfo cleanupPsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\Windows_Cleanup.ps1"));
        private readonly FileInfo cleanupVbsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\Windows_Cleanup.vbs"));
        private readonly FileInfo notificationPsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\Windows_Cleanup_Notification.ps1"));
        private readonly FileInfo notificationVbsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\Windows_Cleanup_Notification.vbs"));
        private readonly FileInfo softwareDistributionPsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\SoftwareDistributionTask.ps1"));
        private readonly FileInfo softwareDistributionVbsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\SoftwareDistributionTask.vbs"));
        private readonly FileInfo tempPsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\TempTask.ps1"));
        private readonly FileInfo tempVbsFile = new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks\\Sophia\\TempTask.vbs"));

        private readonly string cleanupPsAction = @"
# https://github.com/Sophia-Community/SophiApp
# https://t.me/sophia_chat

Get-Process -Name cleanmgr, Dism, DismHost | Stop-Process -Force

$ProcessInfo = New-Object -TypeName System.Diagnostics.ProcessStartInfo
$ProcessInfo.FileName = ""$env:SystemRoot\System32\cleanmgr.exe""
$ProcessInfo.Arguments = ""/sagerun:1337""
$ProcessInfo.UseShellExecute = $true
$ProcessInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Minimized

$Process = New-Object -TypeName System.Diagnostics.Process
$Process.StartInfo = $ProcessInfo
$Process.Start() | Out-Null

Start-Sleep -Seconds 3

$ProcessInfo = New-Object -TypeName System.Diagnostics.ProcessStartInfo
$ProcessInfo.FileName = ""$env:SystemRoot\System32\Dism.exe""
$ProcessInfo.Arguments = ""/Online /English /Cleanup-Image /StartComponentCleanup /NoRestart""
$ProcessInfo.UseShellExecute = $true
$ProcessInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Minimized

$Process = New-Object -TypeName System.Diagnostics.Process
$Process.StartInfo = $ProcessInfo
$Process.Start() | Out-Null";

        // Create vbs script that will help us calling Windows_Cleanup.ps1 script silently, without interrupting system from Focus Assist mode turned on, when a powershell.exe console pops up
        private readonly string cleanupVbsAction = @"
' https://github.com/Sophia-Community/SophiApp
' https://t.me/sophia_chat

CreateObject(""Wscript.Shell"").Run ""powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File %SystemRoot%\System32\Tasks\Sophia\Windows_Cleanup.ps1"", 0";

        // We have to call PowerShell script via another VBS script silently because VBS has appropriate feature to suppress console appearing (none of other workarounds work)
        // powershell.exe process wakes up system anyway even from turned on Focus Assist mode (not a notification toast)
        private readonly string notificationPsAction = @"
# https://github.com/Sophia-Community/SophiApp
# https://t.me/sophia_chat

# Get Quiet Hours status
`$CompilerParameters                  = [System.CodeDom.Compiler.CompilerParameters]::new(""System.dll"")
`$CompilerParameters.TempFiles        = [System.CodeDom.Compiler.TempFileCollection]::new(`$env:TEMP, `$false)
`$CompilerParameters.GenerateInMemory = `$true
`$Signature = @{
	Namespace          = ""WinAPI""
	Name               = ""QuietHours""
	Language           = ""CSharp""
	CompilerParameters = `$CompilerParameters
	MemberDefinition   = @""
[DllImport(""ntdll.dll"")]
private static extern uint NtQueryWnfStateData(
	ref ulong StateName,
	IntPtr TypeId,
	IntPtr ExplicitScope,
	out uint ChangeStamp,
	out int Buffer,
	ref uint BufferSize
);

[StructLayout(LayoutKind.Sequential)]
public struct WNF_STATE_NAME
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
	public uint[] Data;

	public WNF_STATE_NAME(uint Data1, uint Data2) : this()
	{
		uint[] newData = new uint[2];
		newData[0] = Data1;
		newData[1] = Data2;
		Data = newData;
	}
}

// WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED
public static int GetState()
{
	ulong stateName = 0x0D83063EA3BF1C75;
	uint size = sizeof(int);
	uint stamp;
	int state;
	uint status = NtQueryWnfStateData(ref stateName, IntPtr.Zero, IntPtr.Zero, out stamp, out state, ref size);
	return (status == 0) ? state : -1;
}
""@
}

if (-not (""WinAPI.QuietHours"" -as [type]))
{
	Add-Type @Signature
}

# Wait until it will be 0
while ([WinAPI.QuietHours]::GetState() -ne 0)
{
	Start-Sleep -Seconds 600
}

[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

[xml]$ToastTemplate = @""
<toast duration=""Long"">
	<visual>
		<binding template=""ToastGeneric"">
			<text>#TaskScheduler_WindowsCleanupToast_Title#</text>
			<group>
				<subgroup>
					<text hint-style=""body"" hint-wrap=""true"">#TaskScheduler_WindowsCleanupToast_Description#</text>
				</subgroup>
			</group>
		</binding>
	</visual>
	<audio src=""ms-winsoundevent:notification.default"" />
	<actions>
		<action content=""#TaskScheduler_WindowsCleanupToast_Run#"" arguments=""WindowsCleanup:"" activationType=""protocol""/>
		<action content="""" arguments=""dismiss"" activationType=""system""/>
	</actions>
</toast>
""@

$ToastXml = [Windows.Data.Xml.Dom.XmlDocument]::New()
$ToastXml.LoadXml($ToastTemplate.OuterXml)

$ToastMessage = [Windows.UI.Notifications.ToastNotification]::New($ToastXML)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier(""Sophia"").Show($ToastMessage)"
    .Replace("#TaskScheduler_WindowsCleanupToast_Title#", "TaskScheduler_WindowsCleanupToast_Title".GetLocalized())
    .Replace("#TaskScheduler_WindowsCleanupToast_Description#", "TaskScheduler_WindowsCleanupToast_Description".GetLocalized())
    .Replace("#TaskScheduler_WindowsCleanupToast_Run#", "TaskScheduler_WindowsCleanupToast_Run".GetLocalized());

        private readonly string notificationVbsAction = @"
' https://github.com/Sophia-Community/SophiApp
' https://t.me/sophia_chat

CreateObject(""Wscript.Shell"").Run ""powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File %SystemRoot%\System32\Tasks\Sophia\Windows_Cleanup_Notification.ps1"", 0";

        // We have to call PowerShell script via another VBS script silently because VBS has appropriate feature to suppress console appearing (none of other workarounds work)
        // powershell.exe process wakes up system anyway even from turned on Focus Assist mode (not a notification toast)
        // https://github.com/DCourtel/Windows_10_Focus_Assist/blob/master/FocusAssistLibrary/FocusAssistLib.cs
        // https://redplait.blogspot.com/2018/07/wnf-ids-from-perfntcdll-adk-version.html
        private readonly string softwareDistributionPsAction = @"
# https://github.com/Sophia-Community/SophiApp
# https://t.me/sophia_chat

# Get Focus Assist status
# https://github.com/DCourtel/Windows_10_Focus_Assist/blob/master/FocusAssistLibrary/FocusAssistLib.cs
# https://redplait.blogspot.com/2018/07/wnf-ids-from-perfntcdll-adk-version.html

$CompilerParameters = [System.CodeDom.Compiler.CompilerParameters]::new(""System.dll"")
$CompilerParameters.TempFiles = [System.CodeDom.Compiler.TempFileCollection]::new($env:TEMP, $false)
$CompilerParameters.GenerateInMemory = $true
$Signature = @{
	Namespace          = ""WinAPI""
	Name               = ""Focus""
	Language           = ""CSharp""
	CompilerParameters = $CompilerParameters
	MemberDefinition   = @""
[DllImport(""NtDll.dll"", SetLastError = true)]
private static extern uint NtQueryWnfStateData(IntPtr pStateName, IntPtr pTypeId, IntPtr pExplicitScope, out uint nChangeStamp, out IntPtr pBuffer, ref uint nBufferSize);

[StructLayout(LayoutKind.Sequential)]
public struct WNF_TYPE_ID
{
	public Guid TypeId;
}

[StructLayout(LayoutKind.Sequential)]
public struct WNF_STATE_NAME
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
	public uint[] Data;

	public WNF_STATE_NAME(uint Data1, uint Data2) : this()
	{
		uint[] newData = new uint[2];
		newData[0] = Data1;
		newData[1] = Data2;
		Data = newData;
	}
}

public enum FocusAssistState
{
	NOT_SUPPORTED = -2,
	FAILED = -1,
	OFF = 0,
	PRIORITY_ONLY = 1,
	ALARMS_ONLY = 2
};

// Returns the state of Focus Assist if available on this computer
public static FocusAssistState GetFocusAssistState()
{
	try
	{
		WNF_STATE_NAME WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED = new WNF_STATE_NAME(0xA3BF1C75, 0xD83063E);
		uint nBufferSize = (uint)Marshal.SizeOf(typeof(IntPtr));
		IntPtr pStateName = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WNF_STATE_NAME)));
		Marshal.StructureToPtr(WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED, pStateName, false);

		uint nChangeStamp = 0;
		IntPtr pBuffer = IntPtr.Zero;
		bool success = NtQueryWnfStateData(pStateName, IntPtr.Zero, IntPtr.Zero, out nChangeStamp, out pBuffer, ref nBufferSize) == 0;
		Marshal.FreeHGlobal(pStateName);

		if (success)
		{
			return (FocusAssistState)pBuffer;
		}
	}
	catch {}

	return FocusAssistState.FAILED;
}
""@
}

if (-not (""WinAPI.Focus"" -as [type]))
{
	Add-Type @Signature
}

# Wait until it will be ""OFF"" (0)
while ([WinAPI.Focus]::GetFocusAssistState() -ne ""OFF"")
{
	Start-Sleep -Seconds 600
}

# Wait until Windows Update service will stop
(Get-Service -Name wuauserv).WaitForStatus(""Stopped"", ""01:00:00"")
Get-ChildItem -Path `$env:SystemRoot\SoftwareDistribution\Download -Recurse -Force | Remove-Item -Recurse -Force
# Remove files which can be removed in a user scope
Get-ChildItem -Path $env:SystemRoot\SoftwareDistribution\Download -Recurse | Remove-Item -Recurse

[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

[xml]$ToastTemplate = @""
<toast duration=""Long"">
	<visual>
		<binding template=""ToastGeneric"">
			<text>#TaskScheduler_SoftwareDistributionToast_Title#</text>
		</binding>
	</visual>
	<audio src=""ms-winsoundevent:notification.default"" />
</toast>
""@

$ToastXml = [Windows.Data.Xml.Dom.XmlDocument]::New()
$ToastXml.LoadXml($ToastTemplate.OuterXml)

$ToastMessage = [Windows.UI.Notifications.ToastNotification]::New($ToastXML)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier(""Sophia"").Show($ToastMessage)
"
    .Replace("#TaskScheduler_SoftwareDistributionToast_Title#", "TaskScheduler_SoftwareDistributionToast_Title".GetLocalized());

        private readonly string softwareDistributionVbsAction = @"
' https://github.com/Sophia-Community/SophiApp
' https://t.me/sophia_chat

CreateObject(""Wscript.Shell"").Run ""powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File %SystemRoot%\System32\Tasks\Sophia\SoftwareDistributionTask.ps1"", 0";

        // We have to call PowerShell script via another VBS script silently because VBS has appropriate feature to suppress console appearing (none of other workarounds work)
        // powershell.exe process wakes up system anyway even from turned on Focus Assist mode (not a notification toast)
        // https://github.com/DCourtel/Windows_10_Focus_Assist/blob/master/FocusAssistLibrary/FocusAssistLib.cs
        // https://redplait.blogspot.com/2018/07/wnf-ids-from-perfntcdll-adk-version.html
        private readonly string tempPsAction = @"
# https://github.com/Sophia-Community/SophiApp
# https://t.me/sophia_chat

# Get Focus Assist status
# https://github.com/DCourtel/Windows_10_Focus_Assist/blob/master/FocusAssistLibrary/FocusAssistLib.cs
# https://redplait.blogspot.com/2018/07/wnf-ids-from-perfntcdll-adk-version.html

$CompilerParameters = [System.CodeDom.Compiler.CompilerParameters]::new(""System.dll"")
$CompilerParameters.TempFiles = [System.CodeDom.Compiler.TempFileCollection]::new($env:TEMP, $false)
$CompilerParameters.GenerateInMemory = $true
$Signature = @{
	Namespace          = ""WinAPI""
	Name               = ""Focus""
	Language           = ""CSharp""
	CompilerParameters = $CompilerParameters
	MemberDefinition   = @""
[DllImport(""NtDll.dll"", SetLastError = true)]
private static extern uint NtQueryWnfStateData(IntPtr pStateName, IntPtr pTypeId, IntPtr pExplicitScope, out uint nChangeStamp, out IntPtr pBuffer, ref uint nBufferSize);

[StructLayout(LayoutKind.Sequential)]
public struct WNF_TYPE_ID
{
	public Guid TypeId;
}

[StructLayout(LayoutKind.Sequential)]
public struct WNF_STATE_NAME
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
	public uint[] Data;

	public WNF_STATE_NAME(uint Data1, uint Data2) : this()
	{
		uint[] newData = new uint[2];
		newData[0] = Data1;
		newData[1] = Data2;
		Data = newData;
	}
}

public enum FocusAssistState
{
	NOT_SUPPORTED = -2,
	FAILED = -1,
	OFF = 0,
	PRIORITY_ONLY = 1,
	ALARMS_ONLY = 2
};

// Returns the state of Focus Assist if available on this computer
public static FocusAssistState GetFocusAssistState()
{
	try
	{
		WNF_STATE_NAME WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED = new WNF_STATE_NAME(0xA3BF1C75, 0xD83063E);
		uint nBufferSize = (uint)Marshal.SizeOf(typeof(IntPtr));
		IntPtr pStateName = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WNF_STATE_NAME)));
		Marshal.StructureToPtr(WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED, pStateName, false);

		uint nChangeStamp = 0;
		IntPtr pBuffer = IntPtr.Zero;
		bool success = NtQueryWnfStateData(pStateName, IntPtr.Zero, IntPtr.Zero, out nChangeStamp, out pBuffer, ref nBufferSize) == 0;
		Marshal.FreeHGlobal(pStateName);

		if (success)
		{
			return (FocusAssistState)pBuffer;
		}
	}
	catch {}

	return FocusAssistState.FAILED;
}
""@
}

if (-not (""WinAPI.Focus"" -as [type]))
{
	Add-Type @Signature
}

# Wait until it will be ""OFF"" (0)
while ([WinAPI.Focus]::GetFocusAssistState() -ne ""OFF"")
{
	Start-Sleep -Seconds 600
}

# Run the task
Get-ChildItem -Path $env:TEMP -Recurse -Force | Where-Object -FilterScript {$_.CreationTime -lt (Get-Date).AddDays(-1)} | Remove-Item -Recurse -Force

# Unnecessary folders to remove
$Paths = @(
	# Get C:\$WinREAgent path because we need to open brackets for $env:SystemDrive but not for $WinREAgent
	(-join (""$env:SystemDrive\"", '$WinREAgent')),
	(-join (""$env:SystemDrive\"", '$SysReset')),
	(-join (""$env:SystemDrive\"", '$Windows.~WS')),
	(-join (""$env:SystemDrive\"", '$GetCurrent')),
	""$env:SystemDrive\ESD"",
	""$env:SystemDrive\Intel"",
	""$env:SystemDrive\PerfLogs"",
	""$env:SystemRoot\ServiceProfiles\NetworkService\AppData\Local\Temp"",
	""$env:LOCALAPPDATA\CrashDumps""
)

if ((Get-ChildItem -Path $env:SystemDrive\Recovery -Force | Where-Object -FilterScript {$_.Name -eq ""ReAgentOld.xml""}).FullName)
{
	$Paths += ""$env:SystemDrive\Recovery""
}
Remove-Item -Path $Paths -Recurse -Force

# C:\WINDOWS\System32\config\systemprofile\AppData\Local with garbage tw-*.tmp files
# https://www.elevenforum.com/t/folder-c-windows-system32-config-systemprofile-app-data-local-running-full-with-empty-folders-after-latest-upgrade-to-25h2.40624/
Get-ChildItem -Path ""$env:SystemRoot\System32\config\systemprofile\AppData\Local\tw-*.tmp"" -Force | Remove-Item -Force

[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

[xml]$ToastTemplate = @""
<toast duration=""Long"">
	<visual>
		<binding template=""ToastGeneric"">
			<text>#TaskScheduler_TempTaskToast_Title#</text>
		</binding>
	</visual>
	<audio src=""ms-winsoundevent:notification.default"" />
</toast>
""@

$ToastXml = [Windows.Data.Xml.Dom.XmlDocument]::New()
$ToastXml.LoadXml($ToastTemplate.OuterXml)

$ToastMessage = [Windows.UI.Notifications.ToastNotification]::New($ToastXML)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier(""Sophia"").Show($ToastMessage)"
    .Replace("#TaskScheduler_TempTaskToast_Title#", "TaskScheduler_TempTaskToast_Title".GetLocalized());

        private readonly string tempVbsAction = @"
' https://github.com/Sophia-Community/SophiApp
' https://t.me/sophia_chat

CreateObject(""Wscript.Shell"").Run ""powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File %SystemRoot%\System32\Tasks\Sophia\TempTask.ps1"", 0";

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskService"/> class.
        /// </summary>
        public ScheduledTaskService()
        {
            taskScheduler = TaskService.Instance;
        }

        /// <inheritdoc/>
        public IEnumerable<Task?> FindTaskOrDefault(string[] names, bool searchAllFolders = true)
        {
            foreach (var name in names)
            {
                yield return taskScheduler.FindTask(name, searchAllFolders);
            }
        }

        /// <inheritdoc/>
        public Task? FindTaskOrDefault(string name, bool searchAllFolders = true) => taskScheduler.FindTask(name, searchAllFolders);

        /// <inheritdoc/>
        public Task? GetTaskOrDefault(string taskPath) => taskScheduler.GetTask(taskPath);

        /// <inheritdoc/>
        public void RegisterCleanupTask()
        {
            cleanupPsFile.Directory?.Create();
            cleanupVbsFile.Directory?.Create();
            File.WriteAllText(cleanupPsFile.FullName, cleanupPsAction, Encoding.UTF8);
            File.WriteAllText(cleanupVbsFile.FullName, cleanupVbsAction, Encoding.Default);

            // Create "Windows Cleanup" task
            // We cannot create a schedule task if %COMPUTERNAME% is equal to %USERNAME%, so we have to use a "$env:COMPUTERNAME\$env:USERNAME" method
            // https://github.com/PowerShell/PowerShell/issues/21377
            _ = RegisterTask(
                name: "Sophia\\Windows Cleanup",
                description: string.Format("TaskScheduler_WindowsCleanup_Description".GetLocalized(), Environment.UserName),
                action: "wscript.exe",
                arguments: cleanupVbsFile.FullName,
                username: Environment.UserName,
                runLevel: TaskRunLevel.Highest);
        }

        /// <inheritdoc/>
        public void UnregisterCleanupTask()
        {
            cleanupPsFile.Delete();
            cleanupVbsFile.Delete();
            UnregisterTask("Sophia\\Windows Cleanup");
        }

        /// <inheritdoc/>
        public void RegisterCleanupNotificationTask()
        {
            File.WriteAllText(notificationPsFile.FullName, notificationPsAction, Encoding.UTF8);
            File.WriteAllText(notificationVbsFile.FullName, notificationVbsAction, Encoding.Default);

            // Create "Windows Cleanup" task
            // We cannot create a schedule task if %COMPUTERNAME% is equal to %USERNAME%, so we have to use a "$env:COMPUTERNAME\$env:USERNAME" method
            // https://github.com/PowerShell/PowerShell/issues/21377
            _ = RegisterTask(
                name: "Sophia\\Windows Cleanup Notification",
                description: string.Format("TaskScheduler_WindowsCleanupNotification_Description".GetLocalized(), Environment.UserName),
                action: "wscript.exe",
                arguments: notificationVbsFile.FullName,
                username: Environment.UserName,
                runLevel: TaskRunLevel.Highest,
                trigger: new DailyTrigger(daysInterval: 30) { StartBoundary = DateTime.Today.AddHours(21) });
        }

        /// <inheritdoc/>
        public void UnregisterCleanupNotificationTask()
        {
            notificationPsFile.Delete();
            notificationVbsFile.Delete();
            UnregisterTask("Sophia\\Windows Cleanup Notification");
        }

        /// <inheritdoc/>
        public void UnregisterOneDriveTasks()
        {
            taskScheduler.FindAllTasks(task => task.Name.Contains("OneDrive"))
                .ToList()
                .ForEach(t => UnregisterTask(t.Path));
        }

        /// <inheritdoc/>
        public void RegisterSoftwareDistributionTask()
        {
            softwareDistributionPsFile.Directory?.Create();
            softwareDistributionVbsFile.Directory?.Create();
            File.WriteAllText(softwareDistributionPsFile.FullName, softwareDistributionPsAction, Encoding.UTF8);
            File.WriteAllText(softwareDistributionVbsFile.FullName, softwareDistributionVbsAction, Encoding.Default);

            // Create "Windows Cleanup" task
            // We cannot create a schedule task if %COMPUTERNAME% is equal to %USERNAME%, so we have to use a "$env:COMPUTERNAME\$env:USERNAME" method
            // https://github.com/PowerShell/PowerShell/issues/21377
            _ = RegisterTask(
                name: "Sophia\\SoftwareDistribution",
                description: string.Format("TaskScheduler_SoftwareDistribution_Description".GetLocalized(), Environment.UserName),
                action: "wscript.exe",
                arguments: softwareDistributionVbsFile.FullName,
                username: Environment.UserName,
                runLevel: TaskRunLevel.Highest,
                trigger: new DailyTrigger(daysInterval: 90) { StartBoundary = DateTime.Today.AddHours(21) });
        }

        /// <inheritdoc/>
        public void UnregisterSoftwareDistributionTask()
        {
            softwareDistributionPsFile.Delete();
            softwareDistributionVbsFile.Delete();
            UnregisterTask("Sophia\\SoftwareDistribution");
        }

        /// <inheritdoc/>
        public void RegisterTempTask()
        {
            tempPsFile.Directory?.Create();
            tempVbsFile.Directory?.Create();
            File.WriteAllText(tempPsFile.FullName, tempPsAction, Encoding.UTF8);
            File.WriteAllText(tempVbsFile.FullName, tempVbsAction, Encoding.Default);

            _ = RegisterTask(
                name: "Sophia\\Temp",
                description: string.Format("TaskScheduler_TempTask_Description".GetLocalized(), Environment.UserName),
                action: "wscript.exe",
                arguments: tempVbsFile.FullName,
                username: Environment.UserName,
                runLevel: TaskRunLevel.Highest,
                trigger: new DailyTrigger(daysInterval: 60) { StartBoundary = DateTime.Today.AddHours(21) });
        }

        /// <inheritdoc/>
        public void UnregisterTempTask()
        {
            tempPsFile.Delete();
            tempVbsFile.Delete();
            UnregisterTask("Sophia\\Temp");
        }

        /// <inheritdoc/>
        public void SetState(Task? task, bool enabled)
        {
            if (task is not null)
            {
                task.Enabled = enabled;
            }
        }

        /// <inheritdoc/>
        public void TryStop(Task? task)
        {
            try
            {
                task?.Stop();
            }
            catch
            {
                // Do nothing.
            }
        }

        /// <inheritdoc/>
        public void DeleteTaskFolders(string[] folders)
        {
            Array.ForEach(folders, folder =>
            {
                var taskFolder = taskScheduler.GetFolder(folder);

                if (taskFolder?.AllTasks.Any() ?? false)
                {
                    foreach (var task in taskFolder!.AllTasks)
                    {
                        taskFolder.DeleteTask(task.Name, false);
                    }
                }

                taskScheduler.RootFolder.DeleteFolder(folder, false);
            });
        }

        /// <inheritdoc/>
        public void TryDeleteTaskFolder(string name)
        {
            if (!taskScheduler.GetFolder(name)?.AllTasks.Any() ?? false)
            {
                taskScheduler.RootFolder.DeleteFolder(name, false);
            }
        }

        private Task RegisterTask(string name, string description, string action, string arguments, string username, TaskRunLevel runLevel, Trigger? trigger = null)
        {
            var task = taskScheduler.NewTask();

            if (trigger is not null)
            {
                task.Triggers.Add(trigger);
            }

            task.Actions.Add(action, arguments);
            task.Principal.UserId = username;
            task.Principal.RunLevel = runLevel;
            task.Settings.Compatibility = TaskCompatibility.V2_2;
            task.Settings.StartWhenAvailable = true;
            task.RegistrationInfo.Author = "Team Sophia";
            task.RegistrationInfo.Description = description;
            return taskScheduler.RootFolder.RegisterTaskDefinition(name, task);
        }

        private void UnregisterTask(string path)
        {
            taskScheduler.RootFolder.DeleteTask(path, false);
        }
    }
}
