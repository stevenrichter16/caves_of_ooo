# Beating native map terrain

The southern Beating region now has continuous approved sand instead of punctuation on black. The actual seed65 WorldMap resolves107 authored Beating coordinates through the existing canonical-map adapter. Spread remains unchanged.

## Requirements and implementation

Reuse `beating-materials` Sand frames0–3; initialize its four views independently of Spread turf. Each resource can be absent/malformed without disabling the other. Dispose owned tile/sprite views only, retain borrowed textures. Current map graph, dimensions, coordinates, exact owned parts/render identity, visibility/exploration and clean cell state remain required. Undiscovered sinkholes are identical to ordinary terrain; disclosed POI glyphs/colors and player foreground survive. No geometry, RNG, save schema, travel, memory or legacy change.

The first.65sand tint reduced southern lair contrast and failed visual review. The final Beating-only.45ground tint resolves the observed P2; Spread stays.65. The adapter selects tint by its owned tile reference, shared by world ground and plain-terrain readers. POI marker colors stay unchanged. There are no marker-specific black boxes or disclosure clues.

## Verification

RED completed56 cases with confirmed missing-Beating-ground failures; full receipt was null and failure list capped. First GREEN retains56passed and2allocation-inconclusive results because the positive allocation control could not be detected. Final tint run reports58completed, succeeded and no failures; its full result is null. These runs overlap and are not additive. Allocation bounds remain uncertified.

Actual GameBootstrap1280×800normal1× matched47,15HP38Dark:41,359changed world pixels/rightUI0. Walk47,16, actual Look47,17 the Beating, descend17.13at40,12, return47,16; final tint travel repeated. F5turn3380 and freshContinue47,16HP38Dark give0/639,200world differences; logs differ. No injected movement or visibility. Actual Examine modal is not claimed. One immediate descend after exiting Look did not register; observing unchangedWorldMap preceded a successful separate input.

The113.128-second first-candidate Editor profile includes Continue, tools, read-only terrain census and travel: rendererp95.08825ms/max505.065375ms; Mainmax17990.425708ms. It does not measure final matched performance or closeR11. Source/tint review found no actionableP1/P2. Final actual visual review accepts regional continuity, village readability and corrected lair contrast.

## Acceptance limits and next action

This closes107native map cells, not whole-map polish. The lair remains subdued versus black; merchant-on-sand contrast, broader accessibility, map scale, other ASCII regions and abrupt biome steps remain unaudited. No population/shadow acceptance. PhysicalLinux/Deck execution andR11 remain open.

Evidence and final Quick checkpoint: `Docs/Verification/GraphicsExecution/WorldMapBeating`, InternalGameID50e670c30b6d48feb82756df64bd1cbc. Load only in isolated native Play. Current61cc packages exclude this and Cinderhold roads until the next batch. Publish, batch both, then improve one documented sparse RuinField19.12 margin with supported Pillar/BrokenColumn owners, cold-generation and navigation controls. Normal saves, foreign files, approved sources and prior builds preserved; no storage deletion.
