# Sill material continuity

2026-10-28: memory RED confirmed; bounded implementation in progress. Reuse approved pixels through the current flat renderer; no new art, renderer, tile layers, generation, collision, services or save format. Root owns Greybox integration, native execution, actual gameplay and publication. Static-memory author owns only the Sill catalog, new memory fixture and this initial plan; river author owns the separate exact-source adapter/tests. Existing environment/court work and unrelated changes remain preserved.

The actual `Verification/Live16x24/sill-court-after65-north.png` and seed64 return views expose two adjoining continuity gaps: visible river water remains repeated glyph stripes, while remembered settlement ground/walls revert to dark glyph fallback. This pass improves those material transitions without claiming full-town composition or historical memory. Broad lawns, other missing prop/resident art, moving keepers, population density, full-area polish and packages/Deck stay separate.

## Source corrections before implementation

| Initial premise | Current source and bounded correction |
| --- | --- |
| Ordinary river owners are Water/ShallowWater | Neither blueprint is registered. `RiverChunkBuilder.cs:153` creates exact WaterPuddle with a flow tag; Objects.json:22569 defines its water pool/material/thermal state. Zone.cs:796 projects permanent water below the physical source. |
| Native water art is missing | `FlatPixelOpeningGroundCatalog` already provides 16 bank-mask frames in the approved 21-frame, 336x24 opening-ground atlas. Its regional gate excludes Sill. Fen reuse explicitly excludes physical pools; it cannot simply be enabled here. |
| Remembered art is broken or lost | Greybox disables the Sill adapter for remembered cells. The existing fallback selects conservative static terrain, dims authored glyph/background colors and rejects live-state families. It is current static identity, not a saved last-seen snapshot. |
| Visible Sill material methods can safely accept fog | They read live substrate/coatings, wall condition and disclosed adjacency. Keep these APIs strict; add a separate selected-static-memory route. |
| Existing sprites imply complete Sill art coverage | Actual provisioner remains a smiley and forge remains a glyph. Their coverage is still open; neither is part of this pass. |

## Bounded implementation

Static memory accepts only the exact fallback-selected remembered Grass, StoneFloor, StoneWall or VineWall in the current Sill graph. Reuse existing coordinate floor variants, straight stone frame8 and isolated vine frame0. A remembered wall does not cause a search for underlying grass. Do not query hidden neighbors, coating/cultivation values, wall HP/Gone, heat, current solidity, actors or services. Existing fallback ownership/disclosure/live-family refusal remains authoritative. Apply one memory tint, retain no collision authority, native live portrait or Render-event authorization, and leave unexplored cells blank. This is static identity continuity, not a new last-seen-state cache.

Visible river work separately reuses approved opening-ground art for exact current ordinary WaterPuddle, with canonical uniquely owned source components and current water coating. Preserve covering actor/item bodies, unknown/mixed/changed-source fallback and independent hazard marks. Disclosed neighbors alone determine banks. No coordinates, flow tags or remembered coating may invent water. The river author/root define and pin the exact source/thermal/empty-state refusal before production.

## Verification and performance

Write compiling tests against existing public presentation before production and have root retain the exact native RED result. Memory cases pin four material identities, one tint, isolated vine adjacency, hidden mutable-state independence, selected-owner-only behavior, live-family exclusion, malformed identity, exact graph/fog authority, optional-art fallback and borrowed cache lifetime. Existing Sill blanket memory refusal must be narrowed to the new static contract, while live family and reader guards remain. River tests separately cover source/under-actor semantics, banks, hazards, missing resource and removal/fog recovery.

Reuse cached Sprite/Tile views and current dirty/FOV repaint paths. No per-query collections, resources reloads, neighbor scans for memory, additional simulation events or historical caches. Root must check submitted visible-to-memory-to-visible painting, normal walking to the real river, wet/dry transition and unchanged source interactions, travel and fresh Continue. Judge actual matched normal-zoom views for fog distinction, wall silhouette, bank material, repeated water marks and actor readability; tests do not establish those visual outcomes. Report a bounded real profile without speedup or Deck claims.

## Status

Root confirmed all16 intended memory material assertions failed in native EditMode, start19:02:55; retained XML is `Verification/Live16x24/sill-continuity-memory-native-red.xml`. The MCP job167b77 initialization timeout is not counted as a second run or as passing evidence. Production followed that RED: the Sill catalog now exposes the separate `TryResolveRemembered` entry point using existing cached views. Root's Greybox integration, native GREEN, actual gameplay/visual comparison and publication remain pending.

## Root baseline and implementation log

- Decision/pins: `Verification/Live16x24/sill-continuity-plan.json` records comparison with actual opening/glade travel. Sill river glyph rows and abrupt memory fallback have greater immediate continuity impact than another small sprite packet. Wider Spread floor repetition remains separate.
- Earned seed65 checkpoint183 → thirteen ordinary Down movements → Sill39,20, turn520, HP40; F5 archived as immutable `Fixtures/sill-continuity-before184`. Bank at39,17 and river at39,20 have actual before PNG/JSON. No teleport, reveal or simulation mutation.
- Native RED: memory16/16 at19:02:55–58UTC, river18/18 at19:05:22–25UTC. Exact XML retained. Memory MCP initialization timed out, but authoritative Unity XML shows all16 actual tests; no repeat. Native cleanup briefly overlapped import of the separate test file, producing a cleanup-verification notice; wait for runner idle before subsequent tests, preserve files. This is tooling evidence, not a gameplay result.
- Root adds separately gated lazy river lifetime, lower water independent of selected actor, source-only glyph suppression, existing sole-water-mark handoff, selected visible reader, and static memory override after regular presentation. Existing Sill fog assertion changes only from blanket refusal to dim isolated wall; it still forbids hidden lower-ground and unexplored content.
- CoO-original bounded presentation work. Qud reference: existing CP437 fallback/owner authority is retained; no external Qud parity claim. Existing imagegen/native sources are reused unchanged; no new concept or pixel extraction is claimed.
- Remaining gates: native GREEN and overlap regression, normal scene comparison/river crossing/reader/travel/Continue, real profile, independent review, preservation and remote publication. Full-area acceptance stays open.

### Actual-first correction: initialized water color

First combined native run passed202/202, but actual Continue184 still showed river glyph rows. Read-only current pool inspection: owner7174, WaterPuddle,volume120, owned canonical parts,temperature21.208147, glyph`~`, color`&c`. `LiquidPoolPart.Initialize → ApplyDefinitionRender` replaces blueprint`&B` with `Content/Data/LiquidDefinitions/water.json` color`&c` during real bootstrap/load. The factory-only test scope retained`&B`. Add the real definition-backed `&c` positive and custom`&R` refusal RED before admitting both exact canonical lifecycle colors. Preserve the failed actual screenshot/profile as diagnostic evidence; do not claim river acceptance from202green. Memory did visibly retain shaded walls.

Verification correction: the first definition-backed test used Newtonsoft, which EditModeTests does not reference. Corrected to the existing `JsonUtility` + `LiquidDefinitionCollection`; do not add a dependency. One method-filter invocation discovered0 tests and a follow-up ran the old18-case assembly (allfailed after Play had initialized the liquid registry). Neither is accepted as the new fixture result. Require method reflection/updated assembly and19-case native RED before the color correction.

Confirmed updated native19-case RED19:16:01–05UTC:18passed, only `InitializedOrdinaryWaterDefinitionColorRetainsNativeWorldAndReader` failed at native texture assertion. Exact XML retained. Production now admits the two canonical blueprint/initialized water colors; all other guards unchanged.

## Final bounded result and self-review

- 🟢 Final native203passed0failed: memory16, river19 and overlapping Sill/fallback/opening-ground fixtures. Final tested hashes and native XML are retained.
- 🟢 Actual seed65 checkpoint184 (turn520,39,20,HP40) → pool reader/real wet exposure → dry40,24 → court39,7 around an ordinarily roaming merchant → glade11.10 → Sill39,11 → freshContinue185 turn850HP40. New sources/rendering do not alter the save format or generation.
- 🟢 Independent source/actual review0P1/P2; source-corrected `&c` runtime color before acceptance. New actual images are named `sill-continuity-*`; failed first river image remains explicitly diagnostic.
- 🧪 FreshContinue world difference1892/564000 pixels; three full-cell patches near left well markers plus small foreground patches. Local visibility/overlay repaint is plausible, unproven. No exact-restart claim. Broad material treatment persists visually.
- 🧪 Real Editor profile90.804s,54244frames with32768retained wrapped samples. Renderer max55.142ms,p99.201ms;Main max2846.632ms;48maxbatches. Includes UI/tool/idle. No speedup, package or Deck inference.
- ⚪ Uniform water/hard edges, sparse lawns, glyph services and isolated remembered vine knots remain. Four static material identities and one visible pool body are bounded coverage; no whole-area polish acceptance or population increase.
- Preservation:49foreign files and1000normal save entries unchanged; current/prior builds retained; ten whitespace-only Unity metadata rewrites backed up and restored. Initial Sill environment meta pin captured normalized whitespace, so final source metadata differs bytewise but no import settings changed. Storage inspection only,0bytesdeleted.
- Next direct-user scope: [light/contact-shadow pilot](GRAPHICS-LIGHT-SHADOW-PILOT.md), small and reversible, actual before/after first. Recurring prompt updated; no shadow implementation is claimed by this continuity commit.

Files: Greybox routing/lifetime; SillEnvelope selected-static-memory API; new SillRiver adapter;16memory/19river tests and narrowed old fog assertion; plan, coverage/polish records, checkpoints and verification evidence. Reproduce from immutable before184 in owned isolated native launcher, ordinary C Continue at1280×800, then the route above; never use normal save roots or mutate game state to stage the view.
