"""Original connected Spread objects; Unity imports these exact cuboids."""
import json
from copy import deepcopy
from pathlib import Path
ROOT=Path(__file__).parent
PALETTE=json.loads((ROOT.parent/'CurationYard3D'/'kit.json').read_text())['palette']
def box(name,c,s,color): return dict(name=name,center=dict(zip('xyz',c)),size=dict(zip('xyz',s)),color=color)
models=[]
def model(name,boxes): models.append(dict(id='connected-spread-'+name,boxes=boxes))
def parcel(prefix='',y=0):
 return [box(prefix+'parcel',(0,y+.085,0),(.46,.17,.32),5),box(prefix+'fold-left',(-.16,y+.185,0),(.115,.035,.30),17),box(prefix+'fold-right',(.16,y+.185,0),(.115,.035,.30),17),box(prefix+'cord-long',(0,y+.205,0),(.027,.027,.36),19),box(prefix+'cord-wide',(0,y+.21,0),(.49,.024,.025),19),box(prefix+'knot',(.025,y+.241,.02),(.075,.04,.065),22)]
base=[box('clay-base',(0,.10,0),(.80,.20,.70),18),box('base-course',(0,.21,0),(.74,.07,.64),20),box('black-pan',(0,.27,0),(.70,.08,.56),19),box('inner-lining',(0,.319,0),(.57,.03,.42),5),box('back-rim',(0,.355,.255),(.75,.12,.07),18),box('left-rim',(-.345,.355,0),(.07,.12,.49),18),box('right-rim',(.345,.355,0),(.07,.12,.49),18),box('front-rim',(0,.355,-.255),(.75,.12,.07),18)]
cracked=deepcopy(base);cracked=[b for b in cracked if b['name']!='front-rim']
cracked += [box('split-rim-left',(-.235,.341,-.258),(.25,.105,.07),18),box('split-rim-right',(.20,.357,-.255),(.30,.12,.07),18),box('exposed-dark-seam',(-.025,.315,-.15),(.055,.013,.25),19),box('fallen-chip',(-.21,.026,-.425),(.13,.052,.09),18)]
model('pan-cracked',cracked);model('pan-empty',deepcopy(base))
model('pan-covered',deepcopy(base)+[box('cover',(0,.427,0),(.72,.09,.55),11),box('cover-lip',(0,.396,-.28),(.62,.035,.035),12),box('cover-handle',(0,.526,0),(.19,.11,.055),19),box('cloth-corner',(.275,.402,-.31),(.11,.045,.09),17)])
model('pan-ready',deepcopy(base)+[box('cover-set-back',(0,.45,.22),(.65,.08,.26),11),box('cover-handle',(0,.53,.22),(.18,.09,.05),19),box('folded-wrapper',(-.12,.364,-.035),(.27,.07,.28),17),box('wrapper-crease',(-.12,.405,-.035),(.025,.014,.27),20)])
pantry=[box('plinth',(0,.07,0),(.78,.14,.69),20),box('back',(0,.42,.29),(.72,.63,.09),18),box('left-side',(-.32,.38,0),(.08,.56,.61),18),box('right-side',(.32,.38,0),(.08,.56,.61),18),box('shelf',(0,.26,0),(.61,.055,.59),5),box('top',(0,.75,.245),(.79,.09,.17),18),box('front-lintel',(0,.60,-.295),(.72,.055,.07),20)]
model('pantry-empty',deepcopy(pantry));model('pantry-full',deepcopy(pantry)+[box('grain-sack',(-.12,.402,-.015),(.25,.23,.27),5),box('sack-neck',(-.12,.55,-.015),(.11,.09,.12),22),box('sack-tie',(-.12,.552,-.015),(.14,.03,.14),19),box('pulp-pot',(.18,.37,-.02),(.16,.16,.21),18),box('pulp',( .18,.46,-.02),(.115,.025,.16),9)])
tray=[box('left-foot',(-.29,.18,0),(.075,.36,.51),20),box('right-foot',(.29,.18,0),(.075,.36,.51),20),box('tray',(0,.38,0),(.76,.08,.58),5),box('back-edge',(0,.465,.255),(.78,.10,.055),18),box('left-edge',(-.36,.465,0),(.055,.10,.54),18),box('right-edge',(.36,.465,0),(.055,.10,.54),18),box('front-lip',(0,.437,-.255),(.75,.045,.055),18)]
model('pickup-empty',deepcopy(tray));model('pickup-full',deepcopy(tray)+parcel('ready-',.425))
model('reserve-tray',[box('tray',(0,.09,0),(.79,.18,.65),20),box('hollow',(0,.189,0),(.63,.02,.49),19),box('back-rim',(0,.25,.295),(.81,.15,.055),18),box('front-rim',(0,.25,-.295),(.81,.15,.055),18),box('left-rim',(-.375,.25,0),(.055,.15,.57),18),box('right-rim',(.375,.25,0),(.055,.15,.57),18),box('left-stake',(-.32,.51,.22),(.065,.71,.065),18),box('right-stake',(.32,.51,.22),(.065,.71,.065),18),box('tie-line',(0,.72,.22),(.70,.026,.025),17),box('cloth-tie',(-.235,.68,.21),(.12,.14,.036),17),box('seed-label',(.19,.55,.21),(.18,.11,.06),5)])
model('field-meal',parcel())
model('ink-desk',[*[box('leg-'+str(i),(x,.31,z),(.08,.62,.08),20) for i,(x,z) in enumerate([(-.34,-.25),(.34,-.25),(-.34,.25),(.34,.25)])],box('desktop',(0,.64,0),(.88,.09,.71),5),box('grinding-bowl',(-.22,.743,-.11),(.26,.12,.25),11),box('soot-root',(-.22,.806,-.11),(.16,.035,.15),19),box('pestle',(-.24,.917,-.08),(.068,.22,.068),17),box('ink-bottle',(.24,.79,.13),(.14,.20,.14),0),box('bottle-neck',(.24,.933,.13),(.07,.085,.07),11),box('resin-cup',(.08,.74,-.15),(.11,.12,.12),18),box('resin',(.08,.81,-.15),(.085,.027,.09),22),box('label',(-.08,.713,.19),(.30,.033,.15),17),box('label-line',(-.08,.733,.19),(.20,.014,.023),19)])
model('footwork-manual',[box('cover',(0,.026,0),(.43,.052,.58),20),box('pages',(.015,.063,0),(.37,.026,.51),17),box('upper-cover',(0,.087,0),(.43,.026,.58),5),box('spine',(-.19,.055,0),(.055,.08,.60),18),box('footstep-1',(-.09,.106,-.16),(.057,.014,.092),19),box('footstep-2',(.08,.106,-.03),(.057,.014,.092),19),box('footstep-3',(-.04,.106,.12),(.057,.014,.092),19),box('route-line',(.055,.105,.17),(.17,.013,.020),11)])
model('heavy-frame',[box('left-sledge',(-.34,.08,0),(.23,.16,.96),20),box('right-sledge',(.34,.08,0),(.23,.16,.96),20),box('left-upright',(-.32,.76,0),(.19,1.25,.20),18),box('right-upright',(.32,.76,0),(.19,1.25,.20),18),box('cross-head',(0,1.405,0),(.92,.19,.27),20),box('iron-strap-left',(-.32,.40,0),(.215,.07,.225),11),box('iron-strap-right',(.32,.40,0),(.215,.07,.225),11),box('upper-brace-left',(-.22,1.20,0),(.10,.20,.23),20),box('upper-brace-right',(.22,1.20,0),(.10,.20,.23),20),box('pulley-block',(0,1.19,0),(.17,.23,.17),11),box('hanging-rope',(0,.835,0),(.034,.54,.034),5),box('hook-stem',(0,.53,0),(.045,.12,.045),11),box('hook-tooth',(.045,.486,0),(.12,.035,.045),11),box('foot-bolt-left',(-.34,.173,-.31),(.07,.03,.07),12),box('foot-bolt-right',(.34,.173,-.31),(.07,.03,.07),12)])
model('reserve-bed',[
 *[box('furrow-'+str(i),(-.32+i*.16,.018,0),(.085,.036,.78),20) for i in range(5)],
 *[box('corner-stake-'+str(i),(x,.16,z),(.05,.32,.05),18) for i,(x,z) in enumerate([(-.44,-.44),(.44,-.44),(-.44,.44),(.44,.44)])],
 box('front-cord',(0,.215,-.44),(.9,.018,.018),17),box('back-cord',(0,.215,.44),(.9,.018,.018),17),
 box('left-cord',(-.44,.215,0),(.018,.018,.9),17),box('right-cord',(.44,.215,0),(.018,.018,.9),17),
 box('cloth-knot',(-.39,.21,-.44),(.085,.08,.036),17)])
(ROOT/'kit.json').write_text(json.dumps(dict(schemaVersion=1,id='connected-spread-original',palette=PALETTE,models=models),indent=2)+'\n')
