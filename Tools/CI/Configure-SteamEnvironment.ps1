param(
    [string]$Repository='CH4ACKO3/SimpleMendingYourself',
    [string]$Environment='steam-workshop',
    [string]$SteamRoot='D:\Projects\rimworld\work\steam-authorization\steamcmd',
    [string]$RefreshTokenFile='D:\Projects\rimworld\work\steam-authorization\smy-refresh-token.txt'
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
    Push-Location -LiteralPath $SteamRoot
    try {
        & $steamcmd '+@ShutdownOnFailedCommand' '1' '+login' $account $password '+quit'
        if ($LASTEXITCODE) { throw 'SteamCMD login failed; no GitHub secrets were changed.' }
    } finally { Pop-Location }
    if (!(Test-Path -LiteralPath $config)) { throw 'SteamCMD did not create config/config.vdf.' }
    function Set-EnvironmentSecret([string]$Name,[string]$Value) {
        $Value | gh secret set $Name --repo $Repository --env $Environment
        if ($LASTEXITCODE) { throw "Failed to set GitHub environment secret: $Name" }
    }
    Set-EnvironmentSecret 'STEAM_USERNAME' $account
    Set-EnvironmentSecret 'STEAM_PASSWORD' $password
    Set-EnvironmentSecret 'STEAM_REFRESH_TOKEN' ([IO.File]::ReadAllText($RefreshTokenFile).Trim())
    Set-EnvironmentSecret 'STEAM_CONFIG_VDF_BASE64' ([Convert]::ToBase64String([IO.File]::ReadAllBytes($config)))
    Remove-Item -LiteralPath $RefreshTokenFile -Force
    Write-Host ''
    Write-Host 'Configuration complete. The local refresh-token file was removed.' -ForegroundColor Green
    Write-Host 'Return to Codex and reply: 好了' -ForegroundColor Green
} finally {
    if ($pointer -ne [IntPtr]::Zero) {[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)}
    $password=$null;$secure=$null
}
