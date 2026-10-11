using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Cosmetic place detail, never gameplay hearing. Discovery samples at most169 nearby cells
    /// and512 owners on the existing context boundary. Idle revalidates at most seven known sources.
    /// Only an already-visible source can produce a positioned cue; habitat texture carries no entity information.</summary>
    public sealed class WorldAudioSoundscape
    {
        const int Radius=6,OwnerBudget=512;
        enum Kind{Water,Fire,Frog,Dog,Wood,Stone,Bird}
        readonly Entity[] sources=new Entity[7];
        readonly int[] distances=new int[7];
        readonly WorldAudioPlayer audio;
        Zone zone;Entity actor;BiomeType biome;DayBand band;int depth;
        bool active,interior;float remaining;uint random;int cursor;
        string habitat;
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
                ClearDetails();random=Hash(current?.ZoneID);cursor=(int)(random%7);
                remaining=5+Next()*5;
            }
            bool policyChanged=biome!=region||depth!=level||band!=time;
            zone=current;actor=listener;biome=region;depth=level;band=time;
            if(policyChanged)ClearDetails();
            ValidateDetails();
            Array.Clear(sources,0,sources.Length);LastVisitedOwners=0;
            var origin=actor==null?null:zone?.GetEntityCell(actor);
            active=visible&&origin!=null&&Alive(actor)&&zone.ZoneID!=WorldMap.WorldMapZoneID;
            if(!active){habitat=null;SetBed(null);ClearDetails();return;}
            if(interior!=origin.IsInterior)ClearDetails();
            interior=origin.IsInterior;ScanCount++;
            for(int i=0;i<distances.Length;i++)distances[i]=int.MaxValue;
            for(int y=Mathf.Max(0,origin.Y-Radius);y<=Mathf.Min(Zone.Height-1,origin.Y+Radius)&&LastVisitedOwners<OwnerBudget;y++)
                for(int x=Mathf.Max(0,origin.X-Radius);x<=Mathf.Min(Zone.Width-1,origin.X+Radius)&&LastVisitedOwners<OwnerBudget;x++)
                {
                    var cell=zone.GetCell(x,y);if(!cell.IsVisible||!cell.Explored||cell.IsInterior!=interior)continue;
                    for(int j=0;j<cell.Objects.Count&&LastVisitedOwners<OwnerBudget;j++)
                    {
                        LastVisitedOwners++;var owner=cell.Objects[j];if(!Current(owner,out _))continue;
                        int kind=Classify(owner);if(kind<0)continue;
                        int distance=Math.Max(Math.Abs(x-origin.X),Math.Abs(y-origin.Y));
                        if(distance<distances[kind]){sources[kind]=owner;distances[kind]=distance;}
                    }
                }
            SelectBed();
        }
        public void Tick(float wallDelta,bool worldVisible,bool danger)
        {
            if(!active||!worldVisible||!Alive(actor)||zone?.GetEntityCell(actor)==null)
            {active=false;Array.Clear(sources,0,sources.Length);habitat=null;SetBed(null);ClearDetails();return;}
            if(interior!=zone.GetEntityCell(actor).IsInterior){interior=zone.GetEntityCell(actor).IsInterior;ClearDetails();}
            ValidateDetails();
            for(int i=0;i<sources.Length;i++)if(sources[i]!=null&&(!Current(sources[i],out _)||Classify(sources[i])!=i))
            {ClearDetails();sources[i]=null;}
            SelectBed();
            // Muting/menus never accumulate a backlog to replay at once.
            if(WorldAudioSettings.AmbienceVolume<=0||AudioListener.pause){remaining=Math.Max(remaining,5);ClearDetails();return;}
            remaining-=WorldAudioPlayer.SafeDelta(wallDelta);if(remaining>0)return;
            remaining=(10+Next()*12)*(biome==BiomeType.Overwrit?2:1)*(danger?1.8f:1);
            int choices=SourceCount+HabitatDetailCount;if(choices==0)return;
            int chosen=(cursor+++(int)(Next()*choices))%choices;
            for(int i=0;i<sources.Length;i++)
            {
                var owner=sources[i];if(owner==null)continue;if(chosen-->0)continue;
                if(!Current(owner,out var cell))return;
                var origin=zone.GetEntityCell(actor);int distance=Math.Max(Math.Abs(origin.X-cell.X),Math.Abs(origin.Y-cell.Y));
                float gain=(interior?.65f:.7f)*(1f-distance/9f)*(danger?.45f:1);
                if(audio.TryPlayDetail(Id((Kind)i),(cell.X-origin.X)/10f,gain))
                    for(int w=0;w<detailWitnesses.Length;w++)if(detailWitnesses[w]==null){detailWitnesses[w]=owner;detailKinds[w]=i;break;}
                return;
            }
            if(habitat!=null)audio.TryPlayDetail(habitat,(Next()-.5f)*.7f,danger?.2f:.38f);
        }
        void ClearDetails(){audio.ClearDetails();Array.Clear(detailWitnesses,0,detailWitnesses.Length);}
        void ValidateDetails()
        {
            if(audio.ActiveDetailSources==0){Array.Clear(detailWitnesses,0,detailWitnesses.Length);return;}
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
            var water=sources[(int)Kind.Water];
            if(water!=null&&water.GetPart<LiquidPoolPart>()?.LiquidId=="water"
                &&(water.HasTag("FlowsEast")||water.HasTag("FlowsSouth")||water.BlueprintName=="SprayPool"||water.BlueprintName=="HelmwoodWaterPassage"))AmbienceId="water";
        }
        bool Current(Entity owner,out Cell cell)
        {
            cell=null;if(owner==null||actor==null||zone==null||owner==actor)return false;
            var origin=zone.GetEntityCell(actor);cell=zone.GetEntityCell(owner);
            if(origin==null||cell==null||origin.ParentZone!=zone||cell.ParentZone!=zone||actor.SpatialZone!=zone||owner.SpatialZone!=zone||!cell.Objects.Contains(owner)||!cell.Explored||!cell.IsVisible||cell.IsInterior!=origin.IsInterior
                ||Math.Abs(cell.X-origin.X)>Radius||Math.Abs(cell.Y-origin.Y)>Radius)return false;
            var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
            return render?.ParentEntity==owner&&render.Visible&&physics?.ParentEntity==owner&&physics.InInventory==null&&physics.Equipped==null;
        }
        int Classify(Entity owner)
        {
            var destructible=owner.GetPart<DestructiblePart>();
            if(destructible!=null&&(destructible.ParentEntity!=owner||destructible.HP<=0||destructible.Gone))return -1;
            var pool=owner.GetPart<LiquidPoolPart>();
            if(pool?.ParentEntity==owner&&pool.Volume>0&&(pool.LiquidId=="water"||pool.LiquidId=="brine"||pool.LiquidId=="bog-mire"))return (int)Kind.Water;
            if(Flame(owner))return (int)Kind.Fire;
            if(owner.HasTag("Creature")&&Alive(owner)&&owner.GetPart<BrainPart>()?.ParentEntity==owner)
            {
                switch(owner.BlueprintName)
                {
                    case "Bandfrog":case "Reedfrog":case "GinFrog":case "MawToad":case "HelmwoodFrog":return (int)Kind.Frog;
                    case "PetDog":return (int)Kind.Dog;
                    case "Magpie":if(depth<=0&&(band==DayBand.Dawn||band==DayBand.Height))return (int)Kind.Bird;break;
                }
                return -1; // CascadeFather is lore-silent; SummitSinger needs a single-note recording.
            }
            if(owner.BlueprintName=="Tree"||owner.BlueprintName=="Reeds"||owner.GetPart<DoorPart>()!=null&&owner.GetPart<MaterialPart>()?.MaterialID=="Wood")return (int)Kind.Wood;
            if((biome==BiomeType.Ruins||biome==BiomeType.Stump)&&owner.HasTag("Wall"))return (int)Kind.Stone;
            return -1;
        }
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
        static string Id(Kind kind)
        {switch(kind){case Kind.Water:return "water-lap";case Kind.Fire:return "fire";case Kind.Frog:return "frog";case Kind.Dog:return "dog";case Kind.Wood:return "wood-creak";case Kind.Stone:return "stone-tick";default:return "bird";}}
        static uint Hash(string id){unchecked{uint h=2166136261;foreach(char c in id??"")h=(h^c)*16777619;return h==0?1:h;}}
        float Next(){random=unchecked(random*1664525+1013904223);return (random>>8)*(1f/16777216);}
    }
}
