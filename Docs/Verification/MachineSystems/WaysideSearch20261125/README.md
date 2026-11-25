# Orven checks the loading point

November 25 UTC, 2026. Local candidate built on the saved overdue notice.
Independent final review and publication remain open.

Forty ticks after the notice, Orven can take one short walk to check the
loading point and return to the kitchen. The existing recipient does the
walking on ordinary idle turns. The saved assignment follows the exact order
and endpoints. Work on a paid meal and danger take priority. Each leg has a
24-attempt limit, and actual movement uses the same checked paths as a porter.
Finishing the walk leaves the notice open until the shipment is received or
another supported resolution occurs.

## Editor play

This is a controlled interruption branch from the earned seed 64 overdue
checkpoint at tick 250, game `e20820bd3ab048c892516952957e1e56`. The original
archive stayed intact. The editor applied one 60-turn Rooted effect to the
live porter so the player could observe the full search before delivery.
The copied save supplied the original objects, actors, positions and money.
The player then moved and interacted through the ordinary controls.

| State | Observation |
| --- | --- |
| 300 outbound | Orven `6853` had walked to 27,16. His ordinary Examine reader said he was checking the loading point for news of the shipment. |
| 370 along the route | The player moved west. The same searcher was visible at 20,16, eight movement attempts into the walk. |
| 400 loading point | Orven reached 18,16 beside marker `6892`, checked it and started back. |
| 510 returned | He was back at 27,17 beside the receiving shelf. Examine reported the completed check. The overdue notice stayed open and the same carrier still held crate `6896` with stack `6897` of two measures. |
| 530 ordinary trade | The player went back to Orven and opened trade. All five initial food lines and the opening clay stack were available. |

F5 and fresh Continue matched the selected outbound and returned states.
One ordinary wait after fresh Continue at 300 advanced the same assignment
by one checked step. A wait after fresh Continue at 510 left the completed
assignment finished. Both selected purses, exact crate ownership and the open
notice survived. The play branch has a live porter and tests the search walk,
return and existing trade. Recognizing bodies or cargo, reporting evidence
and assigning an employee are separate work.

After the endpoint fix, a separate fresh Continue from the outbound archive
reached tick 310 with Orven one step farther along. At that point the Editor
moved the actual loading marker by one tile in the temporary copy. On the
next ordinary wait Orven turned back without recording a completed check.
He finished at the original shelf by 350. The delivery failed under the
existing order rules and refunded four drams to the supplier. The living
carrier kept the same box and clay, and the notice stayed open. This is an
endpoint interruption fixture. Both original save archives remain intact.

- [Orven's outbound reader](01-outbound.png)
- [The loading point check](02-marker.png)
- [Orven's returned reader](03-returned.png)

## Unity verification

The compiling RED passed 14 of 17, with all three search cases failing before
the assignment was wired. The first targeted pass was 48 of 48. The focused
search and receipt pass was 19 of 19. The added controls cover an inactive
zone, save/reload mid-walk, a commissioned meal, rooting the searcher, receipt
before departure and a blocked route that exhausts its budget without retrying
the entire search. The earlier expanded pass was 456 of 456. Two additional
cases reproduced stale searches after moving the loading marker or removing
the shelf. The final expanded Unity pass was 458 of 458, including both
endpoint cases, with zero compiler errors, skipped or inconclusive cases.
The native receipts pin 32 matching project and test-clone files.
