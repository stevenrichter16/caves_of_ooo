# TagManager saved-source correction

2026-10-24 UTC. Scope: `ProjectSettings/TagManager.asset` and new dependency-free `Tools/ProjectSettings/test_tag_manager.py`. No Unity/editor operations, package installation, other source edits or git mutation.

## Sweep and minimal change

The actual Editor.log reports `Unable to parse file ProjectSettings/TagManager.asset` at line39 (`Expect ':' between key and value within mapping`). The original saved file is historical, not produced by the current graphics patch.

History independently explains its undersized layer table:

- `af7ee58f3`:32 slots.
- `011852b35`: replaced6 blank entries with5 named entries, leaving31 slots.
- `3821fb68a`: replaced ThoughtOverlay plus an empty slot with one bare dash, leaving30 slots.

The repair preserves all30 existing slot values and appends two empty slots; it also normalizes the single bare dash to the file's canonical `- ` delimiter. World8, Sidebar9, PopupOverlay10 and Hotbar11 remain exactly aligned with GameplayRenderLayers constants. No deleted ThoughtOverlay layer is restored. Header, tags, sorting-layer name/ID/locked flag and rendering-layer metadata remain byte-identical.

Unity's own [TagManagerInspector source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/TagManagerInspector.cs) explicitly asserts32 layers. Its [UnityYAML documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html) describes a restricted parser; a generic YAML validator would not establish native compatibility. The standalone test intentionally validates this project's small authored contract, not a general YAML grammar.

## Evidence

Command: `PYTHONDONTWRITEBYTECODE=1 python3 Tools/ProjectSettings/test_tag_manager.py`.

- RED `g2-tagmanager-red.txt`:7 tests,6 pass,1 expected contract error, `Expected32 layer slots, found30`.
- GREEN `g2-tagmanager-green.txt`:7 tests,7 pass. One test contains independent30- and31-slot counterexamples.
- Positive independent32-slot fixture; malformed30/31 controls; name-shift-at-fixed32 control; altered sorting ID control; canonical delimiter control; production layer constants checked.
- `g2-tagmanager-source-comparison.json`: original30 slot values unchanged, appended slots both empty, header/sorting/rendering bytes preserved.
- `/tmp/coo-tagmanager-before.asset`: tiny exact pre-edit snapshot.

## Self-review and honesty bounds

No material blocker found in the bounded source repair. Named indices and serialized meaning are retained; only missing unnamed slots are restored. Tests are independent of Unity, need no installed YAML parser, and create no bytecode cache.

The bare dash is valid general YAML syntax; this review does **not** establish that it caused the Unity parser failure. Nor does the32-slot invariant by itself explain the exact native line39 diagnostic. The native parser, actual loaded layer table, fresh startup log and current editor modal remain root's verification work. Do not report Unity startup repaired until those checks succeed. This source correction is justified independently of that unresolved native diagnosis.

## Independent source review

A second reviewer compared the tracked original and saved source directly:
all30 original slot values, named indices and header/sorting/rendering suffix
are preserved; the two final slots are empty. The seven standalone checks
include actual-source failure coverage and nonvacuous malformed controls.
No actionable P1/P2 issue was found. Native startup remains unverified.
