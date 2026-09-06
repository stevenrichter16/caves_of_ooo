using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Explicit API fixtures; actual world loadouts are covered by GameAuditEntityEquipmentContentTests.
    internal sealed class LoadoutLifecycleFixture : IDisposable
    {
        readonly EquipmentLifecycleFixture _scope = new EquipmentLifecycleFixture();
        readonly System.Random _oldRng = LoadoutPart.Rng;
        readonly Func<LoadoutAuditProbe, GameEvent, bool> _oldHook = LoadoutAuditProbe.AuditHook;
        public readonly EntityFactory Factory = new EntityFactory();
        public readonly Blueprint ActorBlueprint = new Blueprint { Name = "GA03gActor", Baked = true };
        public LoadoutLifecycleFixture(string equip = "", string carry = "", string pick = "", bool body = true, string mode = "")
        {
            try
            {
                LoadoutAuditProbe.AuditHook = null;
                Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
                Factory.RegisterPartType<LoadoutAuditProbe>();
                Factory.RegisterPartType<LoadoutAuditItemMod>();
                ActorBlueprint.Tags["Creature"] = "";
                ActorBlueprint.Props["Anatomy"] = "Humanoid";
                foreach (var pair in new[] { ("Strength", 16), ("Agility", 14), ("Speed", 100), ("Hitpoints", 20) })
                    ActorBlueprint.Stats[pair.Item1] = new StatBlueprint { Name = pair.Item1, Value = pair.Item2, Min = 0, Max = 1000 };
                ActorBlueprint.Parts["Physics"] = new Dictionary<string,string>();
                if (body) ActorBlueprint.Parts["Body"] = new Dictionary<string,string>();
                ActorBlueprint.Parts["Inventory"] = new Dictionary<string,string> { ["MaxWeight"] = "200" };
                ActorBlueprint.Parts[nameof(LoadoutAuditProbe)] = new Dictionary<string,string> { ["Mode"] = mode };
                ActorBlueprint.Parts["Loadout"] = new Dictionary<string,string> { ["Equip"] = equip, ["Carry"] = carry, ["Pick"] = pick };
                Factory.Blueprints[ActorBlueprint.Name] = ActorBlueprint;
                LoadoutPart.Factory = Factory;
                LoadoutPart.Rng = new System.Random(735);
            }
            catch { Dispose(); throw; }
        }
        public Entity Create() => Factory.CreateEntity(ActorBlueprint.Name);
        public void Mod(string blueprint, string mode) => Factory.Blueprints[blueprint].Parts[nameof(LoadoutAuditItemMod)] = new Dictionary<string,string> { ["Mode"] = mode };
        public static Entity OnlyItem(Entity actor) => actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().EquippedItems.Values).Distinct().Single();
        public static void Equipped(Entity actor, Entity item, int slots = 1)
        {
            var inv = actor.GetPart<InventoryPart>();
            var parts = actor.GetPart<Body>().GetParts().Where(p => ReferenceEquals(p._Equipped, item)).ToArray();
            Assert.AreEqual(slots, parts.Length);
            Assert.AreEqual(1, parts.Count(p => p.FirstSlotForEquipped));
            Assert.AreEqual(slots, inv.EquippedItems.Values.Count(e => ReferenceEquals(e, item)));
            Assert.IsFalse(inv.Objects.Contains(item));
            Assert.AreSame(actor, item.GetPart<PhysicsPart>().Equipped);
            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
        }
        public static void Carried(Entity actor, Entity item)
        {
            var inv = actor.GetPart<InventoryPart>();
            Assert.AreEqual(1, inv.Objects.Count(e => ReferenceEquals(e, item)));
            Assert.IsFalse(inv.EquippedItems.Values.Contains(item));
            Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
            Assert.AreSame(actor, item.GetPart<PhysicsPart>().InInventory);
        }
        public void Dispose() { LoadoutAuditProbe.AuditHook = _oldHook; LoadoutPart.Rng = _oldRng; _scope.Dispose(); }
    }
    public sealed class LoadoutAuditItemMod : Part
    {
        public string Mode;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "ObjectCreated") return true;
            string reason;
            bool applied = Mode == "Glow" ? new GlowQuartzTinkerModification().Apply(ParentEntity, out reason)
                : new DuelistCutTinkerModification().Apply(ParentEntity, out reason);
            if (!applied) throw new InvalidOperationException("Actual loadout item mod failed: " + reason);
            return true;
        }
    }
    public sealed class LoadoutAuditProbe : Part
    {
        public static Func<LoadoutAuditProbe, GameEvent, bool> AuditHook;
        public string Mode;
        public int BeforeCount, AfterCount;
        public Entity FirstItem, Injected;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ObjectCreated" && Mode == "Occupied")
            {
                Injected = LoadoutPart.Factory.CreateEntity("IronshodBoots");
                if (!ParentEntity.GetPart<InventoryPart>().AddObject(Injected) || !InventorySystem.Equip(ParentEntity, Injected))
                    throw new InvalidOperationException("Occupied-slot positive control failed.");
            }
            if (e.ID == "BeforeEquip")
            {
                BeforeCount++;
                if (FirstItem == null) FirstItem = e.GetParameter<Entity>("Item");
                if (Mode == "Veto") return false;
            }
            if (e.ID == "AfterEquip") AfterCount++;
            return AuditHook == null || AuditHook(this, e);
        }
    }
    public class GameAuditLoadoutLifecycleTests
    {
        [TestCase(false)] [TestCase(true)]
        public void ActualBootsEquipAppliesArmorPenaltyWhileCarryDoesNot(bool carry)
        {
            using (var f = new LoadoutLifecycleFixture(equip: carry ? "" : "IronshodBoots", carry: carry ? "IronshodBoots" : ""))
            {
                var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor);
                Assert.AreEqual(5, item.GetPart<ArmorPart>().SpeedPenalty);
                if (carry) LoadoutLifecycleFixture.Carried(actor, item); else LoadoutLifecycleFixture.Equipped(actor, item);
                Assert.AreEqual(carry ? 0 : 5, actor.GetStat("Speed").Penalty);
                Assert.AreEqual(carry ? 0 : 1, actor.GetPart<LoadoutAuditProbe>().AfterCount);
            }
        }
        [Test]
        public void UnequippingLoadoutBootsCannotCreateNegativePenalty()
        {
            using (var f = new LoadoutLifecycleFixture("IronshodBoots"))
            {
                var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor);
                LoadoutLifecycleFixture.Equipped(actor, item);
                Assert.IsTrue(InventorySystem.UnequipItem(actor, item));
                Assert.AreEqual(0, actor.GetStat("Speed").Penalty);
                Assert.IsTrue(InventorySystem.Equip(actor, item));
                Assert.AreEqual(5, actor.GetStat("Speed").Penalty);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualGlowQuartzProducerActivatesOnlyForEquippedLoadout(bool carry)
        {
            using (var f = new LoadoutLifecycleFixture(equip: carry ? "" : "Dagger", carry: carry ? "Dagger" : ""))
            {
                f.Mod("Dagger", "Glow"); var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor);
                var glow = item.GetPart<EnhancementGlowQuartz>();
                Assert.NotNull(glow); Assert.AreEqual(2, glow.RadiusBonus);
                Assert.AreEqual(!carry, glow.AppliedBonus);
                Assert.AreEqual(carry ? 0 : 2, item.GetPart<LightSourcePart>()?.Radius ?? 0);
            }
        }
        [Test]
        public void BeforeEquipVetoRetainsExactGrantedItemWithoutHooksOrPenalty()
        {
            using (var f = new LoadoutLifecycleFixture("IronshodBoots", mode: "Veto"))
            {
                var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor); var probe = actor.GetPart<LoadoutAuditProbe>();
                Assert.AreEqual(1, probe.BeforeCount); Assert.AreSame(item, probe.FirstItem);
                Assert.AreEqual(0, probe.AfterCount); Assert.AreEqual(0, actor.GetStat("Speed").Penalty);
                LoadoutLifecycleFixture.Carried(actor, item);
            }
        }
        [Test]
        public void RealTwoHandWeaponAppliesConfiguredBonusAndHookOnlyOnce()
        {
            using (var f = new LoadoutLifecycleFixture("Greatsword"))
            {
                f.Factory.Blueprints["Greatsword"].Parts["Equippable"]["EquipBonuses"] = "Agility:2";
                var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor);
                LoadoutLifecycleFixture.Equipped(actor, item, 2);
                Assert.AreEqual(16, actor.GetStatValue("Agility")); Assert.AreEqual(1, actor.GetPart<LoadoutAuditProbe>().AfterCount);
            }
        }
        [Test]
        public void NoBodyKeepsLoadoutCarriedWithoutLegacyFallback()
        {
            using (var f = new LoadoutLifecycleFixture("IronshodBoots", body: false))
            {
                var actor = f.Create(); LoadoutLifecycleFixture.Carried(actor, LoadoutLifecycleFixture.OnlyItem(actor));
                Assert.AreEqual(0, actor.GetPart<LoadoutAuditProbe>().BeforeCount); Assert.AreEqual(0, actor.GetStat("Speed").Penalty);
            }
        }
        [Test]
        public void ExistingEquipmentRemainsAndNewGrantStaysCarried()
        {
            using (var f = new LoadoutLifecycleFixture("IronshodBoots", mode: "Occupied"))
            {
                var actor = f.Create(); var probe = actor.GetPart<LoadoutAuditProbe>();
                LoadoutLifecycleFixture.Equipped(actor, probe.Injected);
                var carried = actor.GetPart<InventoryPart>().Objects.Single();
                Assert.AreNotSame(probe.Injected, carried); LoadoutLifecycleFixture.Carried(actor, carried);
                Assert.AreEqual(5, actor.GetStat("Speed").Penalty); Assert.AreEqual(1, probe.AfterCount);
            }
        }
    }
}
