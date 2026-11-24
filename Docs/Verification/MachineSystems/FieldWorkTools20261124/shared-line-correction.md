# Shared rod path correction

The initial tool delivery4a7328557 copied only Physics.Solid and Solid-tag
blocking for the intermediate cell. That omitted existing component doors,
sealed archive barriers and Morrowfast's scene-owned blockers. Visibility alone
is not a physical obstruction check, particularly while a door changes state.

The correction is one shared call: `Visible(mid) && !mid.BlocksMovement()`.
It does not change that rule, open a door, reveal a hidden object or add a new
capability. Terminal conductive cells can still be targeted; the intermediate
cell must be visible, explored and clear under the world's existing rule.

NativeREDu:2fail/2controls/0compile errors. A closed component door still offered
a shot, and public door closure during AfterInventoryAction still let charge
commit. An open door and ordinary solid obstruction were passing controls.
FinalGREENv:248/248,49new/199retained, zero compile errors. Late closure now keeps
the door closed, restores the stored charge and emits no electricity.

Current-code compatibility uses the preserved loaded170 checkpoint: native
inventory selection fires at the clear two-cell pipe,180/HP400/charge0. All four
F5 files are archived under SharedLine180 before cleanup; full fresh180 matches
the recorded selected values with the stated normalization. Earlier tool,
cooling, combat and natural-root evidence is retained; only the affected clear
path was replayed for this correction, not counted as new progress.

Independent bounded source and final native/recorded-artifact review:
0remainingP1/P2; reviewer independently checked15pins, final248,32file hashes
and seven selected comparisons without operating Unity or the player. Eight checkpoints/all32files and
seven normalized fresh pairs now cover the batch. Main stopped/rootnull/clean,
original7views/index0, input restored and owned reload locks released.

The4a Mac input export was prepared but never built and remains preserved at
`/private/tmp/coo-mac-4a7328557-20261124-export`. One coherent package will be
built from the corrected runtime. CurrentMac9a remains last bounded delivery
until that receipt qualifies the replacement. All12complete-machine/full-suite/
ordinary discovery/balance/hardware gates remain open. This is0new capabilities.
