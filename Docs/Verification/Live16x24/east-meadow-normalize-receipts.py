import json,hashlib
from pathlib import Path
p=Path(__file__).resolve().parent
def normalized(rows):
 out=[]
 for raw in rows:
  r=json.loads(raw)
  r['equipped']=sorted(('body='+s.split('=',1)[1]) if s.split('=',1)[0].isdigit() else s for s in r['equipped'])
  out.append(json.dumps(r,separators=(',',':'),ensure_ascii=False))
 return sorted(out)
def sha(v): return hashlib.sha256(v.encode()).hexdigest()
b=json.loads((p/'east-meadow-original-generated.json').read_text())['rows'];a=json.loads((p/'east-meadow-revised-generated.json').read_text())['rows'];rows=[]
for x,y in zip(b,a):
 old=normalized(x['tuples']);new=normalized(y['tuples'])
 rows.append({'seed':x['seed'],'original_flowers':x['flowers'],'revised_flowers':y['flowers'],'nonFlowerCount':x['nonFlowerCount'],'original_normalized_hash':sha('\n'.join(old)),'revised_normalized_hash':sha('\n'.join(new)),'normalized_nonflower_equal':old==new,'roadStone_equal':x['roadStone']==y['roadStone'],'reservations_equal':x['reserved']==y['reserved'],'disposition_equal':x['disposition']==y['disposition']})
r={'kind':'derived_normalized_native_generation_comparison','method':'Preserve both raw native receipts. Only numeric equipped-cache keys are mapped to body=; values and duplicate multiplicity retained and ordinally sorted. These keys are allocated BodyPart.ID, not slot types. All other tuple fields unchanged. This proves equipped-item multiset, not exact anatomical slot identity across cold generations.','rows':rows,'all_normalized_nonflower_data_equal':all(x['normalized_nonflower_equal'] for x in rows),'not_gameplay_acceptance':True}
(p/'east-meadow-normalized-generated-comparison.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r,indent=2))
