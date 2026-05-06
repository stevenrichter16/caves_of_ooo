# Tier-2 Weapon-Skills Parity (WSP2)

Continuation of `Docs/WEAPON-SKILLS-PARITY.md`. The prior ship (WSP.0-4b)
landed Tier 1: tree-root crit behaviors, Cudgel_Bludgeon Qud-verbatim
re-tune, and ShortBlades_Bloodletter as a 5th power. This ship works
the **Tier 2 backlog** — passives that need new combat hooks or new
status effects but not the full active-ability infrastructure (Tier 3
remains deferred).

User directive: **"keep going with full qud parity with these skills.
make acceptance criteria and follow CLAUDE.md"** — so each skill in
this plan has an explicit Given/When/Then acceptance invariant + the
test that pins it + counter-checks per CLAUDE.md §3.4.

## Pre-impl verification sweep

| Premise | Status | Source |
|---|---|---|
| `CombatSystem.PerformSingleAttack` computes `totalHit = hitRoll + agilityMod + hitBonus` at line ~167-169 — adding a skill-driven contribution is a one-line edit | ✅ | `CombatSystem.cs:166-169` |
| Miss path emits a message + an FX particle at lines ~174-183 then `return` — adding an `OnMissSkillEffects.Apply` call is a one-line edit before the early `return` | ✅ | `CombatSystem.cs:174-183` |
| `Effect` abstract class has `OnApply` / `OnRemove` / `OnStack` / `AllowAction` / `Duration` virtuals; `StunnedEffect` is the canonical pattern | ✅ | `Effect.cs:1-80`, `StunnedEffect.cs` |
| Defender's `Body.GetEquippedParts()` exists for Cudgel_Hammer's "random-equipped-item Broken" lookup | ⚠️ unverified | needs `Body.cs` read in WSP2.5 pre-impl |
| `Stat.Penalty` accumulates penalties (StunnedEffect uses `dv.Penalty += 4`) — Hobbled can `Stat.Penalty += MOVE_SPEED_PENALTY` similarly | ✅ | `StunnedEffect.cs:20-21, 28-29` |
| `ArmorPart.AV` exists and is a runtime-modifiable stat for ShatterArmor's mechanic | ⚠️ unverified | needs `ArmorPart.cs` read in WSP2.6 pre-impl |
| Existing skill-tree machinery (BaseSkillPart + SkillsPart.HasSkill + JSON-driven SkillRegistry) supports identity-only stub classes for the new skills | ✅ | demonstrated by WS.1-6b + WSP.1-4b ships |

**Two ⚠️ items resolved in their owning sub-milestones** (WSP2.5 + WSP2.6 pre-impl reads). Plan doesn't block on them — they're contained risk.

## Tier 2 skills — full Qud-faithful matrix

### Group A: skills that fit the existing post-damage hook (no new combat events)

#### Skill: `Cudgel_Hammer`
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/Cudgel_Hammer.cs:11-22`
**Mechanic:** On every `DealDamage` event with a Cudgel-class weapon, 2% chance to apply `Broken` to a random equipped item on the defender.
**CoO port:** Faithful — fires on every Cudgel-attribute hit (post-damage), 2% chance, picks one entry from `defender.Body.GetEquippedParts()` at random and applies a new `BrokenEffect` to that item entity. The new effect is `EQUIPMENT`-typed and disables the `IsEquippable` use of the item until removed.
**Cost:** 1 SP, no requirements.
**Acceptance invariant:** *Given* the player owns Cudgel_Hammer and is wielding a Mace + the defender has at least one equipped item, *when* the player swings at the defender across many seeds, *then* at least one of those swings produces a `BrokenEffect` on one of the defender's equipped items.
**Counter-checks:**
  1. Player without Cudgel_Hammer + same setup → no equipment ever Broken across the same seed range.
  2. Player wielding non-Cudgel weapon (LongSword) + Cudgel_Hammer owned → no equipment ever Broken.
  3. Defender with no equipped items → no crash, no effect.

#### Skill: `Cudgel_ShatteringBlows`
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/Cudgel_ShatteringBlows.cs:11-25`
**Mechanic:** 10% chance per Cudgel hit to apply `ShatterArmor(2000)` to the defender. The 2000 magnitude in Qud is an AV-decay parameter; CoO ports as a 4-turn `ShatterArmorEffect` that subtracts a fixed 2 from the defender's effective AV (via a stat penalty).
**CoO port:** Reframed (CoO has no AV-decay mechanic) but flavor preserved — armor crumbles for 4T.
**Cost:** 1 SP, no requirements.
**Acceptance invariant:** *Given* the player owns Cudgel_ShatteringBlows + wields a Cudgel weapon, *when* swinging at a defender (with any AV) across many seeds, *then* at least one swing produces `ShatterArmorEffect` on the defender.
**Counter-checks:**
  1. Without skill → effect never appears.
  2. With non-Cudgel attribute on the damage → effect never appears.

#### Skill: `ShortBlades_Hobble` (passive port)
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/ShortBlades_Hobble.cs:67-84`
**Mechanic:** Qud's Hobble is an **active ability** with a 30-turn cooldown, on-command. The active version applies `Hobbled(16-20T)` (50% move-speed reduction) to the target it's used on.
**CoO port (reframed):** CoO has no ActivatedAbility-skill integration yet (Tier 3 deferred). Port as a **passive on-hit hook**: 15% chance per Piercing-attribute hit to apply `HobbledEffect(8T)`. Lower chance + shorter duration than the active version since the active is an opt-in cost; passive needs to be balanced for "fires every hit". Documents the divergence in the stub class.
**Cost:** 1 SP, no requirements.
**Acceptance invariant:** *Given* the player owns ShortBlades_Hobble + wields a Piercing weapon, *when* swinging at a defender across many seeds, *then* at least one swing produces `HobbledEffect` on the defender.
**Counter-checks:** identical pattern to Bloodletter (without skill / with wrong attribute).

#### Skill: `Cudgel_Expertise`
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/Cudgel_Expertise.cs:6-26`
**Mechanic:** +2 to-hit when wielding a Cudgel-class weapon (passive event-handler on `GetToHitModifierEvent`).
**CoO port:** Faithful — adds +2 to `totalHit` in `CombatSystem.PerformSingleAttack` when the actor owns this skill AND the wielded weapon has `Cudgel` attribute. Implementation: new helper `SkillHitBonus.GetFor(attacker, weapon)` returns 0 / 2 based on owned-skill check + weapon-attribute check; CombatSystem calls it once and adds to totalHit.
**Cost:** 1 SP, no requirements.
**Acceptance invariant:** *Given* the player owns Cudgel_Expertise + wields a Cudgel weapon vs a defender with DV=20, *when* the hit-roll math is computed, *then* the +2 contribution is observable in `totalHit` (i.e. a roll of 18 + 0 agility + 2 weapon + **2 expertise** = 22 hits, would have missed without).
**Counter-checks:**
  1. Without skill → no +2.
  2. With non-Cudgel weapon → no +2.

#### Skill: `Axe_Expertise`
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/Axe_Expertise.cs:6-25`
Same shape as Cudgel_Expertise but gates on `Axe` attribute.
**Acceptance invariant:** identical except weapon class is Axe.

#### Skill: `ShortBlades_Expertise`
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/ShortBlades_Expertise.cs:6-26`
Same shape but **+1 to-hit** (Qud uses 1, not 2, for ShortBlades) and gates on `Piercing` attribute.
**Acceptance invariant:** identical except weapon class is Piercing and bonus is +1.

### Group B: skills that need new combat events

#### Skill: `Cudgel_Backswing`
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/Cudgel_Backswing.cs:21-50`
**Mechanic:** On `AttackerMeleeMiss` event with a Cudgel-class weapon, 25% chance to immediately re-attack with the same weapon. Qud throttles to once per Game.Segments tick (so a single swing-storm doesn't infinitely re-trigger).
**CoO port (faithful + simplification):** New `OnMissSkillEffects.Apply(attacker, defender, weapon, zone, rng)` static class fires from CombatSystem's miss path. If `attacker.HasSkill(Cudgel_Backswing)` AND `damage` had `Cudgel` attribute, roll 25% → call `PerformSingleAttack(attacker, defender, weapon, ...)` once more. Throttle: a per-attacker `_lastBackswingTurn` field on the SkillsPart so each turn only triggers one Backswing (no recursion).
**Cost:** 1 SP, no requirements.
**Acceptance invariant:** *Given* the player owns Cudgel_Backswing + wields a Cudgel weapon vs a defender with DV high enough to dodge most hits, *when* swinging across many seeds with deterministic miss, *then* across the seed range at least one missed swing is followed by a re-attack (observable as `AttackerCount` of swings exceeds the seed count).
**Counter-checks:**
  1. Without skill → never re-attacks on miss.
  2. With non-Cudgel weapon → never re-attacks on miss.
  3. On a *hit* (not miss) → no Backswing chance roll happens.
**Risk:** ⚠️ recursion. If Backswing's re-attack itself misses, it must NOT re-trigger (per-turn throttle handles this; the test pins it).

#### Skill: `ShortBlades_Rejoinder`
**Qud source:** `qud-decompiled-project/XRL.World.Parts.Skill/ShortBlades_Rejoinder.cs:21-92`
**Mechanic:** When a defender (player) dodges an incoming attack and has a ShortBlades weapon equipped, 60% chance to immediately counter-attack the original attacker with that ShortBlades weapon. Qud uses a once-per-action throttle.
**CoO port (faithful + simplification):** Hook into the same miss path as Backswing, but invoked as `OnMissSkillEffects.ApplyDefenderSide(attacker, defender, defenderWeapon, zone, rng)`. If `defender.HasSkill(ShortBlades_Rejoinder)` AND defender has a Piercing-attribute weapon equipped, roll 60% → call `PerformSingleAttack(defender, attacker, defenderWeapon, ...)`. Same per-defender throttle.
**Cost:** 1 SP, no requirements.
**Acceptance invariant:** *Given* the player owns ShortBlades_Rejoinder + has a Dagger equipped + an enemy attacks the player and misses, *when* the miss resolves across many seeds, *then* across the seed range at least one miss is followed by the player counter-attacking the enemy.
**Counter-checks:**
  1. Player without skill → no riposte.
  2. Player without Piercing weapon equipped → no riposte.
  3. On a *hit* (incoming attack lands) → no riposte chance roll.
**Risk:** ⚠️ test fixture complexity — needs an attacker AI swinging at the player. Mitigation: drive the test by directly calling `PerformSingleAttack(npc, player, weapon, ...)` with a seed that misses; observe player.AttackedCount.

## Sub-milestones (smallest blast radius first)

| # | Title | Surface | Test count |
|---|---|---|---|
| WSP2.0 | Plan to disk (this commit) | `Docs/WEAPON-SKILLS-PARITY-T2.md` | n/a |
| WSP2.1 | New status effects (Broken / ShatterArmor / Hobbled) | 3 new `Effect` subclasses + 3 fixtures | 9 (3 per effect: ctor + OnApply + OnRemove) |
| WSP2.2 | Skill-driven hit bonus + on-miss event | `SkillHitBonus.GetFor` helper + `OnMissSkillEffects.Apply{,DefenderSide}` static. Wire into CombatSystem (one-line edit each) | 4 (universal-scaffold contract per the OnHitSkillEffectsTests pattern) |
| WSP2.3 | 3 Expertise passives (Cudgel/Axe/ShortBlades) | JSON content + 3 stubs + branches in `SkillHitBonus` | 6 (1 positive + 1 counter-check per skill) |
| WSP2.4 | `Cudgel_Backswing` | JSON content + stub + `OnMissSkillEffects.Apply` branch | 3 (1 positive + 2 counter-checks) |
| WSP2.5 | `Cudgel_Hammer` | Body API verify; JSON content + stub + branch in OnHit | 4 (1 positive + 3 counter-checks: no-skill / wrong-weapon / no-equipment) |
| WSP2.6 | `Cudgel_ShatteringBlows` | JSON content + stub + branch in OnHit | 3 |
| WSP2.7 | `ShortBlades_Hobble` (passive port) | JSON content + stub + branch in OnHit | 3 |
| WSP2.8 | `ShortBlades_Rejoinder` | JSON content + stub + `OnMissSkillEffects.ApplyDefenderSide` branch | 3 |
| WSP2.9 | Showcase update + cold-eye + roadmap close | mod showcase scenario; cold-eye delegation; impl log | 0 (smoke test reuses existing `Assert.DoesNotThrow`) |

**Total: ~35 new tests, ~10 commits.** Each sub-milestone is independently revertable.

## Pre-flagged self-review findings

- **🟡 Active-ability mechanics still deferred** — Conk, Slam, ChargingStrike, Berserk, Decapitate-toggle, Dismember-active, all 3 LongBlades stances, LongBladesDeathblow are Tier 3 and not in this ship. They need ActivatedAbilities-Skill integration. Tracked.
- **🟡 Hobble simplification** — Qud's Hobble is an active ability with a 30T cooldown applying 16-20T Hobbled. CoO ports as a 15% passive on-hit applying 8T Hobbled. The chance + duration tuning is a balance call (lower chance because it fires every hit; lower duration because passive Hobble feeling spammy is worse than active Hobble feeling rare). Document inline.
- **🔵 BackSwing recursion guard** — the per-turn throttle prevents infinite re-trigger but we need a turn-counter hook on SkillsPart. Could leak into save format. Mitigation: use `Time.frameCount` or a `_lastBackswingTurn` int reset on `BeginTurn` event. Decide in WSP2.4 pre-impl.
- **🔵 SkillHitBonus.GetFor signature** — needs `(Entity attacker, MeleeWeaponPart weapon)`. The weapon might be null (unarmed). Helper returns 0 if either is null. Locked.
- **🧪 Cudgel_Backswing's "missed swings exceed seed count" test condition** — testing this cleanly requires a seed where the first swing always misses AND the Backswing roll succeeds AND the re-attack is detectable. Best-shot fixture: synthetic high-DV defender + low-Agility attacker so totalHit < dv on every seed; assert that across N seeds, attack-event count > miss-event count for at least one seed. Document the seed-loop magic in the test.
- **⚪ JSON foreground colors** — new powers' Foreground glyphs should match their tree's color (Cudgel: 'y', Axe: 'r', ShortBlades: 'c'). Lock in WSP2.3+.

## Verification (post-implementation)

```
run_tests EditMode group_names=[
  "OnHitSkillEffectsTests",
  "OnMissSkillEffectsTests",
  "BrokenEffectTests",
  "ShatterArmorEffectTests",
  "HobbledEffectTests",
  "SkillHitBonusTests",
  "OnHitClassEffectsTests",
  "SkillsScreenStateBuilderTests",
  "SkillsScreenLiveBlueprintTests",
  "ScenarioCustomSmokeTests"
]
```

Expected: 130+/130+ GREEN.

**Live verify (Playmode showcase):**
- Open `Caves Of Ooo / Scenarios / UI / Skill Tree Showcase` (this scenario will be updated in WSP2.9 to pre-buy several Tier 2 skills).
- Equip Mace, swing at high-DV target. Watch:
  - +to-hit observable as fewer misses than baseline (Cudgel_Expertise)
  - On miss with Backswing: re-attack message visible
  - On hit: occasional "X is shattered" / "X's armor cracks"
- Equip Dagger, get attacked by an NPC, observe Rejoinder.

## What gets observable to the player after this ship

| Today (post-WSP.1-4b) | After WSP2.1-9 |
|---|---|
| 5 trees / 5 powers / 4 tree-root crit behaviors | 5 trees / **13 powers** (5 + 8 new) / 4 tree-root crit behaviors |
| Cudgel-trained character: stuns, occasionally shatters armor never. | Cudgel-trained: stuns + cracks armor + occasional re-attack on miss + breaks defender's equipment. **Mace becomes a control weapon, not just a damage one.** |
| Sword-trained character: bleeds opponents | Sword-trained + Expertise: hits more often + bleeds + still gets crit-bleed. |
| Dagger-trained character: confuses opponents | Dagger-trained + full kit: **counter-attacks on dodge** + hits more often + bleeds + hobbles for 8T. **Dagger becomes a duelist's weapon.** |
| Tree-roots are crit-only flavor with one effect each | Tree-roots get expertise +to-hit AND crit-only effect — buying a tree-root for 2 SP (root + Expertise) is now a proper "I main this weapon class" purchase. |
