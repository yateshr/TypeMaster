# Reports and PDF export - version 1.3

Behavioral comparison note: the supplied score examples support the five-stroke penalty fixture. Whether the original also penalizes corrected keystrokes, and how it classifies omitted or inserted words, has not been established. The corrected-error policy described below is the current native implementation, not a claim of original-app parity. The original trial is expired, so additional live comparisons are unavailable.

Complete a typing test, then press Enter or Next on the completion message to open the result screen. The test is saved before that message is displayed. Closing the screen does not lose the result.

| Button | Generated document |
|---|---|
| View & Print Results | One selected attempt: pass status, candidate, test, start date, duration, speeds, accuracy, strokes, error penalty, and the text actually typed. Numeric line breaks are retained and paginate. |
| View & Print Certificate | Completion certificate for a completed typing test, with the same metrics and a blank signature line. Completion and passing are distinct: a certificate records the actual scores, including a completed test that did not meet its pass limits. Partial attempts and course drills cannot produce a test certificate. |
| View & Print Assessment Report | Examination sheet for the selected attempt: candidate details, attempted original passage, typed response, changed words in bold, and candidate/examiner signature lines. |
| Statistics > Progress Report | Complete report for the current profile: usage, current course, study time, course activity history, completion percentage, and all saved typing test summaries. |

All documents open in a native preview with page navigation, zoom, Save PDF, and Print. Saving a PDF writes a real PDF directly; no PDF printer or Python installation is needed. Printing uses the normal Windows print dialog. Export preserves laid-out WPF text as selectable Unicode glyphs with embedded fonts, and lines and shapes as vector graphics. Only original image assets are embedded as lossless images. Text remains sharp at high zoom and supports copying and searching.

New attempts retain the exact original passage and typed response, planned duration, start/end times, strokes, pass limits, and lesson identity in the existing local profile store. Reports use the selected saved record rather than the currently selected test text. Deleting a custom passage does not remove old result snapshots.

Older attempts remain readable, with their saved speed/accuracy/error metrics. Their original and typed text cannot be reconstructed when it was not recorded; the examination sheet clearly says that those fields are unavailable. No candidate response is invented.

Typing tests use the five-stroke error penalty shown in the supplied sample: gross strokes are the characters entered, error hits are recorded incorrect keystrokes multiplied by five, net strokes are gross strokes minus error hits (minimum zero), and accuracy is net strokes divided by gross strokes. Net WPM is net strokes divided by five and elapsed minutes. Corrected typing errors remain part of the error count. Courses and review drills retain their keystroke accuracy and correctly typed character speed. The supplied EXI metadata supplies accuracy and net-speed pass limits; tests without limits are judged by completion. Existing saved results are not recalculated.

The five PDFs in Report Samples use an isolated Sample Candidate profile. They demonstrate English and numeric results, certificate, examination sheet, and complete progress report. They contain demonstration data, not the current user's results.

Source files: ReportFactory.cs (templates and repeated page headers), NativePdf.cs (direct PDF writer), ReferenceReportWindow.cs (preview/print/export), Models.cs (saved snapshots and metrics), ReportSamples.cs and SelfTests.cs (verification examples).

Version 1.5.1: test gross WPM, net WPM and penalty-based accuracy drop fractional parts. Regression examples reproduce 158 strokes / 5 errors => 31 gross, 26 net, 84%; 133 / 2 => 26 gross, 24 net, 92%; 139 / 2 => 27 gross, 25 net, 92%. Old saved records are not recalculated. Full error classification remains unverified.

Version 1.6.0 uses only the original RATAN and KOLORR logos and Nirmal Poly Plast Private Limited in a clean white company header, separated by a vertical divider and a thin bottom rule. Revision and build are omitted. Page numbers remain in the footer, content starts below the header, and the header repeats on continuation pages.


Version 1.6.1: ordinary glyphs use their own Unicode mappings without run-wide ActualText spans, which had collapsed selection hit areas in PDFium viewers. The result report uses Segoe UI regular/bold at approximately the source 9.7-point body size. Wrong words are underlined/bold with bracketed correct words; the left margin shows the number of marked errors per rendered line. Text alignment excludes the unattempted source suffix. Copy verification includes PDFium word hit-testing rather than extraction alone.

Version 1.7.0: the progress-report preview includes Skills & Remarks. Checked skills and multiline free-text remarks are saved per candidate. Only checked skills are printed, grouped by category, below all typing-test summaries. Remarks follow the skills. Pending edits are discarded on Cancel; saved assessments are included in profile backup/recovery.
