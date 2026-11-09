param(
 [string]$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe',
 [ValidateSet('EditMode','PlayMode','WindowsBuild','WebGLBuild')][string]$Mode='EditMode'
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version=(Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion.Split('_')[0]
$project=Join-Path $repo "work/runtime-unity/$version"
New-Item -ItemType Directory -Path "$project/Assets","$project/Packages","$project/ProjectSettings" -Force | Out-Null
$deps=@{'com.unity.test-framework'='1.8.0';'com.unity.modules.jsonserialize'='1.0.0';'com.unity.modules.physics'='1.0.0'}
foreach($name in @('core','runtime','unity')) { $deps["com.serhgamedev.bridge.$name"]='file:'+((Join-Path $repo "Packages/com.serhgamedev.bridge.$name").Replace('\','/')) }
@{dependencies=$deps;testables=@('com.serhgamedev.bridge.unity')} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$project/Packages/manifest.json" -Encoding utf8
"m_EditorVersion: $version" | Set-Content -LiteralPath "$project/ProjectSettings/ProjectVersion.txt" -Encoding utf8
New-Item -ItemType Directory -Path "$project/Assets/BridgeRuntimeDemo" -Force | Out-Null
Copy-Item -Path "$repo/Packages/com.serhgamedev.bridge.unity/Samples~/SceneDemo/*" -Destination "$project/Assets/BridgeRuntimeDemo" -Recurse -Force
$unityArgs=@('-batchmode','-nographics','-projectPath',"`"$project`"",'-logFile',"`"$project/$Mode.log`"")
if($Mode -in @('EditMode','PlayMode')) {
 $xml="$project/$Mode.xml"
 if(Test-Path -LiteralPath $xml){Remove-Item -LiteralPath $xml}
 $unityArgs+=@('-runTests','-testPlatform',$Mode,'-testFilter',$(if($Mode -eq 'EditMode'){'Bridge.Unity.EditorTests'}else{'Bridge.Unity.Demo.PlayTests'}),'-testResults',"`"$xml`"")
} else {
 $web=$Mode -eq 'WebGLBuild'
 $unityArgs+=@('-buildTarget',$(if($web){'WebGL'}else{'StandaloneWindows64'}),'-executeMethod',$(if($web){'Bridge.Unity.Demo.Editor.RuntimeDemoBuild.BuildWebGL'}else{'Bridge.Unity.Demo.Editor.RuntimeDemoBuild.BuildWindows'}),'-quit')
}
$run=Start-Process -FilePath $UnityEditor -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
Write-Output "Unity $version PID=$($run.Id); $Mode; $project"
$run.WaitForExit()
if($run.ExitCode -ne 0){throw "Unity exit $($run.ExitCode); see $project/$Mode.log"}
if($Mode -in @('EditMode','PlayMode')) {
 [xml]$results=Get-Content -LiteralPath $xml
 $results.'test-run' | Select-Object result,total,passed,failed,skipped
 if($results.'test-run'.result -ne 'Passed' -or [int]$results.'test-run'.total -lt 2){throw 'Unity tests did not pass.'}
}
