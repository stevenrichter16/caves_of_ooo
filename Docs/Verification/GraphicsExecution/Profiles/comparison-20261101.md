# Felling controlled performance comparison

Status: bounded Editor comparison recorded; R11 and packaged performance remain open.

Control uses the two renderer catalogs from27445b20d before Felling admission. Current uses exact0a2cc9bae bytes. Other code/art/recorder remain constant. Current files were backed up under `/private/tmp/coo-felling-profile-control-0a2cc9bae`; the guarded restore script restored exact current hashes, independently verified before building. No foreign files were reset.

Both initial intervals started from the unchanged earned after-save: seed704860946,Overworld.3.5.0,turn420,40,13HP40, same GameBootstrap and camera. Each used six native Up/six Down inputs and C/period/Return/Escape for underfoot Examine, ending40,13HP40. No execute_code or screenshot calls occurred within the intervals. Actual before PNGs are1280×800. Editor-context Screen reported2321×1285 for both; read-only GameView.targetRenderSize returned1280×800. Preserve both values: historical Screen labels do not prove the rendered Game-view size.

| Interval | Duration | Renderer p95/p99/max ms | Main Thread p95/max ms | GC p95/max bytes |
|---|---:|---|---|---|
| Control, manual-20261101-211609-562.json |83.68s|0.195/0.257/63.055|5.494/159.962|18169/9521290|
| Current, manual-20261101-212135-928.json |61.55s|0.194/0.271/620.472|5.753/2257.436|427761/1189072546|
| Settled current, manual-20261101-212249-085.json |65.25s|0.183/0.226/59.301|5.733/518.724|18357/9521290|

The initial current interval had a large unexplained Editor-wide allocation/stall burst. It is retained, not treated as a clean passing run. A single subsequent observation was justified by that burst, using the same zone and movement/reader pattern without reloading the initial fixture. Its world turn/actor positions therefore differ; it is a settled follow-up, not a second exact-state control. A CUA focus guard interrupted the first movement attempt; state was refreshed before the full six-up/six-down sequence. No read-only code was injected during any interval.

Steady renderer percentiles are similar; the60ms renderer maxima already exist before Felling admission. This bounds the claim that the new dry-floor mapping caused that common spike, but does not identify all causes or explain the initial620ms event. No clean overall-allocation, GPU-frame-time, packaged-performance or physical Deck claim. The profiler is Editor-wide and Main Thread includes Editor scheduling. Do not spend another repeated broad suite on this unchanged source; pursue package observation and targeted spike traces if the shipped game reproduces a stall.

Evidence: felling-control-before.png, felling-current-before.png, felling-current-profile-end.png and the three raw JSONs. The control alias felling-control-profile.json is byte-identical to its timestamped report. First launch after domain reload again produced a null active zone and was discarded before profiling; Finish/relaunch succeeded. This startup issue is unresolved and recorded, not accepted gameplay.
