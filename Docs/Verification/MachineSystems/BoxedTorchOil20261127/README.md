# Refill an unlit torch from camp storage

November 27, 2026, America/Chicago.

Beside an accessible installed camp box, the player can spend one unit from
a directly stored oil flask to add up to 25 fuel to a carried, unlit torch.
The menu shows the exact flask, torch, and resulting fuel. The flask stays
inside the box. The ordinary carried-flask and lamp-oil choices remain, and
the torch needs its own ignition action.

The box and hearth use the same stored-oil admission rules. Torch refills
share the fuel cap and one-unit payment. The receipt checks the actual box,
flask, and torch again before commit. Rollback restores the oil only after
the credited fuel is retracted. An unrepresentable fuel gain is unavailable.

## Actual GameBootstrap play

1. Continued the supplied Worksite2330 camp with the exact torch at zero
   fuel, flask 2231 with three oil, and four existing fire-moss. Supplied an
   empty camp box 2731, set it down at (40,9), and placed the flask in it
   through normal inventory. The equipped torch had no refill row. After
   unequipping it, the box offered one oil for 25 fuel.
2. Cancelled and reopened the offer. Tick 2340, three oil, and zero torch
   fuel stayed unchanged. Selecting the action reached tick 2350, kept
   exact flask 2231 inside box 2731 with two oil, and left the exact torch
   dark in the pack with 25 fuel.
   [Refill preview](01-legible-refill-preview.png)
3. Walked east to (44,9) and equipped the same torch. One ordinary fire-moss
   lit it at 2390. The same rendered cells at (45,9) and (46,9) rose from
   0.7333 and 0.6667 to 1.0 each. Ambient light stayed at 0.4. The torch
   held 24.7 fuel and three moss remained. The cold hearth stayed cold.
   [Before ignition](02-dark-path-before-kindling.png) · [After ignition](03-torch-lights-the-path.png)
4. Fresh Continue at tick 2390 kept the same equipped flame, 24.7 fuel,
   exact boxed oil and measured light. The rendered view matched the lit
   capture byte for byte. [After Continue](04-reloaded-lit-torch.png)

## Verification

Eight first-run failures reproduced the missing box action. Twenty-seven
controls passed. Two more native cases found older carried-refill failures:
rounding that could grant more than the tariff, and refunding oil while a
detached fuel part still held its credit. All 555 selected cases passed
after the fix, with zero compiler errors or skips.

Independent source and gameplay review checked both raw saves, which
matched all eight checkpoint hashes. The initial font did not render the
transition arrow, so both refill rows now use “to.” The published preview
was captured on the saved 25-fuel torch with two oil still in the box.
Ordinary quick saves and 87 pre-existing modified files matched their
before hashes. The Editor returned to its normal input and save root.

This was guided Flat Editor play on Apple Silicon. The retained camp and
one supplied box isolate the comparison. Paid stock acquisition and
out-of-camp acquisition were outside this run.

[Observations](observations.json) · [Native and save receipts](receipt.json)

## Current Mac package

`/Users/steven/coo-verification/builds/mac-bac91380a-20261127/CavesOfOoo.app`
contains the new refill. Unity verified 32,254 committed inputs and
built the universal app in 21.05 seconds with zero errors and five
reported warnings. Six binaries contain Apple Silicon and Intel slices.
All 186 current and all 186 previous app files matched.

The app started on Apple M5 and initialized a world with 2,715 entities.
Both normal save roots kept all 876 quick-save files. The nine changed
startup preferences were restored and the player stopped. Packaged
gameplay and physical Intel testing remain open.
[Mac startup receipt](mac-startup-verification.json)
