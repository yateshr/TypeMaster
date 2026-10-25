param([string]$NsisPath = '')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
dotnet publish Native\TypeMaster.Native.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Native publish failed.' }
$publishPath = Join-Path $PSScriptRoot 'Native\bin\Release\net10.0-windows\win-x64\publish'
$releasePath = Join-Path $PSScriptRoot 'dist\native-win-x64-1.11.10'
New-Item -ItemType Directory -Path $releasePath -Force | Out-Null
Get-ChildItem -LiteralPath $publishPath | Copy-Item -Destination $releasePath -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Native\README.md') -Destination (Join-Path $releasePath 'README.md') -Force
$testProcess = Start-Process -FilePath (Join-Path $releasePath 'TypeMaster.exe') -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
if ($testProcess.ExitCode -ne 0) { throw 'Native application checks failed. See %TEMP%\TypeMasterNative-tests\results.json.' }
$pdfCheckPython = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
if (Test-Path -LiteralPath $pdfCheckPython) {
    & $pdfCheckPython (Join-Path $PSScriptRoot 'Native\Tools\VerifyPdfReports.py') (Join-Path $env:TEMP 'TypeMasterNative-tests\reports')
    if ($LASTEXITCODE -ne 0) { throw 'PDF text extraction or viewer selection checks failed.' }
    & $pdfCheckPython (Join-Path $PSScriptRoot 'Native\Tools\VerifyCandidateReports.py') (Join-Path $env:TEMP 'TypeMasterNative-tests\candidate-samples')
    if ($LASTEXITCODE -ne 0) { throw 'Candidate report pagination checks failed.' }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Native\UI_MATCH.md'),(Join-Path $PSScriptRoot 'Native\REPORTS.md'),(Join-Path $PSScriptRoot 'Native\SOURCE_GAPS.md') -Destination $releasePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE'),(Join-Path $PSScriptRoot 'THIRD_PARTY_NOTICES.md'),(Join-Path $PSScriptRoot 'README.md') -Destination $releasePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs') -Destination $releasePath -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Native\RuntimeNotices') -Destination $releasePath -Recurse -Force
$reportSamples = Join-Path $releasePath 'docs\reports'
New-Item -ItemType Directory -Path $reportSamples -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $env:TEMP 'TypeMasterNative-tests\reports') -Filter '*.pdf' | Copy-Item -Destination $reportSamples -Force
$zip = Join-Path $PSScriptRoot 'dist\TypeMaster-Native-1.11.10-win-x64.zip'
Compress-Archive -LiteralPath $releasePath -DestinationPath $zip -Force
if (-not $NsisPath) {
    $nsisCommand = Get-Command makensis.exe -ErrorAction SilentlyContinue
    if ($nsisCommand) { $NsisPath = $nsisCommand.Source }
    else {
        $nsisCache = Join-Path $env:LOCALAPPDATA 'electron-builder\Cache'
        if (Test-Path -LiteralPath $nsisCache) { $NsisPath = Get-ChildItem -LiteralPath $nsisCache -Directory -Filter 'nsis*' | Get-ChildItem -Recurse -File -Filter makensis.exe | Select-Object -First 1 -ExpandProperty FullName }
    }
}
if ($NsisPath) {
    $manifest = [System.Collections.Generic.List[string]]::new()
    Get-ChildItem -LiteralPath $releasePath -File -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Substring($releasePath.Length + 1).Replace('$','$$')
        $manifest.Add('Delete "$INSTDIR\' + $relativePath + '"')
    }
    Get-ChildItem -LiteralPath $releasePath -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
        $manifest.Add('RMDir "$INSTDIR\' + $_.FullName.Substring($releasePath.Length + 1).Replace('$','$$') + '"')
    }
    $manifest | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Native\uninstall-files.nsh') -Encoding utf8
    & $NsisPath /V2 (Join-Path $PSScriptRoot 'Native\Installer.nsi')
    if ($LASTEXITCODE -ne 0) { throw 'Native installer build failed.' }
    Write-Output 'Installer: dist\TypeMaster-Native-1.11.10-Setup.exe'
}
Write-Output "Native application: $releasePath\TypeMaster.exe"
Write-Output "Distribution archive: $zip"
$packageFiles = @($zip)
$installerFile = Join-Path $PSScriptRoot 'dist\TypeMaster-Native-1.11.10-Setup.exe'
if (Test-Path -LiteralPath $installerFile) { $packageFiles += $installerFile }
$checksumLines = foreach ($packageFile in $packageFiles) {
    $packageHash = Get-FileHash -LiteralPath $packageFile -Algorithm SHA256
    $packageHash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($packageFile)
}
$checksumLines | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'dist\SHA256SUMS.txt') -Encoding ascii













