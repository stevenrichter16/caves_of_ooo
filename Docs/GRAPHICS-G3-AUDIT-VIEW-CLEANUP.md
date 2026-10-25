# Audit Game view cleanup

Status: two independently reproduced defects fixed;22/22focused native cases
passed and real ConfigureView/RestoreView creation/removal smoke passed. This changes an Editor
capture helper, not gameplay or the inventory presentation.

## Sweep and correction

Ordinary menu audit cleanup logged `Invalid index 0 7 1`. Installed Unity
6000.3.4f1 API inspection and a runtime probe confirmed two separate causes:

| Assumption | Actual contract |
| --- | --- |
| RemoveCustomSize accepts a custom-relative index | It subtracts the builtin count internally and requires the combined index. |
| The full ownership label survives storage | GameViewSize caps baseText at40characters; old21-character prefix plus32-character token is truncated. |

Keep exact full token/dimensions/range guards. Use `CoO ` plus the full GUID
(36characters) for both creation and matching; remove with `built + added`.
Never match a partial token or sweep historical presets by label prefix.

## Native RED and counters

Initial RED7 (6pass/1fail) and index-only attempted GREEN21 (20pass/1fail)
both failed the no-removal assertion because the long label never matched.
They are preserved, not misrepresented as isolated index evidence.

After restoring old production and adding a short launcher-only receipt token
counterpart, RED8 at09:00:40UTC produced two distinct failures: short token
removed the wrong test-owned padding object; full GUID removed nothing after
label truncation. Six controls passed. Actual save-isolation token stayed intact.

The final fixture has8cases: the original4recovery cases, short/full ownership
removal and idempotence, plus mismatched-token and mismatched-dimension counters.
Both final positive label variants assert full stored equality without truncation.
All tests add builtin-count padding so old broken code can only remove a
fixture-owned object, never a user preset. Cleanup uses actual current combined
indices and object references, restores every borrowed view/token and closes
only a view created by the fixture. Independent source review checked this safety.

## Scope and self-review

- 🟡 Fixed: wrong index could leave an audit resolution or remove a different
  custom preset when enough customs existed.
- 🟡 Fixed: long ownership labels silently stopped exact matching.
- 🔵 Three production expression changes; no gameplay, render pipeline,
  saved world, prefab or native sprite changes.
- 🧪 This fix does not retroactively remove historical orphan presets. Other
  old audit helpers are outside this bounded change.

Files: `Assets/Editor/Scenarios/LiveFlatPixelGreyboxTools.cs`, the existing
`Assets/Tests/EditMode/Gameplay/Save/LiveFlatPixelGreyboxRecoveryTests.cs`, this
record, and named `audit-view-removal-*` verification receipts.


## Final verification — 2026-10-25 09:03 UTC

Native final GREEN22/22, zero skipped,09:02:58–59UTC:8recovery/view cases
and14existing save-isolation controls. Independent source review found no new
P1/P2 issue. Root's direct helper smoke at09:03:47 used a fresh full GUID,
observed the exact36-character stored label, and confirmed ConfigureView added
one preset, RestoreView removed that preset, and a repeated restore changed
nothing. Play and save isolation remained inactive. This is an Editor helper
check, not a new gameplay or device-performance acceptance.

Root first removed only the single known orphan from its preceding inventory
session, verified by exact label/dimensions/count and the earlier invalid-index
trace/receipt. No historical sweep or unknown user preset deletion occurred.
There are now zero custom presets in that group; the preceding session had
only this known orphan. Normal user save bytes are unchanged.

All named XML/summary files remain raw, including the intentionally failed
index-only attempted GREEN. The final22pass result is the authoritative result
for the completed fix. Other historical tooling remains outside this slice.
