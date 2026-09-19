# R4 material guidance — cold-eye and coverage review

19 September 2026 UTC. Reviewer: root agent, against the candidate in the independent clone before publication. Every sentence the player reads was traced to the native code that performs the action it describes.

## Truth against consumers (claim → authority)

| Player-visible claim | Verified against | Result |
|---|---|---|
| One measure repairs the oven / well / lantern | `SettlementManager` `OvenRebuild`, `ManualRepair`, `LanternReforge` each call `ConsumeInventoryItem` once for the exact material | True |
| The material is spent; the guide is kept | Guide gated by `HasInventoryItem` (read-only), material by `ConsumeInventoryItem` | True |
| Farmer / keeper / warden | `TeachBaker` ("teach the farmer"), `TeachCaretaker` ("well-keeper"), `TeachWarden`; elder dialogue names the keeper and warden | True |
| "(carried)" / "(missing)" guide status | Helper mirrors `HasInventoryItem` exactly: top-level `inventory.Objects`, exact blueprint `==`, `CanConsumeOne` | Exact parity; pinned by an adversarial matrix against the private consumer |
| Ask the settlement elder for the guide | `Villagers.json` elder nodes `GiveItem` `OvenBuildersGuide`, `WellMaintenanceManual`, `LanternOilRecipe` | True (conditional wording: "about the damaged site") |
| Bell: ask Nemm, inspect the cord, then one clay makes a quiet clapper; the bare setting is free | `MorrowfastQuests`: `accept-bell` starts it; `InspectCordCommand` requires it active and sets `BellDiagnosed`; `RepairBellCommand` requires diagnosis and consumes one `FireClay`; `LoudBellCommand` consumes nothing | True, including after completion (the setting stays changeable; re-sleeving from loud costs another clay) |

## Four questions

- **Q1 symmetry.** `examine_material` mirrors `examine_tonic`: announce, clear popup, render, return. Its refusal branch keeps the menu open, matching the file's "a refused command retains its exact menu" convention. Text is built in `OpenItemActionPopup` on explicit open, never per frame.
- **Q2 consistency.** Both branches emit `event` diag records (`MaterialExamined` / `MaterialExamineRejected`) with actor and target at top level, like sibling inventory records.
- **Q3 counter-checks.** Carried/missing toggled both ways; wrong material, renamed impostor, lookalikes (LampOil, PaleSalt), a guide examined as a material, an empty stack, another actor's copy, an uncarried copy, and a stale selection all refuse. Both diag emissions pinned, including rejection emitting no success record.
- **Q4 docs versus code.** The plan pruned destination guidance; the shipped text names no settlement and advertises no live recipient, matching the plan.

## Findings

- 🔴 **Fixed before publication: the clone did not compile.** The native harness called `InspectCarriedFireClay()`, which had never been written; its edit postdated the last green run (R125). R126 reproduced it (CS0103). The method was implemented with native keys only (I → Tab → arrows → Enter → Examine → Enter → Enter → I; the inventory opens on the equipment panel, so Tab is part of the ordinary path).
- 🟡 **Fixed: the native validator pinned exactly 23 captures.** R4 adds two, so R128 passed all 81 checks and still failed validation (exit 4). The count, both labels and all 11 material checks were added to `RequiredChecks`, which makes the gate stricter, not weaker.
- 🧪 **Added: the dedicated adversarial gate** (`MaterialGuidanceAdversarialTests`, 20 cases). It found no defect in the helper; its value is regression rails, especially the consumer-parity matrix, which fails if the helper is ever changed to search nested containers the repair cannot reach.
- 🔵 **Bounded, not fixed:** the text does not tell the player *where* a damaged site is. The plan deliberately deferred map-derived destinations. A player holding fire clay far from Sill or Wellmeet has a truthful but location-free lead.
- 🔵 **Out of scope, noted for the backlog:** Morrowfast's ground examines as "pink stone" (`TepuiStone`) while rendering olive-green; visible in the R129 bell capture's sidebar.

## Honesty bounds

Native proof covers fire clay only, earned from two real deliveries, inspected and then spent on the quiet bell with a repeat-cost control and F5/F6 restoration. Silver sand and ward oil descriptions, guide-carried branches and the Sill/Wellmeet repairs are covered in EditMode, not native captures. The captures were inspected at 1080p; readability for new players and accessibility settings are not claimed.
