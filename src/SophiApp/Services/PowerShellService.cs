// <copyright file="PowerShellService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using SophiApp.Contracts.Services;
    using System;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;

    /// <inheritdoc/>
    public class PowerShellService : IPowerShellService
    {
        private const string SetExecutionPolicy = "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force;";
        private readonly IProcessService processService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerShellService"/> class.
        /// </summary>
        /// <param name="processService">A service for working with Windows <see cref="Process"/> API.</param>
        public PowerShellService(IProcessService processService)
        {
            this.processService = processService;
        }

        /// <inheritdoc/>
        public void ClearCommonDialogViews()
        {
            var command = "Get-ChildItem -Path \"HKCU:\\Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\Bags\\*\\Shell\" -Recurse | Where-Object -FilterScript {$_.PSChildName -eq \"{885A186E-A440-4ADA-812B-DB871B942259}\"} | Remove-Item -Force";
            _ = Invoke(command);
        }

        /// <inheritdoc/>
        public bool? TurnOffDeviceNetworkAdapterExist()
        {
            var command = @"$AllowTurnOffDevice = $null
Get-NetAdapter -Physical | Where-Object -FilterScript {$_.MacAddress -and ($_.Status -eq ""Up"")} | Get-NetAdapterPowerManagement | Where-Object -FilterScript {$_.AllowComputerToTurnOffDevice -ne ""Unsupported""} | ForEach-Object -Process {
    $AllowTurnOffDevice = $_.AllowComputerToTurnOffDevice -eq ""Enabled""
}
$AllowTurnOffDevice";

            return InvokeOrDefault<bool>(command);
        }

        /// <inheritdoc/>
        public T? InvokeOrDefault<T>(string script)
            where T : struct
        {
            return (T?)Invoke(script.Insert(0, SetExecutionPolicy))[0]?.BaseObject ?? null;
        }

        /// <inheritdoc/>
        public T Invoke<T>(string script)
            where T : struct
        {
            return (T)Invoke(script.Insert(0, SetExecutionPolicy))[0].BaseObject;
        }

        /// <inheritdoc/>
        public List<PSObject> Invoke(string script)
        {
            using var runSpace = RunspaceFactory.CreateOutOfProcessRunspace(new TypeTable(Array.Empty<string>()), new PowerShellProcessInstance(new Version(5, 1), null, null, false));
            runSpace.Open();
            using var instance = PowerShell.Create(runSpace).AddScript(script.Insert(0, SetExecutionPolicy));
            return [.. instance.Invoke()];
        }

        /// <inheritdoc/>
        public void InvokeCommandBypassUCPD(string command)
        {
            var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var powershell = Path.Combine(systemRoot, "WindowsPowerShell\\v1.0\\powershell.exe");
            var powershellTemp = Path.Combine(systemRoot, "WindowsPowerShell\\v1.0\\powershell_temp.exe");
            File.Copy(powershell, powershellTemp, true);
            _ = processService.WaitForExit(name: powershellTemp, arguments: command);
            File.Delete(powershellTemp);
        }

        /// <inheritdoc/>
        public void SetTurnOffDeviceNetworkAdapterState(bool enable)
        {
            var allowTurnOff = enable ? "Enabled" : "Disabled";
            var command = $@"# Checking whether there's an adapter that has AllowComputerToTurnOffDevice property to manage
# We need also check for adapter status per some laptops have many equal adapters records in adapters list
$Adapters = Get-NetAdapter -Physical | Where-Object -FilterScript {{$_.MacAddress -and ($_.Status -eq ""Up"")}} | Get-NetAdapterPowerManagement | Where-Object -FilterScript {{$_.AllowComputerToTurnOffDevice -ne ""Unsupported""}}
$PhysicalAdaptersStatusUp = @(Get-NetAdapter -Physical | Where-Object -FilterScript {{($_.Status -eq ""Up"") -and $_.MacAddress}})

# Checking whether PC is currently connected to a Wi-Fi network
# NetConnectionStatus 2 is Wi-Fi
$InterfaceIndex = (Get-CimInstance -ClassName Win32_NetworkAdapter -Namespace root/CIMV2 | Where-Object -FilterScript {{$_.NetConnectionStatus -eq 2}}).InterfaceIndex
if (Get-NetAdapter -Physical | Where-Object -FilterScript {{($_.Status -eq ""Up"") -and ($_.PhysicalMediaType -eq ""Native 802.11"") -and ($_.InterfaceIndex -eq $InterfaceIndex)}})
{{
	# Get currently connected Wi-Fi network SSID
	$SSID = (Get-NetConnectionProfile).Name
}}

	foreach ($Adapter in $Adapters)
    {{
        $Adapter.AllowComputerToTurnOffDevice = ""{allowTurnOff}""
        $Adapter | Set-NetAdapterPowerManagement
    }}

# All network adapters are turned into ""Disconnected"" for few seconds, so we need to wait a bit to let them up
# Otherwise functions below will indicate that there is no the Internet connection
if ($PhysicalAdaptersStatusUp)
{{
	# If Wi-Fi network was used
	if ($SSID)
	{{
		# Connect to it
		netsh wlan connect name=$SSID
	}}

    while
	(
		Get-NetAdapter -Physical -Name $PhysicalAdaptersStatusUp.Name | Where-Object -FilterScript {{($_.Status -eq ""Disconnected"") -and $_.MacAddress}}
    )
	{{
		Start-Sleep -Seconds 2
	}}
}}";
            _ = Invoke(command);
        }
    }
}
