# Typing input performance — 1.11.10

## Cause

Typing previously performed a durable JSON recovery write (`Flush(true)` plus atomic replacement) on every text change, on the WPF UI thread. The target passage also recreated up to 550 individual text runs and forced layout, sometimes twice because a caret event painted the same input again. Word feedback could allocate a quadratic word-alignment matrix while the candidate was typing.

## Correction

- Keystrokes update the recorded response and error counts immediately; they do not write recovery files or calculate complete live scores.
- The timer refreshes metrics and bounded live word feedback. Final scoring and report word alignment retain the existing rules.
- Adjacent passage characters with identical formatting share a text run; duplicate caret paints are skipped.
- Recovery snapshots are captured approximately once a second and written by a single background worker. Only the newest pending snapshot is retained. Explicit recovery saves on close remain durable. Clearing/completing an attempt invalidates queued work so an old draft cannot reappear.
- The error sound is opened once and reused.

## Local measurement

The isolated `--typing-benchmark` drives the real WPF typing TextBox and drains dispatcher work after each character. Each row measures 120 characters at the stated existing response length, using the same 19,600-character passage. Measurements include input handling, layout and queued UI work on this computer; they are not a guarantee for every PC. The benchmark uses its own temporary demonstration profile.

| Existing characters | Before average | After average | Before p95 | After p95 | Input retained |
|---|---|---|---|---|---|
| 0 | 446.2 ms | 13.9 ms | 457.4 ms | 27.0 ms | All 120 characters |
| 2,000 | 464.3 ms | 16.6 ms | 527.5 ms | 31.1 ms | All 120 characters |
| 8,000 | 485.0 ms | 13.4 ms | 556.9 ms | 20.9 ms | All 120 characters |

Average UI-thread allocations fell from approximately 26–27 MB per measured character to 0.6–0.8 MB. Cold layout and other machine activity still produce individual outliers; this is why averages and p95 are shown.


A repeat using the packaged 1.11.10 executable while installer compression was running measured:

| Existing characters | Packaged average | Packaged p95 | Input retained |
|---|---|---|---|
| 0 | 31.3 ms | 46.9 ms | All 120 characters |
| 2,000 | 30.8 ms | 45.3 ms | All 120 characters |
| 8,000 | 28.8 ms | 46.6 ms | All 120 characters |

## Regression coverage

576 native checks include rapid input preservation and exact scoring, slow-storage isolation, bounded snapshot queues, latest-snapshot ordering, cleared-draft invalidation, recovery restart/caret preservation, corrections, numeric row spacing and scrolling. Existing PDF extraction, selection and pagination checks remain part of the native packaging workflow.

To repeat on Windows after building:

```powershell
& .\Native\bin\Release\net10.0-windows\TypeMaster.exe --typing-benchmark
Get-Content "$env:TEMP\TypeMaster-typing-performance\latest.json"
```

For a practical check, complete a text test and a number test at normal and fast typing speeds, including corrections. Compare the saved response with what was entered. Existing reports from a visibly stalled test should be reviewed and affected candidates retested.
