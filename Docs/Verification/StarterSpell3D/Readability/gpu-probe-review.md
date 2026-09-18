# Readability GPU probe review

Status: implemented after R02b assertion RED; R03 metadata/helper23/23 GREEN; R04 actual GPU19/19 PASS with45 independently decoded/hashed PNGs and69/69 artifact/source checks. Actual imported spell composition remains a separate pending gate.

- The common positive uses actual visible-alpha 80×25 fog with zero RGB and zero ambient/sun contribution. Changing only emission to zero makes the counter black, so local illumination cannot be confused with visibility.
- The split fixture crosses physical X=40 and leaves half its raster in hidden cells. World-bounds counters move the same fixture/camera beyond each of the four physical bounds. Missing/default/wrong-size masks are actual texture bindings, not caller dimension flags.
- The glow fan interpolates mesh vertex alpha. Its flat-alpha counter has identical positions/triangles; base-color alpha has its own zero control.
- The depth-write check renders an invisible glow in the opaque queue before an opaque blue rear plane. Its counterpart is a real opaque front depth writer. This deliberately makes an erroneous glow depth write observable despite ordinary transparent-after-opaque sorting.
- Owned preview resources are closed/disposed; scene activity/dirty flags, active target and shader compilation mode are checked/restored. It never changes production camera/global lighting or gameplay state.
- The old imported-art probe retains its original 21 face/assembled/color cases. Fresh color references preserve emission; retained Color-type metadata remains a separate diagnostic.

These checks isolate material rendering. They do not establish composed-art quality, movement, occupied-cell correctness, actual game input, or performance. Those remain separate import/native acceptance gates. No new Qud-parity claim is made: the local spell readability materials are CoO presentation work.
