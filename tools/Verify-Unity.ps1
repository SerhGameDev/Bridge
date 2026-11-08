param(
    [Parameter(Mandatory=$true)][string]$UnityEditor,
    [ValidateSet('EditMode','WebGLBuild')][string]$Mode = 'EditMode'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = (Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion.Split('_')[0]
if ($version -notmatch '^\d+\.\d+\.\d+[abfp]\d+$') { throw "Unexpected Unity version: $version" }
$project = Join-Path $repo "work\unity\$version"
New-Item -ItemType Directory -Path "$project\Assets", "$project\Packages", "$project\ProjectSettings" -Force | Out-Null
$packagePath = (Join-Path $repo 'Packages\com.serhgamedev.bridge.core').Replace('\','/')
@{
    dependencies = @{
        'com.serhgamedev.bridge.core' = "file:$packagePath"
        'com.unity.test-framework' = '1.8.0'
        'com.unity.modules.jsonserialize' = '1.0.0'
    }
    testables = @('com.serhgamedev.bridge.core')
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$project\Packages\manifest.json" -Encoding utf8
"m_EditorVersion: $version" | Set-Content -LiteralPath "$project\ProjectSettings\ProjectVersion.txt" -Encoding utf8
$sample = Join-Path $repo 'Packages\com.serhgamedev.bridge.core\Samples~\BasicUsage'
New-Item -ItemType Directory -Path "$project\Assets\BridgeSmoke" -Force | Out-Null
Copy-Item -Path "$sample\*" -Destination "$project\Assets\BridgeSmoke" -Recurse -Force
$unityArgs = @('-batchmode','-nographics','-projectPath',"`"$project`"",'-logFile',"`"$project\$Mode.log`"")
if ($Mode -eq 'EditMode') {
    if (Test-Path -LiteralPath "$project\test-results.xml") { Remove-Item -LiteralPath "$project\test-results.xml" }
    $unityArgs += @('-runTests','-testPlatform','EditMode','-testFilter','Bridge.Core.EditorTests','-testResults',"`"$project\test-results.xml`"")
} else {
    $unityArgs += @('-buildTarget','WebGL','-executeMethod','Bridge.Samples.BridgeSmokeBuild.BuildWebGl','-quit')
}
$run = Start-Process -FilePath $UnityEditor -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
Write-Output "Unity $version PID=$($run.Id); mode=$Mode; project=$project"
$run.WaitForExit()
if ($run.ExitCode -ne 0) { throw "Unity exited with $($run.ExitCode). See $project\$Mode.log" }
if ($Mode -eq 'EditMode') {
    [xml]$results = Get-Content -LiteralPath "$project\test-results.xml"
    $results.'test-run' | Select-Object result,total,passed,failed,skipped
    if ($results.'test-run'.result -ne 'Passed' -or [int]$results.'test-run'.total -lt 1) { throw 'Unity tests did not pass.' }
}
