# Opening equipment identity: leather armor and hatchet

Status: native implementation and bounded ordinary gameplay accepted, 2026-10-25 13:03 UTC.

## Goal and current scope

Replace two confirmed ordinary-game letter placeholders with native 16×24
equipment art in the admitted opening journey. Keep the existing ground,
actors, collision, UI, loot selection and mechanics. This is one related item
family, not completion of the opening or a worldwide equipment reskin.

🟢 Existing foreground/exact-subject reader authority and approved native art.
🟢 Actual Hatchet and LeatherArmor now have exact-owner native ground and reader art in the admitted opening; unsupported semantics retain fallback.
⚪ Other items, regional kits, damaged overlays and animation are separate.

CoO-original artwork/integration; Qud-like readable low-resolution silhouettes
are the direction, not a claim of source-code or art parity.

## Verification sweep and actual baseline

| Premise | Verified evidence / correction |
| --- | --- |
| Kitchen P identity can be inferred from glyph | No. Ordinary Continue of earned kitchen save, L/arrows/Return/x identifies Hatchet13663 at24,5, with owned weapon/Hand equipment parts. Battleaxe also uses P, so exact identity is required. |
| Armor6954 is a universal identifier | No. The earlier separate census has a PeatBog6954. In the earned north checkpoint, ordinary exact pile selection identifies LeatherArmor6954 at16,17. Compare within the actual graph. |
| Armor is visibly broken because Repaired=false | No. Its reader says sound, HP15, body armor. Neutral thick leather panels and used straps must not imply missing protection or a repaired state. |
| Lower armor should replace the top coin | No. The actual pile's top owner is GoldCoin. Explicit armor Examine gets armor art; world art keeps the current top owner until ordinary pickup/drop changes it. |
| Any glyph P or [ should receive these sprites | No. Exact blueprint, owned visible Render/Physics, Item and equipment semantics, positive valid stack and single-cell geometry only. Explicit visual overrides retain fallback. |
| Every mutable stat belongs in art admission | No. Armor AV/DV/HP/repair provenance and weapon damage/procs/name can legitimately change. Neutral art does not assert those values. |

Baselines: `incidental-items-kitchen-hatchet-before.png`,
`incidental-items-kitchen-hatchet-reader-before.png`, and
`incidental-items-north-armor-reader-before.png`, with matching JSON, under
`Docs/Verification/Live16x24`. Actual1280×800 Game output, ordinary Continue and
Look/Examine, two isolated copies of earned fresh-origin checkpoints. No grants,
teleport, render-mode override or changed geometry. Reading consumed no turn.
Normal saves, preferences and borrowed Game view are restored between sessions.

## Design / implementation prompt

Use those actual reader/scene captures and the accepted native long sword as
scale/palette context. Generate a concept for a short wood-handled wedge hatchet
and an empty strapped leather vest. Extract clear negative neck/arm openings,
few leather panels/fasteners, compact asymmetric blade and haft. Reject painted
3D light, gradients, excessive detail, drop shadows, invented ornaments and
changed world/layout. Build editable native pixel rows and exact Point/Clamp,
uncompressed binary-alpha sprites; never resize generated pixels into production.

Author RED first against factory-created items and actual world/reader routing.
Use a small optional cached equipment companion, default-only lazy resource
loading, preserving existing injected catalog constructors. Independent native
resources must fail independently, and disposal must not destroy shared textures.
Only admitted local art uses the new frames; current graph/cell/custody/fog and
exact Examine authorization remain upstream. No new scan, actor object or
per-frame texture allocation. Keep item stats/content/save format unchanged.

Test each identity positively, and reject wrong/foreign/missing equipment and
render/physics parts, unrelated blueprint, explicit reskin, invalid stack and
multicell subject. Exercise actual pickup/drop/equip, covered lower owner,
stale documents, hidden/replaced graph, resource absence/malformed dimensions,
peer-cache disposal and neutral art under mutable stats/repair/name changes.
Root runs native RED, then implementation, native GREEN and independent review.

Repeat ordinary item discovery, exact reader, pickup/carried/equipped reader and
drop on a clear tile; save/reload and revisit as appropriate. Capture true Game
output with identity/custody receipts, compare native-size art, fix named defects,
then accept this bounded family. Preserve normal data; fetch/rebase/push exact
owned source, art, metadata, tests, documentation and evidence. Cleanup only
after remote preservation and only if a stale regenerable candidate is verified.

## Acceptance / honesty bounds

Can verify after implementation: native ownership/resource and counter tests,
real ground/carried identity, actual menu actions and sampled save continuity.
Cannot infer: every equipment family, physical Steam Deck feel/performance,
new packaged binaries, complete opening scene quality or full regional coverage.

## Files and continuation

Implemented: equipment companion and metadata; two native resources and editable
pixel source/exporter; narrow Greybox world/subject hooks; 33 new test cases; this
living record, shared continuation/coverage and actual verification captures.
Next action: second-seed whole-scene north approach and Nella lifecycle, using the
earned Warden cellar checkpoint. Do not grow unrelated isolated item packets.


## Implementation and source extraction

Actual reader captures plus the accepted long sword informed imagegen
`ArtSource/Live16x24/incidental-equipment/concept-v1.png`; prompt and extraction
are alongside it. The concept's gradients, excessive axe size and altered sword
were rejected. Editable native rows in `source-cells.json` export through
`Tools/Live16x24/export_incidental_equipment.py`: vest 160 opaque pixels with a
12×18 ink envelope; hatchet 73 pixels, 11×16. The short shaft distinguishes it
from the accepted 22-pixel-high sword. Neutral used leather is not broken armor.
Runtime images are byte-identical exports, Point/Clamp, uncompressed, centered,
16 PPU, binary alpha, with fresh Unity metadata.

`FlatPixelIncidentalEquipmentCatalog` lazily caches independent frames. Its
world and exact-subject hooks in `FlatPixelGreyboxCatalog` preserve upstream
current owner, graph, fog, pile and custody authority. No change to blueprints,
loot, damage, composition, equipment values, saves or scene geometry. Existing
injected constructors never load optional Resources. Disposal owns only the two
Tiles/Sprites, never borrowed resource textures. A missing frame fails locally.

Implementation invariant:
```csharp
if (footprint.ParentEntity != owner
    || owner.SpatialZone != null && !owner.SpatialZone.IsFootprintCurrent(owner))
    return false;
```
This refuses stale registered native art while retaining valid portable items.
It neither repairs the spatial index nor changes generic reader authority.

## Native tests and independent review

| Gate | Actual native Unity result |
| --- | --- |
| Initial missing implementation | 29 tests, all 29 fail, 12:39:49–51 UTC |
| Initial integrated regression sweep | 143/143 pass, 12:41:56–12:42:02 |
| Raw footprint mismatch reproduction | 33 total, 31 pass, the two intended exact-reader cases fail, 12:53:04–07 |
| Corrected final regression sweep | 147/147 pass, zero skipped, 12:54:32–40 |

Evidence uses `incidental-items-red`, `green-retry`, `footprint-red` and
`final-green` XML/summary/job receipts in `Docs/Verification/Live16x24`.
Overlapping totals are not added. The first job-status response timed out after
XML completed; an initial GREEN request reported tests-running without execution.
Saved completed XML, not these stale job responses, supports the result.

The 33 new cases exercise factory identities, wrong/foreign/missing functional
parts, explicit reskins, stack/shape, owned current reader/world routing,
covered lower item, hidden/replaced/stale graph, pickup/equip/drop, independent
bad resources, neutral mutable stats and shared-texture/cache lifetime. The
147 sweep includes existing long-sword, incidental-actor and fallback tests.

Cold source review and an additional footprint/custody review found the raw
footprint mismatch; both corrected source and final evidence were reviewed.
Art/extraction review also inspected actual game captures, with its authorship
limits stated. See `incidental-items-final-code-review.md` and
`incidental-items-independent-review.md`.

## Ordinary gameplay: two actual earned save branches

All captures are actual 1280×800 Game output. Ordinary Continue of isolated
copies, real keyboard/menu movement and F5/F6; no grants, teleports, presentation
forcing or altered geometry. Entity IDs below are local to these saved graphs.

| Branch | Actual verified actions and captures |
| --- | --- |
| Kitchen, seed64 | Look/Examine Hatchet13663 at24,5; walk there and pick up; native Pack portrait; equip into Hand replacing LongSword2703 while retaining Buckler2704; drop (auto-unequip), restore original sword; move24,7; F5/F6 retains hatchet at24,5, HP42 and tick6030. `incidental-items-hatchet-inventory/equipped/dropped/reloaded` plus baseline and reader pairs. |
| North, seed64 | Explicit pile selection exposes lower LeatherArmor6954 at16,17 while GoldCoin remains top world owner; native exact reader says sound, 15lb, Body +3AV/-1DV. Walk/pickup naturally equips empty Body; Pack shows 4AV/7DV. Unequip gives carried armor and 1AV/8DV; drop on clear16,18 and move17,18. F5/F6 retains exact armor ground identity, HP32 and tick2380. `incidental-items-armor-equipped/carried/dropped/reloaded` and exact-reader pair. |

Read-only JSON accompanies each capture. Inventory row order after reload is not
an identity invariant; compare actual IDs/custody. Armor's automatic first equip
is existing gameplay, not new behavior. Protected earned checkpoint copies are
`/tmp/coo-incidental-items-kitchen-earned` and `...-north-earned`.

A stationary active Play interval of 61.592 seconds had unchanged environment
redraw counters and unchanged two equipment Tiles/two Sprites, 8064 runtime
texture bytes. See `incidental-items-idle-before/after.json`. This does not prove
FPS, a speedup, all resource usage or physical Deck performance. Console counts
were zero errors and two existing memoryless-depth warnings.

## In-phase self-review and corrections

- 🟡 Fixed: direct public `CellsRaw` mutation after canonical two-cell registration
  left world art rejected but exact-reader native art admitted. Two RED cases
  reproduced it; the scoped `IsFootprintCurrent` guard fixes both. The initial
  late-`AddPart` hypothesis was false: that API refreshes registration. Positive
  controls explicitly preserve late AddPart and ordinary portable equipment.
- 🟡 Corrected audit state: final cleanup revealed LastGameID had changed from
  the protected original `eac9a734ed804db5b3c222015048356e` to a new unrelated
  test-time `3e117a97febb4d298f2ac5517826d678`. Its metadata timestamp was
  12:53:25, shortly after the footprint RED run; runner Play resumption is
  suspected but not proven. Original save and backups remained byte-identical.
  With Unity stopped and isolation cleared, restored only the verified original
  preference. Preserved the additional save and initial failed-restoration
  receipt; `incidental-items-last-game-restoration.json` and
  `incidental-items-final-restored-corrected.json` record the correction.
  Future native test invocations must protect ordinary preferences/save root
  across delayed runner resumption, then stop before ending isolation.
- 🔵 No accepted actor, floor, layout, long sword or gameplay art was redrawn.
- 🧪 Full opening composition, seed65 north/lifecycle, packages and physical Deck
  remain open; neither 147 passes nor these two identities closes those gates.

## Preservation and next pass

`incidental-items-preservation.json` rehashes original save/backups, the current
Linux archive, and the two unrelated notice PNG variants unchanged. Unity is
stopped, ordinary preferences restored, save override cleared, original scene
and borrowed view restored. The extra audit-time save is retained. Read-only
storage audit found no worthwhile safely stale candidate; zero bytes removed.
Current packages still use source9ffd7c973 and predate this and recent inventory,
Nella, cellar and hotbar work. No packaged or physical Deck acceptance is implied.

Next: continue the earned seed65 Warden from cellar47,19 through actual stairs,
glade, north close service/forage approach, and Nella lifecycle. Verify loaded
zone provenance before calling any visit cold. Preserve accepted art unless
whole-scene inspection reveals a named defect. Keep the separate paid seed65
Breaker save distinct; its rights and items are not the Warden's.
