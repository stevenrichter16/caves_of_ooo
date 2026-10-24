# G1 — normal, reversible flat world presentation

Status: G1.1 native GREEN and reviewed; isolated catalog slice (2026-10-23). Normal launch,
regional admission, existing gameplay rendering and player preferences are
unchanged. Parent plan: [Graphics completion](GRAPHICS-COMPLETION-PLAN.md).
This is original CoO presentation work informed by the requested Qud-like
readability, not a claim of copied art or engine parity.

## G1.1 — identity-preserving fallback

The first independent slice supplies a reusable, native-cell glyph fallback
without enabling it in the live renderer. Existing `TryResolve` and
`IsAdmitted` continue to mean the exact four-zone authored/greybox contract.
The fallback can resolve an unskinned current world independently, but never
promotes that world into the opening's native-art scope.

### Source sweep corrections before implementation

| Assumption | Verified source and correction |
| --- | --- |
| A generic actor/prop shape preserves identity | `FlatPixelGreyboxCatalog.Classify` groups unlike creatures and objects. The new fallback instead resolves the actual owned `RenderPart` glyph, deterministic `GlyphVariants`, foreground and authored background. |
| The UI font is the world glyph bank | `CP437TilesetGenerator.GetAtlasTexture` returns the world 256×256 bank containing gameplay overrides; `GetUiTile` intentionally substitutes plain text. Copy the world bank, not the UI bank. |
| A rectangular grid stretches a glyph into a native cell | World glyphs are 16×16 at16PPU. Copy pixels unchanged into 16×24 frames, centered with four transparent rows above and below. Background fill needs its own full16×24 solid frame. |
| Every allocated byte glyph has visible ink | Some world atlas slots, including gas shade bytes176–178, are undrawn. Precompute alpha occupancy once; empty non-space slots use the visible question frame. Space alone is deliberately blank. |
| Asking `GetTile` for arbitrary Unicode is harmless | Its miss path regenerates the shared atlas. Normalize unsupported/control glyphs to the canonical `?` before lookup; never repeatedly call that miss path. Space remains a valid authored glyph. |
| Glyph selection always uses `RenderString[0]` | `RenderPart.ResolveGlyph(x,y)` is the existing deterministic variant contract. Use it when variants exist; otherwise use `AsciiWorldRenderPolicy`. Validate the resolved character before indexing the256-frame bank. |
| TileColor/DetailColor define world identity | `AsciiWorldRenderPolicy` and `RenderPart` explicitly make RenderString/ColorString/RenderLayer authoritative. Deprecated Tile/TileColor/DetailColor do not replace them. VisualID/VisualVariant must not cause a fallback to impersonate an approved native skin. |
| Visible cell membership alone proves ownership | Verify the indexed canonical anchor, current cell identity, parent zone, owned Render, optional Physics/Stacker parts, custody when present and current footprint. A stale old-cell list is insufficient. Use entity references rather than IDs. |
| Missing Physics makes a canonical rendered owner invalid | `RenderCellCore` and the existing Render-only unknown-owner test allow canonical owned Render without Physics. Accept that case; reject foreign Physics and custody when Physics is present. Do not use `WorldResourceActions.Ground` as a universal prerequisite. |
| All zones need Spread admission | Fallback accepts detached valid zones or a manager's exact current cached zone, irrespective of biome/address. It never generates or repairs a zone. Native skin admission remains unchanged. |
| Memory can use the visible top owner | Restrict memory to current low static terrain. Exclude actors/items/portable owners and crop, harvest, repair, finite-liquid, torch/campfire and door state. No current coating, stock, topology or live-color callback is exposed by memory. |
| Static parsed color is the whole live rendering contract | `ZoneRenderer.RenderCellCore` applies a pooled Render event and then lighting. G1.1 provides static identity colors only; G1.2 must reuse that visible color path without double event dispatch and without hidden event calls. DamageFlash decrements frames on Render, so a second callback is materially wrong; reuse the existing main-cell tint only when its selected owner matches the strict fallback owner. No gameplay callbacks are introduced in this slice. |
| A catalog can authorize inventory examine | Exact-subject glyph lookup is semantic artwork only. Existing document/current-presentation authorization remains the caller's responsibility; it must not grant a carried item world occupancy. |

### Proposed bounded API and ownership

- New `FlatPixelFallbackCatalog : IDisposable`, default constructor borrowing
  `CP437TilesetGenerator.GetAtlasTexture()`, plus explicit `Texture2D`
  injection. Null/malformed injection never consults global resources.
- `TryResolve(Zone, int, int, out FlatPixelGreyboxCell)` reuses the existing
  result shape so G1.2 can plug it into the existing environment cell pass.
  No new renderer, world scan or tracked entity cache is introduced.
- `TryGetSubjectSprite(Entity, int, int, out Sprite, out Color)` returns only
  an owned, visible RenderPart's semantic frame. Custody/disclosure remain
  the already-authorized portrait caller's responsibility.
- One owned point-filtered, clamped, no-mip atlas;256 cached glyph tiles/sprites
  plus one full-cell background. Original glyph pixels are copied once with
  native scale. Repeated colors/coordinates do not create textures or frames.
- Minimal separate `FlatPixelGreyboxCatalog.TryResolveFallback` and
  `TryGetFallbackSubjectSprite` forwarding methods lazily own this catalog
  and dispose it with their owner. Existing resolution stays unchanged.
- Empty/no-background cells use neutral black, not a regional palette. The
  highest valid authored background follows the visible owner stack; memory
  only considers its eligible static terrain. Foreground keeps actual glyph
  and color, including terrain identity. Remembered colors dim by0.35 with
  alpha restored to1; remembered collision metadata is always false.

### Test-first sequence

1. Add `FlatPixelFallbackCatalogTests` with reflection/delegate seams so a
   missing implementation gives native assertion RED, not a fixture compiler
   error. Parent owns Unity execution and records the actual result.
2. Cover exact source pixels/padding, default world-bank binding, variants,
   unsupported glyph fallback, authored foreground/background, source bytes,
   cache lifetime and no global fallback on invalid injection.
3. Cover unskinned zone success versus unchanged native rejection; current
   graph/cell/owner, valid Render-only owners, moved/carried/equipped/zero-stack/foreign-part failures;
   legitimate multi-cell footprint; unexplored and low-layer live-state fog
   counters; semantic subject identity without granting world membership.
4. Implement only after parent confirms actual RED. Run targeted GREEN and
   relevant existing catalog/ownership controls; request independent review.
5. Keep the live activation/global projection, water/hazard representation,
   light/status colors, settings/menu, native portrait routing and actual Play
   checks for G1.2 and subsequent G1 delivery. This slice cannot close them.

### Review and evidence

- 🔵 Source sweep completed before production code. Parent owns Unity/editor.
- 🧪 Test receipt pending; no passing native or gameplay result claimed.
- 🧪 Generic fallback is deliberately interim identity coverage, not final
  regional artwork. Source/font pixels are not new authored native assets.
- 🧪 G1.2 must prove water is actually represented before suppressing existing
  water cues, retain strict disclosure at the renderer/portrait boundary, and
  verify graph transitions and fullscreen camera/grid restoration.

Files intended: this living document; new fallback catalog and fixture with
fresh `.meta` files; only the minimal fallback ownership/forwarding seams in
`FlatPixelGreyboxCatalog.cs`. No content, save, preference or scene mutation.

### G1.1 test handoff

The frozen first fixture contains14 methods /40 parameterized cases. It uses
reflection only at setup/binding, then typed delegates for the warmed allocation
counter. Expected RED is the missing fallback class/forwarding methods; no
production stub was added. The parent will record the authoritative native XML.
Independent pixel/cache fixture review found no material source-contract issue.
The borrowed global world/UI atlases are deliberately neither cleared nor
replaced by test cleanup. All owned synthetic textures are disposed.

### Native RED and source correction

The first actual Unity receipt `Verification/Live16x24/g1-fallback-red.xml`
records40/40 failing cases, zero skipped, all missing the intended type or
forwarding API; there were no compiler errors. Before production, parent
review caught an overstrict missing-Physics assertion: canonical runtime
render-only entities are valid in the existing world renderer. Replaced that
negative case with a positive Render-only owner plus owned/foreign-present
Physics counters. The revised fixture has15 methods /40 cases and awaits a
fresh native RED; the first receipt does not prove the corrected assertion.
No production was changed between these test receipts.

### Corrected RED and implementation handoff

`g1-fallback-red2` used a stale loaded test assembly and is not acceptance
evidence for the Render-only correction. After Unity completed its own test
cleanup/reload, the parent verified that the corrected named test was loaded.
The fresh `Verification/Live16x24/g1-fallback-red3.xml` records40 failed, zero
passed and zero skipped, including that corrected case; every failure is the
intended missing type/forwarder. Production began only after that confirmation.

The owned fallback class now copies the readable world bank once into257
cached tile/sprite frames (256 glyphs plus full-cell background). Resolution
uses current reference ownership, optional-Physics custody, valid footprints,
conservative memory, actual coordinate variants and authored colors. Two lazy
forwarders and disposal are the only existing-catalog changes. Normal launch,
existing native/greybox selection and renderer behavior remain untouched.

- 🔵 No new world scan, entity tracker, content mutation or Render callback.
- 🔵 Missing/invalid injected resources do not silently load global art.
- 🧪 Native GREEN, warmed allocation result and cold review are pending.
- 🧪 This code has no live renderer wiring yet; it cannot establish visual
  delivery, lighting/hazard behavior, normal-mode continuity or Deck performance.

### Targeted native GREEN

`Verification/Live16x24/g1-fallback-green.xml` records40 passed, zero failed
and zero skipped. This includes the corrected Render-only case, exact source
pixel/padding comparisons, strict graph/cell/custody/footprint counters, memory
exclusions, semantic carried-subject boundary, resource lifetime and1,000 warmed
resolutions with zero managed bytes allocated on the test thread. It is a
catalog test, not a runtime frame or Deck performance measurement.

The parent is running one bounded regression using
`FlatPixelGreyboxCatalogTests`, `FlatPixelOpeningArtCatalogTests`,
`AsciiWorldRenderPolicyTests`, `CP437UiTileBankTests`, `QudColorParserHdrTests`
and `NativeGroundStateReadabilityTests`. Production and tests remain frozen
during that run. Independent source reviews are pending; no live integration
or normal-launch claim is implied by the targeted GREEN.

### Final G1.1 verification and bounded review

The fresh `Verification/Live16x24/g1-fallback-regression.xml` records90 passed,
zero failed and zero skipped across the six control fixtures listed above.
Together with the disjoint40 focused cases, this is130 distinct native passes.
The controls preserve existing local art/greybox behavior, canonical ASCII
policy, shared UI glyphs, HDR colors and native ground-state readability.

- 🔵 Self-review found no further correctness blocker. There is one local
  occupant pass, no global discovery scan, no live Render event, and no native
  admission change. Renderer initialization is not modified.
- 🔵 Independent ownership/fog/semantic-boundary review found no actionable
  P1/P2 issue. Canonical Render-only entities remain valid; foreign parts,
  custody and stale current-cell/footprint references do not donate artwork.
- 🔵 Independent final pixel/cache review found no actionable P1/P2 issue: source
  rows, native padding, full-cell fill, normalization, variants and independent
  resource disposal match the tests. No callback or per-resolve allocation was
  introduced by the catalog.
- 🧪 G1.1 is complete only as the independently tested catalog layer. The game
  still has its previous launch behavior. G1.2 must wire the shared strict
  resolution into rendering/portraits, preserve event tint and hazards, and
  prove actual travel; G1.3 must deliver normal reversible launch settings.
  No physical Deck, live visual or frame-time improvement is claimed here.

Implementation files: new `FlatPixelFallbackCatalog.cs` and `.meta`; minimal
forwarding/lifetime changes in `FlatPixelGreyboxCatalog.cs`; new
`FlatPixelFallbackCatalogTests.cs` and `.meta`; this living document and native
RED/GREEN receipts. Approved artwork, scene, gameplay, saves and preferences
are unchanged by this slice.
