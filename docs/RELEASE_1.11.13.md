# TypeMaster v1.11.13 — Exit confirmation and startup update checks

- Ask before exiting TypeMaster through Esc at the welcome screen, the window Close button or tray-menu Exit. No is the default; declining keeps the application open.
- Confirmed exit uses normal cleanup and saves an unfinished typing exercise for recovery. The already-confirmed update installation avoids a duplicate exit prompt.
- Check for a newer stable release on every launch when automatic checks are enabled, including reopening on the same day.
- Stay silent at startup when up to date or offline. Declining an available update skips the notification for that launch; reopening checks again.
- Preserve existing automatic-update preferences and keep download and installation under user control.

Validation: 855 native checks passed before packaging. Release packaging reruns native checks and PDF text-selection and pagination verification. Full manual installation/upgrade is not exercised by these automated checks.

Download the Windows installer, or extract the complete portable ZIP and open native-win-x64-1.11.13/TypeMaster.exe. Close the running application before updating. Existing candidate data stays in the per-user application-data directory.
