# Independent working-court rendering review

Read-only review of the new FlatPixelWorkingCourtCatalog, Greybox integration diff, FlatPixelWorkingCourtTests and GRAPHICS-G2-WORKING-COURT design. Related admission, fallback selection, lower-ground priority and optional-art ownership source were inspected. No Unity, code changes, cleanup or unfinished generation-file review.

## Findings

**No actionable P1/P2 runtime finding in this scoped source review.** This is not native compilation, gameplay acceptance or a performance measurement.

- Authority: exact kitchen address plus existing IsAdmitted verifies the current manager graph. TryResolve reads the canonical requested cell, checks parent, explored and visible before tile state or owner scans. Each floor/grass owner must still be indexed into that cell, physically in the graph, uncarried and unequipped. Visible owned Render and owned Physics are required. The floor further requires both court markers, plain nonsolid/nontakeable terrain, expected glyph variants, no custom visual, no vegetation/soil, and exactly one Render/Physics part. Ambiguous valid floors refuse. No neighbor reads or remembered live-state art were added.
- Ground/foreground: the lower current StoneFloor drives opaque background independently of the fallback-selected foreground. Player, item and furniture remain selected; only an actual selected floor loses its redundant dot. Cultivated soil, any active tile-state record, crop/field rows, liquids/gas and road/floor competitors cause this adapter to refuse and leave existing presentation. Examine returns the same floor sprite only when the validated returned floor is the requested subject.
- Lifetime/missing asset: constructor rejects missing or wrong-sized texture; injected parents never implicitly load Resources. Default optional adapter is cached once, including an unavailable one. Tiles and sprites are cached, owned and destroyed by idempotent disposal; borrowed atlas is neither mutated nor destroyed. No evident runtime leak found in source.
- Cost/scope: no listener, world scan, time advancement, random call or per-cell allocation was introduced. Resolution scans only local cell owners and candidate floor parts, using already-existing graph admission. This establishes source-level bounded work, not an FPS/GC measurement. Generation, rollback and gameplay connectivity are separate pending review.

## Bounded validation gaps

1. **Valid-resource disposal is not yet tested.** `MissingAtlasFallsBackAndDisposeKeepsBorrowedTexture` injects null, so its double Dispose exercises no newly allocated court tiles/sprites. Add a valid-atlas case: capture a returned tile and sprite, call world.Dispose twice, assert the Unity objects compare null and the borrowed texture remains alive. Source disposal looks correct; this is coverage, not evidence of a leak.
2. **Malformed atlas fallback is asserted by code but not a test case.** A 47x24 or48x23 borrowed texture should yield ordinary fallback, remain unmodified/alive and dispose safely. This can be parameterized with the existing missing-asset case.
3. The fixture resolves catalog values directly. It does not claim rendered full-versus-incremental tilemap equivalence, ordinary route feel, final artwork readability or packaged Deck performance. Root's native/play/capture gates remain necessary.

No speculative broad owner-authority rewrite is proposed. Role strings intentionally identify this bounded floor material; this reviewer does not equate them with immutable generation provenance. Current owner/graph checks and the separate generation receipt gates have different responsibilities.

## Reviewed snapshot hashes

- `Assets/Scripts/Presentation/Rendering/FlatPixelWorkingCourtCatalog.cs`: `d4ad1e2511ea4bbe6cffe98b6fbef6f328ad25ad20ba3a764fcce7149c13a9ff`
- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`: `6b8815e309f4518b284324147f762e81783950b5e06e81d6270034359beff4eb`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelWorkingCourtTests.cs`: `81a851b274cc143ebed4854c58e6136a47dae51e21e7393c870957708e758f0e`
- `Docs/GRAPHICS-G2-WORKING-COURT.md`: `f6ea71b9880eaa89f516ba7d52ecf9974260274093892e9ac811fec9a997b381`
