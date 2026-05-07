# Magic & Grimoire Skill Suite — Design Proposal

## 0. Status

**Brainstorm draft** — design only, no code. WSP6 round closed the
weapon-tree skill ports. This doc proposes the next major skill
batch: **a complete magic/spellcasting skill tree**.

---

## 1. Survey of existing magic infrastructure

### 1.1 Spells (Mutations)

CoO models spells as `BaseMutation`-derived Parts. Each declares its
cooldown / range / damage as protected overrides. Casting flows
through `ActivatedAbilitiesPart` (same infrastructure as skills'
active abilities). Tier-1 SP cost is per-mutation in
`MutationDefinition.Cost = 1`.

**31 mutations currently shipped, grouped by element:**

| Element | Mutations | Notes |
|---|---|---|
| **Fire** (Heat) | FireBolt, FlamingHands, Kindle, KindleFlame, EmberVein, Conflagration | Direct damage + Burning effect |
| **Cold** (Frost) | IceLance, IceShard, ChillDraft, FrostNova, RimeNova | Direct damage + Frozen effect |
| **Lightning** (Electric) | ArcBolt, ChainLightning, Thunderclap | Direct damage + Electrified, chains through Wet |
| **Acid** | AcidSpray, PoisonSpit | DoT + AV reduction |
| **Water** | ConjureWater, DryingBreeze, Quench | Utility — wet/dry mechanics, fire suppression |
| **Light/Sacred** | Hearthwarm, WardGleam, PrismaticBeam | Buff aura + multi-element burst |
| **Mental** | Calm, Esper, Telepathy | Hostility manipulation, sensing |
| **Body/Self** | Chimera, Regeneration, UnstableGenome, IrritableGenome, ExtraArmPrototype | Self-buff, evolution |

### 1.2 Damage types

`Damage.cs` recognizes: **Heat, Cold, Electric, Acid, Light,
Disintegration, Bludgeoning** (+ physical). Each has a matching
`<Type>Resistance` stat that modifies damage in
`CombatSystem.ApplyResistances`.

### 1.3 Elemental status effects

| Effect | Trigger | Effect |
|---|---|---|
| `BurningEffect` | Heat damage | Per-turn fire DoT, has aura (IAuraProvider) |
| `FrozenEffect` | Cold damage | Blocks action, melts to Wet on Heat |
| `ElectrifiedEffect` | Electric damage | DoT, conducts through Wet |
| `AcidicEffect` | Acid damage | DoT, degrades Combustibility |
| `WetEffect` | Water source | Vulnerability to Cold + Electric |
| `CharredEffect` | Post-Burning | Fragile — vulnerability stack |
| `SmolderingEffect` | Pre-Burning | Heat residue |
| `SteamEffect` | Water + Fire reaction | LOS obscure (presumably) |
| `HearthAuraEffect` | Hearth/light spells | Warming buff |

### 1.4 Cross-element reactions (already authored)

`Resources/Content/Data/MaterialReactions/` has 13 JSON files for
material+material interactions:
- fire + ice → water (+ Wet)
- water + fire → steam
- oil + fire → bigger fire
- lightning + conductor → ground current
- acid + organic → rapid damage
- cold + metal/crystal/chitinous → brittle (different shatter rules)
- fire + bone/fungal/raw_meat/raw_starapple → element-specific cooks

This is rich — cross-element skill mechanics have a lot of substrate
to lever against.

### 1.5 What's MISSING — required infrastructure additions

To wire magic skills cleanly, we need new hooks parallel to the WSP6
weapon-tree hooks. None exist yet:

| New hook | Purpose | Pattern to mirror |
|---|---|---|
| `OnGetSpellCooldownModifier(actor, mutation)` → int | Reduce/extend spell cooldown | OnGetToHitModifier shape |
| `OnGetSpellRangeModifier(actor, mutation)` → int | Extend spell range | Same shape |
| `OnGetSpellDamageModifier(actor, mutation, baseDamage)` → int | Boost spell damage | Same shape |
| `OnBeforeSpellCast(actor, mutation, target)` → bool | Allow veto / pre-cast hook | New event-veto pattern |
| `OnAfterSpellCast(actor, mutation, target)` → void | Post-cast effects | Like OnAttackerAfterAttack |
| `OnDealElementalDamage(actor, defender, damage)` → void | Element-specific on-damage | Like OnAttackerAfterAttack but element-gated |
| `OnTakeElementalDamage(defender, attacker, damage)` → void | Element resistance / counter | New defender-side hook |
| `CellElementResidue` system | Lingering Burning/Acid/Iced floor patches | New cell-state mechanic — sizable infra ship |

**Estimate**: 5–7 new BaseSkillPart virtuals + matching dispatcher
methods + ~3 new call sites in the mutation-cast flow + 1 new
cell-state subsystem = ~3 infrastructure ships. Skill content ships
ride on top of that infra.

---

## 2. The skill suite — 8 classes, ~42 skills

Naming follows the CoO `<Tree>_<Power>` convention (e.g.
`Pyromancy_Cinder`, parallel to `Cudgel_Slam`). Tree-roots use the
`<Tree>Skill` convention (e.g. `PyromancySkill`).

### Class 1 — Pyromancy (fire mastery)

**Theme**: amplify fire damage / Burning. Reward sustained fire
focus. Punish cold.

| Skill | Tier | Mechanic |
|---|---|---|
| **PyromancySkill** (root) | 1 | On Heat-damage crit: target's existing Burning gains +2T duration |
| Pyromancy_Cinder | 2 | Heat damage you deal generates "embers" (custom counter); 5 embers = next fire spell cooldown -50% |
| Pyromancy_Conflagration | 2 | Your Heat damage to Burning targets does +25% damage |
| Pyromancy_AshFall | 2 | Targets killed while Burning leave a Charred cell (lingering vulnerability tile) |
| Pyromancy_Cauterize | 2 | Heat spells you cast on yourself cure 1 Bleed stack + halve self-Burning |
| Pyromancy_FireResonance | 3 | First fire spell each turn costs 0 cooldown |
| Pyromancy_Pyroclasm | 3 | Active: when you cast a fire spell with Embers ≥10, all enemies in spell radius take +Heat damage equal to ember count |

**New mechanic introduced**: Ember counter (per-actor int on a new
EmbersPart, decays over turns). Cell-tagged Charred floor (needs
the cell-element-residue infra).

### Class 2 — Cryomancy (cold mastery)

**Theme**: control + persistent slowdown. Reward chaining Wet → Frozen.

| Skill | Tier | Mechanic |
|---|---|---|
| **CryomancySkill** (root) | 1 | On Cold-damage crit: extend Frozen duration by +2T (or apply Frozen if not present) |
| Cryomancy_Freezerburn | 2 | +50% Cold damage to Wet targets |
| Cryomancy_DeepFreeze | 2 | Frozen targets in your line of sight take +1 damage per turn frozen |
| Cryomancy_IceArmor | 2 | When you cast a Cold spell, gain temporary +AV (3T) |
| Cryomancy_BrittleStrike | 2 | Your melee hits on Frozen targets do +50% damage (synergy with weapon trees) |
| Cryomancy_Glacial | 3 | Frozen duration +2T baseline on every freeze you apply |
| Cryomancy_Hibernate | 3 | Active: encase yourself in ice (10T immobile + heal 2 HP/turn + 100% Heat resistance) |

**New mechanic**: temp-AV stack (works with existing ArmorPart).

### Class 3 — Galvanism (lightning mastery)

**Theme**: chain damage, Wet synergy, fast-strike.

| Skill | Tier | Mechanic |
|---|---|---|
| **GalvanismSkill** (root) | 1 | On Electric-damage crit: chain to one adjacent enemy at 50% damage |
| Galvanism_Conductor | 2 | Electric damage to Wet targets always crits the chain (treats chain target as Wet too) |
| Galvanism_StaticCharge | 2 | Every melee hit you take grants +1 "static" stack (max 5); next lightning spell does +1 damage per stack |
| Galvanism_Thunderclap | 2 | Lightning spells stun all adjacent targets for 1T |
| Galvanism_GroundCurrent | 2 | Lightning spells leave an Electrified cell (lingers 3T, ticks Electric DoT on walking enemies) |
| Galvanism_Earthing | 3 | When you would take Electric damage, redirect 50% to a random adjacent enemy |
| Galvanism_StormCaller | 3 | Active: turn an entire 5×5 zone Wet for 5T (rains) |

**New mechanic**: "static" stack counter, cell-Electrified state.

### Class 4 — Hydromancy (water mastery)

**Theme**: control + setup. Wet enemies for Cold/Electric synergy.

| Skill | Tier | Mechanic |
|---|---|---|
| **HydromancySkill** (root) | 1 | Water spells you cast also apply Wet for 5T (was 3T or absent) |
| Hydromancy_Floodtide | 2 | Water spells affect all 8 adjacent cells of target |
| Hydromancy_Quickensteam | 2 | Water + Fire combo produces 2× steam radius (synergy with material-reactions) |
| Hydromancy_Dowse | 2 | Active: sense liquid sources within 10 cells (GUI marker overlay) |
| Hydromancy_Tidewalker | 3 | Wet cells you walk on don't slow you + grant +1 AV |
| Hydromancy_Tsunami | 3 | Active: knock all adjacent enemies back 2 cells + apply Wet (1T cooldown shorter than Slam) |

**New mechanic**: Wet floor cells (needs cell-state); player-on-Wet
movement bonus.

### Class 5 — Corrosion (acid mastery)

**Theme**: armor stripping, persistent damage, environment hazards.

| Skill | Tier | Mechanic |
|---|---|---|
| **CorrosionSkill** (root) | 1 | On Acid-damage crit: add +1 stack of AcidicEffect to the target |
| Corrosion_Dissolve | 2 | Acid damage strips +1 AV per stack from armored targets (cumulative ShatterArmor synergy) |
| Corrosion_AcidPool | 2 | Acid spells leave an Acidic cell that ticks Acid DoT on walkers (5T duration) |
| Corrosion_Solvent | 2 | Acid spells do +50% damage to organic targets (no metal/crystal armor) |
| Corrosion_DoubleCorrosion | 2 | Acid spells apply +1 stack of AcidicEffect on hit |
| Corrosion_AlchemicSurge | 3 | Active: target takes (AcidicStacks × 2) Acid damage + clears all AcidicEffect stacks |
| Corrosion_Catalyst | 3 | Acid + any other element → immediately trigger a cross-reaction at 2× radius |

**New mechanic**: AcidicEffect stack interaction, cell-Acidic state.

### Class 6 — Photomancy (light/sacred mastery)

**Theme**: protection, healing-adjacent, anti-undead/dark.

| Skill | Tier | Mechanic |
|---|---|---|
| **PhotomancySkill** (root) | 1 | Light damage you deal also dispels 1 stack of any negative effect on you (CAUSE_EXTERNAL) |
| Photomancy_LanternKeeper | 2 | Your LightSourcePart aura is doubled (radius). Adjacent allies in your aura take +1 AV |
| Photomancy_Hearthwarm | 2 | HearthAuraEffect duration +5T |
| Photomancy_GuardingLight | 2 | Allies in your light aura are immune to Witnessed effect |
| Photomancy_Sunburst | 2 | Active: blind all enemies in your light aura for 2T (–4 hit) |
| Photomancy_Beacon | 3 | While casting any sacred spell, all hostile NPCs in your light aura are revealed (FOV/Mapping bypass) |
| Photomancy_Radiance | 3 | Active toggle: as long as toggled on, your turn-start emits 1 Light damage in light aura (drains 1 HP/turn) |

**New mechanic**: light-aura ally buff query.

### Class 7 — Empathy (mental mastery)

**Theme**: control without damage; pacify, confuse, manipulate hostility.

| Skill | Tier | Mechanic |
|---|---|---|
| **EmpathySkill** (root) | 1 | Mental spells you cast cost no SP (Calm, Esper, Telepathy) |
| Empathy_CalmAura | 2 | Your turn-start: all adjacent hostile creatures roll Will save vs Calm; failed = pacified for 2T |
| Empathy_DistantTouch | 2 | Mental spells cast at +50% range |
| Empathy_CompoundFear | 2 | Mental spells against Confused/Stunned targets get +50% duration |
| Empathy_Stillness | 2 | While you stand still (no movement turn), Mental spell crit chance +25% |
| Empathy_Mindbreak | 3 | Active: target enemy's next turn is skipped (Confused 1T but Stunned-strength) |
| Empathy_Inquisitor | 3 | When you Calm an enemy, you read 1 random tag of their inventory (info-gathering mechanic) |

**New mechanic**: Mental-spell-cost waiver hook, pacify state.

### Class 8 — Spellcraft (universal / meta-magic)

**Theme**: cross-element synergies, generic spell amplification.
Can be combined with any of the elemental classes.

| Skill | Tier | Mechanic |
|---|---|---|
| **SpellcraftSkill** (root) | 1 | First spell each combat encounter is free (no cooldown applied) |
| Spellcraft_Channel | 2 | All spell cooldowns -1 (floor 1) |
| Spellcraft_Resonance | 2 | If you cast the same spell twice within 3T, the second cast has +50% damage |
| Spellcraft_Empower | 2 | Every 5th spell does double damage (counter on a new SpellCounterPart) |
| Spellcraft_Counterspell | 2 | When a hostile creature casts a spell adjacent to you, 25% chance to interrupt (target's spell fails + 5T cooldown) |
| Spellcraft_Booklore | 2 | Spells you've cast 10+ times have -1 cooldown floor and +10% damage permanently (per-mutation tracker) |
| Spellcraft_Synergy | 3 | If your last damage type was different from your current spell's type, +30% damage |
| Spellcraft_Catalyst | 3 | Active: your next spell ignores all elemental resistances on the target |

**New mechanic**: SpellCounterPart (generation-on-cast), per-mutation
"times cast" tracker, "last damage type" tracker.

---

## 3. Cross-class synergy chart

Designing for combos. The tooltip / Discord-style "what works with
what":

| Combo | Outcome |
|---|---|
| Hydromancy + Cryomancy | Hydromancy_Floodtide → Cryomancy_Freezerburn → entire room frozen |
| Hydromancy + Galvanism | StormCaller + ChainLightning → mass-electrified Wet zone |
| Pyromancy + Corrosion | AshFall + AcidPool → cell-tagged DoT compounding |
| Pyromancy + Cryomancy | Direct opposition — but Spellcraft_Synergy rewards alternating |
| Photomancy + Empathy | LanternKeeper aura + CalmAura = pacify-zone for non-violent runs |
| Galvanism + Photomancy | StaticCharge melee build with self-AV buff from light aura |
| Spellcraft + Any | Universal multipliers — Empower / Resonance / Synergy stack on top |

---

## 4. Implementation plan (priority order)

### Tier A — Infrastructure (must precede any class ship)

**WSP7.0 — New BaseSkillPart virtuals + dispatcher methods.** Mirrors
WSP6.6's pattern (ship the OnGetPenetrationModifier hook + consumer
in one go). Add:
- `OnGetSpellCooldownModifier(actor, mutation)` → int
- `OnGetSpellRangeModifier(actor, mutation)` → int
- `OnGetSpellDamageModifier(actor, mutation, baseDamage)` → int
- `OnAfterSpellCast(actor, mutation, target, damage)` → void
- `OnDealElementalDamage(ctx, elementFlag)` → void (sub-shape of OnAttackerAfterAttack)

Wire each into the mutation-cast flow (mostly in `BaseMutation` or
its subclasses' Cast/Activate paths). One commit per hook + one
consumer skill that uses it.

**WSP7.1 — Cell-element-residue subsystem.** New cell tag system
(CellEffect or similar). Cells can carry an element tag for N turns;
walking/standing in a cell triggers the element's tick effect (Burning
deals Heat damage, Acidic deals Acid damage, etc.). Required for
AshFall / AcidPool / GroundCurrent / Tidewalker. ~150 LoC + tests.

**WSP7.2 — Toggleable infrastructure for ActivatedAbilities.** Plumb
`Toggleable` / `IsActiveToggle` / `DefaultToggleState` through
`ActivatedAbility` + `ActivatedAbilitySpec` + `SkillsPart.AddSkill`
wiring. Required for Photomancy_Radiance and (back-port) Axe_Decapitate
as a true toggle. ~50 LoC.

### Tier B — Class ships (each ~3-7 skills + content + tests)

In priority order (most-impactful first; lower-complexity classes
ship first to validate the infrastructure):

1. **WSP7.3 — Spellcraft** (universal/meta-magic). Uses ONLY the
   new generic hooks; no cell-residue or toggleable needed. Smallest
   blast radius — proves the hook infra.
2. **WSP7.4 — Pyromancy**. First element-specific class. Tests
   ember counter + cell-residue (AshFall).
3. **WSP7.5 — Cryomancy**. Mirrors Pyromancy's shape; tests
   Wet+Cold synergy with existing WetEffect.
4. **WSP7.6 — Galvanism**. Static-charge + cell-residue +
   chain-target (uses existing Slam-style adjacency code).
5. **WSP7.7 — Corrosion**. AcidicEffect stack interaction +
   cell-residue.
6. **WSP7.8 — Hydromancy**. Wet floors + utility-heavy.
7. **WSP7.9 — Empathy**. Spell-cost-waiver hook + pacify state.
8. **WSP7.10 — Photomancy**. Toggle (Radiance) + light-aura
   ally-query — most infrastructure-dependent of the bunch.

### Tier C — Showcase + integration

**WSP7.11 — Magic showcase scenario.** A demo zone with one of each
elemental enemy class + a player loadout pre-stocked with mutations
+ skills from each tree. Mirrors the existing weapon-skills
showcases.

**WSP7.12 — Magic-tree integration tests.** Same shape as
`Wsp6IntegrationTests.cs` — verify each class's skills work via the
real cast/dispatch/turn-tick paths. ~30-40 tests.

---

## 5. Open design questions

These need user/playtester input before final scope-lock:

- **Q1: Skill cost convention.** Weapon trees use 1 SP. Acrobatics
  uses 100/50. Magic trees should follow which? I'd recommend 1 SP
  per skill (consistent with weapon trees) given the same player-
  facing economy, but the user has historically valued the cost-split
  nuance. Open question.

- **Q2: SP-economy implications.** ~42 magic skills + 28 weapon
  skills = 70 skills available at 1 SP each. The player gains how
  much SP per level? If the intended endgame "knows ~half the skill
  catalog," 70 skills × 1 SP = 70 SP needed — feels right at ~2-3
  SP/level over a 25-level arc.

- **Q3: Tree access gating.** Should players need to "unlock" a
  tree by casting N spells of that element? Or is everything
  immediately purchasable? Qud has prerequisite chains; CoO's weapon
  trees don't (per the WS.0 plan). I'd recommend mirroring the
  weapon-tree model: flat-purchasable, no prereqs, simplest UX.

- **Q4: Stacking convention for percent multipliers.** If a player
  has Spellcraft_Empower (2× every 5th) + Pyromancy_Cinder (-50% CD)
  + Spellcraft_Channel (-1 CD) — do they multiply or add? Conflict
  resolution per-skill would scope-creep; recommend a global
  "modifiers add unless flagged multiplicative" rule, applied in
  the dispatcher's aggregator.

- **Q5: How "elemental" is each element's signature?** E.g., should
  Pyromancy boost ALL Heat damage, including damage from a Flaming
  Sword? Or only spell-source Heat damage? I'd say all-Heat is
  better (rewards a fire-themed build that mixes weapon + spell).
  But the FlamingSword interaction means a non-magic Pyromancer is
  meaningful — design check.

- **Q6: Anti-element punishment.** If a player goes deep into
  Pyromancy, should Cold damage to them hurt MORE? Qud doesn't do
  this. CoO doesn't have any "elemental affinity" stat. I'd
  recommend NO penalty — additive bonuses only (more fun for
  players who want hybrid builds).

---

## 6. Estimated total scope

- ~42 new skill classes (~150 LoC each) = ~6,300 LoC production code
- ~5 new effect/state classes (Embers, StaticCharge, CellResidue,
  Pacified, AbilityToggleState) = ~500 LoC
- ~5 new BaseSkillPart virtuals + dispatcher = ~200 LoC infra
- ~150 unit tests (≥3 per skill avg) = ~3,000 LoC
- ~40 integration tests = ~1,500 LoC
- ~15 JSON content entries (1 per tree-root + powers) = ~600 LoC
- 1 showcase scenario = ~300 LoC

**Estimated: ~12,400 LoC. ~12-15 commits across WSP7.0 through 7.12.**

The skill system's scaling test — can it absorb a 42-skill content
ship without architectural changes? The answer should be yes — the
WSP3.3 self-contained virtual-override pattern + the per-power JSON
content authoring model means each skill ships independently. The
five new infra hooks are additive (don't modify existing combat
flow paths). The cell-residue subsystem is the only "new substrate"
ship and it's bounded.

---

## 7. Decision points (next session)

Pick one of:

1. **Greenlight all of WSP7** — start with WSP7.0 infrastructure
   ship. Estimated 1-2 commits to land the new hooks.
2. **Subset** — pick 1-2 classes that look most fun (e.g., Pyromancy
   + Spellcraft) and ship just those. Defer the rest.
3. **Iterate on this design** — refine the skill list, add/cut
   skills, change class names. Then ship.
4. **Different direction entirely** — propose alternative magic
   skill ideas not covered here.

Default recommendation: **(2) Subset** — ship WSP7.0 (infrastructure)
+ WSP7.3 (Spellcraft) as a proof-of-concept slice. If that lands
cleanly, expand to the elemental classes.
