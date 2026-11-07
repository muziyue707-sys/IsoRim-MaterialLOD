param([string]$RunName='run03')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$run=Join-Path $root ('validation\'+$RunName)
$result=@{}
foreach($kind in @('protected','workshop')){
 $before=Get-Content -LiteralPath (Join-Path $run ($kind+'-before.json')) -Raw | ConvertFrom-Json
 $changed=@();$missing=@()
 foreach($entry in $before){
  if(!(Test-Path -LiteralPath $entry.Path)){$missing+=$entry.Path;continue}
  if((Get-FileHash -LiteralPath $entry.Path).Hash -ne $entry.Hash){$changed+=$entry.Path}
 }
 $result[$kind]=@{files=$before.Count;changed=$changed;missing=$missing;pass=($changed.Count -eq 0 -and $missing.Count -eq 0)}
}
$result|ConvertTo-Json -Depth 5|Tee-Object -FilePath (Join-Path $run 'protection-verification.json')
