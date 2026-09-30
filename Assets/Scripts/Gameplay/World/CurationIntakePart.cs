using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One local, physical certification. Exact subject and bay references survive
    /// native saves; completion records a past arrangement, never locks future movement.</summary>
    public sealed class CurationIntakePart : Part
    {
        public override string Name => "CurationIntake";
        public const string CertifyCommand = "CertifyCurationIntake";
        public Entity FirstBody, SecondBody, FirstBay, SecondBay, Counterfoil, Filer, Indexer;
        public string ZoneID, IndexID, FirstBodyID, SecondBodyID, FirstBayID, SecondBayID, CounterfoilID, FilerID, IndexerID;
        public int IndexX, IndexY, FirstBayX, FirstBayY, SecondBayX, SecondBayY;
        public bool Configured, Certified;

        internal void Configure(Zone zone, Entity first, Entity second, Entity firstBay, Entity secondBay,
            Entity counterfoil, Entity filer, Entity indexer)
        {
            ZoneID=zone.ZoneID; IndexID=ParentEntity.ID; FirstBody=first; SecondBody=second; FirstBay=firstBay; SecondBay=secondBay;
            Counterfoil=counterfoil; Filer=filer; Indexer=indexer;
            FirstBodyID=first.ID; SecondBodyID=second.ID; FirstBayID=firstBay.ID; SecondBayID=secondBay.ID;
            CounterfoilID=counterfoil.ID; FilerID=filer.ID; IndexerID=indexer.ID;
            var p=zone.GetEntityPosition(ParentEntity); IndexX=p.x; IndexY=p.y;
            p=zone.GetEntityPosition(firstBay); FirstBayX=p.x; FirstBayY=p.y;
            p=zone.GetEntityPosition(secondBay); SecondBayX=p.x; SecondBayY=p.y;
            Configured=true;
        }
        internal static bool Ground(Entity e, Zone zone, string blueprint)
        {
            var physical=e?.GetPart<PhysicsPart>(); var cell=zone?.GetEntityCell(e);
            return e!=null && zone!=null && e.SpatialZone==zone && e.BlueprintName==blueprint
                && cell?.ParentZone==zone && cell.Objects.Contains(e) && physical?.ParentEntity==e
                && physical.InInventory==null && physical.Equipped==null && !physical.Takeable
                && !e.HasPart<SpatialFootprintPart>() && e.GetPart<RenderPart>()?.ParentEntity==e && e.GetPart<RenderPart>().Visible;
        }
        bool Bound(Zone zone) => Configured && ZoneID==MarrowstyeCompositionPlan.ZoneID && zone?.ZoneID==ZoneID
            && ParentEntity?.GetPart<CurationIntakePart>()==this && ParentEntity.ID==IndexID && Ground(ParentEntity,zone,"CurationIntakeIndex")
            && zone.GetEntityPosition(ParentEntity)==(IndexX,IndexY) && AreaCompositionScope.Allows(zone);
        // This finite authored scene keeps its saved graph under explicit unload,
        // just like other bounded discoveries. An unconfigured index grants no claim.
        internal static bool Retain(OverworldZoneManager manager, string zoneID) => zoneID==MarrowstyeCompositionPlan.ZoneID
            && manager?.CachedZones.TryGetValue(zoneID,out var zone)==true && zone.GetReadOnlyEntities()
                .Any(e=>e.GetPart<CurationIntakePart>() is CurationIntakePart intake && intake.Bound(zone));
        public bool OwnsBody(Entity body, int number, Zone zone)
        {
            if(!Bound(zone) || (number!=1 && number!=2) || FirstBody==SecondBody || !Ground(body,zone,"SaltCuredBody")) return false;
            var marker=body.GetPart<CurationReceivingBodyPart>(); var handling=body.GetPart<HandlingPart>();
            return body==(number==1?FirstBody:SecondBody) && body.ID==(number==1?FirstBodyID:SecondBodyID)
                && marker?.ParentEntity==body && marker.Index==ParentEntity && marker.CaseNumber==number
                && body.GetPart<PhysicsPart>().Solid && body.GetPart<PhysicsPart>().Weight==90
                && handling?.ParentEntity==body && handling.Weight==90 && !handling.Carryable && !handling.Throwable
                && !body.HasTag("Creature") && !body.HasPart<BrainPart>() && !body.HasPart<DestructiblePart>();
        }
        public bool OwnsBay(Entity bay, int number, Zone zone)
        {
            if(!Bound(zone) || (number!=1 && number!=2) || FirstBay==SecondBay || !Ground(bay,zone,"CurationReceivingBay")) return false;
            var marker=bay.GetPart<CurationReceivingBayPart>();
            return bay==(number==1?FirstBay:SecondBay) && bay.ID==(number==1?FirstBayID:SecondBayID)
                && marker?.ParentEntity==bay && marker.Index==ParentEntity && marker.CaseNumber==number
                && !bay.GetPart<PhysicsPart>().Solid && !bay.HasTag("Solid") && !bay.HasTag("Creature")
                && zone.GetEntityPosition(bay)==(number==1?(FirstBayX,FirstBayY):(SecondBayX,SecondBayY));
        }
        bool Context(Entity actor, Zone zone, bool pending=true)
        {
            if(!Bound(zone) || pending&&Certified || actor==null || !actor.HasTag("Player") || actor.SpatialZone!=zone
                || zone.GetEntityCell(actor)==null || actor.GetStatValue("Hitpoints")<=0 || CombatSystem.IsDeathHandled(actor)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true || actor.GetPart<InventoryPart>()?.ParentEntity!=actor
                || SpatialQuery.Distance(zone,actor,ParentEntity)>1) return false;
            var manager=WorldLocationContext.For(zone);
            if(manager!=null && (!manager.CachedZones.TryGetValue(zone.ZoneID,out var current) || current!=zone)) return false;
            return Staff(Filer,FilerID,"CurationIntakeFiler",actor,zone) && Staff(Indexer,IndexerID,"CurationJuniorIndexer",actor,zone);
        }
        static bool Staff(Entity person, string id, string blueprint, Entity actor, Zone zone)
        {
            var brain=person?.GetPart<BrainPart>();
            return Ground(person,zone,blueprint) && person.ID==id && person.HasTag("Creature") && brain?.ParentEntity==person
                && person.GetStatValue("Hitpoints")>0 && !CombatSystem.IsDeathHandled(person)
                && !FactionManager.IsHostile(person,actor) && !FactionManager.IsHostile(actor,person)
                && !brain.IsPersonallyHostileTo(actor) && actor.GetPart<BrainPart>()?.IsPersonallyHostileTo(person)!=true;
        }
        bool Arrangement(Zone zone) => OwnsBody(FirstBody,1,zone) && OwnsBody(SecondBody,2,zone)
            && OwnsBay(FirstBay,1,zone) && OwnsBay(SecondBay,2,zone)
            && !DragSystem.IsBeingDragged(FirstBody) && !DragSystem.IsBeingDragged(SecondBody)
            && zone.GetEntityPosition(FirstBody)==zone.GetEntityPosition(FirstBay)
            && zone.GetEntityPosition(SecondBody)==zone.GetEntityPosition(SecondBay);
        internal static bool SinglePhysicalItem(Entity item) => item.GetPart<StackerPart>() is StackerPart stack ? stack.StackCount==1 && stack.MaxStack==1 : true;
        bool StoredCounterfoil()
        {
            var inventory=ParentEntity?.GetPart<InventoryPart>(); var physical=Counterfoil?.GetPart<PhysicsPart>();
            return Counterfoil!=null && Counterfoil.ID==CounterfoilID && Counterfoil.BlueprintName=="CurationCounterfoil"
                && Counterfoil.GetPart<KeyPart>()?.ParentEntity==Counterfoil && Counterfoil.GetPart<KeyPart>().KeyId=="marrowstye-intake-tools" && SinglePhysicalItem(Counterfoil)
                && Counterfoil.SpatialZone==null && physical?.ParentEntity==Counterfoil && physical.Takeable && physical.InInventory==ParentEntity && physical.Equipped==null
                && inventory?.ParentEntity==ParentEntity && inventory.Objects.Count==1 && inventory.Objects[0]==Counterfoil;
        }
        public bool TryCertify(Entity actor, Zone zone)
        {
            if(!Context(actor,zone) || !Arrangement(zone) || !StoredCounterfoil()) return Reject(actor,"conditions-not-met");
            return InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity,CertifyCommand),actor,zone).Success;
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor"); var zone=e.GetParameter<Zone>("Zone")??ParentEntity?.SpatialZone;
            if(e.ID=="GetInventoryActions")
            {
                if(Context(actor,zone,false))e.GetParameter<InventoryActionList>("Actions")?.AddAction("CurationCertify","check the receiving index",CertifyCommand,'c',20);
                return true;
            }
            if(e.ID!="InventoryAction" || e.GetStringParameter("Command")!=CertifyCommand) return true;
            var tx=e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if(tx==null || !Context(actor,zone) || !Arrangement(zone) || !StoredCounterfoil()) { Reject(actor,"conditions-not-met"); return true; }
            foreach(var owner in new[]{ParentEntity,FirstBody,SecondBody,FirstBay,SecondBay,Counterfoil})
                if(!tx.TryClaim(owner,actor,CertifyCommand)) { Reject(actor,"owner-busy"); return true; }
            var from=ParentEntity.GetPart<InventoryPart>(); var to=actor.GetPart<InventoryPart>(); var foil=Counterfoil;
            var source=InventoryTransferSnapshot.Capture(from,foil); var destination=InventoryTransferSnapshot.Capture(to,foil);
            tx.Do(null,source.Restore); tx.Do(null,destination.Restore);
            if(!source.Apply(()=>from.RemoveObject(foil)) || !destination.Apply(()=>to.AddRetrievedObject(foil))
                || !Context(actor,zone) || !Arrangement(zone) || Counterfoil!=foil || foil.GetPart<PhysicsPart>()?.InInventory!=actor
                || to.Objects.Count(item=>item==foil)!=1 || from.Objects.Contains(foil))
            { Reject(actor,"transfer-refused"); return true; }
            bool previous=Certified; tx.Do(()=>Certified=true,()=>Certified=previous);
            tx.AfterCommit(()=>
            {
                MessageLog.Add("Positions checked. Counterfoil issued. The tool cabinet accepts its stamp.");
                Diag.Record("furniture","CurationIntakeCertified",actor,ParentEntity,new{zoneId=ZoneID,first=FirstBodyID,second=SecondBodyID,counterfoil=CounterfoilID});
                var cell=ParentEntity?.SpatialZone?.GetEntityCell(ParentEntity); if(cell!=null)ZoneRenderHooks.MarkCellDirty(cell.X,cell.Y,"CurationIntakeCertified");
            });
            e.Handled=true; return false;
        }
        bool Reject(Entity actor,string reason)
        {
            MessageLog.Add(Certified?"This arrangement has already been certified.":"Certification waits. Match both subjects to their quoted last words, release your hold, and keep the receiving staff present.");
            Diag.Record("furniture","CurationIntakeRejected",actor,ParentEntity,new{reason,zoneId=ZoneID}); return false;
        }
    }
    /// <summary>Exact generated subject identity, independent of its movable position.</summary>
    public sealed class CurationReceivingBodyPart : Part
    {
        public override string Name=>"CurationReceivingBody";
        public Entity Index; public int CaseNumber;
        public bool IsCurrent(Zone zone)=>Index?.GetPart<CurationIntakePart>()?.OwnsBody(ParentEntity,CaseNumber,zone)==true;
    }
    /// <summary>An anchored receiving label; it never blocks or moves its subject.</summary>
    public sealed class CurationReceivingBayPart : Part
    {
        public override string Name=>"CurationReceivingBay";
        public Entity Index; public int CaseNumber;
        public bool IsCurrent(Zone zone)=>Index?.GetPart<CurationIntakePart>()?.OwnsBay(ParentEntity,CaseNumber,zone)==true;
    }
}
