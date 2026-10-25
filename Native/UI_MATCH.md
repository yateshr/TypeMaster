# UI reference mapping — native 1.2

Current 1.11.0 artwork preview: runtime screenshot crops are replaced by original assets and native controls. The original dimensions and placements are retained; historical RGB comparisons below are diagnostic, not acceptance checks. The new image audit decodes all 92 replacements and verifies their exact original pixel dimensions, standalone asset footprints and shared finger cue positions. See `docs/ARTWORK.md` for source files and generation details.

| Native screen | Supplied screenshot time |
|---|---|
| Welcome / existing user | 113337 / 113357 |
| New user | 113501 |
| Choose Course | 113530 |
| Meter launch / training | 113515 / 114255 |
| Typing Test / practice | 114304 / 114314 / 114328 |
| Games | 114338 |
| Statistics / report | 114346 / 114404 |
| Settings / About | 114422 / 114429 |

Screenshots are in the original UI folder, dated 2026-10-23. The reference workspace dimensions and placement are checked automatically. Render captures use isolated test profiles and the supplied assets. Side-by-side images show the reference on the left and native output on the right.

All navigation, text input, dropdowns, buttons, scoring, and game engines execute native C# / WPF code. Screenshot crops provide only illustration artwork for the game selector and meter introduction. No full-screen image replaces an interactive screen.

Remaining visual differences include native vector navigation icons, small font/antialiasing differences, current profile data, report dates, and accurate local-version/licensing text. Source passages determine wrapping. Secondary screens without supplied screenshots use the same native theme. Visual review covered the default light theme at 1440 × 852; other DPI scales and maximized layouts should be checked on the target display.

## 1.2.1 reference artwork pass

Navigation icons and fixed navigation lettering, the Free Version footer, default US keyboard preview, close symbols, new-user arrows, and game caption graphics now reuse their exact supplied reference pixels. Native buttons remain actionable with accessible names. Custom keyboard layouts and the typing keyboard remain dynamically rendered from KBD files.

The new-user information panel is restored. Practice text uses 19-pixel Georgia, the reference column width and paragraph spacing, whole-word highlighting, and centered Cancel controls. Test text ordering, list font, step numerals, outline states, and button underlines are refined. Screen-size selection takes effect on the next profile entry.

The self-test verifies exact RGB equality for the keyboard illustration, Cloud Race caption artwork, footer, selected navigation button, course gradient, and game background at 1440 × 852. It also emits screen-pixel-audit.json with whole-workspace pixel comparisons. These broader comparisons deliberately include live data, native font rendering, and local build/service text; they are not expected to be 100%. No whole-application pixel-identical claim is made. About shows the current application version, build and profile services.



## New guides - 24 October, version 1.3

| Guide timestamp | Native implementation |
|---|---|
| 004454 | Course lesson overview, chapter links, optimal duration and Continue Lesson |
| 004812, 004829, 004924, 005444 | Test selector, Add/Delete menu, imports, duration, completed record actions |
| 005236 | Completion overlay, Enter/Next continuation, typed error underlines |
| 005250, 005647, 005710 | Failed/passed result layouts and the report/certificate actions |
| 005849, 005855 | Native speed and accuracy charts with axes and thresholds |
| 010126 | Meter introduction and launch/settings polish |
| 010206, 010215 | Review options, key sequences, native keyboard and guide hands with a live pointer |

The five PDFs define four distinct template families. Numeric output uses the individual-result format with multiline pagination; both complete-report PDFs use the profile progress format. Assessment now means the selected attempt's examination sheet, rather than the old aggregate report. See REPORTS.md.

The new reference screenshots are 1920 pixels wide. Their central workspace crop is (565,257,790,541); the native render uses the established (325,166,790,541) crop at 1440x852. Compare New UI.ps1 produces side-by-side views without scaling either workspace. Isolated test names, history, sample text, graph values and dates differ by design because the native UI uses live records.

The trial footer is omitted to follow the new guides. Six current artwork-region checks cover the default keyboard, game caption, close symbol, selected navigation row, course gradient and game background. The earlier footer check is retired. This audit does not assert that every screen or DPI scale is pixel-identical.

Version 1.3.1: trailing blank lines no longer add blank rows throughout a numeric test. Exercise rows use the reference's 25-pixel line height; the passage scrolls to retain two rows of context above the active character. Native regression checks cover every numeric row and completion of the full supplied numbers-only passage.

Version 1.3.2: the 022147 and 022242 references require a clockwise elapsed-time pie (empty initially, full at completion), nine-row numeric pages, olive completed text and visible Enter marks in the response. The test screen now implements these details; speed/accuracy remain available on the result screen and pie tooltip. Numeric paging preserves whole rows and replaces the earlier rolling character excerpt.

Version 1.4.0: the original 790x628 shell sits in a downscale-only Viewbox, preserving the 1440x852 reference placement while fitting smaller viewports. Regression checks cover seven pages at five window sizes, including the effective 853x480 viewport for 1280x720 at 150%. Font-mask comparisons ranked Georgia 19 and Segoe UI 27 ahead of the tested alternatives; those source-like families are retained with display formatting and fixed hinting. This does not claim identical rasterization across all monitors.

Version 1.5.0 implements the 03:26-03:36 screenshot course flow: grouped activities, chapter tabs, completion circles, duration ranges and course footer; introduction title/illustration/navigation; finger prompts; missing-key warning; course statistics selector and second accuracy graph. It preserves the original event IDs for profile migration. Charts use real native results and source accuracy targets, while difficult-key classification and adaptive timing remain native approximations. Provided localization and keyboard-name resources are used rather than cloning license dialogs.
