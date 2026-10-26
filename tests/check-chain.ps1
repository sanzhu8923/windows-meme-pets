param([string]$DataDir = (Join-Path $PSScriptRoot 'session-02'))
$events = Get-ChildItem -LiteralPath $DataDir -Filter '*.log' | ForEach-Object {Get-Content -LiteralPath $_.FullName} | Where-Object {$_ -match 'PLAY role='} | Sort-Object
$events | Write-Output
if($events.Count -ne 4) { throw "Expected four spoken clips, got $($events.Count)" }
$expected = @('role=Shaolan.*hop=0 root=Shaolan','role=Sun.*hop=1 root=Shaolan','role=Good.*hop=2 root=Shaolan','role=Sun.*hop=3 root=Shaolan')
for($i=0;$i -lt 4;$i++) {if($events[$i] -notmatch $expected[$i]) {throw "Wrong event at $i"}}
Write-Output 'PASS: four-sentence response chain terminates at hop 3'
