# Content Roadmap — Tiers 1-5

> **Living document.** Update on every content ship. Refer to before
> picking the next task. Status emoji + commit/PR hash = source of
> truth for what's done.

## How to use

- **When picking work:** scan tier in order; pick the first ✅-eligible
  item that fits your time budget.
- **When shipping:** flip status to ✅ and add commit hash. Add a one-line
  blurb under "Recently shipped."
- **When discovering scope creep:** split into a separate item rather
  than letting one ship grow.
- **When finding a new idea:** add to the appropriate tier with `📋`.
  Don't auto-elevate — let it earn promotion through prior wins.

## Status legend

| Emoji | Meaning |
|---|---|
| ✅ | Shipped (commit/branch linked) |
| 🚧 | In progress |
| 📋 | Planned (next-up candidate) |
| 💡 | Idea (not yet committed-to) |
| ❌ | Considered + rejected (with rationale) |

---

## Currently working on

(none — Tier 1 + Tier 2 combat content fully closed. 3 deferred Tier 2 items remain: Lockpicking, Consumable keys, Hunger.)

---

## Recently shipped

| Commit | What |
|---|---|
| `feat/lore-liquids` | **LL — 5 lore-grounded liquids (tepui-thread canon), JSON-only** — drawn from the `claude/ideas-gin-frogs` Felled-Tree lore, each anchored to a faction/cosmological-function/preservation-method, zero new C# via the LQ.5/6 engine: `iron-gall-ink` (Palimpsest/Ink-Bathed — Cond 60 + slow Acid corrosion), `sundew-mucilage` (Drosera — the strongest entrapment, −4 Agi/−5 DV), `choir-wort` (Rot Choir external digestion — 4/turn Acid + −3 Tough), `lumen-slime` (catacomb light economy — −3 DV glow-beacon), `bog-mire` (Bog-Taken — FireDampen 50 + tannic tick + −2 Agi). 12 content/behavior tests; runId-scoped live matrix 60/60 + direct cross-check exact. Doc-vs-impl Q4 correction recorded (lumen/bog shipped Conductivity 0 — 40/20 would be sub-threshold inert). Lore special-features (Ink-Bathed readability, root-in-place, Choir-Touched, true light, passive preservation) tracked as ⚪ deferrals. Self-auditing bench now covers 15 liquids. |
| `feat/liquid-expansion-lava-gel-sap-honey` | **LX — Qud-liquid expansion (lava/gel/sap/honey), JSON-only** — 4 more Qud liquids as pure content via the LQ.5/6 engine, zero new C#: lava (Cond 90 + 8/turn Heat tick + −25 HeatRes), gel (Cond 100 pure conductor), sap (Combust 70 + −2 Agi), honey (Combust 60 + −2 Agi/−3 DV). 12 content/behavior tests. **The self-auditing bench's first real expansion exposed two latent bench bugs** (RunMatrixAudit ran before GameBootstrap's registry load → phantom ×1.00; persisted Diag buffer → stale-read) — fixed via `EnsureLiquidRegistry()` + a loud Rule-4 abort + Rule-8 `runId` scoping; methodology playbook gained Rule 8 + the direct-measurement cross-check corollary. Final runId-scoped matrix 40/40 exact (lava 125/190, gel 200, sap 135, honey 130). Mechanic was always correct (direct measurement); the *bench* was the bug. |
| `feat/liquid-coating-system` | **Liquid Coating System (Qud-parity, LQ.1–LQ.7)** — pools now transfer onto creatures and the coat changes how the world treats them. LQ.2 `LiquidDefinition`/`LiquidRegistry` (JSON data layer, 6 liquids), LQ.3 `LiquidPoolPart` + puddle blueprints, LQ.4 transfer-on-contact (`LiquidCoveredEffect`, merge-not-stack, water→Wet div #3; also fixed a latent `MovementSystem` cell-CHANGE double-trigger bug), LQ.5 consequences (water amps Lightning/damps Fire, oil amps Fire, acid ticks; div #6 no double-amplify with Electrified), LQ.6 3 stat/resistance liquids (brine/pitch/carapace-ichor) via JSON-only + `Stat.Bonus` net-zero engine + the data-only-expansion proof, LQ.7 GameBootstrap wiring + `liquid` diag channel (`Coated`/`CoatRejected`/`StatMod*`/`CoatExpired`) + showcase + 28-test adversarial sweep. ~75 new tests; 253/253 combined sweep GREEN. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / Liquid Hazard Showcase`. |
| `feat/tier2-combat-closeout` | **Tier-2 combat content closeout** — 3 cohesive combat ships: (1) light-emitting equipment via LightMap.Compute Pass 2 walking equipped LightSourceParts (held FlamingSword glows red r4, IceSword cyan r4, ThunderHammer yellow r3); (2) PressurePlateTriggerPart — rearmable variant of TriggerOnStepPart that persists in zone after firing (vs one-shot SpikeTrap); (3) TripWireTriggerPart — multi-segment line trap where N entity-segments share a WireGroupId; stepping on any segment detonates the whole wire. 16 new tests across 3 fixtures (6+5+5). 4 false premises caught + corrected during pre-impl critical-review pass (LightMap doesn't walk inventory; TurnManager.CurrentTurn doesn't exist; PressurePlate cooldown over-engineered; Cell.GetEntitiesAtCell() invented). All 16 GREEN via Unity MCP. **Tier 2 has 3 outstanding 💡 items remaining** (Lockpicking, Consumable keys, Hunger; planned in detail in `Docs/TIER2-CLOSEOUT.md` for follow-on ships). |
| `feat/tier1-content-closeout` | **Tier-1 closeout** — final ship for Tier 1: BleedTonic + CharredTonic (status tonics #9 and #10, with new dispatcher cases bringing StatusTonicPart up to 9 effect aliases) + ElementalCreatureZoo (QA layout for the 9-creature × 4-axis resistance matrix) + TonicTestBench (all 10 tonics on the floor for drink-and-observe via `effect/OnApply` diag). 20 new tests across 4 fixtures (5+6+4+5). **Tier 1 fully closed — 0 outstanding 💡/📋 items**. Showcases: `Caves Of Ooo / Scenarios / Combat Stress / Elemental Creature Zoo` and `Tonic Test Bench`. |
| `feat/quest-system` | **Tier-3 Quest System v1** — completes the M4 placeholder in the pre-existing `StoryletPart` scaffold + ships the full QS.1-8 substrate: 4 conversation predicates + 4 lifecycle actions + 2 reward wrappers + M4 dispatch loop + QuestLogStateBuilder + QuestShowcase scenario + new `quest` diag channel. 49 new tests across 7 fixtures, 144/144 GREEN sweep. Two real production fixes during cycle: phantom-quest-bricking guard in StartQuest + LocalPlayer plumbing for tick-dispatch player-state predicates. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / Quest Showcase`. |
| `feat/shopping-parity` | **Tier-2 Trading Qud-parity gap closeout** — `CanBeTradedEvent` + `NoTrade` tag (quest-item protection — fixes "sell your IronKey by mistake" bug) + trader-state validation (Burning/Stunned/Frozen/Dead refused) + `StartTradeEvent` session hook (future-content unblocker) + `MerchantShopShowcase` scenario + new `trade` diag channel (6th) with `Bought`/`Sold` records. 18 new tests across 3 fixtures. 72/72 GREEN sweep. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / Merchant Shop Showcase`. |
| `feat/lock-and-key` | **Tier-2 Lock & Key v1** — `LockPart` + `KeyPart` + `LockedDoor` / `LockedChest` / `IronKey` blueprints. Bump-to-unlock via `PhysicsPart`'s solid-blocker check: walking into a locked door fires `AttemptUnlock`, `LockPart` matches the actor's inventory `KeyPart.KeyId` against its own, flips `IsLocked=false` + drops `Solid` on success. Master-key model (keys reusable). Adds `furniture/UnlockAttempted` diag channel (5th hook). 21 tests + showcase scenario. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / Locked Door Showcase`. |
| `feat/emberspear-charredhusk` | **Tier-1 EmberSpear + CharredHusk pair** — Heat-axis mirror of CryoLance + IceWight. Second Piercing-class elemental weapon (1d6+1 Piercing/Fire, no sub-class, Burning on-hit) and second 100%-immune creature (HR=100, CR=-50). Resistance-extreme matrix is now symmetric across Cold and Heat axes. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / EmberSpear Showcase`. |
| `feat/cryolance-icewight` | **Tier-1 CryoLance + IceWight pair** — first Piercing-class elemental weapon (1d6+2 Piercing/Ice/LongBlades, PenBonus 3, Frozen on-hit) and first 100%-immune creature (CR=100, HR=-50). Pins the resistance ≥ 100 = total negation path AND the negative-HR creature path. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / CryoLance Showcase`. |
| `feat/trap-furniture` | **Tier-2 Trap furniture** — SpikeTrap, FireTrap, BearTrap. Three single-use mechanical floor traps reusing the existing TriggerOnStepPart pattern. 9 unit tests + smoke. |
| `feat/throwable-consumables` | **Tier-2 Throwable consumables** — tonics shatter on impact with radius-1 AOE. Direct hit / miss / wall hit all shatter; bottle never lands. New `ApplyTonicAoe` helper; `ThrowableTonicsShowcase` scenario. 12 unit tests + smoke. |
| `feat/elemental-tonics` | **4 elemental tonics** — AcidTonic, LightningTonic, FrostTonic, WaterTonic. Pure JSON + 24 tests; StatusTonicPart dispatcher already supported them. Completes Tier-1 status-tonic content row. |
| `8aa469a` | **Tier-2 OnHitEffects abstraction** — class-based hooks (Bludgeoning→Stun, Cutting→Bleed, Piercing→Confuse) + per-weapon overrides (FlamingSword→Burning, IceSword→Frozen, ThunderHammer→Electrified, AcidicDagger→Acidic, DissolutionMaul→Acidic). Activates the weapon-attribute backfill in live gameplay. |
| `9c34cb0` | Weapon-attribute backfill: all 17 unattributed weapons now declare physical-class + sub-class. **25/25 coverage**. DissolutionMaul gains Acid routing via Corrosive material. |
| `f1b906f` | AcidicDagger + AR on CaveSlime (+50) and Scorpion (-50) + AcidicDaggerShowcase — fourth elemental weapon, completes the Fire/Ice/Lightning/Acid quartet |
| `84f5622` | ThunderHammer + ER on StoneGolem (+50) and BrassHusk (-50) + ThunderHammerShowcase — first scenario to expose **negative resistance** in-game |
| `183fbd1` | CONTENT-ROADMAP.md (this doc) |
| `d00299b` | ElementalSwordsShowcase scenario + smoke tests for 4 combat showcases |
| `9e2358a` | IceSword (1d8 Cutting/Ice/LongBlades) + 6 tests |
| `2bbe31b` | Combat hit-log fix: log + FX + dismemberment use post-resistance damage |
| `6de301e` | FlamingSwordShowcase scenario + FlameDemo probe |
| `3261f1f` | FlamingSword (1d8 Cutting/Fire/LongBlades) + 6 tests |
| `5803d3e` | StoneskinTonic (Phase F + T2.4 first content) |

---

## Tier 1 — Quick Wins (≤1 hour, content-only)

> Pure JSON + tests. Reuses existing infrastructure. Each ship adds
> visible gameplay depth without new code paths. The FlamingSword /
> IceSword pattern is the template.

### Elemental weapons (Phase C × E)

- ✅ **FlamingSword** — 1d8 Cutting/Fire/LongBlades — `3261f1f`
- ✅ **IceSword** — 1d8 Cutting/Ice/LongBlades — `9e2358a`
- ✅ **ThunderHammer** — 1d8+1 Bludgeoning/Lightning/Cudgel + first vulnerability case (BrassHusk ER=-50, StoneGolem ER=+50) — `84f5622`
- ✅ **AcidicDagger** — 1d4+1 Piercing/Acid + AR on CaveSlime (+50) and Scorpion (-50) — `f1b906f`
- ✅ **CryoLance** — 1d6+2 Piercing/Ice/LongBlades, PenBonus 3, Frozen on-hit — `feat/cryolance-icewight`
- ✅ **EmberSpear** — 1d6+1 Piercing/Fire (no sub-class — matches Spear convention), 30% Burning on-hit — `feat/emberspear-charredhusk`

### Backfill weapon Attributes — DONE (this branch)

> Every melee weapon now declares an Attributes string. Pinned by
> per-weapon blueprint-shape tests in `WeaponAttributesContentTests.cs`.

- ✅ **LongSword** — `Cutting LongBlades`
- ✅ **Battleaxe** — `Cutting Axe`
- ✅ **Greatsword** — `Cutting LongBlades` (2H)
- ✅ **Mace** — `Bludgeoning Cudgel`
- ✅ **Spear** — `Piercing`
- ✅ **Hatchet** — `Cutting Axe`
- ✅ **Claymore** — `Cutting LongBlades`
- ✅ **Sporeblade** — `Cutting LongBlades`
- ✅ **EchoKnife** — `Cutting Sonic`
- ✅ **ChoirSpine** — `Piercing`
- ✅ **OldWorldPipe** — `Bludgeoning Cudgel`
- ✅ **TemporalShard** — `Piercing`
- ✅ **SeveranceEdge** — `Cutting LongBlades`
- ✅ **GlassblownStiletto** — `Piercing`
- ✅ **DissolutionMaul** — `Bludgeoning Cudgel Acid` (BONUS: routes through AcidResistance via Corrosive material tag)
- ✅ **FirstRootGlaive** — `Cutting Glaive` (new sub-class)
- ✅ **PalimpsestBlade** — `Cutting LongBlades`

**Coverage:** 25/25 weapons declare Attributes (was 8/25). Future
content that hooks on `damage.HasAttribute("Cutting")` etc. now fires
correctly for every weapon in the game.

### Resistant creatures (matching elemental weapons)

> Each creature spawns the resistance interaction for one weapon class.

- ✅ **Glowmaw** — HeatResistance 50 — exists
- ✅ **Snapjaw** — ColdResistance 25 — exists
- ✅ **SnapjawHunter** — ColdResistance 50 — exists
- ✅ **StoneGolem** — ElectricResistance 50 — `84f5622`
- ✅ **BrassHusk** — ElectricResistance −50 (vulnerability — first negative-resistance creature) — `84f5622`
- ✅ **CaveSlime** — AcidResistance +50 — `f1b906f`
- ✅ **Scorpion** — AcidResistance −50 (chitin dissolves) — `f1b906f`
- ✅ **IceWight** — ColdResistance 100 (full Cold immunity, FIRST 100%-immune creature) + HeatResistance −50 (Fire vulnerability) — `feat/cryolance-icewight`
- ✅ **CharredHusk** — HeatResistance 100 (SECOND 100%-immune creature) + ColdResistance −50 (Cold vulnerability) — `feat/emberspear-charredhusk`

### Status tonics (use existing StatusTonicPart dispatch)

> The StatusTonicPart already dispatches to BurningEffect, AcidicEffect,
> WetEffect, ElectrifiedEffect, FrozenEffect, StoneskinEffect via name
> lookup. Each new tonic = 1 blueprint + 1 test.

- ✅ **PoisonTonic, FireTonic, StoneskinTonic** — exist
- ✅ **AcidTonic** — applies AcidicEffect — `feat/elemental-tonics`
- ✅ **LightningTonic** — applies ElectrifiedEffect — `feat/elemental-tonics`
- ✅ **FrostTonic** — applies FrozenEffect — `feat/elemental-tonics`
- ✅ **WaterTonic** — applies WetEffect — `feat/elemental-tonics`
- ✅ **BleedTonic** — applies BleedingEffect (DOT) — `feat/tier1-content-closeout`
- ✅ **CharredTonic** — applies CharredEffect (post-burn vulnerable state) — `feat/tier1-content-closeout`

### Lightweight scenarios

- ✅ **FlamingSwordShowcase, ElementalSwordsShowcase** — exist
- ✅ **ElementalCreatureZoo** — one of every resistance creature in a small zone, label them. Useful for content QA. — `feat/tier1-content-closeout`
- ✅ **TonicTestBench** — vials of every tonic on a shelf, bottles labeled. Drink-and-observe. — `feat/tier1-content-closeout`

---

## Tier 2 — Cohesive Bundles (½–1 day)

> Pair a small system enhancement with content that exercises it. The
> StoneskinTonic ship is the template (Phase F event hook + Effect class
> + tonic blueprint + tests).

### OnHitEffects abstraction — DONE (this branch)

- ✅ **`OnHitClassEffects` static utility** — class-based hooks
  (Bludgeoning→15% Stun, Cutting→25% Bleed, Piercing→10% Confuse) hook
  into `CombatSystem.PerformSingleAttack` post-`ApplyDamage`.
- ✅ **`MeleeWeaponPart.OnHitEffectsRaw` field** — flat-string format
  `"EffectName,ChancePercent,DamageDice,DurationTurns,Magnitude;..."`.
  Wired on FlamingSword (Burning), IceSword (Frozen), ThunderHammer
  (Electrified), AcidicDagger (Acidic), DissolutionMaul (Acidic at
  higher magnitude). Sporeblade not yet wired — no thematic Effect class
  for "Fungal status" exists; deferred.
- Class + per-weapon hooks fire independently — elemental weapons
  stack their elemental effect on top of their class effect (Cutting +
  Burning, Bludgeoning + Electrified, etc.).
- Showcase scenario: `Caves Of Ooo / Scenarios / Combat Stress / On-Hit Effects Showcase`.

### Light-emitting weapons / equipment

- ✅ **`LightSourcePart` propagation through equipment** — held FlamingSword glows red (radius 4), held IceSword glows cyan (radius 4), held ThunderHammer glows yellow (radius 3). LightMap.Compute walks each zone entity's equipped items and adds light at the wielder's cell. — `feat/tier2-combat-closeout`
- Unblocks: torches, lanterns held in hand, additional glowing weapons.

### Throwable consumables — DONE

- ✅ **Tonics shatter on impact when thrown** — `feat/throwable-consumables`. Radius-1 AOE around impact cell. Direct hit, miss, and wall hit all shatter; bottle never re-enters the zone. Pairs with the 4 elemental tonics: thrown FrostTonic freezes a 3×3 cluster, Water+Lightning combo hits everyone caught between, etc. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / Throwable Tonics Showcase`.

### Trap furniture

- ✅ **SpikeTrap, FireTrap, BearTrap** — `feat/trap-furniture`. Three single-use mechanical floor traps reusing the `TriggerOnStepPart` infrastructure. Spike: piercing damage. Fire: heat damage + BurningEffect, routes through HeatResistance. Bear: piercing damage + Stunned + Bleeding (full payload). Showcase: `Caves Of Ooo / Scenarios / Combat Stress / Trap Furniture Showcase`.
- ✅ **PressurePlate** — rearmable variant of TriggerOnStepPart. Fires every time someone steps onto the cell (vs SpikeTrap which consumes itself). EntityEnteredCell fires only on cell-CHANGE moves so a stationary actor doesn't re-trigger; deliberate ON-OFF-ON stepping is correct semantics. — `feat/tier2-combat-closeout`
- ✅ **TripWire** — multi-segment line trap. Each cell of the wire is its own entity sharing a WireGroupId. Stepping on any segment detonates the whole wire — damages actors at every segment cell + removes all segments. — `feat/tier2-combat-closeout`

### Lock & key — DONE

- ✅ **LockPart + KeyPart + LockedDoor/LockedChest/IronKey** — `feat/lock-and-key`. Bump-to-unlock via PhysicsPart's solid-blocker check; matching key in inventory flips `IsLocked=false` + drops Solid (next-bump walks through). Master-key model (keys reusable). Adds `furniture/UnlockAttempted` diag channel. Showcase: `Caves Of Ooo / Scenarios / Combat Stress / Locked Door Showcase`. 21 tests across 4 fixtures + smoke.
- 💡 **Lockpicking skill + lockpick item** — Lock & Key v2. Skill check vs lock difficulty; consumed lockpicks on fail. Deferred to its own ship.
- 💡 **Single-use keys (`Consumable` flag on `KeyPart`)** — strip from inventory after first use. Deferred until playtest demands it.

### Hunger / Food

- 💡 **HungerStat + FoodPart on edibles.** Existing food items (Snapjaw meat, jerky) can declare nutrition. Buff/debuff on full vs starving.

---

## Tier 3 — System Extensions (1-2 days)

> A real new system + content that exercises it. Each one is a
> commitable feature with its own `Docs/<feature>.md` plan.

### Crafting / Forge

- 💡 **AnvilSitePart + RecipeSystem.** Combine ingredients (ore + fuel + Forge interaction) → output items. Unblocks player-created elemental weapons (IceSword from steel + ice essence).

### Quest System — DONE (v1)

- ✅ **Quest substrate v1** — `feat/quest-system` (QS.1-8). NPC dialog → objective → reward loop end-to-end, building on the pre-existing `StoryletPart` scaffold (`QuestState`/`QuestData`/`QuestStageData` types + ISaveSerializable were already there; the M4 dispatch loop was a placeholder). This ship completes M4 + adds: 4 conversation predicates (`IfQuestActive`/`IfQuestStage`/`IfQuestNotStarted`/`IfQuestCompleted`); 4 lifecycle actions (`StartQuest`/`AdvanceQuestStage`/`CompleteQuest`/`FailQuest`); 2 reward action wrappers (`AwardXP`/`GiveDrams`); the M4 stage-trigger dispatch loop (single-pass deterministic, single-source-of-truth `StoryletPart.AdvanceQuestStage` + `StoryletPart.CompleteQuest` helpers); a `QuestLogStateBuilder` snapshot for the (forthcoming) UI; `QuestShowcase` scenario at `Combat Stress / Quest Showcase`; new `quest` diag channel (7th default-on) with `Started`/`StageAdvanced`/`Completed`/`Failed` records. 49 new tests across 7 fixtures. 144/144 GREEN sweep. **Real production fixes shipped during the cycle**: quest-id phantom-bricking guard (StartQuest now validates `StoryletRegistry.FindQuest` before adding to active dict); player-state predicate gap (added `StoryletPart.LocalPlayer` so tick-driven dispatch can evaluate `IfHaveItem` etc — was a critical gap that made tick triggers nearly useless for canonical quest content).
- 💡 **Branching quests** (Stages → Dictionary, NextStageId field) — v2 unblocks non-linear quests.
- 💡 **`IQuestSystem` abstract** for procedural quests — v2 unblocks "GolemQuestSystem"-style content with custom code per quest.
- 💡 **Quest content authoring** — 5+ canonical quests (lost-letter, kill-N-snapjaws, fetch-rare-tonic, escort-NPC, named-target). Pairs with: faction rep, settlement plot.
- 💡 **QuestLogUI MonoBehaviour rendering** — wires the QS.6 state-builder snapshot into a centered tilemap popup with `q` hotkey toggle.
- 💡 **Failed-quest tracking** + "you already failed this" UI feedback — currently failed quests are silently retakeable.

### Trading / Shopkeepers — DONE

- ✅ **Trade system core (CommercePart + Drams + TradeSystem + TradeUI + ConversationActions.StartTrade + TradeStockBuilder + auto-injected [Let's trade.])** — pre-existing implementation with 18/18 unit tests GREEN.
- ✅ **Qud-parity gap closeout** — `feat/shopping-parity` (SP.1-5). Adds: `CanBeTradedEvent` + `NoTrade` tag for quest-item protection (closes the "sell your dungeon key by mistake" bug); trader-state validation (Burning/Stunned/Frozen/Dead refused); `StartTradeEvent` session hook (future-content unblocker for identify/repair services); `MerchantShopShowcase` scenario at `Combat Stress / Merchant Shop Showcase`; new `trade` diag channel (`trade/Bought`, `trade/Sold` records with `perf` for resistance debugging). 18 new tests across 3 fixtures (CanBeTradedEventTests + TraderStateTests + MerchantShopShowcaseDiagTests).
- 💡 **TraderCreditExtended** (Qud-parity polish — buy on credit; trader refuses further trade until debt paid). Deferred — playtest-driven priority.
- 💡 **Service trades** (identify / repair / recharge — Qud has these, listeners on `StartTradeEvent`). Hook is in place; need Part-side listeners + service UI when content needs it.

### Spellbook / Mana / Scrolls

- 💡 **ManaStat + ScrollItemPart that grants one-time-use mutations.** Existing GrimoirePart is similar but for permanent spells. Scrolls = consumables.

### Day/Night cycle

- 💡 **Time-of-day stat + light propagation + monster behavior shifts.** Currently no cycle. Could thread through TurnManager.

### Weather

- 💡 **WeatherSystem rolls per-zone.** Rain damps fire (extinguishes BurningEffect). Snow chills (applies WetEffect → Cold synergy). Storm spawns Lightning damage.

---

## Tier 4 — Long Arcs (3-5 days)

> Multi-system features. Each requires a planning phase, sub-milestone
> breakdown, and probably a saga or two of fixes. Methodology Template
> §1.1 fully applies.

### Procedural quests

- 💡 **Quest templates → procedurally generated objectives.** Hunt N creatures, fetch N items, kill named target, escort NPC. Plugs into Quest System (Tier 3).

### Companion system

- 💡 **Tame / hire NPC followers.** They walk with the player, share inventory, take orders ("attack", "wait", "fetch X"). PetDog already exists — extend its goal stack.

### Skill trees

- ✅ **v1 shipped (ST.1-9, 2026-05).** SP stat (+1 per level), `SkillRegistry` JSON-loaded skills, `SkillsPart` manager + `BaseSkillPart`, `BuySkillAction` (gating + diag), `SkillsScreenUI` (KeyCode.X popup with 4 row states), `SkillTreeShowcase` scenario. v1 content: 1 tree (Acrobatics) + 1 passive power (Dodge: +2 DV via StatShifter, Agility ≥ 15). See `Docs/SKILL-TREE-QUD-PARITY.md`.
- ✅ **v1.5 — weapon-class skills shipped (WS.1-6, 2026-05).** 4 new trees (Cudgel, Axe, Long Blades, Short Blades) with 8 new skills (4 tree-roots + 4 foundational on-hit powers). All priced at **1 SP / no requirements** for accessibility. New `OnHitSkillEffects` static class hooked into `CombatSystem.PerformSingleAttack` post-`ApplyDamage`, parallel to the existing `OnHitClassEffects`. Skills: `Cudgel_Bludgeon` (Stunned proc), `Axe_Cleave` (half-damage to adjacent), `LongBlades_Lacerate` (Bleed proc), `ShortBlades_Jab` (Confused proc). All stack with the universal class hooks (Bludgeoning→Stun / Cutting→Bleed / Piercing→Confuse). See `Docs/WEAPON-SKILLS.md`.
- ✅ **v1.6 — Qud-parity deepening shipped (WSP.1-4b, 2026-05).** Tree-roots became observable: each grants a force-applied effect on critical hits with the matching weapon class (CudgelSkill → 1-4T Stun, AxeSkill → forced cleave, LongBladesSkill → forced 1d4 Bleed, ShortBladesSkill → forced 1d2 Bleed). `Cudgel_Bludgeon` re-tuned to Qud-verbatim 50% / 3-4T (was 35% / 3T). New 5th power `ShortBlades_Bloodletter` (50% Bleed 1d2 per Piercing hit). Worst-case Mace crit can now stack 3 Stun rolls (class + Bludgeon + crit) for 9-10T duration via StunnedEffect.OnStack summing. See `Docs/WEAPON-SKILLS-PARITY.md`.
- ✅ **v2 — full Qud-parity skill SYSTEM shipped (WSP3.0-3.7, 2026-05).** Architectural pivot from central-static-dispatch to per-skill virtual-override pattern mirroring Qud's `Register`/`HandleEvent`/`FireEvent` shape. Each skill class is now self-contained — modifying a skill = editing one file. New: `SkillEventContext` + `SkillEventDispatcher` + 5 virtual hooks on `BaseSkillPart` + ActivatedAbility-Skill integration for command-driven cooldown abilities. **8 new Tier-2 passives** shipped via the new pattern (3 Expertise +to-hit, Hammer, ShatteringBlows, Hobble passive, Backswing, Rejoinder) + **2 Tier-3 active abilities** (Cudgel_Conk targeted strike, Axe_Berserk self-buff) + **4 new status effects** (Hobbled, ShatterArmor, Broken, Berserk). 22 skill classes total. Authoring guide at `Docs/AUTHORING-SKILLS.md` with 3 worked-example patterns. See `Docs/SKILL-SYSTEM-PARITY.md`.
- 💡 **v3 expansion (still deferred).** Remaining Qud active abilities: Slam, ChargingStrike, Lunge, Swipe, Deathblow, Dismember-active, Decapitate-toggle, ShortBlades active versions (Hobble-active, Shank, Puncture, Rejoinder-active, PointedCircle), HookAndDrag, Hammer Toss, LongBlades 3-stance system, dual-wielding. The substrate is in place — each new skill is one file via the pattern in `Docs/AUTHORING-SKILLS.md`.

### Themed dungeon generator

- 💡 **Dungeon templates with theme = monster pool + tile palette + loot table.**
  - **IcyCaves**: SnapjawHunters + IceWights, snow tiles, frostshield loot
  - **FungalMires**: SporeShamblers + Sporeblades, mushroom tiles
  - **RuinedTowers**: SkeletalSentry + Brass items, stone tiles
- Builders/ already has the worldgen scaffolding; need theme injection.

### Boss creatures

- 💡 **Unique encounters with scripted multi-phase behaviors.** A boss has 3 hp thresholds, summons adds at each, has signature attacks. Tied to dungeon endings.

---

## Tier 5 — Aspirational / Big Bets

> Months of work. Require design decisions. Listed for orientation —
> not roadmapped.

- 💡 **Procedural lore generator** — historical events, legendary figures, named items with backstory
- 💡 **Item adjective system** — random magical prefixes/suffixes ("flaming sword of cleaving"), procedural stat rolls
- 💡 **Roguelike permadeath structure** — meta-progression, unlocks across runs
- 💡 **Multiple endings / NewGame+** — branching late-game content
- 💡 **Modding API** — JSON-loaded user content beyond Objects.json
- 💡 **Multiplayer support** — co-op or asynchronous
- 💡 **Steam release polish** — settings menu, save migration, Steam achievements

---

## Cross-cutting concerns

> Things that aren't a single feature but affect many.

- ⚪ **Determinism audit** — ensure all RNG goes through seeded `System.Random` instances; no `UnityEngine.Random` in gameplay code. Important for quest seeds, save reproducibility.
- ⚪ **Save graph completeness** — already 56+ tests; the deep-dive audit (commit `0b00968`) found 0 bugs. Re-run periodically as new fields ship.
- ⚪ **Performance budget** — turn-based combat is forgiving; no profiling done yet. Likely safe until 100+ entities/zone.
- ⚪ **Accessibility** — colorblind mode (currently use `&R`/`&C` color codes for swords; ensure shape/glyph differentiates too).

---

## How to add to this doc

When you have an idea:
1. Pick the smallest tier it could ship in. If it requires new code paths, it's not Tier 1.
2. Add a `💡` bullet under that tier's relevant subsection.
3. Note the system(s) it builds on, and what it unblocks.
4. Don't promote to `📋` unless it's the next thing in line.

When you ship:
1. Flip status to `✅` with the merge commit hash.
2. Add a one-line entry to "Recently shipped" with the same hash.
3. If the ship revealed new ideas, drop them in as `💡`.

When you reject an idea:
1. Mark it `❌` with a one-line rationale (e.g., "❌ Friction-based stamina — duplicates Speed, no clear gameplay payoff").
2. Don't delete — record the decision so we don't re-litigate.
