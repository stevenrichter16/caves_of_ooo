# Temporary territorial enforcement and defensive bumps

Status: implemented; native GREEN and affected regression checks passed, repeated ordinary live journey14/14 passed. Native RED and GREEN recorded 2026-10-09. This is a player-facing correction discovered while validating the Sodden art route, not a change to that route's completion criteria.

## Observed problem and source sweep

During the native Sodden journey `1848f6cae6dd4bbe8a799aceb0de9612`, the player at38,2 attempted A toward the adjacent cutbank watcher, whose Brain.Target was the player. Neither melee nor a turn occurred. The watcher belongs to Villagers and territorial entry deliberately creates no permanent personal grievance. InputHandler admitted bump attacks only through FactionManager.IsHostile, so the active neutral enforcer could fight without allowing the corresponding ordinary defensive input.

The actual passage is authored by SecondExplorationSites.Regional at35,2, with its post at36,2 and a claim covering35,0–45,7. The claim has a warning and two complete grace opportunities. LocalPassagePermitPart sells a four-dram, one-crossing permit before entry; the exact guard/token/holder and physical post determine whether it allows the player. SpreadTerritoryPart reuses KillGoal for combat after grace, without setting faction or personal hostility.

Brain.Target alone is insufficient authority. The territory clears that pointer only on its next scheduled idle action after withdrawal, payment or loss of its post. Immediately after any of those changes it may still name the player. Temporary NoFight, conversation, party alignment and hospitality also require their normal protections. The source sweep found that neutral permit-entry selection independently bypassed the hospitality floor: ClaimsEntry could select a protected player even though FactionManager denied hostility.

Reviewed sources:

- [Territory and temporary combat](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/SpreadTerritoryPart.cs)
- [Real permit and crossing token](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/Exploration/SecondExplorationRoutes.cs)
- [Authored cutbank placement](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.Regional.cs)
- [Native movement and bump dispatch](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Input/InputHandler.cs)
- [Faction, party and cloth rules](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/FactionManager.cs)
- [Whole-action territory execution](/Users/steven/caves-of-ooo/Assets/Scripts/Gameplay/AI/Goals/BoredGoal.cs)

## Design and implementation

SpreadTerritoryPart exposes the pure IsEnforcingAgainst(target, zone) query. It requires the exact current living role owner, attached Brain in the current zone, live target, physical post, configured territory, matching WarningTarget and Brain.Target, exhausted grace, and a still-valid current claim. It refuses party membership/alignment, conversation, NoFight, a noncombat goal and hospitality protection. The target must still occupy the watched area and lack its valid local permit. The query creates no hostility, advances no grace, moves nobody and clears no AI state.

The same narrow ClaimsTarget predicate now checks the territory's real target selection and current enforcement query. The hospitality oath therefore prevents the actual neutral enforcer's attack, as well as withholding a defensive bump against a protected guest. Existing genuine faction/personal hostility continues through its established path.

InputHandler admits a blocked creature if either the established faction predicate is hostile or the exact territory currently enforces against the player. It then uses the existing CombatSystem.PerformMeleeAttack and EndTurnAndProcess once. No alternate combat execution, extra action, global faction rewrite, new saved field, new enemy or scene/harness exemption was introduced. Real combat consequences remain native: dealing actual injury can provoke the ordinary personal response; merely querying temporary enforcement cannot.

## Test-first evidence

[TerritorySelfDefenseInputTests](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Input/TerritorySelfDefenseInputTests.cs) contains21 native cases. The actual RED run is [native-territory-self-defense-red.xml](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/native-territory-self-defense-red.xml):17 passed and4 failed as intended, before production changes.

Failures were both actual A/D defensive bump inputs, neutral permit enforcement against a cloth-protected player, and the absent pure current-enforcement query. Existing passing controls cover no warning, the warning and both grace actions; stale withdrawal; removed/moved post; removed role; wrong warning owner; conversation; calm; party alignment; cloth on either participant; dead guard; a real permit bought before re-entry; and ordinary faction hostility.

Tests drive the existing keyboard/InputHandler and real factory guard, permit command and Brain turns. A melee veto probe counts admission without introducing random injury or permanent hostility; scheduler assertions prove exactly one ordinary player action. The probe is deliberate isolation: damage, death and the live route still require their normal integration evidence. Queries are checked for unchanged targets, grace and personal-enemy state.

The native [territory and worksites regression receipt](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/native-territory-worksites-regression-green.xml) records **123 passed, 0 failed, 0 skipped**. All 21 defensive-input cases pass, together with 88 SecondExploration source, service, site, retention, rope-travel and adversarial cases and 14 existing worksite-manifest cases. This confirms the narrowly changed input/enforcement contract and these affected native regressions; it does not substitute for the repeated Play journey.

## Review and remaining verification

Code review confirmed the additional authority is consulted only for a blocking creature, remains subordinate to the existing normal input/turn/door flow, and does not replace the old faction gate. The guard's actual target selection and defensive admission share permit/area/cloth rules. Withdrawal and permit tests deliberately leave Brain.Target stale and do not give the guard another action before the player bumps. No per-frame zone scan, resource load, mutable cache or new update loop was added; the query runs only on the existing blocked movement branch.

Whitespace checks, native GREEN and the affected 123-case regression gate pass. The repeated live Sodden journey subsequently passed as recorded below. The interrupted earlier broad graphics run is not evidence of passing these changes.


## Repeated ordinary Play result

`Verification/SpreadDiscoveryExpeditions/Native/29dcc15952cb499abeabcccca9733cf1/report.json` completes14/14 checks in237.40seconds with zero failures and zero unexpected runtime errors. The original ordinary player crossed the watched area, resolved the returning defensive combat using the earned mallet, repaired the shelter with recovered timber, bought its preparation service and restored the result with F5/F6. The earlier zero-action guard refusal is gone. The original failed report remains alongside this result; no clock, health, stock, AI or route exception was added to get a pass.

This establishes the scripted seed64 journey and the tested current-claim boundaries. It does not establish all-seed combat balance, universal visual/feel quality or standalone build performance. Native damage and resulting faction consequences occurred through the existing combat path.
