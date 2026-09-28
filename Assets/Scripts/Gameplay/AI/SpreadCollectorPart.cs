using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    public enum SpreadCollectorPhase { Seeking, Carrying, Deposited, Stopped }

    /// <summary>One configured salvage trip. Generation must separately prove the
    /// ambient, loose-item and cache receipts; this role never discovers or creates stock.</summary>
    public sealed class SpreadCollectorPart : AIBehaviorPart
    {
        public override string Name => "SpreadCollector";
        public const int MaxApproachActions = 48;
        public bool Configured;
        public Entity Home, Target;
        public string ZoneID, TargetBlueprint, HomeBlueprint;
        public int HomeX, HomeY, TargetX, TargetY, Quantity, Actions;
        public SpreadCollectorPhase Phase;
        private bool _acting;

        /// <summary>Exact currently carried salvage, including retained goods after a
        /// stopped trip. A new/reloaded view must not infer an acquisition animation.</summary>
        public Entity CurrentCarriedItem => CarriedOwner(ParentEntity?.SpatialZone) ? Target : null;

        public bool Configure(Zone zone, Entity home, Entity target)
        {
            if (Configured || !ActorCurrent(zone, out _, allowUnwired: true) || !HomeSource(home, zone)
                || !Eligible(target) || !SpreadActorContext.Ground(target, zone)
                || home == target || !Unowned(target) || Units(target) < 1) return false;
            var h = zone.GetEntityCell(home); var t = zone.GetEntityCell(target); var a = zone.GetEntityCell(ParentEntity);
            if (Distance(a,h)>12 || Distance(a,t)>12 || Distance(h,t)>12) return false;
            Home = home; Target = target; ZoneID = zone.ZoneID; HomeX = h.X; HomeY = h.Y; TargetX = t.X; TargetY = t.Y;
            TargetBlueprint = target.BlueprintName; HomeBlueprint = home.BlueprintName; Quantity = Units(target);
            Actions = 0; Phase = SpreadCollectorPhase.Seeking; Configured = true;
            return true;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID || !Configured) return true;
            var brain = ParentEntity?.GetPart<BrainPart>();
            // Party's normal follow/duty behavior retains priority. Higher goals
            // never dispatch AIBored; an unconfigured ordinary bird remains unchanged.
            if (brain?.PartyLeader != null) return true;
            e.Handled = true;
            if (_acting) return false;
            _acting = true;
            try { Act(brain?.CurrentZone); }
            finally { _acting = false; }
            return false;
        }

        void Act(Zone zone)
        {
            if (!ActorCurrent(zone,out var brain) || ZoneID != zone.ZoneID || Quantity < 1 || Actions < 0
                || Phase < SpreadCollectorPhase.Seeking || Phase > SpreadCollectorPhase.Stopped) return;
            brain.CurrentState = AIState.Idle;
            if (Phase == SpreadCollectorPhase.Deposited || Phase == SpreadCollectorPhase.Stopped) return;
            if (!CurrentHome(zone)) { Stop("home_unavailable"); return; }
            bool pickup = Phase == SpreadCollectorPhase.Seeking;
            if (!(pickup ? GroundTarget(zone) : Carries(zone))) { Stop("target_unavailable"); return; }
            var destination = pickup ? Target : Home;
            if (SpatialQuery.Distance(zone, ParentEntity, destination) > 1)
            {
                if (Actions >= MaxApproachActions) { Stop("approach_budget"); return; }
                Actions++;
                var from=zone.GetEntityCell(ParentEntity);var to=zone.GetEntityCell(destination);
                var path=FindPath.Search(zone,from.X,from.Y,to.X,to.Y,actor:ParentEntity,contactTarget:destination);
                if (path.Usable && path.Steps.Count>0)
                {
                    var step=path.Steps[0]; MovementSystem.TryMoveDetailed(ParentEntity,zone,step.dx,step.dy);
                }
                Record("approach"); return;
            }
            var originalActor=ParentEntity;
            var result=InventorySystem.ExecuteCommand(pickup ? (IInventoryCommand)new CollectCommand(this) : new DepositCommand(this),originalActor,zone);
            if (!result.Success && ParentEntity==originalActor && originalActor.GetPart<SpreadCollectorPart>()==this) Stop(pickup?"pickup_refused":"deposit_refused");
        }

        bool ActorCurrent(Zone zone,out BrainPart brain,bool allowUnwired=false)
        {
            if (!SpreadActorContext.Actor(ParentEntity,zone,out brain) || ParentEntity.GetPart<SpreadCollectorPart>()!=this
                || (brain.CurrentZone!=zone && !(allowUnwired && brain.CurrentZone==null)) || !brain.Passive || brain.PartyLeader!=null || ParentEntity.HasTag("Player")
                || CombatSystem.IsDeathHandled(ParentEntity)) return false;
            var inv=ParentEntity.GetPart<InventoryPart>();
            return inv!=null && inv.ParentEntity==ParentEntity;
        }
        static bool HomeSource(Entity home,Zone zone)
        {
            if (!SpreadActorContext.Ground(home,zone) || home.HasTag("Creature") || home.HasPart<SpatialFootprintPart>()
                || Special(home)) return false;
            var c=home.GetPart<ContainerPart>();
            return c!=null && c.ParentEntity==home && c.Contents!=null && !c.IsLocked;
        }
        bool CurrentHome(Zone zone)
        {
            if (!Configured || ZoneID!=zone?.ZoneID || !HomeSource(Home,zone) || Home.BlueprintName!=HomeBlueprint) return false;
            var c=zone.GetEntityCell(Home);return c.X==HomeX && c.Y==HomeY;
        }
        bool GroundTarget(Zone zone)
        {
            if (!Configured || !Eligible(Target) || Target.BlueprintName!=TargetBlueprint || Units(Target)!=Quantity
                || !Unowned(Target) || !SpreadActorContext.Ground(Target,zone)) return false;
            var c=zone.GetEntityCell(Target);return c.X==TargetX && c.Y==TargetY;
        }
        bool Carries(Zone zone)
        {
            return ZoneID==zone?.ZoneID && ActorCurrent(zone,out _) && Eligible(Target)
                && Target.BlueprintName==TargetBlueprint && Units(Target)==Quantity && CarriedOwner(zone);
        }
        bool CarriedOwner(Zone zone)
        {
            if (!Configured || !SpreadActorContext.Actor(ParentEntity,zone,out _) || ParentEntity.GetPart<SpreadCollectorPart>()!=this
                || CombatSystem.IsDeathHandled(ParentEntity) || Target==null
                || Units(Target)<1 || Target.SpatialZone!=null) return false;
            var p=Target.GetPart<PhysicsPart>();var inv=ParentEntity.GetPart<InventoryPart>();
            var stack=Target.GetPart<StackerPart>();
            return inv!=null && inv.ParentEntity==ParentEntity && p!=null && p.ParentEntity==Target
                && (stack==null||stack.ParentEntity==Target) && p.InInventory==ParentEntity && p.Equipped==null
                && inv.Objects.Contains(Target) && !inv.EquippedItems.ContainsValue(Target)
                && !OnBody(ParentEntity.GetPart<Body>()?.GetBody(),Target);
        }
        static bool Unowned(Entity e){var p=e.GetPart<PhysicsPart>();return p.InInventory==null&&p.Equipped==null;}
        static bool Special(Entity e) => e.HasTag("QuestItem") || e.HasTag("Quest") || e.HasTag("Unique") || e.HasTag("NoTrade")
            || e.HasTag("Owned") || e.HasTag("Essential") || e.HasTag("Currency") || e.HasTag("NoTake")
            || e.Properties.ContainsKey("Owner") || e.Properties.ContainsKey("OwnerID") || e.Properties.ContainsKey("QuestID")
            || e.HasPart<KeyPart>() || e.HasPart<CavesOfOoo.Storylets.QuestStarter>() || e.HasPart<CavesOfOoo.Storylets.CompleteObjectiveOnTaken>();
        static bool Eligible(Entity e)
        {
            if (e==null || (e.BlueprintName!="Hatchet" && e.BlueprintName!="Cudgel" && e.BlueprintName!="LeatherBoots")
                || !e.HasTag("Item") || e.HasTag("Creature") || e.HasPart<SpatialFootprintPart>() || Special(e)) return false;
            var p=e.GetPart<PhysicsPart>();var s=e.GetPart<StackerPart>();var h=e.GetPart<HandlingPart>();
            return p!=null&&p.ParentEntity==e&&p.Takeable&&!p.Solid
                && (s==null||s.ParentEntity==e) && (h==null||(h.ParentEntity==e&&h.Carryable));
        }
        static bool OnBody(CavesOfOoo.Core.Anatomy.BodyPart part,Entity item)
        {
            if(part==null)return false;if(part._Equipped==item)return true;
            if(part.Parts!=null)foreach(var child in part.Parts)if(OnBody(child,item))return true;
            return false;
        }
        static int Units(Entity e)=>e?.GetPart<StackerPart>()?.StackCount??1;
        static int Distance(Cell a,Cell b)=>AIHelpers.ChebyshevDistance(a.X,a.Y,b.X,b.Y);
        void Stop(string reason){Phase=SpreadCollectorPhase.Stopped;Record(reason);}
        void Record(string reason)
        {
            if(Diag.IsChannelEnabled("ai"))Diag.Record("ai","CollectorProgress",ParentEntity,Target,payload:new
            {reason,phase=Phase.ToString(),home=Home?.ID,quantity=Quantity,actions=Actions});
        }

        // These commands are private capabilities, never general pickup authorization.
        // Their exact owner/configuration is captured before user hooks can run.
        abstract class RoleCommand : IInventoryCommand
        {
            protected readonly SpreadCollectorPart Role;protected readonly Entity Actor,Home,Target;
            readonly string zone,blueprint,homeBlueprint;readonly int hx,hy,tx,ty,quantity;
            protected RoleCommand(SpreadCollectorPart r)
            {Role=r;Actor=r.ParentEntity;Home=r.Home;Target=r.Target;zone=r.ZoneID;blueprint=r.TargetBlueprint;homeBlueprint=r.HomeBlueprint;hx=r.HomeX;hy=r.HomeY;tx=r.TargetX;ty=r.TargetY;quantity=r.Quantity;}
            public abstract string Name{get;}
            protected bool Current(InventoryContext c,bool pickup)
            {
                return c.Actor==Actor && Actor.GetPart<InventoryPart>()==c.Inventory && Role.ParentEntity==Actor && Role.Home==Home && Role.Target==Target
                    && Role.ZoneID==zone && Role.TargetBlueprint==blueprint && Role.HomeBlueprint==homeBlueprint && Role.Quantity==quantity
                    && Role.HomeX==hx&&Role.HomeY==hy&&Role.TargetX==tx&&Role.TargetY==ty
                    && Role.ActorCurrent(c.Zone,out _) && Role.CurrentHome(c.Zone)
                    && Role.Phase==(pickup?SpreadCollectorPhase.Seeking:SpreadCollectorPhase.Carrying)
                    && (pickup?Role.GroundTarget(c.Zone):Role.Carries(c.Zone))
                    && SpatialQuery.Distance(c.Zone,Actor,pickup?Target:Home)<=1;
            }
            public InventoryValidationResult Validate(InventoryContext c)=>Current(c,Name=="CollectorPickup")
                ? InventoryValidationResult.Valid():InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule,"Collector authority changed.");
            public abstract InventoryCommandResult Execute(InventoryContext c,InventoryTransaction tx);
            protected static InventoryCommandResult Refuse()=>InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,"Collector source changed or refused.");
        }
        sealed class CollectCommand:RoleCommand
        {
            public CollectCommand(SpreadCollectorPart r):base(r){}public override string Name=>"CollectorPickup";
            public override InventoryCommandResult Execute(InventoryContext c,InventoryTransaction tx)
            {
                if(!Current(c,true)||!tx.TryClaim(Target,Actor,Name)||!HandlingService.CanLift(Actor,Target,out _))return Refuse();
                var before=GameEvent.New("BeforePickup");before.SetParameter("Actor",(object)Actor);before.SetParameter("Item",(object)Target);
                if(!Actor.FireEventAndRelease(before)||!Current(c,true))return Refuse();
                var being=GameEvent.New("BeforeBeingPickedUp");being.SetParameter("Actor",(object)Actor);being.SetParameter("Item",(object)Target);
                if(!Target.FireEventAndRelease(being)||!Current(c,true)||!HandlingService.CanLift(Actor,Target,out _))return Refuse();
                var cell=c.Zone.GetEntityCell(Target);var snapshot=InventoryTransferSnapshot.Capture(c.Inventory,Target);
                if(!c.Zone.RemoveEntity(Target))return Refuse();
                tx.Do(null,()=>c.Zone.AddEntity(Target,cell.X,cell.Y));tx.Do(null,snapshot.Restore);
                if(!snapshot.Apply(()=>c.Inventory.AddRetrievedObject(Target))||!snapshot.ClaimChanges(tx,Actor,Name))return Refuse();
                tx.Do(()=>Role.Phase=SpreadCollectorPhase.Carrying,()=>Role.Phase=SpreadCollectorPhase.Seeking);
                // Completion hooks observe committed ownership and saved phase. Their
                // independent subsequent work cannot cause this transfer to duplicate.
                tx.AfterCommit(()=>
                {
                    Role.Record("picked_up");
                    var taken=GameEvent.New("Taken");taken.SetParameter("Actor",(object)Actor);taken.SetParameter("Item",(object)Target);Target.FireEventAndRelease(taken);
                    var after=GameEvent.New("AfterPickup");after.SetParameter("Actor",(object)Actor);after.SetParameter("Item",(object)Target);Actor.FireEventAndRelease(after);
                });
                return InventoryCommandResult.Ok();
            }
        }
        sealed class DepositCommand:RoleCommand
        {
            public DepositCommand(SpreadCollectorPart r):base(r){}public override string Name=>"CollectorDeposit";
            public override InventoryCommandResult Execute(InventoryContext c,InventoryTransaction tx)
            {
                if(!Current(c,false))return Refuse();
                var command=new PutInContainerCommand(Home,Target);var validation=command.Validate(c);
                if(!validation.IsValid)return Refuse();
                tx.Do(()=>Role.Phase=SpreadCollectorPhase.Deposited,()=>
                {
                    if(Role.ParentEntity==Actor && Actor.GetPart<SpreadCollectorPart>()==Role && Role.Phase==SpreadCollectorPhase.Deposited)
                        Role.Phase=SpreadCollectorPhase.Carrying;
                });
                var result=command.Execute(c,tx);
                if(result.Success)tx.AfterCommit(()=>Role.Record("deposited"));
                return result;
            }
        }
    }
}
