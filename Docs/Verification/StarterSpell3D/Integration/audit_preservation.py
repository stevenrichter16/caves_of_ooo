"""Compare integration work against the captured pre-existing working tree."""
import gzip
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent
ALLOWED = {
    'Assets/Scripts/Gameplay/Effects/SpellFxSequence.cs',
    'Assets/Scripts/Gameplay/Skills/Galvanism_GroundSurge.cs',
    'Assets/Scripts/Gameplay/Skills/Hydromancy_ConjureRain.cs',
    'Assets/Scripts/Gameplay/Skills/Hydromancy_JetBlast.cs',
    'Assets/Scripts/Gameplay/Skills/ProjectileSpellSkillBase.cs',
    'Assets/Scripts/Gameplay/Skills/Pyromancy_FlamingHands.cs',
    'Assets/Scripts/Gameplay/World/Map/TileReactionSystem.cs',
    'Assets/Scripts/Presentation/Rendering/AsciiFxRenderer.cs',
    'Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs',
    'Assets/Scripts/Presentation/Rendering/Village3DPresenter.cs',
    'Assets/Scripts/Presentation/Rendering/WorldFxCoordinator.cs',
    'Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs',
}
with gzip.open(OUT / 'integration-source-before.json.gz', 'rt') as source:
    baseline = json.load(source)
changed, missing = [], []
for name, before in baseline.items():
    path = ROOT / name
    if not path.is_file():
        missing.append(name)
    elif hashlib.sha256(path.read_bytes()).hexdigest() != before['sha256']:
        changed.append(name)
guids = {}
metas = sorted((ROOT / 'Assets').rglob('*.meta'))
for path in metas:
    for guid in re.findall(r'^guid: ([0-9a-fA-F]{32})\s*$', path.read_text(), re.M):
        guids.setdefault(guid.lower(), []).append(str(path.relative_to(ROOT)))
collisions = {guid: paths for guid, paths in guids.items() if len(paths) > 1}
unexpected = sorted(set(changed) - ALLOWED)
report = {
    'status': 'FAIL' if missing or unexpected or collisions else 'PASS',
    'baselineFiles': len(baseline),
    'changedAuthorizedPreexisting': sorted(set(changed) & ALLOWED),
    'unexpectedChanged': unexpected,
    'missing': sorted(missing),
    'metaFiles': len(metas),
    'guidCollisions': collisions,
}
(OUT / 'source-preservation-and-guids.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report, indent=2))
raise SystemExit(0 if report['status'] == 'PASS' else 1)
