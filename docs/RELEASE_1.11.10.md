# TypeMaster v1.11.10 — Typing responsiveness fix

This release fixes severe input delays during typing tests while preserving the approved UI and PDF design.

- Removed synchronous recovery disk writes from individual keystrokes. Recovery now uses a coalescing background worker with atomic file replacement.
- Reduced passage rendering from hundreds of individual character objects to grouped text runs, and removed duplicate caret redraws.
- Moved live metrics and bounded word feedback to the exercise timer. Final scoring and report comparison rules remain unchanged.
- Reused the error sound instead of reopening it on each mistake.
- Added rapid-input, slow-storage, recovery ordering and cancellation regressions and an isolated typing benchmark.

Local benchmark averages improved from **446–485 ms to 13–31 ms per character across two local runs**, with all measured characters preserved. These are local measurements, not guarantees for every computer. [Validation details](https://github.com/yateshr/TypeMaster/blob/main/docs/PERFORMANCE.md).

**Validation:** 576 native checks, selectable-text PDF checks and short/long report pagination checks passed. Installer compilation passed; a full manual installation/upgrade was not exercised in this run.

## Downloads

- **Setup.exe:** install or update the Windows application.
- **win-x64.zip:** extract completely and run `native-win-x64-1.11.10/TypeMaster.exe` without installation.
- **SHA256SUMS.txt:** verify both downloads.

Windows 10/11 x64; .NET runtime included. Close TypeMaster and Typing Meter before manually replacing portable files. Back up profiles before updating. The installer is unsigned.

If a candidate completed a test while the application was visibly stalling, review that result and retest them using this version.

Developed by **YATESH ROHIT**. [Project](https://github.com/yateshr/TypeMaster) · [Releases](https://github.com/yateshr/TypeMaster/releases)
