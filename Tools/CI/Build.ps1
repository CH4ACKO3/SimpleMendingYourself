param([string]$Tag='', [string]$OutputRoot='artifacts/ci')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $root
$about=[xml](Get-Content About/About.xml -Raw)
$version=[string]$about.ModMetaData.modVersion
$semver='^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$'
if ($version -notmatch $semver) { throw 'Invalid About.xml modVersion.' }
if ($Tag -and $Tag -cne "v$version") { throw "Tag '$Tag' must equal About.xml version 'v$version'." }
foreach ($language in @('en','zh-CN')) {
    if ($Tag -and !(Test-Path -LiteralPath "Docs/releases/$version.$language.md")) { throw "Missing localized release notes: $version.$language.md" }
}
foreach ($xml in Get-ChildItem About,Defs,Languages,Patches -Filter '*.xml' -Recurse) {
    [void][xml](Get-Content -LiteralPath $xml.FullName -Raw)
}
$english='Languages/English/Keyed/Keys.xml'
$baseKeys=@(([xml](Get-Content $english -Raw)).LanguageData.ChildNodes | Where-Object NodeType -eq 'Element' | ForEach-Object Name | Sort-Object)
foreach ($file in Get-ChildItem Languages -Recurse -Filter 'Keys.xml') {
    $keys=@(([xml](Get-Content $file.FullName -Raw)).LanguageData.ChildNodes | Where-Object NodeType -eq 'Element' | ForEach-Object Name | Sort-Object)
    if (Compare-Object $baseKeys $keys) { throw "Translation keys differ: $($file.FullName)" }
}
$out=[IO.Path]::GetFullPath((Join-Path $root $OutputRoot))
if (Test-Path -LiteralPath $out) { throw 'Output directory already exists; use a new release directory.' }
$dependency=Join-Path ([IO.Path]::GetTempPath()) ('smy-dependency-'+[guid]::NewGuid().ToString('N'))
try {
    $simpleMendingDll=& "$PSScriptRoot/Fetch-SimpleMending.ps1" -OutputDirectory $dependency
    dotnet build Source/SimpleMendingYourself/SimpleMendingYourself.csproj -c Release -warnaserror `
        -p:UseReferencePackages=true -p:SkipModCopy=true -p:SimpleMendingAssemblyPath="$simpleMendingDll"
    if ($LASTEXITCODE) { throw 'Build failed.' }
    New-Item -ItemType Directory -Path $out | Out-Null
    $stage=Join-Path $out "SimpleMendingYourself-$version"
    New-Item -ItemType Directory -Path $stage | Out-Null
    foreach ($directory in @('About','Defs','Languages','Patches')) {
        Copy-Item -LiteralPath (Join-Path $root $directory) -Destination $stage -Recurse
    }
    foreach ($file in @('LICENSE','LoadFolders.xml','README.md')) {
        Copy-Item -LiteralPath (Join-Path $root $file) -Destination $stage
    }
    New-Item -ItemType Directory -Path (Join-Path $stage 'Assemblies') | Out-Null
    Copy-Item -LiteralPath 'Source/SimpleMendingYourself/bin/Release/SimpleMendingYourself.dll' -Destination (Join-Path $stage 'Assemblies')
    [IO.File]::WriteAllText((Join-Path $stage 'About/PublishedFileId.txt'),'3671535921',[Text.UTF8Encoding]::new($false))
    $zip=Join-Path $out "SimpleMendingYourself-$version.zip"
    Compress-Archive -LiteralPath $stage -DestinationPath $zip
    $localizedNotes=[ordered]@{}
    $noteTexts=@{}
    foreach ($language in @('en','zh-CN')) {
        $source="Docs/releases/$version.$language.md"
        $text=if (Test-Path -LiteralPath $source) { Get-Content -LiteralPath $source -Raw } else { "Development build $version" }
        if ([string]::IsNullOrWhiteSpace($text) -or $text.Contains('\n')) { throw "Invalid localized release notes: $language" }
        $noteTexts[$language]=$text.TrimEnd()
        $target=Join-Path $out "release-notes.$language.md"
        [IO.File]::WriteAllText($target,$noteTexts[$language],[Text.UTF8Encoding]::new($false))
        $localizedNotes[$language]=(Get-FileHash $target).Hash
    }
    $combined=$noteTexts.en+"`n`n"+$noteTexts.'zh-CN'+"`n"
    [IO.File]::WriteAllText((Join-Path $out 'release-notes.md'),$combined,[Text.UTF8Encoding]::new($false))
    $files=@(Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object {
        [ordered]@{path=[IO.Path]::GetRelativePath($stage,$_.FullName).Replace('\','/');sha256=(Get-FileHash $_.FullName).Hash}
    })
    [ordered]@{version=$version;tag=$Tag;appid='294100';publishedfileid='3671535921';commit=(git rev-parse HEAD);
        archive=[IO.Path]::GetFileName($zip);archiveSha256=(Get-FileHash $zip).Hash;localizedNotes=$localizedNotes;
        notesSha256=(Get-FileHash (Join-Path $out 'release-notes.md')).Hash;files=$files} |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'manifest.json')
    dotnet publish Tools/WorkshopOwnerCheck/WorkshopOwnerCheck.csproj -c Release -warnaserror -o (Join-Path $out 'publisher')
    if ($LASTEXITCODE) { throw 'Workshop ownership checker build failed.' }
    if ($env:GITHUB_OUTPUT) { "version=$version" >> $env:GITHUB_OUTPUT }
    Write-Host "Validated release package: $zip"
} finally {
    if ((Test-Path -LiteralPath $dependency) -and
        [IO.Path]::GetFileName($dependency) -match '^smy-dependency-[0-9a-f]{32}$' -and
        $dependency.StartsWith([IO.Path]::GetTempPath(),[StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $dependency -Recurse -Force
    }
}
