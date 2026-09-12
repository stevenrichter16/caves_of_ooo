"""Run pure and optional actual-Blender toolkit gates, retaining each receipt."""
import argparse,json,os,subprocess,sys,time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--blender',type=Path)
    p.add_argument('--fbx-example',default=None,help='Output folder name containing town.blend and town.fbx')
    p.add_argument('--receipts',type=Path,default=ROOT/'Docs/Verification/VoxelTown/final')
    args=p.parse_args();args.receipts.mkdir(parents=True,exist_ok=True)
    env=dict(os.environ,PYTHONPATH=str(HERE));cases=[]
    commands=[('pure',[sys.executable,'-m','unittest','discover','-s',str(HERE/'tests'),'-p','test_*.py'])]
    if args.blender:
        for script in sorted((HERE/'tests').glob('blender_*.py')):
            if script.name=='blender_fbx_roundtrip.py' and not args.fbx_example:continue
            cmd=[str(args.blender),'-b','--threads','2','--python-exit-code','1','--python',str(script)]
            if script.name=='blender_fbx_roundtrip.py':cmd+=['--',args.fbx_example]
            commands.append((script.stem,cmd))
    for name,cmd in commands:
        start=time.perf_counter();result=subprocess.run(cmd,cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
        (args.receipts/(name+'.log')).write_text(result.stdout)
        row={'gate':name,'passed':result.returncode==0,'exit_code':result.returncode,'seconds':round(time.perf_counter()-start,3)}
        cases.append(row);print(json.dumps(row),flush=True)
    (args.receipts/'summary.json').write_text(json.dumps(cases,indent=2)+'\n')
    return 0 if all(r['passed'] for r in cases) else 1

if __name__=='__main__':sys.exit(main())
