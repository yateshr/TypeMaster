# TypeMaster v1.11.12 — Reliable typing tests and completed-word locking

- Recover text alignment after inserted or omitted characters, avoiding cascading error penalties. The existing five-stroke penalty arithmetic remains; new attempts identify the aligned comparison policy. Historical candidate records are preserved.
- Audit native input, session metrics and disk reload for all 128 bundled samples (up to 120 characters per sample).
- Keep passage wrapping anchored and advance by visual lines rather than shifting at each word.
- Lock completed test words after Space or Enter. The unfinished word stays editable; previous words cannot be changed through Backspace, Delete, cut or selection replacement. Recovery restores the lock.
- Move the typing instruction below the input area with a clear gap.
- Esc follows the current screen's close/cancel action, preserving test cancellation confirmation, and closes secondary windows.
- Preserve extra typed characters and prevent premature completion after insertions. Zero-net results cannot pass with unspecified minimums.

Validation: 849 native checks passed before release packaging. The release build reruns native and PDF extraction, selection and pagination checks. Installer compilation is checked; a complete manual installation/upgrade is not exercised by automated checks.

Download Setup.exe to install, or extract the complete win-x64.zip and open native-win-x64-1.11.12/TypeMaster.exe. Close TypeMaster and the Typing Meter tray icon before updating. Profiles remain in the existing per-user data folder.
