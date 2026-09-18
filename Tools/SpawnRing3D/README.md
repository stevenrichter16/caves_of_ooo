# Spawn-ring verification tools

Run these from the project root with no other Unity Editor running. All Unity
work must be sequential, with source/assets frozen for each attempt. The wrappers
restart MCP before launching Unity, wait for startup, and inspect compiler errors
before accepting fresh results. They do not make a commit or stage files.

```sh
python3 Tools/SpawnRing3D/verify.py --archive R3D-new-unique-suite.xml.gz
bash Tools/SpawnRing3D/import.sh /Users/steven/caves-of-ooo/ArtSource/SpawnRing3D
python3 Tools/SpawnRing3D/native.py --profile full --actions
```

`verify.py --filter <NUnit filter>` selects a bounded regression. Test XML and
logs archive under `Docs/Verification/GameSystemAudit`; use a new archive name.
`import.sh` runs the strict real-model import audit and writes a unique temporary
receipt directory; archive its output under `Docs/Verification/SpawnRing3D/imports`.

`native.py` launches the actual rendered Editor, creates a private disposable
save scope and records native input, border APIs, save/load, UI, screenshots and
cleanup receipts under `Docs/Verification/SpawnRing3D`. Optional `--actions` adds
actual Harvest and one ordinary bump-combat interaction. Optional `--profile`
accepts `full` or `low` and records sustained native idle/walking segments in all
eight zones with raw profiler CSV. Native AI, health, RNG and fog remain active.
Incomplete/blocked/dead workloads must remain failed attempts.

Independently recompute a completed profile from its retained raw samples:

```sh
python3 Tools/SpawnRing3D/recompute_profile.py \
  Docs/Verification/SpawnRing3D/R3D-<run-id>-profile-full.json
```

The recomputation requires the same run's successful native and final cleanup
receipts. Missing counters are unavailable, not zero. These Editor measurements
do not establish production-build frame rates or a causal full/low speedup.

See `Docs/BLENDER-SPAWN-RING-3D-PLAN.md` for accepted runs and limits;
`ArtSource/SpawnRing3D/README.md` explains editing/rebuilding the assets.
