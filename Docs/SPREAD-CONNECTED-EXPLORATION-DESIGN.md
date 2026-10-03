# Connected exploration in the Spread

Status: **D0–D6 gameplay and models implemented; four native build journeys passed. Comparative performance and fresh-player evaluation remain deferred as detailed in §18.**

Design baseline: main `f663ef89762e19ae34e00fe876d866d79520214c`, 2026-10-02. The user initially requested design only, then authorized a saved implementation prompt followed by full autonomous implementation. This document owns the next milestone; [EXPLORATION-DEPTH-DISTRICT.md](EXPLORATION-DEPTH-DISTRICT.md) remains the record of the delivered well/cellar. Historical implementation ledgers remain evidence of their own scope. Proposed behavior is not shipped until recorded in §17.

Classification: original Caves of Ooo design. The reference goal is systemic, readable, quest-free exploration. No claim of exact Qud mechanics, code, content or balance parity. This is a persistent RPG: a different seed means a different new character/world, not a reset of the current character or a metaprogression loop. [PROJECT-IDENTITY.md](PROJECT-IDENTITY.md) governs that distinction.

## 1. Outcome and scope

Deliver roughly one compelling hour of ordinary exploration around the starting glade. Duration is a playtest target, not a promise established by this plan. A player should discover a useful material, choose between practical uses, prepare for a situation, solve or bypass it, and return to a place changed by that choice. At least two such chains must intersect. Neither quest acceptance nor completion of every local activity is required.

The milestone includes all four requested priorities:

| Priority | Concrete delivery |
|---|---|
| Connected starting region | Competing clay investments; a kitchen commission; a marked growing reserve; botanical ink preparation at Marrowstye. |
| Different encounter decisions | Six authored situation designs using actual combat, cover, hauling, harvesting, repair and access rules. |
| Consequential returns | Crops advance during absence; one physical kitchen batch finishes; permissions and breaches persist; consumed supplies stay consumed. |
| Progression and replayability | Finite noncombat accomplishments, immediate useful finds, and frozen variations in access, resources and approach geometry. |

Delivery requires the complete connected milestone. A single new pan, recipe, permit or crop-clock patch is an implementation checkpoint, not completion. All public services already provided by this region remain available on their established terms. The player can ignore both chains and continue exploring.

### Content budget

Reuse the glade, its cellar, seed keeper, wayside kitchen, alembic, forge, trapper and Marrowstye. Add at most one optional satellite situation within four cardinal world-map steps of the glade; do not require a new village. Add no crop species or enemy species. Reuse current original creatures and crops. New portable content is limited to a useful wrapped meal and a practical movement manual. New props are a batch pan, its physical input/output storage, a marked reserve, a mundane ink-preparation desk and the satellite's heavy frame when selected. Art is justified by a new visible function or state.

Two chains are deliberately enough for this milestone. A complete production economy, autonomous inter-zone couriers, offscreen combat, universal theft and additional biomes are deferred.

## 2. Verification sweep and corrected assumptions

Source references S01–S20 are indexed in §16. “Existing” below means source-observed behavior; it does not imply new native play evidence from this design task.

| Premise | Current evidence / correction | Design decision |
|---|---|---|
| Basic mechanics are missing. | Crops, harvest products, repairs, brewing, tempering, trading and saved depletion already work. [S01–S05] | Add situations and connections; preserve their ownership and payment contracts. |
| The opening is Morrowfast. | Current default is Spread glade 11.10. Older crop/repair documents describe a previous start. [S01] | Design around the glade; preserve Morrowfast's existing allotment and services. |
| There are no cross-place supply interactions. | `RegionalSituations` already defines five, including Gantry's grain exchange. [S06] | This milestone must add return development and competing uses, not rename a delivery reward. |
| All starts have the same spells. | Duelist and Breaker do not start with Calm; specialist builds differ. [S07] | No mandatory magic, recruitment or advanced movement gate. |
| One level buys Vault or Recruit. | Leveling gives one SP; a new tree and its power ordinarily cost two. [S08] | Do not count purchased advanced mobility as a first-expedition entitlement. |
| Hobbled slows movement. | Current `HobbledEffect` reduces DV by 3. Cold Snap applies it for six turns. [S09] | Do not design an escape distance around Cold Snap slowing pursuers. |
| Calm grants permission or friendship. | It supplies temporary stationary NoFight, not a faction change. [S09] | Witnessing and permission remain separate from the ability to attack. |
| A hedge necessarily blocks sight. | AI sight uses the actual cell/terrain solidity contract; decorative/physical props are insufficient. [S10] | Prove each cover owner blocks the real observer; label sight-line avoidance honestly. |
| Fire clay restores every masonry object. | Tier 1 uses explicit composition/fault/material recipes. [S04] | Add an explicit pan fault; no blanket repair or later-tier material substitution. |
| Existing free beds/oven/cot can become rewards. | Residents already offer public planting, cooking and rest. [S02] | Permissions cover newly marked reserve owners; pan service is supplemental. |
| All ink already refills books. | `InkVialPart` currently grants 25 rental ink. `GrimoireChargePart.Refill` and `ChargesPerVial=5` have no production re-ink action. [S11] | Re-inking is explicit NEW scope with consumption/UI tests; ordinary vial use is preserved. |
| Time away can be divided by ten without a behavior change. | Crop growth currently follows player TickEnd, while world time includes speed, rest and travel. [S12] | Adopt one world-tick timing policy deliberately (§8), document the speed-dependent change, and replace double ticking. |
| Saving state automatically creates interesting returns. | Spent stock and completed repairs persist; that alone does not create another opportunity. [S01–S05] | Make productive work, crop development and access changes observable after return. |
| Curation certification is general faction standing. | Marrowstye grants one physical counterfoil and finite cabinet access. [S13] | Preserve that exact outcome. New clerical service is separate; no free repeatable reputation. |
| A selected location is guaranteed to exist. | Frozen assignments and safe committed placements are distinct; optional builders can refuse. [S14] | Measure actual realized chains. Refusal never silently substitutes a hidden reward or promises an unavailable service. |

Readiness: 🟢 native owners/transactions, existing crops and products, repairs, local art, world clock and saved graphs; 🟡 local production/access, cross-location knowledge, actual discovery and encounter balance; 🔴 absent re-inking action and absence growth relative to this proposed design; ⚪ broad simulation and universal crime are intentionally out of scope. These are design readiness labels, not a claim of broken shipped features.

## 3. Region and its people

North is up. Locations below are established addresses, subject to their existing cold-generation admission rules.

```text
                      11.8 seed keeper / NEW marked reserve
                      11.9 field alembic
  10.10 Sill --------- 11.10 glade --------- 12.10 forge
                      | cellar                |
                      11.11 trapper -------- 12.11 kitchen / NEW batch pan
                                               |
                                            12.12 Marrowstye / NEW ink desk
```

Give the two particular near-start residents original local names: **Nella Tern**, seed keeper, and **Orven Kett**, wayside cook. Do not rename every generic resident. Preserve any already-authored identity found in the implementation sweep instead of overwriting it. Nella wants viable seed and access for people who tend the shared ground; Orven wants dependable supplies and room to finish useful work. Retain their public dialogue. The keeper's new reserve has visibly tied stakes and contains additional plants, not a retroactive claim over the four public beds. The cook can vouch for actual repairs but does not broadcast information telepathically.

Doreth and Ivrin at Marrowstye remain earnest Curation workers. The new desk prepares ordinary working ink for labels and practical use; it neither cures bodies nor transfers memories. A note can read: “Ink for identifying the vessel. Not a substitute for the vessel.” Absurdity comes from sincere distinctions and material care. Ordinary fully salt-cured bodies stay inert; the existing half-set quarantine remains optional and contained. Do not reveal protected cosmological answers or equate the mainline Curation with the Catchers. [S13, S15]

The forge and alembic remain alternatives for personal use of ingredients. Sill and the trapper remain optional sources of trade, supplies and historical directions. No new obligation, global faction or shortage counter is attached to them.

## 4. Chain A — clay, shared ground and expedition provisions

### A1. The first investment

Preserve the cellar's existing two physical FireClay units and glade well requiring two. Add a supplemental cracked batch pan beside the kitchen, also requiring two FireClay through a new explicit Masonry repair recipe. The pan is distinct from the working public oven: ordinary Cook and the cot do not become broken or paid.

The well supplies its existing drinking/filling service. The pan enables a compact field meal described below. Spending the first two clay units changes the next available service. Neither choice permanently excludes the other: a new marked marlroot plant and its returned seed provide a repeatable clay route through actual cultivation/Prepare. Existing distant sources also remain valid, but no nearby merchant is advertised as a guaranteed clay seller without a stock witness.

Show both uses before a typical first expenditure: retain the glade notice and give the pan a distinct hairline crack, empty setting and short diagnosis. No arrow or checklist instructs which repair is correct.

### A2. The growing reserve

Extend the seed keeper's accepted site with a separate two-bed reserve: Marlroot and Pitchpod, with their usual physical yields and returned seeds. One begins ripe; the other begins as a dry sprout. The frozen `ReservePriority` variation chooses which (§10). Preserve the existing four public beds and all existing inventory. Add one ripe Claspbean on unclaimed prepared ground near the kitchen, with its returned seed. These are new bounded sources using existing species, not changes to the global biome crop distribution.

The keeper explains the reserve and offers two deterministic access routes: pay 8 drams once, or relay the cook's actual witnessed acknowledgment of restoring the kitchen pan. The fee is provisional balance data, not a reputation roll. Permission grants harvesting and replanting on these two exact reserve beds and collection from their defined reserve tray. Public ground remains public. Access does not guarantee stock: crops can be spent, dry or immature. Reserve and kitchen crop locations must have real Terrain + Plantable ground and CultivatedSoil, validated by the actual planting action; decorative furrows alone do not count.

The cook gives one spoken introduction after observing the exact repaired pan and speaking with the living nonhostile player: “Tell Nella I trust your hands.” Save this historical testimony on the player, bound to the world, cook, site, pan and committed repair cause. A specific conversation choice lets the keeper hear and accept it; merely repairing the pan does not change her knowledge. No physical quest token is needed. The introduction establishes past work, not current remote availability, and never grants global or repeatable reputation. Text selected without valid saved testimony cannot forge it.

Gathering without permission is physically possible. The action menu warns on the specific marked crop/tray before the player commits. A living resident who can actually observe the action records a local breach after successful harvest/take; an unseen act does not acquire an omniscient penalty. A breach suspends reserve permission, including a previously earned permission, and the keeper explains why on return. Ordinary personal hostility and assault rules continue independently (§9).

Permission applies to the reserve's future planted owners only while the exact bed remains part of that claim. An unrelated crop on nearby soil is not claimed. Crops/yields elsewhere are not automatically classified as stolen. Rain, looking, walking through a public aisle, failed harvesting and opening a menu do not create a breach.

### A3. A worthwhile kitchen service

Proposed authored recipe: **2 Emberwheat + 1 ClaspbeanPulp + 2 drams → 1 wrapped field meal**, ready after 120 world ticks. One outstanding commission per exact kitchen service: Working or Ready, never both. The fee is transferred once to the cook. Ordinary crop inputs are physically committed once; cancelled or rejected commands consume nothing.

The meal weighs 1, has provisional Commerce value 18, and one Eat action heals `3d4` and removes one ordinary BleedingEffect. It does not prevent later bleeding or cure wounds, poison, disease or narrative conditions. This combined Eat behavior is NEW: attaching a CureTonic does not make it run on Eat. Implement one transactional consumption and the specified two effects, with refusal/rollback and actual UI coverage.

This is deliberately a tactical convenience with a cost: freely cooking the two grains produces two ToastedEmberwheat for `6d4` total healing, and keeping the pulp preserves a separate bleed treatment. The meal compresses weight and healing/treatment into one action at the expense of total healing, the fee and waiting. Full-HP/nonbleeding use still follows explicit food behavior, not a hidden refund or manufactured benefit. Preview the recipe, output and elapsed preparation time before payment.

The cook consumes actual committed ingredients. Waiting does not manufacture ingredients or replenish a commission. A finished meal remains a real item in the dedicated pickup container. Normal merchant stock and its ordinary restock remain separate. No additional healing supply is injected into the cook's random shop table.

### A4. What the player sees on returning

The pan is whole; wet sprouts may have advanced or dried; the batch changes from a covered setting to one wrapped parcel. The keeper acknowledges the work receipt or recalls the observed breach. These are concrete consequences across locations. The player can collect the parcel and continue, commission another using new inputs, cultivate the reserve, or ignore it.

If ignored, the public oven and cot continue working, the pan remains broken, and no unpaid batch starts. Nobody starves or loses all trade because the player did not participate.

## 5. Chain B — root dressings, burning resin and working ink

### B1. Sources that are useful before processing

Add two Sootroot crops to a cultivated recess in the glade cellar: one ripe, one dry sprout, each using the existing crop/seed/yield rules. The cellar's existing bare Floor is not plantable: author two real prepared Terrain + Plantable + CultivatedSoil beds within the staged layout, retaining the surrounding floor/geometry. Add crops and soil within the cellar's authoring transaction before final owner validation. This is a deliberate new source in the managed cellar, not a claim that ordinary cave crop allocation already covers it. Do not regenerate them in a cached cellar. Acceptance must replant the exact harvested bed through ordinary input.

The ripe plant yields two SootrootPulp and a seed. The pulp's existing immediate use is removing BurningEffect. Pitchpod resin from the reserve or other actual world sources can already become a burning brew through existing alchemy. Both are worth retaining for an expedition.

### B2. The clerical desk

Add an ink-preparation desk to Marrowstye's public supply-side working area, outside the existing finite cabinet and quarantine. It is available to a living, nonhostile local player while the assigned worker is alive and available. No filing quest or certification is required. Preserve the existing counterfoil, courier branch, cabinet and quarantine key unchanged.

Recipe: **2 SootrootPulp + 1 PitchpodResin + 3 drams → 1 InkVial**. This is new mundane processing: soot-dark pigment and a resin binder, not body preservation or a new sacred material. The worker explains the practical distinction. It is one immediate paid action; no second delayed-job system is needed. Inputs/fee and output share one transaction. No loose decoration or visually held stamp is counted as an input/tool unless it has an actual owner and rule.

Current base values are 6 + 3 for the botanicals and 10 for the vial. The three-dram service fee is part of the provisional anti-arbitrage budget. Verify current price extrema through the actual trade functions before implementation acceptance; reputation/ego discounts must not create a buy–process–sell money loop. Harvesting and selling one's own work can still be profitable.

### B3. Two real uses for ink

Preserve InkVial's existing **Use** action, which consumes one vial for 25 rental ink. Add **Re-ink** on a carried grimoire with a valid GrimoireChargePart and a positive deficit. It consumes one carried InkVial and restores up to the book's existing `ChargesPerVial` (normally 5), capped at `MaxCharges`.

The menu previews the exact gain. A nearly full book may intentionally waste excess ink after showing the amount; a full book refuses with no payment or turn. Reject malformed negative/overflowing charges or invalid owner/parts rather than repairing them silently. Selection targets the exact carried book and exact payment unit. Cancellation, capacity failure, stale selection, callbacks and outer rollback must preserve book and vial. One vial cannot both refill rental ink and re-ink a book.

This finishes an already declared but unwired capability. It does not change skill-spell cooldowns or make rites free. Preserve current casting authority: `GrimoireInk.FindInked` uses the first qualifying charged book of any kind; this feature selects which book to refill, not which book a rite must spend. Test selected refill identity and subsequent actual debit with two carried books. Rental use remains useful wherever the player has an actual rental opportunity; this milestone does not promise a rental merchant at the glade. A player without a charged book can retain/trade the vial or keep the botanicals. Do not force a spell build.

Provide a reachable book on every seed: retain the cellar's existing even-seed ShatteredRimeGrimoire / odd-seed Buckler, and give the exact new-world Ivrin one additional finite ShatteredRimeGrimoire for ordinary purchase. Its current base value is 20; charge the current `TradeSystem.GetBuyPrice`, with the live quote visible. Add TraderPart with no restock stock table only to this exact worker and preserve his existing inventory/dialogue; this non-villager remains outside normal table restocking. This is one actual copy, not a repeatable dialogue spawn. Do not change the general blueprint or existing cabinet.

Shattered Rime casts at an ordinary valid target without a primer (base damage 6, range 3, cooldown 40); consumed marks improve it and enable its armor payoff. For an accessible optional primer, gather actual FrostLichen at the nearby alembic/trapper and brew the existing cold tonic, then throw it at the target before the rite. Classic also has Rime Grip. Drinking the tonic freezes the drinker; Cold Snap is not a freezing substitute. Verify actual mark consumption and current freeze/thaw timing; a primer is an advantage, not a new hard casting prerequisite. Record the actual brewed output/effect in the implementation sweep rather than trusting the name “frost.”

### B4. Connection and return

The player weighs a burn remedy and offensive resin against future rite uses. Growing the returned seed, leaving for Marrowstye, and coming back makes the source productive without waiting beside it. Kitchen preparation can overlap that trip. An inked book can then change the next contested encounter, while a melee character may prefer the raw treatments and a field meal.

If the player declines Curation help or loses access, original field alchemy, raw treatments, ordinary ink finds/trade and the rest of the world remain available. Desk access is not a global spell-access gate. No autonomous curing, living-body preservation, deity revelation or faction-wide alignment change is implied.

## 6. Resource and consequence table

Numbers below are initial authored balance values, to be checked in ordinary play before calling the milestone complete.

| Resource | Choice A | Choice B | Cost and lasting consequence |
|---|---|---|---|
| First 2 FireClay | Restore glade well | Restore kitchen batch pan | Actual units disappear; one useful service becomes available first. Later clay can restore the other. |
| Marlroot harvest | Prepare its 2 clods into 2 FireClay | Keep/sell clods; retain returned seed | Clay supports another repair; seed supports a later harvest only after planting/water/time. |
| 2 Emberwheat + ClaspbeanPulp | Two free toasted meals plus separate bleed treatment | Fee/time for one wrapped field meal | More total healing versus one-action treatment and reduced carrying weight. |
| PitchpodResin | Burning brew/weapon preparation | One ingredient in working ink | Current combat preparation versus sustained use of a found rite; no free duplicate resin. |
| 2 SootrootPulp | Two separate burn treatments | One ingredient in working ink | Immediate emergency insurance versus another capability's replenishment. |
| InkVial | 25 rental ink | Up to 5 book charges | Same vial, one consumption; different actual uses, neither automatically selected. |
| 8 drams / cook's introduction | Pay for reserve access immediately | Invest clay and return with witnessed testimony | Money versus time/material investment; neither changes public beds. |
| Reserve relationship | Observe terms and keep local access | Take without permission when an opening exists | Immediate goods versus witnessed loss of that privilege; no global omniscient penalty. |

## 7. Six situations and their approaches

These are authored compositions of the two chains and existing encounters, not six new universal systems. At least four must appear through ordinary routes in the acceptance sample, and both chains must work. The optional satellite chooses one of situations 4–5; neither is necessary for escape or a required input.

### 1. Two clay seams

**Notice:** the cracked glade well and the kitchen pan visibly need the same material. A short notice names uses and likely sources without guaranteeing current stock. **Decision:** invest the cellar's first clay in drinking water or compact provisions. **Alternative:** retain the clay, gather marlroot, or leave both alone. **Aftermath:** distinct restored model and actual service; a second visit sees the choice. The existing finite basin remains a separate early source. No remote irrigation or well healing is promised.

### 2. The keeper's tied row

**Notice:** two extra beds behind tied stakes; a visible keeper and clear statement that public beds remain public. **Approaches:** pay, relay the cook's introduction, gather elsewhere, or harvest without permission and risk an observed breach. **Cost:** currency/material investment, longer sourcing, or lost local access. **Aftermath:** actual removed plant, returned seed, permission and remembered breach. The keeper's normal movement/line of sight may create an opening, but no scripted patrol or stealth skill is invented. Calm does not erase observation or authorize taking.

### 3. Sootroot behind the store

**Notice:** pale root shoots under dark skins in a side recess, and a practical description of burn treatment. Keep the cellar's safe landing, return stairs and current finite rewards. **Approaches:** direct combat with its original Marlback, sight-line avoidance behind actual opaque cover, or available control abilities. **Cost:** exposure/time/cooldown; raw treatment competes with ink use. **Aftermath:** the exact source stays harvested and can be replanted with its earned seed. No second enemy is added merely to guard new crops. Guard sight must not cover both retreat and all crop approaches at arrival.

### 4. Wet crossing, dry shoulder — optional satellite variant

**Notice:** visibly wet ground on one short lane, a dry narrow lane, and a longer route behind true opaque terrain. **Sources:** use an exact existing two-MarlbackScrabbler allowance and a real rolled container, or refuse this variant. Do not replace a single-creature roll with a pair or add a third attacker. Add a declared finite supplemental packet of 2 Emberwheat, 1 ClaspbeanPulp and 1 DitchkeepersFootwork manual to that container while retaining its original stock. This new supply budget enables one kitchen batch and later mobility. The heavy-frame alternative receives 2 FireClay and the same single manual instead. These are alternatives, not two free caches at one site.

**Approaches:** Stormcaller can combine real wetting and shock, or keep the dry target for Kindle. Bombardier can expend a finite throwable while avoiding friendly fire. Duelist can isolate one opponent in the dry bottleneck. Breaker can use a clear one-target frontage and existing shove/stun. Every build can retreat and take a longer physical approach. Wet terrain alone is not advertised as electricity; any charged ground must come from an actual supported effect. Cold Snap does not create an escape-speed advantage.

**Aftermath:** consumed supplies, actual damaged/dead actors and depleted cache remain. No fixed reset or paid “cleared” spawn. Seed variation swaps tactical exposure/wetness/cover, not just decorative rotation.

### 5. The heavy frame — optional satellite variant

**Notice:** a 136-weight noncarryable haulable frame across a shortcut, with a visible long outside route. **Approaches:** Breaker (Strength20) and Classic (18) meet the existing drag limit; Strength16 builds do not. All can use the detour. Later earned Vault may cross an appropriate two-cell gap with a clear landing; no tutorial claims everyone owns it.

**Cost:** hauling uses actual time and movement penalty; brute force does not mean free travel. Provide turning room, a release position and a bypass that remains open if the frame is moved badly. Slam does not destroy the frame or a wall. **Aftermath:** physical relocation creates a useful repeat shortcut; no reward for moving it back and forth. Tie the path to actual useful supplies or a reserve approach rather than an empty side room.

### 6. Work left on the pan

**Notice:** a covered paid batch and the cook's explanation that ordinary work takes time. **Approaches:** leave for the cellar/ink desk, do another local activity, or wait safely if the player chooses. **Cost:** committed grain/pulp, fee and one unavailable job slot. **Aftermath:** one real parcel appears once; a dead worker, destroyed station or blocked output has a defined result (§8). The cot still provides existing rest, and its extra elapsed world time advances the batch. No surprise offscreen attack is simulated.

### Actual capability limits

| Build / tool | Useful advantage | Limit the level design must respect |
|---|---|---|
| Duelist | Piercing short-blade kit, Rejoinder, Flurry; benefits from isolated targets | No initial Calm or mobility; do not assume precision selection among several adjacent creatures. |
| Breaker | Strength20; Conk/Slam/Ground Pound; heavy-frame access | No initial ranged attack or Calm; Slam moves/stuns, not wall destruction. |
| Stormcaller | Kindle, Arc Bolt, Jet Blast, Ground Surge and Calm | Wetness interferes with ignition; short-range setup exposes the player. |
| Bombardier | Drench Lob, Cold Snap, Calm and finite throwables | Cold Snap is a DV debuff; gases and splashes can harm others. |
| Classic | Existing six-spell kit; Strength18 | Its broad kit is not the required minimum for the region. |
| Acquired mobility | Vault can clear a two-cell displacement with valid landing | New tree + power ordinarily needs 2 SP; no archive-seal bypass or invisible landing. |
| Social skill | Existing recruitment can add an ally when legal | Not a universal permission check; its hostility/slot/chance/cooldown limits remain. |

## 8. Time, work and saved state

### One explicit world-time policy

**Decision:** use the existing saved `TurnManager.TickCount`/`WorldClock.CurrentTick` as the only elapsed-time source. One authored crop growth/moisture unit equals **10 world ticks**. Existing stage lengths and moisture quantities retain their numeric units. This intentionally changes timing under haste/slowness and adds development during absence; it is not described as perfectly preserving old per-action cadence.

At ordinary roughly-ten-tick actions, a 20-unit stage takes roughly 20 actions. A rest's actual +60 ticks contributes six units, plus whatever time its ordinary action really advances. A world-map move's +10 contributes one, plus actual scheduler time. Never manually credit rest/travel again. Waiting at real time with no gameplay action, menus, saving, loading and merely viewing a zone contribute zero.

Each eligible crop stores a last-reconciled world tick, fractional progress in `[0,9]`, and a timing version. Reconcile before watering, harvesting, relocating or transferring a crop; after the active player scheduler reaches the next input boundary; and on a real arrival after graph and clock restoration, before presenting actionable state/autosave. Use the same idempotent operation at all seams. `GetZone`, metadata queries and partial save hydration must not grow crops. Source review must include `GameBootstrap`'s fresh/load scheduler passes as well as `InputHandler`.

For valid nonnegative elapsed time, consume only water that actually existed during that interval. The remainder means elapsed **wet** ticks toward the next unit: preserve it across stage changes and watering while wet; clear it when moisture expires. Dry time is discarded, not banked, and still advances the timestamp. Nine wet ticks, watering and one more tick produce exactly one unit; ten wet ticks followed by nine dry ticks and watering do not earn another unit after one tick. Water remains max/top-up, not additive. Standing ripe crops remain until Harvest, and legacy automatic-drop crops use their existing yield transaction once. Never retroactively apply rain/weather from an unobserved area. Reconcile first, then apply newly supplied water. A failed yield publication leaves the crop recoverable under the existing moisture/hold policy; no loop over years of missed factory retries.

New plants start at the current tick. A legacy loaded crop without a stamp is initialized at the restored tick with its exact existing stage/progress/moisture, no retroactive growth. Reject malformed future stamps with a diagnostic and no advancement; do not use wall-clock time or an old static cache to repair state. Define overflow handling with checked/widened arithmetic. Save/load must carry the timestamp, fraction and state together.

Implementation must prove a large elapsed interval equals stepping the same valid time locally, including the final wet unit, stage boundary, ripe state and depleted owner. Updates must be bounded by crop states/transitions, not a loop proportional to the absent duration.

### One pending kitchen batch

Proposed states: `Idle → Working → Ready → Idle`; plus `Blocked`/`Cancelled` where appropriate. Proposed fields belong to the exact saved station Part: recipe version, worker/station IDs, job ordinal, input snapshot/escrow, start tick, due tick, progress/blocked status, output owner ID and collection state. These are proposed fields, not current APIs.

- Start requires the exact live station, repaired pan, living nonhostile nearby worker, valid carried inputs/currency, output availability and an ordinary player action. Transfer inputs into physical dedicated escrow and fee to the cook atomically; set due tick once. Failed/rolled-back starts restore all state.
- While absent, no NPC movement, harvest, combat or weather is simulated. Reconciliation derives only this commissioned work from actual elapsed time and saved inputs.
- Only one commission can be outstanding. A full pickup container or missing output definition blocks completion without losing inputs or granting another batch. Retry validates actual current owners; it does not replay elapsed time as more jobs.
- Loss/alteration of a committed escrow input cancels the job, releases only surviving actual inputs and retains the paid fee. Commit resolution before exposing salvage; never recreate a stolen/destroyed unit or also publish a meal. This is separate from a temporarily unavailable output factory/capacity, which keeps the intact job blocked.
- Completion consumes escrow and publishes exactly one real meal into the dedicated pickup container in one transaction. Record output identity before any subsequent read can reenter. Repeated entry, load, restock and menu refresh cannot publish again.
- Death/destruction handling reconciles up to the event's tick while pre-event worker/station state is still known. If work was already due and valid, finish once; otherwise cancel unfinished work and release its unchanged escrow at the surviving station or event cell. No dead worker continues production. The initial service fee is not refunded after a paid commission is interrupted; communicate this before starting. Inputs are never duplicated as both salvage and an output.
- This requires a narrow NEW pre-invalidation integration seam: current `CombatSystem.HandleDeath` spills inventory before `Died`; a normal Died subscriber is too late for this policy. Reconcile the exact associated job before death loot/spillage and before supported station removal, without changing unrelated death behavior. Test due-at-death and immediately-before-due separately.
- Theft/destruction of an already finished meal is a real loss; it does not regenerate. Record that the job has resolved so the service does not remain permanently stuck on a nonexistent item. No automatic receipt reissue or respawn can recreate input or output.

The new pan is not made randomly damageable to manufacture a maintenance loop. Destruction behavior still needs an explicit policy and test for any supported destructive path. Existing NPC death can end that local service; basic cooking elsewhere stays available.

### State authority and retention

| State | Authority | Read/advance seam |
|---|---|---|
| Crop timing | Actual CropPart on the retained entity | Reconcile at valid mutation/arrival/player-boundary seams |
| Repair | Existing exact RepairablePart + new pan recipe | Successful existing repair transaction |
| Kitchen job | Exact station Part + actual escrow/pickup owners | Explicit command, elapsed-time reconciliation, death/destruction |
| Claim/permission | Saved local claim owner with bed/crop/container/worker bindings | Successful relevant action and witnessed local knowledge |
| Work testimony | Saved player knowledge bound to the observed repair, issuer and world | Cook acknowledges locally; keeper learns when explicitly told |
| Discovery awards | Saved player/world-qualified finite accomplishment ledger | First committed eligible accomplishment |
| Region variation | Frozen versioned new-world plan | Allocation once; realized placement recorded separately |

Rebind public Part fields and object references through existing SaveSystem patterns. Do not rebuild saved Parts from changed blueprints. Retain exact accepted graphs through the existing manager policy; destroying the last prop must not reset the address. Existing exploration retention already covers its accepted sites. **The absence guarantee covers actual retained cultivation graphs:** authored crop patches, cultivation/resident sites and these new cellar beds. Legacy seeds planted on arbitrary unretained Plantable terrain keep their current eviction limitation; do not promise persistence there or silently retain the entire world. Timing reconciliation can still operate while such a graph exists. Extend only the cellar/Curation/new satellite boundaries actually needed, and account for retained graph count/save size. Metadata must remain noninteractive and must not become a targetable pile.

## 9. Local access and knowledge

Use a small opt-in claim component for this reserve; do not redefine `SpreadTerritoryPart` as universal ownership. Claims bind exact bed/tray owners, a responsible keeper and the current world. Store `Unknown/Granted/Suspended` per actual player, with a bounded last-breach reason. Ordinary reputation pricing and personal hostility remain their existing authorities.

Permission may be bought or earned. It is not a transferable master key, faction-wide rank or guaranteed safety. A suspended permission is not restored by repeating the original introduction. Initial restitution proposal: provide two Emberwheat through a specific once-per-current-breach reconciliation action; no XP or reputation reward and no duplicated goods. This is a finite way to repair a local relationship, not an experience loop. Repeated assault follows ordinary hostility and is not erased by grain.

Witness requirements: worker alive and grounded in the same current zone; actual supported sight and distance to both actor and claimed source; no occluding terrain; action genuinely committed; exact claim still current. A Calm effect does not grant invisibility or permission. Do not infer witnessing solely because the player can see the NPC. Store the observed event once; failure or rollback must not leave a breach.

For this milestone a witnessed unauthorized harvest/take suspends only this reserve's permission; it does not automatically aggro every villager or change normal trade, the kitchen, Curation or public services. State the consequence plainly. Ongoing attacks remain normal combat. An unseen harvest can deplete the resource without assigning an unknown culprit. Dialogue may acknowledge missing produce without accusing the player.

Do not add a generic “social solution succeeds” roll. Existing reputation, recruitment and Calm keep their real rules. The new specific permission paths are explicit offers with actual payment/proof. Conversation and action menus must agree about availability.

## 10. Progression and seed variation

### Progression contract

Budget a small finite set of noncombat accomplishments against the live thresholds: level 2 requires 115 XP; level 3 requires an additional 220. Each level gives one SP, +2 maximum HP, a living-character heal and one MP. Do not describe a level-up as an innocuous number; the heal can affect encounter tactics. [S08]

Initial milestone budget: **120 XP total**, from four distinct 30-XP accomplishments. The ledger uses four stable IDs, independent of seed variant and approach. A combat approach receives the same accomplishment award plus existing kill XP; a bypass receives no fake kill award.

| Stable ID | Exact committed trigger | Counter / limit |
|---|---|---|
| `cellar-stores` | Player custody of both original authored cellar FireClay units through successful physical transfers; count each source unit once, including split/merged stacks | Opening the cache, unrelated clay, resale/reacquisition and repeated transfer do not count |
| `service-restored` | First successful player repair of either this glade well or this kitchen pan | Both alternatives share one award; second repair, finding an already-repaired object and failed/rolled-back action do not count |
| `dry-bed-recovered` | First successful player harvest of the exact authored initially dry cellar Sootroot sprout after legitimate water and real growth to ripe | Initial ripe neighbor, replacement crops, watering alone, ordinary harvesting and elapsed time alone do not count; legitimate assistance is allowed without inventing watering-actor attribution |
| `kitchen-working` | First successful transfer to the commissioning player of a completed meal from the actual paid kitchen job | Starting/observing a job, later meals, bought meals and drop/reacquire do not count |

These are finite accomplishments, not four mandatory quests. Curation, reserve permission, optional satellite, fighting and reading are not award requirements. Tending locally earns the same outcome as travelling; departure/return is a playtest criterion, not an arbitrary XP condition. World-qualified receipt and award state must commit with the triggering outcome; no award on inspection or speculative UI state.

The target is one attainable first level through the connected activities without required kills. It does not guarantee level 3 or both points for a new mobility tree. Physical treatments, supplies, relationships and books provide immediate capability before a level. Do not award experience for every harvest, batch, repair, payment or return.

Author **Ditchkeeper's Footwork** (`DitchkeepersFootwork`), a new mundane manual inheriting Item with `Grimoire.SkillClassName = Acrobatics_Vault`, its own learn/already-known prose, and no GrimoireCharge or RiteGrimoire inheritance. Current `GrimoirePart.DoRead` already supports this direct teaching. It teaches exactly Vault, grants neither the root nor SP, remains physical after reading, and cannot grant the skill twice. Put one finite copy in either satellite variant behind a usable ordinary bypass, never requiring Vault to reach it or leave. Award no XP for reading. Preserve existing cellar book/buckler rewards and purchase gates elsewhere.

### Variation that affects choices

Freeze a versioned overlay at new-world creation. Preserve prior exploration family allocation and protected addresses; layer at most one satellite on a safely eligible quiet location. Never reroll because a player reloads, destroys a source or visits in a different order. Never use shared population RNG for the new assignments.

Vary a few independent, legible factors rather than every prop:

| Dimension | Initial alternatives | Invariant |
|---|---|---|
| `ReservePriority` | Clay-ready: ripe Marlroot/dry Pitchpod sprout; resin-ready: ripe Pitchpod/dry Marlroot sprout | Same two sources and both legal access paths; use timing differs, public beds unchanged |
| Supplemental find | Clay/repair supply emphasis or treatment/preparation emphasis | Existing first cellar clay remains; finite physical packet, no duplicate rewards |
| Satellite | Wet crossing or heavy frame | No mandatory travel lock; no population stacking; quiet space remains |
| Exposure | Short wet lane vs short dry exposed lane; real cover orientation | Safe initial retreat and an alternate physical approach |
| Supplemental packet | Wet crossing: 2 grain + 1 pulp; heavy frame: 2 FireClay | Same single movement manual in either; stock does not reroll or scale to the character |

Do not vary Curation theology, established characters' identity, global allegiances or NPC hostility arbitrarily. Different local owners/interests can become a later expansion when multiple fully authored factions exist here; the first version varies access circumstances and geometry without inventing a faction swap.

Realization failures are recorded and remain refused, not silently replaced with free sources. A world in which a required chain cannot be realized is a failed milestone acceptance seed, not a successful design with missing content. Resolve placement within bounded authored candidates before shipping; do not conceal failure by cherry-picking screenshots. Historical directions remain explicitly unconfirmed unless the speaker has actual local knowledge.

## 11. Readable models, animation and text

| Element | Reuse | Required change / visible state |
|---|---|---|
| Crop sources | Existing Sootroot, Marlroot, Pitchpod and Claspbean stage/seed/yield models | Current wet/dry/stage owners render in glade cellar and reserve; no 2D fallback |
| Batch pan | Existing kitchen palette and masonry vocabulary | Cracked/open; restored empty; covered working; parcel-ready forms |
| Pantry/pickup | Existing container renderer and inventory | Distinct shape/opening; actual contents govern available pickup, no decorative fake meal |
| Reserve | Existing furrows and crop models | Sparse tied stakes; an open public approach; claim text distinguishable from free beds |
| Field meal | Existing portable item pipeline | One original wrapped parcel, small readable contrast at gameplay zoom |
| Ink desk | Existing Curation desk/stamp/ink forms where accurate | Mundane preparation arrangement; actual one-action output; preserve counterfoil/cabinet shapes |
| Heavy frame | Existing hauling geometry/material conventions | Visibly heavy original frame with readable clearance and release space |
| People | Current seed keeper/cook/Doreth/Ivrin rigs | Existing idle/talk/work clips; brief local finishing gesture only if tied to actual completion |

Do not animate continuous productive work when no input/job exists. Completion while absent is shown through state on return, not an imaginary offscreen animation claim. New animation is required only if existing clips cannot communicate a new physical action; a new rig is unnecessary.

Examine distinguishes source, use, ownership and current condition. Proposed lines:

- Pan: “The lining has split along the rim. Two handfuls of fire clay would hold it. The oven beside it still works.”
- Keeper: “The open beds are for anyone. Those two with the ties are kept for seed. Ask before you pull them.”
- Cook, before commission: “Two sheaves and a squeeze of claspbean. Less supper, but you can eat and stop a cut with one hand. Two drams for the wrapping and the work.”
- Ivrin: “Root-black and resin. Suitable for labels. The label identifies the preserved subject; it does not preserve the subject.”
- Suspended permission: “I saw you pull from the tied row after I asked you to leave it. The open beds are still open.”

These are original draft lines, to be reviewed against the voice cards before content publication. Avoid design vocabulary such as “resource chain,” “permission state,” “production batch slot” or “quest-free” in world prose. Exact costs and refusal reasons belong in ordinary interaction menus, where they support a decision.

No universal action icons, exclamation marks or live remote stock markers. Existing optional field notes can remember directions; a remembered place must not assert that its worker remains alive or its supplies remain available. Important visual changes must survive fog, ordinary camera distance and a player standing beside the object.

## 12. Representative expeditions

These are proposed playtest journeys, not reports of completed play. Each begins with an actual selected build and normal inventory. No grants, teleportation or enemy suppression count as ordinary-route evidence.

**A — Duelist, preparation first.** Notice the well and learn the kitchen's alternative use for clay. Reach the cellar through cover or a one-target fight, retrieve its actual clay and Sootroot, and decide whether to retain the roots as emergency treatments. Restore the batch pan, hear Orven's introduction, and relay it to Nella. Harvest with permission, plant a returned seed, and water using the actual farming access kit. Start a field meal with earned ingredients; visit a nearby worksite while it prepares. Return to the parcel and changed crop stage/moisture. Use the meal during an actual later bleeding encounter. No Calm or purchased Vault is granted to this character.

**B — Stormcaller, sustained rites.** Recover the cellar's charged rite or buy Ivrin's actual finite copy, read it and use a limited charge. Optionally gather/brew FrostLichen and throw the cold tonic to improve its payoff; priming is not a hard casting gate. Take resin and Sootroot to the public clerical desk. Preserve the option to use either ingredient directly instead. Prepare one vial, then choose Re-ink on the exact carried book. Return to replanted Sootroot and a still-finite source. Test the new charges through an actual supported rite and observe which carried book supplies the charge. A witnessed reserve breach leaves a different relationship on this return, even if Calm avoided combat earlier.

**C — Breaker, access through strength.** Find the heavy-frame variant, inspect its load and clear the short path with actual hauling. Retrieve a finite useful packet; leave the long route available. Spend clay on the well first, keep the free oven/cot for now, and use new gathered materials for the pan later if desired. Saved frame position changes subsequent travel; moving it again does not award more XP. Compare with a Strength16 build that must use the detour.

**D — Bombardier, selective expenditure.** At the wet-crossing variant, choose whether a finite grenade/tonic is worth spending for a useful source. Avoid harming a nearby claim owner. Keep raw treatment supplies rather than committing everything to production. Withdraw when the setup is unfavorable; later return with a different earned preparation. Cold Snap's actual DV effect and friendly-fire risks are visible in the record.

At least one acceptance journey declines Curation help, one leaves a repair unfinished, and one uses a noncombat source approach. Refusal or bypass is a legitimate outcome; no closure-ledger or cosmic penalty is attached to merely ignoring these ordinary optional services.

## 13. Implementation sequence and files

All class/record names marked **proposed** are design vocabulary, not promises of existing APIs. Reuse existing structures where they satisfy the contract. Read cited production members again at implementation time; record drift before code.

| Batch | Playable result | Main existing surfaces / proposed addition | Acceptance before next integration |
|---|---|---|---|
| D0 — freeze baseline | Reproducible ordinary builds/routes and content inventory | StartingBuildService; native launchers; current recipes, prices, source manifests | Current four builds + Classic, exact source availability, meaningful reward/balance assumptions recorded. No code change required merely to collect baseline. |
| D1 — elapsed cultivation | Plant, leave, do something else, return to correct growth | CropPart/CropSystem/CropSystemPart; SeedPart; InputHandler; bootstrap/load; proposed timestamp reconciler | Dry/wet/stages/legacy yield equivalence, speed/rest/travel policy, real planted departure-return-save route. |
| D2 — ink completes a loop | Gather, prepare ink, refill a carried book, cast again | Botanical processing pattern; GrimoireChargePart; InkVialPart; transaction/UI dispatch; Curation source/renderer | Both vial uses, capped refill, cancellation/full-book refusal; real ink preparation and paid re-ink; no rental regression. |
| D3 — kitchen and material choice | Clay restores a supplemental service; a commissioned meal finishes during travel | Tier1Repairs/RepairablePart; resident builder; Food/Cure/InventoryTransaction; proposed KitchenBatchPart + composite Eat | Free oven/cot preserved; actual escrow/fee/output; interruption/save/return; exact combined meal effect. |
| D4 — earned local access | Payment or observed repair earns reserve use; witnessed breach changes return | Resident conversation/UI; proposed LocalGatheringClaimPart; harvest/take commit hooks; saved spoken introduction | Exact beds/owners, visibility, rollback, proof learning, restitution, public-ground and hostile counters. |
| D5 — encounters and progression | Actual builds take different approaches and gain finite exploration progress | Existing cellar/composition/AI/hauling; optional satellite overlay; proposed accomplishment ledger | Six designs accounted for, four observed; bypasses; unique awards; utility across selected builds. |
| D6 — whole-region acceptance | Both chains work together in ordinary play | Native new-game/actual-input/save tools; source art pipeline; living doc | Multi-location departure-return chains, meaningful variation, visible models, performance and cold-eye review. |

D1 and D2 can be implemented in separate owned files after their contracts settle. D3/D4 share station/testimony/authority and must agree before either content builder finalizes. D5 reuses those completed semantics. One coordinator owns shared blueprint edits, generation integration, Unity imports/tests and final Git integration. Art can proceed against frozen shape/state requirements without inventing runtime behavior.

Each production batch follows `CLAUDE.md`: failing invariant test before code, positive/counter checks, relevant regressions, dedicated adversarial coverage for multi-surface features, review, and living-document update in the same commit. Preserve actual failing evidence; do not label assertions that already pass as RED fixes. Use §2.3 commit format. Edit Objects.json surgically and prove parsed changes are limited to intended rows; give new Assets C# files fresh hand-written metas. Future implementation retains the user's prior authorization to fetch/rebase and push completed work to main.

## 14. Verification, play criteria and performance

### Correctness matrix

| Positive behavior | Required counter / adversarial cases |
|---|---|
| Elapsed wet crop growth | Dry time, new water after absence, NPC-only event, menu/load without time, speed difference, partial unit, exact last wet unit, unknown timestamp, moved/replaced owner, failed yield, very large delta |
| Job completes once | Missing input, partial stack, invalid worker, interrupted start, due-boundary death, station destruction, output capacity, repeated entry, pending/ready save, removed finished output, outer rollback |
| Re-ink consumes exactly one vial | Full/invalid book, stale target, insufficient/aliased input, rental use of same vial, malformed cap/overflow, cancellation, failed outer transaction, restored book identity |
| Meal heals and cures once | No bleed, unrelated poison, repeat use, one-item stack, dead actor, rejected use, HP cap, outer rollback, duplicate item identity |
| Permission changes access | Other plot, public bed, forged/foreign testimony, suspended grant replay, unseen/wall-obscured observer, Calm witness, cancelled harvest, yield pickup after already-recorded harvest, load, ordinary hostile assault |
| Finite exploration XP | Same outcome by different approach, seed-variant ID, repeated conversation, repeated repair/revisit, save/load, rollback and level-up heal timing |
| Safe source generation | Protected/cached/old manifest, bad content, contested reservations, no route/landing, source callback mutation, failed optional placement, depleted graph unload |

Diagnostics use existing categories where appropriate and include accepted/rejected reason, exact actor/source/service ID, before/after quantities, elapsed ticks, recipe/version, site/variant and job/accomplishment identity. Debug receipts must not leak unseen remote state into player prose. Author structured success/refusal records at decision seams, not a world scan each frame.

### Native play evidence

Use actual selected starting builds and normal inputs. Start with native seeds 64, 1729 and 729490642, then add two seeds selected before viewing outcomes. Exercise all four specialist builds across the corpus, with at least one complete chain-combination journey on a melee build and one on a spell/throwable build. Classic is a regression case, not the sole acceptance character.

Record spawn, route, actual committed sites, inputs found, decisions, consumed resources, earned progression, departures and returns. At least one journey uses real F5/F6 after a pending job and after a permission consequence. Never present a teleport-assisted mechanism test as ordinary travel. Generated previews with reveal are composition evidence only. Existing native acceptance results are baseline evidence, not new results for this plan.

The player-facing acceptance questions are:

1. Did a find change the next destination or preparation? Show the actual decision and resulting use.
2. Did a build supply a useful alternative? Compare cost/outcome, not just success on two paths.
3. Did cooperation or harm alter a later interaction? Observe that local state on return.
4. Did departure and return produce useful development? Show crop/time/work accounting and its visible result.
5. Did different seeds change at least two decisions while preserving understandable routes?

For an approximately 45–75 minute unhurried session, target at least six meaningful decisions across four locations, two departure-return consequences, one forgone useful expenditure, and one successful noncombat approach. These are design targets, not telemetry to game. Count a decision only when the alternatives have different practical costs or consequences. Repeated inventory handling and routine movement do not qualify.

Use a fresh-eye player when available; until then explicitly report that agent-guided paths cannot establish unfamiliar-player comprehension or fun. Do not require the user to stop ongoing work to provide a playtest. Revise the design when a legal alternative is consistently pointless, a product is never worth its cost, or preparation adds chores without changing an encounter.

### Performance

Follow [PERF-FOUNDATION.md](PERF-FOUNDATION.md): scratch lists for active turn processing, content fingerprints for UI, cheap validated caches, and per-cell dirty hooks for visible changes. No new per-frame world scan, recursive remote generation, unbounded job queue or elapsed-time-sized loop. Crop reconciliation visits actual relevant crop owners at bounded seams; source/claim lookup uses local IDs. A region manifest has bounded records; jobs/claims have bounded state.

Profile current and changed builds under comparable 60–90 second movement/combat captures. Report maximum/p99 frame and relevant turn/arrival costs, not only averages or unit-test timing. Include retained graph count, save size and long-absence arrival behavior. Treat regression as a design issue before attempting speculative optimization. New meshes use existing palettes/material grouping and owner-based admission; verify glade, normal Spread and managed cellar separately.

### Honesty bounds

Tests can establish conservation, authority, timing and save behavior. Actual-input native journeys can establish those particular routes and rendered states. They cannot establish universal seed coverage, long-term economy, first-player discovery, visual preference or campaign replayability. The headless runner cannot prove Unity rendering/input/scene integration. Record selected versus full suite scope and environment failures explicitly.

## 15. Compatibility, risks and deferrals

**Existing saves:** initialize timing stamps on actual restored crops without retroactive growth. Preserve saved repair states, crop owners, depleted stock and current permissions (absent new claim means no claim). Do not stamp pans, reserves, plants, desks or satellites into cached chunks. Freeze prior manifest versions literally. New exploration may only receive the additions if an explicit versioned opt-in is designed and tested; baseline decision is that the complete connected district is a new-world feature. Tell players this plainly at delivery. Re-ink can apply to existing valid carried charged books because it is an action on saved Parts, not a regenerated item.

**Balance risks:** existing free cot/rest and cheap food can reduce demand for a prepared meal; judge its action compression in actual encounters. Ink may already be sufficiently available through trade; preparation should remain an alternative, not become the cheapest compulsory routine. Root treatments matter only if fire threats are encountered. The first two clay units must present a visible choice without locking both services. Time policies affect haste and sleep, and deserve explicit comparison before release.

**Implementation risks:** adding owners after a builder has frozen its expected owner set can invalidate an otherwise correct site. Integrate additions inside scoped staging/final validation, not as an unguarded late append. UI world actions may lack the inventory transaction used by direct service tests; exercise actual menus. Parts loaded from saves do not inherit new blueprint parts. Current melee targeting can choose a different adjacent creature than a presentation might suggest; design clear frontages and log this existing limitation rather than promising precision.

**Lore risks:** ordinary optional refusals are not automatically cosmic abandonment. The milestone does not add closure-ledger/Urqu effects. Mainline Curation is not a captive-sacrifice faction; the desk is mundane; ordinary preserved bodies are not conscious. No copied enemies, mystery closure or new faction theology is needed.

**Deferred:** full material-repair Tier 2+, tool/workmanship tiers, universal composition repairs, global crime/ownership, offscreen combat/weather/animal breeding, merchant economy replacement, production queues, NPC travel between areas, all-biome expansion, a universal fire scale, dog fetching and unrelated minor animation fixes. Keep BitLocker disabled in ordinary play. A significant defect blocking these chains is in scope; an unrelated low-impact defect is recorded and deferred.

## 16. Source index

Paths and named members are the implementation-sweep entry points. Recheck them against the implementation base; do not treat old document line numbers as current API contracts.

| ID | Source | What it establishes |
|---|---|---|
| S01 | [Delivered district](EXPLORATION-DEPTH-DISTRICT.md), `FreshGameStart`, `GleanersDistrict` | Actual spawn, cellar clay/reward, restored well, retention and bounded prior evidence |
| S02 | [Residents](SPREAD-FIELD-RESIDENTS.md), [SpreadExplorationResidents](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs) | Free public beds/oven/cot, stock, near-start coordinates, safe generation |
| S03 | [Biome crops](BIOME-CROPS.md), [Objects.json](../Assets/Resources/Content/Blueprints/Objects.json), `BotanicalProcessingPart` | Actual crop yields/utilities, materials, food values and processing |
| S04 | [Repair roadmap](MATERIAL-REPAIR-ROADMAP.md), [Tier 1](REPAIR-TIER-1-AND-CROPS.md), [recipes](../Assets/Resources/Content/Data/Repairs/Tier1Repairs.json), `RepairablePart` | Explicit fault/composition/payment; no universal damage repair |
| S05 | [Specialist sites](SPREAD-CONTENT-EXPANSION.md), `WeaponTemperingService`, `AlchemyStillPart`, [CookingService](../Assets/Scripts/Gameplay/Items/CookingService.cs), [FoodPart](../Assets/Scripts/Gameplay/Items/FoodPart.cs) | Existing preparation and food effects; free field alternatives and actual item consumption |
| S06 | [RegionalSituations](../Assets/Scripts/Gameplay/World/RegionalSituations.cs) | Existing five finite regional exchanges, source identity and outcomes |
| S07 | [StartingBuilds.json](../Assets/Resources/Content/Data/Builds/StartingBuilds.json), `StartingBuildService.Apply/ApplyClassic`, `StartingSpellKit` | Actual equipment, skills, attributes and Classic distinction |
| S08 | [LevelingSystem](../Assets/Scripts/Gameplay/Stats/LevelingSystem.cs), `SkillPurchaseEligibility.Evaluate`, `ConversationActions` AwardXP | Thresholds/rewards; root costs; dialogue XP has no inherent once-only guard |
| S09 | `Spellcraft_Calm`, `Cryomancy_ColdSnap`, `HobbledEffect`, `Hydromancy_JetBlast`, `Hydromancy_DrenchLob`, `Pyromancy_Kindle`, `Galvanism_ArcBolt/GroundSurge` | Real control/wet/fire/targeting limits and costs |
| S10 | `AIHelpers.HasLineOfSight`, `DragRules.CanDrag/MaxDragWeight`, `DragSystem.PenaltyFor`, `Acrobatics_Vault` | True cover, strength thresholds, hauling cost and landing rules |
| S11 | [InkVialPart](../Assets/Scripts/Gameplay/Items/InkVialPart.cs), [GrimoireChargePart](../Assets/Scripts/Gameplay/Magic/GrimoireChargePart.cs) | Current rental-only vial use and unused refill helper |
| S12 | [CropSystem](../Assets/Scripts/Gameplay/Farming/CropSystem.cs), [CropSystemPart](../Assets/Scripts/Gameplay/Farming/CropSystemPart.cs), `CropPart`, `CropYieldService`, [WorldClock](../Assets/Scripts/Gameplay/Turns/WorldClock.cs), `TurnManager`, `RestSystem`, `WorldMapTravelCostPart` | Current local timing, conserved moisture/yield and world-clock contributions |
| S13 | [Curation receiving yard](CURATION-RECEIVING-YARD.md), `CurationIntakePart`, `CurationReceivingBuilder` | Exact local certification, actors, finite cabinet, quarantine and art |
| S14 | [SpreadExplorationPlan](../Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs), `SpreadExplorationBuilder`, [OverworldZoneManager](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs), `BiomeCropPlacement` | Versioned assignment, source receipts, final graph authority and unload policy |
| S15 | [Lore authority](../Lore/README.md), [Bible](../Lore/10_Bible.md), [Second Spine](../Lore/11_SecondSpine.md), [Pale Curation](../Lore/Factions/03_PaleCuration.md), [voice cards](../Lore/Voices/VOICE-CARDS.md) | Current canon and procedural/domestic voices; obsolete Docs/Lore excluded |
| S16 | [SpreadTerritoryPart](../Assets/Scripts/Gameplay/AI/SpreadTerritoryPart.cs), `PlayerReputation`, `FactionManager`, `GroveLaw` | Existing hostile-ground warning and separate faction/legal authorities |
| S17 | [TraderRestockSystem](../Assets/Scripts/Gameplay/Economy/TraderRestockSystem.cs), `TradeSystem.GetBuyPrice/GetSellPrice`, `TraderPart` | Timed random shop stock, prices and service/commission separation |
| S18 | `InputHandler.EndTurnAndProcess`, `InputHandler.HandleZoneTransition`, `GameBootstrap`, [SaveSystem](../Assets/Scripts/Gameplay/Save/SaveSystem.cs), [InventoryTransaction](../Assets/Scripts/Gameplay/Inventory/InventoryTransaction.cs) | Post-scheduler timing, fully hydrated load, actual menu transactions and saved graphs |
| S19 | [Exploration plan](QUEST-FREE-EXPLORATION-PLAN.md), [performance guidance](PERF-FOUNDATION.md), [CLAUDE.md](../CLAUDE.md) | Existing family budgets, profiling, TDD/counterchecks/review and delivery requirements |
| S20 | `SpreadPresentationScope`, `BiomeCropRecipes`, `CurationYard3DLibrary`, `SpreadVoxelLibrary`, existing ArtSource/importers | Current-owner model routing and source-asset preservation |

## 17. Design review and execution ledger

The initial review recorded design work only; no production code, content JSON, art, scene or save changed during that phase, and no gameplay tests were run for it. The user then authorized autonomous implementation. The operative prompt is [SPREAD-CONNECTED-EXPLORATION-IMPLEMENTATION-PROMPT.md](SPREAD-CONNECTED-EXPLORATION-IMPLEMENTATION-PROMPT.md). Subsequent checkpoints below must distinguish newly implemented behavior from this original design.

| Review | Finding / treatment |
|---|---|
| 🟡 corrected — duplicated foundations | Existing regional supply exchanges, public services and crop roster are preserved; new work adds aftermath and competing use. |
| 🟡 corrected — unavailable capability | Re-ink is NEW work; Cold Snap does not slow; Calm is not permission; melee builds have no starting Calm. |
| 🟡 corrected — time semantics | Explicit sole-world-clock redesign replaces an inaccurate claim of unchanged per-action farming. Legacy timestamp initialization is specified. |
| 🟡 corrected — knowledge | Repair is learned through an observed local acknowledgment, not instantaneous remote faction knowledge. |
| 🧪 pending — balance and comprehension | Meal value, ink demand, growth pacing, access price and ordinary discovery require actual play. Numbers in this document are provisional. |
| 🟡 corrected — reusable source | New cellar/reserve/kitchen crops require actual Plantable + CultivatedSoil beds and real replant acceptance. |
| 🟡 corrected — practical ink use | One finite ordinary purchasable rite copy at Ivrin covers odd seeds; priming improves Shattered Rime but is not a hard casting gate. |
| 🟡 corrected — timing/interruption | Fractional wet-time semantics, escrow loss, pre-death settlement and retained-cultivation scope are explicit. |
| ⚪ deliberate — scope | The design is complete; subsequent implementation was authorized. No claims yet of shipped additions, passing feature tests or campaign-depth completion. |

Implementation updates must record each completed batch, source-sweep corrections, RED/GREEN and counter evidence, significant findings/fixes, changed files, native observations and remaining limits. Do not mark the full milestone complete until D6 evaluates both connected chains.

### Implementation checkpoint — 2026-10-02, D1–D3 foundations in progress

- Saved and executed the implementation directive linked above. Parallel file ownership separates crop timing, ink, and kitchen state; the coordinator owns world sources, shared input, native tests and Git.
- Observed native RED before production: the first 86-case run exercised missing timing/ink/kitchen behavior; the second exposed the original `Refill(int.MaxValue)` overflow as well as missing actions. Native content RED ran 19 cases: 18 missing additions failed and the cached-graph counter passed. An earlier content attempt returned zero tests during compilation and is **not** evidence.
- D1 core native sweep: **174 completed, zero failures**, job `a1efbc6f23884fa49920f464414e65f8`. Saved [receipt](Verification/ConnectedSpread/Tests/native-green-cultivation.json). This proves the selected core cases and existing farming regressions; shared post-scheduler/arrival/load wiring and live travel evidence are still pending at this checkpoint.
- D2 and D3 runtime Parts and their counter-tests exist. Initial mixed native runs caught ingredient substitution during output initialization and kitchen cancellation mutating the collection being enumerated. Both are being corrected before final verification. Reference-run evidence remains separate from native evidence.
- Added seven content blueprints by appending individual rows, plus the explicit two-fire-clay pan repair. All pre-existing parsed blueprints remained identical in the [initial parsed diff proof](Verification/ConnectedSpread/Tests/blueprint-diff.json). Generation is being extended inside its original staged receipts; the first integration sweep still rejected some expanded resident footprints and the Curation source.
- Source correction: `CultivatedSoil` is a Part on the actual terrain owner, not a tag or separate floor model. New beds use that existing authority. `RepairablePart.BlocksFunction` is a static owner query. The original public resident helper's seven-argument reflection API is preserved through a separately named connected overload.
- The Unity editor entered a persistent compiling state with no active compiler after repeated imports. The coordinator restarted the idle editor under the user's prior authorization. No game session or scene edit was discarded. This is a tooling interruption, not a passing gameplay result.

Nothing in this checkpoint marks the full milestone complete. The D4 social consequences, D5 progression/optional encounter, new presentation, shared runtime wiring and D6 ordinary multi-area play remain required.

### Integration checkpoint — native services, custody and timing

- The 95-case native integration run `bf77495b6f524062990cdba0d2644779` completed with 15 explicit failures: nine expected missing-art assertions, three seedkeeper placement failures, two missing load reconciliation assertions, and one meal fixture setup failure. All 32 local-reserve cases and all 19 real inventory/death/destruction integration cases passed. [Receipt](Verification/ConnectedSpread/Tests/native-art-red-integration-green.json). This is partial evidence, not a passing whole run.
- After the load seam was implemented, `416e5cd80a49438a839a4b651a647cee` completed nine cases: all eight input/load cases passed; the new actual-world source-binding test failed as intended before source Parts were bound. [Receipt](Verification/ConnectedSpread/Tests/native-source-binding-red.json).
- Source corrections: the Curation's unique-document helper rejects ordinary stackable grimoires even at quantity one; the new finite book uses a separate single-copy check while existing controlled documents retain their stricter validation. Terrain tags and even harmless-ground predicates include decorative overlays, so preparing a bed must select the actual terrain-plan ground owner. Dry new crops start at stage zero; references to a dry sprout mean a planted, unwatered crop, not stage one.
- **Bounded allocation correction:** v10 only installed residents on otherwise quiet addresses. Seed 729490642 assigned older optional families at both promised service addresses, making the connected route absent. New v11 worlds reserve `11.8` and `12.11` for their corresponding residents; this replaces the optional family only at those two addresses. The base v9 nonquiet allocations outside those two addresses remain unchanged. The later domestic pass still enforces same-family spacing, so forcing these residents can suppress a neighboring optional resident and affect which quiet row receives the satellite. Protected addresses and all restored v2–v10 manifests remain unchanged. This is an explicit exception to the original blanket allocation-preservation sentence, justified by a usable, seed-independent starting route.
- World-qualified permissions and finite accomplishments now have a saved GUID separate from the deterministic seed. Two independent games using one seed cannot share authority. v11 saves require that identity; old manifests remain literal and acquire no identity or connected retrofits.
- Player crop descriptions are being corrected to match elapsed world time. The initial seven-blueprint diff proof predates these narrowly scoped wording edits; final parsed comparison must include them.

Remaining: finish actual resident bindings, optional satellite, art import and owner presentation; run connected native expeditions and saved-world checks; review and publish. No statement here claims gameplay balance or discovery quality has been established.

### Allocation review correction — required services and neighboring residents

Fresh v11 allocation also recalculates domestic adjacency around the two required service addresses. The preservation guarantee applies to ordinary v9 nonquiet families outside those two addresses, not to every v10 resident assignment in a newly created world. At seed 7, `12.8` was quiet in the ordinary v9 layer (quiet rank 19), then gained a v10 seedkeeper. Giving the required v11 seedkeeper priority at adjacent `11.8` suppresses that duplicate resident; the resulting quiet row can host the single optional heavy-frame satellite. This is a bounded consequence of preserving the no-adjacent-identical-residents rule. Saved v2–v10 manifests remain literal and are not recalculated.

The native affected regression run `5cb495be549246d4bead7cc966ebeb5f` exposed stale current-version pins and the road-only assertion at that exact seed/address. Current-world expectations were updated to 11, while persisted family IDs and literal old-version fixtures stayed unchanged. The exchange cohort now pins the seed-7 exception, its actual quiet predecessor, the adjacent required keeper and the one-satellite limit; it does not broadly exempt new families. Two direct manifest fixtures also retain the saved world GUID when round-tripping a current v11 wire. A subsequent native rerun is still required for these corrections.

### Integration checkpoint — connected services and native regression

- The combined native core/affected sweep completed **1,646 tests with zero failures** (`1aef1892c09e40289893146e5ea6b2eb`). [Receipt](Verification/ConnectedSpread/Tests/native-green-core-1646.json), [exact 77-class selection](Verification/ConnectedSpread/Tests/native-core-selection.json). It includes crop timing and legacy farming, ink, kitchen/death/destruction, reserve access and saved references, source generation, finite progression, actual input transactions, materials/repairs, inventory transfers/splits/stack identities and new art. This is a selected native sweep, not the entire assembly.
- Imported **14 original inert voxel forms**, using the established palette: cracked/empty/covered/ready pans, stocked/empty pantry and pickup, reserve tray, persistent corded soil, ink desk, field meal, footwork manual and heavy frame. All 11 native art cases passed after the intentionally reproduced incomplete-Ready-save crash was corrected. The bed's cord/stakes persist after harvesting; existing crop models supply growth/wetness presentation instead of a second moisture authority on soil. Source geometry checks pass 5/5. Native expedition screenshots remain pending.
- Independent cold review reproduced actorless ink-menu and missing saved worker/pickup/soil crashes. Seven native public-query cases established one positive counter and six failures before the narrow lookup guards; the subsequent run passed all seven. The new yield progression hook also initially dereferenced a null actor during legacy automatic maturity; that was corrected, then a separate bound/public crop test demonstrated missing reserve provenance. Automatically dropped produce now retains its actual bed claim until pickup, without blaming any actor for maturation.
- Generation finalization now rejects a callback that inserts permission into the existing saved list. Resident naming occurs before its initial integrity snapshot, matching the later idempotent naming pass. No permission or remembered introduction is inferred from a seed alone.
- **Additional bounded allocation correction:** native seed 60 gave the rare pair the required `12.11` kitchen address. A three-case test reproduced both missing allocation and missing actual pan, with a preserved saved-selection counter. New rare selection excludes the two connected service addresses before ranking; `Restore`, `IsEligible` and `Selects` keep literal older selections. This extends the explicit fresh-world allocation exception above without modifying saved worlds.
- The initial five-seed optional-satellite sample had no realizable wet source. It is not evidence that the wet encounter works in normal generation. The census was expanded, before inspecting those outcomes, to integer seeds 1–65 plus 1729, 729490642 and 9091; seed zero is excluded because the engine substitutes a time seed. Selection never rerolls or fabricates creatures to meet a test. Native expanded results remain pending at this checkpoint.
- Existing tests which hardcoded the current manifest version 10 are being updated after native RED to version 11; saved older wire formats and persisted enum IDs retain their original assertions. The old allocation assertions also need the documented two required sites and single formerly quiet satellite exception, not blanket exemptions.
- Final parsed content comparison confirms **eight appended blueprints**, **35 existing crop-examine timing sentences changed**, no removals and no other existing blueprint changes. [Proof](Verification/ConnectedSpread/Tests/blueprint-final-diff.json). The seedkeeper's old instruction to stay in the area has also been corrected to explain wet growth during travel/rest and manual versus automatic harvest.

The complete release still requires the ordinary-input expeditions, expanded generation check, final regression/review and publication. These checkpoints are evidence updates, not completion claims.

### Optional encounter checkpoint — native placement, retained sources and saved packets

The final selected native satellite suite passed **37/37** (`de20ee5133de48b4bb889bec796d6b8b`): 27 feature/counter cases, eight adversarial cases, the fixed 68-world census, and a diagnostic replay of nine previously refused native source pairs. The [full native census](Verification/ConnectedSpread/Tests/sixty-eight-seed-census.tsv) contains **one realized wet crossing, nine realized heavy-frame sites and 58 accepted no-site refusals**. The wet crossing realized at seed 29, `Overworld.13.12.0`. These are measured outcomes of the predeclared corpus, not a promise that every world contains both alternatives.

The earlier native census (`b50607afa39c4b5cae627b07dc8ff44831`) failed its wet-availability gate: zero wet sites and seven heavy sites. Diagnostics established that all nine actual two-Scrabbler source pairs were eligible. Two correctly lacked four free entries in their original sacks; seven stopped at geometric admission before route trials. The layout incorrectly demanded bare ground across every walking corridor as well as each new wall, pool, cache and guard position. A single-candidate fixture reproduced the defect with an untouched frost-lichen patch in the bypass: native `ef06d53b46c340aba9d38612ec96d774` failed exactly that positive case, while the clear control and six safety counters passed.

The correction distinguishes placement from passage. New wall, pool, cache and guard destinations still require strict empty ground. Existing corridors may contain untouched passable foliage or loose objects, but must remain dry, walkable, unreserved and outside interiors. Blocking objects, actual water hazards, unrelated creatures, reserved/interior cells, and foliage occupying a new wall destination still refuse the site. Original owners are retained; no vegetation is cleared, extra hostile created, source roll repeated, cache enlarged, allocation salt changed or saved manifest recalculated. The existing critical-route proof and genuine cover/sight checks remain required. `ConnectedSatelliteRefused` now records exact refusal stage, source eligibility/capacity and bounded footprint-search counts so ordinary no-site outcomes are distinguishable from implementation defects.

The census compares all original entity identities and stock counts on every world, and verifies original positions on refusal. Its first realized site of each variant also uses actual inventory commands to take and read the finite footwork manual, observes the learned Vault skill, and performs a full `SaveWriter`/`SaveReader` round trip. The heavy fixture additionally grabs the actual frame, observes the speed penalty, moves it two steps and releases it. Both restored graphs retain owner IDs/positions, tile layers, depleted stock, the carried manual, the learned skill and the frozen site disposition, then survive unload without refill. These are controlled core-command/save fixtures with a test player placed at the site; they are **not** native-input journeys from spawn.

The final reference counterpart passed 37/37 as well. Its same fixed corpus realizes five wet and six heavy sites, reflecting the runner's deliberately patched string hashing; its seed outcomes must not be presented as Unity outcomes. Native sampling supplies the release availability evidence above.

| Review question | Verified boundary |
|---|---|
| Q1 — authority and conservation | Exact current original population/container receipts are claimed once. Supplemental stock uses fresh owners without merging away existing IDs/counts. Actual cache capacity is enforced. Creation and publication callbacks are rechecked; rollback restores only unchanged owned changes. |
| Q2 — actual mechanics | Wet sites use existing liquid owners and opaque physical cover with two original ordinary Scrabblers. Heavy sites use ordinary hauling: weight 136 refuses Strength 16 and admits Strength 18/20; a physical outside route remains available. The finite manual uses existing skill learning. |
| Q3 — counters and hostile inputs | Missing/stale sources, wrong creature rolls, source mutation, callback mutation, duplicate IDs, lost authority, full original containers, exact late rollback including pool layers, unsafe corridors, occupied placements and depleted packet proofs are covered. Native RED was observed before the corridor correction. |
| Q4 — limits and player experience | Both alternatives are now demonstrated in actual native cold generation and saved graphs. Ordinary-input route completion, visual comprehension, balance, campaign-wide frequency and performance profiling remain separate acceptance work. This checkpoint does not complete the full milestone. |

The owned service/progression review also remains within the combined native 1,646-case sweep above: re-inking spends one actual vial on an exact carried charged book without changing rental ink behavior; mundane desk preparation conserves its two pulp, one resin and three drams; four world-qualified accomplishment awards total 120 XP and publish level-up effects only after the enclosing action commits. Original clay-unit provenance follows merge/split/transfer and cannot be earned again by reacquisition. The actorless automatic-growth lookup and missing saved desk-worker query defects were corrected after observed native failures. These are concrete state/transaction guarantees; they do not substitute for the pending ordinary-input connected journeys.

Owned implementation paths for this checkpoint: `ConnectedSpreadSatellite.cs`, `ConnectedSatelliteTests.cs`, `ConnectedSatelliteAdversarialTests.cs`, `ConnectedSatelliteCensusTests.cs`, and the earlier finite selection/restore changes in `SpreadExplorationPlan.cs`. The prior ink/progression changes and their inventory provenance seams remain covered by the integration ledger. No additional content or blueprint edits were required for the corridor correction.

### Final review checkpoint — destruction and existing-world regression

The existing world-generation, manifest and saved-world selection passed **232/232** native tests (`b6a7f9269fe746f189157484241bb374`), including the corrected current-v11 expectations and seed-60 required kitchen. The separate rare-encounter/lifecycle/pair-clue/Latchcoil selection passed **115/115** (`9d5b0f5ac5654f0fb3f4afb2ebc76743`). Literal older saved selections remain covered. Receipts: [world](Verification/ConnectedSpread/Tests/native-green-world-232.json), [rare encounters](Verification/ConnectedSpread/Tests/native-green-rare-115.json).

🟡 **Fixed: a due meal factory could reenter its owner's destruction before the original idempotency flag.** The new callback is after the veto but must also be after `DestructiblePart.Gone` is set. A native 16-case kitchen integration run reproduced duplicate `Destroyed` notifications in exactly three callback cases (pan, escrow, pickup); the three otherwise identical non-reentrant controls and ten existing cases passed. Moving the existing flag before the kitchen settlement hook preserves the veto and original due-at-invalidation settlement while guarding every subsequent callback. The combined native kitchen, destruction, structural-strike and registration regression then passed **113/113**, including all six new cases. [Observed RED](Verification/ConnectedSpread/Tests/native-red-destruction-reentry.json); [GREEN](Verification/ConnectedSpread/Tests/native-green-destruction-113.json). This correction does not broaden destruction to objects without structural parts or make the pan randomly breakable.

These selected sweeps overlap; their counts must not be added together as unique tests. The complete assembly was not run. Independent Q1–Q4 review also checked physical escrow/output custody, exact crop/yield claims, inventory provenance and post-commit progression, public-query behavior and the service input/time dispatch. Full native player journeys are still required below before release reporting.

### Discovery review and first complete ordinary journey

🟡 **Fixed: the choices existed but their costs and benefit were learned too late.** The initial glade notice still directed both clay units only toward the well; the idle kitchen preview named the fee/ingredients/time without explaining why a meal was useful. Three native RED cases reproduced those omissions, with a literal-v10/saved-text counter passing. Fresh v11 glade notices now name the alternative pan and its same two-clay cost, explain that the public oven/cot need no repair, and leave the decision to the player. The idle pan's actual examine text previews one-action `3d4` healing plus stopping ordinary bleeding, against the greater total healing of freely toasting the two grains and keeping the pulp separately. Existing saved notices and Working/Ready descriptions stay unchanged. The readout/district/kitchen regression passed **89/89** (`9e232b482d9d4fae88f0e2b34c3a8ea6`). [RED](Verification/ConnectedSpread/Tests/native-red-choice-readouts.json); [GREEN](Verification/ConnectedSpread/Tests/native-green-choice-readouts-89.json).

The first complete ordinary-input Duelist journey passed **23/23** checks in 232.4 seconds, with 521 local paid inputs and 22 paid map steps: [run 37c1f14d68fc4c838427728b057b1838](Verification/SpreadDiscoveryExpeditions/Native/37c1f14d68fc4c838427728b057b1838/report.json). It chose the real starting build and retained its actual equipped dagger and two finite healing tonics. It retrieved original clay and the book, used cover and genuine defensive combat, repaired the pan while leaving the well broken, carried Orven's spoken introduction to Nella, replanted both returned seeds, commissioned and collected the meal, prepared one earned ink vial, re-inked the actually debited book, and cast the learned rite again at an existing hostile. The parcel healed actual HP from 32 to 40; no bleeding was present, so this run does not demonstrate its bleeding cure. The cap correctly restored only one missing charge, from 9 to 10, rather than fabricating a five-charge deficit. The later cast used that physical book and reduced it to 9 again.

Actual F5/F6 restored both genuinely pending kitchen work and final changed graphs. The returned original dry Sootroot and the distinct seed-grown replacement were ripe; the emptied original cache stayed empty. This first journey inspected but did not harvest the recovered dry bed, leaving its fourth finite XP award unexercised. The next driver revision adds that actual harvest and saves the depleted source and accomplishment record. It is an extension of evidence, not a claim that the earlier run performed it.

Prior native attempts remain failures: `dda6cceba9c5425289fb265da33dd0a4` conservatively refused an adjacent live pursuer; `f2e67cdfd57e4f8cb3f2c86b7016f5cc` sent movement while a queued level-up announcement was opening; `ed1f4ee6b2564c73aa2cae539aed3f44` treated asynchronous paid spell resolution as a free dismissible menu. The harness now allows bounded real Duelist defense, reads/dismisses actual announcements, waits through real effect completion, and stabilizes the ordinary input gate. Exact paid clock checks remain required; no gameplay owner, clock, resource or AI was changed to make these routes pass.

Visual inspection of the actual reserve and Marrowstye screenshots confirms corded beds survive harvest/replanting and the ink desk appears beside its real attendant, using the current 3D renderer and ordinary lighting/fog. The source contact sheet is also inspected, but it is not a substitute for those gameplay images. These agent-guided routes remain evidence of specific mechanics and appearances, not unaided discovery, campaign depth or measured performance.

## 18. Final implementation and acceptance record

The [saved implementation prompt](SPREAD-CONNECTED-EXPLORATION-IMPLEMENTATION-PROMPT.md) was executed. Both connected chains, all required content owners, original models, transactional actions, local consequences, finite progression and versioned world placement are implemented. The final source review found no outstanding significant correctness defect in this scope.

### Ordinary native journeys

| Actual selected build / seed | Complete result | Concrete observed outcome |
|---|---|---|
| Duelist / 64 | [23/23](Verification/SpreadDiscoveryExpeditions/Native/37c1f14d68fc4c838427728b057b1838/report.json), 521 local paid inputs, 22 map steps | Both chains, actual melee defense, pan repaired/well left broken, permitted reserve, returned seeds planted, prepared meal healed 32→40 HP, earned vial and later cast, pending/final F5/F6. |
| Stormcaller / 64 | [24/24](Verification/SpreadDiscoveryExpeditions/Native/290ed1d3ad144632b1660afe36e7f223/report.json), 516 local paid inputs, 22 map steps | Both chains using the actual defensive spell option. Also harvested the originally dry recovered Sootroot, completed the four 30-XP awards, reached level 2 with one SP and 5 XP toward the next level, and saved/reloaded the depleted crop and award record. Meal consumed at full HP: no healing benefit claimed. |
| Breaker / 1729 | [10/10](Verification/SpreadDiscoveryExpeditions/Native/b64768d2988345c083557f1c6319c418/report.json), 45 local paid inputs, 8 map steps | Strength 20 admitted hauling the original weight-136 frame; two paid pulls, free release and actual passage through its former aperture. Took the finite clay/manual, learned Vault without an SP/ink grant, repaired the original well, and restored moved frame/depleted cache/learned skill through F5/F6. |
| Bombardier / 29 | [11/11](Verification/SpreadDiscoveryExpeditions/Native/e430ea0cc270427aa3257389e6985f18/report.json), 81 local paid inputs, 10 map steps | Spent one original lightning tonic on a genuinely wet hostile (15→11 HP), walked the dry outer route with zero Calm casts, took the finite packet/manual, then harvested the reserve while actually witnessed. Suspension and exact ground yields survived F5/F6 and a return visit. Two earned grain paid restitution; final access/depletion survived another F5/F6. |

These are continuous native-input journeys from the actual new-game build picker, using the normal starting inventory and authoritative owners. No player transfers, owner grants, enemy suppression, fake spell targets, charge/clock edits or resource refills were used. The launchers isolate their saves and restore the prior scene, save root, starting-seed/build-choice settings and input settings. The two satellite seeds were chosen **after** the fixed census established their actual variants; they are targeted acceptance, not a blind sample. This narrows the original five-seed ordinary-journey criterion: all four specialists were exercised across three native seeds, while broader source admission used the separate fixed 68-world native census. Classic remains covered by existing build/bootstrap regressions; no fresh Classic journey is claimed.

The final crop-harvest extension adds one named check to the two full-chain profiles; the earlier 23-case Duelist run predates that extension. The 24-case Stormcaller run covers it, including the level-up and native save. Do not relabel the Duelist report as 24 cases.

Actual screenshots independently inspected include the repaired/working kitchen, reserve after harvest/replanting, Marrowstye ink desk, moved heavy frame, and witnessed reserve consequence. Their ordinary lighting, 3D owner models, changed geometry and in-world text are visible. The bed's persistent cord and the NPC's specific local consequence are readable without a universal quest marker. Inspection of screenshots does not prove that a first-time player will notice or correctly infer every action.

### Verification and scope

- Native Unity EditMode: the 1,646-case affected core sweep, 232 world/manifest cases, 115 rare-encounter cases, 37 satellite/census cases, 113 kitchen/destruction cases and final 89 readout/district/kitchen cases all passed. These selections overlap; no summed unique count or full-assembly pass is claimed. Exact receipts and selection evidence are in [Verification/ConnectedSpread/Tests](Verification/ConnectedSpread/Tests).
- Dedicated adversarial fixtures cover each new gameplay family, alongside paired positive/counter cases. Recorded RED preceded production correction for the service queries, crop clocks/yields, transactions, source admission, visual-state lookup, destruction reentry and final discovery text. Q1–Q4 review checked symmetry, shared contracts, counter-checks and documentation against the actual implementation. Older saved manifests/current-version pins were distinguished explicitly.
- Five source-art tests passed; all 14 inert models imported and native rendering tests passed. Original art/source meshes reuse the established material; existing crop stages and resident rigs remain authoritative. No decorative model performs hidden gameplay work.
- Objects.json was edited locally and parsed before/after: eight appended blueprints, 35 specific crop-timing sentence replacements, no existing blueprint removal or other existing-row changes. All new Assets source/model/test files have valid metas. The seedkeeper's conversation separately explains growth while away.
- The headless runner provided supplemental core evidence and deterministic debugging, **not** native seed outcomes, scene rendering or real input. The native results above supersede it for those boundaries.

### Performance observations and explicit deferrals

The targeted native routes collected bounded frame-delta samples during useful play without padding world time. Breaker sampled 21.9 seconds / 4,807 frames (p99 48.2 ms, maximum 1,672.5 ms); Bombardier sampled 46.5 seconds / 8,342 frames (p99 55.3 ms, maximum 1,946.1 ms). Those windows include cold generation, native save/reload, screenshots, diagnostic/report IO and paced input. Their spikes have **not** been attributed, and these numbers establish neither a rendering regression nor acceptable player-build performance. They are not the comparable 60–90 second prior/current-build profiling requested in §14. That comparative capture, allocation and long-absence arrival latency remain 🧪 deferred; no speculative optimization was added to disguise the missing measurement.

Actual compressed save observations were 135,397 bytes / three cached graphs for the Breaker, and 186,857 then 199,500 bytes / four graphs for the Bombardier. Restored graph counts matched. These small specific journeys do not establish long-campaign save size. Source review and tests establish bounded work and clock arithmetic, not performance targets.

Fresh-player discovery, meal value during an actual bleeding encounter, the 45–75 minute session target, long-term economy and campaign replayability remain 🧪 playtest questions. The native Duelist demonstrated healing from a meal; ordinary bleeding removal has focused native tests but was not observed in these journeys. The optional satellite remains deliberately sparse: 10 realized sites in the fixed 68-world native sample, not a claim that every world contains an additional encounter. Required resident/cellar/Curation chains supply the repeatable starting-region experience.

### Player discovery and compatibility

Start a **new game** for the complete connected region. Existing saves retain their authored chunks and frozen manifests; these additions are not retrofitted. Existing valid charged books can use Re-ink, and restored legacy crops acquire safe timing stamps without retroactive growth. Away-growth persistence applies to retained cultivated graphs, not arbitrary evicted terrain; no offscreen combat, rain simulation or general NPC production economy was added.

From the starting glade (11.10): read the working notice and descend in the western ruin for the two initial clay units and Sootroot beds. Nella's marked reserve is two world-map steps north (11.8). Orven's kitchen is one east and one south (12.11); its public oven/cot remain free, while the new cracked pan competes with the well for clay. Marrowstye is one step farther south (12.12); Ivrin's ordinary ink desk sits in its public receiving area. The optional encounter's address depends on the frozen world and actual safe placement; it is not a guaranteed destination from this guide.

The milestone adds four finite 30-XP accomplishments, local gathering rights and consequences, elapsed cultivation, one kitchen recipe and one ink recipe, a useful meal and a movement manual. It does not add universal crime, hunger pressure, maintenance chores, global faction rewards, extra crop/enemy species or ordinary-play BitLocker.

### Principal changed files

- Cultivation: `CropTime`, `CropPart`, `CropSystem`, `CropSystemPart`, `CropYieldService`, `SeedPart`, actual input/arrival/bootstrap seams.
- Ingredients and use: `GrimoireInkService`, `GrimoireChargePart`, `BotanicalInkDeskPart`, `FieldMealPart`, inventory UI/dispatch.
- Work, access and progress: `KitchenBatchPart`, `CookIntroductionPart`, `LocalGatheringClaimPart`, `LocalGatheringClaims`, `ConnectedSpreadProgress`, `ConnectedClayProvenance`; existing repair, inventory transfer/split/stack, creature death and structural destruction hooks.
- World/content: Spread exploration/rare allocation, resident/cellar/Curation builders, `GleanersDistrict`, manager integration, eight blueprints, pan repair recipe and crop explanation text.
- Presentation: `ConnectedSpread3DLibrary`, source/importer and 14 models; existing SpawnRing/portable/ground routing and VoxelWorld presentation.
- Verification: `Connected*Tests` and updated current-world regression pins; normal-input connected/ink/variant scenario partials and isolated native launcher; design/prompt and checked-in receipts.

One integrated commit contains the dependent production changes, generated model library, native scenarios, tests and this living record. This departs from the proposed per-batch commits: the batches were developed and verified separately, but shared generation, crop timing, transactions and renderer contracts were finalized together before publication. No partially integrated intermediate is published to main.

## 19. Post-publication review (2026-10-02)

The user requested a fresh review and fixes after commit `3afb07f20`. Three independent source passes cover cultivation/claims, kitchen/ink transactions, and generation/progression; the coordinator reviews rendering, native input and integration. Existing accepted scope remains unchanged. Checks target plausible player actions and lifecycle boundaries, with failing regressions before fixes and flipped-condition counters.

### Confirmed findings and corrections

| Severity / finding | Player consequence and verified cause | Correction |
|---|---|---|
| 🟡 Older-save admission | A missing exploration manifest restores a disabled legacy plan whose default version is still 11. Four version-only gates treated it as a connected world: cold glade construction rejected the original well/stair for lacking a world key; Marrowstye gained connected stock; an existing original stair could admit connected cellar crops. | Require both `Enabled` and version 11+ at all four gates. Preserve enabled v10 content and disabled legacy content, without changing the manifest or retrofitting saved chunks. |
| 🟡 Ground-throw reserve bypass | The player can throw adjacent loose produce directly from its claimed bed. No inventory pickup occurs, and later pickup on public ground cannot resolve that original bed, so a witnessed taking escaped Nella's consequence. | Capture the original source before extraction; publish witnessing only after a successful throw commits. Release only the thrown unit's claim marker, preserving the unthrown stack. Show the existing reserve warning in both actual throw menus. |
| 🔵 Kitchen callback lifecycle | A synthetic meal initializer can kill the cook or destroy the pan/output vessel during reconciliation. The recursion guard discarded the native invalidation; completion then refused, but the now-unbound job could never release its paid ingredients. No currently authored meal callback does this. | Remember owner invalidation during the guarded operation, then cancel and release surviving physical escrow through the existing transaction. Preserve the recursion guard, the service fee, exact custody and one-time settlement. |

### Verification sweep and rejected premises

- A version number alone does not authorize new-world content: `SpreadExplorationPlan.Legacy` deliberately disables admission without changing the version default. Fixing the four consumers avoids changing the saved format or historical behavior elsewhere.
- The throw command is reachable directly from adjacent world objects and extracts one unit from a stack. Reusing whole-item pickup release without naming that extracted unit would incorrectly release the unthrown remainder too.
- The first public-ground counter aimed through its own occupied crop bed: the actual projectile correctly landed on that obstruction. The test was corrected to aim at clear public ground. That failure is retained in the raw RED receipt and is **not** reported as a production defect.
- Current FireClay is not throwable. A partial-drop rollback can restore its quantity while losing one origin bit, but acquisition already records the player's once-only custody award before a carried drop is possible. No ordinary missed-XP or material-loss path was established. This low-priority metadata-conservation issue remains 🔵 deferred in `DropPartialCommand`'s split undo / `ConnectedClayProvenance.Split`; it does not justify broadening this patch into generic inventory rollback work.
- Crop elapsed-time arithmetic, wet-time boundaries, failed yields, local/arrival/load reconciliation, service payments, literal saved owners, introductions, source conservation and finite progression received independent source review. No additional significant defect was confirmed. Possible kitchen-crop frontage and performance concerns lacked a reproducer; prior playtest/performance limits in §18 remain in force.

### Regression and review record

Native RED preceded each production correction: [kitchen lifecycle](Verification/ConnectedSpread/Tests/review-red-kitchen-callback-lifecycle.json) ran six cases, with the three invalidation cases failing and all three unchanged controls passing. [Throw and legacy admission](Verification/ConnectedSpread/Tests/review-red-throw-legacy.json) ran fifteen cases: three real legacy failures, six real throw/marker/menu failures, the public-ground test-premise error described above, and five passing controls. Expanded post-fix coverage contains 32 new cases: 14 throw/rollback/menu, 10 old/new/malformed world, six callback lifecycle and two callback-exception cases. The supplemental rollback/menu/version/exception cases were added after the initial RED; they are not claimed as additional pre-fix native runs.

The [55-fixture affected native selection](Verification/ConnectedSpread/Tests/review-native-selection.json) passed [1,031/1,031 tests](Verification/ConnectedSpread/Tests/review-native-green-1031.json), including all 32 new cases. This is a selected affected sweep, not a whole-assembly claim. The native Bombardier journey now includes the actual second ground-throw popup, a no-turn cancellation, a paid ground throw, preservation of the existing single breach, and native replacement-graph save/reload of the moved yield. This journey starts with an already witnessed harvest; first-witness throwing and partial-stack semantics are established by the paired native EditMode cases, not fabricated during the journey.

Q1–Q4 source review checked pickup/throw timing symmetry, extracted-owner versus remainder identity, post-commit/rollback boundaries, the four admission gates against neighboring enabled checks, menu-label consistency and documentation. An independent reviewer found no significant remaining issue in those changes or the native journey extension. Kitchen factory failure keeps a viable job pending but salvages inputs after an actual owner invalidation; ordinary completion and repeated reconciliation remain counterchecks. The native affected sweep and the ordinary-input journey below passed; no significant review finding remains open.

Changed production files: `ThrowItemCommand`, `LocalGatheringClaims`, `HandlingPart`, `InputHandler`, `KitchenBatchPart`, `GleanersDistrict` and `OverworldZoneManager`. New regression fixtures: `ConnectedReserveGroundThrowReviewTests`, `ConnectedReserveGroundThrowBoundaryReviewTests`, `ConnectedLegacyAdmissionReviewTests`, `ConnectedKitchenCallbackLifecycleReviewTests` and `ConnectedKitchenCallbackExceptionReviewTests`, each with a fresh meta. The existing `SpreadDiscoveryNativePlayer.ConnectedVariants` scenario and this living record complete the patch. No blueprint, art, reward amount, world migration or broader crime/economy changes.

### Final ordinary-input acceptance

The extended Bombardier/seed-29 route passed [12/12 checks](Verification/SpreadDiscoveryExpeditions/Native/bc7d141325d94f9ebdfe9eaec30bac4d/report.json), with 89 paid local inputs and 10 map steps. It used the actual build picker, original starting stock, real satellite loot and naturally placed reserve. The original witnessed harvest suspended access once. The new check opened the real ground-throw popup, observed its reserve warning, cancelled without advancing the action clock or releasing ownership, then reopened it and threw a real Marlroot clod onto clear public ground. The clod lost its local claim marker, the existing breach remained at ordinal one, and both this moved item and the suspended state survived F5/F6. The route then left, returned, paid restitution with earned grain and saved/reloaded again. No gameplay owner, witness, inventory or clock was fabricated through editor code.

**Can verify (script-observable):** native command routing, warning-label selection, exact source/landing identity, free cancellation, one paid throw, no duplicate consequence, retained remainder/rollback cases in EditMode, original old-world versus connected fresh-world admission, service custody, native save/reload and all listed checks. The [throw-warning screenshot](Verification/SpreadDiscoveryExpeditions/Native/bc7d141325d94f9ebdfe9eaec30bac4d/variant-ground-throw-warning.png) and post-load screenshot were independently inspected; the whole label fits and the voxel scene remains visible. The editor returned to the original SampleScene in idle Edit mode; the console had no errors.

**Cannot verify (visual / feel):** this targeted route is not unaided first-player discovery, balance, long-session replayability or a performance comparison. The native journey covers an already witnessed harvest followed by throwing; first-witness throwing and stack boundaries are controlled native EditMode coverage. The kitchen death/destruction-during-initialization case is a synthetic adversarial extension point, not a reproduced current authored encounter. Existing §18 deferrals remain explicit; the low-impact clay metadata note above is not claimed fixed.
