#!/usr/bin/env python3
"""Package an already-built final audio-polish20 Linux player; never builds or commits.

All final receipts and the exact current HEAD must be supplied explicitly.
This script writes only the NEW audio-polish20 delivery and refuses to replace
an existing archive. Offline asset checks do not establish subjective listening,
Unity resource-archive contents, or physical Steam Deck performance.
"""
import argparse
import datetime
import hashlib
import json
import math
import pathlib
import re
import shutil
import subprocess
import tarfile
import xml.etree.ElementTree as ET


def require(ok, message):
    if not ok:
        raise RuntimeError(message)


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def stream_sha(stream):
    digest = hashlib.sha256()
    while block := stream.read(1024 * 1024):
        digest.update(block)
    return digest.hexdigest()


def read_json(path):
    return json.loads(path.read_text())


LICENSE_SHA256 = {'CC-BY-4.0.txt': '9ba9550ad48438d0836ddab3da480b3b69ffa0aac7b7878b5a0039e7ab429411', 'CC0-1.0.txt': 'a2010f343487d3f7618affe54f789f5487602331c0a8d03f49e9a7c547cf0499', 'kenney_impact-sounds.txt': 'b49aa9c56b04528b95913de13e506a0f7c5e807b9925db9bfef86af1f91120db', 'kenney_interface-sounds.txt': 'f7966c773bbed0eca6a9c75081c44a178b38eae112724dbb5fdfbd4192d118a9', 'kenney_rpg-audio.txt': '5735dfd72cb64cbbceda4ebc00c380c41ca680edb82ff153aa7c9ab97614c539'}

LICENSE_FILES = ('CC-BY-4.0.txt', 'CC0-1.0.txt', 'kenney_rpg-audio.txt',
                 'kenney_impact-sounds.txt', 'kenney_interface-sounds.txt')


def validate_tests(root):
    """Reject optimistic summaries unless all individual NUnit outcomes agree."""
    require(root.tag == 'test-run', 'Expected final native NUnit test-run XML.')
    summary = {key: int(root.get(key, '-1')) for key in ('total', 'passed', 'failed', 'skipped', 'inconclusive')}
    cases = list(root.iter('test-case'))
    require(root.get('result') == 'Passed' and summary['total'] > 0 and summary['passed'] > 0
            and summary['failed'] == 0 and summary['inconclusive'] == 0 and summary['skipped'] >= 0
            and summary['passed'] + summary['skipped'] == summary['total'] == len(cases),
            'Final NUnit receipt has failures or inconsistent/incomplete counts.')
    require(sum(case.get('result') == 'Passed' for case in cases) == summary['passed']
            and sum(case.get('result') == 'Skipped' for case in cases) == summary['skipped'],
            'Individual NUnit outcomes disagree with summary.')
    return summary


def validate_native(native, expected_mode='world-audio'):
    """Native output proves only the explicitly recorded checks for its selected mode."""
    require(native.get('mode') == expected_mode and native.get('complete') is True
            and native.get('errorsFinalized') is True and native.get('failures') == 0
            and native.get('unexpectedErrors') == 0 and not native.get('fatal') and native.get('runId'),
            'Selected native audit is incomplete or contains failures/errors.')
    cases = native.get('audit', [])
    count = native.get('cases')
    require(type(count) is int and count > 0 and isinstance(cases, list) and len(cases) == count
            and all(isinstance(case, str) and case.startswith('PASS ') for case in cases),
            'Native check count or individual PASS records disagree.')
    require(native.get('canVerify') and native.get('cannotVerify'), 'Native report must retain its honesty bounds.')
    return cases


def evidence_time(value):
    """Timezone-aware evidence timestamp, never presented as a native UTC log."""
    require(isinstance(value, str), 'Evidence time must be an ISO timestamp.')
    try:
        result = datetime.datetime.fromisoformat(value.replace('Z', '+00:00'))
    except ValueError as error:
        raise RuntimeError('Invalid evidence timestamp.') from error
    require(result.tzinfo is not None, 'Evidence time requires an explicit timezone.')
    return result


def exact_file(path):
    path=pathlib.Path(path)
    require(path.is_file() and not path.is_symlink(),'Missing or aliased input: '+str(path))
    return path.resolve()


def bound_file(base, row, confined=False):
    require(isinstance(row,dict) and isinstance(row.get('path'),str),'Missing bound artifact path.')
    raw=pathlib.Path(row['path']);path=exact_file(raw if raw.is_absolute() else base/raw)
    if confined:require(path.is_relative_to(base.resolve()),'Artifact escapes source repository.')
    require(re.fullmatch('[0-9a-f]{64}',str(row.get('sha256',''))) and sha(path)==row['sha256'],'Bound artifact SHA mismatch: '+str(path))
    return path


def approved_json(path,digest):
    path=exact_file(path)
    require(sha(path)==digest,'Approved index/contract SHA mismatch.')
    return read_json(path)


def validate_contract(path,digest):
    value=approved_json(path,digest)
    require(value.get('schemaVersion')==1 and value.get('scope')=='audio-polish-twenty-routes','Wrong route contract.')
    phases=value.get('phases');require(isinstance(phases,list) and [r.get('iteration') for r in phases]==list(range(1,21)),'Require all20 ordered phase contracts.')
    for n,row in enumerate(phases,1):
        checks=row.get('checks');captures=row.get('captures')
        require(row.get('mode')==f'audio-polish-{n:02}' and isinstance(checks,list) and len(checks)>1
                and all(isinstance(s,str) and s and not s.startswith('PASS ') for s in checks)
                and len(set(checks))==len(checks) and checks.count('ordinary_player_and_native_zone')==1,'Bad exact phase checks.')
        require(isinstance(captures,list) and captures and len(captures)==len(set(captures))
                and all(isinstance(s,str) and pathlib.PurePosixPath(s).name==s and s.endswith('.png') for s in captures),'Bad exact capture names.')
    source=value.get('sourceFiles');require(isinstance(source,list) and source,'Contract must retain independently hashed source artifacts.')
    for row in source:bound_file(path.parent,row)
    return value


def validate_iterations(manifest_path,final_native_path,contract_path,contract_sha):
    contract=validate_contract(contract_path,contract_sha);manifest=read_json(exact_file(manifest_path))
    require(manifest.get('schemaVersion')==1 and manifest.get('scope')=='audio-polish-twenty-iterations','Wrong twenty-iteration manifest.')
    rows=manifest.get('iterations');history=manifest.get('history')
    require(isinstance(rows,list) and [r.get('iteration') for r in rows]==list(range(1,21)),'Exactly20 ordered iterations required.')
    require(isinstance(history,list) and [r.get('iteration') for r in history]==[1,2,3,4],'Preserve four separate historical reports.')
    lower=evidence_time(manifest.get('notBeforeUtc'))
    require(lower>=datetime.datetime(2026,10,11,tzinfo=datetime.timezone.utc),'Fresh task lower bound predates audio-polish20.')
    previous=lower;paths=set();ids=set();digests=set();results=[];old=[]
    for historical,collection in ((False,rows),(True,history)):
        for n,row in enumerate(collection,1):
            mode=('native-work-audio' if n==3 else 'world-audio') if historical else f'audio-polish-{n:02}'
            require(row.get('mode')==mode,'Incorrect phase mode.')
            path=bound_file(manifest_path.parent,dict(path=row.get('report'),sha256=row.get('sha256')))
            require(path not in paths and row['sha256'] not in digests,'Duplicate native report path or bytes.')
            native=read_json(path);cases=validate_native(native,mode)
            require(all(type(native.get(k)) is int for k in ('cases','failures','unexpectedErrors')),'Native counts must be integer counts.')
            require(len(cases)==len(set(cases)),'Duplicate native check.')
            require(native['runId']==row.get('runId') and native['runId'] not in ids,'Duplicate or mismatched run identity.')
            duration=native.get('seconds');require(type(duration) in (int,float) and math.isfinite(duration) and duration>0,'Invalid raw native duration.')
            require(row.get('timeBasis')=='report-file-mtime-estimate','Evidence timestamps must remain explicit filesystem estimates.')
            end=evidence_time(row.get('reportModifiedUtc'))
            require(abs(end.timestamp()-path.stat().st_mtime)<.01,'Native report mtime estimate mismatch.')
            try:start=end-datetime.timedelta(seconds=duration)
            except OverflowError as err:raise RuntimeError('Invalid evidence duration range.') from err
            if not historical:require(start>=previous,'Stale, out-of-order or overlapping phase estimate.');previous=end
            screenshots=row.get('screenshots');actual=native.get('screenshots')
            require(isinstance(screenshots,list) and screenshots and isinstance(actual,list) and len(actual)==len(screenshots),'Missing capture evidence.')
            shotpaths=[];shotnames=[];packaged=[]
            for shot in screenshots:
                sp=bound_file(manifest_path.parent,shot);name=shot.get('name')
                require(isinstance(name,str) and sp.name==name and name not in shotnames,'Invalid or duplicate capture name.')
                png=sp.read_bytes()[:24]
                require(len(png)==24 and png[:8]==b'\x89PNG\r\n\x1a\n' and png[12:16]==b'IHDR'
                        and int.from_bytes(png[16:20],'big')>0 and int.from_bytes(png[20:24],'big')>0,'Capture lacks PNG/IHDR dimensions.')
                shotpaths.append(sp);shotnames.append(name);packaged.append(dict(name=name,source=str(sp),sha256=shot['sha256']))
            require(len(set(shotpaths))==len(shotpaths) and {exact_file(s) for s in actual}==set(shotpaths),'Raw native capture references disagree with bound screenshots.')
            if not historical:
                expected=contract['phases'][n-1]
                require(set(cases)=={'PASS '+s for s in expected['checks']} and len(cases)==len(expected['checks']),'Wrong phase checks despite a passing summary.')
                require(set(shotnames)==set(expected['captures']) and len(shotnames)==len(expected['captures']),'Wrong exact phase captures.')
            result=dict(iteration=n,run_id=native['runId'],mode=mode,source_report=str(path),sha256=row['sha256'],cases=native['cases'],passed=len(cases),failures=0,unexpected_errors=0,complete=True,errors_finalized=True,seconds=duration,estimated_start_utc=start.isoformat(),report_modified_utc=end.isoformat(),time_basis=row['timeBasis'],screenshots=packaged,checks=cases,can_verify=native['canVerify'],cannot_verify=native['cannotVerify'],packaged_report=f'{"history" if historical else "iterations"}/{n:02}/report.json')
            (old if historical else results).append(result);paths.add(path);ids.add(native['runId']);digests.add(row['sha256'])
    require(sha(exact_file(final_native_path))==results[-1]['sha256'],'Selected final native must equal exact phase20 report bytes.')
    return dict(iterations=results,history=old,final_iteration=20,route_contract_sha256=contract_sha,not_before_utc=lower.isoformat(),independent_execution_timestamps_verified=False,
                timing_bound='Estimated intervals use each raw report duration and selected file mtime, not independently logged UTC execution or proof of source revision.',
                counting_note='Each of20 live gates keeps its raw count. Prior4 reports are separate history; final phase20 is not an extra run. Native and unit counts are never summed.')


def expected_catalog(spec):
    result=dict(schemaVersion=1,music=[],ambience=[],sounds=[],details=[])
    for row in spec['clips']:
        group={'music':'music','ambience':'ambience','sfx':'sounds','detail':'details'}[row['kind']]
        result[group].append(dict(id=row.get('context') if row['kind']=='music' else row.get('cue'),resource='Audio/World/'+row['path'][:-4],gain=row.get('runtimeGain') if row['kind']=='music' else row.get('gain')))
    for alias_name,kind,group in [('ambienceAliases','ambience','ambience'),('soundAliases','sfx','sounds')]:
        for alias in spec.get(alias_name,[]):
            candidates=[row for row in spec['clips'] if row['id']==alias['target'] and row['kind']==kind]
            require(len(candidates)==1,'Invalid or duplicate alias target.')
            result[group].append(dict(id=alias['id'],resource='Audio/World/'+candidates[0]['path'][:-4],gain=alias['gain']))
    return result


def validate_audio(repo,manifest_path,manifest_sha):
    manifest=approved_json(manifest_path,manifest_sha)
    require(manifest.get('schemaVersion')==1 and manifest.get('scope')=='audio-polish-final-assets','Wrong final asset index.')
    palettes=manifest.get('palettes');require(isinstance(palettes,list) and len(palettes)==5 and {p.get('id') for p in palettes}=={'original','places','atmosphere','utility','regional'},'Missing/duplicate approved palette families.')
    world=repo/'Assets/Resources/Audio/World';paths=set();guids=set();results=[];allcredits=[]
    for palette in palettes:
        label=palette['id'];files={key:bound_file(repo,palette.get(key),True) for key in ('spec','provenance','catalog','credits','receipt')}
        spec=read_json(files['spec']);provenance=read_json(files['provenance']);catalog=read_json(files['catalog']);receipt=read_json(files['receipt']);credits=files['credits'].read_text();allcredits.append(credits)
        require(spec.get('schemaVersion')==provenance.get('schemaVersion')==catalog.get('schemaVersion')==1,'Unknown source schema.')
        rows=spec.get('clips');require(isinstance(rows,list) and rows,'Empty source palette.');names=[r['path'] for r in rows]
        require(len(names)==len(set(names)) and not set(names)&paths,'Duplicate source output path.')
        require(provenance.get('sources')==spec.get('sources'),'Independent source provenance differs from approved spec.')
        for source in spec['sources'].values():require(isinstance(source,dict) and source.get('url') and type(source.get('bytes')) is int and source['bytes']>0 and re.fullmatch('[0-9a-f]{64}',str(source.get('sha256',''))),'Invalid original-source byte/hash provenance.')
        outputs=provenance.get('clips',[]);require(len(outputs)==len(rows) and {r.get('path') for r in outputs}==set(names),'Incomplete output provenance.');outputs={r['path']:r for r in outputs}
        observed=receipt.get('assets',[])
        require(receipt.get('passed') is True and receipt.get('findings')==[] and type(receipt.get('assetCount')) is int and receipt['assetCount']==len(rows)
                and len(observed)==len(rows) and {r.get('path') for r in observed}==set(names) and all(r.get('errors')==[] for r in observed),'Failed/inconsistent per-asset receipt.')
        resources={'Audio/World/'+name[:-4] for name in names};catalogrows=[r for key in ('music','ambience','sounds','details') for r in catalog.get(key,[])]
        require({r.get('resource') for r in catalogrows}==resources,'Catalog must exactly reference this palette union.')
        require(all(isinstance(r.get('id'),str) and r['id'] and type(r.get('gain')) in (int,float) and math.isfinite(r['gain']) and 0<r['gain']<=1 for r in catalogrows),'Invalid runtime binding/gain.')
        if label!='original':require(catalog==expected_catalog(spec),'Catalog differs from declared contexts/cues/gains/aliases.')
        hashes={};importers={};total=0;pcm=0
        for row in rows:
            name=row['path'];rel=pathlib.PurePosixPath(name)
            require(not rel.is_absolute() and '..' not in rel.parts and rel.suffix=='.ogg','Unsafe/non-Ogg source output.')
            clip=exact_file(world/name);require(clip.is_relative_to(world.resolve()),'Output escaped world audio.');info=outputs[name]
            digest=sha(clip);require(info.get('outputSha256')==digest and type(info.get('outputBytes')) is int and info['outputBytes']==clip.stat().st_size,'Output audio/provenance mismatch.')
            require(all(info.get(k)==v for k,v in row.items()),'Output metadata differs from source spec.')
            require(all(row.get(k) for k in ('author','title','page','license','licenseUrl','credit','changes','source')) and row['source'] in spec['sources'],'Missing original attribution.')
            require(row['license'] in ('CC0-1.0','CC-BY-4.0') and all(row[k] in credits for k in ('credit','page','licenseUrl')),'Full credits/source/license missing.')
            require(row['kind'] in ('music','ambience','sfx','detail'),'Unknown imported audio kind.')
            if row['kind']=='music':require(type(info.get('sourceDecodedFrames')) is int and type(info.get('preparedFrames')) is int and info['sourceDecodedFrames']>0 and info['sourceDecodedFrames']==info['preparedFrames'],'Music complete-frame conservation failed.')
            short=row['kind'] in ('sfx','detail')
            if short:require(type(info.get('preparedFrames')) is int and info['preparedFrames']>0,'Missing positive short-cue frame count.');pcm+=info['preparedFrames']*4
            meta=exact_file(clip.with_suffix('.ogg.meta'));text=meta.read_text();match=re.search(r'^guid: ([0-9a-f]{32})$',text,re.M)
            require(match and match[1] not in guids,'Invalid or duplicate imported clip GUID.');guids.add(match[1]);importers[name]=sha(meta)
            policy={'loadType':0,'preloadAudioData':1,'loadInBackground':0} if short else {'loadType':2,'preloadAudioData':0,'loadInBackground':1}
            for key,value in {**policy,'compressionFormat':1,'normalize':0}.items():require(re.search(r'^\s*'+key+': '+str(value)+r'\s*$',text,re.M),'Incorrect importer policy: '+name+' '+key)
            hashes[name]=digest;total+=clip.stat().st_size
        require(type(receipt.get('audioBytes')) is int and receipt['audioBytes']==total,'Asset receipt compressed-byte mismatch.')
        budgets=spec.get('budgets',{});require(total<=budgets.get('compressedBytes',64*1024*1024),'Declared compressed budget exceeded.')
        if pcm:
            measured=receipt.get('sfxDecodedFloatBytes' if label=='original' else 'shortDecodedFloatBytes')
            require(type(measured) is int and measured>0 and pcm<=measured+1152*4*len(rows)
                    and measured<=budgets.get('shortDecodedFloatBytes',4*1024*1024),'Short PCM receipt/budget mismatch.')
        results.append(dict(id=label,asset_count=len(rows),audio_bytes=total,source_asset_sha256=hashes,importer_sha256=importers,artifacts={k:dict(source=str(p),sha256=sha(p)) for k,p in files.items()}));paths.update(names)
    actual={p.relative_to(world).as_posix() for p in world.rglob('*.ogg')};require(actual==paths,'Actual World audio is not the exact approved source-manifest union.')
    licenses={}
    for name in LICENSE_FILES:
        p=exact_file(world/'Licenses'/name);licenses[name]=sha(p);require(licenses[name]==LICENSE_SHA256[name],'Complete license changed.')
    credits='\n'.join(allcredits);require(all(s in credits for s in ('Content ID','video description','music volume/mute')),'Streamer attribution/mute notice missing.')
    return dict(asset_count=len(paths),palettes=results,asset_index_sha256=manifest_sha,license_sha256=licenses,combined_credits_sha256=hashlib.sha256(credits.encode()).hexdigest(),subjective_listening_verified=False,
                verification_bound='Approved source file union, full credits/licenses, import metadata and independent artifact hashes checked. No Unity packed-resource inspection, listening, source-run correspondence or physical Deck performance inferred.')


def safe_copy(source,target,expected):
    source=exact_file(source)
    require(sha(source)==expected,'Input changed since validation: '+str(source))
    require(not target.is_symlink(),'Refusing output alias: '+str(target))
    target.parent.mkdir(parents=True,exist_ok=True)
    if source!=target.resolve():shutil.copy2(source,target)
    require(sha(target)==expected,'Copied evidence bytes changed.')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('native_report',type=pathlib.Path,help='Exact final audio-polish-20 raw receipt')
    parser.add_argument('--repo',type=pathlib.Path,required=True)
    parser.add_argument('--build-report',type=pathlib.Path,required=True)
    parser.add_argument('--editmode-report',type=pathlib.Path,required=True)
    parser.add_argument('--assets-manifest',type=pathlib.Path,required=True)
    parser.add_argument('--assets-manifest-sha256',required=True)
    parser.add_argument('--routes-contract',type=pathlib.Path,required=True)
    parser.add_argument('--routes-contract-sha256',required=True)
    parser.add_argument('--iterations-manifest',type=pathlib.Path,required=True)
    parser.add_argument('--source-commit',required=True)
    parser.add_argument('--folder',type=pathlib.Path,default=pathlib.Path('Builds/SteamDeck/2026-10-11-audio-polish20-current'))
    args=parser.parse_args();repo=args.repo.resolve()
    folder=(args.folder if args.folder.is_absolute() else repo/args.folder).resolve()
    require(folder.is_relative_to((repo/'Builds/SteamDeck').resolve()) and folder.name=='2026-10-11-audio-polish20-current','Destination must be the NEW audio-polish20-current folder.')
    player=folder/'CavesOfOoo-current';archive_root=player.name;audit=folder/'audit'
    archive=folder/'CavesOfOoo-SteamDeck-audio-polish20-current.tar.gz'
    require(not archive.exists() and not archive.is_symlink(),'Refusing to replace an existing archive.')
    paths={k:exact_file(v) for k,v in dict(final_native=args.native_report,build=args.build_report,final_editmode=args.editmode_report,
        assets_manifest=args.assets_manifest,routes_contract=args.routes_contract,iterations_manifest=args.iterations_manifest).items()}
    def git(*arguments):return subprocess.check_output(['git',*arguments],cwd=repo,text=True).strip()
    commit=git('rev-parse','HEAD');require(re.fullmatch('[0-9a-f]{40}',commit) and args.source_commit==commit,'Source commit must equal exact full current HEAD.')
    scopes=('Assets/Scripts','Assets/Editor','Assets/Resources','Assets/Scenes','Tools/SteamDeck','Tools/Audio')
    require(not git('diff','--name-only','HEAD','--',*scopes),'Commit final production source and instructions first.')
    untracked=git('ls-files','--others','--exclude-standard','--',*scopes).splitlines()
    relevant=[s for s in untracked if pathlib.Path(s).suffix in ('.cs','.asmdef','.asmref') or s.startswith(('Assets/Resources/','Assets/Scenes/','Tools/SteamDeck/','Tools/Audio/'))]
    require(not relevant,'Uncommitted source/instructions: '+', '.join(relevant))
    build=read_json(paths['build']);data=build.get('data',{})
    require(build.get('success') is True and data.get('result')=='succeeded' and type(data.get('errors')) is int and data['errors']==0
            and data.get('platform')=='StandaloneLinux64','Require successful zero-error Linux64 build.')
    start=evidence_time(data.get('started_at'));end=evidence_time(data.get('completed_at'))
    require(end>=start and start.timestamp()>=int(git('show','-s','--format=%ct',commit)),'Build predates final HEAD or has bad times.')
    executable=player/'CavesOfOoo.x86_64';reported=pathlib.Path(data['output_path']);reported=reported if reported.is_absolute() else repo/reported
    require(reported.resolve()==executable.resolve(),'Build receipt names another output directory.')
    require(executable.is_file() and (player/'CavesOfOoo_Data').is_dir() and (player/'UnityPlayer.so').is_file(),'Linux player payload incomplete.')
    with executable.open('rb') as stream:elf=stream.read(20)
    require(len(elf)==20 and elf[:4]==b'\x7fELF' and elf[4:6]==b'\x02\x01' and int.from_bytes(elf[18:20],'little')==62,'Player is not x86_64 ELF.')
    testroot=ET.parse(paths['final_editmode']).getroot();summary=validate_tests(testroot)
    native=read_json(paths['final_native']);cases=validate_native(native,'audio-polish-20')
    iterations=validate_iterations(paths['iterations_manifest'],paths['final_native'],paths['routes_contract'],args.routes_contract_sha256)
    audio=validate_audio(repo,paths['assets_manifest'],args.assets_manifest_sha256)
    contract=read_json(paths['routes_contract'])
    tests=dict(mode='EditMode',id=testroot.get('id'),start_time=testroot.get('start-time'),end_time=testroot.get('end-time'),duration_seconds=float(testroot.get('duration','0')),**summary)
    require(not audit.is_symlink() and audit.resolve().is_relative_to(folder),'Audit directory is an external alias.')
    require(not player.is_symlink() and player.resolve().is_relative_to(folder),'Player directory is an alias.')
    for path in player.rglob('*'):
        if path.is_symlink():require(not pathlib.Path(path.readlink()).is_absolute() and path.resolve().is_relative_to(player.resolve()),'External/nonportable payload link.')
    # Metadata trees must be fully alias-free before any copies or chmod.
    for dirname in ('Audio-Assets','Audio-Evidence','Audio-Licenses'):
        path=player/dirname;require(not path.is_symlink(),'Metadata directory alias.')
        if path.exists():require(path.is_dir() and not any(p.is_symlink() for p in path.rglob('*')),'Metadata subtree has aliases.')
    for name in ('Launch-CavesOfOoo.sh','README-Steam-Deck.md','build-info.json','SHA256SUMS','AUDIO-CREDITS.txt'):
        require(not (player/name).is_symlink(),'Metadata file alias.')
    for path in (archive.with_name(archive.name+'.partial'),archive.with_suffix(archive.suffix+'.sha256'),audit/'package-checks.json'):
        require(not path.is_symlink(),'Derived output alias.')
    copies=[]
    for name,path in paths.items():copies.append((path,player/'Audio-Evidence'/('selected-'+name+path.suffix),sha(path)))
    for historical,rows in ((False,iterations['iterations']),(True,iterations['history'])):
        for row in rows:
            target=player/'Audio-Evidence'/row['packaged_report'];copies.append((pathlib.Path(row['source_report']),target,row['sha256']))
            for shot in row['screenshots']:copies.append((pathlib.Path(shot['source']),target.parent/shot['name'],shot['sha256']))
    for i,row in enumerate(contract['sourceFiles']):
        path=bound_file(paths['routes_contract'].parent,row);copies.append((path,player/'Audio-Evidence'/'route-source'/f'{i:02}-{path.name}',row['sha256']))
    fullcredits=[]
    for palette in audio['palettes']:
        for key,row in palette['artifacts'].items():
            source=pathlib.Path(row['source']);copies.append((source,player/'Audio-Assets'/palette['id']/(key+source.suffix),row['sha256']))
            if key=='credits':fullcredits.append(source.read_text())
    world=repo/'Assets/Resources/Audio/World'
    for name,digest in audio['license_sha256'].items():copies.append((world/'Licenses'/name,player/'Audio-Licenses'/name,digest))
    for name in ('Launch-CavesOfOoo.sh','README-Steam-Deck.md'):
        source=exact_file(repo/'Tools/SteamDeck'/name);copies.append((source,player/name,sha(source)))
    destinations=[str(target) for _,target,_ in copies];require(len(destinations)==len(set(destinations)),'Duplicate evidence output destination.')
    for _,target,_ in copies:require(not target.is_symlink(),'Evidence destination alias.')
    # All receipts, exact phase checks, captures, source files and assets are
    # validated before modifying the already-built player. No build is invoked.
    audit.mkdir(parents=True,exist_ok=True)
    for source,target,digest in copies:safe_copy(source,target,digest)
    credits='\n'.join(fullcredits);require(hashlib.sha256(credits.encode()).hexdigest()==audio['combined_credits_sha256'],'Credits changed during validation.')
    (player/'AUDIO-CREDITS.txt').write_text(credits)
    (player/'Audio-Evidence/index.json').write_text(json.dumps(iterations,indent=2)+'\n')
    for name in ('CavesOfOoo.x86_64','Launch-CavesOfOoo.sh'):(player/name).chmod(0o755)
    version=re.search(r'^m_EditorVersion:\s*(.+)$',(repo/'ProjectSettings/ProjectVersion.txt').read_text(),re.M)
    info=dict(commit=commit,source_tree=git('rev-parse','HEAD^{tree}'),packaged_at_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        unity_version=version[1] if version else None,platform='Linux x86_64',package_root=archive_root,packaging_script_sha256=sha(pathlib.Path(__file__)),
        delivery_readme_adjustment='None: copy is byte-identical to committed source.',build_report=build,
        receipt_sha256={name:sha(path) for name,path in paths.items()},tracked_configuration_changes_at_packaging=git('diff','--name-only','HEAD','--','Packages','ProjectSettings').splitlines(),
        validation=dict(selected_final_editmode=tests,selected_final_audio_native=dict(run_id=native['runId'],mode=native['mode'],cases=native['cases'],passed=len(cases),failed=0,unexpected_errors=0,complete=True,seconds=native['seconds']),
            audio_assets=audio,audio_live_iterations=iterations,voice_policy='Existing fixed16-source world pool, unchanged.',
            source_correspondence='Coordinator selects final unit and phase20 receipts before committing unchanged final source and building. Earlier gates describe their incremental revisions. Independent current source/artifact hashes do not retroactively prove source revision identity for raw reports.',
            counting_note=iterations['counting_note'],physical_steam_deck_tested=False,physical_controller_audio_performance_tested=False,linux_player_runtime_tested=False,
            performance_note='Observed Mac/editor routes and source PCM do not establish listening quality, global output, Linux behavior or physical Deck frame rate, thermals, battery, latency or controller feel. No percentage improvement or numeric speedup claimed; prior confounded timings are not attributed to this delivery.'))
    (player/'build-info.json').write_text(json.dumps(info,indent=2)+'\n')
    payload_root=player.resolve()
    for path in player.rglob('*'):
        if path.is_symlink():require(not pathlib.Path(path.readlink()).is_absolute() and path.resolve().is_relative_to(payload_root),'External/nonportable payload link.')
    files=sorted(path for path in player.rglob('*') if path.is_file() and path!=player/'SHA256SUMS' and path.name!='.DS_Store')
    checks={path.relative_to(player).as_posix():sha(path) for path in files}
    require(all('\n' not in name and '\r' not in name for name in checks),'Unsupported newline in payload filename.')
    checksum_manifest=''.join(f'{value}  {name}\n' for name,value in checks.items()).encode();(player/'SHA256SUMS').write_bytes(checksum_manifest)
    partial=archive.with_name(archive.name+'.partial')
    try:
        with tarfile.open(partial,'w:gz',compresslevel=6) as tar:tar.add(player,arcname=archive_root,filter=lambda item:None if pathlib.PurePosixPath(item.name).name=='.DS_Store' else item)
        with tarfile.open(partial,'r:gz') as tar:
            members=tar.getmembers();names=[member.name for member in members]
            require(len(names)==len(set(names)) and all(name==archive_root or name.startswith(archive_root+'/') for name in names)
                and all('..' not in pathlib.PurePosixPath(name).parts for name in names),'Unsafe/duplicate archive path.')
            archived={m.name.removeprefix(archive_root+'/') for m in members if m.isfile() or m.issym() or m.islnk()}
            require(archived==set(checks)|{'SHA256SUMS'},'Archive file set differs from checksum manifest.')
            for name,expected in checks.items():
                stream=tar.extractfile(archive_root+'/'+name);require(stream is not None,'Missing archive payload.')
                with stream:require(stream_sha(stream)==expected,'Archive payload digest mismatch.')
            with tar.extractfile(archive_root+'/SHA256SUMS') as stream:require(stream.read()==checksum_manifest,'Archived checksum manifest differs.')
            for name in ('CavesOfOoo.x86_64','Launch-CavesOfOoo.sh'):require(tar.getmember(archive_root+'/'+name).mode&0o111==0o111,'Executable permission missing.')
        require(git('rev-parse','HEAD')==commit and not git('diff','--name-only','HEAD','--',*scopes),'HEAD/source changed during packaging.')
        # No-overwrite atomic publish; a newly appearing archive is not replaced.
        archive.hardlink_to(partial)
    finally:
        if partial.exists():partial.unlink()
    digest=sha(archive);archive.with_suffix(archive.suffix+'.sha256').write_text(digest+'  '+archive.name+'\n')
    result=dict(archive=str(archive),commit=commit,bytes=archive.stat().st_size,sha256=digest,verified_payload_files=len(checks),archive_stream_digests_match=True,
        archive_file_list_matches=True,executable_permissions=True,editmode=summary,native_run_id=native['runId'],native_cases=native['cases'],native_failures=0,
        live_iterations=[{k:row[k] for k in ('iteration','run_id','mode','cases','sha256')} for row in iterations['iterations']],history_report_count=len(iterations['history']),
        audio_assets=audio['asset_count'],audio_credits_and_licenses_included=True,physical_steam_deck_tested=False)
    (audit/'package-checks.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))


if __name__=='__main__':main()
