# Plot product uses — independent actual-play review

2026-10-24. Read-only review of the `plot-products-uses-*` evidence available at this checkpoint:22 actual PNGs plus paired state receipts and launch receipt. No Unity calls, tests, code/repository changes, gameplay actions or staging. Representative images inspected directly: sludge-carried, seed-ground, gladroot-ground/carried, fireclay-reader, coating-reader, mature, seed-planted and resin-brew-preview. All22 PNGs are1280×800.

**No actionable P1/P2 graphical mismatch was found.** The native sprites remain crisp and distinct, actual popup titles match their examined products, and text/footer bounds are readable. FireClay's first page is visibly paginated1/2, rather than clipping its remaining content. The sparse plot world is coherent with its accepted current scope; this is not acceptance of a finished whole-world scene treatment.

## Resource and state reconciliation

| Action | Observed result |
|---|---|
| Purchase | Nella/trade→bought-sludge at tick1030:42→36drams; sludge0→2. No additional trade turn is asserted. |
| Compost Pitchpod | offered1040→composted1050:sludge2→1; crop24→18ticks/stage; Composted false→true; still stage0/progress0/moisture0. |
| Compost Marlroot | both-composted1060:sludge1→0; Marlroot26→20ticks/stage and Composted=true; no water supplied. |
| Rain and growth | rain1070:moisture39/progress1; sprouting1270:moisture19, both reserve crops stage1; mature1460:both stage2/moisture0. Public gladroot crops are gone and four CandyCarrot owners occupy their two beds. |
| Gladroot consumption | carried1480→eaten1490:count2→1. HP stays40; screenshot shows40/40. This proves consumption at full health, not a healing gain. Two other roots remain in the other public bed. |
| Marlroot harvest/replant | harvested1510:old crop absent; two clods plus one seed on17,6. seed-planted1530:new stage0 crop at17,6, dry/uncomposted/default26ticks per stage; two clods carried, seed absent. Recorded fields do not include entity IDs. |
| Clod preparation | seed-planted1530→fireclay-prepared1540:clods2→1, FireClay0→1. |
| Resin brew | preview→brewed:resin2→1, BrewedTonic0→1. Preview, brewed and coating-reader all remain tick1540; do not claim this brew cost a turn. The preview/reader pixels establish the specific **burning coating** identity; inventory JSON alone records only BrewedTonic. |

The coin/forage/still route is not covered by this plot pass. The generated BrewedTonic portrait gives bounded ordinary evidence for that one works-product frame, not the other six products or their complete use paths.

## Actual portrait verification

Compared every opaque source pixel, replicated at its displayed integer scale, to each screenshot's foreground RGB with zero tolerance. All six observed portraits match exactly:

| Capture | Source atlas/frame | Scale | Entire16×24 frame origin | Opaque screen pixels |
|---|---|---:|---|---:|
| sludge-carried | plot-products/7 |4×|(936,236)|608|
| seed-ground (MarlrootSeed) | plot-products/4 |3×|(687,206)|153|
| gladroot-ground | plot-products/0 |3×|(687,206)|441|
| gladroot-carried | plot-products/0 |4×|(936,236)|784|
| fireclay-reader | opening-items/9 |4×|(936,94)|656|
| coating-reader | north-works-products/5 |4×|(936,148)|1040|

Source paths are under `ArtSource/Live16x24/{plot-products,items,north-works-products}`. These comparisons verify the displayed opaque palette/geometry only, not whole-render performance or every art state. The coating's neutral opaque bottle is the intended shared native vessel and does not promise safe contents.

## Explicit evidence limits

- **seed-planted is not a carried-seed reader.** Its image shows inventory after the ordinary planting shortcut, and the receipt establishes the new crop. No current receipt contains a carried MarlrootSeed snapshot; its exact ground portrait is accepted, its carried portrait remains unobserved.
- The ordinary reader for burning coating visibly offers drink/throw and describes burning. The pass proves brewing and examination; it does not prove throwing, drinking, application, safety, enemy use or damage. No gameplay change is proposed here.
- The JSONs lack exact entity identities and detailed brew effect/form fields; use pixels for the specific brew name and do not claim byte-identical object graphs from these rows.
- Other gameplay continues during growth: the log records a magpie killed by a scrabbler. Therefore unrelated whole-zone owner changes are expected; do not assert that the entire world stayed unchanged while waiting.
- This is new ordinary-use/visual acceptance of the named route. No runtime changes or native-suite reruns occurred in this pass. Save/reload and final editor cleanup will need their own later receipts; they are not inferred from these22 captures or historical cleanup files.

## Final save/load addendum — later evidence, 2026-10-24

Independently read `plot-products-uses-saved.json`, `-reloaded.json` and `-save-comparison.json`, and directly viewed `-reloaded.png`. The reloaded image shows the actual flat plot world, player/FOCUS at (18,7), and the visible **Game loaded.** log. No new graphical defect was observed.

Direct comparison confirms both saved/reloaded receipts contain seed64, tick1550,36drams, Overworld.11.8.0, flat=true, player(18,7),HP40. All **11 inventory rows** compare equal, and all **198 recorded owner rows** compare equal as canonical multisets. This independently agrees with every true result and the explicit limited scope in `-save-comparison.json`. It verifies these observed fields, not every serialized value, exact object identities, other zones, or the save file's byte identity.

The later capture schema now records `displayName`, `brewForm` and `brewEffects`. The read-only capture source `/tmp/coo-plot-uses-capture-template.cs:12` reads the actual `GetDisplayName()`, `BrewItemPart.Form` and `BrewItemPart.EffectsRaw`; the latter's Name:Potency grammar and fields are defined in `Assets/Scripts/Gameplay/Alchemy/BrewItemPart.cs:14–42`. Both final receipts preserve **BrewedTonic×1, displayName `burning coating`, Form `Coating`, EffectsRaw `Burning:1`**. Thus the earlier limitation about JSON not recording the specific brew identity applies to the original action captures only; the final enriched receipts now corroborate the reader pixels and persistence.

The final tick1550 follows later movement/save preparation and does not change the original brew's recorded1540→1540 observation. The carried MarlrootSeed portrait remains unobserved. Drinking/throwing the coating remains untested. Editor/isolation cleanup still needs its own receipt; save/load success does not establish cleanup.

## Subsequent scope check

The independent reviewer also read the final living-record section and confirmed
`ordinary_bounded_accepted` matches the original packet gate (ordinary reachable
harvest/pickup/examine, acquired-seed planting and clod preparation). It does not
require every world/carried permutation. The preserved limits remain necessary.
`works_products=ordinary_partial` is supported only for the one observed flask.
No significant wording/claim issue was reported.
