# Ordinary resin reaches Posy's bench; ascent must not also collect stock

Source baseline70e9e6c08. This increment qualifies an existing ordinary supply
path and corrects an input collision found while separating fixture stock from
earned material. Full-machine acceptance, complete ordinary starting economy,
crop regrowth and the proofing flask destination cap remain separate.

## Source-informed supply route

Continue the earned quartz/Posy checkpoint1140, HP400/regard13. It carries six
fixture resin2167 and four fixture Pitchpod seeds2301; none of that is relabeled
naturally acquired. The current pack has no camp box, so temporary ground custody
is used instead of claiming container storage.

Read-only BiomeCropPlan allocation identified seed64 Pitchpod sites4,9 and14,10;
this is a source-informed itinerary, not blind in-game discovery. The plan does
not instantiate zones or guarantee an old cached zone has a crop. Native travel
actually reaches4,9 at1190 and finds mature PitchpodCrop12313 at20,14 and
unripe12314 at18,14. No ad hoc spawn, grant, teleport or clock jump is used.

The first attempt to separate fixture resin dropped2167x6 at Posy39,14 and
confirmed carried0. Shift+Comma then unintentionally picked it back up before
ascending. Thus the stock was not left at Posy; the extra action and carried6 on
arrival are retained as evidence of the input defect below.

At4,9, drop fixture2167x6 at40,12 and walk north to40,11. Readback1200 confirms
pack has no resin and the exact six remain on the ground. Ordinary walking
reaches21,13 at1410. C southwest offers harvest, Gather into pack and seeds-only
harvest. Selecting Gather1410→1420 removes mature12313 and gives earned
PitchpodResin12315x2 directly to the pack; seed2301 grows4→5 by compatible merge.
The original Grass11278/prepared bed and finite patch record remain. Unripe12314
stays stage0; fixture2167x6 stays at40,12. No loose output remains at the harvest
cell. This does not prove replanting, watering or a complete regrowth cycle.

F5/stop/fresh Continue1420 retains earned2, seed5, separate ground fixture6 and
only the unripe crop. Archive
`/private/tmp/coo-resin-gathered-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.
An immediate restart request during shutdown was refused before creating a new
isolation; the settled retry supplies the fresh-load evidence.

Ordinary ascent from21,13 (no underfoot goods), map step east and descent return
to Posy; walking south reaches the bench at1490. Native row20 lacquers plain
WardedCloak2342 for one earned resin1490→1500:12315x2→1, target now lacquered.
The spare is unworn, so its ArmorPart.AV remains0 until equipped. No fixture
resin was retrieved or available in the direct pack during payment. Archive
`/private/tmp/coo-resin-spent-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.

## Input defect and bounded correction

InputHandler's pickup branch accepts G or Comma, without excluding Shift; its
following branch recognizes Shift+Comma as ascent. A single ascent chord can
therefore collect underfoot goods and charge pickup before travel. With several
items it can also leave a pickup modal active. This makes material custody and
travel costs surprising, and occurred in the actual itinerary above.

Independent source critique approves the smallest correction: only bare comma
enters the comma pickup branch; either Shift+Comma is reserved for ascent.
G, including Shift+G, remains pickup. No stair/travel/ownership/quantity rules or
controller mappings change. Native RED tests exercise actual InputHandler.Update
with both Shift keys, blocked ascent, multiple goods and ordinary pickup controls.
The correction gates only the comma alternative on neither Shift held. Initial
test compilation failed on two inaccessible internal Entity.SpatialZone accesses
(six compiler log entries); that attempt proves no RED. Tests now use public
Zone.GetEntityCell. Valid RED-b compiles cleanly:10 cases,6 expected failures
(two pickup, two refused-travel side effects, two stuck pickup modals),4 passing
ordinary pickup controls. GREEN128/128 covers all ten plus existing native
controller routing/adversarial and world-map traversal cases,0 compiler errors.
Source/test review found no P1/P2. Post-fix play/reload verification follows.

Fresh Continue1500 retains earned12315x1, seed2301x5 and lacquered2342. Ordinary
travel revisits4,9 at1540/40,12, standing directly over fixture2167x6. The mature
crop is still absent, Grass11278 still has its prepared/plantable soil, and the
second crop is stage0. Native LeftShift+Comma1540→1550 now ascends once, leaves
the six underfoot, keeps only earned12315x1 carried and leaves input Normal with
no pickup modal. A read-only check of that already visited source zone confirms
the exact old stack remains at40,12. Descent returns at1560 with all custody
unchanged. One transient observer call returned no result; it supplied no evidence
and the successful readbacks above, not a repeated gameplay action, resolved it.

F5/stop/fresh Continue1560 retains separate ground2167x6/carried12315x1, seed5,
HP400/regard13, lacquered2342, spent mature crop and prepared bed. Archive
`/private/tmp/coo-resin-ascent-fixed-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.
This actual check covers LeftShift ascent over one stack; RightShift, blocked
ascent, multiple-item modal exclusion and ordinary G/comma controls are native
EditMode tests, not extra hand-play claims. Ordinary resin now reaches a useful
town service without drawing on the isolated fixture supply.

## Limits

The player uses fixture-provided equipment and previously earned faction regard.
Ordinary crop acquisition and its finite bench expenditure are the new supply
evidence. Seed totals include four fixture seeds plus one earned seed. Loose
fixture ground custody is checked explicitly, not described as protected storage.
No new lore, graphics assets, FPS/performance or population-survival claim.
Lore framing remains Lore/Factions/05_BowerFolk.md and material-culture sources.

Main stopped/rootnull; owned keyboard/callbacks/settings restored. Fresh84
foreign tracked hashes/861 normal save and preference files remain unchanged,
excluding Unity analytics/TestResults. No whole-directory or whole-plist
byte-exact claim. Current Mac477 predates both the shared expiry-feedback fix and
this chord correction; batch these with the next coherent tool milestone.

## Independent review

Independent bounded source critiques checked ordinary crop/ground custody and
approved the ascent guard before implementation. Source/test review and final
five-image/observation/native-result review found zero P1/P2 or wording blockers.
The reviewer confirmed separate fixture and earned quantities, finite payment,
fresh exact state and the corrected one-action ascent. This is independent
source/gameplay-evidence review, not a second native keyboard operator.
