# Exploration depth: the gleaners' district

Status: character-investment batch implemented and verified; district batch follows.
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

## 7. Character-investment implementation and review

Acrobatics/Persuasion roots and powers now cost1SP, matching actual level awards. Stat requirements stay intact. The registry records stable parent class identity, and SkillPurchaseEligibility is the shared side-effect-free rule for UI and purchase commands. Purchased child powers require their tree; starter/book grants and already learned saves remain exempt. Rejected purchases spend nothing.

RED:17 selected cases,15 expected failures and two exemption controls, recorded in `Docs/Verification/ExplorationDepth/Tests/progression-red.json`. Final new progression coverage is23 cases (14 behavior,9 adversarial), all passing inside the1,601-case native Unity integration run on rebased main `c5d003a8d`. This is native editor execution, not the outside-Unity runner; earned purchase pacing is not established by unit tests.

Self-review: 🟡 fixed UI/command prerequisite disagreement and unreachable100-point costs; counterchecks cover wrong/missing parent, stat minima, insufficient/missing points, negative costs, duplicate ownership, direct teaching and saved powers. Registry class identity avoids duplicate display-name ambiguity. Independent cold review found no significant remaining purchase/save regression. ⚪ existing throwing skill lifecycle hooks remain a separate pre-existing issue; no new lifecycle framework is introduced. Qud-style exploration motivates the direction; this is original CoO balancing, not a Qud parity claim.

Files: Acrobatics/Persuasion JSON; PowerData and SkillRegistry; new SkillPurchaseEligibility; BuySkillAction; SkillsScreenStateBuilder/SkillsScreenUI; existing purchase/UI tests and new progression/adversarial fixtures with meta files. No scope divergence for this batch. District, native route, broader evidence and discovered save/interaction work are separate following batches.

## 8. Native-discovered limb identity repair

The mirrored district audit exposed a pre-existing blank identity on newly severed limbs. Save loading repairs blank IDs, so a new limb changed identity across its first load; the native everything-here picker also omits blank-ID owners. Assign a fresh GUID in SeveredLimbFactory before the owner is published. Keep legacy loading, anatomy, RNG and ownership semantics unchanged.

Native RED:8 cases produced4 expected failures (creation, observer publication, ground and carried save) and4 passing veto/legacy controls. Final fixture10/10 passed inside the1,601-case Unity run, with added picker/removed-ID counterchecks. Mirrored native journey after the fix:16/16 with exact surface, cellar, player, clock, repair and reward restoration; raw journey evidence accompanies the district batch. No item-loss claim follows from the original failure.

Self-review: 🟡 fixed creation-time identity/interaction omission; Q1 checks creation before observer/zone admission versus save restoration; Q2 preserves existing GUID convention; Q3 verifies veto, blank legacy IDs and existing opaque IDs; Q4 records the narrow scope. No save schema change or generalized entity-construction refactor. Files: SeveredLimbFactory.cs; new SeveredLimbIdentityTests.cs +meta; RED evidence; this document.
