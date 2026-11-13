param(
 [Parameter(Mandatory=$true)][string]$ExpectedSteamId,
 [ValidateSet('Upload','Verify')][string]$Mode='Verify',
 [string]$GameDir='E:\SteamLibrary\steamapps\common\RimWorld',
 [string]$ReceiptDir=(Join-Path $PSScriptRoot '..\dist\workshop-localization-040-20261114'),
 [string]$ReviewPath=(Join-Path $PSScriptRoot '..\review\workshop-localization-040-20261114.json')
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$receipt=$ReceiptDir
New-Item -ItemType Directory -Force -Path $receipt|Out-Null
$id=[ulong]([IO.File]::ReadAllText((Join-Path $root 'mod\About\PublishedFileId.txt')).Trim())
$env:SteamAppId='294100'; $env:SteamGameId='294100'; $env:PATH="$GameDir;$env:PATH"
$dll=Join-Path $GameDir 'com.rlabrecque.steamworks.net.dll'
Add-Type -Path $dll
[System.Runtime.InteropServices.NativeLibrary]::Load((Join-Path $GameDir 'steam_api64.dll'))|Out-Null
Add-Type -Path (Join-Path $PSScriptRoot 'WorkshopPublisher.cs') -ReferencedAssemblies @($dll,(Join-Path $PSHOME 'ref\System.Threading.Thread.dll'),(Join-Path $PSHOME 'ref\System.Collections.dll'))
if(-not [Steamworks.SteamAPI]::Init()){throw 'Steam initialization failed'}
try {
 if(-not [Steamworks.SteamUser]::BLoggedOn() -or [Steamworks.SteamUser]::GetSteamID().ToString() -ne $ExpectedSteamId){throw 'Wrong Steam account'}
 $previews=[uint32]0; $children=[ulong[]]@()
 $before=[IrmeWorkshopPublisher]::Query($id,[ref]$previews,[ref]$children,'english')
 if($before.m_eResult -ne [Steamworks.EResult]::k_EResultOK -or $before.m_ulSteamIDOwner.ToString() -ne $ExpectedSteamId){throw 'Item ownership/query mismatch'}
 $reports=@()
 foreach($language in @('english','schinese')){
  $title=[IO.File]::ReadAllText((Join-Path $root "workshop\locales\$language.title.txt")).Trim()
  $description=[IO.File]::ReadAllText((Join-Path $root "workshop\locales\$language.description.bbcode.txt")).Replace("`r`n","`n")
  if($Mode -eq 'Upload'){
   $result=[IrmeWorkshopPublisher]::UpdateTranslation($id,$language,$title,$description)
   $result|ConvertTo-Json -Depth 3|Set-Content (Join-Path $receipt "$language-submit.json")
   if($result.m_eResult -ne [Steamworks.EResult]::k_EResultOK -or $result.m_bUserNeedsToAcceptWorkshopLegalAgreement){throw "Translation submission incomplete: $language"}
  }
  $d=[IrmeWorkshopPublisher]::Query($id,[ref]$previews,[ref]$children,$language)
  $report=[ordered]@{id=$id;language=$language;result=[string]$d.m_eResult;title=$d.m_rgchTitle;titleMatches=($d.m_rgchTitle -eq $title);descriptionMatches=($d.m_rgchDescription.Replace("`r`n","`n") -eq $description);visibility=[string]$d.m_eVisibility;previewCount=$previews;dependencies=$children;bytes=$d.m_nFileSize;contentSizeUnchanged=($d.m_nFileSize -eq $before.m_nFileSize)}
  $report|ConvertTo-Json -Depth 3|Set-Content (Join-Path $receipt "$language-verify.json")
  if($d.m_eResult -ne [Steamworks.EResult]::k_EResultOK -or !$report.titleMatches -or !$report.descriptionMatches -or !$report.contentSizeUnchanged -or $d.m_eVisibility -ne $before.m_eVisibility -or $previews -ne 3 -or $children -notcontains [ulong]3819577715){throw "Translation verification failed: $language"}
  $reports+=$report
 }
 $reports|ConvertTo-Json -Depth 4|Tee-Object -FilePath $ReviewPath
}finally{[Steamworks.SteamAPI]::Shutdown()}
