using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One authored structural fault. Repair spends carried materials and
    /// saves a restored-function flag in the same command receipt. It does not heal
    /// combat HP, resurrect destroyed structures, or change settlement quest stages.</summary>
    public sealed class RepairablePart : Part, IDescribeState, IInventoryActionReceiptOwner
    {
        // ── reader contract (Docs/MACHINE-READERS.md) ──
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 60;
        public string DescribeState(DescribeContext context) => Describe();

        public override string Name => "Repairable";
        public const string RepairCommand="RepairObject";
        public string RecipeId="";
        public bool Repaired;
        /// <summary>Explicit common-gear opt-in. Portable repair removes Broken;
        /// structural repair keeps its one-time restored-function contract.</summary>
        public bool PortableEquipment;
        /// <summary>Committed physical repair cause for local acknowledgment.
        /// Existing repaired saves have no retroactively invented author.</summary>
        public Entity RepairedBy;
        public string RepairCauseID, RepairedZoneID, RepairWorldKey;
        private bool UsesMaterialGrade(RepairRecipe recipe) => UsesMaterialGrade(recipe, PortableEquipment);
        private static bool UsesMaterialGrade(RepairRecipe recipe, bool portableEquipment) => !portableEquipment
            && !string.IsNullOrEmpty(recipe?.MaterialGrade);

        public bool OwnsInventoryReceipt(string command) => command == RepairCommand
            && ParentEntity?.GetPart<RepairablePart>() == this
            && UsesMaterialGrade(RepairRecipeRegistry.Get(RecipeId));

        internal string Bill(RepairRecipe recipe) => FormatBill(recipe, PortableEquipment);

        internal static string FormatBill(RepairRecipe recipe, bool portableEquipment = false)
        {
            string quantity = recipe.Quantity.ToString(CultureInfo.InvariantCulture);
            return UsesMaterialGrade(recipe, portableEquipment)
                ? quantity + " measures of fire clay or prepared lining clay, in any mix"
                : quantity + " " + recipe.MaterialName;
        }
        /// <summary>Shared functional gate. Unknown or malformed faults remain
        /// broken; owners without this opt-in Part retain their ordinary behavior.</summary>
        public static bool BlocksFunction(Entity owner)
        {
            if(owner==null)return false;
            int count=0;bool blocked=false;
            foreach(var part in owner.Parts)
                if(part is RepairablePart fault && !fault.PortableEquipment) { count++;blocked|=fault.ParentEntity!=owner || !fault.Repaired; }
            return count>1 || blocked;
        }
        public string Describe()
        {
            var recipe=RepairRecipeRegistry.Get(RecipeId);
            if(recipe==null)return "The damage cannot be repaired with a known method.";
            if (PortableEquipment && ParentEntity?.HasEffect<BrokenEffect>() != true) return "This equipment is sound.";
            return !PortableEquipment && Repaired ? recipe.RepairedText : recipe.Diagnosis+"\nRepair: "+Bill(recipe)+". Composition: "+recipe.Composition+".";
        }
        /// <summary>Use the same native command receipt as the player menu. This
        /// method charges no turn; successful gameplay callers charge one action.</summary>
        public bool TryRepair(Entity actor, Zone zone)
        {
            // A retained reference must never dispatch a replacement owner's fault.
            string reason=Context(actor,zone,RepairRecipeRegistry.Get(RecipeId));
            if(reason!=null)return Reject(actor,reason);
            return InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity,RepairCommand),actor,zone).Success;
        }
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="GetInventoryActions")
            {
                if(PortableEquipment ? ParentEntity.HasEffect<BrokenEffect>() : !Repaired)
                {
                    var recipe=RepairRecipeRegistry.Get(RecipeId);
                    var actions = e.GetParameter<InventoryActionList>("Actions");
                    if (recipe != null && actions != null)
                    {
                        string bill = Bill(recipe);
                        if (UsesMaterialGrade(recipe))
                        {
                            string amount = recipe.Quantity.ToString(CultureInfo.InvariantCulture);
                            actions.AddActionWithHint("Repair", recipe.ActionText + " (" + amount + " measures)",
                                RepairCommand, "Uses " + bill + ".", 'r', 20);
                        }
                        else actions.AddAction("Repair", recipe.ActionText + " (" + bill + ")", RepairCommand, 'r', 20);
                    }
                }
                return true;
            }
            if(e.ID!="InventoryAction" || e.GetStringParameter("Command")!=RepairCommand)return true;
            var actor=e.GetParameter<Entity>("Actor"); var zone=e.GetParameter<Zone>("Zone");
            var tx=e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if(tx==null) { Reject(actor,"missing-transaction"); return true; }
            if(!Apply(actor,zone,tx))return true;
            e.Handled=true;return false;
        }
        private string Context(Entity actor,Zone zone,RepairRecipe recipe,bool acceptingCommittedFlag=false)
        {
            var owner=ParentEntity;
            if(actor==null || zone==null || owner==null || owner.GetPart<RepairablePart>()!=this)return "missing-context";
            int faultCount=0,compositionCount=0;
            foreach(var part in owner.Parts) { if(part is RepairablePart)faultCount++;if(part is CompositionPart)compositionCount++; }
            if(faultCount!=1 || compositionCount!=1)return "ambiguous-structure";
            if(!PortableEquipment && Repaired && !acceptingCommittedFlag)return "already-repaired";
            if(recipe==null || RepairRecipeRegistry.Get(RecipeId)!=recipe)return "unknown-recipe";
            if(!actor.HasTag("Player") || actor.GetStatValue("Hitpoints",0)<=0 || CombatSystem.IsDeathHandled(actor)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true)return "actor-unavailable";
            var inventory=actor.GetPart<InventoryPart>();
            if(inventory?.ParentEntity!=actor)return "missing-inventory";
            var a=zone.GetEntityCell(actor); var t=zone.GetEntityCell(owner); var physics=owner.GetPart<PhysicsPart>();
            if(actor.SpatialZone!=zone || a?.ParentZone!=zone || !a.Objects.Contains(actor))return "not-local";
            var manager=WorldLocationContext.For(zone);
            if(manager!=null && (!manager.CachedZones.TryGetValue(zone.ZoneID,out var current) || current!=zone))return "stale-zone";
            if (PortableEquipment)
            {
                if (!Carried(owner,actor,inventory,owner.BlueprintName)
                    || (owner.GetPart<StackerPart>()?.StackCount ?? 1) != 1
                    || owner.GetPart<EquippablePart>() == null
                    || (owner.GetPart<MeleeWeaponPart>() == null && owner.GetPart<ArmorPart>() == null)) return "invalid-equipment";
                if (!owner.HasEffect<BrokenEffect>()) return "already-repaired";
            }
            else
            {
                if(owner.SpatialZone!=zone || t?.ParentZone!=zone || !t.Objects.Contains(owner))return "not-local";
                if(SpatialQuery.Distance(zone,actor,owner)>1)return "out-of-reach";
                if(owner.HasTag("Creature") || physics?.ParentEntity!=owner || physics.Takeable || physics.InInventory!=null || physics.Equipped!=null
                    || owner.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP<=0))return "invalid-target";
            }
            var composition=owner.GetPart<CompositionPart>();
            if(composition?.ParentEntity!=owner || !composition.Contains(recipe.Composition))return "wrong-composition";
            return null;
        }
        private static bool Carried(Entity item,Entity actor,InventoryPart inventory,string blueprint)
        {
            var physical=item?.GetPart<PhysicsPart>(); var stack=item?.GetPart<StackerPart>();
            if(item==null || item.BlueprintName!=blueprint || item.HasTag("Creature") || item.SpatialZone!=null
                || physical?.ParentEntity!=item || !physical.Takeable || physical.InInventory!=actor || physical.Equipped!=null
                || (stack!=null && (stack.ParentEntity!=item || stack.StackCount<1)) || !inventory.Objects.Contains(item))return false;
            // Adversarial hypothesis: corrupted legacy/body equipment aliases must
            // not become payment merely because their Physics backlink says carried.
            foreach(var equipped in inventory.EquippedItems.Values)if(equipped==item)return false;
            if(inventory.FindEquippedBodyPart(item)!=null)return false;
            int references=0; foreach(var candidate in inventory.Objects)if(candidate==item)references++;
            return references==1;
        }

        private readonly struct Payment
        {
            public readonly Entity Item;
            public readonly string ID, Blueprint;
            public readonly PhysicsPart Physics;
            public readonly StackerPart Stack;
            public readonly int Count, Units;
            public readonly MaterialProcessItemState Alternate;
            public readonly ConnectedClayProvenance.Snapshot Provenance;

            public Payment(Entity item, int units, MaterialProcessItemState alternate)
            {
                Item = item;
                ID = item.ID;
                Blueprint = item.BlueprintName;
                Physics = item.GetPart<PhysicsPart>();
                Stack = item.GetPart<StackerPart>();
                Count = Stack?.StackCount ?? 1;
                Units = units;
                Alternate = alternate;
                Provenance = new ConnectedClayProvenance.Snapshot(item);
            }

            public bool Current => Item.ID == ID && Item.BlueprintName == Blueprint
                && Item.GetPart<PhysicsPart>() == Physics && Item.GetPart<StackerPart>() == Stack
                && (Alternate == null || Alternate.Matches());
        }

        private static IEnumerable<Payment> SelectPayment(Entity actor, Zone zone, InventoryPart inventory,
            RepairRecipe recipe, bool compatible)
        {
            int remaining = recipe.Quantity;
            var factory = WorldLocationContext.For(zone)?.Factory ?? HarvestablePart.Factory;
            // Prefer the legacy bill independently of pack ordering. A shortage
            // can be filled with measures of the explicitly accepted grade.
            foreach (bool alternate in compatible ? new[] { false, true } : new[] { false })
            foreach (var item in inventory.Objects)
            {
                if (item == null || (item.BlueprintName != recipe.MaterialBlueprint) != alternate
                    || !Carried(item, actor, inventory, item.BlueprintName)) continue;
                if (compatible && (string.IsNullOrEmpty(item.ID) ||
                    inventory.Objects.Count(other => other?.ID == item.ID) != 1)) continue;
                if (!RepairMaterialPart.ReadPayment(factory, item, recipe, out var state)) continue;
                int units = Math.Min(remaining, item.GetPart<StackerPart>()?.StackCount ?? 1);
                yield return new Payment(item, units, state);
                remaining -= units;
                if (remaining == 0) yield break;
            }
        }

        private static bool PaymentCommitted(Entity actor, InventoryPart inventory, Payment bill)
        {
            if (!bill.Current || inventory.Objects.Any(other => other != bill.Item && other?.ID == bill.ID))
                return false;
            int remainder = bill.Count - bill.Units;
            if (remainder > 0)
                return Carried(bill.Item, actor, inventory, bill.Blueprint) && bill.Stack?.StackCount == remainder;
            return bill.Item.SpatialZone == null && bill.Physics.InInventory == null && bill.Physics.Equipped == null
                && !inventory.Objects.Contains(bill.Item);
        }

        private static bool TryRestoreDeathDrop(Zone zone, Cell origin, Payment payment,
            ConnectedClayProvenance.Snapshot paidProvenance)
        {
            var item = payment.Item;
            if (origin == null || !payment.Current || paidProvenance.Changed(item)
                || payment.Physics.InInventory != null || payment.Physics.Equipped != null
                || zone.GetReadOnlyEntities().Any(other => other != item && other.ID == payment.ID))
                return false;

            int remainder = payment.Count - payment.Units;
            if (remainder > 0)
            {
                if (item.SpatialZone != zone || zone.GetEntityCell(item) != origin
                    || payment.Stack?.StackCount != remainder) return false;
            }
            else
            {
                if (item.SpatialZone != null || (payment.Stack?.StackCount ?? 1) != 1
                    || !zone.AddEntity(item, origin.X, origin.Y)) return false;
            }

            if (payment.Stack != null) payment.Stack.StackCount = payment.Count;
            payment.Provenance.Restore(item);
            return true;
        }

        private bool Apply(Entity actor,Zone zone,InventoryTransaction tx)
        {
            var recipe=RepairRecipeRegistry.Get(RecipeId); string reason=Context(actor,zone,recipe);
            if(reason!=null)return Reject(actor,reason);
            if(FieldWorkRigRules.IsRig(ParentEntity) && !FieldWorkRigRules.BeginChange(actor,ParentEntity,zone,tx,false))return Reject(actor,"rig-unavailable");
            var workAudio=WorldAudioWork.Repair(actor,this,zone,recipe);
            if(!tx.TryClaim(ParentEntity,actor,RepairCommand) || !tx.TryClaim(actor,actor,RepairCommand))return Reject(actor,"owner-busy");
            var inventory = actor.GetPart<InventoryPart>();
            bool compatible = UsesMaterialGrade(recipe);
            var bill = SelectPayment(actor, zone, inventory, recipe, compatible).ToArray();
            if (bill.Sum(p => p.Units) != recipe.Quantity) return Reject(actor, "missing-materials");
            foreach (var payment in bill)
                if (!tx.TryClaim(payment.Item, actor, RepairCommand)
                    || compatible && !MaterialProcessService.ReserveInput(payment.Item, tx))
                    return Reject(actor, "material-busy");
            var receipt = InventoryTransferSnapshot.Capture(inventory);
            var paymentCell = zone.GetEntityCell(actor);
            ConnectedClayProvenance.Snapshot[] paidProvenance = null;
            tx.Do(null, () =>
            {
                if (!compatible) { receipt.Restore(); return; }
                var excluded = new HashSet<Entity>();
                int limited = 0;
                bool died = CombatSystem.IsDeathHandled(actor);
                bool spills = died && !actor.HasTag("NoDropOnDeath") && !actor.HasTag("Temporary");
                for (int n = 0; n < bill.Length; n++)
                {
                    var p = bill[n];
                    bool identityChanged = p.Item.ID != p.ID || p.Item.GetPart<PhysicsPart>() != p.Physics
                        || p.Item.GetPart<StackerPart>() != p.Stack
                        || inventory.Objects.Any(other => other != p.Item && other?.ID == p.ID);
                    if (spills && paidProvenance != null && !identityChanged
                        && !inventory.Objects.Contains(p.Item)
                        && TryRestoreDeathDrop(zone, paymentCell, p, paidProvenance[n]))
                    {
                        excluded.Add(p.Item);
                        continue;
                    }
                    if (died || identityChanged || p.Item.SpatialZone != null || p.Physics.Equipped != null
                        || p.Physics.InInventory != null && p.Physics.InInventory != actor)
                    {
                        excluded.Add(p.Item);
                        limited++;
                    }
                }
                receipt.RestoreExcept(excluded);
                if (limited != 0)
                    Diag.Record("furniture", "RepairRefundLimited", actor, ParentEntity, new { count = limited });
            });
            bool applied=receipt.Apply(()=>
            {
                foreach (var payment in bill)
                    for (int n = 0; n < payment.Units; n++)
                        if (!payment.Current || !Carried(payment.Item, actor, inventory, payment.Blueprint)
                            || !(compatible ? inventory.TryConsumeMaterialProcessUnit(payment.Item, tx)
                                            : inventory.TryConsumeOne(payment.Item))) return false;
                return true;
            });
            if (compatible && applied)
                paidProvenance = bill.Select(p => new ConnectedClayProvenance.Snapshot(p.Item)).ToArray();
            // Claims protect native command re-entry; recheck the saved owner and
            // selected recipe after payment before granting the structural benefit.
            if(!applied || !receipt.ClaimChanges(tx,actor,RepairCommand) || Context(actor,zone,recipe)!=null
                || actor.GetPart<InventoryPart>()!=inventory)return Reject(actor,"state-changed");
            if (PortableEquipment) RemoveBrokenWithUndo(tx);
            else tx.Do(()=>Repaired=true,()=>Repaired=false);
            var priorAuthor=RepairedBy;string priorCause=RepairCauseID,priorZone=RepairedZoneID,priorWorld=RepairWorldKey;
            tx.Do(()=>
            {
                RepairedBy=actor;RepairCauseID=Guid.NewGuid().ToString("N");RepairedZoneID=zone.ZoneID;
                RepairWorldKey=WorldLocationContext.For(zone)?.Exploration?.WorldKey;
            },()=>{RepairedBy=priorAuthor;RepairCauseID=priorCause;RepairedZoneID=priorZone;RepairWorldKey=priorWorld;});
            if (!PortableEquipment) ConnectedSpreadProgress.RecordRepair(actor,ParentEntity,zone,tx);
            if (compatible)
            {
                string cause = RepairCauseID;
                var actorCell = zone.GetEntityCell(actor);
                var targetCell = zone.GetEntityCell(ParentEntity);
                tx.BeforeCommit(() => Repaired && RepairedBy == actor && RepairCauseID == cause
                    && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(ParentEntity) == targetCell
                    && actor.GetPart<InventoryPart>() == inventory && Context(actor, zone, recipe, true) == null
                    && bill.Select((p, i) => PaymentCommitted(actor, inventory, p)
                        && !paidProvenance[i].Changed(p.Item)).All(current => current));
            }
            tx.AfterCommit(()=>
            {
                MessageLog.Add(recipe.RepairedText);
                Diag.Record("furniture","ObjectRepaired",actor,ParentEntity,new{recipeId=recipe.Id,material=recipe.MaterialBlueprint,quantity=recipe.Quantity,zoneId=zone.ZoneID,
                    actualPayment=bill.Select(p=>new{blueprint=p.Blueprint,id=p.ID,units=p.Units}).ToArray()});
                var cell=zone.GetEntityCell(ParentEntity); if(cell!=null)ZoneRenderHooks.MarkCellDirty(cell.X,cell.Y,"Repair.Completed");
                workAudio?.Publish();
            });
            return true;
        }
        internal void RemoveBrokenWithUndo(InventoryTransaction tx)
        {
            var owner = ParentEntity;
            var effects = owner.GetPart<StatusEffectsPart>();
            var broken = effects.GetEffect<BrokenEffect>();
            int index = 0; foreach (var effect in effects.GetAllEffects()) { if (effect == broken) break; index++; }
            var melee = owner.GetPart<MeleeWeaponPart>(); var armor = owner.GetPart<ArmorPart>();
            int priorHitPenalty = broken.HitPenalty, priorArmorPenalty = broken.ArmorPenalty;
            bool priorApplied = broken.PenaltyApplied; string priorCause = broken.LastRemovalCause;
            // Register before callbacks; an EffectRemoved listener can throw.
            tx.Do(null, () =>
            {
                // Undo only this repair's restoration, retaining independent
                // enchantment/stat changes made by later callback work.
                if (priorApplied && !broken.PenaltyApplied)
                {
                    if (melee != null) melee.HitBonus -= priorHitPenalty;
                    if (armor != null) armor.AV -= priorArmorPenalty;
                }
                broken.HitPenalty = priorHitPenalty; broken.ArmorPenalty = priorArmorPenalty;
                broken.PenaltyApplied = priorApplied; broken.LastRemovalCause = priorCause;
                effects.RestoreRemovedEffectForInventoryUndo(broken,index);
                EquipmentChangeBus.NotifyChanged(owner.GetPart<PhysicsPart>()?.Equipped);
            });
            effects.RemoveEffect(broken);
        }

        private bool Reject(Entity actor,string reason)
        {
            if(actor?.HasTag("Player")==true)MessageLog.Add(reason=="already-repaired"?"That repair is already complete."
                : reason=="rig-unavailable" && ParentEntity?.HasPart<DraggedPart>()==true ? "Let go before repairing or using this rig." : Describe());
            Diag.Record("furniture","RepairRejected",actor,ParentEntity,new{recipeId=RecipeId,reason});return false;
        }
    }
}
