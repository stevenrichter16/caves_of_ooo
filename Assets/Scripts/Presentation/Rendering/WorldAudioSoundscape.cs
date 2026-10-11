using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Cosmetic place detail, never gameplay hearing. Discovery samples at most169 nearby cells
    /// and512 owners on the existing context boundary. Idle revalidates at most nine detail sources and one flow witness.
    /// Only an already-visible source can produce a positioned cue; habitat texture carries no entity information.</summary>
    public sealed class WorldAudioSoundscape
    {
        const int Radius=6,OwnerBudget=512;
        enum Kind{Water,Fire,Frog,Dog,Wood,Stone,Bird,Leaf,Reed}
        readonly Entity[] sources=new Entity[9];
        readonly int[] distances=new int[9];
        readonly WorldAudioPlayer audio;
        Zone zone;Entity actor;Entity flowWitness;BiomeType biome;DayBand band;int depth;
        bool active,interior;float phraseGap;uint random;int laneCursor;
        readonly float[] laneRemaining=new float[3];
        readonly int[] sourceCursor=new int[2];
        public int WildlifePhrases{get;private set;}
        public int MaterialPhrases{get;private set;}
        public int HabitatPhrases{get;private set;}
        string habitat,habitatWitness;
        readonly Entity[] detailWitnesses=new Entity[2];
        readonly int[] detailKinds=new int[2];
        public string AmbienceId{get;private set;}
        public int ScanCount{get;private set;}
        public int LastVisitedOwners{get;private set;}
        public int SourceCount{get{int n=0;for(int i=0;i<sources.Length;i++)if(sources[i]!=null)n++;return n;}}
        public int HabitatDetailCount=>habitat==null?0:1;
        public WorldAudioSoundscape(WorldAudioPlayer player){audio=player??throw new ArgumentNullException(nameof(player));}
        public void Observe(Zone current,Entity listener,BiomeType region,int level,DayBand time,bool visible)
        {
            bool graphChanged=current!=zone||listener!=actor;
            if(graphChanged)
            {
                ClearDetails();random=Hash(current?.ZoneID);laneCursor=0;phraseGap=0;
                sourceCursor[0]=sourceCursor[1]=(int)(random%(uint)sources.Length);
                laneRemaining[0]=3+Next()*2;laneRemaining[1]=6+Next()*3;laneRemaining[2]=11+Next()*4;
            }
            bool policyChanged=biome!=region||depth!=level;
            zone=current;actor=listener;biome=region;depth=level;band=time;
            if(policyChanged)ClearDetails();
            Array.Clear(sources,0,sources.Length);flowWitness=null;LastVisitedOwners=0;int flowDistance=int.MaxValue;
            var origin=actor==null?null:zone?.GetEntityCell(actor);
            active=visible&&origin!=null&&Alive(actor)&&zone.ZoneID!=WorldMap.WorldMapZoneID;
            if(!active){habitat=null;SetBed(null);ClearDetails();return;}
            if(interior!=origin.IsInterior)ClearDetails();
            interior=origin.IsInterior;ScanCount++;
            for(int i=0;i<distances.Length;i++)distances[i]=int.MaxValue;
            for(int y=Mathf.Max(0,origin.Y-Radius);y<=Mathf.Min(Zone.Height-1,origin.Y+Radius)&&LastVisitedOwners<OwnerBudget;y++)
                for(int x=Mathf.Max(0,origin.X-Radius);x<=Mathf.Min(Zone.Width-1,origin.X+Radius)&&LastVisitedOwners<OwnerBudget;x++)
                {
                    var cell=zone.GetCell(x,y);if(!cell.IsVisible||!cell.Explored||cell.IsInterior!=interior
                        &&(!interior||Math.Abs(x-origin.X)+Math.Abs(y-origin.Y)!=1))continue;
                    for(int j=0;j<cell.Objects.Count&&LastVisitedOwners<OwnerBudget;j++)
                    {
                        LastVisitedOwners++;var owner=cell.Objects[j];if(!Current(owner,out _))continue;
                        int kind=Classify(owner);if(kind<0)continue;
                        int distance=Math.Max(Math.Abs(x-origin.X),Math.Abs(y-origin.Y));
                        if(kind==(int)Kind.Water&&FlowingWater(owner)&&distance<flowDistance)
                        {flowWitness=owner;flowDistance=distance;}
                        if(distance<distances[kind]){sources[kind]=owner;distances[kind]=distance;}
                    }
                }
            SelectBed();
            ValidateDetails();
        }
        public void Tick(float wallDelta,bool worldVisible,bool danger)
        {
            if(!active||!worldVisible||!Alive(actor)||zone?.GetEntityCell(actor)==null)
            {active=false;Array.Clear(sources,0,sources.Length);flowWitness=null;habitat=null;SetBed(null);ClearDetails();return;}
            if(interior!=zone.GetEntityCell(actor).IsInterior){interior=zone.GetEntityCell(actor).IsInterior;ClearDetails();}
            ValidateDetails();
            for(int i=0;i<sources.Length;i++)if(sources[i]!=null&&(!Current(sources[i],out _)||Classify(sources[i])!=i))
            {ClearDetails();sources[i]=null;}
            if(flowWitness!=null&&(!Current(flowWitness,out _)||Classify(flowWitness)!=(int)Kind.Water||!FlowingWater(flowWitness)))flowWitness=null;
            // An already observed water owner may gain flow state without a discovery scan.
            var knownWater=sources[(int)Kind.Water];
            if(flowWitness==null&&knownWater!=null&&FlowingWater(knownWater))flowWitness=knownWater;
            SelectBed();
            // Independent restrained lanes: scenery cannot repeatedly win away an animal's turn.
            if(WorldAudioSettings.AmbienceVolume<=0||AudioListener.pause)
            {for(int i=0;i<3;i++)laneRemaining[i]=Math.Max(laneRemaining[i],5);phraseGap=4;ClearDetails();return;}
            float delta=WorldAudioPlayer.SafeDelta(wallDelta);phraseGap=Math.Max(0,phraseGap-delta);
            for(int i=0;i<3;i++)laneRemaining[i]=Math.Max(-30,laneRemaining[i]-delta);
            if(phraseGap>0)return;
            for(int offset=0;offset<3;offset++)
            {
                int lane=(laneCursor+offset)%3;
                if(laneRemaining[lane]>0||!PlayLane(lane,danger))continue;
                laneRemaining[lane]=((lane==0?8:lane==1?12:20)+Next()*(lane==2?12:8))
                    *(biome==BiomeType.Overwrit?2:1)*(danger?1.8f:1);
                // Four seconds exceeds every shipped environmental phrase (<=3.02s).
                // No overlapping source witness can be displaced by a later phrase.
                phraseGap=4;laneCursor=(lane+1)%3;return;
            }
        }
        static bool Wildlife(int kind)=>kind==(int)Kind.Frog||kind==(int)Kind.Dog||kind==(int)Kind.Bird;
        bool PlayLane(int lane,bool danger)
        {
            if(lane==2)
            {
                if(habitat==null||habitat=="bird"&&sources[(int)Kind.Bird]!=null)return false;
                if(!audio.TryPlayDetail(habitat,(Next()-.5f)*.7f,danger?.2f:.38f))return false;
                habitatWitness=habitat;HabitatPhrases++;return true;
            }
            for(int offset=0;offset<sources.Length;offset++)
            {
                int i=(sourceCursor[lane]+offset)%sources.Length;var owner=sources[i];
                if(owner==null||Wildlife(i)!=(lane==0)||!Current(owner,out var cell))continue;
                var origin=zone.GetEntityCell(actor);int distance=Math.Max(Math.Abs(origin.X-cell.X),Math.Abs(origin.Y-cell.Y));
                float gain=(interior?.65f:.7f)*(1f-distance/9f)*(danger?.45f:1);
                if(!audio.TryPlayDetail(Id((Kind)i),(cell.X-origin.X)/10f,gain))return false;
                for(int w=0;w<detailWitnesses.Length;w++)if(detailWitnesses[w]==null){detailWitnesses[w]=owner;detailKinds[w]=i;break;}
                sourceCursor[lane]=(i+1)%sources.Length;
                if(lane==0)WildlifePhrases++;else MaterialPhrases++;return true;
            }
            return false;
        }
        void ClearDetails(){audio.ClearDetails();Array.Clear(detailWitnesses,0,detailWitnesses.Length);habitatWitness=null;}
        void ValidateDetails()
        {
            if(audio.ActiveDetailSources==0){Array.Clear(detailWitnesses,0,detailWitnesses.Length);habitatWitness=null;return;}
            if(habitatWitness!=null&&habitatWitness!=habitat){ClearDetails();return;}
            // Keep audible source witnesses independent of the latest nearest-source snapshot.
            for(int i=0;i<detailWitnesses.Length;i++)if(detailWitnesses[i]!=null
                &&(!Current(detailWitnesses[i],out _)||Classify(detailWitnesses[i])!=detailKinds[i])){ClearDetails();return;}
        }
        void SetBed(string id)
        {
            if(AmbienceId=="water"&&id!="water")audio.InvalidateAmbience("water");
            AmbienceId=id;
        }
        void SelectBed()
        {
            string previous=AmbienceId;SelectBedCore();
            if(previous=="water"&&AmbienceId!="water")audio.InvalidateAmbience("water");
        }
        void SelectBedCore()
        {
            habitat=null;
            if(!active){AmbienceId=null;return;}
            if(depth>0||biome==BiomeType.Cave){AmbienceId="cave";return;}
            if(interior){AmbienceId=null;return;}
            bool night=band==DayBand.Dusk||band==DayBand.Dark;
            bool green=biome==BiomeType.Spread||biome==BiomeType.Jungle||biome==BiomeType.Grovelands;
            if(green){AmbienceId=night?"night-field":"field";habitat=night?"insect":"bird";}
            else if(biome==BiomeType.Sodden){AmbienceId=night?"night-field":"marsh";habitat=night?"insect":null;}
            else AmbienceId="dry-wind";
            if(flowWitness!=null)AmbienceId="water";
        }
        static bool FlowingWater(Entity owner)
        {
            var pool=owner.GetPart<LiquidPoolPart>();
            return pool?.ParentEntity==owner&&pool.Volume>0&&pool.LiquidId=="water"
                &&(owner.HasTag("FlowsEast")||owner.HasTag("FlowsSouth")||owner.BlueprintName=="SprayPool"||owner.BlueprintName=="HelmwoodWaterPassage");
        }
        bool Current(Entity owner,out Cell cell)
        {
            cell=null;if(owner==null||actor==null||zone==null||owner==actor)return false;
            var origin=zone.GetEntityCell(actor);cell=zone.GetEntityCell(owner);
            if(origin==null||cell==null||origin.ParentZone!=zone||cell.ParentZone!=zone||actor.SpatialZone!=zone||owner.SpatialZone!=zone||!cell.Objects.Contains(owner)||!cell.Explored||!cell.IsVisible
                ||cell.IsInterior!=origin.IsInterior&&!EnclosesListener(owner,origin,cell)
                ||Math.Abs(cell.X-origin.X)>Radius||Math.Abs(cell.Y-origin.Y)>Radius)return false;
            var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
            return render?.ParentEntity==owner&&render.Visible&&physics?.ParentEntity==owner&&physics.InInventory==null&&physics.Equipped==null;
        }
        bool EnclosesListener(Entity owner,Cell origin,Cell cell)
        {
            // Generated room perimeters are intentionally exterior. Only the immediate
            // cardinal face around an indoor listener can cross this presentation boundary.
            if(!origin.IsInterior||cell.IsInterior||Math.Abs(cell.X-origin.X)+Math.Abs(cell.Y-origin.Y)!=1
                ||owner.GetPart<PhysicsPart>()?.ParentEntity!=owner)return false;
            int kind=Classify(owner);return kind==(int)Kind.Wood||kind==(int)Kind.Stone;
        }
        int Classify(Entity owner)
        {
            var destructible=owner.GetPart<DestructiblePart>();
            if(destructible!=null&&(destructible.ParentEntity!=owner||destructible.HP<=0||destructible.Gone))return -1;
            var pool=owner.GetPart<LiquidPoolPart>();
            if(pool?.ParentEntity==owner&&pool.Volume>0&&(pool.LiquidId=="water"||pool.LiquidId=="brine"||pool.LiquidId=="bog-mire")
                &&LiquidSourcePhase.CanDrawWater(zone,owner))return (int)Kind.Water;
            if(Flame(owner))return (int)Kind.Fire;
            if(owner.HasTag("Creature")&&Alive(owner)&&owner.GetPart<BrainPart>()?.ParentEntity==owner)
            {
                if(!CanCall(owner))return -1;
                switch(owner.BlueprintName)
                {
                    case "Bandfrog":case "Reedfrog":case "GinFrog":case "MawToad":case "HelmwoodFrog":return (int)Kind.Frog;
                    case "PetDog":return (int)Kind.Dog;
                    case "Magpie":if(depth<=0&&(band==DayBand.Dawn||band==DayBand.Height))return (int)Kind.Bird;break;
                }
                return -1; // CascadeFather is lore-silent; SummitSinger needs a single-note recording.
            }
            if(owner.GetPart<PhysicsPart>().Takeable)return -1;
            bool outdoors=depth<=0&&biome!=BiomeType.Cave&&!interior;
            if(outdoors)
            {
                if(owner.BlueprintName=="Reeds")return (int)Kind.Reed;
                if(owner.BlueprintName=="Tree"||owner.BlueprintName=="Bush")return (int)Kind.Leaf;
                if(owner.BlueprintName=="DryBrush"&&owner.GetPart<MaterialPart>() is MaterialPart brush
                    &&brush.ParentEntity==owner&&brush.MaterialID=="DryPlant")return (int)Kind.Leaf;
            }
            var material=owner.GetPart<MaterialPart>();
            if(material?.ParentEntity!=owner)return -1;
            bool structure=owner.HasTag("Wall")||owner.GetPart<DoorPart>()?.ParentEntity==owner;
            if(structure&&material.MaterialID=="Wood")return (int)Kind.Wood;
            if((!outdoors||biome==BiomeType.Ruins||biome==BiomeType.Stump)&&owner.HasTag("Wall")
                &&(material.MaterialID=="Stone"||material.MaterialID=="Masonry"))return (int)Kind.Stone;
            return -1;
        }
        static bool CanCall(Entity owner)
        {
            // Explicit sleep/stasis only: roots and pacification do not stop voluntary calls.
            var frozen=owner.GetEffect<FrozenEffect>();
            return !(frozen?.Owner==owner&&frozen.Cold>0)
                &&!ActiveRest(owner.GetEffect<AsleepByGasEffect>(),owner)
                &&!ActiveRest(owner.GetEffect<HibernatingEffect>(),owner);
        }
        static bool ActiveRest(Effect effect,Entity owner)=>effect?.Owner==owner
            &&(effect.Duration>0||effect.Duration==Effect.DURATION_INDEFINITE);
        static bool Flame(Entity owner)
        {
            var fuel=owner.GetPart<FuelPart>();
            if(fuel!=null&&(fuel.ParentEntity!=owner||!float.IsFinite(fuel.FuelMass)||fuel.FuelMass<=0))return false;
            var burning=owner.GetEffect<BurningEffect>();
            if(burning!=null&&burning.Owner==owner&&(burning.Duration>0||burning.Duration==Effect.DURATION_INDEFINITE)
                &&float.IsFinite(burning.Intensity)&&burning.Intensity>0)return true;
            var fire=owner.GetPart<CampfirePart>();var thermal=owner.GetPart<ThermalPart>();
            return fire?.ParentEntity==owner&&!fire.FiniteCooking&&fuel!=null&&thermal?.ParentEntity==owner
                &&float.IsFinite(thermal.Temperature)&&float.IsFinite(thermal.FlameTemperature)&&thermal.Temperature>=thermal.FlameTemperature
                &&!owner.HasEffect<SmolderingEffect>()&&!owner.HasEffect<CharredEffect>();
        }
        static bool Alive(Entity entity)=>entity!=null&&entity.GetStatValue("Hitpoints",0)>0&&!CombatSystem.IsDeathHandled(entity);
        string Id(Kind kind)
        {switch(kind){case Kind.Water:return interior||depth>0||biome==BiomeType.Cave?"water-drop":"water-lap";case Kind.Fire:return "fire";case Kind.Frog:return "frog";case Kind.Dog:return "dog";case Kind.Wood:return "wood-settle";case Kind.Stone:return "stone-shift";case Kind.Leaf:return "leaf-rustle";case Kind.Reed:return "reed-rustle";default:return "bird";}}
        static uint Hash(string id){unchecked{uint h=2166136261;foreach(char c in id??"")h=(h^c)*16777619;return h==0?1:h;}}
        float Next(){random=unchecked(random*1664525+1013904223);return (random>>8)*(1f/16777216);}
    }
}
