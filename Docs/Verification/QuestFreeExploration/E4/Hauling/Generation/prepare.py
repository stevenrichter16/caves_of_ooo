from pathlib import Path
import shutil
p=Path(__file__).parent;r=Path('/Users/steven/caves-of-ooo');d=p/'production'
paths=['Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs','Assets/Scripts/Gameplay/World/Generation/SpreadWildernessSituationPlan.cs','Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs','Assets/Scripts/Gameplay/World/Generation/Builders/SpreadCompositionBuilder.cs','Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationBuilder.cs','Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs']
for x in paths:
 a=d/x;a.parent.mkdir(parents=True,exist_ok=True);shutil.copy(r/x,a);shutil.copy(r/x,p/(Path(x).name+'.before.txt'))
a=d/paths[0];s=a.read_text().replace('CoolingWorkPatch }','CoolingWorkPatch, HeavySalvage }').replace('CurrentVersion=6','CurrentVersion=7').replace('case Formation.Hedgerow:\n','case Formation.Hedgerow:\n                    if(version>=7){uint variant=Rank(seed,id,"family-variant")%4;return variant==0?SpreadExplorationFamily.OccupiedBank:variant==1?SpreadExplorationFamily.CollectorReturn:variant==2?SpreadExplorationFamily.FieldPassage:SpreadExplorationFamily.HeavySalvage;}\n').replace('version!=5&&version!=CurrentVersion','version!=5&&version!=6&&version!=CurrentVersion').replace('version==5?9:10','version==5?9:version==6?10:11');a.write_text(s)
a=d/paths[1];a.write_text(a.read_text().replace(': source is ContainerBuilder c ?',': source is HaulablePropBuilder h ? h.OwnsSourceReceipt(this)\n            : source is ContainerBuilder c ?'))
a=d/paths[2];s=a.read_text().replace('One or two per zone at most','One per zone at most').replace('public int ChancePerMille = 350;','''public int ChancePerMille = 350;
        /// <summary>Opt-in exact ordinary cold source, never scan or replay authority.</summary>
        public bool CaptureSourceReceipts;
        public SpreadGenerationReceipt SourceReceipt { get; private set; }
        private int sourceRevision;
        internal bool OwnsSourceReceipt(SpreadGenerationReceipt receipt)=>CaptureSourceReceipts&&receipt!=null
            &&ReferenceEquals(SourceReceipt,receipt)&&receipt.Revision==sourceRevision;
''').replace('if (zone == null || factory == null || rng == null) return true;','SourceReceipt=null;int revision=++sourceRevision;\n            if (zone == null || factory == null || rng == null) return true;').replace('if (BuilderSpawn.TryPlace(zone, factory, blueprint, x, y) != null)\n                    return true;','''var made=BuilderSpawn.TryPlace(zone, factory, blueprint, x, y);
                if (made != null)
                {
                    if(CaptureSourceReceipts)SourceReceipt=new SpreadGenerationReceipt(this,zone,factory,revision,new[]{made},1);
                    return true;
                }''');a.write_text(s)
a=d/paths[3];a.write_text(a.read_text().replace('for the new-world passage family.','for new-world FieldPassage or HeavySalvage.'))
a=d/paths[5];s=a.read_text().replace('ContainerBuilder containers=null;','ContainerBuilder containers=null;HaulablePropBuilder haul=null;').replace('composition.CapturePassageSources=captured.Version>=5&&assignment.Family==SpreadExplorationFamily.FieldPassage;','composition.CapturePassageSources=captured.Version>=5&&(assignment.Family==SpreadExplorationFamily.FieldPassage||captured.Version>=7&&assignment.Family==SpreadExplorationFamily.HeavySalvage);').replace('if(builder is ContainerBuilder stock)containers=stock;','if(builder is ContainerBuilder stock)containers=stock;\n                    if(builder is HaulablePropBuilder heavy){heavy.CaptureSourceReceipts=captured.Version>=7&&assignment.Family==SpreadExplorationFamily.HeavySalvage;haul=heavy;}').replace('new SpreadExplorationBuilder(this,land,population,containers)','new SpreadExplorationBuilder(this,land,population,containers,haul)');a.write_text(s)
a=d/paths[4];s=a.read_text().replace('readonly ContainerBuilder containers;','readonly ContainerBuilder containers;readonly HaulablePropBuilder haul;').replace('PopulationBuilder population,ContainerBuilder containers)','PopulationBuilder population,ContainerBuilder containers,HaulablePropBuilder haul=null)').replace('this.containers=containers;','this.containers=containers;this.haul=haul;').replace('case SpreadExplorationFamily.CoolingWorkPatch:return Cooking(zone,factory,entry);','case SpreadExplorationFamily.CoolingWorkPatch:return Cooking(zone,factory,entry);\n    case SpreadExplorationFamily.HeavySalvage:return Hauling(zone,factory,entry);')
s=s.replace('  bool Cooking(','''  bool Hauling(Zone z,EntityFactory f,SpreadExplorationEntry entry)
  {
   if(terrain.Plan.Formation!=Formation.Hedgerow||!terrain.CapturePassageSources||haul?.SourceReceipt==null)return Refuse(z,entry,"no-ordinary-haul-source");
   var original=SpreadGenerationReceipt.CaptureFinalState(z,z.GetReadOnlyEntities().Where(e=>!DoorPart.IsBareGround(e)));
   if(SpreadExplorationHauling.TryPlace(z,terrain,haul,()=>Current(z,f,entry),out var load,out var final))
    return Commit(z,f,entry,new[]{load},final);
   if(!Current(z,f,entry)||!original())return false;
   return Refuse(z,entry,"no-useful-heavy-salvage-layout");
  }
  bool Cooking(''');a.write_text(s)
