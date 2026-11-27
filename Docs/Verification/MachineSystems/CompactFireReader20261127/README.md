# Two clear fire lines

Fire descriptions now contain a separate fuel amount and remaining-flame
estimate. The world reader, quick Examine, Look focus, interaction status
and inventory F1 share the same readout. Normal fire descriptions omit the
long use, installation, cooking and flavor paragraphs. Item identity,
existing repair/claim/attack warnings and actual action rows remain.
The shared announcement reader no longer draws the "READING / no turn
passes" footer. Close, back and page controls remain.

Fuel retains fractional values. Hearths account for their ordinary fuel
debit and cooling; coals and older campfires use their own heat horizon.
An empty or packed hearth has zero remaining flame. Unsupported destructive
combustion or invalid metadata reports an unavailable estimate rather than
inventing a duration. These are read-only estimates; the burn rules and
game clock did not change. Cooking-only ovens keep their ordinary reader.

## Native and actual checks

The old runtime compiled and failed ten expected new format/description
cases, with fifteen controls passing. A broader run found five hot hearth
fixtures still marked as packed kits. Their installation setup was
corrected. A later rare-combustion fixture needed an explicit status part.
The final native run passed all 423 selected cases, with no skips or
compiler errors. It covers fire states, fractional fuel, world/pack reader
authority, hidden and stale cells, conflicting custody, ordinary nonfire
controls, current attack warnings and the neighboring camp systems.

Actual GameBootstrap play began from the retained supplied camp's saved
2510 checkpoint. Its lit hearth showed **Fuel: 1.75 / 30** and
**Flame remaining: about 7 turns**. Seven ordinary waits reached 2580
with zero fuel, no light, residual heat and the two zero readouts. Taking
up the same hearth reached 2590 with ambient heat and no flame. Ordinary
inventory F1 showed only the same two lines. Fresh Continue from that
packed checkpoint verified the inventory surface again after removal of
the global reading footer. Reading itself spent no time or health.

The source checkpoint is a controlled supplied-camp trial, not ordinary
acquisition. All 876 normal Quick files across both normal save roots
freshly matched; the isolated save root and input devices were restored.
Four raw packed checkpoint files remain with hashes under
`/Users/steven/coo-verification/checkpoints/CompactFireReader20261127`.
Packaged gameplay, wider layouts and hardware performance remain open.

[Lit fire](01-lit-fire.png) · [Packed fire in inventory](02-packed-fire.png) ·
[Observed state and native receipts](observations.json)
