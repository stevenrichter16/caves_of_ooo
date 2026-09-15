# Ginmere native composition

Status: complete and installed. Final targeted gate 396/396. Full suite 12,570 total / 12,538 passed / 32 unchanged baseline failures / zero C# errors. All 307 new feature cases pass; the suite also adds one surrounding equipment countercontrol.

## Scope and canon

Exactly `Overworld.2.7.0`, `.1`, `.2`: Overgrown Lip, Expedition Terraces,
Drowned Basin. CoO-original composition, not a Qud parity port. Sources:
`Docs/FELLING-WORLD-DESIGN.md` §4.1, `Lore/10_Bible.md`,
`SinkholeSites`, `SinkholeArchetypes`, and the shipped mouth/descent/sima builders.
Ginmere is a coined named drowned sima already mapped in the world. No new
site, sinkhole archetype, hidden passage, climbing mechanic or fall mechanic.
Other sinkholes and depth 3+ retain their existing generation.

## Verification corrections

| Premise | Verified contract |
|---|---|
| Water is cosmetic | MirePool has LiquidPool, TileStateSource, thermal/destruction and burn-off gas; preserve native owners. |
| Sima species name | Exact shipped actor is `PrickleBrowGecko`, not SimaPricklebrow. StumpSima table contains that species alone. |
| Reserve the dry bank | Nest defense needs sixteen unreserved open LOS cells at radius 2–5. Keep real candidate space and place the nest after ordinary residents. |
| Anchors grant climbing | RopeAnchor currently only supplies examinable scenery. |
| The mouth clears interior floor | **Visual sweep correction:** `FormationReachability.ClearFor` removes only Solid-tagged/Physics-solid entities; Grass survives. The composition supplies native SandstoneFloor inside the rim and Grass outside. This is an explicitly walkable bare-stone landing; no missing floor, falling, or invisible blocker is implied. |
| Independent decoration | All substantive props are native owners. Stair registry and reciprocal destinations remain authoritative. |
| Lower-first load lacks an incoming stair | **Hypothesis falsified.** `ZoneManager.GetZone` calls `PrepareZoneForAccess`; the Overworld override already generates uncached sinkhole parents recursively before depths 1/2. The stair builder alone was an incomplete source boundary. No new parent-order fix is needed. |

## Architecture and integration contract

Pure `GinmereCompositionPlan` selects deterministic broad masses, vegetation
patches, connected dry approaches, shelves, cache space and joined basin.
`GinmereCompositionBuilder` realizes only a fresh eligible zone and preflights
all required blueprints before mutation. Priority 1000 replaces generic
solid-earth/strata for the underground slice. The existing mouth builder follows
the surface composition and retains native lip and stair-connection ownership. Underground
stairs and connector remain normal native builders. Descent cache and sima water
are composed once, replacing their old decorative realization rather than
layering duplicate instances. Existing hazard/population/container passes remain.
A final nest wrapper checks real defender capacity after those residents.

## Shipped composition and mechanical bounds

- **Overgrown Lip:** a denser vegetation collar surrounds the known native lip.
  Its inner ellipse is bare SandstoneFloor, retaining the Terrain tag without
  Grass's Plantable tag. The existing eastern stair opening and native lip
  remain authoritative. The enclosed landing is walkable; its visual depression
  is not a new depth, falling system, or impassable shaft.
- **Expedition Terraces:** coherent perimeter cliff masses extend two to four
  cells inward, with three-cell route openings. Four shelves with variable reach, up to
  three cells deep, attach to alternating cliff sides. Three native RopeAnchors
  are examinable scenery. One Sack holds Torch, DriedMeat and HealingTonic;
  adjacent Bones retain their normal native identity.
- **Drowned Basin:** one joined MirePool mass meets the cliff edge, with a broad
  dry eastern bank. Two to four GinFrogs are selected from shuffled dry cells
  within three cells of water, separated and on differing rows. They retain
  normal native movement after generation. The normal StumpSima table supplies
  PrickleBrowGecko residents independently of those frogs.
- **Native water:** placed owners seed their four-turn water coating only after
  the full staged batch commits. A surviving pool renews the lease; a removed
  or destroyed pool stops renewing it, and its water coating dries. No permanent
  synthetic water coating substitutes for the source Part.
- **Communal defense:** the finalizer invokes the existing SimaNestBuilder after
  population and containers. It selects a dry, unreserved nest cell with sixteen
  open, unreserved, line-of-sight defender cells at radius two through five.
  A qualifying native entry event enrolls exactly sixteen real geckos in the
  turn manager, once per nest. Insufficient current space rejects activation
  without consuming the nest; a later valid attempt can succeed. Space is checked
  again at activation, so subsequent actor movement can change that outcome.

The tested seeds establish usable nests and connected final static terrain;
they do not prove every possible seed or guarantee that mobile actors will
never temporarily occupy a route. Populated stair checks precede
the separate static-connectivity audit, which removes only transient actors.

## Verification gates

RED before production; deterministic seeds and strict scope counterchecks;
whole-open-ground traversal; native water connectedness and lifetime; one owner
per blueprint/cell; intact stock; dependency failure without partial world edits;
real nest activation, sixteen enrolled defenders, blocked-space countercontrol;
three-level travel and return; full final-pipeline visual recipe coverage.

Performance: plans and preflight run only during generation. No new frame/turn
loops or per-cube scene objects. Presentation uses existing native dirty-cell
reconciliation and shared meshes. Headless generation/render receipts do not
establish live input feel or FPS. See `Docs/PERF-FOUNDATION.md`.

## Implementation log

- 2026-09-15: Read methodology, combined plan and native sources. Authored initial
  `GinmereCompositionTests` missing-type specifications before any production.
  Requested isolated RED run from root; source editor and Unity untouched.
- 2026-09-15: Root confirmed AR03 missing-type RED. Added native plan and builder,
  named arrival reservation and nest-finalizer helpers. Initial preflight checks
  only blueprint names; root review identified missing-Parts fail-soft exposure.
  Added five malformed-Part tests before the corrective staging implementation.
  Added final-stack, actual nest-event scheduling, and lower-first travel tests.
  Arrival/nest helper implementations preceded their dedicated integration
  assertions; the initial RED covered terrain/defender-space intent, not these
  new helper APIs. This narrower TDD gap is recorded rather than retroactively
  classifying helper assertions as pre-implementation RED.
- 2026-09-15: AR04 native-first run: 75 total, 69 passed, six failed,
  zero compiler errors. Five malformed-Part cases confirmed the preflight bug;
  the other failure was outside Ginmere (Overwrit source circulation).
  Replaced presence-only realization with detached entity staging and native
  contract checks, atomic reservation publication, and owned-placement rollback.
  Added thermal/destruction/gas component-removal regression pins after the fix;
  these three are not claimed as observed pre-implementation RED.
- 2026-09-15: AR06 confirmed both direct-lower-load tests already pass through
  `OverworldZoneManager.PrepareZoneForAccess`'s existing parent-first guard.
  Classified as regression pins, not newly fixed defects. Added two water
  source lifetime cases before the proposed first-action lease-seeding fix;
  their RED run was subsequently confirmed in AR09.
- 2026-09-15: AR09 confirmed both first-action water lease assertions RED
  (expected four turns, actual zero). Added native source seeding only after the
  staged owner batch commits; no permanent water lease is invented.
- 2026-09-15: Inspected AR08 mouth/descent/floor previews. Depth scenery reads
  as thin boundary rails and floating shelves, and frogs form a programmed row.
  Authored four geometry/bank tests before refining these generator rules.
  Also corrected the mouth-floor assumption above by reading ClearFor directly.
- 2026-09-15: AR11 captured all six intended refinement failures (145 total,
  139 passed, six failed, zero compiler errors). Replaced thin rails with
  coherent 2–4-cell perimeter cliff masses; four unequal three-cell-deep
  terraces grow inward from alternating cliff sides. Frogs now select shuffled
  dry bank candidates within three cells of native water, with separation and
  differing rows. Mouth interior uses native non-Plantable SandstoneFloor while
  the outer country retains Grass. Required ground blueprints are collected
  over every cell before staging. This remains a walkable bare-stone landing,
  not an implemented abyss. These refinement gates subsequently passed AR16.
- 2026-09-15: AR16 verified the native refinement assertions. AR17 verified
  350/350 combined targeted tests after the coordinating renderer fixes, including
  all 307 new cases and 43 existing manager/sinkhole cases. Final self-review of
  owned native source found no additional material defect within this scope.
  Staged dependency checks, reservation publication, native source lifetime,
  stair authority and nest capacity remain aligned with the implementation.
  Final full-suite comparison and final visual close-out remain separate gates.

Files owned here: new plan/builder/tests and this document. Shared manager,
renderer and art integration belong to the coordinating implementation.

## Final close-out

AR21 targeted:396/396. AR22 full:12,570 total,12,538 pass,32 failures whose
names and messages exactly match AR01; zero new failures and zero C# errors.
All307 new feature cases pass, plus one added surrounding equipment control.
Final AR18 native gallery contains21 views with zero missing meshes and zero
unmodeled visible owners. Main-project art is installed;405 source/art files
match the isolated copy and196 task metadata GUIDs have no collisions. Static
captures do not establish live input/HUD/lighting feel or sustained FPS. See
`OVERWRIT-GINMERE-COMPOSITION-PLAN.md` for the exact mixed-file commit boundary.
