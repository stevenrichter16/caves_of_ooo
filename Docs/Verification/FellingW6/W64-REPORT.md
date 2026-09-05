# W6.4 — Olderdeep and the Rooted

Status: W6.4 complete. Full suite **7470/7470 GREEN**, zero C# errors,
2026-09-05 18:35:30–18:36:28 UTC. W6.3b baseline 7394 (+76 tests); earlier
spell work remains protected and outside this phase's staging.

## Implemented behavior

Olderdeep's FoundingVillage profile authors a larger, roughly oval chamber and
compact annex. Both repeated plume and niche-home families have four visual variants. The Rooted kneels in the western focus with arched torso and
open arms toward an explicit eastern wall. Eleven walkable plume tiles grow
from his torso side. The six-cell gap before the wall remains bare and reserved;
fixed villagers and wall-backed niches sit outside it. Existing stair entities
and their coordinates survive; their approaches go around the gap.

The plume is the founding hearth. Its actual damage route carries the shared
CatacombFolk war rule, as does the protected body. The other catacomb villages
retain their existing hearths, people and generation. Fresh and rehydrated maps
receive the authored profile; already cached floors preserve player changes.

The founding-wall tender accepts one harvested Tepuibone chip as a one-time
plaque-tending service, giving +50 reputation from a neutral start. This is a
CoO-original content inference from the canon's name-holding stone and tending
customs. Current inventory, reach, standing and the tender's unmasked consent
are rechecked on execution. Only a successful stack-aware exchange sets the
saved completion fact; old cached dialogue choices cannot spend a missing item.

A trusted living player must stand on the exact plume selected. Sleeping uses
RestSystem: full healing, Bleeding cure, nearby-hostile refusal and exactly 60
world-clock ticks. It preserves player energy and waiting input. The first
sleep records RootedMet and an authored dream of contentment; later sleeps do
not duplicate the first-meeting event. A saved expiry property supports two
weeks of patch-bloom recognition in Listening dialogue, without per-turn scans.

`c`, then `.`/Keypad5 opens underfoot interaction. A marked-terrain picker keeps
ordinary player/loot target priority intact. Selected world-action letter keys
must be released before becoming normal movement; this fixes native S selecting
Sleep and then taking an unintended step after the menu closes.

## Evidence

| Gate | Evidence |
| --- | --- |
| Authoring | 9 actual RED →9 GREEN (`W64-authoring-*.xml.gz`) |
| Rest / trust | 24 actual RED →33 GREEN with authoring controls (`W64-rest-trust-*.xml.gz`) |
| Dedicated adversarial gate | 34 final cases; initial31 cases had17 RED; all geometry, dead-player, clock, profile and two-stair fixes pass in the full suite |
| Consent cold-eye | 68-case checkpoint:66 pass, exactly2 new REDs for -1 reputation UI and guest-protection consent laundering (`W64-review-68.xml.gz`) |
| Scenario | Missing-class compile RED, then focused70/70 GREEN (`W64-focused-70.xml.gz`) |
| Router and two-stair regressions | Opposite display-name archetypes2/2 RED, legal two-stair layout1/1 RED before fixes (`W64-profile-review-red-2.xml.gz`, `W64-two-stair-red-1.xml.gz`) |
| Native gameplay | Run `30e595e630c149b59348e356239a6c3d`:8/8 PASS, zero failures, zero C# errors, launcher exit0 (`W64-native-audit.json`) |
| Native failure control | Held-S key leaked into movement; run rejected with exit1, archived gameplay-only lines (`W64-native-held-key-red.txt`) |
| Full suite | **7470/7470 GREEN**, zero C# errors (`W64-final-full-green.xml.gz`). Prior full run hit only the pre-recorded fungal self-cloud flake (`W64-full-suite.xml.gz`). |
| Art | Ten original 16×16 sprites, binary alpha, shared outline; GUID-only template changes; global collision audit (`W64-art-audit.json`) |

Raw XML is gzip-compressed without rewriting its contents. Compiler failures
are checked before XML; no stale results count as evidence. Two fixture API
mistakes (RemoveTag versus Tags.Remove and AddPersonalEnemy versus
SetPersonallyHostile) were corrected before accepting assertion REDs.

## Cold-eye findings resolved

- 🟡 Reliance on random cave walls: author an explicit wall and complete shell.
- 🟡 Plume intruded into the sacred gap: keep it west/on the body column.
- 🟡 Freestanding niches: place against actual wall cells.
- 🟡 Broad stair-collision rejection: protect the actual footprint and try
  additional valid center rows; legal two-stair floors remain buildable.
- 🟡 Display-name routing could ignore the profile: explicit Founding profile
  selects the settlement floor before the name-based fallback.
- 🟡 Liked trust had no earning path: add the one-time real material service.
- 🟡 Visible permission and execution differed at raw-1: share one predicate.
- 🟡 Guest safety masked personal enmity: use the established unfloored consent
  contract, with protected/unprotected hostile controls.
- 🟡 Dead player could invoke rest directly: living-player guard before healing.
- 🟡 Native menu shortcut became movement: release latch, verified with the
  same native pulse duration that originally failed.
- 🔵 Failures are observable through worldgen/furniture/mineral diagnostics;
  ordinary missing stone and untrusted rest also give actionable messages.

## Honesty bounds and scope

**Can verify (script-observable):** actual world routes, explicit wall/gap and
stair connectivity, imported sprite resources, graph-save persistence, consent
and inventory transaction boundaries, dream facts/clock expiry, and real native
keyboard menu selection. The eight native rows cover untrusted refusal, one
stone earned trust, once-only eligibility, exact successful rest, meeting/bloom,
repeat sleep, cancellation and stale adjacent-target refusal.

The native arena is synthetic, staged after the real bootstrap. It does not
prove travelling to Olderdeep or harvesting the stone; separate world/harvest
and save tests cover those surfaces. Its once-only row queries CanOffer;
repeated actual exchanges and stale cached-choice execution are covered in
EditMode. No performance claim comes from this short encounter run. The new
mechanics perform cold interaction/generation work and add no per-turn scan.

**Cannot verify (visual / feel):** in-motion pose readability, perceived room
scale, sound, brightness falloff or atmosphere. The ten-sprite contact sheet was inspected;
the attempted headless ScreenCapture produced no file, so no native screenshot
or visual-playtest pass is claimed. The manual scenario remains available in
Caves of Ooo → Scenarios → World → Olderdeep Founding Encounter.

The sacred gap is culturally forbidden, physically open space. Reservations
prevent generated clutter, not runtime movement; a player or wandering creature
can cross it. No invisible wall or new AI law is implied. No ending, Staking,
full god conversation, global dream-light simulation or public mortal-name
reveal ships in W6.4. The existing tier/source conflict and other recorded debts
remain in the phase plan. No positional save-format change is introduced.

The previously recorded protected spell-camera teardown exception can still
occur after native auditing when Unity exits. It is outside this phase's edits
and remains queued for the user's authorized whole-game audit. Native exit0
certifies the eight completed gameplay rows, not exception-free teardown.
