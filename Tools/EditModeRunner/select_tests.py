"""Write tests.props. With file arguments, compiles exactly those test files.
With --all, includes every EditMode test file that compiles without Unity,
dropping failing files round by round (writes all.list / excluded.list)."""
import os, re, subprocess, sys
here = os.path.dirname(os.path.abspath(__file__))
repo = os.path.realpath(os.environ.get('COO_REPO') or os.path.join(here, '..', '..'))
HARNESS = {'Assets/Tests/EditMode/TestSupport/ScenarioTestHarness.cs',
           'Assets/Tests/EditMode/TestSupport/ScenarioContextExtensions.cs'}
# Files whose Unity-free slice is re-declared in Stubs/TestFixtureShims.cs.
SHIMMED = {'Assets/Tests/EditMode/Gameplay/Save/GameAuditHotbarSelectionTests.cs'}

def write(files):
    with open(os.path.join(here, 'tests.props'), 'w') as f:
        f.write('<Project><ItemGroup>\n')
        for rel in files:
            f.write(f'  <Compile Include="$(RepoRoot)/{rel}" />\n')
        f.write('</ItemGroup></Project>\n')

if sys.argv[1:] != ['--all']:
    write([f for f in sys.argv[1:] if f not in SHIMMED]); sys.exit(0)
cur = sorted(os.path.relpath(os.path.join(d, f), repo)
             for d, _, fs in os.walk(os.path.join(repo, 'Assets/Tests/EditMode')) for f in fs if f.endswith('.cs'))
cur = [f for f in cur if f not in HARNESS | SHIMMED]
excluded = set()
for rnd in range(20):
    write(cur)
    out = subprocess.run(['dotnet', 'build', '-nologo', '-v', 'q'], cwd=here, capture_output=True, text=True).stdout
    bad = {m.group(1) for m in re.finditer(r'(Assets/Tests/[^\(]+\.cs)\(\d+,\d+\): error', out)}
    other = sorted({l for l in out.splitlines() if ' error ' in l and 'Assets/Tests/' not in l})
    print(f'round {rnd}: {len(cur)} files, {len(bad)} failing', flush=True)
    if other:
        sys.exit('non-test compile errors:\n' + '\n'.join(other[:10]))
    if not bad: break
    excluded |= bad
    cur = [f for f in cur if f not in bad]
open(os.path.join(here, 'all.list'), 'w').write('\n'.join(cur) + '\n')
open(os.path.join(here, 'excluded.list'), 'w').write('\n'.join(sorted(excluded)) + '\n')
print(f'compiling: {len(cur)}  excluded: {len(excluded)}')
