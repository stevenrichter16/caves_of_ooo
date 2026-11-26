# Lake perch at The Second Bowl

Lake perch retains its ordinary raw meal. A usable fire can roast each
carried fish into one cooked meal using the existing Cook action. Raw and
roasted perch weigh one pound and have a base value of five drams. The
cooked meal heals `2d4+2` instead of the raw `2d4` and has its own warm
fish icon. A targeted save repair restores only the newly authored recipe
to existing lake perch whose original food is intact.

## Actual GameBootstrap play

The player continued a private copy of the original
[Stump catch and Morrowfast return](../MorrowfastMendedTrap20261125/README.md)
backup at tick 1250. Both current save bodies came from the earned backup,
which already carried raw perch `9426` from the bought trap and recorded
22 HP after ordinary wildlife encounters. The original game and backups
were preserved. The copied backup pair alongside it comes from the earlier
actual catch at tick 1190.

| Tick | Result |
| --- | --- |
| 1250 | The old raw fish kept its exact identity, quantity, weight and value, and now offered the authored Cook action. |
| 1400 | Fifteen ordinary guided steps reached `(48,21)` beside The Second Bowl's actual outdoor stove. The pack offered both Eat and Cook. |
| 1410 | One Cook action removed exact raw fish `9426` and produced one roasted meal `9427`. Health stayed at 22. F5 and fresh Continue retained that meal and its distinct icon. |
| 1420 | The usual Eat action consumed the meal and healed seven HP, reaching 29. F5 and fresh Continue kept that health and the spent meal. |

The earlier raw branch healed eight on a separate roll, so the two observed
heals don't measure the difference between recipes. The authored modifier
and native healing bounds establish the bonus. This branch used the public
stove. The existing portable heat policy also governs the recipe and this
slice adds no new fuel debit. Portable roasting and other seeds remain open
in actual play.

## Verification

Two compiling RED cases reproduced the missing recipe and saved-catch
compatibility. The unrelated old meat guard passed. The final connected
run passed 128 cases with no compiler errors. It covered the exact
one-for-one transformation, old-fish cooking, raw-food migration control,
healing, finite heat and item icons. The generated atlas and index matched
byte for byte. Independent source and recorded-gameplay review checked the
saved fish bodies and screenshots.

- [Both actions on the raw fish](01-raw-choice.png)
- [Roasted fish after fresh Continue](02-roasted-meal.png)
- [Observations and archive pins](observations.json)
