# Repaired kitchen pan readout contradiction — source recommendation

Initial pre-implementation review, 2026-10-24, by /root/projection_cold_review, independently checked by child /root/projection_cold_review/look_focus_draft_review. At that checkpoint no Assets/tests/Unity changes had been made for this fix, and root owned ordinary Play. Subsequent implementation and acceptance are recorded below.

## Severity and cause

P2: successful repair is visually and mechanically real, but the title/FOCUS continue to call it broken and Description instructs the player to repair it again. This undermines the state cue and makes the repaired station look unusable. It is not evidence of failed repair, a wrong native frame, or necessarily a stale focus snapshot.

- Objects.json:54572 permanently authors `split clay batch pan`.
- Objects.json:54593 permanently authors `Repair the split with two fire clay, then commission Orven: ...`.
- Entity.GetDisplayName:484–497 simply returns stored RenderPart.DisplayName plus a stack suffix. Both menu titles and the Look header use it.
- ExaminablePart.BuildDescription:111,135–136 uses that name and raw `Text`.
- ExamineDocumentBuilder.Build:104–106 independently reads raw `examine.Text`; its title at135 uses GetDisplayName.
- RepairablePart.Describe:35–40 and KitchenBatchPart.Describe:367+ already report the current repair/job separately, hence the visible contradiction.
- Repairable.TryRepair publishes the real dirty cell at160. LookFocusRefreshTests already proves dirty-cell refresh; refreshing again cannot fix incorrect source text.

## Minimum reliable fix

Pure presentation projection, no save mutation or repair-time one-off rename:

1. Add two small pan-specific readout methods on KitchenBatchPart, e.g. `ResolveDisplayName(string authoredName)` and `ResolveDescription(string authoredText)`. A shared pure predicate requires exact `ConnectedBatchPan`, owned actual KitchenBatchPart, exactly one owned nonportable Repairable with recipe `clay-batch-pan` and `Repaired=true`. Do not call Configure, Bound, Reconcile, world scan or read worker/stock/job state. Use no HP/mood/permission gate. Duplicate or foreign faults fail closed.
2. Name substitution only for the exact canonical `split clay batch pan`, yielding `clay batch pan`. Perform before Entity.GetDisplayName appends its existing stack suffix. Preserve arbitrary renamed objects. Require actual owned Render for this optional projection, so a foreign part cannot lend the label.
3. Only the exact canonical authored description is substituted. Repaired version retains the same shallow-pan/free-oven/batch costs/time/interruption information, with `Commission Orven: ...` instead of the obsolete repair imperative. Preserve arbitrary custom prose. Saved `Examinable.Text` and its `Description` alias remain raw; no migration or side-effect in a getter.
4. Add an effective flavor getter such as `ExaminablePart.GetDescriptionText()` that consults only its owned actual pan part. Use it from both legacy BuildDescription and ExamineDocumentBuilder's Description section. Updating only one route leaves the demonstrated popup defect.

Expected runtime files: KitchenBatchPart.cs, Entity.cs, ExaminablePart.cs, ExamineDocumentBuilder.cs. No blueprint identity, JSON rename, renderer, inventory, state or timing changes. This handles the current already-repaired checkpoint and future reloads automatically, unlike a repair-event-only rename. A raw JSON neutralization alone would not repair the current saved object's fields.

## First actual RED test / focused counters

Use the real factory `ConnectedBatchPan`; ConnectedKitchenFixture's synthetic pan has blank name/text and cannot reproduce this defect.

Add a small focused fixture alongside MaterialRepairTests, borrowing DensityLootTestScope/HotbarSaveFixture isolation as appropriate:

1. **Factory real repair positive:** live Player adjacent to factory pan; show canonical split name and raw repair instruction before; supply actual2 FireClay via InventoryPart.AddObject; execute Repairable.TryRepair. Assert actual two-unit consumption, Repaired and unchanged BlueprintName. Afterward GetDisplayName and structured document Title equal `clay batch pan`; legacy BuildExamineLine and BuildWorldExamineLine plus document Description contain no `Repair the split` or stale split-name. Current state still contains recipe.RepairedText. Assert raw RenderPart.DisplayName and Examinable.Text/Description fields are unchanged. This is expected behavioral RED today.
2. **Fault authority counter (parameterized small group):** same canonical factory owner with Repaired true, then false / foreign ParentEntity / wrong recipe / PortableEquipment / duplicate repair / wrong blueprint. Substitution disappears; no proof is borrowed from a malformed or unrelated owner. Do not call gameplay reconciliation to make the text pass.
3. **Custom content counter:** valid repaired factory pan with custom Render.DisplayName and custom examine.Text retains both exactly; no broad `.Replace("split",...)` or author-content stripping.
4. **Already-repaired roundtrip:** preserve canonical raw fields through SaveWriter/SaveReader, restore actual owner references, and verify effective name/both descriptions immediately after load. No rename hook, turn or configuration required. Existing MaterialRepairAdversarialTests.NativeEntitySavePreservesFaultAndComposition is the save pattern.
5. If explicit submitted FOCUS coverage is desired, append one case to existing LookFocusRefreshTests using Fixture("ConnectedBatchPan"): give real2 clay to its Player, call TryRepair, Frame once without cursor movement/manual dirty, assert Snapshot.Header and submitted Sidebar contain `clay batch pan` and not the old split title. The existing fixture already exposes exact Target,Player,Zone,Frame and Snapshot; its content factory is private, so supplies can come from a bounded real factory or small exposed test helper. This pins the actual dirty→focus path without another rendering fixture.

Relevant controls after focused GREEN: MaterialRepairTests, MaterialRepairAdversarialTests, ExamineDocumentTests, ExamineDocumentAdversarialTests, LookFocusRefreshTests, ConnectedKitchenTests. One cold source review and actual reopened repaired-pan popup/FOCUS capture close this narrow user-visible contradiction.

## Honesty bounds

This recommendation is source-only. No test was imported/run, no repaired pan was mutated, and no runtime patch was applied by this reviewer. The preceding332 distinct native art-test passes do not prove the new text fix. Root should record the fresh behavioral RED before implementation and the post-fix native/live readout separately.

## Implementation checkpoint (2026-10-24, after actual RED)

Root observed the fresh native `KitchenPanReadoutTests` run: 11/11 expected broken-name failures, job `f9379b49e69041769b2d011162b3b914`, saved as `east-kitchen-readout-red.xml` and summary. The earlier zero-case stale-discovery attempt is not behavioral evidence. Tests were reviewed independently by `/root/projection_cold_review/look_focus_draft_review` before production, source-only.

The approved four-file pure readout patch is now saved and frozen for root native GREEN. It adds an exact, owned, unambiguous repaired-pan projection on KitchenBatchPart, applies its name before the existing stack suffix, and shares ExaminablePart.GetDescriptionText across legacy and structured readers. Raw Render.DisplayName/Text/Description and all job state remain untouched. No new dependency on Configure/Bound/reconciliation, worker presence, inventory, world time or stock was added. `git diff --check` passed for these four files. The eleven cases were not changed after RED.

Current limits: native GREEN and reopened actual popup/FOCUS are pending; the fresh LookQuery assertion is not a submitted-sidebar test. Final independent production review is requested from the same child. Root alone operates Unity and owns docs/commit.

Root native confirmation:11/11 GREEN in jobed54f3afc171489884bf463f9e1c6ede, exact XML east-kitchen-readout-green.xml. First pre-RED discovery returned0cases and is preserved as empty-discovery.xml, explicitly not counted. Actual behavioral RED was the subsequent11/11 expected name failures. Broader regression and ordinary reopened repaired reader remain pending.

Root closure: GREEN11/11 plus152/152 affected regressions. Already-repaired ordinary Continue/Examine matches current title/FOCUS/prose; see east-kitchen-readout-fixed-reader.png. Independent visual P2 closed; final console0errors/2existingMetalwarnings and isolation/view restored.

## Final independent source review — 2026-10-24

Reviewer: `/root/projection_cold_review/look_focus_draft_review`; implementation/source confirmation: `/root/projection_cold_review`. The child independently read the settled four-file production diff and `KitchenPanReadoutTests` after genuine native RED, without modifying files, compiling, running tests or operating Unity. **No actionable P1/P2 finding.**

The review confirmed that the presentation predicate checks the current owned pan and its unique, owned, nonportable repaired fault; canonical-only string substitution preserves custom names/prose and the original serialized fields. Both legacy and structured descriptions share the effective flavor getter, the stack suffix remains singular, and reading performs no job binding, reconciliation, timing change or save migration. A possible extra part-lookup short circuit was classified as a nonblocking micro-optimization without measured impact; it was not added after the freeze.

Final read-only source confirmation found the four runtime changes unchanged from the GREEN handoff. Receipt inspection confirms `east-kitchen-readout-green.xml` **11/11** and `east-kitchen-readout-regression.xml` **152/152**, all passed with zero skips. Together with art **26/26** and its **306/306** regression, these four XML files contain **495 distinct test cases**. The earlier empty discovery is not evidence; the accepted behavioral RED remains eleven actual broken-name failures.

The narrow fixture proves a fresh Look query, not the submitted sidebar by itself. Root separately reopened the already-repaired saved pan through ordinary Continue/Examine and captured the corrected title, FOCUS and prose in `east-kitchen-readout-fixed-reader.png`; the independent visual review closed the reported P2. Those live findings are root-operated acceptance, not a Unity run performed by either source reviewer. This closes the repaired-pan wording defect only, not all kitchen service states, hardware performance or complete graphics acceptance. Sources/tests remain frozen after this record.
