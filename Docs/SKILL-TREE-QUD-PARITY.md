# Skill Tree — Qud parity port plan

> Pre-impl architecture survey of Caves of Qud's skill system from the
> 5385-file decompile at `/Users/steven/qud-decompiled-project/`,
> mapped to a Caves of Ooo parity port. **No code in this commit** —
> this is the plan-to-disk per CLAUDE.md major-feature workflow §1.1.

---

## Source-of-truth files read (Qud)

| File | LOC | Role |
|---|---|---|
| `XRL.World.Skills/SkillFactory.cs` | 305 | XML loader; static singleton (`Factory`); maintains `SkillList`, `SkillByClass`, `PowersByClass`, `EntriesByClass`. |
| `XRL.World.Skills/SkillEntry.cs` | 57 | Data shape for one skill tree. Has `PowerList` + `Powers` dict; `Initiatory` flag forces order-of-purchase. |
| `XRL.World.Skills/PowerEntry.cs` | 230 | Data shape for one power within a tree. Has `Minimum` (stat requirements), `Requires` (prereq powers), `Exclusion` (mutually exclusive powers). |
| `XRL.World.Skills/PowerEntryRequirement.cs` | 58 | Helper that pairs `Attribute` lists (e.g. "Agility,Ego") with `Minimum` lists ("18,12") to OR/AND groups. |
| `XRL/IBaseSkillEntry.cs` | 65 | Abstract base for `SkillEntry` and `PowerEntry`. Inherits `IPartEntry`; connects entry to a runtime `Part` class via `Class` string. |
| `XRL.World.Parts/Skills.cs` | 235 | Per-creature manager Part. `SkillList<BaseSkill>`. `AddSkill`, `RemoveSkill`. Static `GetGenericSkill` for stateless reflection-based queries. |
| `XRL.World.Parts.Skill/BaseSkill.cs` | 153 | Abstract base for the **per-skill runtime class**. 173+ concrete subclasses in `XRL.World.Parts.Skill/`. |
| `XRL.World.Parts.Skill/Acrobatics.cs` | 9 | Empty marker class — represents owning the Acrobatics tree. |
| `XRL.World.Parts.Skill/Acrobatics_Dodge.cs` | 21 | Concrete power: applies +2 DV stat-shift on `AddSkill`, removes on `RemoveSkill`. The simplest "passive bonus" pattern. |

(173 concrete skill/power C# files total, organized as `<TreeName>.cs` for the tree-root and `<TreeName>_<PowerName>.cs` for each power.)

---

## Architecture summary (Qud)

Five distinct layers:

1. **Definition** — `Skills.xml` (data file, loaded at startup). Tree of `<skills>` → `<skill>` → `<power>` nodes with cost/stat-min/requires/exclusion attributes.

2. **Registry** — `SkillFactory` (static singleton). Loads XML once, exposes by-name and by-class lookups. Owns the immutable `SkillEntry` / `PowerEntry` instances.

3. **Per-creature state** — `Skills` Part on actors that participate in the skill economy (Player + named NPCs that learn skills via water-ritual; not random monsters). The `Skills.cs:208` wish-handler does `Player?.GetPart<Skills>()` with a null-check, confirming the part is optional. `SkillList<BaseSkill>` tracks *which* skills/powers the actor has learned. `AddSkill(string class)` looks up the C# type via reflection, instantiates it, calls its `AddSkill(GameObject)` hook, adds the part to the entity.

4. **Per-skill behavior** — concrete `BaseSkill` subclass per power (173 in `XRL.World.Parts.Skill/`). Two patterns observed in source-of-truth reads:
   - **Passive** (e.g. `Acrobatics_Dodge.cs:11`): `AddSkill` applies a `base.StatShifter.SetStatShift("DV", 2)`, `RemoveSkill` calls `base.StatShifter.RemoveStatShifts()`. The simplest pattern; ~10 LOC.
   - **Active** (e.g. `Axe_Berserk.cs:11`): declares `public Guid ActivatedAbilityID = Guid.Empty;` and overrides multiple `HandleEvent` methods (`AIGetOffensiveAbilityListEvent`, `BeforeAbilityManagerOpenEvent`, etc.) to register and dispatch the activated ability. Cooldown + duration constants. ~50-200 LOC per power.
   The 173 files cover both patterns plus mixed/hybrid (passive bonus + active ability in one class). v1 ships a passive only (ST.5); active validation is a follow-on milestone.

5. **UI** — `SkillsAndPowersStatusScreen` renders the tree; `SkillsAndPowersLine` formats per row. Verified via `PowerEntry.Render` (lines 141-190): color-coding operates on **two axes**, not one:
   - **Name color** = requirements-met (green `{{g|...}}`) vs not-met (gray `{{K|...}}`)
   - **Cost color** = affordable (cyan `{{C|N}}`) vs unaffordable (red `{{R|N}}`)
   So a power the player meets-the-requirements-for but can't-afford renders as green name + red cost. v1 should mirror this 2-axis convention; "green=buyable / red=insufficient / gray=locked" (collapsed-to-one-axis) loses information.

### Acquisition paths (4 in-game + debug)
- **SP economy** — Skill Points stat. Earned per level. Spent per purchase. The primary path.
- **Water-ritual learning** — NPC dialog teaches skills; spends faction reputation. `BaseSkill.cs:65-92` (`GetWaterRitualText`, `GetWaterRitualReputationCost`, `GetWaterRitualSkillPointCost`).
- **Skillsoft cybernetics** — instant grant via implant. `XRL.World.Parts/CyberneticsSingleSkillsoft.cs` and `CyberneticsTreeSkillsoft.cs`. ⚪ Out of v1 scope (CoO has no cybernetics system).
- **Gear-granted (`SkillOnEquip`)** — equip an item that has the `SkillOnEquip` part to gain the skill while equipped; lose on unequip. `XRL.World.Parts/SkillOnEquip.cs`. ⚪ Out of v1 scope (could ship later as a 1-Part addition once the substrate lands).
- **`wish skill <name>`** — debug; bypasses gating. Wired in `Skills.cs:163-203`.

### Gating
- `Cost` (SP) — direct purchase price.
- `Minimum` — stat threshold. The format is **two parallel strings** (`Attribute` and `Minimum`), with two delimiters:
  - `|` (pipe) splits **OR groups** — passing any group is enough.
  - `,` (comma) splits **AND-conjuncts within a group** — all must pass.
  - Example single-stat: `Attribute="Agility"`, `Minimum="18"` → Agility ≥ 18.
  - Example AND: `Attribute="Agility,Ego"`, `Minimum="18,12"` → Agility ≥ 18 **and** Ego ≥ 12.
  - Example OR: `Attribute="Agility|Strength"`, `Minimum="18|18"` → Agility ≥ 18 **or** Strength ≥ 18.
  - Verified against `PowerEntry.cs:46-61` (parser) + `PowerEntryRequirement.cs:22-36` (per-group AND check) + `PowerEntry.cs:124-139` (across-group OR check).
- `Requires` — comma-separated list of prereq classes (skills or powers); all must be owned.
- `Exclusion` — comma-separated list; having any blocks purchase.
- `Initiatory` — within a tree, powers must be bought in order; later powers are auto-marked `Hidden` until predecessor owned. `FLAG_INITIATORY = 4` bit on `IPartEntry.Flags`.
- **`FLAG_OBFUSCATED = 2`** (also on `IPartEntry.Flags`) — when set + requirements not met, the entry's name renders as `???`. `IBaseSkillEntry.cs:17` docstring confirms. Lets content authors hide the existence of secret skills until prerequisites are discovered. ⚪ Field worth keeping for parity, but no v1 content uses it.

### Event hooks (Qud bus)
- `BeforeAddSkillEvent` / `AfterAddSkillEvent`
- `BeforeRemoveSkillEvent` / `AfterRemoveSkillEvent`
- These are generic XRL events, fired through the standard event bus.

---

## CoO state survey (existing relevant infrastructure)

| Need | CoO has? | Where |
|---|---|---|
| Per-entity Part container | ✅ yes | `Entity.Parts`, `entity.AddPart<T>()` / `GetPart<T>()` |
| JSON-loaded blueprints | ✅ yes | `EntityFactory` + `Resources/Content/Blueprints/*.json` |
| Per-stat numeric tracking | ✅ yes | `Statistics<string, Stat>` on `Entity`; existing stats include Hitpoints / Strength / Agility / Toughness / Ego / Speed |
| Stat-shift / temporary stat modifiers | ⚠️ partial | `StoneskinEffect` mutates AV directly; no general `StatShifter` helper. Effects can mutate; **need a "stable" stat-shift class** distinct from time-bound effects |
| Leveling / XP system | ✅ yes | `LevelingSystem.AwardKillXP` |
| Mutation-style learnable abilities | ✅ yes | `MutationsPart` + `BaseMutation` + per-mutation classes (KindleFlame, ChillDraft, etc.) — same shape as Qud's per-skill class pattern |
| Activated abilities | ✅ yes | `ActivatedAbilitiesPart` (used by mutations like KindleFlameMutation) |
| Centered tilemap UI popup | ✅ partial | `CenteredPopupLayout.cs` + `GrimoirePickerUI.cs` (shipped, reusable). Note: `QuestLogStateBuilder.cs` is shipped (state-builder/snapshot pattern) but `QuestLogUI` itself is **not** — still 💡 in roadmap. ST.7 borrows the state-builder convention from QuestLogStateBuilder and the rendering pattern from GrimoirePickerUI. |
| Reflection-based "find type by name" | ✅ yes | `EntityFactory.ApplyParameters` uses reflection; same pattern usable for skill class resolution |
| Save/load for parts | ✅ yes | `SaveSystem` round-trips Parts (verified via 60+ existing fixtures) |

**Gaps requiring net-new code:**
- `SkillsPart` (the manager Part — equivalent to Qud's `Skills.cs`)
- `BaseSkillPart` (abstract base — equivalent to Qud's `BaseSkill.cs`)
- `SkillData` / `PowerData` (data shapes — equivalent to Qud's `SkillEntry` / `PowerEntry`)
- `SkillRegistry` (loader — equivalent to Qud's `SkillFactory`; CoO uses JSON not XML, so the loading code differs)
- `StatShifter` helper class for stable +N modifiers (Qud has this; CoO doesn't)
- `SP` (Skill Points) stat on the Player
- A skill-purchase action (validates Cost / Minimum / Requires / Exclusion before spending SP)
- A skill-tree UI screen (mirrors `SkillsAndPowersStatusScreen`)

---

## Verification sweep

| Premise | Status | Source |
|---|---|---|
| Qud's `Skills` Part holds a `List<BaseSkill>` of which skills/powers were learned; reflection-based instantiation by class name | ✅ confirmed | `Skills.cs:19, 86-92` |
| Each concrete skill is its own C# class (`Acrobatics_Dodge`, `Axe_Berserk`, etc.) extending `BaseSkill` | ✅ confirmed | `XRL.World.Parts.Skill/` directory listing (173 files) |
| Passive skills apply `StatShift` on `AddSkill`, remove on `RemoveSkill` | ✅ confirmed (via `Acrobatics_Dodge.cs`) | `Acrobatics_Dodge.cs:11-19`. **Single sample only — pattern not surveyed across all 173 files.** |
| Active skills register an `ActivatedAbilityID` (Guid) and override multiple `HandleEvent`s | ✅ confirmed (via `Axe_Berserk.cs` direct read during critical-review pass) | `Axe_Berserk.cs:11` (`public Guid ActivatedAbilityID = Guid.Empty`), `:23-50` (event handlers) |
| Data shape: `IPartEntry` is the actual base class with `Name`, `Class`, `Attribute`, `Snippet`, `Cost`, `Flags` (4-bit field); `IBaseSkillEntry` adds `Tile`, `Foreground`, `Detail`, `Description`; `SkillEntry` adds `PowerList` + `Powers` dict; `PowerEntry` adds `Minimum`, `Requires`, `Exclusion`, `ParentSkill` | ✅ confirmed via direct read | `IPartEntry.cs:1-65`, `IBaseSkillEntry.cs:1-65`, `SkillEntry.cs:9-11`, `PowerEntry.cs:9-17` |
| Gating attributes: `Cost`, `Minimum` (pipe/comma format), `Requires`, `Exclusion`, `Initiatory` (FLAG_4), `Hidden` (FLAG_1), `Obfuscated` (FLAG_2 — `???` in UI), `ExcludeFromPool` (FLAG_8) | ✅ confirmed | `IPartEntry.cs:7-13` (flags), `PowerEntry.cs:11-18, 63-122`, `IBaseSkillEntry.cs:17` (obfuscated docstring) |
| Qud uses XML for Skills.xml; CoO uses JSON convention for blueprints | ⚠️ **CoO divergence** — port format to JSON; field set stays the same | `EntityFactory.cs` (CoO precedent), `SkillFactory.cs:149-196` (Qud) |
| CoO's `MutationsPart` already mirrors the per-creature-Part pattern; the `SkillsPart` will share its shape | ✅ confirmed | `MutationsPart.cs` (existing) |
| CoO has `Statistics["Stat-name"]` API; can add `SP` like any other stat | ✅ confirmed | `Stat.cs`, `LevelingSystem.cs` (existing) |
| `LevelingSystem.AwardKillXP` is the existing XP-grant entry point; level-transition hook point **needs verification** at ST.4 | ⚠️ **`AwardLevelUp` does NOT exist** — only `AwardKillXP`. ST.4 must read LevelingSystem.cs and find the actual transition point. | `LevelingSystem.cs:24` (only public method declared) |
| Reflection-based class resolution: `Type.GetType(...)` works in CoO's existing dispatcher patterns | ✅ confirmed | `EntityFactory.ApplyParameters` |
| Centered-popup UI pattern: **`QuestLogUI` does NOT exist** — only `QuestLogSnapshot.cs` (the data layer). Use `CenteredPopupLayout.cs` + `GrimoirePickerUI.cs` (which DO exist and are shipped) as the rendering pattern. | ⚠️ **plan correction** | `Presentation/Rendering/QuestLogSnapshot.cs:6` (docstring confirms "forthcoming"); `Presentation/UI/GrimoirePickerUI.cs` (shipped) |
| `Skills.xml` data file is **NOT in the decompile** — only the C# loading code | ⚠️ **gap** — must hand-author CoO's first JSON file | `find /Users/steven/qud-decompiled-project -name "Skills.xml"` returned 0 |
| 4 in-game acquisition paths: SP / water-ritual / Skillsoft cybernetics / **`SkillOnEquip`** (gear-granted) | ✅ confirmed (4th path missed in original draft) | `XRL.World.Parts/SkillOnEquip.cs`, `CyberneticsSingleSkillsoft.cs`, `CyberneticsTreeSkillsoft.cs`; `BaseSkill.cs:65-92` (water ritual hooks) |

**Verification corrections caught during the critical-review pass** (and applied to the plan above):

1. **`QuestLogUI` does NOT exist** — original draft cited it as a reusable UI pattern; it is still 💡 in `CONTENT-ROADMAP.md`. ST.7 corrected to use `CenteredPopupLayout` + `GrimoirePickerUI` (which DO exist).
2. **`LevelingSystem.AwardLevelUp` does NOT exist** — original draft cited it as the SP-grant hook. Only `AwardKillXP` is publicly declared. ST.4 now mandates a verification step before commit.
3. **`SkillData` field list was vague** — original draft said "mirrors X minus runtime fields". `IPartEntry` (the actual base) wasn't read; full field set now enumerated explicitly in ST.2.
4. **Acquisition paths missed `SkillOnEquip`** — gear-granted skills are a 4th in-game path. Added to the list (⚪ deferred for v1).
5. **"Skills Part on every actor" was overstated** — it's optional (null-checked at `Skills.cs:208`). Softened to "actors that participate in the skill economy".
6. **`StatShifter` design** — Qud's StatShifter is a property on `BaseSkill`, not a separate utility. ST.5 now mirrors that.
7. **"Most are passive" extrapolation** — only one of 173 skill files was read. Softened; both passive (`Acrobatics_Dodge`) and active (`Axe_Berserk`) patterns now documented from direct reads.
8. **UI color-coding is 2-axis, not 1-axis** — name color = requirements, cost color = affordability. Corrected per `PowerEntry.Render`.

**Remaining known gaps:** XML→JSON port is mechanical; `Skills.xml` not in the decompile (must hand-author CoO's first tree — fine for v1 since we ship 1 tree, 1 power).

---

## Proposed parity port — sub-milestone breakdown (smallest blast radius first)

### ST.1 — Plan + branch (this commit)
- `Docs/SKILL-TREE-QUD-PARITY.md` (this file)
- Branch `feat/skill-tree-v1` cut from `main` (when ready to start ST.2)

### ST.2 — Data layer (1 commit)
- New: **`SkillData`** (pure POCO). Field list verified against `IPartEntry.cs` + `IBaseSkillEntry.cs` + `SkillEntry.cs`:
  - From `IPartEntry`: `Name` (string), `Class` (string), `Attribute` (string — used by Minimum parsing in PowerData), `Snippet` (string), `Cost` (int, default -999), `Flags` (int) with bits:
    - `FLAG_HIDDEN = 1` (entry hidden until acquired)
    - `FLAG_OBFUSCATED = 2` (renders name as `???` until requirements met)
    - `FLAG_INITIATORY = 4` (tree must be bought in order)
    - `FLAG_EX_POOL = 8` (excluded from random skill-pool acquisition)
  - From `IBaseSkillEntry`: `Tile` (string), `Foreground` (string, default "w"), `Detail` (string, default "B"), `Description` (string).
  - From `SkillEntry`: a list of `PowerData` children (`PowerList` + `Powers` dict).
- New: **`PowerData`** (mirrors `PowerEntry`). Inherits all the above + adds:
  - `Minimum` (string — pipe/comma format per Gating section).
  - `Requires` (string — comma-separated prereq classes).
  - `Exclusion` (string — comma-separated mutually-exclusive classes).
  - `ParentSkill` reference (back-pointer to its `SkillData` — set during loading).
- New: **`SkillRegistry`** static class (mirrors `SkillFactory`); `LoadFromJson(string raw)` parses CoO-shape JSON. Maintains `SkillByName`, `SkillByClass`, `PowersByClass`, `EntriesByClass` (matches Qud's 4-dict shape at `SkillFactory.cs:12-18`).
- New: **`Resources/Content/Data/Skills/Acrobatics.json`** — first content. Tree-root + 1 power (Dodge) for the smallest meaningful test:
  ```json
  {
    "Skills": [
      { "Name": "Acrobatics", "Class": "AcrobaticsSkill", "Cost": 100,
        "Description": "...",
        "Powers": [
          { "Name": "Dodge", "Class": "AcrobaticsDodgePower", "Cost": 50,
            "Attribute": "Agility", "Minimum": "15",
            "Description": "+2 DV when equipped." }
        ] }
    ]
  }
  ```
- Tests: 6 in `SkillRegistryTests` — JSON parse round-trip; lookup by name; lookup by class; missing skill returns null; PowerByClass cross-tree lookup; Flags bit accessors round-trip (Hidden/Obfuscated/Initiatory/ExcludeFromPool).
- **Zero behavior change** — pure data scaffolding so later milestones have something to query.

### ST.3 — Runtime SkillsPart (1 commit)
- New: `SkillsPart` — manager Part; `AddSkill(string skillName, Entity actor, Zone zone)` / `RemoveSkill`
- New: `BaseSkillPart` abstract — base for concrete skill classes; virtual `AddSkill(Entity actor)` / `RemoveSkill` hooks
- Save/load round-trip via existing `SaveSystem` (the part needs `Write` / `Read` overrides — match Qud's pattern at `Skills.cs:69-79`)
- Tests: 6-8 in `SkillsPartTests` — AddSkill creates the C# part on the entity; RemoveSkill removes it; save/load round-trip preserves the list; AddSkill twice is idempotent; AddSkill of unknown class returns false; AddSkill of known class returns true
- Counter-check: `RemoveSkill_OnEntityWithoutSkill_DoesNothing`

### ST.4 — Skill Points (SP) stat (1 commit)
- **PRE-IMPL VERIFICATION** (do BEFORE writing code): read `LevelingSystem.cs` end-to-end. The method I cited in the original draft (`AwardLevelUp`) **doesn't exist**; only `AwardKillXP(killer, victim, zone)` is publicly declared. Find where level transitions actually occur (likely inside `AwardKillXP` after XP threshold check, or via a stat-change listener). Document the actual hook point.
- Add `SP` stat to `Player` blueprint with default 0.
- Add SP-grant call **at the verified level-transition point** (start with 1 SP per level; tunable later).
- Tests: 3 — initial SP=0; level-up grants SP; multiple level-ups accumulate SP.

### ST.5 — First passive power: Acrobatics_Dodge (1 commit)
- **Add `StatShifter` field on `BaseSkillPart` itself** (NOT a separate helper class). Mirrors Qud's pattern: `Acrobatics_Dodge.cs:11` does `base.StatShifter.SetStatShift("DV", 2)` — `StatShifter` is a property on the base class, accessed via `base.` from concrete subclasses. The field tracks `(StatName, Amount)` pairs for that one skill instance; `Apply()` adds to the entity's stat; `RemoveStatShifts()` subtracts back. Save/load must round-trip the shift list (or recompute on `OnAfterLoad`).
- New: concrete `AcrobaticsSkill` part (empty marker — owning the tree gives access to its powers; mirrors `Acrobatics.cs:8` `Priority = int.MinValue` empty body).
- New: concrete `AcrobaticsDodgePower` part — `AddSkill` calls `base.StatShifter.SetStatShift("DV", 2)`; `RemoveSkill` calls `base.StatShifter.RemoveStatShifts()`. Mirrors Qud's `Acrobatics_Dodge.cs:10-19` line-for-line in CoO C# idiom.
- Update `Resources/Content/Data/Skills/Acrobatics.json` with both entries.
- Tests: 5 — buying Acrobatics adds the part; buying Dodge applies +2 DV (assert `entity.GetStatValue("DV", 0)`); removing Dodge restores DV; save/load round-trip preserves the shift; stacking is idempotent (re-AddSkill on owned doesn't double-apply).
- Counter-check: removing Dodge after the wielder is missing the StatShifter base-state (no shift was ever applied) doesn't crash.

### ST.6 — Purchase gating + action (1 commit)
- New: `BuySkillAction` — handler for "buy skill X" intent. Validates:
  - Cost ≤ actor.SP
  - Stat minimums met (parses `Minimum` field per Qud's pipe/comma format — keep XML format for portability of content)
  - All `Requires` are owned
  - No `Exclusion` is owned
- On success: spend SP, call `SkillsPart.AddSkill`
- Tests: 8 — happy path; insufficient SP rejected; missing prereq rejected; exclusion conflict rejected; off-by-one stat min (one below + at threshold + one above); not-already-owned counter-check
- Counter-check on each failure path: assert SP unchanged when purchase fails

### ST.7 — Skill UI overlay (1 commit)
- **PRE-IMPL CORRECTION:** the original draft cited `QuestLogUI` as a reusable pattern. **It does not exist** — only `QuestLogSnapshot.cs` exists (the data layer); its own docstring confirms "*Pure-data snapshot consumed by the (forthcoming) QuestLogUI*". The actual existing UI patterns in `Assets/Scripts/Presentation/`:
  - `Rendering/CenteredPopupLayout.cs` — layout primitive (positioning, padding, framing)
  - `UI/GrimoirePickerUI.cs` — concrete shipped centered-popup UI for spell selection; mirrors closest to what we want
  - `Tests/.../Input/CenteredModalUIViewTests.cs` — test infrastructure for modal popups
- New: `SkillsScreenStateBuilder` (snapshot of the skill tree with per-power purchase state — same state-builder convention as `QuestLogStateBuilder.cs`, which IS shipped).
- New: `SkillsScreenUI` MonoBehaviour, modeled after `GrimoirePickerUI` (centered popup, hotkey toggle, list rendering).
- Hotkey to open (`s` — needs to not collide with existing input map; verify before commit).
- **Color codes per Qud's actual 2-axis convention** (not the 1-axis collapse from the draft):
  - **Name color**: green if requirements met, gray if not.
  - **Cost color**: cyan if affordable, red if not, omitted (— "[—sp]") if owned.
  - White = owned (no cost/requirements badge needed).
  - `FLAG_OBFUSCATED` items: render name as `???` if requirements not met.
- Tests: 5-6 in `SkillsScreenStateBuilderTests` — snapshot reflects current SP/owned/locked state; transitions when SP changes; transitions when prereqs become met; obfuscated-flag pre/post requirement met.
- UI rendering itself is hard to unit-test — rely on state-builder snapshot pattern + the showcase scenario in ST.8 for visual verification.

### ST.8 — Showcase scenario + smoke test (1 commit)
- New: `Scenarios/Custom/SkillTreeShowcase.cs`
- Spawn player with HP 200, Strength 15, Agility 18, **6 SP** to start; print a guide
- Player presses `s` → tree screen opens → buys Acrobatics → buys Dodge → exits → DV is +2
- Smoke test in `ScenarioCustomSmokeTests`
- Manual playtest verifies the UI flow

### ST.9 — Cold-eye Q1-Q4 + roadmap update + merge + push
- Per CLAUDE.md §3.4. Delegate to fresh-eyes subagent (per the methodology lesson logged at `ffc1ce2`).
- Update `Docs/CONTENT-ROADMAP.md` to flip the `Skill trees` Tier 4 entry to ✅ (or split — see Open Questions below)

---

## Pre-flagged self-review findings

- **🟡 ST.5 first power choice — passive vs active.** Acrobatics_Dodge is the simplest (passive +DV stat-shift). Picking it for v1 means active-ability powers (Axe_Berserk, etc.) aren't proven until a follow-on. v1 acceptable; second power in ST.5 or a follow-on milestone could ship an active to validate.
- **🟡 ST.6 stat-min format parsing.** Qud's "Agility,Ego|18,12" format is non-trivial to parse (commas = AND within group, pipes = OR across groups). Either port the parser as-is (faithful but ugly) or simplify to a single per-attribute requirement (lossy but cleaner). Recommend porting as-is for forward-compat with content authoring.
- **🟡 ST.7 UI complexity.** Qud's screen has scroll, search, color-codes, click-to-buy. v1 should be smaller — list-only, no search. Iterate after first playtest.
- **🔵 No Initiatory in v1.** The Initiatory flag (must-buy-tree-in-order) adds complexity. v1 ships without it; first content (Acrobatics) doesn't need it.
- **🔵 No water-ritual learning in v1.** That's a Qud-specific NPC dialog mechanic that ties into faction reputation. Out of scope; can layer on later via the existing `ConversationActions` registry.
- **⚪ Skillsoft cybernetics deferred.** The implant-grants-skill mechanic requires a cybernetics system CoO doesn't have. Skip indefinitely.
- **⚪ Gear-granted skills (`SkillOnEquip`) deferred.** Could ship as a 1-Part addition once ST.3 lands, but no v1 content needs it. Tracked as a separate follow-on.
- **⚪ `FLAG_OBFUSCATED` field implemented but unused.** ST.2's `SkillData` carries the bit; ST.7's UI renders `???` correctly. v1 has no skills that set the flag — it exists for forward content authoring (secret/heretical skills hidden until prereqs discovered).
- **⚪ No XML→JSON content conversion.** Qud's `Skills.xml` isn't in the decompile and would need 173+ skills hand-authored. v1 ships with 1 tree (Acrobatics, 1-2 powers); content team can grow it incrementally.

---

## Open questions

- **Tier classification — honest framing.** The current `Docs/CONTENT-ROADMAP.md` has `Skill trees` as a Tier 4 entry ("Long Arcs, 3-5 days"). The estimate for v1 (9 sub-milestones, ~2-3 days focused work) is a **boundary case** — between Tier 3 ("System Extensions, 1-2 days") and Tier 4 ("Long Arcs, 3-5 days"). Two options:
  - **Narrow v1 to fit Tier 3 strictly**: skip ST.7 (UI overlay) for v1; ship the substrate + 1 tree + 1 passive + a `SkillsListAction` that prints to message log. Visible enough to demo, no UI work. ~1.5 days.
  - **Ship full v1 as Tier 3.5** (current plan): includes the UI overlay; takes 2-3 days; promote `Skill trees` from Tier 4 to Tier 3 in roadmap with a footnote that it's a wide Tier 3.
  Either is defensible; pick before committing ST.1. The narrow-v1 path is the safer first ship; the full-v1 path matches the user's stated "we want Qud parity" intent better. **Recommendation: full v1 (Tier 3.5)** since the UI is the load-bearing piece for "skill tree" being a feature players experience.
- **First content tree choice.** Acrobatics is the simplest (3-4 simple passive powers). Alternative: an offensive tree (Axe with Berserk + Cleave + Decapitate) — more user-visible payoff but requires the active-ability + on-hit hook pattern. Recommend Acrobatics for v1 (de-risks the substrate); follow-on with Axe to validate active powers.
- **Save-format compatibility.** The `SkillsPart` serialization will change the save format. Ship the version-bump alongside ST.3.

---

## Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `EntityFactory.ApplyParameters` reflection-based set-by-name | `EntityFactory.cs` | Skill class resolution (the `Class` field → `Type.GetType` → `Activator.CreateInstance`) |
| `MutationsPart` + concrete mutation classes (KindleFlameMutation etc.) | `MutationsPart.cs` + `Mutations/` | Direct shape-mirror for `SkillsPart` + `BaseSkillPart` + concrete skills |
| `ActivatedAbilitiesPart` | (existing) | When ST.5 follow-on adds an active-ability skill |
| `QuestLogStateBuilder` (state-builder pattern only) | `Presentation/Rendering/QuestLogStateBuilder.cs` | Snapshot/builder convention for ST.7. (`QuestLogUI` itself is **not** shipped — only the snapshot layer is. The UI MonoBehaviour for skills must be built fresh.) |
| `CenteredPopupLayout` + `GrimoirePickerUI` | `Presentation/Rendering/CenteredPopupLayout.cs`, `Presentation/UI/GrimoirePickerUI.cs` | Actual shipped centered-popup UI infrastructure that ST.7 should mirror line-for-line for the rendering side. |
| `CenteredModalUIView` test infra | `Tests/.../Input/CenteredModalUIViewTests.cs` | Reuse the existing test fixture pattern when adding `SkillsScreenUI` smoke tests. |
| `LevelingSystem.AwardKillXP` | `Gameplay/Stats/LevelingSystem.cs` | Hook point for ST.4 SP-grant |
| `Stat` API | `Gameplay/Stats/Stat.cs` | SP stat addition + StatShifter helper |
| `SaveReader` / `SaveWriter` | `Gameplay/Save/SaveSystem.cs` | ST.3 part serialization |
| `Diag` substrate | `Shared/Utilities/Diag.cs` | Optional new `skill` channel for OnSkillAdded/Removed records (mirrors the `quest` channel pattern from QS.3) |

---

## Implementation sequence

```
1. Plan to disk (ST.1, this commit)              [no MCP]
2. ST.2 data layer + tests
   → 1 refresh + 1 test run                      [2 MCP calls]
3. ST.3 SkillsPart + BaseSkillPart + tests
   → 1 refresh + 1 test run                      [2 MCP calls]
4. ST.4 SP stat + LevelingSystem hook + tests
   → 1 refresh + 1 test run                      [2 MCP calls]
5. ST.5 Acrobatics + Dodge (StatShifter) + tests
   → 1 refresh + 1 test run                      [2 MCP calls]
6. ST.6 BuySkillAction + gating + tests
   → 1 refresh + 1 test run                      [2 MCP calls]
7. ST.7 UI screen state-builder + tests
   → 1 refresh + 1 test run                      [2 MCP calls]
8. ST.8 SkillTreeShowcase + smoke test
   → 1 refresh + 1 test run                      [2 MCP calls]
9. Targeted regression sweep
   → 1 test run                                  [1 MCP call]
10. Cold-eye delegation to subagent + merge + push  [no MCP]
```

**Total MCP calls: ~15** spread across 8 ship cycles. Pace ≥20s gaps per the established convention.

Expected total: ~600 lines of new code + ~400 lines of tests + ~50 lines of JSON content + this plan (~500 lines). ~2-3 days of focused work for v1.

---

## What gets observable to the player after this ship

| Today | After ST v1 |
|---|---|
| Player levels up via XP, stats grow with `LevelingSystem`, no agency over advancement | + SP earned per level + skill-tree menu (`s`) + buy Acrobatics + Dodge for +2 DV |
| Combat is determined by stats only (Strength/Agility/Toughness) | + skill-driven passive bonuses (DV from Dodge; future: damage from Axe_Cleave; future: utility from Tumble) |
| Mutations are the only learnable abilities | + non-magical, non-mutation skill path. The Roguelike's "what kind of character do I want to make" question gets a real answer. |
| Roadmap §"Skill trees" Tier 4 = 💡 | Tier 3 = ✅ (v1, narrow scope: 1 tree, 2 powers); Tier 4 reserved for v2 expansion |

The architecture is **load-bearing for future content** — once the substrate is in, adding new skill trees is content work (JSON + a new C# class per power), not engine work. The Tier 3→Tier 4 progression mirrors how Qud organized its 173 skills incrementally.
