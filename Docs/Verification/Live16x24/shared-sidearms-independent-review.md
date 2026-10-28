# Shared sidearms — independent bounded review

2026-10-28. **No remaining P1/P2 finding in the reviewed source or actual item views.** This accepts the shown Dagger, Mace and ShortSword body/reader integration only. It does not accept whole-area polish or a packaged build.

## Source and native evidence

Independent review covered the IncidentalEquipment constructor/resource/lifetime extension, exact sidearm guards, authoritative Greybox world/reader routing and the separate author's fixture. Two initial findings were corrected: the invalid `BodyPart` generic check now uses `Body`, and a known sidearm's refusal cannot fall through to a weaker opening body. The corrected routing keeps its own equipment tile and handles the shared reader before local fallbacks. Existing two-/three-input constructors leave the new optional inputs absent; cached views own their sprites/tiles, not borrowed textures. No further blocker was found in that bounded correction review.

The root ran Unity; this reviewer did not. Independent XML parsing confirms the retained RED contains 23 distinct cases (22 Failed, one existing control Passed). Final GREEN contains 108 distinct Passed cases: 23 sidearm, 48 incidental equipment, 35 LongSword adversarial and two LongSword cases; no failure, skip or inconclusive result.

- RED XML SHA256: `8a27dc4dc42ea235dfaa99661f46c725cd615221201a56ccc1e745fb64cedeae`.
- GREEN XML SHA256: `2d334d578abdfb726495a73831e0c5b96d955e60c063757c4c82d324c1a3c3f6`.
- Reviewed current equipment catalog: `0e6d9c4994ee2260a02b20414dc0dc7c2ea0f8c0d4e1816fecfb83b5028891c5`; Greybox: `8104432fa331d1a66663a39d2546c069fc1f793f7a4fa0c74007d12cda409628`; sidearm fixture: `66b429744e8a6030d432fd91486587b1eb470d3e41a4d428443a1bea1e77b6ad`.

All six PNG/import-metadata hashes in `ArtSource/Live16x24/shared-sidearms/reuse-manifest.json` match current files. The three textures remain 16×16. These are unchanged approved legacy bodies (`item_dagger`, `item_mace`, `item_sword`), not newly authored art or newly established editable-source provenance.

## Actual image review

Opened before02/03 and after02/03/04, 10/11/12 and 13/14/15. The dagger's former cyan slash becomes a recognizable short blade with grip; the broader guarded sword and headed mace are visibly distinct in both carried and ground readers. Their pale silhouettes remain legible against the dark reader panels. Ground bodies are small but discernible beside the player, with no obvious stretch, clipping, mismatched portrait or cell-spanning geometry. The disclosed-owner receipts independently identify Dagger7359, Mace7361 and ShortSword7360 at60,3, with player61,3 and the corresponding native texture selected.

Independent RGB decoding of after05/06 reproduces zero changed pixels in the receipt's declared `[0,96,940,696]` rectangle: 564,000 pixels. This is the shown Dagger fresh **Editor Continue** comparison, not a full-frame, all-three-item or packaged-process restart claim.

## Remaining limits

The bodies improve missing-item coverage; they do not fill the surrounding chunk. The shown Sill viewport still has broad sparse dark patches, repetitive glyph vegetation and mixed prop/NPC coverage. Existing coarse typography and dense reader line wrapping remain visible. Those composition and UI debts are not promoted to accepted polish by these three sprites. No new actual combat, proc, repair, whole-world performance or platform acceptance is established here.
