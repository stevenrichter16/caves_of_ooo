"""Grounded portable states. Anatomy comes from the reviewed source-family map;
none of these models is an animated replacement creature."""
def build(row,g):
 box,beam,ellipsoid,cylinder,torus,newmesh=[g[k] for k in ('box','beam','ellipsoid','cylinder','torus','newmesh')]
 f=row['form'];B=lambda name,p,s,c:box(name,p,s,c,0)
 def E(name,p,s,c):return ellipsoid(name,p,s,c,1)
 def line(name,a,b,r,c):return beam(name,a,b,r,c,6)
 def body(color='wood',size=(.26,.13,.10)):E('Fallen_body',(0,0,.12),size,color)
 def limbs(count=4,color='wood_dark'):
  for i in range(count):
   x=(i//2-(count//2-1)*.5)*.20;y=-1 if i%2==0 else 1
   line('Folded_limb',(x,y*.075,.10),(x+.05,y*.235,.035),.035,color)
 def humanoid(color='cloth_shadow',head='skin',size=1):
  E('Torso',(0,.0,.085),(.14*size,.22*size,.075),color);E('Head',(0,.285*size,.085),(.085*size,.085*size,.07),head)
  for x in (-.085,.085):
   line('Arm',(x*1.7,.12,.065),(x*2.35,-.075,.035),.034,color)
   line('Leg',(x,-.14,.065),(x*1.35,-.38*size,.035),.040,color)
 if f.startswith('corpse-'):
  family=f[7:]
  if family in ['humanoid','ape','stalker','charred-humanoid','troll','skeleton','construct','fungal']:
   color={'ape':'wood_dark','stalker':'cream','charred-humanoid':'black','troll':'moss','skeleton':'cream','construct':'stone','fungal':'moss_dark'}.get(family,'cloth_shadow')
   humanoid(color, 'cream' if family=='skeleton' else color if family!='humanoid' else 'skin',1.06 if family in ['troll','construct','ape'] else 1)
   if family=='fungal':
    for x,y in [(-.09,-.06),(.06,.02),(-.045,.18)]:E('Fungal_cap',(x,y,.19),(.08,.07,.035),'moss_light')
   if family=='skeleton':
    for y in [-.09,-.03,.03,.09]:B('Rib',(0,y,.155),(.25,.027,.021),'cream')
   if family=='construct':
    for y in [-.09,.03,.13]:B('Broken_plate',(0,y,.15),(.25,.08,.04),'iron_light')
  elif family in ['serpent','plant']:
   ps=[(-.35,-.08,.045),(-.21,.10,.045),(-.07,.14,.065),(.1,-.11,.05),(.28,-.08,.055),(.36,.075,.065)]
   for a,b in zip(ps,ps[1:]):line('Coiled_segment',a,b,.052,'leaf_dark' if family=='plant' else 'stone_warm')
   if family=='serpent':E('Blunt_snake_head',ps[-1],(.09,.055,.04),'stone')
   else:
    for x in [-.21,.10,.28]:line('Broken_root',(x,0,.06),(x+.075,-.22,.035),.021,'leaf')
  elif family=='frog':
   body('moss',(.18,.14,.10));E('Broad_head',(.17,0,.09),(.105,.145,.075),'moss_light')
   for y in [-.20,.20]:E('Folded_hindleg',(-.19,y*.6,.07),(.11,.07,.055),'leaf_dark');line('Toes',(-.19,y*.6,.05),(-.27,y,.03),.025,'moss_light')
   for y in [-.16,.16]:line('Foreleg',(.13,y*.4,.06),(.23,y,.028),.025,'moss_light')
  elif family in ['quadruped','lizard','tortoise','ambush-maw']:
   body('wood' if family=='quadruped' else 'moss_dark');limbs()
   E('Low_head',(.28,0,.09),(.105,.085,.075),'wood_end' if family=='quadruped' else 'stone')
   if family=='lizard':line('Long_tail',(-.22,0,.10),(-.46,.07,.03),.040,'moss')
   if family=='quadruped':
    for y in [-.06,.06]:B('Folded_ear',(.28,y,.17),(.043,.027,.06),'wood_dark')
   if family=='tortoise':E('Domed_shell',(-.035,0,.165),(.26,.21,.135),'stone_warm')
   if family=='ambush-maw':
    B('Maw',(.36,0,.07),(.14,.21,.10),'black')
    for y in [-.065,0,.065]:B('Tooth',(.41,y,.10),(.022,.022,.07),'cream')
  elif family in ['spider','scorpion']:
   body('stone_dark',(.17,.105,.07));limbs(8,'stone_dark');E('Head',(.21,0,.085),(.08,.09,.055),'iron')
   if family=='scorpion':
    for y in [-.2,.2]:line('Pincer',(.17,y*.5,.06),(.31,y,.04),.04,'stone');E('Pincer_tip',(.31,y,.045),(.065,.04,.03),'stone_light')
    line('Fallen_tail',(-.17,0,.09),(-.34,-.06,.06),.043,'stone');line('Sting',(-.34,-.06,.06),(-.38,-.18,.04),.026,'stone_dark')
  elif family in ['avian','bat','moth']:
   body('wood_dark',(.09,.18,.065));E('Small_head',(0,.21,.09),(.065,.065,.06),'wood_end')
   if family=='avian':line('Beak',(0,.25,.09),(0,.35,.065),.032,'gold')
   for side in [-1,1]:
    v=[(side*.07,.09,.08),(side*.42,.21,.05),(side*.34,-.2,.025),(side*.08,-.13,.07)]
    color='cloth_shadow' if family=='bat' else 'water_light' if family=='moth' else 'cream'
    newmesh('Folded_wing',v+[(x,y,z+.028) for x,y,z in v],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],color)
    if family=='moth':
     for y in [-.09,.005,.10]:B('Wing_dust_bar',(side*.255,y,.092),(.15,.025,.015),'cream')
  elif family=='slime':
   E('Collapsed_slime',(0,0,.06),(.28,.23,.055),'teal');E('Slime_lobe',(.18,.12,.036),(.13,.11,.03),'water_light')
  elif family=='mimic':
   B('Broken_chest',(0,0,.12),(.42,.30,.22),'wood_dark');B('Fallen_lid',(0,.23,.055),(.43,.17,.08),'wood')
   for y in [-.125,.125]:B('Tooth_row',(0,y,.242),(.32,.022,.026),'cream')
  else:raise ValueError(family)
 elif f=='limb':
  cat=row['category'];part=row['partType'];color={1:'skin',2:'stone',3:'leaf',4:'moss',5:'teal',6:'iron_light',7:'iron',8:'steel',9:'wood',10:'stone_dark',11:'water_light',12:'leather',13:'cream',14:'stone_warm',15:'red',16:'cloth_light',17:'violet',18:'hood_violet',19:'terracotta',20:'water',21:'teal'}[cat]
  if part=='head':E('Head_piece',(0,0,.09),(.12,.13,.085),color)
  elif part=='body':E('Body_piece',(0,0,.07),(.17,.23,.065),color)
  elif part in ['arm','tail','tendril']:
   line('Segment',(0,-.23,.05),(.06,.03,.085),.055 if part=='arm' else .035,color);line('Fold',(.06,.03,.085),(-.035,.21,.04),.035 if part=='arm' else .023,color)
  elif part in ['hand','feet']:
   B('Palm_or_foot',(0,0,.045),(.13,.16 if part=='hand' else .24,.075),color)
   for x in [-.05,0,.05]:line('Digits',(x,.04,.05),(x*1.5,.17,.032),.022,color)
  elif part=='wing':
   newmesh('Wing_fragment',[(-.2,-.14,.03),(.23,-.13,.03),(.09,.21,.03),(-.2,-.14,.06),(.23,-.13,.06),(.09,.21,.06)],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],color)
  else:raise ValueError(part)
 elif f=='witness-book':
  B('Witness_pages',(0,0,.06),(.32,.42,.10),'paper')
  for z in [.01,.12]:B('Worn_cover',(0,0,z),(.36,.46,.025),'leather')
  for y in [-.13,-.05,.03,.11]:B('Binding_stitch',(-.17,y,.08),(.025,.025,.13),'rope')
 elif f=='name-token':
  B('Carved_wood',(0,0,.045),(.27,.33,.07),'wood_light');torus('Cord',(0,.15,.06),.11,.014,'rope',10,4)
  for y in [-.07,0,.07]:B('Carved_name_mark',(0,y,.084),(.15,.013,.01),'wood_dark')
 elif f.startswith('brew-'):
  kind=f[5:]
  if kind=='food':B('Wrapped_mash',(0,0,.08),(.31,.29,.13),'leaf');B('Tied_leaf',(0,0,.16),(.035,.30,.025),'rope')
  else:
   cylinder('Brew_container',(0,0,.15),.13 if kind=='coating' else .11,.28,'terracotta' if kind=='coating' else 'water_light',8)
   cylinder('Stopper',(0,0,.31),.08,.06,'wood',8)
   if kind=='throwable':line('Fuse',(0,0,.34),(.1,.0,.40),.017,'rope')
 elif f=='forged':
  haft='wood' if row['haft']=='oak' else 'wood_light';line('Actual_haft',(0,-.42,.06),(0,.22,.06),.03,haft)
  if row['blade']=='blade':B('Actual_blade',(0,.225,.075),(.15,.35,.06),'steel')
  else:newmesh('Iron_spike',[(-.055,.01,.045),(.055,.01,.045),(0,.44,.065),(0,.01,.105)],[(0,2,1),(0,3,2),(1,2,3),(0,1,3)],'iron')
  for y in [-.01,.055,.115]:B('Binding',(0,y,.08),(.084,.025,.074),'leather')
  if row['binding']=='serrated':
   for y in [.12,.20,.28,.36]:B('Serration',(-.082,y,.078),(.05,.04,.06),'iron_light')
 elif f=='filled-flask':
  # Borrow the base thick glass envelope, add a visible front contents swatch.
  E('Glass_envelope',(0,0,.175),(.16,.14,.16),'water_light');cylinder('Neck',(0,0,.345),.055,.145,'water_light',8);cylinder('Stopper',(0,0,.429),.044,.051,'wood_end',8)
  B('Actual_contents',(0,-.132,.165),(.19,.035,.15),'water')
  B('Glass_glint',(-.105,-.135,.20),(.023,.02,.15),'cream')
 else:raise ValueError(f)
