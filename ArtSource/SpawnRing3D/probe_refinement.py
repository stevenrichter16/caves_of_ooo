import bpy,json,sys,math
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];base=Path(args[0]);root=Path(args[1]);report=Path(args[2])
bc=json.loads((base/'catalog.json').read_text());c=json.loads((root/'catalog.json').read_text());bm={m['id']:m for m in bc['models']};cm={m['id']:m for m in c['models']}
bpy.ops.wm.open_mainfile(filepath=str(base/'ring_kit.blend'))
def bark_count():return sum(1 for o in bpy.data.objects if o.type=='MESH' and o.name.startswith(('Worn_petrified_bark_plate','Flowing_petrified_bark_ridge')))
base_bark=bark_count()
bpy.ops.wm.open_mainfile(filepath=str(root/'ring_kit.blend'));now_bark=bark_count()
grounds=[m for m in c['models'] if m['id'].startswith(('ring-floor-','ring-grass-','ring-tepui-stone-'))];ledges=[cm['ring-descent-ledge-'+str(i)] for i in range(4)]
aspects=[m['boundsSize']['x']/m['boundsSize']['z'] for m in ledges]
checks=[('ground_has_no_raised_microgeometry',all(m['triangles']==12 and m['boundsSize']['y']<=.08001 for m in grounds)),('ledge_variants_have_distinct_aspects',max(aspects)-min(aspects)>.20),('root_detail_density_reduced',now_bark<=base_bark*.60),('all_ids_retained',set(bm)==set(cm)),('per_model_triangles_do_not_increase',all(m['triangles']<=bm[mid]['triangles'] for mid,m in cm.items())),('positive_ground_thickness',all(m['boundsSize']['y']>.05 for m in grounds))]
result={'baseline':str(base),'candidate':str(root),'checks':[{'name':n,'passed':ok} for n,ok in checks],'baselineBarkObjects':base_bark,'candidateBarkObjects':now_bark,'ledgeAspects':aspects,'groundTriangles':{m['id']:m['triangles'] for m in grounds},'failed':sum(not x[1] for x in checks)}
report.write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
