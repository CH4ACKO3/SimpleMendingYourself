param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$output=[IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Dependency output directory must not already exist.' }
New-Item -ItemType Directory -Path $output | Out-Null
$steam=Join-Path $output 'steamcmd'
New-Item -ItemType Directory -Path $steam | Out-Null
Invoke-WebRequest -Uri 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip' -OutFile (Join-Path $steam 'steamcmd.zip')
Expand-Archive -LiteralPath (Join-Path $steam 'steamcmd.zip') -DestinationPath $steam
Push-Location -LiteralPath $steam
try {
    $bootstrap=& (Join-Path $steam 'steamcmd.exe') '+quit' 2>&1
    if ($LASTEXITCODE -notin @(0,7)) { throw 'SteamCMD dependency client initialization failed.' }
    $result=& (Join-Path $steam 'steamcmd.exe') '+@ShutdownOnFailedCommand' '1' '+login' 'anonymous' '+workshop_download_item' '294100' '3657705987' 'validate' '+quit' 2>&1
    $exitCode=$LASTEXITCODE
} finally { Pop-Location }
$text=$result -join "`n"
if ($exitCode -ne 0 -or $text -notmatch 'Success\. Downloaded item 3657705987') {
    throw 'Unable to fetch the pinned Simple Mending build from Steam Workshop.'
}
$dll=Join-Path $steam 'steamapps/workshop/content/294100/3657705987/1.6/Assemblies/ComfyCuddlesWithEuterpe.dll'
if (!(Test-Path -LiteralPath $dll)) { throw 'Simple Mending dependency assembly was not found.' }
$expected='54210D9BE6E7D0D77C3B09A5A6112F51EBEDFED619ABE56D5253A39D0DEF030A'
if ((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash -cne $expected) {
    throw 'Simple Mending dependency changed upstream. Review compatibility and update the pinned checksum explicitly.'
}
Write-Output $dll
