# Release notes

## 1.11.12 — 2026-10-29

- Recover text alignment after inserted or omitted characters, avoiding cascading error penalties. The existing five-stroke penalty arithmetic remains; new attempts identify the aligned comparison policy. Historical candidate records are preserved.
- Audit native input, session metrics and disk reload for all 128 bundled samples (up to 120 characters per sample).
- Keep passage wrapping anchored and advance by visual lines rather than shifting at each word.
- Lock completed test words after Space or Enter. The unfinished word stays editable; previous words cannot be changed through Backspace, Delete, cut or selection replacement. Recovery restores the lock.
- Move the typing instruction below the input area with a clear gap.
- Esc follows the current screen's close/cancel action, preserving test cancellation confirmation, and closes secondary windows.
- Preserve extra typed characters and prevent premature completion after insertions. Zero-net results cannot pass with unspecified minimums.

Validation: 849 native checks passed before release packaging. The release build reruns native and PDF extraction, selection and pagination checks. Installer compilation is checked; a complete manual installation/upgrade is not exercised by automated checks.

## 1.11.11 — 2026-10-25

- Prioritised queued typing input over passage painting; bursts of input share one latest-state display update.
- Removed forced layout from passage painting and deferred scrolling until after normal WPF layout.
- Avoided TextBox geometry queries for empty feedback overlays and redundant idle overlay redraws.
- Added separate input, passage and feedback timings to the isolated benchmark.
- Local measured input handling averaged 0.6–1.1 ms versus 8–12 ms in the previous profiled build; these timings exclude subsequent screen redraw work.
- All 578 native checks passed, including a 600-character burst that preserves the exact response and coalesces to one passage paint. UI layout and final scoring rules remain unchanged.

## 1.11.10 — 2026-10-25

- Fixed severe typing-test input delays caused by synchronous recovery writes and repeated WPF text/layout rebuilding.
- Coalesced recovery snapshots on one background worker, preserving explicit saves and preventing cleared drafts from reappearing.
- Grouped passage text by formatting instead of creating a separate Run for every character, and suppressed duplicate caret redraws.
- Moved live metrics and bounded word feedback to the exercise timer; final scoring and report comparisons remain unchanged.
- Reused the error sound rather than reopening its file on each mistake.
- Added rapid-input, slow-storage, snapshot ordering and cancellation regression checks, plus an isolated typing benchmark.
- Measured average input latency decreased from 446–485 ms to 13–17 ms in the local benchmark; all 360 measured characters were preserved. See [performance validation](docs/PERFORMANCE.md).

## 1.11.9 — 2026-10-25

- Includes all artwork, report and installer refinements developed in the 1.11.0–1.11.8 previews below.
- Validated with 568 native checks and selectable-text PDF pagination checks.
- Explained typing-practice and customised computer-assessment workflows in setup, About and Getting Started.
- Added GitHub project/release links in setup, Start menu and the application.
- Added optional daily update checks and SHA-256-verified installer downloads with user-controlled installation.
- Installer waits for the running application to save and exit; candidate records remain separate.


## 1.11.8 — 2026-10-25 (local preview)

- Applied the approved white company masthead, divider and candidate identity panel.
- Added compact continuation headers and repeated application headings.
- Darkened print borders and ticks, with full-width rows for single skills.


## 1.11.7 — 2026-10-25 (local preview)

- Made training information compact with aligned labels and values on the same row.
- Moved Typing Tests upward and removed the extra company/candidate header dividers.
- Preserved three-column skills, continuation headings, final remarks and examiner details on every page.


## 1.11.6 — 2026-10-25 (local preview)

- Applied the approved three-column skills report and removed the unused assessment label.
- Separated workspace shadows from content rendering for clearer text and stronger menu contrast.
- Added branded installer pages, explicit window/shortcut icons, publisher metadata and a Getting Started guide.


## 1.11.5 — 2026-10-25 (local preview)

- Refined candidate information panel and numeric report dates.
- Added candidate/section context on continuation pages and examiner footer on every page.
- Preserved original typing metrics and skills order, with remarks after all skills.


## 1.11.4 — 2026-10-25 (local preview)

- Applied the approved original Typing Tests and Technical Skills styling.
- Retained the new candidate identity, ruled remarks and examiner/page footer.


## 1.11.3 — 2026-10-25 (local preview)

- Unified progress report typography and aligned typing results into labelled columns.
- Balanced skills checklists with aligned wrapped labels.
- Added a ruled remarks section that flows across pages and a consistent examiner/page footer.

## 1.11.2 — 2026-10-25 (local preview)

- Added a compact candidate identity section to progress reports: labelled name, entered casing, latest typing assessment date and a divider.
- Preserved company branding, typing results, selected skills, remarks and examiner footer.

## 1.11.1 — 2026-10-25 (local preview)

- Added prominent developer credit: YATESH ROHIT.
- About and build information read the version, build ID and developer from compiled application metadata.
- Preserved profile services and report access in the About layout.

## 1.11.0 — 2026-10-25 (local artwork preview)

- 92 original matching-size legacy illustration replacements and 18 standalone controls/game illustrations.
- Original photo/scene replacements with the original frame sizes.
- Removed runtime screenshot cropping from navigation, hands, game selector, close controls, settings keyboard and meter introduction.
- Shared finger coordinates keep introduction and drill markers aligned with the drawn hands.
- Preserved company branding and existing typing/report behavior.
- Corrected the editable TypeMaster wordmark's exported SVG path.
- Added artwork generation documentation and native image-dimension checks.

## 1.10.2 — 2026-10-24

First GitHub release of the native Windows application.

- C# / WPF desktop application with local candidate profiles.
- Typing lessons, text and number tests, custom review and six practice games.
- Saved speed and accuracy statistics and optional Typing Meter.
- Skills and remarks assessment with 521 Excel function names, practical skills and custom application assignments.
- Application-based report sections for Excel, Word, PowerPoint, Outlook and other skills.
- Compact typing-result history, company branding and bottom-left examiner footer.
- Selectable-text PDF results, certificates, examination sheets and complete progress reports.
- Profile backup/restore, save recovery and interrupted-exercise recovery.
- Native caption controls and display-layout fixes.
- Removed licensing controls, labels and legacy welcome padlock.
- Branded repository visuals, actual application screenshots, sample reports and user/build documentation.

Validation: 341 native checks passed; five PDF samples passed text and word-hit-area verification. Installer and portable ZIP are self-contained Windows x64 builds.
