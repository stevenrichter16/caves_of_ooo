# Native quartz supply: a finite vein, Posy sign and temporary light

Supply/sign behavior baseline `47761ba77`, documentation baseline `a0f4c6ce9`.
Review found and this increment fixes shared natural-expiry feedback below;
acquisition/payment/lifetime mechanics are unchanged. No repeated full suite or
package build. Native
GameBootstrap input and normal readers/actions; bridge code launches isolated
saves, supplies keyboard input and reads state. No ad hoc grants, teleports or
time jumps. Canonical Bower-Folk framing comes from Lore/Factions/05_BowerFolk.md
and Lore/History/08_MaterialCulture.md. Quartz is material for two particular
uses, not a universal battery.

## Ordinary acquisition from the world

Continue the earned workshop/Posy checkpoint at tick390, HP400, regard8, resin6,
with no carried quartz. The scenario supplied armor, tools, resin and seeds;
this is not ordinary acquisition of that starting equipment. The quartz below
comes from a normal generated world source, not the scenario loadout.

Ascend and travel from Posy5,9 to Spread11,10; descend at530/40,12. The canonical
glade has finite GlowQuartzVein9366 at31,10 (yield1–2) and another at31,22, both
explored. Initial short input route ended34,10; do not infer all requested keys
produced movement. The subsequent ordinary route via y9 reaches32,10 at650.
C west offers harvest. Enter650→660 produces GlowQuartz10011x2 in the pack and
removes vein9366, leaving the original Grass8139. No quartz drops remain on the
ground. Reopening the source cell has no harvest row.

F5, stop and fresh Continue660 retain the two minerals and depleted source.
Archive `/private/tmp/coo-quartz-mined-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.
Source: ReferenceGladePlan/Builder and HarvestablePart; this is one actually
harvested vein, not acceptance of every mineral source or harvest failure case.

## Transport to a different consumer

Ordinary world-map travel returns to Posy at800/39,11; walking south reaches
39,14 beside SettingBench7298 at39,15 at830. Its late row41 offers the Bower sign
on the already lacquered spare WardedCloak1a3e508a378e4552bb657b91edaeea1a.
Native selection830→840 consumes quartz2→1 and adds exactly one sign to that
same garment; resin stays6. The sign is conditional while carried: regard8.

The actual inventory reader shows both lacquer and the conditional +5 Bower-Folk
regard. Equip(auto) first opens a replacement preview; accepting it replaces the
old coated cloak. Regard becomes13; the signed garment's ArmorPart.AV is1.
Cold35 falls to0 because the old cold-coated cloak is now unworn. Removing the
signed cloak returns regard8; re-equipping returns13, without accumulating the
bonus. These ordinary equipment operations do not advance tick840 under the
existing rules. The observer's actor stat named `AV` is not the derived armor
total; use the visible inventory/HUD AV1 and the exact garment ArmorPart above.

Reopening the bench and attempting the already-signed worn garment at row43
refuses freely: tick840/quartz1/regard13 unchanged. Source: SettingBenchPart and
EnhancementEngraved. This closes successful sign work with naturally acquired
quartz and an existing lacquer treatment. It does not qualify higher tiers,
NPC prices, every reputation interaction or ordinary resin supply.

## Alternative expenditure outside the bench

The remaining mineral offers a native C north row to crack a quartz light on
empty ground39,13. Selection840→850 consumes the last quartz and places
CrackedGlowQuartz10012: enabled, radius4, non-takeable,29 turns remaining after
the placement action. The menu and reader preview thirty world turns and no
recovery; the creation turn consumes the first of those turns. The reader is
static descriptive text, not a live remaining-time display. Posy is already lit:
this proves deployment/finite lifetime and visible object/reader, not a measured
increase in visibility in a dark cave or a creature lure.

F5, stop, fresh Continue850 retains exactly the same light/29 turns and signed
worn cloak/regard13, with zero quartz. Archive
`/private/tmp/coo-quartz-signed-lit-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.
Twenty-eight normal paid Period waits850→1130 leave one turn; one more wait
reaches1140 and removes the light. HP400/regard13 stay unchanged. F5, stop and
fresh Continue1140 retain zero quartz/no light/the same signed worn cloak and
regard13. Expiry does not refund minerals or reset on reload. Archive
`/private/tmp/coo-quartz-spent-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.
Source: EquipmentUtilityActions and LifespanPart. No unloaded-zone expiry claim.

## Expiry feedback defect found during review

Independent review caught a material limit in the first image pair: although the
owner was removed at1140, the stationary world view still showed its cyan shard.
The first `light-expired.png` is retained as the before-fix defect, not visual
expiry acceptance. Zone.RemoveEntity only publishes dirty cells for spatial
footprint owners; ordinary LifespanPart expiry sends no render invalidation.
The dirty-cell renderer also skips recomputing the lightmap.

Implemented correction: successful natural expiry of an enabled
light requests a full refresh once, so lighting and the object both disappear;
other expired props request only their former cell. Active and indefinite timers,
missing zones and already removed owners must not publish expiry feedback.
Existing lifetime, consumption, removal and aftermath rules stay authoritative.

Nine native RED cases compiled with zero errors: three expected feedback
failures and six passing timer/ownership countercases. Focused GREEN46/46 adds
one aftermath preservation control (ten new cases total) and retains steam
scheduling/material-reaction cases. Separate existing item-utility and adversarial
suite62/62 passes;108 distinct targeted passes total, no clean full-suite claim.
Native XML/logs retained compressed. No save fields or lifetime rules changed.

Actual post-fix replay starts from the same earned850/29-turn checkpoint. Twenty-
eight ordinary waits reach1130/one turn, then one wait1140 removes the source
AND its drawn shard while the player remains at39,14. `fixed-last-turn.png` and
`fixed-expired.png` show this change; other NPC motion/log differences remain
ordinary simulation, not exact whole-frame equivalence. A read at1140 sees only
Grass5672 in39,13. F5/stop/fresh Continue1140 retains zero quartz/light, HP400,
the exact signed/lacquered worn cloak and regard13 (`fixed-fresh.png`). Archive
`/private/tmp/coo-quartz-expiry-fixed-earned-20261122/8ab6a6384c2a44deb8b3a68303809d8f`.
The first post-compilation replay failed its bootstrap precondition (handler
world/player pointers absent) and contributes no gameplay evidence. It was
stopped/cleaned, imports settled synchronously, and the successful replay above
started afresh. No input/observer failure is counted as a gameplay result.

## Scope and preservation

Finite world mineral → two mutually competing uses, one across two locations
and one outside the machine. Existing ordinary carrying, torch use and other
equipment choices remain available. No new twelve-machine acceptance, complete
ordinary starting economy, NPC survival, FPS/hardware measurement or packaged
input qualification follows. Current Mac477 contains the acquisition/sign/light
mechanics but predates this expiry-feedback fix; batch a new package at the next
coherent runtime milestone.

Main stopped/rootnull; all owned keyboard devices/callbacks/settings restored.
Fresh comparison:84 inherited foreign tracked hashes and861 normal save/preference
files unchanged, excluding Unity analytics/TestResults. No whole-directory or
whole-preference-file byte-exact restoration claim. Nine screenshots and read-only
observations accompany this receipt. Ordinary resin supply and the known
proofing flask destination cap remain next work; exposure/NPC survival stays gated.

## Independent review

The initial independent artifact review found the stale shard; the bounded
design/source critique approved enabled-light full refresh and ordinary cell
refresh before implementation. Final source/test review found no P1/P2 issues.
Final artifact review independently inspected the three fixed images and parsed
the test files: zero remaining P1/P2,108 distinct passing test names without
overlap,0 compiler errors. It confirmed stationary visual removal/fresh absence,
finite costs, exact signed owner and restoration limits. This is independent
source/gameplay-evidence review, not a second keyboard operator.
