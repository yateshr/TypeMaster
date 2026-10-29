Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "StrFunc.nsh"
!include "FileFunc.nsh"
!include "nsDialogs.nsh"
${UnStrStr}
Name "TypeMaster"
VIProductVersion "1.11.13.0"
VIFileVersion "1.11.13.0"
VIAddVersionKey /LANG=1033 "ProductName" "TypeMaster"
VIAddVersionKey /LANG=1033 "CompanyName" "YATESH ROHIT"
VIAddVersionKey /LANG=1033 "LegalCopyright" "YATESH ROHIT"
VIAddVersionKey /LANG=1033 "FileDescription" "TypeMaster Setup"
VIAddVersionKey /LANG=1033 "FileVersion" "1.11.13"
VIAddVersionKey /LANG=1033 "ProductVersion" "1.11.13"
OutFile "..\dist\TypeMaster-Native-1.11.13-Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\TypeMaster Native"
RequestExecutionLevel user
SetCompressor /SOLID lzma
Icon "TypeMaster.ico"
UninstallIcon "TypeMaster.ico"
!define MUI_ICON "TypeMaster.ico"
!define MUI_UNICON "TypeMaster.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "InstallerAssets\header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "InstallerAssets\welcome.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "InstallerAssets\welcome.bmp"
!define MUI_WELCOMEPAGE_TITLE "Welcome to TypeMaster"
!define MUI_WELCOMEPAGE_TEXT "TypeMaster 1.11.13$\r$\nDeveloped by YATESH ROHIT$\r$\n$\r$\nA typing-practice application customised for computer assessments and candidate PDF reports.$\r$\n$\r$\nSetup installs TypeMaster for your Windows account and creates desktop and Start menu shortcuts.$\r$\n$\r$\nClick Next to continue."
!define MUI_DIRECTORYPAGE_TEXT_TOP "Choose where to install TypeMaster. Existing candidate records are stored separately and are preserved when updating."
!define MUI_FINISHPAGE_TITLE "TypeMaster is ready"
!define MUI_FINISHPAGE_TEXT "TypeMaster 1.11.13 is installed.$\r$\nDeveloped by YATESH ROHIT$\r$\n$\r$\nPractise typing or assess a candidate. Use Statistics > Progress Report for skills and PDF reports.$\r$\n$\r$\nOpen About > Updates & GitHub for updates and project links."
!define MUI_FINISHPAGE_RUN_TEXT "Launch TypeMaster"
!define MUI_FINISHPAGE_LINK "Visit GitHub releases and downloads"
!define MUI_FINISHPAGE_LINK_LOCATION "https://github.com/yateshr/TypeMaster/releases"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\Getting Started.txt"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Open the Getting Started guide"
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
BrandingText "TypeMaster | Developed by YATESH ROHIT"
ShowInstDetails show
ShowUninstDetails show
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\TypeMaster.exe"
!insertmacro MUI_PAGE_WELCOME
Page custom TypeMasterInformation
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
Function TypeMasterInformation
  !insertmacro MUI_HEADER_TEXT "About TypeMaster" "Typing practice customised for computer assessments"
  nsDialogs::Create 1018
  Pop $0
  ${NSD_CreateLabel} 0 0 100% 43u "TypeMaster is a typing-practice application customised for our computer-assessment workflow. Use it to practise typing, test candidates, record technical skills and create individual PDF reports."
  Pop $0
  ${NSD_CreateLabel} 0 47u 100% 34u "Technical skills and remarks are entered by the examiner. Complete a typing test, then open Statistics > Progress Report to prepare the candidate report."
  Pop $0
  ${NSD_CreateLink} 0 88u 100% 14u "Project: https://github.com/yateshr/TypeMaster"
  Pop $0
  ${NSD_OnClick} $0 OpenTypeMasterProject
  ${NSD_CreateLink} 0 106u 100% 14u "Releases: https://github.com/yateshr/TypeMaster/releases"
  Pop $0
  ${NSD_OnClick} $0 OpenTypeMasterReleases
  ${NSD_CreateLabel} 0 133u 100% 28u "Developed by YATESH ROHIT. Update checks and GitHub links are available in About > Updates & GitHub after installation."
  Pop $0
  nsDialogs::Show
FunctionEnd
Function OpenTypeMasterProject
  Pop $0
  ExecShell "open" "https://github.com/yateshr/TypeMaster"
FunctionEnd
Function OpenTypeMasterReleases
  Pop $0
  ExecShell "open" "https://github.com/yateshr/TypeMaster/releases"
FunctionEnd
Function .onInit
  ; Wait for the updating process to save and exit before the usual mutex check.
  ${GetParameters} $R0
  ClearErrors
  ${GetOptions} $R0 "/UPDATEPID=" $R1
  IfErrors update_wait_done
  IntOp $R1 $R1 + 0
  IntCmp $R1 0 update_wait_done
  System::Call 'kernel32::OpenProcess(i 0x100000, i 0, i $R1) p.r2'
  StrCmp $2 "0" update_wait_done
  System::Call 'kernel32::WaitForSingleObject(p r2, i 30000) i.r3'
  System::Call 'kernel32::CloseHandle(p r2)'
  update_wait_done:
  ClearErrors
  install_running_check:
  System::Call 'kernel32::OpenMutexW(i 0x100000, i 0, w "Local\TypeMasterNative") p.r0'
  StrCmp $0 "0" install_ready
  System::Call 'kernel32::CloseHandle(p r0)'
  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "Close TypeMaster before installing the update. If Typing Meter is running, exit it from its tray icon too. Then click Retry." IDRETRY install_running_check
  Abort
  install_ready:
FunctionEnd
Function un.onInit
  uninstall_running_check:
  System::Call 'kernel32::OpenMutexW(i 0x100000, i 0, w "Local\TypeMasterNative") p.r0'
  StrCmp $0 "0" uninstall_ready
  System::Call 'kernel32::CloseHandle(p r0)'
  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "Close TypeMaster and its Typing Meter tray icon before uninstalling. Then click Retry." IDRETRY uninstall_running_check
  Abort
  uninstall_ready:
FunctionEnd
Section "Install"
  SetShellVarContext current
  SetOutPath "$INSTDIR"
  DetailPrint "Installing TypeMaster 1.11.13 and application artwork..."
  File /r "..\dist\native-win-x64-1.11.13\*.*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  DetailPrint "Creating TypeMaster shortcuts and application information..."
  CreateDirectory "$SMPROGRAMS\TypeMaster"
  CreateShortcut "$SMPROGRAMS\TypeMaster\TypeMaster.lnk" "$INSTDIR\TypeMaster.exe" "" "$INSTDIR\TypeMaster.ico" 0
  CreateShortcut "$DESKTOP\TypeMaster.lnk" "$INSTDIR\TypeMaster.exe" "" "$INSTDIR\TypeMaster.ico" 0
  CreateShortcut "$SMPROGRAMS\TypeMaster\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\TypeMaster.ico" 0
  CreateShortcut "$SMPROGRAMS\TypeMaster\Getting Started.lnk" "$INSTDIR\Getting Started.txt"
  WriteINIStr "$INSTDIR\TypeMaster Project.url" "InternetShortcut" "URL" "https://github.com/yateshr/TypeMaster"
  WriteINIStr "$INSTDIR\TypeMaster Releases.url" "InternetShortcut" "URL" "https://github.com/yateshr/TypeMaster/releases"
  CreateShortcut "$SMPROGRAMS\TypeMaster\GitHub Project.lnk" "$INSTDIR\TypeMaster Project.url"
  CreateShortcut "$SMPROGRAMS\TypeMaster\Releases and Downloads.lnk" "$INSTDIR\TypeMaster Releases.url"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "DisplayName" "TypeMaster"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "DisplayVersion" "1.11.13"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "DisplayIcon" '$\"$INSTDIR\TypeMaster.ico$\",0'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "Publisher" "YATESH ROHIT"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "URLInfoAbout" "https://github.com/yateshr/TypeMaster"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "HelpLink" "https://github.com/yateshr/TypeMaster/issues"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "NoRepair" 1
SectionEnd
Section "Uninstall"
  SetShellVarContext current
  ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "TypeMasterNativeMeter"
  ${UnStrStr} $1 $0 "$INSTDIR\"
  ${If} $1 != ""
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "TypeMasterNativeMeter"
  ${EndIf}
  !include "uninstall-files.nsh"
  Delete "$INSTDIR\TypeMaster Project.url"
  Delete "$INSTDIR\TypeMaster Releases.url"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  Delete "$DESKTOP\TypeMaster.lnk"
  Delete "$SMPROGRAMS\TypeMaster\TypeMaster.lnk"
  Delete "$SMPROGRAMS\TypeMaster\Uninstall.lnk"
  Delete "$SMPROGRAMS\TypeMaster\Getting Started.lnk"
  Delete "$SMPROGRAMS\TypeMaster\GitHub Project.lnk"
  Delete "$SMPROGRAMS\TypeMaster\Releases and Downloads.lnk"
  RMDir "$SMPROGRAMS\TypeMaster"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative"
SectionEnd














