# Fifty improvements — player clarity (16–30)

Status: all 15 clarity outcomes in IDs 16–30 are implemented and accepted within the bounds below. All 55 clarity tests pass in the final 831-case compatibility sweep. The isolated native run passes 13 UI checks and all separate meal, combat and world benchmarks, with zero errors. Root and the clarity reviewer inspected all six screenshots. Root alone operates Unity and commits. These are player-facing outcomes, not a count of tests; combat owns IDs 1–15, world interactions own IDs 31–45, and prepared meals own IDs 46–50.

## Intent and bounds

Make existing choices understandable at the moment of choice. Keep the world quiet: compact existing rows, optional full readers, one current hauling cue, no map-wide labels, automatic path suggestions or compulsory extra confirmation. Reuse the existing paginated AnnouncementUI reader and preserve cursor/scroll on return. The new F1/F2 readers are free, keep their full prose out of MessageLog, and preserve pending world announcements. Existing world examination retains its announcement behavior when showing the added surface facts. Existing commands remain authoritative when the player acts. Approved bindings: contextual F1 for selected item/loot/trade/crafting/mod details; F2 for current effects in inventory; `/` for inventory name search. Normal F1/? remains Controls. Search owns its text input and Escape first exits search, so those characters never invoke an item/world action while typing. Preserve existing Skills/Abilities D/P bindings and show contextual footer hints.

This is CoO-original UI work informed by the project's sparse Qud-like information policy. No claim of newly researched Qud-source parity. It does not rebalance combat, add inventory capacity, enable developer recipes, reveal distant contents, or change world generation.

## Source sweep and corrected premises

| Initial candidate | Verified current source | Consequence |
|---|---|---|
| Add equipment comparison | `Assets/Scripts/Gameplay/Items/EquipmentComparisonService.cs:17` already lists candidate/current facts and actual planner displacement choices; `InventoryUI.cs:1144` exposes it | Item 16 adds contribution **deltas**, preserving the existing reader; it is not a second comparison feature. |
| Add item mechanics inspection | `ItemExamineService.cs:18` already covers melee, armor, equip bonuses and liquid volume; `TonicExamineService.cs:39` gives tonic effects/delivery | Reuse these facts. New item 18 supplies access before acquiring loot; item 29 fills the distinct Food omission. |
| Add crafting previews | `InventoryUI.Crafting.cs:536` already renders forge stats; `:568` renders brew property/potency rows; `:599` explains station batching | Item 26 explains complete resolved effects and long data in an optional reader; it does not duplicate preview computation. |
| Add pile target selection | `WorldInteractionSystem.cs:247` already declares PickTarget/PickCell/ViewPile flow | Pruned. No new picker framework. |
| Pack capacity always prevents pickup | `InventoryPart.cs:546` derives Strength burden threshold; `:563` checks overburden; hard `MaxWeight` is separate | Item 22 separates lift strength, pack burden and hard destination capacity. Do not predict a refusal from the burden threshold alone. |
| Ammunition compatibility | File/symbol sweep found an Ammo category but no current item ranged weapon/ammunition implementation | Pruned rather than proposing UI for an absent mechanic. |
| Terrain names/status absent | `CellStatusReadout.cs:27` already shows coating/residue names, duration, heat/cold/charge; `:120` marks slippery liquids | Item 25 adds consequences of identified liquid properties, only on explicit visible-cell inspection. |
| Tinkering available to every player | UI tab exists; `BuildTinkeringRows` requires a BitLocker and known recipes; `DisassembleCommand.cs:38` requires a BitLocker | Items 27–28 explain existing access only. No grants, no replacement actions, no hidden recipes. |
| Need new full spell help | Previous milestone already shipped AbilityDetailsBuilder/RitePreviewBuilder and D/P routes | Pruned in full. Controls reader only documents those existing routes. |

Source paths below are relative to the repository. Line anchors identify the inspected baseline and may move during implementation.

## 16. Equipment contribution deltas

**Player outcome:** The existing comparison says how the candidate changes equipment contributions after every displaced item is removed, instead of asking the player to subtract several fact blocks.

**Source:** `Assets/Scripts/Gameplay/Items/EquipmentComparisonService.cs:54`, `:172`; `Assets/Scripts/Gameplay/Inventory/Planning/EquipPlan.cs:25`; `ItemExamineService.cs:66`.

**Plan/design:** Append a compact delta block to each actual auto/manual EquipPlan choice. Sum each distinct displaced item's armor AV/DV, armor speed penalty and parsed equip bonuses once; subtract those from candidate contributions. Show only changed stats and label them “equipment contributions”; retain natural attack/conditional enhancement facts and the existing warning that other effects are not totaled. Do not sum weapon dice into DPS or claim a final hit chance. Already-equipped candidates need no imaginary swap.

**Acceptance:** A two-hand candidate displacing two different items reports both contributions exactly once. An armor tradeoff reports positive AV and negative DV rather than a generic “better”. Reader remains free and ownership-protected.

**Counters/adversarial:** One two-slot displaced item counts once; zero change is neutral; malformed bonus pairs retain existing parser policy; no absent actor stat benefit is promised; no equipment mutation, events, RNG or final-stat prediction.

## 17. Find an item in a large inventory

**Player outcome:** A player can narrow a crowded pack by item name without losing category context or selecting the wrong item after an action.

**Source:** `InventoryUI.cs:677`, `:1789`; `InventoryScreenData.cs:117` currently sorts items by stable ID before category grouping.

**Plan/design:** Add an inventory-list-only search mode using an explicit `/` footer hint. Match a literal, case-insensitive substring of the current displayed name; no regex or hidden blueprint search. Categories with no matching rows disappear. A small clear/cancel action restores the unfiltered view; Escape first leaves search, then closes inventory. Keep item identity as selection anchor across rebuilds. No search affects equipment, crafting, command eligibility or saved game data.

**Acceptance:** A late-list named item can be found and its ordinary menu opens; matching equipped items remain identified. Clearing returns to the previous item/nearest valid row.

**Counters/adversarial:** Empty query preserves baseline ordering; zero results shows a clear message; deleted or merged selected items never activate another row via a stale index; typing does not trigger drop/throw or world movement.

## 18. Inspect loot before taking it

**Player outcome:** The pickup list can show a selected item's complete description, stack weight and existing equipment/tonic facts before the player acquires it.

**Source:** `PickupUI.cs:114`, `:303` currently supports take/all/close and displays names; `ItemExamineService.cs:18`; `ExaminablePart.BuildExamineLine`.

**Plan/design:** Add optional inspect to the existing ground/container loot popup, preserving the popup, selected object and scroll when the reader closes. Reuse current item facts and pure full-name text; include unit count, per-unit weight and total stack weight. Keep ordinary take keys fast. Use current source admission: visible canonical ground item or contents of the currently opened, accessible adjacent container. No new loot menu and no “inspect” event that executes an action.

**Acceptance:** A long-named multi-effect item can be read in full from ground or open chest, then taken by the existing command.

**Counters/adversarial:** Moved item, relocked container, stale contents row, fogged source and Render.Visible=false refuse details; no pickup, paid turn, RNG use or message-log dump from reading.

## 19. Explain partial Take All results

**Player outcome:** When some loot remains, the existing popup says what was taken and why the remaining rows could not be taken.

**Source:** `PickupUI.cs:236` currently loops commands, ignores returned reasons and closes unconditionally; `:203` already preserves an individual refusal in `_statusMessage`.

**Plan/design:** Keep current command order and successful transfers. Count actual successes, retain actual failed rows and show one bounded aggregate status plus the selected row's reason. Close automatically only when nothing remains. Revalidate each command as today; do not forecast a successful transfer or implement a new atomic batch transaction.

**Acceptance:** Mixed light/too-heavy stock leaves only refused rows, preserves successful items and reports both outcomes; a later retry uses current strength/capacity.

**Counters/adversarial:** All-success keeps current fast close; all-refused transfers nothing; hook refusal is reported rather than replaced by a guessed capacity message; repeated Take All does not duplicate already-taken items or add an extra turn.

## 20. Container destination fit

**Player outcome:** When choosing where to put an item, the player sees which reachable container is locked, full, or can accept a merge, before committing.

**Source:** `InventoryUI.cs:1204` adds one “Put in” row per available destination; `ContainerPickerUI.cs:230` is a source selector for taking contents, not the put destination flow. `ContainerPart.cs:30` uses MaxItems and permits merges into full containers; `PutInContainerCommand.cs:66` remains authoritative.

**Plan/design:** Attach optional details to the existing inventory “Put in <container>” action row: current entries/max entries, selected carried stack count, and a pure current fit/reason based on the exact container capacity/stack compatibility rules. “Unlimited” is not a numeric zero. A full compatible stack merge can fit without consuming an extra entry. Show only current reachable unlocked contents already admitted by the transfer flow; a locked container can say locked without naming its contents.

**Acceptance:** Two same-named containers can be distinguished by fit; a full container accepting a complete merge is shown as fitting.

**Counters/adversarial:** Partial merge requiring a new full entry is refused; foreign/moved/locked owners expose no contents; examining fit never calls AddItem, MergeFrom or inventory events; selection still executes the original command and can fail if state changes.

## 21. Understand a trade's immediate consequences

**Player outcome:** The existing buy/sell confirmation gives whole-stack price, own purse after trade and resulting pack weight, alongside optional item details.

**Source:** `TradeUI.cs:119`, `:529`, `:543`; `TradeSystem.cs:26` uses stack-aware value and `:169`/`:181` rechecks live prices and purse.

**Plan/design:** Build an explicit “entire stack of N” quote from current TradeSystem getters. Show current→after own drams, total stack weight and carried weight; distinguish burden warning from hard MaxWeight refusal. Permit the same optional full-item reader without purchasing. Do not expose trader private purse or predict callback approval. Existing confirmation remains the only confirmation.

**Acceptance:** Buy and sell quote correct opposite purse/weight changes and full stack scope; inspection preserves cursor and does not trade.

**Counters/adversarial:** Insufficient own funds are clear; stale stock, changed prices and denied trade still defer to the transaction; quote changes no purse/stack; no unit price mistaken for whole-stack price; no visibility into non-admitted stock.

## 22. Actor-relative item handling card

**Player outcome:** Item inspection distinguishes “I can carry it”, “my pack will be burdened”, “I can throw it this far”, and “I can only haul it”.

**Source:** `HandlingService.cs:58`, `:71`, `:99`, `:132`, `:157`; `DragRules.cs:108`; `InventoryPart.cs:457`, `:546`.

**Plan/design:** Add a small contextual handling block to owned/visible item readers: effective per-item and stack weight, actual lift/throw Strength gates, current throw range only when throwable, grip, and carry-versus-haul verdict. Use HandlingService/DragRules rather than re-deriving formulas. Explain current pack burden separately from lift and hard capacity. No thrown damage forecast or new throw action.

**Acceptance:** Low-Strength and high-Strength readers of the same item receive different relevant verdicts; a haulable noncarryable beam is correctly described.

**Counters/adversarial:** Rooted scenery and living creatures are not advertised as cargo; nonthrowable items get no range; stack total is not fed into the per-item lift formula; readout does not repair ownership or apply a speed penalty.

## 23. Current hauling cue

**Player outcome:** A compact persistent status tells the player which load they are pulling and its current speed penalty, with the existing release path.

**Source:** `SidebarStateBuilder.cs:41` vitals/status currently omit DragPart; `DragSystem.cs:18`, `:128`, `:166`; `WorldAffordanceQuery.cs:143` already verifies a two-sided release opportunity.

**Plan/design:** One status line only while the player's canonical same-zone two-sided drag link is current: “Hauling <name> (Speed -N)” and a concise interaction/release hint. Read applied current penalty rather than recalculating a promised future penalty. Do not change following, diagonal movement or release actions.

**Acceptance:** Grabbing a load adds the cue, moving updates normally, letting go removes it, save/load reconstruction reflects actual current link.

**Counters/adversarial:** No drag means no line; one-sided/stale/foreign-zone link yields no load-name leak; querying cannot call ValidateLink or repair/detach state; no permanent extra speed mutation.

## 24. Full player status reader

**Player outcome:** An optional player status reader explains every current benefit and affliction, with remaining duration/charges, beyond the sidebar's compressed name list.

**Source:** `SidebarStateBuilder.cs:151` currently joins effect names; `EffectDescriber.cs:13` already describes live fields; `CellStatusReadout.cs:61` presents every effect under “Afflicted”.

**Plan/design:** Add a discoverable free status/details action in inventory's existing stats area, using EffectDescriber for every live effect. Use neutral “Current effects” or honest positive/negative groups based on existing effect metadata. Keep sidebar sparse. Include no hidden NPC effects or invented duration conversion; reuse owner-turn wording for spell charges and root's meal description.

**Acceptance:** Long mixed status list remains fully paginated; charge/duration changes are read on opening; empty state clearly says no current effects.

**Counters/adversarial:** Unknown effect uses existing fallback; reader does not call AllowAction, Apply/Remove or Tick; no cached state across different actors; positive meal/buff is not described as a disease.

## 25. Visible surface properties reader

**Player outcome:** Explicit terrain inspection explains what a named liquid does on contact, rather than only saying water/oil/acid/slippery.

**Source:** `CellStatusReadout.cs:85`; `LiquidDefinition.cs:35` conductivity/combustibility/fire damping, `:72` slip chance, `:84` per-turn coat damage, `:89` modifiers; `LiquidCoveredEffect` and `LiquidSlipSystem` implement these properties.

**Plan/design:** Extend explicit focused ground detail with a bounded property description built from the currently visible cell's actual registered liquids: slip chance, coating damage, elemental interaction, and nonzero stat/resistance effects. Label conditional coat properties “while coated”; no promise that a given target will receive them and no multi-cell reaction simulation. Keep existing compact GroundLine unchanged for idle sidebar/menu titles.

**Acceptance:** Visible slippery liquid reports its live clamped chance; acid gives ongoing coat damage; water describes damping/conduction; current duration still comes from existing ground lines.

**Counters/adversarial:** Dry, unregistered, foreign-cell, unexplored and not-currently-visible cells reveal no new details; no seeding, reaction resolution, RNG, status application or propagation; do not expose dormant metadata as an active effect.

## 26. Full crafting outcome reader

**Player outcome:** A selected recipe/mix can be read in full, including human-readable on-hit or brew effects, delivery form and exact selected inputs, before crafting.

**Source:** `InventoryUI.Crafting.cs:536` clips OnHitEffectsRaw; `:568` prints property/potency rather than effect wording; `ForgePreview.cs:22`; `BrewingService.cs:387`; `TonicExamineService.cs:72` demonstrates shared effect construction without application.

**Plan/design:** Add optional details from the crafting result box. Use the existing ForgePreview/BrewPreview unchanged, format known on-hit specs through the existing shared parser/factory and brew effects through TonicEffectFactory/EffectDescriber. Explain missing components and current station/batch limits using existing values. No detached produced entity, creation callback or preview recomputation with different rules.

**Acceptance:** A long multi-effect result preserves all effects, chance/condition and chosen ingredients; returned craft panel has identical selections.

**Counters/adversarial:** Incomplete/invalid mixes remain unavailable with their actual reason; no consume/craft mark/bit changes; unknown effects use truthful fallback; same input preview remains equal to actual product under existing crafting tests.

## 27. Target-specific modification preview

**Player outcome:** At an already-authorized modification target picker, the player can see how that particular item changes and what is spent.

**Source:** `InventoryUI.cs:2834` currently shows current target stats; `:2135` applies immediately on selection; `ITinkerModification.cs:7`; `SharpTinkerModification.cs:10` has shared bonus constant and CanApply.

**Plan/design:** Optional details in the existing target picker show known recipe description/cost plus target-specific before→after contributions for the existing supported modifications. Add minimal read-only metadata beside those implementations if needed, referencing their actual constants; never call Apply on the live item or a fake clone. Preserve target selection and current access/known-recipe filters. Unknown extension mods get description and “numeric preview unavailable”, not guessed stats.

**Acceptance:** Sharp preview shows current and incremented penetration; armor mod tradeoffs show both gains and penalties; already-modded/stacked incompatible item retains real reason.

**Counters/adversarial:** No BitLocker/unknown recipe grants access; preview spends no bits/ingredient and adds no tag; dynamic current stat changes update; no unsupported modification claims.

## 28. Disassembly yield before destruction

**Player outcome:** A legitimate disassembly action explains exactly which bits one unit yields and which item/quantity will be consumed.

**Source:** `TinkeringService.cs:275`, `:382`, `:433`; `DisassembleCommand.cs:38`; `InventoryUI.cs:1178`, `:1564`.

**Plan/design:** Expose the existing pure yield resolver through a narrow read-only query and add its output to item details/action help. Say “one unit” when the command consumes one, display named counts or the existing bit glyph vocabulary, and explain that recovery is lossy. Do not add a new destruction confirmation or a new disassembly action. Query works only for currently owned positive units and says unavailable when the actor lacks existing tinkering access.

**Acceptance:** NumberMade>1 recipe gives the exact per-unit partial yield used by real disassembly; quantity scope is clear.

**Counters/adversarial:** CanDisassemble=false, empty/foreign item and no BitLocker do not advertise executable access; reading doesn't consume a unit or add bits; recipe fallback and explicit TinkerItem metadata match actual resolver policy.

## 29. Food and prepared-meal decision card

**Player outcome:** Before eating, the player can distinguish immediate healing, a prepared meal's timed benefit, and which current meal that dish replaces.

**Source:** `FoodPart.cs:14`, `:52` current healing/one-unit consumption; `ItemExamineService.cs:18` currently omits Food. Root owns new FoodPart MealStat/MealBonus/MealDuration, DescribeMeal and PreparedMealEffect (IDs 46–50).

**Plan/design:** Read FoodPart.Healing and root's shared DescribeMeal for item facts. Actor-context card also includes current HP missing and active prepared-meal text. State same-meal refresh versus different-meal replacement using root's actual identity/rule; no predicted random healing roll. Raw food is not assigned the cooked benefit. Use existing Eat action; no new effects, cooking recipes or stacking policy here.

**Acceptance:** Cooked dish gives exact benefit/duration and one-unit scope; a different current meal is identified as replaced; same meal says refresh; raw counterpart lists only its actual healing/use.

**Counters/adversarial:** No current meal doesn't invent a replacement; malformed/no FoodPart yields no food card; opening never heals/consumes/refreshes duration; full HP is reported without implying eating is prohibited; absent actor stats follow root's real effect rules.

## 30. Controls as a readable reference

**Player outcome:** F1/? and Pause→Controls open one full, scrollable reference instead of flooding the combat log, including the existing optional detail routes.

**Source:** `ControlsReference.cs:23`, `:50`; `InputHandler.cs:274`, `:794` currently call PrintHelp(MessageLog.Add); previous clarity milestone provides the direct AnnouncementUI reader route.

**Plan/design:** Add a pure full-text builder from the existing binding table and open it with the same direct free reader. Include concise sections for normal play, inventory/search/status, optional item/crafting details and existing Skills/Abilities D/P, matching actual keys after integration. Keep boot's short summary. Preserve pending announcements and the invoking pause/game state.

**Acceptance:** Full controls fit pages and can be read from normal play and pause; closing returns to the same state; reference contains actual new affordances.

**Counters/adversarial:** No log dump, paid turn, modal overwrite or held-key movement after close; no invented bindings; existing debug control remains accurately labeled rather than silently normalizing it as gameplay.

## Implementation sequence, tests and acceptance

1. Root-observed RED for query/invariant tests; then smallest pure changes (readouts, search model, delta formatting) with tests and `.meta` files.
2. Wire the existing popups/readers in short batches. Input tests exercise actual existing selection handlers and return state, not just a presence check. Reflection only permits new query tests to compile together before implementation; assertions after presence guards specify real behavior.
3. Root runs focused GREEN, relevant existing inventory/transfer/announcement/equipment/crafting tests, then broader affected systems. Meaningful counters include ownership/fog, stale selection, multi-slot/stack deduplication and read-only effects. No mirror tests for trivial labels alone.
4. Bounded root-controlled native keyboard witness covers controls, current effects, literal food search and reader return, prepared-food facts, ground-loot details and partial Take All, and whole-stack trade quote/cancel. Trade enters through its existing host method with controlled stock; it is not ordinary dialogue-discovery evidence. Other cards, owner gates, surface thresholds, hauling and contribution accounting are covered by focused tests, not claimed native-key coverage. Prepared meals append the separate root-owned 20-case command/owner-clock/save benchmark. Tinkering remains access-limited and has no ordinary starter-access claim.
5. Inspect actual screenshots for paging/overlap/readability; report exactly which routes were exercised. Automated text proves data and free-state invariants, not readability or player enjoyment.

Initial fixture: `FiftyClarityDetailsTests` (per-item pure/query contracts and counters). The initial eight existing-flow input cases were added to `FiftyClarityReaderInputTests`; the partially successful pickup case targets the real PickupUI method before production. No new scenario/menu framework is required; extend the recent isolated native reader driver or add a compact sibling for native acceptance.

## Ownership and overlap ledger

- Clarity: presentation inventory/pickup/container/trade/sidebar/input reader wiring; new pure detail/browse builders; EquipmentComparisonService delta formatter; narrow existing disassembly resolver query and supported-mod read-only metadata only if required.
- Root: FoodPart/PreparedMealEffect/meal blueprints and its EffectDescriber case. Clarity reads these contracts after they exist. No shared effects edit without coordination.
- Combat agent: mechanics, equipment balance, tonic duration/cures and throwing. Descriptions must consume final stable behavior; no duplicated mechanic fixes.
- World agent: crop/seed/liquid-vessel/waterskin/torch/campfire interactions. No edits to these parts by clarity. Agent will supply exact new paid-command predicate for a coordinated isolated InputHandler hunk.
- Shared InventoryUI/InputHandler edits remain sequential with other agents. No command time-cost classification change belongs to a free reader.

## Implemented map and focused acceptance

| ID | Concrete implementation | Acceptance/counter state |
|---|---|---|
| 16 | Existing EquipmentComparisonService appends actual-plan AV/DV/Speed/equip-bonus deltas, deduplicating displaced entity instances. | Armor tradeoff and two-slot dedup cases; focused native GREEN. |
| 17 | InventoryUI literal displayed-name search, retained selection anchor, matching categories and explicit empty state. | Empty/no-match/order cases and actual keyboard typing/F1/F2/Escape ownership counter. |
| 18 | F1 on current ground/container loot calls the same full fact builder under current source admission. | Visible facts, fog/hidden/foreign refusal, actual modal return/pending notice tests. |
| 19 | Take All removes only actual successes; failed rows remain with actual command reasons. F1 includes the full last-take result when the compact line clips. | Real mixed successful/strength-refused command loop test. |
| 20 | F1 on an existing Put-in row explains current entry capacity and complete compatible merge. | Merge/new-entry and locked-content counters; no alternate destination picker. |
| 21 | F1 full quote plus whole-stack units/purse/pack weight in the existing confirmation; stack weight rows corrected. Confirmation truncates a long name while preserving the complete price. | Buy/sell accounting and no-transfer input tests. Actual painted-row short/long-name regression observed RED, then GREEN after the final correction. |
| 22 | Existing item fact readers append HandlingService gates/range, grip and separate pack burden/capacity. | Nonthrowable, foreign/null, stack-versus-unit and noncarryable haul counters. |
| 23 | Sidebar has one compact HAUL penalty/load line for a current two-sided link; full F2 status includes release-menu guidance. | Absent/stale/released link and actual AppliedPenalty cases. |
| 24 | Inventory F2 opens all current EffectDescriber text in direct AnnouncementUI. | Live charge/window, no virtual action-blocker query, empty-safe and changed actor/zone cancellation cases. |
| 25 | Explicit world examination appends current visible registered surface consequences; idle GroundLine unchanged. | Slip/fog and actual 49/50 conductivity/combustibility threshold counters. |
| 26 | Crafting F1 reads current ForgePreview/BrewPreview, full parsed effects, selected inputs and station/batch context. | Complete/incomplete forge and actual single-Mendleaf/invalid brew cases. |
| 27 | Existing known modification target picker F1 reads current constants for Sharp and four armor mods; unknown mods remain prose-only. | Sharp before/after with no Apply/tag; unsupported numeric preview refusal. |
| 28 | Existing pure disassembly resolver has one public description seam; owned-item F1 states exact partial bits per unit. | NumberMade/one-unit and missing BitLocker counters; no access grants. |
| 29 | ItemExamineService shares FoodPart meal description; F1 adds missing HP and current meal refresh/replacement. | Raw/cooked and same/different current meal counters; no roll/consume/refresh. |
| 30 | Normal F1/? and Pause Controls open the paginated direct reader, preserving queue and return state. | Binding completeness, queue/log/turn preservation. |

New readers call `AnnouncementUI.Open` directly, leave logical menus open, and restore their rendering after queued world announcements drain. A changed actor/zone cancels the old context. Search intercepts input before item shortcuts. The ordinary inventory and world-command dispatchers now recognize the world team's shared `WorldResourceActions.IsCommand` after actual success; a native-key inventory-transfer test checks one turn on success and none after the selected destination is removed.

### Source corrections and scope bounds

- Destination-fit help belongs to the existing inventory Put-in action. ContainerPickerUI selects a source to take from and was not repurposed.
- Equipment deltas report item contributions, not final combat results. They follow the combat team's actual support for previously absent elemental resistance stats.
- Per-unit handling weight and whole-stack pack weight remain separate; the Strength burden threshold is not a hard inventory refusal.
- Surface amplification uses LiquidCoveredEffect's actual thresholds. Root review caught the positive-value error; the below-threshold case failed before the correction was applied.
- The compact partial-loot line remains bounded. Its F1 reader contains the complete actual refusal text.
- F1/F2 preserve the existing D/P skill and ability readers. No tinkering access, recipes, capacity, combat rules or world-generation behavior were added by this work.
- WorldResourceActions' shared successful-action predicate is wired into inventory and world input timing. Real-key transfer tests caught the world's ordinary-actor admission error; that guard was corrected by the world owner, with the input tests unchanged.
- Native coverage is a bounded set of high-impact UI routes. The other cards are supported by focused tests, not claimed keyboard walkthroughs.

### Verification receipts

| Receipt | Observed result | Meaning |
|---|---|---|
| [all-red.xml](Verification/FiftyImprovements/all-red.xml) | Initial 39 clarity cases fail for missing readers/routes. | Observed RED before implementation. |
| [first-implementation.xml](Verification/FiftyImprovements/first-implementation.xml) | 50 of 53 clarity cases pass. | Surface threshold and real transfer-menu availability failures exposed actual integration defects. |
| [review-red.xml](Verification/FiftyImprovements/review-red.xml) | All 53 clarity cases pass. | Threshold and shared-world corrections are effective. |
| [menu-bed-review-red.xml](Verification/FiftyImprovements/menu-bed-review-red.xml) | 51 of 54 clarity cases pass. | Two stale-context renderer assertions and one shared-popup-canvas assertion fail before their fixes; the latter has 926 glyph cells instead of 1,037. |
| [focused-green.xml](Verification/FiftyImprovements/focused-green.xml) | All 342 combined cases pass, including all 54 clarity cases. | Includes actual key dispatch for search/transfer and the corrected menu rendering/return paths. |
| [compatibility-and-price-red.xml](Verification/FiftyImprovements/compatibility-and-price-red.xml) | All 54 earlier clarity cases pass; the new price-row case fails. Combined result: 827 of 829 cases pass. | The actual painted long-name confirmation hides its 146-dram price. The short-name control passes before the same test reaches the long-name assertion. |
| [corrections-green.xml](Verification/FiftyImprovements/corrections-green.xml) | All 829 cases pass, including all 55 clarity cases. | The price remains visible for short and long names; all earlier clarity and selected compatibility checks remain GREEN. |
| [final-green.xml](Verification/FiftyImprovements/final-green.xml) | All 831 cases pass, including all 55 clarity cases. | Final source acceptance after the bounded rest review; the earlier price and menu corrections remain GREEN. |

The earlier [broad sweep](Verification/FiftyImprovements/broad-first.xml) exposed compatibility failures; the final 831-case run rechecked the affected fixtures and milestone tests, rather than repeating the entire broad suite. Source-only runtime, EditMode and Editor compilation also passed. Compiler checks are preflight evidence, not a substitute for Unity results. The initial 39 cases and the later threshold/menu corrections have separately observed RED; supplemental counters were not each individually run RED before implementation.

Current suite: `CavesOfOoo.Tests.FiftyClarityDetailsTests` (25), `FiftyClarityAdversarialTests` (14), and `FiftyClarityReaderInputTests` (16). The dedicated adversarial and input suites cover 30 distinct cases; they were extracted or added for actual ownership, visibility, state, input and presentation risks rather than test-count padding.

### Native witness and player-facing limits

The verified launcher is `Caves Of Ooo/Scenarios/UI/Fifty Clarity Native Audit`. It requires 13 UI checks and records six screenshots: normal Controls, Pause → Controls, current effects, prepared-food details, remaining loot after partial Take All, and a whole-stack trade quote.

The UI witness starts Classic through the ordinary new-game keys, then uses a disclosed controlled fixture: flattened starting zone and player position, one cooked food, a Heart Flame status, loose light timber, one strength-refused load, finite trader stock, and a 1,000-dram purse. Menu entry, search, F1/F2, pause selection, Take All and return keys are native keyboard events. Trade starts through the existing host method with controlled stock; it does not prove ordinary dialogue discovery. The observer reads copied pages through the public paging API.

Three detached benchmarks follow, reported separately: 20 meal checks, 12 combat checks and 15 world checks. They use actual commands, owner scheduling and replacement saves; they do not establish keyboard gameplay or ordinary discovery. The world growth witness deliberately advances its detached clock. Overall completion requires every suite and preserved live UI context, not just a nonempty report.

The [final native report](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/report.json) records completion with 13 of 13 UI checks, 20 of 20 meal checks, 12 of 12 combat checks and 15 of 15 world checks passing, with zero errors. Root restored the editor to EditMode afterward.

Root and the clarity reviewer inspected the three changed final captures (normal Controls, Pause → Controls and partial pickup). The effects, food and trade images are byte-identical to their already-inspected earlier captures. All six final views are linked below:

- [Normal Controls](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/01-controls.png) and [Pause → Controls](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/01b-pause-controls.png): readable first page, intact bounds and visible page-navigation hints; the automated reader also checks the complete two-page text.
- [Current effects](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/02-current-effects.png): current Heart Flame charges and owner-turn window are readable.
- [Prepared food](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/03-prepared-food.png): healing, timed meal benefit, replacement rule and handling facts fit the reader.
- [Partial pickup](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/04-partial-loot.png): the actual successful count and remaining strength refusal are visible. The carry refusal appears redundantly in the facts and result; this is minor wording repetition, not missing decision information.
- [Trade quote](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/05-trade-quote.png): whole-stack units, price, purse and weight after buying are readable. The long-name confirmation fix is verified by the painted-row test, not this quote screenshot.

These receipts establish the captured reader presentation, current values, free reading, queue preservation, ownership/refusal behavior, actual transfer turn payment, and valid return rendering. They do not establish novice comprehension, sustained balance, ordinary item availability or player enjoyment. No such claim is made.

### In-phase review (Q1–Q4)

Q1 — Opposite accounting is explicit: buy/sell signs, candidate/displaced contributions and same/different meal behavior have paired checks. Reading does not execute the matching consume, modify, equip or trade command. Returning from a reader preserves the live menu; replacing its actor or zone cancels it and resumes world rendering.

Q2 — New F1/F2 readers share one direct modal route. Pause clears its shared canvas before Controls draws. Existing world examination and D/P ability routes retain their own behavior. Current visible owners admit loot names; locked containers deny their contents. Search owns typing before item shortcuts.

Q3 — Scope remains information and existing input integration. Shared modification constants replace existing literals without rebalance; TinkeringService exposes its existing pure yield resolver. The native route is deliberately finite, and direct benchmark checks are not counted as keyboard coverage.

Q4 — **Accepted within stated bounds.** All 55 clarity tests and the final 831-case compatibility sweep pass. The native witness passes all 13 UI checks and the separate 20 / 12 / 15 detached benchmark checks. All six screenshots were inspected, with readable text, intact bounds and navigation. The remaining partial-loot wording repetition is recorded above. Historical failures remain in their receipts; ordinary discovery, player understanding and fun remain unmeasured.

### Owned files

Modified:
- `Assets/Scripts/Gameplay/Items/EquipmentComparisonService.cs`
- `Assets/Scripts/Gameplay/Items/ItemExamineService.cs`
- `Assets/Scripts/Gameplay/Tinkering/TinkeringService.cs`
- `Assets/Scripts/Gameplay/Tinkering/Mods/DuelistCutTinkerModification.cs`
- `Assets/Scripts/Gameplay/Tinkering/Mods/FlexweaveTinkerModification.cs`
- `Assets/Scripts/Gameplay/Tinkering/Mods/HardenedShellTinkerModification.cs`
- `Assets/Scripts/Gameplay/Tinkering/Mods/ReinforcedPlatingTinkerModification.cs`
- `Assets/Scripts/Presentation/Input/InputHandler.cs`
- `Assets/Scripts/Presentation/Rendering/SidebarStateBuilder.cs`
- `Assets/Scripts/Presentation/UI/ControlsReference.cs`
- `Assets/Scripts/Presentation/UI/InventoryUI.cs`
- `Assets/Scripts/Presentation/UI/PickupUI.cs`
- `Assets/Scripts/Presentation/UI/TradeUI.cs`

New, each with `.meta`:
- `Assets/Scripts/Gameplay/Tinkering/TinkerModificationPreview.cs`
- `Assets/Scripts/Presentation/UI/InventoryDecisionDetails.cs`
- `Assets/Scripts/Presentation/UI/InventoryUI.Clarity.cs`
- `Assets/Scripts/Presentation/Input/InputHandler.Clarity.cs`
- `Assets/Scripts/Scenarios/Custom/FiftyClarityNativePlayer.cs`
- `Assets/Editor/Scenarios/FiftyClarityNativeBatch.cs`
- `Assets/Tests/EditMode/Presentation/UI/FiftyClarityDetailsTests.cs`
- `Assets/Tests/EditMode/Presentation/UI/FiftyClarityAdversarialTests.cs`
- `Assets/Tests/EditMode/Presentation/Input/FiftyClarityReaderInputTests.cs`

Documentation: `Docs/FIFTY-IMPROVEMENTS-CLARITY.md`.

Native launcher: `Caves Of Ooo/Scenarios/UI/Fifty Clarity Native Audit`. Driver saves `Docs/Verification/FiftyImprovements/NativeClarity/<runId>/report.json` and six named PNGs. The launcher uses existing save isolation and restores scene setup, seed/build-choice settings and input devices/settings. Root alone launches it; root and the clarity reviewer inspected the saved captures.

Exact staging manifest: `/tmp/fifty-clarity-owned.txt` contains 32 owned paths: 13 modified source files, nine new source/meta pairs, and this document. Root integrates verification receipts separately.
