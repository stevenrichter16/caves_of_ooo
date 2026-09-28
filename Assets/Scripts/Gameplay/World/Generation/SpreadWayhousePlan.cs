using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One independent new-world expedition address. Restoring missing
    /// metadata disables installation; no read path generates a destination.</summary>
    public sealed class SpreadWayhousePlan
    {
        public const string PropertyKey = "SpreadWayhouse.Selection.v1";
        public const string InstallationKey = "SpreadWayhouse.Installation.v1";
        public bool Initialized { get; private set; }
        public string ZoneID { get; private set; } = "";
        internal Zone PendingZone, CommittedZone;
        internal Func<bool> PendingValidation;

        internal bool FinalizeGenerated(Zone zone)
        {
            if(!ReferenceEquals(PendingZone,zone))return true;
            var validate=PendingValidation;PendingZone=null;PendingValidation=null;
            bool accepted=validate!=null&&validate();
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen",accepted?"SpreadWayhouseCommitted":"SpreadWayhouseRejected",
                payload:new{zone=zone.ZoneID,reason=accepted?"complete-current-packet":"changed-final-packet"});
            if(!accepted)return false;
            CommittedZone=zone;return true;
        }

        /// <summary>Only the finally accepted graph is protected, never an address alone.</summary>
        internal bool Retain(OverworldZoneManager manager,string id)
            => Initialized && id==ZoneID && CommittedZone!=null
                && manager.CachedZones.TryGetValue(id,out var cached)&&ReferenceEquals(cached,CommittedZone);

        public static SpreadWayhousePlan Create(OverworldZoneManager manager)
        {
            var plan = new SpreadWayhousePlan { Initialized = true };
            uint best = uint.MaxValue;
            int bestBand = int.MaxValue;
            var start = WorldMap.FromZoneID(WorldMap.StartingZoneID);
            for (int y=0; y<WorldMap.Height; y++) for (int x=0; x<WorldMap.Width; x++)
            {
                string id = WorldMap.ToZoneID(x,y,0);
                if (!IsEligible(manager,id)) continue;
                int distance = Math.Max(Math.Abs(x-start.x),Math.Abs(y-start.y));
                int band = distance>=2 && distance<=4 ? 0 : 1;
                uint rank = Rank(manager.WorldSeed,id);
                if (band>bestBand || (band==bestBand && rank>=best)) continue;
                bestBand=band;best=rank;plan.ZoneID=id;
            }
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen","SpreadWayhouseSelected",payload:new{version=1,zone=plan.ZoneID});
            return plan;
        }

        public static bool IsEligible(OverworldZoneManager manager,string id)
        {
            if (!SpreadRareEncounterPlan.IsEligible(manager,id)) return false;
            if (manager.RareEncounters?.PairZoneID==id || manager.RareEncounters?.ViperZoneID==id) return false;
            var formation=FormationSelector.For(BiomeType.Spread,id);
            if (formation!=Formation.OldRoad && formation!=Formation.Fallow) return false;
            foreach(var definition in RegionalSituations.Definitions)
                if(definition.SourceZoneId==id || definition.RecipientZoneId==id)return false;
            return true;
        }

        public bool Selects(OverworldZoneManager manager,string id)
            => Initialized && !string.IsNullOrEmpty(ZoneID) && ZoneID==id && IsEligible(manager,id);

        public static Entity BindForSave(OverworldZoneManager manager,Entity world)
        {
            if(manager?.Wayhouse?.Initialized!=true)return world;
            if(world==null){world=new Entity{BlueprintName="World",ID=Guid.NewGuid().ToString("N")};world.SetTag("WorldEntity");}
            world.Properties[PropertyKey]="1|"+manager.Wayhouse.ZoneID;
            if(manager.Wayhouse.Retain(manager,manager.Wayhouse.ZoneID))
                world.Properties[InstallationKey]="1|"+manager.Wayhouse.ZoneID;
            else world.Properties.Remove(InstallationKey);
            return world;
        }

        /// <summary>Save decoding supplies an exact cached graph, without rebuilding any owner.</summary>
        internal void RestoreInstalledGraph(OverworldZoneManager manager,Entity world)
        {
            if(!Initialized||string.IsNullOrEmpty(ZoneID)||manager==null||world==null)return;
            if(world.GetProperty(InstallationKey)=="1|"+ZoneID && manager.CachedZones.TryGetValue(ZoneID,out var zone))
                CommittedZone=zone;
        }

        public static SpreadWayhousePlan Restore(Entity world)
        {
            var plan=new SpreadWayhousePlan();
            if(world==null || !world.Properties.TryGetValue(PropertyKey,out string value)
                || value==null || !value.StartsWith("1|",StringComparison.Ordinal))return plan;
            string id=value.Substring(2);
            if(id.Length>64)return plan;
            if(id.Length>0)
            {
                var at=WorldMap.FromZoneID(id);
                if(!WorldMapAuthoring.InBounds(at.x,at.y)||at.z!=0||id!=WorldMap.ToZoneID(at.x,at.y,0))return plan;
            }
            plan.Initialized=true;plan.ZoneID=id;return plan;
        }

        private static uint Rank(int seed,string id)
        {
            unchecked{uint h=2166136261u^(uint)seed;foreach(char c in "SpreadWayhouse.v1|"+id)h=(h^c)*16777619u;return h;}
        }
    }
}
