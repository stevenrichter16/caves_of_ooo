# Machine foundations and emergent world systems

Status: M1a direct non-water flask transfer accepted through native checks, ordinary play and independent review, 2026-11-16. Implementation is authorized by the user's direct instruction. This is original Caves of Ooo design, not a Qud parity claim. None of the twelve complete machines is accepted.

The purpose is to build the world conditions, useful materials, actions and consequences that give twelve proposed machines a reason to exist. Machinery should deepen exploration, local problem solving and relationships with places. A machine is a way to act on shared world rules; a menu containing isolated recipes is insufficient. Working installations must be useful before home ownership. Construction and relocation can later let players specialize several bases without making base ownership compulsory.

The graphics automation is cancelled. This program supersedes earlier graphics-first and machine-design-only directions. Existing graphics and required builds remain preserved. The unfinished Cinderhold forge presentation is parked separately and is not an accepted prerequisite.

## Design principles

1. Establish the opportunity before the apparatus. Every proposed machine needs a specific resource, problem, alternative solution, cost, and observable outcome. Do not degrade ordinary movement, crafting or recovery to manufacture demand.
2. Create shared causal rules. A liquid should retain its relevant properties when collected, processed, spilled, carried or used outdoors. Mechanical work, temperature and contamination must have explicit boundaries; a label is not a resource supply.
3. Keep distinct resources distinct. Material identity, structural composition, reagent potency, physical quantities, fuel, electrical charge, living cultures and social obligations are different concepts. Use deliberate conversions with finite inputs rather than silently treating one number as another.
4. Give the player meaningful choices at a few points. Destination, yield, preservation, access and transport matter more than frequent maintenance clicks. A safely stopped machine should stay safely stopped.
5. Make consequences legible. Inspect and action previews explain requirements, output capacity, waste destination, danger and blocked conditions. World objects and actual services expose the same truth as the menus.
6. Make growth earn its complexity. Each foundation must support a useful ordinary-world action and a demonstrated interaction with another system. Broader machinery follows accepted playable increments; a universal factory framework is not the opening milestone.

## Lore authority and regional identity

Follow [the authority rule](../Lore/README.md), [the Bible](../Lore/10_Bible.md), [Second Spine](../Lore/11_SecondSpine.md) and [Mystery Ledger](../Lore/MYSTERY-LEDGER.md). The root `Lore/` corpus governs. `Docs/Lore/` and some older design documents contain an alternate world and cannot establish current canon. Phase-history terminology is subordinate to later amendments and `Lore/TERMS.md`.

Regional working traditions provide the machinery's character:

- Concord workshops and Cinderhold support weighed materials, repairs, salvage, packable equipment and transport agreements. Access, salvage rights and ownership should be understandable; a communal fixture is not abandoned merely because no NPC stands beside it.
- Salt, brine and preservation support bounded processing and treatment around Wellmeet and the Salt-Vault. Salt has practical, economic and funerary uses. Preserve the distinction between treating a consenting living person, preserving a specimen and the cultures' treatment of bodies.
- Lampwell and the catacomb villages support tended cultures, lantern-beetle colonies, optical equipment and living light. Acquire stock through legitimate trade or a disclosed choice. Hearth plumes and defensive plantings are not free industrial feedstock.
- Glow-Quartz amplifies bio-light in the material canon. It is not an established universal battery. A power system needs its own reviewed source, storage and consumer rules.
- Memory-Marble, Mute-Stone and Tepuibone hold names; Choir-Iron, Pale-Salt and Spore-Block serve a different defensive palette. Do not collapse them into one generic ward statistic. Machinery cannot automate the Tent-Right's enacted hospitality oath.
- Instruments may recover physical traces and bounded practical knowledge. They cannot prove protected cosmological answers, reveal live hidden actors, reconstruct a definitive previous world, or give Urqu intention. The earlier echo-loom brainstorm is outside the twelve-machine commitment and remains a separate, unapproved concept.

Sources: [material culture](../Lore/History/08_MaterialCulture.md), [Concord](../Lore/Factions/04_SaccharineConcord.md), [catacomb economy](../catacomb_village_design.md), [underreading](../Lore/Design/UNDERREADING.md). Specific new recipes, names and dialogue require their own canon and voice check.

## Verified foundations and missing contracts

This is a source audit, not a new gameplay qualification. Older document status labels can be stale.

| Area | Executable foundation | What must be designed or added |
| --- | --- | --- |
| Liquids | `LiquidVesselPart`, `LiquidVesselService`, `LiquidPoolPart`: single identity, finite volume/capacity, current-owner checks and transfers. Incompatible mixtures are refused. | Explicit feed composition where needed, supported separation recipes, receivers, waste and conservation. Ordinary pouring must retain its current rules until intentionally extended. |
| Thin films and coatings | `FiniteLiquidFilmPart` tracks finite contacts, not collectible liquid volume. Tile coatings are exposure/hazard state. | No machine may turn these counters into drams. A new physical transfer needs an authored quantity and unit conversion. |
| Heat and fuel | `FuelPart`, `ThermalPart`, thermal events and tile hazard channels exist. | Bounded mechanical work/energy budgets, heat exchange and captured products. Vaporization currently does not create collectible condensate. Tile charge is not battery capacity. |
| Materials | `MaterialPart` describes reactive identity. `CompositionPart` describes repair categories. | Recoverable constituent quantities, component provenance and allowed substitutions. Neither tags nor composition categories authorize arbitrary salvage outputs. Existing mixed fractional/percentage property authoring must not be silently normalized as part of this program. |
| Crafting and salvage | `WeaponAssemblyPart` records components; `WeaponSalvagePart` returns the recorded head and haft, losing binding/improvements. Tinkering bits form a separate economy. | General recovered machinery, discrete components, restoration choices and physical output accounting. Preserve existing pack crafting and weapon salvage. |
| Preparation | `PreparationRecipePart` already shapes components/bandages, concentrates mendleaf and detoxifies GroveRed at a still. `BrewingService` combines reagent properties with transactions. | Liquid fractionation is new. Do not reimplement existing preparation as a new milestone or treat MAX reagent potency as conserved mixture mass. |
| Repairs | `RepairablePart` and repair recipes support opt-in faults, payment and saved restored function. | Bounded machine faults, meaningful substitutions and calibration where justified. No blanket combat-HP repair, resurrection or retroactive damage to existing sites. |
| Handling | `HandlingPart`, `DragRules`, hauling links and `ContainerLoadPart` support weight and spatial following. | Anchored pulling, safe machine disconnection/installation and cross-zone cargo policies. Reuse current weight, contents and cycle protections. |
| Jobs | `KitchenBatchPart` has real escrow, due ticks, blocking and identified outputs. | Reuse its transaction/revalidation approach without inheriting kitchen-specific actors/sites. Define scheduling, interruption and unloaded-zone reconciliation explicitly. |
| Information and relationships | Existing Examine, discoveries, traders, requests and reputation provide entry points. | Sample provenance, bounded assay answers, source-limited survey reports, actual delivery jobs and explicit machine-use/salvage permission. |

The twelve machines are not all ready because individual foundations exist. Content, player actions, combinations, persistence, readable presentation and meaningful balance remain separate acceptance gates.

## Dependency matrix for the twelve machines

All machines remain proposed. M1a supplies single-liquid handling only; it does not supply processing, mixture/provenance, pumping or elapsed work. See [its exact contract and evidence](MACHINE-LIQUID-HANDLING.md).

| Machine | Why it would be useful | Missing support and meaningful choice | First evidence required | Status |
| --- | --- | --- | --- | --- |
| Forge and reclamation rig | Recover useful workmanship from finite damaged finds. | Physical recoverable parts/quantities and irreversible salvage choices; preserve an assembly versus obtain repair stock. Existing weapon salvage is a foundation. | Two genuinely useful competing outputs, one source consumed once, both usable outside the new rig. | Proposed; machine-specific prerequisites open. |
| Fractionating still | Turn a difficult local feed into a useful liquid and retained residue. | Known feed accounting, bounded processing, fuel/work, output and waste receivers. Existing reagent still is not fractionation. | Source-to-receiver accounting, real use of the result, blocked/interrupted/reloaded jobs and waste handling. | Proposed; M1a liquid handling reviewed separately; process prerequisites open. |
| Restoration cradle | Recover the particular function of damaged equipment. | Persistent component slots, explicit compatibility, faults and tested substitution tradeoffs. | Restored apparatus performs its real action; alternate repair changes a legible capability, not merely item flavor. | Proposed; machine-specific prerequisites open. |
| Assay cabinet | Spend evidence to make a practical decision. | Samples with quantity/provenance, authored questions and partial findings. | An answer changes a processing, repair or route choice; missing evidence and hidden knowledge remain unavailable. | Proposed; machine-specific prerequisites open. |
| Culture vat | Grow a finite useful culture from acquired stock. | Strain identity, feedstock, bounded growth, conditions and yield allocation. | Two competing uses for one finite culture, different supported conditions, consent/ownership and persistence. | Proposed; M1a liquid handling reviewed separately; process prerequisites open. |
| Anchored capstan | Recover or reposition loads ordinary carrying cannot handle. | Valid anchors, cable/pull paths, work budget and installed-versus-loose state. | Actual obstruction, terrain and existing cargo weight affect pulling; movement and interruption remain coherent. | Proposed; machine-specific prerequisites open. |
| Sump pump | Reach submerged resources or redirect a local liquid supply. | Finite source/receiver transfer, level/state thresholds, compatible fittings and outlet consequences. | A real passage or resource changes access; transferred liquid appears at a valid destination with no regeneration exploit. | Proposed; M1a liquid handling reviewed separately; process prerequisites open. |
| Thermal exchanger | Use a local source for drying, cooling or processing. | Explicit source/receiver coupling, work limits and bounded local temperature effects. | Useful cooling/heating and a second consequence share existing thermal rules; unsupported sources do not create energy. | Proposed; machine-specific prerequisites open. |
| Dynamo and charging bench | Prepare finite work for portable equipment. | Authored energy source, storage loss/capacity and at least one real consumer. | Charged equipment does useful work away from the generator; finite conversion and refusal paths are verified. | Proposed; machine-specific prerequisites open. |
| Survey resonator | Find a worthwhile direction or cavity without knowing its exact contents. | Bounded signals, calibration and coarse observations tied to actual generated/authored features. | A lead supports a real expedition; uncertainty is legible, false precision and hidden-actor leaks are absent. | Proposed; machine-specific prerequisites open. |
| Signal relay | Coordinate separated people and owned locations. | Connectivity, observed reports and persistent dispatch/delivery state with genuine cargo custody. | One physical delivery or report follows a real arrangement, survives interruption/reload and cannot duplicate goods. | Proposed; machine-specific prerequisites open. |
| Treatment bath | Prepare for a specific environmental exposure or remove a supported contaminant. | Targeted reagents, eligibility, exposure rules and bounded temporary treatment. | Treatment interacts with a real hazard, has an alternative solution and does not become mandatory general healing. | Proposed; M1a liquid handling reviewed separately; process prerequisites open. |
## First playable increment

The first implemented increment is **M1a safe direct non-water flask transfer**. It lets players split/consolidate finite brine, oil and other single liquids without creating ground exposure, using real carried vessels and the ordinary transaction/action/save paths. Actual play exposed and corrected hidden menu placement and missing F1 contents; this is why native tests alone are insufficient. Full details, exact receipts and limitations are in [MACHINE-LIQUID-HANDLING.md](MACHINE-LIQUID-HANDLING.md).

The initial repairable brine condenser is **withheld**: Wellmeet8.16 already has wells; ordinary BrineLens5.16 travel found a well and cistern alongside brine. No useful conversion problem was demonstrated. Salt-Vault is also unqualified. Do not repeat that broad inventory or remove existing water to justify a still. The following processing contract remains a requirement for a future justified installation, not accepted content.

The complete player loop is: discover a physical fault; choose whether to repair it or use an existing water-supply alternative; obtain finite compatible feed and fuel/work; prepare an appropriate receiving vessel and waste destination; commit one bounded job; obtain the actual output; use that output in an existing ordinary-world action; leave and return to the same changed installation.

Before coding, specify the feed and units, yield, concentration/residue, losses, work cost, capacity and all stop states. Do not infer a mass conversion from the word “brine” or mint trade salt from an unlimited well. The first known-feed recipe may stay opt-in, with ordinary single-liquid vessels unchanged. Recovering liquid is not proof that it is safe to drink; supported contaminants must remain accounted for. Initial residue may be a retained physical waste batch rather than an invented valuable commodity.

Choose explicitly between one authored composition for all supported canonical brine and an opt-in feed that carries its own composition/provenance. Existing fill/pour preserves liquid identity and volume, not arbitrary batch metadata. If provenance is required, define its persistence and every permitted transfer; unsupported transfers must refuse rather than erase contaminants or relabel feed. Demonstrate why recovered output improves an existing action compared with the feed and nearby alternatives. If the location offers equally convenient water and no meaningful placement/resource decision, select a better process instead of manufacturing scarcity.

Reuse existing repair, handling and transaction machinery. Build only the minimum new persistent processing state that this real action needs. An active batch must identify its physical input, installation, work committed, receiver and outputs. Moving/removing a participant or blocking output cannot duplicate, silently destroy or teleport contents. Stopping, cancelling and disconnecting need explicit outcomes before implementation.

The first cross-system demonstration should use recovered liquid through an existing supported action such as extinguishing, cooling or another verified water use. A second setup must expose a meaningful placement/capacity or environmental consequence using existing rules. No new thirst tax or degradation of ordinary wells is required to make an optional remote installation useful.

## Expansion after the first accepted loop

1. Build on accepted finite handling; work rules are still missing. Qualify one pump with a real source, destination and access consequence. Add a narrowly scoped mixture contract only when a second real process needs it.
2. Give salvage and restoration a complete shared component loop. Introduce a capstan through an actual heavy recovery objective, preserving existing carrying and hauling.
3. Extend work and heat to one exchanger and one portable charged consumer. Prove finite conversions; avoid a world-spanning power-grid rewrite.
4. Develop sample provenance and two practical assays alongside culture and treatment. Culture becomes interesting through conditions and competing uses, not waiting for free output.
5. Add bounded survey information and a first physical relay delivery. Build on earned knowledge, actual routes and relationships, then connect multiple installations or homes.

This order can change when source verification or play shows a smaller, more useful dependency. Record the reason and keep every machine's prerequisite status visible. Do not quietly drop a machine or mark it complete because its infrastructure exists.

## Emergence to demonstrate

- A pump changes where finite liquid is held; that changes a real approach, while its outlet affects another supported material or exposure. The result follows the same transfer and hazard rules used elsewhere.
- A finite batch can support expedition supplies or a workshop job. Carrying capacity and local access make processing in place, paying for a service and relocating equipment legitimate alternatives.
- A traded culture produces a limited living-light resource; appropriate optical equipment uses that resource for a specific observation. This chain requires new culture/assay rules and must not consume protected communal plantings by default.
- Preservation and controlled examination can keep an authored record readable; entrusting the actual item to another party creates an ownership/delivery consequence. Observations remain evidence, never automatic cosmic truth.

These are target interactions, not implemented features. Accept each only after actual play demonstrates its causes and limits. Avoid an interaction matrix that exists only in prose or test fixtures.

## Acceptance and balance

For each increment, show the ordinary action, an intelligible alternative, a consequence outside its own menu, an interruption and a fresh save/load. Test conservation and callbacks, invalid custody, removed owners, replaced zones, malformed data, capacity, serial reuse, unavailable supplies and old saves. Keep floors/routes, encounter balance, stock, quest identities and accepted presentation intact unless the reviewed increment explicitly changes them.

One authoritative elapsed-game-time policy must govern new work. Existing travel advances time differently from local actor turns, so define and test that boundary rather than using wall-clock time or assuming every zone simulates. Unloaded work is bounded reconciliation with no invisible hazard simulation. No free refill on re-entry, output repeated on reload, unlimited profit from renewable inputs, surprise home destruction or compulsory maintenance cycle.

Native tests, targeted performance checks, real GameBootstrap actions/readers/travel/reload and independent source/gameplay review are required. Use isolated saves. Create appropriate presentation for changed states and retain honest hardware limits. Machine count, document length, passing helpers or an offline render do not establish completion.

Initial independent source and lore/design reviews found no remaining actionable P1/P2 in this plan. Feed identity, useful conversion and interruption accounting remain explicit M0 decisions before production. This review accepts the design scope; it does not accept any machine's gameplay.

## 2026-11-16 play corrections and current scope

M1a uses existing12-unit flasks, not90-unit flasks:90 was natural brine-pool stock. It creates no job, output recipe, mass conversion, currency, provenance, material generation or autonomous work. Stored source11448 decreased90→78; carried12 became11+1, then0+12, then1+11; the final one was poured through ordinary contact rules. Incompatible water3 and brine11 stay separate even with receiver room. Save data uses the same vessel fields and identities.

A bounded Peat Works17.7 source check found60 bog-mire at34,8, dry access to existing salvage, and a crucial rule: depleted natural pools retain their coating source. Zero collectible volume is not drained footing. A pump remains unqualified until a finite optional sump/resource objective and its depletion/destination consequences are designed and played. New authored opportunities may be appropriate; blanket terrain changes or compulsory upkeep are not.

The next design task is a single recoverable wet-store or sump opportunity with explicit custody, finite source/outlet, manual12-unit bailing alternative, and a meaningful optional recovery/route outcome. Validate the proposed location and canon before implementation; if that loop remains merely fewer clicks, move to a recoverable-component/repair choice rather than inventing scarcity. No larger simulation framework is authorized by a successful transfer action.
