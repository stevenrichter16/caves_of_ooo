#!/bin/bash
# verify_compile.sh — offline compile check of BOTH Unity assemblies.
#
# Unity's generated csprojs go stale the moment new .cs files land (they
# regenerate only on editor focus). This script injects any missing
# on-disk sources into throwaway copies and builds them with dotnet,
# which shares Roslyn + the reference set with Unity's own compile —
# authoritative for missing-type/using/ambiguity errors.
#
# Usage: Scripts/verify_compile.sh   (from repo root; exit 0 = clean)
set -uo pipefail
cd "$(dirname "$0")/.."

python3 - <<'PY'
import re, subprocess, sys

def entries(csproj):
    txt = open(csproj).read()
    return txt, set(m.replace('\\', '/') for m in re.findall(r'<Compile Include="([^"]+\.cs)"', txt))

def disk(root):
    out = subprocess.run(['find', root, '-name', '*.cs'], capture_output=True, text=True)
    return sorted(p for p in out.stdout.splitlines() if p)

def inject(src_csproj, dst_csproj, roots, extra_sub=None):
    txt, have = entries(src_csproj)
    missing = [p for r in roots for p in disk(r) if p not in have]
    block = "".join(f'    <Compile Include="{p.replace("/", chr(92))}" />\n' for p in missing)
    i = txt.find('<Compile Include')
    ls = txt.rfind('\n', 0, i) + 1
    out = txt[:ls] + block + txt[ls:]
    if extra_sub:
        out = out.replace(*extra_sub)
    open(dst_csproj, 'w').write(out)
    return missing

m1 = inject('CavesOfOoo.csproj', 'CavesOfOoo._verify.csproj', ['Assets/Scripts'])
m2 = inject('EditModeTests.csproj', 'EditModeTests._verify.csproj', ['Assets/Tests/EditMode'],
            ('Include="CavesOfOoo.csproj"', 'Include="CavesOfOoo._verify.csproj"'))
print(f"[verify_compile] injected {len(m1)} main / {len(m2)} test sources missing from stale csprojs")
PY

dotnet build EditModeTests._verify.csproj -v quiet -nologo 2>&1 | grep -E "error CS|Build succeeded|Build FAILED|Error\(s\)"
status=${PIPESTATUS[0]}
rm -f CavesOfOoo._verify.csproj EditModeTests._verify.csproj
exit $status
