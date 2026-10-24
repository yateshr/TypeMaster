# FAQ and troubleshooting

## Can I test without installing?

Yes. Download the portable ZIP from Releases, extract every file and launch `TypeMaster.exe` inside its versioned folder. Running the EXE directly from the ZIP or copying the EXE alone can leave required assets unavailable.

## Do I need Microsoft Office?

Typing tests and reports do not require Office. To observe a candidate using real Excel, Word or PowerPoint, provide those applications separately. The examiner records the demonstrated skills in TypeMaster.

## Where is candidate data stored?

Under `%LOCALAPPDATA%\TypeMasterNative` for the current Windows account, including when using the portable application. Use profile backup controls before changing computers or Windows accounts.

## Windows displays a publisher warning

The current installer is unsigned. Obtain it from this repository's Releases page and compare its SHA-256 hash with `SHA256SUMS.txt` before deciding whether to run it.

## The installer says TypeMaster is still running

Close the main application and exit Typing Meter from its tray icon, then retry. The installer checks the running-instance mutex.

## Can I copy text from the PDF?

Yes. Current exports contain selectable text and embedded fonts. Use a text-capable PDF viewer. Report mistakes, expected text and affected viewer versions if selection fails.

## Some old reports have no word-by-word corrections

An examination sheet needs the original and typed text snapshots. Historical records created before snapshot storage can retain score metrics without enough data to reconstruct typed words.

## How do I report a problem?

Use the bug-report template. Include the app version, Windows version, display resolution/scaling, affected screen, steps and expected result. Redact candidate names, interview remarks and company-sensitive information from attachments.
