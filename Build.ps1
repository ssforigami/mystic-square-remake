$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskUnity = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $taskUnity)) { throw 'Unity 6000.6.0f1 is required. Set taskUnity to its installed path.' }
New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'QA') | Out-Null
$taskArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + (Join-Path $taskRoot 'MysticSquare') + '"'), '-executeMethod', 'BuildEntry.Run', '-logFile', ('"' + (Join-Path $taskRoot 'QA\unity-build.log') + '"'))
$taskProcess = Start-Process -FilePath $taskUnity -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Build failed. See QA\unity-build.log (exit $($taskProcess.ExitCode))." }
Write-Output 'Build succeeded: Build\MysticSquare.exe'
