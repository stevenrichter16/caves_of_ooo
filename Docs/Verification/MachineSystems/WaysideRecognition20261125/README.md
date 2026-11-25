# Orven reports what he finds

November 25 UTC, 2026. Local candidate for requirements 44 and 45 of the
world opportunity roadmap. Independent review and publication are pending.

During the outbound search, Orven can recognize the missing porter’s
actual remains within three tiles and within his ordinary sight. The saved
observation records the body, location and game time. The receiving shelf
keeps its overdue notice until Orven actually returns with the news. The
original shipment stays unresolved and its surviving contents stay physical.

## Editor play

The branch began with fresh Continue from the earned seed 64 overdue save at
tick 250, game `e20820bd3ab048c892516952957e1e56`. In the temporary copy we
applied one 60-turn Rooted effect to delay the real porter. The original saves
are intact. The player then walked west, equipped their starting dagger, and
used ordinary Flaming Hands, Ground Surge and melee attacks. Combat involved
the real worker at full starting health. Ground Surge displaced the porter
before the next two ordinary northwest inputs moved and struck.

| Tick | Observation |
| --- | --- |
| 370 | The last dagger hit killed porter `6893`. The normal death flow placed his body `6899` and the same crate `6896` at 24,16. The crate held the same two measures of clay. Orven was shaken under the existing death-witness rules. The delivery failed and refunded four drams to the supplier. |
| 560 | After normal waits, Orven’s witness reaction had ended. His search remained outbound and its observation was empty. |
| 620 | Orven was visible on his return leg at 26,14. He had recorded body `6899` at tick 590. The player examined the receiving shelf and still saw the overdue notice. |
| 660 | Orven finished beside the original shelf at 26,17. The player examined the shelf for free and saw the report. The crate remained beside the body and the shipment was unresolved. |

The corpse’s source and killer were checked in the Editor for verification.
The shipment report says what Orven found and leaves blame to the game’s
existing social rules. The original combat also produced ordinary reputation
changes. Recovery of the physical cargo and employee-led searches remain open.

- [The player and the physical aftermath](01-ground.png)
- [The shelf while Orven is on his way back](02-before-report.png)
- [The shelf after Orven reports](03-reported.png)

## Saves and native tests

F5 produced separate four-file checkpoints at 370, 620 and 660. Fresh Continue
at 620 matched the selected saved state, including the observation and cargo.
Four ordinary waits produced the same returned state and report. Fresh Continue
at 660 matched the finished state. The next ordinary wait kept the search
finished and the saved log retained the single previous report. These checks
compare actors, order, observation, cargo, inventory and purses. The snapshots
omit presentation visibility and compare the log separately.

The first native test attempt used an internal-only fixture property and
failed compilation. The corrected compiling RED passed 22 of 23, failing
where a real, now visible body should have made the searcher turn back. The
first targeted pass was 51 of 51. Cases cover an intervening wall, a different
porter with the same blueprint, a body held in inventory, a live carrier and
an observation surviving a fresh load after the body was removed.
The expanded pass was 467 of 467 with zero compiler errors, skips or
inconclusive cases. The final source pins match 33 project and test-clone
files including the native corpse spawner.
