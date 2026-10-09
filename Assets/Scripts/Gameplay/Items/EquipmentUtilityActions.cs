using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Situational verbs for exact worn equipment and a finite dropped mineral light.
    /// Selections are bound to origin, target identity and state. Effects publish only after payment commits.</summary>
    public static class EquipmentUtilityActions
    {
        public const string GlowBlueprint = "CrackedGlowQuartz";
        static readonly string[] Verbs = { "TouchTorch", "FanGas", "GroundCharge", "BraceGrip", "BraceBoots", "CrackQuartz" };
        public static bool IsCommand(string command) => command != null && Verbs.Any(v => command.StartsWith(v + "|", StringComparison.Ordinal));
        internal static bool ActorReady(Entity actor, Zone zone) => WorldResourceActions.ActorCurrent(actor, zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true;
        internal static bool Equipped(Entity actor, Entity item, string slot)
        {
            var pack = actor?.GetPart<InventoryPart>(); var physics = item?.GetPart<PhysicsPart>();
            if (pack?.ParentEntity != actor || item == null || string.IsNullOrEmpty(item.ID) || physics?.ParentEntity != item
                || physics.Equipped != actor || physics.InInventory != null || item.SpatialZone != null || !physics.Takeable
                || pack.Objects.Contains(item) || !pack.EquippedItems.ContainsValue(item) || Quantity(item) != 1
                || CombatSystem.IsDeathHandled(item) || item.GetStatValue("Hitpoints", 1) <= 0) return false;
            return actor.HasPart<Body>() ? pack.FindEquippedBodyPart(item)?.Type == slot : pack.FindEquippedSlot(item) == slot;
        }
        internal static bool Handhold(Entity actor, Entity wall, Zone zone) => WorldResourceActions.Nearby(actor, wall, zone)
            && wall.HasTag("Wall") && wall.GetPart<PhysicsPart>()?.Solid == true && !wall.HasTag("Creature")
            && wall.GetPart<PhysicsPart>()?.Takeable == false && wall.GetStatValue("Hitpoints", 1) > 0
            && !CombatSystem.IsDeathHandled(wall) && !(wall.GetPart<DestructiblePart>() is DestructiblePart structure && (structure.Gone || structure.HP<=0));
        internal static bool StableFeet(Entity actor, Zone zone)
        {
            if (zone == null || actor == null) return false;
            var contacts = zone.GetOccupiedCells(actor);
            if (!contacts.Any()) return false;
            foreach (var cell in contacts)
            {
                if (cell == null || !cell.Occupants.Any(e => e.HasTag("Terrain"))
                    || cell.Occupants.Any(e => e.GetPart<LiquidPoolPart>()?.Volume > 0)) return false;
                var state = zone.TileState.Get(cell.X, cell.Y);
                if (state != null && state.Coatings.Any(c => c.Turns > 0 && LiquidRegistry.Get(c.Id)?.Slippery == true)) return false;
            }
            return true;
        }
        static bool TorchReady(Entity actor, Entity item)
        {
            var light = item?.GetPart<LightSourcePart>(); var heat = item?.GetPart<ThermalPart>(); var fuel = item?.GetPart<FuelPart>();
            return Equipped(actor, item, "Hand") && light?.ParentEntity == item && light.Enabled && heat?.ParentEntity == item
                && WorldResourceActions.Finite(heat.Temperature) && WorldResourceActions.Finite(heat.FlameTemperature)
                && heat.Temperature >= heat.FlameTemperature && fuel?.ParentEntity == item && WorldResourceActions.Finite(fuel.FuelMass)
                && fuel.FuelMass >= 1 && !(item.GetEffect<FrozenEffect>()?.Cold > 0)
                && !(item.GetEffect<WetEffect>() is WetEffect wet && (!WorldResourceActions.Finite(wet.Moisture) || wet.Moisture > .35f));
        }
        static bool Charged(Entity actor) => actor?.GetEffect<ElectrifiedEffect>() is ElectrifiedEffect charge
            && charge.Duration > 0 && WorldResourceActions.Finite(charge.Charge) && charge.Charge > 0;
        static string Verb(Entity actor, Entity item, Zone zone)
        {
            if (!ActorReady(actor, zone) || item == null) return null;
            switch (item.BlueprintName)
            {
                case "Torch": return TorchReady(actor,item) ? "TouchTorch" : null;
                case "LampveinFan": return Equipped(actor,item,"Hand") ? "FanGas" : null;
                case "GroundwireScreen": return Equipped(actor,item,"Hand") && Charged(actor) ? "GroundCharge" : null;
                case "GripfrondWrap": return Equipped(actor,item,"Handwear") && !HasBrace(actor) ? "BraceGrip" : null;
                case "IronshodBoots": return Equipped(actor,item,"Feet") && !HasBrace(actor) && StableFeet(actor,zone) ? "BraceBoots" : null;
                case "GlowQuartz": return WorldResourceActions.Carried(actor,item,false)
                    && HarvestablePart.Factory?.Blueprints.ContainsKey(GlowBlueprint) == true ? "CrackQuartz" : null;
                default: return null;
            }
        }
        static bool HasBrace(Entity actor) => actor.GetEffect<EquipmentBraceEffect>()?.Duration > 0;
        public static void AddActions(Entity actor, Entity item, Zone zone, InventoryActionList actions)
        {
            string verb = Verb(actor,item,zone); if (actions == null || verb == null) return;
            var origin = zone.GetEntityCell(actor);
            if (verb == "BraceBoots") { Add(actions,actor,item,zone,verb,origin.X,origin.Y,null); return; }
            if (verb == "BraceGrip")
            {
                foreach (var target in zone.GetReadOnlyEntities().Where(e => Handhold(actor,e,zone) && VisibleTarget(actor,e,zone)))
                { var p = zone.GetEntityCell(target); Add(actions,actor,item,zone,verb,p.X,p.Y,target); }
                return;
            }
            foreach (var cell in zone.GetReadOnlyEntities().Where(e => VisibleTarget(actor,e,zone)))
            {
                if (verb == "TouchTorch" && MaterialFieldActions.CanKindle(cell) || verb == "FanGas" && GasReady(cell,zone))
                { var p = zone.GetEntityCell(cell); Add(actions,actor,item,zone,verb,p.X,p.Y,cell); }
            }
            if (verb == "FanGas") return;
            var choices = zone.GetOccupiedCells(actor).SelectMany(c => Enumerable.Range(-1,3).SelectMany(dx => Enumerable.Range(-1,3)
                .Select(dy => (x:c.X+dx,y:c.Y+dy)))).Distinct();
            foreach (var p in choices)
                if (TileReady(actor,zone,verb,p.x,p.y)) Add(actions,actor,item,zone,verb,p.x,p.y,null);
        }
        static void Add(InventoryActionList actions,Entity actor,Entity item,Zone zone,string verb,int x,int y,Entity target)
        {
            var origin = zone.GetEntityCell(actor);
            string command = verb + "|" + Escape(zone.ZoneID) + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + N(x) + "|" + N(y)
                + "|" + Escape(target?.ID) + "|" + Fingerprint(actor,item,target,verb);
            string label = verb == "TouchTorch" ? "touch lit torch" : verb == "FanGas" ? "fan gas (up to 5 density)"
                : verb == "GroundCharge" ? "ground held charge" : verb == "BraceGrip" ? "brace against wall (one shove, 3 turns)"
                : verb == "BraceBoots" ? "plant feet (one shove, 3 turns)" : "crack quartz light (1 mineral, 30 turns)";
            actions.AddAction(verb,label + (target != null ? " — " + target.GetDisplayName() : "") + " [" + x + "," + y + "]",command,'\0',18);
        }
        internal static bool TryAct(Entity actor,Entity item,Zone zone,string command,InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            var f = command.Split('|'); string verb = Verb(actor,item,zone);
            if (tx == null || f.Length != 8 || verb != f[0] || !Parse(f[2],out int ox) || !Parse(f[3],out int oy)
                || !Parse(f[4],out int x) || !Parse(f[5],out int y) || WorldResourceActions.Decode(f[1]) != zone.ZoneID)
                return Reject(actor,item,command,"invalid-selection");
            var origin = zone.GetEntityCell(actor); var target = f[6].Length == 0 ? null : WorldResourceActions.ExactGround(zone,f[6]);
            if (origin.X != ox || origin.Y != oy || f[6].Length != 0 && target == null
                || Fingerprint(actor,item,target,verb) != f[7] || !TargetReady(actor,zone,verb,x,y,target))
                return Reject(actor,item,command,"stale-target");
            if (!tx.TryClaim(actor,actor,command) || !tx.TryClaim(item,actor,command)
                || target != null && !tx.TryClaim(target,actor,command)) return Reject(actor,item,command,"in-progress");
            var parts = item.Parts.ToArray(); var targetParts = target?.Parts.ToArray(); var pack = actor.GetPart<InventoryPart>();
            Func<bool> current = () => ActorReady(actor,zone) && zone.GetEntityCell(actor) == origin && actor.GetPart<InventoryPart>() == pack
                && item.Parts.SequenceEqual(parts) && (target == null || target.Parts.SequenceEqual(targetParts))
                && TargetReady(actor,zone,verb,x,y,target);
            if (verb == "CrackQuartz") return Deploy(actor,item,zone,command,tx,current,origin,parts,x,y);
            if (verb == "TouchTorch")
            {
                var fuel = item.GetPart<FuelPart>(); float before = fuel.FuelMass;
                // Only payment is staged. Heat and its irreversible consequences publish after the outer action accepts.
                tx.Do(() => fuel.FuelMass -= 1, () => fuel.FuelMass += 1);
                tx.BeforeCommit(() => current() && Equipped(actor,item,"Hand") && fuel.FuelMass == before-1
                    && item.GetPart<LightSourcePart>()?.Enabled == true && item.GetPart<ThermalPart>().Temperature >= item.GetPart<ThermalPart>().FlameTemperature
                    && !(item.GetEffect<WetEffect>()?.Moisture > .35f) && !(item.GetEffect<FrozenEffect>()?.Cold > 0));
                tx.AfterCommit(() => { if(target!=null) MaterialFieldActions.Kindle(actor,target,zone); else MaterialFieldActions.KindleTile(actor,zone,x,y); });
            }
            else
            {
                var charge = actor.GetEffect<ElectrifiedEffect>(); var gas = target?.GetPart<GasPoolPart>();
                string fingerprint = f[7];
                tx.BeforeCommit(() => current() && (verb=="BraceGrip"||verb=="BraceBoots" ? Equipped(actor,item,verb=="BraceGrip"?"Handwear":"Feet") : Verb(actor,item,zone) == verb) && Fingerprint(actor,item,target,verb) == fingerprint
                    && (verb != "GroundCharge" || actor.GetEffect<ElectrifiedEffect>() == charge));
                if (verb == "FanGas") tx.AfterCommit(() =>
                { gas.Density = Math.Max(0,gas.Density-5); if(gas.Density==0 && WorldResourceActions.Ground(target,zone)) zone.RemoveEntity(target); ZoneRenderHooks.MarkCellDirty(zone.GetCell(x,y),"FanGas"); });
                else if (verb == "GroundCharge") tx.AfterCommit(() =>
                {
                    int amount = (int)Math.Min(2,Math.Ceiling(charge.Charge));
                    actor.GetPart<StatusEffectsPart>().RemoveEffect(charge);
                    ZoneTileStateSystem.AddCharge(zone,x,y,amount,actor,"GroundwireScreen"); ZoneTileStateSystem.ResolveAfterAbility(zone,actor);
                });
                else
                {
                    var brace = new EquipmentBraceEffect { Equipment=item,Handhold=target,ZoneID=zone.ZoneID,OriginX=ox,OriginY=oy,Armed=false };
                    var status=actor.GetPart<StatusEffectsPart>(); bool addedStatus=status==null;
                    if(addedStatus){ status=new StatusEffectsPart(); actor.AddPart(status); }
                    var manager=status;
                    if(addedStatus)tx.Do(null,()=> { if(actor.GetPart<StatusEffectsPart>()==manager&&manager.EffectCount==0)actor.RemovePart(manager); });
                    tx.Do(null,()=>manager.RemoveAddedEffectForInventoryUndo(brace));
                    if(!actor.ApplyEffectWithReceipt(brace,actor,zone,null,null)||actor.GetEffect<EquipmentBraceEffect>()!=brace)
                        return Reject(actor,item,command,"brace-refused");
                    tx.BeforeCommit(()=>current()&&actor.GetPart<StatusEffectsPart>()==manager&&actor.GetEffect<EquipmentBraceEffect>()==brace
                        &&brace.Duration==3&&!brace.Armed&&brace.Owner==actor&&brace.Equipment==item&&brace.Handhold==target
                        &&brace.ZoneID==zone.ZoneID&&brace.OriginX==ox&&brace.OriginY==oy
                        &&Equipped(actor,item,verb=="BraceGrip"?"Handwear":"Feet"));
                    tx.AfterCommit(()=>brace.Armed=true);
                }
            }
            tx.AfterCommit(() => { Diag.Record("event","EquipmentUtilityApplied",actor,item,new { verb,x,y }); MessageLog.Add(Feedback(verb)); });
            return true;
        }
        static bool Deploy(Entity actor,Entity item,Zone zone,string command,InventoryTransaction tx,Func<bool> current,Cell origin,Part[] originalParts,int x,int y)
        {
            var factory = HarvestablePart.Factory; var beacon = factory.CreateEntity(GlowBlueprint);
            var light = beacon?.GetPart<LightSourcePart>(); var physics = beacon?.GetPart<PhysicsPart>();
            if (beacon == null || beacon.SpatialZone != null || light?.ParentEntity != beacon || !light.Enabled || light.Radius!=4
                || light.Intensity!=.6f || beacon.GetPart<LifespanPart>()?.TurnsRemaining!=30 || physics?.ParentEntity!=beacon
                || physics.Takeable || physics.Solid || physics.InInventory!=null || physics.Equipped!=null
                || zone.GetReadOnlyEntities().Any(e=>e.ID==beacon.ID)) return Reject(actor,item,command,"invalid-light-definition");
            var pack=actor.GetPart<InventoryPart>(); int count=Quantity(item);
            var receipt=InventoryTransferSnapshot.Capture(pack,item); tx.Do(null,receipt.Restore);
            if (!receipt.Apply(()=>pack.TryConsumeOne(item)) || !receipt.ClaimChanges(tx,actor,command) || !current()
                || item.BlueprintName!="GlowQuartz" || !item.Parts.SequenceEqual(originalParts)) return false;
            var beaconParts=beacon.Parts.ToArray(); var lifetime=beacon.GetPart<LifespanPart>();
            light.Enabled=false;
            tx.Do(null,()=> { if(beacon.SpatialZone==zone) zone.RemoveEntity(beacon); });
            if(!zone.AddEntity(beacon,x,y)) return false;
            // TargetReady excludes the newly staged light, so independently validate its exact ground and the original paid stack.
            var itemPhysics=item.GetPart<PhysicsPart>();
            tx.BeforeCommit(()=>ActorReady(actor,zone)&&zone.GetEntityCell(actor)==origin&&HarvestablePart.Factory==factory
                && actor.GetPart<InventoryPart>()==pack && item.BlueprintName=="GlowQuartz" && item.Parts.SequenceEqual(originalParts) && (count>1 ? WorldResourceActions.Carried(actor,item,false)&&Quantity(item)==count-1
                    : !pack.Objects.Contains(item)&&item.SpatialZone==null&&itemPhysics.InInventory==null&&itemPhysics.Equipped==null)
                && WorldResourceActions.Ground(beacon,zone)&&zone.GetEntityCell(beacon)==zone.GetCell(x,y)&&beacon.GetPart<LightSourcePart>()==light
                && beacon.BlueprintName==GlowBlueprint&&beacon.Parts.SequenceEqual(beaconParts)&&beacon.Parts.All(p=>p.ParentEntity==beacon)
                && !physics.Takeable&&!physics.Solid&&!light.Enabled&&light.Radius==4&&light.Intensity==.6f&&light.LightColor=="&C"
                &&beacon.GetPart<LifespanPart>()==lifetime&&lifetime.TurnsRemaining==30
                && zone.GetCell(x,y).IsVisible&&zone.GetCell(x,y).IsPassable()&&zone.GetCell(x,y).Occupants.All(e=>e==beacon||e.HasTag("Terrain")&&!e.HasTag("Creature")));
            tx.AfterCommit(()=> { light.Enabled=true; EquipmentChangeBus.NotifyChanged(actor); ZoneRenderHooks.MarkCellDirty(zone.GetCell(x,y),"CrackedGlowQuartz");
                Diag.Record("event","QuartzLightPlaced",actor,beacon,new{x,y,turns=30}); MessageLog.Add("The cracked quartz lights the ground for 30 turns. Its fragments cannot be recovered."); });
            return true;
        }
        static bool TargetReady(Entity actor,Zone zone,string verb,int x,int y,Entity target)
        {
            if(verb=="BraceBoots") return target==null && StableFeet(actor,zone) && zone.GetEntityCell(actor)?.X==x && zone.GetEntityCell(actor)?.Y==y;
            if(target==null) return TileReady(actor,zone,verb,x,y);
            if(!VisibleTarget(actor,target,zone)||zone.GetEntityCell(target)?.X!=x||zone.GetEntityCell(target)?.Y!=y)return false;
            return verb=="TouchTorch"?MaterialFieldActions.CanKindle(target):verb=="FanGas"?GasReady(target,zone):verb=="BraceGrip"&&Handhold(actor,target,zone);
        }
        static bool TileReady(Entity actor,Zone zone,string verb,int x,int y)
        {
            var cell=zone.GetCell(x,y);
            if(cell==null||!cell.IsVisible||!cell.IsPassable()||zone.GetOccupiedCells(actor).Contains(cell)
                ||!zone.GetOccupiedCells(actor).Any(c=>Math.Abs((long)c.X-x)<=1&&Math.Abs((long)c.Y-y)<=1))return false;
            if(verb=="TouchTorch")return MaterialFieldActions.CanKindleTile(zone,x,y);
            if(verb=="GroundCharge")return (zone.TileState.Get(x,y)?.Charge??0)<2;
            return verb=="CrackQuartz"&&cell.Occupants.All(e=>e.HasTag("Terrain")&&!e.HasTag("Creature"));
        }
        static bool VisibleTarget(Entity actor,Entity target,Zone zone) => WorldResourceActions.Nearby(actor,target,zone)
            && target.GetPart<RenderPart>() is RenderPart render && render.ParentEntity==target && render.Visible
            && zone.GetOccupiedCells(target).Any(c=>c!=null&&c.IsVisible&&SpatialQuery.DistanceToCell(zone,actor,c.X,c.Y)<=1);
        static bool GasReady(Entity target,Zone zone)=>WorldResourceActions.Ground(target,zone)&&target.GetPart<GasPoolPart>() is GasPoolPart gas
            &&gas.ParentEntity==target&&!gas.Stable&&gas.Density>0&&!target.HasTag("Creature");
        static string Fingerprint(Entity actor,Entity item,Entity target,string verb)=>verb=="TouchTorch"?N(item.GetPart<FuelPart>().FuelMass)
            :verb=="FanGas"?N(target?.GetPart<GasPoolPart>()?.Density??-1):verb=="GroundCharge"?N(actor.GetEffect<ElectrifiedEffect>()?.Charge??0)+":"+N(actor.GetEffect<ElectrifiedEffect>()?.Duration??0)
            :verb=="CrackQuartz"?N(Quantity(item)):"ready";
        static string Feedback(string verb)=>verb=="TouchTorch"?"You spend torch fuel to kindle the existing material. Fire can spread."
            :verb=="FanGas"?"You fan away part of the cloud. Remaining gas and other haze are still dangerous."
            :verb=="GroundCharge"?"You ground the charge through the screen. Connected wet or metal ground can carry it onward."
            :"You brace for one physical shove or pull. Moving releases the stance.";
        static int Quantity(Entity item)=>item?.GetPart<StackerPart>()?.StackCount??1;
        static string Escape(string value)=>Uri.EscapeDataString(value??"");
        static string N(int value)=>value.ToString(CultureInfo.InvariantCulture);
        static string N(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static bool Parse(string value,out int result)=>int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out result);
        static bool Reject(Entity actor,Entity item,string command,string reason){Diag.Record("event","EquipmentUtilityRejected",actor,item,new{command,reason});return false;}
    }
}
