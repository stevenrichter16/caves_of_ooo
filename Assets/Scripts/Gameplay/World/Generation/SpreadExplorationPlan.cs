using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    public enum SpreadExplorationFamily { None, RoadSpill, OccupiedBank, LastGleanings, WateringMargin, SnakeForage, WorkGang, CollectorReturn, RoadsideExchange, FieldPassage, CoolingWorkPatch, HeavySalvage, HuntThroughCover, FieldAlembic, TemperingShelter, TrappersStore, SeedKeepersPlot, WaysideKitchen, WetCrossing, HeavyFrame }

    /// <summary>Frozen assignment, not evidence that its optional content was placed.</summary>
    public sealed class SpreadExplorationEntry
    {
        public string ZoneID { get; }
        public bool PlacementEligible { get; }
        public bool PersistenceCovered => true;
        public SpreadExplorationFamily Family { get; }
        public SpreadExplorationTopology Topology { get; }
        public int ActorSeed { get; }
        public int RewardSeed { get; }
        internal SpreadExplorationEntry(string id, bool placement, SpreadExplorationFamily family,
            SpreadExplorationTopology topology, int seed)
        {
            ZoneID=id; PlacementEligible=placement; Family=family; Topology=topology;
            ActorSeed=unchecked((int)SpreadExplorationPlan.Rank(seed,id,"actor"));
            RewardSeed=unchecked((int)SpreadExplorationPlan.Rank(seed,id,"reward"));
        }
    }

    /// <summary>Finite new-world assignments and exact accepted graph authority. Disabled legacy
    /// worlds allocate no manifest or generation guard. Reads never generate or consume RNG.</summary>
    public sealed class SpreadExplorationPlan
    {
        public const string PropertyKey="SpreadExploration.Manifest";
        public const int CurrentVersion=14;
        public const string WorldKeyProperty="SpreadExploration.WorldKey";
        public string WorldKey { get; private set; } = "";
        private const int MaxRecords=WorldMap.Width*WorldMap.Height;
        private const int MaxWireLength=65536;
        private readonly OverworldZoneManager owner;
        private readonly bool bound;
        private readonly Dictionary<string,SpreadExplorationEntry> entries;
        private readonly Dictionary<string,Zone> installed;
        private readonly Dictionary<string,int> dispositions;
        private bool validatingAcceptedEntry;
        public bool Enabled { get; }
        public int Version { get; }
        public int AllocationChecks { get; private set; }
        public IReadOnlyList<SpreadExplorationEntry> Entries { get; }
        public int RetainedGraphCount => installed?.Count??0;
        private static readonly IReadOnlyList<SpreadExplorationEntry> Empty=Array.AsReadOnly(new SpreadExplorationEntry[0]);

        private SpreadExplorationPlan(OverworldZoneManager manager,bool enabled,bool isBound,
            IEnumerable<SpreadExplorationEntry> rows=null,int version=CurrentVersion)
        {
            owner=manager;Enabled=enabled;bound=isBound;Version=version;
            if(!enabled){Entries=Empty;return;}
            entries=new Dictionary<string,SpreadExplorationEntry>(StringComparer.Ordinal);
            installed=new Dictionary<string,Zone>(StringComparer.Ordinal);
            dispositions=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(var row in rows??Enumerable.Empty<SpreadExplorationEntry>())entries.Add(row.ZoneID,row);
            Entries=new ReadOnlyCollection<SpreadExplorationEntry>(entries.Values.OrderBy(e=>e.ZoneID,StringComparer.Ordinal).ToArray());
        }
        internal static SpreadExplorationPlan Legacy(OverworldZoneManager manager)=>new SpreadExplorationPlan(manager,false,true);
        internal static SpreadExplorationPlan Unbound(OverworldZoneManager manager)=>new SpreadExplorationPlan(manager,false,false);

        public static SpreadExplorationPlan Create(OverworldZoneManager manager)
        {
            if(manager==null)throw new ArgumentNullException(nameof(manager));
            if(manager.CachedZoneCount!=0)throw new InvalidOperationException("Exploration initialization must precede all graph access.");
            var rows=new Dictionary<string,SpreadExplorationEntry>(StringComparer.Ordinal);var eligible=new List<string>();
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                string id=WorldMap.ToZoneID(x,y,0);if(!CoveredNow(manager,id))continue;
                bool placement=PlacementNow(manager,id);rows.Add(id,new SpreadExplorationEntry(id,placement,SpreadExplorationFamily.None,SpreadExplorationTopology.Legacy,manager.WorldSeed));
                if(placement)eligible.Add(id);
            }
            eligible.Sort((a,b)=>{int priority=(NearWorksite(a)?0:1).CompareTo(NearWorksite(b)?0:1);if(priority!=0)return priority;int order=Rank(manager.WorldSeed,a,"order").CompareTo(Rank(manager.WorldSeed,b,"order"));return order!=0?order:string.CompareOrdinal(a,b);});
            int checks=0;
            foreach(string id in eligible)
            {
                // Quiet rate and neighborhood diversity are tuning measurements. Exclusions and
                // edge adjacency are hard constraints; a refusal leaves the address quiet.
                var family=FamilyFor(FormationSelector.For(BiomeType.Spread,id),9,manager.WorldSeed,id);checks++;
                if((!NearWorksite(id)&&(int)family<13&&Rank(manager.WorldSeed,id,"quiet")%100<35)||family==SpreadExplorationFamily.None||Touches(rows,id,family))family=SpreadExplorationFamily.None;
                var topology=(SpreadExplorationTopology)(1+Rank(manager.WorldSeed,id,"topology")%3);
                rows[id]=new SpreadExplorationEntry(id,true,family,topology,manager.WorldSeed);
            }
            // The two connected services need stable ordinary addresses in every new world.
            // Only these two v11 addresses replace a prior optional family; all other v9
            // allocations and every saved v10 manifest remain literal.
            foreach(string id in eligible.Where(NearResident))
                rows[id]=new SpreadExplorationEntry(id,true,ResidentFamilyFor(manager.WorldSeed,id),rows[id].Topology,manager.WorldSeed);
            // Other domestic sites fill quiet rows without displacing older encounters.
            foreach(string id in eligible.OrderBy(id=>NearResident(id)?0:1)
                .ThenBy(id=>Rank(manager.WorldSeed,id,"resident-order")).ThenBy(id=>id,StringComparer.Ordinal))
            {
                if(rows[id].Family!=SpreadExplorationFamily.None)continue;
                var resident=ResidentFamilyFor(manager.WorldSeed,id);
                if(resident==SpreadExplorationFamily.None||Touches(rows,id,resident))continue;
                rows[id]=new SpreadExplorationEntry(id,true,resident,rows[id].Topology,manager.WorldSeed);
            }
            // One optional bounded satellite occupies a quiet ordinary address.
            // Its variant/address is frozen before any graph or source roll exists.
            string satellite=SatelliteAddress(rows,manager.WorldSeed);
            if(satellite!=null)rows[satellite]=new SpreadExplorationEntry(satellite,true,SatelliteFamily(manager.WorldSeed),rows[satellite].Topology,manager.WorldSeed);
            return new SpreadExplorationPlan(manager,true,true,rows.Values){AllocationChecks=checks,WorldKey=Guid.NewGuid().ToString("N")};
        }
        internal static uint Rank(int seed,string id,string salt)
        {unchecked{uint h=2166136261u^(uint)seed;foreach(char c in "SpreadExploration.v2|"+salt+"|"+id)h=(h^c)*16777619u;h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return h^(h>>16);}}
        private static bool NearWorksite(string id)=>id=="Overworld.11.9.0"||id=="Overworld.12.10.0"||id=="Overworld.11.11.0";
        private static bool NearResident(string id)=>id=="Overworld.11.8.0"||id=="Overworld.12.11.0";
        private static SpreadExplorationFamily SatelliteFamily(int seed)
            =>Rank(seed,ReferenceGladePlan.ZoneID,"satellite-family")%2==0?SpreadExplorationFamily.WetCrossing:SpreadExplorationFamily.HeavyFrame;
        private static string SatelliteAddress(Dictionary<string,SpreadExplorationEntry> rows,int seed)
            =>rows.Values.Where(e=>e.PlacementEligible&&(e.Family==SpreadExplorationFamily.None||(int)e.Family>=18))
                .Where(e=>{var p=WorldMap.FromZoneID(e.ZoneID);return Math.Abs(p.x-11)+Math.Abs(p.y-10)<=4;})
                .OrderBy(e=>{var f=FormationSelector.For(BiomeType.Spread,e.ZoneID);return f==Formation.Fallow?0:f==Formation.FlowerMeadow?1:f==Formation.OldRoad?2:3;})
                .ThenBy(e=>Rank(seed,e.ZoneID,"satellite-address")).ThenBy(e=>e.ZoneID,StringComparer.Ordinal).Select(e=>e.ZoneID).FirstOrDefault();
        private static SpreadExplorationFamily ResidentFamilyFor(int seed,string id)
        {
            if(id=="Overworld.11.8.0")return SpreadExplorationFamily.SeedKeepersPlot;
            if(id=="Overworld.12.11.0")return SpreadExplorationFamily.WaysideKitchen;
            uint quiet=Rank(seed,id,"quiet")%100;
            if(quiet<12||quiet>=22)return SpreadExplorationFamily.None;
            return Rank(seed,id,"resident-family")%2==0?SpreadExplorationFamily.SeedKeepersPlot:SpreadExplorationFamily.WaysideKitchen;
        }
        private static SpreadExplorationFamily FamilyFor(Formation formation,int version,int seed,string id)
        {
            // Fresh worlds put the first examples on three different approaches from the glade.
            // Protected addresses never reach assignment. Saved v2–v8 rows keep the old grammar.
            if(version>=9)
            {
                if(id=="Overworld.11.9.0")return SpreadExplorationFamily.FieldAlembic;
                if(id=="Overworld.12.10.0")return SpreadExplorationFamily.TemperingShelter;
                if(id=="Overworld.11.11.0")return SpreadExplorationFamily.TrappersStore;
                uint work=Rank(seed,id,"worksite")%3;
                uint quiet=Rank(seed,id,"quiet")%100;
                if(quiet>=22&&quiet<35&&(formation==Formation.Fallow||formation==Formation.FlowerMeadow))
                {
                    if(work==0)return SpreadExplorationFamily.FieldAlembic;
                    if(work==1)return SpreadExplorationFamily.TemperingShelter;
                    if(work==2)return SpreadExplorationFamily.TrappersStore;
                }
            }
            switch(formation){case Formation.OldRoad:return version>=4&&WorldTravellers.EntrySample(seed,id)%8==0?SpreadExplorationFamily.RoadsideExchange:SpreadExplorationFamily.RoadSpill;
                case Formation.Hedgerow:
                    if(version>=7){uint variant=Rank(seed,id,"family-variant")%4;return variant==0?SpreadExplorationFamily.OccupiedBank:variant==1?SpreadExplorationFamily.CollectorReturn:variant==2?SpreadExplorationFamily.FieldPassage:SpreadExplorationFamily.HeavySalvage;}
                    if(version>=5){uint variant=Rank(seed,id,"family-variant")%3;return variant==0?SpreadExplorationFamily.OccupiedBank:variant==1?SpreadExplorationFamily.CollectorReturn:SpreadExplorationFamily.FieldPassage;}
                    return version>=4&&Rank(seed,id,"family-variant")%2==0?SpreadExplorationFamily.CollectorReturn:SpreadExplorationFamily.OccupiedBank;
                case Formation.Fallow:return version>=8&&Rank(seed,id,"family-variant")%2==1?SpreadExplorationFamily.HuntThroughCover:version==2?SpreadExplorationFamily.OccupiedBank:SpreadExplorationFamily.WorkGang;
                case Formation.FlowerMeadow:return version==2?SpreadExplorationFamily.None:SpreadExplorationFamily.SnakeForage;
                case Formation.FieldStrips:return version>=6&&Rank(seed,id,"family-variant")%2==1?SpreadExplorationFamily.CoolingWorkPatch:SpreadExplorationFamily.LastGleanings;
                case Formation.RiverMeadow:return SpreadExplorationFamily.WateringMargin;
                default:return SpreadExplorationFamily.None;}
        }
        private static bool Touches(Dictionary<string,SpreadExplorationEntry> rows,string id,SpreadExplorationFamily family)
        {
            var p=WorldMap.FromZoneID(id);
            return Matches(rows,WorldMap.ToZoneID(p.x-1,p.y),family)||Matches(rows,WorldMap.ToZoneID(p.x+1,p.y),family)
                ||Matches(rows,WorldMap.ToZoneID(p.x,p.y-1),family)||Matches(rows,WorldMap.ToZoneID(p.x,p.y+1),family);
        }
        private static bool Matches(Dictionary<string,SpreadExplorationEntry> rows,string id,SpreadExplorationFamily family)
            =>rows.TryGetValue(id,out var e)&&e.Family==family;
        public SpreadExplorationEntry Find(string id)
            =>Enabled&&id!=null&&entries.TryGetValue(id,out var entry)?entry:null;
        public bool TryGetPlacement(OverworldZoneManager manager,string id,out SpreadExplorationEntry entry)
        {
            entry=null;if(!Current(manager))return false;var found=Find(id);
            if(found?.PlacementEligible!=true||!PlacementNow(manager,id))return false;entry=found;return true;
        }
        /// <summary>One dynamic exchange opportunity on the exact active, accepted graph.
        /// Reading it never generates a graph, exposes hidden actors, or consumes the opportunity.</summary>
        public bool TryGetAcceptedEntry(OverworldZoneManager manager,Zone zone,out SpreadExplorationEntry entry)
        {
            entry=null;
            if(zone==null||!Current(manager)||Version<4
                ||!ReferenceEquals(manager.ActiveZone,zone)||!TryGetPlacement(manager,zone.ZoneID,out var found)
                ||found.Family!=SpreadExplorationFamily.RoadsideExchange||DispositionFor(zone.ZoneID)!=1
                ||!installed.TryGetValue(zone.ZoneID,out var accepted)||!ReferenceEquals(accepted,zone)
                ||!manager.CachedZones.TryGetValue(zone.ZoneID,out var cached)||!ReferenceEquals(cached,zone))return false;
            entry=found;return true;
        }
        /// <summary>Commits a real entry-time source once. The transient proof must validate actual
        /// owners and placement; saved committed opportunities never replay this callback.</summary>
        public bool TryMarkAcceptedEntryCommitted(OverworldZoneManager manager,Zone zone,SpreadExplorationEntry exactEntry,Func<bool> validator)
        {
            if(validatingAcceptedEntry||validator==null||!TryGetAcceptedEntry(manager,zone,out var entry)||!ReferenceEquals(entry,exactEntry))return false;
            var map=manager.WorldMap;var rare=manager.RareEncounters;var wayhouse=manager.Wayhouse;
            string pair=rare?.PairZoneID,viper=rare?.ViperZoneID,wayhouseID=wayhouse?.ZoneID;
            validatingAcceptedEntry=true;
            bool proved;
            try{proved=validator();}finally{validatingAcceptedEntry=false;}
            if(!proved||!ReferenceEquals(map,manager.WorldMap)||!ReferenceEquals(rare,manager.RareEncounters)
                ||!ReferenceEquals(wayhouse,manager.Wayhouse)||pair!=manager.RareEncounters?.PairZoneID
                ||viper!=manager.RareEncounters?.ViperZoneID||wayhouseID!=manager.Wayhouse?.ZoneID
                ||!TryGetAcceptedEntry(manager,zone,out var current)||!ReferenceEquals(current,exactEntry))return false;
            dispositions[entry.ZoneID]=2;return true;
        }
        private bool Current(OverworldZoneManager manager)=>bound&&ReferenceEquals(owner,manager)&&ReferenceEquals(manager?.Exploration,this);
        private static bool Canonical(string id)
        {
            if(string.IsNullOrEmpty(id)||id.Length>32)return false;var p=WorldMap.FromZoneID(id);
            return WorldMapAuthoring.InBounds(p.x,p.y)&&p.z==0&&id==WorldMap.ToZoneID(p.x,p.y,0);
        }
        private static bool Supported(string id)
            =>Canonical(id)&&SpreadCompositionPlan.IsWildernessZone(id)&&id!=MultiCellPilotRuntime.ZoneID
                &&(id==ReferenceGladePlan.ZoneID||Array.IndexOf(OverworldZoneManager.AuthoredWildernessZoneIDs,id)<0);
        private static bool CoveredNow(OverworldZoneManager manager,string id)
        {
            if(manager?.WorldMap?.Tiles==null||manager.WorldMap.POIs==null||!Supported(id))return false;
            var p=WorldMap.FromZoneID(id);return manager.WorldMap.GetBiome(p.x,p.y)==BiomeType.Spread&&manager.WorldMap.GetPOI(p.x,p.y)==null;
        }
        private static bool PlacementNow(OverworldZoneManager manager,string id)
        {
            if(!CoveredNow(manager,id)||!SpreadRareEncounterPlan.IsEligible(manager,id)||id==manager.RareEncounters?.PairZoneID
                ||id==manager.RareEncounters?.ViperZoneID||id==manager.Wayhouse?.ZoneID)return false;
            foreach(var d in RegionalSituations.Definitions)if(id==d.SourceZoneId||id==d.RecipientZoneId)return false;
            return true;
        }

        internal void ValidateAccess(OverworldZoneManager manager,string id)
        {
            if(!bound)throw new InvalidDataException("Exploration metadata has not been restored.");
            if(!ReferenceEquals(owner,manager))throw new InvalidDataException("Exploration belongs to another world.");
            if(Enabled&&id!=null&&installed.TryGetValue(id,out var expected))ValidateInstalled(manager,id,expected);
        }
        private static void ValidateInstalled(OverworldZoneManager manager,string id,Zone expected)
        {
            if(expected==null||expected.ZoneID!=id||!manager.CachedZones.TryGetValue(id,out var actual)||!ReferenceEquals(actual,expected))
                throw new InvalidDataException("Retained exploration graph is missing or replaced: "+id);
        }
        internal bool Retain(OverworldZoneManager manager,string id)
        {ValidateAccess(manager,id);return Enabled&&id!=null&&installed.ContainsKey(id);}

        // Weak exact graph keys cannot pin failed attempts. Every pipeline retry replaces its
        // attempt before source callbacks run. Attachment alone never installs a graph.
        private sealed class Attempt
        {
            internal OverworldZoneManager Manager;internal SpreadExplorationPlan Plan;internal WorldMap Map;
            internal SpreadRareEncounterPlan Rare;internal SpreadWayhousePlan Wayhouse;internal string Pair,Viper,WayhouseID,ID;internal bool PlacementCommitted, Validating, Finalizing;internal Func<bool> FinalValidator;
        }
        private static readonly ConditionalWeakTable<Zone,Attempt> Attempts=new ConditionalWeakTable<Zone,Attempt>();
        internal IZoneBuilder Guard(OverworldZoneManager manager,string id)
            =>Current(manager)&&Find(id)!=null&&CoveredNow(manager,id)?new GenerationGuard(manager,this,id):null;
        private sealed class GenerationGuard:IZoneBuilder
        {
            private readonly OverworldZoneManager manager;private readonly SpreadExplorationPlan plan;private readonly string id;
            internal GenerationGuard(OverworldZoneManager m,SpreadExplorationPlan p,string zoneID){manager=m;plan=p;id=zoneID;}
            public string Name=>"SpreadExplorationAttempt";public int Priority=>int.MinValue;
            public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
            {
                if(zone==null)return false;Attempts.Remove(zone);
                if(!plan.Current(manager)||!ReferenceEquals(factory,manager.Factory)||zone.ZoneID!=id||!CoveredNow(manager,id)
                    ||manager.CachedZones.ContainsKey(id)||plan.installed.ContainsKey(id))return false;
                Attempts.Add(zone,new Attempt{Manager=manager,Plan=plan,Map=manager.WorldMap,Rare=manager.RareEncounters,Wayhouse=manager.Wayhouse,
                    Pair=manager.RareEncounters?.PairZoneID,Viper=manager.RareEncounters?.ViperZoneID,WayhouseID=manager.Wayhouse?.ZoneID,ID=id});return true;
            }
        }
        /// <summary>Source composers must use the exact current generation attempt, not an address or loaded graph.</summary>
        public bool TryGetGenerationEntry(OverworldZoneManager manager,Zone zone,out SpreadExplorationEntry entry)
        {
            entry=null;
            if(zone==null||!Attempts.TryGetValue(zone,out var a)||!ReferenceEquals(a.Plan,this)||!AttemptCurrent(a,manager,zone,zone.ZoneID)
                ||!TryGetPlacement(manager,zone.ZoneID,out var found))return false;
            entry=found;return true;
        }
        /// <summary>Token-only fixture mark. Runtime composers must use the final-validator overload.</summary>
        public bool TryMarkPlacementCommitted(OverworldZoneManager manager,Zone zone,SpreadExplorationEntry exactEntry)
            =>MarkPlacement(manager,zone,exactEntry,null);
        /// <summary>Validate the actual successful source packet now and again after generation
        /// callbacks. The callback is transient and never runs during saved graph restoration.</summary>
        public bool TryMarkPlacementCommitted(OverworldZoneManager manager,Zone zone,SpreadExplorationEntry exactEntry,Func<bool> finalValidator)
            =>finalValidator!=null&&MarkPlacement(manager,zone,exactEntry,finalValidator);
        private bool MarkPlacement(OverworldZoneManager manager,Zone zone,SpreadExplorationEntry exactEntry,Func<bool> validator)
        {
            if(!TryGetGenerationEntry(manager,zone,out var current)||!ReferenceEquals(current,exactEntry)
                ||current.Family==SpreadExplorationFamily.None||current.Family==SpreadExplorationFamily.RoadsideExchange||!Attempts.TryGetValue(zone,out var attempt)
                ||attempt.PlacementCommitted||attempt.Validating||attempt.Finalizing)return false;
            if(validator!=null)
            {
                attempt.Validating=true;
                try{if(!validator())return false;}finally{attempt.Validating=false;}
                if(!AttemptCurrent(attempt,manager,zone,zone.ZoneID))return false;
            }
            attempt.FinalValidator=validator;attempt.PlacementCommitted=true;return true;
        }
        /// <summary>0=not accepted, 1=accepted/no-site, 2=accepted placement; never regenerates.</summary>
        public int DispositionFor(string id)=>Enabled&&id!=null&&dispositions.TryGetValue(id,out int value)?value:0;

        private static bool AttemptCurrent(Attempt a,OverworldZoneManager manager,Zone zone,string id)
            =>Attempts.TryGetValue(zone,out var currentAttempt)&&ReferenceEquals(currentAttempt,a)
                &&ReferenceEquals(a.Manager,manager)&&a.Plan.Current(manager)&&ReferenceEquals(a.Map,manager.WorldMap)
                &&ReferenceEquals(a.Rare,manager.RareEncounters)&&ReferenceEquals(a.Wayhouse,manager.Wayhouse)
                &&a.Pair==manager.RareEncounters?.PairZoneID&&a.Viper==manager.RareEncounters?.ViperZoneID&&a.WayhouseID==manager.Wayhouse?.ZoneID
                &&a.ID==id&&zone.ZoneID==id&&CoveredNow(manager,id)&&!manager.CachedZones.ContainsKey(id)&&!a.Plan.installed.ContainsKey(id);

        internal static bool FinalizeGenerated(OverworldZoneManager manager,Zone zone,string id,bool accepted)
        {
            if(zone==null)return false;
            if(!Attempts.TryGetValue(zone,out var a))return accepted&&!(manager.Exploration?.Enabled==true&&manager.Exploration.Find(id)!=null&&CoveredNow(manager,id));
            try
            {
                if(a.Finalizing||a.Validating||!accepted||!AttemptCurrent(a,manager,zone,id))return false;
                a.Finalizing=true;
                if(a.FinalValidator!=null)
                {
                    a.Validating=true;
                    try{if(!a.FinalValidator())return false;}finally{a.Validating=false;}
                    if(!AttemptCurrent(a,manager,zone,id))return false;
                }
                a.Plan.installed.Add(id,zone);a.Plan.dispositions.Add(id,a.PlacementCommitted?2:1);return true;
            }
            finally{Attempts.Remove(zone);}
        }

        public static Entity BindForSave(OverworldZoneManager manager,Entity world)
        {
            var plan=manager?.Exploration;if(plan==null)return world;plan.ValidateAccess(manager,null);if(!plan.Enabled)return world;
            foreach(var pair in plan.installed)ValidateInstalled(manager,pair.Key,pair.Value);
            var wire=new StringBuilder();wire.Append(plan.Version).Append('|').Append(manager.WorldSeed.ToString(CultureInfo.InvariantCulture)).Append('|').Append(plan.Entries.Count);
            foreach(var e in plan.Entries)wire.Append('\n').Append(e.ZoneID).Append('|').Append(e.PlacementEligible?3:1).Append('|').Append((int)e.Family)
                .Append('|').Append((int)e.Topology).Append('|').Append(plan.DispositionFor(e.ZoneID));
            if(world==null){world=new Entity{BlueprintName="World",ID=Guid.NewGuid().ToString("N")};world.SetTag("WorldEntity");}
            world.Properties[PropertyKey]=wire.ToString();
            if(plan.Version>=11)world.Properties[WorldKeyProperty]=plan.WorldKey;
            return world;
        }
        internal static SpreadExplorationPlan Restore(OverworldZoneManager manager,Entity world)
        {
            if(world==null||!world.Properties.TryGetValue(PropertyKey,out string wire))return Legacy(manager);
            if(wire==null||wire.Length==0||wire.Length>MaxWireLength)throw Invalid("length");
            var lines=wire.Split('\n');var header=lines[0].Split('|');
            if(header.Length!=3)throw Invalid("header");
            int version=Number(header[0]);
            if((version!=2&&version!=3&&version!=4&&version!=5&&version!=6&&version!=7&&version!=8&&version!=9&&version!=10&&version!=11&&version!=12&&version!=13&&version!=CurrentVersion)||Number(header[1])!=manager.WorldSeed)throw Invalid("version/seed");
            string worldKey="";
            if(version>=11 && (!world.Properties.TryGetValue(WorldKeyProperty,out worldKey) || !Guid.TryParseExact(worldKey,"N",out _)))throw Invalid("world identity");
            int count=Number(header[2]);if(count<0||count>MaxRecords||lines.Length!=count+1)throw Invalid("record count");
            var rows=new Dictionary<string,SpreadExplorationEntry>(StringComparer.Ordinal);var saved=new Dictionary<string,int>(StringComparer.Ordinal);
            for(int i=1;i<lines.Length;i++)
            {
                var f=lines[i].Split('|');if(f.Length!=5||!Supported(f[0])||rows.ContainsKey(f[0]))throw Invalid("address");
                int mask=Number(f[1]),family=Number(f[2]),topology=Number(f[3]),disposition=Number(f[4]);
                if((mask!=1&&mask!=3)||family<0||family>(version==2?4:version==3?6:version==4?8:version==5?9:version==6?10:version==7?11:version==8?12:version==9?15:version==10?17:19)||topology<0||topology>3||disposition<0||disposition>2
                    ||(mask==1&&(family!=0||topology!=0))||(disposition==2&&(mask!=3||family==0)))throw Invalid("enum/mask/disposition");
                var selected=(SpreadExplorationFamily)family;
                var expected=version>=11&&family>=18?SatelliteFamily(manager.WorldSeed):version>=10&&family>=16?ResidentFamilyFor(manager.WorldSeed,f[0])
                    :FamilyFor(FormationSelector.For(BiomeType.Spread,f[0]),version,manager.WorldSeed,f[0]);
                if(selected!=SpreadExplorationFamily.None&&selected!=expected)throw Invalid("family habitat");
                if(mask==3&&(f[0]==ReferenceGladePlan.ZoneID||f[0]==manager.RareEncounters?.PairZoneID||f[0]==manager.RareEncounters?.ViperZoneID||f[0]==manager.Wayhouse?.ZoneID
                    ||RegionalSituations.Definitions.Any(d=>d.SourceZoneId==f[0]||d.RecipientZoneId==f[0])))throw Invalid("protected placement");
                rows.Add(f[0],new SpreadExplorationEntry(f[0],mask==3,selected,(SpreadExplorationTopology)topology,manager.WorldSeed));
                if(disposition>0)saved.Add(f[0],disposition);
            }
            if(version>=11)
            {
                var satellites=rows.Values.Where(e=>(int)e.Family>=18).ToArray();
                string expectedAddress=SatelliteAddress(rows,manager.WorldSeed);
                if(satellites.Length!=(expectedAddress==null?0:1)||(satellites.Length==1&&satellites[0].ZoneID!=expectedAddress))throw Invalid("satellite selection");
            }
            foreach(var e in rows.Values)if(e.Family!=SpreadExplorationFamily.None&&Touches(rows,e.ZoneID,e.Family))throw Invalid("adjacency");
            var result=new SpreadExplorationPlan(manager,true,true,rows.Values,version){WorldKey=worldKey};
            foreach(var record in saved)
            {
                string id=record.Key;
                if(!manager.CachedZones.TryGetValue(id,out var zone)||zone==null||zone.ZoneID!=id)throw Invalid("installed graph missing");
                result.installed.Add(id,zone);result.dispositions.Add(id,record.Value);
            }
            return result;
        }
        private static int Number(string text)
        {if(!int.TryParse(text,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int n)||n.ToString(CultureInfo.InvariantCulture)!=text)throw Invalid("number");return n;}
        private static InvalidDataException Invalid(string reason)=>new InvalidDataException("Invalid Spread exploration manifest: "+reason);
    }
}
