# Steam Deck direct-launch graphics defaults

Status: implemented; native settings regression GREEN 55/55.
Qud reference: none. This is a launch-time presentation preference fix.

## Problem and design

The packaged launcher sets `COO_HANDHELD=1`, but a Steam shortcut directly to
`CavesOfOoo.x86_64` bypasses that launcher. With no saved preferences, the game
then chooses Full detail, 100% world resolution, shadows and Full spell effects.
Use the exact `SteamDeck=1` environment hint when no valid explicit application
override exists. Keep `COO_HANDHELD=1` and `COO_HANDHELD=0` authoritative. Other
values are not boolean truth; they fall back to the Deck hint. Absent/zero/invalid
Deck hints retain desktop defaults. Saved per-device preferences always win.

This selects the existing Handheld policy: low world detail, 75% world target,
shadows off and Reduced spell effects. HUD resolution, sound, accessibility,
gameplay and saved worlds remain unchanged. Players retain all graphics choices.
No hardware scan, new package, Steamworks dependency or per-frame polling is added.

## Verification sweep

| Premise | Verified evidence / correction |
| --- | --- |
| The launcher is necessary for current automatic defaults | `Tools/SteamDeck/Launch-CavesOfOoo.sh` exports the only recognized hint. `Village3DSettings.UseHandheldDefaults` only tests `COO_HANDHELD`. |
| Effects need a separate new detection path | `SpellFxSettings.Load` already consumes the same `UseHandheldDefaults`; changing that one policy keeps defaults consistent. |
| Steam officially documents this environment variable as a guaranteed native API | Not established. Valve's [Proton launch source](https://github.com/ValveSoftware/Proton/blob/master/proton) itself reads exact `SteamDeck == "1"` for Deck-specific behavior. This is primary implementation evidence for the hint, not a guarantee it exists in every non-Steam launch context. |
| Device defaults may lock graphics choices | Valve's [hardware recommendations](https://partner.steamgames.com/doc/steamhardware/recommendations) permit device-specific defaults but advise preserving accessible graphics choices. Existing graphics settings remain fully available. |
| Any Linux/gamepad session means Deck | False. No OS, username, controller, `SteamGamepadUI` or `SteamOS` heuristic is used. Only the two explicit hints affect absent preferences. |
| Existing tests isolate the newly read environment input | They isolate only `COO_HANDHELD`; extend their scope to save, clear and restore `SteamDeck` too. |

## Implementation plan and acceptance

1. Add real `Load` tests for direct-launch Deck defaults and no persisted keys;
   add exact-value, conflicting hint, saved preference and repeated load counters.
2. Root runs native tests and records the missing Deck behavior as observed RED.
3. Change only the shared startup hint policy. No frame loop or rendering edits.
4. Root reruns graphics/settings/controller compatibility tests; review preference
   restoration, malformed hints and independent resolution/shadow/FX overrides.
5. Include in the next Linux build and document the physical-hardware limit.

## Evidence and self-review

Root-owned native job `fbb6789d3d4843bb9835819217b34431` ran the two settings
fixtures before production changed: **55 cases, 50 passed, 5 failed**. Failures
were the expected missing direct Deck defaults, invalid application-hint fallback
to Deck and the first Deck load in the repeated-load regression. The 19 new cases
include confirmed bugs and passing counter-checks; they are not 19 distinct bugs.

Implementation adds a named `SteamDeckEnvironmentVariable` and resolves exact
application `1`/`0` first, then exact Deck `1`. Both world and FX loading consume
this one policy. No preference writer changed. Root-owned combined native job
`932afe2d8f6345c58061ec619d4746c4` passed **all 55 settings cases**. That combined
118-case job also contained three failures in separate root-owned gas tests; this
is a settings-only GREEN claim, not an all-job GREEN claim.

- 🔵 Test-isolation correction: settings test scope now saves, clears and restores
  both environment hints; existing adversarial tests inherit that same scope.
- ⚪ Direct Deck detection is a hint, not a hardware guarantee. Explicit overrides
  and saved graphics settings retain priority; no OS/controller guessing occurs.
- ⚪ No claimed timing gain from the policy itself. It selects existing settings;
  measured rendering optimizations are tracked separately by the root task.
- Self-review: repeated loading clears old runtime-only overrides; saved Full/Off
  dimensions and sound survive; missing preferences remain missing after Load.
- Cold-eye review: both default consumers use the same policy; exact `0` is checked
  before the Deck fallback; test teardown restores raw environment values and
  preference key presence; there are no new writes or periodic reads. No additional
  actionable findings in this bounded change.

Can verify: source routing and native tested effective settings under controlled
environment values. Cannot verify: which
environment values are present in the user's actual Deck launch, Deck frame times,
thermals or battery life. This fix is not a substitute for measured rendering work.

Files: `Village3DSettings.cs`, existing `SteamDeckGraphicsSettingsTests.cs` and
`SteamDeckGraphicsSettingsAdversarialTests.cs`, this living document.
