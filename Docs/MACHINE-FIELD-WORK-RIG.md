# M1b — recoverable field work rig

Status: bounded Editor implementation accepted after native checks, ordinary play and independent source/gameplay review, 2026-11-16. Publication and current Mac delivery follow separately. M1a liquid handling is separately published at `0cf1fbda3`. This increment qualifies recoverable workshop service, not a complete restoration cradle, reclamation forge, capstan or heat system.

## Player purpose and choice

An abandoned field work rig gives a traveller access to existing station-dependent weapon work away from a settlement forge. Keep its useful workmanship, move it a short distance to a chosen working position, or dismantle its two sound timber braces for portable materials. Two timber already brace a gate or dressing bench; individual lengths can become hafts or serve existing trap interactions. Those uses make reclamation more than a sale-price choice.

Restoration costs two sound lining measures to patch the cracked working surface, once. Fire clay and prepared lining clay can fill that bill in any mix. The resulting rig offers the existing reforge, effectful-brew tempering, recorded-weapon reclamation and batch-forging services. Single-weapon pack crafting stays available everywhere. A player who does not need the station can leave it intact or reclaim it immediately.

Restoring, using and later dismantling is valid. Reclamation always yields only the original two braces; spent repair clay is not refunded. Reclamation permanently removes this station and cannot repeat on return or reload. The menu and reader must state the exact yield and loss before commitment. Do not describe the two choices as permanently exclusive.

## Concrete location and authority

Implemented fresh-only content: one explicitly abandoned rig at **Peat Works, Overworld.17.7.0, 56,5**, inside the north equipment shed. This is a fresh-generation addition only. It does not replace the locker, the four-timber salvage source, a floor, an actor or a route. Existing visited saves receive no retrofit.

Actual GameBootstrap baseline: earned seed224401440, PeatWorksMemoryCorrection/AfterCheckpoint6420HP40 at40,12; ordinary Right14 Up3 reached54,9, Right2 Up4 reached56,5, then Down1 reached56,6. The normal floor menu and full reader disclose StoneFloor30833 at56,5. Read-only inspection confirms that it is the sole owner on the proposed cell and there are zero ForgePart owners in the zone. F12 remained off; no spawning or gameplay mutation. Saved continuation is `Docs/Verification/MachineSystems/FieldWorkRig/BeforeCheckpoint`, turn6660HP40 at56,6. This existing-world save is a preservation/control baseline, not a way to inject new content into a cached zone.

The site contains finite working components and previously lacked a forge. Ordinary play now demonstrates steel-to-mallet refitting, preserving the returned steel head, followed by a separate reclaimed-timber cutting weapon. Both weapons remain available after fresh Continue. This qualifies useful service and a material connection to ordinary crafting; it does not establish new combat balance or a complete machine. Preserve the existing locker stock and all other sources.

Source integration: `SoddenDistrictPlan.Works` owns this furnishing, with a distinct role, before `SoddenDistrictBuilder` captures detached and final receipts. The cell is already reserved because every shed interior is reserved; do not relax reservations or append a late scatter pass. Validate the rig's exact physical, handling, repair and finite-reclamation payload. New optional-content admission is Works-specific: absent/invalid blueprint data must not make the other districts depend on it. Preserve base-district behavior where the optional content cannot be admitted, and retain full rollback for failed creation. Test the exact final owner set, unchanged floor/locker/old salvage/approaches and literal saved graphs.

Lore anchors: `Lore/10_Bible.md` as amended by `Lore/11_SecondSpine.md`; `Lore/History/08_MaterialCulture.md` on mundane surface work, portable Concord equipment and practical materials; `Lore/Factions/04_SaccharineConcord.md` on delegated routine contracts. Reader text must expressly relinquish the rig: an empty building alone grants no salvage right. Do not claim a new faction reward, a supernatural origin or a protected mystery answer.

## Minimum mechanical contract

### Compatible lining clay at the Peat Works

Roadmap 10. The cracked rig needs two sound lining measures at the
same masonry surface. Fire clay, prepared lining clay and a mixture can
qualify through the existing material matcher. The admission belongs to
this repair bill. The portable workshop's position, ownership, setup,
finite reclaimed parts and saved restored state stay under its existing rules.

The native repair preview and structured Examine fact show the same bill.
The Editor trial carried an earned prepared stack from the wayside cook and
assembled a peat mallet with existing locker stock. The faulted rig refused
the ordinary reclaim action. After repair the same action recovered a head
and haft from the exact mallet. Legacy and mixed grades and unsuitable
stock have native coverage.
[Evidence](Verification/MachineSystems/FieldRigCompatibleClay20261125/README.md)

- Weight120, solid, noncarryable, nonthrowable, explicit Handling opt-in and no artificial minimum strength above the existing drag rule. STR18 can drag144; pack capacity does not determine single-object lifting. Weaker characters can restore or dismantle the rig in place.
- One known structural fault and one exact FireClay recipe through existing RepairablePart. No new component inventory, durability simulation or combat-HP restoration.
- Reuse the finite HarvestablePart transaction for the two expressly authored braces, including its spent state and hauling rollback. Unsupported/malformed sources, duplicate owners, stale menus, callbacks and capacity failure must not mint or silently erase resources.
- The implemented ForgeWorkReceipt and FieldWorkRigRules add a narrow readiness gate for this recoverable rig. The original ForgePart.IsNearForge fault/haul gap was reproduced with native RED checks. The station query, local menu and dispatch must agree about fault, current owner, destruction/reclamation and active hauling. A rig must be released before working. Existing ordinary forges remain available.
- Local hauling uses existing obstruction, terrain, grip and movement-cost rules. No cross-zone transport promise: current zone removal releases the hauling link. No folding/deployment verb is invented; call it a field work rig.
- These are the existing abstract workshop services, not new thermal processing. Current tempering consumes effectful BrewItem mixtures; M1a brine/oil flasks do not become valid quench reagents by analogy.
- One successful deliberate repair, reclamation or workshop action uses the established game-action clock. This slice creates no elapsed job or unloaded work. The processing-time policy remains open for the first actual processing machine.

## Acceptance and adversarial checks

Independent design/source critique favors this over a new wet-store gate: natural MirePool depletion does not create dry footing, and five flask trips alone do not justify a pump. Critique also rejected finished steel blades as arbitrary salvage from a frame and rejected calling the rig folding without a working fold state.

Before production changes, run native RED checks for the actual readiness gap and the exact new content contract, with ordinary-forge and single-pack-craft controls. Cover repair then reclaim, double reclaim, wrong/stale custody, conflicting parts, removed/replaced zones, output overflow, failed callbacks and rollback of an interrupted haul. Exercise the real command/menu boundary rather than helper-only success.

Actual acceptance requires a newly generated rig, readable loss/yield and fault, ordinary acquisition of repair supplies, a short successful haul and blocked/interrupted countercase, release, repair, useful station work, and use of a recovered output outside the station on a separate branch. Travel and fresh Continue must retain identity, position, fault/spent state and exact resources. Compare an older cached Peat Works save to prove it is not repopulated. Independent source and gameplay review must resolve P1/P2 findings. No test receipt substitutes for this play.

Keep native/glyph presentation truthful and reuse approved assets where they fit; the parked Cinderhold graphics draft remains untouched. Package a current Mac at the resulting coherent playable milestone, with real launch/action/travel/save/reload. Until those gates pass, keep this proposal and its baseline separate from accepted M1a.

## Current continuation — 2026-11-16 04:52 UTC

M1b completes its bounded Editor gates on top of main ff80985d3. Native code and content are implemented; source publication and current Mac delivery are the next steps. Root alone owns Unity/Git; the 84 prior dirty tracked files, normal saves/preferences, approved art, parked Cinderhold draft and prior builds remain preserved.

Actual earned seed704860946: buy FireClay4 from Orrit for12; sell watering grimoire11 and buy steel blades2 for44 at Cinderhold, leaving5 drams. AcquisitionCheckpoint3330HP40 at56,7 preserves this ordinary acquisition. Fresh Peat Works contains rig14199 at56,5. Existing locker supplies were acquired normally; one solid-locker bump is disclosed. Single-weapon pack crafting made steel weapon16155 from the purchased blade and existing locker haft/binding. FreshRigCheckpoint3820 and PackWeaponCheckpoint retain the pre-service states.

Service branch: haul120-weight rig from56,5 to56,6, with ordinary movement cost; repair while hauling refused without consuming clay. Release and repair consumed exactly2 FireClay (4→2). Actual station refit changed weapon16155 from steel blade to peat-packing mallet and returned its steel head; two steel heads remained. The mallet was equipped and read normally. ServiceCheckpoint3960HP40 is preserved. Actual review identified an unclear release instruction, absent refit preview and sword icon on the mallet; source fixes now give an explicit release message, full result/consume/return/temper preview and approved mallet icon for the exact owned assembly. Actual corrected icon and reverse-refit preview were reviewed; the reverse preview was cancelled, not executed. Ordinary17.7↔18.7 travel and fresh Continue of ServiceReturnCheckpoint4260HP40 at56,7 preserve rig identity, repaired state, position and resources. One earlier return wall bump and dropped rapid key events are disclosed; deterministic100ms input completed the route.

Reclamation branch starts from the earned ServiceReturnCheckpoint. The full dismantling warning was read. Actual dismantling removed rig14199 and yielded exactly2 SalvagedTimber, with no clay refund. One timber was shaped into a field haft; one of two KnotflaxCord was braided into a binding. Ordinary pack crafting, with the station gone, consumed that haft/binding and one purchased steel head to make a cutting backup. It was equipped and its full reader inspected; the refitted mallet16155 remained carried. ReclaimCheckpoint4310HP40 followed movement. Ordinary17.7↔18.7 travel, return to56,7 and F5 produced ReclaimTravelCheckpoint4600HP40. Fresh GameBootstrap Continue of that checkpoint retained the equipped cutting weapon, carried mallet, no rig, FireClay2, steel head1, timber1 and cord1; F12 remained off. Read-only inventory probe excludes equipped items, so the accompanying equipment screenshot establishes the second weapon. A first diagnostic probe passed null to GetEntityCell after dismantling and failed; its null-safe correction succeeded. This was a diagnostic error, not a gameplay action or acceptance failure.

Native evidence: initial26 RED19/7 controls→26 GREEN; review37 RED7/30 controls→targeted423/423 before final input/source corrections. Late original-harvest-Part rollback RED1 reproduced source loss and was corrected. Input RED4 exposed valid dispatch failure; final70/70 covered rig/harvest/input. Legacy-control RED reproduced null-context failure; final78/78 passed after guard. Clarity initial13 contained genuine icon/null failures and six fixture-visibility failures; corrected previewRED7 had six genuine missing-preview failures plus one stale-owner control. Final98/98 passed the rig49, icon and reader checks after correcting two fixture-only issues in the initial98 attempt. Receipts overlap; do not add them into one test total. The compile-overlap47 attempt remains excluded. Independent final source critique found no concrete remaining P1/P2 in current authored inputs; minor culture-parsing parity remains a disclosed non-blocking limitation. No broad performance or new combat qualification is claimed.

Independent bounded reclamation gameplay review found0 visible/gameplay P1/P2. The exact equipped cutting-weapon ID was not captured by the inventory-only probe; its presence and equipment are established visually. Do not claim that unrecorded ID comparison.

Final actual controls, 2026-11-16 04:58 UTC: fresh Continue of the old BeforeCheckpoint6660 seed224401440 kept StoneFloor30833 as the sole owner at56,5 and zero ForgePart owners in Peat Works. FreshRigCheckpoint3820 seed704860946 showed the held faulted rig refusing repair with “Let go before repairing or using this rig.” Read-only inspection confirmed rig14199 remains faulted/dragged, FireClay4 and steel heads2. The first old-cache diagnostic used a nonexistent API and failed to compile; its corrected read-only probe succeeded. An initial post-haul Look cursor targeted a floor; it was corrected before the actual repair attempt. Neither diagnostic/input correction changed gameplay state. Final source review reports0 remaining P1/P2; prior preview/icon/reclamation reviews are retained and the independent final two-control review closed the release-wording P2 and reports0 remaining P1/P2 for bounded M1b gameplay acceptance.

Scope/performance: existing adjacent-cell lookup remains bounded to nine cells; new rig guards run during discovery/actions and create no background workers, unloaded simulation or new save schema. The canonical item icon guard is bounded to owned parts. Native/action/readers/travel/reload checks show no hang or gameplay error. No comparative frame benchmark or packaged FPS qualification was performed; previous R11 spike-attribution uncertainty remains open. This slice does not claim broad performance acceptance or combat effectiveness.

NEXT: review and stage exact owned changes, fetch/integrate/push and verify remotely. Package current Mac for accepted M1a+M1b and verify real launch/action/travel/save/reload, preserving old packages. Future capstan/work proposals remain design only; no elapsed processing, heat or cross-zone cargo claim. Do not repeat acquisition or the successful service/reclamation branches merely to accumulate receipts.

Unity stopped after archiving the isolated reclamation reload root at /private/tmp/coo-machine-rig-reclamation-reload-root. Reclamation action root is also preserved at /private/tmp/coo-machine-rig-reclamation-root. The hourly task remains ACTIVE; graphics remains cancelled and Deck deferred.


Current Mac delivery — 2026-11-16: sourcec29039d88 includes M1a+M1b and passed actual packaged actions/readers plus fresh-process reload; workshop branch also passed ordinary movement and17.7↔18.7 travel. See [the bounded delivery record](Verification/MachineSystems/BatchC29039/README.md) for the excluded low-health combat attempt, preserved saves/builds, logs and exact limits.

Latest continuation — 2026-11-16: source acceptance c29039d88 and current Mac delivery 095d17cb2 are remotely verified. The earlier publication/delivery NEXT is complete. Continue with [M1c relocation](MACHINE-RIG-RELOCATION.md): the actual northern route and off-site working position are qualified, the strict plain-ground crossing contract is reviewed, and native RED/countercases are next. No relocation implementation or acceptance yet.
