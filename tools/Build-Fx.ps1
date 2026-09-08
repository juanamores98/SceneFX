param(
    [Parameter(Mandatory = $true)][string]$ManagedPath,
    [switch]$Deploy
)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
foreach ($required in @('Assembly-CSharp.dll', 'ColossalManaged.dll', 'ICities.dll', 'UnityEngine.dll')) {
    if (!(Test-Path (Join-Path $ManagedPath $required))) { throw "Missing game reference: $required" }
}
$evidence = @()
foreach ($mod in @('SceneFX', 'LumenFX', 'AtmosphereFX', 'ClassicLightFX')) {
    $project = Join-Path $root "$mod/$mod.csproj"
    if (!(Test-Path $project)) { throw "Clone all four repositories side by side: $project" }
    & dotnet build $project -c Release "-p:ManagedDLLPath=$ManagedPath" "-p:DeployMod=$($Deploy.IsPresent.ToString().ToLowerInvariant())"
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $mod" }
    $dll = Join-Path $root "$mod/bin/Release/net35/$mod.dll"
    $evidence += [pscustomobject]@{ Mod=$mod; Commit=(& git -C (Join-Path $root $mod) rev-parse HEAD); DLL=$dll; SHA256=(Get-FileHash $dll -Algorithm SHA256).Hash }
}
$evidence | Format-Table -AutoSize
$evidence | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'build-evidence.local.json')
