# Starter magic — actual Unity captures

All images come from the single completed native acceptance run `0d7a74963b314b6f9c9270a00d13846d` (S3D28c). Its report records zero failures, successful cleanup and 81.683 seconds across the four measured Full/Reduced town/south phases. Earlier failed or intermediate runs are not mixed into this package.

- `starter-spells-unity-contact-sheet.png`: seven local spell views plus full GameView context thumbnails.
- `starter-spells-unity-native-timing.mp4`: preferred small, full-color motion preview.
- `starter-spells-unity-native-timing.gif`: inline looping alternative.
- `town-full-gameview.png` and `south-full-gameview.png`: byte-identical copies of the original full captures.
- `media-verification.json`: original screenshot hashes, exact crop bounds, each shot's timestamps, output hashes and measured encoder timing error.
- `compose_native_spell_preview.py`: reproducible composer; run with the native report path and a **new** output directory. Requires Pillow, ffmpeg, ffprobe and macOS Arial.

The player and seven successful outcomes were exercised through the actual command path in the real Main scene, using disposable deterministic recipients and one crop. The separate acceptance report also verifies an ordinary keyboard hotbar cast. These showcase clips are explicitly command fixtures, not an uninterrupted ordinary play session.

The seven local scene crops repeat original pixels exactly at 2× size. Colors, geometry and motion have not been retouched or replaced with Blender renders. Full GameView context thumbnails are reduced to fit the sheet; the two full-size PNGs remain unchanged.

The loop preserves each screenshot's measured interval and starts at the first available sample of each cast. It contains 8.364499 seconds of captured playback, plus seven clearly labelled 0.6-second title pauses. Capture sampling is approximately 10 frames per second; no motion is interpolated. The MP4's maximum timestamp difference is 0.518 milliseconds; GIF timing is limited by its 10-millisecond format and differs by at most 4.641 milliseconds. This sampled preview cannot establish how smooth the uncaptured frames felt in the Editor.

The final visual review accepts all seven spell silhouettes and fixed actor roots. Ice and water retain their lighter colors after the native color conversion fix. The compact ground/status markers leave the carved Surge stitches, Jet folds and lower Rime clamps visible; no prior-case ground marks remain in later showcases. Calm keeps the recipient's head clear. Rain is a subtle, localized crop effect at the normal game scale. Natural fog boundaries remain visible in the full GameView context.

No Unity assets, Blender sources, user scenes or preferences were changed to compose these review files.
