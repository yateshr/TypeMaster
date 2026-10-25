# TypeMaster v1.11.11 — Faster typing input

This release shortens the synchronous typing-input path while preserving the approved UI layout and final scoring rules.

- Queued input takes priority over passage painting. Repeated text/caret events share one latest-state display update.
- Passage scrolling follows normal WPF layout; typing no longer forces passage layout.
- Empty error-feedback overlays skip line-geometry calculations and redundant idle redraws.
- The isolated benchmark now separates synchronous input work from subsequent dispatcher/redraw work.

**Local measurements:** average synchronous input handling fell from about **8–12 ms to 0.6–1.1 ms**. These are not physical key-to-screen timings: subsequent screen redraw work still averages approximately 21–27 ms in this run and varies with machine load. Every measured character was preserved. [Performance details](https://github.com/yateshr/TypeMaster/blob/main/docs/PERFORMANCE.md).

**Validation:** 578 native checks passed, including a 600-character input burst coalesced into one latest-state passage paint, exact response/scoring preservation, number scrolling, corrections and recovery. Existing PDF extraction, selection and pagination checks passed. Installer compilation passed; a complete manual installation/upgrade was not exercised in this run.

## Downloads

- **Setup.exe:** install/update for the current Windows account.
- **win-x64.zip:** extract completely and run `native-win-x64-1.11.11/TypeMaster.exe` without installing.
- **SHA256SUMS.txt:** verify both downloads.

Windows 10/11 x64; .NET runtime included. Back up profiles and close TypeMaster and the Typing Meter before manually updating. The installer is unsigned.

Developed by **YATESH ROHIT**. [Project](https://github.com/yateshr/TypeMaster) · [Releases](https://github.com/yateshr/TypeMaster/releases)
