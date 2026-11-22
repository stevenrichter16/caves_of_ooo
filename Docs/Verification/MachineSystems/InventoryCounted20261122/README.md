# Counted inventory groups — native qualification, November22,2026

Matching suitable items now share counted Inventory rows. Individual owners and
physical stack quantities remain intact. Five empty flasks can occupy one row;
three same-liquid2/12 flasks can occupy another. Using one changes only its owner
and the rows regroup when the existing model refreshes.

Parent documentation head307886174; exact13owned source/test/meta pins are in
source-pins.json. This receipt qualifies the bounded grouping slice, not global
physical merging, shop/container UI redesign or a complete-machine delivery.

## Contract and source

InventoryItemGroups captures an internal ephemeral identity during the existing
InventoryScreenData model rebuild. Explicit audited built-in Part types compare
their scalar payloads, exact blueprint/base name, stats/condition, tags/properties,
contents/capacity, charges and treatments/lifetimes. Material tag sets are compared
canonically; finite floats/doubles use round-trip formatting. Keys are bounded to
16,384 characters and never displayed. Unknown/subclass/non-scalar or active bookkeeping
payloads, malformed contents, stale custody, equipped owners and live protected
claims stay separate. Ordinary containers, kits, traps and flickering torches
remain conservative exclusions unless their complete Part graph is admitted.
Existing physical stacks continue to work.

Grouping metadata stores retained member displays, the selected member and
aggregate units/weight. ItemDisplay.Item/StackCount and all actual entities are
unchanged. Search filters cached member displays before projecting groups; paint,
query, toggle and resize do not rescan Parts or invoke state/action providers.
This grants no physical merge, split, processing or save-migration permission.

Grouped is default. G or the painted grouping control shows individual entries;
the controller Commands menu offers Show individual items / Group matching items.
Selecting a member then regrouping preserves that exact owner. The action popup
reserves a separate scope line before the fitted name: selected1, or the selected
physical stack's actual size, out of the grouped total. It says to leave the menu
before toggling; G inside the popup retains its existing action-hotkey meaning.
Commands act on that selected owner, never implicitly on every group member.
For example, grouped physical stacks2+3 show5 but Drop still drops the selected
physical stack, with its real quantity disclosed.

## Native tests and review corrections

Initial REDa26:13fail/13controls,0CS. Initial GREENb25/26; the one new pour fixture
had no matching ground-water recipient and needed an unbound factory fallback.
It was corrected to an existing empty water recipient, then to respect the real
paid-action UI close/reopen. This did not change production liquid behavior.

Independent source review found three P2s: popup G instruction conflicted with
the existing modal hotkey, long names could hide action scope, and controller
users lacked a route to hidden group members. Review REDd4 contained3 relevant
failures and one inconclusive old float control. All three defects were fixed:
explicit Escape-then-toggle instructions, independent scope/name line budgets,
and a controller command. REDc overlaps d and is not an extra total.

The old float control used a Torch with an unadmitted flicker Part, so it never
proved key discrimination. The final admitted-Dagger fixture first proves equal
thermal states group, then an adjacent finite float separates. Round-trip
formatting is defensive hardening, not a fourth reproduced runtime defect.

Final GREENg: **329/329**,0CS:30new and299previous inventory/reader/tool cases.
Five existing tests were updated without removing assertions: four multi-owner
navigation cases explicitly choose Singles; public-state search now checks the
grouped quantity2 and both owners in Singles. Earlier e153/155 had the fixture
failure and old two-row expectation. Runs overlap and are not additive.

Additional neighboring f257/258 had one failure:
DensityLiquidVesselAdversarialTests.WellDrawRemainsProtectedFromUnrelatedGroundSpill.
The exact test also fails on unmodified307886174 in separate clone4 (h1/1fail).
It is not a grouping regression; no all-adjacent-green claim or unrelated fix.
Two isolated mutants are killed with0CS: ignore identity11fail; ignore selected
member1fail. No EditMode test ran in the main Editor.

## Actual GameBootstrap play

Start: earned regional-mire checkpoint2610, Overworld.10.11.0,40,9,HP399/400.
Equipment, capacities and camp are inherited scenario fixtures, not acquisition
or balance evidence. The starting Inventory has69owners/234units, projected into
61rows. Native input shows six empty flasks grouped, three full mire flasks
grouped, and two timber stacks17+10 shown as27. All owners remain separate.

To establish the user's exact partial-flask example through ordinary actions:
-2236's2 water were poured west onto39,9 (2610→2620).
- Existing still2148 processed only fixture flasks2232–2234: each12mire→3water
  across2620→2650. Hearth2147 fuel20.5→4.5 includes15process fuel plus1 ordinary
  burn across the initial pour and three batches. This is not new ordinary feed.
- One ordinary westward pour from each of those three flasks paid2650→2680.
  All three now hold2/12 water and occupy one row with quantity3.
- Native Drop on empty2236 and2237 removed only those two owners from the pack,
  individually, keeping tick2680 under the existing free-drop policy. Five empty
  flasks2238–2242 remain in one row with quantity5. Both dropped owners remain
  empty on the player's ground cell; nothing was merged or destroyed.
- G shows the five individual entries. Native selection chose third owner2240;
  regrouping and opening actions preserved2240 and disclosed selected1of5.
  Filling from existing pool14783 paid2680→2690: only2240 gained1 water, the pool
  retired, and the other four empty owners were unchanged.
- Native use of group member2232 poured one water west,2690→2700. Only2232 fell
  2→1;2233/2234 remain2. The1/12 row now groups2232+2240, and the2/12 row groups
  2233+2234. Ground pool14784 holds6 water. Carried6+ground6=12 water, matching
  starting3 plus9produced. Original mire2231/2235 stays11/1, source13151 stays48.

The exact selected2232 reader's two pages were read through F1/PageDown and
returned freely. Its contents remain1/12, separate from group quantity2.
F5 saved2700; archive was copied BEFORE Stop. Fresh normal Play/Continue retains
exact measured world state, every recorded pack/flask owner, both dropped owners,
contents, source48, pool6, hearth3.25fuel/207.244644/light on and actor.
Fresh filtered group rows also match exactly, including selected representatives.

1280×800 captures retain44row geometry; the unchanged layout at1920×1080 measures
66capacity,10×20px body glyphs and full canvas corners. The1080p filtered capture
shows grouping controls and rows; it does not claim66distinct visible owners.
No physical keyboard/controller feel, packaged interaction, FPS or performance
timing acceptance. Native screenshots, read-only observations and archive pins
are retained; independent review is evidence review, not a second game operator.

## Preservation and remaining work

Main stopped, save override null, scene clean, profiler off, original7GameView
presets/custom0/index0 restored. Fresh18:46UTC84foreign and861normal file hashes
unchanged; Unity analytics/TestResults.xml excluded. No whole-directory or
byte-exact preference claim. All prior builds remain preserved.

A transient MCP routing failure during clone exit posted no verified action;
root repinned and checked unchanged2610 before retry. First fresh-start request
was refused while Stop cleanup still held its old override; after observed
rootnull normal fresh succeeded. Both are excluded from acceptance.

The separate water-cooling P2 and unloaded-fire chronology limitation remain
open. Category/sort, further explicitly audited grouping capabilities and physical
input remain separate future work. This user-requested inventory slice takes
priority; next machine work resumes the reviewed finite-cooling correction.

Native XML remains verbatim. Initial REDa has four trailing spaces in Mono
stack-trace lines; this is the only staged whitespace-check exception. Owned
source and prose pass the whitespace check.
