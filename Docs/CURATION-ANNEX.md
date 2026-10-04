# Marrowstye: the continuing inspection

Status: initial annex implemented and reviewed, 2026-10-03. The subsequent gallery/combat iteration is documented in [GALLERY-AND-TACTICAL-COMBAT.md](GALLERY-AND-TACTICAL-COMBAT.md); it splits recovery stock and adds fixed tendril commitments. Historical evidence below describes the initial annex.

Initial status: Native containment passed; combat gameplay passed with the existing attack-pose capture limitation retained. This is the chosen content avenue after the user asked for a substantial, coherent addition rather than more small disconnected mechanics.

## Player outcome and scope

Rebuild Marrowstye's disused wing into a sizeable optional destination within the existing Curation settlement, one zone east and two south of the starting glade (`Overworld.12.12.0`). Preserve the receiving-yard cargo, courier, filing, keys and ink service. Replace the six-cell cage with an occupied inspection gallery, separate holding chamber, and masonry-screened service passage. Useful supplies are actual finite container contents. Recovery can involve combat, a risky dash, or preparing doors and relocating the living threat. Existing door state, repair payment, inventory, AI and saved entity positions determine the result.

This is original Caves of Ooo content. The Qud reference is optional exploration with spatial solutions, readable hazards and useful finds; no claim of copying Qud source or exact mechanical parity.

The half-set is a deceased, incompletely cured subject occupied by Bloom. Containment does not restore the person or cure Bloom. Ordinary preserved bodies remain inert. Mainline Curation does not preserve unwilling living people. Root `Lore/` is authoritative; the older `Docs/Lore/` Curation account is incompatible.

## Verified source sweep and corrections

| Premise | Verified correction and decision |
| --- | --- |
| The current wing already supports exploration | `MarrowstyeCompositionPlan` provides a 19×6 room; `CurationReceivingBuilder` encloses only 3×2 usable cells. Enlarge the wing to 27×9 at Y15, away from the central public route. |
| Quarantine rails conceal or seal a passage | Rails only block movement. Use existing sandstone masonry to screen the service passage. |
| An unlocked copy of the inspection gate works | Its authored Physics is solid until real unlocking. New unlocked doors must use `Physics.Solid=false`; `DoorPart.IsClosed` supplies their obstruction. |
| A broken gate needs a new repair mechanic | `RepairablePart`, `CompositionPart` and `timber-gate-frame` already implement two-timber repair and disable door operation until paid. Reuse them. |
| Closing a gate can trap something standing in its threshold | `DoorPart` rejects occupied thresholds, including loose items and gas. Provide room to retreat and keep gate cells clear. |
| The subject can be hauled to safety | Creature hauling is deliberately refused. Use ordinary pursuit and doors; do not invent a rescue command. |
| Returning needs an annex-completed flag | `CurationIntakePart.Retain` retains the real finite settlement graph. Stock, repaired faults, deaths, positions and door states already persist. |
| A new abstract service reward is needed | Ivrin's actual presence enables the existing ink desk; releasing the hostile can physically threaten staff. Recoverable pulp/resin have an immediate existing use. |
| Medical stock is named FieldDressing | The actual existing item is `SoddenFieldDressing`; imported emergency stock is identified as such. |

Read before implementation: `CLAUDE.md`, `Docs/CURATION-RECEIVING-YARD.md`, `Docs/RELEASE-VISION.md`, root Curation/Archive/Bloom lore and voice cards; composition plan/builder, receiving builder/intake, doors, repair part/recipe/composition, Curation art library/source, native Curation scenario, receiving tests and current blueprints.

## Authored plan

Relative to the expanded disused wing: public vestibule X1–8; gallery X10–19/Y1–5; holding X21–25/Y1–5; screened service passage at Y7. Masonry partitions at X9 and X20, and along Y6. Public north entrance moves to the vestibule. Existing locked inspection gate at (9,2); unlocked transfer gate at (20,3); closed gallery service gate at (15,6); broken-open holding escape gate at (23,6). Ordinary arrival remains safe because the gallery and transfer entrances are closed.

Place the existing half-set in the gallery. A safe maintenance rack supplies two timber lengths. The gallery contains an inspection slab, old restraint rail, and finite supplies: two sootroot pulp, one pitchpod resin, two fire clay, one imported dressing and protective gloves. The first implementation used one cabinet; the subsequent iteration separates the botanicals into a deeper conservation case. The existing nearby ink service gives the botanical stock an immediate purpose. A placard and staff dialogue explain the real layout, repair and risk without map icons, quest flags or an automatic solution.

Create matching voxel forms for the new working furniture, placard and visibly broken service gate; reuse compatible existing healthy gate and actor art. No new animation system is needed for existing pursuit and door actions.

## Implementation sequence and acceptance

1. Observe focused failing tests for expanded real generation, useful finite stock, repairable door behavior, document wiring and art selection.
2. Implement layout/content/art through existing systems. Verify ordinary arrival safety, distinct direct and bypass routes, transfer and escape thresholds, and current public services.
3. Exercise both successful and refused repairs, occupied-door refusal, stock depletion, saved/reloaded owners and replay refusal. Counter-check missing required content and unrelated public route integrity.
4. Run affected native Unity EditMode suites. Reuse the existing native Curation player scenario for actual generated-site input, NPC turns and save/load; inspect screenshots. Do not substitute a test-runner result for a visual or playability claim.
5. Cold review Q1–Q4 and fix significant findings. Record implementation, evidence, limitations and changed files here in the same commit. Fetch/rebase and push the completed change to main.

## Readiness and exclusions

🟢 Existing AI, doors, structural repair, containers, art pipeline, save ownership and public services cover the intended interactions.

🟢 Expanded geometry, useful stock and new art are implemented. The real generated-site containment route passed through native input and ordinary NPC turns.

⚪ No preservation chemistry, Catcher quest arc, conscious-body rescue, automatic morality/reputation reward, restocking or offscreen slaughter. Existing cached settlements keep their previous layout; new generation gets the annex. Migration is not invented for this pre-release content change.

## Implementation and review

Implemented seven new blueprints, the expanded physical plan, four real door placements, finite source-owned stock, three conversations and the maintenance record. Existing 677 blueprint definitions remain semantically identical. The original receiving-key/tool reward is separate from the recovery cabinet. Every new entity uses existing Parts; there is no new quest, saved completion flag, capture command or reward service.

Five new voxel models extend the existing Curation library from 15 to 20 forms. The original 15 source definitions and palette are unchanged. Healthy doors reuse existing gate models; repair visibly replaces the broken-open frame with the ordinary working gate state. Imported receipt: 211 cuboids / 2,532 triangles across the complete pack. Static racks/cabinets do not visually count their inventory; their real inventory is authoritative.

### Self-review

- 🟡 Fixed: new unlocked gates must not retain the keyed gate's solid Physics flag. They begin non-solid, with ordinary closed-door obstruction.
- 🟡 Fixed: the late builder's flood check must count closed DoorPart state, not only Physics.Solid; only the inspection gallery may become inaccessible.
- 🟡 Fixed: maintenance instructions originally led through the occupied gallery too early. They now give the safe vestibule/passage approach and the transfer-then-service closure order.
- 🔵 Fixed integration: renamed two shadowing locals before final compilation; a partially written native scenario briefly blocked reload. A test invocation against the last valid assembly is retained as an explicitly stale intermediate report, not counted as final verification.
- ⚪ Existing cached settlements keep their saved geometry. No migration, refill or regeneration was added.
- 🧪 Unit containment/save fixtures stage the same subject in holding to test boundaries. Only the separate native-input scenario can prove actual pursuit and the playable transfer.
- 🧪 Deferred: the optional combat observer again failed to capture a qualifying attack pose. This is the same documented limitation as `CURATION-RECEIVING-YARD.md`'s final follow-up, not a demonstrated new gameplay defect. All combat gameplay checks passed. No speculative animation change or retry-until-green was made.

Independent Q1–Q4 source review is in `Verification/CurationAnnex/review.md`. The review confirms lifecycle symmetry, cross-system state ownership, counter-checks and lore consistency. The old keyed inspection gate was rechecked: actual menu/bump unlocking clears Physics.Solid; no change to its blueprint was necessary.

### Evidence

- Initial native RED: 48 cases, 34 failed / 14 passed. This includes an initially proposed rail-axis check subsequently pruned because the annex uses one interior fitting, not an enclosing rail perimeter.
- Source art: 5 tests, observed RED then 5/5 GREEN; exact-source Blender views visually inspected.
- Final affected Unity EditMode sweep: **301/301 passed**, including all 37 new cases. Raw `Verification/CurationAnnex/affected-green.xml`.
- Native-input containment: **20/20 passed**, zero unexpected errors, in `Verification/CurationAnnex/Native/a1048617f69448e6b6d69946c880cf32/report.json`. This includes the original public filing, hauling, finite tool stock and key route, followed by the new annex. The player read the real placard, took two timber lengths, paid for the service-frame repair, approached the roaming subject, lured it through the transfer gate, closed that gate and escaped through the service gate. The subject retained its actual ID and all 14 HP; the player finished at 37/40 HP. Recovery stock depleted exactly; twelve further ordinary waits retained containment; native F5/F6 restored the graph, positions, HP, repaired gate, closed doors and empty containers.
- Three intermediate failed native routes remain recorded: an old approach helper automatically opened the inspection gate; waiting beyond sight did not attract the roaming subject; an unnecessarily long escape let the subject enter the service threshold. Corrected the test player's actual approach/route. Gameplay AI, speed, door refusal and damage rules remain unchanged.
- Optional combat: **20/21 checks passed**, zero unexpected errors, in `Verification/CurationReceiving/Native/ebc20e73454f43a18b3794fa8b433bd6/report.json`. All gameplay checks passed: actual earned-key opening, hostile attack attempt, player attack/damage, death/removal and surviving normal-play finish. The ordinary player used one existing Rime Grip and the earned salt rake, finishing at 40/40 HP without tonic use. Only `curation_actual_attack_pose` failed, so the raw scenario correctly reports `complete=false`. An initial obsolete entrance-wait route failed before ordinary approach into the enlarged gallery was added; its report is retained too.

Native reports distinguish script-observable results from visual readability, discovery and balance. The successful run uses one disclosed travel shortcut from the glade to generated Marrowstye; all local actions use actual input, without grants, creature relocation, damage calls or AI changes. Native screenshots of repair, holding and restored aftermath were visually inspected. Native persistence is F5/F6, not a walk across a world border; explicit unload/revisit is covered by the separate saved-zone fixture. This is one ordinary build/seed and does not prove universal balance or spontaneous discovery.

### Finding it in play

On a new game, or before Marrowstye has been generated in a save, travel one zone east and two south from the starting glade. Enter the southwestern disused wing through its northern vestibule. The notice and maintenance rack are accessible before opening the occupied gallery. Repair the service exit first if attempting containment; close the transfer gate from its southern side and take the direct exit, keeping the pursuer behind you. Stock and layout do not refresh in an already-cached settlement.

### Files

Gameplay/content: `MarrowstyeCompositionPlan`, `CurationReceivingBuilder`, the seven added Objects.json blocks, FriendlyNPCs.json and CodexDocuments.json. Art: existing Curation source/library/builder plus five meshes/prefabs. Verification: dedicated annex generation/art cases, updated receiving/composition/document pins, and an extension of the existing isolated native Curation scenario. No core combat, AI, repair, door, inventory or save rules were changed.
