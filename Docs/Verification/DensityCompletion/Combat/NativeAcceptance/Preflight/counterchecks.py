from pathlib import Path
import subprocess,os,xml.etree.ElementTree as E,json,hashlib
root=Path('/tmp/coo-c2-native');helper=root/'DensityCombatNativeEvidence.cs';source=helper.read_text();props=root/'Runner/tests.props';old=props.read_text();runs=[]
mutations=[('marker-target','||rows[i].ActorId!=player||rows[i].TargetId!=enemy','||rows[i].ActorId!=player'),('buried-goal','&&ReferenceEquals(brain.CurrentZone,zone)&&ReferenceEquals(brain.PeekGoal(),exactGoal)','&&ReferenceEquals(brain.CurrentZone,zone)'),('foreign-move','&&Text(row,"scheduledActor",out string scheduled)&&scheduled==enemy','&&Text(row,"scheduledActor",out string scheduled)')]
try:
 for name,before,after in mutations:
  assert source.count(before)==1
  p=root/('mutant-'+name+'.cs');p.write_text(source.replace(before,after))
  props.write_text(old.replace(str(helper),str(p)))
  xml=root/('counter-'+name+'.xml');log=root/('counter-'+name+'.log')
  with log.open('w') as out: subprocess.run([str(root/'Runner/run.sh'),'class =~ /DensityCombatNativeEvidenceTests/',str(xml)],env=dict(os.environ,COO_REPO='/Users/steven/caves-of-ooo'),stdout=out,stderr=subprocess.STDOUT)
  tree=E.parse(xml).getroot();fails=[e.attrib['fullname'] for e in tree.iter('test-case') if e.attrib.get('result')=='Failed']
  runs.append(dict(name=name,total=tree.attrib.get('total'),passed=tree.attrib.get('passed'),failed=tree.attrib.get('failed'),failures=fails,log=str(log),xml=str(xml)))
  print(name,len(fails),flush=True);assert fails
finally:props.write_text(old)
(root/'counterchecks.json').write_text(json.dumps(dict(sourceSha256=hashlib.sha256(helper.read_bytes()).hexdigest(),candidateUnchanged=helper.read_text()==source,runs=runs),indent=2)+'\n')
