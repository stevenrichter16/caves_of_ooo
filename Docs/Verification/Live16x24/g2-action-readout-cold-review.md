# G2 grouped damage/readout cold review

Reviewed 2026-10-24 by the independent camera/art reviewer. Read-only production review; no Unity calls, tests, gameplay changes, or repository edits performed by this reviewer.

## Result

No actionable P1/P2 found in the settled four-file change, including the subsequent actor-body candidate rejection. Proceed to the already planned native regression and matched ordinary combat capture. This is source acceptance, not a claim that the pending run or actual display has passed.

## Checks performed

- `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:1193–1203`: the existing clamped HP amount is still emitted once in the central damage path, now with the actual owner. The lethal allowance is captured before `HandleDeath` removes the owner. No combat arithmetic, action cost or damage ordering change was introduced here.
- `Assets/Scripts/Gameplay/Effects/AsciiFxBus.cs:413–433,470–506`: owner visibility requires exact zone membership, owned visible RenderPart, current canonical origin cell and current managed graph. Carried/equipped or foreign Physics parts fail. A number is one invariant decimal string/value; repeated damage does not use word deduplication. The coordinate compatibility overload is explicitly origin-only, while the sole production combat call is owner-aware.
- `Assets/Scripts/Presentation/Rendering/AsciiFxRenderer.cs:316–360`: admission and every active update recheck the original disclosed cell and current owner. Removal of a living owner retires the receipt. Only a previously captured lethal receipt may survive at its finite origin, with no ID lookup/rebinding; a visible same-ID replacement or different living actor at that origin retires it. Hidden origin and expired lifetime also retire it permanently.
- `AsciiFxRenderer.cs:363–435`: words and full numbers share a capped chronological reservation pass. A retained valid lane is reused; full groups are clamped together. Same-row spans require a blank column, and later groups do not overwrite earlier values. No available nearby slot means omission of the transient, with the combat log retained. Three candidate lanes and at most 32 active groups keep work bounded.
- `AsciiFxRenderer.cs:418–425`: the new actor candidate check gates on cell visibility/exploration before looking at its top object, then requires an exact canonical visible Creature/Player owner. Hidden actor occupancy therefore cannot change an otherwise identical placement. This is deliberately protection of the disclosed one-cell top actor, not screen-space bounds of large legacy models or interpolated bodies.
- `AsciiFxRenderer.cs:1033–1066` and `WorldFxCoordinator.cs:345`: complete readouts use pure UI glyphs, retain precedence over decorative marks, and bypass destination fog only after strict origin/owner authorization. FloatingNumber is a semantic readout in FX Off. Existing nonblocking computation does not include these groups; wall time, rather than decorative speed, ages them.
- `AsciiFxBus.cs:179–188` and `AsciiFxRequest.Reset`: resetting on Release drops owner/zone/origin references immediately. Reset nulls borrowed path/flight references; it does not clear the underlying lists already copied into renderer instances. Read all production Release call sites: coordinator accepts/copies fields before release, and scenario scopes release only their temporary drained requests before restoring separately held requests. No production use-after-release dependency found. Clear and renderer cancellation also release/clear custody.

## Test and acceptance bounds

The reviewed 35-case `GroupedDamageReadoutTests` fixture exercises real ApplyDamage and actual submitted Tilemap glyphs: adjacent 1/2 versus genuine 12, repeated real hits, word/number mixing in both orders, finite lethal removal, origin/owner fog, same-ID living replacement, corners, cancellation, Off/Reduced, wall-clock expiry, warmed renderer allocation, and visible/hidden neighboring-body placement. The actor-cover case at lines 349–381 checks the actual Stunned-over-neighbor defect and the hidden-neighbor non-disclosure counter.

Parent/implementation agent reported the initial two-case numeric RED (one failure plus genuine-12 control pass), followed by the 35-case run (34 pass, actor-cover RED). The final actor guard and broader regression were running when this review closed; this reviewer did not run or independently claim those results.

The fixture does not itself prove a physical Deck result or ordinary combat readability. Native assertions establish complete rectangles and finite ownership. The parent should still inspect the new actual 1280×800 sequence: separate damage values must read as separate events, and Stunned must leave the neighboring player's body unobscured. No additional artwork is requested on source evidence alone.
