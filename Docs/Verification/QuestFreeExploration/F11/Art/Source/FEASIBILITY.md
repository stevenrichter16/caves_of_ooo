# F11 original furrowstalker — art and cover feasibility

Status: private source audit and proposal only. No new model, content, test, Unity run or gameplay change. F9 acceptance remains separate. The user permits new models and environment changes; the original F11 body and cover should serve an observable hunt/escape decision. Root must approve the complete mechanic/source contract and execute meaningful RED before binding or production.

## Audited facts and corrections

- The roadmap proposes one original hunter replacing a hostile allowance and one grazer replacing an ambient allowance, with approach/pursuit/feed **or escape**, not a predetermined winner (`Docs/QUEST-FREE-EXPLORATION-PLAN.md:194`). Existing Beasts are not automatically predator/prey. Mechanics owns the exact scoped relationship, saved state and source budget.
- Current original grazer art is a broad, high barrel body with blunt pale muzzle and reed ridge. I reopened `ArtSource/QuestFreeSpread3D/Export/renders/questfree-forms-three-quarter.png`; this is a source preview, not a new camera witness. Its catalog declares420 triangles, nine bones and five clips. The pipeline already supports rigid weighted block geometry, approved24-swatch palette, generic rig, controller, source hashing and static remains (`QuestFreeSpreadArtBuilder.cs:25`, `:73`, `:137`; `QuestFreeSpreadArtLibrary.cs:15`). Reuse that pipeline, not the grazer geometry under a new name.
- The presenter currently maps actual movement to Walk(.10s), Attack/Hit to resolved action windows and committed interaction to Interact(.6s), with root motion disabled (`SpawnRing3DPresenter.cs:605`, `:619`, `:629`; builder`:79`). Named Stalk/Chase/Feed clips would not play automatically. Start with the five actual states; additional phase-specific routing is a separate tested decision.
- **Post-consumption gesture trap:** `EntityVisualHooks.cs:50` and presenter`:636` require actor and interaction target to remain current physical owners. If feeding removes its corpse, calling EmitInteraction afterward will refuse it. Freeze the feed transaction first; use an explicit committed role transition or narrow signal if needed. Never manufacture Attack just to animate eating, emit before commit, or replay feeding merely because a loaded actor is already fed.
- A quadruped cannot be armed by a NaturalWeapon property alone: actual body attack fallback uses an on-owner MeleeWeapon with BodyNaturalAttack (`CombatSystem.cs:140`). Mechanics has identified this; exact bite, stats, faction and glyph are not frozen by this art plan.
- ReedbackGrazerCorpse is takeable but has no authored Harvestable; its70% corpse chance remains real (`Objects.json:36734`, `:36852`; `CorpsePart.cs:143`, `:192`). Do not promise meat, a guaranteed body, or a free second reward. Bind actual SourceBlueprint/SourceID and existing kill provenance; killed-by-someone-else or removed bodies must not trigger feeding imagery.

## Proposed original form and bounded pack

Provisional identifiers: Furrowstalker / FurrowstalkerCorpse; `spread-furrowstalker` / `spread-furrowstalker-remains`; separate optional two-entry library. Root freezes names with content before tests interpret missing art.

Use a low, long quadruped with a narrow waist, weight-forward shoulders, a flattened wedge muzzle, close-set backward ears and a tapered uneven tail. A small pale throat/cheek break and dark jaw separate it from ground at ordinary camera scale. Keep muted earth/rust/charcoal from the approved palette. Avoid the grazer's broad barrel, reed ridge and pale square muzzle, the marlback's shale armor and hand tools, a stock wolf recolor, luminous eyes or telegraphic overhead symbols.

Proposed first budget: one skinned mesh/material, roughly400–700 triangles and9–12 rigid bones, one static collapsed remains mesh around120–200 triangles. These are design targets, not measured imported results. One logical cell; no collision, root-motion travel, equipment socket or footprint expansion. A jaw bone and flexible head/neck are justified by the bite/feed states; extra tail segments are optional if silhouette does not need them.

| Actual source state | Authored motion / proof |
|---|---|
| Idle | Restrained breathing and watchful head, feet stationary; no implied target knowledge. |
| Real movement | Low deliberate alternating stride; travel remains native transform movement. Do not call every Walk “stalking” or claim faster pursuit without mechanics. |
| Canonical attack | Brief head/jaw snap and shoulder compression, readable inside the existing attack window; no lunge that suggests extra reach. |
| Actual damage | Short recoil through existing Hit hook; no fake damage/death flash. |
| Committed feeding | Head lowers toward the actual body, jaw works; signal/lifecycle must meet the post-consumption correction above. |
| Death | Real native corpse mapping, flattened distinct silhouette; no promised death animation or guaranteed drop. |

## Cover is mechanics, not decoration

`AIHelpers.HasLineOfSight:59–97` checks Cell.IsSolid and sight-obscuring tile state. `Cell.cs:99` uses Solid tags/closed doors, whereas movement additionally uses Physics.Solid(`:128`). Current Hedge has only Physics.Solid (`Objects.json:29208`): it obstructs travel but does **not** hide prey. Tree carries Solid (`Objects.json:5044`). Ordinary Bush/grass cannot be sold as hiding cover.

Smallest honest first layout: an optional local thicket replacing eligible original opaque tree allowances, with two staggered clumps, an open observation edge, and at least two connected escape routes around cover. Existing tree art can establish mechanical feasibility first. If a lower original scrub form improves the field context, use an exact opaque child/approved source budget, dense interlocking foliage whose height visibly warrants occlusion, and retain destruction consequences. Do not globally make Hedge opaque or add invisible blockers. Exact counts/dimensions remain the source designer's tested grammar, not adopted constants here.

Check actual LOS from hunter to prey before and after a lateral escape, eight-direction connectivity including diagonal corner behavior, safe arrivals and a substantial player bypass. Healthy grazer currently greedily steps away only from player/hostility within3 (`SpreadGrazerPart.cs:30`); a visually plausible gap does not prove it can use cover. Mechanics must supply and witness scoped cover-aware flight, lost-sight search and no hidden-target pursuit.

## Completion gates

1. Freeze the two actor/corpse identities, native attack, saved hunt/feed transitions and real cover budget. Source tests must refuse missing/foreign/current-hidden owners, mismatched corpse provenance and stale phase; existing grazer/ordinary actors remain controls.
2. Test original anatomy/palette/buffers and meaningful motion; reviewed source top/oblique views then exact import/hash/inert-component proof. No broad library rebuild. Optional dispatch misses stay cheap; no per-frame world scan, body rebuild or atlas work (`PERF-FOUNDATION.md:10`).
3. Native game-camera images at fixed ordinary zoom: hunter beside grazer; actual strike, head-down feed only when committed; current corpse; prey visibly clearing cover; daylight and a dim existing scene if available. Yield actual frames for animated views—prior same-editor-frame sampling can produce misleading skinned pixels. Pin world-space bone/mesh movement, not local-axis assumptions.
4. Real unscripted outcome pair: an actual kill may create a finite body and feeding aftermath; an actual living grazer escapes via cover and is not followed using hidden coordinates. Keep any isolated transfers or controlled actors explicit. Current source/tests and F9 successes do not prove F11 occurred.

Only after those gates assess the fixed multi-seed choices, save/return and real60–90s movement/dense-scene timing if the presenter changes. No new cue architecture, ecology simulator, broad AI redesign or cosmetic second variant belongs in this first complete slice.
