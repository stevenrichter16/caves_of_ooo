# Independent assembled-package audit

Reviewed README.md, captions.md, manifest.json, findings.md, index.html and evidence/restoration.json after final assembly, 17 October 2026. Also compared the final selection against the earlier image-review fragments. No final deliverable, image, Unity state or repository file was changed by this reviewer.

## Result

The evidence inventory is internally consistent. All52 selected PNGs exist, have unique IDs, match their SHA-256 and recorded dimensions, and reference existing sessions. There are33 primary images, all1280×800;19 supplements comprise12 at1280×800 and7 at1280×720. The screenshots tree has exactly52 PNGs and no unmanifested originals. Captions contain52 matching image sections and the index references all52 originals. All related-image IDs resolve.

The README distinguishes tracked-clean source from pre-existing untracked reports, matches the loaded assembly/source evidence, and identifies SampleScene versus ReferenceGlade. It correctly separates ordinary journeys, controlled travel routes and altered throwing/skill fixtures. BASE menu full reveal, runtime-only manual fog override, ordinary999-cell FOV, different camera baselines and unknown per-frame route settings are disclosed. Individual menu presenter flags are not misread as a2D fallback.

No claim of completed harmful-gas comparison, captured spell impact, verified attack animation, physical Deck operation or all-passing native routes was found. The actual enemy death is not labeled player death. The ripe-crop/harvested-light pair is described as imperfect rather than a matched light test. Source-backed diagnoses are separated from visible concerns and unresolved causes in findings.md.

Captions begin with concrete, understandable descriptions and a useful review question. Most supporting context is clear, but their generated Location rows still expose dictionary syntax; see the polish items below.

## Concrete issues for the assembler

1. **Plain-English caption locations — low severity, all52 sections.** Location lines contain Python representations such as `world {'x': 11, 'y': 10, 'depth': 0}; player {'x': 40, 'y': 12}`. This is not wrong metadata, but it reads like a technical dump rather than the requested human caption. Format as “world(11,10), depth0; player tile(40,12)” and preserve the map's raw-versus-logical distinction. Full structured dictionaries belong in manifest.json.
2. **BASE review pointer — low severity.** BASE-01..04 manifest entries set `reviewEvidence` to `evidence/manual-first-review.md`; their individual open-and-inspect receipts actually live in `evidence/base-review.md`. Both files exist and the latter was used, so this is a provenance-link polish issue, not unreviewed images.
3. **Preassembly coverage-note status — low severity.** `evidence/final-coverage-review.md` explicitly describes its synthesis-time49 files plus3 pending skill copies. Final assembly now correctly has all52 copies. Add a short superseded-checkpoint note or final-inventory link so a reviewer reading that “final” evidence note does not mistake completed packaging work for an outstanding requirement. Its scientific/coverage limitations remain valid.

No blocker requiring new capture, code changes or another Unity session was found. These are documentation/provenance corrections only. Re-run file/reference validation after any regeneration.

## Restoration receipt

The receipt records Editor not playing or compiling, SampleScene clean, isolation inactive with exit code0, saveRoot=null, original last-game ID3082bdbee25f40c9af550330bcabf688, seed0 and starting-choicefalse equal to recorded originals, Game view index0 equal to original, and no owned capture view remaining. It lists only original Keyboard/Mouse/Pen IDs1/2/3, with no virtual gamepad left. Runtime graphics fields match the baseline values available in this package: native on, full detail, world scale1, shadows on, Full FX, speed1, flash0.15 and shake0.

CRT is not repeated in the final restoration JSON, so the receipt does not independently remeasure that field; captured manual records establish CRT off and no route change to it is documented. The README's runtime-only reveal override ended with Play shutdown and the clean SampleScene still owns its original serialized setting. This receipt supports restored session/settings state; it is not a checksum audit of every user-save byte. The user's save was not used by these private capture sessions.

## Independent inventory check details

```json
{
  "selected": 52,
  "uniqueIds": 52,
  "sets": {
    "supplement": 19,
    "primary": 33
  },
  "pngCount": 52,
  "hashFailures": [],
  "dimensionFailures": [],
  "missingSessions": [],
  "missingFiles": [],
  "badRelatedIds": [],
  "orphanPngs": [],
  "captionSections": 52,
  "captionIdsMissing": [],
  "indexImages": 52,
  "indexMissingSelectedPaths": []
}
```

## Resolution verification

The assembler applied all three documentation corrections. Independently reread the regenerated manifest and captions: all52 caption Location lines now use readable coordinates, with zero dictionary representations; BASE-01..04 now point to `evidence/base-review.md`; and the final coverage note explicitly supersedes its provisional49+3 checkpoint with the assembled52 originals,33 primary and19 supplements. All52 PNG hashes and dimensions were checked again and still match.

The restoration receipt now also records `crtEnabled=false`, `captureSizesRemaining=0`, and an additional read-only Unity check confirming the Editor remains idle. This resolves the previously noted CRT remeasurement limitation and independently supports removal of the named capture view entries.

**Final disposition: all identified package issues resolved.** No outstanding documentation correction or additional capture/session operation is required by this audit. The deliberately stated coverage limits remain unchanged.
