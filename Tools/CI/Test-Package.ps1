param([Parameter(Mandatory=$true)][string]$ArtifactRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$root=(Resolve-Path -LiteralPath $ArtifactRoot).Path
$publisher=Join-Path $PSScriptRoot 'PublishWorkshop.ps1'
$testRoot=Join-Path ([IO.Path]::GetTempPath()) ('smy-package-test-'+[guid]::NewGuid().ToString('N'))
Copy-Item -LiteralPath $root -Destination $testRoot -Recurse
try {
    $manifestPath=Join-Path $testRoot 'manifest.json';$original=Get-Content $manifestPath -Raw;$manifest=$original|ConvertFrom-Json
    $stage=Join-Path $testRoot "SimpleMendingYourself-$($manifest.version)"
    function Assert-Rejected([string]$expected) {
        $rejected=$false;try {& $publisher -ArtifactRoot $testRoot -DryRun}catch{if ($_.Exception.Message -notlike "*$expected*"){throw};$rejected=$true}
        if (!$rejected){throw "Expected rejection: $expected"}
    }
    & $publisher -ArtifactRoot $testRoot -DryRun
    $manifest.publishedfileid='0';$manifest|ConvertTo-Json -Depth 5|Set-Content $manifestPath;Assert-Rejected 'existing Simple Mending Yourself'
    [IO.File]::WriteAllText($manifestPath,$original,[Text.UTF8Encoding]::new($false));$manifest=$original|ConvertFrom-Json
    $manifest.tag='v999.0.0';$manifest|ConvertTo-Json -Depth 5|Set-Content $manifestPath;Assert-Rejected 'explicit release tag'
    [IO.File]::WriteAllText($manifestPath,$original,[Text.UTF8Encoding]::new($false));$manifest=$original|ConvertFrom-Json
    $manifest.files[0].path='../outside.txt';$manifest|ConvertTo-Json -Depth 5|Set-Content $manifestPath;Assert-Rejected 'Invalid or duplicate'
    [IO.File]::WriteAllText($manifestPath,$original,[Text.UTF8Encoding]::new($false))
    $license=Join-Path $stage 'LICENSE';$bytes=[IO.File]::ReadAllBytes($license);Add-Content $license 'tampered';Assert-Rejected 'Content checksum mismatch';[IO.File]::WriteAllBytes($license,$bytes)
    Set-Content (Join-Path $stage 'unexpected.txt') 'unexpected';Assert-Rejected 'Unexpected files';Remove-Item (Join-Path $stage 'unexpected.txt')
    $notes=Join-Path $testRoot 'release-notes.en.md';[IO.File]::WriteAllText($notes,"引号 `"quote`" / path C:\test`nEnglish update",[Text.UTF8Encoding]::new($false))
    $manifest=$original|ConvertFrom-Json;$manifest.localizedNotes.en=(Get-FileHash $notes).Hash;$manifest|ConvertTo-Json -Depth 5|Set-Content $manifestPath
    & $publisher -ArtifactRoot $testRoot -DryRun
    $vdf=Get-Content (Join-Path $testRoot 'workshop-upload.vdf') -Raw
    if ($vdf -notmatch '\\"quote\\"' -or $vdf -notmatch 'C:\\\\test' -or $vdf -notmatch "`nEnglish") {throw 'VDF escaping failed.'}
    if ($vdf -match '"(title|description|visibility|tags|previewfile)"') {throw 'Unexpected Workshop metadata update.'}
    Write-Host 'PASS: invalid identity, tag, path, modified/extra files and metadata preservation.'
} finally {
    if ($testRoot.StartsWith([IO.Path]::GetTempPath(),[StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($testRoot) -match '^smy-package-test-[0-9a-f]{32}$') {Remove-Item -LiteralPath $testRoot -Recurse -Force}
}
