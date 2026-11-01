# Stillleaf descent material pilot

Status: bounded material implementation observed; broader area and R11 gates open, 2026-11-01. V1 material coverage; no whole-area polish,
population, new shadow or packaged acceptance. Baseline source e18eefaf7/runtime0a2cc9bae.

## Observed defect and scope

Actual seed704860946, earned Felling save→world map north/west→Stillleaf2.4.0→
normal auto-walk/descent reaches2.4.1 at52,12,HP40,turn610. Most sandstone ground
and walls remain ASCII. `Verification/GraphicsExecution/Stillleaf/before.png`
and unchanged `before-save/Quick.*` freeze that checkpoint. No FOV/movement injection.

Reuse approved shallow-caves wall0–1 and beating-materials floor12–15 and the
existing ordinary shallow owner views. Straight boundaries, geometry, authored
composition, RNG, lighting and save format stay unchanged. No new raster art is
needed. Existing layered walls/quieter floor satisfy the material direction.

Source correction: Stillleaf uses CreateSanctumPipeline and its authored composition,
not generic CreateSinkholePipeline. The large cross-world sandstone census is a
prioritization signal, **not this pilot's denominator**. Only canonical2.4.1 is in scope.

## Acceptance

- Separate site instance; ordinary wilderness cave admission remains unchanged.
- Current zone-owned manager/cache object, full map arrays, canonical address,
  real SealedLibrary site identity and absence of surface-keyed saved lair claim.
- Preserve persisted-profile story renames and legacy Stillleaf name; the conflicting FoundingVillage
  profile revokes. Unknown profiles follow existing name fallback, matching shared
  generation identity rather than inventing a presentation-only identity policy. Re-evaluate warm authority without generation/ledger creation.
- Existing exact owner, state, disclosure, optional-resource and reader rules remain.
  DescentLedge foreground and deeper floors are excluded; real sandstone beneath
  a ledge remains valid ground. Missing art must expose fallback.
- Native RED then GREEN plus adjacent shallow/site/sinkhole tests; actual matched
  capture, reader, walking, F5/fresh Continue and independent review.
- Source/records in same reviewed main commit. Current packages remain older until batched.

## Performance

Use a separate immutable-view catalog and one fixed-address gate; no per-cell parse,
allocation, mutable admission cache or gameplay dirty hook. Existing renderer handles
cell redraw. Read Docs/PERF-FOUNDATION.md. Baseline/current 60–90s native gameplay
profiles use the same earned checkpoint, three Down/three Up, wait, cast prompt/cancel. Underfoot Examine uses C→period→Return separately.
Keep raw maxima and GC; do not infer hardware/build performance from Editor samples.

## Remaining work

Implementation, native checks, actual after/read/reload and independent review complete within this pilot. Publication is the final delivery step; broader gates below remain open.
Special ledges, full-chunk composition, memories beyond existing fallback, shadow
expansion, other sinkholes/depths and physical Deck remain separate open work.

Source/test corrections: zero discovery was invalid, then authored wall-over-floor
fixture repaired before genuine18-fail RED. Public reader returns glyph fallback
as success, so revocation asserts atlas exclusion rather than false; detached graph
may decline the entire view. DescentLedge is a non-solid PhysicalObject, not Terrain;
its own glyph remains over the real floor. True player-covered underfoot test exposed
a separate reader RED; covered-floor permission is limited to the new site instance.

## Verification progress

Native final128/128 pass (23 Stillleaf,56 shallow,20 special-site,12 sinkhole-floor,
17 sinkhole-stack), zero skipped. Genuine18 initial missing-material failures plus
separate true-underfoot failure retained. The intermediate `green-initial.xml`
filename is historical: that run failed assertion-semantics checks, not a green receipt.
Final green.xml is authoritative. Independent source review found0P1/P2.

Actual matched `after.png` uses unchanged turn610 checkpoint at52,12,HP40; floors
and walls now draw native art. No new shadows, composition changes or fog overrides.
The first baseline profile was47.61s, below60–90s target; preserve it as a limited
observation rather than silently promoting it. Current profile and reader/reload follow.

Actual profile input correction: raw period waits and Return opens the current
ability prompt; Escape cancelled it. Both baseline/current used that same input
sequence, with no cast enacted. Underfoot reader was separately opened correctly
with C→period→Return at51,12 and captured in reader.png.

## Bounded implementation result — 2026-11-01

Matched actual before/after, true player-covered floor reader, explicit2× inspection,
three Down/three Up, surface return and descent, F5turn730 and fresh Continue52,12
HP40 were observed. `zoom-inspection.png` contains Pause and is failed inspection;
`zoom2.png` is the successful closer view. Fresh Continue world crop differs5895
of639200 pixels, so no exact image-equality claim. Source/visual review0P1/P2;
residual ledges and black-backed symbols, modest wall relief, sparse open center and
muted-green object contrast remain named defects. No whole-area acceptance.

Performance observations (same start checkpoint and input sequence, OSXEditor,
M5, target GameView1280×800; raw Screen2321×1285): baseline47.61s/current61.98s.
Renderer p95 .207/.209ms, max54.70/85.65ms; MainThread p95 6.51/13.66ms,
max173.41/160.03ms. GC p95 18219/18397 bytes, max9.55/9.62MB; batches39/44.
The baseline is short and Editor scheduling differs. Typical renderer cost is similar,
but increased maxima and MainThread p95 are unresolved, not a performance pass.
R11 remains open. See raw Profiles/manual-20261101-{214025-520,214947-152}.json.

Preservation:75 foreign tracked files hash-unchanged; ordinary saves/preferences use
isolated launcher restoration, final Play=false/root=null/scene clean. No cleanup.
Current Mac/Linux packages remain source0a2cc9bae and exclude this pilot.
Next compare Marrowstye12.12 masonry shadow/grounding in actual play, then its
whole-chunk composition; do not automatically add occupants to deliberate haul lanes.

Independent final reload review: all5895changed pixels differ by at most1RGB level
inside415,240–666,425 around player illumination. Cause unestablished; no visible
geometry/material/owner discrepancy. No pixel-identical claim.
