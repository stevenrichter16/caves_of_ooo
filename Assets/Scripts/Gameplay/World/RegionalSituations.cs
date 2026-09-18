using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    public enum RegionalSituationKind { Supply, Recovery }

    /// <summary>Immutable first-wave binding; templates reuse native owners and
    /// verbs, while identities, destinations and outcomes remain explicit.</summary>
    public sealed class RegionalSituationDefinition
    {
        public readonly string Id, Title, RecipientZoneId, SourceZoneId, ItemBlueprint, RewardBlueprint;
        public readonly int ItemCount, RewardDrams;
        public readonly RegionalSituationKind Kind;
        public readonly BiomeType SourceBiome;
        internal readonly string RecipientBlueprint, ResidentId;
        internal readonly int SourceX,SourceY,RecipientX,RecipientY;
        internal readonly string RecipientName,RecipientProfile,RecipientFaction;
        internal RegionalSituationDefinition(string id, string title, string recipient, string source,
            string item, int count, int drams, string reward, RegionalSituationKind kind,
            BiomeType biome, string recipientBlueprint, string resident = null)
        {
            Id=id; Title=title; RecipientZoneId=recipient; SourceZoneId=source;
            ItemBlueprint=item; ItemCount=count; RewardDrams=drams; RewardBlueprint=reward;
            Kind=kind; SourceBiome=biome; RecipientBlueprint=recipientBlueprint; ResidentId=resident;
            var from=WorldMap.FromZoneID(source);SourceX=from.x;SourceY=from.y;
            var to=WorldMap.FromZoneID(recipient);RecipientX=to.x;RecipientY=to.y;
            var place=WorldMapAuthoring.PlaceAt(to.x,to.y);
            if(place.HasValue){RecipientName=place.Value.Name;RecipientProfile=place.Value.Profile;RecipientFaction=place.Value.Faction;}
        }
    }

    /// <summary>Five bounded native situations. Generation is cold-path only;
    /// cue and action queries never generate another chunk or repair its owners.</summary>
    public static class RegionalSituations
    {
        private static readonly RegionalSituationDefinition[] definitions =
        {
            new RegionalSituationDefinition("morrowfast-iron", "Iron for Orrit's repairs", "Overworld.3.6.0", "Overworld.1.5.0",
                "ChoirIron",1,8,"FireClay",RegionalSituationKind.Supply,BiomeType.Grovelands,null,"southwest-craftsperson"),
            new RegionalSituationDefinition("cinderhold-iron", "Iron for the pruning-post smith", "Overworld.6.6.0", "Overworld.5.6.0",
                "ChoirIron",1,8,"FireClay",RegionalSituationKind.Supply,BiomeType.Grovelands,"Weaponsmith"),
            new RegionalSituationDefinition("gantry-grain", "Grain for Gantry's exchange", "Overworld.7.8.0", "Overworld.7.7.0",
                "Emberwheat",2,6,"HealingTonic",RegionalSituationKind.Supply,BiomeType.Spread,"Merchant"),
            new RegionalSituationDefinition("sumphold-oil", "The stranded lamp-oil consignment", "Overworld.15.6.0", "Overworld.16.6.0",
                "WardOil",2,12,"SilverSand",RegionalSituationKind.Recovery,BiomeType.Sodden,"Merchant"),
            new RegionalSituationDefinition("wellmeet-filters", "The lost filter-sand consignment", "Overworld.8.16.0", "Overworld.8.17.0",
                "SilverSand",2,12,"FireClay",RegionalSituationKind.Recovery,BiomeType.Beating,"Merchant")
        };
        public static IReadOnlyList<RegionalSituationDefinition> Definitions { get; } = Array.AsReadOnly(definitions);
        public static RegionalSituationDefinition Find(string id)
        { foreach(var d in definitions) if(d.Id==id) return d; return null; }
        public static string InstanceId(RegionalSituationDefinition definition,int worldSeed)
            => definition == null ? null : "regional:"+worldSeed.ToString(CultureInfo.InvariantCulture)+":"+definition.Id;
        public static string SourceId(RegionalSituationDefinition definition,int worldSeed)
            => definition == null ? null : InstanceId(definition,worldSeed)+":source";
        internal const string CargoDefinition = "RegionalCargoDefinition", CargoInstance = "RegionalCargoInstance";
        internal const string HabitatIds = "RegionalHabitatIds", HabitatBonus = "RegionalHabitatBonus";
        private const string RecipientPrefix = "RegionalRecipient:";
        private const string CargoTag = "RegionalSituationCargo";
        private sealed class Authority
        {
            public Authority() { }
            internal readonly Dictionary<string,Entity> Recipients=new Dictionary<string,Entity>(StringComparer.Ordinal);
            internal readonly Dictionary<string,Entity> Cargo=new Dictionary<string,Entity>(StringComparer.Ordinal);
            internal readonly List<Entity> CargoCandidates=new List<Entity>(2);
        }
        private static readonly ConditionalWeakTable<Zone,Authority> authority = new ConditionalWeakTable<Zone,Authority>();

        /// <summary>Called only after a fresh native pipeline, before it is put
        /// into the cache. Explicit replay on a cached/restored graph is a no-op.</summary>
        public static void OnZoneGenerated(Zone zone,OverworldZoneManager manager)
        {
            if(zone==null || manager?.Factory==null || manager.CachedZones.ContainsKey(zone.ZoneID)) return;
            foreach(var d in definitions)
            {
                if(zone.ZoneID==d.SourceZoneId && SourceAllowed(d,manager)) InstallSource(zone,manager,d);
                if(zone.ZoneID==d.RecipientZoneId && RecipientMapAllowed(d,manager)) BindRecipient(zone,manager,d);
            }
        }

        internal static bool SourceAllowed(RegionalSituationDefinition d,OverworldZoneManager manager)
        {
            if(d==null||manager?.WorldMap==null)return false;
            return manager.WorldMap.GetBiome(d.SourceX,d.SourceY)==d.SourceBiome
                && manager.WorldMap.GetPOI(d.SourceX,d.SourceY)==null && !SinkholeSites.IsMouth(d.SourceX,d.SourceY);
        }
        internal static bool RecipientMapAllowed(RegionalSituationDefinition d,OverworldZoneManager manager)
        {
            if(d==null||manager?.WorldMap==null)return false;
            var poi=manager.WorldMap.GetPOI(d.RecipientX,d.RecipientY);
            return d.RecipientName!=null && poi?.Type==POIType.Village && poi.Name==d.RecipientName
                && poi.Profile==d.RecipientProfile && poi.Faction==d.RecipientFaction;
        }
        private static Entity StateOwner(Zone zone)
        {
            var cell=zone?.GetCell(0,0);if(cell==null)return null;
            foreach(var e in cell.Objects)if(e.HasTag("Terrain"))return e;
            return null;
        }
        private static bool RecipientShape(Entity e,RegionalSituationDefinition d)
            => e!=null && e.HasTag("Creature") && e.GetStatValue("Hitpoints")>0
                && !CombatSystem.IsDeathHandled(e) && e.HasPart<ConversationPart>() && e.HasPart<InventoryPart>()
                && e.HasPart<TraderPart>() && (d.ResidentId!=null || e.BlueprintName==d.RecipientBlueprint);
        private static void BindRecipient(Zone zone,OverworldZoneManager manager,RegionalSituationDefinition d)
        {
            var anchor=StateOwner(zone);if(anchor==null || !string.IsNullOrEmpty(anchor.GetProperty(RecipientPrefix+d.Id)))return;
            Entity recipient=null;
            if(d.ResidentId!=null)recipient=MorrowfastSceneRuntime.FindOwner(zone,d.ResidentId);
            else foreach(var e in zone.GetReadOnlyEntities())if(RecipientShape(e,d)){recipient=e;break;}
            if(!RecipientShape(recipient,d) || recipient.HasPart<RegionalRequestPart>())return;
            recipient.AddPart(new RegionalRequestPart { DefinitionId=d.Id,InstanceId=InstanceId(d,manager.WorldSeed),RecipientId=recipient.ID });
            anchor.Properties[RecipientPrefix+d.Id]=recipient.ID;
            authority.GetOrCreateValue(zone).Recipients[d.Id]=recipient;
        }
        /// <summary>O(1) after first restored-zone lookup. The saved binding is
        /// independent of mutable Part fields; a cloned/transplanted Part cannot
        /// turn another native person into the authorized recipient.</summary>
        internal static Entity Recipient(Zone zone,RegionalSituationDefinition d)
        {
            if(zone==null||d==null)return null;
            var index=authority.GetOrCreateValue(zone);
            if(index.Recipients.TryGetValue(d.Id,out var recipient))return recipient;
            string id=StateOwner(zone)?.GetProperty(RecipientPrefix+d.Id);
            if(!string.IsNullOrEmpty(id))foreach(var e in zone.GetReadOnlyEntities())
                if(e.ID==id){if(recipient!=null){recipient=null;break;}recipient=e;}
            index.Recipients[d.Id]=recipient;
            return recipient;
        }
        internal static Entity FindOwner(Zone zone,string id)
        { if(zone==null||string.IsNullOrEmpty(id))return null;foreach(var e in zone.GetReadOnlyEntities())if(e.ID==id)return e;return null; }
        private static Entity Create(EntityFactory factory,string blueprint)
        { if(factory==null||!factory.Blueprints.ContainsKey(blueprint))return null;try{return factory.CreateEntity(blueprint);}catch{return null;} }
        internal static Entity CreateItem(EntityFactory factory,string blueprint)
        {
            var e=Create(factory,blueprint);var p=e?.GetPart<PhysicsPart>();
            return e!=null&&e.BlueprintName==blueprint&&p!=null&&p.Takeable&&!p.Solid
                &&e.GetPart<RenderPart>()?.Visible==true&&e.HasPart<ExaminablePart>()
                &&(e.GetPart<StackerPart>()?.StackCount??1)==1?e:null;
        }

        private static bool DrySeat(Zone zone,int x,int y,bool[,] reachable)
        {
            if(x<3||x>=Zone.Width-3||y<3||y>=Zone.Height-3||!reachable[x,y]
                ||zone.GenReservedCells.Contains((x,y)))return false;
            var c=zone.GetCell(x,y);if(c.BlocksMovement()||c.IsInterior)return false;
            foreach(var e in c.Objects)
                if(!e.HasTag("Terrain")||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>()
                    ||e.BlueprintName!="Grass"&&e.BlueprintName!="Sand"&&e.BlueprintName!="Floor"&&e.BlueprintName!="RoadStone")return false;
            return c.Objects.Count>0;
        }
        private static bool[,] Flood(Zone zone)
        {
            var seen=new bool[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();
            for(int y=0;y<Zone.Height;y++)if(!zone.GetCell(0,y).BlocksMovement()){seen[0,y]=true;queue.Enqueue((0,y));break;}
            while(queue.Count>0)
            {
                var c=queue.Dequeue();foreach(var n in Neighbors(c.x,c.y))
                    if(zone.InBounds(n.x,n.y)&&!seen[n.x,n.y]&&!zone.GetCell(n.x,n.y).BlocksMovement())
                    {seen[n.x,n.y]=true;queue.Enqueue(n);}
            }
            return seen;
        }
        private static IEnumerable<(int x,int y)> Neighbors(int x,int y)
        {yield return(x-1,y);yield return(x+1,y);yield return(x,y-1);yield return(x,y+1);}
        private static bool HealthyHabitat(Entity e)
        {
            var d=e?.GetPart<DestructiblePart>();var gas=e?.GetPart<BurnOffGasPart>();
            return (e?.BlueprintName=="PeatBog"||e?.BlueprintName=="MirePool")
                &&d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible&&gas!=null&&gas.DamagePer>0&&gas.GasId=="marsh-gas";
        }
        private static void InstallSource(Zone zone,OverworldZoneManager manager,RegionalSituationDefinition d)
        {
            string id=SourceId(d,manager.WorldSeed),instance=InstanceId(d,manager.WorldSeed);
            if(FindOwner(zone,id)!=null)return;
            string blueprint=d.Kind==RegionalSituationKind.Recovery?"Sack":d.ItemBlueprint=="ChoirIron"?"ChoirIronVein":"RipeCropRow";
            var owner=Create(manager.Factory,blueprint);var physics=owner?.GetPart<PhysicsPart>();
            if(owner==null||physics==null||owner.GetPart<RenderPart>()?.Visible!=true||!owner.HasPart<ExaminablePart>())return;
            var staged=new List<Entity>{owner};
            if(d.Kind==RegionalSituationKind.Recovery)
            {
                if(physics.Solid)return;
                var container=owner.GetPart<ContainerPart>();if(container!=null){if(container.Contents.Count!=0)return;owner.RemovePart(container);}
                var stack=owner.GetPart<StackerPart>();if(stack!=null)owner.RemovePart(stack);
                var handling=owner.GetPart<HandlingPart>();if(handling!=null){handling.Carryable=true;handling.Throwable=true;handling.Weight=6;}
                // Ordinary world sacks are not takeable. This explicitly
                // authored, sealed consignment is a portable six-pound owner.
                physics.Weight=6;physics.Takeable=true;
                if(!owner.HasPart<DestructiblePart>())owner.AddPart(new DestructiblePart{HP=12,MaxHP=12});
                var damage=owner.GetPart<DestructiblePart>();if(damage.HP<=0||damage.Gone||damage.Indestructible)return;
                owner.Properties[CargoDefinition]=d.Id;owner.Properties[CargoInstance]=instance;
                // Set before AddEntity so the native zone tag index and saves
                // include this portable owner wherever it is later dropped.
                owner.Tags[CargoTag]="";
                owner.AddPart(new RegionalCargoPart());
                owner.GetPart<RenderPart>().DisplayName=d.Id=="sumphold-oil"?"sealed lamp-oil consignment":"sealed filter-sand consignment";
                owner.GetPart<ExaminablePart>().Text=d.Id=="sumphold-oil"
                    ?"A sealed consignment for Sumphold's merchant. Its oil belongs in trade, not in the peat. Burning this wet ground releases marsh gas; leaving the marked bank intact earns a small preservation payment."
                    :"Filter sand for Wellmeet's merchant. Height sun parches a bare head; headgear or actual shelter breaks the exposure. Carry the sealed consignment back intact.";
            }
            else if(d.ItemBlueprint=="ChoirIron")
            {
                var harvest=owner.GetPart<HarvestablePart>();
                if(!physics.Solid||!owner.HasTag("MineralVein")||harvest?.YieldBlueprint!="ChoirIron"||harvest.YieldMin<1||harvest.YieldChance!=100)return;
                owner.GetPart<ExaminablePart>().Text+=" A nearby request can also be supplied with iron brought or bought elsewhere. Digging here incurs the Choir's law.";
            }
            else
            {
                var harvest=owner.GetPart<FieldHarvestPart>();if(harvest==null||harvest.Harvested||harvest.YieldBlueprint!=d.ItemBlueprint||physics.Solid)return;
                for(int i=1;i<d.ItemCount;i++)
                {var row=Create(manager.Factory,blueprint);if(row?.GetPart<FieldHarvestPart>()==null||row.GetPart<PhysicsPart>()?.Solid!=false)return;staged.Add(row);}
            }
            // Validate native outcome dependencies before publishing a recoverable
            // promise; malformed content must not consume or replace scenery.
            if(CreateItem(manager.Factory,d.ItemBlueprint)==null||CreateItem(manager.Factory,d.RewardBlueprint)==null)return;
            var reachable=Flood(zone);Cell seat=null;var habitat=new List<Entity>();
            for(int y=3;y<Zone.Height-3&&seat==null;y++)for(int x=3;x<Zone.Width-3&&seat==null;x++)
            {
                if(!DrySeat(zone,x,y,reachable))continue;
                bool clear=true;foreach(var n in Neighbors(x,y))if(!zone.InBounds(n.x,n.y)||!reachable[n.x,n.y]||zone.GetCell(n.x,n.y).BlocksMovement())clear=false;
                if(!clear)continue;
                for(int i=1;i<staged.Count;i++)if(!DrySeat(zone,x+i,y,reachable))clear=false;
                if(!clear)continue;
                habitat.Clear();
                if(d.Id=="sumphold-oil")
                {
                    foreach(var e in zone.GetReadOnlyEntities())
                    {
                        if(!HealthyHabitat(e)||e.HasPart<RegionalHabitatPart>())continue;
                        var at=zone.GetEntityCell(e);
                        if(Math.Abs(at.X-x)+Math.Abs(at.Y-y)<=7)habitat.Add(e);
                    }
                    if(habitat.Count<2)continue;
                    habitat.Sort((a,b)=>{var ac=zone.GetEntityCell(a);var bc=zone.GetEntityCell(b);return (Math.Abs(ac.X-x)+Math.Abs(ac.Y-y)).CompareTo(Math.Abs(bc.X-x)+Math.Abs(bc.Y-y));});
                    if(habitat.Count>2)habitat.RemoveRange(2,habitat.Count-2);
                }
                seat=zone.GetCell(x,y);
            }
            if(seat==null)return;
            var added=new List<Entity>();
            for(int i=0;i<staged.Count;i++)
            {
                var e=staged[i];e.ID=i==0?id:id+":"+i;
                if(!zone.AddEntity(e,seat.X+i,seat.Y)){foreach(var a in added)zone.RemoveEntity(a);return;}
                added.Add(e);
            }
            if(d.Kind==RegionalSituationKind.Recovery)authority.GetOrCreateValue(zone).Cargo[d.Id]=owner;
            if(habitat.Count>0)
            {
                var ids=new List<string>();foreach(var e in habitat)
                {
                    var at=zone.GetEntityCell(e);
                    e.AddPart(new RegionalHabitatPart{InstanceId=instance,OriginalX=at.X,OriginalY=at.Y,InitialHP=e.GetPart<DestructiblePart>().HP});ids.Add(e.ID);
                    var examine=e.GetPart<ExaminablePart>();
                    if(examine!=null)examine.Text+=" This bank borders the stranded lamp-oil consignment. Sumphold's merchant pays a small extra amount if both nearby banks remain untouched.";
                    owner.GetPart<ExaminablePart>().Text+=" The bank at local ("+at.X+","+at.Y+") is included.";
                }
                owner.Properties[HabitatIds]=string.Join("|",ids);owner.SetIntProperty(HabitatBonus,3);
            }
            Diag.Record("worldgen","RegionalSituationSourcePlaced",target:owner,payload:new{definition=d.Id,zone=zone.ZoneID,instance});
        }
        internal static bool IsCargo(Entity item,RegionalSituationDefinition d,string instance,string sourceId=null)
        {
            var p=item?.GetPart<PhysicsPart>();var damage=item?.GetPart<DestructiblePart>();
            return item!=null&&item.ID==(sourceId??instance+":source")&&item.BlueprintName=="Sack"
                &&item.GetProperty(CargoDefinition)==d.Id&&item.GetProperty(CargoInstance)==instance
                &&p!=null&&p.Takeable&&!p.Solid&&item.GetPart<RenderPart>()?.Visible==true&&item.HasPart<ExaminablePart>()
                &&!item.HasPart<StackerPart>()&&(item.GetPart<ContainerPart>()?.Contents.Count??0)==0
                &&damage!=null&&damage.HP>0&&!damage.Gone;
        }
        /// <summary>Native inventory membership alone is insufficient: a stale
        /// list entry may be equipped or owned elsewhere. Keep this requirement
        /// local to delivery rather than changing general consumption behavior.</summary>
        internal static bool OwnedPayment(Entity actor,InventoryPart inventory,Entity item)
        {
            var physics=item?.GetPart<PhysicsPart>();
            return actor!=null&&inventory!=null&&physics!=null
                &&ReferenceEquals(physics.InInventory,actor)&&physics.Equipped==null
                &&inventory.CanConsumeOne(item);
        }

        /// <summary>Read-only cue check. Fresh source generation retains its
        /// actual cargo reference; a restored zone resolves once. Steady-state
        /// queries use only cached identity, physics and native spatial lookups,
        /// with no entity/inventory scan or per-frame identity allocation.</summary>
        internal static bool KnownCargoAvailable(OverworldZoneManager manager,Entity actor,
            RegionalSituationDefinition d,string instance,string sourceId)
        {
            if(!manager.CachedZones.TryGetValue(d.SourceZoneId,out var source))return true;
            var index=authority.GetOrCreateValue(source);
            if(!index.Cargo.TryGetValue(d.Id,out var cargo))
            {
                cargo=FindRestoredCargo(manager,actor,d,instance,sourceId,index.CargoCandidates);
                index.Cargo[d.Id]=cargo;
            }
            if(!IsCargo(cargo,d,instance,sourceId))return false;
            var physics=cargo.GetPart<PhysicsPart>();
            if(ReferenceEquals(physics.InInventory,actor)&&physics.Equipped==null)return true;
            return physics.InInventory==null&&physics.Equipped==null&&source.GetEntityCell(cargo)!=null;
        }
        // Taken fires after native packing, including extraction from a saved
        // container. Publish only the exact validated reference into its own
        // world's existing source index. If the surrounding command rolls back,
        // subsequent cue queries see the restored inventory owner and reject it.
        internal static void RegisterTakenCargo(Entity cargo,Entity actor)
        {
            var zone=actor?.SpatialZone;
            var manager=WorldLocationContext.For(zone);
            if(manager==null||zone==null||!manager.CachedZones.TryGetValue(zone.ZoneID,out var live)
                ||!ReferenceEquals(live,zone)||zone.GetEntityCell(actor)==null
                ||cargo==null||!cargo.HasTag(CargoTag))return;
            var d=Find(cargo.GetProperty(CargoDefinition));
            if(d?.Kind!=RegionalSituationKind.Recovery||!SourceAllowed(d,manager)
                ||!manager.CachedZones.TryGetValue(d.SourceZoneId,out var source))return;
            string instance=InstanceId(d,manager.WorldSeed);
            if(!IsCargo(cargo,d,instance)||!OwnedPayment(actor,actor.GetPart<InventoryPart>(),cargo))return;
            var index=authority.GetOrCreateValue(source);
            if(index.Cargo.TryGetValue(d.Id,out var known)&&known!=null&&!ReferenceEquals(known,cargo))return;
            index.Cargo[d.Id]=cargo;
        }
        // Cold restored-world resolution only. A parcel may have been dropped
        // in another cached chunk before saving; source-only lookup would cache
        // a false absence forever. Native tags avoid scanning terrain/actors, and
        // all later cue reads retain this same owner's changing physics state.
        private static Entity FindRestoredCargo(OverworldZoneManager manager,Entity actor,
            RegionalSituationDefinition d,string instance,string sourceId,List<Entity> candidates)
        {
            Entity found=null;
            foreach(var zone in manager.CachedZones.Values)
            {
                zone.GetEntitiesWithTagNonAlloc(CargoTag,candidates);
                foreach(var candidate in candidates)
                {
                    if(!IsCargo(candidate,d,instance,sourceId))continue;
                    var physics=candidate.GetPart<PhysicsPart>();
                    if(physics.InInventory!=null||physics.Equipped!=null||zone.GetEntityCell(candidate)==null)continue;
                    if(found!=null&&!ReferenceEquals(found,candidate)){candidates.Clear();return null;}
                    found=candidate;
                }
            }
            candidates.Clear();
            var inventory=actor?.GetPart<InventoryPart>();
            if(inventory!=null)foreach(var candidate in inventory.Objects)
            {
                if(!IsCargo(candidate,d,instance,sourceId)||!OwnedPayment(actor,inventory,candidate))continue;
                if(found!=null&&!ReferenceEquals(found,candidate))return null;
                found=candidate;
            }
            return found;
        }
        internal static Entity CarriedCargo(Entity actor,RegionalSituationDefinition d,string instance)
        {
            var inv=actor?.GetPart<InventoryPart>();if(inv==null)return null;
            foreach(var e in inv.Objects)if(IsCargo(e,d,instance)&&OwnedPayment(actor,inv,e))return e;return null;
        }
        internal static int PreservationBonus(Entity cargo,Zone source,string instance)
        {
            string ids=cargo?.GetProperty(HabitatIds);if(source==null||string.IsNullOrEmpty(ids))return 0;
            var expected=ids.Split('|');if(expected.Length!=2||expected[0]==expected[1])return 0;
            foreach(string id in expected)
            {
                var owner=FindOwner(source,id);var p=owner?.GetPart<RegionalHabitatPart>();var at=owner==null?null:source.GetEntityCell(owner);
                if(p==null||p.InstanceId!=instance||p.Disturbed||!HealthyHabitat(owner)||at==null
                    ||at.X!=p.OriginalX||at.Y!=p.OriginalY||owner.GetPart<DestructiblePart>().HP!=p.InitialHP)return 0;
            }
            return cargo.GetIntProperty(HabitatBonus)==3?3:0;
        }
    }
}
