# Causeway upper acid and dry brush

Scope: ordinary current Sodden surfaces, actual baseline Overworld.19.7.0. Fix two black-backed acid waves at27,5/6 and two brush glyphs at36,4/5. The previously corrected lower MirePool53,19 is excluded. This is material coverage, not additional population, shadows or whole-area acceptance.

## Design and source corrections

Reuse shipped `Sprites/Environment/acid_pond` without editing pixels: a centered16×16 opaque acid surface inside the native16×24 cell, over its actual canonical Grass. Provenance is GRAPHICS-PASS14. That original AcidPond owner has different semantics: this adapter admits only AcidPool with owned Acid material, positive acid stock, neutral finite temperature, a sole permanent acid coating and current disclosed placement. No alias between the two owners. Empty stock, freezing, boiling, mixtures or unsupported state retain fallback. Positive contact coats creatures without consuming stock; volume60 is a baseline, not an eligibility constant.

DryBrush reuses north-vegetation frame3 and the existing Beating eligibility predicate. No new art generation or mechanics. Art reuse follows the professional plan exception; original textures and imports remain unchanged.

Actual acid baseline has Render~ &G layer1, LiquidPool acid60, temperature20.3267422, permanent acid coating with no projection owner. DryBrush has DryPlant,4/4HP,25°C. The blue wave was the generic coating mark, not blue acid artwork. Native acid representation must reach both ordinary and optimized paint paths before relinquishing that exact mark. Other heat/cold/charge/cloud/residue marks remain independent.

## Gates and evidence

Baseline: `Verification/GraphicsExecution/CausewayUpper/before.png` and `before-observation.json`, earned CoveredMire/AfterCheckpoint, GameID50e670c30b6d48feb82756df64bd1cbc,40,12HP24/40Height,1280×800. No gameplay injection.

Native RED:35cases,10genuine failures/25passes. Initial zero-discovery run was rejected; refreshing assets compiled the new tests. First runtime candidate247:238passed,6failed,3allocationinconclusive. All six failures identify missing RepresentedAcid forwarding in optimized painting; corrected before acceptance. These receipts are nonadditive.

Current status: implementation/testing in progress. Actual after, independent review, performance and publication pending. Required cases include source/receiver disclosure, exact readers, actor/item covers, missing/malformed groups, depleted/unsupported liquid state, independent marks, incremental removal/depletion, disposal, current graph and adjacent mire regression. No whole-area or hardware claim.

Performance: cached immutable tiles/sprites, existing renderer layers and current-owner selection. No new Update, RNG, mutable game cache or per-frame collection. Extra bounded local checks; actual profile still required. Global R11 spike attribution remains open.

## Actual follow-through — 2026-11-02

Final native250:247passed,0failed,3allocationinconclusive. Candidate2 optional-group tests dereferenced tile state after owner removal correctly removed it; corrected test assumption. Depletion now checks incremental and full repaint equality. Source review found no additionalP1/P2; optimized forwarding was corrected and old method signatures retained. DryBrush backing allowance is restricted to ordinary Sodden, not shared special-site admission.

Matched actual before/after:2,314world pixels changed, rightUI unchanged. Remote DryBrush36,5 reader shows native DryPlant4/4; AcidPool27,5 reader shows native acid and real3acid-damage/turn disclosure. Ordinary Right/Left, map49,10, return40,12, F5turn2830HP24, fresh Continue preserves all4owners and states. Reload differs by210world pixels at885..917,192..243; cause unproven. No exact scene equality claim.

First approach was a separate failed trial:90ms inputs missed,200ms repeats overshot north, and native sari-snake combat killed the player. Retained failed screenshot/checkpoint; no reset within that trial, no approach/contact/balance acceptance. The successful reader/travel trial freshly loaded the original earned checkpoint. Default-duration discrete keys produced exact Look navigation. A read-only cursor probe after leaving Look hit a null field; no gameplay event injection.

After-only144.436s profile includes failed approach/combat and tool scheduling; rendererp95.094292ms,max289.894ms,Mainmax17,444.629ms. R11 remains open; no speedup or packaged-performance claim. Capture resolution1280×800 and profiler-reported Editor target2321×1285 are kept distinct.

Root ended Playfalse,isolatedrootnull,sceneclean. No storage deletion. Actual hourly report21:07:11UTC; next22:07:11UTC. Native/evidence counts are nonadditive; current8dbecMac/Linux packages exclude this slice. Independent visual review/publication remain pending.

## Independent disposition

Source and actual visual review found no remaining newP1/P2 for this bounded material slice. Both portraits and real map/reload readability accepted. Acid's rectangular green speckling can resemble moss; stronger liquid edge/hazard prominence is named later polish debt. All4owner rectangles changed384pixels each; another778worldpixels changed outside them, cause unproven. Do not attribute all2,314pixels solely to these four objects. Saved/reload creature coloration differs within the retained210pixel bounds. No whole-scene equality claim.

Publication-ready scope: actual ordinary AcidPool/DryBrush presentation and targeted guards. Remaining empty/frozen/mixed states, whole-area polish, population, broader shadows, globalR11 and hardware gates stay open. Current8dbecpackages remain preserved; batch this slice with subsequent accepted work.
