# SETTLEMENT PLACEMENT PLAN: where each machine goes, in what order, and why

Status: design only. Nothing was run, built, compiled or written to the project. Repo `/Users/steven/caves-of-ooo`, HEAD `c0c65d6f9` plus an uncommitted working tree.

Evidence tags: **[V]** I read the cited line this session. **[R]** taken from `ARCHITECTURE-SPEC.md`, a `machine-*.md` dossier, `lore.md` or a `settlement-*.md` report, not re-read. **[I]** my inference or design. **[U]** unverified; the named step settles it. Lore authority is root `Lore/` only (via `lore.md`); `Docs/Lore/` is not used.

Inputs: `ARCHITECTURE-SPEC.md`, the twelve `machine-*.md` dossiers, `lore.md` sections 5-7, `enumerate.md`, `placement.md` section 9-10, `presentation.md`, and the 20 `settlement-*.md` reports (fit tables, candidate sites, do-not-use cells). Standing gate: `CLAUDE.md` still says "No new runtime changes"; every wave below needs the owner to lift that hold, and the S10/S12 critique gates stay.

---

## 0. Bottom line (read this first)

1. **The honest answer to "some machines in every town" is a ratchet, not a switch.** Of the twelve machines, exactly one has a purpose-qualified, shippable town placement today (the forge, three towns; the steeping trough is conditional at one town). Every other machine's dossier says "no honest purpose today" or "needs a new system". The plan therefore does three things at once: ships the forge where it has a verified local gap (Wave 1); gives each remaining town its *own best-fit machine* behind named gates (Waves 2-3), so no town is given a padded or fake service; and records canon exemptions with cited sources so "every town" is a measurable census, not a claim.
2. **Wave 1 is small and low-regret.** First visit to a town in any world bakes its layout; a visited town never gains or loses a machine (spec C3, old-world rule). So a row that is wrong is permanent in every save that visited that town. Wave 1 therefore ships only rows with a verified local problem, and keeps the thin rows (Tine, Slip, Sumphold, Wellmeet) as owner decisions or test-only rows.
3. **Counts.** Of 20 census places: 2 already have machines (Sill, Cinderhold); Wave 1 adds 3 (Gantry, Tally, the Last Counter) and up to 3 owner-gated (Tine, Slip, Wellmeet); Wave 2 adds 1 (the Salt-Vault) and upgrades terms; Wave 3 targets Morrowfast, Lampwell, Marrowstye, Quillhold, Posy and others behind named gates. Exempt with a cited reason: the First Tent, the Quiet's Door, Olderdeep. Pending with a named opportunity-first owner and no honest placement today: Sumphold, the Drowned Ledger, Spivenor (section 6). I do not pad these; section 9 lists the choice the owner has if they want them filled sooner.
4. **"Same machine, different place" is real here.** The forge becomes a wright's bench at a crossroads (Gantry), a fitting bay at an exchange (Tally), field smithing beside a finite weapon-kit chest (the Last Counter), and a salt-yard bench where Pale-Salt infusion is the local problem (Wellmeet, later the Salt-Vault). Section 7 maps what differs per place.
5. **Corrections to the inputs, found while verifying** (details in section 11):
   - S1 and S2 are already present in the working tree, uncommitted (`Assets/Scripts/Gameplay/Installations/InstallationMarkerPart.cs`, `SavedPartContract*`, `InstallationRetentionTests.cs`, one added conjunct in `OverworldZoneManager.CanUnloadZone`) [V]. The spec header says nothing was written. I did not run them; the plan assumes S1/S2 as the spec orders them and does not claim they pass.
   - Pale-Salt infusion needs `ForgePart.IsNearForge`, not a station kind [V `MineralPreparationPart.cs`], so a non-forge bench cannot host infusion until S4/S9 plus a new kind token exist. This moves the Salt-Vault from Wave 1 to Wave 2.
   - The spec's S8 illustrations (forge at Sumphold; reagent stills at Posy and Quillhold) are not purpose-qualified by the dossiers and are rejected here.
   - The spec's S11 puts a `Fee` forge at Tally; Tally has no Concord keeper (test-pinned), so there is no operator to pay. Tally stays Open.
   - The spec's default `Ground` list lacks `Sand`; every Beating row needs it [U: confirm the ground owner name in a generation probe].

---

## 1. Waves, gates and what each one is allowed to contain

| Wave | Contains | Needs (spec slices) | Never contains |
|---|---|---|---|
| **0 Foundation** | no player-visible placement; the census | S1 saved-Part golden, S2 marker plus retention, S6 registry plus the 20-place census (all places classified Installed / Present / Exempt / Pending, ratchet only downward) | any machine |
| **1 First forges** | existing `TinkersForge`, Open terms, exterior, non-solid; one pilot outside any town (settling still); one vended instrument (sounding-rod) | Wave 0, S7 generic post-builder, S5 reader (so the placed forge says what it does), S8 fan-out of rows | solid blueprints, interiors, fees, new bodies |
| **2 Contracts and new bodies** | terms-bearing rows; one fire-free station body with a second station kind; steeping trough; pump pair | S3 (disabled rows, declared time), S4 (station contract), S9 (hardening), S10/S11 (terms, after critique), S13 (work-kind table, `StationPart`), small new Parts | anything needing jobs, receivers or stored work |
| **3 New systems and opportunity-first** | carriage, cradle pump-head, Beetle-Master, assay desks, lamp-works, relay nodes, Morrowfast, thermal retrofit | named gates per row (Gate Q, cultures unparked, base system, owner permission idiom, local find manifest) | charge, mana, battery, any protected-subject reading |
| **Parked** | capstan (no honest load), dynamo (no consumer, canon), thermal exchanger beyond a retrofit | owner lifts parking and a purpose test passes | placement rows of any kind |

Wave 1 depends only on shipped mechanics plus S1/S2/S6/S7 (about 6 isolated cycles on the spec's critical path, plus actual play). Carriage (Wave 3) needs only S1 and the ConcordAccount architecture, so it is an independent lane that can be scheduled earlier than other Wave 3 items once the owner accepts its purpose test; it stays Wave 3 because the dossier classifies it as a new system (first cross-zone custody, critique-gated admission).

---

## 2. Placement mechanics every row relies on (so the rows can stay short)

- **Builder.** `InstallationPostBuilder`, priority 4650, registered at the tail of `CreateVillagePipeline` for the 16 shared-pipeline villages [R spec C3]. It never reads `rng`, never returns false, omits only the failing row with a `worldgen/InstallationOmitted` diag, and places exterior, bare, unreserved, dry cells only (never `RoadStone`, never within Chebyshev 1 of a door, stairs or the Well) [R]. Morrowfast returns before this pipeline (`OverworldZoneManager.cs:975-979` [R]); the three catacomb villages use other pipelines (spec E12). Both need their own hooks.
- **Anchor rule language.** A row names an owner anchor list (`Near`, with `Well` appended) and a `Radius`; candidates are ordered nearest-first by Chebyshev distance then a stable hash [R]. That is an approximation of "in front of the room X". Precise plan-relative frontage (Tally's goods-store apron, the Last Counter's supply forecourt) is the S24 `ISettlementPlanView` form and is deferred. Each row below states (a) the plan-relative target taken from the dossiers, (b) the `Near`/`Radius`/`Ground` approximation, (c) the do-not-touch cells. Wave 1 acceptance tests assert the placed cell falls inside the dossier's verified window across all plan variants; if it does not, the row's `Near` is tuned or the row waits for S24.
- **Proposed one-field addition to S6 (additive, data-only): `MinRadius`** (default 2, range 0..Radius). Reason: `ConcernWorld` lands readable documents and guests on the first open cell in rings from the square's centre (Tally dossier 3.2, `ConcernWorld.cs:217-241` [R]), and Slip's name wall needs rings 1-2 around the Well at least half open [R]. A row that must keep Chebyshev 3 clear sets `MinRadius: 3`.
- **Retention.** `None` for every Wave 1-2 row (stateless fixture; a relapse regeneration re-places it at the same cell). `Zone` only for a state-bearing body (pump head, later). Relapse-capable places: Sill, Wellmeet, Tine, the Drowned Ledger, Cinderhold [R spec section 1 item 8].
- **Old worlds.** No version gate. An unvisited town in an old world gets its row on first visit; a visited town never does. State this in every evidence record.
- **Presentation.** A placed forge draws as the red glyph `n` outside admitted opening zones, as the Cinderhold forge does today [R presentation 3]; four stations share that glyph and differ only by colour. Graphics are cancelled, so every row is **GLYPH** unless it says NEW-BODY. Readers (`x`, F1, Look) and the S5 marker line "Set here to be used." are the discovery surface.
- **Mode column.** FIXTURE = player-operated, no operator. SERVICE = an NPC performs it, with a fee, barter or gift. HYBRID = fixture whose terms name a present operator.
- **Voice.** Every authored string (names, hook lines) is a draft and needs an intent card and the fresh-context blind gate (lore R10, L17). No hook text below is final.

---

## 3. Wave 0: foundation rows (data and census, no placement)

`Installations.json` after Wave 0 carries zero `Installations` and these classifications (spec C3 shape; the exemption list is for owner confirmation):

```json
"Exemptions": [
 { "Place": "the First Tent",   "Reason": "monument to a choice; no machine installed",                  "Source": "Lore/Factions/07_TentRight.md:119 (via lore.md section 6)" },
 { "Place": "the Quiet's Door", "Reason": "refuses all outside contact; installed machines by outsiders forbidden", "Source": "catacomb_village_design.md:409-416 (via lore.md section 6)" },
 { "Place": "Overworld.4.6.2",  "Reason": "founding village: combustible plume is the hearth, the Rooted and east wall are protected, no machine at the chamber", "Source": "lore.md sections 4, 6; settlement-olderdeep.md section 0" }
],
"Pending": [ /* every other place not Present, each with Owner = the gate named in section 6 */ ]
```

Present (pinned by a test that generates both and finds `TinkersForge`): Sill, Cinderhold. Optional spec gap flagged in the pump dossier: exemptions are place-level; add an optional `Kinds` array so a place can be exempt for one kind (the pump at Wellmeet, the forge at the Drowned Ledger's bodies) without blocking others.

---

## 4. Wave 1: the first forges (and two non-town pilots)

### 4.1 Rows that ship (purpose-qualified; Retention None; Open terms; FIXTURE; GLYPH)

All three place the existing `TinkersForge` (non-solid, layer 5, glyph `n &R`, `ForgePart` only; no Thermal or Light Part, so no fire hazard near cloth) [V `Objects.json:5597-5640`]. Forge verbs, the three `ForgeCold` processes and Pale-Salt infusion all resolve any ready forge in the actor's 3x3 [V `ForgeWorkReceipt.Resolve`; `MineralPreparationPart`], so no new service code is needed.

**W1-A the Last Counter (`last-counter-supply-forecourt-forge`).** Strongest case in the set. The supply post issues a finite weapon-component kit in a chest whose own sign says to bring the kit to a forge, and the zone has none [R settlement-last-counter 0, 4.3; `LastCounterCompositionBuilder.cs:176-177`]. The sign is both the problem statement and the discoverability hook: it becomes truthful.
- Plan-relative target: the supply-post forecourt, either the 1x4 row sheltered by the west return or the 3x2 block, offsets from the supply-post origin per formation; both verified clear across all 120 plan variants [R 3.3]. Kept clear: x >= 58 and the sign cell, the plaza lanes, the three shoulder windows, the east frontier (intentionally empty), the WorkersHouse door aisle.
- Row approximation: `Near ["SaccharineEnvoy","Well"]`, `Radius 9`, `MinRadius 2`, `Ground ["Floor","StoneFloor","Sand"]` [U: Sand].
- Operator: none (Open). Envoy is a later Fee operator (Wave 2 option) and the carriage keeper (Wave 3).
- Alternatives (all kept): assemble the kit in the pack, process away from a station (M3c), the Peat Works rig 11-12 map cells north, the Cinderhold forge, buy finished goods.
- Risks: the plan builder's closed `Valid()` whitelist nulls the zone on an unknown blueprint, but the post-builder runs after it and adds one entity, so this is not triggered [I]; exact-count tests on Envoy/Sign/Campfire/Chest are untouched by +1 forge [U: run them]; random village props may already occupy a candidate, so the row omits cleanly.
- Acceptance: fresh seeds 64 and 704860946: walk to the post, Look at the forge, assemble from the chest kit, forge or re-forge at it, F5, fresh Continue; cached-save control shows none.

**W1-B Tally (`tally-exchange-forge`).** Verified gap: Tally's traders sell all six weapon components and rent loaners yet it has no forge [R settlement-tally 4.2].
- Plan-relative target: the court in front of the goods-store door (north-east front apron, always unreserved) or a sheltered north-east court; sited by room role from `plan.Rooms`, never absolute [R 3.2-3.5]. Do not use: the service cell at room origin (X+5, Y+2), any cell within Chebyshev 3 of (40,12) (document and guest rings), the well apron, cloth interiors.
- Row approximation: `Near ["Well"]`, `Radius 9`, `MinRadius 3`, `Ground ["Floor","StoneFloor","Grass"]`.
- Operator: none. **No Fee in any wave unless an operator is added**: Tally has no `ConcordFactor` or `SaccharineEnvoy` and a test pins that [R `TallyCompositionTests.cs:40`]. Do not give the Counter a machine verb (canon beat, lore section 6).
- Alternatives: pack assembly needs no forge; the Sill and Cinderhold forges; buying assembled weapons.
- Hook: the Exchange's own stock (six weapon components) plus the marker line; a Merchant or Quartermaster line is voice-gated.

**W1-C Gantry (`gantry-forecourt-forge`).** A crossroads with no workshop at all; demand is inferred from traffic [R settlement-gantry 4] and must be proven in play (the forge dossier says so; record the finding, do not inflate if it fails).
- Plan-relative target: the exchange forecourt pad, rows out2-out3 from the exchange door, columns door.x +-2..9, either side; the pad's cells touch the road row, which supplies the standing cell (candidate G-A) [R 3.4]. Do not use: out1 row and the door column (route to the road), every approach and service anchor with its 3x3, the guest-cloth pole and rest, the court x37-43/y10-14, cells within 2 of `TentWall`.
- Row approximation: `Near ["Well"]`, `Radius 9`, `MinRadius 4`, `Ground ["Grass","StoneFloor","Floor"]`. Add `GantryWayboard` as a first anchor only if a probe shows the Well approximation misses the pad [U; wayboard is seed-invariant [R]].
- Operator: none. Alternatives: carry intact to Cinderhold, sell to the Merchant, assemble in the pack.
- Hook: optional fresh-only appended line on the wayboard text, modelled on `SoddenDistrict.DescribeTownRoute` (a read-only cold lead on an existing owner [R placement 10.3]); voice-gated.

### 4.2 Rows that stay owner-gated or test-only

The spec's S7 matrix needs three structurally different pipelines. Gantry, Tally and the Last Counter are all composed towns, so the plain `VillageBuilder` pipeline (Slip, Posy, the Salt-Vault, the Quiet's Door, Sill) and the wet, fast-relapse pipeline (Tine) are covered by **test-only rows** loaded through `InstallationRegistry.InitializeFromJson`, exactly as the forge dossier's correction C-a says [R]. Whether any of them also ships as data is the owner's call (section 9, decision 2):

| Row | Purpose evidence | Recommendation |
|---|---|---|
| `wellmeet-work-yard-forge` | two real local reasons: Pale-Salt is local to the camp, and the pan-scales lasting repair wants 2 spikes plus a binding; infusion and `point` need a forge; a camp's work tent exists and its back yard is always free [R settlement-wellmeet 3, 4] | **Approve.** Open terms are the honest "given" idiom; a later `Gift` row (Wave 2) only restates it. North court-edge pocket (plan-wide always free), at least 2 cells clear of any tent wall, trunks and the court. `Ground ["Sand","StoneFloor"]` (never `RoadStone`, the public route) [U: Sand]. Never attach to the Well, Oven or Lantern owners (art veto lists `ForgePart` as foreign meaning [R]). Never price it (lore L3). |
| `tine-lakeside-forge` | surface villages natively own forges (faction matrix O); no local find; weir needs timber, haft and a hatchet, which a forge does not make [R settlement-tine 4] | **Thin. Approve only for the regression value** (fastest relapse at 150 turns, wet neighbours), or keep test-only. Cell: unreserved exterior dry cell away from the lake edge, the well apron, the trunk road and every boat frame's dry frontage; boat frames must never become reclaimable [R]. |
| `slip-west-square-forge` | forge plausible (Merchant sells FireClay, Tinker sells components) but the better local installation is a lamp-works (Wave 3) [R settlement-slip 6, 7] | **Decline in favour of Wave 3**, keep test-only for the plain-pipeline matrix. West half of the square only; the east half is the unwritten Hush quarter and must stay open; rings 1-2 of the Well half open. |
| `sumphold-*` | the Works rig two zones away already reclaims; `SumpholdFurnishingBuilder.Payload` forbids `ForgePart` [R]; no local use | **Decline**; test-only row to prove the 4650 builder runs cleanly after the 4600 furnishing builder [U: its payload validator must not re-run over later additions]. |

### 4.3 Non-town wave-1 items (not counted toward "every town")

- **Settling still pilot** at the Sodden dressing shelter 15.7 (a plan flag like the oil strainer; stateless; needs S1 only, one-line fallback for turn cost). The still dossier's own purpose audit finds it serves zero of the 20 places; it has exactly one honest site, so it **fails the two-place rule honestly** and is not listed as a town machine [R machine-still 0, 2.1, 7.4]. I do not invent a second site.
- **Sounding-rod** as a `TinkerStock` row: vended, carried, not an installation. A Tinker rolls in 70% of the 16 shared-pipeline villages [V `VillagePopulationBuilder.cs:132-134`], so a traveller meets it in roughly 11 of 16 towns, none guaranteed, none in a town already cached. It is a purpose-gated pilot (resonator Q0) and the dossier says it must not count toward the census [R]. Whether a First Tent Tinker may stock it is an owner question.

### 4.4 Wave 1 type-presence rule (each wave-1 type in at least two places)

| Type | Places a traveller meets it | Verdict |
|---|---|---|
| Forge | Sill, Cinderhold (present); Gantry, Tally, the Last Counter (new); Wellmeet if approved | meets the rule; spans Spread, Grovelands and Beating |
| Sounding-rod (vended) | about 11 Tinker towns, probabilistic | meets the rule as an item, not as an installation |
| Settling still | the Sodden dressing shelter only | does **not** meet the rule; pilot, honest |

---

## 5. Waves 2 and 3: rows with named gates

### 5.1 Wave 2

| Row | Machine and form | Where | Needs | Notes |
|---|---|---|---|---|
| W2-A | **Infusion bench**: a fire-free `StationPart` body carrying kinds `cold-work` and `infuse` (a new kind token the spec lacks; S9 routes `MineralPreparationPart` through `StationReceipt.Begin(..., "infuse")`) | the Salt-Vault, exterior, west block of the village square (square-relative: `GetVillageSquareBounds` x34-37,y9-14 is structural, not seeded) | S4, S9, S13b, new body (glyph+colour unique, graphics cancelled so glyph), voice gate on the name | Real gap P1: Pale-Salt is abundant next door and no forge-class station exists in the zone [R settlement-salt-vault 0, 5]. Pale Curation voice is condition-method-status; never "bath" on a person or body; nothing near the Salted, the file or the Indexer. **Do not ship an early plain forge here**: it is sticky (visited saves keep it) and tonally wrong ("cold, clean, quiet") [R]. |
| W2-B | **Terms on shipped forges**: `Fee` at the Last Counter with the Envoy as present operator; `Gift` restated at Wellmeet; Tally and Cinderhold stay Open | the Last Counter, Wellmeet | S10 independent critique (who is paid, absent-operator UX, spoken end), S11, blind gate | The Cinderhold Weaponsmith's "paying customer" line is not enforced and stays unenforced by default [R]. Terms only on new rows that name a free alternative. |
| W2-C | **Steeping trough** (gear-only, stateless, one acid row) | Sill, only after actual play shows a Soursprayer at the glade-ring worksites (11.9, 11.11) on two fresh seeds; Sumphold only if a Soursprayer is found in the Sodden (its mire acid is 1 point and floors, so the trough would do nothing there) | S3, S4, S5, equipped-item turn hook G1, `SteepRecipes.json` (second table) | Bath dossier: "every town" is not honest for this machine; Pending rows for it are forbidden; Sill is Present, so an extra row must not disturb `SillCourtComposition` exact-count tests [U]. FIXTURE, NEW-BODY. |
| W2-D | **Pump pair** (hand pump plus sunken tool-store) | the Sodden Peat Works 17.7 (a plan flag, not a town) | S3, S5, liquid-holder contract extracted from `LiquidVesselService` | Zero towns. Purpose test can fail and park it. |

### 5.2 Wave 3 (each row names its gate; none ships without it)

| Row | Machine and form | Where | Gate |
|---|---|---|---|
| W3-A | **Concord carriage** (conversation choices on ledger keepers; SERVICE, fee, no object) | Cinderhold (`ConcordFactor`), the Last Counter (`SaccharineEnvoy`); Sill only when the main well is Concord-contracted (the Factor arrives) | owner accepts its purpose test and the admission-table critique (R1); S1 only technically; works in old saves; Tally is not an endpoint (no keeper) |
| W3-B | **Pump-head cradle** (`cinderhold-pump-head`, Retention `Zone`) | Cinderhold east yard, fresh worlds only | the pump gains a physical action first; one shared repair authority; Cinderhold is relapse-capable so it must retain; cradle dossier says do not schedule in waves 1-2 |
| W3-C | **Beetle-Master** (SERVICE, barter; sells starter colony jars from finite seed-deterministic stock) and the tended colony jar | Lampwell 12.3.2; the shipped Olderdeep descent jar becomes tendable (a carried vessel, not an installation) | cultures unparked by the owner; catacomb attach point (spec E12 lifted for this one row); a new `BeetleMaster` role with voice card; a deliberate `TraderPart` in a plan that says none |
| W3-D | **Assay desk** (condition board / collation table / weigh bench) | Gantry, Marrowstye (attach to the existing `CurationSaltBench` instance), Quillhold (copy-hall nook), the Last Counter (supply-post far wall), later others | **Gate Q** (a real sample stream and a question the free readers cannot answer); in-room anchors need S24; every fence in the assay dossier 4.3 |
| W3-E | **Lamp-works** (item process: pitchpod resin to lamp oil, third work-kind row, station body) | Slip, west half of the square | S13a work-kind table, S13b bench, registry row, voice gate; local consumers: `slip.east` lamps, `quillhold.lamps`, torch refuel; every alternative stays (buy, strain at Peat Works, candles, read the wall) |
| W3-F | **Forge at Morrowfast** (additive native entity installed by its own scene hook, first generation only; never a row in `definition.json`) | Orrit's west yard, outside the frog habitat box, keeping Orrit's five talk cells and the coil strip free; or attach a `ForgePart` to the existing worktable if `Clear` is refused | an authored finite local damaged-find manifest (for example the lost-property crate), the explicit work-permission idiom (Orrit's bench is his workplace; S10 critique), blind gate; Morrowfast is a local non-canon proposal and must be labelled so |
| W3-G | **Relay nodes (V2)**: capability on the Gantry wayboard, Tine (scribe endpoint), Sumphold (records shelter), Quillhold | Gantry, Tine, Sumphold, Quillhold | a home/base second node (S19) and timed jobs (S16); must not auto-complete `tine.scribe`; must not voice Slip's names |
| W3-H | **Thermal retrofit**: make the repaired village oven a working heat source | Sill, Wellmeet (their oven is a tracked repair site) | an owner-approved first consumer; never at the First Tent; keep heat 3 cells from cloth walls |
| W3-I | **Posy finishing frame** (object-only steeping; carried gear only; Bower permission) | Posy square west block | trough V1 in production; the permission idiom (S10 critique); change nothing in the Posy concern |
| W3-J | **Spore-Wedded cultivation bay** (opportunity first: a resident supplier plus barter or trust path, then vat or assay) | Spivenor | cultures unparked; new role; consent patterns; never the patch or ring as feed |
| W3-K | **Physical survey-line frame** (the stake drift made an owner, then a cradle-shape repair) | the Drowned Ledger | opportunity-first content; relapse retention (SurveyLine 700 turns); every fence in the Ledger dossier |

Parked, no rows: capstan (no honest load; every dossier rates it poor), dynamo (no consumer, no charge canon; Cinderhold winding bench is a recorded design only), thermal exchanger beyond W3-H.

---

## 6. Per-settlement ledger (all 17 authored villages plus the 3 catacomb villages)

Columns: pipeline, what exists, wave rows, the exact reason when nothing ships, one-line discoverability hook (draft channel, not text), mode, art and reader flags.

| # | Place | Pipeline | Exists today | Planned installation (wave) | None / exemption: exact reason | Hook | Mode | Flags |
|---|---|---|---|---|---|---|---|---|
| 1 | **Sill** | plain `VillageBuilder` + `SillCourtComposition` | hub forge and still at the square, Smithy forge, Alembic still (2 and 2, test-pinned) | Present. W2-C trough (conditional). W3: Concord-contract carriage endpoint; oven retrofit | n/a | the Smithy shop and the square hub already; Look | FIXTURE | trough = NEW-BODY, voice |
| 2 | **Morrowfast** | fixed scene, own hook | no machine; 8 residents; starter garden | W3-F forge via scene hook | not a Wave 1 target: scene pinned at 71 owners/2000 cells, no manifest, no permission rule | Orrit's dialogue (voice-gated); not the notice | HYBRID (Orrit's workplace) | glyph; label as local proposal |
| 3 | **Gantry** | composed (service cells) | exchange, registry desk, wayboard, guest-cloth pole, rest | **W1-C forge**; W3 assay desk, relay capability on the wayboard | n/a | appended wayboard line | FIXTURE | glyph; reader S5; voice |
| 4 | **Tine** | composed (lake) | boat frames, scribe, weir site | W1b optional forge (test-only otherwise); W3-G relay endpoint | no strong fit; weir needs haft, timber, hatchet | none required | FIXTURE | glyph |
| 5 | **Cinderhold** | composed | forge, anvil, Weaponsmith, find, oil tray, mine-pump site | Present. W3-A carriage (Factor), W3-B pump-head cradle; dynamo parked | n/a | Weaponsmith line, notice board, the Pump concern | FIXTURE + SERVICE | W3-B NEW-BODY |
| 6 | **Posy** | plain | nothing | W3-I finishing frame | not placed earlier: no purpose, Bower permission canonical, forge/still/vat/assay all poor or forbidden | none; Elder only | FIXTURE under permission | NEW-BODY, voice |
| 7 | **Marrowstye** | composed (Curation intake) | salt bench "offers no preservation service", conservation case, slab, annex | W3-D condition board on the existing bench | nothing before Gate Q; forge and bath are poor; annex, slab, disused wing and bodies are out | the bench's own reader | HYBRID (issued by staff) | reader S5 |
| 8 | **Quillhold** | composed (Archive) | copy desks, loan shelf, scribe | W3-D collation table (copy-hall nook) | nothing before Gate Q; resonator forbidden (Sealed Libraries); forge/still/bath poor | Scribe line | FIXTURE | needs S24 in-room anchor |
| 9 | **Tally** | composed (Exchange) | goods store, rental frontage, rest court, no keeper | **W1-B forge** (Open); W3 assay weigh bench | Fee impossible without an operator (test-pinned absence) | stock of six weapon components | FIXTURE | glyph |
| 10 | **Wellmeet** | composed (Tent camp) | salt exchange, guest locker, scales site | **W1b forge** (infusion and scale work); W2-B Gift terms; W3-H oven retrofit | pump forbidden (water given); no dram-priced service (L3) | SaltMaster line; the Scales concern | FIXTURE (given) | glyph |
| 11 | **the First Tent** | composed (Tent camp, first) | poles, salt shelter, pilgrim rest | none | **Exempt**: a monument to a choice, no machine installed (`Lore/Factions/07_TentRight.md:119`); the settlement report's weak "communal bench" option is not taken | none | n/a | none |
| 12 | **the Salt-Vault** | plain + fresh archive entrance | Indexer, locked file, Salted chamber | **W2-A infusion bench** | no Wave 1 row: sticky plain forge is tonally wrong; nothing near the Salted | the bench reader; Indexer line | FIXTURE | NEW-BODY, voice |
| 13 | **Sumphold** | composed + 4600 furnishing | boats, trestles, toll rolls, five furnishings | none now; W3-G relay hub | **No honest town-level problem today**: the Sodden circuit's machines (strainer, rig, still, pump pair) sit one to two zones away and the toll roll is their staging hook; a forge is redundant with the Works rig; trough does nothing against 1-point mire | toll-roll work-slip clause (existing `DescribeTownRoute` pattern) | n/a | voice |
| 14 | **the Drowned Ledger** | composed (Excavation) | reading tent, bodies, stakes, survey-line site | none now; W3-K survey frame | **Canon and structure**: pump is "allowing decay" (L5, L6); every machine near bays or bodies is forbidden; the only local problem (stake drift) is abstract site state with no physical owner | Searcher or Sorter explains, voice-gated | n/a | relapse-capable (700) |
| 15 | **Slip** | plain | well, chalked names wall | test-only forge now; **W3-E lamp-works** | no Wave 1 real row (thin); no machine may read or restore the names or map the Hush | the Slip and Quillhold lamp concerns | FIXTURE | NEW-BODY |
| 16 | **the Last Counter** | composed (Concord post) | Envoy, finite kit chest, beds, no forge | **W1-A forge**; W2-B Fee; W3-A carriage; W3-D weigh bench | n/a | the chest sign | FIXTURE, later HYBRID and SERVICE | glyph |
| 17 | **the Quiet's Door** | plain (surface only) | Elder, ward jars | none | **Exempt**: refuses all outside contact; installed machines by outsiders and a survey pulse at the Root's doorstep are canon affronts | none | n/a | none |
| 18 | **Olderdeep** (4.6.2) | `FoundingVillageBuilder` | hearth plume, niches, plaque wall, Rooted, descent jars | none (the shipped descent jar becomes tendable, a carried item) | **Exempt**: the plume is the hearth and combustible, the east wall and Rooted are protected, no heat, light or listening taken | none | n/a | none |
| 19 | **Lampwell** (12.3.2) | `StrandedSettlementBuilder` | hearth patch, Drosera ring, beetle jars, two residents, no trader | W3-C Beetle-Master | no row before cultures are unparked; fire kinds forbidden (open flame is an offence); patch and ring are never feed | the plaque-tender's village talk | SERVICE (barter) | new role, voice |
| 20 | **Spivenor** (16.4.2) | `StrandedSettlementBuilder` | same kit, Warden and Plaque-Tender | W3-J cultivation bay (opportunity first) | no code-verified local problem; thermal, dynamo, resonator forbidden (patch war, whisper culture) | none | n/a | new role |

Counts if every gate passes: Installed or Present at 14 places (Sill, Cinderhold, Gantry, Tally, the Last Counter, Wellmeet, the Salt-Vault, Morrowfast, Marrowstye, Quillhold, Posy, Slip, Lampwell, Spivenor), Tine optional, Exempt at 3 (First Tent, Quiet's Door, Olderdeep), and Sumphold and the Drowned Ledger remaining structurally empty until their opportunity exists. That is a statement of the honest ceiling, not a target to force.

Outside the 20: the Sodden circuit (15.7 still pilot and dressing bench, 17.7 rig, strainer, pump pair) carries the district machines; merchant camps, the Spread wayhouse, Stillleaf, Ginmere and the Deepest Cathedral get none (Choir law, protected subjects, no residents) [R].

---

## 7. Same machine, different place

| Axis | Gantry | Tally | the Last Counter | Wellmeet | Salt-Vault |
|---|---|---|---|---|---|
| Local problem | crossroads wagon trade with no workshop (inferred) | sells every weapon component, rents loaners, no forge | kit chest that tells you to find a forge | local Pale-Salt, scale repair spikes | local Pale-Salt, no infusion station |
| Body | tinker's forge on the exchange forecourt | tinker's forge in a court by the goods store | tinker's forge by the supply forecourt | tinker's forge in the salt-yard pocket | fire-free bench, infusion kind |
| Terms | Open | Open, no operator exists | Open now; Fee with the Envoy later | Open (given) | Open |
| Consequence | traveller convenience; record whether anyone uses it | assembled-in-place weapons beside the component stock | smaller kit-carrying burden on the way east | infused gear before the wasteland | salt spent on infusion rather than barter |
| Fence | do not meter guest-right or the cloth | do not give the Counter a verb | do not claim delivery past the post | never price water or the oath | nothing near the Salted |

Other machines vary by faction idiom, not by copy: carriage is Concord contract, the Beetle-Master is catacomb barter, an assay is a Curation condition board in one place and a Recension collation table in another.

---

## 8. Test and acceptance shape per wave (kept to the 5-minute cycle)

- **Wave 0.** S6: one test per validator code E01-E14 plus a valid neighbour each; `CoverageClassifiesAll20PlacesExactlyOnce`; `PendingCountMayOnlyDecrease`; `EveryVillagePipelineHasUniquePriorities`, `Priority4650IsFree`.
- **Wave 1.** S7 control matrix per row: exactly one forge at a deterministic cell across seeds 64, 704860946, 1729 and a 24-seed sweep; seed-sensitive but stable per seed; other owners identical with the row removed (blueprint plus position sets, never IDs, because the forge shifts later entity IDs by one); `rng` untouched; flood connectivity and stairs preserved; cached zone never retrofitted; second `BuildZone` on a populated zone changes nothing; creation-callback and missing-blueprint injection omit only the row; per-row window test (the placed cell lies in the dossier-verified set). Actual play: walk to each town in two fresh seeds, Look, `x`, use a forge verb, F5, fresh Continue; cached-save control. Limits stated: glyph only, Linux and Deck unexecuted, first visit only.
- **Wave 2.** S3 declared-time parity table; S4 oracle truth table over at least 40 layouts; S9 callback-moves-station REDs; infusion bench: `InfuseRunsAtBenchWithNoForgePart` and its counter.
- **Wave 3.** Each row's own dossier slices and a purpose test that can fail and park it.

---

## 9. Owner decisions (this plan does not make them)

1. Lift the "no new runtime changes" hold for Waves 0-1 (Wave 2-3 each need their own gate).
2. **Thin forge rows.** Recommended: approve Wellmeet (two real local reasons); approve Tine only for regression value, or keep it test-only; decline Slip (lamp-works instead) and Sumphold (redundant). The cost of approving more is padding and a permanent sticky machine in every save that visits the town. If the owner wants "every town" faster, the cheapest filler is the Tine and Slip forges; I do not recommend it.
3. Confirm the three exemptions (First Tent, Quiet's Door, Olderdeep), and the First Tent in particular since its own report offers a weak bench option.
4. Accept the `MinRadius` field and a kind-scoped `Kinds` array on exemptions in S6.
5. Tally stays Open; a Fee there needs a Concord operator, which is itself a canon decision about the Counter.
6. Salt-Vault: wait for the fire-free bench (recommended) versus an early plain forge (sticky, tonally wrong).
7. Carriage as an independent early lane after its purpose test.
8. Revise the design stance in `VillagePopulationBuilderTests.BuildZone_NonStartingVillage_PlacesNoCraftingStations` ("a spawn amenity, not generic village decor") to "placed by the registry, only where purpose-qualified". The test itself runs only `VillagePopulationBuilder` and is not broken by a 4650 builder [V].
9. Whether the First Tent may stock the sounding-rod through a Tinker roll.
10. Blind-gate owner for every hook line and body name.

---

## 10. Risks (placement-specific)

1. **Stickiness.** Visited towns keep the first layout forever; wrong rows cannot be repaired by data. Mitigation: low-regret Wave 1.
2. **Relapse regeneration** at Sill, Wellmeet, Tine, the Drowned Ledger, Cinderhold: stateless rows re-place identically; any state-bearing body needs `RetainZone` (S2). S2's own premise test may narrow the hazard [R].
3. **Cell contention** after the 4650 builder: none; before it, random oven, lantern, stalls, chests and NPCs can occupy candidates, so rows omit cleanly and the census (not luck) is the gate.
4. **Exact-count and village-count tests** that mention `ForgePart` (`SpreadExplorationWorksitesTests`, `SpreadWorksiteManifestTests`, `ChunkActivityCensusBenchTests`) may assume none in a village [U: run them in S7].
5. **Glyph collision.** Four stations share `n`; a Wave 2 body must take a unique glyph and colour (S23 ratchet).
6. **Voice debt.** No shipped machine string has a recorded blind-gate pass [R lore L17].
7. **Canon landmines by place** (ids from `lore.md` section 7): Marrowstye (L5), the Drowned Ledger (L5, L6), Posy (L7), Wellmeet and the First Tent (L3), Quillhold (L6), Spivenor and Lampwell (L2, L4, L8), the Quiet's Door (Asleep Tradition).

---

## 11. Verification record

Read this session [V]: `ARCHITECTURE-SPEC.md` sections 0-3, C3, C5, C7-C11, slice plan and sections 8-10; `ForgeWorkReceipt.cs` (Ready, Near, Resolve, Begin); `TinkersForge` blueprint header (non-solid, glyph `n`, colour `&R`); `VillagePopulationBuilder.cs` lines 130-136 (Tinker 70%); `MineralPreparationPart.cs` lines 14-40 (infusion gated on `ForgePart.IsNearForge`); `WorldMapAuthoring.Places` (17 places, factions, profiles); `VillagePopulationBuilderTests` lines 318-337; working tree status and the `OverworldZoneManager.CanUnloadZone` diff; `InstallationMarkerPart.cs` and the head of `InstallationRetentionTests.cs`.

Taken from reports [R]: all settlement free-space windows, fit ratings and canon facts; the dossiers' purpose audits, verbs and gates; builder priorities and test pins other than the three above.

Not verified [U], each settled by a named step: the `Sand` ground owner name in Beating towns; whether `SumpholdFurnishingBuilder`'s payload validator re-runs after a 4650 addition; village-count test assumptions about forges; whether S1/S2 in the working tree compile and pass; exact blueprint tokens for `Near` anchors (`GantryWayboard`, `SaccharineEnvoy`, `Quartermaster`) in a generation probe; the effect of `MinRadius` on window membership.

No Unity, test, build or git write was performed. The only file written is this report.
