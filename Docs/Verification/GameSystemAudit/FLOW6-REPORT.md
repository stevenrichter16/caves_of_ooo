# FLOW6 — deliberate Look and movement

Status: COMPLETE. Baseline8834GREEN, HEAD5e3d1ab6; final8886/8886GREEN. Recorded A49/readiness12
repair. CoO-original control consistency; no new binding, content, sprite or save field.

## Behavior and source correction

Normal L belongs to Look. Holding it after Look closes, or after its fresh press was
swallowed by the movement delay, no longer becomes an unintended east move/attack.
Fresh L still opens Look; a fresh L inside Look moves its cursor east. D, Right and
numpad6 retain normal east movement/repeat. Other existing movement aliases and modal
interaction directions remain available. No delayed-input buffering is promised.

Runtime change removes one held-L disjunct from GetMoveInput. Three source comments
and the historical implemented-controls description now match the actual contract.
ControlsReference already advertises the correct bindings and remains unchanged.
InputHandler also contains protected spell changes; stage only this wave's incremental
hunks against /tmp/codex-flow6-input-before.cs.

## RED, controls and adversarial gate

| Archive | Total/pass/fail | UTC / duration | Meaning |
|---|---|---|---|
|FLOW6-red.xml.gz|13/10/3|06:02:19–06:02:20;.9280037s|Three genuine held-L paths move actor10,10→11,10; existing controls already pass |
|FLOW6-minimum.xml.gz|53/53/0|06:04:06–06:04:09;2.575117s|Minimum fix plus neighbors |
|FLOW6-adversarial.xml.gz|110/110/0|06:05:44–06:05:50;5.6412708s|34dedicated cases plus neighbors |
|FLOW6-staging-red.xml.gz|5/0/5|06:08:57;.2500505s|Scenario absent before authoring |

Every listed run0compilererrors; compiler check precedes fresh XML parsing.
The held-key fixture advances an actual InputSystem frame without releasing L, and
asserts isPressed with !wasPressedThisFrame; repeated Update in one frame would not
prove this boundary. Dedicated matrix covers intended diagonal precedence, all normal
cardinal aliases, six modifiers, actual C→east Chest selection and displayed help.
These form player-flow hypotheses beyond the initial three reproduced failures.

## Native proof and honesty bounds

The scenario uses existing StoneFloor/Chest content, no enemies, actual InputHandler
and disposable save-slot isolation. Native queued keys hold L across Escape without
releasing L. Assertions include exact actor cell, turns/energy and Chest HP: unchanged
position alone could hide an unintended bump attack against the solid chest. Fresh
modal L moves the cursor once; continued hold does not repeat it. C→L targets the
actual Chest. The driver moves south to clear ground before D/Right/numpad6 hold and
release controls, requiring actual further movement and tick advancement.

Can verify: script-observable keyboard state, input mode, cursor coordinates, exact
interaction target, movement/attack/time invariants and the test-covered rate boundary.
Cannot verify: rendered pixels, subjective smoothness or desktop mouse delivery.
The native wall-clock driver does not claim to reproduce a particular rate-window
race; deterministic EditMode tests cover that boundary. No FPS/speedup claim: this
removes a conditional input alias, without adding rendering or per-frame work.

Launcher preserves the prior save preference, unregisters the synthetic runtime and
removes only its own disposable slot. Known A31shutdown errors, if present, are
reported separately from the native gameplay result. Focused/native/full/GUID and
independent review outcomes will be appended before completion.

## Focused/native review

Focused57/57GREEN,06:16:11–06:16:14UTC,2.8942102s,0CS. This selection
includes52new FLOW6tests and5existing Look tests; two intended shortcut filter names
were not actual class names. The earlier110GREEN gate includes the real neighbors,
and the full suite is the final cross-system gate. No inflated focused count.

Native6d04c04d262f4442ba91d1c079771520: **21/21PASS**,6.374252083s,
exit0,0CS. Exact raw JSON and native log retained. Two known A31destroyed-camera
exceptions occur at shutdown after the successful audit, so exit0 does not claim
exception-free teardown.2407uniqueGUIDs/0collisions.

Independent cold-eye review0must-fix: checked genuine continuous held input,
Chest damage guard, cursor property observation, clear movement lane, all three
repeat/release controls, isolated save preference and InputSystem cleanup, staging
TurnManager.Active restoration and protected-file incremental diff. Qud parity is
not claimed: this is consistency with CoO's shipped L Look binding.

## Close-out

Full **8886/8886GREEN**,06:17:55–06:20:02UTC,127.2927773s,
0compilererrors/failed/skipped/inconclusive.52newcases:13regression,
34dedicated adversarial,5staging. Native21PASS;2407uniqueGUIDs/0collisions.

Self-review: 🟡 reproduced accidental held-L movement fixed after actual RED.
🔵 full-vi normal-movement documentation corrected; displayed help already correct.
⚪ native rate-window timing deliberately excluded, deterministic tests cover it;
known A31shutdown debt remains separate. 🧪 no physical keyboard, mouse, pixels or
subjective-feel claim. Independent source/native cold-eye complete,0must-fix.

Files: incremental InputHandler alias/comments; historical control docs/readiness
item; three test files; LookKeyBench/Player/Batch; report/raw and living docs.
Ownership: whole paths exclude all1027protected paths; only the three incremental
InputHandler hunks are staged in that shared file. Protected spell/art work preserved.
