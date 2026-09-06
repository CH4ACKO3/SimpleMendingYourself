param([Parameter(Mandatory=$true)][string]$ArtifactRoot,[switch]$DryRun,[switch]$CheckOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. "$PSScriptRoot/SteamSession.ps1"
$root=(Resolve-Path -LiteralPath $ArtifactRoot).Path
$m=Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
if ($m.appid -cne '294100' -or $m.publishedfileid -cne '3671535921' -or
    $m.version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$' -or
    ($m.tag -cne "v$($m.version)" -and !(($DryRun -or $CheckOnly) -and !$m.tag))) {
    throw 'Manifest must identify an explicit release tag and the existing Simple Mending Yourself Workshop item.'
}
$stage=Join-Path $root "SimpleMendingYourself-$($m.version)"
$archive=Join-Path $root $m.archive
if ((Get-FileHash $archive).Hash -cne $m.archiveSha256 -or
    (Get-FileHash (Join-Path $root 'release-notes.md')).Hash -cne $m.notesSha256) { throw 'Archive or notes checksum mismatch.' }
foreach ($language in @('en','zh-CN')) {
    $note=Join-Path $root "release-notes.$language.md"
    if ((Get-FileHash $note).Hash -cne $m.localizedNotes.$language) { throw "Localized release notes checksum mismatch: $language" }
}
$seen=@{}
foreach ($file in $m.files) {
    if ($file.path -match '(^/|:|\\|(^|/)\.\.(/|$))' -or $seen.ContainsKey($file.path)) { throw 'Invalid or duplicate manifest path.' }
    $seen[$file.path]=$true
    $path=Join-Path $stage $file.path
    if (!(Test-Path -LiteralPath $path) -or (Get-FileHash $path).Hash -cne $file.sha256) { throw "Content checksum mismatch: $($file.path)" }
}
if (@(Get-ChildItem -LiteralPath $stage -Recurse -File).Count -ne $seen.Count) { throw 'Unexpected files in package.' }
$about=[xml](Get-Content -LiteralPath (Join-Path $stage 'About/About.xml') -Raw)
if ($about.ModMetaData.modVersion -cne $m.version -or
    (Get-Content -LiteralPath (Join-Path $stage 'About/PublishedFileId.txt') -Raw).Trim() -cne '3671535921') { throw 'Package identity mismatch.' }
function Escape-Vdf([string]$value) {$value.Replace('\','\\').Replace('"','\"').Replace("`r",'')}
$changeNote=Get-Content -LiteralPath (Join-Path $root 'release-notes.en.md') -Raw
$vdf='"workshopitem"'+"`n{`n"+'  "appid" "294100"'+"`n"+'  "publishedfileid" "3671535921"'+"`n"+
    '  "contentfolder" "'+(Escape-Vdf $stage.Replace('\','/'))+'"'+"`n"+'  "changenote" "'+(Escape-Vdf $changeNote)+'"'+"`n}`n"
$vdfPath=Join-Path $root 'workshop-upload.vdf'
[IO.File]::WriteAllText($vdfPath,$vdf,[Text.UTF8Encoding]::new($false))
if ($DryRun) {Write-Host 'PASS: checksums, release identity and content VDF validated. No Steam login or upload performed.';return}
foreach ($key in @('STEAM_USERNAME','STEAM_PASSWORD','STEAM_CONFIG_VDF_BASE64','STEAM_REFRESH_TOKEN')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) {throw "Missing steam-workshop environment secret: $key"}
}
$checker=Join-Path $root 'publisher/WorkshopOwnerCheck.dll'
dotnet $checker check
if ($LASTEXITCODE) {throw 'Authorization or ownership preflight failed; no files uploaded.'}
$steam=Join-Path ([IO.Path]::GetTempPath()) ('smy-steam-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $steam 'config') -Force | Out-Null
try {
    Invoke-WebRequest -Uri 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip' -OutFile (Join-Path $steam 'steamcmd.zip')
    Expand-Archive -LiteralPath (Join-Path $steam 'steamcmd.zip') -DestinationPath $steam
    $bootstrap=& (Join-Path $steam 'steamcmd.exe') '+quit' 2>&1
    if ($LASTEXITCODE -notin @(0,7)) {throw 'SteamCMD initialization failed. No login attempted.'}
    [IO.File]::WriteAllBytes((Join-Path $steam 'config/config.vdf'),[Convert]::FromBase64String($env:STEAM_CONFIG_VDF_BASE64))
    Restore-SteamSession $steam
    Push-Location -LiteralPath $steam
    try {
        $probe=& (Join-Path $steam 'steamcmd.exe') '+@ShutdownOnFailedCommand' '1' '+@NoPromptForPassword' '1' '+login' $env:STEAM_USERNAME '+quit' 2>&1
        $cached=$LASTEXITCODE -eq 0 -and ($probe -join "`n") -match 'Waiting for user info\.\.\.\s*OK'
    } finally {Pop-Location}
    $args=@('+@ShutdownOnFailedCommand','1','+@NoPromptForPassword','1','+login',$env:STEAM_USERNAME)
    if (!$cached) {$args+=$env:STEAM_PASSWORD}
    if (!$CheckOnly) {$args+=@('+workshop_build_item',$vdfPath)}
    $args+='+quit'
    Push-Location -LiteralPath $steam
    try {$output=& (Join-Path $steam 'steamcmd.exe') @args 2>&1;$exitCode=$LASTEXITCODE} finally {Pop-Location}
    $text=$output -join "`n"
    if ($exitCode -eq 0 -and $text -match 'Waiting for user info\.\.\.\s*OK') {Save-SteamSession $steam}
    Write-Host ("SteamCMD diagnostics: exit={0}; userInfoComplete={1}; guardRequested={2}; invalidPassword={3}; networkFailure={4}" -f
        $exitCode,($text -match 'Waiting for user info\.\.\.\s*OK'),
        ($text -match '(?i)Steam Guard|two.factor|AccountLogonDenied|auth.*code|confirm.*sign.in'),
        ($text -match '(?i)InvalidPassword|Invalid Password'),($text -match '(?i)NoConnection|No Connection|Failed to connect|timeout'))
    if ($CheckOnly) {
        if ($exitCode -ne 0 -or $text -notmatch 'Waiting for user info\.\.\.\s*OK') {throw 'SteamCMD login check failed. Refresh Steam Guard locally; no Workshop content changed.'}
        Write-Host 'PASS: Steam login and Workshop ownership verified. No Workshop writes performed.';return
    }
    if ($exitCode -ne 0 -or $text -notmatch '(?i)\bSuccess\.\s+(?:Published|Updated)[^\r\n]*\b3671535921\b') {
        throw 'Steam did not confirm the Workshop update. Refresh authorization locally; raw authentication output is withheld.'
    }
    Write-Host "Steam confirmed Workshop item 3671535921 for $($m.tag)."
} finally {
    if ($steam.StartsWith([IO.Path]::GetTempPath(),[StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($steam) -match '^smy-steam-[0-9a-f]{32}$') {Remove-Item -LiteralPath $steam -Recurse -Force}
}
