"""Source-authored missing portable forms. Uses borrowed mesh-kit primitives,
not its models; writes no shared assets. Coordinates are Blender Z-up."""
def build(row,g):
    box,beam,ellipsoid,cylinder,torus,newmesh=[g[k] for k in ('box','beam','ellipsoid','cylinder','torus','newmesh')]
    n,f=row['blueprint'],row['form']
    def B(label,p,size,color):return box(label,p,size,color,0)
    def shaft(a,b,r=.028,c='wood_dark'):return beam('Haft',a,b,r,c,6)
    def blade(length=.65,width=.075):
        v=[(-width,-.22,.045),(width,-.22,.045),(-width,.3,.045),(width,.3,.045),(0,length-.22,.045),
           (-width,-.22,.075),(width,-.22,.075),(-width,.3,.075),(width,.3,.075),(0,length-.22,.075)]
        return newmesh('Hardened_blade',v,[(0,2,4,3,1),(5,6,8,9,7),(0,1,6,5),(0,5,7,2),(2,7,9,4),(4,9,8,3),(3,8,6,1)],'steel')
    if f in ('codex','pale-register'):
        B('Paper_block',(0,0,.085),(.35,.46,.12),'paper')
        for z in (.02,.155):B('Bound_cover',(0,0,z),(.4,.51,.035),'cream' if f=='pale-register' else 'book_blue')
        B('Spine',(-.2,0,.087),(.05,.51,.155),'leather')
        if f=='pale-register':
            B('Tied_slate',(.03,0,.198),(.255,.24,.035),'stone_dark')
            for y in (-.08,.08):B('Slate_cord',(.03,y,.223),(.29,.023,.015),'rope')
        else:
            number=int(n[-2:]);B('Title_plaque',(.035,.13,.18),(.21,.085,.018),'paper')
            for i in range(4):
                if number&(1<<i):B('Spine_band_'+str(i),(-.2,-.15+i*.1,.11),(.065,.028,.17),'gold')
    elif f=='sealed-writ':
        B('Folded_sheet',(0,0,.028),(.38,.48,.046),'paper');B('Fold',(0,.04,.058),(.375,.055,.02),'cream')
        cylinder('Wax_seal',(.105,-.105,.07),.05,.026,'red',8)
        for y in (.12,.16,.20):B('Ink_strokes',(-.025,y,.057),(.23,.013,.01),'book_blue')
    elif f in ('iron-key','tagged-key'):
        torus('Worn_bow',(0,-.20,.055),.09,.022,'iron_light',8,4);shaft((0,-.12,.055),(0,.29,.055),.025,'iron')
        for y,w in ((.20,.075),(.28,.105)):B('Key_tooth',(w*.45,y,.055),(w,.037,.055),'iron_light')
        if f=='tagged-key':
            shaft((.075,-.20,.065),(.17,-.10,.065),.009,'rope');B('File_tag',(.185,-.06,.047),(.13,.19,.025),'paper')
    elif f=='wedge-cleaver':
        shaft((.035,-.37,.06),(.025,.29,.06),.035)
        v=[(.025,.10,.04),(-.27,.045,.04),(-.31,.34,.04),(.025,.28,.04),(.025,.10,.125),(-.27,.045,.065),(-.31,.34,.065),(.025,.28,.125)]
        newmesh('Salvaged_wedge',v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'steel')
        for y in (.10,.16,.22):B('Grit_lashing',(.03,y,.115),(.085,.032,.045),'rope')
    elif f=='tempered-sword':
        blade(.68,.069);B('Guard',(0,-.245,.063),(.25,.04,.075),'iron');shaft((0,-.26,.06),(0,-.44,.06),.027)
        B('Peened_pommel',(0,-.445,.06),(.07,.043,.06),'iron_light')
        B('Hardened_edge',(-.063,.02,.08),(.014,.48,.015),'cream')
    elif f=='counterweight-maul':
        shaft((0,-.38,.06),(0,.28,.06),.035);B('Striking_head',(0,.25,.105),(.39,.21,.18),'iron')
        for x in (-.17,.17):B('Striking_face',(x,.25,.11),(.06,.225,.19),'steel')
        B('Lower_counterweight',(0,-.34,.085),(.15,.10,.11),'iron_light')
    elif f in ('fine-mail','riveted-plate'):
        B('Folded_body',(0,0,.075),(.39,.46,.12),'iron')
        for x in (-.22,.22):B('Folded_sleeve',(x,.075,.075),(.12,.24,.11),'leather' if f=='riveted-plate' else 'iron')
        if f=='fine-mail':
            for x in range(6):
                for y in range(7):torus('Small_link',((x-2.5)*.055,(y-3)*.055,.15),.021,.0065,'steel',6,4)
        else:
            for y in range(3):
                B('Overlapping_plate',(0,(y-1)*.14,.15),(.355,.135,.045),'steel')
                for x in (-.13,.13):B('Broad_rivet',(x,(y-1)*.14,.18),(.038,.037,.027),'iron_light')
            for x in (-.12,.12):B('Shoulder_strap',(x,.245,.10),(.055,.10,.045),'leather')
    elif f=='gourd':
        ellipsoid('Gourd_body',(0,0,.17),(.18,.16,.16),'wood_end',2);cylinder('Gourd_neck',(0,0,.355),.055,.19,'wood_light',8)
        cylinder('Gourd_cork',(0,0,.455),.038,.045,'wood_dark',8);torus('Cord_neck',(0,0,.405),.063,.012,'rope',8,4)
    elif f=='waterskin':
        ellipsoid('Leather_pouch',(0,0,.085),(.21,.285,.075),'leather',2);B('Stitched_edge',(.18,.015,.1),(.025,.37,.025),'rope')
        cylinder('Stoppered_neck',(0,.27,.11),.05,.085,'wood_dark',8)
        for i in range(7):B('Stitch',(.185,-.15+i*.05,.12),(.043,.014,.014),'cream')
    elif f=='flask':
        ellipsoid('Thick_glass',(0,0,.175),(.16,.14,.16),'water_light',2);cylinder('Narrow_neck',(0,0,.345),.055,.145,'water_light',8)
        cylinder('Stopper',(0,0,.429),.044,.051,'wood_end',8);B('Glass_highlight',(-.07,-.13,.2),(.035,.02,.15),'cream')
    elif f=='roasted-mushroom':
        cylinder('Stem',(0,0,.12),.058,.22,'cream',8);ellipsoid('Roasted_cap',(0,0,.265),(.21,.18,.087),'terracotta',2)
        for x,y in ((-.09,0),(.02,-.05),(.08,.05)):B('Toasted_mark',(x,y,.336),(.075,.037,.025),'wood_dark')
    elif f in ('engraved-stone-chip','pale-stone'):
        ellipsoid('Pale_stone',(0,0,.095),(.235,.19,.085),'stone_light' if f=='pale-stone' else 'cream',1)
        if f=='engraved-stone-chip':
            for y in (-.06,0,.06):B('Incised_line',(.01,y,.176),(.24,.012,.01),'stone_dark')
    elif f=='living-thread':
        points=[(-.19,-.13,.044),(-.21,.12,.055),(-.08,.21,.048),(.16,.16,.068),(.20,-.03,.055),(.08,-.15,.05),(-.035,-.05,.068),(.045,.065,.075)]
        for a,b in zip(points,points[1:]):shaft(a,b,.025,'leaf_light')
        shaft(points[-1],(.105,.14,.09),.018,'leaf');shaft(points[-1],(.0,.17,.09),.017,'leaf_light')
    elif f=='sealed-body':
        ellipsoid('Waxed_wrapping',(0,0,.12),(.22,.425,.115),'cream',2)
        for y in (-.29,-.12,.08,.27):B('Binding_cord',(0,y,.165),(.425,.027,.09),'rope')
        B('Intake_label',(.055,-.08,.24),(.14,.16,.014),'paper');cylinder('Sealed_stamp',(.065,-.105,.252),.035,.015,'red',6)
    elif f in ('marlback-corpse','corpse-provenance'):
        # Species-specific selector variants are authored separately; this model
        # is only the native Marlback or unnamed base geometry study.
        ellipsoid('Low_fallen_body',(0,0,.115),(.30,.19,.095),'wood' if f=='marlback-corpse' else 'cloth_shadow',1)
        ellipsoid('Short_face',(.31,.04,.095),(.105,.105,.075),'wood_end',1)
        for x,y in ((-.19,-.17),(.12,-.16),(-.20,.17),(.09,.18)):shaft((x*.7,y*.5,.075),(x,y,.04),.045,'wood_dark')
        if f=='marlback-corpse':
            for x in (-.17,-.055,.06,.175):B('Shale_plate',(x,.045,.22),(.10,.22,.04),'stone_dark')
    else:raise ValueError((n,f))
