param(
    [string]$Repository='CH4ACKO3/SimpleMendingYourself',
    [string]$Environment='steam-workshop',
    [string]$SteamRoot='D:\Projects\rimworld\work\steam-authorization\steamcmd',
    [string]$RefreshTokenFile='D:\Projects\rimworld\work\steam-authorization\smy-refresh-token.txt',
    [string]$CredentialBundle=(Join-Path $env:LOCALAPPDATA 'Codex\SteamWorkshopPublisher\credentials.dpapi'),
    [switch]$ReuseSteamConfig
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$steamcmd=Join-Path $SteamRoot 'steamcmd.exe'
$config=Join-Path $SteamRoot 'config/config.vdf'
if (!(Test-Path -LiteralPath $steamcmd) -or !(Test-Path -LiteralPath $RefreshTokenFile)) {
    throw 'SteamCMD or the newly generated refresh token is missing.'
}
$account=Read-Host 'Steam account'
$secure=Read-Host 'Steam password' -AsSecureString
$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    if (!$ReuseSteamConfig) {
        Push-Location -LiteralPath $SteamRoot
        try {
            & $steamcmd '+@ShutdownOnFailedCommand' '1' '+login' $account $password '+quit'
            if ($LASTEXITCODE) { throw 'SteamCMD login failed; no GitHub secrets were changed.' }
        } finally { Pop-Location }
    }
    if (!(Test-Path -LiteralPath $config)) { throw 'SteamCMD did not create config/config.vdf.' }
    function Set-EnvironmentSecret([string]$Name,[string]$Value) {
        $Value | gh secret set $Name --repo $Repository --env $Environment
        if ($LASTEXITCODE) { throw "Failed to set GitHub environment secret: $Name" }
    }
    $refreshToken=[IO.File]::ReadAllText($RefreshTokenFile).Trim()
    $configBase64=[Convert]::ToBase64String([IO.File]::ReadAllBytes($config))
    Set-EnvironmentSecret 'STEAM_USERNAME' $account
    Set-EnvironmentSecret 'STEAM_PASSWORD' $password
    Set-EnvironmentSecret 'STEAM_REFRESH_TOKEN' $refreshToken
    Set-EnvironmentSecret 'STEAM_CONFIG_VDF_BASE64' $configBase64
    $bundleDirectory=Split-Path -Parent ([IO.Path]::GetFullPath($CredentialBundle))
    New-Item -ItemType Directory -Path $bundleDirectory -Force | Out-Null
    $json=[ordered]@{version=1;username=$account;password=$password;refreshToken=$refreshToken;configVdfBase64=$configBase64} | ConvertTo-Json -Compress
    $protected=$json | ConvertTo-SecureString -AsPlainText -Force | ConvertFrom-SecureString
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($CredentialBundle),$protected,[Text.UTF8Encoding]::new($false))
    Remove-Item -LiteralPath $RefreshTokenFile -Force
    Write-Host ''
    Write-Host 'Configuration complete. The reusable credential bundle is protected for the current Windows user.' -ForegroundColor Green
    Write-Host 'The plaintext local refresh-token file was removed.' -ForegroundColor Green
    Write-Host 'Return to Codex and reply: 好了' -ForegroundColor Green
} finally {
    if ($pointer -ne [IntPtr]::Zero) {[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)}
    $password=$null;$refreshToken=$null;$configBase64=$null;$json=$null;$secure=$null
}
