$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$release=Join-Path $project 'release\Windows桌宠三件套'
$originals=Join-Path $project 'upstream-assets'
$count=0
foreach($pair in @(@('SunPet','SunPet',28),@('ShaolanPet','ShaolanPet',37),@('GoodThingPet','GoodThingPet',11))) {
 $folder=Join-Path $release "assets\$($pair[0])"
 $source=Join-Path $originals "$($pair[1])\Assets"
 if((Get-FileHash -LiteralPath "$folder\face.png").Hash -ne (Get-FileHash -LiteralPath "$source\face.png").Hash){throw 'Avatar changed'}
 $clips=Get-Content -LiteralPath "$folder\clips.json" -Raw | ConvertFrom-Json
 if($clips.Count -ne $pair[2]){throw 'Wrong clip count'}
 foreach($clip in $clips){if(-not(Test-Path -LiteralPath "$folder\Clips\$($clip.file)")){throw 'Missing clip'}}
 foreach($file in Get-ChildItem -LiteralPath "$source\Clips" -File){if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath "$folder\Clips\$($file.Name)").Hash){throw 'Original audio changed'}}
 foreach($name in @('LICENSE','NOTICE.md')){if(-not(Test-Path -LiteralPath (Join-Path $release "licenses\$($pair[0])\$name"))){throw 'Missing attribution'}}
 $count+=$clips.Count
 Write-Output "PASS: $($pair[0]) original image/audio preserved; $($clips.Count) runtime clips"
}
foreach($name in @('SunPet','ShaolanPet','GoodThingPet','一起启动')) {
 $bytes=[IO.File]::ReadAllBytes((Join-Path $release "$name.exe"))
 if($bytes[0] -ne 77 -or $bytes[1] -ne 90){throw 'Invalid Windows executable'}
}
if(Test-Path -LiteralPath (Join-Path $release 'IntegrationHost.exe')){throw 'Test host leaked into package'}
if(Get-ChildItem -LiteralPath $release -Recurse -File -Include '*.log','*Tests.exe'){throw 'Test files leaked into package'}
if($count -ne 76){throw 'Missing audio'}
Write-Output 'PASS: portable package contains four Windows EXEs, 76 clips and all attribution; no test host/logs'
