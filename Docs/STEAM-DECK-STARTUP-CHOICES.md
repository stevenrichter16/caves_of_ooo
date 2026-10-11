# Steam Deck saved-game choices

Status: compatibility fix verified in native tests. User subsequently confirmed the controls work in the current build; the initial report concerned an older build. This change covers an independently reproduced keyboard-output compatibility gap, not a diagnosis of a broken current native gamepad.

## Verified input paths and plan

`InputHandler` selects Menu context before polling the boot/death prompt.
Native gamepad South/West map to Return/I; `GamepadChoiceInputProbe` translates
those native keys to C/N (boot) or L/R (death). However, it reads Return/I only
from `NativeGamepadInput`, excluding identical keyboard output from Steam Input.
Existing tests cover native gamepads, not this keyboard-output path. The user's
earlier Deck setup included keyboard mappings; whether those are still active
has been asked, not assumed proven.

1. Reproduce native success and keyboard-output failure with actual Input System
   events, including no connected gamepad, idle input and no duplicate action.
2. Accept Enter for Continue/Load and I/X for New Game/Restart only inside these
   prompts, through the existing shared input adapter. Preserve C/N and L/R,
   native modifier/context quarantine, and Continue's same-frame priority.
3. Verify affected controller, save-choice and character-choice tests. Record
   limitations honestly: simulated input is not physical Steam Deck testing.
4. Build a fresh Linux package in a new directory ending `-current`, retaining
   the previous delivery and saves. Verify archive contents/permissions.

This is a compatibility fix, not a claim that Steam Input's current layout or
Linux device detection has been inspected on the user's Deck. No world bindings,
save format or controller button positions change. The extra checks run only
while a save/death choice prompt is open; no device scanning or new Update loop.

## Evidence and review

Observed RED: `keyboard-choice-red.xml`, 69 cases, 63 passed and all six new keyboard-only boot/death choices failed. After production: `keyboard-choice-green.xml`, 104 affected input/controller cases passed, zero failed. Native A/X, modifier/held routing and keyboard-only Enter/I/X are included. No physical Deck input claim. Final native45-fixture regression passes1359/1359, including complete BootMenuController, DeathScreenController, SaveLoadInputController, StartingBuildMenu, PauseMenu and actual input fixtures. Receipt: `Verification/WorldAudio/broad-final-green-1359.xml`.
