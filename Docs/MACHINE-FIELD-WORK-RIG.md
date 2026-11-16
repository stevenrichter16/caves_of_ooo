# M1b — recoverable field work rig

Status: reviewed design candidate and actual site baseline, **not implemented or accepted**. M1a liquid handling is separately published at `0cf1fbda3`. This increment qualifies recoverable workshop service, not a complete restoration cradle, reclamation forge, capstan or heat system.

## Player purpose and choice

An abandoned field work rig gives a traveller access to existing station-dependent weapon work away from a settlement forge. Keep its useful workmanship, move it a short distance to a chosen working position, or dismantle its two sound timber braces for portable materials. Two timber already brace a gate or dressing bench; individual lengths can become hafts or serve existing trap interactions. Those uses make reclamation more than a sale-price choice.

Restoration costs two existing FireClay to patch the cracked working surface, once. The resulting rig offers the existing reforge, effectful-brew tempering, recorded-weapon reclamation and batch-forging services. Single-weapon pack crafting stays available everywhere. There is no new fuel tax, maintenance timer or forced use. A player who does not need the station can leave it intact or reclaim it immediately.

Restoring, using and later dismantling is valid. Reclamation always yields only the original two braces; spent repair clay is not refunded. Reclamation permanently removes this station and cannot repeat on return or reload. The menu and reader must state the exact yield and loss before commitment. Do not describe the two choices as permanently exclusive.

## Concrete location and authority

Proposed new content: one explicitly abandoned rig at **Peat Works, Overworld.17.7.0, 56,5**, inside the north equipment shed. This is a fresh-generation addition only. It does not replace the locker, the four-timber salvage source, a floor, an actor or a route. Existing visited saves receive no retrofit.

Actual GameBootstrap baseline: earned seed224401440, PeatWorksMemoryCorrection/AfterCheckpoint6420HP40 at40,12; ordinary Right14 Up3 reached54,9, Right2 Up4 reached56,5, then Down1 reached56,6. The normal floor menu and full reader disclose StoneFloor30833 at56,5. Read-only inspection confirms that it is the sole owner on the proposed cell and there are zero ForgePart owners in the zone. F12 remained off; no spawning or gameplay mutation. Saved continuation is `Docs/Verification/MachineSystems/FieldWorkRig/BeforeCheckpoint`, turn6660HP40 at56,6. This existing-world save is a preservation/control baseline, not a way to inject new content into a cached zone.

The site is a plausible provider because it already contains finite working components and lacks a forge. **That is not yet acceptance of the service's usefulness.** Before accepting M1b, ordinarily acquire the needed supplies and demonstrate a worthwhile reforge, temper or recorded-weapon reclamation. Batch convenience alone is insufficient. Preserve the existing locker stock and all other sources. If the demonstrated task is merely making and immediately unmaking an otherwise unwanted weapon, revise the example or the design.

Source integration: `SoddenDistrictPlan.Works` owns this furnishing, with a distinct role, before `SoddenDistrictBuilder` captures detached and final receipts. The cell is already reserved because every shed interior is reserved; do not relax reservations or append a late scatter pass. Validate the rig's exact physical, handling, repair and finite-reclamation payload. New optional-content admission is Works-specific: absent/invalid blueprint data must not make the other districts depend on it. Preserve base-district behavior where the optional content cannot be admitted, and retain full rollback for failed creation. Test the exact final owner set, unchanged floor/locker/old salvage/approaches and literal saved graphs.

Lore anchors: `Lore/10_Bible.md` as amended by `Lore/11_SecondSpine.md`; `Lore/History/08_MaterialCulture.md` on mundane surface work, portable Concord equipment and practical materials; `Lore/Factions/04_SaccharineConcord.md` on delegated routine contracts. Reader text must expressly relinquish the rig: an empty building alone grants no salvage right. Do not claim a new faction reward, a supernatural origin or a protected mystery answer.

## Minimum mechanical contract

- Weight120, solid, noncarryable, nonthrowable, explicit Handling opt-in and no artificial minimum strength above the existing drag rule. STR18 can drag144; pack capacity does not determine single-object lifting. Weaker characters can restore or dismantle the rig in place.
- One known structural fault and one exact FireClay recipe through existing RepairablePart. No new component inventory, durability simulation or combat-HP restoration.
- Reuse the finite HarvestablePart transaction for the two expressly authored braces, including its spent state and hauling rollback. Unsupported/malformed sources, duplicate owners, stale menus, callbacks and capacity failure must not mint or silently erase resources.
- Add a narrow, consistent readiness gate for this recoverable rig. Current ForgePart.IsNearForge accepts a ForgePart without checking its structural fault. The station query, local menu and dispatch must agree about fault, current owner, destruction/reclamation and active hauling. A rig must be released before working. Existing ordinary forges remain available.
- Local hauling uses existing obstruction, terrain, grip and movement-cost rules. No cross-zone transport promise: current zone removal releases the hauling link. No folding/deployment verb is invented; call it a field work rig.
- These are the existing abstract workshop services, not new thermal processing. Current tempering consumes effectful BrewItem mixtures; M1a brine/oil flasks do not become valid quench reagents by analogy.
- One successful deliberate repair, reclamation or workshop action uses the established game-action clock. This slice creates no elapsed job or unloaded work. The processing-time policy remains open for the first actual processing machine.

## Acceptance and adversarial checks

Independent design/source critique favors this over a new wet-store gate: natural MirePool depletion does not create dry footing, and five flask trips alone do not justify a pump. Critique also rejected finished steel blades as arbitrary salvage from a frame and rejected calling the rig folding without a working fold state.

Before production changes, run native RED checks for the actual readiness gap and the exact new content contract, with ordinary-forge and single-pack-craft controls. Cover repair then reclaim, double reclaim, wrong/stale custody, conflicting parts, removed/replaced zones, output overflow, failed callbacks and rollback of an interrupted haul. Exercise the real command/menu boundary rather than helper-only success.

Actual acceptance requires a newly generated rig, readable loss/yield and fault, ordinary acquisition of repair supplies, a short successful haul and blocked/interrupted countercase, release, repair, useful station work, and use of a recovered output outside the station on a separate branch. Travel and fresh Continue must retain identity, position, fault/spent state and exact resources. Compare an older cached Peat Works save to prove it is not repopulated. Independent source and gameplay review must resolve P1/P2 findings. No test receipt substitutes for this play.

Keep native/glyph presentation truthful and reuse approved assets where they fit; the parked Cinderhold graphics draft remains untouched. Package a current Mac at the resulting coherent playable milestone, with real launch/action/travel/save/reload. Until those gates pass, keep this proposal and its baseline separate from accepted M1a.

Current next action: native RED for the missing fault/haul readiness gate, new rig content and fresh-only authored placement, with ordinary-forge/pack-craft and old-save controls. Then implement the smallest shared readiness rule and finite existing-part assembly; do not add a universal machine framework. No M1b production or native test changes have been made yet. The actual baseline is archived at `/private/tmp/coo-machine-field-rig-baseline-root`; Unity was stopped with save-root override null and a clean SampleScene afterward.
