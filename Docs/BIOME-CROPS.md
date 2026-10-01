# Biome crops: discovery, useful harvests and regional cultivation

Status: implemented and reviewed, based on `38252ce7e`; native selected regression sweep 1930/1930, independent cave Play route 5/5, surface Play growth interrupted by ordinary combat as documented below. User authorizes implementation of discoverable existing crops, five useful crops per biome, native models, world placement, and any required harvest mechanics. This is original Caves of Ooo content, not a copied Qud roster or an exact-parity claim.

## Directive

Ship a complete find → examine → harvest → use → replant → water → grow loop. A crop description cannot promise a mechanical benefit that its real harvested item cannot deliver. Preserve real owners, routes, the authored terrain of special places, saved depletion, and prior crops. Keep regional ecology recognizable; avoid identical full gardens in every chunk. Publish the plan, evidence, reviewed scope and limitations with the code.

## Scope and corrections from source audit

| Assumption | Verified source | Decision |
|---|---|---|
| Ten biome enum IDs means ten current surface regions | WorldMap.cs:15–45 retains Cave/Desert/Jungle/Ruins for legacy and underground routes; the current map has six canonical surface biomes. | Five NEW species for each canonical surface biome plus five for actual underground caves:35 new species. No invented extra Desert/Jungle/Ruins surface regions. Existing three crops stay. |
| Existing crops are obvious near spawn | Last native route used one labelled travel shortcut. Current actual spawn is Morrowfast; planted crop site is one chunk west. | Add natural town directions and visible local guidance; verify arrival/discovery, not just blueprint existence. |
| Duration makes any stat tonic temporary | TonicPart.StatBoost changes stat Boost permanently. | No renewable permanent-stat plants. Use verified temporary effects and supported consumables. |
| All reagents do something individually | Existing brewing uses property combinations; heat alone does not produce the heat+combustible burn recipe. | Validate actual harvest-to-use outcomes for every species; unsupported mixes are not counted as utility. |
| A pocket light illuminates its carrier | LightSource affects loose/equipped items, not stowed inventory. | Tell players to equip/drop portable lights and test the native path. |
| Any fiber can repair the same fault | Repair recipes match exact material blueprint. | Processing outputs real accepted supplies; no implied substitutes. |
| New vegetation can be added at the end of any zone | Spread worksite validators capture owner sets/Parts before their final checks. | Respect generation ordering and protected-site receipts; additive placement only where the final graph remains valid. |
| Overwrit should receive ordinary farms | Canon describes a scraped, faint region and excludes restoration/farmable bleed. | Sparse new botanical growth at margins; no resettlement, recovered lost people or bleed-resource farming. |

## Planned milestones

1. Record the complete35 species roster, non-food uses, growth durations, habitat and readable silhouettes. Validate all reused mechanics against source; implement only necessary shared gaps.
2. Add a validated data catalogue and35 seed/crop/harvest triples. Reuse standing maturity, real ground outputs and returned seed. Add one guarded field-processing action where a raw plant must become a usable material. Tests first, including refusal/rollback/ownership and finite supply.
3. Add deterministic regional discovery placement, varying safe cultivated patches and explicit coverage of all 5species per ecology. Protect authored towns, lairs, archives and existing worksite owners. Make already-existing three crops findable through town guidance. Explicitly decide save compatibility after the placement audit rather than blindly replacing cached areas.
4. Create an independent original model pack for all 35 species, each with seed/sprout/ripe wet/dry stages and seed/harvest objects (280forms), with distinct silhouettes rather than palette swaps. Extend both native wilderness and authored village owner renderers without changing old 37forms or global biome style.
5. Native Unity integration: catalogue validation, each real harvested item's use, world coverage/access, identity/rollback/save/unload, old farming and renderer regressions, and real keyboard discovery/harvest/growth/use footage. Inspect representative models at the actual camera.
6. Q1–Q4 plus dedicated adversarial review, fix significant findings, keep bounded lower-priority issues documented, update evidence and living docs. Fetch/rebase before authorized push to main.

## Roster and actual uses

- Spread: Claspbean, Pitchpod, Drawgourd, Wickrush, Marlroot.
- Sodden: Sumpsieve, Drowsebell, Chillcress, Slipsedge, Peatlantern.
- Beating: Sunbladder, Shalebean, Shadefan, Cinderpea, Spurgrass.
- Grovelands: Choirwick, Knitmoss, Sourmantle, Murmurpod, Sealbark.
- Overwrit: Absentmint, Margincress, Binderroot, Greybladder, Hollowchime.
- Stump: Raingourd, Prismreed, Scarlet Sundew, Cloudwick, Gripfrond.
- Underground: Lampvein, Knucklecap, Sootroot, Veilpuff, Brinebutton.

## Spread — five new species
| Stem / harvested item | Model silhouette | Growth / yield | Exact utility Parts | Play use / lore |
|---|---|---|---|---|
| `Claspbean` → `ClaspbeanPulp` | low paired kidney pods held by clamping pale tendrils; &g | 20 moist rounds/stage; 2 output +1 seed | Tonic: Drink=false; CureTonic: CureEffect=BleedingEffect | Apply once to stop one BleedingEffect; other ailments remain. A hedge-row dressing that rural hands squeeze onto cuts. |
| `Pitchpod` → `PitchpodResin` | three resin-heavy orange lantern pods on a forked stalk; &W | 24 moist rounds/stage; 2 output +1 seed | Reagent: PropertiesRaw=heat:1,combustible:1 | Brew a burning coating through the shipped heat+combustible rule; throw/apply uses actual tonic effects. Mundane lamp and fire-working resin, never named as an existing refueling mechanic. |
| `Drawgourd` → `DrawgourdShell` | low looped vine with one tall narrow-necked buff gourd; &y | 28 moist rounds/stage; 1 output +1 seed | Waterskin: Capacity=3, Charges=0 | Fill near real clean water, then drink three stored doses; empty harvest has no free water. The familiar river-country reusable drinking vessel. |
| `Wickrush` → `WickrushCandle` | upright rush tuft ending in cream waxy clubs; &Y | 22 moist rounds/stage; 1 output +1 seed | TorchLight: LightTemperature=450; Equippable: Slot=Hand; LightSource: Radius=3, LightColor=&Y, Intensity=0.45, Enabled=false; Thermal: Temperature=25, AmbientTemperature=25, FlameTemperature=300, HeatCapacity=0.5; Fuel: FuelMass=24, MaxFuel=24, BurnRate=0.5, HeatOutput=0.5 | Equip or drop, light beside an actual fire, then extinguish; finite fuel uses TorchLight. A rush dipped by its own wax; a cheap field light, not permanent bio-light. |
| `Marlroot` → `MarlrootClod` | five red-brown root knuckles showing through a blue-green rosette; &r | 26 moist rounds/stage; 2 output +1 seed | BotanicalProcessing: OutputBlueprint=FireClay, OutputCount=1 | Process one harvested clay-coated clod into one existing FireClay; use the real well-lining recipe. Roots bind fine local clay; the useful yield includes their attached mineral clod. |

## Sodden — five new species
| Stem / harvested item | Model silhouette | Growth / yield | Exact utility Parts | Play use / lore |
|---|---|---|---|---|
| `Sumpsieve` → `SumpsievePad` | flat perforated leaf disks held above a dark bulb; &g | 24 moist rounds/stage; 2 output +1 seed | Tonic: Drink=false; CureTonic: CureEffect=PoisonedEffect | Apply to remove PoisonedEffect; gas-poison and fungal infection are separate ailments. Peat-cutters keep the bitter filtering leaves on dry shelves. |
| `Drowsebell` → `DrowsebellBladder` | drooping blue inflated bells on bent stems; &B | 30 moist rounds/stage; 1 output +1 seed | GasGrenade: GasId=sleep-vapor, Density=20, Level=1 | Throw to create actual sleep vapor; breathing targets may sleep and damage wakes them; allies are affected too. A bog bell that exhales when its ripe skin splits. |
| `Chillcress` → `ChillcressTip` | low silver fern with hooked cyan new fronds; &C | 22 moist rounds/stage; 2 output +1 seed | Reagent: PropertiesRaw=cold:1 | Brew the frost rule; its resulting coating can be thrown to apply Frozen. Cold peat-fed sap; no promise of refrigeration or heat immunity. |
| `Slipsedge` → `SlipsedgeGel` | ribbon sedge with translucent droplets on flattened tips; &c | 24 moist rounds/stage; 2 output +1 seed | Reagent: PropertiesRaw=viscous:1 | Brew the slick rule for a Wet coating; this is actual Wet status, not an unimplemented trip trap. Glossy gel from leaves adapted to waterlogged soil. |
| `Peatlantern` → `PeatlanternCup` | cupped amber fungal shields held on short black stems; &w | 32 moist rounds/stage; 1 output +1 seed | Equippable: Slot=Hand; LightSource: Radius=3, LightColor=&Y, Intensity=0.45, Enabled=true | Equip in Hand or drop to cast amber light; stowed light is occluded. A modest cultivated bog fungus, not a manufactured lantern. |

## Beating — five new species
| Stem / harvested item | Model silhouette | Growth / yield | Exact utility Parts | Play use / lore |
|---|---|---|---|---|
| `Sunbladder` → `SunbladderShell` | low ribbed succulent with one swollen pale aqua fruit; &c | 32 moist rounds/stage; 1 output +1 seed | Waterskin: Capacity=2, Charges=1 | One saved drink in the ripe fruit; fill its surviving shell with two drinks at clean water. A modest water reservoir at sheltered well margins, never an infinite water source. |
| `Shalebean` → `ShalebeanWax` | low grey angular leaves with stacked ochre wax beans; &y | 30 moist rounds/stage; 1 output +1 seed | Tonic: Drink=false; StatusTonic: EffectName=Stoneskin, EffectMagnitude=1, EffectDuration=6 | Apply temporary one-point Stoneskin reduction for six turns. Existing stacking is bounded by duration, not permanent stats. A mineral-rich wax grown in narrow irrigated shade beds. |
| `Shadefan` → `ShadefanHood` | broad accordion leaf on a stubby bent central rib; &Y | 28 moist rounds/stage; 1 output +1 seed | Equippable: Slot=Head, UsesSlots=Head; Armor: AV=0, DV=0 | Equip on Head to satisfy the actual Beating sun-cover gate; zero armor advertised. The dry leaf opens into a head-sized sun hood. |
| `Cinderpea` → `CinderpeaOil` | blackened-looking crescent pods with red seams; &R | 32 moist rounds/stage; 1 output +1 seed | Tonic: Drink=false; StatusTonic: EffectName=Burning, EffectMagnitude=0.35, EffectDuration=0 | Throw the oil pod as the existing tonic splash to apply Burning; self-Apply also burns and must be plainly warned. Heat-concentrating oil from an exposed wasteland legume. |
| `Spurgrass` → `SpurgrassSpine` | compact radial grass with long ivory spear-shaped awns; &W | 26 moist rounds/stage; 2 output +1 seed | MeleeWeapon: BaseDamage=1d3, PenBonus=1, MaxStrengthBonus=2, Attributes=Piercing; Handling: GripType=OneHand, BulkClass=Light | Throw via real penetration/AV damage; recover the spine where it lands. No invented poison property. Caravan children gather the rigid awns from protected rows. |

## Grovelands — five new species
| Stem / harvested item | Model silhouette | Growth / yield | Exact utility Parts | Play use / lore |
|---|---|---|---|---|
| `Choirwick` → `ChoirwickBranch` | pale forked fruiting horns threaded with violet luminous tips; &M | 30 moist rounds/stage; 1 output +1 seed | Equippable: Slot=Hand; LightSource: Radius=3, LightColor=&M, Intensity=0.45, Enabled=true | Equip or drop as a quiet violet light; no song/confidence effect is invented. Grown, never manufactured; a small cultivated extension of the living substrate. |
| `Knitmoss` → `KnitmossPad` | pale braided cushions with green seams and no red fruit; &G | 24 moist rounds/stage; 2 output +1 seed | Tonic: Drink=false, Healing=2d4 | Apply a moss pad to heal 2d4 once. An explicitly non-red wound dressing so the existing grove red-food warning remains true. |
| `Sourmantle` → `SourmantleFold` | tiered lime frills around a narrow pale stalk; &g | 28 moist rounds/stage; 2 output +1 seed | Reagent: PropertiesRaw=corrosive:1 | Brew an acid tonic for the actual Acidic splash path; drinking is harmful. A frilled substrate fruit whose sap loosens what the grove wants to take apart. |
| `Murmurpod` → `MurmurpodBladder` | three violet balloon pods joined by a pale grown cage; &m | 34 moist rounds/stage; 1 output +1 seed | GasGrenade: GasId=confusion-vapor, Density=20, Level=1 | Throw for a real confusion cloud: DV/Agility penalties and confused movement, with friendly fire. The ripe bladder exhales a disorienting vapor; no new mind-control or lore truth. |
| `Sealbark` → `SealbarkSlat` | rectangular curling plates grown around a thick short stalk; &y | 34 moist rounds/stage; 2 output +1 seed | BotanicalProcessing: OutputBlueprint=SalvagedTimber, OutputCount=1 | Process one shed bark slat into one existing SalvagedTimber for the native gate-brace repair. Naturally grown structural plates; preparation makes a usable brace, not a Choir sawmill. |

## Overwrit — five new species
| Stem / harvested item | Model silhouette | Growth / yield | Exact utility Parts | Play use / lore |
|---|---|---|---|---|
| `Absentmint` → `AbsentmintLeaf` | sparse paired grey leaves with one conspicuous missing pair; &w | 30 moist rounds/stage; 2 output +1 seed | Tonic: Drink=false; CureTonic: CureEffect=ConfusedEffect | Apply to remove ordinary ConfusedEffect; never removes Thinning, Urqu or narrative memory loss. New sparse margin growth; its ordinary sharp scent is an aid to concentration. |
| `Margincress` → `MargincressRibbon` | very low pale rosette with thin magenta-edged curled leaves; &m | 26 moist rounds/stage; 2 output +1 seed | Tonic: Drink=false; CureTonic: CureEffect=BleedingEffect | Apply to stop one ordinary bleeding instance. Pioneer leaves that cling to the edge of intact soil, not recovered undertext. |
| `Binderroot` → `BinderrootKnot` | one white looped root above ground under sparse teal shoots; &c | 34 moist rounds/stage; 1 output +1 seed | Reagent: PropertiesRaw=binding:1 | Brew through the shipped ward rule for Stoneskin. This is ordinary damage reduction, not a cosmic name-anchor. A stubborn new root; player-facing prose avoids claiming to heal the scraped region. |
| `Greybladder` → `GreybladderCup` | thin grey bulb with a folded spout and two nearly colorless leaves; &w | 34 moist rounds/stage; 1 output +1 seed | LiquidVessel: Capacity=6, Volume=0, LiquidId= | Fill from a finite liquid pool and pour through actual conserved volume commands; this vessel does not drink or purify. A naturally hollow pioneer growth; remains a mundane shell after harvest. |
| `Hollowchime` → `HollowchimePod` | three open hanging grey capsules with yellow inner membranes; &Y | 38 moist rounds/stage; 1 output +1 seed | GasGrenade: GasId=stun-vapor, Density=15, Level=1 | Throw to release real stun vapor. No sound attraction or enemy luring system is claimed. Air-filled surface capsules; never a singular bleed curio or erased historical object. |

## Stump — five new species
| Stem / harvested item | Model silhouette | Growth / yield | Exact utility Parts | Play use / lore |
|---|---|---|---|---|
| `Raingourd` → `RaingourdCup` | small five-leaf rosette holding an open stone-pink cup; &M | 30 moist rounds/stage; 1 output +1 seed | Waterskin: Capacity=4, Charges=0 | Harvest an empty four-drink cup, fill at true fresh water, then drink stored doses. A foothill rain-cup relative in actual soil pockets, not on the sacred bare circle. |
| `Prismreed` → `PrismreedPith` | tall jointed reeds with square cyan crystalline-looking tips; &C | 34 moist rounds/stage; 2 output +1 seed | Reagent: PropertiesRaw=conductive:1 | Brew the shock rule; the resulting tonic applies Electrified through native splash/use. Conductive sap, not a new mined mineral or industrial wire. |
| `ScarletSundew` → `ScarletSundewDew` | radial red spoon leaves bearing pale adhesive beads; &R | 32 moist rounds/stage; 1 output +1 seed | Tonic: Drink=false; StatusTonic: EffectName=Acidic, EffectMagnitude=0.35, EffectDuration=0 | Throw as a corrosive tonic splash; self-Apply is harmful and examination must say so. A deliberately original cultivated sundew form rooted in the canon slope ecology. |
| `Cloudwick` → `CloudwickTuft` | three white club flowers rising from deep-blue narrow leaves; &W | 34 moist rounds/stage; 1 output +1 seed | Equippable: Slot=Hand; LightSource: Radius=3, LightColor=&C, Intensity=0.45, Enabled=true | Equip or drop to give cool light; no light in a closed pack. Cultivated luminous associations in moist fissure soil. |
| `Gripfrond` → `GripfrondWrap` | paired hooked rose-grey fronds over a dense basal curl; &y | 36 moist rounds/stage; 1 output +1 seed | Equippable: Slot=Handwear, UsesSlots=Handwear; Armor: AV=1, DV=0 | Equip as handwear for actual one AV; no climb speed, traction or new climbing mechanic is claimed. Tough folded fronds protect the hands used on sharp stone. |

## Underground Cave — five new species
| Stem / harvested item | Model silhouette | Growth / yield | Exact utility Parts | Play use / lore |
|---|---|---|---|---|
| `Lampvein` → `LampveinFan` | low blue fan-gills crossed by two bright white veins; &C | 32 moist rounds/stage; 1 output +1 seed | Equippable: Slot=Hand; LightSource: Radius=4, LightColor=&B, Intensity=0.45, Enabled=true | Equip or drop as a four-cell blue lamp. Catacomb cultivation serves the established bio-light economy; no new theology assertion. |
| `Knucklecap` → `KnucklecapWax` | five squat tan caps with knuckle-like raised plates; &y | 38 moist rounds/stage; 1 output +1 seed | Tonic: Drink=false; StatusTonic: EffectName=Stoneskin, EffectMagnitude=2, EffectDuration=4 | Apply two-point Stoneskin reduction for four turns; no permanent AV gain. The stiff wax is harvested from a cultivated mat, not a petrified god fragment. |
| `Sootroot` → `SootrootPulp` | charcoal root bulb crowned by short pale hooked stems; &K | 28 moist rounds/stage; 2 output +1 seed | Tonic: Drink=false; CureTonic: CureEffect=BurningEffect | Apply to remove BurningEffect; the environment can ignite the user again. A cool root dressing used near cramped underground hearths. |
| `Veilpuff` → `VeilpuffBladder` | single pale puffball partly wrapped by blue slit leaves; &C | 40 moist rounds/stage; 1 output +1 seed | GasGrenade: GasId=cryo-mist, Density=10, Level=1 | Throw for actual cryo gas, cold damage and Frozen; affects damageable matter and friends, not a harmless smoke screen. Cold moisture condensed into a cultivated underground puffball. |
| `Brinebutton` → `BrinebuttonPaste` | tiny white buttons with black central pores on a low pink mat; &W | 40 moist rounds/stage; 1 output +1 seed | Tonic: Drink=false; CureTonic: CureEffect=FungalInfectionEffect | Apply to cure actual fungal infection and restore its stat shift; no blanket disease cure or lasting immunity. Salt-rich cultivated buttons prized by keepers of clean catacomb beds. |


## Placement and save contract

Cold generation only: newly explored eligible areas, including new exploration in an existing save, gain the regional patches. Cached populated zones are not rewritten. Each permanent patch record prevents unloading or destroying crops from minting another crop/seed supply. Surface patches are sparse and exclude authored or receipt-owned sites. Cave species use ordinary reachable stair columns and successive depths, not arbitrary inaccessible depth IDs. Catalogue presence and scheduled candidate zones alone are not evidence of physical coverage.

## Verification discipline

Follow CLAUDE.md: failing tests before production, source corrections above, paired counters, dedicated adversarial tests and in-phase review. Native Unity is authoritative for rendering, actual keyboard actions and native seed topology. Reference-runner checks alone cannot prove pixels, real input or scene behavior. Keep new C# metas explicit; append Objects.json surgically with parsed before/after proof. Preserve unrelated logs and prior untracked work; stage only owned files.

## Implementation and how to find it

- **Existing plants:** ask Sella about growing beds or Orrit about repair supplies in Morrowfast. Their ordinary Start choices are available without a quest or payment. Leave town west; the next field (`Overworld.2.6.0`) contains the allotment notice, ripe knotflax/hearthbulb/seamleaf, and their younger plants. Sella's existing finite seed stock provides those three seeds; Orrit's existing stock supplies repair materials. The six southern town beds remain available for planting.
- **New surface plants:** `BiomeCropPlan` allocates up to 50 small patches:10 each in Spread/Sodden/Beating/Grovelands and5 each in Overwrit/Stump. A patch has two plants of one species, one ripe and one seed/sprout. Five species cycle through the eligible ranked addresses; no whole-biome garden repeats in every chunk. Look for actual pale/grown plant silhouettes and prepared-soil marks; Examine explains the harvest's use. Exact locations vary with the world seed. No quest or map pin unlocks them.
- **Cave plants:**16 eligible ordinary columns are selected, with the five species cycling across depths1–5. Actual incoming stairs and reachable ground are required. No cave mouth is manufactured by this feature, and special/lair/sinkhole columns are excluded, including saved lair claims whose POI has since changed.
- **Harvest and grow:** Harvest the ripe plant, then pick up the physical yield and returned seed. Plant on an empty cultivated bed; use Conjure Rain and spend active-area turns. Each stage needs20–40 moist rounds (40–80 for seed→ripe). Dry plants pause; longer crops need more than one watering. Ripe crops stand until harvested. No offscreen progression, seasons or automatic agriculture was added.
- **Prepare:** one carried marlroot clod becomes one FireClay; one sealbark slat becomes one SalvagedTimber. These are the exact materials accepted by existing repairs, not a new broad substitution rule. A successful inventory Prepare closes the menu and costs one action like Cook; refusal keeps both the item and the menu and costs no action. Factory callbacks, changed owners, capacity, duplicate identities, replay, equipped/carried aliases and outer action failure are guarded.
- **Models:** an independent `BiomeCrops3D` pack provides 280 native meshes/prefabs:35×(three stages×wet/dry + seed + harvested item). The source kit is editable/rebuildable, shares the existing glade palette, and borrows no copied enemy or crop models. Existing37 repair/cultivation models remain unchanged. Growth switches real meshes; this is not a new skeletal animation set.
- **Underground rendering:** actual cached ordinary caves now enter the native renderer with existing depth-floor/wall models. A detached same-ID zone or naked address does not gain authority. Current crop Parts/terrain/ownership determine all appearances; transplanting a crop to another biome keeps its art.

The placement record is saved and prevents depletion from resetting on unload, even after both plants/bed markers are removed. It is explicitly metadata, so it cannot appear as an interactable pile or hijack the bare-soil target. Exact existing terrain remains in place; installation adds soil/Plantable state and validates all output owners before publishing. Optional placement can refuse unsafe terrain instead of deleting occupants.

## In-phase findings and corrections

| Severity | Finding | Resolution |
|---|---|---|
| 🟡 fixed | Ordinary cave graphs could not use the native crop renderer. | Native RED3/3 before the bounded receiving-manager cave admission and depth-floor/wall aliases; all 37 art/cave tests subsequently passed. |
| 🟡 fixed | A later crop factory callback could invalidate an earlier staged crop; missing tags/zero outputs were accepted. | Shared full-packet current-definition validation; four adversarial REDs fixed. |
| 🟡 fixed | Removing a current map POI could reclassify a saved lair column. | Read-only saved-lair claim exclusion; RED/counter pair. |
| 🟡 fixed | Invisible persistence owner appeared in tile/pile actions. | Explicit metadata tag filtered consistently by world interaction, retaining ordinary unrendered-object behavior; standing/depleted REDs plus 41 previous query tests. |
| 🟡 fixed | Handling defaults put shadefan/gripfrond on Hand despite their intended armor slots. | Explicit Head/Handwear UsesSlots in only those two new blueprints; actual equipment/glare/armor commands tested. |
| 🟡 fixed | Design notes leaked into plant Examine prose; harmful raw sap omitted self-Apply warnings. | Replaced all 35 flavor suffixes with in-world prose and warned for both harmful plants in seed/crop/yield/catalogue text. |
| 🟡 fixed | Mutable Part dispatch retried a refused preparation using a newly substituted recipe. | One-attempt event latch; adversarial RED→GREEN. |
| 🟡 fixed | Outer inventory rollback could merge an invalid carried/equipped item into itself. | Reject contradictory locations before taking the snapshot; invalid alias counter passes without doubling the input. |
| 🟡 fixed | New Prepare did not join the paid inventory action list. | Native success/refusal RED pair; added only this command to the existing one-action signal. |
| 🔵 fixed | &o/&O were not supported glyph colors. | Valid palette IDs and malformed-color counter. |
| 🧪 corrected evidence | Seven native placement mutation tests did not invoke their factory probes. | Explicit test-Part registration and invocation assertions; the reference runner had merged assemblies and hid this fixture omission. Original failed native receipt is retained. |
| 🧪 corrected old pins | Two older planting expectations contradicted behavior already shipped in38252ce7e. | Pure committed-source baseline reproduced both; correct no-payment/backlink-repair counters and earlier refusal reason, without changing planting code. |

## Q1–Q4 review

**Q1 symmetry:** explicit seed→crop→physical harvest→returned seed uses the established crop transaction. Plantable add/remove notifications pair; failed additions roll back only owned changes. Prepare's output merge/input payment share the outer receipt and undo before the outer action snapshot. Equip utilities are also unequipped in tests. Save/load retains depleted owners rather than recreating them.

**Q2 consistency:** one immutable catalogue provides species IDs, exact blueprints, model stems and use instructions to placement and art. Every new seed requires cultivated soil, every new ripe crop waits for Harvest, mutable tools remain unstacked, and no new crop silently changes legacy automatic crops. Existing blueprints are preserved; old three species are not counted among the 35 new ones. Address aliases do not invent extra regions.

**Q3 counters:** missing/invalid catalogue data, bad links/palette, dry/ripe/premature crops, stale owners, exact final capacity, action veto/throw, duplicate IDs, reentry, factory mutation, absent stairs, occupied/reserved/barren/liquid ground, cached reinstall, saved depletion and wrong-manager/render authority all have paired or dedicated adversarial coverage. Real-use tests exercise every new species, not just the existence of its Parts.

**Q4 documentation:** actual uses are finite or conserved as stated. Wound poison cure does not remove gas poison; liquid cups do not drink/purify/irrigate; lights work equipped/loose rather than stowed; fire candles need nearby fire and finite fuel; harmful gases affect allies. No permanent stat boosts. Direct wax Stoneskin lasts 4/6 turns and follows existing additive stacking; binderroot brewing retains the existing 30-turn recipe. This is a balance limit to playtest, not a non-stacking claim. The crop roster is original content rather than a claim of exact Qud implementation parity.

## Verification status and limits

The source/reference RED→GREEN evidence, first native run, native import receipt and seven reviewed model sheets live under [Verification/BiomeCrops](Verification/BiomeCrops). Initial native run 207/214: the seven failures were unregistered test probes; all 95 then-current content/use cases and all 37 model/cave cases passed. The broader native run passed 1928/1930. Its two older planting expectation failures were reproduced exactly against a pure `38252ce7e` source archive (baseline 9/11): a missing carried backlink is now refused, and barren ground is rejected earlier with `source_changed`. Tests were corrected to assert refusal/no payment and successful planting once the backlink is repaired; no SeedPart production behavior changed. Final native acceptance passed **1930/1930** (zero failures/skips, 186.85 seconds; 80 selected fixtures, including 218 new fixture cases and two new paid-action cases). This is a native Unity regression sweep, not the entire repository suite. The three native world censuses each found all 35 species along actual connected routes. Native live results and bounds follow below.

Cold-generation content reaches fresh worlds and newly explored eligible areas in old saves. **Previously cached areas are not migrated, and no new35-species seed merchant is shipped.** A completely explored old world may therefore require a fresh world to find the new species. Existing three-species merchant stocks follow existing finite-stock/restock rules, not instant saved-shelf replacement.

Three sampled world seeds are bounded coverage evidence, not a proof for every seed, future catalogue reorder or modded terrain. Safe placement may refuse. The roster order is part of version1 allocation policy; expanding/reordering it needs an explicit allocation-version decision. Screenshots and native input prove only the inspected finite route, not universal visual quality, long-term farming economy, combat balance, standalone performance or unassisted player discovery.


## Native acceptance and remaining playtest boundary

Surface receipt `72856a61409344b797a54033b6a81700` passed eight checks through actual harvest, pickup, Prepare to FireClay, returned-seed planting, learning rain and watering. The plant remained intact and progressed eight paid moist rounds. The ordinary player was attacked by a marlback scrabbler (9, 21 and 7 damage) and the route stopped at 3 HP. Sprout, full maturity, repeat harvest and that route's F5/F6 were not reached; lifecycle/save tests provide their automated evidence. This is a documented exposed-farming playtest limit, not a reason to remove enemies or manufacture growth.

Independent cave receipt `1c383cb0539e4c089cf2df1361826b9e` passed 5/5 with zero errors. A real connected depth 5 Lampvein source was harvested and picked up through keys; the autoequipped fan was stowed through the actual equipment panel and dropped to increase actual lighting. Source coordinates and the one explicit travel shortcut are recorded. The surface stop screenshot and cave Examine/dropped-light screenshots were visually inspected.

**Can verify:** actual input/actions for the inspected finite route; all 35 native lifecycle/use tests; three native sampled worlds with physical accessible crop coverage; current-owner art/stage selection, transactional payment and persistence. **Cannot verify:** natural player discovery, full surface Play maturation in this run, all-seed coverage, long-term balance, standalone performance or all-biome visual quality in Play. See [raw receipts and exact limits](Verification/BiomeCrops/README.md).

## Files changed and implementation log

- Gameplay: new `BiomeCropCatalog`, regional JSON catalogue, 105 seed/crop/yield blueprints and `BotanicalProcessingPart`; one narrow invalid-owner prevalidation and the existing paid inventory-action list.
- Discovery: new `BiomeCropPlan`, `BiomeCropPlacement` and saved `BiomeCropPatchPart`; receiving world generation/unload hook, saved-lair exclusion and world-metadata query filtering. Existing Sella/Orrit conversations give ordinary crop directions.
- Art: `ArtSource/BiomeCrops3D`, native library/source/recipes/importer and 280 mesh/prefab pairs; existing wilderness/village render dispatch and bounded current-manager cave admission. Old 37 model forms preserved.
- Verification: 12 new EditMode fixtures (218 cases), two additional paid-action UI cases, two corrected old planting expectations, native crop/cave menu routes, source tests and receipts.
- Sequence: source/lore/API sweep and roster first; meaningful RED gates; minimal catalogue/processing/placement/art implementations; dedicated adversarial and Q1–Q4 review; native import, broader regression and finite keyboard routes. Final discovery/doc audit found no additional significant code issue; corrected the two roster slot-field descriptions to `Equippable.UsesSlots`.
