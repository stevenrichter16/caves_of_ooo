# CRITIQUE: canon, player experience, discoverability and balance (placement plan, dossiers, architecture spec)

Lens: root `Lore/` (via `lore.md` section 1 R1-R11 and section 7 landmines), the user pitch in `Docs/MACHINE-ROSTER-VISION.md`, and what a player would actually meet. Read-only; nothing was run, built or written to the project. Only this file was written.

Evidence tags: **[V]** I read the cited line this session. **[R]** taken from a research report or dossier, not re-read. **[I]** my inference. Lore authority is root `Lore/` only.

## Verdict: accept with changes

The plan is honest about the thing that matters most: only the forge has a placeable body today, "every town" is a ratchet, canon exemptions carry sources, and Waves 2-3 are gated. I found no canon-forbidden placement in the Wave 0-2 rows as written. I found one shipping hazard that already exists in the working tree (P1), and a cluster of P2 problems where the plan's purpose claims are stronger than the evidence, where one row contradicts canon and its own settlement report, and where the player-facing promise is not advanced by the shipping waves.

## What I checked and found clean (so it is not re-litigated)

- **No dram mint from the forge.** `ForgedWeapon` has a flat `Commerce` value of 25; each `WeaponComponentItem` is 8, so a three-component batch is 24 -> 25 before any trade spread [V `Objects.json` Commerce scan]. Mineral infusion never touches `CommercePart` [V: no reference in `MineralInfusionTinkerModification.cs`; `TradeSystem.GetItemValue` reads base `Commerce` only, `TradeSystem.cs:28-39`], so a free forge next to cheap Pale-Salt does not convert salt into sale value. The three `ForgeCold` rows conserve weight and value (forge dossier F5 [R], consistent with the 46 -> 45 / 46 -> 11 / 8 -> 8 numbers).
- **Ordinary repair, healing and carrying are untouched** by Waves 0-2: the forge rows add a cell, not a gate; single-weapon assembly stays free (see P2-1); no fee or term applies to a shipped station.
- **Protected subjects are structurally excluded** where the dossiers use allow-lists (resonator allow-list; process registry `Portable()`; carriage admission census). Tent-Right `Fee` is rejected at load (spec C7).
- **Carriage economics** are bounded by the dossier's own inequality (fee >= price margin, salt refused, no reconciler). I have no objection to its admission table beyond P2-7.
- **Sounding-rod noise cost** is a genuine price, and its allow-list refuses sinkhole mouths, villages and authored zones. Fine.
- **Voice debt** (no shipped machine string has a recorded blind-gate pass, L17) is correctly carried as a gate on every authored string.

## P1

### P1-1. The working tree already ships the spec's "mechanism coverage" rows (Gantry, Tine, Slip), contradicting the placement plan, and they are permanent in every save that visits those towns

Facts, all read this session:
- `Assets/Resources/Content/Data/Installations.json` (untracked, written 18:57, after the plan at 18:55) carries three shipping rows: `gantry-forecourt-forge`, `tine-forecourt-forge`, `slip-forecourt-forge`, each with the identical Purpose and `Basis: "... mechanism coverage"` [V]. Its `Pending` list assigns Tally, Wellmeet and the Last Counter to "S11 terms-bearing placements" [V], the opposite of the plan's Wave 1 (Open forge at Tally and the Last Counter).
- `OverworldZoneManager.cs` is modified: `CreateVillagePipeline` now registers `InstallationPostBuilder` and `CanUnloadZone` gains `!InstallationMarkerPart.Retain(...)` [V `git diff`]. `Docs/MACHINE-INSTALLATIONS.md` (untracked) records decision 1 as "chosen for mechanism coverage ... owner sign-off in lieu of an interactive answer" [V lines 73-78].
- The placement plan says the opposite for the same towns: Slip "Decline in favour of Wave 3, keep test-only"; Tine "Thin. Approve only for regression value, or keep test-only"; the forge dossier says "Coverage is not purpose ... covers Tine/Slip/Sumphold with test-only rows" (C-a) [V plan 4.2; R dossier 1].
- The in-tree builder has no `MinRadius`: candidates are every cell in the Chebyshev box around the first `Well`, sorted by distance then hash, and `Fits` only excludes Chebyshev 1 of a door, stairs or Well [V `InstallationPostBuilder.cs:79-84, 123-131`]. So each forge lands at the first valid cell, Chebyshev 2 from the Well, in the middle of the public square. The plan's section 2 says this is the wrong default for Slip (the name wall needs rings 1-2 around the Well at least half open) and Tally (ConcernWorld lands documents and guests on the first open ring) and proposes `MinRadius`.

Why P1: first visit bakes the layout and a visited town never changes (plan 0.2, 10.1; spec C3). A shipping row justified only as "mechanism coverage" permanently places a communal forge, at the plaza centre, in every world that visits Tine or Slip, with no player-facing purpose (the plan itself finds none: Tine's weir needs haft, timber and a hatchet; Slip's better local installation is a lamp-works). That is the progression-padding and fake-service pattern the project principles forbid, and it cannot be undone by data in a cached save. The doc's "owner sign-off in lieu of an interactive answer" does not satisfy plan decision 2 / spec decision 2, which reserve exactly this choice to the owner.

Fix:
1. Do not commit or build `tine-forecourt-forge` or `slip-forecourt-forge` as shipping rows. Keep them as test-only specs loaded through `InstallationRegistry.InitializeFromJson` in the placement-matrix tests (the matrix needs three pipelines, not three shipped machines). Keep the three-pipeline coverage by tests, which is what the plan and the forge dossier already say.
2. Hold `gantry-forecourt-forge` out of shipping data until a play observation shows demand (P2-1), or ship it only if the owner knowingly accepts a convenience row; relabel its Purpose honestly instead of "mechanism coverage".
3. Add `MinRadius` (default 2, plan 2) to `InstallationRow` before any shipping row, and make the first shipping row `last-counter-supply-forecourt-forge` (the only well-qualified one).
4. Change the `Pending` owners for Tally and the Last Counter from "S11" to the Wave 1 Open rows or to "probe", per the plan.
5. Correct the doc's decision log: the first-town choice is an owner decision, not a recorded sign-off.

## P2

### P2-1. Wave 1 purpose claims are stronger than the evidence; only the Last Counter is remote and the forge adds less than the plan says

- A single weapon assembles anywhere: `ForgeWeaponCommand.cs:83-96` ("CRAFTING-FROM-THE-PACK C2: a single weapon can be assembled anywhere ... A forge is what lets you work a BATCH") [V]. The forge-gated verbs are batch forging, re-forge, temper/quench, salvage of a forged weapon, mineral infusion and `ForgeCold` processes (`ReforgeWeaponCommand`/`TemperWeaponCommand` and `MineralPreparationPart.cs:28` all call `ForgePart.IsNearForge`) [V].
- The Last Counter chest holds one consignment (blade, haft, binding) and its sign already says "Assemble one weapon from the pack, or bring the kit to a forge" (`LastCounterCompositionBuilder.cs:177`) [V]. So the sign is not an unfulfilled promise ("it becomes truthful", plan 4.1 W1-A) and batch forging is moot for a one-kit chest. What the forge really adds there is re-forge (binding comparison) and temper. That is still a real, modest reason, but it is "a travel saving for re-forge and temper at the final supply post", not a missing capability.
- Distances from Sill (10,10), Chebyshev [V `WorldMapAuthoring.cs:210-226`]: Gantry 3, Tally 4, Cinderhold 4, the Last Counter 8 (12 from Cinderhold). Sill already holds two forges. Tally's own report says "Urgency is modest (Sill is near), so this fills a gap more than it solves a pain" [R settlement-tally.md:176]. Gantry's demand is "inferred from traffic and must be proven in play" [V plan 4.1 W1-C], which contradicts the plan's own Wave 1 rule (section 0.2: "ships only rows with a verified local problem").
- Because rows are sticky, "record the finding, do not inflate if it fails" (W1-C) cannot be honoured in any save that already visited Gantry.

Fix: Wave 1 ships the Last Counter row alone as the purpose-qualified row. Tally ships only after the probe in P2-9 shows it lands in the verified window and the owner accepts "convenience" as the stated purpose. Gantry is a test-only row until a fresh-world play pass records unprompted use. Rewrite the plan's "verified local problem" wording to "reachability convenience" where that is what it is.

### P2-2. The placed forge does not say what it does, and its shipped description misleads; the proposed "Works here" line would advertise verbs that have one feedstock in the whole world

- `TinkersForge` Examinable: "Fitting a blade, haft and binding here makes a working weapon." (`Objects.json:5639`) [V]. That reads as "this is where weapons are made" when single assembly is free anywhere (P2-1).
- Plan section 1 says S5 is needed "so the placed forge says what it does". S5 contributes only the marker line "Set here to be used." (spec C6) [V]; the derived "Works here" line is S13a, after S12 [V spec C6, S13]. The forge dossier proposes an earlier V1b line (C-c) but the plan's Wave 1 never lists it.
- That V1b line would read "Works here: dismantle tool; reclaim iron and timber; dress iron point" [R forge 3.6]. The three `ForgeCold` rows take `ReleasedExtractionTool` (one placement, `CinderholdReclamation.cs`) or `RecoveredIronBlank` (only an output of `reclaim` on that same tool) [V `MaterialProcesses.json` SourceBlueprint lines 5, 22, 39; grep of `Assets/Scripts` and `Content/Data`]. The leather offcut is a `HandCutting` row, not a forge row, and is a separate finite packet on fresh Tinkers (`Docs/MACHINE-LEATHER-FEED.md:48`) [V]. So three new forges would advertise forge processes whose entire world supply is one unit.
- Process rows live on the carried input item, not the forge (F4; presentation finding 6) [R], so a player with nothing eligible sees nothing on the forge about them.
- Discovery is the glyph `n &R` shared with anvil, rig and alchemy shelf (presentation finding 1 [R]) plus Look and `x`; no in-world mention exists for any shipped machine (finding 7 [R]). The plan's "Hook" column is mostly "none", "stock" or "the chest sign".

Fix: Wave 1 includes forge V1b, but the line must be truthful about supply: list the always-valid verbs (batch forging, re-forge, temper, mineral infusion, salvage of forged weapons) and phrase `ForgeCold` as conditional on carried released goods. Add one voice-gated mention per placed forge through an existing channel (a read-only `DescribeTownRoute`-style line, `PlaceConditions` Heard text or a discovery report), not only Look. Treat the shipped description as a separate small text decision for the owner (it is shipped text at Sill and Cinderhold).

### P2-3. The Wellmeet forge contradicts canon and the settlement report that qualifies it, and its two stated reasons do not hold

- Canon: Tent-Right "The well and the salt-work are the fixed points; everything else moves" and "They make almost nothing permanent" (`Lore/History/08_MaterialCulture.md`, Tent-Right section, "Dwelling" and "Craft & art") [V]; `lore.md` M1 allows Tent-Right use "only as portable/lent kit" [R]. The Wellmeet report rates the forge strong only "as a portable/relocatable communal field rig (not a smithy)" [R settlement-wellmeet.md:90]. The plan places a fixed `TinkersForge` (described as "A hearth and work surface") and says "Approve" (4.2) [V].
- Reason (b) is false: the pan-scales repair needs 2 `IronSpikeComponent`; spikes come only from `point` on a `RecoveredIronBlank`, which exists only as the output of `reclaim` on the single released tool, and the forge dossier itself states "One find yields one spike, so it can contribute to but never complete a two-spike repair" (F7) [R]. A forge at Wellmeet does not unlock that repair; the Tinker remains the only source.
- Reason (a) is narrow: `EnhancementPaleSalt` adds bonus damage only against `Undead`-tagged defenders (`EnhancementPaleSalt.cs:35`) [V]; only three blueprints carry that tag (`SkeletalSentry`, `IceWight`, `CharredHusk`) [V Objects.json scan]. It also competes with the SaltMaster's `WantsMineral PaleSalt` reputation reward [R settlement-wellmeet.md:32], which is a good choice, but a thin purpose for a sticky fixed smithy.
- The in-tree doc records that Wellmeet "found no cell next to [its] well within radius 9 in the seed-64 test" [V `MACHINE-INSTALLATIONS.md` decision 3]; the plan's "north court-edge pocket" is not reachable by the generic builder as parameterised.

Fix: reclassify Wellmeet as `Pending(owner: portable lent rig, after S19 installable Portable)` rather than "Approve", or exempt it for fixed forges alongside the First Tent. Do not ship a fixed forge in a Tent-Right camp. Keep the Gift terms row and L3 fence unchanged.

### P2-4. "Same machine, different place" is a label, not a mechanism; Wave 1 delivers locations, not the roster's promise

- Section 7's rows differ in prose ("wright's bench", "fitting bay") but the blueprint, verbs, terms (Open) and rule (nearest valid cell to the Well) are identical; every forge ends up about 2 cells from the Well [V in-tree `InstallationPostBuilder.cs:79-84`]. The "Consequence" row ("smaller kit-carrying burden", "infused gear before the wasteland") describes no mechanic.
- Against the pitch (`MACHINE-ROSTER-VISION.md`): choices about what survives (one authored find, one copy, at Cinderhold only), inputs decide outputs (no change), local consequence (none for a placed forge), visible state (the forge shows none), ordinary convenience stays (kept). The pitch's lead example, "a ruined workshop contains recognizable opportunities rather than interchangeable scrap", is not advanced by any Wave 0-2 row; the forge dossier gates the second find (V1.1, G2) and the plan never schedules it [R forge 3.11; V plan sections 4-5].

Fix: say plainly in the owner summary that Wave 1 is location work. Add a named, owner-gated content row to the plan: a second finite released find with three exclusive fractions placed where a forge is in reach. A canon-fitting candidate is the Concord's withdrawn frontier counters east of the Last Counter (`AbandonedCounterZoneA/B`, `lore.md` section 3 "restored tower" row; the Concord "released for salvage" idiom is already shipped, `lore.md` section 2) [I]. It needs the independent economy/canon critique the forge dossier already requires (M3a took 294 checks), and must not resolve "what is sitting on the east road". Do not claim per-town differentiation until plan-relative frontage (S24) exists.

### P2-5. Tine is recommended "for the regression value" (padding by admission)

Plan 4.2 says "Approve only for the regression value (fastest relapse at 150 turns, wet neighbours), or keep test-only" and section 9 decision 2 names the Tine and Slip forges as "the cheapest filler" for "every town" (while not recommending it) [V]. Regression value is exactly what a test-only row loaded through `InitializeFromJson` provides (the plan's own mechanism and the forge dossier correction C-a). A shipped row adds a permanent machine with no purpose to every world that visits Tine. Fix: change the recommendation to "decline as shipping data, keep as test-only", and remove "filler" as an option from decision 2 (see P1-1).

### P2-6. The Fee on a fixture (W2-B) recreates a problem the carriage dossier already solved, and splits saves

Plan W2-B puts a `Fee` on the Last Counter forge with the Envoy as present operator. Spec C7 defines presence as a living person within 3 cells; the Envoy is an ordinary mobile NPC ("keepers move normally", spec C11), so a player at the forge can find it closed because the Envoy walked away, which is the "absent-keeper UX" the spec leaves open. The carriage dossier notes the clean shipped idiom: a conversation choice on the keeper guarantees presence, so no operator-presence rule is needed (`machine-relay.md` R-1) [R]. Also, a Wave 1 forge in a visited save stays Open forever while a fresh world after Wave 2 gets the Fee, so two Last Counters behave differently, and the Fee charges for a station whose main alternative (pack assembly) does not offer re-forge or temper, so "names a free alternative" is only partly true.

Fix: do not put `Fee` on fixtures. Keep placed forges Open. If a Concord-run service is wanted later, make it a keeper conversation choice (ConcordAccount/carriage shape) so presence and payee are the conversation's, and write the spoken-end line only if it is a standing arrangement (R9).

### P2-7. Carriage's authored purpose is the Last Counter forge problem, so shipping the forge removes it

The carriage dossier's one real purpose: "do the forge work at Cinderhold, send the finished goods to the Last Counter, set out east lighter" because the post "has no forge" [R machine-relay.md section 3]. W1-A puts a forge at the post. What remains is "leave heavy salvage at the last post", which is weak, and the dossier's own purpose test says to park it if carriage is ever strictly dominated or unused. Plan decision 7 ("Carriage as an independent early lane") rests on a purpose W1-A removes. Also, Sill is a carriage endpoint only if the player contracts the well to the Concord (`PlaceConditions.json` `ConcordContract.MainWell`) [R], so a repair-path choice decides whether a machine exists; that is a legitimate consequence but should be stated to the owner.

Fix: run the carriage purpose test after W1-A exists and expect it may fail; do not schedule it early. State the repair-choice dependency in the owner summary.

### P2-8. The Salt-Vault infusion bench is thin and is carrying the justification for S13b

The "real gap P1" is an infusion with a narrow payoff (P2-3: bonus vs three monster blueprints) for a one-town body that needs a new Part, a new station kind token, S4 and S9 changes, a unique glyph and a voice gate. The shipped alternative (a forge at Sill, 5 cells away, or Cinderhold) stays. The plan's Wave 2 table says "salt spent on infusion rather than barter" as the consequence, but infusion consumes one mineral for one enhancement slot and does not change sale value (checked above).

Fix: keep S13b, but justify it as the cold-work bench (the assay dossier reaches the same conclusion, section 0.6), and list the Salt-Vault infusion as its optional first placement, not its reason. Do not let the infusion kind token drive S9 scope.

### P2-9. The Tally row cannot land in its verified window as written

The dossier's verified window is "NE front apron x=50..58, y=9..11" in front of the GoodsStore door [R settlement-tally.md:152]. That apron is "a 4-deep RoadStone apron" [R :119] and the builder never uses `RoadStone` (plan section 2; in-tree `Fits` takes only `Ground` tokens). It is also at least 10 cells from the Well at x=40, outside `Radius 9`. The row lists `Grass` in `Ground`, but Tally's builder whitelist is `Floor`/`StoneFloor`/`RoadStone` only (`TallyCompositionBuilder.cs:64`) [V]. The in-tree doc says Tally "does place" in seed 64 but not where [V decision 3]. The alternative "N-E court x=43..48, y=3..9" has no recorded ground type.

Fix: the window-membership test must run over at least 24 seeds and assert both the ground token and the window; until it passes, Tally stays `Pending(owner: S24 plan-reserved frontage)`. Also carry the report's `RentalPart` caveat: Tally's Quartermaster rents `LoanerDagger/Spear/Longsword` [V `TallyCompositionTests.cs`], and the forge picker lists any melee weapon that is not a component (`ForgePart.cs` "Weapon (re-forge / temper)" section) [V], so a rented loaner can be selected for temper. I found no value uplift from tempering, so this is not an exploit, but the forge dossier leaves it [U]; add the characterization test before the row ships.

### P2-10. The "14 places" ceiling and the owner-facing census are optimistic

Plan section 6 ends with "Installed or Present at 14 places if every gate passes". It counts rows the dossiers recommend not building: the assay desk at Marrowstye, Quillhold, Gantry and the Last Counter (assay dossier section 0: "no honest purpose ... do not build or place", Gate Q); the Posy finishing frame (bath dossier section 4: Posy "Not V1"; it also consumes `PitchpodResin` that the Posy Elder's concern needs, and the bath dossier says not to create `Pending` rows to fill the census); Slip's lamp-works and Lampwell/Spivenor rows that need cultures unparked. After Waves 0-2 on the plan's own recommendations the realistic count is the forge at Sill, Cinderhold, the Last Counter, plus Tally if its probe passes, and the Salt-Vault bench: 4 to 5 of 20 places, plus three canon-exempt and the pilots outside towns. That does not meet "some machines in every town" and the plan should not let 14 stand as the headline.

Fix: report the realistic number, name the cause (the dossiers' purpose audits find no honest opportunity for most places), and ask the owner to choose between accepting the ratchet and funding opportunity-first content (a finite find, a resident supplier, a local problem) ahead of apparatus. Remove the Posy row from the ceiling.

## P3

1. **Citation off by one.** The First Tent exemption cites `Lore/Factions/07_TentRight.md:119`; the "monument to a choice" sentence is line 118 [V grep]. The same cite is in the plan, `lore.md` and the in-tree `Installations.json`. The exemption itself is sound.
2. **Sounding-rod findability.** One `TinkerStock` row, Tinker in about 70% of shared-pipeline villages (`VillagePopulationBuilder.cs:132-134` [V]); no in-world mention. A player may never meet it in a given world. Acceptable for an optional item, but record "unmentioned" in the evidence and decide the First Tent Tinker question (plan decision 9) with Tent-Right "make almost nothing permanent" in mind.
3. **Marrowstye assay attached to `CurationSaltBench`.** The shipped bench says "the bench offers no preservation service" and `lore.md` section 2 calls it the exemplar of "instrument present, service withheld". An assay board on it is plausible only if issued by staff and read-only; keep it behind Gate Q (assay dossier) and the intent card. Not a Wave 1-2 issue.
4. **Draft strings.** "Set here to be used." is a design-register line with no voice owner; it is shown on every placed machine. Run it through the blind gate before S5 ships; the Concord card flags "free" wording (L15), so avoid "free to take" copies.
5. **Glyph collision.** Four stations share `n`; a Wave 2 body needs a unique glyph/colour (already plan risk 5). With graphics cancelled this is accepted; just do not describe glyph presentation as "found by players" in evidence.
6. **Cinderhold faction data** (map Concord vs concern Villagers, `lore.md` section 11 item 8) matters only if terms are ever enforced there; keep it on the owner list.

## Home/base check from this lens

I found no shared-contract blocker for later player installation or ownership: the marker carries `Origin`, `Owner` fails closed, positional-authority fields are linted, retention is one predicate, and carriage keys endpoints by post identity. The canon constraints the plan already records (Tent-Right base is never auto-guest-right or priced, catacomb patch and niches not claimable, Curation/Recension homes need a spoken end) still apply; the only new item from this critique is P2-3: do not create a fixed-forge precedent in Tent-Right camps that a Tent-Right base would then have to contradict.

## Verification record

Read in full or in the relevant ranges: `design/PLACEMENT-PLAN.md`; `design/ARCHITECTURE-SPEC.md` sections 0-10; `design/machine-forge.md` sections 0-5; `machine-relay.md` sections 0-4; `machine-resonator.md` sections 0-6; verdict sections of `machine-bath/pump/cradle/vat/assay/still.md`; `Docs/MACHINE-ROSTER-VISION.md`; `lore.md` sections 0-8; `presentation.md` sections 1-3; selected `settlement-last-counter/tally/wellmeet/salt-vault.md` lines.

Code and content read: `ForgePart.cs` (action rows and eligibility), `ForgeWeaponCommand.cs:80-100`, `ReforgeWeaponCommand.cs`, `MineralPreparationPart.cs`, `EnhancementPaleSalt.cs`, `TradeSystem.cs:25-40`, `RentalPart.cs`, `LastCounterCompositionBuilder.cs:177`, `TallyCompositionBuilder.cs:58-70`, `WorldMapAuthoring.cs:208-226`, `Objects.json` (Commerce values, `TinkersForge` text, Undead-tag scan), in-tree `Installations.json`, `InstallationPostBuilder.cs:60-135`, `Docs/MACHINE-INSTALLATIONS.md:70-100`, `git diff` of `OverworldZoneManager.cs`, root `Lore/History/08_MaterialCulture.md` (Tent-Right section) and `Lore/Factions/07_TentRight.md:100-124`.

Not verified: where the Tally forge lands relative to the dossier window; whether any other source of `ReleasedExtractionTool` or `RecoveredIronBlank` exists outside the Cinderhold placement (I found none in `Assets/Scripts` or `Content/Data`); whether `TryReforge` rejects non-forged weapons; whether tempering a loaner is blocked elsewhere; the economic effect of carriage fees against `RegionalValue` for every admitted blueprint (the dossier's bench is the check). No Unity, test, build or git write was performed.
