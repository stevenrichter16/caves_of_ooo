# Item Enhancement System (Stones, Sigils, Infusions)

> **Single source of truth.** This file consolidates the multi-phase
> Item-Enhancement feature — plans, progress, cold-eye reviews,
> adversarial sweeps, audit findings, and Qud-parity tracking.
> Updated as each sub-milestone ships. Mirrors the structure of
> `Docs/FOLLOWERS.md`.
>
> **Origin:** user request — "I want my game to allow players to,
> for lack of a better term, 'enchant' their weapons with sigils
> and minerals." Combined with the `claude/ideas-gin-frogs` branch's
> `IDEAS.md` line 730 entry ("Stones with stake — minerals as relics,
> infusions, and faction currency").

---

## Table of contents

- [Status banner (cumulative)](#status-banner-cumulative)
- [Why we're building this](#why-were-building-this)
- [Phases overview](#phases-overview)
- [Working principles](#working-principles)
- [Qud-parity status (forward-looking)](#qud-parity-status-forward-looking)
- [Verification sweep](#verification-sweep)
- [Scope-prune](#scope-prune)
- [Design lockdowns](#design-lockdowns-pin-before-e2-starts)
- [Phase E.1 — Enhancement infrastructure](#phase-e1--enhancement-infrastructure-)
- [Phase E.2 — First 3 enhancements (validate the pattern)](#phase-e2--first-3-enhancements-)
- [Phase E.3 — Mineral content + faction-wanting hook](#phase-e3--mineral-content--faction-wanting-)
- [Phase E.4 — Showcase + playtest closure](#phase-e4--showcase--playtest-)
- [Phase E.5+ — Deferred polish queue](#phase-e5--deferred-polish-)
- [Cross-references](#cross-references)
- [Open design questions](#open-design-questions)

---

## Status banner (cumulative)

| Field | Value |
|---|---|
| **Current phase** | E.1 ✅ shipped (54 tests, 0 bugs); E.2 next |
| **Cumulative tests** | 54 |
| **Real bugs surfaced + fixed** | 0 (E.1 substrate had minimal surface; real adversarial coverage in E.2+) |
| **Audit passes run** | 1 in-phase cold-eye (E.1.5); formal post-feature both-angle audit deferred until E.2-E.3 ship concrete enhancement surface |
| **Phases planned** | E.1 → E.4 (4 phases + E.5+ polish queue) |
| **Last updated** | 2026-05-11 |
| **Cumulative tests** | 0 (planning) |
| **Real bugs surfaced + fixed** | — |
| **Audit passes run** | 0 |
| **Reference codebase** | Qud (`/Users/steven/qud-decompiled-project/XRL.World.Parts/IModification.cs` + 237 `Mod*.cs` files) |
| **Content reference** | `claude/ideas-gin-frogs` branch — `IDEAS.md` line 730 "Stones with Stake" |

---

## Why we're building this

The user's framing: **"enchant weapons with sigils and minerals."**

Qud's `IModification` system is a ~237-file Mod catalog. Each Mod is a
`Part` attached to an item that hooks events (combat damage, examine
display, on-hit, on-equip). The Mod lifecycle: `Configure` → `ApplyTier`
→ `ModificationApplicable(obj)` → `ApplyModification(obj)` →
`HandleEvent(...)` → `Remove()`. Each ~50-150 LOC. The Mods cover
offensive (serrated, freezing, hypervelocity), defensive (lacquered,
nanon, flexiweaved), utility (engraved, magnetized, spring-loaded),
and "magical" (timereaver, phase-conjugate, fatecaller) categories.

The user's `claude/ideas-gin-frogs` branch's `IDEAS.md` (line 730)
proposes the **"Stones with Stake"** mechanic — a CoO-native flavor of
this same pattern, where:

1. **Passive** stones grant ongoing effects while carried
2. **Infused** stones are forged into items (consumed in the process)
3. **Architectural** stones grant cell-level effects to chambers
4. Each major faction has coveted/avoided stones (politicized economy)
5. Pairs of stones interact (combinatorial crafting)
6. Some stones decay or corrupt over time (long-running character)

**The clean mapping:** Qud's `IModification` is the engine for category
#2 (Infused). Category #1 (Passive) is the existing Effect system
applied to inventory items. Category #3 (Architectural) is the
existing Cell tile system. The Faction-wanting / politics layer
is a new `WantsMineralPart` Part on NPCs.

**Sigils** in the user's framing: same architecture as minerals. A
sigil is just an item that, when applied via the Tinker action,
produces a Mod. Mineral-sourced vs sigil-sourced is a content tag,
not an architectural distinction.

---

## Phases overview

| Phase | What ships | Status | Tests (est) | Branch (planned) |
|---|---|---|---|---|
| **E.1** | `IItemEnhancement` infra + `EnhancementFactory` + `ItemEnhancing.Apply` + `IMeleeEnhancement` + adversarial | ✅ Shipped | 54 (planned 25-35; bonus coverage) | `feat/item-enhancements-e1-infra` |
| **E.2** | First 3 concrete enhancements (offensive + defensive + utility) | ⏳ Not started | 25-35 | `feat/item-enhancements-e2-first-three` |
| **E.3** | 3 mineral items + `WantsMineralPart` + crafting workflow | ⏳ Not started | 30-40 | `feat/item-enhancements-e3-minerals` |
| **E.4** | Showcase scenario + manual playtest closure | ⏳ Not started | 5-10 smoke | `feat/item-enhancements-e4-showcase` |
| **E.5+** | Polish queue: architectural mode, combinatorial crafting, more minerals/factions, decay/corruption | ⏳ Deferred | — | TBD |

---

## Working principles

Mirrored from CLAUDE.md. Constraints every phase follows:

1. **TDD per §2.1** — write the failing test first, confirm RED,
   implement minimum, confirm GREEN.
2. **Smallest blast radius first** (§1.4) — each sub-milestone ships
   as one reviewable, independently revertable change.
3. **Verification sweep before code** (§1.2) — every phase opens with
   a corrections table reading every reference the plan cites.
4. **Cold-eye review after multi-commit features** (§Q1-Q4).
5. **Adversarial sweep when 2+ taxonomy surfaces hit** — gated by the
   CLAUDE.md taxonomy.
6. **Qud parity over CoO-originals** — when Qud has a pattern that
   addresses the problem, use it. CoO-originals get a 🟡 or ⚪
   marker explaining the divergence.
7. **Post-feature audit gate — BOTH angles** (CLAUDE.md
   §Post-implementation cold-eye review):
   - **Angle A:** bug-class taxonomy (null safety, atomicity, etc.)
   - **Angle B:** Qud-parity-first (side-by-side with Qud source)
   - Both, not one. F.3 Followers proved the angles catch different
     bug classes (the `"Died"` hook bug was missed by Angle A and
     surfaced by Angle B).
8. **Empty-faction-string + edge-input parser tests** — the F.3
   GrantsRepAsFollowerPart parser needed several rounds of
   defensiveness. Future enhancement Parts that accept content-
   defined parameters (Faction lists, damage formulas, status names)
   should ship parser malformed-input tests from day one.

---

## Qud-parity status (forward-looking)

Plan target for each phase. ✅ = ported in plan; ⚪ = deferred; 🟡 = simplified.

| Qud feature | Plan target |
|---|---|
| `IModification` base class with `Configure`/`TierConfigure`/`Applicable`/`ApplyModification`/`Remove` lifecycle | ✅ E.1 — port as `IItemEnhancement : Part` |
| `Tier` field (int) + `ApplyTier(int)` + `TierConfigure()` scaling hook | ✅ E.1 |
| `ModificationFactory` registry (by-name + tinker-display-name lookup) | ✅ E.1 — port as `EnhancementFactory` |
| `ItemModding.ApplyModification(obj, name, tier)` static helper | ✅ E.1 — port as `ItemEnhancing.Apply(obj, name, tier)` |
| `IMeleeModification` filter (only applicable to melee weapons) | ✅ E.1 — port as `IMeleeEnhancement : IItemEnhancement` |
| `IModification.WantEvent` + `HandleEvent` for tinker-reverse-engineer flow | 🟡 E.1 v1 ships the lifecycle hooks; the reverse-engineer flow is content for E.5+ |
| Combat-event hooks (on-hit damage, on-melee, on-equip) | ✅ E.1+E.2 — three concrete enhancements exercise these in E.2 |
| `Examiner.Difficulty` / `Examiner.Complexity` adjustment (`IncreaseDifficulty`, etc.) | ⚪ E.5+ — Examiner system not in CoO yet |
| `TinkerItem` Part (tracks "bits required" for crafting) | 🟡 E.3 — mineral cost model is the simpler v1 equivalent (one mineral = one enhancement; no bit-economy) |
| 237 concrete Mod implementations | ⚪ Out of scope — CoO ships 3 in E.2 + 3 more via E.3 minerals + grows organically as content needs it |
| `Tinkering_ReverseEngineer` skill | ⚪ E.5+ — not v1 |
| Sifrah-minigame integration | ⚪ E.5+ (same as F.3's Sifrah deferral — entire subsystem missing in CoO) |
| `WishCommand("modify", ...)` for debug | 🟡 E.4 — debug menu entry to apply any enhancement to any item (similar to scenario-system pattern) |
| `Examiner.Difficulty` scaling | ⚪ E.5+ |
| `ModNanon` self-repair pattern (per-turn HandleEvent on `"EndTurn"`) | ⚪ One example in E.5+ if/when content needs it |

**CoO-original additions** (not in Qud, but in the gin-frogs IDEAS spec):

| CoO feature | Plan target |
|---|---|
| Mineral economy (3-category: Passive / Infused / Architectural) | E.3 ships the Infused category fully; Passive partial via tag-based effects; Architectural ⚪ E.5+ |
| Faction-wanting matrix (mineral × faction × rep delta) | ✅ E.3 — `WantsMineralPart` Part |
| Combinatorial crafting (mineral-pair recipes) | ⚪ E.5+ |
| Decay/corruption (Black-Gall corrodes its own item over uses) | ⚪ E.5+ (one mineral content example would exercise this) |
| Unique named relic stones (First-Root-Chip, Singing Pyrite, etc.) | ⚪ E.5+ content |
| Mineral-as-tinker-reagent vs mineral-as-passive-carry distinction | ✅ E.3 |

---

## Verification sweep

Per CLAUDE.md §1.2. E.1.1 sweep completed. Both original 🔴 blockers
resolved; one architectural finding (Examiner Part missing) confirmed
as already-deferred to E.5+ per plan.

| What I assume | What's there | Status |
|---|---|---|
| `Part.HandleEvent(GameEvent e)` virtual override | `Part.cs:49` — `public virtual bool HandleEvent(GameEvent e)`. Return `true` continues propagation; `false` consumes. Used by F.3 GrantsRepAsFollowerPart already. | ⚪ confirmed |
| Equip / unequip events | `EquipCommand.cs:131` fires `"BeforeEquip"` (vetoable); `EquipCommand.cs:175` fires `"AfterEquip"` (informational). Both carry `Actor`, `Item`, `Slot` parameters. **E.2.3 EnhancementLacquered + E.2.4 EnhancementEngraved hook here.** | ⚪ confirmed |
| On-hit / damage-dealt event | `CombatSystem.cs:741` fires `"DamageDealt"` on the attacker. Parameters: `Attacker`, `Defender`, `Amount` (int), `Damage` (object — the full Damage struct). **E.2.2 EnhancementSerrated hooks here.** | ⚪ confirmed |
| `MeleeWeaponPart.Attributes` field for damage-type filter | `MeleeWeaponPart.cs:49` — `public string Attributes = ""` (space-delimited, e.g. `"Cutting LongBlades"`). `IMeleeEnhancement.Applicable` will check via `Attributes.Contains(...)` or via the parsed `Damage.HasAttribute`. | ⚪ confirmed |
| `Damage.HasAttribute(string)` for attribute filter | `Damage.cs:103` — `public bool HasAttribute(string name)`. Also `HasAnyAttribute(List<string>)` for multi-match. Damage carries fully-populated attribute list at hit-resolution time. | ⚪ confirmed |
| Registry pattern template | `SkillRegistry.cs:26` — lazy-init via `EnsureInitialized()`, JSON-loaded from `Resources/Content/Data/Skills/*.json`. Four backing dicts: `_skillsByName`, `_skillsByClass`, `_powersByClass`, `_entriesByClass`. Lookup methods: `TryGetSkillByName`, `TryGetSkillByClass` (line 214-226). **`EnhancementFactory` will mirror this shape** but loads from `Resources/Content/Data/Enhancements/*.json` (TBD; E.1.3 may keep it code-side for v1, JSON in E.5+ content). | ⚪ confirmed |
| `Diag.DefaultOnCategories` array | `Diag.cs:119` — `private static readonly string[] DefaultOnCategories = { "event", "effect", "damage", "turn", "furniture", "trade", "quest", "skill" }`. **E.1.5 adds `"enhancement"` here** + the channel starts on by default. | ⚪ confirmed |
| `Diag.Record(category, kind, actor, target, payload)` | `Diag.cs:201` — exact signature `public static void Record(string category, string kind, Entity actor = null, Entity target = null, object payload = null, string cause = null)`. No-op if category disabled. | ⚪ confirmed |
| OnHit reuse pattern (existing `OnHitClassEffects` + `OnHitWeaponEffects`) | `OnHitWeaponEffects.cs:27-28` — `Apply(weapon, damage, actualDamage, defender, attacker, zone, rng)` called from `CombatSystem.PerformSingleAttack` after damage. **E.2.2 EnhancementSerrated likely reuses this hook point** OR listens to `DamageDealt` directly (cleaner — keeps enhancement code self-contained). E.2 sweep will lock the choice in. | 🟡 design call deferred to E.2 |
| Mineral item minimum Parts (carryable, not equippable) | `PhysicsPart { Takeable = true }` + `RenderPart`. **NO `EquippablePart`** — that's what marks it as a wieldable. `PickUpCommand` checks `PhysicsPart.Takeable` for inventory inclusion. | ⚪ confirmed |
| Inventory-action surface (originally 🔴 blocker for E.3) | `PerformInventoryActionCommand.cs:70` — 3-event pattern: `"BeforeInventoryAction"` (vetoable), `"InventoryAction"` (handled=true means success), `"AfterInventoryAction"` (informational). Each carries `Actor`, `Item`, `Command`, `Zone`. **E.3.4 mineral-apply action wires here**: mineral items have a Part listening for `"InventoryAction"` with command `"ApplyEnhancement"`. | ✅ blocker resolved |
| Tinker UX surface (originally 🔴 blocker for E.3) | The PerformInventoryActionCommand pattern IS the Tinker surface. Player → opens inventory on a mineral → selects "Apply Enhancement" → game prompts for a target item from inventory → mineral consumed + enhancement Part added to target. No new infra needed. | ✅ blocker resolved |
| Reflection-based Part save-load (SL.6) | `SaveSystem.cs:1126` generic fall-through (already used by F.3 GrantsRepAsFollowerPart). All enhancement Parts use this; round-trip pinned by per-enhancement tests. | ⚪ confirmed |
| `Examiner` Part (Qud's Difficulty/Complexity holder) | **Does not exist in CoO** — confirmed by file search. Already deferred to E.5+ per plan. No blocker for E.1-E.4. | ⚪ confirmed deferred |

**Both original 🔴 blockers resolved.** The `PerformInventoryActionCommand`
3-event pattern IS the tinker-action surface (inventory-action menu
visibility comes free), and the inventory-action menu wiring is the
same pattern. E.3 can proceed without new UI surface work.

**One design call deferred to E.2:** whether `EnhancementSerrated` hooks `DamageDealt`
directly (cleaner, enhancement-code-self-contained) or reuses
`OnHitWeaponEffects` (existing shared dispatch). Will lock in during E.2.1 sweep.

---

## Scope-prune

What Qud has that this plan **doesn't ship** (mapped to phases):

| Qud feature | Why deferred |
|---|---|
| `Examiner.Difficulty` / `Examiner.Complexity` scaling | E.5+ — Examiner system not in CoO yet |
| `Tinkering_ReverseEngineer` skill (player can disassemble Mods for bits) | E.5+ — content layer |
| `TinkerItem` Bits economy | 🟡 simplified to "one mineral consumed = one enhancement applied" |
| Sifrah minigame for tinkering | E.5+ — entire subsystem missing |
| 237 concrete Mod implementations | Out of scope — ship 3-6 across E.2/E.3, grow organically |
| `WishCommand` debug | 🟡 E.4 ships a smaller debug menu entry |
| Combinatorial crafting (Choir-Iron + Pale-Salt → anti-Choir composite) | E.5+ — content polish |
| Architectural-mode minerals (wall-stone palette per village) | E.5+ — needs new Cell-tile authoring layer |
| Decay/corruption (Black-Gall corrodes its item over uses) | E.5+ — one specific mineral content example |
| `*allvisible-equivalent` wildcard or content variations on the Faction-wanting matrix | E.5+ — content polish, can mirror F.3 GrantsRepAsFollowerPart's wildcard if desired |
| Unique named relic stones (First-Root-Chip, Singing Pyrite) | E.5+ — narrative content |

---

## Design lockdowns (pin before E.2 starts)

These are pinned in-doc before E.2 ships. E.1 (infrastructure) doesn't
need lockdowns; the contract is "port Qud's `IModification` shape."

### Lockdown #1 — Lifecycle method names

| Qud method | CoO equivalent | Why |
|---|---|---|
| `Configure()` | `Configure()` | Direct parity |
| `TierConfigure()` | `TierConfigure()` | Direct parity |
| `ApplyTier(int)` | `ApplyTier(int)` | Direct parity |
| `ModificationApplicable(GameObject)` | `Applicable(Entity)` | Shorter; `obj`→`Entity`; Modification-prefix dropped since the type name is `IItemEnhancement` |
| `ApplyModification(GameObject)` | `Apply(Entity)` | Same logic |
| `Remove()` | `Remove(Entity)` | Same logic; passing entity to mirror Effect's OnRemove style |
| `GetModificationDisplayName()` | `GetDisplayName()` | Standard CoO naming |

### Lockdown #2 — Tier semantics

Tier ranges 1–4 (mirrors Qud's `Tier` field defaults). Tier 1 is the
baseline; higher tiers scale the enhancement's primary number (damage
bonus, status duration, etc.) by `Tier × multiplier`. The multiplier
is per-enhancement; `Configure()` sets the base, `TierConfigure()`
scales.

### Lockdown #3 — Apply atomicity

`Apply(Entity item)` may add Parts, modify stats, or set Tags on the
item. If `Apply` fails partway (exception), the item's state must be
recoverable — track the changes in a private list and unroll on
exception. Mirrors F.3.4 `GrantsRepAsFollowerPart`'s atomicity fix
(Finding #8, audit pass 1). Eager-flag pattern recommended.

### Lockdown #4 — Diag emission contracts

Every enhancement Apply / Remove emits a record. Pinned schemas:

| Category | Kind | When | Payload |
|---|---|---|---|
| `enhancement` | `Applied` | `Apply` succeeded | `item`, `enhancement`, `tier`, `source` (e.g. "mineral:PaleSalt") |
| `enhancement` | `ApplyFailed` | `Apply` rejected (Applicable returned false, or atomic rollback) | `item`, `enhancement`, `tier`, `reason` |
| `enhancement` | `Removed` | `Remove` succeeded | `item`, `enhancement`, `tier` |

`category=enhancement` is a new diag channel — register in
`Diag.DefaultOnCategories` during E.1.

### Lockdown #5 — Save/load reach

Each enhancement Part follows the SL.6 reflection contract: all
public fields with simple types (int, string, bool, Entity), no
hidden non-serializable refs. Round-trip tests per enhancement
mirror the F.3.5 pattern. **Pin this in every E.2 sub-milestone's
test suite** — otherwise save/load drift accumulates silently.

### Lockdown #6 — Slot count

Each item can hold up to `MAX_ENHANCEMENTS_PER_ITEM = 2`
enhancements. Adding a 3rd is rejected at `Applicable` time. Matches
the gin-frogs IDEAS spec ("A weapon can hold one or two infusions;
trying to add a third destroys the weapon" — CoO ships veto-mode,
not destroy-mode, for v1).

---

# Phase E.1 — Enhancement infrastructure ⏳

## Goal

Port Qud's `IModification` pattern to CoO. Ship the base class, the
factory, the Apply/Remove helper, and the `IMeleeEnhancement`
specialization. NO concrete enhancements yet — those land in E.2.

## Qud reference

| Qud file | Symbol | Lines | What it does |
|---|---|---|---|
| `XRL.World.Parts/IModification.cs` | `IModification` abstract base | 1-274 | Tier + Configure/TierConfigure/Applicable/Apply lifecycle |
| `XRL.World.Parts/IMeleeModification.cs` | `IMeleeModification : IModification` | 1-50ish | `Applicable` filter for melee weapons |
| `XRL.World/ModificationFactory.cs` | static registry | full | By-name + tinker-display-name lookups |
| `XRL.World.Tinkering/ItemModding.cs` | `ApplyModification(obj, name, tier)` | full | Static helper that the wish-command + skill use |

## Sub-milestones

| # | What | Tests (est) |
|---|---|---|
| E.1.1 | Plan + verification sweep — confirm 🔴 blocker resolution paths (Tinker surface, inventory-action menu); pin design lockdowns | 0 |
| E.1.2 | `IItemEnhancement` abstract Part + lifecycle methods (Configure, TierConfigure, Apply, Remove) | 10-15 |
| E.1.3 | `EnhancementFactory` registry (`Register`, `GetByName`, `GetByDisplayName`) | 6-8 |
| E.1.4 | `ItemEnhancing.Apply(entity, name, tier)` static helper + slot-count veto (Lockdown #6) | 8-10 |
| E.1.5 | `IMeleeEnhancement : IItemEnhancement` filter + diag-channel registration + adversarial sweep + cold-eye + merge | 5-8 + adversarial |

**E.1 ship target: 30-40 tests, ~250 LOC infra, 0 concrete enhancements yet.**

---

# Phase E.2 — First 3 enhancements ⏳

## Goal

Validate the E.1 pattern by shipping 3 concrete enhancements that
exercise different event-hook categories. NOT mineral-sourced yet
(that comes in E.3); just the enhancement Parts attached via
`ItemEnhancing.Apply` directly.

## Three enhancements (one per category)

### E.2.2 — `EnhancementSerrated` (offensive)

| | |
|---|---|
| Qud parity | `XRL.World.Parts/ModSerrated.cs` |
| Effect | On-hit, X% chance to apply BleedingEffect to defender. Tier scales the chance. |
| Restriction | Cutting weapons only (`IMeleeEnhancement` + `Damage.HasAttribute("Cutting")` filter in Applicable) |
| Event hooks | Listen for the on-hit event the OnHitEffects feature already pipes through (verify exact name in E.2 sweep — likely `DamageDealt` or `OnDamage`) |
| Tests | Apply succeeds on cutting weapon; rejected on bludgeoning; bleed fires on hit; tier scales chance; save/load round-trip |

### E.2.3 — `EnhancementLacquered` (defensive)

| | |
|---|---|
| Qud parity | `XRL.World.Parts/ModLacquered.cs` |
| Effect | +N AC while equipped + small (<10%) chance per turn to repair 1 HP of item damage |
| Restriction | Armor or shield items |
| Event hooks | `OnEquip`/`OnUnequip` for AC bonus; per-turn for self-repair |
| Tests | AC bonus applied on equip + removed on unequip; tier scales repair chance; idempotent equip; save/load round-trip |

### E.2.4 — `EnhancementEngraved` (utility)

| | |
|---|---|
| Qud parity | `XRL.World.Parts/ModEngraved.cs` |
| Effect | On equip, +1 faction-rep with one named faction (the engraving's faction). On unequip, the bonus drops. |
| Restriction | Any equippable item |
| Event hooks | `OnEquip`/`OnUnequip` |
| Tests | Apply succeeds; rep flows on equip; rep reverses on unequip; double-equip idempotent; save/load preserves the AppliedBonus flag (mirrors F.3.4 atomicity) |

## Sub-milestones

| # | What | Tests (est) |
|---|---|---|
| E.2.1 | Plan + sweep — confirm on-hit event surface, equip/unequip event surface | 0 |
| E.2.2 | `EnhancementSerrated` + tests | 8-10 |
| E.2.3 | `EnhancementLacquered` + tests | 7-9 |
| E.2.4 | `EnhancementEngraved` + tests | 7-9 |
| E.2.5 | Adversarial sweep + cold-eye + audit pass (BOTH angles — taxonomy AND Qud-parity-first) + merge | 5-8 + adversarial |

**E.2 ship target: 25-35 tests, ~250 LOC content.**

---

# Phase E.3 — Mineral content + faction-wanting ⏳

## Goal

Ship the mineral economy layer: 3 mineral items + the `WantsMineralPart`
Part for the faction-wanting matrix + the Tinker action that consumes
a mineral and applies the corresponding enhancement.

## Three minerals (chosen for non-overlapping faction politics)

From the gin-frogs IDEAS spec:

| Mineral | Enhancement produced | Coveted by | Avoided by |
|---|---|---|---|
| **Pale-Salt** | `EnhancementPaleSalt` — +N bleed-bonus + preservation tag (inventory food doesn't spoil) | Pale Curation, Tent-Right wasteland cultures | Rot Choir, Driving Bloom |
| **Choir-Iron** | `EnhancementChoirIron` — anti-fungal aura (no Driving Bloom infection in 1-cell radius while equipped) | Pale Curation, Catacomb-villagers | Rot Choir, Driving Bloom |
| **Glow-Quartz** | `EnhancementGlowQuartz` — +1 light radius (extends the item-bearer's LightSource) | Catacomb-villagers, Bower-Folk, Saccharine Concord | (none structurally) |

All three are `IMeleeEnhancement`-applicable for v1 — keep the scope
narrow. Armor + accessory variants are E.5+ content.

## `WantsMineralPart`

New Part on NPCs. Public fields:
- `string Mineral` — comma-delimited list of mineral blueprint names this NPC wants (Qud-parity comma-delim like `GrantsRepAsFollowerPart`)
- `int RepReward` — rep delta on successful trade
- `string Faction` — which faction's rep moves (often the NPC's own faction)

On NPC interaction with the player carrying a wanted mineral, an
inventory-trade action surfaces. Trading the mineral consumes the
player's mineral + grants the rep + (optionally) drops a tinker-bit
reward.

## Sub-milestones

| # | What | Tests (est) |
|---|---|---|
| E.3.1 | Plan + sweep — resolve E.1's 🔴 Tinker-surface blocker; design the player-side application flow (Tinker activated ability? Workstation entity? Inventory action?) | 0 |
| E.3.2 | Mineral item blueprints (Objects.json entries for `PaleSalt`, `ChoirIron`, `GlowQuartz`) + a content-validation test that loads each | 5-7 |
| E.3.3 | `EnhancementPaleSalt` / `EnhancementChoirIron` / `EnhancementGlowQuartz` Parts | 12-18 |
| E.3.4 | Tinker action — player consumes a mineral from inventory + applies the corresponding enhancement to a held item | 8-10 |
| E.3.5 | `WantsMineralPart` + faction-rep flow on trade + tests | 7-10 |
| E.3.6 | Adversarial sweep + BOTH-angle cold-eye + merge | 5-8 + adversarial |

**E.3 ship target: 35-50 tests, ~400 LOC content + Part.**

---

# Phase E.4 — Showcase + playtest ⏳

## Goal

Manual-playtest validation. Player can:

1. Pick up a Mace + a Pale-Salt fragment + a Glow-Quartz fragment.
2. Activate Tinker → select Mace → select Pale-Salt → mace now has `EnhancementPaleSalt`.
3. Swing the mace at a Snapjaw → observe enhanced bleed effect.
4. Activate Tinker → select Mace → select Glow-Quartz → mace gains a second enhancement (light-radius bump).
5. Try to add a THIRD enhancement → rejected (`MAX_ENHANCEMENTS_PER_ITEM = 2`).
6. Trade Choir-Iron to a Pale-Curation NPC → faction-rep with Pale Curation increases.

## Sub-milestones

| # | What | Tests (est) |
|---|---|---|
| E.4.1 | `Scenarios/Custom/ItemEnhancementShowcase.cs` — preloaded player + minerals + NPCs | 1 smoke |
| E.4.2 | Menu wiring + playtest validation report | 0 (manual) |
| E.4.3 | Post-feature audit pass (BOTH angles) + final post-mortem in this doc | 0 + audit |

**E.4 ship target: 1 smoke test + playtest closure + post-mortem.**

---

# Phase E.5+ — Deferred polish ⏳

These are nice-to-have features without immediate gameplay need.
Each can land as a single-commit addition when its content shows up.

| Piece | Notes |
|---|---|
| `Examiner.Difficulty` / `Examiner.Complexity` scaling | Qud's tech-identification difficulty. CoO doesn't have Examiner yet. |
| `Tinkering_ReverseEngineer` skill | Player disassembles a Mod-item to recover Bits |
| Bits economy | TinkerItem's per-Mod resource cost. CoO v1 uses 1-mineral=1-enhancement instead |
| Sifrah minigame | Tinker-skill minigame. Whole subsystem missing in CoO. |
| Combinatorial crafting | Choir-Iron + Pale-Salt = stable anti-Choir composite |
| Architectural minerals (wall-stone palette) | Cell-tile authoring layer for chamber-level effects |
| Decay / corruption (Black-Gall over uses) | One mineral-content example |
| Unique named relic stones (First-Root-Chip, Singing Pyrite, Bower-Lapis Crown) | Narrative content |
| Multi-faction wanting (one mineral wanted by multiple factions at different rates) | Trade-route depth |
| More enhancements — port 6-10 more from Qud's Mod catalog as content needs them | Each ~70-100 LOC |
| Sigil-themed enhancements as content | Same architecture, different naming (sigil-source items rather than stone-source) |

---

## Cross-references

### Qud reference files

| File | What it informs |
|---|---|
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/IModification.cs` | E.1 abstract base shape |
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/IMeleeModification.cs` | E.1 melee filter |
| `/Users/steven/qud-decompiled-project/XRL.World/ModificationFactory.cs` | E.1 registry |
| `/Users/steven/qud-decompiled-project/XRL.World.Tinkering/ItemModding.cs` | E.1 Apply helper |
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/ModSerrated.cs` | E.2.2 reference |
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/ModLacquered.cs` | E.2.3 reference |
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/ModEngraved.cs` | E.2.4 reference |
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/TinkerItem.cs` | E.3 Tinker action reference |

### CoO context files (already-shipped infra this builds on)

| File | What it provides |
|---|---|
| `Assets/Scripts/Gameplay/Entities/Part.cs` | The base `Part` class enhancements extend |
| `Assets/Scripts/Gameplay/Events/GameEvent.cs` | Event dispatch for HandleEvent hooks |
| `Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs` | Melee weapon classification (for `IMeleeEnhancement` filter) |
| `Assets/Scripts/Gameplay/Effects/Concrete/BleedingEffect.cs` | Reused by `EnhancementSerrated` |
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs` | `WritePublicFields` reflection save (SL.6 contract) |
| `Assets/Scripts/Gameplay/AI/PlayerReputation.cs` | `Modify(faction, delta, silent)` for `WantsMineralPart` rep flow |
| `Assets/Scripts/Gameplay/AI/GrantsRepAsFollowerPart.cs` | Pattern reference for `WantsMineralPart` (same Qud-parity comma-delim shape) |
| `Assets/Scripts/Gameplay/Skills/BaseSkillPart.cs` | If Tinker becomes a skill, this is the base |
| `Assets/Scripts/Gameplay/Inventory/InventoryPart.cs` | For inventory-side mineral storage + consumption |

### Content-design reference

| File | What it informs |
|---|---|
| `claude/ideas-gin-frogs` branch's `IDEAS.md` line 730 — "Stones with Stake" | Full mineral content spec (9 minerals + 12-faction politics map + combinatorial crafting + open design questions) |

---

## Open design questions

Carry forward through phases:

- **Tinker surface UX** — player activates an ability? Bumps into a workstation? Picks "Tinker" from an inventory-action menu? E.1 sweep must resolve.
- **Where do minerals come from?** — mining cells (worldgen feature)? Drops from enemies? Trade with NPCs? Faction-quest rewards? Probably all four, but E.3 v1 ships a single source (NPC drops + scenario-seeded inventory) and grows from there.
- **Inventory weight model** — gin-frogs IDEAS notes stones are heavy. Does CoO's inventory have a weight system? If yes, set per-mineral weights. If no, defer.
- **Slot-count enforcement nuance** — what happens if a player tries to apply a 3rd enhancement? Veto (Lockdown #6) OR Qud-style destroy-the-item? F.3-pattern says veto. Revisit after playtest.
- **Visual signal for enhanced items** — when an item has enhancements, does its render color change? Does its name get a prefix ("serrated mace")? Mirror Qud's `GetDisplayNameEvent` pattern (HandleEvent on the item).
- **Faction-wanting inventory action visibility** — does the trade option appear in the NPC dialogue automatically, or does the player have to know to ask? Qud has it auto-surface via `GetInventoryActionsEvent`. CoO needs an equivalent — probably hook the existing conversation-action surface.
- **What's the "remove enhancement" path?** — Qud's `Tinkering_ReverseEngineer` skill. CoO v1 might not need this if the cost of mistakes is low; if mistakes become punishing in playtest, add `EnhancementRemoval` action.
- **Cross-cutting with weapon-quality/durability?** — does CoO have a quality system? If yes, do enhancements interact with quality (e.g., legendary mace + serrated stacks differently)? F.3-pattern recommends keeping the systems orthogonal for v1.

---

*This file is updated as each sub-milestone ships. Cumulative status
+ phase post-mortems + cross-refs all live here.*
