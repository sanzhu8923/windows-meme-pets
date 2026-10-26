$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$release = Join-Path $project 'release\Windows桌宠三件套'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework = Split-Path $compiler
$wpf = Join-Path $framework 'WPF'
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Runtime.Serialization.dll','System.Drawing.dll','System.Windows.Forms.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $wpf $_) }
$sources = @((Join-Path $project 'src\Core.cs'), (Join-Path $project 'src\App.cs'))
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/win32manifest:$project\src\app.manifest" "/out:$release\SunPet.exe" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
foreach ($name in @('ShaolanPet','GoodThingPet','一起启动')) {
 Copy-Item -LiteralPath (Join-Path $release 'SunPet.exe') -Destination (Join-Path $release "$name.exe") -Force
}
$config = '<?xml version="1.0"?><configuration><startup useLegacyV2RuntimeActivationPolicy="true"><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/></startup></configuration>'
foreach ($name in @('SunPet','ShaolanPet','GoodThingPet','一起启动')) { [IO.File]::WriteAllText((Join-Path $release "$name.exe.config"),$config) }
$originals = Join-Path $project 'upstream-assets'
foreach($pair in @(@('SunPet','SunPet'),@('ShaolanPet','ShaolanPet'),@('GoodThingPet','GoodThingPet'))) {
 $assets = Join-Path $release "assets\$($pair[0])"
 New-Item -ItemType Directory -Path $assets -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $originals "$($pair[1])\Assets\face.png") -Destination $assets -Force
 Copy-Item -LiteralPath (Join-Path $originals "$($pair[1])\Assets\clips.json") -Destination $assets -Force
 Copy-Item -LiteralPath (Join-Path $originals "$($pair[1])\Assets\Clips") -Destination $assets -Recurse -Force
 $legal=Join-Path $release "licenses\$($pair[0])"
 New-Item -ItemType Directory -Path $legal -Force | Out-Null
 foreach($name in @('LICENSE','NOTICE.md')) { Copy-Item -LiteralPath (Join-Path $originals "$($pair[1])\$name") -Destination $legal -Force }
}
python (Join-Path $project 'tools\prepare_audio.py') $release
if($LASTEXITCODE -ne 0){throw 'Audio preparation failed'}
Copy-Item -LiteralPath (Join-Path $project '使用说明.txt') -Destination $release -Force
Copy-Item -LiteralPath (Join-Path $project 'NOTICE.md') -Destination $release -Force
Write-Output "BUILD OK: $release"
