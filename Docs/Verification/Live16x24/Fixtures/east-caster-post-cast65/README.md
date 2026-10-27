# Earned East caster baseline — post-cast65

Earned endpoint after an actual southwest-range2 Lunge hit and actual AI Ember Spit in the same ordinary turn. It preserves a spent cooldown/state endpoint, not a pre-cast control.

This is an unchanged ordinary-play Quick save archived by the root operator from the isolated E1 session on 2026-10-27. It continues the earlier `east-shelter-pre-generation65` game. Root reports the existing baseline source revision as `bf38` (shorthand); no new caster motion art or runtime implementation is represented here.

| Field | Verified value |
| --- | --- |
| Game | `50e670c30b6d48feb82756df64bd1cbc` |
| Seed / save version | 65 / 8 |
| Current graph | `Overworld.12.10.0` |
| Player cell | 69,18 |
| Tick / HP / level | 1630 / 30/40 / 1 |
| Save timestamp UTC | `2026-10-27T03:32:23.1899320Z` |
| Quick.json | 411 bytes; SHA256 `8999945cd8200f33f477323c55bbebf04ffc40c9cccb7b7ca0848c5c7066dc96` |
| Quick.sav.gz | 119536 bytes; SHA256 `3e47f76d097d025959e40aeb7312b3e23f7fdfb72fa73498429c73c46c44bd28` |

Player remains69,18; Lunge targeted caster67,20 without moving the player. Caster4920 had9 HP after3 damage and cast Pyromancy_EmberSpit from67,20 to magpie4884 at65,20. The magpie took3 damage to3 HP and displayed Burning. No future cast or survival is guaranteed.

Related actual screenshot: [baseline image](../../east-caster-before-05-post-cast.png). The `before-03-reader` filename denotes the visible target/action selection menu, not a complete F1 examine-document capture. See [independent baseline review](../../east-caster-baseline-independent-review.json) for frame and hook evidence.

The binary prefix was read without loading gameplay: it proves the game/seed, active graph and exact player cell, and cached Glade plus East graphs. Tick, HP, level and timestamp come from Quick.json. Entity bodies, carried/equipped stock and AI goals were not independently decoded by this prefix audit. Root's ordinary run used the existing Warden LongSword/Buckler; this README does not invent an inventory census or unused ammunition state. FireMoss is not Ember Spit ammunition.

Load only through the existing isolated ordinary Continue procedure, protecting all normal saves and preferences. Do not overwrite a normal save or edit these bytes. Future AI choices, combat outcomes and image timing may differ. These are Editor baseline fixtures, not package/Deck acceptance or proof of post-implementation motion, restart or full-world completeness. Capture helpers only observed actual frames and callbacks; root reports no event dispatch, grants, timing changes or AI overrides.
