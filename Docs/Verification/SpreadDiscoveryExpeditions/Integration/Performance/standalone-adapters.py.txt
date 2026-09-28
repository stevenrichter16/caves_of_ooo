"""Determinism patch, applied at build time (never to the repo's sources).

.NET randomizes string.GetHashCode() per process; Unity's Mono does not. The
game seeds zone RNG with `WorldSeed ^ zoneID.GetHashCode()`, so without this
every run would generate different zones. Each listed file is copied into
Patched/ with the call routed through CooRun.StableHash, and patched.props
swaps the copy in for the original. Values are deterministic but NOT
guaranteed equal to Unity's, so a seed-specific outcome may differ from the
editor; before/after comparisons inside this runner are exact."""
import os, sys
here = os.path.dirname(os.path.abspath(__file__))
repo = os.path.realpath(os.environ.get('COO_REPO') or os.path.join(here, '..', '..'))
FILES = {
    'Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs': [('zoneID.GetHashCode()', 'CooRun.StableHash.Of(zoneID)')],
    'Assets/Scripts/Gameplay/World/Map/ZoneManager.cs': [('zoneID.GetHashCode()', 'CooRun.StableHash.Of(zoneID)')],
    'Assets/Scripts/Gameplay/Economy/TraderRestockSystem.cs': [('e.ID.GetHashCode()', 'CooRun.StableHash.Of(e.ID)')],
    'Assets/Scripts/Gameplay/Materials/GasPoisonPart.cs': [('gasType?.GetHashCode() ?? 0', 'CooRun.StableHash.Of(gasType)')],
    'Assets/Scripts/Gameplay/Settlements/SettlementSiteVisuals.cs': [('(entity.ID ?? "").GetHashCode()', 'CooRun.StableHash.Of(entity.ID ?? "")')],
}
out_dir = os.path.join(here, 'Patched')
os.makedirs(out_dir, exist_ok=True)
lines = ['<Project><ItemGroup>']
for rel, subs in FILES.items():
    src = open(os.path.join(repo, rel)).read()
    for a, b in subs:
        if a not in src:
            sys.exit(f'patch.py: "{a}" no longer in {rel}; update FILES')
        src = src.replace(a, b)
    open(os.path.join(out_dir, os.path.basename(rel)), 'w').write(src)
    lines.append(f'  <Compile Remove="$(RepoRoot)/{rel}" />')
lines += ['  <Compile Include="Patched/*.cs" />', '</ItemGroup></Project>']
open(os.path.join(here, 'patched.props'), 'w').write('\n'.join(lines) + '\n')
