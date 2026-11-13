param(
 [ValidateSet('Probe','Upload','Verify')][string]$Mode='Probe',
 [ValidateSet('Public','Unlisted')][string]$Visibility='Public',
 [Parameter(Mandatory=$true)][string]$ExpectedSteamId,
 [string]$GameDir='E:\SteamLibrary\steamapps\common\RimWorld',
 [string]$ReleaseDir=(Join-Path $PSScriptRoot '..\dist\workshop-040-20261114'),
 [string]$ChangeNote='0.4.0: optional dynamic normal/AO materials and LOD service; API77 compatibility, shared textures and safe private-material release. Compatible vehicle packs retain base camouflage 3D at LOD0 without this addon. Author: 水表狂魔.'
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$env:SteamAppId='294100'; $env:SteamGameId='294100'; $env:PATH="$GameDir;$env:PATH"
$dll=Join-Path $GameDir 'com.rlabrecque.steamworks.net.dll'
Add-Type -Path $dll
[System.Runtime.InteropServices.NativeLibrary]::Load((Join-Path $GameDir 'steam_api64.dll')) | Out-Null
Add-Type -Path (Join-Path $PSScriptRoot 'WorkshopPublisher.cs') -ReferencedAssemblies @($dll,(Join-Path $PSHOME 'ref\System.Threading.Thread.dll'),(Join-Path $PSHOME 'ref\System.Collections.dll'))
if(-not [Steamworks.SteamAPI]::Init()){throw 'Steam initialization failed'}
try {
 if(-not [Steamworks.SteamUser]::BLoggedOn() -or [Steamworks.SteamUser]::GetSteamID().ToString() -ne $ExpectedSteamId){throw 'Wrong Steam account; no upload performed'}
 if($Mode -eq 'Probe'){[pscustomobject]@{LoggedOn=$true;Persona=[Steamworks.SteamFriends]::GetPersonaName();AppId=294100}|ConvertTo-Json;return}
 New-Item -ItemType Directory -Force -Path $ReleaseDir|Out-Null
 $receipt=Join-Path $ReleaseDir 'item.json'
 $idFile=Join-Path $root 'mod\About\PublishedFileId.txt'
 $stage=Join-Path $ReleaseDir 'content'
 $description=[IO.File]::ReadAllText((Join-Path $root 'workshop\locales\english.description.bbcode.txt')).Replace("`r`n","`n")
 if(Test-Path $receipt){$item=Get-Content $receipt -Raw|ConvertFrom-Json; $id=[ulong]$item.id}
 elseif(Test-Path $idFile){$id=[ulong]([IO.File]::ReadAllText($idFile).Trim())}
 elseif($Mode -eq 'Upload'){
  if(!(Test-Path (Join-Path $ReleaseDir 'ready.json'))){throw 'Release validation manifest missing'}
  if(!(Test-Path (Join-Path $stage 'Assemblies\IRME.dll'))){throw 'Staged addon missing'}
  $created=[IrmeWorkshopPublisher]::Create()
  $created|ConvertTo-Json -Depth 3|Set-Content (Join-Path $ReleaseDir 'create-result.json')
  if($created.m_eResult -ne [Steamworks.EResult]::k_EResultOK){throw "Create failed: $($created.m_eResult)"}
  $id=[ulong]$created.m_nPublishedFileId.m_PublishedFileId
  @{id=$id;url="https://steamcommunity.com/sharedfiles/filedetails/?id=$id";createdUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content $receipt
  [IO.File]::WriteAllText($idFile,[string]$id)
  if($created.m_bUserNeedsToAcceptWorkshopLegalAgreement){throw "User must accept the Workshop agreement for item $id; item ID has been saved"}
 }else{throw 'No existing item ID; nothing to verify'}
 if($Mode -eq 'Upload'){
  $manifest=Get-Content (Join-Path $ReleaseDir 'ready.json') -Raw|ConvertFrom-Json
  foreach($entry in $manifest.content_files){if((Get-FileHash -LiteralPath (Join-Path $stage $entry.path)).Hash.ToLowerInvariant() -ne $entry.sha256){throw "Staged content changed: $($entry.path)"}}
  $previews=[uint32]0; $children=[ulong[]]@(); $before=[IrmeWorkshopPublisher]::Query($id,[ref]$previews,[ref]$children)
  if($before.m_ulSteamIDOwner.ToString() -ne $ExpectedSteamId){throw 'Item owner mismatch'}
  $gallery=@(); if($previews -eq 0){$gallery=@(Get-ChildItem (Join-Path $root 'workshop\details') -Filter '*.jpg'|Sort-Object Name|Select-Object -ExpandProperty FullName)}
  elseif($previews -ne 3){throw 'Unexpected gallery count; inspect before retrying'}
  [IO.File]::WriteAllText((Join-Path $stage 'About\PublishedFileId.txt'),[string]$id)
  $result=[IrmeWorkshopPublisher]::Upload($id,([IO.File]::ReadAllText((Join-Path $root 'workshop\locales\english.title.txt')).Trim()),$description,(Resolve-Path $stage).Path,(Join-Path $root 'workshop\cover.jpg'),[string[]]$gallery,($Visibility -eq 'Public'),$ChangeNote)
  $result|ConvertTo-Json -Depth 3|Set-Content (Join-Path $ReleaseDir 'submit-result.json')
  if($result.m_eResult -ne [Steamworks.EResult]::k_EResultOK){throw "Submit failed: $($result.m_eResult)"}
  if($result.m_bUserNeedsToAcceptWorkshopLegalAgreement){throw "User must accept Workshop agreement for item $id"}
  $dep=[IrmeWorkshopPublisher]::Dependency($id)
  $dep|ConvertTo-Json -Depth 3|Set-Content (Join-Path $ReleaseDir 'dependency-result.json')
  # Updating an existing item can report DuplicateRequest for an already-added dependency.
  # The remote verification below must still confirm the required child ID.
  if($dep.m_eResult -notin @([Steamworks.EResult]::k_EResultOK,[Steamworks.EResult]::k_EResultDuplicateRequest)){throw "Dependency failed: $($dep.m_eResult)"}
 }
 $previews=[uint32]0; $children=[ulong[]]@(); $d=[IrmeWorkshopPublisher]::Query($id,[ref]$previews,[ref]$children)
 $verification=[ordered]@{id=$id;url="https://steamcommunity.com/sharedfiles/filedetails/?id=$id";result=[string]$d.m_eResult;title=$d.m_rgchTitle;visibility=[string]$d.m_eVisibility;previewCount=$previews;dependencies=$children;bytes=$d.m_nFileSize;ownerMatches=($d.m_ulSteamIDOwner.ToString() -eq $ExpectedSteamId);descriptionMatches=($d.m_rgchDescription.Replace("`r`n","`n") -eq $description);banned=$d.m_bBanned}
 $verification|ConvertTo-Json -Depth 4|Tee-Object -FilePath (Join-Path $ReleaseDir 'remote-verification.json')
 $expectedVisibility=if($Visibility -eq 'Public'){[Steamworks.ERemoteStoragePublishedFileVisibility]::k_ERemoteStoragePublishedFileVisibilityPublic}else{[Steamworks.ERemoteStoragePublishedFileVisibility]::k_ERemoteStoragePublishedFileVisibilityUnlisted}
 if($d.m_eResult -ne [Steamworks.EResult]::k_EResultOK -or !$verification.ownerMatches -or !$verification.descriptionMatches -or $previews -ne 3 -or $children -notcontains [ulong]3819577715 -or $d.m_eVisibility -ne $expectedVisibility -or $d.m_bBanned){throw 'Remote publication verification incomplete'}
}finally{[Steamworks.SteamAPI]::Shutdown()}
