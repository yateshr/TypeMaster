# TypeMaster v1.11.9

TypeMaster is a native Windows typing-practice application customised for company computer assessments and candidate reports. Developed by **YATESH ROHIT**.

## What's new since v1.10.2

- Original TypeMaster artwork: 110 editable SVG illustrations and matching PNG exports, four photo/scene sources, logo assets and native runtime integration.
- Approved progress-report layout: aligned company masthead and divider, candidate identity panel, compact training information and original typing-result metrics.
- Three-column application skills, full-width single-skill rows, compact continuation headers, remarks after all skills and examiner details on every page.
- Selectable PDF text, embedded fonts and clearer borders/ticks for black-and-white printing.
- Clearer application rendering, explicit window/taskbar/shortcut icons, developer credit and build information.
- Branded installer pages, Getting Started information and GitHub project/release links.
- Optional daily update checks with SHA-256-verified installer downloads. Download and installation start only when the user chooses them. Profiles are saved before an update.
- Updated documentation, screenshots and demonstration PDF reports.

## Downloads

| File | Purpose |
|---|---|
| `TypeMaster-Native-1.11.9-Setup.exe` | Install for the current Windows account |
| `TypeMaster-Native-1.11.9-win-x64.zip` | Extract completely and run `native-win-x64-1.11.9/TypeMaster.exe` without installation |
| `TypeMaster-Artwork-1.11.9.zip` | Individual original artwork and logo files, with editable SVG sources |
| `SHA256SUMS.txt` | SHA-256 hashes for these downloads |

Windows 10/11 x64. The .NET runtime is included. The installer is unsigned and Windows may display a publisher warning.

**Updating from 1.10.2:** download and run the new installer manually; the earlier release does not contain the new update checker. Portable users should extract the new ZIP into a separate folder and keep its files together. Close TypeMaster and the Typing Meter before replacing files. Candidate profiles are stored separately; use Profile Backup & Restore before upgrading.

## Validation

568 native application checks passed, plus five PDF extraction/selection checks and short/long four-attempt report pagination checks. Installer compilation passed; a full manual installation/upgrade was not exercised in this validation run.

Office skills are examiner-entered observations; TypeMaster does not automatically execute or grade Excel, Word or PowerPoint tasks.

[Source and documentation](https://github.com/yateshr/TypeMaster) · [Screenshot gallery](https://github.com/yateshr/TypeMaster/blob/main/docs/SCREENSHOTS.md) · [Sample progress PDF](https://github.com/yateshr/TypeMaster/blob/main/docs/reports/complete-progress-report.pdf)
