param([string]$RunName='lod-run01', [ValidateSet('lod','material','comparison','floor')][string]$Mode='lod', [switch]$Baseline, [string]$HostDll, [switch]$FreshComparison)
$ErrorActionPreference='Stop'
$root=Join-Path $PSScriptRoot '..\validation'
$repo=Join-Path $PSScriptRoot '..\..\IsoRim-public-api'
$addon=Join-Path $PSScriptRoot '..'
$game='E:\SteamLibrary\steamapps\common\RimWorld'
$ws='E:\SteamLibrary\steamapps\workshop\content\294100'
if($RunName -notmatch '^[a-zA-Z0-9_-]+$'){throw 'RunName must be a simple directory name'}
$runtime=Join-Path $root ('runtime-'+$RunName)
$run=Join-Path $root $RunName
if(Test-Path -LiteralPath $run){throw 'Use a new run name'}
if(Get-Process -Name RimWorld -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $runtime 'RimWorld.exe') }){throw 'This test runtime is already running'}
if(!(Test-Path -LiteralPath $runtime)){
 New-Item -ItemType Directory -Path $runtime,(Join-Path $runtime 'Mods') -Force|Out-Null
 Get-ChildItem -LiteralPath $game -File|ForEach-Object {New-Item -ItemType HardLink -Path (Join-Path $runtime $_.Name) -Target $_.FullName|Out-Null}
 foreach($n in @('Data','vulkan')){New-Item -ItemType Junction -Path (Join-Path $runtime $n) -Target (Join-Path $game $n)|Out-Null}
 Copy-Item -LiteralPath (Join-Path $game 'Rendering') -Destination $runtime -Recurse
 Copy-Item -LiteralPath "$ws\2009463077" -Destination (Join-Path $runtime 'Mods\Harmony') -Recurse
 New-Item -ItemType Directory -Path (Join-Path $runtime 'Mods\IsoRim\1.7\Assemblies') -Force|Out-Null
 foreach($n in @('About','Defs','Models','Shaders','Textures','Languages','LUTs')){New-Item -ItemType Junction -Path (Join-Path $runtime ('Mods\IsoRim\'+$n)) -Target (Join-Path $repo $n)|Out-Null}
 Copy-Item -LiteralPath (Join-Path $repo 'LoadFolders.xml') -Destination (Join-Path $runtime 'Mods\IsoRim')
 New-Item -ItemType Junction -Path (Join-Path $runtime 'Mods\Example') -Target (Join-Path $addon $(if($Baseline){'tests\BaselineExample'}else{'example'}))|Out-Null
}
if(!$HostDll){$HostDll=Join-Path $addon 'build\IsoRim.dll'}
if($Mode -eq 'floor'){
 $floorTarget=Join-Path $runtime 'Mods\IsoRimZ'
 if(!(Test-Path $floorTarget)){Copy-Item -LiteralPath "$ws\3824503834" -Destination $floorTarget -Recurse}
}
Copy-Item -LiteralPath $HostDll -Destination (Join-Path $runtime 'Mods\IsoRim\1.7\Assemblies\IsoRim.dll')
foreach($pair in @(@('Addon','mod'),@('Tests',$(if($Mode -eq 'comparison'){'tests\Comparison'}else{'tests'})))){$dest=Join-Path $runtime ('Mods\'+$pair[0]);if(!(Test-Path $dest)){New-Item -ItemType Junction -Path $dest -Target (Join-Path $addon $pair[1])|Out-Null}}
New-Item -ItemType Directory -Path (Join-Path $run 'Config') -Force|Out-Null
Set-Content -LiteralPath (Join-Path $run 'Config\ModsConfig.xml') '<ModsConfigData><version>1.7.4995</version><activeMods><li>brrainz.harmony</li><li>ludeon.rimworld</li><li>peren.isorim17</li><li>shuib.rimzrism.materials</li><li>shuib.rimzrism.materials.example</li><li>shuib.rimzrism.materials.tests</li></activeMods></ModsConfigData>'
Set-Content -LiteralPath (Join-Path $run 'Config\Prefs.xml') '<PrefsData><runInBackground>True</runInBackground><pauseOnLoad>False</pauseOnLoad><pauseOnError>False</pauseOnError><devMode>True</devMode><langFolderName>English</langFolderName></PrefsData>'
$protected=Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios'
Get-ChildItem -LiteralPath "$protected\Config","$protected\Saves" -File -Recurse | Get-FileHash | Select-Object Path,Hash | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $run 'protected-before.json')
Get-ChildItem -LiteralPath "$ws\3819577715","$ws\3824503834" -File -Recurse | Get-FileHash | Select-Object Path,Hash | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $run 'workshop-before.json')
$configFile=Join-Path $run 'Config\ModsConfig.xml'
$configText=[IO.File]::ReadAllText($configFile)
if($Mode -eq 'floor'){$configText=$configText.Replace('<li>peren.isorim17</li>','<li>peren.isorim17</li><li>peren.isorimprojectz</li>')}
if($Baseline){$configText=$configText.Replace('<li>shuib.rimzrism.materials</li>','')}
if($Mode -eq 'comparison'){
 $configText=$configText.Replace('shuib.rimzrism.materials.tests','shuib.rimzrism.comparison')
 New-Item -ItemType Directory -Path (Join-Path $run 'Saves') -Force|Out-Null
 if(!$FreshComparison){Copy-Item (Join-Path $root 'material-run01\Saves\IRME_Acceptance.rws') (Join-Path $run 'Saves\Comparison.rws')}
}
[IO.File]::WriteAllText($configFile,$configText)
Get-FileHash $HostDll,(Join-Path $addon 'mod\Assemblies\IRME.dll'),(Join-Path $addon 'tests\Assemblies\IRME.Tests.dll')|Select-Object Path,Hash|ConvertTo-Json|Set-Content (Join-Path $run 'binaries.json')
$extra=if($Mode -eq 'floor'){'-projectzfloors=3'}else{''}
if($FreshComparison){$extra+=' -irme-fresh-comparison'}
$process=Start-Process -FilePath (Join-Path $runtime 'RimWorld.exe') -WorkingDirectory $runtime -ArgumentList "-NoWindow -quicktest -isorim3d -isorim-normal-$Mode-test $extra -savedatafolder=$run -NoFileLog -screen-fullscreen 0 -screen-width 1600 -screen-height 1000" -RedirectStandardOutput (Join-Path $run 'Player.log') -RedirectStandardError (Join-Path $run 'stderr.log') -WindowStyle Hidden -PassThru
@{pid=$process.Id;run=$run}|ConvertTo-Json | Tee-Object -FilePath (Join-Path $run 'process.json')
