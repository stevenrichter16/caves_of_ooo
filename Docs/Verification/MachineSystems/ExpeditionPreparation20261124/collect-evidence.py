from pathlib import Path
import hashlib,json,shutil,gzip,xml.etree.ElementTree as ET,re,datetime
repo=Path('/Users/steven/caves-of-ooo');out=Path("/Users/steven/caves-of-ooo/Docs/Verification/MachineSystems/ExpeditionPreparation20261124");native=out/'Native';native.mkdir(exist_ok=True)
prefixes=['fish_shared_red_20261124a','fish_legacy_red_20261124a','fish_shared_green_20261124b','expedition_pair_red_20261124a','expedition_batch_red_20261124b','expedition_batch_red_20261124c','expedition_batch_green_20261124a','expedition_batch_green_20261124b','expedition_batch_green_20261124c','expedition_review_red_20261124a','acid_ownership_red_20261124a','expedition_adjacent_green_20261124a','expedition_mut_20261124_m1-no-distill-fuel','expedition_mut_20261124_m2-no-rinse-final-gate','expedition_mut_20261124_m3-no-spent-cartridge']
summary={}
for prefix in prefixes:
 row={}
 for p in Path('/Users/steven/coo-verification/results').glob(prefix+'.*'):
  if p.suffix=='.log':
   (native/(p.name+'.gz')).write_bytes(gzip.compress(p.read_bytes(),mtime=0))
   row['compilerErrors']=len(set(re.findall(r'error CS[0-9]+[^\n]*',p.read_text(errors='replace'))))
  elif p.suffix in ('.xml','.out'):shutil.copy2(p,native/p.name)
  if p.suffix=='.xml':
   elem=ET.parse(p).getroot();row['result']={k:elem.attrib.get(k) for k in ['total','passed','failed','skipped','inconclusive','result']}
 summary[prefix]=row
(out/'native-results.json').write_text(json.dumps(summary,indent=2)+'\n')
for name,src in [('Before','/private/tmp/coo-expedition-before-earned-20261124'),('After','/private/tmp/coo-expedition-after-earned-20261124'),('Fresh','/private/tmp/coo-expedition-fresh-earned-20261124')]:
 dest=out/'Checkpoints'/name;assert not dest.exists();shutil.copytree(src,dest)
state=json.loads(Path('/private/tmp/coo-expedition-current-state-20261124.json').read_text());paths=[p for p in state['owned'] if p.endswith('.cs')]+list(state['newMetaPins'])
pins={p:hashlib.sha256((repo/p).read_bytes()).hexdigest() for p in paths};(out/'source-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
shutil.copy2('/private/tmp/coo-expedition-mutants-20261124.json',out/'mutant-plan.json')
print(json.dumps({'native':summary,'sourcePins':len(pins),'checkpointFiles':len(list((out/'Checkpoints').rglob('Quick*')))},indent=2))

