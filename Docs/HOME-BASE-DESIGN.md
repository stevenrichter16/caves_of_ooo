# Home/base: design and architecture ("set-down places")

Status: design only. No project file, Unity, test, build or git command was changed or run. Written 2026-11-18 (UTC 2026-11-19 00:xx) against the on-disk working tree (HEAD plus 84 pre-existing tracked changes).

**November 28 direction.** The portable camp design below remains the foundation for placement and retention. The new proposal adds opt-in security for explicitly enrolled belongings at homes and standalone field installations. A certified site keeps its equipment and stock safe during hostile attempts. Actual routes determine whether a buyer reaches the loading point and gets the goods to their destination. [Secure field sites and collection routes](SECURE-FIELD-SITES-AND-COLLECTION.md)

Authority: canon is root `Lore/` only (`Docs/Lore/` unused). Governing user text: `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `Docs/MACHINE-SYSTEMS-DESIGN.md:11,136`, `Docs/MACHINE-ROSTER-VISION.md` (home/base hooks).

Evidence tags: **[V]** I read it in this pass at the cited `path:line`. **[P]** taken from the prior reports, the three proposals or the three judge reports, not re-read by me. **[I]** my inference or proposal. **[U]** unverified, with the check that settles it.

Machine-spec alignment: `design/ARCHITECTURE-SPEC.md` did **not** exist when I started (only the three drafts did) and **landed while I was working**. I then read it for everything home touches (sections 0-3, contracts C1-C7 and C11, the catalogue, slices S1-S2, S7, S19, resolved conflicts, home/base hooks, owner decisions) and re-aligned this document to it. **The spec governs.** `[MC-x]` tags map to its contracts (section 2); section 1 records every correction the spec forced on my first draft.

Handoff gate: CLAUDE.md currently says "No new runtime changes" for the machine continuation. Nothing in this document starts until the owner lifts that gate; PB0 is the first thing to ask for (decision D-H9).

---

## 0. The verdict in twelve lines

1. **Concept: Portable base as the physical layer, with Raised's retention function, hand-over verb and placement-as-data, and Claims' closure rule and offer seam kept for a later, gated terms layer.** The panel scored Portable 7.8 / 8 / 7 (canon, architecture, deliverability); Raised 7 / 7.6 / 6; Claims 6 / 5.4 / 4.5.
2. **A base is not an object.** It is derived: fixtures the player has set down, grouped by position. No deed, tier, level, zone claim, declaration or stored list. Specialisation is only what is standing there.
3. **Two reversible verbs on fixtures that have two states (carried / standing) plus one retention marker.** Set down, take up. A third verb, hand over, releases the marker and leaves the thing where it is.
4. **The hole that makes this safe is fixed first.** A village zone whose settlement repair relapses is unloaded and regenerated at the next access (`OverworldZoneManager.cs:1487-1489`, `SettlementManager.cs:87-138` [V]). The machine spec's S2 (one conjunct in `CanUnloadZone`) closes it for everything the player has set down and for the rig wherever it is hauled; home adds one conjunct for staying companions.
5. **Smallest saved surface in the panel:** one new 2-field Part (`PackableFixturePart`), the machine program's marker and installable Parts, one data file, **no World-entity ledger, no player property, no zone table**. A player who declines has zero new save footprint.
6. **Nothing simulates away, nothing upkeeps, nothing is owed.** Time-dependent work belongs to the machine program's reconciler. The base adds no timer.
7. **v1 lives on wild surface ground** (Spread, Sodden, Beating, Stump) outside protected ground. Underground ordinary cave columns come later (PB7, audited); village-adjacent placement and person-held terms come last and are gated.
8. **Free alternatives stay bit-identical:** rest, repair, carrying, inns, waysites, communal stations. A portable bed is withheld behind a balance decision (D-H1).
9. **Lore is protected by exclusion as data, not by silence:** Choir ground, Overwrit, catacomb villages, niches, hearth patches, authored scenes and Bower sites are never placement targets; hospitality stays an enacted act; no text answers a ledgered mystery.
10. **Slice 1 (PB0)** is home's addendum to the spec's S1-S2: the second relapse source, a staying-companion conjunct and the hospitality source pin, proven by RED tests against `UnloadZone` and (in PB4) a live relapse bench. It is implementable from section 13 alone, including S1-S2 if the machine program has not landed them.
11. **Honest limits are first-class:** no safety promise, no construction, no wagon, no map markers, no Linux/Deck claim, hostile reach unverified.
12. **Eleven decisions belong to the user or lore owner** (section 16); none blocks PB0 except D-H9 (the standing hold).

---

## 1. Conflicts between proposals and judges, and how each is resolved

| # | Conflict | Resolution | Why |
|---|---|---|---|
| C1 | Portable defers underground; Raised admits cave columns without a biome filter | Underground **ordinary columns** are a later slice (PB7) with Portable's biome allow-list (Spread, Sodden, Beating, Stump surface biomes only; Grovelands and Overwrit columns excluded) and an audit first. `IsOrdinaryColumn` is real and reachable (`UndergroundLayoutPlan.cs:14,30-31`; `OverworldZoneManager.cs:617-635` [V]) but is biome-agnostic, so the filter is ours to add. | Serves the "listening post second base" hook without Choir-law or Root ground. |
| C2 | Raised ships its own `HeldPart`/`IHoldsZone`; Claims ships `HoldPart`; the three machine drafts define three different markers | **One marker, the spec's** (C2/S2): `InstallationMarkerPart { InstallationId, Origin (Generated\|Player\|Relocated), Revision, RetainZone }` with static `Retains(Entity)` and `Retain(ZoneManager, zoneID)`. My proposed `HoldsZone(zone)` method, `PlaceId` and `Retains` field are **dropped**: "placed and not carried" holds because a carried entity is not in the zone's part index, and the spec's S1 lint forbids zone-id fields on new Parts. Home asks only for one optional hardening (R2: ignore a marker whose owner is `Loose`). | Three markers for one job is three permanent save names (unresolved Part names desync the load stream, `SaveSystem.cs:1644-1648` [P]); the spec resolved the spelling. |
| C3 | Reuse the generation-time `Waysites.IsEligible` exclusion list wholesale (Portable) vs not (judges: ~71% of sparse Spread would refuse) | **Split into two classes.** *Protected ground* (always refuse) and *family-owned* (Spread exploration families; refuse until audited, then lift). The family test is an extraction from `IsEligible` guarded by a seed-sweep equivalence test. `Waysites.cs:36-51` states the rationale is generation-time authority [V]; `SpreadExplorationPlan.ValidateInstalled` compares zone object identity only (`:226-243` [V]), so a player fixture does not break its validation. | The exclusion protects generation; runtime placement needs different reasons. |
| C4 | Portable says no `Closed/Refused` emitter exists; judges say it does | Judges are right: `StoryletPart.End` emits `closure/Closed` or `closure/Refused` with `spoken=true` (`StoryletPart.cs:244-258` [V]), but only for ledger entries. **Self-held ends do not emit `closure`.** They emit `furniture/TakenUp` or `furniture/HandedOver` with a `spoken:true` field. `closure` is reserved for ledger-backed, person-held terms (PB10). | Overloading the vocabulary would make unrelated records look like ledger events. |
| C5 | `build` diag category for set-down (Portable) | Not used. `build` is already `StartingBuildService.cs:206` [V]. Use `furniture` (set-down, take-up, hand-over, refusals) and the spec's `worldgen/ZoneRetained` (reason gains `staying-companion`). Both default-on (`Diag.cs:119-120` [V]); the spec adds no category. | Avoids a collision; bench categories must be default-on. |
| C6 | `BedPart.Owner` set on held beds (Portable, Raised) vs canon "owner-locked beds must not refuse a hosted guest" | **v1 does not set `BedPart.Owner`.** The portable bed is withheld (D-H1); when it ships, the owner check gets an explicit host-guest exception (a guest whose `UnderTheClothEffect.HostId` is the owner) with a RED test. `PlayerBedService.cs:31,35` is the refusal path [V]. | Guest-right is enacted hospitality; a base must not make the player's own guest sleep on the floor. |
| C7 | Bedroll in the first kit batch (Raised H1c) | Withheld. A held bed is "rest anywhere there is no hostile within 8 for a free full heal" (`RestSystem.cs:22-23,113` [P]); `Docs/CHUNK-ACTIVITY.md:39` says wild rest was deliberately *located* at waysites [P]. Ships only after D-H1, with instrumented rests per outing. | The biggest balance lever in the whole concept; Raised alone shipped it without a flag. |
| C8 | Cost wiring: two hand-maintained whitelists must be edited (judges) vs my first claim that none is needed | **Both were partly right; verified.** Set-down is a *pack* action: registering `HeldFixtureActions` inside `CombatUtilityActions` (the native-utility composition point) is enough, because `PerformInventoryActionCommand.cs:83` and `InventoryUI.cs:1521` already route and charge every command that `CombatUtilityActions.IsCommand` accepts [V]. **Take-up and hand-over are world-menu actions on the standing fixture**, and the world chain at `InputHandler.cs:3459-3471` has **no** `CombatUtilityActions` clause [V]; a command in no chain falls to a bare `InventoryAction` event with no transaction (spec section 1 item 5, confirmed by reading `:3500-3510`). Resolution: declare `ActionTime.OneAction` through the spec's **S3** when it lands; until then add one additive clause beside `WorldResourceActions.IsCommand` (`:3469`). | Removes a silent-refusal risk; pins it with a RED (PB2a). |
| C9 | The rig: marker on every release (Portable) vs only on committed crossing | **The spec's rule:** `RigRelocation` stamps `Origin="Relocated"` after a committed crossing (S19); same-zone hauling stamps nothing. The spec's legacy blueprint list **already retains the rig wherever it is hauled** (`AbandonedFieldWorkRig`, C2), so retention never depends on the stamp; the stamp gives the derived-list entry and reader line. Home adds the reader line, the list entry and hand-over. | The rig already works wherever it stands; a same-zone drag is not a decision to keep a place. |
| C10 | Claims: 3-day rent with a permanent ledger entry per run | **Rejected.** The later terms layer (PB10) is prepaid, non-compounding, lapse suspends and never seizes, and never holds an open ledger act while live (Clean => Open==0 gates the ending, `ClosureLedger.cs:42`, `EndingSpine.cs:54` [V]). | A 3-day term lapses on every expedition; it reads as rent and violates "no compulsory maintenance". |
| C11 | `Owned` tag: Portable says do not set it (N-15); R12 says evaluate | **Evaluate in PB1a; default yes, while Installed only.** `Owned` is an existing instance-level "creatures and exploration families leave this alone" convention with eleven references in seven files, all exclusions, none punishments (`SpreadCollectorPart.cs:127-129`, `SpreadPredatorPart.cs:310`, `SpreadExplorationPassage.cs:18,100`, `SpreadExplorationHunt.cs:27,39`, `SpreadExplorationCooking.cs:20`, `ConnectedSpreadSatellite.cs:201,203`, `SecondExplorationActions.cs:28` [V]). Tags persist (`SaveSystem.cs:988,1013`; `Entity.SetTag`, `Entity.cs:196` [V]). Setting it is the composable path to lift `family-owned` (C3). It is never a crime flag and no consumer may punish it (N-15 holds). | One instance tag works for situations nobody authored (principle). |
| C12 | Hospitality source pin: "only `Hospitality_ExtendCloth` and `ClothClaims` construct `UnderTheClothEffect`" | The real set is **three files**: `ClothClaims.cs:73`, `Hospitality_ExtendCloth.cs:79`, **`ConversationActions.cs:818`** [V]. The pin is an allow-list of those three. | A pin that omits an existing caller fails on day one. |
| C13 | One-time "honest limits" line needs saved state (Portable) | Dropped. Honest limits live in the kit's Examine text and in refusal strings (always visible, derived from the definition row). No saved flag, no nag. | Zero footprint; no popup. |
| C14 | PB2 sized at 2 cycles | Split: **PB2a** same-entity transaction and verbs (no player-visible kit), **PB2b** the box and its sourcing. | `CordSnareActions` consumes and creates; a same-entity pack-to-cell conversion is new code (`CordSnareActions.cs:47-77` [V]). |
| C15 | Companion retention: separate behaviour change or part of PB0 | Included in PB0 as its own commit (**PB0b**), flagged as a behaviour change (D-H2). Reads `CompanionOrders.IsStaying` (`CompanionOrders.cs:17-25` [V]); no new Part, no write. | A staying companion in a relapsing village is at the same risk as a fixture; both are "no surprise destruction". |
| C16 | Zone and coordinate fields on home Parts (my first draft had `InstalledZoneID/X/Y` and `PlaceId`) | **Dropped.** The spec's S1 hygiene lint rejects fields named `ZoneID` or ending in `ZoneID`/`ReceiverID` and `StationX/Y` on any new Part (`ARCHITECTURE-SPEC.md:80-81`); positional authority is what makes the kitchen family non-relocatable. Position is always the live entity position. `InstallablePart` is the spec's `{Mode, State}`; home adds none. | Keeps every home Part lint-clean and relocatable. |
| C17 | Who owns the install Parts and the verbs | Spec **S19** owns `InstallablePart`, `IRebindable`, the events and the rig stamp; home **PB2a** owns the verbs and the transaction. If S19 has not started they ship as one joint two-cycle slice (R4). Home requests a `BeforeUninstall` veto event the catalogue does not list (R3). | One owner per Part name; no competing definition. |

---

## 2. Alignment with the machine architecture spec

`design/ARCHITECTURE-SPEC.md` (96 KB, repo HEAD `9316f9330`) governs. The three drafts it synthesises disagreed on spelling and agreed on the job; the spec resolved the spelling (its section 8: "Marker fields: 4 fields now", "Retention scope: per kind", "Legacy retention: blueprint-name list"). Home depends on **the spec's names, exactly**.

| Tag | Spec contract | What home consumes | Home request or fallback | Slices |
|---|---|---|---|---|
| **MC-Save** | C1 / S1: saved-Part golden plus hygiene lint (`ARCHITECTURE-SPEC.md:78-82`, `:93-117`) | every new home Part passes the lint: namespace `CavesOfOoo.Core`, public parameterless constructor, serializable public fields, **no field named `ZoneID` or ending in `ZoneID`/`ReceiverID`, no `StationX/Y`**; class names unique project-wide (the factory registers by simple name, item 17) | `PackableFixturePart { Kind, SetTick }` passes by construction | PB2a, PB2b |
| **MC-Marker** | C2 / S2: `InstallationMarkerPart { InstallationId, Origin: Generated\|Player\|Relocated, Revision, RetainZone }`; static `Retains(Entity)` (= `RetainZone \|\| Origin != Generated \|\| legacy blueprint`) and `Retain(ZoneManager, zoneID)`; **one** conjunct in `CanUnloadZone`; legacy list `PeatOilStrainer`, `AbandonedFieldWorkRig` (the rig wherever hauled), `CinderholdOilRecoveryTray`; diag `worldgen/ZoneRetained { zone, reason, count }`; policy **per kind** (stateless kinds regenerate; player and relocated always retain); the marker also contributes the line "Set here to be used." through `IDescribeState` | the whole retention story for fixtures | **R1** add the well-keeper-renewal row to S2's RED list (a second `changed` source, `SettlementManager.cs:102-112`); **R2** (optional hardening) `Retains` ignores a marker whose owner has `InstallablePart.State == Loose` | PB0 |
| **MC-Install** | C11 / S19: `InstallablePart { Mode: Fixture\|Portable, State: Installed\|Loose }`, `IRebindable`, events `BeforeInstall`, `Installed`, `Uninstalled`, `StationRebound`; a player install adds a marker with `Origin="Player", RetainZone=true`; a Loose part vetoes readiness through `IStationReadinessGuard`; `RigRelocation` stamps `Origin="Relocated"` after a committed crossing; the kitchen family is non-installable | the Parts, events and rig stamp | **R3** add a `BeforeUninstall` veto event (the catalogue lists none; home needs a take-up veto for job escrow; fallback: PB2a raises its own when the first stateful kind needs it). **R4** S19 owns Parts, events, stamp; home PB2a owns verbs and transaction; ship as one joint two-cycle slice if S19 has not started | PB2a, PB5, PB6 |
| **MC-Cost** | C4 / S3: `ActionTime { Unspecified, Free, OneAction }` on `InventoryAction`; `AddAction(..., time:)` returns the row; `AddDisabled(name, display, code, reason)` rows; the legacy chains are kept | declared cost and named refusals | **Fallback until S3** (verified, section 1 C8): set-down is a pack action registered in `CombatUtilityActions`; take-up and hand-over need one additive clause in the world chain `InputHandler.cs:3459-3471` | PB2a |
| **MC-Station** | C5 / S4: `StationQuery.Resolve/Near/Collect`, `IStationCapability`, `IStationReadinessGuard`, `IStationAccessGuard` | parity of communal and held stations (M-4); `IStationAccessGuard` is the seam for `Owner` terms | the rig only (existing `ForgeWorkReceipt`) until S4 | PB5, PB6 |
| **MC-Describe** | C6 / S5: `IDescribeState`, `DescribeContext.State/Uses/Limits/Access`, `StateReader.Collect` | `PackableFixturePart : IDescribeState` | before S5: `Describe()` plus the existing Examine builder | PB3 |
| **MC-Terms** | C7 / S10 (gated on critique): `ServiceTermsPart.Access = Open\|Fee\|Gift\|Withheld\|Owner`, `OwnerID` reserved; an unregistered `Owner` **fails closed**; **registering `Owner` and the spoken-end verb are the home design's job** (spec hook 2) | PB10 only | not a v1 dependency; an absent terms Part is Open, which is today's behaviour | PB10 |
| **MC-Work** | C9 / S15-S16: one reconcile registry, `WorkJobPart` | started jobs finish once, on arrival; take-up veto while escrow exists | PB6 uses a stateless kind; stateful kinds wait | PB6+ |
| **MC-Receiver** | C10 / S17 | relay and pack-less outputs | not before PB11 | PB11 |
| **MC-Placement** | C3 / S6-S8 and hook 6: `InstallationPlacement` extracted "when the install verb needs them" | an optional secondary route-preservation check for solid fixtures | home's local 8-open-neighbours rule is primary; **do not reuse the authored `Geometry.Place` "unreserved" clause after load**: it reads `GenReservedCells`, which is not serialized (N-13) | PB1 |
| **Not used** | S6-S8 authored census and post-builder; S20 NPC keeper (N-17); S21 `PartUpgrade` (lazy attach at the player's act instead) | - | - | - |

Gates:

- **G1 (spelling) is resolved by the spec.** Home adopts exactly: `Origin` values `Generated`/`Player`/`Relocated`, `RetainZone`, `InstallationId = "player:<entityId>"`, diag `worldgen/ZoneRetained`, and the spec's namespace and hygiene rules.
- **G2 (terms).** Nothing in home defines a private permission model (M-29). The terms layer waits for the independent critique of explicit work permission (spec S10).
- **G3 (hold).** The standing "no new runtime changes" hold (spec section 0 and owner decision 1; D-H9) must be lifted before any slice. Spec S1 and S2 are on home's critical path; if the machine program is doing them, home waits and runs PB0a-PB0c after them.

## 3. The concept on one page

### 3.1 Vocabulary (working labels only; every in-world noun passes a voice card and the blind gate, R10)

| Working label | Meaning |
|---|---|
| fixture | an object the player can use or store in: a box, a bench, the rig, later a stake. It exists in two states: **carried** (in the pack, or in tow) and **standing** (installed in a zone). |
| kit | a carried fixture that can be set down. |
| set down | the paid, atomic, reversible act that makes a kit a standing fixture in an adjacent legal cell. Same entity. |
| take up | the atomic reverse. Same entity, same contents. |
| hand over | release: remove the marker and the `Owned` tag, leave the object where it stands. The spoken end of "kept in this place". |
| places you keep (list) | **derived**: standing fixtures with a player-origin marker, clustered within Chebyshev 8 per zone. Never saved. |

Terms to avoid in neutral player copy: *home, room, warm, kept* (Choir lexicon, N-19), *listening post* near catacombs, technology nouns (regulator, cell, transmitter, power; L16). `Docs/Lore/` and Kyakukya are unused.

### 3.2 Why this fits the user's constraints

- **Consequence, never precondition.** Every ordinary path is untouched; the system adds two verbs on objects a player first chooses to acquire. A declining player has no new save data, text, prompt or shortage.
- **Canon.** Concord: "the caravan is the truer 'home'; standardized, modular, packable" (`Lore/History/08_MaterialCulture.md:143` [P]). Tent-Right: portable; "the well and the salt-work are the fixed points; everything else moves" (`:159` [P]). Both are packing cultures. Choir, catacomb, Bower, Curation and Recension ground are excluded by data (section 7).
- **Weights specialise without a tier system.** At Strength 16 the pack carries 240, a drag carries 128, one lift is about 30 [P]. What you cannot carry, you leave; what you leave defines the place. No weight is changed anywhere (MACHINE-SYSTEMS-DESIGN.md:31).
- **Reversibility is the consent story.** Take-up returns the same entity with its contents, escrow and condition.
- **Retention follows instances**, never a ZoneID or settlement-name list (N-13).

### 3.3 What the player gets, and what they do not (the "thin vertical" answer)

| Gets | Does not get |
|---|---|
| Choice of where: a stash at a dig, a bench beside a spring, a post on a ridge | A promise of safety (rest is still refused with a hostile within 8; a box is destructible and spills) |
| Persistence: the room as left, no refill, no regeneration, the same entity IDs | Rooms, roofs, shelter, comfort, a respawn point, hired labour, passive output |
| Work that finishes while they are elsewhere (machine program: a started batch yields once, on arrival) | A stat, a level, a tier, a count anything reads |
| A derived list of the places they keep, with days since set down | Map markers, remote deposit/withdraw, teleporting goods |
| A second site for survey and relay (later) | A reason to stop using inns, waysites, communal stations |

The honest weakness: until the machine program ships installables, a standing box is close to what dropped items in a never-evicted wild zone already give. The base layer is the **place** and the **guarantee**; the machine program supplies the function. That is the correct dependency direction (MACHINE-SYSTEMS-DESIGN.md:11).

### 3.4 Weight classes (arithmetic [I]; rules [P])

| Class | Rule | Ceiling at Strength 16 | Examples |
|---|---|---|---|
| Carried kit | pack `Str x 15`, one-item lift `ceil(w/2)` (+1 one-hand) | 240 pack; single item about 30-32 | camp box, bedroll, survey stake, light bench frame |
| Hauled fixture | drag `Str x 8`, `HandlingPart`, same zone; one adjacent border only under the M1c contract (`RigRelocation.cs`) | 128 | the field work rig (120) |
| Fixed | no movement | - | wells, ovens, MillStone-class; out of scope |

A wagon is a hauled container (tare + contents <= 128) that adds at most `128 - tare` beyond a 240-capacity pack, at a speed penalty, with no cross-border path. It does not pay for itself under today's rules (D-H10; not now).

---

## 4. Player experience

### 4.1 Vignettes (map to the requirement stories S1-S10)

**A. The stash at the dig (S2).** A player works a Beating dig over several outings. A Concord post sells a *carried box* through the ordinary stock tables. Three zones on, at the dig, the box's action menu offers "set down" per legal adjacent cell, exactly like laying a cord snare. The preview names the first failing reason, or says it will stand. One action later it stands. Heavy finds go in; while it was in the pack they weighed on the player (the box opts into `ContainerLoadPart`); standing, they do not. Forty days of travel and rest later it is the same entity with the same contents, after F5 and a fresh Continue as well. Its reader says what is true: set down N days ago; nothing here changes while you are away; nothing guards it; it can be destroyed and spills.

**B. The rig and the box (S1, S7).** The player repairs the rig (M1b), hauls it across the border (M1c), releases it beside the box. The committed crossing stamps the marker (`Origin="Relocated"`, spec S19). Refits there use the same recipes, previews, costs and readiness as at Peat Works (M-4 parity). They may instead reclaim it for 2 timber, carry parts, or use the communal rig. All three stay valid.

**C. The player who declines (S6).** Never buys a kit. No list, no journal entry, no prompt. Waysite rest, inns, communal forges and carrying are bit-identical (M-12). The save has no new Part, property or ledger (M-3).

**D. Two places, specialised (S7).** A forge place (rig + box) at the peat edge; a stash place at the dig. The list shows both with the zone's name, kinds and days since set down. Moving goods between them is carriage. The reader never offers a deposit or withdrawal at a distance.

**E. Walking away (S10).** The player simply leaves. Nothing is recorded, nothing breaks, nothing is owed. If they choose to say it, "hand over" removes the marker and leaves the thing standing for whoever finds it; the cap slot returns.

### 4.2 The pitch hooks, and what home contributes to each

| Hook (MACHINE-ROSTER-VISION.md) | Home contributes | Machine program supplies | Limit stated |
|---|---|---|---|
| "second bases (a listening post)" | place class `OrdinaryColumn` (PB7), carried survey stake, box; retention | survey instrument, coarse leads | answers "hollow or solid", never "which hollow"; records the *sari... sari...* sound, never classifies it; no bleed map (lore L1, L11); avoid the word "listening post" near catacombs |
| "connecting scattered places (a restored tower)" | the *where* and the guarantee that both ends persist; a derived list keyed by entity ids | relay endpoints, transmitter, receiving container, carriage | never writes to a non-active zone from a timer; goods in transit are one saved entity; reports say "departed" until something arrives; a Slip relay is an unreliable channel, never Urqu choosing (R1); a relayed message is never guest-right, a cloth claim or a spoken "no" |
| "a modest fuel-powered version keeps ordinary homes viable" | nothing special; a thermal fixture is hauled-class with fuel escrowed for a bounded job | thermal exchanger | read as **convenience and processing** (drying, culture, a workable bench), never a cold or shelter need (N-11; decision D-H7) |
| "several bases specialised" | derived list; per-zone and per-world caps; no tier | which fixtures exist | specialisation is only what stands there |
| "Working installations must be useful before home ownership" | communal stations keep working; one station query serves both (M-4) | `StationQuery` | none |
| "Normal healing and recovery should remain accessible" | no change to rest, repair, carrying | - | bit-identical (M-12) |

### 4.3 Discoverability and onboarding (no nagging; N-2)

1. **The object.** A kit's Examine/F1 states what it is, its weight, how it is set down, what refuses it and what it will not do, generated from its definition row.
2. **Traders.** A stock-table line in fresh worlds (Concord posts first; Concord idiom). Existing worlds: see PB1a (partial reach via the shelf-restock roll, section 8.1).
3. **The rig reader.** After M1b's "expressly relinquish" text, one line that a standing rig stays where it is set.
4. **The standing fixture.** Its reader says it stays, what it will not do, and how to take it up or hand it over.
5. **The list.** Appears in the existing journal-style surface only while at least one player-origin marker exists (derived on open; no saved flag); no empty state.
6. **Decline path.** Nothing changes; nothing is locked behind it; quests, recipes, routes, endings never read it (M-2: a source-pin grep fails on any consumer outside readers and the list).

No popup, counter, countdown or one-time tutorial line.

---

## 5. Data model

Principles: definitions in data, state on instances, capability Parts, no base class, no ZoneID or settlement registry (N-13), no ledger (M-3), no unit conversions (N-12).

### 5.1 Definitions (data; not saved per instance)

`Assets/Resources/Content/Data/PackableFixtures.json` (new, additive; flat arrays of small objects, JsonUtility-compatible):

| Field | Meaning |
|---|---|
| `Kind` | key; referenced by the instance Part |
| `Class` | `Carried` (installable `Mode=Portable`) or `Hauled` (the rig: not installable, stamped `Relocated` by S19) |
| `Blueprint` | the entity blueprint (also referenced by stock tables) |
| `Solid` | whether a standing fixture is solid (box: yes) |
| `GroundClass` | named predicate set: `OpenGround` (box, bench), `BareGround` (stake) |
| `Rounds` | effort rounds for set-down and take-up (v1: 1) |
| `Protects` | which protection capabilities refuse a spot nearby (dormant until tags exist, section 7.3) |
| `Manifest` | explicit finite salvage manifest for a reclaim verb; empty by default (M-17) |
| `Voice` | an intent-card id for strings, never the strings themselves |

Validation at load (Preflight style): unknown blueprint, duplicate `Kind`, `Class` that disagrees with the blueprint's `HandlingPart`/`Takeable`, negative rounds. A bad row disables only that row, never the game; a reduced content pack (blueprint missing) omits the row.

**Missing-definition semantics (new, missed by all three proposals).** A saved `PackableFixturePart.Kind` that no longer resolves makes the fixture **inert but retrievable**: marker and retention still hold; the reader says it cannot tell how it was meant to be moved; **hand over works** (needs no definition); **take-up refuses with `unknown-kind`**; destruction works normally.

### 5.2 Instance Parts (additive; primitives and strings only; reflection-saved by public field name)

| Part | Owner | Fields | Role |
|---|---|---|---|
| `InstallationMarkerPart` [MC-Marker] | machine program (spec C2/S2) | `InstallationId` (`player:<entityId>`), `Origin` (`Generated` / `Player` / `Relocated`), `Revision`, `RetainZone` | retention and discovery; present only while installed; a player install writes `Origin="Player", RetainZone=true` |
| `InstallablePart` [MC-Install] | machine program (spec C11/S19) | `Mode` (`Fixture` / `Portable`), `State` (`Installed` / `Loose`) | physical state, independent of `Takeable`; home adds no field |
| `PackableFixturePart` | **home** (the only new Part this design owns) | `Kind`, `SetTick` (-1 while carried) | definition key and the one timestamp readers need |
| existing `PhysicsPart.Takeable/Solid`, `ContainerPart`, `ContainerLoadPart` | existing | existing public fields | the kit's own state, flipped by install |
| entity tag `Owned` | existing convention | tag set while Installed, cleared on take-up and hand-over | wild creatures and exploration selectors leave it alone (C11) |

Not stored: zone, cell or owner ids (the S1 lint forbids them; the live entity position is the authority), counterparty ids (no counterparty in v1), a provenance enum (bought/found is not tracked and nothing reads it), fee, term, owner, permission.

`ServiceTermsPart` is **never attached in v1**. A held fixture with no terms Part is exactly today's behaviour.

### 5.3 The derived list (never authoritative, never saved)

`HeldPlaces.Enumerate(manager)` scans `CachedZones` through the lazily seeded part index (`Zone.GetReadOnlyEntitiesWithPart<T>`, `Zone.cs:95-130` [V]) for `InstallationMarkerPart` with `Origin` in {`Player`, `Relocated`}; clusters within Chebyshev 8 per zone (the same radius as the rest-refusal radius [P]) by single-link, ordered by (x, y); reads `SetTick`. It runs only when the list opens. The first query per zone seeds its bucket by scanning that zone's entities once; later queries are O(1) per zone. PB3 profiles it against a synthetic 300-zone world; if it exceeds budget the remedy is a rebuild-on-mismatch cache with a diag record, **not** a ledger. A mismatch is impossible today because there is nothing to mismatch (M-26).

### 5.4 Save impact

- FormatVersion stays 8; strict equality is untouched (`SaveSystem.cs:48,167` [P]). Changes: new Part classes, new blueprints, one new data file. No Part or field removed or renamed (N-13).
- Round-trip through `SaveEntityBody`/`LoadEntityBody`; every field primitive or string.
- Old save: has none of the new Parts, loads exactly as before. A pre-feature v8 save with cached zones loads, plays, saves and reloads; the zone-content diff equals the player's own actions (M-25).
- New save with an older binary: **no promise** (unresolvable Part type names desync the stream, `SaveSystem.cs:1644-1648` [P]). Stated limit.
- **Caps** (provisional; profile before raising; save capture is main-thread and roughly linear in entities, about 71 MB/s [P]): **8 standing fixtures per zone, 32 per world**, counted by scan at set-down. Recorded as limits, not design (MACHINE-SYSTEMS-DESIGN.md:35).

---

## 6. Retention and persistence

### 6.1 The hole (verified)

- `CanUnloadZone` is an explicit chain of nine predicates (LairStacks, GleanersDistrict, SoddenDistrict, Wayhouse, Exploration, CurationIntakePart, RepairCultivationSite, BiomeCropPlacement, SecondExplorationSites), none generic (`OverworldZoneManager.cs:1241-1246` [V]); the base implementation returns true (`ZoneManager.cs:105` [V]). `UnloadZone` removes the zone from `CachedZones` and the next access regenerates it (`ZoneManager.cs:125-139` [V]).
- The only production caller is the village access path: on access to a Village POI, `AdvanceSettlement` runs and, if it returns changed while the zone is not active, `UnloadZone` follows (`OverworldZoneManager.cs:1487-1489` [V]; grep over `Assets/Scripts` excluding Tests and Scenarios [P]).
- `AdvanceSettlement` returns changed from **two** branches: a well-keeper renewal (`SettlementManager.cs:102-112` [V]) and a relapse to Fouled of any Structure, HeatStone, LightBeacon or Well site (`:113-130` [V]). Both unload. Both need a RED row.
- The relapse happens at **access**, i.e. when the player comes back after a long absence. That is exactly when "the room as left" is most at risk.
- Wild zones are never evicted by production code today, so v1 camps are safe without the clause; the clause turns an accident into a guarantee and is the prerequisite for ever placing beside a village or hauling the rig into one.

### 6.2 The clause

Home adds **nothing** to the fixture clause. It is the spec's C2/S2 conjunct, appended once to the chain (`OverworldZoneManager.cs:1241-1246` [V]):

```csharp
&& !InstallationMarkerPart.Retain(this, zoneID)
```

`Retains(Entity)` is `RetainZone || Origin != Generated || blueprint in the legacy list`. A player install writes `Origin="Player", RetainZone=true`; a relocated rig is stamped `Relocated`; and the legacy list retains the rig wherever it is hauled even before a stamp exists. "Not carried" is implicit: an entity inside a pack is not in a zone's part index (`Zone.cs:95-130` [V]). Cost is O(markers of one zone), only inside `UnloadZone`. Releasing the last marker (take-up, hand-over, destruction) releases retention with no bookkeeping (M-7). Two arbitrary zones retain independently.

Home adds **one** conjunct of its own, committed separately (PB0b), for staying companions:

```csharp
&& !StayingCompanionRetention.Retain(this, zoneID)
```

It returns true when the cached zone holds a creature for which `CompanionOrders.IsStaying` is true. No new Part, no write.

Diag: both use the spec's record `worldgen/ZoneRetained { zone, reason, count }`; the companion path writes `reason: "staying-companion"`; nothing is recorded when released.

### 6.3 Consequences, stated

- A retained relapsing village stops **regenerating**. Well, oven and lantern entities re-sync from live `SettlementState` (`WellSitePart.cs:34-57` reads the live stage on every render; structure patches hold no entity) [P, arch judge]; retention therefore freezes only **build-time content** (stock rolls, containers). This matches "no free refill" (it removes a refill: the code comment calls out "would refill that zone's containers", `home.md` 3.9 [P]). It also means such a village no longer refreshes build-time stock by relapse (decision D-H2).
- The site-sync question (do site entities stay in step with `SettlementState` in a retained, relapsed village? requirements R2) is benign by code reading [P]; PB0 records a read-only trace of it in the slice doc and PB4's village case confirms it in play.
- Exploration-family zones are already retained by `SpreadExplorationPlan.Retain` and validated by zone identity (`:226-243` [V]); the new clause is additive and cannot conflict.

### 6.4 The spilled-contents gap (gate for any evictable zone)

A destroyed container spills (`DestructionSystem.cs:155,339-350` [P]); the marker dies with the entity; in an evictable zone the zone is then releasable and the next relapse deletes the spill. This does not occur in v1 (every v1 zone is wild and never evicted) but **gates PB11/PB12** (village-adjacent placement). Options, to be chosen before then: (a) a destroyed held container leaves a marker-bearing husk until emptied; (b) held containers in evictable zones are not destructible by ordinary means; (c) refuse placement in evictable zones. Default: (c) until a decision. Option "accept the loss" is not acceptable (a later regeneration deleting items is a surprise).

### 6.5 Time

The base adds **no** time logic and **no** call at the three entry points (arrival `InputHandler.cs:1204-1205`, after a paid player turn `:1318-1319`, load of the active zone `GameBootstrap.cs:728-729,1033-1034` [P]). Time-dependent work in a standing fixture reconciles through its own contract: `CropTime.ReconcileZone` and `KitchenBatchPart.ReconcileZone` today, MC-Work later. Consequences (all required): a job completes only when the player is in that zone (M-8, M-28); a started job yields at most one batch (M-10, MC-Work's cap); generation and partial hydration never call reconcile (`CropTime.cs:3-9` [P]); two places reconcile independently, each on arrival. No decay, cooling, fuel burn or relapse for held things (M-11, N-4).

### 6.6 Old saves

Zero retrofit (N-14). Legacy fixtures (the rig, the oil strainer, the recovery tray) gain Parts only at the player's own act (the rig's committed crossing, or a set-down), never at load. Cached-zone upgraders (`MultiCellPilotRuntime`, `FellingSceneRuntime`, `MorrowfastSceneRuntime`, `SaveSystem.cs:1135-1149` [V]) rewrite specific zones at load, so those zones are **protected ground** (section 7.1).

---

## 7. Placement

### 7.1 Zone classes (data-driven; one classifier, pure on saved data)

`HeldPlaceZoneClass.Classify(manager, zoneId)`, evaluated in this order:

| Order | Class | Test | v1 result |
|---|---|---|---|
| 1 | `poi-zone` | `WorldMap.GetPOI(x,y) != null` (villages, camps, lairs, Choir nodes, catacomb villages, Root, sinkholes, Felling-Site, Overwrit POIs, Morrowfast) | refuse (PB11/PB12 under terms) |
| 2 | `protected-ground` | not `WaysitePlan.IsEligibleIgnoringFamily` (the extracted inner test: opening zones, Reference Glade, Gleaners cellar, Wayhouse, rare-encounter pair and viper zones, Tenth Fire, Woven Doll, abandoned counters, regional-situation zones) **or** in `AuthoredWildernessZoneIDs` (`OverworldZoneManager.cs:771-772` [V]) **or** `MultiCellPilotRuntime.ZoneID` or any zone with a cached-zone upgrader | refuse |
| 3 | `biome` | surface biome not in allow-list `{Spread, Sodden, Beating, Stump}`: **Grovelands** (Choir law: "everything here is somebody"), **Overwrit**, Cave and others | refuse |
| 4 | `family-owned` | `manager.Exploration.TryGetPlacement(...)` with `Family != None` | refuse **until PB1a lifts it**, then admit if every runtime consumer honours `Owned` or is generation-only |
| 5 | `wild-surface` | everything else on the surface | admit |
| - | `ordinary-column` | `z > 0`, `UndergroundLayoutPlan.IsOrdinaryColumn`, `SurfaceBiome` in the allow-list | refuse in v1 (`column-pending`); admit in PB7 |

Stump is allowed because waysites already stand there (a watcher's camp), but the lore owner should confirm (D-H6). Opening-zone and exploration exclusions come from `WaysitePlan.IsEligible` (`Waysites.cs:36-51` [V]); PB1 **extracts** `IsEligibleIgnoringFamily` and `IsFamilyOwned` and re-expresses `IsEligible` as their conjunction, guarded by a seed-sweep equivalence test (three seeds x every cell). It is a pure refactor of accepted generation code.

### 7.2 `PlacementCheck` (one pure function, shared by preview, action and diag; M-15, M-24)

`Evaluate(zone, cell, definition, actor) -> {ok, reasons[]}`. Saved data only; never `GenReservedCells` (not serialized, `Zone.cs:397-404` [P]). One reason code per refusal; the preview prints the first failing reason and how many others fail; refusals cost no action.

| # | Code | Rule |
|---|---|---|
| 1 | zone-class | section 7.1 |
| 2 | `ground` | **property predicate**, not a blueprint list: walkable; no liquid or gas pool part; no cloud or coating on the tile; no occupant with `TriggerOnStepPart`, `LiquidPoolPart`, `GasPoolPart`, burning or aflame (the same style as `SpreadPredatorPart.cs:300-305` [V]); terrain not in the data list `RouteGround` (RoadStone); the rig's verified set (Grass, Floor, StoneFloor) must pass |
| 3 | `cell-occupied` | target cell holds no other object; at most one solid fixture per cell |
| 4 | `open-neighbours` | solid fixture: all 8 neighbours walkable and non-hazard (one fixture can never plug a corridor or pen a creature); non-solid: 4 neighbours |
| 5 | `edge-margin` | at least 3 cells from every zone edge (arrival and border lanes) |
| 6 | `stairs-doors` | no `DoorPart`, `StairsUpPart`, `StairsDownPart`, `Well` or exit entity within Chebyshev 2 (saved entities; the spec's authored rule uses 1, runtime solid fixtures use 2) |
| 7 | `protected-near` | no entity with a protection capability within 6 cells. **Dormant**: at HEAD no `ProtectedSubject`/`LivingTended`/`SacredFixture`/`InertScene` tags exist [U]; PB1a inventories what does. The preview never claims it checked when it did not. |
| 8 | `cap-zone`, `cap-world` | section 5.4 |
| 9 | `state` | the fixture is not in a transaction, not equipped, not hauled (unless a hauled-class release), not destroyed |
| 10 | `class-specific` | the definition's `GroundClass` |

Reuse note (spec hook 6). The authored builder's `Geometry.Place(x,y)` admits only exterior, dry, **unreserved** cells (`SpreadWildernessSituationBuilder.cs:167-227` per the spec [V*]); the "unreserved" test reads `GenReservedCells`, which is not serialized, so after load it would admit cells that were reserved at generation. The runtime check therefore never calls it. The local 8-open-neighbours rule above is the primary, deterministic, O(1) gate; once `InstallationPlacement` is extracted (hook 6), `PreservesRoutes` and `BorderReach` may serve as a secondary route-preservation check for solid fixtures, behind a test that pins post-load behaviour.

### 7.3 Unsupported-state matrix (honest refusals, M-27)

| Request | v1 answer |
|---|---|
| set down in a village, camp, lair, Choir, catacomb, Root, sinkhole, Felling-Site, Overwrit or any POI zone | refuse `poi-zone` |
| set down underground | refuse `column-pending` until PB7 |
| set down in Grovelands or Overwrit | refuse `biome`; never an apology that attributes intent to the ground |
| set down in an exploration-family zone | refuse `family-owned` until PB1a lifts it |
| haul any fixture across a border | only the rig, only the M1c contract; everything else refuses with the existing message class |
| haul across stairs, world-map travel, diagonals | refuse |
| take up a machine mid-job | refuse `job-escrow` (MC-Work) until the job is finished or stopped |
| take up an over-weight load | refuse `overweight-load`, naming it; nothing spills |
| a kitchen or other zone-pinned Part | not installable; the reader says so (kitchen family binds `KitchenZoneID`/station coordinates/owner ids [P]) |
| a roof, a room, a shelter | none; `Cell.IsInterior` is builder-set and no derivation exists [P] |
| safety, wards, "safe sleep" | none promised; rest refusal within 8 is unchanged |

---

## 8. Lifecycle

### 8.1 How a fixture comes to exist and be acquired (no declaration; no retrofit)

In order of shipping:

1. **The relinquished rig** (exists, M1b/M1c). Needs no kit; stamped on a committed crossing (C9).
2. **Bought kits** at trade posts through the existing stock tables, `Tender.For` and `Ledger`; no new sink. The Concord idiom (standardized, packable, priced) [I]. The Concord/Quartermaster table names are chosen in PB1a.
3. **Found** in fresh worlds only (waysite or cargo stock). Never placed into cached zones (N-14).
4. **Made** from finite materials: later, via the machine program's explicit-manifest process engine.
5. **Lent** by a Tent-Right host (gift): PB10.

**Old-world sourcing (answered in part).** `TraderRestockSystem` rolls the shopkeeper's *saved table name* from current data at restock, but only when the shelf is low: `CountStockUnits < ShelfLowWaterMark (3)` and the restock interval has passed (`TraderRestockSystem.cs:88-111` [V]). So a new stock-table line **does** reach already-generated shopkeepers, **slowly**, as their shelves run down. Default (D-H3): accept that trickle; **no per-character grant**; no free item is ever given at load. PB1a records which tables and keepers carry the line and confirms with a scripted probe.

### 8.2 Set down (carried kit): one atomic transaction

Offered by the kit in the pack, one row per legal adjacent cell (up to 8), exactly as `CordSnareActions.AddActions` (`:21-34` [V]); executed through `PerformInventoryActionCommand`'s native-utility branch (`:83` [V]); registered in `CombatUtilityActions` (C8). The command binds `{zoneId, originX, originY, targetX, targetY, kitEntityId}`; a changed origin or quantity refuses `stale-selection`.

Steps inside one `InventoryTransaction` (`Do(apply, undo)`, `TryClaim`, `Commit`, `Rollback`, `InventoryTransaction.cs:117-236` [V]):

| # | Step | Undo |
|---|---|---|
| 1 | Re-evaluate `PlacementCheck`; refuse with the first reason | - |
| 2 | Fire the spec's `BeforeInstall` (veto participants: fault state, any `IStationReadinessGuard`; job escrow is n/a on set-down) | - |
| 3 | `TryClaim` actor and kit (`in-progress` if claimed) | release |
| 4 | Capture `InventoryTransferSnapshot` and the kit's `Takeable`, `Solid`, tags, Parts | restore |
| 5 | Remove the kit from the pack through the receipt | restore pack |
| 6 | Flip `Takeable=false`, `Solid` per definition, set tag `Owned` | restore flags and tag |
| 7 | Place the **same entity** in the target cell (the drop path's placement; PB2a locates the helper the drop command uses) | remove from cell |
| 8 | Attach or update `InstallablePart` (`Mode=Portable`, `State=Installed`), `InstallationMarkerPart` (`Origin="Player"`, `RetainZone=true`, `InstallationId="player:<entityId>"`), `PackableFixturePart.SetTick=now` | remove or restore Parts |
| 9 | Commit; then fire `Installed`; diag `furniture/SetDown` | - |

Cost: set-down is registered in `CombatUtilityActions` (section 1 C8) and charges one action on commit, none on refusal. When the spec's S3 has landed, each direction that fails `PlacementCheck` is shown as a **disabled row** (`AddDisabled`) carrying its reason instead of vanishing; before S3 only legal directions are listed and a forced refusal logs the reason.

A forced failure at any step restores pack, cell, flags, tags and Parts exactly (M-14). Lazy attach to legacy instances (the rig, the strainer, the tray) happens here at the player's act, never at load.

### 8.3 Take up

Offered by the standing fixture to an adjacent actor (the same event family the companion orders use, `CompanionOrders.HandleEvent` `GetInventoryActions`, `CompanionOrders.cs:28-40` [V]; PB2a's first probe confirms a non-Takeable world fixture reaches this menu). Validates adjacency, `Class`, the definition (else `unknown-kind`), that the load including `ContainerLoadPart` contents fits the actor (else `overweight-load`), and `BeforeUninstall` vetoes (job escrow). Steps mirror 8.2, with the spec's `Uninstalled` event after commit and the `BeforeUninstall` veto (request R3); the world-menu cost clause is required (section 1 C8); the same entity returns to the pack with the same contents, escrow and condition; marker removed in the same commit; tag `Owned` cleared; `Takeable` restored; diag `furniture/TakenUp`.

### 8.4 Hand over (the spoken end of "kept in this place")

Removes the marker and the `Owned` tag, clears nothing else, leaves the object standing, in the same commit; diag `furniture/HandedOver {spoken:true, contents}`. The object is then an ordinary, un-held object of the zone, as the rig's relinquished provenance is. It returns the cap slot. Its confirmation line says plainly what it means; in every v1 zone it has no side effect beyond the reader (wild zones are never evicted); in an evictable zone, which v1 forbids, it would say the ground can be cleared. Default option is cancel.

### 8.5 Moving, hauling and the rig

- **Move** = take up, carry, set down. Same entity, same ID, same contents, same condition.
- **Hauled class** (the rig): the existing release and the M1c one-border contract are unchanged. Home adds nothing to same-zone hauling. The committed crossing stamps `Origin="Relocated"` (spec S19); retention of the rig does not wait for the stamp (the spec's legacy list). The rig's `Payload` validator tolerates extra Parts (it counts six named types and forbids Container/Inventory/Stacker/SpatialFootprint but does not pin the total list; `FieldWorkRigRules.cs:17-31`, `RigRelocation.cs:62-75` [P]); a named test pins that.
- A rig crossing into an evictable zone is retained by the same clause; this is a beneficial side effect and the reason PB0 precedes any crossing stamp.

### 8.6 Walking away, destruction, death

- **Walk away**: always legal, no mechanical penalty, **records nothing**. With no counterparty and no timer there is no truthful moment to record `Abandoned`; inventing one would be a hidden clock. Whether self-held abandonment ever feeds the closure gradient is a lore-owner decision; recommendation: no (D-H5).
- **Destruction** (a hostile, fire, the player's own act while present) is an in-presence consequence: a destroyed container spills; the marker dies with the entity. Nothing is destroyed by absence, relapse, reconcile, reputation or arrival rolls (N-3).
- **Death** is recoverable by reload; the base is never a respawn point and does not alter death semantics (N-21).

### 8.7 Closure vocabulary rule (standing)

For any person-held arrangement: **never register an open ledger act while the arrangement is live** (`ClosureLedger.Clean => Open == 0` gates the Practice ending, `ClosureLedger.cs:42`, `EndingSpine.cs:54` "ledger_open" [V]). If an entry is ever written it is `UndertakeAct` then `CloseAct`/`RefuseAct` **in one motion at the end**, weight zero, never exposed as a gauge (L11). `StoryletPart.End` forces `Spoken=true` (`StoryletPart.cs:250` [V]); `UndertakenTurn` is the detection tick. Self-held ends never touch the ledger.

---

## 9. People

### 9.1 Residents, labour, visitors
None. No hiring, schedules, labour or passive production (N-17). NPC use of a held machine is a machine-program contract blocked today by the player-FOV visibility requirement (`MaterialProcessPlan.cs:76-77` per capability-first C11 [P]). Nothing arrives because a place exists (arrival rolls are once per zone: `WorldTravellers` 1-in-8 and at most 3 per game, `ClothClaims` rolls once; both unaffected by a later set-down [P]).

### 9.2 Companions
The existing saved `StayHere` order is the only way anyone "lives" at a place. A staying companion counts toward retention (6.2, the second conjunct), dormant while away (removed from the scheduler on leaving, `InputHandler.cs:1188-1199` [P]), with no needs and no schedule. Party transit skips staying companions [P]. PB0b fixes the standing risk that a staying companion in a relapsing village is regenerated away; flagged as behaviour change D-H2.

### 9.3 Guests and hospitality
Tent-Right guest-right is an enacted, in-person, three-day act; a base never grants, withdraws, extends, prices, auto-hosts or automates it (N-8, C1, R7, L3). A place supplies physical adjacency only: a fire, water you already carry, ground. Water is given, never sold, to a guest; there is no trade at a held place. **Source pin (allow-list of exactly three constructors):** `ClothClaims.cs:73`, `Hospitality_ExtendCloth.cs:79`, `ConversationActions.cs:818` [V]; the PB0 characterization test fails if a fourth appears.

### 9.4 Beds and guests on the bedroll
v1 sets no `BedPart.Owner`. When the portable bed ships (PB8) the owner check gains a host-guest exception (a guest under the owner's `UnderTheClothEffect`) with a RED. A per-bed "lend it" verb is enacted and voice-sensitive; not v1.

### 9.5 Never homeable
Catacomb niches, plaques, the hearth patch, the Founding Plume, Choir groves/tendrils/seeps, the tenth fire, the woven doll, the Felling-Site, the Overwrit, the Sealed Library, Glassblown sites, Bower installations (N-9, C3-C5, C10): excluded by zone class and biome, then by protection capabilities as they come to exist. A player fixture never resembles or explains the tenth fire (N-10).

### 9.6 The later terms layer (PB10; gated; designed only enough that v1 does not preclude it)

When a *person* is the counterparty (a Concord lease, a Tent-Right gift, a Curation filing): carried through the existing offer seam (`ConversationManager` append hooks, `ConversationManager.cs:227-231`, `ConcordAccount.AppendChoices` [P]) so existing cached NPCs offer it with **no zone retrofit**; offer rows as data keyed by giver capability and `Tender.For(giver)`; per-faction forms: Concord bay lease (drams through `Ledger` with a named sink), Tent-Right gift (no fee; refuses only oathbreakers), Curation file (withheld service shown as a reason), ruin release.

Standing rules for it: prepaid, non-compounding, due at arrival through `EconomyUpkeep`-style reconcile; **lapse suspends new work and new storing but never blocks take-out, output collection or take-down**; never seizes or deletes; counterparties by entity id with `Bound()`-style revalidation (dead or replaced counterparty resolves to a defined state, never to destruction); Option P closure (8.7); relay telemetry only for places that hold a relay endpoint; one zone-local terms record shared by several fixtures **only if** several share one term. The spec reserves `ServiceTermsPart.Access = "Owner"` with `OwnerID` and makes an unregistered `Owner` **fail closed** (C7, hook 2). Registering `Owner` (the owner and invited ids, enforced through `IStationAccessGuard`) and the spoken-end verb are **this design's job**, delivered only in PB10; until then a held fixture carries no terms Part. Gates before any slice here: the independent explicit-work-permission design critique (G2), PB0 proven in play, the spilled-contents decision (6.4), and the site-sync trace. Geography caveat: Concord keepers exist only at Cinderhold and Last Counter (`LandmarkBuilder.cs:964-1015` [P]), and the one relocatable machine (the rig, at 17.7) cannot realistically reach a bay, so H5-style acceptance must be honest about that.

---

## 10. Machine integration

The base is a place a machine can stand, not a machine. It adds verbs, retention, readers and the derived list; the machine program decides which stations are installable and what a carried frame can support.

| Topic | Design |
|---|---|
| Station parity | One station query serves communal and held instances identically (M-4, [MC-Station]); PB5 proves it with the same recipes, previews, costs and readiness at the Peat Works rig and a relocated rig. |
| Take-up while working | Vetoed while escrow exists (`job-escrow`, [MC-Work]); stateless kinds are takeable at once. |
| Storage | Ordinary `ContainerPart`, small `MaxItems`; carried kinds opt into `ContainerLoadPart` (no weight laundering: `HandlingService.cs:48-57`, `SecondExplorationObjects.cs:72-96` [P]); standing weight is free as a chest's is; no global stash, no remote deposit, no loadout retrieval (N-7). |
| Rest | Existing Bed rest, unchanged (full heal, +60 ticks or next band, hostile-within-8 refusal, no `WellRested`). A portable bed is gated by D-H1: no player verb places a campfire today, so wild rest exists only at waysites (grep [P]). |
| Farming plots | Already retain their zone (`CultivatedSoilPart`, `RepairCultivationSite.cs:109` [P]) and grow by the wet-only `CropTime` rule; a place beside water can garden with no new code. A portable planter is not v1. |
| Kitchen | Zone-pinned Parts cannot be set down; a place cooks only with existing fires. |
| Survey (S3) | A carried stake on bare ground is the surface-shaped second base; underground placement is PB7. Canon nouns: Recension "sounding-rod", Pale Curation "survey stake", Tent-Right "water-finder's rod". |
| Relay (S4) | See 4.2; the place contributes the address (entity ids, never zone names) and persistence. |
| Thermal | Convenience and processing only (N-11); fuel escrowed for a bounded job or the fixture sits stopped; nothing burns while away (M-13). |

Class guidance for the twelve machines (the machine program owns the final classes; [I]):

| Machine | Likely class | What the base layer needs from it |
|---|---|---|
| 1 Forge and reclamation rig | hauled (rig 120, verified) / fixed at towns | nothing new; marker on crossing |
| 2 Fractionating still | hauled or fixed (liquid in tank) | take-up vetoed while a job or contents exist |
| 3 Restoration cradle | hauled | veto while component slots hold escrow |
| 4 Assay cabinet | carried | a stateless kind: candidate for PB6 |
| 5 Culture vat | hauled or fixed | living contents; visible state; never carried |
| 6 Capstan | parked | none |
| 7 Sump pump | fixed (needs a bounded reservoir and an authored space) | not a kit |
| 8 Thermal exchanger | hauled (modest fuel version); strong versions at unusual authored places | fuel escrow; N-11 reading |
| 9 Dynamo and charging bench | carried (hand power) / hauled (fuel) | cell custody is the machine program's |
| 10 Survey resonator | carried stake | instrument limits (L1, L11) |
| 11 Signal relay | fixed tower (communal, authored) + carried endpoint | goods in one saved entity; honest reports |
| 12 Treatment bath | fixed or hauled | normal healing stays free |

---

## 11. Safety, economy, custody

### 11.1 Threat model (what a place inherits and does not change)
- Hostiles are where they were left and act only while the player is present; whether they path through, or damage, a fixture is **[U]**: nothing advertises doors, walls or fixtures as protection (N-16, D13).
- `Rest` is refused with a hostile within 8 and no line-of-sight test [P]; unchanged. A place is never a safe-sleep zone.
- Fire: wood and cloth fixtures can burn while the player is there (MaterialSim ticks the active zone only, `InputHandler.cs:1320` [P]). That is honest local consequence, not upkeep.
- Wild creatures that pick up items (`SpreadCollectorPart` takes only named blueprints Hatchet, Cudgel, LeatherBoots; `SpreadPredatorPart` only raw or dried meat) already exclude `Owned`, `NoTake`, containers and non-Takeable solids [V]; a standing box is excluded three ways.

### 11.2 Exploit table

| Attempt | Why it fails or what it costs |
|---|---|
| install -> save -> reload -> install duplicates | same entity; no destroy-and-recreate; test pins ID, contents, escrow |
| refill by leaving and re-entering | retention plus no regeneration; stock/offcut/clay counts identical (M-6, N-5) |
| output on reload | no output created at load, generation or hydration; the base has no outputs |
| weight laundering via a carried box | carried kinds must opt into `ContainerLoadPart`; counter-test with and without |
| teleporting goods between places | no remote deposit or withdraw; carriage or an arranged delivery only (N-7) |
| set-down/take-up loop for free rest or heal | no output; rest is the existing rest; bed gated by D-H1 |
| plugging a corridor or penning a creature | `open-neighbours`; fixtures are destructible |
| blocking a border lane or stairs | `edge-margin`, `stairs-doors` on saved data |
| sitting on protected ground | zone class, biome, protection capabilities |
| claiming land | there is none; only instances carry anything |
| mint | no coin, salt or unit conversion anywhere (N-12); kits are ordinary goods, resale at the trader's normal spread |
| cap evasion by hand-over | hand-over returns the slot but leaves the entity; bought kits are priced goods, so growth is self-limiting; the world still holds every entity (same as dropped items today) |
| take-up that loses contents | refused with the load named, never partial |

### 11.3 Economy
Kits cost drams or barter at traders through existing stock tables, `Tender.For` and `Ledger`. No upkeep, fee, rent or timer. A fee exists only with a person counterparty in PB10: through `Ledger`, due at arrival, non-compounding, lapse suspends and never seizes (M-21). Price is set by the economy owner through the standard value formula; a kit must never be cheaper than the sum of any finite materials it later requires.

### 11.4 Performance (CLAUDE.md requires this section; patterns used)
- `CanUnloadZone` clause: O(markers + creatures of one zone), only inside `UnloadZone` (today only the village relapse path). No allocation beyond the existing part-index list; the creature scan (`GetEntitiesWithTag`) allocates only inside `UnloadZone`, never per frame or per turn.
- Menu rows: `PlacementCheck` x up to 8 per menu open; the zone class is computed once per call and not cached across calls. Not a per-frame path.
- List: on demand; first-use seeding of a zone's part bucket is one scan of that zone's entities; PB3 profiles it.
- No new `Update`/`LateUpdate`, no per-turn listener, no new cache, no `ZoneRenderHooks` change except the existing `MarkCellDirty` the placement already triggers.
- Caps (8/32) and the 300-zone synthetic profile belong to PB1 and PB3.

### 11.5 Diag vocabulary (every gate emits one record, default-on categories only; M-24)

| Category | Kind | Payload |
|---|---|---|
| `furniture` | `SetDown` | `fixtureId, kind, zoneId, x, y, zoneClass` |
| `furniture` | `SetDownRefused` | `fixtureId, kind, reason, others` |
| `furniture` | `TakenUp` | `fixtureId, kind, zoneId, contents` |
| `furniture` | `TakeUpRefused` | `fixtureId, reason` (`unknown-kind`, `overweight-load`, `job-escrow`, `stale-selection`, `in-progress`) |
| `furniture` | `HandedOver` | `fixtureId, kind, zoneId, spoken:true, contents` |
| `worldgen` | `ZoneRetained` (the spec's record) | `zone, reason (marker \| legacy-blueprint \| staying-companion), count` |
| `closure` | (PB10 only, ledger-backed) | per `StoryletPart.EmitClosure` |
| `trade` | `PlaceTermsPaid` (PB10) | `payer, payee, amount, sink` |

Tests pin every emission (the canonical pattern: `Diag.ResetAll()`, act, `DiagQuery.Apply`).

---

## 12. Readers, voice and honest limits

- Examine/F1 derive from the same state as the action: one `PlacementCheck` result feeds preview, action and diag. `PackableFixturePart : IDescribeState` (spec C6) contributes the `State` line ("you set this down N days ago"), the `Limits` line (nothing guards it; it can be destroyed; nothing here changes while you are away) and, for a carried kit, the `Uses` line; before the spec's S5 lands, a `Describe()` line plus the existing Examine builders (`ExaminablePart.cs:132-141` [P]).
- Intent cards (not strings) are required for: kit Examine; set-down preview and each refusal reason; standing-fixture reader; take-up refusal reasons; hand-over confirmation; list entry; rig line. Each passes the fresh-context blind gate (R10); no thesis sentences or design-doc register; no Choir lexicon; technology nouns re-grounded first (L16).
- The unverified-gate honesty rule: if the `protected-near` check is dormant, no string claims "no protected ground nearby".
- Remembered fixtures show current state (no last-seen frame exists); acceptable because nothing changes at a held place while away.
- No new art is required (N-22); new blueprints use unique glyph and colour pairs recorded as a disclosed fallback (the machine program's S6 rule).

---

## 13. Ordered slice list

Each slice is individually shippable, RED-first, with counter-checks and a `HeldPlaces*AdversarialTests.cs` file. Adversarial surfaces: state atomicity and rollback, save/load reflection, cross-actor flows (companion, killed or replaced counterparty), anti-exploit (duplication, weight laundering, relapse regeneration), stacking (two fixtures one cell), boundary inputs (null actor, zone, manager; overflow ticks), diag dispatch (one reason per refusal). Any player-visible slice also owes actual GameBootstrap play with isolated saves, an independent source and gameplay review, and a lore and voice review of every string. Native tests alone are not acceptance (`MACHINE-SYSTEMS-DESIGN.md:99,138`).

| ID | Slice | Cycles | Depends on machine contracts |
|---|---|---|---|
| **PB0** | Retention addenda to the spec's S1-S2: second relapse source, staying-companion conjunct, hospitality source pin | 1-2 (plus S1-S2 if not landed) | MC-Marker, MC-Save |
| **PB1** | Zone classifier, `IsEligible` extraction with sweep, `PlacementCheck` | 2 | none (protection tags if present) |
| **PB1a** | Read-only audits (six questions) | 1 | none |
| **PB2a** | Same-entity set-down/take-up/hand-over transaction and verbs (joint with the spec's S19; no player-visible kit) | 2 | MC-Install, MC-Cost (fallback chain clause), MC-Save |
| **PB2b** | The camp box and its sourcing | 1-2 | PB1a |
| **PB3** | Readers, derived list, hand-over surface, diag, source pins | 2 | MC-Describe (fallback `Describe()`) |
| **PB4** | Live retention bench and actual-play acceptance of PB0-PB3 | 1 (play) | none new |
| **PB5** | The rig as a held fixture (list, reader, hand-over, parity) | 2 | MC-Install, MC-Station, spec S19 |
| **PB6** | A second, **stateless** fixture kind in a second zone | 1-2 | MC-Station (spec S4) and the spec's S13b `StationPart` cold bench (or the assay cabinet) |
| **PB7** | Ordinary-column placement class | 2 + audit | PB1a |
| **PB8** | Portable bed (decision-gated) | 1-2 | D-H1 |
| **PB9** | Clusters, break-camp, one-border move for opt-in hauled kinds | 2 | MC-Install |
| **PB10** | Person-held terms layer | 2 + critique | MC-Terms, G2 |
| **PB11** | Relay and survey integration | per machine | MC-Work, MC-Receiver |
| **PB12** | Village-adjacent placement under terms | 2 | PB10, 6.4 decision, site-sync trace |

Estimated 11-13 cycles plus one actual-play pass for PB0-PB6.

### PB0. Retention addenda to the spec's S1-S2 (home's slice 1)

**Goal.** The spec's S2 closes the relapse hole for fixtures; PB0 adds what home needs that S2 does not cover. No player-visible feature; zero change when no player marker, staying companion or home Part exists.

**Step 0 (read, five minutes).** Check HEAD for `InstallationMarkerPart` and `SavedPartContract.txt`. If the spec's S1 and S2 have **not** landed, they are on home's critical path: build them exactly as the spec's C1, C2, S1 and S2 write them (do not vary any spelling; section 2) and run S2's Step 0 premise test first (`RelapseRegeneratesUnmarkedVillage_Control`, using `OverworldZoneManager.CreateDetached(scope.Factory, 64, true)` per the spec; fall back to `new OverworldZoneManager(null, seed)` plus `ReplaceLoadedState(zones, activeId, connections)`, `ConnectedTimberPalletTests.cs:139-145` [V]). If the second `GetZone` returns the **same** instance, the hazard is narrower than claimed: record it and narrow PB0b accordingly. Then do PB0a-PB0c.

**PB0a. Second relapse source (a test-only extension of S2; request R1).** `AdvanceSettlement` returns changed from two branches: a well-keeper renewal (`SettlementManager.cs:102-112`) and a relapse to Fouled (`:113-130`) [V]; both unload a non-active village (`OverworldZoneManager.cs:1487-1489` [V]). S2's RED list names only the relapse. Add `WellKeeperRenewalDoesNotUnloadMarkedVillage` with its unmarked control. The renewal needs `WellKeeperPurification.Condition` on the settlement state and a Well site in `TemporarilyPurified` past `RelapseAtTurn`; set both through the same `SettlementManager` calls `SettlementManagerTests` and `PlaceSiteTests` use (`PlaceSiteTests.cs:119-122` [V]).

**PB0b. Staying-companion conjunct (its own commit; behaviour change D-H2).**

Files: NEW `Assets/Scripts/Gameplay/Installations/StayingCompanionRetention.cs`; MOD `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs` (one conjunct appended after S2's); NEW `Assets/Tests/EditMode/Gameplay/Installations/InstallationCompanionRetentionTests.cs` and `InstallationCompanionRetentionAdversarialTests.cs` (namespace `CavesOfOoo.Tests`; naming per spec section 3).

```csharp
namespace CavesOfOoo.Core {
    public static class StayingCompanionRetention {
        // Called only from CanUnloadZone, i.e. inside UnloadZone (today: the village relapse path).
        public static bool Retain(ZoneManager manager, string zoneId) {
            if (manager == null || string.IsNullOrEmpty(zoneId) || !manager.CachedZones.TryGetValue(zoneId, out var zone)) return false;
            var creatures = zone.GetEntitiesWithTag("Creature");          // allocates only here
            for (int i = 0; i < creatures.Count; i++) {
                if (!CompanionOrders.IsStaying(creatures[i])) continue;    // CompanionOrders.cs:17-25 [V]
                if (Diag.IsChannelEnabled("worldgen"))
                    Diag.Record("worldgen", "ZoneRetained", target: creatures[i],
                        payload: new { zone = zoneId, reason = "staying-companion", count = 1 });
                return true;
            }
            return false;
        }
    }
}
// OverworldZoneManager.CanUnloadZone, appended after the spec's S2 term:
//   && !StayingCompanionRetention.Retain(this, zoneID)
```

RED (write the control first; it passes today and proves the harness can fail):
1. `ZoneWithoutStayingCompanionStillUnloads` (control).
2. `StayingCompanionRetainsZone` (RED today): a real `RecruitedEffect` with `StayHere`, recruiter, `PartyLeader` and `PartyMembers` links as `IsStaying` requires; after `UnloadZone` the zone object, the companion's entity ID and position are unchanged.
3. `FollowingCompanionDoesNotRetain` (`StayHere == false`).
4. `ExpiredOrDismissedCompanionDoesNotRetain` (`Duration == 0`, recruiter missing, party link broken).
5. `StayingCompanionInAnotherZoneDoesNotRetainThis` (M-7).
6. `CompanionDeathOrRemovalReleasesRetention`.
7. `ZoneRetainedDiagCarriesStayingCompanionReason` and `NoDiagWhenNothingRetains`.
8. `RetentionSurvivesSessionRoundTrip` (the order is saved on `RecruitedEffect`, `RecruitedEffect.cs:51` [P]).
9. Independence of the two conjuncts: marker only, companion only, both, neither (four rows).
10. Adversarial: null manager; null or unknown zone id; a dead creature carrying a stale StayHere effect; two staying companions (one record); `Retain` called repeatedly (idempotent, mutates nothing); the active zone.

**PB0c. Hospitality source pin (characterization; passes today).** `UnderTheClothEffect` is constructed only in `ClothClaims.cs:73`, `Hospitality_ExtendCloth.cs:79` and `ConversationActions.cs:818` [V]. A test greps the source tree and fails if a fourth file appears (N-8). Test-only.

**PB0d (waits for S19).** `LooseMarkerDoesNotRetain` if R2 is accepted.

**Acceptance.** Native green; the spec's S2 suite and the existing retention suites (`SecondExploration*Retention*`, `SoddenDistrictIntegration`, `CurationIntake`) unchanged and green; `diag_assert category=worldgen kind=ZoneRetained` with each reason; no gameplay change in the decline path; the compile-verification ladder (grep the Editor log for `error CS`) before trusting a green run. Live acceptance of the companion row belongs to PB4.

**Risk.** Low. PB0b is the only change a player could notice: a village holding a staying companion stops refreshing build-time stock on relapse (D-H2). Commit it alone so it reverts alone.

### PB1. Zone classifier and `PlacementCheck`

- **Goal.** One classifier and one pure `Evaluate` (7.1, 7.2); extract `WaysitePlan.IsEligibleIgnoringFamily` and `IsFamilyOwned`, re-express `IsEligible`.
- **RED.** One table-driven test per reason code with a counter-case; placement after a save round trip onto a cell that was reserved at generation is decided by saved entities only (N-13); the biome allow-list excludes Grovelands and Overwrit; `IsEligible` equals the conjunction on a seed sweep (three seeds x every cell) and the old behaviour is unchanged; the protected-ground set agrees with `AuthoredWildernessZoneIDs` and the pilot zone; the ground predicate passes Grass, Floor, StoneFloor and refuses RoadStone and every hazard in the predator list; cap counting by scan; a 300-zone synthetic profile of the cap scan.
- **Risk.** Medium: protection tags may be sparse at HEAD [U]; record what exists and make the dormancy visible. Extraction touches accepted generation code; the sweep guards it.

### PB1a. Read-only audits (one page of decisions, no code)

1. **Kit sourcing** (answered in part, 8.1): which Concord/Quartermaster tables carry the line; scripted probe that a restocked shelf draws it.
2. **Exploration-family runtime consumers:** enumerate every family selector (Hauling, Hunt, Passage, Worksites, Cooking, `SecondExplorationActions`, `ConnectedSpreadSatellite`) as reads-ordinary-owners-at-runtime / honours-`Owned` / generation-only; lift `family-owned` iff each honours `Owned` or is generation-only.
3. **Hostile reach:** can a hostile damage or path through a Solid fixture or closed door.
4. **Protection tags:** which blueprints and tags exist for niches, hearth patch, plaque walls, Choir groves, tendrils, seeps, Bower installations, and which wild zones border them.
5. **Beating sand** as plain ground (actual play).
6. **List surface:** where the journal-style surface hooks a list (`QuestLogStateBuilder.cs:115-126` [P]) and which existing resolver names a zone for display.

### PB2a. Same-entity transaction and verbs (joint with the spec's S19)

- **Goal.** `HeldFixtureActions` (set down / take up / hand over) and `InstallationTransaction` on the spec's S19 Parts and events. No kit blueprint; tests use a test-only blueprint. If S19 has not started, PB2a and S19 ship as one two-cycle slice (R4); the Part-level tests S19 lists (`OldSaveWithoutPartIsNotInstallable`, `InstallMarkerRoundTrip`, `UninstallReleasesRetention`, `RebindFailureRollsBackInstall`, `LooseVetoesReadiness`) belong to it.
- **Cost wiring (verified, section 1 C8).** Set-down is a pack action: register `HeldFixtureActions` in `CombatUtilityActions.IsCommand/AddActions/TryAct` (three one-line edits). Take-up and hand-over are world-menu actions on a standing fixture: declare `ActionTime.OneAction` if S3 has landed; otherwise add `|| HeldFixtureActions.IsCommand(action.Command)` beside `WorldResourceActions.IsCommand` in the world chain (`InputHandler.cs:3469`, the same additive pattern). Once S3 has landed, a failed placement reason is also shown as a disabled row.
- **RED.**
  1. `SetDownKeepsEntityIdContentsAndEscrow`.
  2. `TakeUpIsConservingAndRefusesOverload`.
  3. `ForcedFailureAtEachStepRestoresPackCellFlagsTagsAndParts` (one forced failure per row of 8.2).
  4. `InstallSaveReloadInstallDoesNotDuplicate`.
  5. `CarriedBoxWithoutContainerLoadLaundersWeight` (counter) and, with the part, contents weigh.
  6. `TwoFixturesOneCellRefused`; `StaleSelectionRefused`; `CapRefusesNinthFixture`.
  7. `OwnedTagSetWhileInstalledAndClearedOnTakeUpAndHandOver`.
  8. `TakeUpWithoutAChainClauseHasNoTransaction` (a RED that **demonstrates the hazard**, then GREEN once the clause or S3 exists).
  9. `PackSetDownChargesOnceOnCommitAndNeverOnRefusal` and `WorldTakeUpChargesOnceOnCommitAndNeverOnRefusal`.
  10. `BeforeInstallVetoRefusesWithItsReason`; `BeforeUninstallVetoRefusesWhileEscrowExists` (R3).
  11. `StandingFixtureOffersTakeUpAdjacent` (the first actual-play probe if EditMode cannot reach that menu).
  12. `HandOverNeedsNoDefinition`; `TakeUpOfUnknownKindRefuses`.
  13. `TakableFlipAudit`: an adversarial list over the `Takeable` consumers (pickup, drag/handling, trade/sell, inventory actions, ground-item display, `portable_bed`, stacker); 330 read sites in 201 files per the judges [P].
  14. `PackableFixturePartPassesTheS1HygieneLint` and `PackableFixturePartRoundTripsItsPublicFields`.
  15. **Zero footprint when declined (M-3):** `SessionWithNoSetDownsHasNoNewSaveFootprint` (a `GameSessionState.Capture` of an untouched session adds no home Part, property or ledger; null stays null) and `OldSessionWithoutHomePartsRoundTripsUnchanged` (zone entity tuples equal; a pre-feature v8 save loads, plays, saves and reloads, M-25).
- **Precedent.** `RecoverableLampPart.Apply` flips `Takeable` and weight with rollback (`SecondExplorationObjects.cs:7-31` [P]).
- **Risk.** Medium: new code on a shared custody path; touches two accepted files additively (`CombatUtilityActions`, `InputHandler`).

### PB2b. The camp box

- **Goal.** A small container kit: blueprint (`Takeable` while carried, `ContainerPart` small `MaxItems`, `ContainerLoadPart`, destructible like a chest, `InstallablePart` Portable/Loose, `PackableFixturePart`), a stock line per PB1a, glyph fallback recorded (no art; N-22).
- **Acceptance.** Native plus adversarial; fresh-world purchase; old-world reach per PB1a; a stock line for a missing blueprint is skipped (reduced content pack).
- **Risk.** Medium: first player-visible kit; copy needs voice cards.

### PB3. Readers, derived list, hand-over surface

- **Goal.** Examine/F1 from one state for kit and standing fixture; the list (scan, appears only while a marker exists); hand-over confirmation; diag; the M-2 source pin (a grep-based test fails on any consumer of `HeldPlaces.Enumerate` outside the readers and the list).
- **RED.** Reader versus action parity for each refusal; list rebuild after destroying, moving, and deleting the only fixture (list empties, entry vanishes); no entry for communal or authored fixtures; no record on walk-away; the list profile against the synthetic world.

### PB4. Live retention bench and actual-play acceptance (PB0-PB3)

**Deterministic self-auditing bench** (`Scenarios/Custom/HeldRetentionBench.cs`, CLAUDE.md rules 1-8): `Apply()` forces the stimulus programmatically; snapshot, stimulate, measure, restore; **a control row per class**; preconditions guarded **loudly** (`RetentionBenchSkipped`, never a neutral-looking pass); one machine-checkable `furniture/RetentionBenchCell` record per cell; confounds neutralised and documented; a per-run `runId` plus a `MatrixAuditRun` marker (the diag buffer outlives a Play session, so readers must scope to the newest run); validate live before merge, since the EditMode smoke only proves `Apply()` does not throw.

Matrix: zone class {relapsing village (Tine Weir 150, Cinderhold Pump 600), non-relapsing village, wild surface, retained authored zone (Wellmeet), family-owned} x holder {none (control), marker, marker released, staying companion, both}. Each cell measures zone object identity, holder ID, and the sorted (ID, blueprint, quantity, condition) tuples of contents before and after a forced relapse, a clock advance, a leave and a return. Expected matrix is data.

**Actual play, isolated saves.** Fresh world; buy or be given the box; set it down in a wild zone; fill it; leave across a long clock jump (many days of travel and rest); return; F5; fresh Continue; take it up; set it down elsewhere; hand it over. Cached-save control (old zone untouched); decline path (no new Part, property or text); the village relapse case through the test route; staying companion in a relapsing village. Honest-limits report: hostile and door behaviour unverified, no map markers, no construction, no stairs cargo, no Linux or Deck execution, no full-area claim.

### PB5. The rig as a held fixture

- **Goal.** After the spec's S19 stamps `Origin="Relocated"` on a committed crossing: list entry, reader line, hand-over, parity; retention of the rig wherever hauled already comes from the spec's legacy list; `Payload` tolerates the marker (named test); `RigRelocation.Prepare` still succeeds after an install/take-down cycle.
- **RED.** Same recipes, previews, costs and readiness at the communal Peat Works rig and a relocated rig in a different zone (M-4); reclaim for exactly 2 timber still works and drops the marker with the entity; hauling, repair, refitting, crossing suites unchanged; retention of a rig crossed into an evictable zone (benefit check).
- **Risk.** Medium: additive touch on accepted rig code; if the relapse reachability check shows the rig cannot reach a relapsing village, the claim narrows to wild zones and is recorded.

### PB6. A second, stateless fixture kind in a second zone

- **Goal.** The first machine-program installable (the spec's S13b `StationPart` cold bench, blueprint `TinkersBench`, with `InstallablePart.Mode=Portable`; or the assay cabinet) through the same path, to meet "more than one instance and location, a useful ordinary alternative, a cross-system consequence" (`SYSTEMS-DESIGN-PRINCIPLES.md:15`). Stateless, so no MC-Work veto is required yet.

### PB7. Ordinary-column placement class

- **Goal.** Admit `ordinary-column` zones (biome allow-list; Grovelands and Overwrit columns excluded). Audit first: reachability of an ordinary column from the surface, hazards, populations (`PopulationTable.UndergroundTier`), lairs, `CaveClimbOutStairsBuilder` climb-outs (`OverworldZoneManager.cs:617`), `UndergroundRouteReservationBuilder`.
- **RED.** Refusals for Sinkhole/Root columns, authored columns, Grovelands-surface columns, roomed versus natural layouts; placement after load; the underground cell predicate (floors are not Grass/Floor/StoneFloor, which is why the ground predicate is property-based).

### PB8-PB12 (summaries)
- **PB8 portable bed (D-H1).** Two-round set-down (interrupted effort stays spent), the host-guest exception on `BedPart.Owner`, rest identical to existing rest, `furniture` diag counting rests per outing so the balance question is measured. Waysite beds stay communal and unmovable.
- **PB9 clusters and break-camp.** A macro that runs atomic take-ups and stops at the first refusal; generalise the M1c one-border contract only to hauled kinds that opt in, with a refusal matrix for stairs, diagonals and world-map cargo.
- **PB10-PB12.** Section 9.6 and 6.4.

---

## 14. Coverage of every MUST and NEVER

### MUST
| ID | How satisfied |
|---|---|
| M-1 | no machine loop reads the feature; communal installs untouched |
| M-2 | source pin: the held-place query has no consumer outside readers and the list |
| M-3 | no ledger anywhere; null stays null; PB2a save-footprint test |
| M-4 | one station query serves both; PB5 parity |
| M-5 | the spec's S2 clause (6.2) plus PB0a/PB0b rows and the PB4 live bench, control included |
| M-6 | same entity, retention, no regeneration; tuple comparison across leave/return/F5/Continue (PB4) |
| M-7 | retention is Part presence; two-zone and released-marker tests |
| M-8 | no time logic in the base; nothing simulates away |
| M-9 | the base has no reconcile; fixture reconcile is MC-Work's and must be idempotent and credit before output |
| M-10 | enormous-clock test (PB4); caps are MC-Work's; no base timers |
| M-11 | no decay, cooling or fuel burn for held things; 10x clock test |
| M-12 | no change to `RestSystem`, drag/haul/pack limits, repair, free waysite rest; no `WellRested`; golden comparison |
| M-13 | take-up vetoed with escrow; stopped state otherwise |
| M-14 | same entity, atomic install, conservation, rollback tests (PB2a) |
| M-15 | `PlacementCheck` on saved data only; refusal in preview and diag; caps |
| M-16 | only the rig, only M1c; every other class refuses honestly |
| M-17 | `Manifest` explicit and finite; no tag-inferred yield |
| M-18 | terms veto only when MC-Terms has landed; self-held fixtures need none; no private permission model |
| M-19 | counterparties by entity id with revalidation (PB10); dead or removed resolves to a defined state |
| M-20 | take-up and hand-over record `furniture` kinds with `spoken:true`; `closure` reserved for ledger-backed terms (C4); walk-away legal, no penalty |
| M-21 | no fee in v1; later through `Ledger`, due at arrival, bounded, lapse suspends |
| M-22 | zone class, biome, protection capabilities; per-class tests |
| M-23 | intent cards; every string passes the blind gate |
| M-24 | one state feeds preview, action, reader and diag; default-on categories only |
| M-25 | FormatVersion 8; additive Parts; old-save fixture; zone-content diff |
| M-26 | the list is a scan; nothing to drift |
| M-27 | unsupported matrix (7.3) |
| M-28 | writes nothing to a non-active zone; relay constraints held on the machine program |
| M-29 | no private permission model; terms deferred to the critique |

### NEVER
| ID | How held |
|---|---|
| N-1, N-2 | no consumer reads it; no prompt, counter or one-time popup |
| N-3, N-4, N-5, N-6 | nothing time-driven touches held things; retention, no regeneration, no output at load |
| N-7 | no remote anything |
| N-8 | three-file allow-list source pin |
| N-9, N-10 | excluded by zone, biome, protection checks; no base text explains a ledgered mystery |
| N-11 | no ordinary alternative degraded; no cold or shelter need; bed gated |
| N-12, N-13 | no unit conversion or mint; no ZoneID/settlement registry; FormatVersion 8 |
| N-14 | no retrofit; lazy attach only at the player's act |
| N-15 | no property-crime system; `Owned` is an exclusion convention, never a punishment |
| N-16 | nothing promises safety |
| N-17, N-18 | no residents, labour, levels or tiers |
| N-19, N-20 | player copy avoids Choir lexicon; `HouseDramaData.Home`, `QuietDistilleryHomestead` are not prior art |
| N-21, N-22 | no respawn point; no construction, roof, tent or new art required |

---

## 15. What each decision unlocks

| Decision | Without it |
|---|---|
| D-H9 lift the handoff gate | nothing starts |
| D-H2 accept the PB0b behaviour change | PB0 ships without PB0b |
| D-H1 portable bed | PB8 stays withheld; every other slice ships |
| D-H3 old-world sourcing (trickle, no grant) | PB2b ships fresh-world only |
| D-H6 Stump and the allow-list | Stump drops from v1 |

---

## 16. Decisions for the user or lore owner, risks, unverified

### Decisions
- **D-H1 Rest anywhere.** A portable bed gives a free full heal wherever no hostile is within 8. Recommendation: ship the stash and work first; add the bed later with a two-round set-down and instrumented rests per outing.
- **D-H2 Village retention behaviour change.** A village that holds a fixture or a staying companion stops regenerating on relapse, so its build-time stock no longer refreshes by relapse (and its containers no longer refill). Recommendation: accept; ship PB0b as its own commit.
- **D-H3 Old-world kit sourcing.** Recommendation: the slow shelf-restock trickle; no per-character grant; never a free item at load.
- **D-H4 Nouns and voice.** "Set down", "take up", "hand over" are working labels; the lore owner assigns canon words per faction.
- **D-H5 Self-held abandonment.** Recommendation: no gradient and no event.
- **D-H6 Choir-law and Stump.** Confirm the Grovelands and Overwrit exclusion; confirm Stump placement, and whether a no-fire fixture may ever stand in Choir-law ground.
- **D-H7 "keeps ordinary homes viable."** Recommendation: convenience and processing, never a cold or shelter need (N-11).
- **D-H8 Caps.** 8 per zone and 32 per world are provisional; profile before raising.
- **D-H9 The standing hold** ("No new runtime changes", spec owner decision 1) must be lifted for PB0 and sequenced with the machine program's S1-S2 and S19.
- **D-H10 Wagon.** Recommendation: wait for a cargo contract.
- **D-H11 Village placement at all** (PB12) and under which resident grants.

### Risks
1. Home now depends on the spec: R1 (renewal row in S2), R3 (`BeforeUninstall` veto), R2 (optional Loose hardening) are small amendments; if declined, the fallbacks in section 2 apply. The spec's own gate (the "no new runtime changes" hold) blocks everything.
2. Rest-anywhere (D-H1).
3. `Takeable` flip reaches 330 read sites in 201 files [P]; the adversarial consumer list mitigates.
4. No native art for new blueprints: glyph fallback is disclosed.
5. Old-world sourcing is slow (D-H3).
6. Protection tags are sparse at HEAD; zone and biome exclusions carry v1.
7. Village retention and the spilled-contents case (6.4).
8. `ContainerLoadPart` has only been exercised on authored cargo; carried-contents weight must be proven in PB2a.
9. The family-owned lift depends on PB1a's consumer audit.
10. Stale or conflicting machine spelling after the spec lands.
11. The EditMode harness may not reach `PrepareZoneForAccess`; the spec's S2 Step 0 premise test and the live bench are the fallbacks.
12. Until the spec's S3 lands, take-up and hand-over need one additive clause in the world chain (`InputHandler.cs:3469`); a command in no chain silently loses its transaction.
13. The spec's `Retains` does not check `InstallablePart.State`; a stale Player marker on a Loose entity would retain a zone (R2 closes it).

### Unverified [U]
Hostile pathing and damage against fixtures; Beating sand as plain ground; which protection tags exist; whether `Takeable=false` fixtures reach the adjacent-interact menu (PB2a probe); the exact place the drop command places an entity (PB2a); save-size growth; older-binary behaviour on new Part classes; whether the EditMode harness reaches the relapse path; the exact `GetInventoryActions` route that offers verbs on a non-Takeable standing fixture in the world menu (PB2a probe); whether any retention predicate misbehaves with a null factory.

---

## 17. Not now

- A wagon or cart; any hauled container beyond the rig.
- A pitched tent, walls, a roof, a room, shelter, `IsInterior` derivation, a construction mode, new art.
- Underground placement (before PB7); village, catacomb, Choir, Bower, Curation or any POI placement (before PB11/PB12).
- World-map cargo, stairs cargo, cross-zone hauling of arbitrary loads, raising any weight or limit.
- A global stash, remote deposit or withdrawal, loadout retrieval, teleporting or relayed goods without a physical custodian.
- Residents, labour, schedules, passive production, NPC use of a held machine.
- Automating, granting or pricing hospitality; lending a bed to a guest.
- A safe-sleep, ward, defence or hostile-exclusion mechanic; `WellRested` from a place.
- Theft, trespass, per-place reputation or a theft meaning for `Owned`; leases and fees (before PB10 and its critique).
- Map markers; a stored ledger of places; a tier, level, upgrade tree or stat.
- A respawn point; kit crafting recipes; kit art; Linux or Steam Deck claims.
- Kitchens and other zone-pinned Parts as installable fixtures.
- A "base" quest, achievement, tutorial, nag or counter.
