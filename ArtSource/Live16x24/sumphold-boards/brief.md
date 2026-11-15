# Sumphold Duckboard palette comparison

Actual baseline: SumpholdWater/waterfront-after.png, source669a18696. The board strips remain close in value to the yard. Native foreground tint is already white. All16 masks encode joins, so finish changes cannot swap frames.

Built-in imagegen reference used the actual waterfront and approved sodden-travel atlas. Prompt: “Generate a pixel-art material reference comparing existing dull grey-brown boards with slightly warmer, lighter worn honey-grey plank tops on the muted grey-brown yard beside dark teal water. Preserve narrow route footprint and spacing. No glow, orange saturation, extra clutter, rails or new shadows. Palette reference only, not production atlas or actual-game proof.” The generated reference is palette-reference.png; it is not used by Unity.

Independent critique by sumphold_baseline_review: right-hand muted warmer direction suitable; preserve all native pixel positions/alpha/joins; do not copy generated larger planks, bevels or nails. Keep seams/grain/darkest colors; boards must remain duller and less yellow than sandstone walls. Accept only after actual1× long-run/junction comparison.

Native candidate: unchanged16 original frame cell arrays and12 named part groups. Only board face726C59→887A61 and worn88806B→A08C6E change. Dark322E25/504B3B and grain635D4B remain exact. Separate optional Sumphold-only atlas; shared source/atlas protected by hashes. Missing resource must retain original boards. Full game/reader/saved authority acceptance pending.
