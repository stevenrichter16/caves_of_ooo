# GA02a — carried consumables and penalty restoration

Status: COMPLETE. Full7798/7798 GREEN at21:59:24–22:00:58UTC, zero C# errors.

## Outcome and scope

A02: actual carried ownership and positive quantity are required before food,
tonics, Ink vials or optional single-use schematics grant their benefit. Actor-
aware ground menus omit consumption; direct commands reject unavailable units.
Each successful use spends one unit. Tonic payload callbacks run after payment,
so a recursive call cannot reuse the last unit. Nonconsuming thrown payloads and
ordinary reusable schematics retain their supported behavior.

A05, partial: consuming a handling-bearing stack refreshes carry penalties.
Action rollback restores inventory structure, then stats, then the derived
tracker. FinalizeLoad rebases that tracker without charging a second penalty.
No reflected save field or save version changed. Other crafting/transfer
quantity refresh paths remain in the audit plan.

Execution diagnostics distinguish refused consumption, unit payment and applied
payload. Menu queries do not emit attempt records. Four redundant private
consumption helpers were consolidated; they were active duplication, not newly
discovered dead mechanics.

## Evidence

All listed runs first checked the compiler log: zero C# errors.

| Gate | Raw evidence | Result |
|---|---|---|
| Initial/expanded ownership RED | GA02a-red.xml.gz; GA02a-red-expanded.xml.gz |19:16 failures/3 controls;22:17 failures/5 controls |
| Minimum GREEN | GA02a-green.xml.gz |335/335 |
| Initial adversarial pins | GA02a-adversarial.xml.gz |55/55 |
| Review RED | GA02a-review-red.xml.gz |12/12 failures:4 rollback,2 save tracker,6 diagnostic |
| Review GREEN | GA02a-review-green.xml.gz |384/384 |
| Bench staging RED/GREEN | GA02a-bench-red.xml.gz; GA02a-bench-green.xml.gz |3 missing-type failures;70/70 |
| Native record RED | GA02a-native-record-red.xml.gz |disabled channel fails; enabled control passes |
| Final focused | GA02a-final-focused.xml.gz |94/94 |
| First full | GA02a-full.xml.gz |7792/7793; existing flicker fixture timing failure retained |
| Final full | GA02a-full-green.xml.gz |7798/7798 GREEN;83 added cases |

The earlier rollback RED's two tonic cases added a duplicate HandlingPart and
failed their own penalty precondition. Corrected fixtures configure the existing
Part; the subsequent four cases genuinely reproduce the rollback defect. Both
raw runs are retained. The complete new suite has22 regression,56 dedicated
adversarial and5 staging/diagnostic cases (83 total). Most adversarial cases
are already-correct pins; they are not56 separately discovered bugs.

A42, verification repair: the existing campfire fixture claimed30 successive
Render events advanced30 frames. EditMode synchronous calls share Time.time,
so a near-zero Perlin offset could make the test fail despite correct wiring.
The test now primes the base, writes an impossible intensity sentinel, then
requires the actual Render event to replace it within authored bounds. Removing
the flicker Part is the countercheck. No production flicker behavior changed.

## Native observations and honesty bounds

| Startup condition | Raw report | Run ID | Results |
|---|---|---|---|
| Isolated boot modal active | GA02a-native-menu-green.json |fd302e5d815d469e8425eb15b65b74e2 |10/10,5.074737708s, zero failures |
| Same isolated modal already dismissed through native input | GA02a-native-no-menu-green.json |a4a561c8f0ca42d3936ecc43fd0f02ca |10/10,3.601891834s, zero failures |

Corresponding native logs are archived. Native input opens the actual tonic
world menu, verifies a real Examine control and absent Apply, moves to and picks
up that same entity, opens its inventory popup, selects Apply and checks healing,
consumption and menu exit. Reflection only reads UI state.

The first attempted no-menu fixture omitted its marker, allowing save discovery
to select another existing save. Its9/9 result is retained as a fallback control,
not proof of an empty-save bootstrap. The corrected fixture always isolates its
save slot and dismisses the modal with native N before the audit. It reproduced
the extra-N movement failure before the driver started checking IsActive.
The failed raw JSON predates corrected failure counting (one failed assertion
reported twice). Interactive menu launch now shares isolation and cleans up on
manual stop; unsaved scene confirmation is only in that user-invoked menu path.

**Can verify:** keyboard/menu routing, original item identity/quantity, health,
refusal and diagnostic state, isolated batch completion. **Cannot verify:**
rendered pixels, visual feel, fresh no-save boot behavior, or native Food/Ink/
Schematic use (those have EditMode coverage). Interactive menu cleanup received
source review, not a separate GUI execution claim. No ordinary per-frame/turn
hot path changed; this one-shot repair does not claim a performance improvement.

## Cold-eye review and remaining boundaries

- 🟡 Fixed: consumption exposed stale carry-penalty trackers on rollback/load.
- 🟡 Fixed: new refusal gates lacked diagnostic records.
- 🟡 Fixed: audit menu skipped isolated saves and assumed an active boot modal.
- 🟡 Fixed: scenario channel disabled silently hid native observation records.
- 🔵 Fixed: native assertion double-count; stale consumption-order comment and
  inaccurate structure/tracker/stat wording.
- 🧪 Recorded A41: generic action snapshots still omit Ink properties,
  effects/cures and learned recipes. Exception/outer rollback can retain those
  benefits while restoring payment; this predates the repair and has no found
  production Before/AfterInventoryAction subscriber. Direct public consuming
  Tonic calls lack a transaction; normal UI uses the command facade.
- ⚪ Optional consuming schematics and handling-bearing consumables are supported
  API contracts; their corresponding authored default-world reach is limited.

Independent inventory and native reviewers cleared their must-fix findings after
correction. Root reviewed symmetry, ownership/quantity boundaries, dispatcher
return semantics, save compatibility, existing callers, and documentation.
This is CoO repair work, not a new Qud-parity claim.
