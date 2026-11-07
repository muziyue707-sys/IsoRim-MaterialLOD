param([string]$GameDir='E:\SteamLibrary\steamapps\common\RimWorld', [string]$IsoRimSource=(Join-Path $PSScriptRoot '..\..\IsoRim-api61-release'), [string]$HarmonyDll='E:\SteamLibrary\steamapps\workshop\content\294100\2009463077\1.7\Assemblies\0Harmony.dll', [string]$IsoRimZDll='E:\SteamLibrary\steamapps\workshop\content\294100\3824503834\1.7\Assemblies\IsoRimProjectZ.dll', [switch]$HostOnly, [string]$HostOutput=(Join-Path $PSScriptRoot '..\build'), [string]$PrebuiltHostDll)
$ErrorActionPreference='Stop'
$repo=$IsoRimSource
$game=$GameDir
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$refs=@(Get-ChildItem -LiteralPath $game -File -Filter '*.dll' | Select-Object -ExpandProperty FullName)
$refs+=$HarmonyDll
$metadata=[Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
foreach($path in $refs){try{$null=[Reflection.AssemblyName]::GetAssemblyName($path);$metadata.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path))}catch [BadImageFormatException]{}}
$parse=[Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithPreprocessorSymbols([string[]]@('RW16','RW17'))
$options=[Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary).WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release).WithAllowUnsafe($true).WithDeterministic($true)
$folder=$HostOutput
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$output=Join-Path $folder 'IsoRim.dll'
if($PrebuiltHostDll){
 Copy-Item -LiteralPath $PrebuiltHostDll -Destination $output -Force
 Write-Output 'HOST: using exact supplied binary (no host source changes)'
}else{
$trees=[Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
$sources=@(Get-ChildItem (Join-Path $repo 'Source') -Filter '*.cs' -Recurse | Where-Object FullName -NotMatch '\\(bin|obj)\\' | Select-Object -ExpandProperty FullName)

foreach($path in $sources){$source=[Microsoft.CodeAnalysis.Text.SourceText]::From([IO.File]::ReadAllText($path),[Text.Encoding]::UTF8);$trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($source,$parse,$path))}

$compilation=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('IsoRim',$trees,$metadata,$options)
$diagnostics=$compilation.GetDiagnostics()
$errors=@($diagnostics | Where-Object Severity -eq Error)
if($errors.Count){throw ($errors -join "`n")}
$folder=$HostOutput
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$diagnostics | ForEach-Object ToString | Set-Content (Join-Path $folder 'diagnostics.txt')
$output=Join-Path $folder 'IsoRim.dll'
$stream=[IO.File]::Create($output)
try{$result=$compilation.Emit($stream)}finally{$stream.Dispose()}
if(!$result.Success){throw ($result.Diagnostics -join "`n")}
"BUILD PASS sources=$($sources.Count)  output=$output"
Get-FileHash -LiteralPath $output
}

if($HostOnly){return}
$hash=(Get-FileHash -LiteralPath $output).Hash
$addon=Join-Path $PSScriptRoot '..'
Set-Content -LiteralPath (Join-Path $addon 'mod\Source\Compatibility.cs') -Value ('namespace RimZrism.Materials { internal static class Compatibility { internal const string IsoRimSha256="'+$hash+'"; } }')
$metadata.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile((Resolve-Path $output).Path))
function Compile-Addon($name,$files,$destination){
 $syntax=[Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
 foreach($file in $files){$syntax.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([Microsoft.CodeAnalysis.Text.SourceText]::From([IO.File]::ReadAllText($file),[Text.Encoding]::UTF8),$parse,$file))}
 $compile=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create($name,$syntax,$metadata,$options)
 $errors=@($compile.GetDiagnostics()|Where-Object Severity -eq Error)
 if($errors.Count){throw ($errors -join "`n")}
 New-Item -ItemType Directory -Force -Path (Split-Path $destination)|Out-Null
 $stream=[IO.File]::Create($destination)
 try{$r=$compile.Emit($stream)}finally{$stream.Dispose()}
 if(!$r.Success){throw ($r.Diagnostics -join "`n")}
 Write-Output "BUILD PASS $name"
}
$addonDll=Join-Path $addon 'mod\Assemblies\IRME.dll'
Compile-Addon 'IRME' @(Get-ChildItem (Join-Path $addon 'mod\Source') -Filter '*.cs'|Select-Object -ExpandProperty FullName) $addonDll
$metadata.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile((Resolve-Path $addonDll).Path))
$floorDll=$IsoRimZDll
if(Test-Path $floorDll){$metadata.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile((Resolve-Path $floorDll).Path))}
$testFiles=@(Get-ChildItem (Join-Path $addon 'tests') -Filter '*.cs' | Where-Object { $_.Name -ne 'FloorRuntime.cs' -or (Test-Path $floorDll) } | Select-Object -ExpandProperty FullName)
if($testFiles.Count){Compile-Addon 'IRME.Tests' $testFiles (Join-Path $addon 'tests\Assemblies\IRME.Tests.dll')}

if(Test-Path (Join-Path $addon tests\Comparison\Runtime.cs)){Compile-Addon IRME.Comparison @((Join-Path $addon tests\Comparison\Runtime.cs)) (Join-Path $addon tests\Comparison\Assemblies\IRME.Comparison.dll)}
