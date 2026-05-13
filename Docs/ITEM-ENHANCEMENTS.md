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
| **Current phase** | **E.5.1 deep-audit ✅ shipped — 3 real bugs found + fixed**; E.5+ polish queue active |
| **Cumulative tests** | **322** across E.1 + E.2 + E.3 + E.4 + E.5.1 — all green, no fixtures skipped |
| **Real bugs surfaced + fixed** | E.2.5 compile fix; E.3.4 substrate gap (auto-discovery); E.4.2 latent atomicity (dispatcher transaction wrap); **E.5.1 deep audit caught 3 more REAL bugs**: (1) Apply-to-already-equipped didn't fire OnEquipped → Lacquered/Engraved/GlowQuartz silently no-op on Tinker-to-worn-item; (2) Remove-from-equipped didn't fire OnUnequipped → bonus stayed applied after Part destroyed; (3) DispatchOnHit iterator skipped parts when a hook self-removed. **First two are gameplay-visible silent failures** in the Tinker flow. |
| **Audit passes run** | E.1.5 cold-eye; E.2.5 adversarial sweep + cold-eye; E.3.1 verification sweep; E.3.6 adversarial sweep; E.4.3 cumulative BOTH-angle cold-eye (5 findings); **E.5.1 second-round deep audit (12 RED tests written first — 3 confirmed bugs RED→GREEN + 4 pinned-as-correct invariants + 1 cross-system integration + plumbed Tinker shim through to OnEquipped firing)** |
| **Phases planned** | E.1 → E.4 (4 phases + E.5+ polish queue) |
| **Last updated** | 2026-05-11 |
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
| **E.2** | First 3 concrete enhancements (Serrated/Lacquered/Engraved) + ItemEnhancementDispatch + adversarial | ✅ Shipped | 92 (planned 25-35; bonus coverage incl. cross-enhancement) | `feat/item-enhancements-e2-first-three` |
| **E.3** | 3 mineral items + 3 Enhancement Parts (tag-bonus base + Pale-Salt + Choir-Iron + Glow-Quartz) + EnhancementFactory auto-discovery + 3 Tinker recipe shims + `WantsMineralPart` + `MineralTradeService` + adversarial | ✅ Shipped | 111 (10 mineral content + 14 Glow-Quartz + 28 tag-bonus + 16 Tinker + 18 WantsMineralPart + 20 adversarial + 5 substrate touch-ups) | `feat/item-enhancements-e3-minerals` |
| **E.4** | `ItemEnhancementShowcase` scenario + probe Part + menu entry + smoke test + BOTH-angle cold-eye review (5 findings → 1 latent 🟡 atomicity fix + 1 🟡 hypothesis pinned as already-correct + 3 minor cleanups) | ✅ Shipped | 9 (1 smoke + 4 transaction tests + 3 MaterialPart round-trip + 1 Configure-vs-TierConfigure pin) | `feat/item-enhancements-e4-showcase` |
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

# Phase E.2 — First 3 enhancements ✅

## Goal

Validated the E.1 pattern by shipping 3 concrete enhancements that
exercise different event-hook categories (on-hit, on-equip/unequip,
faction-rep). NOT mineral-sourced yet (that's E.3); attached via
`ItemEnhancing.Apply` directly.

## Verification sweep (completed before E.2.2)

| Premise | Status | Source |
|---|---|---|
| `CombatSystem.PerformSingleAttack` has an `if (hpAfter > 0)` block already running on-hit pipelines (OnHitClassEffects, OnHitWeaponEffects) | ✅ confirmed | `CombatSystem.cs:333-344` |
| `EquipCommand.cs` fires `AfterEquip` event at line 175-179 — clean insertion point for DispatchOnEquip | ✅ confirmed | `EquipCommand.cs:175-179` |
| `UnequipCommand.cs` has TWO paths (`Execute` + `UnequipAndRemove`) — both need DispatchOnUnequip for throw-while-equipped coverage | ✅ confirmed | `UnequipCommand.cs:93-96, 152-155` |
| `Part.ParentEntity` is the canonical "Part → its Entity" reference | ✅ confirmed | `Part.cs:13` |
| `Damage.HasAttribute("Cutting")` is the canonical check for Cutting class | ✅ confirmed | `Damage.cs:99-103` |
| `BleedingEffect(saveTarget, damageDice, rng)` is the ctor — defaults 15 / "1d2" / null | ✅ confirmed | `BleedingEffect.cs:20` |
| `PlayerReputation.Modify(faction, delta, silent)` is the rep API | ✅ confirmed | `PlayerReputation.cs:73` |
| Qud's `ModSerrated` is actually a **dismember** mod (3% chance), not a bleed mod | ⚠️ corrected | `qud-decompiled-project/XRL.World.Parts/ModSerrated.cs:109-113` |
| Qud's `ModLacquered` is liquid-repulsion / rust-immunity — the +AV mod is `ModReinforced` | ⚠️ corrected | `qud-decompiled-project/XRL.World.Parts/ModLacquered.cs` + `ModReinforced.cs` |

**Two false premises corrected during E.2 sweep (per CLAUDE.md §1.2
"saved an estimated 2-3 days").** Documented as scope divergences in
each enhancement's class doc.

## Three enhancements (one per category)

### E.2.2 — `EnhancementSerrated` (offensive) ✅

| | |
|---|---|
| Qud parity | `XRL.World.Parts/ModSerrated.cs` — name only; mechanic diverges (see below) |
| Effect | OnAttackerHit: rolls tier-scaled chance (10% per tier, 1-4) → applies `BleedingEffect` to defender |
| Restriction | Cutting melee weapons only (`IMeleeEnhancement` + `weapon.Attributes.Contains("Cutting")`) |
| Event hooks | `OnAttackerHit` (via `ItemEnhancementDispatch.DispatchOnHit` from `CombatSystem.PerformSingleAttack`) |
| Tests | 22 (Applicable filter, tier scaling, OnAttackerHit semantics, diag emission, round-trip, adversarial trigger-rate) |
| Scope divergence | Qud's ModSerrated rolls a 3% dismember; CoO can't easily reach `CombatSystem.CheckCombatDismemberment` (private) from outside, so substituted bleed bonus. Documented in class doc. |

### E.2.3 — `EnhancementLacquered` (defensive) ✅

| | |
|---|---|
| Qud parity | `XRL.World.Parts/ModReinforced.cs` (mechanic) — kept name from `ModLacquered.cs` (thematic) |
| Effect | OnEquipped: `ArmorPart.AV += Tier`; OnUnequipped: subtract back. Net change zero across cycle. |
| Restriction | Armor only (`item.GetPart<ArmorPart>() != null`) |
| Event hooks | `OnEquipped` + `OnUnequipped` (via `ItemEnhancementDispatch`) |
| Tests | 17 (Applicable filter, tier scaling, AV mutation symmetry, atomicity flag, double-equip idempotent, unequip-without-equip guard, round-trip with AppliedBonus preserved) |
| Scope divergence | Qud's ModLacquered = liquid-repulsion + rust-immunity. CoO doesn't have liquids/rust systems yet. Shipped the +AV mechanic from `ModReinforced` under the user-preferred "Lacquered" thematic name. |

### E.2.4 — `EnhancementEngraved` (utility) ✅

| | |
|---|---|
| Qud parity | CoO-original mechanic (Qud has faction-sigil items but no direct rep-modifier mod) |
| Effect | OnEquipped (player only): `PlayerReputation.Modify(Faction, +Tier*5)`. OnUnequipped: subtract back. NPC equipping is a no-op. |
| Restriction | Any equippable item (`EquippablePart`) |
| Event hooks | `OnEquipped` + `OnUnequipped` |
| Tests | 18 (Applicable, tier scaling, player vs NPC equip gate, faction-unset no-op, double-equip atomicity, round-trip with Faction string + AppliedBonus) |
| Atomicity | F.3.4 GrantsRepAsFollowerPart lesson — `AppliedBonus` flag set EAGERLY before the rep mutation, symmetric guard in unequip prevents subtract-without-apply on stale-loaded equipped state. |

## Sub-milestones (all shipped)

| # | What | Tests | Status |
|---|---|---|---|
| E.2.1 | `ItemEnhancementDispatch` static helper + `OnAttackerHit`/`OnEquipped`/`OnUnequipped` virtual hooks on `IItemEnhancement` + hook into CombatSystem + EquipCommand + UnequipCommand | 13 | ✅ commit `62a6a45` |
| E.2.2 | `EnhancementSerrated` + tests | 22 | ✅ shipped |
| E.2.3 | `EnhancementLacquered` + tests | 17 | ✅ shipped |
| E.2.4 | `EnhancementEngraved` + tests | 18 | ✅ shipped |
| E.2.5 | `E2EnhancementsAdversarialTests` + cold-eye + merge | 22 | ✅ in-progress |

**E.2 actual ship: 92 tests, ~600 LOC content + 1100 LOC tests.**

## E.2 cold-eye review (Methodology Template §Q1-Q4)

- **Q1 Symmetry** — Lacquered's OnUnequipped is structurally
  symmetric to OnEquipped (subtract is the inverse of add); Engraved
  same. Serrated has no symmetric pair (one-shot proc). Symmetry
  check passes.
- **Q2 Cross-feature consistency** — All three concrete enhancements
  emit diag under `category="enhancement"`. Kinds: `BonusApplied`/
  `BonusRemoved` (equip-style); `Triggered` (proc-style). Payload
  shape is consistent: every record names `enhancement` + `tier`. No
  divergence found.
- **Q3 Counter-check completeness** — Each positive-assertion test
  has a paired counter-check (Cutting↔Bludgeoning, player↔NPC,
  faction-set↔faction-empty, double-equip↔no-op, etc.).
- **Q4 Doc-vs-impl drift** — Class docs updated alongside code in
  each commit. This doc-section update closes the design-doc loop.

---

# Phase E.3 — Mineral content + faction-wanting ⏳

## Goal

Ship the mineral economy layer: 3 mineral items + the `WantsMineralPart`
Part for the faction-wanting matrix + Tinker recipes that consume
a mineral and apply the corresponding enhancement.

## Verification sweep (2026-05-12)

Per CLAUDE.md §1.2 "single highest-leverage step in the protocol."
Sweep ran an Explore agent across 10 question surfaces before any
code was written. Findings table:

| Plan claim | Reality | Action |
|---|---|---|
| Pale-Salt "preservation tag — inventory food doesn't spoil" | ❌ Aspirational — `FoodPart.cs` has Healing/Message/Cooking only; no spoilage mechanic exists | **Scope-prune.** Defer preservation passive until food-spoilage system ships (E.5+ candidate). Ship Pale-Salt as the "vs-Undead bonus damage" mineral instead (real mechanic; faithful to IDEAS.md's "Pale-Salt-edged weapons inflict bonus damage on... undead-tier enemies"). |
| Choir-Iron "anti-fungal aura — no Driving Bloom infection in 1-cell radius" | ❌ Aspirational — no `DrivingBloomEffect`, no infection mechanic, no fungal-status implementation | **Scope-prune.** Defer aura passive until Driving Bloom effect ships. Ship Choir-Iron as the "vs-Fungal bonus damage" mineral (IDEAS.md's "weapons resist Choir colonization" + "armor reduces Bloomed status duration" both presuppose Choir infrastructure that doesn't exist). |
| Glow-Quartz "+1 light radius" | ✅ Real — `LightSourcePart.cs:14` has `public int Radius = 6`; `LightMap.cs` reads both entity + equipped-item LightSourceParts | **Ship as-spec.** OnEquipped extends bearer's LightSource by +Tier radius (adds a LightSourcePart if missing). |
| `WantsMineralPart` mirrors `GrantsRepAsFollowerPart` comma-delim pattern | ✅ Real — `GrantsRepAsFollowerPart.cs:72, 156-166, 212-234` has the parser; field shape "Faction[:N],Faction[:N]" | **Mirror the pattern.** Copy the parsing or refactor into a shared utility. |
| Tinker / crafting infrastructure exists | ✅ Real — `TinkerRecipeRegistry` + `TinkerRecipe` (Type/Ingredient/TargetBlueprint/TargetPart), Tinker NPC spawns at 70% in villages | **Add a recipe type.** E.3.4 ships `Type: "EnhancementApply"` recipes. NOT the E.1 🔴 blocker the plan feared. |
| Blueprint JSON loader supports new Parts | ✅ Real — `BlueprintLoader.cs` + `EntityFactory.cs` use reflection; new Part types auto-load | **No registration needed.** New `Enhancement*` classes load from JSON automatically. |
| Faction registry (7 cited factions) | ⚠️ Partial — registered: `PaleCuration`, `RotChoir`, `SaccharineConcord`, `Villagers`, `Snapjaws`, `Palimpsest`, `GlassblownRemnant`, `Cultists`. Missing: `DrivingBloom`, `CatacombVillagers`, `BowerFolk`, `TentRight` | **Use registered factions only for v1.** Lore-faction stubs (BowerFolk etc.) added when WantsMineralPart needs them. |
| Existing mineral blueprints in Objects.json | ❌ None — `MaterialID:"Iron"` etc. are crafting materials, not collectible item entities | **Create 3 new blueprints.** E.3.2. |
| `Fungal` / `Undead` MaterialTagsRaw on creatures | ✅ Real — `SporeShambler` has `MaterialTagsRaw:"Organic,Fungal,Living"`; various skeletons have `"Bone,Dry,Undead"` etc. `MaterialPart.HasMaterialTag(string)` is the query API | **Ground the bonus-damage mechanic in the existing tag system.** |
| Player inventory action surface | ✅ Real — `InventoryAction` framework; FoodPart's "Eat" is the template | **N/A for v1.** Minerals get applied via Tinker recipes, not inventory-action — keeps the surface narrow. (Direct-apply inventory-action is E.5+ polish.) |

**Two false premises caught (food spoilage + Driving Bloom infection)
before any code.** Per CLAUDE.md §1.2 lesson — verification swept this
exact pair of aspirational mechanics out of scope, saving the rebuild
cost when their dependencies later don't exist.

## Three minerals — REVISED for v1 (mechanics-grounded)

| Mineral | Enhancement produced | Mechanic (v1) | Forward-compat with full IDEAS.md design |
|---|---|---|---|
| **Pale-Salt** | `EnhancementPaleSalt` | OnAttackerHit: +Tier flat damage if defender has `Undead` MaterialTag | Faithful to IDEAS.md "bonus damage on... undead-tier enemies." Preservation passive deferred to E.5+. |
| **Choir-Iron** | `EnhancementChoirIron` | OnAttackerHit: +Tier flat damage if defender has `Fungal` MaterialTag | Faithful to IDEAS.md "weapons resist Choir colonization" intent — currently expressed as bonus damage vs Fungal-tagged. "Bloomed status duration" passive deferred. |
| **Glow-Quartz** | `EnhancementGlowQuartz` | OnEquipped: `bearer.LightSourcePart.Radius += Tier` (auto-creates LightSourcePart if missing). OnUnequipped: subtracts back. AppliedBonus atomicity. | Direct map to IDEAS.md "Glow-Quartz-tipped lantern-rods extend bio-light range substantially." |

All three are `IMeleeEnhancement`-applicable for v1 — keep the scope
narrow. Armor + accessory variants are E.5+ content. **Wait — actually:**
GlowQuartz makes more sense as a generic `IItemEnhancement` (any
equippable) since lantern-tipped lights aren't melee-weapon-specific
in IDEAS.md. The two bonus-damage minerals stay `IMeleeEnhancement`.

## Faction politics (v1 with registered factions)

| Mineral | Coveted by (real factions) | Avoided by (real factions) | Lore stubs (deferred until added to FactionManager) |
|---|---|---|---|
| **Pale-Salt** | PaleCuration | RotChoir | Tent-Right; Driving Bloom |
| **Choir-Iron** | PaleCuration, Villagers | RotChoir | Catacomb-villagers; Driving Bloom |
| **Glow-Quartz** | SaccharineConcord, Villagers | (none) | Catacomb-villagers; Bower-Folk |

WantsMineralPart instances on real-faction NPCs trade-rep on delivery.
Lore-faction-specific WantsMineralPart entries are E.5+ polish (when
Factions.json grows).

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

| # | What | Tests | Status |
|---|---|---|---|
| E.3.1 | Verification sweep + plan-doc revisions | 0 | ✅ commit `1771188` |
| E.3.2 | Mineral item blueprints in Objects.json + content-validation | 10 | ✅ shipped |
| E.3.3 | `EnhancementGlowQuartz` + `EnhancementTagBonusBase` + `EnhancementPaleSalt` + `EnhancementChoirIron` Parts + tests | 42 | ✅ shipped |
| E.3.4 | Tinker recipe shims (3) + `EnhancementFactory.EnsureInitialized` auto-discovery + Recipes_V1.json entries | 16 | ✅ shipped |
| E.3.5 | `WantsMineralPart` + `MineralTradeService` + 18 tests | 18 | ✅ shipped |
| E.3.6 | Adversarial sweep + cold-eye review | 20 | ✅ shipped |

**E.3 actual ship target: 60-90 tests (revised up from 35-50 since each
Enhancement Part needs full tag-check counter-checks), ~500 LOC.**

---

# Phase E.4 — Showcase + playtest ✅

## Goal

Manual-playtest validation + final cross-phase audit pass.

Player flow (verifiable in the live game by spawning the scenario):

1. Pick up minerals from inventory (pre-staged: 3 PaleSalt, 2 ChoirIron, 2 GlowQuartz).
2. Walk to Tinker NPC W. Activate Tinker → select LongSword → select "Infuse with Pale-Salt" → weapon now has `EnhancementPaleSalt`.
3. Swing the now pale-salt-edged LongSword at the SkeletalSentry N → observe TWO `[E4Demo]` log lines per swing (primary + bonus damage).
4. Counter-check: swing the same weapon at the Snapjaw NE (no Undead tag) → only ONE `[E4Demo]` line, no bonus.
5. Apply Choir-Iron via Tinker → second enhancement attaches (slot-cap=2 now full).
6. Try to apply a THIRD enhancement → rejected with "Item already has the maximum number of enhancements."
7. Apply Glow-Quartz to spare Mace via Tinker → equip Mace → observable light radius increase around player.
8. Walk to Pale Curation Scribe SW. Trade Pale-Salt → "Your reputation with PaleCuration improves." Trade Choir-Iron → second improvement. Try to trade Glow-Quartz → "not_wanted" rejection.

## Sub-milestones (all shipped)

| # | What | Tests | Status |
|---|---|---|---|
| E.4.1 | `ItemEnhancementShowcase.cs` + `ItemEnhancementDemoProbePart` + menu entry + smoke test | 1 smoke | ✅ commit `45c8c8a` |
| E.4.2 | BOTH-angle cold-eye review + 5 findings addressed (1 latent 🟡 atomicity fix + 1 🟡 hypothesis falsified with pin tests + 3 minor cleanups) | 8 | ✅ shipped |
| E.4.3 | Final doc closeout + cross-phase post-mortem + merge | 0 | ✅ this commit |

## E.4.3 cross-phase post-mortem (BOTH-angle cold-eye review)

The audit ran across the FULL E.1 + E.2 + E.3 + E.4 surface (16 source
files + 16 test fixtures). 5 findings surfaced; all addressed inline
in commit E.4.2.

**Angle A — bug-class taxonomy:**

- 🟡 **Finding #1 (FIXED)** — `EquipCommand` + `UnequipCommand` fire
  `ItemEnhancementDispatch.DispatchOnEquip/Unequip` OUTSIDE the
  `InventoryTransaction`, so a rollback after equip-success leaves
  enhancement mutations permanent. Today no caller does that, but the
  pattern matches the F.3.4 atomicity lesson. Fix: wrap each
  dispatcher in `transaction.Do(apply: null, undo: () => DispatchOpposite(...))`.
  The concrete enhancements' `AppliedBonus` eager-flag makes the
  undo idempotent. 4 new tests in `EnhancementDispatchTransactionTests`.

- 🟡 **Finding #2 (FALSIFIED)** — audit hypothesized that
  `MaterialPart.MaterialTags` HashSet wouldn't survive save/load
  reflection (`Initialize()` only fires on `AddPart`, not on load),
  silently breaking Pale-Salt / Choir-Iron tag-bonus damage on
  loaded saves. Wrote 3 RED-then-GREEN tests. ALL 3 PASS — the
  reflection serializer evidently round-trips the HashSet directly.
  The tests are now valuable regression pins on the actual
  correct behavior; the audit hypothesis was wrong.

- 🔵 **Finding #3 (FIXED)** — `EnhancementFactory.ResetForTests`
  semantics were subtle: after `Reset`, `EnsureInitialized` becomes
  a no-op. Docstring rewrite makes the gotcha explicit, with pointer
  to `ForceReinitialize` for tests that want auto-discovery.

- 🧪 **Finding #5 (FIXED)** — added test pinning that `ApplyTier(N)`
  doesn't clobber Configure-set stable fields (SaveTarget, DamageDice
  in EnhancementSerrated). Catches a future change accidentally
  moving stable-field init into TierConfigure.

**Angle B — Qud-parity-first:**

- 🔵 **Finding #4 (FIXED)** — `EnhancementLacquered`'s Qud-parity
  docstring overclaimed. Qud's `ModReinforced.ModificationApplicable`
  restricts +AV to body/back slots; CoO's Applicable accepts any
  ArmorPart. Docstring now enumerates 3 divergences (name swap +
  mechanic swap + slot-filter divergence).

- ⚪ **Finding #6 (documented)** — Qud's `WorksOnSelf=true` semantic
  is intentionally not ported. CoO's `Apply(item)/OnEquipped(actor,item)`
  signature distinction subsumes the concept.

**Process check:** the E.4.3 audit ran BOTH angles per the
CLAUDE.md "Run BOTH audit angles" directive. Angle A surfaced 3 of 5
findings; Angle B surfaced 1 of 5; Angle A also surfaced the
documented-but-fine ⚪ Finding #6. The split confirms the audit
guidance — neither angle alone would have caught all 5.

## Final feature post-mortem (E.1 → E.4)

### What shipped

| Layer | What |
|---|---|
| **E.1** | `IItemEnhancement` abstract base (Configure → ApplyTier → TierConfigure → Applicable → Apply → Remove lifecycle); `EnhancementFactory` registry with reflection auto-discovery; `ItemEnhancing.Apply/Remove` API with 6 rejection paths each emitting `enhancement/ApplyFailed` diag + reason; `IMeleeEnhancement` filter base; `ItemEnhancementDispatch` content-hook dispatcher with 3 hooks (OnAttackerHit, OnEquipped, OnUnequipped) + transactional rollback wiring (E.4.2 fix); slot-cap of 2 per item (Lockdown #6) |
| **E.2** | 3 concrete enhancements covering distinct mechanical templates: `EnhancementSerrated` (Cutting-melee → tier-scaled bleed chance), `EnhancementLacquered` (armor → tier-scaled +AV with `AppliedBonus` atomicity), `EnhancementEngraved` (any equippable → tier-scaled faction rep with player-only gate + F.3.4 atomicity) |
| **E.3** | 3 mineral blueprints (PaleSalt / ChoirIron / GlowQuartz) in Objects.json; `EnhancementTagBonusBase` abstract base for the bonus-damage-vs-tag pattern + 2 concrete subclasses (PaleSalt vs Undead, ChoirIron vs Fungal); `EnhancementGlowQuartz` (any equippable → LightSourcePart radius extension); 3 Tinker recipe shims bridging the older `ITinkerModification` system to the new `IItemEnhancement` system; Recipes_V1.json entries; `WantsMineralPart` + `MineralTradeService` for player↔NPC mineral trade |
| **E.4** | `ItemEnhancementShowcase` scenario exercising the full loop; menu entry; smoke test; BOTH-angle cold-eye review with 5 findings addressed |

### Counters

| Counter | E.1 | E.2 | E.3 | E.4 | Total |
|---|---|---|---|---|---|
| Tests added | 54 | 92 | 111 | 9 | **308** |
| Commits | 5 | 5 | 7 | 4 (incl. cold-eye fixes) | **21** |
| Scope-prunes documented | 0 | 3 (Serrated dismember→bleed; Lacquered liquid-repel→AV; Engraved CoO-original) | 3 (PaleSalt preservation passive; ChoirIron aura passive; 4 lore-factions to E.5+) | 0 | **6** |
| False premises caught BEFORE code | 0 | 2 (Qud ModSerrated≠Bleed mod; Qud ModLacquered=liquid-repel) | 2 (food-spoilage; Driving Bloom infection) | 0 | **4** |
| Real gameplay bugs surfaced | 0 | 0 | 0 | 0 | **0** |
| Substrate gaps fixed (in-flight) | 0 | 1 (StatusEffectsPart.Effects compile error) | 1 (EnhancementFactory production registration missing) | 1 (transaction.Do wrap on dispatcher) | **3** |

### What's still open for E.5+

- Player-facing dialog UI for `MineralTradeService.TryTrade` (service-layer ready)
- 4 lore-faction stubs (DrivingBloom, CatacombVillagers, BowerFolk, TentRight)
- Food-spoilage system → unlocks Pale-Salt's preservation passive
- Driving Bloom effect / infection mechanic → unlocks Choir-Iron's anti-fungal aura
- Per-recipe Tier reading from the consumed mineral's Tags (currently hardcoded in each shim)
- `ITinkerModification` ↔ `IItemEnhancement` unification
- Slot-filtered `EnhancementLacquered` matching Qud's body/back-only restriction
- A proper PaleCuration NPC blueprint (currently the showcase inline-builds one)

### Lessons that earned their place in CLAUDE.md

- **§1.2 verification sweep is the highest-leverage step.** Caught 4 false premises across E.2 + E.3 BEFORE any code; saved an estimated 3-4 days of building against fictional APIs.
- **§5 in-phase self-review with severity markers + fix 🟡+ pre-commit.** Caught the StatusEffectsPart compile error during E.2.5's Unity-reconnect verification.
- **BOTH audit angles, not one.** The E.4.3 audit's split (3 findings from Angle A, 1 from Angle B, 1 documented-fine from A) confirms the F.3 Followers lesson — a single angle has blind spots.
- **0 real gameplay bugs across 4 phases.** Not because the feature is trivial — it's 21 commits, 308 tests, 3 substrate gaps, 6 documented scope-prunes. The verification sweep + RED→GREEN per-sub-milestone + adversarial gate + cold-eye reviews compound into "the bugs got caught before they could ship."

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
