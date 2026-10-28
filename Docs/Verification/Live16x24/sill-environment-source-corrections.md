# Sill environment source/test preflight

Test-only draft, 2026-10-28. No production edit or native execution by this agent.

- Exact existing adapter is `FlatPixelSillEnvelopeCatalog`: default resource loading and five-input injection are separate. New expected overload appends a sixth `Texture2D` after the borrowed selector; old five-input injection must explicitly forward null rather than implicitly loading new art.
- Atlas contract is 144x24: Grass0–3, StoneFloor4–7, intact StoneWall8. Existing coordinate-only VariantAt is unchanged (75% quiet). Grass/floor retain current physical owner and material/TileState priority. StoneWall has only one current frame, so this pass does not create adjacency-aware wall geometry.
- Foreground and portrait retain current selector/disclosure and exact owner checks. Damaged-but-intact structural HP is not pinned; zero/Gone refuses. New art must not change simulation state or borrowed atlas/import settings.
- Existing five-input fallback uses sill-envelope20–23, cellar-floor0–3, opening-cellar8. Their default-only expectations will migrate after root native RED. Explicit injection controls remain old resources. No shared art is edited.
- Added five focused `Environment_*` methods (seven native cases, correcting preliminary estimate of eight) to the existing fixture; no new meta needed. Three default exact-owner cases first assert actual wrong current texture name, before checking resource availability. Four reflected-injection methods are compile-safe while the sixth overload is absent.
- New coordinate sampling for cached nine-frame coverage includes all variants in both floor families: source-only hash calculation gives Grass[66,9,11,10], stone[72,7,8,9]. This is setup verification, not native test evidence.
- Native class: `CavesOfOoo.Tests.FlatPixelSillEnvelopeTests`. Root should use the five full new method names (default method expands three test cases) rather than assuming a prefix filter discovers tests.
- Root owns actual baseline, native RED/GREEN, imagegen/art, Unity and publication. No actual Play or acceptance claim yet.

## Native RED and frozen runtime

Root confirms actual job f7d74807954342288082ec811d0d4fa1: seven completed, all seven intended failures (three old shared textures, four absent sixth overload), before this implementation. This agent has not executed Unity.

Minimum runtime adds only default EnvironmentResourcePath load, an explicit optional sixth injected atlas with old five-input null forwarding, three frame choices and cached-view disposal. No ownership/state/disclosure/selection/VariantAt/reader authorization changes. All four original atlas inputs stay independent and borrowed.

Default Sill Grass/StoneFloor/StoneWall expected resources migrated; NotEnvelopeGround also rejects new Environment, preserving meaningful negative assertions. Root separately authorized exactly one existing SoddenTravel Sill default case at line554 to the new path; its Sodden Fen case and all legacy/injected GrassAtlas controls remain unchanged.

Recommended compact native group: CavesOfOoo.Tests.FlatPixelSillEnvelopeTests; CavesOfOoo.Tests.FlatPixelSoddenTravelTests; CavesOfOoo.Tests.FlatPixelSharedVineWallTests. Covers changed defaults, shared refusal authority, old compatibility and existing material priorities. No cave art/routes changed, so a broad cave rerun is not proposed.

Source self-review: no apparent behavioral mutation, per-query allocation, disposal ownership or fallback inconsistency. Actual art/GPU/material hierarchy remains root acceptance; contact review is not gameplay evidence.

Frozen source SHA256:
- `Assets/Scripts/Presentation/Rendering/FlatPixelSillEnvelopeCatalog.cs`: `bb1948cf48705b26d8243d845f9053fc9559fae94903fa68994d78357ac9eafd`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSillEnvelopeTests.cs`: `49fcbd581c3e3b692b60a54727f3bc5b277df10b98e73ce27cc4e963dd75af1a`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSoddenTravelTests.cs`: `ca2bc3151ca855e71bc5120cf5b24c7bfa0b77d8c131ac9ecc74994de2edbdfe`

Root notes: runtime7RED confirmed before implementation. First attempted GREEN218 ran the old loaded assembly and retained the seven RED failures; not accepted. Root then verified constructor lengths[1,5,6]. Forced refresh readiness timed out after60s; subsequent probe showed not compiling/updating. Second native job reported initialization timeout with0 cases while underlying TestRunner remained active; no overlapping rerun/Play.
