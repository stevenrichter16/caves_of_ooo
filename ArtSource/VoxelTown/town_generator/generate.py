"""CLI entry point. Pure model first, then optional Blender realization.

python3 -m town_generator.generate --data-only --output /tmp/town
blender -b --threads 2 --python-exit-code 1 --python .../generate.py -- --render --output /tmp/town
"""
import argparse
import contextlib
import gzip
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import time

if __package__ in (None,''):
    sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from town_generator.config import Config,ARCHETYPES
from town_generator.layout import generate_town
from town_generator.model import to_dict

ARTIFACT_NAMES = frozenset(('town.json','town.blend','town.fbx','town.voxels.json.gz',
                            'gameplay.png','stats.json','fbx-export.log'))


def _write_json(path,value):
    path.write_text(json.dumps(value,indent=2,sort_keys=True)+'\n')


def _publish(stage,output,files,seed,mode):
    """Publish a complete generation; restore replaced files on ordinary I/O errors.

    Only fixed generated names are replaced. Unrequested prior artifacts remain
    on disk but are explicitly excluded from the new hash manifest. The manifest
    is replaced last. This is not a process-crash or concurrent-writer transaction;
    consumers must check its hashes before treating a bundle as coherent.
    """
    if not set(files)<=ARTIFACT_NAMES:raise ValueError('Unknown generated artifact')
    records={}
    for name in files:
        data=(stage/name).read_bytes()
        if not data and name!='fbx-export.log':raise ValueError(f'Empty generated artifact: {name}')
        records[name]={'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    retained=sorted(name for name in ARTIFACT_NAMES-set(files) if (output/name).is_file())
    _write_json(stage/'bundle.json',dict(schema=1,complete=True,seed=seed,mode=mode,
                files=records,retainedPreviousArtifacts=retained,
                policy='Only files listed here belong to this generation; verify their hashes. Other files are preserved.'))
    names=sorted(files)+['bundle.json']
    if output.exists() and not output.is_dir():raise ValueError('Output must be a directory')
    for name in names:
        target=output/name
        if target.exists() and not target.is_file() and not target.is_symlink():
            raise ValueError(f'Generated artifact destination is not a file: {target}')
    # Back up current files before any replacement; never delete the directory.
    previous=stage/'previous';previous.mkdir()
    for name in names:
        target=output/name
        if target.exists() or target.is_symlink():shutil.copy2(target,previous/name,follow_symlinks=False)
    existed=output.exists();output.mkdir(parents=True,exist_ok=True);published=[]
    try:
        for name in names:
            os.replace(stage/name,output/name);published.append(name)
    except BaseException:
        for name in reversed(published):
            backup=previous/name
            if backup.exists() or backup.is_symlink():os.replace(backup,output/name)
            else:(output/name).unlink()
        if not existed and not any(output.iterdir()):output.rmdir()
        raise


def _export_fbx(bpy,scene,stage):
    """Keep full exporter output; aggregate only verified duplicate-slot noise."""
    from town_generator.scene import OWNER
    meshes={};compatible=True;selection=[];shared_users=0
    for obj in scene.objects:
        selection.append((obj,obj.select_get()))
        selected=obj.type=='MESH' and obj.get('coo_voxel_owner')==OWNER
        obj.select_set(selected)
        if selected:
            layout=tuple(slot.material for slot in obj.material_slots)
            if obj.data in meshes:
                shared_users+=1
                if meshes[obj.data]!=layout:compatible=False
            meshes[obj.data]=layout
    captured=io.StringIO()
    try:
        with contextlib.redirect_stdout(captured):
            result=bpy.ops.export_scene.fbx(filepath=str(stage/'town.fbx'),use_selection=True,
                object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
                add_leaf_bones=False,bake_anim=False,use_custom_props=True)
        if 'FINISHED' not in result:raise RuntimeError('FBX export did not finish')
    finally:
        for obj,selected in selection:obj.select_set(selected)
        raw=captured.getvalue();(stage/'fbx-export.log').write_text(raw)
        suppressed=0
        for line in raw.splitlines(keepends=True):
            if compatible and shared_users and line.startswith('WARNING: Cannot register a valid material index'):
                suppressed+=1
            else:print(line,end='')
        if suppressed:
            print(f'FBX: {suppressed} duplicate shared-mesh material registrations; identical slot layouts verified. Full output: fbx-export.log')


def _generate_staged(town,config,args,stage,output,start,layout_seconds):
    """Create all requested artifacts without touching destination bundle files."""
    _write_json(stage/'town.json',to_dict(town))
    mode='data-only' if args.data_only else 'blender'
    files={'town.json','stats.json'}
    stats=dict(seed=config.seed,mode=mode,layout_seconds=layout_seconds,output=str(output),
               bundle_manifest='bundle.json',artifacts=dict(semantic=True,blend=False,voxels=False,fbx=False,render=False))
    if not args.data_only:
        import bpy
        from town_generator.scene import realize
        scene,manifest,scene_stats=realize(town)
        stats.update(scene_stats)
        bpy.context.window.scene=scene;scene.cycles.samples=args.samples
        stats['generation_seconds']=time.perf_counter()-start
        raw=json.dumps(manifest,sort_keys=True,separators=(',',':')).encode()
        stats['voxel_manifest_sha256']=hashlib.sha256(raw).hexdigest()
        (stage/'town.voxels.json.gz').write_bytes(gzip.compress(raw,mtime=0))
        files.update(('town.voxels.json.gz','town.blend'))
        stats['artifacts'].update(blend=True,voxels=True)
        if args.export_fbx:
            _export_fbx(bpy,scene,stage);files.update(('town.fbx','fbx-export.log'));stats['artifacts']['fbx']=True
        # Render only into staging. Store the durable destination render path in
        # the saved Blender source, never the soon-to-be-deleted temporary path.
        if args.render:
            scene.render.filepath=str(stage/'gameplay.png');render_start=time.perf_counter()
            if 'FINISHED' not in bpy.ops.render.render(write_still=True,scene=scene.name):
                raise RuntimeError('Render did not finish')
            stats['render_seconds']=time.perf_counter()-render_start
            files.add('gameplay.png');stats['artifacts']['render']=True
        scene.render.filepath=str(output/'gameplay.png')
        for screen in bpy.data.screens:
            for area in screen.areas:
                if area.type=='VIEW_3D':
                    area.spaces.active.region_3d.view_perspective='CAMERA'
                    area.spaces.active.shading.type='MATERIAL'
        if 'FINISHED' not in bpy.ops.wm.save_as_mainfile(filepath=str(stage/'town.blend'),copy=True):
            raise RuntimeError('Blender save did not finish')
    _write_json(stage/'stats.json',stats)
    _publish(stage,output,files,config.seed,mode)
    return stats


def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output',type=Path,required=True)
    p.add_argument('--config',type=Path)
    p.add_argument('--seed',type=int)
    p.add_argument('--town-size',type=float)
    p.add_argument('--building-count',type=int)
    p.add_argument('--population',type=int)
    p.add_argument('--all-archetypes',action='store_true')
    p.add_argument('--data-only',action='store_true')
    p.add_argument('--render',action='store_true')
    p.add_argument('--export-fbx',action='store_true')
    p.add_argument('--samples',type=int,default=48)
    args=p.parse_args(argv)
    if args.data_only and (args.render or args.export_fbx):
        p.error('--data-only cannot be combined with --render or --export-fbx')
    values=json.loads(args.config.read_text()) if args.config else {}
    for key in ('seed','town_size','building_count','population'):
        if getattr(args,key) is not None:values[key]=getattr(args,key)
    if args.all_archetypes:values['archetypes']=ARCHETYPES
    if 'archetypes' in values:values['archetypes']=tuple(values['archetypes'])
    config=Config(**values).validate()
    if not 1<=args.samples<=4096:p.error('--samples must be between 1 and 4096')
    start=time.perf_counter();town=generate_town(config);layout_seconds=time.perf_counter()-start
    output=args.output.resolve();output.parent.mkdir(parents=True,exist_ok=True)
    # TemporaryDirectory owns precisely this unique sibling staging directory;
    # exception cleanup never removes user output or unrelated files.
    with tempfile.TemporaryDirectory(prefix='.voxel-town-',dir=output.parent) as temporary:
        stats=_generate_staged(town,config,args,Path(temporary),output,start,layout_seconds)
    print('VOXEL TOWN GENERATED '+json.dumps(stats,sort_keys=True),flush=True)


if __name__=='__main__':
    main(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else None)
