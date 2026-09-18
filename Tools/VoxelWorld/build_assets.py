#!/usr/bin/env python3
"""Explicit offline native voxel bake; never launches on import."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Village3D'))
from run_common import ROOT, UNITY, MCP, restart_mcp, refuse_unity, utc

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('name')
    parser.add_argument('--recipes-only',action='store_true')
    args=parser.parse_args()
    if not args.name or Path(args.name).name!=args.name:
        parser.error('name must be one path component')
    run=ROOT/'Docs/Verification/VoxelWorld'/args.name
    run.mkdir(parents=True,exist_ok=False)
    refuse_unity()
    mcp=restart_mcp(MCP,args.name)
    (run/'mcp-start.json').write_text(json.dumps(mcp,indent=2)+'\n')
    method='CavesOfOoo.Editor.VoxelWorldToolkitImporter.'+('BuildFromCommandLine' if args.recipes_only else 'BuildAllFromCommandLine')
    command=[str(UNITY),'-batchmode','-quit','-projectPath',str(ROOT),'-executeMethod',method,
             '-voxelWorldMeshReport',str(run/'mesh-bake.json'),'-voxelWorldToolkitReport',str(run/'toolkit-import.json'),
             '-logFile',str(run/'unity.log')]
    start=utc()
    result=subprocess.run(command,cwd=ROOT)
    log=(run/'unity.log').read_text(errors='replace')
    errors=[line for line in log.splitlines() if 'error CS' in line]
    print('C# error lines:',len(errors),flush=True)
    report={'startedUtc':start,'finishedUtc':utc(),'unityExit':result.returncode,'compileErrors':errors,'command':command}
    (run/'receipt.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2),flush=True)
    return result.returncode or bool(errors)
if __name__=='__main__':
    raise SystemExit(main())
