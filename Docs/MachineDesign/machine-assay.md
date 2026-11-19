# MACHINE V1 DOSSIER: Assay cabinet

Status: design synthesis only. Repo `/Users/steven/caves-of-ooo` at HEAD `c0c65d6f9`. No project file, Unity run, test, build or git write was made. All C#/JSON below is a sketch for the implementing engineer.

Evidence tags: **[V*]** I read the cited line in this session. **[R]** taken from a research report or the architecture spec, not re-read. **[I]** inference or design. **[U]** unverified; a named RED test or first-cycle step settles it. Lore authority is root `Lore/` only; `Docs/Lore/` is not used.

Inputs read: `design/ARCHITECTURE-SPEC.md` (all), `Docs/MACHINE-ROSTER-VISION.md`, `Docs/MACHINE-SYSTEMS-DESIGN.md`, `Docs/SYSTEMS-DESIGN-PRINCIPLES.md`, `Docs/MACHINE-RECLAMATION-PROCESSING.md`, `Docs/MACHINE-SYSTEMS-EXECUTION-PLAN.md` (milestones), `lore.md` (sections 0-11), the assay rows of all 25 `settlement-*.md` dossiers, and the shipped code listed in section 1.

---

## 0. Verdict

1. **Classification: `wave3-needs-new-system`.** The assay cabinet has **no honest purpose in the shipped world today**. Not "a thin purpose": none that survives the project's own rules.
2. All three of the user's example questions fail on verified evidence (section 1.1): the first is already answered for free by the process quote, the second has no subject (no pump entity exists, and no acid-on-metal rule exists to read), the third has no sample and no neutralizer content.
3. The shipped game's convention is **free, exact, rule-derived readers** (tonics, repair diagnosis, process quotes, live brew previews, even the "slippery" flag on a liquid layer). An assay that sells information the game could show for free is exactly the "degrade ordinary convenience to create demand" move the user forbids.
4. The project's own plan of record already puts the assay with samples, cultures and treatments at **M5, "Planned"** (`Docs/MACHINE-SYSTEMS-EXECUTION-PLAN.md:44`; `Docs/MACHINE-SYSTEMS-DESIGN.md:118`), after "sample provenance" exists. This dossier agrees with that ordering and shows why.
5. **Recommendation: do not build or place an assay desk in this program phase.** Keep it `Pending(owner: "assay Gate Q")` in the installation census. It must **not** be used to fill "a machine in every town" even though it has the widest lore fit (spec section 10 item 3 names it as such); lore fit is not purpose. Section 2 defines the exact gate (Gate Q) that would change this, and section 3 is the **shelf V1** (a complete, test-first design to build the moment Gate Q passes). Section 7 gives the slices and RED tests, with the qualification guard first.
6. If the owner wants the second consumer of the station contract proven in the near term, the spec's fire-free cold bench (S13b) is the better choice: it has a real purpose (`ForgeCold` processes without a forge); the assay desk does not.

---

## 1. Evidence

### 1.1 The pitch questions against the shipped world

Pitch: `Docs/MACHINE-ROSTER-VISION.md:41-45` [V*]: "Analyze a material, liquid, organism, or machine fragment... 'Which component can I recover?', 'Will this fluid corrode my pump?', 'What substance would neutralize this secretion?' Basic item descriptions remain freely available... the player choosing which question is worth consuming a scarce sample." Design-doc bar (`MACHINE-SYSTEMS-DESIGN.md:88` [V*]): "An answer changes a processing, repair or route choice; missing evidence and hidden knowledge remain unavailable."

| Pitch question | What the shipped game already does | Missing subject | Verdict |
|---|---|---|---|
| "Which component can I recover?" | Each process is quoted before payment: source id/count, station, output types and counts, spent fastening, action cost, destination (`Docs/MACHINE-RECLAMATION-PROCESSING.md`, planner section [V*]); `MaterialProcessService.Quote` (`MaterialProcessService.cs:23-39` [V*]) is the retained quote; the accepted UI adds a selected-action preview with the stated losses (`MACHINE-RECLAMATION-PROCESSING.md`, "Bounded M3a acceptance" [V*]); source blueprints carry readable action/loss labels and full descriptions [V*, same doc]. The recoverable set of every shipped row is also in plain data: six rows in `MaterialProcesses.json` (`dismantle, reclaim, point, cut_binding, guard_lashing, serrated_kit`, lines 4, 21, 38, 53, 68, 90 [V*]). | none: the answer is free before any spend | **Not honest.** Charging a sample to hear what the quote already says is a tax. The Cinderhold dossier reaches the same conclusion (`settlement-cinderhold.md:216`: "the existing reclaim preview already answers 'what can I recover?'") [R]. |
| "Will this fluid corrode my pump?" | Grep of `Assets/Scripts` for `Pump` finds only `BackgroundSaveQueue.Pump` and `InputHandler` save pumping (`SaveSystem.cs:683-711`, `InputHandler.cs:472-474`) [V*]; no pump entity or Part exists (machine 7 is "Proposed", `MACHINE-SYSTEMS-DESIGN.md:91`). The Cinderhold "mine pump" is a data repair site, not a fluid-handling machine [R]. The shared reaction table has 12 rules (`Assets/Resources/Content/Data/MaterialReactions/*.json` [V*]); the only acid rule is `acid_plus_organic`; there is **no acid-on-metal rule** (`cold_plus_metal` is the only Metal rule). | the pump, and the rule the answer would be read from | **Not honest.** An answer would have to be invented, which breaks the principle that behaviour comes from shared rules (`SYSTEMS-DESIGN-PRINCIPLES.md`: "Apply the same implemented rules throughout the world"). |
| "What would neutralize this secretion?" | Bandfrog has a `CausticSkin` Part (reflect 2, 25% poison for 3) and flavor-only Examine text (`Objects.json:35883-35935` [V*]); it has no `Corpse` harvest, so there is **no secretion sample item**. The only neutralizer text in content is a tonic line, "The bitter tonic neutralizes the poison." (`Objects.json:7614` [V*]), already shown exactly by `TonicExamineService` (header comment lines 8-17: "succinct but EXACT lines... Drift-proof by construction" [V*]). Oil/pitch/honey body coats already have a visible remover (`BodyCoatingCleanupActions.cs:10-60` [V*]). | a sample item and a neutralizer recipe | **Not honest.** No subject, no authored answer, and the nearest answer is free. |

Net: **zero of three** pitch questions qualify. The same finding is independently repeated across the settlement dossiers, written without reference to each other [R, all read]: "nothing a player needs assayed that Examine does not already say" (Ginmere `:185`); "no sample/provenance model and no hidden information exist to answer (item properties are already shown in Examine)" (Marrowstye `:266`, `:337`; Salt-Vault `:186`); "Needs samples with provenance (none in code)" (Lampwell `:136`); "0 assay/sample-provenance code exists; weak until samples with provenance exist" (merchant camps `:144`, `:116`); "The decision it would change here is thin today" (Drowned Ledger `:208`); "Basic Examine already informs, so questions must be deeper" (Sumphold `:164`); "No sample stream or decision it would change" (Slip `:166`); "No unknown samples or provenance here" (Tine `:96`). I re-verified that `Assets/Scripts` and `Assets/Resources/Content` contain **no** occurrence of "assay" (case-insensitive grep, 0 files [V*]). The word appears only in design docs (`MACHINE-SYSTEMS-DESIGN.md:69,88,118,127`, `MACHINE-SYSTEMS-EXECUTION-PLAN.md:44`).

### 1.2 The free-information inventory any future question must beat

Everything below is shown to the player without a machine or a sample [V*]:

- Examine facts: Type, Material, Composition, Repair requirement, Weight, Stack, Structure (`ExamineDocumentBuilder.cs:141-173`, repair fact `:175-187`); item mechanics (`:117`); "Field uses" for repair materials, flasks, FrogOil, timber/cord including the missing-guide line (`MaterialUseDescription.cs:13-103`, called at `ExamineDocumentBuilder.cs:122`).
- Flask contents and volume (`ItemExamineService.cs:41-52`).
- Repair diagnosis, required quantity and composition (`RepairablePart.cs:40`).
- Process quotes (above); live brew preview that resolves the mix before anything is spent (`BrewPreview.cs` class summary [V*]); brew discoveries logged by rule id (`BrewKnowledgePart.cs`).
- Tonic effects, exactly and drift-proof (`TonicExamineService.cs:8-17`).
- Even a liquid's decision-relevant behaviour on the ground: the cell readout appends "(slippery)", remaining turns and "firm footing" to liquid layers (`CellStatusReadout.cs:162-171` [V*]).

**What is genuinely undisplayed** (checked: `Combustibility`, `Conductivity`, `FlameTemperature`, `PerTurnDamage` appear in no reader under `Look/`, `Effects/` readers, `Entities/` or `Items/`; only mechanics read them [V*]): per-liquid numbers in `LiquidDefinition` (conductivity, combustibility, fire dampening, flame temperature, per-turn damage, stat/resistance modifiers, stickiness), per-entity `MaterialPart` numbers (combustibility, conductivity, porosity, volatility, brittleness), and creature special defenses such as `CausticSkin`. These are the only candidate "answers" the world has. But the project's precedent for a decision-relevant liquid fact is a **free reader line** (the slippery flag). So the right owner decision for these is "show it free" (an S5 reader contribution), which dissolves the assay's purpose here rather than creating it. This is owner decision D1 in section 2.

### 1.3 Foundations that already exist toward it (verified) and what each lacks

| Foundation | Where | Gives | Lacks |
|---|---|---|---|
| Process quote + transactional executor | `MaterialProcessRegistry.cs`, `MaterialProcessPlan.cs`, `MaterialProcessService.cs`; accepted M3a-c | Retained quotes, exact-selection custody, rollback, selected-action preview | **Outputs are Portable items only** (`MaterialProcessRegistry.Portable`, `Valid` [V*]); work kinds `ForgeCold`/`HandCutting` only. It cannot output a finding. Do not generalize it (item-only validators are pinned). |
| Exact-selection command pattern | `BodyCoatingCleanupActions.cs` (command string with zone/cell/target/identity tokens, re-validated in `TryAct`, `tx.BeforeCommit(Current)`) [V*]; `LiquidVesselPart.cs:15-22` registers four action providers in one `GetInventoryActions` handler (lines 18-21) [V*] | The way to add a flask-side verb without a new Part | none for V1 |
| Single-liquid vessel | `LiquidVesselPart.cs:6-11` (`Capacity=12`, `Volume`, `LiquidId`) [V*]; `LiquidVesselService`, `LiquidTransferActions` [R] | Finite unit accounting, no ground contact on transfer | No one-unit "spend to nothing" claim; volume reaching 0 must clear `LiquidId` (`ItemExamineService.cs:44-46` treats `Volume==0 && LiquidId!=""` as invalid) |
| Per-actor knowledge store | `KnowledgePart` (`NarrativeState/KnowledgePart.cs:11-33`: monotone int tiers by topic string, `ISaveSerializable`) [V*]; `BrewKnowledgePart` (permanent discovery log) [V*]; `NarrativeStatePart` global facts [V*] | A saved, additive place to remember "this player learned X" with no new saved Part | `KnowledgePart` is NPC-oriented; using it on the player is [U] (RED `KnowledgeOnPlayerRoundTrips`) |
| Bounded provenance | `ConnectedClayOriginPart` + `ConnectedClayProvenance` (`ConnectedClayProvenance.cs:7-76` [V*]) | The only shipped "provenance bits conserved across merge/split/consume" pattern (FireClay only, 2 bits, world key) | Not general; there is **no provenance on any other item** (grep of `Assets/Scripts` for "provenance": combat, clay, claims, lair stacks only [V*]) |
| Station contract, readers, installation placement | `ARCHITECTURE-SPEC.md` C3-C6 (S3-S7) [R] | Station kind lookup, receipts, disabled rows with reasons, readers, deterministic fresh-only placement | Not shipped yet; all are design |
| Faction-flavoured wording | `lore.md` section 8 [R] | Names and voice by faction | none |

### 1.4 Why "scarce sample" is structurally weak here (honest tension)

The pitch needs a **scarce** sample. In this game: (a) knowledge is a permanent personal state, so a question about a *kind* (for example "what does pitch do") is paid for once; (b) most samples are cheap or renewable (12-unit flasks refillable from pools, endless wells, stacks of cord); (c) the only non-renewable, decision-bearing specimens are *unique finite finds* (the released tool; later pre-Felling fragments, purchased rare strains, specimens), and those are exactly the things that **have no hidden manifest** today (the released tool's manifest is the public quote). So scarcity and hidden information coincide only for content that does not exist yet (cradle finds, vat strains, provenance-bearing specimens). This is why the plan of record pairs the assay with M5, and why building the apparatus first would be "apparatus before opportunity" (`MACHINE-SYSTEMS-DESIGN.md`, principle 1).

---

## 2. Gate Q (honest purpose) and what must exist first

An assay desk may ship only when **all** of G1-G7 hold for at least one registered question. Each is a test, so "no purpose" becomes a failing build rather than a claim.

| Gate | Requirement | Test that enforces it |
|---|---|---|
| G1 Hidden | The answer is shown by no existing reader, preview or quote | `AssayQuestionIsNotAlreadyFreeInAnyReader` (for each question's representative subject, the answer's key facts are absent from `ExaminablePart.BuildExamineLine`, `ExamineDocumentBuilder.Build`, `ItemExamineService`, `MaterialUseDescription`, `CellStatusReadout`, the process quote) |
| G2 Decision | Names at least one shipped command or choice it changes and an ordinary alternative that stays undegraded | validator requires `Decision` (command family id) and `Alternative` non-empty; `OrdinaryPathUnchangedWithAssayContentRemoved` |
| G3 Derived or finite | Answer derived from shared rules at ask time, or from an authored finite manifest; never a free-text table that can drift | `FindingChangesWhenTheUnderlyingRuleChanges` (mutate `LiquidDefinition` in-test; text follows); `AuthoredKindRequiresManifestRef` |
| G4 Scarce | The sample is non-renewable at the point of use, or the finding is recorded once and never re-sold | `RenewableSourceSampleRefused` (wells, pools); `RepeatQuestionOnKnownKindSpendsNothing` |
| G5 Leak-safe | If an identity is hidden, every reader and menu path that could reveal it is enumerated and pinned | `HiddenIdentityLeaksNothingInAnyReaderOrMenu` (enumerated matrix) |
| G6 Lore-safe | Subjects are an **allowlist**; protected subjects refused; strings passed the blind gate | `ProtectedSubjectsRefused` (table), `EveryAuthoredStringHasABlindGateRecord` |
| G7 Not required | No shipped command, repair, quest or trade requires a finding | `NoShippedCommandRequiresAFinding` (source pin over command/Part requirement lists) |

**What must exist first (smallest honest set), ranked:**

- **O-C (recommended, already planned):** *unique finite specimens whose decision-relevant property is a hidden authored manifest or a provenance*: pre-Felling fragments for the cradle, a purchased rare strain for the vat, a provenance-bearing mineral lot. Needs: the cradle or vat content (parked), or a general provenance Part (second consumer: vat strain identity, economy salt provenance, `ConnectedClayOriginPart`). This is M5. Lore wording is already safe (physical, practical answers only; `lore.md:117`, `:129`).
- **O-A (cheaper, owner decision):** *unmarked sealed vials* as authored fresh-only finds: a new item that holds a hidden `{LiquidId, Volume}` in its own Part, so **no existing reader reads it** (leak-safe by construction: the vial has no `LiquidVesselPart` until opened). Blind alternative: "break the seal" into an empty flask or onto the ground (true liquid, true exposure, per existing rules). Needs a voice-gated item, 2-3 finite placements, and careful authoring so blind opening is not punitive (no lava) and not free (flat vial value, not content value). Weakness: knowledge is per kind, so scarcity is thin (section 1.4); the opportunity is real, the sink is small. Allowlist for vial contents [I]: `oil, pitch, brine, sap, honey, bog-mire, acid` (all exist, `LiquidDefinitions/*.json` [V*]); never `iron-gall-ink` (section 4 landmine), never any deep-binding liquid.
- **O-B (not recommended):** hidden-defect goods at Concord traders ("fraud outward"). Canon likes it (`04_SaccharineConcord.md:87`), but it needs a change to stock generation and is the purest form of manufacturing demand.

**Owner decisions this dossier cannot make**

- **D1.** Are liquid and material behaviours "basic description" (free reader lines) or "deeper knowledge" (assay)? Recommendation: free, by the slippery-flag precedent (`CellStatusReadout.cs:162-171`); that makes Gate Q fail for liquids on purpose and leaves only O-A/O-C.
- **D2.** Commission O-A now, wait for O-C at M5, or neither.
- **D3.** Finding scope: per-kind permanent (recommended; `KnowledgePart` topics) versus per-instance.
- **D4.** Whether the assay ever carries a fee (default Open, per spec S10 gate).
- **D5.** Confirm the census stance in section 6 (assay stays `Pending`; it is not a filler).
- **D6.** Voice owner and blind-gate pass for every authored string (lore R10, L17).

---

## 3. The shelf V1: "assay desk, one sample, one question, one finding"

Build only after Gate Q and D1/D2. If D1 resolves to "free reader", skip Q2 and Q3 below and keep only identity for O-A vials (and note that blind-opening makes that nearly redundant; expect Gate Q to fail then).

### 3.1 What it is

A stateless, non-fire **station** of kind `assay` (a `StationPart` with `Kinds = {"assay"}`, per spec C5/S13b) plus one small **service** that spends exactly one unit of a carried sample, answers one registered question from shared rules, and records the finding as player knowledge. It stores nothing: no queue, no timer, no stock. It is therefore trivially movable and installable later (spec section 9 hooks), and `Retention: None` in the registry (E07 passes: no state-bearing Part).

### 3.2 Player verbs

1. **Look / Examine the desk** (world reader via spec C6): name, "Set here to be used." (installation line), the questions it answers with the per-question sample cost, and the limits line (section 3.7). A "Works here" line is generated from the question registry, so it cannot drift (spec S13a pattern).
2. **Test a sample** (one world-menu or pack-menu row per eligible sample and question; see 3.3). The row lives on the sample, the accepted M3a/M1a pattern (`LiquidVesselPart.cs:18-21` provider list; `MaterialProcessInputPart` rows): the sample teaches the player the desk exists. When no desk is in reach the row is listed **disabled with its reason** (spec S3): "No assay desk within reach."
3. **Read the finding**: message-log line at once; thereafter a "Tested:" line in the sample's reader for every carried flask of that kind (spec C6 `IDescribeState` on `LiquidVesselPart`).
4. **Walk away.** Nothing is pending; there is no job to cancel.

### 3.3 Inputs, units, questions (finite)

- **Sample:** exactly **1 unit** from one carried `LiquidVesselPart` (`Volume - 1`), or one carried sealed vial (O-A). The subject must be on the **allowlist** (liquid id table in `AssayQuestions.json`); `MaterialPart` tags or `CompositionPart` categories never authorize a sample (`MACHINE-SYSTEMS-DESIGN.md:63`).
- **Policy:** the last unit is not sampled (`last-unit` refusal), so the finding always stays on a surviving physical object and the vessel never empties through the desk. Equipped, held, claimed (live recovery claim), or unstackable-state samples are refused under `MaterialProcessPayloadPolicies.CanMergeProcessingInputs`-style rules [R, spec C8.2].
- **Questions in the shelf V1 (3, each 1 unit, each derived from `LiquidDefinition`/shared rules, never an authored table):**
  - `identity` "What is it?": reveals the liquid's display name (meaningful only for O-A vials).
  - `fire-and-spark` "Will it burn, or carry a charge?": bands derived from `Combustibility`, `FlameTemperature`, `FireDampen`, `Conductivity`.
  - `skin-and-hold` "What will it do to skin and stay on?": bands derived from `PerTurnDamage`, `Adsorbence`, `Sticky`, `StatModifiers`, `ResistanceModifiers`. (Footing is not asked: it is already free at the cell readout.)
- Because each question costs a unit and a 12-unit flask stays above 1 unit, the **choice of question is real but cheap**; the dossier states this honestly rather than selling scarcity (section 1.4).

### 3.4 Outputs, loss and waste

- **Output:** knowledge only: `KnowledgePart.Reveal("assay.<question>.<liquidId>", 3)` on the player (additive, monotone, saved by the existing `ISaveSerializable` path), a log line, and reader lines. No item output, so no finding-slip duplication, trade or custody exploit.
- **Loss:** the unit is **spent in the test**: destroyed, not recovered, not poured, not a dram sink; no ground exposure; preview says so. Destination: none.
- **Failure:** any refusal or callback failure restores the unit (`InventoryTransferSnapshot` pattern) and writes no knowledge.

### 3.5 Cost and time

One ordinary action, declared (`ActionTime.OneAction`, spec C4), identical in the world and pack menus; cancelling at the preview is free. **No fee in V1** (terms are Open; Concord Fee and Tent-Right Gift terms arrive with S10/S11). No elapsed job, no reconciler, no per-turn cost. Sample cost: 1 unit.

### 3.6 Preview (shown before payment, same text at commit)

> Test at the assay desk: "Will it burn, or carry a charge?" Spends 1 of 11 pitch (10 remain). Written to your own record; no slip, no fee. The sample is spent in the test. Cannot tell: where it came from, what it means, or anything living.

The quote is revision-pinned so a changed station, sample or question between preview and commit refuses with nothing spent (spec C8.3 pattern).

### 3.7 Limits line (instrument limits live on the instrument, `lore.md:344`)

"It reads what the sample is made of and how it behaves. It does not read where it came from, what it means, or anything alive." No Urqu, no layers, no history, no persons.

### 3.8 Blocked, failed, interrupted, saved states

| Condition | Result | Code (diag `craft/AssayRefused`) |
|---|---|---|
| No desk within reach / desk fouled / two providers / stale zone | row disabled with reason; commit refused | `no-station`, `faulted`, `two-providers`, `stale-zone` (spec `StationReadiness` codes) |
| Sample not allowlisted, protected, or renewable-source | not offered; refusal names why | `not-eligible`, `protected`, `renewable` |
| Last unit | refused, nothing spent | `last-unit` |
| Sample held, equipped, live claim, changed state | refused | `held`, `stacks-incompatible` |
| Question already known for this kind | refused, nothing spent | `already-known` |
| Callback moves desk or sample between preview and commit | refused, rollback | `changed-during-payment` |
| Actor purse | untouched (no fee) | n/a |
| Save/reload | nothing in flight; knowledge persists; old saves lack the topics and load clean | n/a |
| Unsupported (a protected subject, an unregistered question) | an honest line, never a guess | `unsupported` |

### 3.9 Alternatives the player keeps (at least two genuinely different)

1. **Use the sample unexamined** (existing pour, transfer, coat rules act on the true liquid; the cost is the exposure you learn from). Ordinary flasks are unchanged.
2. **Break the seal blind** (O-A only): full volume now, true exposure per existing rules, no machine.
3. **Ask a person** (an existing conversation path or an authored Apothecary/Elder line; [U]: confirm the conversation system can express a knowledge-granting line without a new action).
4. **Sell or carry it sealed** at the flat vial value (never content value, pinned by test).
5. **Leave it alone.** Nothing depends on a finding (G7).
6. **Another desk** (a different town's); findings are player knowledge, so they travel.

### 3.10 Consequence outside its own menu

- **Hazard and liquid rules:** the knowledge changes whether and where the player pours, coats or carries a liquid, and the *true* liquid then acts under the same shared rules (pool, coat, ignition, electrified wet). The desk adds no effect of its own.
- **Economy:** flat vial value versus content value creates a real "open, sell, or test" choice without a new currency or sink (`Ledger` untouched; spec C7 no-sink default). [U] economy review: vial value must not exceed expected content value (finite-packet printing).
- **Save/home:** knowledge persists; the desk is stateless and portable-ready.
- **Presentation:** reader and Look lines name the desk, so placed machines are discoverable beyond a glyph (spec risk 5).

---

## 4. Canon: names, operators, landmines

### 4.1 Name and voice (drafts; each needs an intent card and the blind gate, lore R10/L17)

- **In-world name:** **"assay desk"** (Concord idiom, plain and untechnical; `lore.md:127`, bank row 4 at `:312`). Blueprint id `AssayDesk`, display "assay desk". Avoid "cabinet" in player text for Concord places only if the voice owner prefers; the user's pitch word is "cabinet" and the glass-case art reuse (`presentation.md:174`: `CurationConservationCase` frame 41 or `CurationToolCabinet` frame 56, recoloured) fits either.
- **Faction variants (data, later):** Recension "collation table" ("attested, collated, variant"), Pale Curation "condition board" (triplet voice, never "dead"), Tent-Right "salt scales" (Gift terms only), surface "apothecary's bench". Draft reader line (Concord register, not shippable): "Assay desk. One unit of your own sample, one question, written down. Spent in the test; no claim outstanding."
- Reader strings are engine-neutral for refusals; any faction-voiced line passes the blind gate (`lore.md:35`, rule R10; spec C5).

### 4.2 Operator NPC roles (shipped roles only; V1 needs none)

V1 is `Open` and needs no present operator; named roles only **explain** (existing dialogue; spec S24 mention later). Natural keepers by faction [R, `lore.md:130`, `:356`]: Concord `ConcordFactor` (Cinderhold), `Quartermaster` (Gantry exchange), `SaccharineEnvoy` (Last Counter); Recension `Scribe`, `RecensionScribe`; Pale Curation `FilerClerk`, `CurationSorter`, `CurationIntakeFiler`; Tent-Right `SaltMaster`; surface `Apothecary`, `Arcanist`. NPC-run assay is S20 and out of V1.

### 4.3 Landmines the machine must avoid (ids from `lore.md`)

| Id | Constraint on the assay |
|---|---|
| R1, L2 | No text attributes intent, wanting or offering to Urqu; no source drawn from Urqu or Sari-Pyrite |
| R2, L1 | Subjects are an **allowlist**; never the tenth fire, woven doll, Glassblown remnant, Gin Frogs (`MYSTERY-LEDGER.md` #8), what the Bower sees, what the Root dreams, layer count or dating. The allowlist is itself the enforcement point; protected scenes stay inert (`AuthoredWildernessZoneIDs`, `OverworldZoneManager.cs:771` [R]) |
| R3, R4, L6 | Physical-material answers only, flagged partial; never underreads, never un-scrapes, never dates or counts layers (Recension crimes; Quillhold dossier `:163`) |
| R8, L9 | Findings keep Choir-Iron, Pale-Salt, Spore-Block, Memory-Marble, Tepuibone, Mute-Stone, Glow-Quartz distinct; no "metal" or "battery" collapse; no score or stat that merges them |
| L5 | No body, preserved specimen, bog-taken, enzyme disposal; Salted's chamber and Marrowstye disused wing untouched |
| L8 | No ancestral bone, plaque, niche or patch sample; the patch is "tended, never used" |
| L19 | The deep-binding liquids in the registry (`memory-bath`, `held-breath-lacquer`, `felling-counter-resin`, `bower-resin-amber`, `veined-pulse-mycelium`, `tepuibone-slurry`, `choir-wort`, `choir-mirror-mucilage`) are never eligible subjects, never described |
| L11 | Never maps or forecasts Urqu-bleed; never exposes the closure ledger as a gauge |
| L3, R7 | Tent-Right places: assay only the player's own samples, never a guest's goods; no dram price; no machine verb on the guest-cloth pole, locker or well |
| L16 | No technology nouns in player text (regulator, cell, transmitter, power) |
| L17 | No recorded blind-gate pass exists for any shipped machine string; every V1 string needs one |
| L18 | Service terms are unenforced at the shipped forge and still; V1 keeps `Open` and states the limit |
| L15 | Concord voice contradicts itself on "free"; prefer "claim waived" |
| Gantry conflict | The Gantry dossier rates assay "strong" (`settlement-gantry.md:178`); `lore.md:250` says the Sixth Crate dispute is "a person's testimony, not a machine reading", and the dispute concerns a crate held by a guest under the oath. **Do not wire the desk to the Sixth Crate** (`Concerns/Gantry.json` `gantry.manifest` [V*]: approaches `read` and `sign`) |
| Wellmeet note | `wellmeet.scales` ("Short Weight", approaches `true` and `book` [V*]) is an authored concern about a real mis-weighing; a calibration test could one day be a third approach, but that is a separate concern slice with owner and lore sign-off, not a V1 consumer |
| Quillhold ink | iron-gall ink exists as a liquid (`iron-gall-ink.json`) and is the same substance as rental and embalming ink (`lore.md:52`); an assay that "tells" this resolves a canon quiet horror. Exclude it from the allowlist |

### 4.4 Placement fit if and when Gate Q passes (inference [I] from the dossiers and `lore.md` sections 5-6)

- **Plausible with a real sample stream:** Gantry (own samples only), Tally, Last Counter, Cinderhold (weakest need: the reclaim preview already answers), Sill (apothecary's bench; starting village; plain language, no cosmology), Marrowstye and Salt-Vault (condition board; no body subjects), Quillhold (collation table; physical only), Sumphold (peat and mire samples; no bog-taken), Wellmeet (Gift terms; low priority).
- **Exempt or poor:** the First Tent, the Quiet's Door (machines forbidden; the Elder's "oil does the same work" question ends in protected cosmology), Olderdeep, Spivenor, Deepest Cathedral, Ginmere, Stillleaf, Slip, Posy (aesthetic scoring forbidden), Tine, Spread wayhouse, the Drowned Ledger (fences tight, decision thin), Morrowfast (non-canon proposal; later), Lampwell (needs the parked vat and strain trade).

---

## 5. Architecture fit (against `ARCHITECTURE-SPEC.md`)

**Reuses (verified or spec-planned):** `StationPart`/`IStationCapability` kind `assay` (C5; S4, S13b); declared `OneAction` and disabled rows (C4, S3); `IDescribeState` readers (C6, S5); `InstallationMarkerPart` and `InstallationPostBuilder` at priority 4650, `Retention: None` (C2, C3, S2, S6, S7); `InventoryTransaction`/`InventoryTransferSnapshot` exact-selection commands (`BodyCoatingCleanupActions.cs`); `KnowledgePart` for findings; diag category `craft`; deterministic placement by `FormationSelector.StableIndex`.

**New Parts introduced by V1:** none of its own. The desk is a `StationPart` instance; findings live in `KnowledgePart`; the flask verb is a static provider called from `LiquidVesselPart.HandleEvent` (one added line beside the existing four, `LiquidVesselPart.cs:18-21`). O-A would add one Part (`UnmarkedContentsPart`), namespace `CavesOfOoo.Core`, public default-constructible fields, no `ZoneID` fields (spec C1 hygiene).

**What the spec lacks (this machine needs):**
1. **An information output.** The process engine (C8) outputs items only. Add a small private `AssayService` beside it that reuses the transaction primitives and the quote/preview convention. Do not generalize `MaterialProcessRegistry`. Promote to a shared "information action" contract only when the survey resonator or relay is a second real consumer (spec R1).
2. **A one-unit sample claim for liquid vessels.** `IngredientClaim` (S12) covers item stacks; vessels need a private `SampleClaim` following `LiquidVesselService` conventions, including clearing `LiquidId` at zero volume.
3. **A question registry** (`AssayQuestions.json` + validator, modelled on `MaterialProcessRegistry`). This is the **second** new data table after `Installations.json`; spec section 8 says "extract `DefinitionTable<,>` on the second". Decide then; do not pre-build.
4. **An allowlist/protected-subject check as data** (spec R8 "canon limits are data"), shared later with resonator and relay as `InstrumentLimits` (`lore.md:344`).
5. **Free-reader contribution for flasks** (`LiquidVesselPart : IDescribeState`, S5) for the "Tested:" line; this is the same mechanism D1 would use to make liquid behaviour free.

**Saves:** additive only; `FormatVersion` stays 8; no existing Part field changes; old saves lack the knowledge topics and load clean (RED below); zones are never retrofitted (fresh-only placement). `KnowledgePart` is already in the saved-Part golden; adding it to the player is a runtime `AddPart`, not a schema change.

**Diag (every gate emits):** `craft/AssayCommitted {question, subject, spent:1}`, `craft/AssayRefused {code, question, subject}`, `furniture/StationRefused` (spec), `event/ActionDisabled` (spec), `worldgen/Installation*` (spec).

**Performance:** runs at menu and action time only; scans the pack and one 3x3 station query; no per-frame or per-turn allocation; scratch lists, no LINQ (spec section 3).

**Determinism and RNG:** answers are deterministic functions of data; O-A contents are chosen with `StableIndex(zoneID + ":vial:" + n, k)`, never the pipeline `rng`.

**Home/base hooks:** stateless; no `ZoneID` or station-coordinate fields; a player-installed desk is `InstallationMarkerPart{Origin="Player", RetainZone=false}` (no state to retain); a future stateful variant that holds specimens would need `RetainZone=true` and a work-store, and is deferred.

---

## 6. Placement and census stance

- `Installations.json`: **zero assay rows** until Gate Q passes. Each row would need the mandatory `Purpose`, `Alternative` and `Basis` (E09). The honest `Purpose` cannot be written for any town today; the spec's own validator is the gate working as designed.
- Census: classify every lore-fit town as `Pending(owner: "assay Gate Q", note: "no sample stream; see machine-assay.md")` and the exempt/poor list above as `Exempt` with the cited source. Do not list the assay as `Installed` anywhere, and do not count it toward "every town".
- If placed later, first rows for mechanism diversity: **Sill** (proof of reuse; stations already present; apothecary's bench, plain voice) and **Last Counter** (supply-post bench in the trade room, expedition prep; `settlement-last-counter.md:97`: 82/66/97 always-free interior cells), both `Retention: None`. Exterior-only placement is the generic builder's limit; in-room anchors are S24.
- Old worlds: visited towns never gain a desk (stated limit, spec C3).
- Presentation: glyph fallback unless a body is approved; reuse of a glass-case frame is possible; four stations already share glyph `n` (spec S23 ratchet requires a unique glyph+colour pair).

---

## 7. Slices and RED-first tests

Cadence: one cycle is one isolated-clone EditMode run (about 5 minutes). Every slice is RED-first, carries counter-checks, an adversarial file where two or more taxonomy surfaces apply, a cold-eye pass with both audit angles, diag pins, and (for placement) actual play in a fresh world plus a cached-save control. Dependencies: S1, S3, S4, S5, S6/S7 from the spec; S13b `StationPart`.

| Slice | Content | Cycles | Gate |
|---|---|---|---|
| **A0 Gate Q record** | `Docs/MACHINE-ASSAY.md` with section 1.1 table, D1-D6 decisions, chosen opportunity, voice-owner assignment. No code. | 0 | owner |
| **A1 Qualification guard** | `AssayQuestions.json` validator and the G1-G7 tests with a test-assembly subject; production registry stays empty | 1 | A0 |
| **A2 Service** | `AssayService`, `SampleClaim`, knowledge write, rows on `LiquidVesselPart`, refusals and diag | 2 | A1, S3 |
| **A3 Station and readers** | `StationPart` desk blueprint (test-only until placed), disabled rows, reader and "Works here" lines | 1-2 | A2, S4, S5, S13b |
| **A4 Opportunity packet (O-A only)** | `UnmarkedContentsPart`, vial blueprint, allowlisted contents, 2-3 fresh-only finite placements, leak matrix | 3 [I] | D2, blind gate |
| **A5 Placement** | one `Installations.json` row per approved town, S7 matrix | 1 + play | A3, S6, S7 |

### RED-first test names

**A1 guard (these make "no purpose" a failing test):**
`AssayQuestionIsNotAlreadyFreeInAnyReader` (G1) and its counter `UnregisteredQuestionDoesNotTrigger`; `QuestionWithoutDecisionRejectedAtLoad`; `QuestionWithoutAlternativeRejectedAtLoad`; `QuestionWithoutRuleOrManifestRefRejectedAtLoad`; `FindingChangesWhenTheUnderlyingRuleChanges`; `AuthoredKindRequiresManifestRef`; `RenewableSourceSampleRefused`; `NoShippedCommandRequiresAFinding`; `ForbiddenVocabularyRejected` (charge, mana, battery, quartz, pyrite, urqu intent, light, glow; spec C10 list); `EveryProtectedLiquidRefused` (each L19 id, table-driven, nothing spent); `ProtectedSubjectAllowlistIsDataNotCode`; `OneInvalidRowDoesNotDropOthers` (per-row drop, spec C3 rule); `RegistryDefaultsAreInertUntilRowsExist`.

**A2 service (positive plus counter for each):**
`TestingOneUnitSpendsExactlyOneUnitAndRecordsKnowledge`; `LiquidSampleSpillsNothingAndCreatesNoPool`; `LastUnitRefusedAndNothingSpent`; `VolumeNeverReachesZeroThroughTheDesk` and counter `ManualPouringStillEmptiesAndClearsLiquidId`; `FindingTextEqualsDerivedBandsForTheTrueLiquid`; `UnallowlistedLiquidRefused`; `MaterialTagNeverAuthorizesASample`; `RepeatQuestionOnKnownKindSpendsNothing`; `DoubleSubmitSpendsOnce`; `ThrowingCallbackRestoresUnitAndRecordsNothing`; `SampleMovedBetweenPreviewAndCommitRefuses`; `EquippedHeldOrClaimedSampleRefused`; `ConservationSweep` (random flask volumes: before = after + 1 spent); `NoDramsMoved` (purse unchanged); `KnowledgeOnPlayerRoundTrips`; `OldSaveWithoutTopicsLoadsAndIsNotKnown`; `SavedPartContractGoldenAdditionOnly`; `DiagPinned` for `AssayCommitted` and each `AssayRefused` code.

**A3 station and readers:**
`DeskWithAssayKindIsAStation`; `BareNamedBlueprintWithoutPartIsNotAStation`; `ShippedForgeAndStillAreNotAssayStations` (counter); `RowListedDisabledWithReasonWhenNoDeskInReach` and `RowEnabledWhenDeskAdjacent`; `DeclaredOneActionChargesExactlyOneTurnInBothMenus`; `CancelAtPreviewChargesNothing`; `PreviewTextEqualsCommitText`; `PreviewShowsSpendRemainingAndLimit`; `DeskReaderListsQuestionsCostsAndLimitInBothReaders`; `WorksHereLineGeneratedFromRegistryAndCannotDrift`; `FlaskReaderShowsTestedLinesOnlyForKnownKinds`.

**A4 opportunity packet (O-A):**
`HiddenIdentityLeaksNothingInAnyReaderOrMenu` (enumerated matrix: world and inventory Examine, Field uses, transfer, pour, trade name, log lines, `GetDisplayName`); `SealedVialHasNoLiquidVesselPartUntilOpened`; `BreakingTheSealMovesTrueContentsIntoAnEmptyFlaskOrOntoTheGround`; `VialValueIsFlatAndIndependentOfContents`; `BlindOpeningNeverPlacesLavaOrDeepBindingLiquids`; `VialPlacementIsDeterministicFreshOnlyAndFinite`; `CachedZoneNotRetrofitted`; `ContentsChosenByStableIndexNotPipelineRng`.

**A5 placement (spec S7 matrix applied per row):**
`FreshTownPlacesExactlyOneDeskAtTheDeterministicCell`; `PlacementIsSeedSensitiveButStablePerSeed`; `OtherOwnersIdenticalWithRowRemoved`; `RngUntouched`; `FloodConnectivityAndStairsPreserved`; `SolidBlueprintKeepsAStandingCellAndRoutes`; `MissingBlueprintOmitsOnlyTheRow`; `ExemptPlacesNeverReceiveTheDesk` (First Tent, Quiet's Door, Olderdeep, Spivenor, Deepest Cathedral, Ginmere, Stillleaf); `ForbiddenFactionsNeverReceiveInstallation`; `GantryDeskNeverReadsTheCrateOrGuestGoods`; `PendingCountMayOnlyDecrease`.

**Adversarial file (`AssayAdversarialTests`), taxonomy rows:** state atomicity and rollback (partial failure at each step), parser malformed JSON (empty, duplicate ids, non-token ids, negative units, oversize text), stacking (flask stack vs unique vessel), save/load reflection, cross-actor (non-player actor with a flask), anti-exploit (renewable-well farming, question re-asking, vial arbitrage, knowledge duplication), boundary inputs (null actor, zone, desk, flask, question), diag dispatch (refusal emits `StationRefused` and `AssayRefused` in order), multi-instance (two desks in reach resolve deterministically), self-referential gates (actor equals station owner), probability (none: no RNG).

**Acceptance (when built):** native green; actual play in a fresh world: find the desk by Look or Examine, read the questions and limits, open the pack menu on a flask, see the disabled row away from the desk and the enabled row at it, spend one unit and read the finding, leave, return, F5, fresh Continue (the knowledge and the reduced volume persist); cached-save control: a town already visited shows no desk; ordinary flask transfers, pours and douses are unchanged. Evidence limits stated as the project's usual (CUA-only images, glyph presentation, Linux and Deck unexecuted).

---

## 8. Pitch behaviours deferred, and why

| Behaviour | Deferred to | Why |
|---|---|---|
| "Which component can I recover?" | after cradle or pre-Felling find content with hidden manifests | the quote already answers it for every shipped row; needs a fragment class whose manifest is not public |
| "Will this fluid corrode my pump?" | after a pump (machine 7) and an acid-on-metal reaction rule exist | no subject and no rule; inventing an answer breaks shared-rule honesty |
| "What would neutralize this secretion?" | after an organism sample item, a neutralizer recipe and consent rules exist | no sample or recipe; Bandfrog has no harvest; organism samples touch Bloom, Catcher and Curation sensitivities (L5, L12) |
| Provenance ("where did this come from?") | M5, with a general provenance Part | only FireClay has bounded provenance (`ConnectedClayProvenance.cs`); second consumer is vat strain identity |
| Partial or uncertain findings, multi-sample quantitative questions | V2 | need a calibration and uncertainty model; V1 answers are derived bands |
| Finding as a physical record that other systems consume (quote modifiers, S14) | V2 only with a qualified consumer | otherwise a slip is a gate or a modifier with no purpose; V1 gates nothing |
| Fees, operator presence, Withheld/Gift terms | S10/S11 | gated on the independent critique; V1 is Open |
| NPC-run assay | S20 | needs `StationPerception` and a keeper goal |
| Organism and living samples | later, with consent design | Bloom-watch, Choir encasement, Curation chambers are protected (L5, L12) |
| Anything touching protected subjects or place scenes | never | allowlist is the enforcement point |

---

## 9. Verification record

**Read this session [V*]:** the architecture spec (all); `MACHINE-ROSTER-VISION.md`; `MACHINE-SYSTEMS-DESIGN.md`; `SYSTEMS-DESIGN-PRINCIPLES.md`; `MACHINE-RECLAMATION-PROCESSING.md`; execution-plan milestone table; `lore.md` sections 0-11; the assay rows of all dossiers; `KnowledgePart`, `FactBag`, `NarrativeStatePart`, `QualityRegistry`; `ExaminablePart` description builder; `ExamineDocumentBuilder` (sections, facts, repair fact); `ItemExamineService` (head, vessel branch); `MaterialUseDescription`; `TonicExamineService` (head); `RepairablePart.cs:40`; `LiquidDefinition`; `MaterialPart`; `MaterialReactionBlueprint` and the 12 reaction rule names; `LiquidVesselPart`; `BodyCoatingCleanupActions`; `CausticFilmPart`; `Bandfrog` and `MawToad` blueprints; `ConnectedClayProvenance`; `BrewKnowledgePart`, `ReagentPart`, `BrewPreview`; `MaterialProcessRegistry` (head, `Portable`, `Validate`); `MaterialProcessService` names; `CellStatusReadout.DescribeLayer`; `Concerns/Wellmeet.json`, `Gantry.json`, `Tally.json` approaches; `MaterialProcesses.json` row ids; grep results for assay, pump, neutralize, corrode, provenance, diagnos, contaminat.

**Corrections to inputs:** (1) the Gantry dossier's "strong" assay rating conflicts with `lore.md:250`; the lore rule governs (section 4.3). (2) The architecture spec calls the assay bench "the widest lore fit" (section 10 item 3, `:708`); true for lore, not for purpose. (3) `MACHINE-SYSTEMS-DESIGN.md:69` says existing Examine, discoveries and reputation "provide entry points"; verified: they provide the *free answers* that make most pitch questions redundant.

**Not read [U], each settled by a named test or first-cycle step:** the full world pool reader beyond `CellStatusReadout.DescribeLayer` (`NoQuestionDuplicatesAFreeReader`); whether `KnowledgePart` can sit on the player without a blueprint edit (`KnowledgeOnPlayerRoundTrips`); whether the conversation system can express a knowledge-granting NPC line (alternative 3 in section 3.9); the exact `MaterialProcessPayloadPolicies.CanMergeProcessingInputs` signature for flask-adjacent claims; economy balance of a flat vial value; waysite and `SpreadExplorationWorksites` hooks for O-A placements; blind-gate status of every string.

**Honesty bounds:** this dossier proves the absence of a qualifying question from the code and docs I read; it cannot prove one will never exist. It does not claim that thin or cheap findings are worthless in play, only that the project's rules (no padding, free ordinary description, shared-rule answers) leave none that survive today. No test, build, Unity run or git write was performed.
