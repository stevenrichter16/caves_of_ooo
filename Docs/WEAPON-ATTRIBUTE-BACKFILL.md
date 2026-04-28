# Weapon Attribute Backfill

> Tier-1 maintenance ship. Branch: `feat/weapon-attribute-backfill`.
> Closes the gap where most existing melee weapons declare no
> `Attributes` string and so don't participate in Phase C physical-class
> attribution.

## Goal

Backfill `MeleeWeaponPart.Attributes` on the 17 weapons that currently
declare none, so every melee weapon in the game contributes physical-class
+ weapon-family tags to the Damage object on hit. This unblocks future
content that conditions on those attributes (Cutting-resistant armor,
Bludgeoning-only Stoneskin, achievements, AI reactions, material reactions,
etc.) without requiring new code.

## User-visible invariant

"Every melee weapon's Attributes string declares its physical class
(Cutting/Bludgeoning/Piercing) and most also declare a weapon-family
sub-class (LongBlades, Cudgel, Axe, Glaive, etc.). Future content that
hooks on `damage.HasAttribute("Cutting")` etc. fires correctly for every
weapon, not just the elemental ones we already shipped."

## Phase mapping

| Phase | Surface | Used here |
|---|---|---|
| Phase C | `MeleeWeaponPart.Attributes` reflective load + `damage.AddAttributes` propagation | Every weapon now declares its class |

No new code, no new resistances, no new mechanics. Pure content backfill.

## Verification sweep

| Premise | Status | Source |
|---|---|---|
| Currently 8 weapons have Attributes (Dagger, ShortSword, Cudgel, Warhammer, FlamingSword, IceSword, ThunderHammer, AcidicDagger) | ✅ confirmed | grep on Objects.json |
| 17 weapons currently lack Attributes | ✅ confirmed | python3 inventory pass |
| `damage.AddAttributes(weapon.Attributes)` is the propagation site (already proven) | ✅ confirmed | `CombatSystem.cs:231-232` |
| Existing test pattern: `weapon.Attributes` exact-string assertion via `_harness.Factory.CreateEntity(...)` | ✅ confirmed | `WeaponAttributesContentTests.cs` (Dagger, ShortSword, Cudgel, Warhammer pattern) |

**No false premises.** Pure content addition.

## Designed attributes

| Weapon | Damage | Existing material tags | Designed Attributes | Reasoning |
|---|---|---|---|---|
| LongSword | 1d8 | Metal,Conductor | `Cutting LongBlades` | Standard one-handed sword |
| Battleaxe | 2d6 | Metal,Conductor | `Cutting Axe` | Axe head |
| Greatsword | 1d12 | Metal,Conductor | `Cutting LongBlades` | 2H sword |
| Mace | 1d8+1 | Metal,Conductor | `Bludgeoning Cudgel` | Matches Warhammer's existing pattern |
| Spear | 1d6+1 | Metal,Conductor | `Piercing` | Standard pole-thrust |
| Hatchet | 1d6 | (none) | `Cutting Axe` | Small axe |
| Claymore | 2d8 | (none) | `Cutting LongBlades` | 2H sword |
| Sporeblade | 1d8+1 | Metal,Fungal | `Cutting LongBlades` | "blade" = LongBlades; Fungal lore stays in material tags |
| EchoKnife | 1d4+3 | Crystal,Sonic | `Cutting Sonic` | Crystal blade with existing Sonic material tag — `Sonic` becomes a damage attribute too, ready for future Sonic-resistance content |
| ChoirSpine | 1d4+1 | Bone,Organic | `Piercing` | "Spine" suggests piercing; lore stays in material tags |
| OldWorldPipe | 1d6 | Metal,Conductor | `Bludgeoning Cudgel` | Pipe = club-like |
| TemporalShard | 1d6+2 | Crystal,Temporal | `Piercing` | "Shard" = stab; lore in material tags |
| SeveranceEdge | 1d8 | (none) | `Cutting LongBlades` | "Edge" + "Severance" → Cutting blade |
| GlassblownStiletto | 1d4+2 | Glass,Crystal,Brittle | `Piercing` | Stiletto = piercing dagger |
| DissolutionMaul | 2d6+2 | Metal,**Corrosive** | `Bludgeoning Cudgel Acid` | **BONUS**: Corrosive material → Acid attribute. This makes DissolutionMaul a SECOND Acid-routing weapon (alongside AcidicDagger). Tested below: hits a CaveSlime for halved damage, hits a Scorpion for amplified damage. Free synergy with the AcidicDagger ship. |
| FirstRootGlaive | 2d6+1 | Wood,Living,Organic | `Cutting Glaive` | Glaive = polearm cutter; "Glaive" as a new sub-class (no other glaives currently) |
| PalimpsestBlade | 1d10+1 | Metal,Ancient | `Cutting LongBlades` | "Blade" = sword; lore in material tags |

### Note on lore tags

I kept lore-flavored material tags (Fungal, Living, Ancient, Temporal) **in the Material part's `MaterialTagsRaw`** rather than promoting them to Damage attributes. They're already there and could be promoted later if/when consumer content ships. Damage attributes here stick to physical class + weapon family + (sometimes) elemental routing — what the existing system already reads.

### Note on DissolutionMaul

This is the only "non-trivial" change. The maul currently produces only `[Melee, Strength]`-tagged damage; with the backfill it produces `[Melee, Strength, Bludgeoning, Cudgel, Acid]`. The `Acid` attribute will route the hit through `AcidResistance` (Phase E). Effects:

- Hits on CaveSlime (AR=+50): halved damage (slime resists corrosion)
- Hits on Scorpion (AR=−50): amplified 1.5× (chitin dissolves)
- Hits on anything else: unchanged

This is a **behavior change**, not just metadata. The dedicated test `DissolutionMaul_AcidAttribute_RoutesThrough_AcidResistance` pins this contract end-to-end. Documented in commit body as a 🟡 finding.

## Sub-milestones

### B.1 — Plan + branch (this commit)

### B.2 — RED + GREEN (one commit)

- Add 17 blueprint-shape tests in `WeaponAttributesContentTests.cs`
  (one per weapon, exact-string assertion).
- Add 1 integration test for DissolutionMaul's Acid-routing behavior.
- Run RED → confirm 18 fail (none of the weapons declare Attributes yet).
- Add Attributes line to each of 17 weapon blueprints.
- Run GREEN → confirm 18 pass.
- Combat regression sweep.

### B.3 — Self-review + commit + merge + push + roadmap update

## Implementation log

(populated as work progresses)
