# C15: scoped native held and worn equipment

The scoped renderer and12 fitted wearable meshes are published and imported. All167 equipment tests pass in the actual Unity editor. Six staged fit-gallery cases plus six original-enemy diagnostic counterchecks are published for the next native run; visual fit and whole-biome equipped-item census closure remain open. Root owns native execution and screenshots. This slice owns held/worn views; the renderer slice owns creature rigs/body art.

## Verified source corrections

- Actual native census missing gear includes Hatchet57, Gloves72, Boots72, ShortSword11, LongSword7, LeatherArmor18, Cap20, Spear9, Dagger7. Some owners are currently static unrigged regional NPCs; equippable content alone cannot create an attachment. Humanoid rig work and gear work are distinct requirements.
- At the initial sweep, Village3DEquipmentViews accepted only Head armor and Hand weapons/shields. It mapped Head to one generic helmet and Hand to generic blade/club/staff, with no actual Body/Feet/Handwear/Back/Torch path. Most weapons with only Cutting/Piercing attributes missed its class-token aliases.
- Equipment is native Inventory.EquippedItems plus Physics.Equipped, no InInventory, positive stacks, and matching first Body slot. Preserve every existing check, add parent/backlink controls where missing. One item occupying several native slots remains one logical item; no duplicated native entities.
- Existing Nam/player/Sien/Marlback rigs have Head/Hand.L/Hand.R/Back sockets. New human rig adds Body and Feet.L/R. Original actor assets are borrowed and cannot be edited for gear. Scoped anchors may be created only on the owned live instance and verified known named bones. Animals with no legitimate attachment capability must remain explicit fallbacks, not fake equipment success.
- Actual armor slots: Body (LeatherArmor,ChainMail,PlateArmor,FineRingMail,RivetedPlate), Head (LeatherCap,IronHelmet), Feet (LeatherBoots,IronshodBoots), Handwear (LeatherGloves), Back (Cloak,WardedCloak), Hand (Buckler/IronBuckler and actual weapons/Torch). The model must follow current equipped item identity and any assembly state, never an inventory neighbor or display name.

## Implementation sequence

1. Add actual native fixture RED for ordinary Player, live rigged local NPC and original Marlback: exact original equipped item, visible adopted form/material, actual matching animated attachment; include wrong Physics owner, foreign item part, no Body slot, carried item, unsupported animal rig, removed actor, hidden owner and non-Spread control. Validate bodies after successful source/runtime authority, preserving quest refusals.
2. Implement a scoped equipment plan resolver using exact portable/crafted state, slot and actual owner anatomy. Preserve legacy constructor behavior outside Spread. Pass explicit receiving-scope policy from SpawnRing3DPresenter; no global singleton/map lookup from the item alone.
3. Held items use their actual owned portable model with measured socket-local grip transforms. Preserve native weapon source, enhancement and current forged assembly. No generic blade/club claim counts as exact item coverage.
4. Worn armor needs explicit worn orientation and limb fitting. Inspect actual source geometry before deciding which dropped forms can be reused. Boots/gloves may need paired attachments; a pair must follow both actual bones, dispose/hide together and never register two native item owners. Avoid a folded ground inventory object presented as fitted armor merely to satisfy a renderer count.
5. Native unit and viewed gameplay frames: animate walk/attack, equip/unequip, reforge, save/load, actor death/removal, body/slot change and profile reversal. Compare source bytes, borrowed skeleton/mesh/material references and original body ownership. Report unsupported nonhumanoid equipment honestly.

## Boundaries

No gameplay equipment grants in acceptance walkthroughs. EditMode uses explicit factory-owned fixture gear, clearly not acquisition or balance proof. Native play uses starter/actual loot. Geometry presence is not appearance or fit acceptance; screen-scale pictures and real bone motion are separate gates.

## Sweep corrections and evidence

- Exact inherited content contains 50 concrete equippable blueprints: 38 Hand items and 12 worn items. Seven portable base definitions are not equipment designs.
- Humanoid Feet and Handwear are each one abstract native body slot. Their visual pairs remain one native item; gloves must not reserve the same attachment occupancy keys as held weapons. Quadruped Feet have different semantics, so humanoid pair art cannot claim universal anatomy. Losing a Hand does not unequip the abstract Handwear slot; hide only the absent-side visual without changing equipment.
- Native one-unit pickup may auto-equip into a free hand. A null InInventory reference is not evidence of an item loss or stack merge. The separately corrected portable lifecycle fixture now observes equipped ownership before Drop.
- Private recipe tests recorded missing-API compile RED, then72/72 GREEN. The50 positive cases pin exact identity, slot and piece count; negative cases cover foreign part/physics/body/inventory references, missing actual slot, carried state, natural/unknown identity, current forged assembly and no mutation. This proves core recipe selection, not native attachment or visual fit.
- The initial `SpreadEquipmentIntegrationTests` had61 native cases; the reviewed expansion has71. It compiles against actual Unity references. It compares exact persistent Mesh references and the renderer-owned material with the approved palette; it does not accept generic nonempty geometry as model coverage. Root ran the initial55-failure/6-control RED and the final native GREEN.

## Native acceptance still required

Still required: viewed fit captures for adult/child/tall-hat/Marlback bodies, actual glove/tool motion and clipping, whole-biome exact equipped-style census, and gameplay screenshots with starter/earned gear. Native assertions already cover all50 concrete forms, current owners/meshes/palettes, real NPC and Marlback loadouts, paired attachments, actual both-boot Walk motion, limb/removal/hide controls, save replacement and receiving-biome reversal. Geometry and ownership do not establish appearance or fit.

## Published candidate and evidence

The61-case pre-implementation native fixture ran:55 intended failures and6 controls passed (`Integration/native-spread-equipment61-red`). Every concrete50-item positive failed; three additional malformed foreign Physics/Equippable/Render cases exposed the legacy view's missing backlink checks. The reviewed implementation was published in the root-authorized window after exact before-hash validation; native adoption and acceptance remain pending.

The candidate reuses the exact38 held portable forms and authors12 fitted wearable forms (174 cuboids,4,176 vertices total), with open-neck armor, hats/helmet, one boot/glove form instanced on both actual limbs, and cloaks. It uses the existing42-cell portable material/palette. The source gallery has been inspected; it proves source form/color distinctions only, not in-game fit. Tall authored hats, child proportions and the original low Marlback bodies require actual viewed native frames before visual closure.

`SpreadEquipmentRig` derives owned-instance anchors from the actual nine-bone imported bindposes. Current animation pose does not become the fit reference. The one-bone persistent wearable meshes follow actual anchor matrices under a single logical item's root; no source skeleton, prefab, equipped item, body part, statistic or save field is changed. Held tools retain their exact persistent portable meshes with an explicit grip transform. Scope is passed from the current receiving renderer, preserving the legacy view outside Spread.

The presenter gains `TryGetApprovedEquipmentStyle(actor,item,out evidence)`: all current pieces must match exact adopted mesh, current native item identity/attachment key, real owned target bones, visibility and renderer-owned palette, including property-block overrides. The returned evidence is one representative pair after all pieces pass. Missing or legacy items return named failures.

Independent review caught two concrete defects: a lateral quadruped Feet slot could claim a humanoid pair (actual1 RED/87 controls), and a current item moved to the other hand could report stale geometry before Refresh. Pair recipes now require one nonlateral root Feet/Handwear slot; a current attachment key covers occupied hand and surviving glove sides, and both the evidence query and cache reuse compare it. Glove/weapon occupancy remains separate. The importer also refuses compilation and unimported foreign output files; it builds cube buffers directly without creating an active-scene primitive.

Private pure checks are89/89 GREEN. Full runtime, editor and expanded native-fixture reference compiles report zero errors. The native set is71 integration+7 library+74 recipe+15 source cases (167 total). Root ran the missing-library RED, adopted12 meshes+one library under the owned folder, and passed the complete167-case equipment set after the test-root correction recorded below. Equipped-body visual inspection remains open. The original tortoise synthetic-gear counter remains an explicit unsupported anatomy case and does not count as generated coverage closure.

## Source publication checkpoint

The exact26-file candidate is published. The presenter was rebased onto the current terrain refinement and retains main-thread MaterialPropertyBlock allocation/cleanup. All three actual-reference compiles are clear after including the new shared terrain helper in the private compiler input list. `Verification/DensityCompletion/SpreadBiome/Equipment/publication.json` records every before/after hash. No native fitted assets were generated and no Unity tests or imports were launched by the publishing agent. Assets are frozen for the root-owned missing-library RED/import/GREEN gate.

## First actual native equipment run

Root imported all12 wearable meshes plus the library (4,176 vertices). The combined424 native run passed423 and failed only the boot Walk fixture; the other166 equipment cases passed. Source inspection found the test sampled the outer ring-player prefab wrapper, while actual FBX animation curves belong to its nested Animator object. The corrected test samples that object and strengthens the check to both actual leg rotations and both boots’ world-space baked vertices, with repeated-same-pose zero-motion controls and exact persistent mesh/target references. This one test-only correction is published; native re-run remains pending. No production animation was changed.

## Native equipment logic gate complete; visual fit remains open

The root-owned native rerun passed all167 equipment cases, including actual imported Walk motion of both legs and both boot meshes plus the same-pose stationary control. Receipt: `Verification/DensityCompletion/Integration/native-gear167-green-fauna-scenery81-red` (equipment167 GREEN; the separately scoped81 art tests contain their intended/premise RED and are not an overall GREEN claim). No runtime animation repair was needed. Actual garment fit, tall-hat intersections, child proportions and Marlback clipping remain pending the six-body close-up gallery and viewed in-game captures.

## First fit captures and diagnostic repair

Player and VillageChild produced all five actual-camera frames. Root and the equipment agent viewed their front captures; root also viewed Walk/back captures and found the fitted forms readable. Four other actors stopped at native equip because the fixture added a duplicate of an authored loadout item left carried by CleanGear. Eight actual core controls confirm normal stack merging consumes that new reference; the corrected art fixture reuses the existing carried specimen and observes real split-unit identity where needed. No anatomy, actor or equip rules changed.

The original-enemy audit’s helper recorded2 positive REDs/4 passing controls: it assumed exactly one descendant skin and the old unpainted body. Its owner-aware repair now accepts only the exact current adopted body after excluding real currently approved equipped-view subtrees; unknown/copied extras remain refusals. The original moth retains explicit exact prior fine-baked source proof, with no new-palette claim. The audit records the actual managed Spread starting zone before validation, accommodating the deliberate new glade start. These two narrow fixture/harness corrections are published; native rerun and the other four bodies’ viewed fit remain pending.

## Native fit and old-audit identity rerun

All12 fit/helper cases now pass in the actual editor (`ef48e0d774504985ac9796938e38a2f4`, within the root21-case run). All six bodies produced their five capture frames; all old-body positive/negative controls pass. Root’s four-zone exact-style census reports zero equipped-item style failures. Root inspection of the remaining four bodies and wider whole-biome coverage remains in progress; no complete-biome or unviewed-fit claim follows from this bounded result.

## Viewed fit defects and read-only measurements

The numeric12-case fit/helper gate did not close visual fit. Root viewed the remaining four bodies: the MarlbackBreacher’s armor and helmet are mostly buried in its low shale-backed body, while Farmer/Scribe/Elder authored crowns protrude through worn headgear. These are real presentation defects; Player and VillageChild remain the viewed positive controls. No anatomy or equipment rules will be changed to conceal the problem.

The six-case `SpreadEquipmentFitMeasurementTests` diagnostic is now published with its exact two-path receipt (`Verification/DensityCompletion/SpreadBiome/Equipment/measurement-publication.json`). It records actual persistent body vertices grouped by imported bone, bind-space sockets and currently equipped garment bounds. It makes no fit changes. Root will run this before a test-first fit correction. A horizontal Marlback cuirass fit and scoped cosmetic-headwear covering are hypotheses awaiting those measurements, not completed behavior.

## Measured Marlback correction: RED retained

The revised six native measurements compare the exact one-bone skinning equation with both BakeMesh overloads. `useScale:true` agrees with boneWorld × bindpose × vertex; default BakeMesh had an extra root-scale factor in the original diagnostic. Actual canonical Marlback torso bounds extend to±0.5625 horizontally and0.9525 high; current armor only reaches±0.45 and0.6256. The current helmet is±0.2776 against the actual head’s±0.375.

Six new native fit cases executed: four expected torso/head clearance failures across Breacher/Scrabbler, two exact human-anchor controls passed. A private single-source repair derives bounds from actual imported bind vertices, rotates the shared cuirass into a horizontal shell, and fits the broad helmet while retaining its original vertical face clearance. Held, back, feet, gloves and all human anchors remain unchanged. Native rerun and viewed frames are required before acceptance; headwear cover remains a separate test-first slice.

## Measured fit GREEN; cosmetic cover RED and private candidate

The actual root-owned native run `70cbf2e710f844f59ee46338123a35b4` passed all six Marlback/human fit cases after the narrow bind-bounds repair. The same run reproduced nine cosmetic-headwear feature failures while all six negative/control cases passed: missing authored markers, real equipped-owner cover, and actual forward/shadow/depth fragment visibility. Receipt: `Integration/native-calm37-fit6-green-headwear9-visitor13-red`. This is measurement/behavior evidence; new equipped images still determine visual acceptance.

The reviewed private cover candidate adds explicit UV2 bits only to cosmetic crowns/brims/caps/hoods/goggles on the existing52 owned humanoid meshes. Face, eyes, ears, body and long rear hair remain unmarked. Exact current equipped Head ownership and the existing persistent gear style proof control one default-zero property on the owned body; both property-block layers preserve existing lighting/fog values. All three actual shader passes share the same cosmetic discard. Original source rigs/materials, native anatomy and equip rules are unchanged. Renderer peer review found no concrete blocker, and actual-reference compiles are clear. Native import/GREEN and viewed fit remain pending; no shared production publication is implied by this private checkpoint.

## Native cosmetic cover and seven-body gallery gate

Root review caught an indexed-property-block bug before publication: creating a cover-only indexed block would replace the native renderer-level visibility/lighting block. The repair updates only existing nonempty indexed overrides; an absent override stays absent. A new real-owner test explicitly clears that override, retains renderer-level transient/sentinel values and verifies actual equip leaves the index absent. The old candidate and source-review counterexample were preserved; this was not mislabeled as an executed native RED.

Root imported all52 scoped humanoid outputs. The saved before/after hashes prove every mesh’s shape, UV0, weights, bindposes and topology remained identical; only explicit cosmetic UV2 metadata was added. The actual native run `71b3489b15ed4836b61fb450c3833321` passed headwear16, fit6 and gallery7 (29/29), producing35 equipped native camera frames. Receipts: `Equipment/headwear-shape-integrity.json`, `Equipment/headwear-owned-current.json`, and `Integration/native-headwear16-fit6-gallery7-green`. Both Marlback sizes and five representative human bodies are included. Root’s current frame review remains the visual acceptance gate; numerical success is not a claim that every garment is aesthetically correct.
