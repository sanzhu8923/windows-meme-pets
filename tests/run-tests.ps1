$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework=Split-Path $compiler
& $compiler /nologo /target:exe "/out:$PSScriptRoot\CoreTests.exe" /reference:System.Runtime.Serialization.dll "$project\src\Core.cs" "$PSScriptRoot\CoreTests.cs"
if($LASTEXITCODE -ne 0){throw 'Core test compilation failed'}
& "$PSScriptRoot\CoreTests.exe"
if($LASTEXITCODE -ne 0){throw 'Core tests failed'}
$refs=@('System.dll','System.Core.dll','System.Xaml.dll')|ForEach-Object {'/reference:'+(Join-Path $framework $_)}
$refs+=@('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll')|ForEach-Object {'/reference:'+(Join-Path $framework "WPF\$_")}
& $compiler /nologo /target:exe "/out:$PSScriptRoot\MediaTests.exe" "/reference:$project\release\Windows桌宠三件套\SunPet.exe" @refs "$PSScriptRoot\MediaTests.cs"
if($LASTEXITCODE -ne 0){throw 'Media test compilation failed'}
Copy-Item -LiteralPath "$project\release\Windows桌宠三件套\SunPet.exe" -Destination "$PSScriptRoot\SunPet.exe" -Force
& "$PSScriptRoot\MediaTests.exe" "$project\release\Windows桌宠三件套"
if($LASTEXITCODE -ne 0){throw 'Media tests failed'}
