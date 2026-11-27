# The oil delivery lights Quillhold's desks

Quillhold's copyists maintain the lamps after either existing oil handover.
The original two desks cast local light through the existing light map.
The nearby visible work floors use that light in Flat Pixel mode. The
desk reader says whether its lamp is lit. The report collation keeps its
own outcome. Older saves with the matching desks can read the saved
condition on entry.

I continued an ordinary seed-65 Duelist at Sill. I found Yevven Reedfall
and used the native trade screen to buy his three bottles for a watering
grimoire and 28 drams. The character kept 3 drams. I traveled to
Quillhold, walked to the copy hall and accepted The Dark Copy-Desks
from Nerrit Halm. The native bottled-oil handover spent all three. One
ordinary wait applied the supply outcome. Reporting awarded the existing
3 ink and 50 XP. Sari-snakes interrupted the walk and the wait.

Both captures use the same standing tile `(14,5)` and ambient light `0.4`.
The renderer measured `0.7333 → 1.0` at west floor `(14,4)` and
`0.4 → 0.8875` at east floor `(23,4)`. The aisle control `(19,8)` stayed
at `0.4`. The first live pair exposed a display bug because authored
floors bypassed light. The final pair applied the normal light in both
work bays. A fresh Continue kept the exact two desks and their light.

The previous code passed 42 cases and failed five expectations for the
new behavior. The final native run passed all 330 targeted cases,
including different worlds, the mirrored layout, invalid desks, lighting
without movement, and unchanged remembered or distant floor colors.
The bottled route was played in the Editor. Both oil routes share the
same condition in native tests. This slice adds no new oil payment.

Independent review confirmed the trade, supply and reward across all 16
save pins. Both visible floor crops brightened in the final image pair.
A distant corridor crop retained its exact pixels. Ordinary movement
after Continue kept the condition. The snakes interrupted that walk at
20 HP. All 876 normal Quick files and 87 pre-existing tracked changes
matched. Play Mode ended and the private input and save state cleared.

[Unlit floor](05-matched-floor-before.png) ·
[Native handover](02-copy-room-oil-handover.png) ·
[Lit floor after Continue](06-matched-floor-after.png) ·
[Desk reader](04-copy-desk-reader.png) ·
[Recorded states and save pins](observations.json)

## Mac package

The universal Mac package built from `b4b057b49` with zero errors and
five warnings. It reached GameBootstrap startup on Apple M5. Six
binaries contain arm64 and x86_64. All 186 previous-app files and 876
normal Quick files matched their pre-launch hashes. Preferences were
restored. Gameplay evidence above is from the Editor. The packaged
check covers startup on Apple Silicon.
[Startup receipt](mac-startup-verification.json)
