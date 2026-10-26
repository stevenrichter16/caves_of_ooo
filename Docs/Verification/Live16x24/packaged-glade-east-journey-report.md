# Current packaged opening journey — 2026-10-26

## Scope and versions

New normal SampleScene Mac and Linux packages were built from frozen runtime
`dce87081d30286ffad870b7b300811f6d898d0e5`, including the approved glade and eastern
forge composition. Both succeeded with zero errors and four reported warnings;
Mac27.57s, Linux28.07s. Existing builds were preserved. The later underfoot
interaction fix in this commit is **not in these frozen packages**.

- Mac: `Builds/Verification/2026-10-26-glade-east-dce87081d-current/CavesOfOoo.app`
- Linux archive: `Builds/SteamDeck/2026-10-26-glade-east-dce87081d-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`
- Archive128,644,536bytes, SHA256 `abb435abb015333722781641b2aeb38b94babba22dc1b3a2ea57b8ede1aa5c05`.
- Build and per-file archive receipts use the `packaged-glade-east-` prefix here.
- No Linux execution, physical Steam Deck controls/performance or new performance benchmark is claimed.

## Actual ordinary journey

Root opened the Mac app, chose a new Warden with ordinary keys and retained the
random seed200893250/game2e93a161b86a495483a3c2d81dab34e2. No forced seed, supplies,
teleport, resurrection or damage bypass. The same game visited all six graphs:
glade11.10.0; cellar11.10.1; north11.9.0; Nella reserve11.8.0; east12.10.0;
kitchen12.11.0. `packaged-glade-east-six-zone-save.json` parses the literal save
header/zone references; it does not interpret entity bodies or prove service use.

| Stage | Actual outcome | Capture suffix |
| --- | --- | --- |
| Fresh glade | Flat16×24, ordinary Warden40HP. | 01-fresh-opening |
| Cellar | Walked around worn-wall blockage, descended and recovered finite crate supplies. | 03-cellar-entry |
| Inventory | Damage1d8, buckler1AV+1DV and rightmost[E] visible; no clipping in reviewed capture. | 04-earned-inventory |
| North | Defeated patch bearer through ordinary combat, looted short sword/haft, harvested Stoneburr and FrostLichen from adjacent cells, brewed frozen+stoneskin coating. | 06-north-contested,07-north-still,08-north-brewed |
| Reserve | Entered actual graph; Nella killed by a viper before her service was reached. **No reserve-service success.** Right-anchor58 frontier not closely surveyed. | 10-reserve-arrival,11-reserve-nella-killed |
| Glade return | Ordinary combat and level2 popup;42HP maximum. Other NPC combat/deaths occurred normally. | 12-level-up |
| East | Reached native workpad, defeated guard, selected brewed coating and looted short sword; real result `frost-quenched short sword`.30HP remained. | 13-east-entry,14-forge-menu,15-forge-tempered |
| Kitchen | Reached court23,9; spoke to Orven Kett and read cot clue. No cooking/rest transaction claimed. | 17-kitchen-arrival,18-kitchen-court,20-kitchen-service,21-kitchen-cot-clue |
| Continue | F5, quit app, reopen and ordinary C returned same game to kitchen23,9,30/42HP,50drams,level2,47weight. | 22-continued-kitchen |

Capture files are original CUA JPEG window captures1280×856 including56px
window chrome and1280×800 game, not concepts. Prefix all suffixes above with
`packaged-glade-east-` and append `.jpg`. Some attempted-action filenames precede
verified outcomes and must not be overstated:02-cellar-entry is still glade;
09-reserve-entry is still north;16-kitchen-entry is still East with pause menu;
19-cook-dialogue is approach with no dialogue open.20 captured typewriter text
partway through;21 records the subsequent cot topic. These originals are retained
as failed/intermediate observations rather than relabeled as successful actions.

`Fixtures/packaged-glade-east-earned-kitchen/` preserves literal Quick files from
this run. It is earned state, not a manufactured completion fixture. Original
normal saves were never loaded or overwritten by this playtest.

## Defect fixed during acceptance

Standing on north Stoneburr12,18 caused C+. to show a generic pile without
Harvest, although Focus advertised it. Adjacent C+right did expose Harvest.
[Underfoot action consistency](../../GRAPHICS-G3-UNDERFOOT-ACTIONS.md) records
the minimal actor-aware query fix:18-case RED15fail/3pass, final176/176 native
cases, independent review, and successful ordinary editor replay. The six first
regression failures were confirmed pre-existing FoundingRest fixture drift at
HEAD; adding real Physics ownership and two refusal counters repaired the fixture
without relaxing runtime rest authority.

Root loaded the exact earned kitchen save through isolated ordinary Continue,
walked back to East23,17 and pressed C+. Harvest appeared directly. H yielded
one StoneburrSeed and advanced4540→4550 ticks; HP30 and drams50 were unchanged.
Reopening showed grass/Examine only, no extra Harvest or turn. Original PNGs and
read-only state JSON are `underfoot-earned-*`. This verifies fixed editor source,
not the older frozen dce package. Native tests do not substitute for these views.

## Independent scene review and next gate

Read-only visual review of actual opening, inventory, north, forge and kitchen
captures finds a coherent palette/projection and readable inventory columns.
It does **not** accept complete opening art: kitchen/orchard has obvious Beehive
`6` glyphs; the service destinations are small low-contrast clusters amid broad
green space. Repeated straight hedge bands compete with useful objects.

Next bounded slice: kitchen court and adjoining orchard. Use these actual
arrival/Continue images as greybox baselines; reinterpret via imagegen, extract
Beehive owner/states and only named unclear service silhouettes, create editable
native sprites, integrate and verify ordinary arrival/action/return/Continue.
Give the court a modest readable entrance/work patch without filling the whole
field with decorations. Preserve existing services, collision, finite stock,
accepted cook artwork and harvested/state ownership. The seed's Nella-viper
service loss is a separate demonstrated acceptance risk, not a graphics regression
proven by these screenshots; do not erase it or silently grant immunity.

## Preservation and limits

Normal SampleScene, native-test isolation, view and preference domains were
restored. All776 protected normal save/backup files match pre-play hashes;
owned fresh standalone saves were archived, not deleted. Build-generated settings
and whitespace-only metadata are preserved then restored separately. No caches,
old builds or other storage were deleted. Foreign PNG variants remain untouched.

Can verify: native routing/counter-tests, actual Mac ordinary input and outcomes,
six visited graphs, fresh default-flat launch, carried crafting chain, Continue,
fixed-source underfoot menu/harvest/depletion, preserved files and package integrity.
Cannot verify: all six services succeeded, complete scene/biome/state artwork,
physical controller feel, Linux execution, Deck performance, crop maturation on
this random-seed run, or post-fix package behavior. Those gates remain open.
