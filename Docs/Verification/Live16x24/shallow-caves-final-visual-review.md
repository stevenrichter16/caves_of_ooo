# Shallow caves — bounded final visual review

2026-10-27. **The corrected foundation is a useful integration checkpoint in the sampled ordinary depth1/depth2 views. The first trial's pit-like background defect is corrected. This does not accept whole areas as polished or complete.**

Reviewer: `/root/portable_equipment_final_review`. I opened each of the seven PNGs below at its actual 1280×800 resolution. I authored the native fixture, so my test review is self-review; my runtime/art and actual-image review is independent of the runtime/art authors. Root controlled Unity, gameplay, captures and native execution. This review made no Unity/Git calls and changed only this record.

## Actual visual findings

- **04, corrected entry:** The tan traversable field now continues beneath rubble and through cells without a literal floor owner. Near-black individual squares no longer imply pits. Darker planar walls still define the room and passage; the standing stalagmite has a separate pointed silhouette. The backing does not add scenery or establish a new terrain owner.
- **07, stalagmite reader:** The selected small world obstacle and enlarged portrait share the same pointed pale-ridge/dark-body silhouette. The sidebar identifies a solid stalagmite over sandstone floor; the reader is legible. Dark glyph strips beyond the currently lit presentation remain conspicuous but are not evidence of empty terrain or permission to reveal concealed state.
- **12, wall wreckage:** The screenshot shows the player on a cell whose contents are rubble, while the log reports the sandstone wall's destruction. The remaining wall border and low debris are visually distinct. This supports the visible result of root's ordinary action sequence; the screenshot alone is not an independent simulation trace.
- **15, labeled fresh Continue:** The resumed view retains the same material language and quiet backdrop, with HP23 visible. The active En Garde state and remembered glyph regions remain visible. I inspected this image; root owns the fresh-session/state-persistence verification. I did not infer pixel identity from a single resumed image.
- **16, rubble reader:** The portrait is a loose, low stone scatter, consistent with the small world piles and the selected `rubble` terrain owner. It does not resemble a new wall or an intact stalagmite. The description and portrait fit inside the reader.
- **19, second column:** The same palette and geometry remain coherent across a different branching passage layout. Rubble reads on the floor without dark tile backing; walls form darker continuous boundaries. Broad quiet passages contain repeated debris clusters rather than newly invented decorative objects.
- **25, depth2:** The wider room keeps the floor/wall separation and pointed stalagmite readable. The sidebar shows HP33 and the player's selected cell contains sandstone floor and stairs leading up. Root reports normal travel to `Overworld.16.13.2`, Healing Tonic use from HP23 to40, then ordinary Scrabbler combat to33; I did not execute these actions. The retained depth2 fixture metadata independently reads that zone, turn11910 and HP33/40. Other creature, loot and hazard presentations remain outside this six-owner review.

The entry and later stair-arrival images identify stairs beneath the player through the sidebar; they do not by themselves provide unobscured portrait/body acceptance for both stair directions. Root's other reader/travel captures and state evidence remain the source for those claims.

## Named repair and color evidence

I previously opened `shallow-caves-before-02-depth1-entry.png` and the flawed `shallow-caves-after-01-entry.png`. Their comparison exposed black rectangular cells inside the disclosed tan room, including transparent rubble backing. The correction is visible in after04 without changing the frozen wall atlas.

Read-only RGB sampling of the actual PNGs confirms a local match: pixel `(746,264)` changes from near-black `(6,4,3)` in after01 to `(94,67,41)` in after04. The adjacent floor pixel `(730,264)` is `(94,67,41)` in both captures. Within world rectangle `[0,40,940,680]`, 17,789 formerly near-black pixels change to that same dominant floor RGB. This is a bounded actual-image color observation, not a claim that every pixel is identical: torch/postprocess shading varies slightly between captures.

The source/runtime review found no significant new blocker in the separate `AmbientBackdrop` correction. It changes background treatment without promoting an owner, `LocalGround`, floor reader or represented-liquid flag. The two painter paths honor the same flag. Hidden/custodied owners cannot toggle the backdrop; true floor failures, authored backgrounds and live hazards retain their existing refusal paths. CPU tint tests alone did not establish the pixel match above; the actual capture supplies that evidence.

## Remaining visual and coverage debt

1. Large rooms still repeat a small family of floor scratches and rubble shapes. The quiet field improves legibility, but these samples do not establish varied landmarks, environmental storytelling or full-chunk composition.
2. Remembered regions retain the conservative dark glyph presentation, producing a sharp transition from current native scenery. That is a visible consistency limit, not a finding that fog or remembered space is bare.
3. Non-kit glyph owners and some contrasting dark/colored overlay cells remain visible, especially in depth2. Their actual materials/states must be identified before replacement; this review does not classify those cells as empty or approve covering them with ambient floor color.
4. Small rubble is subdued at normal zoom but distinguishable from the taller stalagmite and darker solid walls. No further speculative texture, outline, added props or density change is requested from these images.

No full80×25 chunk census, all-column traversal, deeper-band admission, completed actor/container/hazard coverage, whole-area polish, hardware performance, packaged cave run, Linux execution or physical Steam Deck acceptance is claimed here. Unexplored and remembered space is excluded from fullness judgments.

## Evidence identity

The current native XMLs were read, not executed by this reviewer: `shallow-caves-backdrop-green.xml` contains56 passes and `shallow-caves-backdrop-related-green.xml`339 passes. Their fullname sets are disjoint: **395 distinct passes, zero failures/skips/inconclusives**. Earlier44/383 executions are not added. These checks support runtime validation but do not replace the actual visual findings above.

| Opened capture | SHA256 |
| --- | --- |
| `shallow-caves-after-04-backdrop-entry.png` | `f9c332ae5b73b5b7c28e2257191f38eedce76fc4d451312860eea62619c096c0` |
| `shallow-caves-after-07-stalagmite-reader.png` | `2ba6edf886bda09770563c552eb9215974878cb59a1066a42d10c6c1f59e2301` |
| `shallow-caves-after-12-wall-wreckage.png` | `83cc00652517f6fcc0a879f517e24fd3b2bd7e9d41e91a5572c3c21bc78a4e1d` |
| `shallow-caves-after-15-fresh-continue.png` | `27abbb74fa898b4d9cd250b174089ed5aef4fc7534331392901d47491f272603` |
| `shallow-caves-after-16-rubble-examine.png` | `066e1858836cc847a16cbcba02393255401557740ecbc8bfbdb1fac00ed61419` |
| `shallow-caves-after-19-second-column.png` | `eb9f14f83b61530878971a1c48a8f4f262a9299a067ddf7487879b64ffe6d861` |
| `shallow-caves-after-25-depth2.png` | `ace94b7b925c1c2298bbde66a8a26de293b7a2dd08bd187ed77b0f4224ea0f50` |

Native XML hashes: focused `189ebe9792c41cc8e7d765a2c2e6d84b604aa611a83d6affefc341d1c5cdd5cb`; current related `ad2353650ad520a64d6df46e16b86a5a654448b353c54fba216dc14d8d31c345`.
