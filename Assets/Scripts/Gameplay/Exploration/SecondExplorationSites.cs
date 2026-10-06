using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Optional additions after the existing cold pipeline has accepted.
    /// No access/load hook calls this installer. Saved empty destinations are retained.</summary>
    public static partial class SecondExplorationSites
    {
        public const int FirstVersion = 15;
        public const string RoleKey = "SecondExploration.Role", PenZone = "Overworld.12.10.0";
        static readonly string[] Destinations = { QuillholdCompositionPlan.ZoneID, CinderholdCompositionPlan.ZoneID,
            TallyCompositionPlan.ZoneID, WellmeetCompositionPlan.ZoneID, "Overworld.4.6.1", "Overworld.2.7.1", "Overworld.2.7.2",
            SoddenDistrictPlan.CrossingZoneID, SoddenDistrictPlan.WorksZoneID, PenZone, OverworldZoneManager.AbandonedCounterZoneA };
        static readonly ConditionalWeakTable<Zone, object> installed = new ConditionalWeakTable<Zone, object>();
        public static bool Retain(OverworldZoneManager manager, string id) => manager?.Exploration?.Enabled == true && manager.Exploration.Version >= FirstVersion && Destinations.Contains(id);
        internal static void Install(OverworldZoneManager manager, Zone zone)
        {
            if (manager?.Exploration?.Enabled != true || manager.Exploration.Version < FirstVersion || zone == null
                || manager.CachedZones.TryGetValue(zone.ZoneID, out var cached) && cached != zone || installed.TryGetValue(zone, out _)) return;
            installed.Add(zone, new object());
            // Existing optional collectors and visible spike mechanisms gain verbs
            // on the same accepted owners; no collector, trap or loot is spawned here.
            Packet(manager, zone, p =>
            {
                foreach (var owner in p.Before)
                {
                    if (owner.GetPart<SpreadCollectorPart>()?.Configured == true) p.AddPart(owner, new CollectorBarterPart());
                    if (owner.BlueprintName == "SpikeTrap" && TrapJammingPart.IsSupported(owner)
                        && (Retain(manager, zone.ZoneID) || manager.Exploration.RetainsGeneratedGraph(manager, zone)))
                        p.AddPart(owner, new TrapSalvagePart());
                    if (Destinations.Contains(zone.ZoneID) && new[] { "Villager", "PeatCutter", "Scribe", "Weaponsmith", "Quartermaster", "Merchant", "TentRightHost", "SaltMaster" }.Contains(owner.BlueprintName))
                    { p.AddPart(owner, new CivilianCourtesyPart()); p.AddPart(owner, new CivilianAidPart()); p.AddPart(owner, new CivilianEquipmentGiftPart()); }
                    if (owner.HasTag("Graveyard")) p.AddPart(owner, new BurialPart());
                }
            });
            switch (zone.ZoneID)
            {
                case QuillholdCompositionPlan.ZoneID: Packet(manager, zone, Archive); break;
                case CinderholdCompositionPlan.ZoneID: Packet(manager, zone, Smith); break;
                case TallyCompositionPlan.ZoneID: Packet(manager, zone, Exchange); break;
                case WellmeetCompositionPlan.ZoneID: Packet(manager, zone, Guest); break;
                case "Overworld.4.6.1": Packet(manager, zone, DescentLamp); break;
                case "Overworld.2.7.1": case "Overworld.2.7.2": Packet(manager, zone, RopeLanding); break;
                case SoddenDistrictPlan.WorksZoneID: Packet(manager, zone, Works); break;
                case SoddenDistrictPlan.CrossingZoneID: Packet(manager, zone, Passage); break;
                case PenZone: Packet(manager, zone, Pen); break;
                case OverworldZoneManager.AbandonedCounterZoneA: Packet(manager, zone, CounterStore); break;
            }
        }
        static void Packet(OverworldZoneManager manager, Zone zone, Action<PacketPlan> plan)
        {
            using (var p = new PacketPlan(manager, zone))
                try { plan(p); p.Commit(); }
                catch (Exception ex) { Diag.Record("worldgen", "SecondExplorationRejected", payload: new { zoneId = zone.ZoneID, reason = ex.Message }); }
        }
        static void Archive(PacketPlan p)
        {
            var scribe = p.One("Scribe"); p.AddPart(scribe, new ScribeCopyServicePart());
            var shelf = p.Make("QuillholdLoanShelf", "loan-shelf");
            p.StockNew(shelf, "QuillholdLoanWardGleam"); p.StockNew(shelf, "QuillholdLoanDryingBreeze");
            p.Near(shelf, p.Zone.GetEntityPosition(scribe), 22);
            p.Explain(scribe, "I can copy the exact original you select for one ink vial and five drams. The separate loan shelf lends its two utility volumes for Ink; study is permanent, the physical loan can be returned.");
        }
        static void Smith(PacketPlan p)
        {
            var smith = p.One("Weaponsmith"); p.AddPart(smith, new ArtisanRepairServicePart()); p.AddPart(smith, new LocksmithServicePart());
            foreach (string material in new[] { "SteelBladeComponent", "SalvagedTimber", "LeatherBindingComponent" }) p.StockExisting(smith, material);
            p.Explain(smith, "I can repair selected damaged portable gear for eight drams while my finite materials last. Ordinary nearby chest locks cost six drams to open.");
        }
        static void Exchange(PacketPlan p)
        {
            var desk = p.One("Quartermaster"); p.AddPart(desk, new RentalDeskPart());
            p.Explain(desk, "Return just the loan you select for its stated Ink refund, or buy that exact loan permanently at its current drams quote. The old Ink fee is not refunded on a buyout.");
            var grave = p.Make("Graveyard", "rest-court-graveyard"); grave.AddPart(new BurialPart());
            var rest = p.Before.Where(e => e.HasPart<BedPart>()).OrderBy(e => p.Zone.GetEntityPosition(e).y).FirstOrDefault() ?? desk;
            p.Near(grave, p.Zone.GetEntityPosition(rest), 18);
            p.Explain(grave, "The rest-court burial ground can receive one carried ordinary body you select. The same body stays here; no payment or reputation is awarded.");
        }
        static void Guest(PacketPlan p)
        {
            var host = p.One("TentRightHost"); var chest = p.Make("WellmeetGuestLocker", "guest-locker");
            p.Near(chest, p.Zone.GetEntityPosition(host), 18);
            p.Explain(host, "A locker in the guest court can be claimed while the cloth protects you. Once claimed, its ordinary storage remains available after the oath expires.");
        }
        static void DescentLamp(PacketPlan p)
        {
            var jar = p.Before.Where(e => e.BlueprintName == "BeetleJar").OrderBy(e => p.Zone.GetEntityPosition(e).y).ThenBy(e => p.Zone.GetEntityPosition(e).x).FirstOrDefault();
            if (jar == null) throw new InvalidOperationException("missing-used-descent-jar");
            p.AddPart(jar, new RecoverableLampPart());
            p.Explain(jar, "This descent jar can be unhooked. The same lantern-beetles will light a hand-held jar, leaving this resting seat darker. It needs a real free hand to equip.");
        }
        static void RopeLanding(PacketPlan p)
        {
            bool down = p.Zone.ZoneID == "Overworld.2.7.1"; const int x = 23, y = 18;
            Entity anchor = p.Before.FirstOrDefault(e => e.BlueprintName == "RopeAnchor" && p.Zone.GetEntityPosition(e) == (x,y));
            if (anchor == null) { anchor = p.Make("RopeAnchor", "rope-landing"); p.At(anchor, x, y, bareInterior: true); }
            p.AddPart(anchor, new RopeShortcutPart { ZoneID = p.Zone.ZoneID, X = x, Y = y, OtherZoneID = down ? "Overworld.2.7.2" : "Overworld.2.7.1", OtherX = x, OtherY = y, Down = down });
            p.Explain(anchor, "Two knotflax cord will rig a reciprocal rope to the corresponding lower terrace. First visit both existing levels and confirm their landings; then use the ordinary stair controls at the installed line.");
        }
        static void Works(PacketPlan p)
        {
            var locker = p.One("SoddenWorksLocker");
            if (locker.HasPart<HandlingPart>() || locker.GetPart<ContainerPart>()?.Contents == null) throw new InvalidOperationException("changed-works-locker");
            p.AddPart(locker, new HandlingPart { Weight = 40, Carryable = false, MinLiftStrength = 0 }); p.AddPart(locker, new ContainerLoadPart());
            p.StockExisting(locker, "FilterHood"); p.StockExisting(locker, "AcidworkerApron");
            p.Explain(locker, "The works locker can be opened here or hauled as one physical load. Its actual contents add to the shell's weight; taking selected supplies first can make hauling easier. The same finite stock moves with it.");
        }
        sealed class PacketPlan : IDisposable
        {
            internal readonly OverworldZoneManager Manager; internal readonly Zone Zone; internal readonly Entity[] Before;
            readonly Func<bool> original; readonly List<(Entity e,int x,int y)> additions = new List<(Entity,int,int)>();
            readonly List<Func<bool>> modifications = new List<Func<bool>>(); readonly HashSet<Entity> modified = new HashSet<Entity>();
            readonly HashSet<Entity> created = new HashSet<Entity>(); readonly HashSet<string> ids = new HashSet<string>();
            readonly List<(Entity e,string id,string bp)> identities = new List<(Entity,string,string)>();
            readonly InventoryTransaction tx = new InventoryTransaction(); readonly SpreadWildernessSituationBuilder.Geometry geometry;
            bool committed;
            internal PacketPlan(OverworldZoneManager manager, Zone zone)
            { Manager = manager; Zone = zone; Before = zone.GetReadOnlyEntities().ToArray(); original = SpreadGenerationReceipt.CaptureFinalState(zone, Before); geometry = new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity>()); foreach (var e in Before) ids.Add(e.ID); }
            internal Entity One(string bp) => Before.FirstOrDefault(e => e.BlueprintName == bp) ?? throw new InvalidOperationException("missing-source:" + bp);
            internal Entity Make(string bp, string role = null)
            {
                if (!Manager.Factory.Blueprints.ContainsKey(bp) || !original()) throw new InvalidOperationException("missing-content-or-changed-source:" + bp);
                var e = Manager.Factory.CreateEntity(bp);
                if (!SecondExplorationActions.Fresh(e, bp) || !created.Add(e) || !ids.Add(e.ID) || !original()) throw new InvalidOperationException("invalid-created-owner:" + bp);
                identities.Add((e,e.ID,bp)); if (role != null) e.Properties[RoleKey] = role; return e;
            }
            internal void AddPart(Entity e, Part part)
            {
                if (e.Parts.Any(p => p.GetType() == part.GetType())) return;
                if (created.Contains(e)) { e.AddPart(part); return; }
                modified.Add(e); modifications.Add(() => { tx.Do(() => e.AddPart(part), () => e.RemovePart(part)); return part.ParentEntity == e && e.Parts.Contains(part); });
            }
            internal void Explain(Entity e, string text)
            {
                Action apply = () => e.GetPart<ExaminablePart>().Text += "\n" + text;
                if (e.GetPart<ExaminablePart>() == null) AddPart(e, new ExaminablePart());
                if (created.Contains(e)) { apply(); return; }
                modified.Add(e); modifications.Add(() => { var examine = e.GetPart<ExaminablePart>(); string old = examine.Text; tx.Do(apply, () => examine.Text = old); return true; });
            }
            internal void StockNew(Entity owner, string bp)
            {
                var item = Make(bp); var inv = owner.GetPart<InventoryPart>(); var container = owner.GetPart<ContainerPart>();
                if (!(inv != null ? inv.AddObject(item) : container?.AddItem(item) == true)) throw new InvalidOperationException("invalid-new-stock:" + bp);
            }
            internal void StockExisting(Entity owner, string bp)
            {
                var item = Make(bp); modified.Add(owner);
                modifications.Add(() =>
                {
                    var inv = owner.GetPart<InventoryPart>(); var c = owner.GetPart<ContainerPart>();
                    var receipt = inv != null ? InventoryTransferSnapshot.Capture(inv,item) : InventoryTransferSnapshot.Capture(c,item);
                    tx.Do(null,receipt.Restore); return receipt.Apply(() => inv != null ? inv.AddObject(item) : c.AddItem(item));
                });
            }
            internal void At(Entity e, int x, int y, bool bareInterior = false)
            {
                if (!Free(x,y,bareInterior)) throw new InvalidOperationException("occupied-packet-cell:"+x+","+y);
                additions.Add((e,x,y));
            }
            internal bool Free(int x, int y, bool bareInterior = false)
            {
                if (additions.Any(p=>p.x==x&&p.y==y) || !Zone.InBounds(x,y) || x<2 || y<2 || x>=78 || y>=23) return false;
                if (!bareInterior) return geometry.Place(x,y);
                var c = Zone.GetCell(x,y); return !c.BlocksMovement() && Zone.TileState.Get(x,y)?.IsEmpty!=false && c.Objects.All(DoorPart.IsBareGround);
            }
            internal void Near(Entity e, (int x,int y) anchor, int radius)
            {
                foreach (var at in from y in Enumerable.Range(2,21) from x in Enumerable.Range(2,76) let d = Math.Max(Math.Abs(x-anchor.x),Math.Abs(y-anchor.y)) where d<=radius orderby d,y,x select(x,y))
                {
                    if (!Free(at.x,at.y)) continue;
                    var positions=additions.Select(p=>(p.x,p.y)).Concat(new[]{at}).ToArray();
                    if (!geometry.PreservesAgainst(geometry,positions)) continue;
                    var blocked=new HashSet<(int,int)>(positions); var reached=geometry.Flood(blocked,null,int.MaxValue,null);
                    if (!new[]{(at.x-1,at.y),(at.x+1,at.y),(at.x,at.y-1),(at.x,at.y+1)}.Any(c=>geometry.Walk(c.Item1,c.Item2,blocked)&&reached[c.Item1,c.Item2])) continue;
                    additions.Add((e,at.x,at.y)); return;
                }
                throw new InvalidOperationException("no-safe-placement:"+e.BlueprintName);
            }
            internal void Commit()
            {
                if (modifications.Count==0 && additions.Count==0) { committed=true; tx.Commit(); return; }
                var positions=additions.Where(p=>p.e.GetPart<PhysicsPart>()?.Solid==true||p.e.HasTag("Creature")||p.e.GetPart<DoorPart>()?.IsClosed==true).Select(p=>(p.x,p.y)).ToArray();
                if (!original() || !geometry.PreservesAgainst(geometry,positions) || identities.Any(p=>p.e.ID!=p.id||p.e.BlueprintName!=p.bp)) throw new InvalidOperationException("changed-staged-packet");
                var untouched=SpreadGenerationReceipt.CaptureFinalState(Zone,Before.Where(e=>!modified.Contains(e)));
                foreach(var mutation in modifications) if(!mutation()) throw new InvalidOperationException("source-mutation-refused");
                if(!untouched()) throw new InvalidOperationException("changed-unrelated-source");
                var amended=SpreadGenerationReceipt.CaptureFinalState(Zone,Before);
                foreach(var p in additions)
                { tx.Do(null,()=>{if(p.e.SpatialZone==Zone)Zone.RemoveEntity(p.e);}); if(!Zone.AddEntity(p.e,p.x,p.y))throw new InvalidOperationException("placement-refused"); }
                if(!amended() || identities.Any(p=>p.e.ID!=p.id||p.e.BlueprintName!=p.bp) || additions.Any(p=>p.e.SpatialZone!=Zone||Zone.GetEntityPosition(p.e)!=(p.x,p.y)))throw new InvalidOperationException("changed-final-packet");
                Configure?.Invoke();
                tx.Commit();committed=true;Diag.Record("worldgen","SecondExplorationInstalled",payload:new{zoneId=Zone.ZoneID,added=additions.Count,amended=modified.Count});
            }
            internal bool Preserves((int x,int y)[] positions)=>geometry.PreservesAgainst(geometry,positions);
            internal (int x,int y) Planned(Entity e){var p=additions.Single(p=>p.e==e);return(p.x,p.y);}
            internal Action Configure;
            public void Dispose(){if(!committed)tx.Rollback();}
        }
    }
}
