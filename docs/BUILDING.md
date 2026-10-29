# Build and verify TypeMaster

## Prerequisites

- Windows x64 with a .NET 10 SDK supporting WPF.
- Git to clone the repository.
- NSIS only if you need an installer; `makensis.exe` can be supplied explicitly.
- Python with `pypdf`, `pdfplumber` and `pypdfium2` for independent PDF verification.

## Compile or run

```powershell
dotnet build Native/TypeMaster.Native.csproj -c Release
dotnet run --project Native/TypeMaster.Native.csproj -c Release
```

The `Asset` and `UI` directories are build inputs. Preserve their paths and filenames.

## Full release build

```powershell
& '.\Build Native.ps1'
# Optional explicit NSIS location:
& '.\Build Native.ps1' -NsisPath 'C:\Tools\NSIS\makensis.exe'
```

The script publishes `dist/native-win-x64-1.11.12`, runs the application's checks, copies demonstration reports and creates a ZIP. It builds the NSIS installer when NSIS is available. An installer build is not implied if NSIS is absent.

## Native checks

The published application supports an isolated self-test run:

```powershell
$testRun = Start-Process '.\dist\native-win-x64-1.11.12\TypeMaster.exe' -ArgumentList '--self-test' -PassThru -Wait
$testRun.ExitCode
Get-Content "$env:TEMP\TypeMasterNative-tests\results.json"
```

Run in an interactive Windows desktop session. Tests cover scoring, persistence, recovery, report generation, assessment editing, display layout and native screen rendering. Test profiles and outputs use the temporary test directory rather than live candidate storage.

## PDF verification

```powershell
python -m pip install pypdf pdfplumber pypdfium2
python Native/Tools/VerifyPdfReports.py "$env:TEMP\TypeMasterNative-tests\reports"
```

The build script automatically runs this verifier when its configured bundled Python runtime is present. Otherwise run it explicitly with the command above. It checks parsing, extracted text, company headers and PDF-viewer word hit areas. Render and visually inspect PDFs after report layout changes; successful text extraction alone does not establish visual correctness.

## Release checksums

```powershell
Get-FileHash dist/TypeMaster-Native-1.11.12-Setup.exe -Algorithm SHA256
Get-FileHash dist/TypeMaster-Native-1.11.12-win-x64.zip -Algorithm SHA256
```

Candidate data, backups, logs and local branding settings must not be committed or packaged.
