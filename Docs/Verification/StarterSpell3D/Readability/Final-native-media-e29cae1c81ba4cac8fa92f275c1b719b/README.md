# Final native spell preview

Primary: `readability-full-gameview-native-1x.mp4` — full1920×1080 native pixels at recorded1× timing. All decoded RGB frames match their source pixels exactly. The seven named full-GameView PNGs (five primary spells plus Flaming Hands and Conjure Rain) are byte-for-byte copies of the original captures.

Secondary: `readability-separate-inspection-native-1x.mp4` — a wider704×528 crop enlarged exactly2× with nearest-neighbor pixels, clearly labeled outside the scene. Consult the receipt for a nondefault crop.

The captures sample about10 frames/sec; holding each recorded image does not invent the motion between samples. Any requested title pauses are separately labeled timeline records, not gameplay duration. Lossless RGB H.264 decode is verified with ffmpeg; other player compatibility is not asserted.

Final acceptance, source hashes, selected timestamps, complete timelines, ffprobe metadata and decoded RGB hash verification are retained in `media-verification.json`.

Optional playback companion: `readability-full-gameview-native-1x-COMPATIBILITY-chroma-compressed.mp4` uses standard H264 High/yuv420p. It preserves full-frame dimensions and recorded variable frame holds without motion interpolation, but RGB-to-YUV conversion, chroma subsampling and lossy compression change colors. The lossless RGB master and original PNGs remain authoritative. Codec-level/ffmpeg verification does not guarantee every app can play it.
