"""Explicit item-form choices. Unknowns stay visible in the design backlog."""
FORMS={}
def group(form,names):
 for name in names.split():
  if name in FORMS:raise ValueError(name)
  FORMS[name]=form
# Weapon types are shape decisions, not random geometric variants.
group('knife','Dagger LoanerDagger AcidicDagger VenomDagger EchoKnife GlassblownStiletto')
group('sword','LongSword ShortSword LoanerLongsword Greatsword Claymore FlamingSword IceSword Sporeblade SeveranceEdge PalimpsestBlade ForgedWeapon')
group('axe','Battleaxe Hatchet WarlordCleaver')
group('polearm','Spear LoanerSpear CryoLance EmberSpear FirstRootGlaive')
group('hammer','Warhammer ThunderHammer DissolutionMaul')
group('club','Mace Cudgel ChoirSpine OldWorldPipe')
group('shard','TemporalShard')
group('shield','Buckler IronBuckler')
group('helmet','IronHelmet LeatherCap')
group('boots','LeatherBoots IronshodBoots')
group('gloves','LeatherGloves')
group('coat','LeatherArmor ChainMail PlateArmor')
group('cloak','Cloak WardedCloak')
group('vial','InkVial LampOil GlimmerBrine BogSap LanternOil WardOil')
group('root','CandyHeartRoot SparkRoot CandyCarrot SaltbriarSprig')
group('herb','FireMoss FrostLichen MendleafSprig GroveRed')
group('crystal','GlacierSalt PaleSalt ChoirIron GlowQuartz Tepuibone SilverSand FireClay')
group('fruit','Starapple EmberFruit RoastedStarapple WildBerries')
group('seed','StoneburrSeed BlastcapSpore CandyCarrotSeed EmberwheatSeed')
group('sac','VenomGland ShamblerSporeSac InertSludge')
group('meat','RawMeat CookedMeat DriedMeat')
group('mushroom','Mushroom')
group('honeycomb','Honeycomb')
group('sheaf','Emberwheat')
group('bone','Bone')
group('coin','GoldCoin')
group('torch','Torch')
group('doll','WovenDoll')
group('blade-component','SteelBladeComponent SerratedEdgeComponent IronSpikeComponent')
group('haft-component','OakHaftComponent WillowHaftComponent')
group('binding','LeatherBindingComponent')
BASES={'Item','MeleeWeapon','FoodItem','TonicItem','ArmorItem','WeaponComponentItem','ReagentItem'}
def form(row):
 n=row['blueprint']
 if n in BASES:return None
 if n in FORMS:return FORMS[n]
 if 'Tonic' in row['parts']:return 'tonic'
 if n.endswith('Grenade'):return 'grenade'
 if 'Grimoire' in n or n.endswith(('Manual','Guide','Recipe')) or n.startswith('Schematic'):return 'book'
 return None

def element(row):
 n=row['blueprint']
 for names,typ in [('Fire Flaming Ember Kindle Conflagration Burn Hearth Charred','fire'),('Ice Cryo Frost Rime Chill Glacier Quench','ice'),('Acid Venom Poison Verdigris Bog','acid'),('Thunder Lightning Arc Storm Spark Stun Fulmination','storm'),('Water Rain Brine Scald Steam','water'),('Blood Bleed Raw','blood'),('Calm Sleep StillHeart Echo Palimpsest','mind'),('Healing Mend Antidote Panacea Grove','life')]:
  if any(x in n for x in names.split()):return typ
 return 'plain'

def designs(rows):
 return [{'blueprint':r['blueprint'],'displayName':r['displayName'],'form':form(r),'element':element(r),'material':r['parts'].get('Material',{}).get('MaterialID',''),'description':r['description'],'status':'design-ready' if form(r) else 'base-or-unresolved'} for r in rows if r['category']=='item']
if __name__=='__main__':
 import json
 from pathlib import Path
 p=Path(__file__).parent;d=designs(json.loads((p/'coverage.json').read_text())['blueprints']);(p/'item-designs.json').write_text(json.dumps(d,indent=2)+'\n');print('Designs',sum(x['form'] is not None for x in d),'unresolved',[x['blueprint'] for x in d if not x['form'] and x['blueprint'] not in BASES])
