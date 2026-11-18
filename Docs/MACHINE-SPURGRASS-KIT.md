# Spurgrass to serrated kit — qualification and contract

2026-11-17. Follows the accepted M3c composite foundation (`c600701d0`) and the read-only source selection in
`Verification/MachineSystems/SpurgrassBaseline/source-selection.md`.

**Status: native RED→GREEN complete; independent design critique incorporated; actual play and Mac package still open.**
Execution ownership of Unity and Git moved to this Claude session on 2026-11-17 20:30 CST. The Codex
`caves-of-ooo-machine-foundations` heartbeat is still ACTIVE, but its runs hit a usage limit (3–7 s polls; weekly window
resets about 2026-11-22). Pause it or hand it back before it resumes real work; this document and the execution plan are the handover.

## Player choice

2 harvested SpurgrassSpine + 1 KnotflaxCord → 1 existing SerratedEdgeComponent, by an ordinary pack action away from any
station, with a cutting tool carried. It is an alternative to buying the kit (WeaponsmithStock, 40% roll), to keeping the
spines as recoverable thrown weapons, and to keeping the cord for binding, bandages, snares and well-line repair. Nothing requires it.

## Verification sweep: premises against the code

| # | Premise (source note or plan) | What the code says | Consequence |
|---|---|---|---|
| S1 | Spines are `Item` + `Stacker` + `Handling` + `MeleeWeapon` with no Material/Composition | Confirmed (Render, Physics, Examinable, Stacker, Handling, MeleeWeapon, Commerce; value 3, weight 1) | none |
| S2 | "Explicitly admit canonical MeleeWeapon and Handling payloads" | **Confirmed and concrete.** `MaterialProcessPayloadPolicies` registered scalar policies for ten Part types and **not** `MeleeWeaponPart` or `HandlingPart`, so `MaterialProcessTemplate.Capture` returned null and the spine was refused. Both Parts hold only public scalar fields | both registered with the existing scalar policy |
| S3 | Stacking of harvested spines is unproven | `CropYieldService` creates **one entity per unit** and packs them through `AddCraftedUnitWithinCapacity`; identical fresh spines merge in `StackerPart.CanStackWith` (blueprint, display name, `StackPayloadIdentity`). Pinned: `FreshSpinesPackIntoOneStack` | two fresh spines share one stack |
| S4 | Existing throwing/recovery must be preserved | `ThrowItemCommand.Extract` separates the thrown unit with `StackerPart.RemoveOne()`; `RecordLanded` marks the landed unit; `ThrowingRules.Track` attaches `ThrownRecoveryPart` on first preparation and **nothing ever removes it** (only `Invalidate()`). `StackPayloadIdentity` ignores it, so recovered spines merge with fresh ones today. Pinned by passing baseline tests | see S5 |
| **S5** | **(new) Declaring spines a process input is safe** | **False as the guards stood.** For a declared input `CanSplitProcessingInput` required every Part to carry a scalar policy and `CanMergeProcessingInputs` required equal Part counts. `ThrownRecoveryPart` had no policy, so a spine stack carrying the mark could not be split (`RemoveOne()` null, `CloneForStack()` throws) and a recovered spine stopped merging with fresh ones. **Reproduced** with the catalogue row and weapon admission alone and no guard change: `ARecoveredSpineMergesBackIntoItsStack`, `TwoRecoveredSpinesMergeWithEachOther` and `AStackCarryingThrowTrackingStillSplitsOffOneUnit` went red | shared refinement below |
| **S11** | **(new, from the critique) The hazard is wider than spines** | `EnsureStatusEffectsPart` attaches a permanent, usually empty `StatusEffectsPart` the first time any effect touches an item. It has no policy either, so **today** a cord or leather-binding stack that has ever had an effect cannot be split or merged as a declared M3c input. This is a latent bug of the shipped M3c slice, reproduced by `AStackThatHasHadAnEffectStillSplitsAndMerges` for Cord and Leather before any spurgrass change | covered by the same refinement |
| S6 | Work permission is open | `WorkKind` is `ForgeCold` (a forge) or `HandCutting` (`CuttingToolPart.FlexibleStock`, carried only by the Dagger). `MaterialCuttingWorkReceipt.Resolve(actor, source, zone)` takes the **source** stack, and here the source is the **cord**: the flexible stock the knife cuts to length for the lashing. The spines are additional rigid inputs and are never "cut" | reuse `HandCutting`; the menu says "lash", never "cut spurgrass" (pinned); no new capability or tool |
| S7 | Equipped inputs must be refused | Additional inputs resolve only from `InventoryPart.Objects`. **Correction:** `SpurgrassSpine` has no `EquippablePart`, so `EquipCommand` refuses it and a wielded spine cannot exist. The structural claim that remains and is pinned: only *packed* spines count; a spine on the ground, even in the actor's own cell, is never an ingredient | `OnlyPackedSpinesAreIngredients` |
| S8 | Old saves | Saved cords already carry `PreparationRecipePart` with `braid_binding`; the `guard_lashing` row selects it with `SourceRecipeId`. Spines are admitted by blueprint template; no new Part on any saved owner | no migration, no retrofit (token-graph round trip pinned) |
| S9 | Output is creatable | `SerratedEdgeComponent` has Render, Physics, Examinable, Stacker, Commerce and `WeaponComponent` (already scalar-admitted); proven by the catalogue validator against real content and by the produced-kit stacking test | GREEN |
| S10 | Material | No Material/Composition tag on spine or kit; plant points lashed by cord; the kit is an authored finishing component | no organic-to-metal conversion, no composition inference |

## Contract (as implemented)

**Catalogue row** (`MaterialProcesses.json`), `serrated_kit`: Mode `Process`, WorkKind `HandCutting`, SourceBlueprint
`KnotflaxCord` × 1 with `SourceRecipeId=braid_binding` (the existing saved permission), AdditionalInput `SpurgrassSpine` × 2,
Output `SerratedEdgeComponent` × 1, action "lash serrated edge kit". Value and weight do not rise, so nothing is minted
(pinned); the player pays one ordinary action and keeps the knife.

**Payload admission.** `MeleeWeaponPart` and `HandlingPart` join the scalar policy list. Anything else on a spine (an
enhancement, a temper marker, edited weapon stats, a foreign tag or property, an unknown Part) still refuses, as for every other input.

**Shared guard refinement (the S5/S11 fix).** An internal `IMaterialProcessTrackingPolicy` marks *bookkeeping* Parts that
gameplay attaches to any item and never removes. Two are built in and nothing outside the file can add one:

| Part | Inert when |
|---|---|
| `ThrownRecoveryPart` | no thrower, item, blueprint or zone recorded and serial 0 (every receipt in the pack, since pickup invalidates it) |
| `StatusEffectsPart` | `EffectCount == 0` |

- `CanSplitProcessingInput` accepts a Part with a scalar policy **or an inert tracking Part**.
- `CanMergeProcessingInputs` compares the two inputs with their inert tracking Parts removed, in order, exactly as
  `StackPayloadIdentity` already ignores group-0 Parts.
- `MaterialProcessTemplate.Matches` tolerates an inert tracking Part even for a *plain* ingredient, so a marked stack and an
  unmarked stack are equivalent choices (lowest identity wins) rather than an "ambiguous" refusal.
- The captured state compares **inertness only, never `Revision`** (critique P1-b). Consuming the last unit of a stack
  invalidates its receipt and bumps `Revision`; a revision comparison would make the very commit that consumed the stack fail its own check.
- A **live** tracking Part (an outstanding recovery claim, an active effect) is private state, exactly like an unknown Part:
  split, merge and ingredient admission all refuse. The conservative direction is deliberate.
- `Register` refuses to replace a built-in tracking Part's policy (`ArgumentException`).
- An authored template can never declare a tracking Part (`ValidTemplate` is false), so no catalogue output can create one.

**Disclosed limit (independent review P2).** Two spines must share ONE pack stack. A recovered unit returned by the Recovery
skill re-enters the pack through `AddRetrievedObject`, which does not merge, so throwing one of a harvest's two spines and
recovering it leaves two stacks of one and the menu entry silently disappears; dropping and picking both up again (ordinary
pickup merges) restores it. This is the single-stack limit in the scope cuts made visible, not a new defect, but it is a
discoverability problem: the menu has no "blocked because" row (a known gap, P8 of the architecture audit). The principled fix is
multi-stack ingredient claims in the shared process engine, queued for the architecture work rather than patched here.

**Disclosed limit.** Reserved-bed harvest output carries a `ReserveYieldPart` bookkeeping Part that is not on the allowlist
(unreachable with shipped content: reserved beds hold Marlroot, Pitchpod and Claspbean). A spine or cord stack carrying it would
refuse, conservatively. A spine carrying an enhancement or a rented-item Part is not an ingredient (the Part is private state
the recipe could not preserve). The shipped spine is never enhanced or rented by ordinary play; this is stated, not hidden.

## Interactions pinned by native tests

- throwing one spine from a stack, recovering it and merging it back keep working with the process loaded (a regression
  guard that went red without the refinement);
- a stack that has carried a throw mark, or an empty effects Part, is consumed exactly once, including the exact-quantity
  case that bumps the mark's revision;
- a pending measured-throw preparation on a consumed stack is invalidated and the actor still round-trips;
- processing never touches a unit thrown onto the ground;
- a retained quote refuses a changed stack; partial inputs plus an output merge have exact outer rollback (`FailAfter`);
- a saved cord and saved spines work without new Parts or old tokens; non-player actors obey the same rules;
- the produced kit stacks with an authored kit.

## Consumer comparison (to be confirmed in actual play)

On the Binding slot the kit gives +1 penetration and the authored `Bleeding,20,1d2,15,0` on-hit effect; GuardLashing gives
+2 hit and −1 penetration; a leather binding +1 hit; a plain cord binding the weakest. Against GuardLashing it trades 2 hit
for 2 penetration and the bleeding effect. It does not teach skills, guarantee bleeding, or replace the other bindings.
Actual stats and an actual refit remain an acceptance gate.

## Scope cuts

No new tool, capability, Part or save field; no raw ore, heat, tanning, jobs, capstan or culture work; no change to the
blueprints of spine, cord or kit; no retrofit of saved owners; no allocation across several smaller stacks (two spines must
share one stack); no price tuning to rescue the recipe.

## Results

Native: `MachineSpurgrassKitTests` 42/42 before the live-claim variants and the live-mark helper described below (rerun result recorded at the end). Before the production change 16/39 passed (23 red); with only the catalogue row and
weapon admission 26/39 passed, and the three baseline throw/merge pins above were red, which proves the hazard. Two cases
were added after that run (live bookkeeping still protects a declared input; an effect-touched cord source still works) and
pass.

Full EditMode suite in the isolated clone (before the last three cases): 39,016 run, 90 failed, 9 inconclusive, 7 skipped. The 41
failing fixtures were rerun on unmodified HEAD in a second clone: 85 of the 90 fail identically there (stale pins against
already-shipped M1b/M3b changes, graphics/density/voxel fixtures that need art sources the clone does not carry, and
reflection-signature mismatches). The other five (3 `ComposableBladeTests`, 1 `GroundStateLookTests`, 1 timed-out
`SpreadBiomeCoverageTests` seed) did not reproduce when run with the change in isolation (69/69 across those fixtures plus the kit),
so they are order- or timing-dependent in the full run; that is an honest limit, not a proven pre-existing failure.

Not yet done: actual play (acquisition by ordinary harvest, stacking, throw/recover, the process, refit, travel, F5, fresh
Continue), a Mac package, and an independent source/gameplay review. The recipe is therefore **native-accepted only**.
