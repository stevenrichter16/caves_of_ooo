# Inventory action release — final independent source review

Reviewed UTC: 2026-10-25T07:18:05.105555+00:00

**No significant correctness or regression finding in the frozen diff.** Native GREEN and actual held-key replay remain root-owned gates; this report claims source review only.

## Verified implementation

- `InventoryUI.cs:1346–1349` preserves the exact name/signature `ExecuteItemAction(int)`, delegating to a differently named keyed implementation. Existing reflection callers remain unambiguous and receive the original single parameter; pointer/Return calls carry `KeyCode.None`.
- Alphabetic dispatch passes the actual selected letter at `InventoryUI.cs:1319`. It does not sample arbitrary held keys or infer which input probably triggered the action.
- `_closingActivationKey` is assigned only alongside the pre-existing successful paid-action receipt at `InventoryUI.cs:1500–1503`. Rejected commands, nonclosing operations and the separate throw branch do not acquire it. Existing command validation, quantities, costs and scheduling authority are unchanged.
- `Close()` preserves the one-shot receipt, while `Open()` clears it. `ConsumeClosingActivationKey()` resets it to None immediately. A successful Return/pointer action also writes None, preventing stale provenance from surviving a prior attempt.
- `InputHandler.cs:4055` consumes the letter before returning to Normal and processing the paid turn. The pre-existing Normal-input release fence then blocks the held letter; after physical release it clears and ordinary movement resumes. The change does not add timers or modify gamepad quarantine.

## Frozen four-case tests

The primary case uses a factory PitchpodSeed, cultivated actual Grass and the genuinely gathered `PlantSeedAt|Overworld.11.10.0|5|4` action at ordinal b. Full queued keyboard → `InputHandler.Update` → command → scheduler dispatch is retained. Only the existing movement/wait repeat clocks are bypassed for EditMode time.

`AssertPlantedOnce` now explicitly asserts WaitingForInput and CurrentActor==Player, avoiding a scheduler no-op falsely satisfying held-input checks. Crop identity/count, one-unit consumption, original player cell and energy accounting establish exactly the intended paid planting action. Three further held B updates preserve post-plant cell/tick/energy; release is free; fresh B moves southwest and pays its ordinary action.

The two refusal cases prove the real veto is reached, a held rejected shortcut does not re-dispatch, cancel is free, and subsequent Return success cannot inherit B. The separate initial Return case has no alphabetic fence, does not repeat payment, and retains fresh B movement. The original single-argument wrapper also preserves all existing pointer/transaction fixtures without a signature migration.

These tests do not claim physical keyboard/Steam Deck feel or actual framebuffer verification. They complement, not replace, the short ordinary held-B replay the parent is preparing.

## Reviewed bytes

- `Assets/Scripts/Presentation/UI/InventoryUI.cs`: `a695bfabe39b0fbf5aeb32ce4414d067ce68013a43ce1fe11e1abab1547cd1df`
- `Assets/Scripts/Presentation/Input/InputHandler.cs`: `8706f0b1ab77a5f9174c91a3bcff2bdb8c282380be9b960efb1ad57003325b97`
- `Assets/Tests/EditMode/Presentation/Input/InventoryActionReleaseTests.cs`: `d572e3524d069196aebdccf4e5556f11f8bef1f65f9432ad61957cf1315b0232`
