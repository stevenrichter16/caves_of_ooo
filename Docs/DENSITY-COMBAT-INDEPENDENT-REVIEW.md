# C2 independent review

Reviewed 2026-09-26 against the live uncommitted C2 changes after the C1 loadout
integration. This is an independent read-only review; the reviewer did not author
C2, change production, run Unity, or substitute this review for native evidence.

**Result:** no concrete P0–P2 production defect found in the inspected scope.
Ordinary-stat scheduler/encounter acceptance remains pending with the combat
owner. Existing synthetic 500-HP fixtures establish mechanic behavior, not balance.

## Inspected evidence

- `CombatTacticsPart`: six explicitly supported skill classes; NPC-only creation;
  existing-command collision rejection; real Skills/ActivatedAbilities ownership;
  usable cooldown and matching registered command; equipped weapon class; exact
  hostile target and zone; clamped chance; deterministic injected randomness;
  actual command dispatch and handled-result fallback.
- `KillGoal`: tactic dispatch precedes adjacent attack/movement and returns after
  a successful cast, avoiding a second ordinary action. Declined tactics use the
  ordinary fallback. Authored actors cannot bypass audited target previews through
  the old unaudited ranged helper.
- `ShortBlades_Shank`, `LongBlades_Lunge`, `Axe_Berserk`, the elemental powers and
  `SkillLine`/`LineTargeting`: preview uses the runtime's adjacent-first or actual
  line-first target semantics, including targetable scenery and footprint contact.
  A nearer ally/creature prevents a cast aimed through it. Natural weapons alone
  do not satisfy the equipped-class requirement. Berserk consumes its own action.
- `BrainPart.SetPersonallyHostile` and `BoredGoal`: explicit and first proactive
  hostility both reach the local assistance hook. Receivers must opt in, match the
  faction, remain idle/alive/nonpassive, see caller and threat, respect radius and
  sight, and remain outside party ownership. Receiver delivery disables relay,
  preventing chain recruitment across the zone.
- Current content: Scavenger's dagger/shortsword alternatives, Hunter's spear,
  bandits' actual shortswords, chieftain's longsword and warlord's real cleaver
  are compatible with the authored skill classes. IceWight enters only depth
  groups tier2+, with exactly one individual when that group row is selected.
- `DensityCombatTacticsTests`, `DensityCombatContentTests`, and
  `DensityCombatAdversarialTests`: successful powers/cooldowns, chance controls,
  blocked/off-ray paths, movement fallback, registration corruption/collision,
  target/zone boundaries, all eight rays, footprint contact, one-action behavior,
  assistance boundaries/nonrelay, diagnostic enable/disable and saved owner/
  cooldown persistence. These were read; their execution results are owned by
  the combat/native receipts, not asserted here.

## Q1–Q4

1. **Symmetry:** successful ability returns immediately; refused/no candidate/
   chance/cooldown paths preserve fallback. Personal and proactive assistance use
   the same delivery guards. Saved skills retain existing ownership/cooldown;
   loading old actors does not invent a newly authored kit.
2. **Consistency:** grant/use diagnostics share command names. Refusals identify
   missing context, invalid registration, cooldown, equipment class, range/ray,
   chance and command veto. Assistance emits a bounded count and has receiver
   refusal reasons. Existing global equipment/death contracts remain in force.
3. **Counter-checks:** zero/100/intermediate chances, removed/dead/neutral targets,
   no weapon versus actual equipped weapon, blocked versus removed blocker,
   caller/receiver opt-in, party/engaged/range/LOS refusal, save cooldown expiry,
   duplicate creation and command collision are represented. No new production
   branch was discovered that required a speculative additional test here.
4. **Claims:** safe-target mechanics are supported by preview and matching tests.
   This does not yet establish ordinary turn pacing, number of casts before
   fleeing/death, threat/reward balance, naturally generated encounter behavior,
   native telegraph readability, or how often a player encounters each authored
   roster member. The combat owner's ordinary-stat TurnManager scenario and
   root-owned native verification must supply those bounded observations.
