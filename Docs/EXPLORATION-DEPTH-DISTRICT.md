# Exploration depth: the gleaners' district

Status: implemented and reviewed; core district native acceptance complete. Longer prepared-utility expedition remains a documented verification limit.
Owner: this document is the authoritative design, execution plan and evidence log for this work.
Baseline: main `200d2f7b4`, 2026-10-01. This is original Caves of Ooo design inspired by systemic exploration; no claim of Qud code parity.

## 1. Direction and observable outcome

The next improvement must change an ordinary expedition, not merely enlarge the content catalogue. A fresh player should see a useful broken service, learn where a material might be found, choose how to retrieve it, and return to an actual improvement. A second visit must retain what was taken and repaired. Equipment, movement and preparation should change the available decisions.

The opening district already contains the glade, Sill, an alembic, forge, trapper, seed keeper, kitchen and Marrowstye. Preserve these places and link their existing functions. Add one compact underground destination and one useful surface repair. Correct the skill economy so existing mobility and social abilities are attainable. Do not create a new quest chain, class system, universal crafting framework or world simulation for this slice.

The campaign-scale recommendations remain direction, not an assertion that this district delivers an entire game's replayability. Expansion to other biomes follows the evidence from this working slice.

## 2. Verification sweep and corrections

| Initial premise | Source / observed correction | Design consequence |
|---|---|---|
| New games start in Morrowfast. | `FreshGameStart.CandidateZoneIds` prefers glade11.10 for empty configuration; SampleScene serializes empty; a read-only live editor inspection confirmed empty. Five-seed existing tests pin the same policy. | Keep the Spread start. Never move the opening district into tier3 Grovelands merely to fit a mistaken summary. |
| Recently added services are far from spawn. | Alembic11.9, forge12.10, trapper11.11, seedkeeper11.8, kitchen12.11, Curation12.12 are near glade11.10. Assignment can still refuse unsafe generation. | Reuse the services; verify actual placement and make directions useful. |
| Tier1 repairs already participate in that opening. | `RepairCultivationSite.ZoneID` is2.6, thirteen cardinal world cells away. | Preserve the original allotment and add a different repair application at the glade. |
| Mobility skills are a practical investment. | Leveling grants1SP, but Acrobatics uses100/50 and Persuasion100/100/25. | Normalize to the live1SP economy; test real levels and purchases. |
| Buying a power requires its tree. | Purchase and UI currently inspect only explicit `Requires`, absent in the shipped trees. | Share the purchase prerequisite rule; roots gate purchased powers. Preserve direct teaching, starting grants and already learned saves. |
| The ordinary player has only a dagger. | Actual starter also has six combat/control spells and farming instruction. | Test different approaches with the real kit. Do not grant imaginary handicaps or nerf Calm without encounter evidence. |
| A nearby cave is guaranteed. | Ordinary surface entrance is a50% roll; special glade pipeline has none. | Add an optional exact shallow glade cellar with a real return connection. Reserve only its first underground level. |
| A torch is the first access to darkness. | Player already has innate light. | Light finds improve practical use; do not advertise a nonexistent hard darkness gate. |
| Existing timber repair can lock a shortcut. | Its diagnosis describes a gate hanging open. | Do not repurpose it as a closed lock. Use existing hauling/bypass geometry instead of introducing a misleading gate. |
| Curation certification changes all faction relationships. | It grants a local counterfoil and finite access; it does not change global standing. | Keep this precise local consequence and existing canon. No arbitrary reputation bonus advertised as a new faction tier. |
| Earlier scenarios prove uninterrupted ordinary play. | Many explicitly transfer the original player. The discovery harness has a separate native-input ordinary route. | Distinguish isolated mechanism tests, agent-guided native journeys, screenshots and unmeasured first-player comprehension. |

Reviewed source: FreshGameStart; ReferenceGladePlan/Builder; OverworldZoneManager; ZoneManager connections; CaveEntranceBuilder; SpreadExplorationPlan/Worksites/Residents; RepairablePart/RepairCultivationSite and Tier1 recipes; SkillRegistry/BuySkillAction/skill JSON and UI state; StartingSpellKit/NewGameLoadout; existing native discovery and glade launchers. Supporting audits are in the session scratch folder and are inputs, not competing plans.

## 3. Readiness and deliberate reuse

| Area | Readiness | Action |
|---|---|---|
| Repairs and saved item ownership | Green | Reuse actual composition, material consumption and saved Repairable state. |
| Ordinary input, travel, saving | Green | Extend the established isolated native launcher and actual key input driver. |
| Local exploration connections | Yellow | Make the glade well and underground recovery a connected loop. |
| Character investment | Red | Fix incompatible point scale and purchase/UI prerequisite disagreement. |
| Workshops, crop products, Curation | Green mechanisms / yellow ordinary experience | Exercise and direct players to existing useful services before adding duplicates. |
| Art | Green existing owner models / yellow new composition | Reuse approved well, stairs, walls, salvage, enemies and item models; inspect the composed space in Play. Add art only if a genuine readability gap remains. |
| General campaign replayability | Not proven | This release is one tested district; do not claim repeated play across a whole campaign. |

## 4. District design

North is up. Distances are native world-map transitions, not teleports.

```
             11.8 seed keeper
             11.9 alembic
10.10 Sill — 11.10 glade — 12.10 forge
             | cellar      |
             11.11 store — 12.11 kitchen
                          12.12 Marrowstye
```

### A. The broken gleaners' well

Add one damaged masonry well to the glade's working ruins. Its visible examination and native Repair action name the two fire-clay units needed. The well is separate from the existing three-dram basin; the basin remains the immediate finite source. Successful repair unlocks the existing well's ordinary drinking and filling behavior. It does not heal structural HP, water crops remotely, create loot, or refill the old basin.

Materials are recovered elsewhere: the underground supply store contains a finite physical clay supply. Existing crop processing and material traders remain compatible alternative sources wherever the player actually finds them. Never claim a particular trader stocks clay without checking that trader. No material pile beside the well makes the expedition unnecessary.

The nearby notice names the old underground store and the northern alembic/eastern forge, with practical uses. Directions are grounded prose and Examine text, not map checklists or an omniscient event marker system.

### B. The below-ground supply store

The first glade underground level is a compact original ruin built with existing native owners. Its stairs down and up are paired and persist. Deeper ordinary cave space is left alone; the authored cellar must not overwrite loaded zones or protected canon locations. Missing optional dependencies must leave the normal world usable.

Players enter a clear landing with an obvious return. The nearer supply approach exposes a small original enemy presence. A longer side approach avoids the guarded line and includes a movable obstruction with a usable shoulder and landing. Withdrawal never requires killing an enemy, spending a consumable, or repairing the only exit. Ground shape, examination and owner models expose the alternatives.

The store holds finite physical fire clay for the well and a useful discovery drawn from a small deterministic set of equipment/preparation packages. Include at least one charged rite book package; do not hand out all rewards on every seed. Other packages use genuinely usable equipment and materials. Publish the exact final contents and selection rule in the implementation log. Alternative approach does not duplicate the same cache or award XP for taking a scripted path.

Vary meaningful layout/approach and supplies across the fixed seed corpus without changing the promised safe return or material availability. Preserve the generated owners on unload/save/revisit so extracted rewards and repaired infrastructure never regenerate.

### C. Character approaches

These are comparative play approaches, not mutually exclusive classes. All start with the actual ordinary kit.

| Approach | Decision and investment | Expected advantage | Real cost / limitation |
|---|---|---|---|
| Confrontation | Use dagger and spells; recover useful equipment; purchase a weapon root and power using earned SP. | Remove a defender and use the direct route. | Damage, healing, durability and time. |
| Control / movement | Use existing Calm/Rime Grip/Jet Blast; learn attainable Vault/Tumble through ordinary SP or an actual discovered book. | Separate or bypass a threat and take the longer route without clearing the area. | Cooldown, landing geometry, acquisition cost; bypassing does not currently award kill XP. |
| Preparation / utility | Recover clay, restore the well, gather/brew and use the forge. | Establish a practical return source and improve a chosen weapon. | Finite material/time, carried capacity, tempering durability cost. |

Purchased child powers require their tree. Books and existing grants keep their established ability to teach directly. Nothing unlearns existing powers. All rows of the skill screen must agree with the command, including failure explanation; no points are spent on rejected purchases.

### D. Existing faction anchor

Marrowstye already offers a concrete Curation choice: physical intake certification gives a counterfoil for finite tools; quarantine is a separate voluntary path. Refer to that existing local effect honestly. This slice does not invent faction-wide permission from a certificate or turn ordinary salt-preserved bodies into conscious prisoners.

## 5. Implementation batches and acceptance

1. **Reachable character investment.** RED tests for ordinary-point purchases and missing parent; shared purchase/UI rule, normalized costs; countercheck direct teaching and restored grants; focused regression and dedicated adversarial review.
2. **Connected surface and cellar.** RED tests against actual generated glade/cellar: well absent before change, exact bidirectional stairs, finite usable clay/reward, reachability and repeat generation bounds. Implement cold-generation composition and retention with existing owners. Verify three native seeds (64,1729,729490642), malformed content, wrong biome/POI, cached-save behavior and return access.
3. **Directions and native expedition.** Extend the existing isolated native audit using actual N, movement/travel, world/item menus, repair, F5/F6. Capture the well before/after and cellar alternatives. Run comparative actual-input approaches where feasible, explicitly recording any setup, interruption or budget limit. Validate ordinary starter survival and useful material expenditure, not just method return values.
4. **Cold review and delivery.** Read every changed public contract and symmetrical path; adversarial suite for source ownership, duplicate/cached/rejected content and save identity. Correct significant findings; record lower-priority issues with severity and rationale. Run affected regressions once after final changes, then fetch/rebase and push authorized work to main with the CLAUDE §2.3 commit format and this document in the same commit.

No generation test is evidence of fun. No fixture SP grant is evidence of earned progression. No direct transfer is described as ordinary travel. Actual-input automation knows the map and cannot prove unfamiliar-player understanding. Broad all-suite totals must state the actual selection and environment.

## 6. Execution prompt used for this implementation

Implement this document in order. Start each production batch with a meaningful failing assertion and retain the evidence. Reuse actual world, inventory, repair, skill, renderer and save contracts. Correct false premises in this document before relying on them. Keep the player's visible decision and practical consequence as the acceptance unit. Use bounded parallel file ownership; the coordinator alone runs Unity and integrates shared files. Preserve existing unrelated changes, saved games and inspector settings. Fix significant correctness issues; do not let a minor animation or unrelated pet issue stall the district. Review, test, update the living document, commit and push the completed work. Continue without asking the user to decide routine implementation choices.

## 7. Evidence and implementation log

- Pre-implementation source audits completed. Live editor idle in SampleScene; FreshGameZoneID observed empty, confirming ordinary glade policy. No gameplay claim follows from this read-only inspection.
- Initial repository modifications are Unity MCP logs and pre-existing untracked evidence; preserve and exclude them from commits.
- Initial RED: surface five cases completed, four expected missing-feature failures; progression17 cases,15 expected failures with two exemption controls; cellar absent-builder assertions then separate late-callback assertions. Exact native records are in `Docs/Verification/ExplorationDepth/Tests/`.
- First comprehensive affected regression: Unity EditMode **1,074/1,074 passed**, zero failed/skipped,67.54s,76fixtures. This covers skills, repairs, starting glade, fresh spawn, the new district, actual serialized repair/loot, and art eligibility. It is a selected native editor run, not a full-project suite or runner substitute.
- Batch1 implemented: Acrobatics/Persuasion roots and powers cost1SP; original stat requirements remain. `SkillPurchaseEligibility` is shared by UI and purchase command. Parent identity is the registry class, not a potentially duplicated display name. Books, starter grants and learned saves remain exempt. Prerequisites precede affordability consistently; negative-cost/missing-SP rows cannot appear buyable. Independent cold review found no significant new purchase/refund/save regression. Legacy throwing lifecycle hooks remain a separate pre-existing concern.
- Batch2 implemented: glade well41,8, working notice41,10 and cellar steps28,19 to11.10.1@40,12. The exact current surface endpoint and route authorize cold cellar generation. Cellar-first entry generates the surface first; old cached half-pairs never get retrofitted. Two bounded addresses are retained even when emptied; retention itself grants no generation authority and preserves old generic graphs literally.
- Cellar contents: two actual fire-clay units and one actual reward; even seeds provide Shattered Rime with10/10charges, odd seeds a Buckler. Seed parity chooses north/south side passage, and modulo3 shifts store/guard depth. A native15HP Marlback keeps ordinary AI/loadout. Physical beam hauling and a permanent detour are both tested; no claim of permanent enemy-free safety.
- Rendering review found and corrected a real admission gap: the protected glade column was outside ordinary underground3D scope. Exact current managed11.10.1 under Spread/noPOI now uses Spread presentation; other depths and detached graphs remain excluded. Legacy owners at that address gain presentation only, not regenerated content.
- Cold reviews found malformed owner/callback gaps. RED probes exposed late changes to terrain collision, stairs, beam weight, surface world authority and repaired state. Cellar semantic validation and surface detached/placed receipts now reject those before publishing. All four initial malformed-stair/well probes now pass; final regression includes them.
- First ordinary native run `ab68a46f10e64c5ea59d30163aaa547a`:14/15 checks passed in32.42s, no transfers or grants. Real clay retrieval, return, material repair, drinking and native save/reload ran; the aggregate restored-graph comparison failed and is being diagnosed. It is **not** a completed acceptance run. Player finished with40HP,50drams, no earnedXP; this route proves bypass and utility, not combat progression pacing.
- Viewed native cellar landing and repaired-well screenshots: submitted voxel actors, walls, floor, stairs, crate, signs and well are visible in the playable viewport; repaired well has a distinct shape. The thick masonry composition is intentionally compact. First-player discovery, broad aesthetic preference and long-term replayability are unmeasured.

## 8. Next expansion gate

The next connected-region milestone is designed in [SPREAD-CONNECTED-EXPLORATION-DESIGN.md](SPREAD-CONNECTED-EXPLORATION-DESIGN.md), with its own implementation directive and evidence ledger. It extends this delivered district; it does not retroactively expand the acceptance claims below.

After the district is working, prefer expansions that change a choice: a biome-specific resource with two practical uses, an existing repair that opens a meaningful return service, or a faction permission that changes access. Require current-source audit, actual discoverability, finite ownership, saved consequences, readable art and an ordinary route. Do not automatically reproduce the same cellar or five-object worksite in every biome. Campaign history and broader faction pressure need their own design and play evidence; they are not silently declared finished by this slice.

### Current-main integration

Main advanced to `c5d003a8d` during implementation. Rebased the uncommitted district onto the starting-build and freeze/thaw changes, preserving existing work and logs. The new build picker does not run under native-audit save isolation; these native routes exercise the legacy ordinary kit, not a witnessed picker selection. Source review confirms all builds can repair the well and use the permanent bypass; direct book teaching stays exempt from purchase roots. Duelist/Breaker already own a buckler, so that reward is spare equipment for them. Equipped weapons must be put in the pack before the current forge lists them; the working notice explains this. The new starting-build and freeze/thaw suites join final integration regression.

### Native comparison and diagnostic follow-up

- Covered seed64 after current-main integration: `60990c78f2114441a3cc375d0b9b3843`, **16/16**,34.09s. No injury or consumable use; actual Shattered Rime learning and strict saved graph/clock/ownership all passed. The first aggregate save failure was not sufficiently instrumented to identify its clause; it is retained as an incomplete run, not called a proven save fix.
- Confrontation seed64: `a52fd264d546404c9c750593f945204b`, **16/16**,31.15s. Original dagger defeated the ordinary15HP defender;15XP earned, no level/SP, no tonic consumed; actual book learning, well repair and strict reload passed. This is one observed combat outcome, not balance evidence.
- Mirrored seed1729: `93ca4b0a35104559a965e128a8506bab`, **15/16**. Actual shield equipment, repair and their persisted ownership passed. Exact surface comparison found an unrelated `SeveredLimb` with emptyID before save and assignedID after load. The native check was retained unchanged; the narrow creation fix and final passing repeat are recorded below.
- The reward-use driver initially hit a stale pane assumption: opening inventory can already focus its item pane when the viewport changes the pointer's grid position. Blind Tab switched away. The native helper now observes the actual pane and uses Tab until reaching inventory; no gameplay state is set by the audit. Failed report `adae24472add411f848d0a01f3528561` is retained.
- Raw reports and selected independently viewed native screenshots are copied to `Docs/Verification/ExplorationDepth/Native/`. Reports retain their original screenshot locations; only selected images are committed. Full original local captures remain on disk.

### Narrow save/interaction repair discovered in native play

`SeveredLimbFactory.Create` published a portable limb with an empty ID. Full save load correctly repaired blank legacy IDs, which exposed a before/after identity change in the strict mirrored journey. The same blank identity also excluded a new limb from the native “everything here” picker. This is an actual creation/interaction issue, not demonstrated item loss. Eight focused native EditMode cases produced four expected RED failures (creation, dismember publication, ground save, carried save) and four GREEN veto/legacy controls. The factory now assigns a fresh identity on creation; legacy save repair is unchanged. A new picker countercheck pins the player-visible consequence.

Final selected native Unity EditMode regression on current main: **1,601/1,601 passed**, zero failed/skipped,83.42s,88 fixtures. Includes the new10 limb cases, current starting-build/freeze suites and affected anatomy/interaction regressions. No claim of a whole-project run. Exact selection and native tool result are committed alongside earlier RED evidence.


## 9. Final review and deliberate limits

### Methodology §5 / cold review

- 🟡 **Fixed — unusable generated owners.** Initial/late callback probes caught solid or invisible stairs, a well without matching composition, altered cell authority, and late cellar terrain/beam changes. Stage and validate the entire packet, preserve original owners, and publish only with unchanged authority. The positive generated routes and negative malformed/legacy/POI cases pass.
- 🟡 **Fixed — cellar presentation exclusion.** The special glade column fell outside ordinary underground presentation. Admit only the current managed Spread cellar address; retain wrong-depth, wrong-biome and detached-owner counterchecks. Actual native screenshots were viewed.
- 🟡 **Fixed — limb identity and interaction.** Native reload comparison exposed missing creation IDs. New limbs receive IDs before dismember observers or zone publication; target picking and exact ground/carried saves now pass. Old saves still repair anonymous owners on load. Ten focused tests pass within the1,601-case run.
- 🔵 **Fixed — repaired wording.** The well's persistent name/examination no longer describe it as still cracked after repair; the native repair part supplies current diagnosis.
- 🧪 **Bounded — comparative play.** Covered retrieval and direct confrontation both complete through actual inputs. The prepared-confrontation attempt completed harvesting, brewing, tempering and actual return, then stopped because the native clock checker allows at most two End events per action. Its30-tick/unchanged-energy result is consistent with three automatic turns, but the missing failed-event window prevents proving a stun cause. Retain the incomplete report; do not claim a combat fix or freeze-proc proof. Future audit failures now retain that event window. Verify preparation through the covered route as a utility expedition; this adds a connected activity loop, not a third independent cellar route.
- ⚪ **Deliberate — existing art and services.** Reuse working voxel models, repair recipes, material inventory, ordinary enemy AI and actual workshops. No new modeling is needed to expose this slice. Only two deterministic reward packages and one shallow authored location are shipped; this is not a new generator for every biome.
- ⚪ **Deliberate — old generated chunks.** Preserve cached glade/cellar graphs exactly. New games obtain the complete district. Do not silently patch existing saves or regenerate an emptied cache. Both district addresses stay retained, including legacy/emptied graphs; this is a bounded two-zone memory tradeoff.
- ⚪ **Deliberate — progression evidence.** Real level-award unit tests establish one-point affordability and exact root gating. Native confrontation earned15XP, below the next level. Earned purchase pacing, chosen-build picker acceptance, unfamiliar-player discovery, broad visual preference and campaign replayability remain unmeasured.

Cold review Q1 checked upward/downward connection ownership and retention before/after save; Q2 checked shared UI/command skill eligibility and registry class identity; Q3 mapped cost, stat, parent, legacy, wrong-address, occupied, malformed and creation-callback branches to positive/negative cases; Q4 reconciled final scope and reports with current main. Independent source reviews found no remaining significant defect after the listed fixes. No Qud source-parity claim applies to this original district.

### Changed files

- Skills: Acrobatics/Persuasion data; PowerData/SkillRegistry; shared SkillPurchaseEligibility; BuySkillAction; SkillsScreenStateBuilder/SkillsScreenUI; progression and existing purchase/UI tests.
- District: GleanersDistrict; GleanersCellarBuilder; OverworldZoneManager; SpreadPresentationScope; generated-world, adversarial, layout/hauling and art-scope tests.
- Native evidence: SpreadDiscoveryNativeBatch; SpreadDiscoveryNativePlayer and its new District partial; exact test selection/results, raw incomplete and completed route reports, and selected screenshots under `Docs/Verification/ExplorationDepth`.
- Narrow discovered fix: SeveredLimbFactory; SeveredLimbIdentityTests.
- This living design/evidence document; all new Asset C# files have unique `.meta` GUIDs. Objects.json and unrelated work/log files are untouched by the feature commits.


### Final native follow-up

Mirrored seed1729 after the limb fix (`e8b0aae12d874031b2e73ab9df55dc8e`) passes **16/16** with no surface/cellar graph differences. The actual buckler remains equipped after F6; material is spent and the well usable. A later prepared-utility run stopped because a real hostile occupied the forge and all adjacent goals failed the audit's safety clearance. Its report remains as evidence of refusal. The driver now uses ordinary movement toward a safe aligned casting position and the original ready Calm, then checks the real stationary effect, paid action and cooldown. Only a currently valid stationary Calm excludes that owner from threat clearance; occupied creature cells and active-hostile clearance remain blocked. No spawn reroll, AI suppression or cooldown wait is introduced. Existing native evidence/Calm authority tests pass **57/57** after this harness update.


## 10. Delivery acceptance and bounded follow-up

| Verification | Result | What it establishes |
|---|---|---|
| Current-main native Unity EditMode integration |1,601/1,601,0failed,0skipped|88 affected fixtures, including103 new feature/identity cases; full-project suite not run. |
| Native evidence and Calm authority helpers after driver change |57/57|Exact event receipts and current stationary Calm authority, including expiry/counterchecks. |
| Covered seed64 |16/16|Actual clay/reward acquisition, book learning, repaired service and exact reload. |
| Direct confrontation seed64 |16/16|Original dagger defeats defender,15XP, actual repair/reward and exact reload. |
| Mirrored seed1729 after identity fix |16/16|South passage, actual equipped buckler, spent stock/repair and exact reload. |
| Prepared confrontation attempt |Incomplete|Native harvest, brew, temper and3-map-step return all passed; action-clock bound interrupted later combat. No successful full-trip or freeze-proc claim. |
| Prepared utility attempts |Incomplete|Changing active threats caused honest route refusal; real original Calm was used and verified in the last run. No successful full-trip or saved-tempered-weapon claim. |

Final prepared-utility attempt `a140e539cbce4cdc9e7f23b306f5bfc3` used one paid original Calm against a pursuer and then refused an unavailable safe route while the spell cooled down. This is a bounded automation policy limit in existing populated workshops, not a demonstrated broken forge, inaccessible production zone or reason to suppress enemies. Preserve that report and the earlier refusal. Per the user's prioritization instruction, stop this lower-priority audit branch; a future route test can use a broader ordinary tactical policy and explicit blocked-turn receipts. The new district, repair, reusable supplies, two cellar approaches, progression corrections and original-world persistence are delivered now. No user intervention is required for those systems.

Scope divergence: reuse existing voxel models/services; no new art assets were necessary. Two deterministic reward packages replace the broader informal package idea. The longer workshop-plus-cellar trip is only partially witnessed, and native earned skill purchases are not witnessed. Character-affordability unit tests and separate workshop actions are not substituted for those missing play claims. Existing saved chunks are deliberately untouched. All changes were rebased onto main `c5d003a8d` before final production verification; later source edits only improve audit observations/routing and passed compilation plus the57-case evidence helper suite.

Player route in a **new game**: the lined well is near the glade camp at41,8; examine the working notice at41,10. Find the cellar steps in the western ruin at28,19 (southwest of the initial landing). Below, read the tally by the return stair; use the broad defended passage or the longer service passage. Bring two fire-clay units back to the well and use its repair action. Alembic is one world cell north, forge one east; put an equipped weapon into the pack before selecting it at the forge. No quest activation is required.
