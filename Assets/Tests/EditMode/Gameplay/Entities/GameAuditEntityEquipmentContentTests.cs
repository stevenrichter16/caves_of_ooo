using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    internal sealed class EntityEquipmentContentFixture : IDisposable
    {
        readonly EquipmentLifecycleFixture _scope = new EquipmentLifecycleFixture();
        readonly System.Random _oldLoadoutRng = LoadoutPart.Rng, _oldTraderRng = TraderPart.Rng;
        readonly Dictionary<string, LootTableData> _tables = (Dictionary<string, LootTableData>)typeof(LootTableRegistry)
            .GetField("_byName", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        readonly FieldInfo _initialized = typeof(LootTableRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        readonly Dictionary<string, LootTableData> _oldTables;
        readonly bool _oldInitialized;
        public readonly EntityFactory Factory = new EntityFactory();
        public readonly List<string> Messages = new List<string>();

        public EntityEquipmentContentFixture()
        {
            _oldTables = new Dictionary<string, LootTableData>(_tables);
            _oldInitialized = (bool)_initialized.GetValue(null);
            try
            {
                Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
                LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
                LoadoutPart.Factory = TraderPart.Factory = Factory;
                LoadoutPart.Rng = new EquipmentContentRng();
                TraderPart.Rng = new EquipmentContentRng();
                MessageLog.OnMessage = Messages.Add;
            }
            catch { Dispose(); throw; }
        }

        public Entity Create(string blueprint) => Factory.CreateEntity(blueprint);
        public void Dispose()
        {
            _tables.Clear();
            foreach (var pair in _oldTables) _tables.Add(pair.Key, pair.Value);
            _initialized.SetValue(null, _oldInitialized);
            LoadoutPart.Rng = _oldLoadoutRng; TraderPart.Rng = _oldTraderRng;
            _scope.Dispose();
        }
    }

    internal sealed class EquipmentContentRng : System.Random
    {
        public bool OptionalAbsent, LastPick, MaximumCount;
        public override int Next(int maxValue)
        {
            if (maxValue <= 0) return 0;
            if (maxValue == 100) return OptionalAbsent ? 99 : 0;
            return LastPick ? maxValue - 1 : 0;
        }
        public override int Next(int minValue, int maxValue) => MaximumCount ? Math.Max(minValue, maxValue - 1) : minValue;
    }

    public sealed class EquipmentContentKit
    {
        public readonly string Blueprint, Equip, Carry, Pick;
        public EquipmentContentKit(string blueprint, string equip, string carry = "", string pick = "")
        { Blueprint = blueprint; Equip = equip; Carry = carry; Pick = pick; }
        public override string ToString() => Blueprint;
    }

    public class GameAuditEntityEquipmentContentTests
    {
        // Literal content requirements, independent of the blueprint's actual authored values.
        public static readonly EquipmentContentKit[] Kits = {
            new EquipmentContentKit("Snapjaw", "Dagger;LeatherCap:20"),
            new EquipmentContentKit("SnapjawScavenger", "LeatherGloves:35", pick: "1;Dagger;ShortSword"),
            new EquipmentContentKit("SnapjawHunter", "Spear;LeatherBoots:35"),
            new EquipmentContentKit("SnapjawChieftain", "ShortSword;LeatherArmor;LeatherCap"),
            new EquipmentContentKit("SnapjawWarlord", "LeatherArmor;IronHelmet;IronshodBoots"),
            new EquipmentContentKit("DesertBandit", "ShortSword;LeatherCap:30"),
            new EquipmentContentKit("RuinScavenger", "Dagger;LeatherGloves:35"),
            new EquipmentContentKit("SkeletalSentry", "IronHelmet"),
            new EquipmentContentKit("AmbushBandit", "ShortSword;LeatherArmor:35"),
            new EquipmentContentKit("RuneCultist", "Dagger;LeatherGloves:35"),
            new EquipmentContentKit("Warden", "LongSword;LeatherArmor;LeatherBoots"),
            new EquipmentContentKit("Quartermaster", "Spear;LeatherArmor;LeatherBoots"),
            new EquipmentContentKit("Weaponsmith", "LeatherGloves;LeatherBoots"),
            new EquipmentContentKit("Tinker", "Dagger;LeatherGloves"),
            new EquipmentContentKit("Farmer", "LeatherBoots;LeatherGloves"),
            new EquipmentContentKit("WellKeeper", "LeatherBoots;LeatherCap"),
            new EquipmentContentKit("PeatCutter", "LeatherBoots;LeatherGloves", carry: "Dagger"),
            new EquipmentContentKit("Elder", "LeatherCap;LeatherBoots"),
            new EquipmentContentKit("Scribe", "LeatherGloves;LeatherCap"),
            new EquipmentContentKit("Merchant", "ShortSword;LeatherBoots"),
            new EquipmentContentKit("TentRightHost", "LeatherBoots;LeatherCap", carry: "Dagger"),
            new EquipmentContentKit("SaltMaster", "LeatherGloves;LeatherBoots"),
            new EquipmentContentKit("RecensionScribe", "LeatherGloves;LeatherBoots"),
            new EquipmentContentKit("CurationSorter", "LeatherGloves;LeatherCap"),
            new EquipmentContentKit("GantryRegistrar", "LeatherGloves;LeatherBoots"),
            // Stillleaf Archive SA.2: the Searcher wears her house's gloves and boots (inherits RecensionScribe).
            new EquipmentContentKit("StillleafSearcher", "LeatherGloves;LeatherBoots"),
            // Stillleaf Archive SA.3: the Indexer wears Curation's gloves and cap (inherits CurationSorter).
            new EquipmentContentKit("StillleafIndexer", "LeatherGloves;LeatherCap")
        };

        [TestCaseSource(nameof(Kits))]
        public void ActualBlueprintCreatesItsAuthoredKitWithRealOwnership(EquipmentContentKit kit)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create(kit.Blueprint);
                var loadout = actor.GetPart<LoadoutPart>();
                Assert.NotNull(loadout, kit.Blueprint + " has no authored starting equipment.");
                Assert.AreEqual(kit.Equip, loadout.Equip); Assert.AreEqual(kit.Carry, loadout.Carry); Assert.AreEqual(kit.Pick, loadout.Pick);
                var expected = kit.Equip.Split(';').Where(x => x.Length > 0).Select(x => x.Split(':')[0]).ToList();
                if (kit.Pick.Length > 0) expected.Add(kit.Pick.Split(';')[1]);
                var inventory = actor.GetPart<InventoryPart>();
                var equipped = inventory.GetAllEquipped();
                CollectionAssert.AreEquivalent(expected, equipped.Select(x => x.BlueprintName));
                foreach (var item in equipped)
                {
                    Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
                    LoadoutLifecycleFixture.Equipped(actor, item);
                    Assert.AreEqual(item.GetPart<EquippablePart>().Slot,
                        actor.GetPart<Body>().GetParts().Single(x => ReferenceEquals(x._Equipped, item)).Type);
                }
                foreach (string name in kit.Carry.Split(';').Where(x => x.Length > 0))
                    LoadoutLifecycleFixture.Carried(actor, inventory.Objects.Single(x => x.BlueprintName == name));
                Assert.LessOrEqual(inventory.GetCarriedWeight(), inventory.MaxWeight);
                Assert.IsFalse(f.Messages.Any(x => x.StartsWith(actor.GetDisplayName() + " equips ")),
                    "Factory-created NPC gear must not announce offscreen equipment.");
            }
        }

        [TestCase("Equip")] [TestCase("Pick")] [TestCase("Carry")]
        public void AutomaticCreationIsQuietWhileItsEquipmentHooksAndReceiptsRemain(string mode)
        {
            // Supported API fixture proves the command seam before the 24 real producers exist.
            using (var f = new LoadoutLifecycleFixture(equip: mode == "Equip" ? "IronshodBoots" : "",
                carry: mode == "Carry" ? "IronshodBoots" : "", pick: mode == "Pick" ? "1;IronshodBoots" : ""))
            {
                bool old = Diag.IsChannelEnabled("event"); Diag.SetChannel("event", true);
                var messages = new List<string>(); MessageLog.OnMessage = messages.Add;
                LoadoutAuditProbe.AuditHook = (probe, e) => {
                    if (e.ID == "ObjectCreated") probe.ParentEntity.ID = "GA03i-" + Guid.NewGuid().ToString("N");
                    return true;
                };
                try
                {
                    var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor); var probe = actor.GetPart<LoadoutAuditProbe>();
                    bool equip = mode != "Carry";
                    if (equip) LoadoutLifecycleFixture.Equipped(actor, item); else LoadoutLifecycleFixture.Carried(actor, item);
                    Assert.AreEqual(equip ? 5 : 0, actor.GetStat("Speed").Penalty);
                    Assert.AreEqual(equip ? 1 : 0, probe.BeforeCount); Assert.AreEqual(equip ? 1 : 0, probe.AfterCount);
                    Assert.AreEqual(equip ? 1 : 0, DiagQuery.Count(new DiagQuery.Filter { Category = "event", Kind = "LoadoutEquipResult", Actor = actor.ID, Target = item.ID }).Count);
                    Assert.IsFalse(messages.Any(x => x.Contains(" equips ")), "Automatic creation leaked an equip announcement.");
                }
                finally { Diag.SetChannel("event", old); }
            }
        }

        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void OrdinaryEquipAndAutoEquipRemainAudibleForBodyAndLegacyActors(bool auto, bool body)
        {
            using (var f = new LoadoutLifecycleFixture(carry: "IronshodBoots", body: body))
            {
                var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor);
                var messages = new List<string>(); MessageLog.OnMessage = messages.Add;
                Assert.IsTrue(auto ? InventorySystem.AutoEquip(actor, item) : InventorySystem.Equip(actor, item));
                Assert.AreEqual(5, actor.GetStat("Speed").Penalty);
                Assert.AreEqual(1, messages.Count(x => x == actor.GetDisplayName() + " equips " + item.GetDisplayName() + "."));
            }
        }

        [Test]
        public void QuietCreationCannotMuteAnIndependentEquipInsideItsHook()
        {
            using (var f = new LoadoutLifecycleFixture("IronshodBoots"))
            {
                var other = f.Factory.CreateEntity("Player"); var dagger = f.Factory.CreateEntity("Dagger");
                Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(dagger));
                var messages = new List<string>(); MessageLog.OnMessage = messages.Add;
                bool nested = false; int attempts = 0;
                LoadoutAuditProbe.AuditHook = (probe, e) => {
                    if (e.ID == "BeforeEquip") { attempts++; nested = InventorySystem.Equip(other, dagger); }
                    return true;
                };
                var actor = f.Create(); var boots = LoadoutLifecycleFixture.OnlyItem(actor);
                Assert.AreEqual(1, attempts); Assert.IsTrue(nested);
                LoadoutLifecycleFixture.Equipped(other, dagger); LoadoutLifecycleFixture.Equipped(actor, boots);
                Assert.AreEqual(1, messages.Count(x => x == other.GetDisplayName() + " equips " + dagger.GetDisplayName() + "."));
                Assert.IsFalse(messages.Any(x => x == actor.GetDisplayName() + " equips " + boots.GetDisplayName() + "."));
            }
        }

        [Test]
        public void RefusedAutomaticEquipKeepsItsGrantWithoutSuccessProse()
        {
            using (var f = new LoadoutLifecycleFixture("IronshodBoots", mode: "Veto"))
            {
                var messages = new List<string>(); MessageLog.OnMessage = messages.Add;
                var actor = f.Create(); var item = LoadoutLifecycleFixture.OnlyItem(actor);
                LoadoutLifecycleFixture.Carried(actor, item);
                Assert.AreEqual(0, actor.GetStat("Speed").Penalty);
                Assert.AreEqual(1, actor.GetPart<LoadoutAuditProbe>().BeforeCount);
                Assert.AreEqual(0, actor.GetPart<LoadoutAuditProbe>().AfterCount);
                Assert.IsFalse(messages.Any(x => x.Contains(" equips ")));
            }
        }

        [Test]
        public void UnselectedBaseVillagerAndSummitFrogRetainTheirExistingAnatomyAndNoKit()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                foreach (string name in new[] { "Villager", "SummitSinger" })
                {
                    var actor = f.Create(name);
                    Assert.IsNull(actor.GetPart<LoadoutPart>());
                    Assert.AreEqual(0, actor.GetPart<InventoryPart>().GetAllEquipped().Count);
                    Assert.NotNull(actor.GetPart<Body>().GetBody());
                }
            }
        }
    }
}
