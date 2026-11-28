# Scraper reach below Tine

The scraper offers a natural deposit when the traveler can see it and has a clear line to it. The pack supports a distance of two tiles. Interact supports an adjacent deposit. Both keep the quoted zone, cell and source fixed until confirmation. The usual sound, exact stock debit and vent-scale stack still follow the work.

## Ordinary play in GameBootstrap

Seed 2 began from the private copy of the ordinary Tine arrival. The traveler had 50 drams and no scraper. Tinker Vellin Rillpost sold the shelf scraper for 32 drams. The traveler took the surface stair at (21,14) and the depth-one stair at (40,12), reaching the second cave level at tick 780 with 40 HP. [Trade](01-purchased-scraper-quote.png)

| Approach | Observed result |
| --- | --- |
| At (24,11), tick 940, prior runtime `c9a40dd46` | The pack named the vent at (22,10), through solid rock. Confirming took one turn, added one vent scale, and dropped the shared stock from four to three. [Prior menu](02-old-vent-through-rock.png) |
| Fresh Continue at the same (24,11), tick 940, current runtime | The pack omitted the obscured vent. Opening and closing left the turn, stock and exact pack unchanged. [Current menu](03-new-no-vent-through-rock.png) |
| Walked around the rock to (24,9), tick 960 | The now visible vent had a clear line at a distance of two tiles. The pack named it. One action put one scale in the pack and took the shared stock from four to three. [Clear approach](04-clear-side-pack-quote.png) |
| Walked to (23,9) and used Interact | The action named the same adjacent vent. One action added a second scale to the same stack and took the shared stock from three to two. [Adjacent action](05-adjacent-source-scrape.png) |
| Fresh Continue at tick 990 | The traveler returned to (23,9) with the exact scraper, two scales and two catches remaining in the stock shared by both vents. The marlback gleaner’s damage also persisted at 9 HP. |

The gleaner hit the traveler while they moved and worked. The action log reported that the scraping rang. This route did not isolate how creatures respond to the sound or process the recovered scale into a later product.

## Verification

- The prior source passed 43 controls and failed the 13 targeted cases expected to expose stale, concealed or obstructed work. The final source passed all 229 focused Unity EditMode cases. Late cancellation checks cover exact stock and catch refunds while retaining independent injury.
- An independent source reviewer and play evidence reviewer checked fixed quotes, normal trade and movement, the two physical catches, shared stock and private saves. The separate save readback confirmed the same scale stack and stock debit at each paid checkpoint.
- Unity Editor on Apple M5 verified this route. The 880 checked normal save files, 87 pre-existing tracked edits, and both preference files matched their recorded bytes after the run. [Exact observations and checks](observations.json)

## Current Mac build

Runtime `c216af284` replayed the private BehindRock checkpoint on Apple M5. The pack omitted the vent on the blocked side. A marlback gleaner occupied the approach, and a fight delayed the walk around the rock. From (24,9), the pack offered the vent again. The scrape put one scale in the pack and left three of four catches. A second launch restored the saved position, 39/40 HP, scraper and scale at tick 1010. The shared stock was independently read from the private save.

The app was built from 32,330 committed inputs with zero errors and the same four warnings as the previous build. All six universal binaries were checked for their Apple silicon and Intel slices. Live testing used Apple silicon. All 19 pinned prior apps matched their recorded files. [Blocked pack](mac-01-vent-obscured.png) · [Clear pack](mac-02-clear-scrape.png) · [Fresh Continue](mac-04-fresh-continue.png) · [Build and save receipt](mac-package.json)
