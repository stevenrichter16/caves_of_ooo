# Machines and tools in recurring play

User direction, November22,2026: develop systems of gameplay around the tools
and machines, including their long-term usefulness. This is a proposal and
planning contract. It does not promote partial foundations into complete
machines. Current status and dependencies remain in
[MACHINE-SYSTEMS-DESIGN.md](MACHINE-SYSTEMS-DESIGN.md); specific existing actions
and earlier ideas remain in [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md).
Graphics stays cancelled; capstan/culture implementation stays parked.

## The recurring journey

Explore a place, recognize a useful material or damaged find, choose what to
carry, recover or leave, process it through available skills/services, prepare
equipment for the next destination, then travel with the consequences. New finds,
routes, relationships and process choices supply reasons to repeat this journey.
Ordinary pack crafting, carrying, recovery and NPC services remain useful options.

The same parts and material rules must support both a town service and a portable
or player-installed counterpart where installation is implemented. Town expertise
can save equipment, travel or setup costs; a remote camp can save a return journey.
Future bases become useful because of location, storage, access and the work the
player chooses to do there. Multiple bases must retain real inventories and finite
work accounting. No automatic population production or unattended wall-clock pay.

## Existing footholds and proposed repeated uses

| System | Current bounded foothold | Proposed reason to use it again and meaningful choice |
| --- | --- | --- |
| Field gathering and preparation | Finite mineral harvest, crop gathering, component preparation and selected actual supply-to-use chains are qualified separately. | Choose materials for a destination: sell the find, keep a portable preparation, or carry inputs to a service. Local material properties and equipment needs change the choice. |
| Reclamation forge | Explicit damaged-item manifests and selected reclaim/cold-dressing paths exist. General ore refining is not qualified. | Keep an intact tool, recover its components, restore it, or convert finite stock into useful components. Later finds offer different compositions and losses, with no automatic refund of improvements. |
| Fractionating still | Camp still preparation/settling recipes exist; a complete fractionating machine remains open. | Turn selected regional feedstocks into portable process inputs. Receiver space, finite fuel where applicable, useful fractions and a real waste destination govern batch size. Only supported recipes establish output quality or potability. |
| Restoration cradle | Opt-in repairs and temporary/lasting repair choices exist in separate systems. Cradle remains proposed. | Preserve an item's distinctive improvements when that is worth the material/service cost; compare dismantling, replacement and selling. Avoid compulsory wear introduced solely to feed repair work. |
| Assay cabinet | Examine and information/relationship foundations exist. Cabinet remains proposed. | Use a known sample to make a better material/process choice. Report supported observations and uncertainty, retain sample provenance, and preserve protected mysteries. Knowledge can make later work simpler without making every batch require a new assay. |
| Culture vat — parked | Complete vat cycle remains proposed. | Future finite substrate/conditions produce deliberately chosen batches for particular uses. Preserve ordinary gathering/services; no compulsory daily tending or free propagation. |
| Anchored capstan — parked | Strength, hauling, dismantling and selected relocation already provide alternatives. | Future optional equipment for a useful heavy recovery or relocation job. Anchor, route, load and work limits matter; ordinary movement and dismantling remain practical. |
| Sump pump | A town pump's repair/state has actual evidence; physical drainage remains open. | Future finite transfers could clear access or recover process liquid. Source depletion and receiving capacity must cause visible changes; relocating or containing water is a choice with a real destination. |
| Thermal exchanger | Heat/fuel/status/proofing foundations exist; full exchange cycle remains open. | Future controlled heat transfer could support several processes at one site. Compare fuel use, available natural heat and portable preparation. Thermal protection does not imply solar/dehydration protection. |
| Dynamo and charging bench | Full mechanical-work/storage/charging cycle remains open. | Future mechanical inputs feed bounded storage used by specific devices. Preview useful work per charge and losses; retain a practical unpowered/service option. Glow-quartz is not an assumed general battery. |
| Survey resonator | The sounding rod has an actual noise-to-creature-response chain; complete resonator remains open. | Revisit unfamiliar geometry, plan a route or deliberately create audible disturbance. Reports stay within the device's supported evidence; creatures and people can react to the same noise. |
| Signal relay and freight | Actual Concord carriage sends finite goods between two posts, waits game time and returns the same goods. A relay, player-base delivery and broader network remain proposed. | Future requests/reports can help plan real transport and local work. Choose freight fees/delay versus carrying and travel; distinguish transmitting information from transporting matter. |
| Treatment bath | Selected finite temporary/permanent proofing, wear/unwear and removal paths are qualified. A complete bath remains open. | Prepare particular equipment for an expedition, choose which garment or material to treat, and revisit when the destination or equipment changes. Disclose compatibility, cost and duration; lasting treatments need no invented upkeep. |
| Camp hearth, shade, storage and traps | Cooking, selected field processing, awning protection, storage/placement and individual trap chains have bounded evidence. None establishes universal camp safety or autonomous survival. | Establish a useful stop near work, dry/prepare/store supplies, choose a route and move camp when useful. Protect NPCs through supported behavior before exposure enrollment. Traps spend actual bait/charges/materials and interact with ordinary creatures and hazards. |

These rows are choices within shared systems, not mandatory consecutive steps.
For example, assay must not become a compulsory click before every reclaim, and
freight must not be required for ordinary carried crafting. No chain may create
a purchase/process/resale loop that generates unlimited profit or stock.

## A useful increment must answer these questions

Record the player problem and at least two practical approaches. Identify finite
inputs, capacities, provenance, outputs and waste; show what the player can know
before committing. Explain why another use becomes worthwhile later: a different
find, environment, route, task or equipment choice. State the cost of leaving,
cancelling, moving the apparatus or choosing the NPC service.

Demonstrate a shared capability outside the apparatus and a consequence in another
system, using actual normal input. Save a partial state, reload it freshly, and
verify the same quantities, custody, remaining work and visible state. Repeat
use must consume/produce the correct finite stock; travel and reload cannot reset
capacity, charges, rewards or progress. One-time installations can remain one-time
if their lasting world change is the purpose; do not invent repeat rewards.

Use a concrete trip to evaluate usability: find a material, locate the right item
in the pack, prepare/use it, decide what to carry or send, then use the result on
the next leg. Record friction as well as success. The denser inventory screen is
a direct support system for this journey.

## Next player-facing priority: readable inventory density

Follow [INVENTORY-DENSITY-REDESIGN-PLAN.md](INVENTORY-DENSITY-REDESIGN-PLAN.md).
The first layout increment now qualifies44 real entries at1280×800 and66 at1920×1080, at the existing readable body size,
with full-height text columns, contextual details and stable item selection.
See the bounded InventoryDensity20261122 receipt; category buttons, additional
state badges and sorting remain open. Use the existing large workshop
pack to check finding a reagent, a charged tool, a treated garment and a camp kit.
The first layout has source/native/readability/action/fresh-reload evidence;
use that receipt without repeating qualified tests. Next distinguish disclosed
liquid contents, charges and treatments in rows/search, retaining exact item
identity, equipment comparisons, full readers and existing commands.

Qualification must include navigation, resizing/search, cancellation and an actual
paid item action, preserving custody and turn costs. This is functional inventory
work with the existing art; it does not resume the cancelled graphics program.
Chronology, the remaining hydration policy/normalization and protective NPC behavior remain separate
required gates before exposure enrollment.

Bounded independent design critique found no material blocker. It corrected the
inventory plan to display each current pack's actual hard limit, including
unlimited and scenario-expanded values. This document supplies the design contract; the linked first-layout receipt
supplies its separate bounded implementation evidence.

## Public state and the next ordinary processing trip

The bounded InventoryPublicState20261122 receipt qualifies finding an empty or
filled flask and a treated garment, then existing finite pour/recovery actions
with a world exposure consequence and exact fresh state. Separate carried-water
UI play shows a paid2->1 charge change and fresh1/3. This helps repeated preparation
without introducing upkeep. The raw workshop mire is fixture stock, not ordinary
acquisition.

Next source/design/native-baseline candidate: collect actual regional mire, carry
it to an existing communal or portable still, choose a useful supported output
(water, oil or fire clay) and use it through another existing system. Establish
the real source/capacity/provenance and alternative carrying/services/finished-stock
choices first; this paragraph ships no recipe or supply. Earlier fixture settling/
cold/oil chains and the bounded finite-poured-fuel correction are already qualified,
so reuse their evidence and do not reopen the closed oil-film P2. Full12dependencies
and acceptance remain unchanged; no passive production or compulsory processing.
