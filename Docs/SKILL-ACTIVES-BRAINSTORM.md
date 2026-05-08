# Active Ability Brainstorm — 30 unique mechanics across 10 trees

## Status

**Brainstorm draft.** No code yet. Each ability sketches the
mechanic + targeting + flavor + a one-line "what makes this unique
across the whole table." After review, candidates can be ported one
at a time using the WSP3.5 ActivatedAbility infrastructure proven
in WSP6/WSP7.

## The constraint

The user explicit asked: **3 actives per skill category, but each
ability must be MECHANICALLY UNIQUE across the whole table** — no
"FireBolt for fire / IceBolt for cold" copy-paste with the element
swapped. Every ability claims a distinct mechanic axis (movement,
control, terrain, summon, redirect, consume-stacks, parry, etc.).

The matrix below maps each ability to its primary mechanic axis so
duplication is visible at a glance.

## Categories (10 total)

- **4 weapon trees:** Cudgel, Axe, LongBlades, ShortBlades
- **5 magic trees:** Spellcraft, Pyromancy, Cryomancy, Galvanism, Corrosion
- **1 utility:** Acrobatics

## Mechanic-axis matrix

| # | Ability | Tree | Primary mechanic |
|---|---|---|---|
| 1 | Cudgel_ChargingStrike | Cudgel | Move N cells then strike (charge attack) |
| 2 | Cudgel_GroundPound | Cudgel | Self-AOE knockback (all 8 adjacent) |
| 3 | Cudgel_Disarm | Cudgel | Force-drop target's equipped weapon |
| 4 | Axe_Decapitate (active) | Axe | Single-target execute below HP threshold |
| 5 | Axe_Whirlwind | Axe | Self-AOE attack (all 8 adjacent at full damage) |
| 6 | Axe_RendArmor | Axe | Apply ShatterArmor stacks directly (armor-shred) |
| 7 | LongBlades_Lunge | LongBlades | Reach extension — strike target 2 cells away |
| 8 | LongBlades_Riposte | LongBlades | Reactive parry-attack (one-shot toggle) |
| 9 | LongBlades_EnGarde | LongBlades | Sustained toggle stance (+DV) |
| 10 | ShortBlades_Backstab | ShortBlades | Position-bonus damage (flanked target) |
| 11 | ShortBlades_Flurry | ShortBlades | Multi-strike (3 quick attacks) |
| 12 | ShortBlades_Disengage | ShortBlades | Mobility (move N cells, ignore opportunity attacks) |
| 13 | Acrobatics_Tumble | Acrobatics | Swap positions with adjacent creature |
| 14 | Acrobatics_Vault | Acrobatics | Leap over a 1-cell obstacle |
| 15 | Acrobatics_EvasiveRoll | Acrobatics | Cleanse one negative status on self |
| 16 | Spellcraft_Counterspell | Spellcraft | Reactive — interrupt enemy spell within N turns |
| 17 | Spellcraft_ArcaneSurge | Spellcraft | Self — reset all spell cooldowns to 0 |
| 18 | Spellcraft_LeyTap | Spellcraft | Drain HP for next-spell damage boost |
| 19 | Pyromancy_Pyroclasm | Pyromancy | Detonate target's Burning stacks → AOE explosion |
| 20 | Pyromancy_FireWalk | Pyromancy | Trail — leave Burning cell on every step |
| 21 | Pyromancy_HeartFlame | Pyromancy | Sacrifice — halve own HP for double-damage spell |
| 22 | Cryomancy_Hibernate | Cryomancy | Self-stasis (immobile + heal + immune Heat) |
| 23 | Cryomancy_Frostbind | Cryomancy | Single-target rooted (movement blocked) |
| 24 | Cryomancy_IceMirror | Cryomancy | Summon decoy clone for 1 turn |
| 25 | Galvanism_StormCaller | Galvanism | Zone-wide Wet (all cells in zone for N turns) |
| 26 | Galvanism_Overload | Galvanism | Chain through Wet/Electrified targets in line |
| 27 | Galvanism_LightningRod | Galvanism | Toggle — incoming Electric redirects to nearest enemy |
| 28 | Corrosion_AcidPool | Corrosion | Cell residue — 3-cell radius acid puddle |
| 29 | Corrosion_AlchemicCatalyst | Corrosion | Trigger any cross-element reaction at 2× radius |
| 30 | Corrosion_Liquefy | Corrosion | Single-target debuff — -50% movement, +100% Acid take |

**Uniqueness check:** every ability claims a distinct primary
mechanic. No two abilities share BOTH (a) target shape AND
(b) effect type. Where target shape overlaps (e.g. multiple
self-AOE), effect type differentiates (knockback vs damage vs
consume-stacks vs trail-residue).

---

## Per-tree details

### Cudgel — heavy bludgeoning, control

**Theme:** big swings, body-rocking impact, weapon-as-club.

#### 1. Cudgel_ChargingStrike

- **Targeting:** DirectionLine (player picks direction, charges 3-5
  cells then attacks the entity at endpoint)
- **Mechanic:** move actor up to `CHARGE_DISTANCE` (3) cells in
  the chosen direction. If a creature is at the endpoint or first
  blocked cell, attack with `CHARGE_DAMAGE_BONUS_PERCENT` (50)
  bonus damage.
- **Cooldown:** 30T
- **Per Qud:** mirrors `Cudgel_ChargingStrike`. The "charge then
  swing" mechanic is distinct from CoO's existing Slam (single-
  target adjacent push) and Conk (single-target stun).
- **Why unique:** the only ability where the actor MOVES
  themselves before attacking (Lunge extends reach without moving;
  Disengage moves but doesn't attack).

#### 2. Cudgel_GroundPound

- **Targeting:** SelfCentered (no targeting — fires around caster)
- **Mechanic:** all 8 adjacent creatures take `GROUND_POUND_DAMAGE`
  (weapon-base × 0.75) + `Stunned` for 1T + knockback 1 cell away
  from caster.
- **Cooldown:** 40T
- **Why unique:** the only self-AOE knockback in the table.
  Whirlwind does self-AOE damage but no knockback. Pyroclasm and
  Overload do AOE but consume stacks rather than radiate from self.

#### 3. Cudgel_Disarm

- **Targeting:** AdjacentCell
- **Mechanic:** force-drop the target's currently-equipped melee
  weapon (or primary item if it has one) onto the target's cell.
  Target spends 1T re-equipping next turn.
- **Cooldown:** 50T
- **Why unique:** the only ability that mutates an enemy's
  equipment binding (RendArmor mutates AV via ShatterArmor stacks
  — different layer; Disarm changes the equipment slot itself).

---

### Axe — chopping arcs, sustained damage

**Theme:** big cleaving swings, dismemberment, blood.

#### 4. Axe_Decapitate (active version)

- **Targeting:** AdjacentCell
- **Mechanic:** if target HP ≤ `EXECUTE_THRESHOLD_PERCENT` (25%) of
  max, instant-kill. Otherwise, deal `EXECUTE_FAILED_DAMAGE`
  (weapon-base × 0.5) and refund half cooldown.
- **Cooldown:** 60T
- **Per Qud:** Axe_Decapitate already exists in WSP6.18 as a
  marker-skill (modifies Dismember's candidate pool). This active
  version is a separate ability shipped under the same Decapitate
  umbrella — the marker handles "passive on-Dismember-proc target
  the head"; the active is the player-driven finisher.
- **Why unique:** the only execute-mechanic in the table. Phoenix
  isn't an active execute, it's a self-revive trigger.

#### 5. Axe_Whirlwind

- **Targeting:** SelfCentered
- **Mechanic:** spin-attack that hits ALL 8 adjacent creatures with
  full weapon damage (no knockback). Each hit rolls all the
  attacker's normal on-hit hooks (Cudgel_Bludgeon-style procs fire
  per target).
- **Cooldown:** 50T
- **Why unique:** the only self-AOE multi-target FULL-DAMAGE attack.
  GroundPound is reduced damage + knockback; Pyroclasm consumes
  stacks. Whirlwind is "I get N free swings."

#### 6. Axe_RendArmor

- **Targeting:** AdjacentCell
- **Mechanic:** apply `REND_ARMOR_STACKS` (3) of `ShatterArmorEffect`
  to the target directly (no chance roll, no weapon swing). Target's
  effective AV drops for the duration of the effect.
- **Cooldown:** 30T
- **Why unique:** the only ability that DIRECTLY applies armor-
  reduction stacks. Cudgel_ShatteringBlows is the passive variant
  (chance on hit); RendArmor is the guaranteed-on-cast active.
  Different from Disarm (equipment-slot mutation) — Rend is
  AV-stat reduction.

---

### LongBlades — duelist's blade, reach + reactive

**Theme:** elegant fencing, parry-and-thrust, stance-based.

#### 7. LongBlades_Lunge

- **Targeting:** Direction (player picks one of 8 directions)
- **Mechanic:** strike a target up to `LUNGE_RANGE` (2) cells away
  in the chosen direction. Actor doesn't move; the blade extends.
- **Cooldown:** 25T
- **Per Qud:** mirrors `LongBladesLunge` (CoO simplification — no
  stance dependency).
- **Why unique:** the only ability that extends ATTACK REACH without
  moving the actor. ChargingStrike moves the actor; Lunge extends
  the strike.

#### 8. LongBlades_Riposte

- **Targeting:** SelfCentered (toggle ON for next N turns)
- **Mechanic:** while toggled on, when an adjacent enemy melee-
  attacks the actor and misses, the actor immediately makes a free
  weapon swing at that enemy (one per Riposte activation; toggle
  auto-deactivates after firing).
- **Cooldown:** 30T (resets after the riposte fires OR the toggle
  duration expires)
- **Per Qud:** mirrors the dueling-stance riposte from
  `LongBladesCore`'s reactive-attack pattern.
- **Why unique:** the only **one-shot reactive** ability — fires on
  the first qualifying enemy action then deactivates. Different
  from EnGarde (sustained passive bonus) and from
  ShortBlades_Rejoinder (passive, fires every miss not a single
  charged shot).

#### 9. LongBlades_EnGarde

- **Targeting:** SelfCentered (toggle)
- **Mechanic:** while toggled on, +`EN_GARDE_DV_BONUS` (3) DV.
  Toggling off has no cooldown; toggling ON has a 5T cooldown
  (prevents flicker-toggle exploits during combat).
- **Cooldown:** 5T (on toggle ON only)
- **Per Qud:** the dueling/defensive-stance idea from Qud's
  `LongBladesCore` simplified to a single +DV toggle (no
  multi-stance system in CoO yet).
- **Why unique:** the only **sustained passive-buff toggle**.
  LightningRod (Galvanism) is also a toggle but reactive-redirect,
  not a passive stat. FireWalk (Pyromancy) is a sustained
  trail-leaving toggle.

---

### ShortBlades — speed, positioning, finesse

**Theme:** quick stabs, footwork, kidney-shanks.

#### 10. ShortBlades_Backstab

- **Targeting:** AdjacentCell
- **Mechanic:** if the target has another creature directly opposite
  the actor (i.e. target is "flanked"), the strike deals
  `BACKSTAB_DAMAGE_MULTIPLIER` (×2) damage. Otherwise: normal
  weapon damage with no bonus.
- **Cooldown:** 20T
- **Why unique:** the only ability that gates on **positional
  geometry** (flanking — actor + ally on opposite sides). Tumble
  (Acrobatics) lets you position for it.

#### 11. ShortBlades_Flurry

- **Targeting:** AdjacentCell
- **Mechanic:** make `FLURRY_STRIKE_COUNT` (3) quick weapon strikes
  on the target. Each rolls hit + damage independently. Bonus on-
  hit procs (Bloodletter, Jab) fire on each hit.
- **Cooldown:** 35T
- **Why unique:** the only ability that fires the **attacker's full
  combat pipeline multiple times in one activation**. Whirlwind
  hits multiple TARGETS once; Flurry hits ONE target multiple
  times.

#### 12. ShortBlades_Disengage

- **Targeting:** Direction
- **Mechanic:** move actor `DISENGAGE_DISTANCE` (3) cells in chosen
  direction, ignoring any "opportunity attack" mechanic adjacent
  enemies might trigger (CoO doesn't have OpAttacks today, so the
  ability is "free movement" — when OpAttacks ship, this becomes
  the canonical bypass).
- **Cooldown:** 25T
- **Why unique:** the only ability that grants pure mobility WITHOUT
  attacking. Tumble swaps; Vault crosses obstacles; Disengage just
  moves you N cells in the open.

---

### Acrobatics — mobility, dodging, recovery

**Theme:** tumbler / monk / dodge-master.

#### 13. Acrobatics_Tumble

- **Targeting:** AdjacentCell
- **Mechanic:** swap positions with the targeted creature (works on
  enemies AND allies). On enemies: leaves the target Confused 1T.
  On allies: free swap, no debuff.
- **Cooldown:** 20T
- **Per Qud:** mirrors `Acrobatics_Tumble`.
- **Why unique:** the only ability that **exchanges cells** with
  another creature.

#### 14. Acrobatics_Vault

- **Targeting:** Direction (over 1 obstacle)
- **Mechanic:** leap 2 cells in the chosen direction, skipping
  one wall/door/solid cell at distance 1 if the cell at distance 2
  is open. Useful for crossing pits, walls, doorways the player
  can't normally walk through.
- **Cooldown:** 15T
- **Per Qud:** mirrors `Acrobatics_Jump` simplified.
- **Why unique:** the only ability that **bypasses a wall/solid
  cell**. Disengage moves through open cells; Vault crosses
  blockers.

#### 15. Acrobatics_EvasiveRoll

- **Targeting:** SelfCentered
- **Mechanic:** remove ONE active negative status effect from self
  (random pick if multiple, with priority for action-blocking
  effects: Stunned > Frozen > Paralyzed > Bleeding > others).
- **Cooldown:** 60T
- **Why unique:** the only self-cleanse / status-removal active.
  Acrobatics_SwiftReflexes (passive in Qud) reduces effect
  *durations*; EvasiveRoll outright removes one.

---

### Spellcraft — universal magic mastery

**Theme:** meta-magic, spell amplification, anti-magic.

#### 16. Spellcraft_Counterspell

- **Targeting:** SelfCentered (toggle ON for next N turns)
- **Mechanic:** while toggled on, the next spell cast by a hostile
  adjacent creature is interrupted (their cast fails and the
  ability gets +5T cooldown applied to it). Toggle auto-deactivates
  after firing once.
- **Cooldown:** 60T
- **Per Qud:** new mechanic — Qud doesn't have a counterspell, but
  the magic suite needed an anti-magic option.
- **Why unique:** the only ability that interacts with ENEMY
  spell-casts. Riposte interacts with enemy melee misses; Counter-
  spell with enemy spell launches.

#### 17. Spellcraft_ArcaneSurge

- **Targeting:** SelfCentered
- **Mechanic:** reset ALL spell cooldowns the actor has to 0
  (mutations + future skill-ability spell flavors). Long cooldown
  on Surge itself prevents abuse.
- **Cooldown:** 250T
- **Why unique:** the only ability that **manipulates other
  abilities' cooldowns**.

#### 18. Spellcraft_LeyTap

- **Targeting:** SelfCentered
- **Mechanic:** drain `LEY_TAP_HP` (15%) of current HP. Buffer
  granted: next spell cast within 3T deals `LEY_TAP_DAMAGE_BONUS`
  (HP cost × 2) bonus damage on-hit.
- **Cooldown:** 40T
- **Why unique:** the only ability that **trades own HP for buff**.
  HeartFlame (Pyromancy) also trades HP but for fire-specific
  bonus; LeyTap is universal.

---

### Pyromancy — fire mastery

**Theme:** sustained burn, detonation, ash.

#### 19. Pyromancy_Pyroclasm

- **Targeting:** AdjacentCell (target must have BurningEffect)
- **Mechanic:** consume the target's Burning effect; deal damage
  in a 3×3 radius equal to `PYROCLASM_DAMAGE_PER_BURN_TURN` (3) ×
  the consumed Burning's remaining duration. Tagged Heat.
- **Cooldown:** 40T
- **Why unique:** the only ability that **consumes a status effect
  for damage**. Overload (Galvanism) chains rather than consumes;
  AlchemicCatalyst (Corrosion) triggers reactions rather than
  consumes.

#### 20. Pyromancy_FireWalk

- **Targeting:** SelfCentered (toggle)
- **Mechanic:** while toggled on, every cell the actor moves OUT OF
  gets a Burning trail for 3T (creatures stepping on it take Heat
  damage). Walking 5 cells leaves 5 burning cells behind.
- **Cooldown:** 30T (when toggling OFF, no cooldown when toggling ON
  — but a sanity check prevents toggling on while already toggled)
- **Why unique:** the only **per-step terrain-creation** mechanic.
  AcidPool drops a single radius pool on activation; FireWalk
  drops one cell per step indefinitely.

#### 21. Pyromancy_HeartFlame

- **Targeting:** SelfCentered
- **Mechanic:** sacrifice 50% of current HP; next 3 fire spells
  cast within 5T do `HEART_FLAME_DAMAGE_MULTIPLIER` (×2) damage.
- **Cooldown:** 100T
- **Why unique:** like LeyTap (HP-for-buff) but element-specific
  AND multi-cast (3 spells, not just the next one) AND multiplier
  not flat-bonus. Hibernate (Cryomancy) heals; HeartFlame burns
  away HP.

---

### Cryomancy — cold mastery

**Theme:** stasis, control, locking down.

#### 22. Cryomancy_Hibernate

- **Targeting:** SelfCentered
- **Mechanic:** actor enters stasis for `HIBERNATE_DURATION` (10)
  turns. Cannot act, but: heals `HIBERNATE_HEAL_PER_TURN` (5%) of
  max HP per turn AND has 100% HeatResistance + 100% ColdResistance
  + immune to all status effects. Ends early if attacked by
  Heat-tagged damage that exceeds 50% HP threshold (the fire melts
  the ice).
- **Cooldown:** 200T
- **Why unique:** the only **self-stasis with healing trade-off**.
  Bashguard (proposed Cudgel) blocks but doesn't immobilize.
  Hibernate is "I take myself out of combat to recover."

#### 23. Cryomancy_Frostbind

- **Targeting:** AdjacentCell
- **Mechanic:** target is "rooted" — cannot move from current cell
  for `FROSTBIND_DURATION` (4) turns. Target can still attack and
  cast. Melts on Heat damage to target (>50% HP threshold).
- **Cooldown:** 35T
- **Why unique:** the only ability that **locks a target's MOVEMENT
  but not their actions**. Stun blocks ALL action; Frostbind only
  movement (target can still slug at adjacent enemies).

#### 24. Cryomancy_IceMirror

- **Targeting:** SelfCentered
- **Mechanic:** spawn a 1-turn ice clone of the actor in an
  adjacent cell. Clone has 1 HP, 0 actions, but enemies prefer
  attacking it (AI-target weight bias). Clone shatters on any
  damage; before shattering, it absorbs 1 attack.
- **Cooldown:** 80T
- **Why unique:** the only **summon** in the table. IceMirror
  doesn't actually fight — it's a 1-turn aggro-decoy. Distinct
  from a "summon a fire elemental that attacks" mechanic.

---

### Galvanism — lightning mastery

**Theme:** chain damage, conductivity, redirect.

#### 25. Galvanism_StormCaller

- **Targeting:** SelfCentered (zone-wide effect)
- **Mechanic:** for `STORM_DURATION` (8) turns, every cell in the
  current zone is treated as Wet (water+lightning + cold synergies
  apply). Targets standing in the rain pick up `WetEffect` each
  turn they end the turn in.
- **Cooldown:** 250T
- **Why unique:** the only **zone-wide environmental effect**.
  AcidPool is 3-radius local; FireWalk is per-step; StormCaller
  changes the entire zone for the duration.

#### 26. Galvanism_Overload

- **Targeting:** Direction (chain attack)
- **Mechanic:** lightning bolt that travels in chosen direction up
  to `OVERLOAD_RANGE` (8) cells. Each Wet OR Electrified target
  the bolt passes through takes Electric damage and the chain
  continues. Non-conductive targets break the chain.
- **Cooldown:** 50T
- **Why unique:** the only **chain-through-targets** mechanic.
  Pyroclasm radiates from a target; Overload travels through
  multiple targets in sequence.

#### 27. Galvanism_LightningRod

- **Targeting:** SelfCentered (sustained toggle)
- **Mechanic:** while toggled on, any Electric damage targeting the
  actor is redirected to the nearest enemy creature within 5 cells
  (and tagged with the actor's resistance reductions, so the
  damage deals the same as if the actor took it raw). Toggle drains
  `LIGHTNING_ROD_DRAIN` (1 HP per turn) while active.
- **Cooldown:** 20T (on toggle ON)
- **Why unique:** the only **damage-redirect** mechanic. Riposte
  counter-attacks; Counterspell interrupts; LightningRod
  redirects-to-third-party.

---

### Corrosion — acid mastery

**Theme:** persistent damage, dissolve, terrain hazard.

#### 28. Corrosion_AcidPool

- **Targeting:** Direction (radial — places pool centered on
  adjacent cell in chosen direction)
- **Mechanic:** create a 3-cell-radius (cardinal cross + center)
  acid pool that lasts `ACID_POOL_DURATION` (6) turns. Creatures
  ending their turn in the pool take Acid damage and pick up
  AcidicEffect.
- **Cooldown:** 50T
- **Why unique:** the only **on-cast cell-residue radius**.
  FireWalk drops one cell per step (different placement);
  StormCaller is zone-wide (different scale); AcidPool is a fixed
  3-radius blob at activation time.

#### 29. Corrosion_AlchemicCatalyst

- **Targeting:** AdjacentCell (target must have AT LEAST one
  elemental status effect)
- **Mechanic:** trigger any cross-element reaction the target's
  status combination would produce, but at 2× radius. E.g., a
  target who is BOTH Burning AND Wet → fire+water reaction →
  generate a 2× steam cloud; a target who is Electrified AND on a
  Conductor → 2× chain lightning. Reads the target's effects and
  the cell's MaterialReactions for the cross-product.
- **Cooldown:** 70T
- **Why unique:** the only ability that **interacts with the
  cross-element reaction system directly**. The MaterialReactions
  table normally fires on natural conditions; Catalyst force-fires
  it at 2× scale on the target.

#### 30. Corrosion_Liquefy

- **Targeting:** AdjacentCell
- **Mechanic:** apply `LIQUEFY_DURATION` (5) turns of:
  - `-50%` movement speed (target moves at half rate)
  - `+100%` AcidResistance reduction (target takes 2× damage from
    Acid)
  - target gets visible `Liquefying` status (separate from
    AcidicEffect)
- **Cooldown:** 60T
- **Why unique:** the only ability that **multiplies a target's
  damage-take-modifier** rather than dealing damage. RendArmor
  reduces AV (different layer); Liquefy makes the target take 2×
  acid spec. Combo with Corrosion_AcidPool: Liquefy a target +
  drop AcidPool on them = devastating.

---

## Implementation priority

If we ship in batches, the order should match infrastructure
readiness:

### Tier 1 — uses only existing infrastructure (immediate ports)

1. **Cudgel_ChargingStrike** — uses Slam-style movement primitives
2. **Axe_Whirlwind** — uses existing 8-adjacent iteration
3. **LongBlades_Lunge** — uses Slam-style direction targeting
4. **ShortBlades_Flurry** — multi-call PerformSingleAttack
5. **Acrobatics_Tumble** — Zone.MoveEntity for the swap
6. **Spellcraft_LeyTap** — uses the new OnGetSpellDamageModifier hook
7. **Cryomancy_Hibernate** — Self-buff via stat shift + duration
8. **Pyromancy_Pyroclasm** — consumes existing BurningEffect.Duration
9. **Cryomancy_Frostbind** — needs a "Rooted" effect (small new)
10. **Galvanism_Overload** — adapt ChainLightning's chain logic

### Tier 2 — needs the toggle infrastructure (deferred to WSP9)

11-14. LongBlades_Riposte, LongBlades_EnGarde, Pyromancy_FireWalk,
Galvanism_LightningRod, Spellcraft_Counterspell — all toggleable.

### Tier 3 — needs cell-element-residue subsystem

15-17. Pyromancy_FireWalk (per-step trail), Galvanism_StormCaller
(zone-wide), Corrosion_AcidPool (radial residue) — all need the
cell-effect substrate from `MAGIC-SKILLS-DESIGN.md` §Tier-A
infrastructure.

### Tier 4 — needs new effect classes / mechanics

18-30. Frostbind needs Rooted effect, IceMirror needs decoy summon
infra, AlchemicCatalyst needs cross-element reaction force-fire,
Liquefy needs damage-modifier-attribute, EvasiveRoll needs
status-removal helper, Counterspell needs spell-interrupt event,
ArcaneSurge needs cooldown-reset path, HeartFlame needs damage-
multiplier-tracking, Disarm needs equipment-strip with drop-on-
cell, Decapitate (active) needs HP-threshold check + execute path,
RendArmor needs direct-stack-apply, Backstab needs
flanking-detection, Disengage needs OpAttack-bypass primitive.

## Open design questions

- **Cost convention:** all 30 land at 1 SP per the
  weapon-tree convention? Some powers feel premium (Hibernate's
  10T stasis, ArcaneSurge's full cooldown reset). Could make
  premium powers 2 SP. Current default: 1 SP each.
- **Tree ownership:** does Decapitate-active count toward the
  3-active-per-tree quota even though Decapitate-marker already
  shipped? My count: yes — they're separate skills sharing a
  flavor-themed namespace.
- **Reactive timing:** Riposte and Counterspell both fire on enemy
  actions. The toggle deactivates after firing — should it instead
  block multiple times until the duration expires? Current design:
  one-shot for tactical resource management. Could be either.
- **Mobility skills (Disengage / Vault / Tumble) overlap with
  movement:** all three produce the actor in a different cell.
  They differ in mechanism (open-cell vs over-obstacle vs
  swap-with-creature). Acceptable distinction or too clustered?
  Defensible — each addresses a different obstacle class.
