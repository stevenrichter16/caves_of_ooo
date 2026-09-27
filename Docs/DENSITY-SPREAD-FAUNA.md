# C15: original rigged lair fauna

Status: reviewed25-path candidate published; native import and acceptance pending. Actual native baseline has14 intended missing-art failures and18 passing controls in SpreadCreatureArtTests32. This bounded slice covers GiantSpider, JungleApe, the closed/awake MimicChest, and the visibly dropped Glowmaw. It does not close the entire107-creature roster.

## Verified plan and source corrections

All four species are existing authored gameplay content. Their new forms preserve existing blueprints, faction, AI, attacks, loot and save identities. This is original presentation work, with no external game parity claim.

GiantSpider has eight distinct low bent legs and separate mouthparts. JungleApe has broad hunched shoulders, long knuckle arms, short hind feet and no tail. The dormant mimic remains an ordinary closed chest; only its real unfinished DormantGoal selects that state. Awake geometry has the hinged jaw, teeth and tongue. Glowmaw’s new body requires both its actual HasDropped flag and current visibility; its native lure, light and ambush remain unchanged.

The verified runtime methods are AIAmbushPart.Initialize (pushes the native dormant goal before first turn), DormantGoal.Finished (reads only the wake flag), and GlowmawAmbushPart.HasDropped/Visible. Selection checks the actual current managed Spread zone, owner anchor and part backlinks, canonical glyphs, nonportable creature state and all existing named native refusals. It consumes no RNG and changes no goals or gameplay state.

The source exporter originally described Glowmaw generically; the final verified rig family is `grasping` with four named Arm bones. The exact native clip contract is Idle, Walk, Interact, Attack and Hit, not invented Hurt/Die tracks. The exported five models use the existing24-color glade palette, actual bone weights and five real clips. A viewed Blender source gallery distinguishes the five forms; it is not native image or animation acceptance.

## Bounded adoption and evidence

The exact published candidate is recorded in `Verification/DensityCompletion/SpreadBiome/Fauna/publication.json`:25 explicit paths,12 under Assets and13 source/export files. The six existing renderer hooks preserve published equipment, scenery and shadow changes; the presenter was rebased against its exact current preimage. All source and destination hashes matched before publication. Independent source tests recorded6 RED then6 GREEN, and the incoming review reran6/6. Runtime/editor and ownership-fixture reference compiles are clear; they do not establish Unity behavior.

The importer preflights exact IDs, blueprint/rig/bone/clip contracts, source hashes and paths, the24 actual native palette pixels, and existing output asset types. It writes only owned FBX/controllers/prefabs/library paths and uses a disposable preview scene. It is a scoped importer, not a transactional rollback system: malformed deep controller state or a later import failure can leave only owned outputs partly updated. The receipt must list those outputs before a bounded retry.

An additional two-case native validation probe is ready for after valid import: an intact cloned prefab must validate; a matching-name bone borrowed from a separate hierarchy must refuse. Current validation checks names but not transform membership, so that hypothesis must be executed before any ownership-guard repair and before final commit. No speculative guard change has been made.

## Remaining acceptance

Root owns native import, the32 feature cases plus two ownership probes, nearby regressions and viewed native forms. Required checks include actual imported clip deformation, current-owner movement/picking/FOV/removal, mimic transition without altering the goal, hidden or undropped Glowmaw refusal, precise mesh/palette coverage, save behavior and live screenshots. Full-biome coverage must continue to report all unimplemented species honestly.

The four-zone census also found two Sill Villager owners without committed views. Source diagnosis: the village builder creates Hallun and Ellun using Villager with deliberate r/b glyphs and exact quest/conversation markers; the generic humanoid allowlist accepts only canonical @ for unmapped Villager content. Exact native owner fields still need a confirming snapshot. Follow-up must recognize validated authored variants, preserving foreign marker and arbitrary-glyph refusal controls; a blanket glyph relaxation is not the proposed fix.

## First native adoption and review

Root imported all five forms into35 owned paths with the scene unchanged. The34 fauna cases passed32: one real validation defect accepted a matching-name bone from a foreign hierarchy; one fixture measured the conservative animated culling bounds (ape1.538) against the intended visible-body height limit (authored bind height1.325). The narrow repair now verifies prefab ancestry, measures actual persistent vertices under the original1.5 limit, and adds an inflated-culling-bounds counter. Both changes are published for native rerun; no anatomy or height limit was loosened.

Root and the incoming owner viewed native Idle and Attack galleries. The five silhouettes are distinct and actual pose changes are visible. Spider leg joints show dark speckled strips: a new private source test identifies16 overlapping coplanar differently colored faces at its eight upper/bend joints (1 RED/6 controls). Increasing only those joint depths by0.02 removes the coplanar faces (7/7 private source GREEN); export/import/viewed verification is pending. This remains an open art defect until the actual native images are checked.

A regenerated native Sill graph confirms Hallun/Ellun as authored Villager variants with the exact r/red or b/cyan appearance, conversation and quest-beacon identities and valid backlinks. Thirteen test-only cases are now published before production: actual authored positives, full-identity mutation controls and an arbitrary named/recolored Villager control. The proposed exception would retain the actual Villager body family, without asserting a newly authored unique role silhouette or changing any quest behavior.

## Corrected native gates

All34 fauna cases pass after the hierarchy repair and correct visible-body measurement. The isolated spider joint source correction was reimported; root viewed the new native Idle and Walk images and confirmed the speckled strips are gone while the original forms and motion remain. The other four source FBXs stayed byte-identical. Current owned hashes are in `Fauna/owned-current-manifest.json`; all earlier publication/native receipts remain unchanged.

All13 Sill appearance cases now pass after the exact authored-variant exception. This represents the two actual Villager quest owners using their existing profession family, preserving native names, glyphs, themed colors, conversations, quest markers and ownership. It is not a claim of two newly authored silhouettes.

The separate actual107-creature receiving-biome gate still records42 missing or unapproved visitor forms. Party travel makes that closure relevant. See `DENSITY-SPREAD-VISITOR-MODELS.md`; the five-form fauna result is bounded and does not close that work.
