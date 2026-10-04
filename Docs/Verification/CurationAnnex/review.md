# Curation annex: independent source review

Reviewed 2026-10-03 against `Docs/CURATION-ANNEX.md`, the final-on-disk composition and receiving builders, the seven new blueprints, ordinary door/repair/container/save behavior, `CurationIntakePart`, `BotanicalInkDeskPart`, and authoritative `Lore/Factions/03_PaleCuration.md`. This review changes no gameplay code. Native test and input results remain separately reported by the coordinator.

## Q1 — Symmetry and lifecycle

Opening and closing use the same existing `DoorPart` authority. The three new unlocked doors have `Physics.Solid=false`; their closed state supplies obstruction, so reopening cannot leave an invisible physical wall. The repairable service gate starts open, refuses operation while broken, and keeps its actual open/closed state after repair. Repair spends the existing two-timber recipe; it neither teleports nor damages the occupant.

The new goods follow the existing finite-container lifecycle. Bounded stacks are created only during fresh generation, stored under their actual container owner, and included in the existing uniqueness/ownership checks. Replay refuses before any refill. Saved repair, gate, inventory and entity-position state use existing Parts; the configured receiving index retains the same named zone even after depletion or staff death.

## Q2 — Cross-system consistency

The gallery, holding room and service passage have genuine sandstone partitions. These block movement, ordinary nonseeping gas diffusion and AI sight; the retained rail is an observation fitting, not a claimed sealed barrier. New closed doors participate in the builder's reachability calculation even though their Physics field is false. The only deliberately newly unreachable walkable area is the occupied gallery; public routes and the initially empty holding room remain accessible.

Public cargo, quoted bays, courier, four-item certification cabinet, two staff and the optional connected ink desk retain their original placement/identity contracts. Ivrin's survival and presence continue to govern the existing ink service. No annex-completed flag, magical return reward, automatic reputation change, custom capture action or refill system was added.

## Q3 — Counter-checks and evidence bounds

The dedicated generation fixture covers three declared seeds, reachable maintenance/holding versus unreachable occupied gallery, real masonry screening, unavailable repair without payment, paid repair, occupied threshold refusal, repeat repair refusal, finite source depletion, exact prior-owner preservation, replay refusal, missing new owners and malformed initially open containment gates. A native save round trip checks the repaired closed gate, same persistent enemy identity, retained HP/position and depleted stock after explicit unload/revisit.

**The unit geometry fixture manually moves the same enemy into the holding room.** That test proves enclosure and persistence, not successful play or luring. The separate native-input route must prove actual movement, pursuit, gate interaction and time costs. This source review does not claim that route has passed. It also does not prove balance across every build, spontaneous discovery or visual readability.

## Q4 — Design and lore consistency

The on-disk layout matches the planned 27×9 wing at Y15, vestibule opening at relative X4–6, gallery X10–19/Y1–5, holding X21–25/Y1–5 and screened service passage Y7. The four apertures match (9,2), (20,3), (15,6), (23,6). The finite recovery stock is two sootroot pulp, one pitchpod resin, two fire clay, one imported Sodden field dressing and one pair of leather gloves. The safe maintenance rack holds two timber lengths.

The half-set remains the original 14-HP Bloom-driven intake; ordinary salt-cured subjects stay inert. Closing doors contains the growth without restoring the deceased person, endorsing involuntary living preservation or making mainline Curation into the expelled Catchers. Imported emergency supplies fit the documented Concord logistics relationship. This is original authored exploration, not a claim of copied Qud mechanics or source parity.

## Review disposition

- No additional gameplay defect found in the reviewed final source. Native acceptance and the affected test sweep are reported separately in the living doc.
- Fixed integration finding: a possible C# declaration-scope collision between the new local `door` and the existing `DoorPart door` pattern was reported to the coordinator during its active test window. The coordinator renamed the new local `annexDoor` on disk. This is an integration/compiler correction, not a change to the interaction design.
- Accepted limitation: already-cached saved Marrowstye layouts remain literal. Fresh generation receives the expanded annex.

## Native scenario preflight review

The native test route was independently reviewed before execution. The reviewed candidate reads the actual placard (not merely its examine summary), routes around closed doors, chooses a currently clear side of the transfer gate for closure, and reports the actual bounded wait count. This changes observation/navigation of the test actor, not gameplay rules or the subject. Rubble on the east escape route is ordinary passable terrain. A subject beyond sight must be approached through ordinary movement; the route may fail honestly instead of setting its target or position.

The first live run exposed one test-route defect: the older generic Curation approach selected the inner frontage of the inspection gate and the actor-aware pathfinder used the earned key to open it. The closed-boundary check correctly failed. Curation approaches now choose genuinely reachable frontage using paths that treat closed doors as barriers; subsequent walking uses the same restriction. No gameplay door or pathfinding rule was changed. Raw failed run is retained.

The second live run passed public containment and paid repair but its distant wait did not attract the roaming subject. The route now visibly approaches through the opened transfer aperture until ordinary AI notices the player, then retreats. No sight radius, wandering behavior, target or enemy position is changed. This is part of the actual spatial solution rather than a scripted attraction effect.

The third live run achieved ordinary pursuit into holding and closed the transfer gate, but the test's long escape around the east wall let the subject cut diagonally into the service threshold. The real door correctly refused closure. The test now favors the southern standing place beside the transfer gate and takes the direct service exit, keeping pursuit behind the player. It still refuses an occupied threshold; no creature speed, position or door rule was changed.

## Final native containment acceptance

Run `a1048617f69448e6b6d69946c880cf32` passed all 20 checks with no unexpected errors. Actual pursuit, paid repair, the two closures, finite stock, twelve further scheduled waits and F5/F6 graph replacement all passed. The subject retained 14 HP and its identity; the player had 37/40 HP. Screenshots of closed holding and restored aftermath were inspected. This closes the real-lure acceptance gap; it does not claim multiple-build balance or natural player discovery. The separate unit fixture covers explicit zone unload/revisit; the native route covers quicksave/quickload.

## Optional combat and accepted visual limit

Run `ebc20e73454f43a18b3794fa8b433bd6` passed 20 of 21 checks with zero unexpected errors. Gameplay passed: keyed entry, actual hostile HitRoll, ordinary player damage and death/removal, and survival. The subject first missed the player; one original Rime Grip and one earned-rake attack killed it, leaving the ordinary player at 40/40 HP. Only the attack-pose capture assertion failed. The prior receiving-yard living document already records the same final-run limitation; no animation production code changed here. The raw result remains incomplete rather than being relabelled green. Following the user's severity guidance, this visual measurement issue is deferred instead of retrying until a favorable sample or expanding the content task into animation infrastructure.

The earlier optional-combat route (`8d96b39567754ae68964c860ddd8ca74`) waited outside the enlarged gallery beyond the subject's sight. It was corrected to walk toward the real subject before observing its first attack. This changed native test navigation only.
