#!/usr/bin/env python3
"""Read-only, fixture-specific verification of the earned seed242967504 temper journey.

Usage: python3 coo-earned-temper-audit-final.py --baseline DIR --before-restart DIR
       --after-restart DIR --output NEW_JSON
Each input DIR must contain Quick.json and Quick.sav.gz. Output is created
exclusively, outside every input directory; existing paths are never overwritten.
Only standard-library file reads and a JSON report write are used. No Unity or
runtime loading. The 26 named predicates below are evaluated from saved bytes.
This is a scoped format8 parser reused from the earlier checkpoint audit, not a
universal save validator: entity bodies are bounded by serialized check markers,
then selected public Part fields are read. Unexpected structure raises an error
rather than recording a pass. No intermediate coating identity or actual Burning
hit is proved. Restart comparison covers emitted records, not every save field.
"""
from pathlib import Path
import argparse, collections, datetime, gzip, hashlib, io, json, struct, sys

class Reader:
 def __init__(self,data):self.f=io.BytesIO(data)
 def n(self,n):
  v=self.f.read(n)
  if len(v)!=n:raise ValueError('truncated stream')
  return v
 def i(self):return struct.unpack('<i',self.n(4))[0]
 def b(self):
  v=self.n(1)[0]
  if v not in (0,1):raise ValueError('invalid bool')
  return bool(v)
 def s(self):
  if not self.b():return None
  n=0
  for shift in range(0,35,7):
   v=self.n(1)[0];n|=(v&127)<<shift
   if not v&128:return self.n(n).decode('utf8')
  raise ValueError('bad string length')
 def check(self,label):
  value=17
  for ch in label:value=(value*31+ord(ch))&0xffffffff
  assert self.i()==struct.unpack('<i',struct.pack('<I',value))[0],label

def load(path):
 p=Path(path)/'Quick.sav.gz'
 data=gzip.decompress(p.read_bytes());r=Reader(data)
 assert r.i()==0x304f4f43 and r.i()==8;r.s();r.check('GameSession.Begin');assert r.i()==8
 for _ in range(2):r.s()
 seed=r.i();active=r.s();r.i();r.check('Player');player=r.i();r.check('World');r.i();r.check('ZoneManager');assert r.b();assert r.i()==seed;assert r.s()==active;assert r.b();assert r.i()==seed
 for x in range(20):
  for y in range(20):
   r.i()
   if r.b():r.i();r.s();r.s();r.i();r.s()
 for _ in range(400):r.b()
 zones={};positions=collections.defaultdict(list)
 for _ in range(r.i()):
  z=r.s();assert z==r.s();r.n(28);cells={}
  for x in range(80):
   for y in range(25):
    r.b();r.b();r.b();tokens=[r.i() for _ in range(r.i())];cells[x,y]=tokens
    for t in tokens:positions[t].append((z,x,y))
  zones[z]=cells

 def checkbytes(s):
  v=17
  for ch in s:v=(v*31+ord(ch))&0xffffffff
  return struct.pack('<I',v)
 def dct(r,f):
  n=r.i();assert 0<=n<100000
  out={}
  for _ in range(n):k=r.s();out[k]=f()
  return out
 start=checkbytes('Entity.Begin');end=checkbytes('Entity.End');owners={};pos=0
 while True:
  pos=data.find(start,pos)
  if pos<0:break
  t=struct.unpack('<i',data[pos-4:pos])[0];br=Reader(data[pos:]);br.check('Entity.Begin');eid=br.s();bp=br.s();tags=dct(br,br.s);props=dct(br,br.s);ints=dct(br,br.i);stats={}
  for _ in range(br.i()):
   name=br.s();br.s();br.s();stats[name]=[br.i() for _ in range(6)]
  n=br.i();finish=data.find(end,pos);assert finish>pos;body=data[pos:finish]
  def part(name,schema):
   at=body.find(('CavesOfOoo.Core.'+name+',').encode())
   if at<0:return None
   tail=body.find(b'PublicKeyToken=null',at);assert tail>=0
   pr=Reader(body[tail+len(b'PublicKeyToken=null'):]);fields={}
   for _ in range(pr.i()):
    k=pr.s();typ=schema[k];fields[k]=pr.b() if typ=='b' else pr.i() if typ=='i' else struct.unpack('<f',pr.n(4))[0] if typ=='f' else pr.s()
   return fields
  physics=part('PhysicsPart',{'Solid':'b','Weight':'i','Takeable':'b','ThrowBase':'f','ThrowHeight':'f','ThrowIntercept':'i','Category':'s','InInventory':'i','Equipped':'i'})
  door=part('DoorPart',{'IsOpen':'b','OwnerId':'s','QuarterTurns':'i'})
  assert t not in owners, 'duplicate entity token'
  owners[t]={'id':eid,'bp':bp,'tags':tags,'props':props,'stats':stats,'physics':physics,'door':door,'pos':positions[t],'body':body}
  pos=finish+4
 assert all(t in owners for c in zones.values() for ts in c.values() for t in ts)

 assert len({o['id'] for o in owners.values()}) == len(owners), 'duplicate entity ID'
 return {'owners':owners,'Reader':Reader,'seed':seed,'active':active}

schemas={
 'StackerPart':{'StackCount':'i','MaxStack':'i'},
 'MeleeWeaponPart':{'BaseDamage':'s','PenBonus':'i','HitBonus':'i','MaxStrengthBonus':'i','Stat':'s','Attributes':'s','OnHitEffectsRaw':'s','EmitGasOnHitRaw':'s'},
 'WeaponTemperPart':{'TemperCount':'i','HpPenaltyTotal':'i','AppliedSpecsRaw':'s'},
 'BrewItemPart':{'EffectsRaw':'s','Form':'s'},
 'RenderPart':{**{k:'s' for k in ['DisplayName','RenderString','ColorString','BackgroundColor','GlyphVariants','DetailColor','TileColor','Tile','VisualID','VisualVariant']},'VisualFacing':'i','RenderLayer':'i','Visible':'b'}
}
def reader(m,o,name):
 body=o['body'];at=body.find(('CavesOfOoo.Core.'+name+',').encode())
 if at<0:return None
 tail=body.find(b'PublicKeyToken=null',at);assert tail>=0
 return m['Reader'](body[tail+len(b'PublicKeyToken=null'):])
def part(m,o,name):
 r=reader(m,o,name)
 if r is None:return None
 result={}
 for _ in range(r.i()):
  k=r.s();typ=schemas[name][k];result[k]=r.i() if typ=='i' else r.b() if typ=='b' else r.s()
 return result

def snapshot(path):
 m=load(path);O=m['owners'];ids={o['id']:o for o in O.values()};T={o['id']:t for t,o in O.items()}
 def owner(o):
  obj={k:o[k] for k in ['id','bp','tags','props','stats','pos']};p=o['physics']
  obj['physics']=None if p is None else {**p,'InInventory':O[p['InInventory']]['id']if p['InInventory']else None,'Equipped':O[p['Equipped']]['id']if p['Equipped']else None}
  for n in schemas:
   q=part(m,o,n)
   if q is not None:obj[n]=q
  return obj
 player=ids['2706'];sword=ids['2710'];r=reader(m,player,'InventoryPart');capacity=r.i();carried=[O[r.i()]['id']for _ in range(r.i())];equipped={}
 for _ in range(r.i()):k=r.s();equipped[k]=O[r.i()]['id']
 guard=ids.get('6930')
 snap={'fixture':str(path),'sha256':hashlib.sha256((Path(path)/'Quick.sav.gz').read_bytes()).hexdigest(),'metadata':json.loads((Path(path)/'Quick.json').read_text()),'seed':m['seed'],'active_zone':m['active'],'body_owner_count':len(O),'player':{'id':'2706','pos':player['pos'],'stats':player['stats'],'properties':m['owners'][T['2706']]['props'],'carried_ids':carried,'equipment_slots':equipped,'capacity':capacity},'sword':owner(sword),'resin':owner(ids['13757']) if '13757'in ids else None,'carried':[owner(ids[i])for i in carried],'north_guard':owner(guard)if guard else None,'guard_corpse_or_provenance':[owner(o)for o in O.values()if o['props'].get('SourceID')=='6930'],'all_brew_items':[owner(o)for o in O.values()if part(m,o,'BrewItemPart') is not None]}
 return snap

def audit(before, after, restarted):
 a=after['sword'];b=before['sword'];checks={
  'same_seed':before['seed']==after['seed']==242967504,
  'before_nella_player62_6':before['player']['pos']==[('Overworld.11.8.0',62,6)],
  'after_east_player45_6':after['player']['pos']==[('Overworld.12.10.0',45,6)],
  'after_tick11960_and_hp25_42':after['metadata']['Turn']==11960 and after['player']['stats']['Hitpoints'][0]==25 and after['player']['stats']['Hitpoints'][5]==42,
  'same_sword2710_blueprint':a['id']==b['id']=='2710'and a['bp']==b['bp']=='LongSword',
  'sword_equipped_both_backrefs':a['physics']['Equipped']==b['physics']['Equipped']=='2706'and not a['physics']['InInventory']and not b['physics']['InInventory'],
  'sword_equipped_both_inventory_map':list(before['player']['equipment_slots'].values()).count('2710')==list(after['player']['equipment_slots'].values()).count('2710')==1,
  'sword_not_carried_or_grounded':all('2710'not in s['player']['carried_ids']and not s['sword']['pos']for s in [before,after]),
  'resin_same13757_two_to_one':before['resin']['id']==after['resin']['id']=='13757'and before['resin']['StackerPart']['StackCount']==2 and after['resin']['StackerPart']['StackCount']==1,
  'resin_still_owned':after['resin']['physics']['InInventory']=='2706'and '13757'in after['player']['carried_ids'],
  'no_brew_item_remains_in_after_saved_graph':after['all_brew_items']==[],
  'no_prior_temper':b.get('WeaponTemperPart') is None and b['MeleeWeaponPart']['OnHitEffectsRaw']=='',
  'single_burning_temper':a['WeaponTemperPart']=={'TemperCount':1,'HpPenaltyTotal':2,'AppliedSpecsRaw':'Burning,30,,0,1'} and a['MeleeWeaponPart']['OnHitEffectsRaw']=='Burning,30,,0,1',
  'weapon_durability10_to8':b['stats']['Hitpoints'][0]==b['stats']['Hitpoints'][5]==10 and a['stats']['Hitpoints'][0]==a['stats']['Hitpoints'][5]==8,
  'damage_pen_hit_unchanged':all(a['MeleeWeaponPart'][k]==b['MeleeWeaponPart'][k] for k in ['BaseDamage','PenBonus','HitBonus','MaxStrengthBonus','Stat','Attributes','EmitGasOnHitRaw']),
  'flame_quenched_name':a['RenderPart']['DisplayName']=='flame-quenched long sword',
  'guard_before_alive20':before['north_guard']['stats']['Hitpoints'][0]==20 and before['north_guard']['pos']==[('Overworld.11.9.0',64,3)],
  'guard_after_removed_or_dead':after['north_guard'] is None or not after['north_guard']['pos']and('_DeathHandled'in after['north_guard']['tags']or after['north_guard']['stats']['Hitpoints'][0]<=0),
  'guard_corpse_provenance':any(o['props'].get('SourceBlueprint')=='MarlbackPatchbearer'and o['props'].get('KillerID')=='2706'for o in after['guard_corpse_or_provenance'])
 }
 checks.update({
  'restart_same_game_seed':after['metadata']['GameID']==restarted['metadata']['GameID']==before['metadata']['GameID'] and after['seed']==restarted['seed']==before['seed'],
  'restart_same_tick_hp_position':after['metadata']['Turn']==restarted['metadata']['Turn']==11960 and after['active_zone']==restarted['active_zone']=='Overworld.12.10.0' and after['player']['pos']==restarted['player']['pos'] and after['player']['stats']['Hitpoints']==restarted['player']['stats']['Hitpoints'],
  'restart_same_sword_all_audited_fields':after['sword']==restarted['sword'],
  'restart_same_inventory_and_equipment':after['player']==restarted['player'] and after['carried']==restarted['carried'],
  'restart_same_resin1':after['resin']==restarted['resin'] and restarted['resin']['StackerPart']['StackCount']==1,
  'restart_coating_still_absent':not restarted['all_brew_items'],
  'restart_guard_corpse_preserved':after['north_guard']==restarted['north_guard'] and after['guard_corpse_or_provenance']==restarted['guard_corpse_or_provenance']
 })
 assert len(checks)==26
 return checks

VISUAL_CONCLUSION = (
 "Resolved by independent source and screenshot review: earned-resin-17-coating-details.jpg "
 "shows approved native north-works-products frame5. BrewedTonic with canonical Coating "
 "intentionally shares the neutral flask; no missing asset mapping or fallback defect."
)

def main():
 cli=argparse.ArgumentParser(description=__doc__)
 cli.add_argument('--baseline',required=True,type=Path)
 cli.add_argument('--before-restart',required=True,type=Path)
 cli.add_argument('--after-restart',required=True,type=Path)
 cli.add_argument('--output',required=True,type=Path)
 args=cli.parse_args()
 directories=[p.resolve(strict=True) for p in (args.baseline,args.before_restart,args.after_restart)]
 inputs=[p/n for p in directories for n in ('Quick.json','Quick.sav.gz')]
 for p in inputs:
  if not p.is_file():cli.error('Missing regular input file: '+str(p))
 output=args.output.resolve()
 if any(output==p or p in output.parents for p in directories):
  cli.error('Output must be outside all input checkpoint directories.')
 if output.exists() or args.output.is_symlink():
  cli.error('Output already exists; refusing to overwrite any file.')
 if not output.parent.is_dir():cli.error('Output parent must already exist.')
 hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in inputs}
 before,after,restarted=[snapshot(p) for p in directories]
 checks=audit(before,after,restarted)
 unchanged={str(p):hashlib.sha256(p.read_bytes()).hexdigest()==hashes[str(p)] for p in inputs}
 if not all(unchanged.values()):raise RuntimeError('An input changed during the audit; no report written.')
 report={
  'kind':'read_only_earned_temper_saved_state_audit',
  'recorded_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
  'method':'Read gzip format8 bytes, check header/zone/cell tokens and exact selected public-field schemas. No gameplay loading/hooks. Input files hashed before and after. Output created exclusively.',
  'bounds':[
   'Fixture-specific expected seed242967504 and owner IDs2706/2710/13757/6930; unsupported or malformed inputs raise rather than pass.',
   'Checks serialized endpoint records, not exact key timing or screenshot pixels.',
   'No intermediate coating checkpoint: resin decrement, no remaining BrewItem and final authoritative temper payload support consumption, but do not identify the consumed coating ID.',
   'A Burning weapon spec does not establish an actual on-hit proc.',
   'Restart equality covers emitted player, sword, carried item, resin and guard-corpse fields, not every field/part in the whole save.',
   VISUAL_CONCLUSION
  ],
  'checks':checks,'all_checks_pass':all(checks.values()),
  'input_files_unchanged':unchanged,'input_sha256':hashes,
  'before':before,'before_restart':after,'after_restart':restarted,
  'summary':{'checks_passed':sum(checks.values()),'checks_total':len(checks),'visual_followup':VISUAL_CONCLUSION}
 }
 with args.output.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
 print(json.dumps({'report':str(output),'checks_passed':sum(checks.values()),'checks_total':len(checks),'all_checks_pass':all(checks.values()),'input_files_unchanged':all(unchanged.values())}))
 return 0 if all(checks.values()) else 1

if __name__=='__main__':
 sys.exit(main())
