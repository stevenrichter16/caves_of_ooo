import importlib.util,json,tempfile
from pathlib import Path
from PIL import Image
base=Path('/Users/steven/caves-of-ooo/Docs/Verification/StarterSpell3D/Readability')
def module(name):
 spec=importlib.util.spec_from_file_location(name,base/(name+'.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
m=module('compose_final_native_media');p=module('check_r00_preservation');checks=[]
def check(name,ok):
 checks.append({'name':name,'passed':bool(ok)});assert ok,name
report={'runId':'synthetic','workloadComplete':True,'failures':0,'unexpectedErrors':0,'fatal':'','screenWidth':1920,'screenHeight':1080}
for k in ('shutdownObserved','shutdownRootHeld','shutdownSavingUnregistered','displayPreferencesRestored','inputSettingsRestored'):report[k]=True
pin={'status':'PASS','stage':'final','runId':'synthetic','nativeReportSha256':'a'*64}
m.final_pin(report,'a'*64,pin);check('synthetic matching final pin accepted',True)
for label,mutation in [('candidate',{'stage':'candidate'}),('wrong-run',{'runId':'other'}),('wrong-hash',{'nativeReportSha256':'b'*64})]:
 try:m.final_pin(report,'a'*64,pin|mutation)
 except ValueError:check(label+' refused',True)
 else:check(label+' refused',False)
row={'frames':[{'wallSeconds':.01},{'wallSeconds':.12},{'wallSeconds':.34}],'seconds':.5}
check('measured capture durations preserved',m.capture_durations(row)==[.11,.22000000000000003,.15999999999999998])
for end in (.2,.34,float('nan')):
 try:m.capture_durations(row|{'seconds':end})
 except ValueError:check('bad end timestamp '+str(end),True)
 else:check('bad end timestamp '+str(end),False)
name='Assets/Art3D/SpellFx/synthetic.asset';before={name:{'sha256':'a','bytes':1}};after={name:{'sha256':'b','bytes':1}}
approval={'reviewedBy':'synthetic reviewer','changes':[{'path':name,'change':'modified','beforeSha256':'a','afterSha256':'b','owner':'fixture','reason':'deliberate synthetic test'}]}
check('unchanged files require no pin',p.compare(before,before,{})==( [], [], []))
check('owned directory does not autoapprove',not p.compare(before,after,{})[0][0]['approved'])
check('exact reviewed hash accepted',p.compare(before,after,approval)[0][0]['approved'])
check('stale hash refused',not p.compare(before,{name:{'sha256':'c','bytes':1}},approval)[0][0]['approved'])
check('new neighboring file unapproved',not next(c for c in p.compare(before,after|{name+'.other':{'sha256':'z','bytes':1}},approval)[0] if c['path'].endswith('.other'))['approved'])
protected='Assets/Scripts/Gameplay/synthetic.cs';pp={'reviewedBy':'fixture','changes':[approval['changes'][0]|{'path':protected}]}
check('protected gameplay cannot be pinned away',not p.compare({protected:before[name]},{protected:after[name]},pp)[0][0]['approved'])
check('duplicate approval rejected',bool(p.compare(before,after,approval|{'changes':approval['changes']*2})[2]))
work=Path(tempfile.mkdtemp(prefix='coo-native-media-codec-smoke-'));times=[.127369,.233746,.089257];timeline=[];cursor=0
for i,duration in enumerate(times):
 image=Image.new('RGB',(96,64));image.putdata([((x*17+i*31)%256,(y*23+i*7)%256,((x+y)*13+i*47)%256) for y in range(64) for x in range(96)])
 path=work/(str(i)+'.png');image.save(path);timeline.append({'encodedPath':str(path),'start':cursor,'duration':duration});cursor+=duration
codec=m.encode_lossless(timeline,work/'synthetic-lossless-rgb.mp4',(96,64));check('RGB decode exact and timing verified',codec['decodedRgbMatchesEveryInput'] and codec['maximumPtsErrorSeconds']<=.0011)
result={'status':'PASS','assertions':len(checks),'checks':checks,'syntheticOnly':True,'codec':codec,'scope':'Synthetic calibration frames only; no candidate/final game report was composed or published.'}
(base/'review-tools-smoke.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'status':'PASS','assertions':len(checks),'codecPath':str(work),'ptsError':codec['maximumPtsErrorSeconds']},indent=2))
