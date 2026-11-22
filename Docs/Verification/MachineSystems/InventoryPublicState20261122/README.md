# Public item state in the inventory — November 22, 2026

Baseline651079345/runtimefd4a638b4. A bounded second inventory increment:
the same44/66-entry layout now shows and searches public flask contents, water
charges and selected garment treatments. It helps choose finite feedstock,
receivers and expedition gear without opening each identical name separately.
Category buttons/sort, full redesign and all twelve full-machine gates stay open.

## Contract and source corrections

ItemDisplay snapshots State once with the existing model. Existing IToolMenuState
supplies flask/skin summaries; a new optional default-empty GetInventoryState hook
on IItemEnhancement opts in lacquer, lasting proofing and temporary coats. Both
explicit capabilities can contribute on one Part. No arbitrary Part/type/private
field scan, blanket enhancement-name or full-description extraction. The default
enhancement display name can return an implementation type name, so it is not used.

Extra summaries require current two-sided carried/equipped custody, visible owned
Render/Physics Parts and current provider membership/ParentEntity. Recheck after
callbacks and exceptions; faulty providers cannot publish after rehoming themselves
or changing custody/visibility. Errors become generic unavailable text, never
exception messages. Summaries are stable/distinct/sorted, normalized to one line,
bounded160/provider and256 total with ellipses. This is the complete cached public
summary, not unlimited provider text or a new universal information policy.
The existing Examine system has no general identification gate; protected lore
and non-opted-in metadata gain no new search route.

Shared flask/skin providers now report malformed quantities, missing registry or
unknown/unlabelled liquid state as unavailable. Valid labels/grouping stay; raw
unknown LiquidId is not a fallback. Treatment labels use existing public canonical
element policy (including fire→heat and unknown→acid), never raw Kind/bonus flags.
Exhausted coats say spent and malformed coats say unavailable. Labels do not add
a remaining-duration/bonus promise. Lacquered describes treatment presence.
Striker charge counters remain in the existing public name, not a duplicated badge.

Name and state have independently fitted budgets in the existing name cell.
Both overflows are marked; the unabridged cached summary leads selected/F1 details.
At800p some combined treatments truncate in a row but remain complete in the
selected pane;1080p has room for the observed combined treatments. Glyphs, row
height, quantity/weight/equipped columns and44/66 capacity remain unchanged.

Search matches retained name plus State literally, ignoring case. It rebuilds
rows rather than the model: no name/action/state rescan while typing,
backspacing, matching, hovering or resizing. Selected prose refreshes on real
owner/model changes, rather than same-owner paint or search cancellation. Editing hides the selected pane but
retains its private same-owner document, avoiding a second description on cancel.
No public details/document or Details hit is exposed during typing. Open, committed
commands and existing reader-return refreshes still rebuild; actions retain their
current-state validation. This is a snapshot, not live polling of the world.

No new saved Part fields, command, recipe, custody, stack/merge or game-time rule.
Treatment summaries do not alter world-menu grouping. Shared provider validation
also makes malformed world-menu labels truthful, without changing valid commands.

## RED, countercases and regression

Valid primary REDb34:33fail/1striker control,0CS. Initial REDa used nonexistent
striker Charges in its fixture; actual source field is Strikes. That compile-invalid
attempt has no results and is excluded. Initial GREENc34/34 preceded further review.
Review REDd10:2fail/8controls,0CS, reproduced lost second capability and publication
after provider reparenting. Both fixed. Controls pin malformed coat totals, public
canonical fallbacks and shared valid/invalid flask world-menu grouping.

Broad GREENe299:298pass/1 inherited assertion required the private document to be
cleared while typing. It was retargeted to require a retained private snapshot AND
null public presentation/text/no Details hit, preserving suppression and custody/
free-time checks. Final GREENf299/299,0CS =44new+255existing distinct cases.
Do not add overlapping runs. No full-suite/hardware qualification.

Native cases include public quantities/treatments, same-name owners, unknown
enhancements/IDs, hidden/stale/foreign ownership, throwing/rehomed/dual-role providers,
bounded multiline data, literal snapshot matching, free cached search/cancel/resize,
exact model/description/action query counts, rebuild-after-state-change, fitted
badges and44-entry density. Main Editor ran no EditMode suite.

## Actual normal-input qualification

Current GameBootstrap Continue with owned temporary InputSystem keyboard, ordinary
UI and action handlers. No runtime grants, teleport, direct command execution or
tick jump. Previous inventory-density images supply the unchanged-source before
baseline with identical flask names; these are not new acquisition evidence.

An initial Play produced enabled bootstrap/input components but no player/zone.
No input was posted. It was stopped and its root restored; reopening the SAME
clean saved SampleScene then ordinary Play recovered. No bootstrap method was
invoked manually and no scene file saved. Cause is unproven. Several read-only
observers used incorrect API/visibility assumptions and failed in-memory compilation;
only corrected successful observations are evidence. No global clean-console claim.

### Workshop branch

Earned1590, Overworld.4.9.0 at40,12,69owners, fixtureHP400/hard5000/soft1050.
Find heat coat selects worn2342 with heat coat; lacquered. Find empty yields
eight empty receivers; End/F1 selects2242 and both reader pages return to it.
Model hash is unchanged through those queries. Actual44 entries at800p and66
at1080p, physical10×20 glyphs and full target corners are recorded; resize is free.
Combined treatments/full/empty flasks and striker[19] are visibly distinguishable.

Find bog mire selects2231 among four full12-unit flasks. Ordinary popup navigation
to pour one east was previewed then cancelled: exact world snapshot unchanged at1590.
Reopening and choosing the same row paid1590→1600,2231:12→11, created real pool12315
at41,12 holding1. Reopened pack/reader shows11/12, not the old cached12. Both
mire-reader pages read and returned. All other flasks unchanged.

Ordinary right step to41,12 paid1600→1610 and applied existing mire-soaked contact:
HP400→399, no WetEffect. Bog-mire data declares1 Acid damage; the native message
records mire-soaked and its later expiry. This is existing fluid exposure, not new
shared-exposure/NPC enrollment or a mitigation claim.

C then period opens the actual world menu on pool12315. Its two fill choices
distinguish11/12 bog mire via2231 from empty via2235. Cancel leaves exact1610 state.
Reopening and choosing the empty receiver paid1610→1620:2235 gains1,2231 stays11,
other three full flasks stay12, pool retires. The counted flask+pool stock is48
before, after pour, after contact, after recovery and after fresh load. This is
not a claim that visual contact counters are collectible material.

Find bog mire now shows five same-name owners with11,12,12,12,1. F5 saved1620,
HP399,41,12, active coat97. Graph copied BEFORE Stop, then fully fresh GameBootstrap
Continue gives exact measured world snapshot, same69IDs and all69cached name/
state rows.
No health/damage reset. The fixture stock is not ordinary mire acquisition.
Archive: /private/tmp/coo-inventory-public-state-earned-20261122/
8ab6a6384c2a44deb8b3a68303809d8f.

### Carried-water branch

Separate earned60 trial archive from WaterskinTransactions20261122, not a new
production-source migration. Find waterskin shows2254[2/3], debt1, HP40,
Strength17/Agility17/Toughness18. Native drink row spends60→70 and2→1, clears
one debt and restores Strength/Agility18. HP40 unchanged; no healing benefit.
Reopened inventory shows same2254[1/3]. F5/archive BEFORE Stop and wholly fresh
Continue70 retain exact measured actor/skin snapshot, all7cached name/state rows
and1/3 badge.
Archive: /private/tmp/coo-inventory-public-skin-earned-20261122/
9c8e062b087341acb5c7e5f81250eb78. Earlier water trials are not replayed/added.

## Review, preservation and limits

Independent design and final bounded source critiques found no remaining P1/P2.
Final durable artifact/gameplay-evidence review found zero P1/P2, recorded in
review.md; no second native
operator/hardware play claim. source-pins.json pins15source/test/meta paths;
four XML and14PNGs plus observations/test-summary/preservation accompany this
receipt. PNGs are actual GameView readback with vertical row orientation corrected,
no resampling. Other private duplicate captures and both source archives remain.

Main stopped/not compiling, clean scene, save override null, profilerfalse,
viewOwnedfalse. Original seven GameView presets/custom0/selected0 restored by
removing only owned absolute7. Owned keyboard/settings/callbacks removed.
Fresh17:21UTC84foreign tracked and861normal selected files unchanged; no whole-
directory/plist byte-exact claim. All prior builds, normal saves/preferences,
foreign files and parked art preserved. No new profiler/FPS/performance claim;
cache behavior is source/native-test evidence. Controller/pointer tests are not
physical hardware/Deck evidence. Macfd4 predates this increment until a new receipt.

Next: coherent current Mac candidate, then source/design and actual baseline for
an ORDINARY regional mire supply→existing camp/community processing→useful output
trip. Existing fixture still/oil/cold chains and finite-poured-fuel correction are
already bounded qualified; do not repeat or reopen the closed oil-film P2.
No new recipe is implied. Establish real source/capacity/provenance and practical
carry/service/processing alternatives before implementation. Keep chronology and
protective NPC behavior gated before shared exposure enrollment.
