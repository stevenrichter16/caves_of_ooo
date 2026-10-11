#!/usr/bin/env python3
"""Assemble explicitly selected, complete audio-polish evidence. Never run a game/build.

No latest-success discovery, raw receipt rewriting, or generated PASS results.
C# parsing below recognizes the current narrow declaration/capture syntax only;
it is not control-flow analysis or proof a historical run used this revision.
"""
import argparse
import datetime
import importlib.util
import json
import os
import pathlib
import re
import shutil
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('audio_polish_package', HERE/'package-audio-polish20.py')
pack = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pack)
require = pack.require
PALETTES = ('original', 'places', 'atmosphere', 'utility', 'regional')
ARTIFACTS = ('spec', 'provenance', 'catalog', 'credits', 'receipt')
LEX = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_]\w*|\d+|==|>=|<=|=>|&&|\|\||[^\s]')


def tokens(source):
    """Keep strings as single tokens so comments/quoted code cannot add call sites."""
    return [t for t in LEX.findall(source) if not t.startswith(('//', '/*'))]


def literal(token):
    require(token.startswith('"') and token.endswith('"'), 'Expected plain C# string literal.')
    try:
        value = json.loads(token)
    except (ValueError, TypeError) as error:
        raise RuntimeError('Unsupported string escape in route contract.') from error
    require(isinstance(value, str) and value, 'Empty route string.')
    return value


def occurrences(ts, sequence):
    return [i for i in range(len(ts)-len(sequence)+1) if ts[i:i+len(sequence)] == sequence]


def close_at(ts, start, left, right):
    require(start < len(ts) and ts[start] == left, 'Missing opening source delimiter.')
    depth = 0
    for i in range(start, len(ts)):
        if ts[i] == left:
            depth += 1
        elif ts[i] == right:
            depth -= 1
            if depth == 0:
                return i
    raise RuntimeError('Unclosed source delimiter.')


def string_array(ts, name):
    starts = occurrences(ts, [name, '='])
    require(len(starts) == 1, 'Missing/duplicate route check declaration: '+name)
    start = starts[0]+2
    if ts[start:start+3] == ['new', '[', ']']:
        start += 3
    end = close_at(ts, start, '{', '}')
    body = ts[start+1:end]
    require(body, 'Empty phase check array.')
    result = []
    for i in range(0, len(body), 2):
        result.append(literal(body[i]))
        require(i+1 == len(body) or body[i+1] == ',', 'Nonliteral check array expression.')
    require(len(result) == len(set(result)), 'Duplicate phase check.')
    return result


def finite_capture_values(ts, at, name):
    """Current finite foreach + ternary name; reject every other dynamic pattern."""
    candidates = []
    for pos in occurrences(ts, ['foreach', '(']):
        end = close_at(ts, pos+1, '(', ')')
        header = ts[pos+2:end]
        if end+1 >= len(ts) or ts[end+1] != '{':
            continue
        body_end = close_at(ts, end+1, '{', '}')
        if not end+1 < at < body_end:
            continue
        # foreach(var biome in new[]{BiomeType.Sodden,BiomeType.Grovelands})
        if not (len(header) == 15 and header[0] == 'var' and header[2:7] == ['in','new','[',']','{']
                and header[8] == '.' and header[10] == ',' and header[12] == '.' and header[14] == '}'):
            continue
        one, two = header[7:10], header[11:14]
        if one == two or one[0] != two[0]:
            continue
        scope = ts[end+2:body_end]
        assigns = occurrences(scope, [name, '='])
        if len(assigns) != 1 or any(scope[max(0,k-1):k] in (['ref'],['out'])
                or scope[k+1:k+3] in (['+','='],['?','='],['+','+'],['-','-'])
                for k in occurrences(scope,[name])):
            continue
        p = assigns[0]
        expr = scope[p+2:p+12]
        # biome == BiomeType.Sodden ? "sodden" : "grovelands" ;
        if (p == 0 or scope[p-1] != 'string' or len(expr) != 10
                or expr[0:2] != [header[1], '=='] or expr[5] != '?'
                or expr[7] != ':' or expr[9] != ';' or expr[2:5] not in (one, two)):
            continue
        require(end+2+p < at, 'Capture name initialized after capture.')
        values = [literal(expr[6]), literal(expr[8])]
        require(values[0] != values[1], 'Finite capture values are not distinct.')
        candidates.append(values if expr[2:5] == one else list(reversed(values)))
    require(len(candidates) == 1, 'Unsupported/unproven finite capture expression: '+name)
    return candidates[0]


def phase_contract(source, iteration):
    ts = tokens(source)
    checks = string_array(ts, f'AudioPolish{iteration:02}Checks')
    captures = []
    for start in occurrences(ts, ['Capture', '(']):
        end = close_at(ts, start+1, '(', ')')
        expression = ts[start+2:end]
        if len(expression) == 1:
            names = [literal(expression[0])]
        elif (len(expression) == 5 and expression[1] == expression[3] == '+'
                and re.fullmatch(r'[A-Za-z_]\w*', expression[2])):
            names = [literal(expression[0])+value+literal(expression[4])
                     for value in finite_capture_values(ts, start, expression[2])]
        else:
            raise RuntimeError('Unsupported Capture expression; review finalized source before assembly.')
        for name in names:
            require(re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_-]*', name), 'Unsafe capture name.')
            captures.append(name+'.png')
    require(captures and len(captures) == len(set(captures)), 'Empty/duplicate source capture names.')
    return checks, captures


def no_alias(path):
    path = pathlib.Path(os.path.abspath(path))
    # macOS exposes these fixed system aliases; reject user-created alias ancestry.
    for exposed, canonical in (('/tmp','/private/tmp'),('/var','/private/var')):
        root = pathlib.Path(exposed)
        if path.is_relative_to(root) and root.is_symlink() and root.resolve() == pathlib.Path(canonical):
            path = pathlib.Path(canonical)/path.relative_to(root)
    require(all(not p.is_symlink() for p in (path, *path.parents)), 'Symlink input/output ancestry: '+str(path))
    return path


def repo_file(repo, value):
    require(isinstance(value, str) and value, 'Missing explicit input path.')
    path = pathlib.Path(value)
    path = no_alias(path if path.is_absolute() else repo/path)
    path = pack.exact_file(path)
    require(path.is_relative_to(repo), 'Input must be in the selected repository: '+str(path))
    return path


def binding(path):
    return dict(path=str(path), sha256=pack.sha(path))


def write_json(path, data):
    path.write_text(json.dumps(data, indent=2, allow_nan=False)+'\n')


def native_row(repo, selected, mode):
    path = repo_file(repo, selected.get('report'))
    raw = pack.read_json(path)
    pack.validate_native(raw, mode)
    shots = raw.get('screenshots')
    require(isinstance(shots, list) and shots, 'Missing raw screenshots.')
    captures = []
    for item in shots:
        require(isinstance(item, str) and pathlib.Path(item).is_absolute(), 'Raw screenshot reference must remain an explicit absolute path.')
        shot = repo_file(repo, item)
        captures.append(dict(name=shot.name, **binding(shot)))
    stamp = datetime.datetime.fromtimestamp(path.stat().st_mtime, datetime.timezone.utc).isoformat()
    return dict(iteration=selected['iteration'], mode=mode, report=str(path), sha256=pack.sha(path),
                runId=raw['runId'], timeBasis='report-file-mtime-estimate', reportModifiedUtc=stamp,
                screenshots=captures)


def assemble(repo, selection_path, output):
    repo = no_alias(repo).resolve()
    require(repo.is_dir(), 'Missing repository.')
    selection_path = pack.exact_file(no_alias(selection_path))
    output = no_alias(output)
    require(not output.exists() and not output.is_relative_to(repo/'Assets')
            and output != repo and not repo.is_relative_to(output), 'Output must be new and outside Assets/repository ancestors.')
    selection_hash = pack.sha(selection_path)
    choice = pack.read_json(selection_path)
    require(choice.get('schemaVersion') == 1 and choice.get('scope') == 'audio-polish-evidence-selection', 'Wrong selection schema.')
    lower = pack.evidence_time(choice.get('notBeforeUtc'))
    phases, history = choice.get('phases'), choice.get('history')
    require(isinstance(phases, list) and [r.get('iteration') for r in phases] == list(range(1,21))
            and all(type(r['iteration']) is int for r in phases), 'Explicit ordered selection of20 phases required.')
    require(isinstance(history, list) and [r.get('iteration') for r in history] == [1,2,3,4]
            and all(type(r['iteration']) is int for r in history), 'Explicit separate selection of4 history reports required.')
    common = repo_file(repo, choice.get('commonSource'))
    dispatcher = repo_file(repo, choice.get('dispatcherSource'))
    native_sources = repo/'Assets/Scripts/Scenarios/Custom'
    require(common == native_sources/'ReferenceGladeNativePlayer.cs'
            and dispatcher == native_sources/'ReferenceGladeNativePlayer.AudioPolish.cs', 'Shared inputs must be canonical route sources.')
    ct, dt = tokens(common.read_text()), tokens(dispatcher.read_text())
    require(len(occurrences(ct, ['Check','(','"ordinary_player_and_native_zone"',','])) == 1
            and len(occurrences(ct, ['Capture','(','"01-gameplay-arrival"',')'])) == 1
            and len(occurrences(ct, ['if','(','_audioPolishIteration','>','0',')','{','yield','return','RunAudioPolishAudit','(',')',';','yield','break',';','}'])) == 1,
            'Shared ordinary boot/capture/audio route handoff changed; review its source.')
    source_files = [common, dispatcher]
    contracts = []
    for n, selected in enumerate(phases, 1):
        source = repo_file(repo, selected.get('source'))
        require(source == native_sources/f'ReferenceGladeNativePlayer.AudioPolish{n:02}.cs', 'Phase input must be its canonical route source.')
        require(source not in source_files, 'Duplicate route source.')
        text = source.read_text();ts = tokens(text)
        checks, captures = phase_contract(text, n)
        case = occurrences(dt, ['case',str(n),':','yield','return'])
        require(len(case) == 1, 'Missing/duplicate dispatcher case: '+str(n))
        start = case[0]+5
        method = dt[start] if start < len(dt) else ''
        require(method.startswith(f'RunAudioPolish{n:02}') and dt[start+1:start+6] == ['(',')',';','break',';']
                and occurrences(ts, ['IEnumerator',method,'('])
                and occurrences(dt, [f'AudioPolish{n:02}Checks']), 'Dispatcher does not bind selected phase source/checks: '+str(n))
        require('ordinary_player_and_native_zone' not in checks and '01-gameplay-arrival.png' not in captures, 'Phase duplicates shared bootstrap evidence.')
        contracts.append(dict(iteration=n, mode=f'audio-polish-{n:02}', checks=['ordinary_player_and_native_zone']+checks,
                              captures=['01-gameplay-arrival.png']+captures))
        source_files.append(source)
    extra = choice.get('additionalSourceFiles', [])
    require(isinstance(extra, list), 'Invalid additional source file list.')
    for value in extra:
        path = repo_file(repo, value)
        require(path not in source_files, 'Duplicate additional source file.')
        source_files.append(path)
    routes = dict(schemaVersion=1, scope='audio-polish-twenty-routes', phases=contracts, sourceFiles=[binding(p) for p in source_files])
    iterations = [native_row(repo, row, f'audio-polish-{n:02}') for n,row in enumerate(phases,1)]
    old = [native_row(repo,row,'native-work-audio' if n==3 else 'world-audio') for n,row in enumerate(history,1)]
    manifest = dict(schemaVersion=1, scope='audio-polish-twenty-iterations', notBeforeUtc=lower.isoformat(), iterations=iterations, history=old)
    history_only = dict(schemaVersion=1, scope='audio-polish-four-report-history', history=old,
                        countingNote='Separate earlier evidence; never substitutes for or adds to the20 new phase counts.')
    palettes = choice.get('palettes')
    require(isinstance(palettes,list) and len(palettes)==5 and {p.get('id') for p in palettes}==set(PALETTES), 'Explicit five palette artifact sets required.')
    assets = dict(schemaVersion=1, scope='audio-polish-final-assets', palettes=[])
    for label in PALETTES:
        selected = next(p for p in palettes if p['id']==label)
        assets['palettes'].append(dict(id=label, **{key:binding(repo_file(repo,selected.get(key))) for key in ARTIFACTS}))
    final = repo_file(repo, choice.get('finalReport'))
    files = {'routes-contract.json':routes, 'assets-manifest.json':assets,
             'iterations-manifest.json':manifest, 'history-manifest.json':history_only}
    with tempfile.TemporaryDirectory(prefix='coo-audio-polish-assembly-') as temp:
        staging = pathlib.Path(temp)
        for name,value in files.items():write_json(staging/name,value)
        runs = pack.validate_iterations(staging/'iterations-manifest.json', final, staging/'routes-contract.json', pack.sha(staging/'routes-contract.json'))
        audio = pack.validate_audio(repo,staging/'assets-manifest.json',pack.sha(staging/'assets-manifest.json'))
        require(pack.sha(selection_path)==selection_hash, 'Selection changed during validation.')
        # Recheck evidence/source artifact hashes and mtimes immediately before publishing.
        pack.validate_iterations(staging/'iterations-manifest.json', final, staging/'routes-contract.json', pack.sha(staging/'routes-contract.json'))
        for palette in audio['palettes']:
            for row in palette['artifacts'].values():require(pack.sha(pathlib.Path(row['source']))==row['sha256'], 'Asset artifact changed during validation.')
            for name,digest in palette['source_asset_sha256'].items():require(pack.sha(repo/'Assets/Resources/Audio/World'/name)==digest, 'Audio changed during validation.')
            for name,digest in palette['importer_sha256'].items():require(pack.sha((repo/'Assets/Resources/Audio/World'/name).with_suffix('.ogg.meta'))==digest, 'Importer changed during validation.')
        result = dict(schemaVersion=1, scope='audio-polish-manifest-assembly', selection=binding(selection_path),
                      outputs={key:dict(name=key,sha256=pack.sha(staging/key)) for key in files}, assets=audio, **runs,
                      source_bound='Final source declarations/capture sites and dispatcher bindings checked. Raw historical reports do not independently embed this source revision. Restricted source parsing is not C# control-flow verification.',
                      publication_bound='Manifest validation only. No build, package, Unity run, visual review, continuous audio capture, subjective listening or physical Steam Deck verification.')
        write_json(staging/'assembly-validation.json',result)
        no_alias(output.parent);output.parent.mkdir(parents=True,exist_ok=True)
        output.mkdir()  # exclusive reservation; never overwrite even an empty existing directory
        try:
            for p in staging.iterdir():shutil.copyfile(p, output/p.name)
        except BaseException:
            shutil.rmtree(output)
            raise
    return result


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo',type=pathlib.Path,required=True)
    parser.add_argument('--selection',type=pathlib.Path,required=True)
    parser.add_argument('--output',type=pathlib.Path,required=True)
    args=parser.parse_args()
    result=assemble(args.repo,args.selection,args.output)
    print(json.dumps(dict(output=str(args.output.resolve()),outputs=result['outputs'],assetCount=result['assets']['asset_count'],
                          phases=len(result['iterations']),separateHistory=len(result['history']),packageExecuted=False),indent=2))

if __name__=='__main__':main()
