Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "StrFunc.nsh"
${UnStrStr}
Name "TypeMaster"
OutFile "..\dist\TypeMaster-Native-1.10.2-Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\TypeMaster Native"
RequestExecutionLevel user
SetCompressor /SOLID lzma
Icon "TypeMaster.ico"
UninstallIcon "TypeMaster.ico"
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\TypeMaster.exe"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
Function .onInit
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
  File /r "..\dist\native-win-x64-1.10.2\*.*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\TypeMaster"
  CreateShortcut "$SMPROGRAMS\TypeMaster\TypeMaster.lnk" "$INSTDIR\TypeMaster.exe"
  CreateShortcut "$DESKTOP\TypeMaster.lnk" "$INSTDIR\TypeMaster.exe"
  CreateShortcut "$SMPROGRAMS\TypeMaster\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "DisplayName" "TypeMaster"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "DisplayVersion" "1.10.2"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative" "DisplayIcon" "$INSTDIR\TypeMaster.exe"
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
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  Delete "$DESKTOP\TypeMaster.lnk"
  Delete "$SMPROGRAMS\TypeMaster\TypeMaster.lnk"
  Delete "$SMPROGRAMS\TypeMaster\Uninstall.lnk"
  RMDir "$SMPROGRAMS\TypeMaster"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\TypeMasterNative"
SectionEnd














