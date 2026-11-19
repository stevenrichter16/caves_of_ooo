# MACHINE V1 DOSSIER: Culture vat (roster item 5)

Status: design only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. All names, numbers and text below are drafts for the implementing engineer and the owner; every authored string needs the blind voice gate (lore R10/L17).

Evidence tags: **[V]** I read the cited line in this pass. **[R]** taken from a prior report (`lore.md`, `settlement-*.md`, `arch.md`, `ARCHITECTURE-SPEC.md`) and not re-read. **[I]** inference or design. **[U]** unverified; the named RED test settles it.

Inputs read: `design/ARCHITECTURE-SPEC.md` (all), `Docs/MACHINE-ROSTER-VISION.md`, `Docs/MACHINE-SYSTEMS-DESIGN.md`, `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `lore.md` (sections 0-4 M5, 5-8, 10-11), the 24 per-settlement culture-vat rows, `home-design/proposal-raised.md:377` and `proposal-portable.md:299`, plus the code named below.

---

## 0. Verdict (read this first)

1. **Classification: `wave3-needs-new-system`.** Nothing about the vat is a "machine in every town": of the 24 settlement dossiers that rate it, 21 are "poor" or "forbidden" and only Lampwell and Spivenor are "strong by canon, blocked in practice" with Olderdeep "plausible" [R: rows quoted in section 9]. Its only canon homes are catacomb/sinkhole villages, which the spec's generic post-builder excludes (spec E12) and whose pipeline has no trader and no operator by design [V `StrandedSettlementBuilder.cs:33-35`].
2. **The pitch has no unmet need in this world today.** Four of the five pitched outputs already ship as farm yields (section 2.1), reagents are sold renewably by every village trader [V `TradeStockBuilder.cs:20-50`], no settlement concern or repair recipe asks for a cultured item (section 2.2), and there is **no specimen, capture, strain or provenance model anywhere in `Assets/Scripts`** (case-insensitive grep for `specimen` returns nothing in gameplay code). The roster's own gate (`MACHINE-SYSTEMS-DESIGN.md:89`: "finite culture identity/feed/growth/conditions and competing uses remain open") is accurate.
3. **What can honestly be built is a small kernel, not an installation:** a **tended colony jar**, a carried hand-light vessel that holds a finite living colony, can be tended under two declared conditions, and whose timed result is a pure function of the world clock. It has one verified real consumer (held light, `LightMap.cs:109-160`) and one optional second consumer that needs a small additive edit. It is the smallest version that satisfies "finite culture identity, feed, bounded growth, conditions, two competing uses, persistence" without duplicating farming.
4. **The installed vat (a fixture in a hatchery or base, several slots, thermal conditions, receivers) is V2** and needs spec slices S16, S17, S19 and the thermal exchanger. It must not be promised by V1.
5. **Recommendation to the owner:** do not schedule the vat in the first machine wave. If an early proof of "living stock plus elapsed time" is wanted, the kernel slices CV1-CV3 below are small and use only the spec's first slices (S1, S3, S5). The Lampwell source (CV5) is the wave3 part and needs the parking lifted, the catacomb attach point, a new operator and a canon sign-off.

---

## 1. Verified ground truth, corrections to prior reports

| # | Fact | Evidence | Consequence |
|---|---|---|---|
| F1 | `CLAUDE.md` parks the vat: "capstan/cultures parked"; execution plan M5 is "Planned". | `CLAUDE.md` header; `Docs/MACHINE-SYSTEMS-EXECUTION-PLAN.md:3,44` [V] | Any runtime change needs the owner to lift parking and the "no new runtime changes" hold. |
| F2 | The process engine cannot express growth. `Valid()` rejects a row whose output blueprint equals any ingredient blueprint. | `MaterialProcessRegistry.cs:134` [V] | "Lichen in, more lichen out" is impossible as a `MaterialProcessRegistry` row by design. The living stock must be saved state, and outputs must be different blueprints. The vat cannot reuse the six-row registry. |
| F3 | The elapsed-time contracts that exist: `CropTime` (world ticks, 10 per authored unit, wet-only, lazy reconcile, no away simulation) and `KitchenBatchPart` (zone-pinned commission, escrow, due tick, 120 ticks, fee 2, blocked reasons). | `CropTime.cs:6-24,47-91` [V]; `KitchenBatchPart.cs:15-34` [V] | Policy for the vat is already decided: lazy, bounded, nothing created by time alone. The kitchen cannot be moved (stores `ZoneID`, station cell, owner IDs), so it is not a base for a portable culture. |
| F4 | Only the `Grass` blueprint carries the `Plantable` tag, but cold-generation crop patches are installed on other ground by `BiomeCropPlacement` (priority 4310). | grep over `Objects.json` (Plantable at `:8357`) [V]; `BiomeCropPlacement.cs:9-70` [V] | I do **not** build the vat's purpose on "farming cannot work off grass": authored patches already exist in every biome including Cave. |
| F5 | The one shipped finite living colony the player can take: the Olderdeep descent `BeetleJar`. `RecoverableLampPart` removes it from its seat and turns the same entity into a hand-held item (`EquippablePart{Slot="Hand"}`, `HandlingPart{Weight=2}`) keeping its `LightSourcePart`; the packet text says it "leav[es] this resting seat darker". Fresh worlds only (exploration version gate) [R for version]. | `SecondExplorationObjects.cs:7-33` [V]; `SecondExplorationSites.cs:91-97` [V] | This is the legitimate, disclosed-choice wild find the pitch asks for ("wild specimens become meaningful finds"). It is also a single jar per world. |
| F6 | Held light is real: equipped items with an enabled `LightSourcePart` project light at the wielder's cell. A torch, by contrast, can only be lit "beside a burning fire" and its controlled flame is affected by Wet and Frozen. | `LightMap.cs:109-160` [V]; `TorchLightPart.cs:38-41,111` [V] | The jar's honest edge over a torch: no fuel, no ignition source, but it costs a Hand slot. That is the one verified real consumer. |
| F7 | Two luminous liquids are fully defined and wired but unobtainable in play: `lantern-beetle-ichor` (`LightRadius 6`) and `lumen-slime` (`DV -3`); the coat attaches a `LightSourcePart` to the coated creature. They appear only in `LiquidSpellTestBench.cs:173,187`. | `LiquidDefinitions/lantern-beetle-ichor.json`, `lumen-slime.json` [V]; `LiquidDefinition.cs:108-115` [V]; grep for other uses [V] | A shipped, unsourced consumer chain. A culture output could feed it. |
| F8 | Item to coating is a hard-coded switch (`FireMoss` Kindle, `FrostLichen` Freeze, `PitchpodResin` Coat pitch, `Honeycomb` Coat honey ...). A new coat source means editing it. | `MaterialFieldActions.cs:162-179` [V] | Same "edit every interacting system" smell the spec fixes elsewhere. See capability fix CV4. |
| F9 | No `BeetleMaster` blueprint exists; the role appears only in a plaque text. | grep of `Objects.json` for `BeetleMaster` returns nothing [V]; plaque at `Objects.json:36916` [V] | Operator NPC is new content. |
| F10 | Catacomb villages have no `TraderPart` and no stock table: "an economy phase, not this one". | `StrandedSettlementBuilder.cs:33-35` [V] | A starter source at Lampwell needs a deliberate, owner-approved departure. |
| F11 | `Tender`: catacomb folk are barter-first; Concord drams; Choir none. | `Tender.cs:9-40` [V] | Starter stock is acquired by barter, not by a dram sink. |
| F12 | `ChoirCutting` is a takeable Tier-3 item whose text ties it to "the sleeping Root" (ending material). It comes from the blood offering. | `Objects.json:39190` [V]; `ChoirOffering.cs:8-14` [V] | Must be refused as stock or feed by allow-list. |
| F13 | `HearthPatchPart`: any player-sourced damage to a patch tile sets CatacombFolk reputation to war once for the whole village. | `HearthPatchPart.cs:43-67` [V] | The vat can never accept, touch or draw from patch cells. |
| F14 | Food already has a conversion: `Mushroom` (Healing 1d4, value 3, weight 1) cooks to `RoastedMushroom` (3d4, value 8); no hunger system exists (grep for Hunger/Satiety/Starv in `Assets/Scripts` finds nothing). | `Objects.json:11981,43849` [V] | A "larva paste" food output would be padding. Rejected (section 3.7). |
| F15 | Spec correction: `ForbiddenFactions` in `Installations.json` is global (example lists `RotChoir` and `CatacombFolk`), and E06 rejects any row at a forbidden faction's place. Canon is per kind: catacomb folk forbid open flame but are the native culture of this machine. | `ARCHITECTURE-SPEC.md` C3 JSON and E06, E12 [V] | The spec needs **kind-scoped** faction rules before any vat row can exist (section 6). |
| F16 | Grep correction to `lore.md` M5: "Needs new blueprints: Beetle-Master, Patch-Tender, Slime-Milker". Confirmed absent. `BeetleJar`'s text promises a Warden alarm that no Part implements. | [V] F9; `settlement-lampwell.md:69` [R] | V1 must not cite or imply the alarm. |

---

## 2. Purpose test (principle 1: establish the opportunity before the apparatus)

### 2.1 The pitch outputs against shipped content

| Pitched output (`MACHINE-ROSTER-VISION.md:48-49`) | Already shipped as | Evidence |
|---|---|---|
| sealing resin | `PitchpodResin` (Pitchpod, Spread): coat verb, ink desk, conservation case, Posy concern | `BiomeCrops.json:19`; `MaterialFieldActions.cs:176`; `Posy.json` |
| luminous tissue | four Light crops: Peatlantern (`:139`), Choirwick (`:229`), Cloudwick (`:424`), Lampvein (`:454`) | `BiomeCrops.json` [V] |
| medicinal cultures | Cure/Healing crops: Knitmoss (`:244`), Claspbean, Sumpsieve, Brinebutton ... | `BiomeCrops.json` [V] |
| conductive fibers | Prismreed pith (charged coating) (`:394`); and canon-hostile: conductive mycelium is the Choir's nervous system and the Bloom's vector | `BiomeCrops.json`; `lore.md:76` [R] |
| filtering membranes | nothing, **and no consumer** | none found |

So a vat that "grows luminous or medicinal material" duplicates farming. The vat's honest territory is what a seed cannot be: living stock that is not a seed (a colony), kept in a vessel, tended under chosen conditions.

### 2.2 Is there a demand today?

- Concerns request `LampOil` x2, `WardOil`, `InkVial`, `PitchpodResin`, `FireClay` ... none requests a cultured item [V grep of `Concerns/*.json` blueprint keys]. Repair recipes consume timber, clay, cord, spikes, bindings [V `Tier1Repairs.json`, `PlaceSites.json`].
- Reagent supply is renewable by design: "Villager traders are the renewable source" for `FireMoss`, `FrostLichen`, `MendleafSprig`, `BlastcapSpore`, `Mushroom`, `LampOil` [V `TradeStockBuilder.cs:20-50`]. A vat that multiplies reagents manufactures scarcity that was removed on purpose.
- The one real, verified gap the kernel fills is small: a **fuel-free, ignition-free hand light** (F6). Light is not scarce (torches, `LampOil`, four light crops). So V1's gameplay purpose is **modest**, and its justification is: (a) canonical Lampwell identity ("the headline trade good", `settlement-lampwell.md:135` [R]); (b) it is the cleanest first consumer of a living-stock Part plus a lazily evaluated timed state, both reused by every later culture, still and exchanger job; (c) it turns the single shipped colony find into a meaningful decision.
- **The roster gate "two competing uses for one finite culture" is only satisfiable with a second real consumer.** Larva paste (canon) is not one (F14). The ichor vial (F7, F8) is, with one small additive edit. If the owner rejects that edit, the vat stays a kernel and **does not pass its roster gate**; say so in acceptance.

---

## 3. V1: the tended colony jar

### 3.1 Name, voice, register (all drafts, voice gate pending)

- Item: **colony jar** (canon trade good: "A tended starter colony in a glass jar", `catacomb_village_design.md:316-317`; lantern-beetle). Verb: **tend**. Never "vat" in catacomb speech; never "culture" in catacomb speech (lore M5 names/voice). The surface/Concord word "cultivar tray" is reserved for the V2 installed form.
- Keeper line (lore sample, `lore.md:138`): "Two dims down, past the jars. We don't use the colony, guest. We tend it."
- Daughter jar: **daughter jar** (a light only; it will not breed). Second output: **beetle-ichor vial** (display name follows the shipped liquid id `lantern-beetle ichor`; the catacomb trade word for painted light is "slime-paint"; the voice owner picks).
- Forbidden in any new string: vat, charge, mana, battery, power, the dream, Urqu intent, "alarm" (F16).

### 3.2 Parts and data (additive saves only)

- **`CultureStockPart`** (new, namespace `CavesOfOoo.Core`, unique class name; spec S1 hygiene: no `ZoneID`, no station or position field):
  - `int Cycles` tendings left (starter default 3, recovered descent jar default 2, daughter 0) [tune].
  - `string Mode` "", "Careful", "Brisk".
  - `int StartTick`, `int DueTick`.
  - `string Source` "descent" | "barter" | "daughter" (reader text only).
- The jar carries existing Parts only otherwise: `LightSourcePart`, `EquippablePart{Slot="Hand"}`, `HandlingPart`, `Examinable`, plus `IDescribeState` (S5) on `CultureStockPart`. No `StackerPart` (unique, never merges). A new Item blueprint **`LanternBeetleJar`** carries these Parts (barter starter and daughter); the Olderdeep recovered `BeetleJar` entity gets `CultureStockPart` added by one line at the end of a successful `RecoverLamp` (F5). Jars recovered before the change stay plain lamps (stated limit, RED-pinned).
- **Rows**: two rows in a small **code table** inside the culture adapter (no JSON loader; spec R1 two-consumer rule; trigger for extraction is a third timed row). Rows are validated by a unit test, not by `MaterialProcessRegistry` (F2).

### 3.3 Player verbs, inputs, outputs, loss, cost, time

| Row | Verb text (draft) | Feed (units) | Time (world ticks) | Cycle cost | Output | Declared loss |
|---|---|---|---|---|---|---|
| `tend-careful` | "tend the colony (careful)" | 3 `Mushroom` | 1200 (one day, `WorldClock` day = 1200 [R `home.md:141`]) | 1 | 1 `LanternBeetleJar` (Cycles 0) | "The rest of the mushroom is spent on the medium and is not recoverable." |
| `tend-brisk` | "tend the colony (brisk)" | 2 `Mushroom` | 400 | 1 | 2 `BeetleIchorVial` | same text, 2 spent |

Feed identity (Mushroom: sold renewably, `MushroomRing` harvest 2-4, value 3 [V `Objects.json:11981`, `:30933`]) and the lore link "beetles fed on cave fungus" are **[I] design choices for the lore owner**; the row table reads the feed from the row, never from code. Tuning numbers are **[tune]** and pinned by the economic invariants in 3.6, not by taste.

- **Commit:** feed is removed from the actor's pack inside one `InventoryTransaction` (same shape as M1d straining, `OilStrainingPart` [R]); `Cycles` decrements at tend start (so a lost jar loses the cycle, never gains one); `Mode`, `StartTick = WorldClock.CurrentTick`, `DueTick = StartTick + duration` (computed in `long` and clamped, as `CropTime.cs:72-76` does).
- **Collect:** allowed iff `WorldClock.CurrentTick >= DueTick`; creates exactly the previewed outputs through the actor's pack with the existing capacity rules; clears `Mode`/ticks. If the pack cannot take them, **nothing is created and nothing is lost** (state stays Ready).
- **Covered while tending:** `LightSourcePart.Enabled = false` while Tending or Ready, restored on collect (saved switch state, `LightSourcePart.cs:26-27` [V]; "covered while stowed" idiom of the light crops, `BiomeCrops.json` Peatlantern text [V]). This is the competing use of the **same finite starter**: lamp now, or tend for a daughter or ichor.
- **Action cost:** tend and collect are each one action (declared `OneAction`, spec S3, in both menus). No fee, no drams, no sink. No permission gate: it is the player's own jar.
- **Time:** the jar needs no reconciler, no zone, no per-turn work. Outputs are created only by the player's collect verb.

### 3.4 Preview and reader (S3 disabled rows, S5 reader)

Row label shows cost before the click; the disabled reason is a one-sentence token (R5):

```
tend the colony (careful)  uses 3 mushroom, ready in about a day, gives 1 daughter jar, 2 tendings left after
tend the colony (brisk)    uses 2 mushroom, ready in about a third of a day, gives 2 beetle-ichor vials, 2 tendings left after
Disabled codes: no-feed ("needs 3 mushroom; you carry 1"), spent ("this colony has no tendings left"),
  tending ("tending; ready in about N ticks"), not-held ("carry the jar to tend it"), blocked ("no room for the result; nothing is lost")
```

Reader (`x`, F1, full document, both via S5): `Colony jar. Tendings left: 2. Tending (careful): ready in about 340 ticks. Kept covered; no light until collected.` / `Ready.` / `Spent: it still lights.` Daughter: `Daughter jar. A light only; it will not breed.`

### 3.5 Failed, blocked, interrupted, saved

| State | Entry | Player sees | Exit |
|---|---|---|---|
| Idle | stock present, `Mode == ""` | tend rows | tend |
| Tending | after commit, clock < DueTick | `tending` reason, light off | clock reaches DueTick |
| Ready | clock >= DueTick | `Collect` row | collect |
| Blocked | collect with no pack room | `blocked` reason; **state stays Ready** | free pack space |
| Spent | `Cycles == 0` and Idle | `spent`; the jar still lights | none (never destroyed) |
| Invalid stamp | `StartTick > clock` or malformed | refuses and records `invalid-stamp`; **never completes early** | leave alone |
| Reload mid-tending | public fields round-trip (reflection save, `FormatVersion` untouched) | identical due tick | as above |

Interruption: there is no cancel and nothing to cancel: a jar left alone stays safely stopped, never decays (no neglect clock, no compulsory maintenance, `MACHINE-SYSTEMS-DESIGN.md:136`). Sale or gift **while Tending or Ready** is vetoed (`CanBeTraded`, rental precedent) so a job cannot be duplicated or laundered; an idle jar trades normally.

### 3.6 Finite-accounting invariants (each is a test)

- I1 conservation: feed removed == row feed; outputs created == row outputs; `Cycles` down exactly 1 per tending.
- I2 bounded generation: a daughter has no `CultureStockPart` Cycles to spend, so total jars derived from one starter <= its `Cycles` regardless of feed; stock only grows by barter or by finding another colony.
- I3 no sell loop: for every row, value of feed (traders' price function) >= value of outputs at the sell function, and `buy starter + max tendings + sell all outputs` never nets drams. Values are set by this test, not by feel.
- I4 no duplicate on reload, double-submit, give/drop-pickup or save-copy.
- I5 allow-list: only the row's declared feed blueprint is accepted; only `CultureStockPart` jars with `Cycles > 0` can be tended.

### 3.7 Rejected on purpose

- **Larva paste (canon staple) as an output:** no hunger system, Mushroom and RoastedMushroom already heal, so it would be padding (F14). Revisit only if a hunger or provisioning system exists.
- **Free multiplication of reagents (FrostLichen, FireMoss, BlastcapSpore):** traders sell them renewably (F2 also forbids the row shape).
- **Environmental conditions (warmth, damp, light):** can only be read at action time (no away simulation); the thermal exchanger does not exist. V1's "conditions" are the two declared modes. This is stated honestly: modes are **declared conditions**, not environment.
- **Any Choir, Bloom, patch, plume, doll, Gin Frog, ancestral-bone or mother-strain input** (allow-list excludes by construction).

---

## 4. Where the player finds it, and the two locations

1. **Existing: Olderdeep descent jar** (fresh worlds). Unhooking it is the disclosed choice (the seat goes dark). With CV3 the recovered jar becomes tendable. Single instance per world; it satisfies "wild find", not "more than one location".
2. **New: Lampwell Beetle-Master** (`Overworld.12.3.2`, the Concord-allied Patch-Bright hub, `Lore/History/02_Geography.md:76` [R]) sells starter jars **by barter** at a finite, world-seeded stock (spec EC1/EC6 conventions; `Tender` barter-first). This is the second location and the first catacomb attach point. It requires: new `BeetleMaster` blueprint and conversation (voice gate), a deliberate `TraderPart` in a village whose plan said "No TraderPart" (F10), and a sinkhole post-builder (spec E12 lifted for this one row).
3. The **installed hatchery** (V2) at Lampwell and, later, a base is not V1.

Not placed: Spivenor (Spore-Wedded, Choir-integrated; consent and permission pattern absent), Olderdeep `.2` (sacred, "no machine"), every surface town (`poor`), Tent-Right places (do not culture), Choir places and the Quiet's Door (forbidden/refuse), Curation/Recension places (poor; Bloom-watch, bodies).

## 5. Alternatives the player keeps

1. **Torches and `LampOil`** (shipped, M1d0/M1d), **four light crops**, or simply **buy a ready jar** from the Beetle-Master: the machine is never the only source of light.
2. **Leave the jar alone:** an unhooked or bought jar lights as is; tending is optional; nothing decays or demands a visit; the descent seat can also simply be left lit.
3. **Do not take the colony at all:** the descent jar's seat stays lit; Lampwell trade is optional.
4. **Spend cycles differently:** light-for-me (daughter), marking tool (ichor), or keep the starter as an equipped lamp and tend nothing. Three real branches, one of which is "none".
5. Ordinary convenience (repair, healing, carrying, descriptions) is untouched; nothing is degraded to create demand (R7).

## 6. Consequences outside its own menu

1. **Hand slot and sight:** the jar occupies a Hand and lights the wielder's cell (`LightMap.cs:109-160` [V]); a torch needs a fire to light and its flame is affected by Wet/Frozen (`TorchLightPart.cs:38-41,111` [V]). Carrying a jar changes what a fight in the dark looks like and what your off-hand can do.
2. **Ichor on a creature:** the coat attaches a `LightSourcePart` to the coated creature and later removes it (`LiquidDefinition.cs:108-115`, `LiquidCoveredEffect` wiring [V]); a daubed creature stays visible in the dark. Modest and [U] in practical value; actual play must observe it and the removal.
3. **Economy and faction:** starter acquisition by barter ties to catacomb reputation; the existing patch war rule (F13) closes the village to a player who offends; killing the keeper uses existing consequence rules.
4. **Light competition at Lampwell:** carrying a flameless light where open flame is an offence in canon (canon only; not implemented, state it as a canon rationale, not a mechanic).

## 7. Canon: names, operators, landmines

Operators: **Beetle-Master** (new role; canon `catacomb_village_design.md:67-74` [R]; plaque text proves the role). Not used in V1: Patch-Tender (patch is never feed), Slime-Milker, Drosera-Keeper. Shipped roles that could sell or explain: none are a natural fit; do not give the two existing Lampwell residents ad hoc trade roles (`settlement-lampwell.md:160` R9).

Landmines (ids from `lore.md` section 7) and how V1 avoids each:

| Id | Landmine | V1 control |
|---|---|---|
| L1 | Doll mycelium (#5), Gin Frogs (#8), the Root's dream-light (#2): must not be identified, cultured or advertised | allow-list rows; light described as lamp light, never "the dream"; test bans the words in new strings |
| L2 | No heat/light/power from Urqu, Sari-Pyrite, Founding Plume, hearth patch | patch and plume are scenery, never inputs; no claim of intent anywhere (R1) |
| L4 | Choir crimes; Choir cuttings are not stock | `ChoirCutting` explicitly refused (F12) |
| L8 | Patch mother-strain never sold or used; plaques, ancestral bone | allow-list; stock sold is a daughter-class starter only |
| L10 | Mana-bar behaviour: no storing charms or making flowers permanent | no charge/charm interaction; jar light is not a stored charm |
| L16 | Technology nouns | none in strings; lint test |
| L17 | No blind-gate record for shipped machine strings | every string here is a draft; gate before RED merge |
| L19 | The shipped deep-magic liquids must not be producible | rows output only the two blueprints; a test scans every row's outputs against the list. `lantern-beetle-ichor` and `lumen-slime` are **not** on the L19 list but are test-bench-only definitions: confirm with the lore owner **[U]** |
| R9 | Standing commitments need a spoken end | V1 creates none (an item, not an arrangement); V2 hatchery service would need one |

Do not use `Docs/Lore/`. No canon fact here comes from it.

## 8. Fit with the architecture spec, and what the spec lacks

Uses: S1 (golden entry + hygiene for the new Part), S3 (declared `OneAction`, disabled rows), S5 (`IDescribeState`). Does not use: S2/S6-S8 for the jar (carried; no retention, not placed by the generic builder), S4 stations (no station), S10 terms, S12-S14, S16 jobs.

**What the spec lacks for the vat** (each is a decision or a new slice, not an assumption):

1. **A living-stock identity contract.** `CultureStockPart` is vat-specific; a second consumer (assay samples, later cultures) would generalise it. Spec C10 only lists forbidden vocabulary.
2. **A statement that a carried item may hold a lazily evaluated job.** Policy C9 ("lazy bounded reconciliation ... time never creates another job") supports it; the spec describes only zone-resident `WorkJobPart`. V1 needs no S15/S16; V2 does.
3. **Kind-scoped faction rules in the registry (F15).** `ForbiddenFactions` is global and E06/E12 block catacomb rows. The vat needs `Kind` on rows and per-kind allowed/forbidden factions, plus the catacomb attach point the spec lists under "Not now" with the trigger "a spec targets them". **This machine is that trigger.**
4. **A capability for field coating** to replace the closed `MaterialFieldActions` switch (F8): additive `FieldCoatPart { Verb, LiquidId, Amount }` consulted before the switch; the switch stays as the legacy path. Without it the second row costs a switch edit and a recorded debt.
5. **Allow-list protection** (`lore.md` section 10 `ProtectedSubject`/`LivingTended`). V1 gets safety from allow-list rows; a capability tag is the later generalisation.
6. **Row schema** for timed rows with counts and modes (spec C8.4 "row schema v2 not scheduled"). V1 uses two code rows; the kitchen would be the second consumer when S16 lands.
7. **Home bridge:** no zone ids or cell fields on the Part (S1 lint), so a base can store, carry or later install a jar; the installed hatchery refuses take-down while Tending (matches `home-design/proposal-raised.md:377`: "take-down refused while contents or a job exist").

## 9. Census and "every town"

The vat is **never** added by the generic post-builder. In `Installations.json` it contributes: Lampwell `Pending(owner="catacomb attach point + Beetle-Master")`; every other settlement is simply not a vat host and must not appear in the coverage ratchet as a defect (the ratchet tracks "some machine", not "every machine"). Culture-vat dossier rows (all `[R]`, `settlement-*.md`): Lampwell strong/blocked (`:135`); Spivenor strong/blocked (`:89`); Olderdeep plausible (`:166`); poor: Cinderhold `:217`, Cathedral `:129`, Drowned Ledger `:209`, First Tent `:209`, Gantry `:179`, Last Counter `:135`, Merchant camps `:150`, Morrowfast `:125`, Posy `:217`, Quiet's Door `:120`, Quillhold `:168`, Salt-Vault `:187`, Sill `:154`, Slip `:167`, Spread wayhouse `:123`, Stillleaf `:117`, Sumphold `:168`, Tally `:180`, Tine `:97`; forbidden by canon: Ginmere `:186`, Marrowstye for body/Bloom feed `:267`. (The Wellmeet vat row was not extracted.)

## 10. Slices (prefix CV; each RED-first; sized for the ~5 minute isolated cycle)

Prerequisites: owner lifts the parking and hold; spec S1, S3, S5 exist.

| Slice | Deliverable | Tier | Cycles |
|---|---|---|---|
| CV0 | Design gate, no code: owner decisions D1-D6 (section 12) and lore/voice sign-off of names and feed | gate | review |
| CV1 | `CultureStockPart`, `LanternBeetleJar` item, golden entry, hygiene, round-trip | wave1 | 1 |
| CV2 | Tend/collect verbs, two code rows, lazy clock, preview, disabled reasons, light-covered state, trade veto, diag | wave1 | 2 + 1 adversarial |
| CV3 | Acquisition adapter on `RecoverLamp` (Olderdeep jar) | wave1 | 1 |
| CV4 | `FieldCoatPart` capability (additive) + `BeetleIchorVial` Daub | wave2 | 1-2 |
| CV5 | Lampwell source: `BeetleMaster`, barter stock, catacomb attach, kind-scoped faction rule | wave3 | 3 + actual play |
| CV6 | Actual-play acceptance, fresh world and cached control | gate | play |

Honesty: CV1-CV3 alone give a usable kernel with **one** location; the roster gate "more than one location" is met only at CV5. CV4 is what makes "two competing uses" true; without it, CV2's brisk row must be omitted and acceptance must say the gate is not met.

## 11. RED-first test names (counter-checks marked C, adversarial surfaces marked A)

**Stock and save (CV1).** `CultureStockPart_RoundTripsPublicFieldsViaSaveGraph`; `OldSaveWithoutCultureStockLoadsAsPlainJar` (C); `CultureStockPart_HasNoZoneOrPositionFields`; `SavedPartContract_ListsCultureStockAsAddition`; `StarterHasCyclesAndDaughterHasNone` (C); `SpentJarStillLights`; `HandJarEquipsInHandAndProjectsLightButCarriedUnequippedDoesNot` (C, via `LightMap` pass 2); `ClassNameCultureStockPartIsUniqueProjectWide`.

**Tending (CV2).** `TendCarefulSpendsFeedExactlyAndSetsDueTick`; `TendBriskSpendsFeedExactlyAndSetsDueTick`; `TendRefusedWhenFeedShortAndNothingSpent` (disabled `no-feed`, C: same layout with enough feed succeeds); `TendRefusedWhenSpent`; `TendRefusedWhileTendingShowsRemainingTicks`; `TendRefusedWhenJarNotCarried`; `CollectOneTickBeforeDueRefusesAndChangesNothing` (C: at due succeeds); `CollectCarefulGivesExactlyOneDaughterJarWithZeroCycles`; `CollectBriskGivesExactlyTwoIchorVials`; `CycleDecrementsOnceAtStartNotAtCollect`; `CollectWithFullPackBlocksLosesNothingAndRetrySucceeds` (A: atomicity); `CollectTwiceCreatesOutputsOnce` (A: double-submit); `SaveReloadMidTendingKeepsDueTickAndCompletesOnce`; `LongAwayCompletesOnceNeverCreatesSecondJob`; `ClockBelowStartTickIsInvalidStampNotFreeCompletion` (A: overflow/long widening at near-int-max ticks); `LightDisabledWhileTendingAndRestoredOnCollect`; `TradeOrGiveVetoedWhileTendingOrReady` (C: idle jar trades); `DuplicateViaDropPickupOrSaveCopyImpossible` (A); `DeclaredOneActionRowsChargeOneTurnInBothMenus`; `DisabledRowsListReasonsAndSpendNothingAndKeepMenuOpen`; `ReaderShowsTendingReadySpentInQuickAndFullDocument`; `TwoJarsNeverMergeOrShareState` (A: stacking); `DiagEmittedForEveryGate` (`craft/CultureTend`, `CultureTendRefused{reason}`, `CultureCollect`, `CultureCollectBlocked`).

**Allow-list, canon, economy (CV2).** `RowsAcceptOnlyTheDeclaredFeedBlueprint`; `ChoirCuttingRefusedAsFeedAndAsStock`; `BeetleJarSceneryAndHearthPatchNeverAcceptedAsStock`; `NoRowOutputsAProtectedLiquidOrItem` (scans rows against the L19 list); `NewStringsContainNoForbiddenVocabulary` (vat, charge, mana, battery, dream, alarm, Urqu intent); `NoSellLoopProfitForEveryRow` (I3); `OneStarterBoundsTotalOutputsByItsCycles` (I2, with unlimited feed); `RowTableHasNoDuplicateIdsAndEveryOutputDiffersFromItsFeed`.

**Acquisition (CV3, CV5).** `RecoverLampAddsCultureStockToTheRecoveredJar`; `PreviouslyRecoveredJarStaysPlainLamp` (C); `NonRecoverableLampwellJarGetsNothing` (C); `BeetleMasterBartersAStarterAtBarterRate`; `StarterStockIsFiniteAndSeedDeterministic`; `HatedCatacombReputationRefusesTrade`; `KillingTheKeeperAppliesExistingPenalty`; `LampwellAttachPlacesOnceDeterministicallyAndCachedZoneUntouched`; `GenericPostBuilderNeverPlacesAHatcheryAnywhere` (S7 stays vat-free); `CensusDoesNotRequireAVatInEveryTown`; `KindScopedFactionRuleAllowsHatcheryAtCatacombAndStillForbidsForgeThere`.

**Daub (CV4).** `FieldCoatPartCoatsAcrossExistingTransactionAndRemovesOnExpiry`; `LegacySwitchBlueprintsBehaveIdenticallyWithoutFieldCoatPart` (oracle); `IchorDaubLightsTheCreatureAndRestoresItAfter`; `IchorVialRefusedOnNonCreatureOrDistantTarget` (C).

## 12. Owner decisions and risks

D1 lift parking and the runtime hold. D2 accept that the vat is **not** an every-town machine. D3 accept V1 as a carried jar with modest purpose, or defer. D4 accept the ichor Daub (CV4) as the second use, or keep the vat a kernel that does not meet its gate. D5 approve a trader in Lampwell and the Beetle-Master role (reverses "no TraderPart"). D6 the feed identity and the lore link. D7 economic constants (Cycles, feed counts, times) set by invariants I1-I5.

Risks: (1) thin purpose; (2) `MaterialFieldActions` closed switch (mitigated by CV4's capability); (3) first catacomb attach point is generation work with the usual cached-zone limits (first visit in old worlds only; no retrofit); (4) all text lacks a voice-gate record; (5) the Olderdeep source exists only in fresh worlds at or above the exploration version gate [R], and is one jar per world.

## 13. Deferred pitch behaviours and why

| Pitch behaviour | Deferred to | Reason |
|---|---|---|
| sealing resin, medicinal, luminous tissue as outputs | never as vat outputs | already farm yields (2.1) |
| conductive fibers | never | canon-hostile (Choir nerves, Bloom vector, `lore.md:76`) |
| filtering membranes | until a consumer exists | none exists |
| wild specimen capture, provenance, strains, rare properties | new system (assay samples, S16-era) | no capture or provenance exists (grep) |
| conditions from the environment (warmth, damp, fouling, Choir creep, Bloom) | V2 with exchanger and action-time hazard gates | no away simulation; no thermal source |
| machine visibly reflects contents | reader line and light on/off only; art later | graphics cancelled |
| installed, multi-slot hatchery; base tray | V2 with S16, S17, S19 | needs jobs, receivers, install verbs |
| several uses (paste, slime-paint, dew-ink, textile) | later, each needing a consumer | padding otherwise |
| Concord rental or export of colonies | later | rental precedent exists; no consumer today |
| Spivenor/Choir cultivation | later, with consent pattern | permission and Choir law |

## 14. Honesty bounds

Can verify now (read): all [V] items above. Cannot verify without play or tests: practical value of the ichor daub; whether `LiquidCoveredEffect` with a light-bearing liquid behaves on every creature type; feed and lore link; price relations; the exploration version gate for the Olderdeep jar; the Wellmeet vat row; that no existing class is named `CultureStockPart`. No Linux or Deck execution is implied. No art is in scope.
