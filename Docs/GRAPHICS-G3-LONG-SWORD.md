# Exact LongSword world and Examine art

2026-10-25 UTC. Status: implemented; bounded ordinary world/reader, pickup/equip, save/load and cellar-return checks accepted. Native regression391/391 passed. Full ordinary temper/combat/six-site gates remain open. Earlier planning checkpoints below are historical.

## Goal and boundary

Give the real LongSword used on the opening's north-material → east-forge
journey one readable native16×24 image shared by visible ground and authorized
Examine. Preserve accepted Player and item art, world ownership/fog rules, and
the underlying shape after legitimate tempering. This is original CoO art in
the approved native direction, not a new Qud-parity mechanic.

Exact `LongSword` only. Do not alias LoanerLongsword, the distinct authored
TemperedLongSword, variable ForgedWeapon, other weapons or generic slash glyphs.
Do not add a Player-held overlay, fake enchantment glow, equipment state,
resources, gameplay powers or a broader region-admission rule.

## Verification sweep and corrections

| Source | Verified result |
| --- | --- |
| `Objects.json:9075` | Exact LongSword is a long straight steel blade with plain crossguard,8weight,1d8,PV2,`Cutting LongBlades`,10HP. Current RenderString is `/`. |
| `EnvironmentSpriteRenderer.cs:2066` | An older `item_sword.png` exists and is16×16. The current flat route does not use this legacy mapping. There is no project-wide absence of sword art. |
| `FlatPixelOpeningArtCatalog.cs:482–509` | Twelve loose-item native frames exist; none admits LongSword. No exact LongSword mapping was found in other current flat catalogs/native source manifests. |
| `FlatPixelGreyboxCatalog.cs:775–834`; `ZoneRenderer.FlatPixel.cs:49–66` | Flat world and exact-subject reader share native selection, then canonical glyph fallback. Flat reader deliberately does not create an older model preview. |
| `WeaponTemperingService.cs:145–197` | A real temper changes on-hit specs, count, HP and DisplayName, not BlueprintName. The same LongSword silhouette must remain. Authored TemperedLongSword is a separate blueprint. |
| Authored BrewedTonic / runtime brewing | The blueprint is a vessel shell with no BrewItemPart. A valid fixture quench must attach the resolved effect payload as brewing does; reading a nonexistent part would fail before testing temper behavior. Cold review corrected this draft premise before import. |
| `FlatPixelOpeningArtCatalog.cs:228–239` | Existing portable-item guard rejects explicit reskins, invalid roles, foreign/invalid stack and solid/untakeable physics; callers separately authorize current custody and disclosure. |

## Smallest implementation after observed RED

One independently optional resource, `Sprites/Live16x24/long-sword`, with one
full16×24 frame. Add it to the existing shared opening-art catalog with borrowed
texture/cached owned Sprite and Tile. Keep the accepted twelve-frame item atlas
unchanged. Earlier injected constructors omit the new resource without loading
it; default construction loads it. Missing/malformed texture preserves fallback
and other art. Reuse existing ground/subject routing, disposal and scope gates.

Selector requires exact LongSword, the portable guard, and its actual owned
MeleeWeapon and Hand Equippable parts. Mutable display names, glyph/color, HP,
price and damage do not define new art states. No blanket family selection.

Root owns ordinary pre-change exact-item capture and imagegen. Use the real
owned sword reader plus approved loose-item contact, then extract straight
blade, plain guard, short grip and pommel. Author editable integer-cell Blender
geometry, source-cells JSON and reproducible generator; verify native output
and unchanged accepted PNG hashes. Source and runtime path conventions follow
`build_opening_items.py`, `build_cindercaller.py` and `build_east_kitchen.write_meta`.
No imagegen output or contact sheet is gameplay evidence.

## Verification sequence

1. Import two initial tests using only current APIs. Prove current visible
   world and equipped reader still select fallback before testing resource
   existence. Record actual behavioral RED.
2. Add the narrow selector/resource injection/disposal implementation and native
   asset. Run the focused suite; then add meaningful counterchecks for wrong
   exact identities, malformed/foreign functional parts, reskins/roles/custody,
   stale cells/reader documents, fog, missing resource and borrowed disposal.
3. Use real WeaponTemperingService to prove legitimate name/spec/HP changes
   retain base art without art queries mutating simulation or ownership.
   Rerun affected item/product/portrait/tempering regressions. Later counterchecks
   are not independently RED-proven unless their actual failures are recorded.
4. Ordinary owned reader → drop/ground → pickup/equip → save/load/revisit.
   Continue actual frost quench only when current health/equipment permits;
   record the real transformed unit and effects. A historical earned checkpoint
   must be labeled separately from recovery of the current lost blade.

## Self-review and acceptance limits

- 🟡 Avoided scope error: no slash/LongBlades/name-based weapon alias and no
  rewriting the approved item atlas. Runtime temper keeps base geometry.
- 🧪 Pending native RED/GREEN, cold review, actual imported resource and ordinary
  world/reader evidence. Source audit alone does not close these gates.
- 🧪 Full assembly/weapon coverage, full-parry/Riposte observation, complete
  six-site journey, packaged build and physical Steam Deck remain open.

Can verify after tests: exact selection, real transaction state, custody/fog,
borrowed resources and submitted frame. Cannot infer from those tests: native
readability in ordinary gameplay, visual feel, device performance or release
quality. Root records actual captures and limitations before acceptance.

## Implementation log and files

- Source audit recorded outside the repo at `/tmp/coo-longsword-art-plan.md`.
- This living plan is the only production-tree change in the drafting step.
- Initial tests, fresh meta, proposed runtime patch and expanded checks are
  staged in `/tmp/coo-long-sword-draft`; none is imported or applied by this draft.
- Two initial behavioral cases passed independent source review. The runtime
  patch and additional draft countercases also received bounded cold review;
  the BrewedTonic fixture premise above was corrected. One added factory Player
  hand Equip/Drop control covers the distinction from the simpler legacy-slot
  fixtures. There are now35 draft countercases plus2initial cases; these counts
  enumerate test attributes and are not executed results.
- Planned runtime edits: `FlatPixelOpeningArtCatalog.cs` and
  `FlatPixelGreyboxCatalog.cs`. New exact native asset/source and focused test
  are to be recorded here with actual RED/GREEN and ordinary evidence.


## Native RED and implementation — 2026-10-25 UTC

Actual initial2 cases both failed the intended glyph-vs-native assertions at00:10:04–05, before resource checks (`long-sword-red.xml`). Only then root applied the optional resource selector patch and imported35 further countercases. No claim those added counters independently failed RED. Real baseline uses an unmodified copy of the earned checkpoint, not recovery of the failed forge run: Continue, equipped Examine, unequip, drop, stepRight andF5 at1050. Actual reader/world PNGs and replay/hash receipt are preserved.

## Focused GREEN and review

`long-sword-green.xml` ran37 cases at00:15:15–17UTC:37pass,0fail,0skip. This includes2 behavioral RED cases and35 later counters, including actual factory Player Body/Hand equip/drop and real single/split-unit temper transactions. Pre-import review caught that authored BrewedTonic has no BrewItem payload; fixture now attaches the resolved brewing payload before calling the real temper service. Independent source review found no remaining significant issue. A later suggested Body namespace import was checked against its actual Core declaration and correctly rejected as unnecessary. NativeGREEN validates that compilation boundary too. Wider regression and ordinary new-art capture still pending at this checkpoint.

`long-sword-regression.xml` adds the affected item/works-product/portrait/fallback/reforge/unit suites:391distinct cases at00:17:12–40UTC, allpass,0skip. Focused37 are included. No whole-project test claim.


Continuation correction: NativeSaveIsolation.Finish deletes its disposable audit root after restoring user settings. The injured east-run F5 checkpoint was not separately exported before cleanup; its screenshots/events remain, but that disposable5HP save cannot be resumed. The separately exported full-health earned checkpoint is preserved and the sword pass explicitly replays that branch. No user save was removed or overwritten.

## Final ordinary integration and review — 2026-10-25 UTC

Actual Play evidence is indexed in `Docs/Verification/Live16x24/long-sword-play-summary.json`. Root used ordinary keyboard input only. LongSword2699 was dropped beside the glade cellar stairs at28,19 and displayed in world/ground Examine. Pickup automatically equipped that same item in the left hand. Equipment F1 displayed the matching native portrait; F5/F6 and normal cellar descent/ascent retained the item and portrait. Receipts include custody, visible neighbors, health and tick. Equipped/reload/revisit frames are separate captures even where static pixels match. Equipment slot glyphs remain canonical; this scope changes world/Examine art only.

The healthy earned branch remains42HP, level2, RP1, glade28,19 after the roundtrip. Root exported its final F5 checkpoint to `/tmp/coo-long-sword-verified-checkpoint` **before** disposable isolation cleanup; hashes are in `long-sword-checkpoint-backup.json`. It has no brewed8995 or recovered severed blade from the failed east branch. That earlier branch's pictures/events remain, but its disposable save was not exported and cannot be resumed. This corrects the earlier handoff without erasing failed-route evidence.

### Source and visual review

Imagegen reinterpreted the real baseline reader and accepted item contact; its printed resolution labels were not trusted. Fresh editable Blender integer-cell geometry produced one neutral16×24 frame. Generator, Blend, explicit cell JSON, concept/prompt/critique, parts/contact and validation are retained in `ArtSource/Live16x24/long-sword`. Six approved colors,66opaque pixels, binaryalpha and runtime/source equality pass. Approved88PNG/meta files match canonical committed bytes. The22 historical metadata differences were Unity trailing whitespace already present in the original source snapshot; both hash sets and reconciliation evidence remain.

Independent source/tests/art review found no remainingP1/P2. The ground reader silhouette matches all66native pixels at3×. Root inspected actual equipped/reload/revisit frames too. The source verifier reads imagery without editing it. No accepted Player/item art was replaced.

### Profile and console bounds

`long-sword-profile.json` records335.60seconds of mixed Editor play, UI and idle/tool time. Its131072sample buffers wrapped: retained renderer mean0.0191ms/p990.1039ms, main-thread mean1.1008ms/p992.1146ms, maximumdrawcalls37/batches36. This establishes neither a controlled speedup nor physical Deck performance. Two existing Metal memoryless-depth warnings remained; direct Unity console counts were0errors/2warnings/58logs. The MCP console adapter labels normal `Debug.Log` Autosaved as Exception; raw adapter output and direct counts are both retained.

Cleanup at00:25:02UTC restored stopped SampleScene, null save override, no isolation/artPending/borrowedview. No user save or current build changed. Native391includes37focused; later counters were not separately RED-proven. No whole-project test claim.

### In-phase self-review

- 🟡 Resolved: stale opening status and34countercase typo corrected; actual additional count35. No open significant runtime finding.
- 🔵 Exact blade only; no held overlay, statusglow or weaponfamily alias. Previous art and variable forging remain intact.
- 🧪 Remaining ordinary gates: temper recipe/output/on-hit loop, full parry/Riposte, post-fix periodic damage after travel, crop lifecycle and six-site continuity. Package/physical Deck remain open.

### Next action

Continue the connected ordinary material→forge loop from the explicitly exported healthy replay branch using real preparation/encounters, without grants or RNG manipulation. Keep unsuccessful-route captures. Prioritize feedback/readability defects on that route before another disconnected asset packet. The recurring graphics/storage run remains active every10minutes: preserve/push exact work first; remove only verified stale regenerable outputs. Current bounded inspection found no worthwhile safe removal;0bytes reclaimed.
