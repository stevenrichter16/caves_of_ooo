**Gleaners freight passed the scoped renderer regression — November 26 UTC, 2026.**
The older native failures named only the porter, loading marker, parcel
and shelf. The previous 3D census now treats exactly those four as
intentional glyph owners under the existing presentation policy. Four
new native cases check each current owner’s glyph pixels, exact selection
and fog behavior. All seven Spread census cases and 83 related checks
passed. Prior actual porter receipts still support the visible hire and
handover. The production renderer and art stayed on their existing code.
Whole-suite acceptance remains open.

**Routine playtest walking uses guarded native input batches — November 26 UTC, 2026.**
The same 24 keys from the same Cinderhold checkpoint reached cell 39,11
at turn 3080 in both runs. Fixed delays took 7.38 seconds and the guarded
batch took 2.94. The batch checks every step and stopped without issuing
a move into an occupied cell or while the inventory was open. Keep
separate observation points for new interactions and persistence.
[Walk procedure and evidence](../Tools/NativePlaytest/README.md)

**Choir-iron jobs now cover a useful tool purchase — November 26 UTC, 2026.**
Cinderhold and Morrowfast each pay 24 drams and one fire clay for one iron.
At Cinderhold, the player earned the ore, paid one to Perret and bought
a 22-dram spike. Fresh Continue kept eight drams, the spike, clay and
the spare ore, with no repeat request action. Focused native tests passed
118 of 118. The latest Mac build predates the change.
[Reward playtest](Verification/MachineSystems/RegionalIronRewards20261126/README.md)

**Shared steam catch and applied weapon feedback Mac delivery — November 26 UTC, 2026.**
Universal runtime `ceee937b7` completed GameBootstrap on Apple M5.
All 186 files in each retained app and six new universal binaries were
verified. Startup kept the four normal saves and logical preferences.
Packaged actions, physical Intel and the full native regression remain open.
[Delivery receipt](Verification/MachineSystems/MacCandidateCEEE937B7/README.md)

**Sumphold's exact silver sand restores Sill's filtration ring — November 26 UTC, 2026.**
The ordinary generation 31 player brought the one earned sand to Sill,
took the elder's manual and paid Perret for the existing stable repair.
Fresh Continue kept the same repaired well, retained manual and spent
reward. The keeper's maintenance conversation became available.
Ordinary water draws worked both before and after. This connects
one recovery reward to a lasting result for #39 and #40.
[Evidence](Verification/MachineSystems/SillEarnedSandFiltration20261126/README.md)

**A recovered consignment stocks Sumphold's merchant — November 26 UTC, 2026.**
In the ordinary version 31 seed 2 world, Vellin's reader led to the marked
six-pound sack. Both banks remained intact. Native take and fresh Continue
retained the exact sealed cargo. Delivery spent it, paid 15 drams and one
silver sand, and left three ward oil in the existing merchant listing.
Fresh Continue preserved the payout, completed request and actual
trade row. This advances #39 and #40 in one guided current layout.
[Evidence](Verification/MachineSystems/SumpholdOilRecovery20261126/README.md)

**An earned hand pump clears the Peat Works shortcut — November 26 UTC, 2026.**
The ordinary seed 64 generation 27 branch took the exact cellar clay,
bartered and forged its own pump, returned across the north edge and
moved all 12 mire units into the chosen outlet. Sixteen steps and one
stroke reached the original faulted rig. Fresh Continue retained the
same pump, empty flask, stock and five HP. The acquisition and approach
cost two tonics and two pieces of dried meat. This extends #40 with a
single earned tool-to-field-use route using existing runtime.
[Evidence](Verification/MachineSystems/PeatWorksEarnedPump20261126/README.md)

**Quarry pale-salt reaches an ordinary field fight — November 26 UTC, 2026.**
The original dagger from the ordinary quarry packet and forge route
landed four extra damage on a natural Charred Husk below Spivenor.
The old log displayed only the base attack. The corrected native replay
showed the four-point bonus and the husk's actual remaining health.
Fresh Continue retained the equipped dagger, injury, target health and
visible log. The connected native suite passed 379 of 379. This extends
#40 through a landed effect in one retained world layout.
[Evidence](Verification/MachineSystems/QuarryPaleSaltFieldUse20261126/README.md)

**Caught steam serves a Gleaners crop — November 26 UTC, 2026.**
Ordinary seed 64 travel took an earned condenser, a found waterskin and
a harvested drawgourd shell to a natural Sodden vent. Each vessel took
one measure from the same supply. The waterskin's drink started growth
in the replanted garden. Fresh Continue kept the wet bed and the other
vessel's drink. The playtest also caught a second direct-fill path that
gave two drinks without spending vent stock. The corrected menu uses
one catch per measure and closes that path. Fresh Continue preserved
exhaustion with room in both vessels. The connected native run passed
571 of 571. This advances the guided ordinary route for #40. Wider
first-pass acceptance remains open.
[Evidence](Verification/MachineSystems/CondenserWaterVessels20261126/README.md)

**Both current Wayside routes supply the same meal — November 26 UTC, 2026.**
Guided ordinary v31 seed 64 used the exact porter's delivered clay and
preserved Orven's opening stock. Seed 2 earned two cord at Peat Works,
bought one spike and forged a reusable sieve. Both repaired the original
pan, commissioned the same recipe and took one meal from the board.
Both meals survived Continue and were eaten once at full health.
The full regression on `ee0287659` is tracked separately below.
[Evidence](Verification/MachineSystems/WaysideCurrentMealComparison20261126/README.md)

**Trap wedge, hearth oil and equipment sign Mac delivery — November 26 UTC, 2026.**
Universal runtime `aa016396d` completed GameBootstrap on Apple M5.
All 186 app files matched their pins and all six Mach-O binaries are
universal. The four normal startup saves matched and preferences were
restored logically. Packaged actions and physical Intel remain open.
[Delivery receipt](Verification/MachineSystems/MacCandidateAA016396D/README.md)

The new full native regression started from published `ee0287659` in
the separate `clone-full-ee0287659`. The prior result remains historical
until the new run completes. The first run stopped before producing XML
when the Unity session ended. The pinned retry stopped after about 90
minutes with signal 9 and no result XML. Its 134 MB log remains under
`/Users/steven/coo-verification/results/machine_full_ee0287659_retry_20261126.log`.
The termination cause remains unconfirmed. A focused run on that same
snapshot completed with 6 passes and 4 failures, including all 3
settlement tests passing. The 4 failures reported missing rendering
registrations for the Gleaners' porter, parcel, loading marker and
receiving shelf. Two cases had passed in the older `full_s9.xml` result.
The other two had timed out in that run. The focused log and XML share
the prefix `machine_regression_seam_ee0287659_20261126` in the results
directory. The later corrected scope passed 90 cases with the prefix
`gleanner_glyph_scope_green_20261126`. Whole-suite acceptance remains open.

**Hand water and Conjure Rain grow a second drawgourd — November 26 UTC, 2026.**
The ordinary seed `1498104061` branch extended the earlier Gleaners'
sign, harvest, three-drink basin and planting route. Both methods
started from the same dried plant and ripened it at tick 1160. The
harvest gave a second shell and a seed. Ordinary transfers kept one
drink in the new shell after manual work and two after the spell. Both
settled results survived fresh Continue at 40 HP. Guided play on existing
source advances #40. The wider initial acceptance gates remain open.
[Evidence](Verification/MachineSystems/DrawgourdWateringComparison20261126/README.md)

**Broken equipment shows its signed penalty — November 26 UTC, 2026.**
Fresh Continues from Sill tick 160 used the same carried broken dagger.
The native F1 view changed `?2 melee accuracy` to `-2 melee accuracy`
at the same tick and on the same line. Both captures differ only within
the sign glyph. All 241 focused native cases passed, including the old
UI glyph sweep. The whole regression has yet to be rerun on this change.
[Evidence](Verification/MachineSystems/BrokenPenaltyReadout20261126/README.md)

**Reclaimed liquid oil can power a second brine batch — November 26 UTC, 2026.**
At the same camp arrival, the still refused a second batch at four fuel.
Three previously earned oil let the exact hearth pay for it. One timber
worked too and left more fuel. On a later branch the exhausted warm
hearth refused; waiting and feeding the cold hearth kept its flame out.
Fresh Continues kept the oil payment, output and separate cold result.
Native checks passed 352 of 352. The original camp and stats were supplied.
The existing Mac package predates this slice.
[Evidence](Verification/MachineSystems/LiquidHearthFuel20261126/README.md)

**A field haft can wedge an exposed mechanism — November 26 UTC, 2026.**
The timber-first jam action accepts one sound loose rough field haft.
Guided seed 64 play compared the same yard from one saved arrival.
The direct crossing spent the haft and recovered one spike. Hauling
the fallen beam kept the haft and left the trap armed. Both reached
the same crate and took the same dried meat at 40 HP. Fresh Continues
confirmed the jam, salvage and haul outcome. A creature triggered the
first visited yard's trap before the player reached it. The connected
native run passed 248 of 248 checks. Wider discovery and the original
45 acceptance checks remain the current work before the improvement pass.
The `35566d84a` Mac package predates this slice.
[Evidence](Verification/MachineSystems/FieldHaftTrapWedge20261126/README.md)

**Dressing bench and meal sharing Mac delivery — November 26 UTC, 2026.**
The universal package from `35566d84a` reached GameBootstrap on Apple
Silicon. It includes the mixed dressing bench repair and companion meal
sharing. The package and four normal startup saves matched their hashes.
Packaged gameplay and physical Intel verification remain open.
[Delivery receipt](Verification/MachineSystems/MacCandidate35566D84A/README.md)

**A rough haft restores Sella’s dressing service — November 26 UTC, 2026.**
The player sold two earned timber in Sumphold, returned and shaped one
of the remaining pair. Reading and cancelling the mixed repair was
free. One action spent the timber and haft. Fresh Continue kept the
repair, and a second action paid a pad, cord and two drams for one
dressing. Fresh Continue kept the dressing, remaining goods and both
payments. Final native run passed 280 checks with five matching historical
equipment-art failures. An actual older save displayed the updated bill
and kept its original route guidance. Independent source and save review
passed. The preceding Mac build predates this slice.
[Evidence](Verification/MachineSystems/SellaMixedBrace20261126/README.md)

**Ordinary hand and Concord routes restore Tine’s weir — November 26 UTC, 2026.**
Fresh seed 2, one ordinary 50-dram start and a common tick 360 checkpoint.
The contract paid 45 and earned the 15-dram report. Hand work bought
one oak haft and a hatchet, sold one recovered Glade sword, gathered
four Peat Works timber and took one locker oak haft. The one-action
repair spent the two oak hafts and two timbers and kept the hatchet.
Both reported and returned to the same merchant. Four perch stocked on
each selected shelf. The actual single purchases cost 15 and 14 drams.
Fresh Continues held both results at 40 HP. Guided current seed only.
[Evidence](Verification/MachineSystems/TineOrdinaryWeir20261126/README.md)

**A cooked catch can support a companion — November 26 UTC, 2026.**
The original earned perch reached an injured Gleaners' warden after
ordinary salvage, purchase of the 58-dram recruitment bell and recruitment.
Reading the selected Share row and returning spent no turn. One portion
and one action raised the warden from 22 to 28 HP, confirmed by fresh
Continue. The final connected run passed 252 native cases. Independent
source and recorded-play review found no blocking issues. Current Mac
runtime ad0a28f61 predates this change.
[Evidence](Verification/MachineSystems/SharedRoastedPerch20261126/README.md)

**Portable knotflax cleaning feeds the ordinary allotment — November 26 UTC, 2026.**
The exact earlier gathered bundle and a supplied canonical sieve went
two steps out of frame range. The native pack row took one action and
yielded one seed. The player used it in an ordinary prepared bed and
watered the new crop from the existing rain flask. Fresh Continue kept
the crop, sieve, remaining water and spent stand. The final connected
run passed 271 cases.
[Evidence](Verification/MachineSystems/PortableKnotflaxSieve20261126/README.md)

**The earned hand pump opens clean Cinderhold salvage — November 26 UTC, 2026.**
The original tray, pump and fitting were present at tick 1560. Reading
the east outlet quote cost no time. One stroke moved 12 oil to clear
floor. The player took the same axe head cleanly using the ordinary
pickup list. Fresh Continue preserved tray, fitting and outlet. One
step into the moved oil coated the player through the ordinary liquid
rules. The final native run passed 247 cases. The feature keeps the
flask and wet reach alternatives and the finite shared transfer.
[Evidence](Verification/MachineSystems/CinderholdTrayPump20261126/README.md)

**Fair-weather shelves restore a wet striker at North Works.** Two
freshly generated shelves provide a one-action reduction of up to
25 moisture points on one exact carried item. Actual North Works play
read the 50% to 25% quote, aired the supplied damp striker and used
one strike to light the previously unlightable torch. Fresh Continue
kept both results and a separate cached-site Continue kept the original
ordinary shelves. The patchbearer encounter left 15 HP. The final
connected native run passed 163 cases.
[Evidence](Verification/MachineSystems/NorthFairWeatherDrying20261126/README.md)

**A satchel supplies an ordinary healing dose in one action.** Both
private Sill forks began with the same labeled two-dose stack. Direct
use kept the remainder in the satchel. The free unpack and ordinary
Drink control kept the remainder loose. Both spent one action,
reduced carried weight by one pound and retained the label on fresh
Continue. Six hundred and six connected native tests passed.
[Evidence](Verification/MachineSystems/StoredTonicUse20261126/README.md)

**Wayside witnesses can guide a live search — November 26 UTC, 2026.**
The real body at `24,16` had a free note. After six ordinary steps,
the player told Orven in one action. Fresh Continue kept the report
before Orven's own sighting. He checked the lead within his existing
route budget and returned with his own finding. The original box, clay,
payments and open notice remained. Saves now retain progress in the timed
pacing goal. Native F5 and fresh Continue kept three of twenty steps.
The final connected run passed 194 cases.
[Evidence](Verification/MachineSystems/WaysidePlayerWitness20261126/README.md)

**A mixed clay bill can seal the Cinderhold seep — November 26 UTC, 2026.**
The original two fire clay and two prepared lining clay reached the elder
after an ordinary return from the cellar. The handover spent the exact
four units and the elder paid the existing report once. The smith's
current lot remained. The next due lot offered a sounding rod for 60
drams, and the exact purchase survived fresh Continue. The final connected
run passed 115 cases.
[Evidence](Verification/MachineSystems/CinderholdMixedClay20261126/README.md)

**A cutter recovers a usable forged head in the field — November 26 UTC, 2026.**
The earned Peat Works branch used its dagger to recover the recorded mallet
head in one action, leaving its clay and broken rig alone. Fresh Continue
kept the head. The player recovered cord and timber, shaped a haft,
braided binding, assembled and equipped a different mallet away from the
rig, walked and continued again. The prior working forge comparison returns
both head and haft and requires a usable station. This advances portable
reclamation and equipment preparation while the full forge gate remains open.
[Evidence](Verification/MachineSystems/PeatFieldHead20261126/README.md)

**A Stump catch becomes an optional cooked meal — November 26 UTC, 2026.**
The earned old lake perch kept its raw meal and gained a normal Cook action.
After fifteen steps to The Second Bowl, one ordinary action produced one
roasted perch at the same weight and base value. Fresh Continue kept the
cooked fish. Eating healed seven HP and another fresh Continue kept the spent
meal and 29 HP. Native checks passed 128 cases. The raw and cooked actual
healing rolls were independent. The recipe uses existing heat rules and has
a dedicated inventory icon.
[Evidence](Verification/MachineSystems/RoastedLakePerch20261126/README.md)

**Field head recovery and lake perch Mac delivery — November 26 UTC, 2026.**
The universal Mac package from `8308de3a4` reached GameBootstrap on
Apple Silicon. Package and ordinary startup save hashes matched. Full
packaged play and Intel execution remain open.
[Delivery receipt](Verification/MachineSystems/MacCandidate8308DE3A4/README.md)

**A field satchel travels as one sealed parcel — November 26 UTC, 2026.**
The paid Sill satchel held two meat and two tonics, weighing seven pounds.
Cinderhold quoted all contents and four drams. The player cancelled free,
then sent the bag and continued from a save. The Last Counter withheld it
until due. Waiting locally led to a wildlife death, so the successful
branch waited on the world map. The exact bag and goods returned once.
Fresh Continue preserved them, and drinking a delivered tonic on the
world map restored ten HP. Ordinary trade retains the empty-storage rule.
[Evidence](Verification/MachineSystems/PackedConcordFreight20261126/README.md)

**Working pan scales help Wellmeet trade — November 26 UTC, 2026.**
The actual player used two earned silver sand at the original scales.
Sennel quoted the same three tonics at 129, then 125 while the patch
worked, and 129 on return after expiry. Fresh Continues kept the working
and expired states. The same rate is available from a working iron body
in native tests. The lasting report stays separate. Wellmeet's shared buy
and sell quotes allow local and reputation discounts to narrow the
spread to parity. The guided play checked quotes without purchase.
[Evidence](Verification/MachineSystems/WellmeetWorkingScales20261126/README.md)

**Sealed freight and working scales Mac delivery — November 26 UTC, 2026.**
The universal Mac package from `31a2513ff` reached GameBootstrap on
Apple Silicon. Package and ordinary startup save hashes matched.
Packaged gameplay and Intel execution remain open.
[Delivery receipt](Verification/MachineSystems/MacCandidate31A2513FF/README.md)

**An earned rough haft can brace an ordinary door — November 26 UTC, 2026.**
One loose rough field haft uses the same latch and recovery as an iron
spike. The actual trip bought the second timber, repaired the Gleaners'
wicket, and opened it normally before testing the brace. Fresh Continue
kept the exact haft in the doorway and opening was refused without time
spent. Removal returned the haft. The player opened and crossed the
wicket, and a second fresh Continue kept the result. The final 56 native
cases passed. The current Mac package predates this change.
[Evidence](Verification/MachineSystems/FieldHaftDoorBrace20261126/README.md)

**A working anvil can dress reclaimed iron — November 26 UTC, 2026.**
One ordinary iron blank became the same spike at Cinderhold's existing
anvil using the earned peat-packing mallet. The action kept the mallet and
completed in one turn. The player used the exact spike and recovered wood
to make bellows with a binding cut from bought leather. Fresh Continues
kept the spike and the later bellows. The first scoped receipt passed 240
native cases. After correcting the stale Cinderhold profile test to
require the published find and its loose custody, the connected run
passed 261 cases.
The anvil supplies this one operation through the existing process.
[Evidence](Verification/MachineSystems/AnvilColdDressing20261126/README.md)

**Gantry accepts grain from two ordinary journeys — November 26 UTC, 2026.**
Two current-version playthroughs brought two physical sheaves to the
merchant. Seed 64 followed the marked field directions and finished at
tick 810 with 25 HP. Seed 2 brought original Gleaners' grain and finished
at 420 with 40 HP. Both paid six drams and one tonic. The exact sheaves
joined trade stock and both receipts survived fresh Continue. The guided
comparison leaves wider seed coverage and unguided discovery open.
[Evidence](Verification/MachineSystems/GantrySupplyComparison20261126/README.md)

**Door brace and anvil Mac delivery — November 26 UTC, 2026.**
The universal package from `ad0a28f61` reached GameBootstrap on Apple
Silicon. It includes both the rough-haft brace and the anvil option.
Package and ordinary startup save hashes matched. Packaged gameplay
and Intel execution remain open.
[Delivery](Verification/MachineSystems/MacCandidateAD0A28F61/README.md)

**An earned resin lot seals Posy's east composition — November 26 UTC, 2026.**
Separate ordinary seed 64 starts brought one early resin or two ripe
measures to actual elders. One showed the missing second measure and
preserved the held option. Two sealed the composition after one wait,
kept the seed, and paid one report. Both fresh Continues matched.
[Evidence](Verification/MachineSystems/PosyResinHandover20261126/README.md)

**A second steam catch supplies a dry crop — November 26 UTC, 2026.**
A native Sodden vent quoted the existing flask's fill change from
1/12 to 2/12. Free reader and cancellation kept the three remaining
catches. Actual use spent one catch and kept the same flask. The branch
then carried it to the shelter and used one water on a dry sumpsieve.
Fresh Continue kept 39 moisture ticks and one water. A direct route
attempt died and the successful detour ended at 4 HP after watering.

**A fresh cave fork distinguishes felt from bare rubble — November 26 UTC, 2026.**
Initial play found that saving an already-finished wander child made it
unfinished on load. The stored completion now survives Continue. The
new matched fork let the same native Pale Stalker hear a bare step and
stay untargeted after the felt step. The strip was recovered and the
exact outcome persisted at fresh tick 1500. Final connected native run
passed 359 cases. [Evidence](Verification/MachineSystems/CaveFeltAndSteamTopUp20261126/README.md)

**A paid porter order waits during ordinary travel — November 26 UTC, 2026.**
The existing loading guide explains the pause before payment. The
actual loaded Gleaners' order stayed put through map travel and a
fresh Continue while away. It resumed on return and transferred the
exact clay box once. A subsequent wait and fresh Continue kept the
same terminal receipt. The final targeted run passed 76 cases.
[Evidence](Verification/MachineSystems/PorterAwayLifecycle20261126/README.md)

**Measured oil lights Quillhold's copy desks — November 26 UTC, 2026.**
The expedition branch brought ordinary settled oil to Esset. The
direct handover used three units, retained the flask, and progressed
the existing oil approach on the next turn. Reporting paid once.
Fresh Continue matched the completed project and the exact current
shop lot, with drying books named for a future consignment.
The final targeted run passed 303 cases.
[Evidence](Verification/MachineSystems/QuillholdMeasuredOil20261126/README.md)

**An installed camp box can supply the hearth — November 26 UTC, 2026.**
The same loaded box let the player feed one stored timber in one
action or take a stack and feed from the pack in two. The existing
hearth used its normal burn and heat rules. Two ordinary still batches
spent that heat, and the exact fuel, stored remainder and water flasks
survived Continue. Final targeted run passed 144 cases.
[Evidence](Verification/MachineSystems/CampBoxTimberFeed20261126/README.md)

**Brine can leave a recoverable solid — November 26 UTC, 2026.**
The existing hot still can give up one water to retain a pound of crust.
The supplied seed 64 expedition camp showed the ordinary option from
the bench and offered crust one step onto clear ground. The exact loose
crust survived fresh Continue, was picked up and proofed the original
worn leather cap away from the still. The final coat and independent
water flask also survived fresh Continue. Final targeted run 586/586.
[Evidence](Verification/MachineSystems/RetainedBrineCrust20261126/README.md)

**The rough field haft makes a useful hand bellows — November 26 UTC, 2026.**
Original Sumphold timber made the grip. In Cinderhold, the ordinary forge
spent that grip, a bought spike and a binding cut from a bought offcut.
The other timber stayed in the pack. The same finished bellows moved a
supplied thin cloud on the road and stayed in the pack. The oak bill is
still available. The current Mac package predates this increment.
[Evidence](Verification/MachineSystems/FieldHaftBellows20261126/README.md)

**An ordinary sounding carries across bare sandstone — November 26 UTC, 2026.**
The shared floor check now lets the purchased rod work on genuinely bare
stone in an ordinary natural cave. A guided native walk beyond Cinderhold
reproduced the old refusal at the same tile. The new action produced a
12-pace sounding and recorded it at the usual one-action cost. The
ordinary notebook action and fresh Continue kept the same entry.
[Evidence](Verification/MachineSystems/SoundingRodBareFloor20261126/README.md)

**Late-cycle lichen at North Works — November 26 UTC, 2026.**
Fresh v31 can offer a finite shallow crust reached without entering the
working yard. Native seed 64 gathered its exact one lichen and used it
to freeze a local puddle. The test calendar was advanced and the flask
was supplied. Fresh Continue kept the ice, water and depleted source.
The deeper patch and services were retained.
[Evidence](Verification/MachineSystems/NorthSeasonalLichen20261126/README.md)

**Item icons default on in the pack and storage — November 26 UTC, 2026.**
The atlas covers 357 current portable item types and 35 additional
storage owners. Graphics settings can hide the art, including both
trade panels and storage destination choices. Color marks and text
labels remain available. A native Sill Continue kept the labeled seed
stack in the exact camp box, and lifting the box exposed the indented
stack in the pack. The final 378 connected native tests passed.
[Evidence](Verification/MachineSystems/ItemIconFinal20261126/README.md)

**Sumphold's finite cargo can be moved, stocked or taken in place — November 26 UTC, 2026.**
Fresh v30 admits one exact crate after the final approach and berth check.
Seed 64 hauled the loaded west crate through the turn, deposited the exact
rations, then recovered and shaped original timber into a field haft.
Seed 65 took both timber units from the east crate without relocating it.
The parked, worked and emptied states matched on fresh Continue.
Final native coverage passed 467 cases with one opt-in skip. Carry forward
both layouts and continue with another distinct field or seasonal use.
The current Mac package predates this increment.
[Evidence](Verification/MachineSystems/SumpholdPublicCargo20261126/README.md)

**A search can bring news of the living porter — November 26 UTC, 2026.**
At the wayside kitchen, a searcher can see the exact original porter alive
along the existing route. The cook can publish that historical observation
at the shelf after returning or after the paid hand reports in person. The
shipment stays overdue until the existing physical receipt. An earlier
sighting and a later independently seen body can be distinguished.
[Evidence](Verification/MachineSystems/WaysideLivingPorter20261126/README.md)

**Earlier Sumphold cargo approaches — November 26 UTC, 2026.**
Ordinary cold entries reached the existing crates in seeds 64 and 65.
On-foot approaches reached dry working and roadside berths. This scout
kept both crates in their original places.
[Scout](Verification/MachineSystems/SumpholdCargoScout20261126.md)

**Causeway scale is readable and useful in the field — November 26 UTC, 2026.**
The vent reader explains what the scale can do, including on existing
v29 saves with spent stock. Proofing flasks put the clothing treatment
directly after Examine. Native play spent one previously gathered scale
on the original cloak, preserved the separate water flask and confirmed
the coat's worn-turn behavior and persistence in a private save. Continue
with another current layout. A dry acid harvest worked before the cave
bear interrupted the return at 4 HP. The safer surface branch took two
brine catches, brewed and used one tonic, and coated the buckler with the
other catch. Both successful alternatives and the brine reaction used
existing rules. Keep actual heat damage and a future combined brew as
separate checks.
[Evidence](Verification/MachineSystems/CausewayFieldProofing20261126/README.md)

**The pack opens vessels inline and supports labels on ordinary items — November 26 UTC, 2026.**
Native play used all three label modes on carried gear, equipment, and a
stored stack. Clicking the loaded satchel revealed its exact children as
indented rows while the other satchel stayed separate. Ordinary stowing
preserved the labels and the same total pack weight. The focused run
passed 313 cases and the wider run passed 491 with one opt-in case
skipped. Continue the 45 with existing field and regional opportunities.
[Evidence](Verification/MachineSystems/InlineInventoryLabels20261126/README.md)

**Repair bills and carried storage read clearly in play — November 26 UTC, 2026.**
Examine puts an amber repair warning above the usual details and names the
actual inputs and tools for the live fault. Field satchels and portable camp
boxes use their combined weight and can organize goods from the pack. Their
saved labels support swatches, colored icons, and colored text. Continue the
45 with the verified shared-capability work and an exact downstream purpose
for the Causeway materials.
[Design](CARRIED-STORAGE-LABELS-DESIGN.md) ·
[Evidence](Verification/MachineSystems/RepairAndCarriedStorage20261126/README.md)

**A visible Causeway vent has a physical cave counterpart — November 26 UTC, 2026.**
Fresh version 29 can place an ordinary steam vent off the Sodden Causeway
approach. The same original stair leads to an existing return with a local
mineral stain. Ordinary seed 64 play located and read both clues, then
returned to the shoulder. A separate supplied-tool continuation verified
that condensation and scraping spend the same finite supply. Continue with
a contrasting underground phenomenon and a downstream use for its material.
[Evidence](Verification/MachineSystems/CausewaySteamSeam20261125/README.md)

**An ordinary condenser catches water at a natural vent — November 25 UTC, 2026.**
An earned tray measure and a bought spike became a condenser at Cinderhold.
A bought flask took one water from a finite Sodden vent group in ordinary
play. Fresh Continue kept the same tools and the exact remaining catch
stock. The separate v28 Causeway route has a verified down stair and
physical return with candidate ground for a future steam clue.
[Evidence](Verification/MachineSystems/OrdinaryCondenser20261125/README.md)

**One early pitchpod bead gives a shorter working route — November 25 UTC, 2026.**
At fresh v28 seed 64, ordinary travel reached one feature in thirteen steps
and the existing ripe source in nineteen. The new gathered resin paid for
the exact worn cloak’s first lacquer with ordinarily earned regard.
The new source and the existing patch retain their separate stock on save.
All 168 connected native cases passed. Continue ordinary layout comparisons
and scout a physical surface-to-cave heat route as its own slice.
[Evidence](Verification/MachineSystems/PosyEarlyPitchpod20261125/README.md)

**A found quarry packet has two earned uses — November 25 UTC, 2026.**
An ordinary seed 2 search opened the neighboring Spread cave cache,
took only the quarry packet and screened it with the earned sieve.
The original dagger took the screened salt at Cinderhold. The same
screened stock supplied a separate cave return branch where
dry carved floor now takes the same sand mark. Both exact outcomes
survived fresh Continue. All 77 focused native cases passed.
Continue #31 with a separate bounded seasonal material opportunity and
the wider #40 work with additional current ordinary layouts.
[Evidence](Verification/MachineSystems/OrdinaryQuarryLoop20261125/README.md)

**A finite mire shortcut at the Peat Works — November 25 UTC, 2026.**
Fresh version 27 checks the actual shed gap, dry detour and legal
outlet before staging two finite bog-mire pools. The ordinary southern
entrance keeps its dry walking distance. Actual north entry reached the
same work rig in 23 detour steps or 17 actions using either an ordinarily
bought flask or a pump supplied to a separate comparison copy. Both
clearing methods retained the liquid and open ground through reload.
All 134 focused native cases passed.
[Evidence](Verification/MachineSystems/PeatWorksMireShortcuts20261125/README.md)

**Ordinary shops and recovered clay supply a hand pump — November 25 UTC, 2026.**
An earned version 25 Gleaners’ save carried both original clay measures
to Cinderhold. Ordinary trade spent 44 on one spike and two offcuts.
The starter dagger made two bindings. The existing forge used all five
measures to give one five-pound pump, retained through fresh Continue.
Seven current seed 64 Sodden scouts gave no ordinary acid-pool supplier.
Keep the natural use and a meaningful route alternative open.
[Evidence](Verification/MachineSystems/OrdinaryHandPumpAcquisition20261125/README.md)

**Morrowfast offers one affordable mended fish trap — November 25 UTC, 2026.**
Fresh Orrit sold the exact mended kit for 35 drams in actual play. The
player carried it to the natural Stump cascade, placed it on the shore,
retreated to the world map then came back for one fish. Retrieval
preserved the trap. Wildlife injured the player and the meal restored
eight HP on return. Fresh Continue kept the exact kit, no replacement
stock and the pond’s saved count. All 76 focused native cases passed.
Continue a contrasting current seed and broaden ordinary tool discovery.
[Evidence](Verification/MachineSystems/MorrowfastMendedTrap20261125/README.md)

**Current universal Mac package — November 25 UTC, 2026.**
Runtime eb13f3104 reached GameBootstrap startup on Apple Silicon.
The current package and all 186 files were verified after launch. Actual
packaged play, Intel execution and the full inherited regression remain open.
[Package receipt](Verification/MachineSystems/MacCandidateEB13F3104/README.md)

**Ordinary routes through Quillhold and Sodden — November 25 UTC, 2026.**
The fresh sign save supports two guided v26 forks. Sodden’s ordinary ripe
crop yielded two pads and a seed with the shower offer left unused. Quillhold
loaned the existing Drying Breeze volume for 29 Ink before the supply project.
The learned rite cleared Wet after using one drink of water from Gleaners’.
Both direct forks retained full health and matched fresh Continue. Keep the
fatal Sill detour separate. Continue contrasting current layouts and extend
finite ordinary tool acquisition where the next useful use is already present.
[Evidence](Verification/MachineSystems/OptionalWorkOrdinaryRoutes20261125/README.md)

**The short sword also cuts prepared stock — November 25 UTC, 2026.**
The existing cutting system recognizes an explicit short-sword capability.
Actual Sill trade brought one offcut from Nerrel’s lot. The exact sword was
the sole available tool for one native cut on ordinary ground. Fresh Continue
kept the sword, recovered dagger, one binding and vendor remainder. All 190
connected native checks passed. Continue fresh regional route comparisons.
[Evidence](Verification/MachineSystems/ShortSwordCutting20261125/README.md)

**A fresh Gleaners’ notice points to either lining grade — November 25 UTC, 2026.**
The current well and fresh notice use the same bills as the native action.
Actual fresh seed 64 reading preserved time and the distant kitchen stayed
unloaded. F1 and Examine on an earned earlier save used the current bill
and local directions with exact saved flavor and sign retained. All 147
focused native cases passed. Continue the second flexible-stock work provider.
[Evidence](Verification/MachineSystems/GleanersDiscovery20261125/README.md)

**Two useful treatments at Sella’s shelter — November 25 UTC, 2026.**
The bench reader and Sella explain independent portable treatments and a
combined dressing. Actual new harvesting and movement brought timber and
cord to a common saved branch with the earned two pads. The player either
used a pad and bandage with the bench still broken, or repaired it and paid
to combine the same supplies into one application. Both exact outcomes
survived fresh Continue. Native verification passed 409. Continue broader
ordinary discovery and practical work choices across other regions.
[Evidence](Verification/MachineSystems/SoddenTreatmentChoices20261125/README.md)

**A repair menu shows the complete compatible bill — November 25 UTC, 2026.**
The three lining-clay receivers share a shorter action and wrapped bill.
Actual F1 cancellation kept time and both exact goods at the Peat Works rig.
At Gleaners’ one loose measure was insufficient. An ordinary six-dram
purchase supplied the second grade. Mixed repair reopened the well without
touching the separate tray reserve, and fresh Continue matched. The final
native run passed all 203. The next distinct comparison uses a free portable
treatment and Sella’s prepared dressing.
[Evidence](Verification/MachineSystems/RepairBillReaders20261125/README.md)

**Kitchen commissions pause with real staff availability — November 25 UTC, 2026.**
An unavailable cook declines a start and pauses a paid order. Exact held
portions refund safely if a late callback invalidates the unpaid start.
GameBootstrap verified the active reader, fresh Continue and ordinary
recovery with no extra fee or parcel. The connected native run passed
160 with five explicit allocation skips. Next, qualify readable material
options at normal screen widths and use that evidence in further #37/#40
comparisons across regions and current-generation layouts.
[Evidence](Verification/MachineSystems/KitchenStaffAvailability20261125/README.md)

**Two ordinary routes to the same field meal — November 25 UTC, 2026.**
Fresh v26 seed 2 earned a reusable sieve through native trade and crafting.
The earlier physical shipment in retained v18 seed 64 provided the other
route. Both broken kitchens offered free cooking. Both clay routes repaired
the same kind of pan, then the same commission cost and timing yielded a meal
that was collected and eaten. Fresh Continue verified the working local job
and the collected import. The next scoped work is the late cook availability
and exact ingredient custody regression. Three native failures are reproduced
in `kitchen-staff-transaction-red-20261125.xml`.
[Evidence](Verification/MachineSystems/KitchenOrdinaryContrast20261125/README.md)

**One trace follows the real parcel into Orven’s stock — November 25 UTC, 2026.**
Actual GameBootstrap movement advanced one earned wayside shipment from
booking through paid pickup and shelf settlement to the exact merchant lot.
The four committed records agreed on the saved order and physical cargo.
The native suite passed 145 checks with one utility skip. Continue with
ordinary contrasting work across distinct regional situations.
[Evidence](Verification/MachineSystems/PorterCausalTrace20261125/README.md)

**One existing measure can be taken from the public tray — November 25 UTC, 2026.**
The earned Gleaners’ load offered one pound or a whole take. Ordinary use
split the original provenance across two physical owners, and fresh Continue
preserved them. An ordinary second retrieval reunited them and the well
spent both in its existing repair. The next work is the committed porter
and receiving receipts for the same bounded delivery lifecycle.
[Evidence](Verification/MachineSystems/TrayOneMeasure20261125/README.md)

**A shower can water the Sodden sumpsieve bed — November 25 UTC, 2026.**
Fresh version 26 adds an optional one-portion channel at Sella’s shelter.
Normal travel, native actions and waiting took the dry crop from the channel
to a later cast, an actual harvest and two pads plus one seed in the pack.
The separate ripe crop was untouched. The next carrying comparison is exact
one-measure withdrawal from the public tray so small trips work both ways.
[Evidence](Verification/MachineSystems/SoddenRainGuide20261125/README.md)

**A porter quote compares the complete clay load — November 25 UTC, 2026.**
The ordinary quote shows five pounds boxed, two pounds loose, the two
carrying allowances, walking distance, one action and settlement. The
shorter opening still reduced the fare to seven. Actual free cancellation
returned the full amount and fresh Continue retained the exact box and
stock. The final native run passed 241 cases with one utility skip. Next,
test whether a local shower can substitute for one carried crop watering.
[Evidence](Verification/MachineSystems/PorterLoadComparison20261125/README.md)

**Known sieve packets explain their usable yield — November 25 UTC, 2026.**
Examine and the carried and camp-box menus share the declared products.
Actual play used an earned clay packet for firm footing and a supplied
quarry lot for an exit mark. Both consequences survived fresh Continue.
The final run passed 324 native checks with one explicit utility skip.
The next comparison covers the porter’s full boxed and loose loads.
[Evidence](Verification/MachineSystems/ScreeningReader20261125/README.md)

**Wayside clay restores forge service at Peat Works — November 25 UTC, 2026.**
The earned two-measure prepared lot arrived through normal movement. The
player used existing locker stock to make a pack mallet, and its reclaim
action refused beside the faulted rig. Examine and action named the same
compatible bill. The repair spent the two measures and the same station
then reclaimed one head and one haft from the mallet. Fresh Continue matched.
The connected suite passed 275 with one identical existing art-model failure
on the baseline. Graphics remain parked.
[Evidence](Verification/MachineSystems/FieldRigCompatibleClay20261125/README.md)

**Mac delivery for clay staging and seasonal knotflax — November 25 UTC, 2026.**
The universal build of `b47282110` reached GameBootstrap on this Mac.
All 186 package files matched afterward. It is the current startup-verified
package. The earlier packages remain available.
[Delivery](Verification/MachineSystems/MacCandidateB47282110/README.md)

**One-pound staging at Gleaners’ — November 25 UTC, 2026.**
Seed 64 v25 exercised ordinary movement into and out of the original cellar.
Native split and deposit left one exact measure near the well. Fresh Continue
and ordinary travel kept it there. The packed-delivery shelf refused the second
loose measure. The public tray admitted it, returned both units in the same
physical stack, and ordinary repair and water use completed the loop. Fresh
Continue preserved the working well, empty tray and independent shelf. The
connected suite passed 760 checks with one explicit generator skip. Next,
qualify the same lining grades at the Sodden field rig.
[Evidence](Verification/MachineSystems/PublicClayTray20261125/README.md)

**One knotflax source supports distinct seasonal work — November 25 UTC, 2026.**
Seed 65 ordinary movement and the starting dagger reached the optional
Morrowfast stand. An early cut spent it for one cord, fresh Continue kept
that exact cord, and ordinary preparation made a bandage. A separate
uncut copy advanced to the ripe phase and supplied the usual bundle and
frame seed. Both spent branches stayed depleted at the late phase. The
final native run passed 197 focused checks. The next connected choice is
small individual deposits in a separate public store by the Gleaners’ well.
[Evidence](Verification/MachineSystems/SeasonalKnotflaxStems20261125/README.md)

**Mac delivery for Tine’s restored fish supply — November 25 UTC, 2026.**
A universal build of `d23329978` reached GameBootstrap on this Mac. All
186 package files matched afterward. It is the current startup-verified
package. The earlier packages remain available.
[Delivery](Verification/MachineSystems/MacCandidateD23329978/README.md)

**Both durable Tine methods supply the same fish — November 25 UTC, 2026.**
The same saved Tine start supported an ordinary hand repair and an ordinary
45-dram contract branch. Both supported the real merchant’s four-perch
lot on return. Actual quantity purchase and food use reduced each lot
to three. Fresh Continue preserved the distinct method and price, stock
and purse. The final native run passed 83 and one explicit utility skip.
The next distinct trial is an early cord or later seed choice at one
finite seasonal knotflax stand.
[Evidence](Verification/MachineSystems/TineWeirStock20261125/README.md)

**Mac delivery for installed camp-box screening — November 25 UTC, 2026.**
A universal build of `020f5a34a` reached GameBootstrap on this Mac. All
186 package files matched afterward. It is the current startup-verified
package. The earlier packages remain available.
[Delivery](Verification/MachineSystems/MacCandidate020F5A34A/README.md)

**Screening uses an installed own box as real stock — November 25 UTC, 2026.**
The same sieve and actual acquired packet traveled to eligible Spread
ground. Ordinary placement, deposit and fresh Continue established the
stock, and the real box menu spent it for the two declared fractions.
The pack gained three pounds and the result survived fresh Continue. Normal
take-up retained the box, and ordinary grit use left saved footing. The
final focused run passed 536 executable
checks and one explicit generator skip. Next, trial equivalent future
market supply from Tine’s durable weir repairs.
[Evidence](Verification/MachineSystems/CampBoxScreening20261125/README.md)

**Mac delivery for source-side screening — November 25 UTC, 2026.**
A universal build of `e33d0dba0` reached GameBootstrap on this Mac. The
isolated source had zero build errors. The package retains the 186 exact
post-build file hashes after startup. The earlier packages remain.
[Delivery](Verification/MachineSystems/MacCandidateE33D0DBA0/README.md)

**Source-side screening leaves ordinary recoverable material — November 25 UTC, 2026.**
From the existing one-packet Morrowfast heap, portable screening can carry
two pounds of clay and leave one pound of grit at the worksite. Fresh Continue
retained the exact grit. The clay repaired the well, and an ordinary return,
pickup and scatter used the remaining fraction. The parallel all-in-pack
choice still takes one action. The next narrow trial is screening from exact
stock in an installed own camp box with the same carried sieve.
[Evidence](Verification/MachineSystems/GroundScreening20261125/README.md)

**Mac delivery for the Gleaners’ crossing and clay — November 25 UTC, 2026.**
A universal build of `2e7b924f0` is verified on this Mac. The isolated source
had zero build errors and the same five warnings. Startup succeeded and
all 186 package file hashes matched afterward. The previous packages remain.
[Delivery](Verification/MachineSystems/MacCandidate2E7B924F0/README.md)

**Original cellar clay stacks and pays for ordinary work — November 25 UTC, 2026.**
Both original counts and their saved origin survived ordinary pickup and fresh
Continue in the Gleaners’ cellar. On the surface the exact recovered stack
paid for the well. The prepared clay stayed in the pack, water became
available, and the repaired result survived a fresh load. The final focused
run passed 467 executable checks, including the 15 reproduced failures
that exposed the issue. The larger inherited project regression remains open.
The next distinct trial is source-side screening that can leave the coarse
fraction in the world as recoverable stock.
[Evidence](Verification/MachineSystems/OriginalClayHandling20261125/README.md)

**The worn crossing changes a local porter fare — November 25 UTC, 2026.**
In seed 64, the actual five-pound box quoted 25 steps and eight drams with
the wall standing. Ordinary strikes exposed a 23-step seven-dram path. The
marker reader agreed with that quote after fresh Continue. The same loaded
box crossed the gap in the ordinary branch, then a combat casualty triggered
the six-dram unearned refund and physical spill. A separate clear-route
control delivered and reloaded the exact parcel and final fare. The final
targeted run passed 153 executable cases with one explicit skip. A broader
comparison exposed an earlier original-clay stack problem, the next slice.
[Evidence](Verification/MachineSystems/PorterCrossing20261125/README.md)

**Mac delivery for fair-weather winnowing — November 25 UTC, 2026.**
Runtime `31fcaf2ed` is verified on main and packaged for arm64 and x86_64.
GameBootstrap startup and package integrity passed on the Mac. Weather,
native installation, feeding and fresh Continue were verified in Editor play.
[Delivery](Verification/MachineSystems/MacCandidate31FCAF2ED/README.md)

**Fair-weather winnowing and camp fuel — November 25 UTC, 2026.**
The same public frame gives a seed in rain or fair weather. In a dry fair
window it also retains the bundle's one-pound chaff fraction. In Editor
play 16 ordinary waits took the same earned bundle through the change in
weather. Fresh Continue retained both products. Ordinary travel, installation
of a supplied camp hearth, and feeding added five fuel without ignition.
Fresh Continue matched the seeded output and spent chaff state. The expanded
native run passed 417 executable cases with one utility skip. Item 18 is the
next trial, using a physical opening and the existing porter quote.
[Evidence](Verification/MachineSystems/FairWeatherChaff20261125/README.md)

**Mac delivery for loaded timber side parking — November 25 UTC, 2026.**
Runtime `bd469bc21` is verified on main and packaged for arm64 and x86_64.
GameBootstrap startup and package integrity passed on the Mac. Loaded
movement, parking, recovery and both checkpoints were verified in Editor
play. The next trial is the optional fair-weather winnowing output in
roadmap 34. Ordinary seed work remains the baseline.
[Delivery](Verification/MachineSystems/MacCandidateBD469BC21/README.md)

**A loaded turn and side parking — November 25 UTC, 2026.**
An eligible fresh v24 timber opening can describe the dry shoulder after
the base site is accepted. In Editor play an actual sled and pallet turned,
parked aside and separated using ordinary actions. Fresh Continue matched
the active grip and the parked state. The player crossed the opening,
returned by the outside walk and reached both sides of the original cache.
The exact sled and the finite intact source were preserved. All 318
targeted cases passed. The run used one supplied canonical sled.
[Evidence](Verification/MachineSystems/TimberSledShoulder20261125/README.md)

**Mac delivery for seeded loading approaches — November 25 UTC, 2026.**
Runtime `24fd59ec2` is verified on main and packaged for arm64 and x86_64.
GameBootstrap startup and package integrity passed on the Mac. The short
and longer carried delivery play and fresh Continue ran in the Editor.
The next slice joins the canonical cargo sled to the existing dry timber
opening and checks a turn of the physical load itself.
[Delivery](Verification/MachineSystems/MacCandidate24FD59EC2/README.md)

**Seeded loading approaches — November 25 UTC, 2026.**
Fresh version 23 can choose a different physical porter approach to the
same kitchen. Each branch delivered a five-pound box containing two
measures of clay. Eight versus twelve actual steps produced five
versus six drams of supplier cost and forty ticks of arrival difference.
Both retained the exact saved results in fresh Continue. The connected run
passed 462 executable cases with one utility skip. The next route trial
should qualify the actual loaded sled or drag footprint.
[Evidence](Verification/MachineSystems/WaysideLoadingApproaches20261125/README.md)

**Mac delivery for delegated search — November 25 UTC, 2026.**
Runtime `35d2c5d7e` is verified on main. The new universal Mac package
passed startup and package integrity checks. Editor gameplay established
the search walk, ordinary trade, report, late delivery and reload. Continue
the next eligible loading approach trial in the 45-requirement roadmap.
[Delivery](Verification/MachineSystems/MacCandidate35D2C5D7E/README.md)

**Delegated kitchen search — November 25 UTC, 2026.**
One optional kitchen hand can accept a saved two-dram search in a fresh
version 22 wayside delivery layout. Orven kept ordinary trade open. The
worker reported in person and returned to his original stand before the
actual late box closed the case and stocked trade. Editor play used a
controlled delay on the existing porter. Fresh Continue matched 23 selected
fields. The final connected run passed 392 executable cases with one
utility skip. Broader staffing, delivery routes and full regression remain
open. [Evidence](Verification/MachineSystems/WaysideDelegatedSearch20261125/README.md)

**Mac delivery for local porter and seasonal work — November 25 UTC, 2026.**
Runtime `e87ae17a8` is verified on main and packaged in a new universal Mac
build. GameBootstrap startup completed on the Mac. Connected gameplay and
reloading were verified in separate Editor runs. The full regression remains
open. Continue with the delegated search trial in the local porter design.
[Delivery](Verification/MachineSystems/MacCandidateE87AE17A8/README.md)

**Porter recovery and connected world work — November 25 UTC, 2026.**
The earlier delivery and timber evidence review verified 84 checkpoint files.
It found that a refilled clay stack could keep its original identity and
close the wrong case. Successful withdrawal now records that the parcel has
been unpacked. Ordinary shelf storage and collection still work. The live
counter used the original clay, and the native counters used one substitute
measure before and after reload. The integrated run passed 615 cases with
one utility skip. Fresh Continue matched 23 selected fields. The inherited
full-project regression remains open.
[Evidence](Verification/MachineSystems/PorterParcelIntegrity20261125/README.md)

Continue the 45-requirement queue with one actual delegated search and
returned report. Establish a worker and a finite offer from the kitchen
before extending the existing assignment. The supplier at the loading point
has an independent role and the current personal search remains available.
Keep the 30-minute check-in for recovery and advance eligible slices
consecutively during a run.

**Seasonal knotflax and public frame candidate — November 25 UTC, 2026.**
Fresh version 21 can offer a finite knotflax bundle in the second saved
seasonal phase. The same fixed frame can collect rain and winnow that bundle
into an ordinary seed. In seed 65 Editor play, native gathering, both frame
actions, ordinary planting and manual watering produced a persistent wet crop.
The clock and an empty flask were supplied. The independent source review
rechecked the transaction fixes. Publication and packaged acceptance remain
open. [Evidence](Verification/MachineSystems/SeasonalSeedFrame20261125/README.md)

**Reclaimable heavy opening candidate — November 25 UTC, 2026.**
Fresh Spread version 20 can place a finite timber pallet in the optional
heavy opening. A strong traveler can pull it aside. Dismantling it opens
the same gap and yields two portable boards. The outside walk is still
available. Seed 1 Editor play crossed after each action. It also carried both
boards back to repair the garden wicket. Fresh Continue matched the hauled
state and the repaired state, including the spent source and untouched cache.
All 237 targeted native cases passed. A follow-on aligned the Focus hint with
the action menu. All 112 targeted cases passed, and opening and cancelling
the menu was free in Editor play. Independent review and delivery remain open.
[Evidence](Verification/MachineSystems/TimberOpening20261125/README.md)

**Seeded wayside clay alternatives — November 25 UTC, 2026.**
Fresh Spread version 19 can choose one safe finite sorting lot by the kitchen
or keep the existing paid physical shipment. Both share opening trade and
the same pan repair. In fresh seed 2, ordinary exploration found and carried
the lot. A supplied hand sieve separated two fire clay and one grit. In seed
64, the real delivery stocked two prepared clay that the player bought. Both
methods restored the same meal service and the selected saved states survived
fresh Continue. All 518 expanded native cases passed. Independent final
review, publication and Mac delivery remain
open. This evidence concerns one kitchen's materials and receiving routes.
[Evidence](Verification/MachineSystems/WaysideClayLayouts20261125/README.md)

**Recovered clay shipment candidate — November 25 UTC, 2026.**
The player can carry the original crate from the scene to the kitchen shelf.
Orven takes the original contents into normal stock and closes the notice once
he is available at the shelf. The same failed transit and settled fare remain
on record. Starting from the prior report save, actual play picked up the crate,
deposited it, bought the same two measures, and restored the batch pan. Fresh
Continue matched the stocked and repaired checkpoints. All 470 expanded native
cases passed. Independent final review, publication and Mac delivery remain
open. Work on generated layouts can use the existing cold placement and route
checks. A delegated search still needs a real available worker and payer.
[Evidence](Verification/MachineSystems/WaysideRecovery20261125/README.md)

**Wayside recognition candidate — November 25 UTC, 2026.**
Orven can recognize the actual missing porter while outbound and bring the
news back to the kitchen. A controlled delay held the porter during a
player-caused combat branch from the earned overdue save. Ordinary spells and
dagger attacks left the real body and crate on the ground. Orven was shaken,
then resumed his search, recognized the body, and updated the receiving shelf
on return. Fresh Continue matched the observed and returned states. Physical
cargo recovery, delegated searches, independent review and publication are open.
All 467 expanded native cases passed.
[Evidence](Verification/MachineSystems/WaysideRecognition20261125/README.md)

**First visible wayside search candidate — November 25 UTC, 2026.**
Orven can follow one saved search from an open overdue notice to the actual
loading point and back. Existing idle AI, safety and paid meal work govern
when he acts. A controlled interruption from the earned overdue checkpoint
kept the porter in place. Through ordinary player movement and Examine,
Orven was observed on his outbound route, at the marker and back at the
kitchen, where normal trade was available. Fresh Continue matched the outbound
and returned saves. With the endpoint guard in place, fresh Continue advanced
the same walk, and a controlled loading-marker displacement turned Orven back
to the original shelf. All 458 expanded native cases passed. A finished search
leaves the shipment issue unresolved until receipt or another supported outcome.
Independent review, evidence recognition and delegated search remain open.
[Next recognition slice](WORLD-LOCAL-PORTERS-AND-SHIPMENTS.md)
[Evidence](Verification/MachineSystems/WaysideSearch20261125/README.md)

**Wayside delivery deadline candidate — November 25 UTC, 2026.**
The selected NPC order now saves its expected time and one optional overdue
notice. In ordinary seed 64 play, a player read the agreement, left the area
and crossed the grace period on the world map. The worker and cargo stayed
put. On local return Orven posted the notice. The same physical crate arrived
and closed it before unpacking. Fresh Continue matched both open and closed
states. The earlier carrying save retained its original terms. All 451
targeted native cases passed. Independent
review and search behavior remain open.
[Evidence](Verification/MachineSystems/WaysideDeadline20261125/README.md)

**Wayside kitchen shipment candidate — November 25 UTC, 2026.**
Requirement 25 has a local candidate. A selected supplier at a compatible
fresh wayside kitchen books the existing porter service with a finite purse.
The same two-measure clay box moves from supplier to porter to shelf. Orven
stocks the exact delivered clay and leaves his opening clay and food in place.
In ordinary seed 64 play the porter held the box at 160, delivered it at 170,
and was paid five drams in total. Orven offered both lots by 190. The player
bought the delivered lot for 11 drams and repaired the pan at 210. Separate
fresh loads matched the selected cargo and repaired-pan checkpoints. All 446
targeted native cases passed. A first
live layout exposed the unreachable chair case and the new shelf fits within
reach of both regular cook positions. Independent final review and publication
remain open. Deadline source work can use the committed physical shelf
receipt as the end of transit and the existing saved game clock.
[Evidence](Verification/MachineSystems/WaysideShipment20261125/README.md)

**Paid local porter action integrity — November 25 UTC, 2026.**
Two native cases reproduced stale hire and cancel actions affecting later
bookings. Quotes now carry the last order ID and cancellation carries the
active order ID. The amended candidate passed all 253 targeted cases.
From the earned carrying checkpoint at 390, actual play returned the same
box and refunded seven drams at 400, then accepted a new quote for that box.
Fresh Continue at 410 matched the new order, cargo and escrow. Independent
final review and publication remain open. The source work for requirement 25
identified Orven's kitchen as a candidate for a separately owned finite
clay shipment with a real trade and pan consumer. The active automation
follows the 45 requirements and starts the next eligible slice immediately
within each run.
[Evidence](Verification/MachineSystems/LocalPorter20261125/README.md)

**Paid local porter candidate — November 25 UTC, 2026.**
Requirements 21–24 and 28 now have a working paid-route candidate. The draft
generates a carrier and a loading marker at the existing Gleaners' fittings.
The marker owns the saved order and actual currency. Booked cargo remains
reserved, then moves through the worker's inventory to the normal shelf.
Ordinary seed 64 booked for eight drams, fresh-loaded at 390 with one earned
and seven held, received the same box at 670, and used its clay at the well at
700. A separate carrying branch cancelled and fresh-loaded at 400 with the
same box returned and seven drams refunded. Native order scaffolding failed
all four initial cases as expected. The expanded native and gameplay evidence
is indexed in the receipt. The candidate remains local pending independent
final source and recorded-play review. No new Mac package. Compare exact
working-tree paths, keep the branches separate, and avoid repeating the earned
positive route. Next source work is the finite NPC shipment adapter under 25.
[Evidence](Verification/MachineSystems/LocalPorter20261125/README.md)

**Physical receiving at Gleaners' — November 24 local / November 25 UTC, 2026.**
The first delivery slice now has a real two-measure box with loaded mass and a
one-box public shelf by the lined well. Native 148/148 after two real review
REDs. Seed 64 bought and packed two measures, carried the five-pound box and
deposited it while the well was still split. At 550 the shelf and exact contents
survived fresh Continue. The player retrieved the same clay, repaired the well
and drank at 580. Final fresh Continue retained the spent clay, usable well and
reusable empty box. Actual hostile interruptions mean this run does not earn a
safe porter route. Independent source and recorded-play review completed. Next
start the paid quote, stable order record and reserved physical handover, then
wire one available actor and route. The Mac package still predates this slice.
[Evidence](Verification/MachineSystems/LocalReceiving20261125/README.md)

**Compatible clay trial — November 24 local / November 25 UTC, 2026.**
This connects the first material suitability and common-result requirements.
Fresh wayside cooks have two prepared measures for sale in addition to their
existing food. The well and batch pan accept fire clay, prepared lining clay or
a mix. The same saved repair condition enables each fixture's existing use.
Ordinary seed 64 bought the clay at tick 190 for 11 drams. Separate branches
repaired the pan at 200 and restored and used the well at 300. Three selected
F5/fresh comparisons match. A kitchen batch was outside this play. Expanded
native testing passed 329/331, with both retained failures reproduced on the
prior source and all new repair and cook placement cases passing. Independent
source and recorded-play review passed within the tested scope. The current Mac
still carries `7ecee70b3`. Next, establish a physical parcel and receiving shelf
for manual delivery and the well, then add one paid carrier on the same route.
The scheduled queue follows the 45 requirements and starts each unblocked slice
as soon as the preceding one finishes.
[Evidence](Verification/MachineSystems/CompatibleRepairMaterial20261124/README.md)

**Current Mac delivery — November 24 local / November 25 UTC, 2026.**
Runtime `7ecee70b3` is packaged with the verified grit and size screening.
The universal build succeeded with zero errors and five existing warnings.
The owned player reached completed startup and closed. Package review passed,
including all 186 application files and the 437 prior Part field shapes.
Packaged gameplay remains open. The next slice scopes compatible lining clay
to the existing well and batch pan repairs.
[Delivery receipt](Verification/MachineSystems/MacCandidate7ECEE70B3/README.md)

**New replayability/overdue-shipment direction — November24local/25UTC,2026:**
[45 implementation requirements](WORLD-OPPORTUNITY-IMPLEMENTATION-ROADMAP.md)
separate existing foundations from missing compatibility/outcome/transport/
generation/seasonal/social work. Preserve alternative builds/tools, optional
investments and baseline comfort. Porters have an agreed expected date; an
overdue recipient can search personally or send real available staff, so a
player who killed the porter may meet those actual searchers. Save one bounded
case, finite actors/budget, real route/reports/evidence and no omniscient guilt.
Reconcile due dates with unloaded local pause before enabling; no fabricated
offscreen travel/discovery or automatic extra fees. This remains design only.
See [porter architecture](WORLD-LOCAL-PORTERS-AND-SHIPMENTS.md).

**New local-porters design — November24,2026:** User requests paid local
physical-load carrying and selected visible NPC/vendor shipments, composed from
shared Parts. See [architecture and source map](WORLD-LOCAL-PORTERS-AND-SHIPMENTS.md).
Further direction: similar practical outcomes admit different builds and tools;
baseline service works without prescribed road repairs. Optional lasting
improvements compete with other approaches. Seeded roads/movable loads vary
layouts without rerolling saved sites or making basic access tool-dependent.
Design only; no porter implementation/actual play. Bounded independent critique
finished0remainingmaterialP1/P2 after durable reservation/import/survival
clarifications; native RED/implementation follow current sieve qualification.
Capstan/cultures remain parked, graphics cancelled and Deck deferred. All twelve
complete-machine gates remain open.

**G2 qualified checkpoint — November24local/25UTC:** The size-partition, optional
finite clay-spoil source and one-shot guided start have bounded Editor
qualification. Native final f156/156,0CS includes51 new/current G2 cases and105 retained
material/repair/grit cases. Earlier d51/47pass/4review RED and e51/50pass/1rollback
RED exposed real Handling-weight admission, optional heap identity and limited
same-ID rollback defects, now addressed in f. Exact-source membership refuses
an alias; scoped rollback does not reinsert a colliding identity or erase the
independent alias. Main compiled0consoleerrors/stopped/rootnull/clean after
refresh. Actual guided seed65 craft20/realharvest200/readers-freecancel200/
screen210/wellrepair240/freecoolwater240/grit250, F5/all4/fresh210/250 selected
exact, source and recorded-play/artifact review0remainingP1/P2. Seed64 died129
before acquisition, preserved separately. All8earned external files retained;
four isolationtokens finished/temporaryroots cleaned. Fresh86foreign861normal
bodies unchanged. Mainstopped/rootnull/clean/profileroff/originalviews/inputrestored/
unlocked/seed0. Native input only, guided initial address/raw construction+forge
supplied, real mixed source/products not granted. No normal-route discovery,
new oily-surface trial, thirst relief, fullsuite, NPC, wholemachine or Mac claim.
Exact publication follows; coherent Mac then porter P1/compatible outcome work.
See [evidence](Verification/MachineSystems/GeneralScreening20261124/README.md).

**G1 continuation — November24,2026:** One new bounded consumable-grit field
capability is native/supplied-play qualified: e139/139, f32overlapping post-label
polish,0CS; source/recorded-play review0remainingP1/P2 after earned allocation and same-ID
custody REDs. Native pickup20, free readers/cancel, scatter30, ordinary step40
and last-unit refresh50 retain oil/40HP; two selected fresh pairs match. All12
checkpointfiles/three inspected PNGs retained; four isolation sessions finished
and temporary roots cleaned,
main stopped/rootnull/clean/inputrestored/unlocked/RequestedSeed0. Fresh861normal
and86foreign tracked bodies unchanged. Ordinary-source trial10 found no mender
in the real default11.10 start and obtained no grit. No generic sieving, mixed
clay source, repair-combination, NPC or Mac acceptance. Next: finite dry-clay
source and capability-driven size partition; ordinary acquisition must be earned.
Keep other consumer streams medium-depth; all12 complete machines open. See
[G1 evidence](Verification/MachineSystems/GritMaterial20261124/README.md).

**Sieve content continuation — November24,2026 (America/Chicago):** The user
asked to continue the content and uses brainstorm. Four concrete optional loops
now cover clay/repairs/grit, abrasives/lasting finishing, shavings/packing/freight
and detached seeds/planting. Fourteen wider streams and encounter cues connect
materials to practical consumers and later projects. They are proposals, not
implemented capabilities. Preserve ordinary harvests, gear, freight and services;
seasonal opportunities change available work/materials without compulsory upkeep.
Next implementation remains shared suitable-grit traction, then a finite dry-clay
lot and real size-based screening; proposed initial lot is three weight containing
two known clay plus one grit, separately from the unchanged four-clay bank.
Other consumers must exist before their stocks. No runtime/tests/Unity/build
change in this content pass. See [content plan](MACHINE-GENERAL-SIFTING-CONTENT.md).

**General sieve content qualification — November25UTC,2026:** Independent
bounded source/design critique is complete for the first four proposed streams.
Optional dry fire-clay-working spoil is the leading candidate because fines feed
existing repairs; suitable coarse-grit traction would be a new capability and
must be qualified first. Preserve the existing four-clay bank harvest and direct
Emberwheat/flax benefits. Ash, peat and workshop names provide no recoverable
budget; Grove compost rows are bodies, not gardening feedstock. Renewing
SourceStock is unsuitable for finite residue/mineral lots. Size/quantity/purity
and useful-byproduct gates remain open; no new runtime or source placement.
See [source qualification](MACHINE-GENERAL-SIFTING-CONTENT.md). Current runtime/
Mac995d16a36 and all twelve complete-machine statuses remain unchanged.

**Coherent Mac delivery — November24local/25UTC,2026:** Runtime995d16a36 is
pushed/remotelyverified and packaged at
`/Users/steven/coo-verification/builds/mac-995d16a36-20261124/CavesOfOoo.app`.
Three bounded capabilities: cargo sled, hand sieve and finite direct pump.
Their overlapping native finals352/190/220 and actual Editor/F5/fresh/source
and recorded-play reviews remain qualified separately. Build0errors/5exactA023
warnings,61.15871s/364386397bytes;32129Git/tar/SHA inputs,32132effective,
83changed0missing3added/exact86overlay,592external/186playerpins. SelectedCecil
250checks(217retained33new),4newParts/all433priorfieldshapesretained/current437.
OwnedPID21936 startup2678->2679/7creatures,CLOSED;4normalMacsavesbyteexact/
18logicalprefsrestored. Fresh00:38:34UTC28apps5208files(27prior5022+186new),
84foreign861normal2otherforeignexact. Mainstopped/rootnull/clean/profileroff/
original7viewsindex0/inputrestored/unlocked/RequestedSeed0. Packageintegrity/
startup only; no controlled packaged input/Continue/save/render/Intel/controller/
FPS claim. Independent package/artifact review0remainingP1/P2. All12complete-machine/wider
failed-suite/discovery/balance/NPCgatesopen. See
[delivery](Verification/MachineSystems/MacCandidate995D16A36/README.md).

**New user direction — broader sieve content:** The current explicit packet is
only a starter. The user wants a general tool for separating smaller/larger
granules, rocks, minerals and other material, and asked first to brainstorm the
missing content plus uses for both fractions. Candidate mixed streams include
clay-bank spoil, road/masonry gravel, workshop/kiln residue, eligible spent-wood
fire residue, optional threshing/seed-cleaning lots, finished compost, peat,
saltwork breakings and pigment/quartz/herb preparations. These are proposals;
new source budgets, output identity and concrete consumers need qualification.
Screening separates size, not mineral identity or contamination. Keep ordinary
harvest/crafting/services convenient; bonus mixed finds and local projects should
broaden useful work without compulsory chores. No new sieve-content runtime is
implemented by this package.


**Direct hand pump — November24local/25UTC,2026:** Third bounded capability in
the sled/sieve/pump batch has native and recorded Editor qualification. Finalg
220/220=30pump+190retained,0CS; independent source0remainingP1/P2 after three real
rollback REDs were fixed. Actual supplied normalStrength18/40HP: forge20,
reader/freecancel20, stroke30(source40/outlet20)/fresh30, same-pump strokes40/50
empty source2142 and its legacy acid projection into same60-unit outlet2144.
Safe step onto former pool60 retains40HP/fresh60; separately preserved outlet
exposure80 causes acidcoat36/HP37, retaining60acid. Three F5archives/all12files,
two ID-sorted selected fresh pairs andonePNG. Mainstopped/rootnull/clean/input
restored/unlocked/RequestedSeed0; four isolation tokens finished. Failed queued
compilation fresh attempt preserved/excluded; no ordinary acquisition, whole-world,
installed sump/access, NPC, full-suite or hardware claim. CurrentMac a02338565
predates allthree; exact publication then coherent delivery pending. Independent
recorded-play/final wording review0remainingP1/P2. See [contract](MACHINE-DIRECT-LIQUID-PUMP.md)
and[evidence](Verification/MachineSystems/DirectLiquidPump20261124/README.md).

**Portable hand sieve — November24,2026:** One new bounded screening capability
has native and recorded Editor qualification: reusable sound carried sieve,
finite authored packet -> one pale-salt/two silver sand, explicit two-weight
waste. Native e190/190=19new+171retained,0CS; independent source0remainingP1/P2.
Real late-cancellation injury rollback RED1 fixed by explicit material receipt
ownership. Actual separate supplied branch: forge20, walk70, free reader/cancel70,
screen80/fresh80, existing sand note90, same-sieve reuse100, existing dagger
infusion160/fresh160. Selected fresh pairs exact, three F5archives/all12files,
one PNG. A separate ordinary search1760 is preserved but obtained no packet;
opened real cache contained quartz only. No ordinary-acquisition/economics claim.
Final main stopped/rootnull/clean/inputrestored/unlocked/RequestedSeed0. Current
Mac a02338565 predates sled/sieve; coherent delivery follows broader batch.
Independent recorded-play/final wording review:0remainingP1/P2. Exact-owned
publication follows; native/recorded bounded acceptance is complete.
All twelve complete-machine/full-suite/NPC/hardware gates remain open. The direct
liquid pump remains proposed only. See [contract](MACHINE-HAND-SIEVE.md) and
[evidence](Verification/MachineSystems/HandSieve20261124/README.md).

**Cargo sled — November 24, 2026:** One new bounded local hauling capability
has native and recorded Editor qualification. Final h352/352 includes25 sled
cases (two ordinary controls) and327 retained adjacent cases, zero compiler
errors. Independent design/source/recorded-play and final wording reviews found
0 remaining P1/P2 after two real callback failures were reproduced and fixed.
Actual supplied Strength18/40HP route: forge20, fit30, same-owner hauling43/55,
blocked step55, F5/fresh55 with active grip, release55/Speed100, original-sled
recovery65, existing pallet dismantle75 and field-haft preparation85/fresh85.
Three checkpoints/all12files, two exact selected fresh pairs, three PNGs. Added
six physical weight and half hauling penalty use shared handling, movement and
energy accounting; no extra pulling strength, general cross-zone cargo or
capstan. Normal alternatives remain. Current delivered Mac a02338565 predates
this increment; batch another useful connection before coherent package work.
Fresh22:51:53UTC scoped84foreign/861normal/twootherforeign/27apps5022files/fourMac
saves exact. Main stopped/rootnull/clean/inputrestored/unlocked/RequestedSeed0.
All twelve complete-machine, full-suite, ordinary acquisition, economics and
sustained NPC gates remain open. Screen/crossing are candidates, not shipped.
See [contract](MACHINE-CARGO-SLED.md) and [evidence](Verification/MachineSystems/CargoSled20261124/README.md).

**Route tools Mac delivery — November 24, 2026:** Runtime `a02338565` is
pushed and remotely verified. Current Mac:
`/Users/steven/coo-verification/builds/mac-a02338565-20261124/CavesOfOoo.app`.
Three new capabilities have 325/325 targeted native checks (29 new, 296 retained),
actual Editor forging, actions, readers and movement, three four-file F5
checkpoints, two selected fresh-load comparisons and four screenshots.
Independent source, recorded-play and package reviews found 0 remaining P1/P2.
The Mac build has 0 errors and the same five preceding warnings; 217 selected
assembly checks passed (188 retained, 29 new). All 429 prior Part field shapes
remain unchanged. The owned startup reached 2,703 ready entities and seven
creatures, then closed. Four normal Mac saves remain byte-exact and 18 logical
preferences were restored. All 27 apps / 5,022 files remain preserved within the
recorded scope. Main Editor is stopped, root null and scene clean, with original
views and input settings restored and reload locks released.

Package qualification covers integrity and startup only; controlled packaged
play, Continue, actions, saves, rendered-image review, Intel execution, controller
and comparable FPS remain unverified. All twelve complete-machine gates, the
failed wider regression, ordinary discovery, balance and sustained NPC behavior
remain open. Next broaden useful material and inhabited-place connections at
medium depth. Active chat work need not wait for the 30-minute schedule.
See [delivery](Verification/MachineSystems/MacCandidateA02338565/README.md).

**Route work tools — November24,2026:** three new bounded connections now have
native/recorded Editor qualification: bellows move the same transient gas owner;
silver sand lays player-authored navigation marks; an actual singleton spike
braces shared ordinary doors and returns with its identity. Nativei325/325
(29new/296retained),0CS. Actualforge20/brace40/mark50/fresh50/recover60/open+cross120/
brush140/north150/poison160/bellows170/fresh170. Movedcloud19atcommit/18afterordinary
decay through reopeneddoor,390HPstillpoisoned; no detox or permanent safe-areaclaim.
Three F5archives/all12files/twoselectedfreshpairs; fourPNGs. Initial pause-navigation,
invaliddiagnostic/test attempts and one backgroundpure-readtimeout retained/excluded.
DoorVillage hasnoDestructiblePart: no ordinarydoorbreaking claim. Throwaimcursor42
versusactualdiagnosticcenter40 disclosed, no accuracyclaim. Source and recorded-play review0P1P2;
exact-owned publication/coherentMacnext. Mainstopped/rootnull/clean/profileroff/
original7viewsindex0/inputsettingsrestored/reloadlockreleased. CurrentMac152bpredates
thisbatch. All12complete-machine/full-suite/discovery/balance/sustainedNPC/hardware
gates stayopen. See [contract](MACHINE-ROUTE-WORK-TOOLS.md) and
[evidence](Verification/MachineSystems/RouteWorkTools20261124/README.md).

**Field tools Mac delivery — November24,2026:** corrected runtime152b2cdcf
(three tools4a plus shared rod-blocking fix) is pushed/remotely verified and has
currentMac `/Users/steven/coo-verification/builds/mac-152b2cdcf-20261124/CavesOfOoo.app`.
Native248/248 (49new/199retained), three distinct capabilities; eight F5 archives/
32files/seven selected fresh pairs. Source/recorded-play review0P1/P2. Mac build
0errors/5exact9awarnings64.18378s364300237bytes;32103Git bodies/32106effective/
86exactoverlay/592external/186playerpins,188selected assembly checks143retained+
45new. One new Kind/ChargePart; all428old Part fields retained. Owned82514startup
2668->2669/7creatures/window71789bounds640x428; closed,4normal saves byte-exact/
18logicalprefsrestored. No packaged controlled input/Continue/actions/save/render/
Intel/controller/FPS claim. Fresh scoped26apps4836files/84foreign861normal2other
foreign exact. Main stopped/rootnull/clean/profileroff/7viewsindex0/inputrestored/
reloadlocksreleased. All12complete-machine/full-suite/discovery/balance gates open.
Final independent package review recorded in delivery receipt; next3–5newdistinct
medium-depth connections, especially town/route benefits and ordinary discovery.
See [delivery](Verification/MachineSystems/MacCandidate152B2CDCF/README.md).

**Shared rod path — November24,2026:** one-line integration correction uses
Cell.BlocksMovement for the two-cell intermediate path, preserving existing
solid, closed-door, sealed-barrier and Morrowfast rules. NativeREDu2fail/2controls
-> final248/248 (49new/199retained),0CS. Current-code native clear170->180 and
fresh180 retain expected charge0/HP400; all4files archived. Batch now8checkpoints/
32files and7normalizedfreshpairs. This is0newcapabilities; three field tools below
remain the additions. Independent bounded source and final recorded-artifact review0P1/P2. Main stopped/rootnull/clean/inputrestored/unlocked. Runtime4a is
pushed; this correction ships separately without rewriting it. The prepared4a
Mac export was not built and is preserved; coherent package from final corrected
runtime next. Current boundedMac9a remains prior delivery. All12complete-machine,
failed-full-suite, ordinary discovery/balance and hardware gates remain open.
See [correction](Verification/MachineSystems/FieldWorkTools20261124/shared-line-correction.md).

**Field work tools — November24,2026:** three new bounded capabilities are
qualified in native tests and recorded Editor play: five-fuel aimed hearth heat,
one-root/one-charge portable discharge with two-cell straight reach, and finite
steam-water catching using the existing elected proofing source stock. Three
forge manifests consume existing iron/clay/cord; recipes are not extra features.
Final native245/245 (46new/199retained), independent current-source/recorded-play
review0remainingP1/P2. Actual native forging70, paid thaw140, loaded-rod fresh170,
two-cell discharge180, water catches230/240, ordinary hearth cooling/fresh310,
free cancel/walking/save330, and natural wet-viper shock/death/fresh460 qualify
these connections. Native pickup of its ordinary death-loot root and reload/fresh490
retain one discharge, corpse/waterpool1, source1/3 Last220 and player398HP.
Seven F5 checkpoints/all28files archived before cleanup; six fresh selected pairs
match after sorting fixture enumeration byID. Five PNGs total;03 is an excluded
failed menu attempt. Actual F1 exposed token invalidation: current-quote retention
now covers tool and shared processing menus, with nativeREDs. Steady steam uses
its declared source, with frozen admission retained; passive thermal cooling does
not impose reheating. Main stopped/rootnull/clean/profiler-off/original7views/index0,
input restored and owned assembly locks released.19:41UTC scoped84foreign,
861normal,2otherforeign and25priorapps4650files exact. CurrentMac9a predates this
batch; exact-owned publication and coherent delivery next. All12complete-machine,
failed-full-suite, ordinary-discovery/balance and hardware gates remain open.
See [tool contract](MACHINE-FIELD-WORK-TOOLS.md) and
[evidence/exclusions](Verification/MachineSystems/FieldWorkTools20261124/README.md).
Root sole Unity/Git owner; ACTIVE every30minutes. Graphics cancelled;
capstan/cultures parked; Deck deferred.

**Worksite Mac delivery — November24,2026:** runtime
9a0d55f9303b76c033af98ee28a0d985663c3624 is pushed/remotely verified and now has a
current bounded universal Mac package. Three distinct additions: spare leather
armor recovery, selected own-box material work and lossy field rebinding. Native
287/287 (72new/215retained); actual Editor menus/readers/movement/fresh states
and independent source/recorded-play review0P1/P2. Package0errors/5exactprior
warnings62.790529s;32097Git/SHA source32100effective/86exactoverlay/592external/
186playerpins.143selected assembly checks;428Part names/fields unchanged,
including immediate6b. OwnedPID75323startup2688->2689/7creatures/window71754
640x428only; no packaged input/actions/save/render/Intel/controller/FPS claim.
4normalMacsaves byte-exact/18logicalprefs restored. Fresh18:18:40UTC25apps4650files,
84foreign861selectednormal2otherforeign4Macsaves scoped exact. Main stopped,
rootnull,scene clean,profileroff,original7views/index0/inputrestored. Final independent package
review0remainingP1/P2. All12complete-machine/full-suite/discovery/hardware
gates stay open. See [delivery receipt](Verification/MachineSystems/MacCandidate9A0D55F93/README.md).
Next qualify another3–5distinct medium-depth tool/material/lasting-project
connections; do not replay this accepted batch as new work. Root sole owner;
task ACTIVE every30minutes. Graphics cancelled, capstan/cultures parked, Deck deferred.

**Worksite connections — November24,2026:** accepted three bounded new
capabilities: explicit plain unworn LeatherArmor -> one cured offcut using a
cutter; supervised MaterialProcess work from the selected adjacent installed own
CampBox's direct contents; field replacement of only an untempered carried
singleton weapon's binding, old binding destroyed, same weapon/head/haft kept.
Normal forge refits retain refunds. Native287/287 (72new/215retained), targeted
RED/countercases and independent source/recorded-play review0P1/P2. Actual native
armor/box1840->1860/fresh, stored-armor F1/cancel/execute1890/fresh, and earned axe
4420->4430/fresh/equip/walk4450 qualify this scope. All32 checkpoint files archived
before Finish;5gamePNGs include1excluded initial menu attempt. Main stopped,
rootnull,scene clean,profileroff,7views/index0/inputrestored. CurrentMac6b predates
this batch; publication/coherent package next. All12complete-machine/full-suite/
ordinary-discovery/hardware gates stay open. See [worksite receipt](Verification/MachineSystems/WorksiteConnections20261124/README.md).

**Camp safety Mac delivery — November24,2026:** runtime
6b5421a82567a1085d767b5289b06e25fefbb8bb is the pushed/remotely verified current
bounded Mac package.9line movement guard protects installed player-held camp
fixtures from accidental bump breaking; deliberateC Break remains. This is
0newcapabilities/1correction, not padded progress toward the3-5new-feature target.
[Delivery receipt](Verification/MachineSystems/MacCandidate6B5421A82/README.md):
0errors5exactpriorwarnings56.062199s/32085Git-SHA source32088effective/86exact
inputoverlaybodies/592external186playerpins.110selected assembly checks, all428
Part names/fields unchanged, InputHandler fields unchanged versus7a. Final
independent package/artifact review0actionableP1/P2; no reviewer native actions.
OwnedPID98430startup log2687->2688/7creatures/window71710bounds640x428only; no
PNG/renderjudgment, packaged input/action/save, Intel/controller/FPS acceptance.
4normalMacsaves byte-exact,18logicalprefs restored. Fresh16:32:06UTC24apps4464files,
84foreign861selectednormal2otherforeign4Macsaves scoped exact. Main stopped,
rootnull,clean,profileroff,original7views/index0/inputrestored. Accepted Editor
bump/fresh1840and explicitBreak1850 are separate recorded evidence; all20files
archived before Finish,3screenshots. Preserve intact fresh1840 for worksite
trials, not destructive1850; laterproofing2330/2150 and ordinary2440 retained.
Next implement distinct source-qualified worksite capabilities: plain armor
recovery and explicit own-box materials are proposed; qualify a third real gap
rather than repeat recipes or the existing paid artisan repair. All12complete-
machine/fullregression/W1b/W2/ordinarydiscovery gates remain open. ACTIVE30m.

**Camp movement safety — November24,2026:** fixed accidental bump destruction
of installed player-held camp fixtures. Actual trace showed CampBox2150 survived
all distil/trap work at14HP/25C, then a normal south bump destroyed it through
legacy break-through input. New9line guard refuses freely and points to C move/
take-up/deliberate break; generated/handed-over/tag-only obstacles retain breaking.
Valid nativeRED10/5fail5controls0CS; final104/104 including10new94retained, no
full-suite clearance. Two invalid test-compile attempts and a diagnostic-channel
fixture failure are retained/excluded. Actual matched1840south bump preserves
14HP and tick/position; fullfresh1840same and free2page reader. ExplicitC Break
still destroys it for one paid action1850, separate preserved counterbranch.
All4F5files archived before Finish; 3screenshots; no fullworld/savebyte equality.
Final independent source/recorded-play review0remainingP1/P2; currentMac7a
predates this correction, coherent package next. [Receipt](Verification/MachineSystems/CampBumpSafety20261124/README.md).
Fresh16:24UTC84foreign861normal2otherforeign23apps4278files4Macsaves scoped exact;
main stopped/rootnull/clean/profileroff/original7views/index0/inputrestored.
This is0newcapabilities,1correction. Two promising new worksite candidates,
plain armor recovery and own-box work supplies, remain unimplemented; conditional
companion repair demand unqualified. Do not count existing paid artisan repairs
or duplicate recipes as additions. All12complete-machine gates/failed full suite
remain open. Root sole owner, task ACTIVE every30minutes. Preserve actual intact
box branch /private/tmp/coo-camp-bump-fresh-earned-20261124, not destructive1850;
later proofing2330/2150 and ordinary2440 remain separate and preserved.

**Expedition Mac delivery — November24,2026:** runtime
7a193eae699421f97cbd9f9ffe9dc45877389697 is pushed/remotely verified and now the
current bounded Mac package. Three new capabilities: finite brine distillation,
water acid-rinse and recoverable unsprung loaded traps; separate shared fish
corrections. [Delivery receipt](Verification/MachineSystems/MacCandidate7A193EAE6/README.md)
records0errors/5unchangedwarnings,57.488087s,32083Git/SHA source/32086effective
inputs/86exactoverlaybodies/592external/186playerpins.104selected assembly checks,
all428Part names/instancefields unchanged versus074d/088/238. Independent final
package/artifact review0actionableP1/P2; no reviewer native operations. Owned
PID68501startup log+window71695/640x428 only; no extra screenshot/render judgment,
packaged input/actions/save/Intel/controller/FPS acceptance.4normal Mac saves
byte-exact/18logicalpreferences restored. Fresh15:38:48UTC23apps4278files,
84foreign861selectednormal2otherforeign4Macsaves exact, scoped only. Main stopped,
rootnull,clean,profileroff,original7views/index0/inputrestored. Actual1840->2100/
fresh2100 and extra matched85acid control/rinse2150 remain Editor evidence,
not packaged gameplay. All12complete-machine gates/failed full suite/W1b/W2/
ordinary opportunity density remain open. Next implement another3-5distinct
medium-depth material/tool/project connections; reuse accepted evidence/builds.
Supplied2100/2150 and ordinary2440 remain separate. Root sole owner; ACTIVE30m.

**Expedition preparation — November24,2026:** three new capabilities implemented
and qualified through actual supplied GameBootstrap1840->2100/fresh2100: finite
4brine+5fuel->2water in one chosen flask;2water removes up to20acid and leaves Wet;
recover/re-lay unsprung loaded spring traps while spent traps stay spent.
Actual brine12->4/two2-water flasks,one timber,then one distilled flask emptied
on acid60->5 (20rinse+35normal dry-down). Hazard still dealt3damage; no healing or
complete protection claim. Old water3/second distilled2/brine4/one loadedkit
persisted exact selected-state fresh2100 at48,16HP352/50drams. Five screenshots,
free readers, all4save files archived before each Finish; compressed bytes differ.
Final valid RED74/53fail21controls0CS -> GREEN154/154, adjacent152/152 separately;
review RED3+4 and3mutants killed. Fish callback/whole-pond/legacy-holder fixes
separately102/102; no replay of older fishing actual1100. No new saved Parts/fields.
Independent source/recorded-play review including matched addendum0P1/P2.
Matched85acid extra branch saves3HP over equal3actions for2water; primary2100
fresh remains separate. Bounded Editor/native accepted. CurrentMac238 excludes
this batch. [Contract](MACHINE-EXPEDITION-PREPARATION-BATCH.md) /
[receipt](Verification/MachineSystems/ExpeditionPreparation20261124/README.md).
Fresh15:26UTC84foreign861selectednormal2otherforeign22apps4092files4Macsaves
exact; main stopped/rootnull/clean/profileroff/original7views/index0/inputrestored.
All12complete-machine gates/failed full suite/W1b/W2/ordinary discovery density
remain open. Next publish the reviewed batch and one coherent Mac, then broaden
another material/tool/project connection. Supplied fresh2100 retained; ordinary2440
separate. Root sole owner; task ACTIVE every30minutes,3-5new capabilities per run.

**Regional fuel Mac delivery — November24,2026:** runtime
238096389f84ff55c97dc7691b2bfe089b959799 is now the current bounded Mac delivery.
[Package receipt](Verification/MachineSystems/MacCandidate238096389/README.md)
records the isolated universal nondevelopment build:0errors/5unchanged warnings,
55.975677s; new -nographics build flag disclosed.32071committed Git/SHA inputs,
32074effective/86exact overlays/0runtime C# import changes;592external/186player
pins.75selected assembly checks/all428Part names and fields unchanged against
074d/088; no complete IL/behavior equivalence claim. Owned read-only Mac startup
PID60440/window71614 rendered world/save-choice/HUD at actual640x428, then closed.
4normal save files unchanged/18logical preferences restored; no controlled
packaged actions/save/Intel/controller/FPS claim. Independent final bounded
source/package/startup-artifact review0P1/P2; no reviewer Unity/player/native tests.
Fresh12:58UTC all22apps4092files/84foreign861selectednormal2otherforeign4Macsave
pins exact. Main stopped/rootnull/clean/profiler-off/original7views/index0.
The accepted resin->finite fuel->ordinary flame->still clay behavior remains
Editor/native/recorded-play evidence; prior acquisition/lacquer and clay/well
connections reused. All12complete-machine statuses/failed full suite/W1b/W2 and
natural opportunity density remain open. Next broaden a different useful
material/tool/project connection at medium depth; preserve supplied1100 and
ordinary2440, no replay of accepted branches. Keep screenshots to a handful,
usually3-5 meaningful views. Root sole owner; task ACTIVE every30minutes.

**Optional reagent fuel — November24,2026:** implemented; bounded native and
actual checks accepted after final independent recorded-play review0P1/P2. Stable
combustible reagents feed finite fires through their own C menu:1resin=5fuel,
1LampOil=15, actual clamped gain/surplus quoted, no heat or ignition. Strict local
payload/full-parser/ID/count/reach/commit guards and one-unit delta receipts;
no saved Part/field added. Attached Part receipt hook covers existing torch/
hearth kindling plus new fuel without another executor command switch.
Native250total/249pass/1Explicit utilityskip/0CS,45new; final45/45 after copy-only
polish overlaps. Valid RED34(18fail16controls), review2+4RED reproduced three P2s,
all fixed; three focused mutants killed. First test compile failure excluded.
Actual supplied1030->1100/HP364/50drams: normal waits cool hearth, two resin8->6
add4->9->14fuel while dark; normal relight ->13.75fuel, still8mire->1clay/25->26,
normal burn leaves3.5fuel. Timber26/moss4 kept. Fullfresh1100 selected exact;
free readers/F5/all4files-before-Finish passed. Compressed bytes differ, no whole-
world equality. Five screenshots; last precedes minor wording polish only.
[Contract](MACHINE-REAGENT-FUEL.md) / [receipt](Verification/MachineSystems/ReagentFuel20261124/README.md).
Regional resin acquisition/lacquer and clay/well consumer evidence reused, not
replayed. This actual feed uses fixture resin; no natural discovery/balance,
seasonal reuse or complete-machine acceptance. All12gates/failed full suite/W1b/
W2 open. CurrentMac088 predates this feature; coherent delivery next.
Retain supplied1100 `/private/tmp/coo-reagent-fuel-fresh-earned-20261124` and all
prior roots; ordinary2440 separate. Main stopped/rootnull/clean/original7views/
index0/profileroff; scoped84foreign861normal2otherforeign4Macsaves exact12:32UTC.
Root sole owner; task ACTIVE every30minutes; screenshots a handful, usually3–5.

**Source-caught cold coat — November24,2026:** unchanged runtime088; bounded
Editor integration accepted after independent recorded-play review0P1/P2. Earned540
seed64/400HP/Strength70, no new grants: one fixture-vent scrape -> rime2727 ->
flask2235 -> one split leather cap. Original three caps became two plain plus
one coated; free equip gave35cold. Equal normal frost-tonic doses at dry25C/
34,7:0resistance froze0.895 after first thaw, recovery110ticks;35resistance
froze0.545, recovery80ticks/coat93. Viper/poison injury400->364 retained and
excluded from the cold comparison. Source2389 stock5->4/Last820; two tonics spent.
Fullfresh930 selected snapshot exact, actual free cap reader/remaining93 passed;
both four-file archives verified before Finish. Compressed bytes differ, no
whole-world equality. Five real screenshots. [Receipt](Verification/MachineSystems/SourceProofing20261124/README.md).
No runtime/tests/build changes; no ordinary supply/natural discovery, environment
balance or immunity claim. All12complete-machine gates,W1b/W2/failed full suite
remain open; currentMac088 unchanged. Main stopped/rootnull/clean/original7views/
index0/profileroff; scoped84foreign861normal2otherforeign4Macsaves exact11:37UTC.
Retain supplied930 `/private/tmp/coo-source-proofing-fresh-earned-20261124` and
prior540; ordinary2440 separate/preserved. Next broaden after this bounded
prerequisite, no repeat depletion/pressing/coat or unchanged tests/builds.
Root sole owner; task ACTIVE every30minutes; screenshots a handful, usually3–5.

**Reusable hush-strip lane — November24,2026:** unchanged runtime088; actual
bounded qualification accepted, final source/evidence review0P1/P2. Supplied
proofing420 seed64/HP400/Strength70, no new grants: native plain steps stamped
Noise420/430; three paid placements8->5 and steps onto2710/2711/2712 retained430.
Step off stamped500. Paid recovery retired2712/stack5->6; re-lay made2714/6->5,
step onto it kept500. Fullfresh540 selected three devices/pack5/Noise500/HP400/
50drams/41,14 exact; fresh menu offered TakeUp, examining free. Archives verified
before both Finish calls; compressed bytes differ, no whole-world equality.
Five actual screenshots; future captures a handful, usually3–5 per meaningful
change, superseding older “frequent” guidance. No runtime/new tests/build; current
Mac088 unchanged. [Receipt](Verification/MachineSystems/HushLane20261124/README.md).
Hunter avoidance, ordinary supply, attack/cast counters, search expiry, regional
density,W1b/W2/failed full regression and all12complete-machine gates stay open.
Scoped84foreign861normal2otherforeign4Macsaves exact10:34UTC; main stopped/
rootnull/clean/original7views/index0/console0errors. Retain latest supplied540
`/private/tmp/coo-hush-lane-fresh-earned-20261124`; ordinary2440 separate/preserved.
Next broaden to a different connection; do not replay this bounded lane.
Root sole owner, scheduled task ACTIVE every30minutes.

**Ordinary timber circuit — November24,2026:** unchanged runtime088; actual
bounded Editor accepted after independent source/evidence review0P1/P2.
Same ordinary seed1498104061 continued610, no grants/reroll: signs and visible
route -> cellar1200 -> shared beam haul -> pallet dismantle1650 ->2timber ->
optional wicket repair2390 -> native open/cross/close2440. Surface warden killed
the first scrabbler after player injury/one tonic; cellar return interception
cost9HP, escaped with29HP/50drams. No player kill/loot claim. Pallet removed once;
timber4750x2 consumed. Fullfresh2440 selected gate/provenance, moved beam, spent
pallet and pack/HP/purse exact; fresh real menu/reader passed. Compressed save
bytes differ, no whole-world equality. Both four-file archives verified before
Finish.41real screenshots retain failed searches/encounters. No runtime/native
test/build changes or replay. One authored clearing/cellar; regional density,
stranger comprehension,W1b/W2, failed full regression and all12complete-machine
gates stay open. [Receipt](Verification/MachineSystems/OrdinaryReclamation20261124/README.md).
CurrentMac088 unchanged. Scoped84foreign861normal2otherforeign4Macsaves exact
09:44:50UTC; main stopped/rootnull/clean/original7views/index0/console0errors.
Retain latest earned2440 at `/private/tmp/coo-ordinary-reclamation-fresh-20261124`
and both prior roots; do not reroll/replay. Next broaden to a different
medium-depth connection after this checkpoint. Root sole owner, task
ACTIVE every30minutes, frequent real screenshots.

**Current Mac delivery — November24,2026:** runtime088 package accepted after
final independent source/package/startup-artifact review0P1/P2.
Runtime088029e00241a9b59e20d2cc0bcbf99dba69ae16 packages accepted Cinderhold supply
and water-menu clarity together. Universal6000.3.4f1/nondev,57.231673s364197149bytes,
0errors/5exact632warnings.32065Git/SHA source,32068effective/83changed0missing3added,
86overlayexact/0runtimeC#changes/592external186playerpins.57metadata/literal/call
checks; all428Partnames/instancefields equal074d and immediate632, not complete
IL equivalence or gameplay proof. Owned87186/window71597 actual640x428startup/
save-choice/world,7creatures2701generated2702ready; closed. Fresh no-request
post/AXfalse/screentrue; no input/activation/request.4normalMacsavefiles exact in
place,18logicalprefs restored. All21apps/3906files freshlyexact (20prior3720+186new),
84foreign861selectednormal2otherforeign exact08:59:51UTC; main stopped/rootnull/
clean/profileroff/original7views/index0. [Receipt](Verification/MachineSystems/MacCandidate088029E00/README.md).
Current `/Users/steven/coo-verification/builds/mac-088029e00-20261124/CavesOfOoo.app`.
Supersedes older632=currentMac references below. Controlled packaged interactions/save,
FPS/controller/Intel, failed full regression,all12complete-machine gates,W1b/W2
remain open. Root sole owner; task ACTIVE every30minutes; frequent real screenshots.

**Ordinary discovery / water-menu clarity — November24,2026:** bounded Editor
accepted after native, actual and independent source/artifact/gameplay-evidence review0P1/P2.
Real pack rows now reuse the existing truthful waterskin availability producer:
empty/no-fresh-water/full reasons, preserved d/f keys20/19priorities and enabled
costs. No service/quantity/source/saved-field changes. Native213total/212pass/
0fail/1Explicit utilityskip/0CS includes14new. Fresh ordinary Classic seed1498104061,
40HP/18Strength/50drams, no grants: visible gleaning/sign -> bypass wicket ->
natural drawgourd shell+seed -> actual3drink basin -> replant -> spend1water.
Postfix disabled selection free340; fill440 shell0->3/basin3->0; plant600/water610
shell3->2/crop2731progress1moisture39. Fullfresh610 selected snapshot exact.
29real screenshots include failures/search; one authored clearing example, not
regional density or W2 seasonal reuse. [Receipt](Verification/MachineSystems/OrdinaryDiscovery20261124/README.md).
Main stopped/rootnull/clean/original7views/index0/console0errors; fresh scoped
84foreign/861normal/2otherforeign/4Macsavepins exact08:42UTC. CurrentMac632 predates
this correction and Cinderhold supply; these can form a later coherent batch.
All12complete-machine gates, failed full regression,W1b/W2 stay open; root sole
owner/task ACTIVE every30minutes, frequent real screenshots. Ordinary earned610
is retained for later distinct exploration; do not restart or reroll it.

**Lasting Cinderhold project supply — November24,2026:** bounded Editor accepted
after native, actual and independent source/artifact/gameplay-evidence review0P1/P2.
Local stable/improved Pump OR existing Sealed adds one paid sounding rod to future
smith consignments. Temporary/remote/Running-only progress does not. Two ordinary
specials and staple floors remain; no current-lot rewrite or new repair cost.
Merchant-only bounded notebook identity grants no grouping/stack permission.
Native316total/315pass/0fail/1Explicit utilityskip/0CS includes34new cases.
Actual earned10660 old lot retained; ordinary walk/shop10810 produced g1 rod30387
plus battleaxe30385/warhammer30386. Paid57 (67->10); two-page readers, cancel,
surface refusal and F5/four-file-before-Finish passed. Full fresh10810 selected
snapshot exact; actual fresh shop did not refill/reroll. Strong inherited fixture,
no new funding/discovery/cave-sounding or physical drainage claim. Fresh84foreign,
861selectednormal,2otherforeign,4Macsaves exact; stopped/rootnull/clean.
[Receipt](Verification/MachineSystems/CinderholdProjectSupply20261124/README.md).
CurrentMac632 predates this extension; batch another coherent milestone.
All12complete-machine gates, failed full regression, W1b/W2 remain open.
Root sole owner; task ACTIVE every30minutes; show frequent real screenshots.
Next broaden to another useful medium-depth connection after publishing this one.

**Current Mac delivery — November24,2026:** runtime6325062a1cb3d9f2a1065fec5458f1d8b9b8959d
packages accepted knife guidance and portable fire-moss ignition together at
`/Users/steven/coo-verification/builds/mac-6325062a1-20261124/CavesOfOoo.app`.
Universal6000.3.4f1/nondev,57.154212s364196125bytes/0errors/5samepriorwarnings.
32061Git/SHA source pins,32064effective/83changed0missing3added/exact86overlay,
592external/186player pins. Cecil38feature/wiring/field checks; all428metadata
Part-derived names/instance fields match074d (broader than native saved-contract
eligibility); no new saved Part or complete-IL semantic equivalence claim.
Owned46251/window71581 actual640x428save-choice/world/HUD7creatures2686entities,
closed. Five owned window entries included four33pxmenu bars; one game qualified.
Fresh AX/postfalse/screentrue; no input/activation/permission request.4normalMacsave
files stayed byteexact in place,18logicalprefs restored. Fresh20apps/3720files exact
(19prior3534+186new),84foreign/861selectednormal/2otherforeign pins match. Main
stopped/rootnull/clean/profileroff/original7views/index0. Independent final package/
source/startup-artifact review0P1/P2; [receipt](Verification/MachineSystems/MacCandidate6325062A1/README.md).
Controlled packaged interactions/save/controller/FPS/Intel, original failed full
regression, all12complete-machine gates, W1b/W2 and natural discovery stay open.
Root sole owner; task ACTIVE every30minutes. Next broaden after publication;
do not repeat accepted tests, branches/build/startup. Supersedes older074d=currentMac
references below. Graphics cancelled; capstan/cultures parked; Deck deferred.

**Portable fire-moss — November24,2026:** bounded Editor accepted after native,
actual and independent final source/artifact/gameplay-evidence review0P1/P2. A quoted1moss row starts an existing
fueled torch or finite hearth; real flame/flint stay preferred. Moss adds no fuel;
legacy light/stoke commands never spend it. Exact source/target receipt, one-unit
late refund and explicit unsupported-custody limits protect finite stock. No new
recipe, saved Part/field or compulsory upkeep. Final188total/187pass/0fail/
1Explicit utilityskip/0CS includes35new; prior runs overlap. Actual play caught a
world-menu integration gap after native GREEN; compiling1RED and Part-event fix
preceded final GREEN and actual C-menu success. Hearth moss6→5 at370 enabled
still8mire→1clay at380 (24→25,4mire left). Nearby flame kept moss5 at390; final
Part-produced torch fallback spent5→4 at420. Full fresh390/420 selected snapshots
exact; native cost/cancel/readers/screenshots and F5/fourfile-before-Finish passed.
Strong earned fixture, no new acquisition/discovery/economy claim. Main stopped/
rootnull/clean; fresh scoped foreign/normal pins preserved. [Receipt](Verification/MachineSystems/PortableKindling20261124/README.md).
CurrentMac074d predates knife/kindling; next coherent package can batch both.
All12complete-machine gates, failed full regression, W1b/W2 and natural discovery
remain open. Root sole owner; task ACTIVE every30minutes, frequent real screenshots.

**Knife fieldwork guide — November23/24,2026:** bounded Editor accepted after
native, actual readers and independent source/gameplay-evidence review0P1/P2. A capable knife now explains up to
three existing flexible-stock preparations using the validated live catalogue:
leather binding, guard lashing and serrated edge. It directs the player to choose
prepared material in the pack; no new recipe, direct knife action or saved field.
Exact-tool availability and normal combat details are retained. Core139 gives
138pass/0fail/1Explicit utilityskip/0CS; final66/66 overlaps and includes12new
cases. Live font defect led to2ASCII-format RED and the supported arrow fix.
Native F1/world reads were free; normal drop/pickup/unequip retained exact2127,
280→290. Full fresh290 selected snapshot and derived guide matched. Separate
wrong-picker300 branch is archived/excluded. Main stopped/rootnull/clean; scoped
normal/foreign pins preserved. [Receipt](Verification/MachineSystems/CuttingToolGuide20261123/README.md).
CurrentMac074d excludes this reader; batch at another coherent milestone.
All12complete-machine gates, failed full regression, W1b/W2 and natural discovery
remain open. Root sole owner; task ACTIVE every30minutes. Next broaden to another
useful material/tool or lasting-project connection, with frequent real screenshots.

**Current Mac delivery — November23,2026:** runtime074d84fa9f9c2ee1db2eca407dd8aea19d694912
packages accepted W1a shower collection and direct vessel-oil torch refill at
`/Users/steven/coo-verification/builds/mac-074d84fa9-20261123/CavesOfOoo.app`.
Universal6000.3.4f1/nondev,56.457165s364182637bytes/0errors/5samepriorwarnings.
32055Git/SHA source pins,32058effective/83changed0missing3added/86overlayexact,
592external/186player pins,27Cecil feature/wiring/field checks. One owned69751/
window71562/640x428 actual save-choice/world/HUD7creatures2690entities, closed.
NormalMacSaves now present from01:50UTC before this build; oldhelperrefused
BEFORElaunch, source-qualified read-only startup preserved4files byte exact in
place and restored18logical preferences. Freshpost/AXfalse/screentrue; no input,
activation or request. All18prior3348files+new186exact=19apps preserved; fresh
84foreign/861selectednormal/other2foreignexact. Mainstopped/rootnullclean/
profileroff/original7views/index0. Independent source/package/startup artifact review0P1/P2 is
recorded in the [Mac receipt](Verification/MachineSystems/MacCandidate074D84FA9/README.md).
Controlled packaged interaction/save/controller/FPS/Intel remain open.
Final shared-command integration103total/102pass/0fail/1Explicit utilityskip/0CS
(52weather+23oil+28savedPart), overlapping prior runs. All12whole-machine gates,
original failed full regression, W1b/W2 and natural-discovery/frequency remain
open. Root sole owner/task ACTIVE every30minutes. User wants numerous connected
mechanics with medium-depth nuance and frequent progress screenshots. Next
broaden through another useful material/tool or lasting-project connection;
qualify its source, ordinary alternative and visible consequence before RED.
This supersedes older currentMac7ed references below; do not repeat earned tests,
branches/builds. Graphics cancelled; capstan/cultures parked; Deck deferred.

**Vessel oil and torch light — November23,2026:** direct finite liquid oil
now refills an unlit packed torch:1oil→up to25fuel, existing LampOil tariff,
no saleable item minting. Final core native257total/256pass/0fail/1utilityskip;
final oil/readability54/54/0CS overlaps (23new oil cases). Actual earned oil3→2
at290, exact torch48.8→50/Out, normal ignition/travel/renderer light contribution,
and full fresh1040selectedsnapshot exact:HP391/purse50/oil2/torch29.3000526/held/lit.
Viperbite/poison damage and fist kill retained; later relight used finite flint.
Strong fixture/earlier earned oil provenance, not new ordinary acquisition or
balance. Source review0P1/P2; final source/gameplay/artifact gate recorded in the
[receipt](Verification/MachineSystems/VesselOilTorch20261123/README.md).
Current Mac7ed predates rain/oil. All12whole-machine gates and original failed
full regression remain open. Root sole owner/task ACTIVE every30minutes.
User priority: numerous connected interactions with medium-depth nuance; frequent
screenshots at visible checkpoints. W1a is publishedc90a below; W1b/W2/discovery
remain separate. Next broaden through a useful additional material/tool/project
connection after qualification; batch coherent delivery while preserving18apps.

**Optional shower collection — November23,2026:** W1a is bounded Editor
accepted: two fixed public Morrowfast frames share3water per600-tick shower;
ordinary flask/skin collection feeds existing manual crop watering. Final native
106total/105pass/0fail/1Explicit utilityskip/0CS (52new weather cases); separate
saved-contract28/27pass/1skip overlaps. Actual fair refusal/cancel, paid14420
collection, second-frame shared exhaustion, collected-water crop action14500,
travel and full fresh14500/14590 passed. Independent source/gameplay-evidence
review0P1/P2; directed strong fixture, no natural discovery/frequency claim.
W1b automatic crop rainfall, W2 seasonal second frame job, wholeW1 and package
acceptance remain open. Current Mac7ed predates weather. All12machine statuses
and original failed full regression remain open. Root sole Unity/Git owner;
task ACTIVE every30minutes. Latest user direction: develop numerous connected
mechanics with medium-depth nuance; stop expanding one instance after its
meaningful gates and share frequent screenshots at visible progress points.
Next qualify a useful additional material/tool consumer, preserving ordinary
alternatives and finite custody. See [W1a receipt](Verification/MachineSystems/WeatherCollection20261123/README.md)
and [weather continuation](WEATHER-DESIGN-CONTINUATION-2026-11-23.md).

**Current Mac delivery — November23,2026:** accepted inventory1fda and weapon
supply7ed source are now packaged together at
`/Users/steven/coo-verification/builds/mac-7edc5a4c2-20261123/CavesOfOoo.app`.
Runtime7edc5a4c2d74a25008272198b5c835ef95d8ae4d; universal6000.3.4f1/nondev,
55.841123s364155309bytes/0errors/5same prior7c warnings.32043committed/
32046effective/83changed0missing3added/exact86overlay/0runtimeCSimportchanges/
592external/186player pins. Git blob/source proof exact; Cecil56checks new
shared weapon profile/fresh8fields/promoted-core guard/UI categories/sorts and
armor text, with unchanged saved fields and selected prior shared contracts.
Reconcile intentionally changes; no unchanged or byte-identical IL claim.
Artifact-only obsolete-Cecil and BlueprintName-property assumptions corrected;
no source/player rebuild. Oneactualowned80552/window71174/640x428starting
chooser/world/HUD/bootstrap7creatures2684entities. Freshpostfalse/AXfalse/
screentrue, no input/activation/request. Exactownedplayerclosed;10logicalprefs
restored/MacSavesabsent nonsymlink.17prior3162files+new186exact=18apps retained;
84historicalforeign/861selectednormal exact; separately dirty foreign handoff
and PackageManagerSettings recorded without staging. Mainstopped/rootnullclean/
profileroff/original7builtinviews/index0. Independent read-only artifact review
completed with0remainingP1/P2; source/package/startup/preservation only.
Controlled packaged interaction/save/controller/FPS/Intel remain open. See
[delivery receipt](Verification/MachineSystems/MacCandidate7EDC5A4C2/README.md).
UI263/263 and weapon220/219pass/0fail/1Explicit utilityskip/49new plus actual
Editor fresh10660/12210 remain separate evidence. All twelve complete-machine
statuses and original failed full regression stay open. Root sole owner/hourly
ACTIVE. Next source-qualify finite recovered/settled oil for suitable portable
lights, preserving existing LampOil convenience, finite custody/capacity and
ordinary alternatives. This supersedes older currentMac7c references below.

**Weapon stock continuation — November23 evening:** Editor acceptance after
independent read-only source/gameplay-evidence review0remainingP1/P2. Genuine
matching WeaponsmithStock now gets dagger1/shortsword1/spear1/oak1/binding1;
Cinderhold keeps spike2. Two saved specials from9remaining original kinds;
old promoted members remain physical ordinary goods at due rotation. Current
lots/clock/custody/ordinary sold goods/services/prices/save fields protected.
Full8fresh standard-weapon fields close24realcallback-publicationRED cases.
Final220/219pass/0fail/1Explicit utilityskip/0CS,49new. Actual oldCinder10660g0
unchanged; Quillhold had no weaponNPC; real Sill17366at6,17: paid41for17369at
11410purse67to26, fresh11410sameg0/noinstantreplacement, exact300no-refill11710,
310one30418refill11720, full1200window12210g1Token30427/Mace30428. Oldordinary
17367/stun17375x2/player17369retained; fresh12210exactpurse26/HP412/lot/ownership.
Native movement/waits/readers/save; strong supplied fixture/scouting/traffic
and helper exclusions disclosed. Old promoted-special migrations native-only.
See [design](MACHINE-WEAPON-MERCHANT-SUPPLY.md) and
[receipt](Verification/MachineSystems/WeaponMerchants20261123/README.md).
The older UI receipt counting note is corrected:87descriptionpasses,46initial
weapon cases(41fail5pass); finalweapon49. UI native263/263 and28+54new remain.
Current Mac7cpredates accepted UI1fda and this stock source. NEXT build one
coherent Mac from accepted committed source, preserving all17previous apps and
normal preferences/saves. Mainstopped/rootnullclean/original7views/index0;
fresh84foreign/861normalexact. Root sole owner/hourlyACTIVE; all12complete-machine
gates and original failed full regression remain open. Standing purpose is
wonder through a broad surface of consistent practical capabilities, not only
catalogue variety. Next runtime candidate after delivery: source-qualify finite
recovered/processed oil for compatible lights, preserving existing LampOil and
honest material/access/capacity limits before any recipe/permission extension.

**Inventory readability continuation — November23 evening:** native263/263/0CS
and actual old-save UI qualification are accepted after independent read-only
source/gameplay-evidence review with0remainingP1/P2. F3categories/F4sorting,13actual categories instead of7,
62groups/68owners/211units retained;44-row1280view and readable480query. Exact
2242member selection, literal water/category intersection, late Documents,
separate AV/DV/Hit/Pen readers and requested armor-paragraph removal verified.
Final/fresh10660HP412/purse67/26timber/quest/pump/originalCinderlot exact; compressed
save payload hash unchanged. Earlier input-interference/stepped-helper branches
are preserved and excluded; no new recovery claim. See the [receipt](Verification/MachineSystems/InventoryBrowse20261123/README.md).
Main stopped/rootnull/clean/original7views/index0; fresh84foreign/861normal exact.
Current Mac7cpredates UI; batch next coherent package with weapon-stock extension.
User clarified overarching wonder: discovering the broad practical surface of
shared equipment/mechanics/items should expand what players can attempt; see
[SYSTEMS-DESIGN-PRINCIPLES.md](SYSTEMS-DESIGN-PRINCIPLES.md). All12 dependency/status
rows and original failed full regression remain open. Root sole owner/hourlyACTIVE.
Next explicit request: genuine weapon merchants beyond Cinderhold get standard
weapons and rotating specials. [Weapon supply qualification](MACHINE-WEAPON-MERCHANT-SUPPLY.md)
is in progress, not yet accepted. Do not conflate its pending source/tests with
this inventory increment. Preserve all foreign files, saves/preferences/art/apps.

**Current Mac delivery — November23,2026:** published Cinderhold runtime7c248f7f06d437d91fb3abbb2f7f2678da5c652a
is packaged at /Users/steven/coo-verification/builds/mac-7c248f7f0-20261123/CavesOfOoo.app.
Universal6000.3.4f1/nondev56.152444s364149165bytes/0errors/5warnings (same5unique
prior1a messages).32035committed/32038effective/83changed0missing3added/
exact86overlay/noRuntimeCSimportchange/592external/186app pins. Git blobs exact;
Cecil36checks new local profile/catalogue/place/fresh fields and unchanged shared
contracts/saved fields/grouping/mire. Raw Reconcile comparison initially stopped
on generated29→30 identifier labels; narrow normalization retains all instructions,
offsets/constants/other operands; not byte-identical IL. DLL
e0e8ee5d3abd0db0aaef9b386a4460212af1377218ebb22973e3feb76325d352.
Oneactualowned91031/window71119/640x428chooser/world/HUD/7creatures/2672entities;
postfalse/AXfalse/screentrue, no input/activation/request. Exactownedplayer closed;
10logicalprefs restored/MacSavesabsent nonsymlink;16prior app maps/current186/
84foreign/861selectednormal exact. Mainstoppedrootnullclean/original7views.
Independent read-only artifact review0P1/P2; controlled packagedplay/save/controller/
FPS/Intel remain open. See [delivery receipt](Verification/MachineSystems/MacCandidate7C248F7F0/README.md).
Editor200/199pass/0fail/1Explicit49new and actual reclaim+purchase+lastingpump/
existingcommerce/fresh10660 accepted separately. All12/fullregression gates open.
HourlyACTIVE/root sole owner; preserve every app/checkpoint/art/foreign change.
Next qualify dense-inventory category/sort friction or another practical supply
project from actual evidence; neither is accepted simply by this suggestion.
These lines supersede older currentMac1a references.

**Current Cinderhold supply foundation — November23,2026:** bounded Editor
acceptance after independent source/gameplay-evidence0P1/P2. Genuine local
WeaponsmithStock now keeps dagger1/spike2/oak1/binding1 and two saved specials
from all11remaining original optional goods. Existing regional/sold goods and
non-Cinder smith/Tinker/Quartermaster policies preserved; no new save field,
whitelist, repair/reward/price rule. Canonical fresh8component/5stun fields close
native reproduced callback corruption.49newcases; final200total/199pass/0fail/
1Explicit utilityskip/0CS. Actual oldtown gap/Tinker alternative/free44dram refusal
with15drams qualified separately from full consumer. Supplied strong branch
picked finite released tool, reclaimed/pointed one spike, bought one spike+oak+
binding for66 (providedbook sale73 disclosed), then actual world pump mend10370
consumed four units into StableRepair/PumpRunning. Travel return10450 added
existing Quartermaster2spikes; actual report10610 existing50drams/2200XP. Fresh
10660 retained questdone/purse67/level5/HP412/26timber/no spent inputs and exact
g0/install9490/28104+28105. Existing snake interruption, NPC flight, level healing,
ignored modal inputs and affordability limits disclosed. No physical drainage,
route, automatic production or complete-machine claim. See [design](MACHINE-CINDERHOLD-SUPPLY-QUALIFICATION.md)
and [native receipt](Verification/MachineSystems/CinderholdSmithSupply20261123/README.md).
Current Mac1a predates this increment; next coherent package remains open.
Mainstopped/rootnull/clean/original7views;84foreign/861selectednormal exact.
All12 gates and failed full regression remain open; hourlyACTIVE/root sole owner.

**Current Mac delivery — November23,2026:** published runtime1a153dc6948a838c87b8cfd3b3fd06db9421ad85
now includes mire/pith recovery and both corrected readers. Universal nondevelopment
app: /Users/steven/coo-verification/builds/mac-1a153dc69-20261123/CavesOfOoo.app.
60.534291s364148141bytes/0errors/7warning occurrences=same5uniqueprior203.
32033committed/32036effective/83changed0missing3added/exact86overlay/
0runtimeCSharpimportchange/592external/186app pins. Git blobs/export exact;
Cecil15checks newmire/guides/unchanged body and merchant contracts/saved fields.
DLL884be79e6dbc8dd02f4e67567b3bd09062cb828891c2fbae35df7e666ae4aa85.
Actualowned55465/window71103/640x428chooser/world/HUD/7registeredcreatures;
inputpostfalse/AXfalse/screentrue, no input/activation/request posted. Exact owned
player closed;10logicalprefs restored/MacSavesabsent nonsymlink; all15prior app
maps/current186/84foreign/861selectednormal exact. Mainstopped/rootnull/clean/
profileroff/original7views/index0. Independent read-only artifact review0P1/P2;
no controlled packaged play/save/controller/FPS/Intel/full12/fullregression claim.
See [delivery receipt](Verification/MachineSystems/MacCandidate1A153DC69/README.md).
Next source-qualified candidate: [Cinderhold smith repair supply](MACHINE-CINDERHOLD-SUPPLY-QUALIFICATION.md).
The concern promises smith-sold spikes absent from its catalogue; one reclaimed
blank makes one spike, pump lastingrepair needs two. Actual shop/pump/census and
complete optional-payload baseline remain required. Quartermaster absence is
not established; no new pump reward, route or merchant profile is shipped here.
Current all12 statuses unchanged/open; hourlyACTIVE/root sole owner. These lines
supersede older currentMac203 references. All previous apps/checkpoints preserved.

**Mire field-recovery continuation — November23,2026:** Bounded Editor recovery
is accepted after independent source/gameplay-evidence review0P1/P2 (read-only). Existing pith body cleanup admits bog-mire;
one pith removes at most20 from self/adjacent visible nonhostile party, with
unchanged payment/claims/exact-effect/save behavior. Both readers name mire;
finite discard, partial penalties, no healing and thin-film/pool limits explained.
Native RED19new failures+separate readerRED1; final261total/260pass/0fail/
1Explicit utilityskip/0CS,20newcases. Actual earned7100:272coat/8pith/218HP/
Agility16; paidwipe7110→245/7/217/16, ordinary movement/waits7440→14/7/184/16,
finalwipe7450→none/6/184/18; F5/fresh7450 exact. No ordinary acquisition or
new exposure/AI claim; all12 complete-machine dependency/status gates stay open.
Current Mac203 predates this slice; next coherent package remains open. Original
full regression stays FAILED. Main stopped/rootnull/clean;84foreign/861selected
normal hashes exact. See [the recovery design](MACHINE-MIRE-FIELD-RECOVERY.md)
and [native receipt](Verification/MachineSystems/MirePithRecovery20261123/README.md).
HourlyACTIVE; root sole owner; preserve dense inventory/separate stats/all apps.

**Current Mac delivery — November23,2026:** runtime203023f315f4b0e3942b2ffd251937e56abb35ac
is published on main. Universal nondevelopment Mac artifact/startup checks pass:
/Users/steven/coo-verification/builds/mac-203023f31-20261123/CavesOfOoo.app.
Build58.084523s364146605bytes/0errors/7warning occurrences (same5unique messages
as prior73,2duplicates),32031source/32034effective/592external/186app pins;
83changed0missing3added/exact86overlay/no runtime C# import change. Cecil7calls,
new profiles/pure condition reader/8payload types and unchanged8saved fields.
DLLb7f704bbb54281301e459b0582ee09cec5bcf56393e86a19c55d837d7efe8e04.
Owned26842/window71086actual640x428chooser/world/HUD; native inputfalse/AXfalse,
no input or activation posted. Controlled packaged play/save/controller/FPS and
Intel execution remain open. Exact owned player closed;10logicalprefs exact,
MacSavesabsent/nonsymlink;14previous full186-file app maps+current186 exact,
84foreign/861selectednormal exact. Mainstopped/rootnull/clean/profileroff/original
7/index0. Independent artifact review0remainingP1/P2 (read-only). Source/Editor237/236pass/0fail/
1Explicit/59new and actual project/book/drying/fresh8040&9320 remain accepted.
See [delivery receipt](Verification/MachineSystems/MacCandidate203023F31/README.md).
All12 full-machine gates remain open; original full regression FAILED. HourlyACTIVE.

**Current Editor merchant extension — November23,2026:** ProvisionerStock now
keeps dependable lamp oil and all previously guaranteed basic goods; its original
optional goods cycle as saved specials. ScribeStock in authored Quillhold keeps
ink, blank copies and canonical Codex04, with utility manuscripts and a third
DryingBreeze book in future lots after the existing finite copy-room oil project.
No new saved Part/field or ordinary stacking/grouping permission. Final targeted
237total/236pass/0fail/1Explicit utilityskip/0CS;59newcases. Independent source
and gameplay-evidence review0remainingP1/P2 (not a second native operator). Actual purchased
3oil for49, supplied lamps, retained g2/install8030 through fresh8040, waited one
game-day window, bought bonus25644 for107 and read it, then used its actual world
ability to remove finite-water Wet at9270/9320. Fresh9320 retained project/book/
skill/water0,2,2/purse10/HP172 and exact remaining g3 books. Fixture-funded sales,
read-only scouting, native detours, first ignored Pause cast and source limits
are disclosed. See [the receipt](Verification/MachineSystems/QuillholdConsignments20261123/README.md).
All12 complete-machine statuses unchanged/open; original full regression FAILED.
Runtime203023f31 is published; current Mac delivery is recorded above. Next qualify a real
route/import or useful machine/tool supply project. Do not repeat accepted play.
Root sole Unity/Git owner/hourlyACTIVE. Preserve all84foreign files,861selected
normal states, all apps/art/checkpoints, dense inventory and separate armor/dodge/
hit/penetration lines; category/sort remains a follow-up. Graphics cancelled,
capstan/cultures parked, Deck deferred. Mire-washing is separate recovery debt,
not a claim about Drying Breeze. These lines supersede older continuations.

**Whole-game architectural direction — user, November16,2026 (America/Chicago):** Follow Docs/SYSTEMS-DESIGN-PRINCIPLES.md. Compose machines, NPCs and objects through Parts, stable capabilities and shared events; attach future effects/behaviors to instances/components without per-scenario scripting. Apply common-sense interactions wherever supported, with finite accounting and persistence. Continue the active reclamation/processing implementation.

**Current user priority —2026-11-17 03:20UTC:** focus reclamation forge and regional raw-material processing into components. Capstan/culture qualification is parked. Follow Docs/MACHINE-RECLAMATION-PROCESSING.md; this supersedes older capstan NEXT text below.

**Standing user design direction —2026-11-17:** machinery supports open, reusable world interactions with several useful approaches. Reject single-solution tool puzzles and progression padding. Capstans are one option among ordinary strength/tools, dismantling, destruction, terrain/route changes and help where those actions are supported. Review real alternatives and cross-system effects for gathering, terrain and environmental combat; compare meaningful costs/consequences, not cosmetic menu choices. Preserve convenient existing methods and explicit finite salvage yields. Apply this to the active reclamation/processing slice; capstan implementation stays parked. See MACHINE-SYSTEMS-DESIGN.md, “User direction: open world interactions.”

**User authorization — 2026-11-16:** Continue autonomously for as long as possible alongside the ACTIVE hourly machine task. New code, content and visuals/graphics are authorized where they serve this systems program. The cancelled graphics automation remains cancelled; Steam Deck remains deferred.

# Machine foundations execution plan

## Active handback continuation — November 21, 2026

Merchant pilot — November23: FarmerStock and ArmorerStock now retain dependable
staples and saved special consignments. Native234 total/233pass/0fail/1Explicit
utilityskip/0CS;64 new feature/countercase executions. Independent source review
found0remainingP1/P2. Actual GameBootstrap showed both shops/readers, free cancel,
unaffordable armor refusal, exact seed purchase, elder guide and one carried fire
clay rebuilding the oven, current-lot retention, next-lot extra baking, buying and
eating that meal (HP360→367/ColdResistance0→20), and fresh4630/4870 persistence.
The strong supplied fixture is not acquisition/economy/difficulty proof. Ordinary
staple top-ups merge safely; bought/resold goods leave consignment membership;
changed/escaped goods survive rotation. One1200tick game-day window from actual
installation, one bounded catch-up, strict>300tick staple top-ups, no wallclock.
See [merchant contract](MERCHANT-STAPLES-AND-SPECIAL-STOCK.md) and
[pilot receipt](Verification/MachineSystems/MerchantConsignments20261123/README.md).
Current Mac artifact/startup for73fb59da4 is verified in the
[delivery receipt](Verification/MachineSystems/MacCandidate73FB59DA4/README.md).
Controlled packaged gameplay remains open. Next qualify an honest route/import
or equipment-enabled second project using real existing conditions. Those expansions, global unique issuance and other merchant profiles
remain proposals. All12 machine statuses remain unchanged/open; original full
regression remainsFAILED. HourlyACTIVE/root sole Unity/Git owner. Preserve dense
inventory/readers and the category/sort follow-up, all84foreign changes/normal
states/apps, cancelled graphics, parked capstan/cultures and deferred Deck.

Current Mac delivery — November23, merchant pilot73fb59da4:
/Users/steven/coo-verification/builds/mac-73fb59da4-20261123/CavesOfOoo.app.
Universal6000.3.4f1/nondev55.315611s364143533bytes/0errors/5sameFFFwarnings.
32027source/32030effective/592external/186app pins;83changed0missing3added,
86overlay/no runtimeCSharpchange. Cecil5integrationcalls/8savedfields verified;
DLL24dad1063682e79e2ffc3f5e124a8f1998f642739ddde00da4dbd92252f2e6fe.
Owned62144/window71065actual640x428chooser/world/HUD; nativeinputfalse,
no events/activation posted. Packaged controlledplay/save/controller/perf remain
open. First capturehelper aborted withoutAppKit; retrymenubar probe excluded;
correctPID/height-filtered gameframe retained. Bothownedplayers closed normally,
10logicalprefs exact/Savesabsentnonsymlink,12priorapps+current full186maps exact,
84foreign/861selectednormal exact. HelperactualCavesOfOoo.LastGameID literal
corrected for future reuse; branch not exercised by this chooser-only startup.
Source-only fix leaves both successful restorations valid. Mainstopped/rootnull/
clean/profileroff/original7/index0. All12full-machine gates remainopen; hourlyACTIVE.
[Delivery receipt](Verification/MachineSystems/MacCandidate73FB59DA4/README.md).

Prior well-supply Mac delivery — November23: published well-supply runtimefff96f4c1b6465fae594a1dcb437c5b1e7c6be5b
is packaged at /Users/steven/coo-verification/builds/mac-fff96f4c1-20261123/CavesOfOoo.app.
Universal6000.3.4f1/nondev53.037878s364128829bytes/0errors/5sameFBEwarnings/0CS.
32021committed/32024effective/592external/186app pins;83changed0missing3added,
exact86overlay/no runtime import change. Packaged SourceAvailable IL has the new
pool/emitter branch and retained purity call; DLL a4912c2f4a05a98206f6dcef71bb83e5a98ee9d7bcb185e3a7f939cc8b696577.
Owned88413/window71006 rendered world/HUD2690->2691/7creatures. Picker log ON,
observed Warden/initial checkpoint; advancement cause unestablished. No input
posted by this task; fresh native input accessfalse. PID-scoped all-window
capture, no app activation. Packaged controlledactions/save/controller/performance
remainopen; Editor252/actualseparatewater-mire/fresh4580&4590 qualifies separately.
ExactPID/exe close. Initial defaults-import cleanup rejected leftover app-created
LastGameID+4screenkeys; all5 verified absentbefore/stillmatchingownedafter and
removedonlythose. Userpaused/resumed; fresh complete postchecks nowpass11priorapps
with186fileinventorieseach/current186/84foreign/861selectednormal/10logicalprefs
exact/Savesabsentnonsymlink. No byteexactplist claim. All apps/projects/exports
retained. Mainstopped/rootnull/clean/profileroff/original7views. HourlyACTIVE.
[Delivery receipt](Verification/MachineSystems/MacCandidateFFF96F4C1/README.md).
This is a delivery records update; runtime remainsfff. All12 full-machine/gates
remainopen; M3c remains lastFULL delivery, original full a9 regression remains
FAILED. Do not repeat completed bounded tests/play/builds without new concerns.
Next choose a useful shared reclamation/processing connection from the gameplay
loops. One source-identified candidate: settled flaskoil does not currently feed
LampOil-only torchrefueling. It is a proposal requiring bounded design/source
critique, nativeRED/accounting/countercases and actual input before implementation
acceptance. Preserve ordinary LampOil, safe unlit-target/finite/capacity rules,
other light options and honest source/price/quality limits. No generic oil-fuel
adapter or new recipe is shipped by this paragraph. Keep accepted dense Inventory,
separate weapon/armor stat rows and further category/sort plans. Graphicscancelled,
capstan/culturesparked/Deckdeferred; NPCsurvival/unloadedfire/economics stayopen.

Well supply/spill correction — November23: the earlier conflict is resolved by
restoring the documented drawn Stock versus exposed LiquidPool distinction.
Shared admission preserves unlike-pool/emitter refusal, no dry-pool fallback,
phase/ice, repair, oath, custody, finite quantities and callbacks. The original
legacy spill success assertion is unchanged. Corrected native REDb40=16fail/
24controls; GREENb252/252/0CS includes42new cases, non-null acid exposure and
metered last-unit controls. Exposed-safety mutant killed9/43; clone3 source
restored to all7 exact main pins, next native run imports restored source.
Actual ordinary source-guided travel from4200 reaches TankBrocchinia17798 at
Overworld.3.4.0,47,6. Carried2235 pours1mire4570, then C-fill draws12water4580
while ground19577 remains1/Stock-1. Direct drink stays free; F5/fresh4580 measured
state exact. Native C-recovery puts that1mire into2231(11->12), removes only the
ground owner, keeps water2235=12; F5/fresh4590 exact. Both four-file checkpoints
archived;8actual PNGs and raw observations retained. Finite/metered/acid/oil/
exposed-refusal branches remain test-only; inherited hero/items are fixtures.
Independent source/gameplay-evidence review0P1/P2, not a second native operator.
Fresh84foreign/861selectednormal hashes match; main stopped/rootnull/clean/
profileroff/original7views. No new save fields, supply, tariff, immunity or
full-machine acceptance. Original full a9 run stays FAILED40813/104fail;
no inferred full count,91baseline failure names remain open. All12 statuses stay
open. Graphics cancelled, capstan/cultures parked, Deck deferred, hourlyACTIVE.
See [contract](WELL-SUPPLY-PROVENANCE.md) and
[receipt](Verification/MachineSystems/WellSupplyProvenance20261123/README.md).
Current Mac fff now packages this correction, as recorded above. Latest
reusable ordinary checkpoint is /private/tmp/coo-well-provenance-recovery-earned-20261123/8ab6a6384c2a44deb8b3a68303809d8f at4590; preserve earlier4200/4580 too.
Next continue useful shared machine/tool gameplay loops; preserve accepted UI
separate Damage/Hit/Penetration and AV/DV rows, safe complete-state stacking,
dense Inventory and further category/sort plans. NPC survival, unloaded-fire
chronology, general fractionation/economics and full delivery remain open.

Current Mac candidate — November22/23: published UI sourcefbe0742f235ba5c92237cee710571ff0ee1d1a7b
is packaged at /Users/steven/coo-verification/builds/mac-fbe0742f2-20261122/CavesOfOoo.app.
Universal6000.3.4f1/nondev55.993293s364128829bytes/0errors/5samea9warnings.
32019committed/32022effective/592external/186app pins;83changed0missing3added,
exact86overlay and no runtimeCSimportchange. NewDLLeb6f1655ac27aaf481ad8169107861908e0c198c3c826fda996c1478a7688da3
has section/snapshot APIs and AV/DV labels absentina9. Actual owned17768/window70955
chooser/world/HUD2676->2677; fresh nativeinputaccessfalse and NOinputposted.
Packaged Inventory/actions/save/controller/performance/full delivery remainopen;
Editor27screens/native471/savefresh qualify UI separately. PID/exeguardedclose,
10logicalprefsequal/Savesabsentnonsymlink; all10priorapps186file inventories/hashes
exact, currentapp186still exactafterboot. Fresh84foreign861selectednormal match;
mainstopped/rootnull/clean/profileroff/7builtins/custom0/selected0/noownedview.
Allapps/projects/exports retained. See [Mac receipt](Verification/MachineSystems/MacCandidateFBE0742F2/README.md).
All12 complete-machine/full-delivery gates remainopen; last FULL deliveryM3c.
Historical delivery record: fbe is this package's source. The November23 well
supply correction below supersedes the old unresolved-spill next step; continue
useful shared machine/tool loops without repeating accepted UI work or reviving
cancelled graphics/parked capstan-cultures/Deck. HourlyACTIVE.

Current inventory description increment — November22/23: implemented and bounded
Editor-qualified. Weapons/armor retain typed logical sections, contrasting
headings, complete effects/comparison/flavor and cached overviews. Armor value
(AV), Dodge value (DV), Damage, Hit bonus and Penetration bonus each have their
own row. Exact provider/Part/slot/owner guards and dense capacity are preserved.
Native compiling RED20 retained16 meaningful failures/2 corrected fixture errors/
2controls; final combined native471/471/0CS includes20new UI cases and corrected
regression fixtures. Actual isolated GameBootstrap native keyboard routes inspect
Dagger2127, boots2351, modified cloak3c6..., worn cloak2342 and every reader page;
normal views/resize/F1/F5/fresh Continue preserve measured4200/HP360/energy1000/
weight536/66owners. At2315x1189:61countedrows/210items/capacity76. All27 current
screenshots use real end-of-frame capture. A separately disclosed hybrid test
item exercises both stat families and native popup equip; it is never saved,
with raw4file before/after hashes and ordinary checkpoint retained. Independent
source/gameplay-evidence review0P1/P2, not second native operator. See
[UI receipt and gallery](Verification/MachineSystems/InventorySections20261122/README.md)
and [description contract](INVENTORY-ITEM-DESCRIPTIONS.md).

Full pinned a9 regression completed40813total/40692pass/104fail/9inconclusive/
8skipped/0CS,3887pins exact. Compare BOTH full_s9/fullrun_h:13new failure names,
all rerun separately on unchanged snapshot (13processes,1caseeach,0CS):10invalid
water fixtures fail,1old well/spill conflict fails,2input/random fixtures pass.
Correct actor+pool physics, scope/restore oil-slip RNG with landing assertion,
and explicitly test keyboard/controller journal footer; behavioral assertions
remain. Corrected classes are included in focused471GREEN, not a new full run.
The older well conflict is now resolved by the November23 correction above;
baseline91failure names remain open. Common
message differences are clone paths or HushStripLaid already present in fullrun_h;
no graphics resumed. [Full assessment](Verification/MachineSystems/FullRegressionA9DE3D3D9/ASSESSMENT.md).

The user's explicit instruction to start before the full run finished superseded
older freeze/after-full-run text. Root is sole Unity/Git owner; clone2 was never
resynced/edited. Fresh84foreign/861selectednormal hashes match; main stopped,
rootnull/clean/profileroff/7builtins/custom0/selected0/noownedview. No balance,
save fields or authored hybrid/recipe added. Current Mac fbe packages this UI; its bounded receipt is above. Packaged UI
interaction remains open; all previous apps are retained.
All12 complete-machine/full-delivery gates stay open, last full deliveryM3c.
Graphics cancelled, capstan/cultures parked, Deck deferred; hourlyACTIVE. Further
machine expansion follows this UI completion and outstanding regression policy
work; retain long-term loops, safe stacking and dense category/sort priorities.

November22 [current well/flask Mac candidate](Verification/MachineSystems/MacCandidateA9DE3D3D9/README.md)
packages pushed/remotely verified runtimea9de3d3d9de9c21a917748c0d74c387b17ab7b3d.
Universal6000.3.4f1/nondev58.360659s364121661bytes/0errors/5samewarnings928.
32019source/32022effective/592external/186app pins;83changed0missing3added/exact86
overlay/runtimeC#unchangedonimport. ProducedDLLcf2b442bbbf9cecb7d5f67c36e14087b8a89bbf82b12a8583ae3b610fafeeb98
includesnewWellFlaskFilling metadata/UTF16commandprefix, absent928. Owned54602/
window70863 actualchooser/world/HUD2684generation->2685/7creatures. Freshnative
inputpreflightfalse, NOinputposted: packagedactions/save/controller/full delivery
remainopen. Guardedplayerclosed; fresh10logicalprefsequal/Savesabsentnonsymlink,
all9prior machineapps186fileinventories/hashesexact. Mainstopped/rootnull/clean/
profileroff/7builtins/custom0/selected0/noownedview;00:45UTC84foreign861normal
selected hashes unchanged. Independentfinalartifactreviewrecordedinreceipt;
notsecondgameoperator. Allapps/projects/exports preserved. Well/flask191native+
additional222passes/1isolatedunchangedlegacyfailure+mutants6/13/3 and actual
endless/finite40->28/gather-carry-cool10+recover2/fresh3930&4200/fullreader qualify
the bounded shared correction separately. FiniteStock3native-only. All12complete-
machine and full-delivery gates remain open; last full deliveryM3c/current420.
NEXT full corrected-head regression through idle clone2, pin exact current code
and compare both acceptedfull_s9 and inheritedfullrun_h; rerun new failures alone
before attributing regressions. No new native play/builds during that run.
Then continue useful machine/tool gameplay and further audited Inventory groups/
category-sort. Unloaded-fire chronology/NPCexposure/economics remain open.
Graphicscancelled/capstan-culturesparked/Deckdeferred/hourlyACTIVE.

November22 [ordinary flasks at usable wells](Verification/MachineSystems/WellFlasks20261122/README.md)
closes native-reproduced broken/oath pool-backed well bypasses and supplies the
missing pool-less flask draw. Shared Well/Waterskin source admission, exact
source-kind/quantity/recipient quote, action lease, current Part/anchor/access/
phase guards and bounded quantity undo preserve independent injury/drop/stock.
No saved field, pool invention, tariff change or flask-drinking action. Native
191/191(42new149adjacent); additional222/223 with1unchanged legacy spill test
reproduced alone on ef5 baseline; mutants killed6/13/3, zero compiler errors.
Actual GameBootstrap Inventory group2empty/selected2241 fills12 from restored
Gleaners well at3650, other2242unchanged/Stock-1. Native C preview/cancel free.
Ordinary route to Grovelands4.7 real GroveSeep17170: finite pool40->28/exact2242
empty->12 at3930, fresh3930fullmeasuredstate/equipment exact. Carry to camp,
select2242individual;10water cools hearth393.9534->25/dark,2surplus recovered,
source28/endless2241water12 retained. NormalF5/archive-beforeStop/fresh4200
fullmeasuredsources/receivers/fire/repair stock exact, HP360. Full3page reader
shows2242water2/12 freely; no grants/teleports/stat resets. Pool-less finiteStock3
native-only. Equipment/acquisition/economics and unloaded-fire chronology open.
Source/artifact/gameplay-evidence review recorded in receipt; notsecondoperator.
Initial non-Inventory before-capture/frozen fixtures/transport and transient startup
failures disclosed; actual branches verified separately. Mainstopped/rootnull/
clean/profileroff/original7presets/custom0/selected0/noownedview; fresh00:38UTC
84foreign861normal selected hashes unchanged. Allapps/projects/exports preserved.
NEXT publish/currentMac then fullcorrected-head regression and further useful
machine/tool loops; denseInventory/countedauditedgroups/category-sort remembered.
All12complete-machine/deliverygates open; graphicscancelled/capstan-culturesparked/
Deckdeferred/hourlyACTIVE. [Contract](WELL-FLASK-FILLING.md).

November22 [current atomic-clay Mac candidate](Verification/MachineSystems/MacCandidate928A7244F/README.md)
packages pushed/remotely verified runtime928a7244ff3355628320724d129aabaf94d8b5af.
Universal6000.3.4f1/nondev54.489795s364114861bytes/0errors/5samewarnings451.
32015source/32018effective/592external/186app pins;83changed0missing3added/exact86
overlay/runtimeC#unchangedonimport. ProducedDLLbdf9b2f8be57294ab2e5fd5a8ee761482696ad50d45733754744ea828b3cba46
includesnewbakeandunitreceiptmetadata, differs451. Owned22790/window70848 actual
chooser/world/HUD2678generation->2679/7creatures. Freshnativeinputpreflightfalse,
NOinputposted; packagedactions/save/controller/full deliveryremainopen. Closed
withPID/executableguard; fresh10logicalprefsequal/Savesabsentnonsymlink, all8prior
machineapps186fileinventoriesexact. Mainstopped/rootnull/clean/profileroff/7builtins/
custom0/selected0/noownedview; fresh23:18UTC84foreign861normalhashesunchanged.
Independentfinalartifactreviewrecordedinreceipt; notsecondnativeoperator. All
builds/projects/exports retained. Clay127native+actualfinitegather/processing/
lastingrepair/fresh3540and3640 separatelyqualified; all12complete-machinegatesopen.
NEXT [well/flask contract](WELL-FLASK-FILLING.md): missingpool-lessfill plus
source-reviewedpool-backedrepair/oathbypass. ReuseSourceAvailable/oath/phase/safety,
finitepoolprecedence/Stock/-1policy, exactone-unitquantity/capacity/receipt and
nativeREDbeforeimplementation; actualpack/Cselection/cancel/use/F5fresh finite
accounting. No newwelltariff, upkeep orgeneralframework. Notimplemented yet.
FurtherInventorygroups/category/sort, fullcorrectedregression, unloadedfiretime
andNPCexposure remainremembered. Userresumedtask/hourlyACTIVE; graphicscancelled/
capstan-culturesparked/Deckdeferred. NoownedruntimeWIP after thispublication.

November22 [atomic clay baking and regional well repair](Verification/MachineSystems/RegionalClay20261122/README.md)
closes a reproduced two-clay/eight-mire callback duplication bug. Single-action
lease before factory, bound source/output Parts, exact8mire/10fuel payment and
one-unit output undo preserve independent injury/stock/custody. No saved fields,
recipe or ordinary settling changes. Native127/1270CS=25new102adjacent; three
mutants killed3/5/3. Actual native GameBootstrap gathered24mire fromsource48->24,
baked/physically merged2FireClay, traveled and repaired realGleanerswell9998 with
only produced stock. Fixture20clay stayedboxed. Two timber/one strike/20process
fuel+.75ordinary burn;8mire retained; combat/poison retainedHP360. NormalF5 then
separate freshContinue3540 and3640 measured exact, finalrepaircause/menu/working
DrawWaterAtWell persist; existingXP9->39 once. LegacyendlesswellStock-1, no generic
LiquidFlask filling or hydration-benefit claim; Waterskin/source alternatives
remain source-supported. Mainstopped/rootnull/clean/profileroff/7presets/custom0/
selected0/noownedview; fresh23:11UTC84foreign861normalhashes unchanged. Compile-
import/startup failures and wrong-target firstPNG explicitly excluded. Independent
final source/artifact/gameplay-evidence review recorded in receipt, not second
operator. CurrentMac451excludes this correction; NEXT publish then currentMac,
then qualify pool-lesswell/genericflask gap as a bounded shared capability.
KeepdenseInventory/countedauditedgroups, categories/sort, fullcorrectedregression,
unloadedfirechronology/NPCexposure and all12complete-machine/deliverygates open.
Graphicscancelled/capstan-culturesparked/Deckdeferred/hourlyACTIVE; user resumed
after temporarypause. Preserveforeignchanges/normalstate/art/allbuilds.

November22 [current finite-cooling Mac candidate](Verification/MachineSystems/MacCandidate45170B1B6/README.md)
packages pushed/remotely verified runtime45170b1b6965ccfcf0b3460b08440aa3d94fa997.
Universal6000.3.4f1/nondev61.304002s364109005bytes/0errors/5samewarnings as3fe.
32011source/32014effective/592external/186app pins;83changed0missing3added exact86
overlay, runtimeC# unchangedonimport; producedDLL includesnewcoolingreceiptmetadata
and differs3fe. Owned63833/window70708 actualchooser/world/HUD; generation2658,
final2659/7creatures. Freshnativeinputpreflightfalse, NOinputposted: packaged
cooling/Inventory/actions/save/controller/full delivery remainopen. Ownerprocess
closed, fresh10logicalprefsallvaluesequal/Savesabsentnonsymlink; all7priorapps each
186fullinventory/hashesexact. Mainstopped/rootnull/clean/profileroff/original7presets/
custom0/selected0/noownedview; fresh19:47UTC84foreign861normalunchanged. Independent
finalartifactreview0P1/P2; notsecondgameoperator or full12/deliveryacceptance.
Finitecooling175native+mutants15/3/1+actualwater12->7/coldhearth/F5/fresh2760HP390
is separately qualified. FirstunsavedInventory-modalbranch/archive2700isexcluded.
NEXT: another useful existing crop/hydration/repair output need and real supply/
servicealternatives; category/sort and furtherauditedgroups remain remembered.
Keepunloaded-fire chronology/exposureNPCprerequisites open; graphicscancelled,
capstan/culturesparked,Deckdeferred,hourlyACTIVE. Preserveall8apps/projects/exports.

November22 [finite hearth cooling](Verification/MachineSystems/FiniteCooling20261122/README.md)
closes the independently reproduced water-cost P2 with a bounded newly-poured
sink: ceil-to-ambient at40degrees/unit, only unused surplus recoverable, older
pools/natural sources preserved. Bound menu quote/current Parts/claims, bounded
quantity undo and committed shared ApplyHeat preserve outer veto and independent
injury/custody. Callback-moved/replaced targets cannot receive stale contact or
false success. Native175/1750CS=43new132previous; mutants15/3/1fail; final independent
source/artifact/gameplay-evidence review0P1/P2 (not a second native operator).
Actual2700->2760:1spent/nohearthfill; recover old6; quote4spent2surplus/freecancel;
quench25/dark/fuel2.75; recover2; realDown/Up; NormalF5/archive-before-Stop/fresh2760
full measuredworld exact. Water12->7, source48, groupedthree2/12; viper/poison
retainedHP390. First unsaved Inventory-modal branch/archive2700 explicitlyexcluded.
Mainstopped/rootnull/clean/profileroff/original7presets/custom0/index0; fresh19:37
84foreign861normal hashes unchanged. No new full12/delivery/exposure acceptance.
Current Mac3fe9efc1c excludes this correction. NEXT current Mac candidate, then
existing useful crop/hydration/repair endpoint with practical supply/service
alternatives; category/sort and further audited grouping remain remembered.
Unloaded-fire chronology remains open; graphicscancelled/capstan-culturesparked/
Deckdeferred/hourlyACTIVE.

November22 [current counted-inventory Mac candidate](Verification/MachineSystems/MacCandidate3FE9EFC1C/README.md)
packages pushed/remotely verified runtime3fe9efc1c7918fe301091bb0e483d3158fc03ded.
Universal6000.3.4f1/nondev62.30237s364099789bytes/0errors/5samewarnings as910.
32009source/32012effective/592external/186app pins;83changed0missing3added exact
86overlay, no runtimeC# importchange; producedassembly includes grouping metadata
and differs910. Own19198/window70694 chooser/world/HUD observed, generation2700
final2701/7creatures. Freshnativeinputpreflightfalse, NOinput posted: packaged
Inventory/actions/save/controller and full delivery remain open. Ownerprocess
closed, freshall10logicalprefs equal/Savesabsent; all6priorapps each186exact.
Mainstopped/rootnull/clean/profileroff/noownedviewport; fresh18:57UTC84foreign861normal
unchanged. All copies/builds preserved; no new full12/delivery acceptance.
Counted Inventory has329native+actualfiveempty/three2/12/selectedmember/fresh2700
qualification in its separate receipt; unknown graphs stay conservative.
NEXT: resume independent water-cooling cost P2 policy/RED/smallestshared correction,
preserving old pooledstock/natural sources/rollback/callbacks. Keep category/sort,
further audited groups and physical-input gates remembered; no compulsory upkeep
or physical stack/save migration is implied.


November22 counted Inventory groups are qualified in
[InventoryCounted20261122](Verification/MachineSystems/InventoryCounted20261122/README.md)
and [the grouping contract](INVENTORY-COUNTED-GROUPS.md). Same complete audited
state shares a counted row; exact individual owners/physical stacks remain.
DefaultGrouped, G/paintedtoggle/Commands->individuals; selected member survives
regrouping. Group quantity/weight never overwrites physicalStackCount. Actions
explicitly apply to selected1 or selectedphysicalN, with reserved scope line;
unknown/active/stateful graphs/equipped owners remain conservative exclusions.
Native REDa26=13fail13controls; independent review found3P2s, REDd3relevantfails;
fixed popupG instruction, longname-hidden scope and controller member access.
Finalg329/3290CS=30new299previous. Mutantsidentity11fail/selection1fail0CS.
NeighborfWellDraw... failure reproduced on unmodified307 viah; no all-suitegreen.
Actual2610->2700: sixempty/threefull groups; fixturemire2232–34 processed to3water
then pouredone each ->three2/12 rowqty3. Free Drop2236/37 leftfiveempty rowqty5,
bothsame-IDowners onground. Singles chose2240; regroup thenfillonly2240 0->1;
oneuse2232 2->1 leaves2233/34 at2. Groupsnowtwo1/12 andtwo2/12; water12 conserved
as6carried+6ground; natural source13151 stays48. Exacttwo-page2232reader/F5archive
BEFOREStop/fresh2700 measuredworld+grouprows exact. Fixturegear/HP/capacities/feed
and practicalbalance excluded. 1080p66rowgeometry/10x20glyphs/corners retained.
Mainstopped/rootnull/clean/profileroff, original7presets/custom0/index0; fresh18:46
84foreign861normal unchanged. No physicalinput/FPS/complete-machine acceptance.
Implementation, exact13source/test/meta pins, tests and docs are in this commit.
NEXT current Mac candidate, then separate reviewed water-cooling P2 correction;
category/sort and further audited grouping graphs remain remembered. Existing
watercoolingP2/unloaded-fire chronology are not fixed by this view-only slice.


November22 [natural regional mire baseline](Verification/MachineSystems/RegionalMire20261122/README.md)
uses ordinary GameBootstrap input: Works17.7 pool13151 at34,8,60→48 into empty2236
0→12 at2030→2040; archive-before-Stop/fresh2040 exact. Source-guided route,
fixture equipment/HP399/max400/capacity and existing mire2231–2235 excluded.
Return via4.9 (no still there) then actual kept camp10.11: still2148/hearth2147,
exact2236 batch2570→2580 12→3water, fuel26.5→21.25 =5batch+.25turn.
One-water pour2580→2590 banks, not extinguishes, hearth; water2carried+1ground.
NEW water-cooling P2 reproduced: recover1 into sameflask2590→2600, repour2600→2610
earns another40-degree dose without netwaterloss. Pool14783 retains1,2236 has2,
hearth20.5fuel/243.584885/lighton; finalarchive-before-Stop/fresh2610 exact.
Not the closed oil-fuel P2. No runtime/tests/build changes or finitecooling/full12
acceptance. Existing unloaded-fire accounting limitation persists.
Main stopped/rootnull/clean/profileroff, original7presets/custom0/index0;
fresh18:02UTC84foreign861normal unchanged. Full source/design and bounded
artifact/gameplay-evidence review in receipt; not a second native operator.
NEXT: reviewed newly-poured cooling-dose policy, native RED/countercases and
smallest correction protecting older pooled water, natural sources, rollback
and callbacks; actual cooling/recovery/save proof. Then useful output against
an existing crop/hydration/repair need and practical supply/service alternatives.
Inventory44/66 and publicstate/search stay published, category/sort remembered.
Long-term lifecycle distinctions/comparisons now explicit in MACHINE-GAMEPLAY-LOOPS.
Current Mac runtime910f86dae unchanged; physical input and all12gates open.


November22 [current public-state Mac candidate](Verification/MachineSystems/MacCandidate910F86DAE/README.md)
packages pushed/remotely verified runtime910f86dae. Universal6000.3.4f1/nondev,
56.487838s/0errors/5warnings exactly matchingFD4. Own74682/window70681 actual
chooser/world/HUD boot; generation2667->2668final/7creatures. Fresh nativeinput
preflightfalse, NO input posted, so packagedinventory/actions/save/controller
remain open.32005source/32008effective/592external/186app pins;83changed0missing/
3added exactoverlay, runtimeC# unchanged on import. Independent final artifact
review0P1/P2; fresh full logicalprefs/Savesabsent and all5priorapps186files each
independently checked. Main stopped/rootnull/clean/profilerfalse,84foreign861normal
fresh17:37unchanged. All copies/builds retained; no new delivery/full12 acceptance.
NEXT: ordinary regional mire supply->existing processing->useful output
source/design/native baseline. Public inventory state and44/66 layout are
published; category/sort and physical-input gates remain separate. Reuse accepted
fixture oil/cold and finite-poured-fuel evidence; do not reopen closed P2.
Chronology/protective NPC prerequisites remain before shared exposure enrollment.

November22 [public inventory state](Verification/MachineSystems/InventoryPublicState20261122/README.md)
adds cached public flask contents/water charges and opt-in garment treatments to
rows, selected/F1 details and literal search, preserving44/66 entries and exact
owners. Shared IToolMenuState validators hide malformed/private liquid IDs;
default-empty enhancement summaries avoid arbitrary type/description scans.
Queries now rebuild retained rows only; same-owner search/cancel retains hidden
private prose while exposing no details during typing. Valid RED34/33fail and
reviewRED10/2fail -> final299/299 (44new255existing), noCS. One old private-cache
assertion was retargeted to retained-private/null-public/no-hit behavior.
Actual workshop1590->1620: findheatcoat/empty/mire;
freecancel;2231pour12->11 createspool1; ordinary step applies mire-soaked/HP400->399;
Cmenu distinguishes11/12 via2231 fromempty via2235; emptyfill gains1 and retires
pool, countedflask+pool units48throughout. Fresh1620 retains exact measured
world/69IDs/coat97/badges. Separate carried-water60->70:skin2254 2->1/debt1->0,
Str/Agi17->18,HP40, row2/3->1/3; fresh70 exact. Archives copied BEFORE Stop.
Initial enabled-but-uninitialized Play recovered by reopening same clean saved
scene then ordinary GameBootstrap; no manualbootstrap/grants/teleports.
Main stopped/rootnull/clean/profilerfalse, original7presets restored,84foreign/
861normal unchanged. Independent source/artifact/gameplay-evidence review
accompanies receipt; no hardware/fullredesign/complete-machine acceptance.
NEXT: current Mac candidate, then ordinary regional mire supply->existing
camp/community processing->useful output source/design/native baseline. Existing
fixture still/oil/cold chains and finite-poured-fuel correction stay accepted;
do not reopen the closed oil-film P2. No new recipe implied. Category/sort remain
separate; chronology/protective NPC prerequisites still precede exposure.

November22 [current inventory Mac candidate](Verification/MachineSystems/MacCandidateFD4A638B4/README.md)
packages committed/pushed/remotely verified runtimefd4a638b4. Universal6000.3.4f1,
nondevelopment63.42s/0errors/5existingwarnings. Own43735/window70668 chooser/
world/HUD boot captured; fresh nativeinputpreflightfalse and NO input posted,
so packagedinventory/actions/save/controller and full delivery remain open.
32003source/32006effective/592external/186app pins;83changed0missing3added exact
overlay, runtimeC# unchanged on import. Main stopped/rootnull/profilerfalse,
Saves restored absent/full logical prefs equal;84foreign861normal and prior
74/477/f8/7c3 each186app files freshly unchanged. All copies/builds preserved.
Independent bounded artifact review accompanies receipt. Inventory layout is
published; category/state/sort broader redesign and all12complete-machine gates
stay open. NEXT: source/privacy audit then disclosed contents/charge/treatment
rows/search to support recurring preparation/processing/freight choices; shared
tool-state capability already supplies flask/skin world-menu distinctions.
Chronology/protective NPC work still precedes exposure enrollment.

November22 [inventory layout qualification](Verification/MachineSystems/InventoryDensity20261122/README.md):
dedicated full-height Inventory shows44 actual entries at1280×800 and66 at1080p
versus10 before, with measured final10×20px body glyphs at both targets.125% text gives30 at800p.
Stable exact owners/search/resize, fitted modals and current hard/soft capacities;
all existing command/time/custody rules remain. Valid RED27/26fail, reviewRED5/
4fail, projectionRED3/2fail; final160+4 with3 overlapping strengthened cases =
161 distinct passes (41new/120existing). Native arrows/pages/search/two-page
reader/cancel/free wear/unwear, paid food2→1 and F5 verified. Separate earned
1580→1590 minimal branch archived BEFORE Stop and fresh Continue preserves exact
actor/food/flasks/treatments/69IDs; first1600 save was not archived and has no
fresh claim. Misrouted follow-up made one real south move, retained/disclosed.
Final corrected camera projection fills both targets. Profiler windows yielded
no marker events, so no timing claim. Main stopped/rootnull/profilerfalse,
original7GameView presets restored;84foreign/861normal unchanged. Final
independent artifact/gameplay-evidence review accompanies the receipt. No new
drop/liquid/camp actual-play or physical controller acceptance. Full redesign
category filters/state badges/sort and all twelve full-machine gates remain open.
NEXT: publish this layout and a current Mac candidate, then make disclosed flask/
charge/treatment state easier to find and compare during recurring gathering,
processing, preparation and freight trips. Preserve choices/services/lasting
upgrades; no compulsory upkeep. Chronology/protective NPC work still precedes
exposure enrollment; graphics cancelled, capstan/cultures parked, Deck deferred.

November22 user priority update: keep the ACTIVE hourly task synchronized with
completed work; retain the denser [inventory screen](INVENTORY-DENSITY-REDESIGN-PLAN.md)
as the next player-facing layout increment after this water/package checkpoint.
Obtain current source/UI critique and native baseline before implementation;
original44/64-entry targets were proposals; the newer receipt above qualifies
44/66 for the first layout. New
[recurring gameplay contract](MACHINE-GAMEPLAY-LOOPS.md) connects finite gathering,
processing, preparation, logistics and settlement services to long-term machine/
tool usefulness. Preserve alternatives and lasting upgrades, with no compulsory
upkeep or unearned production. All twelve full-machine gates and exposure/NPC
prerequisites remain open; capstan/cultures stay parked.

November22 [Mac water milestone candidate](Verification/MachineSystems/MacCandidate7C3BB8A57/README.md):
runtime7c3bb8a57 packages all three water increments; universal nondevelopment,
56.22s/0errors/5existingwarnings. Own83931 chooser/world/HUD boot captured, but
native input preflight false; no packaged movement/action/save/controller gate.
Clean committed31997/effective32000/external592/app186 pins;83generated changes,
0missing/3added with exact overlay, no runtime C# import changes. Player/batch
closed, Saves restored absent/full logical prefs equal;84foreign/861normal and
prior74/477/f8 each186files freshly unchanged. Main stopped/rootnull; two
memoryless renderer console messages disclosed, no clean-console claim. All
copies/builds retained. NEXT: denser inventory layout/source critique and native baseline; chronology/
protective NPC work remains required before enrollment. No source migration or full shared
hydration/twelve-machine/delivery acceptance. Do not repeat qualified water tests,
prototype branches or this package without a new runtime change.

November22 [carried-water transaction correction](Verification/MachineSystems/WaterskinTransactions20261122/README.md):
shared transient relief/quantity receipts, current source/recipient validation,
and native skin snapshot ownership preserve independent injuries/effects/drops
without free water. Spring ownership, rehomed effects and callback paralysis
review gaps have native RED; final288 GREEN incl52new. Existing skin publication/
audio tests now require gameplay rejection/refund, while flask/visibility policy
stays. Actual partial20→fill30→away50→drink60 leaves skin2/debt1/stats17; fresh60,
menu cancellation and full two-page reader preserve exact data/HP40. No global
migration/exposure/NPC/strain or full shared-hydration acceptance. Main stopped/
rootnull;84foreign/861normal unchanged. NEXT: batch current Mac candidate at this
water milestone, then chronological exposure/source verification and protective
NPC behavior before enrollment. Full twelve-machine gates remain open.

November 22 [isolated finite-dose prototype](Verification/MachineSystems/MeteredHydration20261122/README.md):
default-false source policy; only new trial cisterns opt in. One water unit buys
one Parched level, direct batches in one paid action; legacy/infinite stay free/
full. Stale mode/quantity quotes reject, direct undo preserves independent deltas.
Native32 RED/26fail plus5 adverse RED→172 distinct GREEN; final39 after wording.
Actual separate initial10 branches: small1→debt2/fresh20, thenneeded2→clear30;
direct3→clear20/fresh20; fill3→carryaway→three drinks50/60/70→clear. Cancel
preventive quote80 is free; actual viper damage/poison retained, fresh80HP36.
No NPC/exposure/production-source migration or shared-hydration acceptance.
At that prototype baseline the next gate was waterskin fill/drink delta undo,
generic stat-snapshot boundary and commit validation; the newer correction
above closes that bounded gate before chronology/NPC enrollment. Main stopped/rootnull,
84foreign/861normal unchanged; current Macf8 predates both water increments.

November 22 [consistent water-source admission](Verification/MachineSystems/WellSourceConsistency20261122/README.md):
plain direct wells now honor phase/mixing; pool-backed fills honor well faults/
existing oath. Free well rows use the shared outer transaction; one-unit/full-
cure and infinite-well policy unchanged. Native27 RED/19fail→133 focused GREEN,
29 new cases; rollback preserves independent penalties and unspent Stock, pool
redraw only after commit. Actual natural trough12274 at4,9: native drink1770/
energy1000 unchanged, complete reader and fresh1770 exact source/custody/HP400.
No carried skin or existing Parched in that checkpoint: fill/cure/fault/freeze/
oath/veto are test-only. No metering/exposure/AI rollout or new full-machine gate.
Main stopped/rootnull; Macf8 predates this fix. Next: metered finite-dose proposal,
chronological exposure settlement and protective NPC behavior before enrollment.

November 22 [current Mac candidate](Verification/MachineSystems/MacCandidateF8C835549/README.md):
sourcef8c835549 packages expiry/ascent/flask-access fixes; universal arm64/x86_64,
60.64s/0errors/5existingwarnings. Actual own43696 boot/chooser/world/HUD captured;
native input preflight false, no packaged actions/save/reload acceptance. Own
player closed; standalone Saves restored absent/fresh logical prefs equal,
84foreign/861normal unchanged; prior74/477 each186app files freshly unchanged.
Clean source31983/effective31986/external592/app186 pins; generated83changed/
0missing/3added disclosed with exact overlay, runtime C# unchanged on import.
All copies/builds preserved, main stopped/rootnull. No new accepted delivery.
Next: gated shared exposure/NPC survival source trial or other demonstrated
machine integration gaps; do not repeat qualified supply/menu/freight slices.

November 22 [complete proofing destinations](Verification/MachineSystems/ProofingDestinations20261122/README.md):
removed only Press3-flask cap;8 native RED/7fail→96 focused GREEN, filters and
transactions unchanged. Producer volume sort remains but shared menu text sort
can override its order. Actual earned1560 naturalresin12315x1 offers8empty;
last2242 selection1560→1570 spends1/gains1heat, all other bottles unchanged.
Latecoat45→Ward2342 at1580 empties2242, keeps lacquer, adds heat100. Equip
Heat35/ArmorPart.AV1; old signed cloak off/regard13→8. Full reader and fresh
1580 retain exact bottles/coat/fixture6ground/seed5/HP400. Main stopped/rootnull,
84foreign/861normal unchanged. Next: batch current Mac with expiry/ascent/access
corrections, then gated shared exposure/NPC survival; twelve-machine gates open.

November 22 [ordinary resin and ascent custody](Verification/MachineSystems/ResinSupply20261122/README.md):
source-guided seed64 Pitchpod4,9, fixture2167x6 stashed at40,12, walk away and
verify carried0. Gather mature12313 at1410→1420 gives earned12315x2/seed4→5;
fresh1420 keeps separate fixture6 and spent bed. Native Posy row20 lacquer2342
1490→1500 spends earned2→1; fresh1500 exact. Found actual Shift+Comma also
picked up before ascending; fixed comma branch to exclude either Shift, G kept.
Initial compile-invalid RED excluded; valid10 RED/6fail4controls→128 native
GREEN. Actual1540 ascent over fixture6→1550 map keeps ground6/pack1/no popup;
descend1560/F5/fresh keeps exact custody, lacquer, bed, HP400/regard13. No
regrowth/economy/complete-machine acceptance. Main stopped/rootnull,84 foreign/
861 normal hashes unchanged. Current Mac477 predates expiry and chord fixes.
Next: proofing flask destination cap, then batch Mac; exposure/NPC trial gated.

November 22 [ordinary quartz supply and consumers](Verification/MachineSystems/QuartzSupply20261122/README.md):
earned Posy390 had no quartz. Native travel to Spread11,10; vein9366 harvest
650→660 yields2, consumes source; fresh660 retains depletion. Return Posy830,
late41 sign on lacquered Ward1a3… costs1 quartz840. Equip/unequip/re-equip gives
regard8→13→8→13 without accumulation; old cold coat is unworn/Cold0. Already
signed refusal is free. Other mineral makes nonrecoverable radius4 light850,
29 turns after placement; fresh850 keeps29. Twenty-nine paid waits expire it
1140; fresh1140 keeps no quartz/light and same signed worn garment/regard13.
Independent review found a stationary stale shard after expiry. Shared
LifespanPart now invalidates enabled-light expiry fully (lighting), other expiry
only at its former cell. Nine RED cases/3 expected failures; GREEN46+62=108
distinct passes, including aftermath. Native same850→1140 replay visibly removes
the shard without movement; fresh1140 retains state. Initial post-compile boot
precondition failure excluded; settled retry qualified. Current Mac477 predates
this correction. Higher tiers/ordinary resin/package controls remain open.
Main stopped/rootnull,84 foreign/861 normal hashes unchanged. Next: resin supply,
proofing flask destination cap, then gated exposure/NPC survival. Hourly ACTIVE.

November22 [native freight qualification](Verification/MachineSystems/Freight20261122/README.md):
fixture60drams/timber8 plus separateprearrivedspikes2; actual Cinderhold half
shipment420 costs3drams, carry4/cart4, ready4020; fresh420 exact. Ordinary
travelLastCounter, early780noCollect,324nativepaidwaits→4020HP40, collectmerges
Timber4→8/cartempty/57drams; fresh4020 no duplicate collection. No runtime
changes/newtests/ordinaryacquisition or all-machine acceptance. Archives
`/private/tmp/coo-freight-{sent,delivered}-earned-20261122/6ea58bdf6fe04067a63967f244bc4e4a`.
Mainstopped/nullroot/seed0/inputrestored;84foreign/861normal unchanged. Next:
ordinary resin/quartz supply and proofing flask destination access, then gated
shared exposure/NPC survival trial. Current Mac477 unchanged; hourly ACTIVE.


Newest [Posy bench integration](Verification/MachineSystems/PosyBenchIntegration20261121/README.md)
qualifies successful native lacquer, crowded late bench work and shared garment
slots. Earned workshop160→ordinarytravelPosy320; cheapbrightseed+moss350→370
earn8regard. WorncoatedWard lacquer370→380/resin8→7/ArmorAV0→1/Cold35 kept;
third-slot sign refusal atlate40free. LateplainWard19 lacquer380→390/resin7→6,
exactsplit/no duplicatewornbonus. Fullreader andfresh390HP400 keep allowners,
frame200200/PaidPoints4/regard8/two-treatment limit. Fixture supplies, no ordinary
acquisition/sign/hazard/higher-tier acceptance. Independent source/evidence0P1/P2;
mainstopped/nullroot/seed0,84foreign/861normal hashes unchanged. Archive
`/private/tmp/coo-posy-bench-integrated-earned-20261121/8ab6a6384c2a44deb8b3a68303809d8f`.
No runtime changes/new tests; Mac477 alreadycontains behavior. Next: freight,
ordinary resin/quartz supply and consistent proofing flask destination access, then
gated shared exposure/NPC survival trial. Keep hourly program ACTIVE.


Newest package [47761ba77 Mac candidate](Verification/MachineSystems/MacCandidate47761ba77/README.md)
includes both menu fixes and finite fuel: universal arm64/x86_64,59.76s,0errors/
5samewarnings; actual own-process boot/chooser/world/HUD. Native OS input access
false, no packaged movement/actions/reload qualification; last accepted delivery
unchanged. Independent pins/boot/restoration review0P1/P2; ownplayer53179closed,
standaloneSaves restoredabsent/freshlogicalprefs equal,84foreign/861normal hashes
unchanged. Copy/sourcebundle/app/priorpackages preserved. No runtime edits here.
Next Editor integration: use earned workshop supplies to qualify successful Posy
bench work and shared garment slots/effects; ordinary acquisition remains separate.
Freight and gated exposure/AI remain available follow-ups.


Newest correction: [proofing menu access](Verification/MachineSystems/ProofingMenuAccess20261121/README.md).
Coat12/trough12/unpick6 caps removed, no UI changes. Native9RED failures;107/107
focused GREEN plus strengthened late-unpick1/1. Actual native47-choice bottle
menu/late Ward stack split/coating, wearerCold35, paused oldcoat and fresh100/120;
ordinary two reagent presses→4units→latePlate trough costs4+2clay/split/proof/fresh150;
unpick160 refundsnothing, no-proofing refusalfree. Crowd8unpick tested, actual2.
Legacy25row UI suspicion was incorrect: retained popup already scrolls; retracted.
First fresh attempt interrupted by delayedEditorreload excluded; later settled
100/120/150 fresh loads valid. Mainstopped/nullroot/seed0,84foreign/861normal
hashes unchanged. Earned archives in receipt; newestunpicked160, permanent150
retained separately. Next: coherent current Mac candidate (prior74 excludes both
menu fixes), freight and ordinarybench supply; shared exposure/AI stays gated.


[Sounding/noise qualification](Verification/MachineSystems/SoundingHunter20261121/README.md):
ordinary stairs/exploration reached natural depth5 PaleStalker18659. Silent600
Wander/no target→paid rod strike610 acquires player/moves toward them; short
hearing becomes remembered search. Fresh610HP40 retains hunter/target/rod4logs.
No runtime edits/new tests/hush-strip/search-expiry proof. Archive
`/private/tmp/coo-sounding-hunter-earned-20261121/741e3cb41b084fc69e0dcda33fef22b0`.
Independent source/evidence0P1/P2; stopped/nullroot,84foreign/861normal unchanged.
Next bounded correction: analogous bottle-coat/trough/unpick row caps; existing
proofing workshop can provide actual crowded garment menu. Freight and ordinary
bench-material supply remain open; no exposure population activation.


[Posy menu access](Verification/MachineSystems/PosyMenuAccess20261121/README.md):
removed frame8/bench12 candidate caps after four native RED failures;121/121
focused GREEN. Actual formerly hidden fold placement, equal-quality refusal,
finite moss/seed spending and frame→bench eligibility survive fresh1490HP29.
Successful lacquer/sign and crowded bench actual play remain open. User renewed
autonomous machine/tool improvement authorization; next qualify supply routes,
sounding/noise and freight, then choose the next shared capability increment.


[Native Cinderhold lasting repair](Verification/MachineSystems/TownPump20261121/README.md)
now verifies actual readers, finite4unit bill,1320→1330 cost, stable/running/no
repair rows and fresh1330HP29 persistence. Physical drainage/pressure/economy,
patch timing and elder path unqualified. No runtime changes/new tests here.
Grove launch limitation source-traced to installation eligibility→Spread fallback;
actual travel, not requested parameters, qualified the rule. Remaining: sounding
noise, Posy/freight, packaged controls with OS input access, gated exposure trial.

Latest native [NPC/Grove checks](Verification/MachineSystems/NpcAndGrove20261121/README.md)
earn a nonplayer Jet-Blast-forced trap trigger, spent/creature-health persistence,
canonical6,6 fire refusal without charge and allowed cold alternative with fresh
reload. No runtime changes/new tests or general AI avoidance claim. Requested
wild-biome launch differed from observed start; actual travel established the
tested Grove. Main clean/nullroot/defaults restored, normal/foreign preserved.
Next available: sounding noise and town/Posy/repair/freight. Mac packaged control
gate remains blocked by unavailable OS native input; no complete-machine acceptance.

[Mac candidate74d85569a](Verification/MachineSystems/MacCandidate74d85569a/README.md)
now includes the fix below:0errors/5warnings, actual character-chooser/world boot.
Native OS input denied/unavailable; packaged movement/actions/reload remain open,
so this is not accepted delivery. Own player stopped; standalone Saves absent as
before, app preferences logically restored,84foreign/861normal file hashes unchanged.
Preserve both new/prior candidates. Continue available Editor NPC/Grove/town checks.

Latest: [finite poured-fuel correction](FINITE-POURED-FUEL.md) implemented;
native RED23/11fail, GREEN229/229, five mutants killed, independent source0P1/P2.
Actual trap1unit and ordinary ember2unit consumption, collection-row removal
and fresh300HP388/no-oil persistence are in the
[after receipt](Verification/MachineSystems/PouredFuelFix20261121/README.md).
This supersedes the ordinary-poured-oil P2 below; natural/multi-cell source
accounting stays open. Next current Mac packaging/available standalone checks,
then NPC/Grove/town qualification. Main clean/nullroot, foreign/normal preserved;
clone3 has final test mutant and must resync. All12 complete-machine gates open.

Direct-user follow-up connected both Unity sessions and produced a
[nonaccepted Mac candidate](Verification/MachineSystems/MacCandidateBc0511d4d/README.md)
from committed bc0511d4d plus disclosed editor-generated inputs (0errors/5warnings).
[Native cold/brine and camp oil play](Verification/MachineSystems/ColdOilPlay20261121/README.md)
verified freezing/slip/thaw,12mire→3oil, fire/oil damage+smoke/embers, spent
persistence and no retrigger. A source-reviewed P2 remains: ordinary recoverable
oil pools are not debited when their film ignites. Prioritize bounded combustion
accounting RED/design before accepting finite fuel; no runtime changed here.
Both main and owned batch-build copy connected; main stopped clean/nullroot.
All twelve machine gates and standalone candidate gameplay verification remain open.

The scheduled [Beating shared-exposure design pass](BEATING-EXPOSURE-SLICE-PLAN.md)
now specifies a proposed five-actor native audition, elapsed-time/state policy,
finite water corrections, NPC fallback and migration/RED/actual-play gates.
It activates no exposure/AI behavior and accepts no machine or sustainable-town
simulation. Preserve the existing machine-play queue; future implementation of
this proposal begins with its bounded RED/owned-source plan.

Later native [spring-trap and surface-rod checks](Verification/MachineSystems/SpringTrapPlay20261121/README.md)
verified surface refusal without time/log changes, charge-on-brine shock/stun,
fire-on-bare-ground countercase, no retrigger, and fresh armed/spent persistence.
These are bounded fixture checks with remaining reaction/NPC/Grove gates; no
runtime change, complete-machine acceptance or current Mac delivery follows.

The user's later [Beating environment investigation](BEATING-ENVIRONMENT-AUDIT-2026-11-21.md)
confirmed native player-only glare versus shared fire/material effects, and the
missing NPC sun/shelter/water response. Generalizing exposure through per-actor
capabilities is a proposal requiring its own slice; this audit changes no runtime
rules and closes no machine gate.

The user explicitly handed repository, Unity and Git control to Codex and asked
for the shared-source correction, regression assessment and native machine/camp
play without further permission questions. This supersedes the older Claude
ownership hold below. [The current handoff](MACHINE-PROGRAM-HANDOFF.md) is the
entry point for the November 18–21 additions.

The shared source now traverses the bounded full 80×25 zone and retains a saved
stock owner before electing a new one. Native six-case RED, 184-case GREEN,
two killed mutants and independent source review are recorded in
[the stock receipt](Verification/MachineSystems/SharedSourceStock20261121/README.md).
Topology changes that expose separate legacy budgets remain outside this fix.

Actual GameBootstrap camp/workshop play verified camp placement, finite fuel,
night rest, mire separation, same-ID box custody, cancellation, world-map notes,
travel/Continue and pack-up; coating plus lasting proofing combined with gear to
prevent an ordinary frost-tonic freeze. Play found missing turn declarations on
the newer hearth work and proofing press, now corrected through the shared action
contract. [Native play and timing receipts](Verification/MachineSystems/CampPlay20261121/README.md)
record exact limits and remaining gates. The inherited fullrun_h assessment is
complete: 40,376 cases, 40,267 passed, 92 failed, nine inconclusive and eight
skipped. The 91 baseline failures remain; the only new failure was a 180-second
timeout, whose entire family passed 30/30 on an independent isolated rerun (the
exact case took 1.97 seconds). This is not a clean suite or a full run on the
corrected head. See the
[regression assessment](Verification/MachineSystems/Regression20261121/README.md).
Further machine gameplay remains in progress; no complete-machine acceptance or
new Mac delivery is claimed here.

Later bounded play added two paid sounding-rod readings, a free survey log and
fresh Continue retaining both entries. Awning placement provided shade and reset
exposure but a scorpion interrupted the longer comparison. Ascent was refused
with company nearby, so sounding's surface refusal remains open. See
[camp-tool evidence and exclusions](Verification/MachineSystems/CampTools20261121/README.md).
The subsequent awning follow-up confirmed fresh standing reload, ten protected
paid actions with exposure0/no new Parched, and same-ID take-up removing shade
and resuming exposure. Remaining ring-exit/alternative/travel checks stay open.

The user additionally requested an [inventory density redesign plan](INVENTORY-DENSITY-REDESIGN-PLAN.md).
It proposes full-height columns and compact details over existing commands;
implementation and native readability qualification remain future work.

Current continuation — 2026-11-16: the user explicitly cancelled the graphics scheduled task and authorized an hourly task to design, refine and build the world systems needed by the twelve-machine roster. The replacement `caves-of-ooo-machine-foundations` is ACTIVE every hour in the existing chat. `overnight-clearing-art-pipeline` was deleted and its configuration is absent. Machine-support work is implementation-authorized; the old design-only restriction no longer applies to this program. Broader traversal proposals and full settlement-management systems remain outside scope except for a reviewed direct dependency.

Read [the design and twelve-machine dependency matrix](MACHINE-SYSTEMS-DESIGN.md) and `CLAUDE.md`, then inspect Git/Unity ownership. Root owns Unity and Git. Follow the current runnable step without restarting completed inventories. Use bounded independent agents for source, lore and review. Preserve unrelated work, normal saves/preferences, approved editable art and every required build. Steam Deck work remains deferred. No storage reclamation is authorized.

## Current evidence and next action

Initial independent source and lore audits are complete and incorporated into the design. Corrections include the alternate lore corpus, single-liquid vessels rather than mixtures, contact counters rather than drams, material categories rather than salvage quantities, hazard charge rather than batteries, existing concentration/detoxification recipes, existing hauling despite stale design prose, and existing saved kitchen batches. The supporting systems are partial; none of the proposed machines is accepted by this audit.

**Current slice:** M1a safe direct non-water flask transfer is accepted. Native final258/258 plus final51/51 compact-label follow-up (261 distinct checks across receipts) and ordinary acquisition, finite collection, split/consolidate, cancellation, incompatible receiver, ground/contact consequence, movement, travel and fresh Continue are verified; independent source/gameplay review found0 remainingP1/P2. See [MACHINE-LIQUID-HANDLING.md](MACHINE-LIQUID-HANDLING.md). No complete machine is accepted.

**Publication:** M1a is pushed and remotely verified at `0cf1fbda342ee9afa2b04239bfcec84ad7977353`. Existing unrelated changes remain outside that commit. M1b is accepted and remotely published at `c29039d884bbaf0b0d97c3f557f55ebdcaf94d8e`; current Mac delivery for both foundations is verified at `095d17cb2a9f3f4834dcc5ddf37b91ab02657a65`.

**Next runnable action — 2026-11-17 02:32 UTC:** M2a Cinderhold contained recovery accepted/publishedc43631ad6, remotelyverified. Final56/56native; retained398/316overlap, independentwhole-slicesource/gameplay0P1P2. Actualbothbranches/headcraft/captured-oilcontact/travel/freshreload/oldcache complete; do not repeat. New Mac sourcec43631ad6 built0errors6warnings; actualprocess60223recover/move/WorldMap/reenter/F5, fresh60403Continue2710HP40at59,6/headqty1/dry; bothclosed. BatchC43631 package/logs/checkpoint/manifest/restoration; independentdeliveryreview0concreteP1/P2;186packagepins/fixtures/checkpoint/restorationverified. Restoration60396tracked/84dirty/normalstates/priorbuildsexact, Unitystopped/rootnull/clean. NEXT deliveryrecords push, then qualify a useful finite-fluid access problem or component-restoration choice from livingdesign; start with source/lore/problem+actualbaseline, no new machine implementation yet. Full12machines/pump/work/heat/power remain open. Graphicscancelled, Deckdeferred, machinehourlyACTIVE.

M1d implementationb93ad80a9 and current Mac sourceb93 are published with final delivery recordb170940aa, remotely verified. Actual finite process/refuel/relight/travel/F5/freshContinue3620HP20 at59,6 and independent source/gameplay/delivery0P1P2; do not repeat accepted branches or rebuild. Unity stopped/rootnull/clean, baseline root archived,84 prior dirty/normal saves/gameplay preferences preserved; three Unity session-metadata changes disclosed. Graphics cancelled, Deck deferred, hourlyACTIVE, last actual report10:13:24UTC.

**M1b baseline:** ordinary earned seed224401440 PeatWorksMemoryCorrection checkpoint6420HP40; walk Right14 Up3→54,9, Right2 Up4→56,5, Down1→56,6. Normal reader shows sole StoneFloor30833 at56,5; read-only zone inspection finds0 forges. F5 saves6660HP40 at56,6, F12off, under Verification/MachineSystems/FieldWorkRig/BeforeCheckpoint. This cached save is a before/old-save control; acceptance must generate the new fixture ordinarily in a fresh destination. Source confirms the cell is an already-reserved interior furnishing position, off the entrance and locker approaches. Integrate into the authored district packet before receipt capture; no broad reservation relaxation, late scatter or cached retrofit. Normal stocking and the four-timber source stay intact. Unity stopped/rootnull/clean after the baseline, isolated root archived in /private/tmp/coo-machine-field-rig-baseline-root.

**Do not repeat:** Wellmeet/BrineLens water-scarcity qualification failed because wells/cistern already exist. The original condenser remains withheld. Ordinary wells, travel, recovery and crafting are unchanged. M1a is supervised handling with no elapsed work or new save schema; time/work policy remains required for the first processing machine.

**Actual state:** earned isolated seed64; source acquired via Sill purchase22 and Wellmeet barter24→23, finite brine collected90→78 at5.16. AfterCheckpoint is11.10turn2110HP9 with water3/brine11 and poured brine1. AfterTravelCheckpoint is10.10turn2160HP9, same carried contents, F12off. These are QA saves, not normal player saves. The character survived final movement/travel after stepping off a loose waterskin; avoid repeating hazardous travel merely for evidence.

**Recorded obstacles, not new-machine failures:** first sky-sari ambush killed the character after an ordinary transfer action. Later Shift+Comma first picks up a loose underfoot item (InputHandler.cs992), spending a turn before ascent; sari attacks killed that attempt. Reloaded the already saved state and stepped to an empty tile before normal ascent, which succeeded. Preserve that pre-existing keyboard issue as a separate follow-up; do not call either death a successful final run. Earlier malformed isolated-marker load attempts and one zero-test compile-overlap attempt are excluded from acceptance.

## Delivery stages

| Stage | Deliverable | Exit gate | Status |
| --- | --- | --- | --- |
| M0 | Source/lore inventory, twelve-machine dependencies, first loop contract | Canon conflicts resolved; real APIs and a useful optional process selected | Initial audits complete; condenser withheld after actual site qualification; M1a handling implemented |
| M1 | Safe handling prerequisite, then one justified finite processing installation and usable outputs | RED/GREEN/countercases; actual repair, load, process, output use, interruption, travel/reload; source and gameplay review | M1a+M1b published/current Mac verified; M1c accepted/published/current Mac; M1d supervised preparation accepted/published/current Mac verified; elapsed jobs/heat/physical waste open |
| M2 | Finite liquid relocation and environmental consequence | One pump changes a real access/resource situation with a valid destination; no quantity/reset exploit | M2a contained-source/recovery Editor acceptance complete: native56, actual clean/manual/headcraft/ground consequences/travel/reload/old-cache and independent review. Publication/current Mac pending. Pump purpose remains open. |
| M3 | Recoverable components, restoration and heavy relocation | A finite find yields a meaningful repair/salvage choice; capstan and existing cargo rules agree | M1b accepted/published/current Mac; M1c bounded adjacent relocation accepted/published/current Mac; general cargo/components and capstan open |
| M4 | Work, heat and bounded stored power | One exchanger plus a portable consumer interact with ordinary environment rules and finite sources | Planned |
| M5 | Samples, cultures and targeted treatments | Actionable assay, competing finite culture uses and actual hazard interaction; ownership and consent preserved | Planned |
| M6 | Survey, relay and multiple installations | Honest coarse information and one real persistent delivery between meaningful places | Planned |
| M7 | Integrated roster and balance | All twelve have demonstrated purposes/prerequisites, alternatives, readable world states, cross-system play, save stability and reviewed delivery | Open |

Stages may be split into smaller independently reversible commits. Change their order only with a recorded dependency/play finding. Complete code and content together so each accepted slice provides an actual player action. Add shared abstractions only after their consumers and invariants are concrete. Continue beyond the first machine; a completed stage is not completion of the program.

## Per-increment workflow

1. Record the invariant in player terms, inspected sources, lore anchors, scope cuts, content availability and testable outcomes. Treat inherited design documents as proposals until executable behavior is verified.
2. Critique the intended loop for choices, discoverability, pacing, alternative solutions, regional identity, economy and unintended combinations. Resolve actionable findings before implementing.
3. Run native failing tests before production code. Add paired controls and adversarial cases for quantities, custody, callbacks, interruption, time, save/load, invalid resources and scene/zone identity.
4. Implement the minimum complete behavior through normal commands and real owned world objects. Keep documents and UI descriptions truthful. Do not use diagnostic mutation as actual play.
5. Run targeted regressions and relevant performance checks, then real GameBootstrap movement/readers/actions and a fresh reload with isolated saves. Show meaningful images when the change is visible. Keep script-observable and experiential evidence separate.
6. Obtain independent source and gameplay review. Fix P1/P2 issues. Record exact tested behavior, limits, rejected approaches and remaining work.
7. Update this continuation and the dependency matrix in the same commit. Stage exact owned files, safely fetch/integrate upstream, push main and verify remotely. Package current Mac at coherent playable milestones and verify actual launch/action/travel/save/reload; preserve prior packages.

If a required gate is unavailable, record the exact obstacle and pursue bounded independent design/content work. Do not label an unplayed draft accepted or broaden testing without a reason. Keep the user's 40% faster execution target through concise outputs, batching and reuse, without skipping correctness or review. Notify on meaningful progress, accepted milestones, failures or decisions; unchanged automation polls remain quiet.

## Graphics handoff

Accepted main before this priority switch: `b6ab99aba019627f933d8cfd854bd021da28bc9c`, following accepted Drowned Ledger palette `0eb6f6e4c`. Current source `843fb514f` Mac/Linux packages include Olderdeep and Last Counter, exclude Drowned Ledger; all builds are preserved. Graphics completion remains incomplete and is not the active scheduled objective.

Cinderhold's unfinished forge presentation was stopped before actual after-play. Its two production files were restored byte-for-byte from HEAD; its new fixture was moved outside Assets. The exact source, patch, fixture, meta and hash manifest are in `Docs/Verification/GraphicsExecution/CinderholdWorkshop/ParkedDraft`. Native evidence retained: RED 52 failures/2 controls; initial adjacent 325/329 with four fixture errors; corrected forge 54/54, with 275 distinct adjacent passes retained. This is 329 distinct checks across receipts, not a final combined run. A zero-test stale-assembly request is excluded.

Independent review found one still-open P2: the draft reader needs `zone.IsFootprintCurrent(owner)` for an optional footprint, plus a stale-index countercase. No actual after comparison, final review acceptance, publication or build includes this draft. Do not restore it incidentally while implementing machine mechanics.

At handoff Unity was stopped, save-root override null, SampleScene clean and console free of errors after restoring source. The 83 foreign tracked dirty files plus one prior neutral importer change remain preserved. New design/verification records are separately owned by this task.

## Delivery boundary

Current Mac sourceb93ad80a9 includes accepted M1a/M1b/M1c/M1d0/M1d. Two explicit package processes verify finite2-to-1 processing, away-from-bench refuel44.1/50, relight, ordinary travel, F5 and fresh Continue3620HP20 at59,6 with38.1/50 and no returned oil. Previous8bb2 delivery retains relocation/refit acceptance; no repeat required. Known shutdown diagnostics retained. Restoration verified60,315 tracked objects/84 dirty files/normal states/prior builds. No Linux rebuild/runtime, Deck or FPS claim. [Current delivery record](Verification/MachineSystems/BatchB93AD8/README.md). Hourly task ACTIVE; program incomplete.

Last actual itemized progress report: **2026-11-16 03:12 UTC** (minute precision). Completed handling/readers/conservation/ground effects/travel/fresh reload; publication in progress; condenser/pump purpose open; existing packages preserved and exclude M1a. This is not program completion.

Actual itemized progress report **2026-11-16 04:12 UTC** (minute precision): field-rig implementation and423-test native pass; final parent-restoration/input-turn checks running. Ordinary acquisition saved at Cinderhold3330HP40. Mainff80985d3 unchanged; M1b uncommitted/unaccepted, prior builds preserved. Next actual freshWorks repair/haul/use/reload and independent review, then coherent Mac delivery. Hourly task ACTIVE.


Actual itemized progress report **2026-11-16 05:12:03 UTC**: M1b accepted and publishedc29039d88, final98 native checks and independent0P1/P2; current Mac liquid transfer/reload plus workshop repair/refit/travel/reload passed. Combat-interrupted liquid movement excluded. Delivery preservation/records in progress at report time; restored60050tracked objects/84dirty/normalstates/priorbuilds at05:12:56UTC. Next qualify adjacent-border relocation; twelve full machines, processing/time/energy and cross-zone cargo remain open. Hourly task ACTIVE.

Actual itemized hourly report **2026-11-16 06:12:30 UTC**: M1a/M1b and current Mac are published; M1c passes42 native checks for paired placement and two source-effort payments, still unaccepted. Callback/save controls and saved-reader instructions in progress, then actual relocation/refit/travel/reload. Main39f272532 and currentMac sourcec29039d88 exclude uncommitted M1c. No current access blocker; complete processing/heat/power/roster gates open.

Actual itemized hourly report **2026-11-16 07:12 UTC** (minute precision): M1c relocation and current Mac source8bb2b8f82 accepted, delivery published17b4e77e7;193 distinct native checks and source/gameplay/delivery0P1P2. Ordinary marsh oil/lighting purpose qualification in progress; no M1d recipe/code/native acceptance. Finite processing/heat/power and all twelve-machine gates open. Hourly machine task ACTIVE.

Actual itemized hourly report **2026-11-16 08:12:04 UTC**: M1c/current Mac remain published17b4e77e7. M1d0 torch fuel/state reader is accepted through175 distinct native checks, ordinary movement/extinguish/F5/fresh Continue2310HP40 and independent source/gameplay0P1/P2; publication in progress. Existing held-spare ignition also relit the same torch at2320HP40. Oil process yield/waste usefulness and all twelve full-machine gates remain open, no current access blocker. Reader is not yet in the current8bb2b8f82Mac; batch at a coherent processing milestone. Hourly task ACTIVE.

Actual itemized hourly report **2026-11-16 09:13:48 UTC**: main32fe8a800 includes accepted torch reader, M1a/M1b/M1c remain accepted. CurrentMac source8bb2b8f82 excludes reader/strainer. Oil strainer canonical-output final54/54 native and independent source0P1/P2 complete; actual ordinary acquisition2FrogOil and freshWorks arrival3250HP20 in progress. Processing/output use/travel/fresh reload and gameplay review remain required. Adjacent389 receipt includes one pre-existing rig art failure, not all-green. Full machine roster gates open; hourly ACTIVE.

Actual itemized hourly report **2026-11-16 10:13:24 UTC**: Peat Works M1d processing accepted/publishedb93ad80a9, final54mechanics/17presentation/20adjacenticons (separate receipts; no overlap sum). Actual process/refuel/relight/travel/reload and independent source/gameplay/delivery0P1P2. Mainb170940aa/currentMacb93 verified. Cinderhold M2a contained-liquid design and fresh2640HP40 baseline in progress; no production/native acceptance. Separate contained source and central recovery rules required;12-unit receiver correction incorporated. No access blocker; heat/power/fullroster gates open. Next nativeRED, implementation, ordinary acquisition and both recovery branches. HourlyACTIVE.

Actual user-facing progress report:2026-11-17T01:03UTC, answering the overnight status question. Report distinguished five delivered additions from the unfinished contained-oil draft and disclosed that later hourly triggers produced no further accepted features.

Actual progress report **2026-11-17 02:32 UTC**: completed M2a publicationc43631ad6 and current Mac actual recovery/travel/fresh reload; independent delivery review/records in progress. Native56/56 final, prior overlapping398/316 regression; actualclean/manual/headcraft/groundslip/reload/oldcache. No active access blocker; twelve-machine prerequisites remain incomplete. Next qualify useful finite-fluid access or component restoration.

Next foundation qualification constraint: `LiquidPoolPart.Volume` currently drives bounded contact exposure; it is not established as water depth or a passability threshold. A pump proposal must author and test the physical access relation and finite receiving destination before implementation. The accepted12-unit tray is deliberately easy with one flask, so it cannot justify compulsory pumping. Prefer a genuinely useful recoverable component/restoration choice if no supported finite-fluid access situation qualifies. This is a design constraint, not implemented new behavior.

Independent delivery/next-slice review —2026-11-17 02:38UTC:0P1/P2 package/source/helper/inputpins/normalstate. Next concrete qualification: use an ordinary low-Strength character against the existing120-weight work rig, via normal starter/menu/save acquisition, without stat editing. Inspect `DragRules` and `FieldWorkRigRules` actual limits first. Compare restore-in-place, reclaim2braces, and moving the intact service. If ordinary hauling fails for this build and moving it serves a real off-site purpose, a bounded anchored capstan may have a justified role. Preserve existing hauling and load weights; no cargo enlargement or compulsory relocation. Obtain actual baseline/readers/refusal before proposing mechanics. This is an unimplemented next-slice hypothesis, not accepted capstan behavior.

## Qualification correction —2026-11-17 02:43UTC

The proposed low-Strength120-weight-rig experiment is withdrawn after source verification. All seven shipped starter presets have Strength16+; ordinary haul budget isStrength×8, so even the lowest128budget exceeds120. RigMinLiftStrength0 adds no refusal. Do not manufacture a weaker character, impairment, or heavier rig to justify a capstan. Independent bounded review agrees.

Existing candidates: MillStone150 exceeds128 but is explicitly off-spindle scenery with no working consumer and is placed clear of corridors; moving it proves hauling only. SmithAnvil120requiresStrength18, but communal service is not released salvage. Better candidate: ConnectedHeavyFrame136, ditchkeepers' hoist frame, placed byConnectedSpreadSatellite into an optional short approach while a detour remains open. Description explicitly permits dragging aside and denies harvestable reward. It could establish mechanical advantage for ordinaryStrength16 only if actual route savings justify using an anchor; it does not establish machinery restoration or a recovered service.

NEXT actual ordinaryStrength16 starter/HeavyFrame shortcut baseline: inspect source-supported existing build-picker audit opt-in before launching, then walk/read/refusal and compare existing detour. ExistingLiveFlatPixelGreyboxTools suppresses buildchoice by default. Attempted normalN under that launcher createdClassicStrength18, seed64,11.10 at41,13 and was excluded; no stat editing or mechanism testing occurred. Entire root archived `/private/tmp/coo-capstan-classic-excluded-0ebe2490`, stopped/rootnull/clean verified. NativeAuditBootstrapSettings.AllowStartingBuildChoice is an existing owned-audit opt-in, but it must survive the normal bootstrap/domain reload and be restored after session; use a minimal reviewed operator path, not the directed-melee audit's simulation driver. No production/native changes. Mainae6d383f8/currentMacc43631remain accepted; all84foreign changes/normal saves/builds preserved.

## Reclamation continuation —2026-11-17 03:44UTC

M3a proposedtwo-contract loop reviewed0concreteP1/P2: fixed damagedtool manifest (failedbinding only, soundcanonicalCinderhookhead+fieldhaft) offers dismantle versus destructiveironblank+timber; ironblank cold-dressed intoexistingIronSpike1d4/+1PV/Piercing. NoIron->Steel, no genericCompositionmass, nooriginalcomponentID/temperpreservationclaim, noheat/smelting/jobs/oreprocessingacceptance. Basevalue45preservedparts vs11processedspike+timber means actual usefulness/tradechoice stillgate; one spike cannotcomplete two-spikerepairs. ExistingWeaponSalvage unchanged.

Actualbeforebaselinecomplete: earnedseed704860946 Cinderhold62,7 tick2720HP40, target62,8soleStoneFloor12078 visible/explored, ordinaryLook/reader. ReclamationBaseline/SiteCheckpoint cachedcontrol; reuseBeforeEntry2390forfuturefreshgeneration. QAroot399e6198archived, Unitystopped/rootnull/clean. All84foreignchanges/normalstates/buildspreserved. No production/native changes yet. NEXT nativeRED for bounded validatedmanifest/recipe planner and callback-safe executor, then smallest implementation/content and ordinarybothbranches/use/trade/travel/reload/oldcache/review. PreparationActions.Transform lacks finalparticipantvalidation; do notassumeexistinghelperhandlesallcallbacks. Approvedartreusefit/routingreviewrequired; parkedforgegraphicsnotaccepted. Main/currentMacc43631unchanged.

## Current runnable continuation —2026-11-17 04:04UTC

User priority remains reclamation and regional component processing; capstan and cultures are parked. M3a actual Cinderhold before baseline and bounded design/lore/placement review are complete. Read-only registry/input-permission/plan code and two native fixture files are uncommitted under Assets; no default catalogue, menus, executor, new blueprints or town placement exists. Final50/50 native checks pass, after initial16RED, content7/24RED and planner16RED, plus review7/50RED. Overlapping receipts are not additive. Independent final draft source review0P1/P2 closes null-content/Solid-tag/station-blueprint gaps only. Evidence is ReclamationBaseline and ReclamationDraft; no new playable machine is accepted.

NEXT native executor RED and callback/save/capacity/scheduling controls, then smallest complete implementation/content and actual both-branch/output-use/trade/travel/reload/no-retrofit acceptance. Source-specific yields only; no Iron->Steel, tag-derived stock, heat/smelting or generic jobs. Native plan.Current is insufficient permission. Preserve existing weapon salvage, pack crafting, smith/tray and independently optional profile ownership. Blank art/usefulness remain unresolved acceptance gates, not access blockers. See MACHINE-RECLAMATION-PROCESSING.md for exact scope and owned draft.

MCP test startup may timeout while Unity later executes the run. Review-red.xml is the authoritative50-case native RED receipt43/7 at04:01:50; final-green.xml/job2dbc9d35e09f422399eece6b9d748824 is50/50. Excluded zero-start/stale jobs are disclosed. Verify existing TestRunnerApi.IsRunActive=false and loaded source types before changing Assets or running again, in addition to compilation state.

Main93f1114cb/currentMacc43631ad6 remain unchanged; no new commit/build or post-change gameplay is claimed. Unity stopped/rootnull/scene clean after baseline and final tests. Existing84 foreign tracked changes, normal states, approved art, parked forge graphics and required builds remain outside owned edits. Machine hourly remains ACTIVE; graphics cancelled, Deck deferred.

Actual itemized hourly report **2026-11-17 04:07UTC**: completed actual Cinderhold before capture/readers and reviewed finite-transform design; in progress uncommitted read-only registry/permission/plan with50/50 native checks and bounded source0P1/P2. No new gameplay acceptance, publishing or build. Next transactional executor RED and complete recovery/use loop; truthful blank presentation and economic usefulness remain open. Main93f1114cb/currentMacc43631ad6 unchanged. No current Unity access blocker; full twelve-machine gates open.

## Current runnable continuation —2026-11-17 05:19UTC

M3a executor/default definitions/source content are implemented but UNCOMMITTED/UNACCEPTED. Final native294/294 (100feature+194adjacent) jobb6c1c245446a438098a3a29e65314ea8; overlapping older receipts not additive. Independent bounded final source review0remainingP1/P2 after retainedquote/metadata, instance stacking+allclonepaths, creation-safe policy and full portabledependency fixes. No new gameplay/delivery acceptance. Read latest MACHINE-RECLAMATION-PROCESSING.md and ReclamationDraft/owned-files.json; these owned source changes must not be treated as foreign.

NEXT actual fresh GameBootstrap acquisition/both recovery branches/component use/actions/ticks/trade/travel/fresh reload/old-cache controls. BeforeEntryCheckpoint2390HP40 WorldMap36,9 -> ordinaryEnter gives fresh6.6 at39,11; workshop find around62,8, readyforge55,5. Existing ReclamationBaseline/SiteCheckpoint2720HP40 is cached no-retrofit control. No injection, deletion or old accepted oil branch repetition. Legacy glyph presentation is provisional; source/blank visual review remains required. No additional native suite unless a new change/failure/concern justifies it. Then exact owned records/source commit, upstream integration/push verification and Mac coherent milestone.

Whole-game composition principle saved in CLAUDE.md/SYSTEMS-DESIGN-PRINCIPLES.md: reusable Parts/capabilities/events on machines/NPCs/objects; meaningful alternatives and common-sense interactions throughout the world. Before-veto/committed reactions and live-instance policies implemented. General yield/cost modifier contributions and NPC job planning remain open. Current main93f1114cb/currentMacc43631ad6 unchanged. Preserve84foreignfiles, normal states, parked graphics and all builds. Graphicscancelled, Deckdeferred, machinehourlyACTIVE.

Actual itemized progress report2026-11-17 05:08UTC: feature86/86 and adjacent194/194 thenpassed; three independent source review safeguards still being closed, no accepted release/build. Subsequent final294/294 and source0P1/P2 are native/source evidence only; ordinary actual play remains next.

## Current runnable continuation —2026-11-17 06:38UTC

M3a bounded reclamation/cold-dressing accepted through actual play and independent0remainingP1/P2. Final mechanics294/294 reused; selected-action clipping fixed by shared ledger preview with2RED,27GREEN and2strengthenedcontrols (28distinct UIcases; overlap not additive). Actual fresh source13538, both exclusive recovery branches and blank→existingpoint each pay one action, freecancel/awaymenu, normalbindingtrade, ordinary weaponassembly/equip and actualcombat/travel/freshreload complete. Preservedaxe13679 fresh3440HP15at79,6; reclaimedpointweapon fresh3890HP25at79,6. SameIstren intactfind16 versushead14+haft1/point2+haft1; quoteonly/nointactsale. Cached2720control sourcecount0/soleStoneFloor12078 complete. See ReclamationActual/README.md; no accepted oil branch repetition.

NEXT exact owned records/source commit, safe fetch/integrate/pushmain/remote verification, current Mac milestone with actual process/equip/move/travel/F5/freshContinue and independent delivery review. Current main93f1114cb/currentMacc43631ad6 still exclude M3a until publication. Preserve all84foreign trackedfiles hash-exact, normalstates/approvedart/parkedgraphics/allbuilds. Unitystopped/rootnull/scene clean after actual controls. Source/blank legacy glyphs have0blockingvisualissues, blankbarcosmeticdebt. No additional mechanics suite unless newchanges/failures/concerns.

Afterdelivery qualify regional rawmaterial→existingusefulcomponent: sourceverification/lore/finiteordinaryacquisition+problem+alternatives before recipe. Recoveredmanufacturedblank is notrawore. Noiron→steel/tag-derivedstock/heat/smelting/jobs acceptance. Generalquoted yield/cost modifiers, arbitraryeffects, NPCjobplanning and all12fullmachines remainopen. Capstan/culturesparked, graphicscancelled, Deckdeferred, hourlyACTIVE.

Last actual itemized hourly report2026-11-17 06:08:29UTC: both recovery routes plus readable preview and point chain in progress; main/currentMac unchanged. Actual06:38 milestone update distinguished bounded acceptance from publication and regional processing.

## Current runnable continuation —2026-11-17 06:55UTC

Main1237eae9f is pushed/remotelyverified; acceptedM3a and currentMac same source delivered. Build29565098a8 32.714707s0errors4warnings; actualprocesses1667/3127 reclaim→point/awayhaft→normalbindingbarter→existingassembly/equip→worldmap36,9/reentry→F5/freshContinue3230HP40at56,12,equippedpoint/2drams/28weight. No repeatedacceptedEditorbranches needed. Independentdelivery0P1/P2 validates186packagefiles/665effectivepins (2operationalMCPlogdiffs), checkpoint/archive and60500snapshotobjects/84dirty/normalstates restored06:51:23UTC. TwoAXrelaunchincidents excluded (older1736 savefailure,current3008noactions); useexactapppath andavoidAXqueryafterquit. LogsretainComputeBuffer/threadshutdownwarnings; noLinux/Deck/FPSclaim. Source/currentMac nowincludeM3a; nextrecordcommit isdocs-only.

NEXT regionalrawmaterial→existingusefulcomponent qualification, not newcodeyet. Boundedsource/design review underway: avoidduplicate shipped timber→FieldHaft,cord→PlainBinding,Tepuibone→CudgelHead. Candidate hide→LeatherBinding or speciesstave→OakHaft needs realfinite authoredsource/provenance, usefulordinaryconsumer, lore/alternatives/economy and actualbaseline before design/nativeRED. Do not inferleatherfromanycorpse or oakfromgenericSalvagedTimber; no mandatoryhunting/chore/heat/job introduced. Followreviewedcandidate, keeprootUnity/Git. All12machines/generalquotedmodifiers/arbitraryeffectpropagation/NPCplanningopen. Capstan/culturesparked, graphicscancelled, Deckdeferred, machinehourlyACTIVE. Unitystopped/rootnull/clean/tests idle, bothMacprocessesclosed; preservationrestored. Lastactualhourlyreport06:08:29UTC, deliverymilestoneupdate06:49UTC.

## Current runnable continuation —2026-11-17 07:14UTC

Main9dd99383a remotely verified; current Mac source1237eae9f and boundedM3a are accepted/delivered. Do not repeat either recovery branch or delivery. All84foreign trackedpaths remain hash-exact; Unitystopped/rootnull/scene clean after new qualification.

Next proposal is Docs/MACHINE-LEATHER-FEED.md: already-cured harness-leather offcut→existingLeatherBinding, not rawhide/tanning. Independent bounded source/design critique0concreteP1/P2; no newproduction/nativechanges or offcut supplier yet. Reused accepted actual IstrenFennit13585 Tinker66,6 binding23/book10+13barter/+1Hitweaponconsumer. Fresh baseline root9be3812f at2770HP40/56,6 has visibleTinker but no visibleWeaponsmith; do not repeat oldfruitlesssmithsearch or infer wholezoneabsence. No naturally damaged eligibleleathercarried; armorrepair source exists via leather-equipment-stitch/PortableEquipment/BrokenEffect but not newlyplayed. Rootarchived/private/tmp/coo-leather-feed-baseline-9be3812f.

NEXT lock finite opening supplierpacket separate from TraderPart's renewable tables, exact inputweight/value/trimmingyield, and accessibleusablecuttingcapability/selection/brokenpolicy; then nativeRED and smallest complete implementation/actualaway-cutting/consumer/travel/reload/review. Names/Piercing/tags do not establish cutting. Generic timber is not oak, corpse harvest cannot silently discard meat for hide, freshhide is nottannedleather. Existing timber/cord/tepuibone shaping alreadyships. No compulsoryforge/newmachine/toolwear/heat/jobs justified by offcutcutting. All12fullmachines/generalmodifiers/arbitraryeffectpropagation/NPCplanning remainopen; capstan/culturesparked, graphicscancelled, Deckdeferred, hourlyACTIVE.

Actual itemized hourly report2026-11-17 07:08UTC (minute precision): completedM3a/native294plus28distinctUI/actualbothbranches; deliveredcurrentMac and published9dd99383a; inprogresscuredleatherfeedqualification; noaccessblocker/fullmachineheatrawhideorejobsopen. Clock checked07:09:02UTC after report.


Actual itemized hourly report2026-11-17 08:08:01UTC: M3b leather cutting focused51/51 and adjacent172/172, final independent source0P1/P2; actual fresh acquisition/use/reload in progress. Main730761f26/currentMac1237eae9f unchanged; M3b remains uncommitted/unaccepted, no current access blocker. Full machines/rawhide/heat/jobs remain open. Supplier actual correction later switched finite packet from optionalTinker to existingWeaponsmith only; focused9/9 at08:17UTC closes source-specific stock/RNG/composition/restock/no-retrofit controls. Earlier receipts overlap and are not additive.


## Current runnable continuation —2026-11-17 08:52UTC

M3b bounded prepared-leather feed is Editor accepted, UNCOMMITTED and not yet packaged. Final retained45feed mechanics (from51focusedreceipt),9updatedsupplier,172adjacent and28itemicons each pass; overlappingolderruns are not additive. Independent source/gameplay/visual0remainingP1/P2. Exactowned changes are LeatherFeedDraft/owned-files.json, not foreign work.

Actual earnedseed704860946 freshWeaponsmith supplied finite2offcuts separatelyfromrenewabletable; normalCinderholdstock preserved. Bought1for11 vs22finishedbinding, cancelled/no-toolrefusal at3520, recoveredDagger2679 then3530→3540awaycutting60,20:1binding13681/nooffcut/sametool. Existinghead13679+haft13680+newbinding→equippedaxe13682; movement/WorldMap36,9/reentry/F5/freshContinue3570HP40 at59,20 with4drams/30weight/nooffcut/noloosebinding/supplier1. Cached2790SupplierBefore actualtrade retainsall11stockIDs/counts/offcuts0. Source/visualreview correctedpreexistingwrong sword portrait throughapprovedHatchet/exactownedassembly sharedworld/reader routing; final28icons includesnull-art refusal. Actual correctedportrait reviewed. No newart/combat/armorrepair/rawhide/tanning/fullmachineclaim. See MACHINE-LEATHER-FEED and LeatherFeedActual.

NEXT review/stageexactownedfiles withlivingrecords, safelyfetch/integrate/pushmain/remoteverify, then coherentcurrentMac actualcutting/outputuse/movement/travel/F5/freshprocessContinue and independentdeliveryreview. Currentmain730761f26/currentMac1237eae9f still excludeM3b untilpublication. Do not repeat acceptedM3a/oil/rig branches; newMacmayreuse earnedBeforeCut3530. Afterdelivery qualify a real regional rawfeed/recovery opportunity before another recipe; do not duplicate shippedtimber/cord/tepuibone or inventtag-derivedyields/ore/tanning/heat/jobs. Full12machines/generalmodifiers/arbitraryeffectpropagation/NPCplanning remainopen.

Unitystopped/rootnull/sceneclean/nativeidle,84foreignhash-exact, normalstates/approvedart/parkedgraphics/allbuildspreserved. Graphicscancelled,capstan/culturesparked,Deckdeferred,machinehourlyACTIVE. Lastactualhourlyreport08:08:01UTC; actualboundedmilestoneupdate08:52UTC.

## Current runnable continuation —2026-11-17 09:06UTC

M3b accepted source4dcb2d5bc555a628fecdb3cc02d748ea4a6482cd is committed/pushed/remotelyverified. CurrentMac same source builda0f154a745 succeeds30.400383s346.75MB0errors4warnings. Exactpath guarded processes23615/24069 pass earnedBeforeCut3530→ordinaryawaycut1offcut→binding→packassembly/equipcorrectHatchet→walk59,20/worldmap36,9/reentry/F5/freshContinue3570HP40/4drams30weight. No new acquisition/oldcache/M3a/oil branch repetition; those Editor controls alreadyaccepted. Bothplayersclosed, noAXrelaunch exclusions. Batch4DCB2D5 has186packagepins/674effectiveinputpins, only2operationalMCPlogs differ. Source/gameplay/visual reviews0P1/P2, nativeevidence unchanged/no redundant suites.

Guardedrestoration09:06:15UTC verifies all60564trackedobjects/60562filehashes,84prior dirtypaths, normalEditor states/preferences, originallyabsentstandaloneSaves, previousMac1237EAE and inheritedLinuxsnapshotentries. Build's exactsolepreloadedAssets insertion into alreadydirtyProjectSettings archived then restoredpriorbytes; cleanURPserializations restored; entireownedtestSaves archived/private/tmp/coo-machine-restored-4dcb2d5bc. Allrequiredotherbuilds untouched; do not claim every laterLinuxarchive rehashed. StandardshutdownComputeBuffer+threadnotices retained,0Exception:/Error:lines. NoFPS/hardware claim.

NEXT exactownedrecordscommit/push/remoteverify (independentdeliveryreview0P1/P2), then ordinary regionalrawfeed/recovery qualification before furtherproduction. No duplicate timber/cord/tepuibone, noComposition-derivedyields, noassumedrawhide/tanning/ore/heat/jobs. Full12machines/generalmodifiers/arbitraryeffectpropagation/NPCplanning open. Unitystopped/rootnull/clean/tests idle; all84foreign/normalstates/approvedart/parkedgraphics/builds preserved. Graphicscancelled,capstan/culturesparked,Deckdeferred,hourlyACTIVE; lastactualhourlyreport08:08:01UTC.

Actual requested itemized hourly report2026-11-17 09:08UTC (minute precision): accepted/publishedM3b4dcb2d5bc; currentMac same source actualcut→assembly/equip→travel/F5/fresh3570HP40, allindependentreviews0P1/P2.54featurecases across45+9separatereceipts,172adjacent,28icons; nooverlapsummed. Restoration60564tracked/84foreign/normalstates/priorbuildsexact. Deliveryrecordpublication andnextregionalfeedqualification inprogress; noaccessblocker. Fullmachines/heat/rawhide/generalmodifiers/effectpropagation/NPCplanningopen.

## Current runnable continuation —2026-11-17 09:24UTC

M3b source4dcb2d5bc and currentMac are accepted, deliveryrecordb3b739353 pushed/remotelyverified. Do not repeat recovery/leather/packagedeliverybranches. Allindependentreviews0remainingP1/P2; nativeevidence unchanged. Hourlytask refreshedACTIVE.

NEXT read MACHINE-COMPOSITE-PROCESSING.md. Bounded sourceinventory favors qualification of leatherbinding+cord→existingGuardLashing (+2Hit/−1PV vs leather+1Hit), not anotherreleasedfind or bracedhaftupgrade. Proposedonly: no production/native changes or newprocessacceptance. Existing registry is single-source; a qualified real composite need would justify finite ingredientclaims/sharedquote/rollback, not a simulationframework. Mixedinput admission for existing savedstock, instanceeffects, exacttool/custody and economics must be decided beforeRED. Neutralperf.35 proposedbuy32/sell8; maxperf.95/Loved.85 buy11/sell27 exposespotentialmargin, not provenrenewablevendor/freecycle. Do not hideoutputresale penalties or addcompulsorywear/fees torescue a recipe.

Actual prerequisiteinspection completed from separateearlierFieldWorkRig/ReclaimTravel4600HP40/seed704860946,17.7 at56,7,5drams59weight: carriedcord16147×1/timber18845×1 and visiblebandage/plainbinding/snaresmenu; cancellednoinputloss/tickadvance. CapturedCompositeBindingBaseline; archived/private/tmp/coo-composite-binding-baseline-356fada9. This branch is NOT the acceptedCinderhookbranch; nevermergeinventories. Excludedfirstroota3e884d0copiedfixtureunderGameIDwhileownedLastGameIDstillpointedemptyAuditMarker; non-gziprefusalcorrect, newgamekeyexcluded. Earnedgzipvalid; freshsession exactpairreplacedmarkerthenContinueworked. Notruntimecorruption. Futurefixtureload: replaceownedauditmarkerbeforeContinue; F5writesactualGameID.

Next ordinary actual: use LeatherFeedActual/ConsumerTravelCheckpoint3570HP40 at59,20; inspectCinderhookaccuracy/penetration/finishedalternatives, acquirecordfromexistingordinarysource, compareopportunitycost. Do not alterstats, mergebranches, inventrawhide/ore, inferIronfromSteelDagger or stockfromMaterial/Composition. Existingbasic timber/cord/tepuibone recipes remainshippedalternatives. No naturallydamagedeligibleleatherrepairqualified. Full12machines/heat/jobs/generalmodifiers/arbitraryeffectpropagation/NPCplanning remainopen. Independent boundeddesign/sourcecritique0remainingP1/P2 after explicitchest-source and gain1Hit/lose1PV corrections; no newprocessacceptance. Unitystopped/rootnull/clean/nativeidle;84foreignhash-exact, normalstates/approvedart/parkedgraphics/allbuildspreserved. Graphicscancelled,capstan/culturesparked,Deckdeferred,hourlyACTIVE; lastactualrequestedhourlyreport09:08UTC minuteprecision.

Independentsourcecorrection09:27UTC: GuardLashing is a finiteLastCounterChest supply, not a verifiedvendorgood. Proposedtradeformula is an analyticalvaluetest, not an observedGuardvendorquote. CurrentCinderhook3570hasnoloosebinding; existingTryReforge returnsdisplacedbinding. Nextcandidateactualpath: acquiretwoordinarycords, braidonePlainBinding, realforge55,5reforgeuntemperedCinderhook, retainreturnedLeatherBinding+secondcord. Thisexistingrecoverypath needsactualqualification beforecompositeimplementation; noinputgrant/branchmerge/newdismantlingclaim.

Refinement: returnedbinding is canonicalblueprinttype, not originalID/privateeffects; preserveexistingtemperloss. Design/sourcecritique0remainingP1/P2 after corrections; nextactualqualification remainsrequired.


## Continuation —2026-11-17 10:29UTC: M3c implemented, acceptance pending

M3c finite composite processing is implemented, UNCOMMITTED and not yet accepted. Same Cinderhook branch actually acquired two cords via ordinary ripe knotflax harvest at2.6, braided one and used existing forge refit to recover canonical leather; AfterRefitCheckpoint4350HP40 supplies real inputs without grants/branch merging. The shared optional bounded extra-input contract now supports1cord+1leather→existingGuardLashing away from a forge with a working explicit cutting tool. Existing saved cord opts in through its authored PreparationRecipePart braid_binding permission; no migration, source injection or content retrofit. Exact instance claims, before-veto/commit events and stack/consumption safety preserve shared Parts behavior. See MACHINE-COMPOSITE-PROCESSING.md.

Final327/327 native checks (50 feature+277adjacent), jobbc4cf8e6e54047539577ab12c1c68411; earlier receipts overlap. Independent source review0remainingP1/P2 after native RED fixes for protected consumption, null metadata and catalogue revocation. Actual Editor cancellation/missing-leather control4350, same binding pickup4360, offsite process4370, existing refit/equipHit0/PV−1 and travel/F5/freshContinue4420HP40at57,6/4drams31weight passed. Same weapon13682, returnedPlain13695, inputs spent once. Root archives preserve earned inputs/after/reload; Unity stopped/rootnull/clean. All84 foreign tracked hashes exact; normal states/approved art/parked draft/all builds preserved. Last requested hourly report actually sent2026-11-17 10:08:16UTC.

NEXT independent gameplay/visual review, exact records/source commit/push/remotelyverify, then current Mac at that source. Do not rerun unchanged native suites or accepted prerequisite acquisition. Current delivered Mac4dcb2d5bc remains valid and excludes M3c. No new heat/jobs/ore/tanning/full-machine acceptance. Graphics cancelled; capstan/cultures parked; Deck deferred; hourly machine task ACTIVE.


Acceptance update10:32UTC: independent gameplay/visual review accepts the demonstrated bounded M3c slice,0remainingP1/P2; separate source review same. Review receipts distinguish root read-only custody observations from screenshots and preserve limits. M3c Editor/native acceptance complete; exact publication/current Mac next. No full-machine acceptance.


## Continuation —2026-11-17 10:37UTC: published M3c/current Mac final review

Mainc600701d0 pushed/remotelyverified; M3c327/327native and independent source/gameplay/visual0P1/P2 accepted. Exactowned41paths include livingrecords/tests/evidence. CurrentMac same source actualprocess→existingrefit/equip→walk/worldmap/reentry/F5→freshprocessContinue4410HP40at57,6/4drams31weight passed; both38683/38867closed. Package186files,678effectiveinputpins/twooperationalMCPlogs only; no clean-tree claim. Snapshot/restorer60612tracked/84preexistingdirty/normalstates/priorMac4dcb+inheritedLinuxexact10:36:20UTC; ownedSave tree archived, no deletion. Independent finaldeliveryreview0P1/P2; currentMac accepted. NEXT finalreview/recordscommit/push, then strongest independently source-qualified regionalfeed/recoveryproblem actualbaseline. No newruntime qualified. Full12machine/heat/ore/tanning/jobs/generalmodifiers/arbitraryeffectpropagation/NPCplanning open; do not reimplementshippedtimber/cord/tepuibone. Unitystopped/rootnull/clean/tests idle; graphicscancelled,capstan/culturesparked,Deckdeferred,hourlyACTIVE. Lastactualrequestedhourlyreport10:08:16UTC.


Delivery acceptance10:39UTC: independent BatchC600701/delivery-review.md verifies all186packagefiles/678effectiveinputpins, checkpointgzip/fixtureprovenance/freshprocess consistency, exact dirtyProjectSettingsdelta, all60612trackedobjects/84preexistingchanges and normalstates/priorMac+inheritedLinuxsnapshotentries.0remainingP1/P2. CurrentMac sourcec600701d0 accepted; records commit/push next. Do not repeat accepted process/refit/travel/reload or unchanged tests. Next qualification stays source/lore/problem/actualbaseline before any new runtime.


## Handoff publication correction

M3c implementationc600701d0 and currentMac delivery420f82367 are accepted, pushed and remotely verified. Do not repeat accepted processing/refit/travel/reload/tests or delivery. Next is ordinary qualification of the proposal in Docs/Verification/MachineSystems/SpurgrassBaseline/source-selection.md:2SpurgrassSpine+1KnotflaxCord→existingSerratedEdgeComponent. This is source selection only, not a shipped recipe or actual acquisition. Real harvest/stacking/throw-recovery/consumer and finished-stock comparisons must establish purpose; explicit work permission and canonical weapon-payload/equipped-input/save policies require independent design critique before RED. No new runtime changes. All twelve complete-machine gates remain open; graphics cancelled, capstan/cultures parked, Deck deferred. Preserve84preexistingtrackedchanges/normalstates/art/parkedgraphics/allbuilds; sole Unity/Git owner coordinates with ACTIVE hourly task.

Last observed Unity stopped/rootnull/clean. A later bounded agent run hit a usage limit; subsequent heartbeat arrivals do not establish implementation progress. Inspect current ownership/state before continuing. The saved hourly prompt has been corrected to the completed-delivery state and proposed qualification.


## Continuation —2026-11-17 (Claude, sole Unity/Git owner): spurgrass kit and shared bookkeeping guard

Ownership: Claude took Unity/Git on 2026-11-17 20:30 CST. The Codex `caves-of-ooo-machine-foundations` heartbeat stays ACTIVE but its runs hit a usage limit (weekly reset about 2026-11-22); pause or hand back before it does real work. See [MACHINE-SPURGRASS-KIT.md](MACHINE-SPURGRASS-KIT.md).

Qualified and implemented natively (actual play, Mac package and independent review still open): 2 SpurgrassSpine + 1 KnotflaxCord -> 1 existing SerratedEdgeComponent by the shared M3c HandCutting contract, with the cord as the cutting-permission source. Independent design critique found the proposal unsafe as specified: a declared input could not tolerate the permanent throw receipt (`ThrownRecoveryPart`) that gameplay attaches, so throwing one spine from a stack and merging a recovered spine would have broken; the same latent hazard already existed for cord and leather through the permanent empty `StatusEffectsPart`. Fix: an internal built-in allowlist of inert bookkeeping Parts (compared by inertness, never by `Revision`), tolerated by split, merge and ingredient admission; a live claim or active effect still refuses; `Register` cannot override a built-in. `MeleeWeaponPart` and `HandlingPart` joined the scalar payload list. The wielded-spine premise was corrected: spines have no `EquippablePart`, so only packed spines count (pinned).

Evidence: `MachineSpurgrassKitTests` 42/42 (then strengthened after independent review: live-claim variants, live-mark helper); hazard reproduced by 3 throw/merge baseline pins that fail with only the catalogue row; full EditMode run in the isolated clone 39,016 tests / 90 failures, of which 85 fail identically on unmodified HEAD and 5 did not reproduce in isolation (details in the kit record). Editor actual play completed 2026-11-18 (harvest, pickup stacking, throw/recover merge, process, forge assembly, equip, travel, F5, fresh Continue; see SpurgrassActual/observations.json). Mac package and independent gameplay review remain open. No new Part, save field, tool or capability.

## Direction change —2026-11-17: machines in the world, modular architecture, then home/base

The user directed: flesh out the modular machine/object architecture, then add machines to every town and village so players can find and use them, then design the home/base system. The user's own twelve-machine pitch is stored verbatim in [MACHINE-ROSTER-VISION.md](MACHINE-ROSTER-VISION.md). This supersedes the one-qualification-at-a-time cadence for placement work; the per-increment workflow above (RED first, actual play, independent review) still governs every shipped behavior. Design inputs are produced by read-only research workflows whose reports live in the session scratchpad and are summarized into living docs as they are accepted.


## Continuation —2026-11-18: machine architecture slices S1, S2, S6, S7, S4, S5 (Claude)

The user's direction (a modular machine/object architecture, machines in every town, then home/base) was worked through read-only research workflows (survey, three-way architecture design with synthesis,
twelve per-machine V1 dossiers, a settlement placement plan, and three independent critiques; reports are in the session scratchpad and are summarised into the living docs below) and then implemented in the spec's order:

- S1 saved-Part golden, S2 marker and relapse retention, S6 registry and census, S7 generic placement with Editor actual play: [MACHINE-INSTALLATIONS.md](MACHINE-INSTALLATIONS.md). Forges now stand in Gantry, Tine and Slip (the Last Counter row was pulled to Pending on 2026-11-18, see the update at the end);
  census Installed 4, Present 2, Exempt 5, Pending 9. Editor actual play (fresh world, three towns, F5 and fresh Continue, cached-save control) is recorded in `Verification/MachineSystems/InstallationsActual/`.
- S4 station contract with oracle parity: [MACHINE-STATIONS.md](MACHINE-STATIONS.md). S5 reader contract: [MACHINE-READERS.md](MACHINE-READERS.md).
- The home/base design is a separate read-only design (concept: set-down places on a portable-base foundation; prerequisite slice: retention addenda) in the session scratchpad; it is not implemented.
- Honest finding for the owner: the dossiers and the independent critiques agree that, apart from the forge, none of the twelve machines has an honest purpose in the shipped world yet (they need a finite find, a resident supplier or
  a local problem ahead of the apparatus), so "a machine in every town" is a ratchet (Pending may only fall), not a claim. Tally and Wellmeet forges are held back on recorded gates; the three surface-village forges are convenience rows placed on the
  owner's standing instruction and can be deleted as data before release.

### Update — 2026-11-18 (later): S3 declared action time and disabled rows, and the rig explains itself

- S3 is committed and pushed (`f0e99eb08`): [MACHINE-ACTION-ROWS.md](MACHINE-ACTION-ROWS.md). First consumer: a fouled, dragged, dismantled or wrecked field work rig lists a dimmed "work at the rig" row with the reason; played in the Editor
  at the Peat Works. PB0a (a well-keeper renewal also unloads a village) is covered by two retention tests with no production change (starting village and Wellmeet only).
- Last Counter forge played in the Editor (fresh world, forge at 34,10 beside the Envoy, interact menu opens).
- Test status: all four commits were verified with targeted and adjacent suites; the one adjacent failure in S3's set and eight in the forge/rig/hauling set fail identically on the HEAD baseline. The full-suite comparison (3.4 hours) is still running
  in the verification clone against the S1-S7 tree and will be recorded when it finishes.
- Next: S12 (multi-stack claims and reasoned process rows, critique-gated), S9 (stations on one receipt, probe table first), S19 (installable state for home/base), then opportunity-first content (the dossiers' honest V1s).

### Update — 2026-11-18 (evening): home/base PB0a to PB3, S12b and where things stand

- **Home/base (built, pushed):** PB0a retention coverage, PB1 zone classifier and pure placement check, PB2a same-entity set down / take up / hand over on the install state (S19), PB2b the camp box (35% Quartermaster line, glyph only), PB3 reader line and derived list.
  Played in the Editor with real keys: set down, reader, F5 and a fresh Continue, deposit, take up with contents, hand over (`Verification/MachineSystems/HeldFixturesActual`). Play found that a solid box could not be filled; an adjacent-deposit change fixed it. Record: [HOME-BASE-IMPLEMENTATION.md](HOME-BASE-IMPLEMENTATION.md).
- **S12b (built, this commit):** extras-only multi-stack pooling after an independent critique, with its rollback fix: [MACHINE-MULTI-STACK-CLAIMS.md](MACHINE-MULTI-STACK-CLAIMS.md), [critique](MachineDesign/critique-s12-multistack.md). The spurgrass kit row no longer vanishes when the spines are two stacks.
- **Design reports** are in the repo: [MACHINE-ARCHITECTURE-SPEC.md](MACHINE-ARCHITECTURE-SPEC.md), [HOME-BASE-DESIGN.md](HOME-BASE-DESIGN.md), `MachineDesign/`.
- **Owner decisions to confirm or reverse:** the Gantry, Tine and Slip convenience forges (data rows); the camp box as the first kit and its glyph-only art; mechanical placeholder strings pending voice review; lifting family-owned zones for set-down (11 of 22 zones near the start are refused).
- **Next:** S12a disabled-row reasons; S9 stations on one receipt (probe table first); lifting family-owned zones after the consumer audit (PB1a); the rig as a held fixture (PB5, with the S19 relocation stamp); a second honest machine per the dossiers. The full-suite comparison against the HEAD baseline (3.4 hours) was still running when this was written.

### Update — 2026-11-18 (late): kits, readers and what makes a camp worth having

Built and pushed since the previous update: the rig is stamped `Relocated` on a committed crossing (S19); set-down is admitted in Spread exploration-family zones after an independent audit (opening yard kept refused); three more kits made by composing existing parts, each carried-inert and a
station only while set down: the **camp hearth** (finite cooking fire with feed, relight, dry gear, cook; lights the night only while burning; keeps its fuel through take up), the **camp bench** (a portable cold forge the shipped process rows find with no scanner edit) and the **camp still** (the
alchemy station made portable); an **arrival note** ("You keep here: ..."); **"works here"** lines on every forge, bench and rig; and the S9 probe table ([MACHINE-STATION-USERS.md](MACHINE-STATION-USERS.md)). Kits are sold by the Quartermaster (box 35%), Provisioner (hearth 35%), Tinker (bench 25%) and Apothecary (still 20%).

Why this is the engagement answer so far: a camp is now a set of machines the player assembles from real shops and finite materials where the work is (stash, fire, cold bench, still), each legible (reader lines, "why not" rows), each with a consequence (weight, price, fuel, fire beside a wooden box, a flame that must be carried
from a village fire), and none of them conjures a resource.

Played in the Editor: the camp box end to end, and the hearth's set down, standing menu and feed (the flame requirement found in play). The bench and still are native-only.

Open owner decisions: the Gantry, Tine and Slip convenience forges; glyph-only art for all four kits; voice review of placeholder strings; a portable bed (D-H1); rest at a lit hearth (a balance decision: the shipped coals opt out of rest, and resting by a finite fire would need a fuel cost); a world-map marker for kept places.
Next candidates: S9 (stations on one receipt, probe table done), hearth-and-still synergy only if the owner wants brewing to depend on heat, the hostile-reach and fire audit (PB1a item 3), found kits in waysites (changes fresh generation, needs pinned-test care), and the full-suite comparison recorded below when it finishes.

### Update — 2026-11-18 (night): full-suite comparison and the Last Counter row

- Full EditMode run against the HEAD tree: 39181 tests, 96 failed against a baseline of 90. Of the six differences, three were timeouts under contention (`SpreadBiomeCoverageTests.EveryActualSurfaceAndCommittedLairHasNativeGeometry` seeds 1 and 1729, and a `ThrowingPayloadComparisonTests` W7 case); all 37 tests in those two classes pass when rerun alone.
- The other three were real: `ThresholdCampsRenderingTests.EveryVisibleNativeOwnerAcrossTwoChunksHasAReadOnlyModel` failed on seeds 64, 1729 and 729490642 because the Last Counter forge is an unmodeled native blueprint in the 3D spawn-ring library (`LastCounterVoxelKitLibrary` has no `TinkersForge` family). The forge row for the Last Counter is therefore Pending, not Installed (census Installed 3, Present 2, Exempt 5, Pending 10). Gantry, Tine and Slip are unaffected. Detail in [MACHINE-INSTALLATIONS.md](MACHINE-INSTALLATIONS.md) Decision 1.
- Verified after the change: installation, pipeline-priority, threshold-camps-rendering and saved-Part-contract suites, 194 tests, 193 pass, 0 fail, 1 not run.
- Lesson recorded: a new native blueprint placed by the world generator needs a check against the 3D spawn-ring coverage ratchet, not only the installation suites.
- **Bench and still played in the Editor (2026-11-19):** a fresh world, real keys, a wild zone. The carried bench lists no craft rows; set down, its world menu is the full forge picker and three granted parts forged a weapon; taken up, it returned to the pack. The carried still lists no brew rows; set down, it brewed a mending flask from mendleaf and fire-moss. No defect found. Not observed: save and reload of either kit in play, a mishap brew, the dismantle/reclaim/point rows at the bench. A first launch was lost to a mid-play script recompile and is not claimed. See `Verification/MachineSystems/HeldFixturesActual/observations.json`.
- **Independent review of the home work (2026-11-19):** three confirmed defects fixed with RED-first tests (`CampKitReviewTests`): a dropped, or taken-up-then-dropped, camp bench or still was a working station (`PackableFixturePart` is now a readiness guard and its `IsInertKit` rule feeds the forge and still menus and the presence-only rule); a hearth's light did not reach the light map until something moved; a burning hearth that was taken up stayed lit for ever (take-up now puts the fire out and keeps the fuel). The registry-wide carried-kit pin was strengthened and mutation-checked. Recorded as honest limits, not fixed: fire fuel does not burn through rest or absence (nothing in the build simulates a zone you are not in). Stale doc sentences corrected. The full-suite run in progress at the time of the review was started before these fixes.


### Update — 2026-11-19 (late): invented uses, and the flint striker

- **Direction.** The user: "if a use for a machine does not exist, analyze the game and the purpose of the machine and invent a use for that machine that flows naturally with the rest of the game mechanics and the lore", and "you have full free reign of the game". [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) records the analysis (five rules a machine can attach to), one invented use per machine, and the order. It replaces the dossiers' "do not build without a demonstrated purpose" advice for these machines; the systems principles still bind.
- **Built:** the flint striker (the dynamo's first invented use, "Spark"): a finite, non-stacking tool that is one more answer to `IgnitionSources.Find` (torch and finite cooking fire light from it through their ordinary verbs, real fires always first and free), that offers "strike a spark" toward an adjacent cell only where one point of heat would light a combustible coating (through the shared `ApplyFireToTile` routine, run after the action commits), and is renewed by knapping with an iron spike. 36 tests (`FlintStrikerTests`, `FlintStrikerAdversarialTests`); the neighbouring torch, hearth, review and saved-Part contract suites (101 in all) pass. A mutation run (free strikes, wetness ignored) was caught by 10 of the 36. Sold by the Tinker at 40%.
- **Not done and not claimed:** no Editor play of the striker yet; no grove-law claim (not tested for a bare burning slick); voice review of the placeholder strings; art (glyph `/`); the charge rod and every later invented use.
- **Full-suite comparison for the previous session's tree** (`full_s9`, 39182 tests): 91 failures against the 90-failure baseline. The one extra, `SpreadBiomeCoverageTests.EveryActualSurfaceAndCommittedLairHasNativeGeometry(1)`, is a 180 s timeout under contention and passes alone (3 of 3 seeds, 52 to 56 s each).

### Update — 2026-11-19 (night): rest at the hearth, settling at the still, and the Editor play that did not happen

- **Built since the striker** (all RED-first, reviewed by a fresh read-only agent, review findings fixed and pushed): rest beside a lit camp hearth (`HearthRestPart`: the fire burns ticks times its burn rate of fuel for the time slept, cools by the thermal part's own decay, and is refused up front with the numbers if the fire is out, short of fuel or would die before the rest ends, so one lighting is about one night); settling at the still (`LiquidSettlingPart`: a flask of bog-mire settles to water or oil, four to one, over a lit fire that spends 5 of its own fuel; brine deliberately excluded). Both are in [MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) 3b and 3c. Commits `9088368d5`, `0ad433082`, `2d110e404` and the review fixes `479b20584`, `31e2ea9fe`.
- **Editor play was attempted and not achieved.** The main Editor compiled the new code (about 15 minutes after the refresh) and the Camp Kits scenario launched: the HUD showed the scenario applied (weight 108 of 270, the loadout plus the striker). Key input then never reached the game: the Input System only takes keyboard events when the Game view has focus, and neither injected key events nor background OS key presses were accepted while Unity was not the frontmost application; full-screen control was requested and not answered (the owner was away). Play was stopped, the scene is clean, and no file was changed. **The striker, hearth rest and settling are therefore verified by native tests (RED/GREEN, mutation for the striker, adversarial files) and independent review only, not by play through the real menus.** The first thing to do when the owner is present: run `Caves Of Ooo > Scenarios > Home > Camp Kits`, answer N at the save prompt, and try light torch (away from the campfire), spark the slick two cells west, then travel to wild ground (Overworld.10.11.0), set down the hearth and still, feed and light the hearth, rest, and settle a flask of mire.
