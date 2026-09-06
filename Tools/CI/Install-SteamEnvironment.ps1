param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')][string]$Repository,
    [string]$Environment='steam-workshop',
    [string]$CredentialBundle=(Join-Path $env:LOCALAPPDATA 'Codex\SteamWorkshopPublisher\credentials.dpapi')
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$bundle=[IO.Path]::GetFullPath($CredentialBundle)
if (!(Test-Path -LiteralPath $bundle)) {throw 'Reusable Steam credential bundle not found. Run Configure-SteamEnvironment.ps1 once.'}
$encrypted=[IO.File]::ReadAllText($bundle)
$secure=$encrypted | ConvertTo-SecureString
$plain=[Net.NetworkCredential]::new('', $secure).Password
try {
    $values=$plain | ConvertFrom-Json
    if ($values.version -ne 1) {throw 'Unsupported Steam credential bundle version.'}
    gh api -X PUT "repos/$Repository/environments/$Environment" --input NUL *> $null
    if ($LASTEXITCODE) {throw 'Failed to create or update the GitHub deployment environment.'}
    foreach ($entry in ([ordered]@{
        STEAM_USERNAME=$values.username
        STEAM_PASSWORD=$values.password
        STEAM_REFRESH_TOKEN=$values.refreshToken
        STEAM_CONFIG_VDF_BASE64=$values.configVdfBase64
    }).GetEnumerator()) {
        $entry.Value | gh secret set $entry.Key --repo $Repository --env $Environment
        if ($LASTEXITCODE) {throw "Failed to set GitHub environment secret: $($entry.Key)"}
    }
    gh variable set STEAM_PUBLISH_ENABLED --body false --repo $Repository
    if ($LASTEXITCODE) {throw 'Failed to disable the repository Steam publishing gate.'}
    Write-Host "PASS: reusable Steam credentials installed for $Repository; publishing remains disabled." -ForegroundColor Green
} finally {
    $plain=$null;$values=$null;$secure=$null
}
