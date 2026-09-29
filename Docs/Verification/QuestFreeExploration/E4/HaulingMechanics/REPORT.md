# F9 hauling mechanics audit

Status: complete, audit only. No shared writes, Unity calls or Git mutations. No production defect was confirmed and no gameplay repair is proposed for this bounded milestone.

The original `/tmp` audit and raw receipts were erased by a machine restart. Its conversational result was 20/20, but that is not retained evidence. The persistent reconstruction reran the hypotheses against unchanged current inputs: **20/20 passed**, zero failures/skips, 0.442929 seconds. The earlier fixture correction is recorded honestly: FallenBeam lacks DestructiblePart, unlike HaulBarrel; the test must preserve nullable structural state. There was no production RED/GREEN cycle.

## Decision

The important gap is useful world placement. Existing hauling, exact owner follow, stored slowdown, free release and full-session replacement work for the proposed beam/barrel sources. Continue F9 placement and source provenance; a hauling rewrite, friction system or animation rewrite is not justified by this audit.

The 20 paired cases cover two paid pulls against ungripped movement; held versus released full-session saves after two pulls; exclusive haul/let-go menus and no second offered grip while occupied; actual strength thresholds below/at; and blocked/open ordinary movement with conditional scheduler debit. Saved cases preserve an unrelated seven-point Speed penalty and assert that release refunds the hauling penalty once. Exact source ID, current owner count, existing parts/properties, health where present, and expected anchor remain correct.

## Native action contract

The adjacent UI sequence is **C, direction toward the load, G**. `GameAuditHaulingBenchPlayer.HaulMenu` already checks the exact native target, menu command, shortcut and free clock/energy. Reuse the relevant sequence, not that old benchmark's CP437-only visual claims.

`HandlingPart.HaulCommand` is `HaulObject`; `ReleaseCommand` is `ReleaseHaul`. Both use hotkey `g`, and only one appears on the load. The declaration has no zone context; actual input reach and TryGrab enforce current adjacency. InputHandler sends the target an InventoryAction event with Actor and Zone, without ending the turn. Grab/release are deliberately free.

Each normal movement pays the usual 1,000 energy. DragPart follows AfterMove by moving the exact load into the vacated cell. The native witness must move only through ordinary player keys, never relocate the load directly. InputHandler then processes turns and one material/tile-state update.

For a real Player at Strength18/Speed100, from tick17/energy1000:

| Source | Weight | Speed while held | Two-pull ticks | Final energy |
| --- | ---: | ---: | ---: | ---: |
| FallenBeam | 60 | 76 | 27 | 1052 |
| HaulBarrel | 75 | 70 | 29 | 1030 |
| Same sources, ungripped control | unchanged | 100 | 20 | 1000 |

These are single-player core scheduler measurements, not native wall time or enemy-pressure measurements. Speed affects all paid actions while gripped; there is no separate hauling movement-cost multiplier. The saved AppliedPenalty refunds exactly once and preserves other penalties.

Minimum haul Strength is8 for FallenBeam and10 for HaulBarrel. MillStone150 exceeds the normal player's144 capacity. Both sources are noncarryable single-cell objects. FallenBeam has no Destructible, Material or Thermal part, so do not promise breaking/burning the timber. Barrel and Hedge already support Break; the barrel has no container contents.

## Readability and native limits

WorldAffordanceQuery currently supports harvesting, opening containers, door actions and selected readable objects, but no Haul/LetGo cue. The actual menu is truthful. Adding a subtle current-target hauling cue may be a useful follow-up, but this audit does not claim it exists or modify it.

The existing presentation identity tests cover a moved/saved beam's stable model choice. Haulables are batched: native proof should use TryGetApprovedStyle and exact source/submitted fragments, not the generic Visual helper's independent-object requirement. Root must inspect images separately.

Can verify here: shipped core rules and menus/events, ordinary follow, conserved quantities and owner identity, full saved replacement graph, slowdown/refund and scheduler arithmetic.

Cannot verify here: actual keyboard routing, rendered pixels/animation, Unity seed-specific source availability, threat-containing journeys, timing/feel or discovery frequency. The requested native witness remains a separate root-owned gate with one disclosed player transfer, real two-pull/release/crossing, measured eight-direction bypass, and parked save/load aftermath.
