# Recommendation after release R3: make regional materials intelligible

18 September 2026. Read-only recommendation; no production changes, tests or Unity executions. Read against the current isolated candidate at `/tmp/coo-regional-verification-20260917`, with the latest state document read from live `/Users/steven/caves-of-ooo/Docs/GAME-STATE-2026-09-17.md` because that document is not in the clone. The state document's **bottom checkpoints**, not its historical RS28 summary, establish R1/R2 progress. R3 explicit regional opt-in remains the prerequisite; the trade/faction opacity polish has its separate R113/R115/R116 gates.

## Select one gap

**When the player receives fire clay, silver sand or ward oil, the existing inventory Examine action should tell them what useful native action it enables, what else they need, and where to start.** The explanation must be visible while the inventory is open. Start with these three regional repair goods, not every material or an encyclopedia.

This is a good next slice because it connects already working requests, goods and settlement services. It improves the ten-minute prepare/travel/solve loop without adding another quest system, production economy or arbitrary recipe. Fire clay also has an immediate, genuinely competing use in Morrowfast: a quiet bell clapper instead of the free loud setting.

## Evidence and corrections

Paths below are relative to the project root; line references are the inspected candidate, before any R3 changes.

| Finding | Source | Implication |
|---|---|---|
| Release calls for discoverable uses and competing demands, using normal starting supplies | `Docs/RELEASE-VISION.md`, “Crafting and resources,” “Power, preparation and alternatives”; latest `GAME-STATE-2026-09-17.md` §§8–10 | Prefer a legible existing use over adding recipes or handing out the developer material kit. |
| Regional iron jobs pay fire clay; Sumphold recovery pays silver sand; Wellmeet recovery pays fire clay | `Assets/Scripts/Gameplay/World/RegionalSituations.cs:42–52` | Rewards should lead into a player-understandable decision after the receipt. |
| Generic merchants already stock all three repair materials | `Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs:790–824` | This also helps purchased goods; it must not depend on the item having been a quest reward. |
| Most items already have an Examine action | `Assets/Scripts/Gameplay/Entities/ExaminablePart.cs:54–81` and `:91–133` | Do not claim a missing verb. The generic action emits a MessageLog line, usually only name/flavor/effects. |
| Only tonics/brews receive the inventory modal description path | `Assets/Scripts/Presentation/UI/InventoryUI.cs:1116–1121`, `:1144–1152`, `:1315–1321`; `Assets/Scripts/Gameplay/Items/TonicExamineService.cs:28–40` | Repair goods use ordinary Examine, whose log is hidden behind the fullscreen inventory. Use the existing announcement-over-inventory flow instead of a new panel. |
| Crafting already has real previews; tinkering already describes selected recipes | `Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs:89–125`; `InventoryUI.cs:2461–2504` | Do not describe a missing crafting system or duplicate its preview logic. This slice starts at the *material*, before the player has found the relevant service. |
| Manual repairs consume one material, require the corresponding physical guide, and leave the guide intact | `Assets/Scripts/Gameplay/Settlements/SettlementManager.cs:138–159`, `:190–211`, `:248–269`; `SettlementRepairDefinitions.cs:5–15` | Say “carry the guide,” not “learn its recipe,” “use a forge,” or “this material repairs everything.” No bit cost is involved. |
| Tracked repair sites are Sill and exact-profile Wellmeet | `Assets/Scripts/Gameplay/Settlements/SettlementSiteDefinitions.cs:20–28` | Generic town furniture is not a repair service. A changed/missing runtime POI must suppress a specific destination claim. |
| The elder provides the manuals through actual conversations | `Assets/Resources/Content/Conversations/Villagers.json:529–542`, `:557–570`, `:613–627` | “Ask the local elder” is implemented. Do not claim every merchant sells these books: generic repair stock contains the goods, not these guides. |
| Fire clay has a second existing use | `Assets/Scripts/Gameplay/World/MorrowfastQuests.cs:116–140` | After the existing cord diagnosis, one clay enables the quiet clapper; the bare/loud setting costs no material. Do not make examination accept or advance that quest. |

The name `LanternOilRecipe` is misleading if read as a crafting API: it is a carried Item checked by the repair service. Likewise, none of these three repair goods is interchangeable with alchemy LampOil or mineral PaleSalt. Instructions should be derived from the actual consumer contract and protected by wrong-item controls.

## Bounded player experience

Keep one `Examine` choice. For recognized repair goods it opens a readable, wrapped announcement over the inventory, preserving the selected item and returning to the same inventory panel when dismissed.

Examples of the information to convey (wording is illustrative, not a frozen copy contract):

- **Fire clay:** “One measure can rebuild a settlement oven. Carry an oven builder's guide and speak to its farmer. The material is spent; the guide is kept.” Separately: “Morrowfast's bell can use one measure for a quieter clapper after its fault is diagnosed; the bare setting uses none.”
- **Silver sand:** “One measure and a carried well maintenance manual can give a damaged settlement well a stable repair. Speak to its keeper. The sand is spent; the manual is kept.”
- **Ward oil:** “One measure and a carried lantern oil recipe can give a settlement watch lantern a stable repair. Speak to the warden. The oil is spent; the recipe is kept.”

Show the required guide's human-readable name and whether the player currently carries a qualifying copy. An optional destination line may name/map-locate Sill or Wellmeet only when the current `WorldMap` and `SettlementSiteDefinitions.IsTrackedVillage` support it. A destination line is **a location of the practice**, not a promise that a living recipient and unfinished site are currently available. If the zone is already cached, suppress unavailable/dead/destroyed recipient claims or say the site has already been repaired. Never call `GetZone` to inspect/repair/spawn a destination, and never write Field Notes or accept work merely by examining an item.

A fresh source-only implementation should begin with the plain use/requirement explanation and actual local service state. Map-derived destination guidance can remain a small second step within this slice **only** if it reuses the existing authoritative map/known-site query without generating or inventing availability. The exit criterion is an actionable, truthful lead, not a new route marker feature.

## Small implementation shape

- New engine-independent read-only helper, for example `MaterialUseDescription.TryBuild(actor, item, context, out description)`. Recognize exactly the three native blueprint contracts. Return no help for an unrelated renamed item, malformed/null item or an unrecognized material; do not claim universal support based on display names or the broad `Mineral` tag.
- Reuse `SettlementRepairDefinitions` for material/guide identifiers. Read native carried-item eligibility through the same inventory queries the consumers use. Keep the authored short explanations next to this bounded map and pin them against actual native outcomes; do not clone settlement repair state or perform a dry-run repair that mutates it.
- In `InventoryUI`, route these recognized items' existing Examine choice to the established announcement path. Avoid adding a second Examine alongside the event-supplied one. Preserve unrelated items' existing actions, tonic effect descriptions and normal action ordering. Read current item ownership again when selection executes so a stale row is not treated as a usable carried item.
- If ordinary world Examine is enhanced too, call the same builder; do not maintain different claims for a dropped versus carried copy. This is optional polish, not permission to rewrite all ExaminablePart behavior.
- Build text on explicit opening/selection only, not per frame or per camera redraw. No world-entity scan, save field, new state machine, asset, blueprint or mechanic is necessary.

Likely touch set: one new Items helper, a narrow InventoryUI action branch, focused tests and a living material-guidance document. Shared repair definitions only need a change if extracting a small existing requirement descriptor prevents literal drift. Do not opportunistically change repair eligibility, recipe availability, stock quantities, guide distribution, mining law or payments.

## Verification gates

1. **Actual RED first.** Three real factory-created goods must expose a visible description through the actual inventory action path; today they reach hidden log-only Examine. Check displayed useful action, correct guide and consumption, rather than exact prose. Wrong material, same display-name impostor and plain item controls must retain their own normal behavior.
2. **Truth against consumers.** For each advertised repair use, actual `SettlementManager` plus shipped village state/conversation fixtures: guide+good succeeds, missing guide or wrong material refuses, one unit is consumed, guide remains, stable stage persists and repeat repair does not consume again. Fire-clay bell test pairs diagnosed quiet one-unit use with free loud choice and undiagnosed rejection. These pin the existing mechanics; they do not authorize altering them to match explanatory text.
3. **No phantom service.** Exact Sill/Wellmeet positive versus ordinary village, removed/changed POI, already repaired site and cached missing/dead service owner. Description construction must not increase cached-zone count, spawn entities, consume inventory, accept/release/complete work, change reputation or mutate notes. If live availability cannot be established cheaply, use conditional wording rather than invent it.
4. **UI and lifecycle.** One Examine row; material modal actually appears above inventory; text fits at the shipped 80×45 layout; dismiss restores the panel/cursor/camera; missing/stale item fails safely; tonics and an unrelated item's actions remain unchanged. Include repeated opening, stock purchases, normal inventory and post-load items.
5. **Native ordinary-player proof.** Use the already verified opening/first resource route to receive fire clay without developer grants, inspect it using real input, then use the existing Nemm/cord/quiet-bell route. Capture description plus outcome. A separate actual Wellmeet fixture can obtain its guide through the real elder conversation and exercise one stable repair; label any synthetic travel/setup explicitly rather than claiming an entire ordinary pilgrimage. Inspect all three material descriptions at gameplay text scale.
6. **Closeout.** Focused paired/adversarial tests, independent source review of consumer truth, current full suite without exemptions, exact source/meta publication and honest native screenshot receipt. This is event-driven UI, so do not add a continuous profiler subsystem or claim a frame-rate improvement. If implementation adds a hot-path query, apply the project's real-session profiling gate before acceptance.

## Why not the other attractive next steps

A complete middle-game faction chain is still the larger release priority, but benefits from knowing players can recognize and use the supplies they already earn. Universal material codices, new resource-processing chains, crafting grants, offscreen production and a general ecological director are wider changes. First resolve this concrete usability seam and observe whether a new player can explain their material's use without source knowledge.

The completed deliverable would be **three intelligible useful goods**, with one ordinary native use demonstrated. It would not certify all crafting progression, promise a new campaign arc, or imply that an inspected item has created an obligation.

## Root adoption / verification sweep (after R3 gates)

The next implementation slice will keep the existing inventory Examine action and display a bounded description for FireClay, SilverSand and WardOil. The staged 18-case fixture exercises real InventoryUI and InputHandler announcement handoff without requiring a proposed new API. No production or test Assets have been changed for R4 yet.

| Verified premise | Implementation decision |
|---|---|
| Ordinary Examine already exists and logs behind fullscreen Inventory | Route recognized goods through the existing announcement overlay; exactly one material Examine row. |
| Tonics already have a special description plus inherited ordinary Examine | Preserve their existing commands in this slice; do not turn the material repair into a universal inventory redesign. |
| Native repair eligibility uses InventoryPart.CanConsumeOne | Use that exact read-only membership/quantity predicate for carried material and required guide, including execution-time recheck. |
| Guides are carried items, not learned crafting recipes | Say the material is spent and guide retained; name actual native guide and responsible resident. |
| Fire clay quiet bell has diagnosis prerequisite and free bare alternative | Explicitly preserve those conditions; item inspection never starts or advances that quest. |
| Dynamic destination guidance needs more map/owner queries | Prune it from this first slice. Explain the conditional repair practice and local roles; do not advertise live distant actors or create zones. Morrowfast bell is described as a conditional use, not an active task. |

This is CoO-original interaction polish, with no parity claim, new artwork, stock grants or save fields. Description building runs on explicit item-menu selection only, not a renderer/frame loop; no new cache or continuous update. Existing repair/bell mechanics remain the authority. Files will be a small read-only description helper plus metadata, narrow InventoryUI routing, tests, native acceptance and this living document. Actual RED and all native/full gates must complete before publication.

## Implementation log

- **R124 RED (previous agent, 18 Sept 05:27 UTC):** 18 cases, 7 intended failures (material guidance hidden in the gameplay log), 11 controls passing, zero compiler errors.
- **Implementation (previous agent):** `MaterialUseDescription.TryDescribe` plus one narrow `InventoryUI` branch (`examine_material`), as planned. **R125:** focused 44/44.
- **Handoff gap (found 19 Sept):** after R125 the previous agent extended the native harness with `InspectMaterialAndBell`, which calls `InspectCarriedFireClay()` — a method never written. The clone did not compile; the last green run predates the edit. **R126** reproduced it (CS0103, which also masked whether any test assembly compiled).
- **Root completion:** implemented `InspectCarriedFireClay` with native keys only (I → Tab → arrows → Enter → Examine → Enter → Enter → I; reflection observes, never selects). Added `MaterialGuidanceAdversarialTests` (20 cases: boundary inputs, cross-actor ownership, empty stack, consumer-parity matrix against the private repair gate, lookalike materials, renamed goods, save round-trip, purity). **R127:** 64/64, zero compiler errors; the sweep found no helper defect.
- **R128 native:** all 81 checks passed, but validation failed (exit 4) — the batch validator requires exactly 23 captures and R4 adds two. Raised the exact count to 25, pinned both labels and added all 11 material checks to `RequiredChecks`. **R129 native: 81/81, 1,010 steps, validated, frozen sources, clean restoration.**
- **R130 full: 15,010/15,010**, zero failures, skips, inconclusive or compiler errors; 416 s.

Root inspected both R129 material captures at 1080p. The modal wraps within its box, shows the missing-guide status plainly and returns to the same row; the bell capture shows the promised outcome ("You spend one fire clay to sleeve the clapper…"). Review detail: [coverage review](Verification/MaterialGuidance/coverage-review.md).

## Publication

The exact 10-file candidate ([manifest](Verification/MaterialGuidance/candidate-manifest.json)) and compact R124–R130 receipts are committed with this document to local main. The clone adaptations (`run_editmode.py`, `run_common.py`) and its ProjectSettings were excluded; after publication, live Assets were compared file-by-file against the tested clone with no remaining source difference. No remote push, save migration or release certification. The live editor was idle in edit mode; its next refresh will import the change, and whether its loaded assembly has picked it up should be confirmed in the editor.

Remaining limits: silver sand and ward oil are EditMode-covered, not native-captured. The description gives no destination for a damaged site (deliberately deferred). Destination guidance is the natural follow-up if playtesting shows players stall there; the documented release priority remains one connected middle-game chain (GAME-STATE §20, step 3).
