# Bog-mire field recovery

November 23, 2026. Bounded Editor recovery accepted after independent source and
gameplay-evidence review. Current Mac203 predates this increment; packaging is open.

An actual retained Quillhold branch at tick 7100 has 272 units of bog-mire,
Agility 16 instead of 18, HP 218/400 and eight carried prismreed pith. Its real
inventory popup offers brewing but no body-cleanup action. Baseline screenshots
and a fresh F5 archive are retained under `/private/tmp/coo-mire-pith-20261123`
and `/private/tmp/coo-mire-pith-before-earned-20261123`. This branch inherited a
strong supplied test character and pith; it does not prove ordinary acquisition.

## Source corrections and alternatives

Bog-mire is an acidic coating, not internal poison: its definition deals one
Acid damage each turn and applies Agility -2. `LiquidCoveredEffect` naturally
loses Fluidity + Evaporativity = seven units per EndTurn at ordinary temperature;
heat above 50 C accelerates that loss. The earlier 272-unit coat could therefore
dry naturally in 39 turns. That is a source-compatible explanation, not proof
of the unexplained removal on the earlier merchant play branch. The acidworker
apron's resistance does not make one-point acid damage harmless.

Existing body cleanup already spends one pith to remove at most 20 units of
oil, pitch or honey from self or an adjacent visible, living, nonhostile party
creature. Ground wicking is a separate action for temporary thin films; it
cannot drain permanent sources or real pools. Neither action recovers liquid.
Pith also remains a brewing ingredient for a charged coating. The baseline
description names brewing; a separate existing tactical-use reader already
names oil/pitch/honey body cleanup. Actual after-play exposed that additional
reader, so its list must also gain mire rather than leaving inconsistent help.
Existing crop
harvest yields two pith and one seed; this slice does not change that economy.

## Smallest change and accounting

Add bog-mire to that existing explicit body-coating adapter. Reuse its exact
effect token, quantity/position/custody bindings, claims, payment, rollback,
diagnostics and normal effect-removal path. No new Part, save field, dispatcher,
water-washing rule, neutralization, ground-pool removal or output item.

One pith and the absorbed coating are discarded. Partial cleaning keeps the
remaining coat's full damage and Agility penalty; removing the last coating
reverses only its recorded modifiers and does not heal prior injury. A paid
world turn also permits normal drying: 272 to 245 can mean 20 wiped plus seven
dried, not a 27-unit pith capacity. Waiting remains an ordinary alternative.
Manual companion care does not add exposure or change NPC survival AI.

Public item guidance must explain brewing, body targets, finite removal,
discarded supplies, remaining penalties and the separate thin-film/pool limit.
Do not advertise poison cure, lasting protection or acid immunity. Bulk bath
treatment remains a proposal; no new machine is accepted through this slice.

## Independent critique and gates

Pre-RED independent bounded design/source review found no P1/P2 objection:
reuse the manual adapter; preserve all seven rejected-liquid controls; cover
self/party and 1/20/21/large amounts, partial/final modifiers, unrelated statuses
and ground liquid, replacement/stale authority, outer veto and binary reload.
Actual play must distinguish the wipe from natural drying, retain previous
injury, show the public reader and cancellation, and reload finite resources.
Do not spend the entire stock simply to prove the final-removal endpoint.

Final native261total/260passed/0failed/1Explicit regeneration utilityskip/0CS,
including20new cases; compiling RED19 plus readerRED1 preceded the behavior and
reader corrections. Actual earned7100→7450 play demonstrated free cancel/read,
paid partial20+natural7 loss, movement/waiting, final14 removal, Agility16→18,
retained HP184 and six pith, then F5/fresh7450 exact. Independent bounded source
and gameplay-evidence review found0remainingP1/P2 (read-only, not a second native
operator). See [the native receipt](Verification/MachineSystems/MirePithRecovery20261123/README.md).
All twelve full-machine acceptance gates and the original failed full regression
remain open. Current Mac203 excludes this slice; build at the next coherent
playable package milestone while preserving every prior app.
