# S3D28 retained Rain presentation failure

The run remains **failed**. An unchanged complete rerun is justified; the current receipt provides no new native acceptance.

Rain's copied crop target, watering, cooldown, native entry and caster-controller restoration are correct. Its observed peak is zero meshes, versus 15 in S3D24. The capture timestamps jump from **0.103987792 to 0.761869084 seconds**, a **657.88 ms gap** spanning the entire authored visible interval: positive-scale Rain samples 15–59 at 100 fps, with clear at 0.60 seconds. Gesture sampling also drops from 74 samples/61.15° previously to 11 samples/7.94°.

All six earlier showcases pass with maximum capture gaps of about 102–103 ms. The first ordinary keyboard cast passes with library 1→1, prepared pool 384→384 and 21.81 ms maximum observed Main Thread time. The renderer, library, coordinator and ASCII runtime hashes are unchanged from the pre-ground review; canonical Rain art is unchanged.

This supports presentation timing starvation. A GPU-only color/culling fault cannot itself explain a zero CPU `ActiveMeshCount`. The Unity log records MCP resource-discovery reflection on the Editor main-thread queue between the Rain command and exit, but supplies no duration: its causal role is **unproven**. The capture gap likewise does not prove a single engine frame hitch. No profile phase was reached, so CPU/GPU/GC/screenshot attribution is unavailable. PNG metadata hashes were not finalized because the run aborted before `WaitForFiles`.

No clocks or acceptance gates were changed. Retain all failed artifacts and require a fresh run id to pass the complete existing native workload, images, gestures, profiles and cleanup gates. Save isolation, scenes, Game view, preferences and private-root cleanup all succeeded; no unexpected errors were observed within the captured run.
