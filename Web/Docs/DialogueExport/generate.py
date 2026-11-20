from pathlib import Path
import json,re,html,collections,hashlib
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle,getSampleStyleSheet
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import BaseDocTemplate,PageTemplate,Frame,Paragraph,Spacer,PageBreak,KeepTogether
from reportlab.platypus.tableofcontents import TableOfContents
from docx import Document
from docx.shared import Pt,Cm,RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];OUT=ROOT/'Client/wwwroot/downloads';OUT.mkdir(parents=True,exist_ok=True)
CORE=ROOT/'Core/Content'
plot=json.loads((HERE/'plot-order.json').read_text());mapping=json.loads((HERE/'coverage-mapping.json').read_text())
runtime=json.loads((HERE/'runtime-dialogue.json').read_text());extras={x['id']:x for x in json.loads((HERE/'supplemental-narrative.json').read_text())['items']}
convs={};nodes={};node_anchor={};names={};locations={};sources={}
for p in sorted((CORE/'Conversations').glob('*.json')):
 for c in json.loads(p.read_text())['Conversations']:
  assert c['ID'] not in convs;c['_source']='Assets/Resources/Content/Conversations/'+p.name;convs[c['ID']]=c
  for n in c['Nodes']:nodes[(c['ID'],n['ID'])]=n
for x in mapping['blueprint_conversations']:names.setdefault(x['conversation'],x.get('display_name') or x['blueprint'])
for x in mapping.get('dynamic_speakers',[]):names[x['conversation_id']]=x['display_name'];locations[x['conversation_id']]=x.get('place','')
quest_names={}
for p in (CORE/'Data/Storylets').glob('*.json'):
 for s in json.loads(p.read_text()).get('Storylets',[]):quest_names[s['ID']]=s.get('Quest',{}).get('Name',s['ID'])

def clean(s):
 s=str(s or '')
 while re.search(r'\{\{[^|{}]+\|([^{}]*)\}\}',s):s=re.sub(r'\{\{[^|{}]+\|([^{}]*)\}\}',r'\1',s)
 return re.sub(r'[&^][kKrRgGwWbBmMcCyY]','',s).replace('\r\n','\n')
def human(s):return re.sub(r'(?<=[a-z])(?=[A-Z])',' ',str(s)).replace('_',' ').replace('-',' ')
def name(cid):
 n=clean(names.get(cid,human(re.sub(r'_[0-9]+$','',cid))));return n[:1].upper()+n[1:]
def src(s):
 s=str(s or '')
 if s.startswith('Content/'):return 'Assets/Resources/'+s
 if s.startswith('Source/'):return 'Assets/Scripts/'+s.removeprefix('Source/')
 return s
ending_names={1:'Renewal through a vessel',2:'Renewal through practice',3:'Gathered',4:'Kept'}
def gate(p):
 k,v=p.get('Key',''),str(p.get('Value',''));q=quest_names.get(v,human(v))
 if k=='IfFact' and v.startswith('Ending:=:'):
  n=int(v.rsplit(':',1)[1]);return 'before any ending' if n==0 else 'after '+ending_names.get(n,str(n))
 lookup={'IfQuestNotStarted':f'before accepting {q}','IfQuestActive':f'while {q} is active','IfQuestCompleted':f'after completing {q}','IfQuestRefused':f'after declining {q}',
 'IfHaveProperty':f'when {human(v)} is recorded','IfNotHaveProperty':f'when {human(v)} has not been recorded','IfHaveItem':f'carrying {human(v)}','IfHaveTag':f'carrying the {human(v)} tag','IfHasEffect':f'affected by {human(v)}'}
 if k in lookup:return lookup[k]
 if k=='IfQuestStage':
  a,_,b=v.partition(':');return quest_names.get(a,human(a))+' — '+human(b)+' stage'
 if k in ('IfStatAtLeast','IfNotStatAtLeast'):
  a,_,b=v.partition(':');return human(a)+(' below ' if k.startswith('IfNot') else ' at least ')+b
 if k=='IfStatBelowPercent':a,_,b=v.partition(':');return human(a)+' below '+b+'%'
 if k=='IfReputationAtLeast':a,_,b=v.partition(':');return human(b)+' or better with '+human(a)
 if k=='IfFact':return 'fact '+v
 if k=='IfNotFact':return 'fact not satisfied: '+v
 if k.endswith('Can'):return human(k[2:-3])+' permits '+v
 return human(k.removeprefix('If'))+((': '+v) if v else '')

blocks=[];seen_nodes=set();seen_runtime=set();seen_extras=set();choice_count=0;message_count=0;scene_count=0
chapter_order={c['id']:i for i,c in enumerate(plot['chapters'])};first_chapter={}
# Allocate stable cross references before writing any text.
for c in plot['chapters']:
 for entry in c.get('conversations',[]):
  cid=entry['id'];first_chapter.setdefault(cid,c['id']);wanted=entry.get('nodes',['*']);excluded=set(entry.get('exclude_nodes',[]))
  chosen=[n for n in convs[cid]['Nodes'] if ('*' in wanted or n['ID'] in wanted) and n['ID'] not in excluded]
  # Explicit lists also specify narrative order.
  if '*' not in wanted:chosen.sort(key=lambda n:wanted.index(n['ID']))
  for n in chosen:
   key=(cid,n['ID']);assert key not in node_anchor,key;node_anchor[key]=f'D{len(node_anchor)+1:03d}'
assert len(node_anchor)==len(nodes)==672

def add(kind,text='',**more):blocks.append({'kind':kind,'text':clean(text),**more})
def extra(eid):
 if eid in seen_extras:return
 e=extras[eid];seen_extras.add(eid);add('h3',e['title'],anchor='extra_'+eid)
 add('note',e.get('conditions',''));add('label',{'ending_epilogue':'Ending narration','choice_label':'Player choice','readable_world_text':'Reading / examination'}.get(e['kind'],'Narration'))
 add('speech',e['text']);add('source',src(e['source']))
def runtime_record(i):
 assert i not in seen_runtime,('runtime duplicate',i);seen_runtime.add(i);r=runtime[i]
 label=r.get('role') or r.get('kind') or 'Narration';speaker=clean(r.get('speaker',''))
 if label=='NPC speech' and speaker:label=speaker
 elif label=='Player choice':label='Player'
 add('h4',f'R{i+1:03d} · {label}',anchor=f'R{i+1:03d}')
 context=r.get('context','');notes=r.get('notes','')
 if context:add('note',context)
 if notes:add('note',notes)
 if r.get('template'):add('note','Template: braces identify values supplied by the current world or quest state.')
 add('speech',r['text']);add('source',src(r['source'])+((':'+str(r['line'])) if r.get('line') else ''))

staged={i for c in plot['chapters'] for e in c.get('conversations',[]) for k in ('runtime_indices_before','runtime_indices_after') for i in e.get(k,[])}
# Chapter-level staging is supported too.
staged.update(i for c in plot['chapters'] for k in ('runtime_indices_before','runtime_indices_after') for i in c.get(k,[]))
opening=[i for i,r in enumerate(runtime) if i not in staged and r.get('conversation_id')=='Warden_1' and r['source'].endswith('FirstHourGuidance.cs')]
groups=collections.defaultdict(lambda:collections.defaultdict(list))
place_chapter={'Sill':'03','Gantry':'05','Tine':'05','Sumphold':'05','Posy':'05','Quillhold':'05','Wellmeet':'05'}
for i,r in enumerate(runtime):
 if i in staged or i in opening:continue
 cid=r.get('conversation_id');file=Path(r['source']).name;concern=r.get('concern_id')
 if concern:
  ch=place_chapter.get(r.get('place'),'07');key='Local story: '+r.get('place','')+' — '+r.get('title',concern)
 elif file=='LocalPeople.cs':ch='17';key='Villagers: regional and personal variations'
 elif file.startswith('Morrowfast'):ch='04';key='Morrowfast: additional outcomes'
 elif file.startswith('Stillleaf'):ch='08';key='Stillleaf: additional conditional outcomes'
 elif file in ('FoundingPlumePart.cs','FoundingTrustService.cs'):ch='10';key='The Rooted: tending, sleep and dream'
 elif file in ('EndingSpine.cs','EndingRoutes.cs'):ch='12';key='At the threshold: choices, conditions and changed examinations'
 elif file in ('CookIntroductionPart.cs','LocalGatheringClaimPart.cs'):ch='01';key='The field residents: harvesting and the first meal'
 elif file=='ChoirOffering.cs':ch='06';key='The Choir: an offering'
 elif file in ('TentRightOath.cs','ClothClaims.cs'):ch='07';key='Guest-right and the cloth'
 elif file=='ConcordAccount.cs':ch='07';key='The Concord: credit and account'
 elif cid in first_chapter:ch=first_chapter[cid];key=name(cid)+': additional state and location variants'
 else:ch='17';key='Shared dialogue: '+human(Path(r['source']).stem)
 groups[ch][key].append(i)

kind_labels={'opening':'Opening','main_context':'Main-story context','main_chain':'Story chain','optional_early':'Optional early stories','optional_regional':'Optional regional stories','optional_drama':'Optional household drama','ending_preparation':'Ending preparation','ending_threshold':'Final choices','alternative_ending':'Alternative ending','ambient_appendix':'Reference appendix','supplementary_unplaced':'Additional authored material'}
for ch in plot['chapters']:
 add('h1',ch['id']+' · '+ch['title'],anchor='chapter_'+ch['id']);add('note',kind_labels.get(ch['kind'],human(ch['kind'])))
 add('body',ch.get('summary',''))
 if ch.get('kind')=='alternative_ending':add('notice','This is an alternative ending. It does not follow the previous ending in the same playthrough.')
 if ch['id']=='18':add('notice','These conversations are authored source material. Their inclusion does not assert that every speaker has a confirmed ordinary-world encounter.')
 if ch['id']=='01':
  add('h2','The clearing warden: current opening')
  for i in opening:runtime_record(i)
 for i in ch.get('runtime_indices_before',[]):runtime_record(i)
 for eid in ch.get('narrative_extra_ids',[]):extra(eid)
 for entry in ch.get('conversations',[]):
  cid=entry['id'];scene_count+=1;title=entry.get('title') or name(cid)
  if cid=='Warden_1':title=name(cid)+' — base and village-location wording'
  add('h2',title,anchor='scene_'+str(scene_count));add('source',convs[cid]['_source']+' · '+cid+((' · '+locations[cid]) if locations.get(cid) else ''))
  if entry.get('summary'):add('body',entry['summary'])
  for i in entry.get('runtime_indices_before',[]):runtime_record(i)
  wanted=entry.get('nodes',['*']);excluded=set(entry.get('exclude_nodes',[]));chosen=[n for n in convs[cid]['Nodes'] if ('*' in wanted or n['ID'] in wanted) and n['ID'] not in excluded]
  if '*' not in wanted:chosen.sort(key=lambda n:wanted.index(n['ID']))
  for n in chosen:
   key=(cid,n['ID']);assert key not in seen_nodes;seen_nodes.add(key);anchor=node_anchor[key]
   add('h3',anchor+' · '+('Opening / return' if n['ID']=='Start' else human(n['ID'])),anchor=anchor)
   add('label',name(cid));add('speech',n.get('Text',''))
   if n.get('AllowEscape') is False:add('note','This scene requires a reply before leaving.')
   for j,q in enumerate(n.get('Choices',[]),1):
    choice_count+=1;add('choice',q.get('Text',''),number=j)
    if q.get('Predicates'):add('gate','Available when: '+'; '.join(gate(p) for p in q['Predicates'])+'.')
    target=q.get('Target','End')
    if target in ('End','__end__',''):add('route','Conversation ends.')
    elif target=='Trade':add('route','Open trade.')
    else:
     dest=node_anchor.get((cid,target));assert dest,(cid,target);add('route','Continue at '+dest+' — '+human(target)+'.',target=dest)
    for a in q.get('Actions',[]):
     if a.get('Key') in ('AddMessage','Print'):
      message_count+=1;add('result','After this choice: '+a.get('Value',''))
  for i in entry.get('runtime_indices_after',[]):runtime_record(i)
  for eid in entry.get('narrative_extra_ids_after',[]):extra(eid)
 for label,items in groups[ch['id']].items():
  add('h2',label)
  if any(runtime[i].get('concern_id') for i in items):
   def sortkey(i):
    r=runtime[i];field=r.get('field','')
    rank=0 if field=='Pitch' else 1 if field=='Withheld' else 4 if field=='Thanks' else 5 if field.startswith('Refusal') else 2
    return rank,i
   items=sorted(items,key=sortkey)
  for i in items:runtime_record(i)
 for i in ch.get('runtime_indices_after',[]):runtime_record(i)
assert seen_nodes==set(nodes),(len(seen_nodes),len(nodes))
assert seen_runtime==set(range(len(runtime))),set(range(len(runtime)))-seen_runtime
# Any unplaced supplementary text is kept visibly at the end rather than omitted.
if set(extras)!=seen_extras:
 add('h1','Supplementary narrative passages',anchor='extra_unplaced')
 for eid in extras:
  if eid not in seen_extras:extra(eid)
add('h1','Source and reading notes',anchor='source_notes')
add('body','This is a source-faithful compilation of the authored game at main revision '+plot['source_commit']+'. The sequence is an editorial reading order, not a claim that optional stories are mandatory or that all branches occur in one expedition. Speaker labels, chapter summaries, route references and availability notes are editorial. Dialogue and displayed narrative retain authored wording apart from removal of display-color markup.')
add('body','The current opening is the Spread clearing followed by Sill. Several older design documents describe a different opening. Runtime passages can replace or extend the fixed conversation nodes according to place, character and quest state. Dynamic values are shown as descriptive placeholders in braces; no single generated world or save has been substituted.')
add('body','All four implemented endings and their matching post-ending replies are included. Unwritten or unimplemented lore scenes—such as the deeper Selen/Cathedral scene, Reader–Salted reconciliation and Dohren gate—have not been fabricated. Ordinary combat logs and generic inventory/system feedback are outside this dialogue script.')
add('body',f'Coverage: {len(convs)} conversation trees from 31 files; {len(seen_nodes)} dialogue nodes; {choice_count:,} player choices; {message_count} choice-triggered narrative messages; {len(runtime)} audited runtime passages, including all 22 concerns; and {len(extras)} supplementary narrative passages. Each fixed node and audited runtime record appears once. Mutually exclusive wording remains visible as alternatives.')
add('h2','Conversation source index')
for cid in sorted(convs,key=lambda c:name(c).casefold()):add('source',name(cid)+' — '+cid+' — '+convs[cid]['_source'])
add('h2','Source audit')
add('body','Compiled 10 November 2026 from stevenrichter16/caves_of_ooo, main. Plot ordering checks current gameplay triggers and source text against ENDING-SPINE, ENDING-ROUTES, SPREAD-FIRST-HOUR-PLAN, MIDGAME-STILLLEAF-ARCHIVE and CANONICAL-VILLAGE-QUESTS. Runtime source references accompany their passages. Design history is not treated as implemented dialogue.')

(HERE/'script-blocks.json').write_text(json.dumps(blocks,ensure_ascii=False,indent=2)+'\n')
intro=[
 'A complete reading script of the authored conversations, with branching choices, optional stories and all four endings.',
 'Read chapters 1–12 as progression toward the final choice. Chapters 13–16 are mutually exclusive endings. Chapters 17–18 hold ambient services and additional authored scenarios. Optional chapters can occur in another order during play.',
 'D001-style labels identify dialogue nodes; player choices point to the next node. R001-style labels identify runtime variants and interactions. Availability notes are not spoken dialogue. Text in braces is supplied by the world or current quest state.',
 'Source: main · '+plot['source_commit'][:12]+' · compiled 10 November 2026.'
]
# Plain-text edition.
txt=['CAVES OF OOO','DIALOGUE IN STORY ORDER','',*intro,'','CONTENTS']
txt.extend(c['id']+' · '+c['title'] for c in plot['chapters']);txt.append('')
for b in blocks:
 k=b['kind'];t=b['text']
 if k=='h1':txt.extend(['','='*72,t.upper(),'='*72])
 elif k in ('h2','h3','h4'):txt.extend(['',t])
 elif k=='choice':txt.append('  PLAYER '+str(b['number'])+': '+t)
 elif k in ('gate','route'):txt.append('    '+t)
 elif k=='source':txt.append('  [Source: '+t+']')
 elif k in ('note','notice'):txt.append('  ['+t+']')
 elif k=='label':txt.append(t.upper()+':')
 else:txt.extend([t,''])
(OUT/'Caves_of_Ooo_Dialogue.txt').write_text('\n'.join(txt)+'\n')

# Editable Word edition.
doc=Document();section=doc.sections[0];section.page_width=Cm(21);section.page_height=Cm(29.7);section.top_margin=Cm(2);section.bottom_margin=Cm(2);section.left_margin=Cm(2.1);section.right_margin=Cm(2.1)
styles=doc.styles
for key in ('Normal','Body Text'):
 styles[key].font.name='Georgia';styles[key].font.size=Pt(10);styles[key].paragraph_format.space_after=Pt(5)
for h,size in [('Title',32),('Heading 1',23),('Heading 2',16),('Heading 3',11),('Heading 4',10)]:
 styles[h].font.name='Calibri';styles[h].font.size=Pt(size);styles[h].font.color.rgb=RGBColor.from_string('354B3D')
 styles[h].paragraph_format.keep_with_next=True
styles['Caption'].font.name='Calibri';styles['Caption'].font.size=Pt(8)
doc.add_paragraph('CAVES OF OOO','Subtitle');doc.add_paragraph('Dialogue in\nstory order','Title')
for t in intro:doc.add_paragraph(t)
doc.add_page_break();doc.add_heading('Contents',0)
for c in plot['chapters']:doc.add_paragraph(c['id']+' · '+c['title'])
bookmark_id=0
for b in blocks:
 k=b['kind'];t=b['text']
 if k=='h1':doc.add_page_break();p=doc.add_heading(t,1)
 elif k in ('h2','h3','h4'):p=doc.add_heading(t,int(k[1]))
 elif k=='label':p=doc.add_paragraph();r=p.add_run(t);r.bold=True
 elif k=='choice':p=doc.add_paragraph();p.paragraph_format.left_indent=Cm(.3);r=p.add_run('Player '+str(b['number'])+': ');r.bold=True;p.add_run(t)
 elif k in ('note','notice','gate','route','source'):
  p=doc.add_paragraph(t,'Caption');p.paragraph_format.space_after=Pt(4)
  if k!='source':
   for r in p.runs:r.italic=True
 elif k=='result':p=doc.add_paragraph(t);p.paragraph_format.left_indent=Cm(.5)
 else:p=doc.add_paragraph(t)
 if b.get('anchor'):
  bookmark_id+=1;start=OxmlElement('w:bookmarkStart');start.set(qn('w:id'),str(bookmark_id));start.set(qn('w:name'),b['anchor']);end=OxmlElement('w:bookmarkEnd');end.set(qn('w:id'),str(bookmark_id));p._p.insert(0,start);p._p.append(end)
footer=section.footer.paragraphs[0];footer.alignment=2;footer.add_run('Caves of Ooo · Dialogue script  |  ')
f=OxmlElement('w:fldSimple');f.set(qn('w:instr'),'PAGE');footer._p.append(f)
doc.core_properties.title='Caves of Ooo — Dialogue in story order';doc.core_properties.subject='Authored dialogue, branches, runtime variants and four endings';doc.core_properties.author='Caves of Ooo source compilation'
doc.save(OUT/'Caves_of_Ooo_Dialogue.docx')

# Typeset PDF with linked contents, bookmarks and branch references.
fonts=Path('/usr/share/fonts/truetype/dejavu')
for family,file in [('Book','DejaVuSerif.ttf'),('BookBold','DejaVuSerif-Bold.ttf'),('UI','DejaVuSans.ttf'),('UIBold','DejaVuSans-Bold.ttf'),('UIItalic','DejaVuSans-Oblique.ttf')]:pdfmetrics.registerFont(TTFont(family,str(fonts/file)))
pdfmetrics.registerFontFamily('UI',normal='UI',bold='UIBold',italic='UIItalic',boldItalic='UIBold');pdfmetrics.registerFontFamily('Book',normal='Book',bold='BookBold',italic='Book',boldItalic='BookBold')
ink=colors.HexColor('#26362d');muted=colors.HexColor('#68736b');accent=colors.HexColor('#405c49')
ps={
 'body':ParagraphStyle('body',fontName='Book',fontSize=10,leading=14,textColor=ink,spaceAfter=7),
 'speech':ParagraphStyle('speech',fontName='Book',fontSize=10,leading=14,textColor=ink,spaceAfter=7),
 'h1':ParagraphStyle('h1',fontName='BookBold',fontSize=23,leading=29,textColor=accent,spaceAfter=16,keepWithNext=True),
 'h2':ParagraphStyle('h2',fontName='UIBold',fontSize=14,leading=19,textColor=accent,spaceBefore=16,spaceAfter=9,keepWithNext=True),
 'h3':ParagraphStyle('h3',fontName='UIBold',fontSize=10,leading=14,textColor=accent,spaceBefore=12,spaceAfter=5,keepWithNext=True),
 'h4':ParagraphStyle('h4',fontName='UIBold',fontSize=9,leading=12,textColor=accent,spaceBefore=9,spaceAfter=3,keepWithNext=True),
 'label':ParagraphStyle('label',fontName='UIBold',fontSize=8.5,leading=11,textColor=ink,spaceBefore=4,spaceAfter=3,keepWithNext=True),
 'choice':ParagraphStyle('choice',fontName='UI',fontSize=9,leading=12.5,textColor=ink,leftIndent=12,spaceBefore=5,spaceAfter=2),
 'gate':ParagraphStyle('gate',fontName='UIItalic',fontSize=7.7,leading=10.5,textColor=muted,leftIndent=12,spaceAfter=2),
 'route':ParagraphStyle('route',fontName='UI',fontSize=7.7,leading=10.5,textColor=accent,leftIndent=12,spaceAfter=4),
 'note':ParagraphStyle('note',fontName='UIItalic',fontSize=8,leading=11,textColor=muted,spaceAfter=5),
 'notice':ParagraphStyle('notice',fontName='UIBold',fontSize=8.5,leading=12,textColor=accent,spaceBefore=5,spaceAfter=12),
 'source':ParagraphStyle('source',fontName='UI',fontSize=7,leading=9,textColor=muted,spaceAfter=7),
 'result':ParagraphStyle('result',fontName='Book',fontSize=9,leading=12.5,textColor=ink,leftIndent=16,spaceAfter=7),
}
def escaped(t):return html.escape(str(t)).replace('\n','<br/>')
class ScriptPDF(BaseDocTemplate):
 def __init__(self,path):
  super().__init__(str(path),pagesize=A4,rightMargin=48,leftMargin=48,topMargin=54,bottomMargin=48,title='Caves of Ooo — Dialogue in story order',author='Caves of Ooo source compilation')
  self.chapter='Dialogue in story order';f=Frame(self.leftMargin,self.bottomMargin,self.width,self.height,id='body');self.addPageTemplates(PageTemplate(id='main',frames=f,onPage=self.page_frame))
 def page_frame(self,c,d):
  if d.page==1:return
  c.saveState();c.setFont('UI',7);c.setFillColor(muted);c.drawString(48,A4[1]-30,'CAVES OF OOO  /  DIALOGUE SCRIPT');c.drawRightString(A4[0]-48,27,str(d.page));c.setStrokeColor(colors.HexColor('#d7ded8'));c.setLineWidth(.5);c.line(48,39,A4[0]-48,39);c.restoreState()
 def afterFlowable(self,f):
  if isinstance(f,Paragraph) and getattr(f,'bookmark',None):
   self.canv.bookmarkPage(f.bookmark)
   if getattr(f,'toc_level',None) is not None:
    self.notify('TOCEntry',(f.toc_level,f.getPlainText(),self.page,f.bookmark));self.canv.addOutlineEntry(f.getPlainText(),f.bookmark,level=f.toc_level,closed=f.toc_level>0)
flow=[Spacer(1,90),Paragraph('CAVES OF OOO',ParagraphStyle('eyebrow',fontName='UIBold',fontSize=12,leading=18,textColor=accent)),Spacer(1,14),Paragraph('Dialogue in<br/>story order',ParagraphStyle('cover',fontName='BookBold',fontSize=38,leading=47,textColor=ink)),Spacer(1,30)]
for t in intro:flow.append(Paragraph(escaped(t),ps['body']))
flow.extend([Spacer(1,24),Paragraph('FULL STORY SPOILERS · ALL FOUR ENDINGS',ps['notice']),PageBreak(),Paragraph('Contents',ps['h1'])])
toc=TableOfContents();toc.dotsMinLevel=0;toc.levelStyles=[ParagraphStyle('toc1',fontName='UI',fontSize=9.3,leading=14,textColor=ink,spaceBefore=7,rightIndent=30)];flow.extend([toc,PageBreak()])
first=True
for b in blocks:
 k=b['kind'];t=escaped(b['text'])
 if k=='h1':
  if not first:flow.append(PageBreak())
  first=False
 if k=='choice':t='<b>Player '+str(b['number'])+':</b> '+t
 if b.get('target'):t='<link href="#'+b['target']+'" color="#405c49">'+t+'</link>'
 if b.get('anchor'):t='<a name="'+b['anchor']+'"/>'+t
 p=Paragraph(t or '&#160;',ps.get(k,ps['body']))
 if b.get('anchor'):p.bookmark=b['anchor']
 if k=='h1':p.toc_level=0
 flow.append(p)
pdf=ScriptPDF(OUT/'Caves_of_Ooo_Dialogue.pdf');pdf.multiBuild(flow)
manifest={'sourceCommit':plot['source_commit'],'conversationFiles':31,'conversations':len(convs),'nodes':len(seen_nodes),'choices':choice_count,'choiceMessages':message_count,'runtimePassages':len(seen_runtime),'supplementalPassages':len(seen_extras),'blocks':len(blocks),'wordCount':len(re.findall(r'\b\w+\b',' '.join(b['text'] for b in blocks))),'files':{p.name:{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in OUT.glob('Caves_of_Ooo_Dialogue.*')}}
(HERE/'export-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest,indent=2))
