using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase A6 — economy plumbing
    /// (Docs/BIOME-OVERHAUL.md §2 A6, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Four verified dead-ends: (1) Quartermaster/Scribe/Elder/Warden
    /// carry sellable stock but no Drams wallet, so they can never BUY
    /// (TradeSystem "trader can't afford that") and TraderRestockSystem
    /// skips them forever; (2) Ink — the rental currency — has zero
    /// renewable sources (the starting 50 is a lifetime supply);
    /// (3) GoldCoin/Bone have no CommercePart (worth 0, filtered from
    /// trade); (4) CandyCarrot/Emberwheat — the game's only harvest
    /// outputs — inherit Item, not FoodItem, so they don't group as food.
    /// </summary>
    [TestFixture]
    public class BiomeEconomyPlumbingTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static int DramsOf(string blueprint)
        {
            var e = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, blueprint);
            return e.GetIntProperty(TradeSystem.CURRENCY_PROP, -1);
        }

        // ── 1. Wallets ───────────────────────────────────────────

        [Test]
        public void NamedVillageNpcs_CarryDramWallets()
        {
            Assert.AreEqual(150, DramsOf("Quartermaster"), "quartermaster");
            Assert.AreEqual(100, DramsOf("Scribe"), "scribe");
            Assert.AreEqual(120, DramsOf("Elder"), "elder");
            Assert.AreEqual(80, DramsOf("Warden"), "warden");
        }

        [Test]
        public void VillagerLineage_InheritsWallet_Pin()
        {
            // Discovered during the A6 verification sweep: IntProps DO
            // inherit through blueprint bake (BlueprintLoader.Bake), so
            // WellKeeper/Farmer already carry Villager's 100 drams. Pin
            // it so a future bake change can't silently disinherit them.
            Assert.AreEqual(100, DramsOf("WellKeeper"), "well-keeper inherits Villager wallet");
            Assert.AreEqual(100, DramsOf("Farmer"), "farmer inherits Villager wallet");
        }

        [Test]
        public void WildCreature_HasNoWallet_CounterCheck()
        {
            Assert.AreEqual(-1, DramsOf("Snapjaw"),
                "wallets are a villager-NPC thing; wild creatures stay walletless");
        }

        // ── 2. InkVial — renewable rental currency ───────────────

        [Test]
        public void InkVial_Blueprint_IsWiredForTrade()
        {
            var vial = _factory.CreateEntity("InkVial");
            Assert.IsNotNull(vial, "InkVial blueprint must exist");
            var commerce = vial.GetPart<CommercePart>();
            Assert.IsNotNull(commerce, "tradeable");
            Assert.AreEqual(10, commerce.Value);
            Assert.IsNotNull(vial.GetPart<StackerPart>(), "stacks like tonics");
            Assert.IsNotNull(vial.GetPart<InkVialPart>(), "usable");
        }

        private static Entity MakeActorWithInventory()
        {
            var actor = new Entity { ID = "drinker" };
            actor.AddPart(new RenderPart { DisplayName = "drinker" });
            actor.AddPart(new InventoryPart { MaxWeight = 100 });
            return actor;
        }

        private static void FireUse(Entity item, Entity actor)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "UseInkVial");
            e.SetParameter("Actor", (object)actor);
            item.FireEvent(e);
            e.Release();
        }

        [Test]
        public void InkVial_Use_AddsInkAndConsumes()
        {
            var actor = MakeActorWithInventory();
            RentalSystem.SetInk(actor, 10);
            var vial = _factory.CreateEntity("InkVial");
            var stack = vial.GetPart<StackerPart>();
            stack.StackCount = 2;
            actor.GetPart<InventoryPart>().AddObject(vial);

            FireUse(vial, actor);
            Assert.AreEqual(35, RentalSystem.GetInk(actor), "+25 per vial");
            Assert.AreEqual(1, stack.StackCount, "stack decremented, not destroyed");
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(vial));

            FireUse(vial, actor);
            Assert.AreEqual(60, RentalSystem.GetInk(actor));
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(vial),
                "last vial in the stack is consumed outright");
        }

        [Test]
        public void InkVial_Use_EmitsTradeDiag()
        {
            var actor = MakeActorWithInventory();
            var vial = _factory.CreateEntity("InkVial");
            actor.GetPart<InventoryPart>().AddObject(vial);

            Diag.ResetAll();
            FireUse(vial, actor);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Kind = "InkRefilled",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, records.Count,
                "ink refill emits exactly one trade/InkRefilled record");
            StringAssert.Contains("25", records[0].PayloadJson, "amount in payload");
        }

        [Test]
        public void Scribe_VillageBuild_StockedWithInkVials()
        {
            // The Scribe is the diegetic ink source (they copy grimoires
            // for a living). Every village build stocks them.
            FactionManager.Initialize();
            try
            {
                var poi = new PointOfInterest(POIType.Village, "Test Village", "Villagers");
                var zone = new Zone("Overworld.10.10.0");
                Assert.IsTrue(new VillageBuilder(BiomeType.Cave, poi)
                    .BuildZone(zone, _factory, new Random(42)));
                Assert.IsTrue(new VillagePopulationBuilder(poi)
                    .BuildZone(zone, _factory, new Random(42)));

                Entity scribe = null;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == "Scribe") { scribe = e; break; }
                Assert.IsNotNull(scribe, "starting village always has a Scribe");

                int vials = 0;
                var inv = scribe.GetPart<InventoryPart>();
                Assert.IsNotNull(inv);
                foreach (var item in inv.Objects)
                    if (item.BlueprintName == "InkVial")
                        vials += item.GetPart<StackerPart>()?.StackCount ?? 1;
                Assert.GreaterOrEqual(vials, 2, "scribe carries ink for sale");
            }
            finally
            {
                FactionManager.Reset();
            }
        }

        // ── 3. GoldCoin / Bone become tradeable ──────────────────

        [Test]
        public void GoldCoinAndBone_AreTradeGoods()
        {
            var coin = _factory.CreateEntity("GoldCoin");
            Assert.IsNotNull(coin.GetPart<CommercePart>(), "coin commerce");
            Assert.AreEqual(1, coin.GetPart<CommercePart>().Value);
            Assert.IsNotNull(coin.GetPart<StackerPart>(), "coins stack — chest treasure");

            var bone = _factory.CreateEntity("Bone");
            Assert.IsNotNull(bone.GetPart<CommercePart>(), "bone commerce");
            Assert.AreEqual(1, bone.GetPart<CommercePart>().Value);
            Assert.IsNotNull(bone.GetPart<StackerPart>(), "bones stack — butchery yield");
        }

        // ── 4. Harvest foods group as food ───────────────────────

        [Test]
        public void HarvestFoods_AreRealFoodItems()
        {
            foreach (var (name, healing) in new[] { ("CandyCarrot", "1d4"), ("Emberwheat", "2d4") })
            {
                var food = _factory.CreateEntity(name);
                Assert.IsTrue(food.HasTag("Food"), $"{name}: Food tag from FoodItem base");
                Assert.AreEqual("Food", food.GetPart<PhysicsPart>().Category,
                    $"{name}: groups with food in inventory UI");
                Assert.AreEqual(healing, food.GetPart<FoodPart>().Healing,
                    $"{name}: own Food params survive the inherit merge");
            }
        }
    }
}
