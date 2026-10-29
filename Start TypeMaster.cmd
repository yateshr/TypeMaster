@echo off
cd /d "%~dp0"
if exist "dist\native-win-x64-1.11.12\TypeMaster.exe" (
  start "" "%~dp0dist\native-win-x64-1.11.12\TypeMaster.exe"
  exit /b
)
dotnet run --project Native\TypeMaster.Native.csproj -c Release
if errorlevel 1 pause








