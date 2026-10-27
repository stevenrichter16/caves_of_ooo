# Earned East caster baseline — approach65

Earliest retained East approach, before the recorded partner fight. Preferred replay start when both defender context and ordinary contact matter.

This is an unchanged ordinary-play Quick save archived by the root operator from the isolated E1 session on 2026-10-27. It continues the earlier `east-shelter-pre-generation65` game. Root reports the existing baseline source revision as `bf38` (shorthand); no new caster motion art or runtime implementation is represented here.

| Field | Verified value |
| --- | --- |
| Game | `50e670c30b6d48feb82756df64bd1cbc` |
| Seed / save version | 65 / 8 |
| Current graph | `Overworld.12.10.0` |
| Player cell | 55,3 |
| Tick / HP / level | 1130 / 40/40 / 1 |
| Save timestamp UTC | `2026-10-27T03:11:58.7746000Z` |
| Quick.json | 411 bytes; SHA256 `d3079f5af3b0e83a672b65cdc597d195c9ad5a01952d0d09e981fa55a66e3e4d` |
| Quick.sav.gz | 119407 bytes; SHA256 `81ec1a19ad5aa0ad9c2d9e245b4fbb31e6c98a8d9543a5298692729b703249f6` |

The ordinary route from the earlier Glade pre-generation save crossed Right into East and used an observed upper lane. This save is already East55,3; do not replay an assumed path without checking the current visible actors.

Related actual screenshot: [baseline image](../../east-caster-before-02-approach.png). The `before-03-reader` filename denotes the visible target/action selection menu, not a complete F1 examine-document capture. See [independent baseline review](../../east-caster-baseline-independent-review.json) for frame and hook evidence.

The binary prefix was read without loading gameplay: it proves the game/seed, active graph and exact player cell, and cached Glade plus East graphs. Tick, HP, level and timestamp come from Quick.json. Entity bodies, carried/equipped stock and AI goals were not independently decoded by this prefix audit. Root's ordinary run used the existing Warden LongSword/Buckler; this README does not invent an inventory census or unused ammunition state. FireMoss is not Ember Spit ammunition.

Load only through the existing isolated ordinary Continue procedure, protecting all normal saves and preferences. Do not overwrite a normal save or edit these bytes. Future AI choices, combat outcomes and image timing may differ. These are Editor baseline fixtures, not package/Deck acceptance or proof of post-implementation motion, restart or full-world completeness. Capture helpers only observed actual frames and callbacks; root reports no event dispatch, grants, timing changes or AI overrides.
