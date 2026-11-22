# Protection tool menus: all held garments stay reachable

November21,2026; parent3b2998a3a. Bottle coats, lasting trough proofing and
unpick now expose every existing candidate instead of12/12/6 and forced-stashing
“more” dead ends. Eligibility, sorting, exact item/element commands, prices,
transactions, clocks and saved fields are unchanged. No inventory renderer changed.
Design/source correction: [PROOFING-MENU-ACCESS](../../../PROOFING-MENU-ACCESS.md).

## Native tests

- `proof_menu_red_20261121a`:9/9 expected failures, zero compiler errors. Real late
  coat absent,15coat choices truncated13,16trough choices truncated13,8unpick
  choices truncated7,32garment/element choices truncated13.
- `proof_menu_green_20261121b`:107/107, zero compiler errors. Includes coat,
  trough, press, real inventory coat input, separate-one UI, action release and
  retained inventory interactions. Four new input cases use30 distinct garments:
  real Down/Enter beyond26, generated retained selected label, first/last painted
  hit bounds map to the same absolute choice, browsing free, one unit/10ticks
  committed only to the chosen ID, stale garment/empty flask/disabled no charge.
  Hit testing is invoked directly; this is not an OS mouse-click qualification.
- Initial GREENa103/107: four input cases used `isActiveAndEnabled`, incompatible
  with HotbarSaveFixture's deliberately inactive root. Corrected predicate checks
  generated label's own enabled/activeSelf state without activating bootstrap.
  Actual pixels are separately verified below. Both runs are retained.
- Final review strengthened unpick to choose WardedCloak beyond the old six-row
  cap; `proof_menu_unpick_late_20261121c`:1/1, zero compiler errors. This revised
  case is separate from the107 run, not an extra distinct test. It removes only
  that owner's enhancement, refunds nothing, and refuses a repeated stale command.
- Other countercases retain exact4units/2clay garment+element bill, other liquid
  and owners unchanged, and short-clay refusal. Independent source review0P1/P2.

## Actual GameBootstrap input

Resumed earned workshop90HP400 at40,9, game8ab6a6384c2a44deb8b3a68303809d8f.
This authored fixture supplies gear/materials; ordinary acquisition is not proved.
Eighteen held garment owners include one worn ColdwardCloak. Before: flask2237
cold-proofing1, popup42choices, twelve coats plus disabled “6 more not shown”.

An initial source review mistook unused legacy25-row rendering for the current
popup. Actual Down26 visibly selects the first coat with19–27/42 choices; current
LedgerPopups already scrolls and uses absolute hit indices. That suspected UI bug
was retracted before implementation. The fix only changes the producers.

After: same90HP400, popup47choices/all18coats. Native Down43 reaches formerly
hidden WardedCloak2342x3. Screenshot shows it highlighted36–44/47. Enter90→100
spends flask1→0/empty, splits stack3→2 plus singleton
`3c6a92d5471b4e47add2a686e7fca289`, coldcoat35/100turns. Other armor remains
plain; preexisting worn ColdwardCloak coating100→99 from normal turn passage.
F5/stop/fresh Continue100 retains exact split owners, coat and empty flask.

Native Equip(auto), then visible replacement confirmation, puts the new cloak on
at no time cost under existing equip rules: cold resistance35. Old ColdwardCloak
moves to pack with coat99 paused. Paid waits100→110→120 keep HP400/Cold35; new
coat100 then99 (existing first-boundary skip), old coat99. Full native two-page
F1 reader describes cold35, wear time and Back slot. F5/stop/fresh Continue120
retains worn owner/Cold35/coat99, paused old99, stack2 and spent flask.

Native reagent menus press two FrostLichen portions into flask2235,0→2→4
cold-proofing,120→130→140. Reopening the trough2133 southwest offers19 actual
proofing choices, including refusals and items previously hidden by its cap.
Native Down19 selects PlateArmor2363x2; screenshot confirms the late row.
Enter140→150 spends flask4→0 and fireclay22→20. Stack2→1 plus singleton
`a724be89e3cb492fba914e0a14fbd7f0` with lasting cold proofing. Other armor and
liquids remain as observed; worn Ward coating96, old Coldward99 paused.
F5/stop/fresh Continue150 retains exact plate split/proof, empty flasks, HP400,
worn Cold35/coating96. This connects ordinary reagent pressing, carried coating,
stack custody, installed trough, equipment effects and persistence.

Native trough menu unpicks that plate150→160, removes its lasting proofing only,
keeps both plates and returns no liquid or clay (clay20/lichen3; flask2235empty).
Reopened menu has no plate-unpick row. Selecting disabled no-proofing leaves160
and possessions unchanged. Worn cloak coat95; paused old99. Saved160 is archived,
but no further fresh160 claim. Crowded unpick8 is tested; actual play used two.

All gameplay used production keyboard/menu/readers/F5/Continue; bridge only
configures isolation and observes. One first fresh attempt lost runtime bindings
while the Editor completed a delayed script reload; excluded from load evidence.
After explicit refresh/compilation settlement the100,120 and150 reloads succeeded.
Transient MCP reconnects were repinned before proceeding; failed scheduling and
null/compile-error observation attempts are not successful gameplay evidence.

## Preservation, archives and limits

Private earned roots: `/private/tmp/coo-proof-menu-coated-earned-20261121` (100),
`coo-proof-menu-worn-earned-20261121` (120),
`coo-proof-menu-permanent-earned-20261121` (150),
`coo-proof-menu-unpicked-earned-20261121` (160); all under `/private/tmp/`,
all contain game8ab6a6384c2a44deb8b3a68303809d8f. The earlier90 archive remains.
Fresh84foreign tracked hashes/861normal save/preference hashes unchanged;
Unity analytics/TestResults excluded. Keyboard/callbacks/settings restored and
main stopped/nullroot. No whole-directory/preference byte-exact claim.

No new full-suite pass, packaged control, ordinary material-acquisition, hazard
combat/damage, all-element/beyond150item inventory or machine-roster acceptance.
No FPS/hardware benchmark. Current Mac74 candidate predates this correction and
Posy's caps fix. Inventory density redesign and population exposure remain separate.

Independent final source and gameplay-evidence review: zero remaining P1/P2.
Reviewer inspected all seven images, JSON, native results/logs and source;
this is artifact review, not a second native keyboard operator.
