# Original TypeMaster artwork

Released in 1.11.9. 92 legacy images have matching-size replacements, with 18 additional standalone UI assets. Company logos are preserved. Each legacy PNG export has the original pixel width and height; existing screen positions and control footprints remain in place. These are independent redraws with matching subjects and layout, not pixel-identical copies.

The runtime no longer crops screenshots for navigation, close buttons, teaching hands, the game selector, the settings keyboard or the meter introduction. Keyboard rendering remains native and reflects the selected layout. The hands and both lesson views share one set of finger cue coordinates.

## Source files and regeneration

`Asset/TypeMasterArtwork/manifest.json` maps 92 old filenames to replacement PNGs and records dimensions and hand coordinates. SVG originals and exact-size PNG exports are adjacent. Four high-resolution photo/scene sources are in `Sources/`. Company branding paths are not remapped.

For authoring only, install `Pillow` and `resvg-py`, then run:

```powershell
& '.\Native\Tools\NewBrandAssets.ps1'
python Native/Tools/NewArtwork.py
```

The Windows app needs neither Python nor SVG support; WPF loads the committed PNG exports. Legacy bitmap names retain their old extension in the new filename (for example `keyboard_home_row.gif.png`) to avoid collisions.

## Photo/scene prompts

The built-in imagegen tool created four independent images, using the corresponding supplied file as a composition reference. Small UI graphics and educational diagrams were drawn deterministically in SVG.

### typing-photo

Original bright high-key commercial photograph for a typing tutor: extreme close-up of adult hands correctly typing on a silver laptop keyboard, hands occupy the middle and lower half, clean white desk, soft white window light, blurred green houseplant leaves at upper right. Portrait composition approx 170:326, designed to crop to a very narrow vertical panel. Fingers anatomically correct, realistic, professional. No text, logos, watermark, UI, or visible face. Match the supplied reference's subject and framing but create a distinct original photo.

### junior-photo

Original school computer-lab photograph for a typing tutor. Vertical narrow portrait approx 170:326. Four schoolchildren sitting in a diagonal row at desktop keyboards, closest smiling child at bottom left, three classmates receding toward upper right, colorful softly blurred classroom. Classic computer keyboard prominently in lower third. Friendly natural light, authentic candid educational stock photography, anatomically correct hands. No text, logos, watermark, or UI. Recreate the reference's subject and layout as a distinct original photo.

### typing-tip-photo

Original close-up photograph of adult hands typing on an ivory desktop computer keyboard viewed diagonally from above, a computer mouse at upper left, dark desk background and warm amber desk lighting. Narrow portrait panel approx 193:289. Hands in lower half, keyboard extends from upper right toward lower middle. Realistic anatomically correct hands and keys. No text, logos, watermarks or UI. Similar subject, crop and warm lighting to the reference, distinct original photography.

### robinhood-scene

Create a distinct original black-and-white woodcut illustration for a Robin Hood typing lesson. Very wide landscape aspect approx 862:369. Two medieval English forest archers at left, one holding a tall longbow and the other pointing toward the right; dense oak forest frames a distant small stone castle in a clearing at center-right. Decorative rectangular woodcut border, bold black ink and ivory white cuts, historical book illustration, rich leaf and bark linework. Similar subject and placement to the reference but new independent composition, no copied contours. No text, labels, logos, or watermark.
