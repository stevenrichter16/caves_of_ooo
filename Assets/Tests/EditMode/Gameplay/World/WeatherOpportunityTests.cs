using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Reflection keeps the missing-feature RED compiling against the real core.
    public static class WeatherTestApi
    {
        public static Type Type(string name)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core." + name);
            Assert.NotNull(type, "Missing weather opportunity contract: " + name);
            return type;
        }
        public static object Call(string name, params object[] args)
        {
            var method = Type("RegionalWeather").GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, name);
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException e) { throw e.InnerException; }
        }
        public static Part Part(string name) => (Part)Activator.CreateInstance(Type(name));
        public static object Get(object owner, string name)
        {
            var field = owner.GetType().GetField(name); Assert.NotNull(field, name); return field.GetValue(owner);
        }
        public static void Set(object owner, string name, object value)
        { var field = owner.GetType().GetField(name); Assert.NotNull(field, name); field.SetValue(owner, value); }
        public static bool Rain(int seed, BiomeType biome, int window) => (bool)Call("IsRainWindow", seed, biome, window);
        public static int Count(int seed, BiomeType biome, int first, int end) => (int)Call("CountRainWindows", seed, biome, first, end);
    }

    public sealed class RegionalWeatherTests
    {
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)]
        [TestCase(BiomeType.Beating)] [TestCase(BiomeType.Grovelands)]
        public void VersionOneIsARepeatableFinitePatternWithWetAndFairOpportunities(BiomeType biome)
        {
            int wet = 0;
            for (int i = 0; i < 64; i++)
            {
                bool rain = WeatherTestApi.Rain(64, biome, i);
                if (rain) wet++;
                Assert.AreEqual(rain, WeatherTestApi.Rain(64, biome, i + 64));
                Assert.AreEqual(rain, WeatherTestApi.Rain(64, biome, i + 64 * 10000));
            }
            Assert.That(wet, Is.InRange(1, 63));
            Assert.AreEqual(wet, WeatherTestApi.Count(64, biome, 0, 64));
        }
        [TestCase(0, 1)] [TestCase(3, 150)] [TestCase(127, 517)]
        public void PrefixCountsMatchAnExplicitBoundedReference(int first, int end)
        {
            int expected = Enumerable.Range(first, end - first).Count(i => WeatherTestApi.Rain(17, BiomeType.Spread, i));
            Assert.AreEqual(expected, WeatherTestApi.Count(17, BiomeType.Spread, first, end));
        }
        [Test] public void LongAbsenceUsesWholeCyclesWithoutOverflowOrHistoricalReplay()
        {
            int per = WeatherTestApi.Count(64, BiomeType.Sodden, 0, 64);
            Assert.AreEqual(per * 1000000, WeatherTestApi.Count(64, BiomeType.Sodden, 0, 64000000));
            int first = int.MaxValue - 200;
            int expected = Enumerable.Range(first, 200).Count(i => WeatherTestApi.Rain(64, BiomeType.Sodden, i));
            Assert.AreEqual(expected, WeatherTestApi.Count(64, BiomeType.Sodden, first, int.MaxValue));
        }
        [Test] public void SnapshotUsesPassedWorldTickAndRejectsUnknownPolicy()
        {
            var before = WeatherTestApi.Call("At", 64, BiomeType.Spread, 599, 1);
            var after = WeatherTestApi.Call("At", 64, BiomeType.Spread, 600, 1);
            Assert.AreEqual(0, WeatherTestApi.Get(before, "Window"));
            Assert.AreEqual(1, WeatherTestApi.Get(after, "Window"));
            Assert.True((bool)WeatherTestApi.Get(before, "Valid"));
            Assert.False((bool)WeatherTestApi.Get(WeatherTestApi.Call("At", 64, BiomeType.Spread, 600, 0), "Valid"));
            Assert.False((bool)WeatherTestApi.Get(WeatherTestApi.Call("At", 64, BiomeType.Spread, -1, 1), "Valid"));
        }
        [Test] public void UndergroundDoesNotReceiveSurfaceRain()
        { Assert.AreEqual(0, WeatherTestApi.Count(64, BiomeType.Cave, 0, 6400)); }
        [Test] public void InvalidIntervalsDoNotManufactureCredit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WeatherTestApi.Count(1, BiomeType.Spread, -1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => WeatherTestApi.Count(1, BiomeType.Spread, 10, 9));
        }
    }

    public sealed class RainCollectionTests : FiftyWorldFixture
    {
        Part allowance, firstPart, secondPart;
        Entity anchor, first, second;
        OverworldZoneManager manager;
        const string Prefix = "CollectRain|";
        [SetUp] public void SetUpRain()
        {
            Zone.ZoneID = "Overworld.2.6.0";
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            Zone.CurrentWindSpeed = 13; Zone.CurrentWindDirection = "NE";
            for (int x = 8; x <= 13; x++) for (int y = 7; y <= 12; y++) Place("Grass", x, y);
            anchor = Place("Signpost", 10, 9); allowance = WeatherTestApi.Part("RainCatchmentPart"); anchor.AddPart(allowance);
            WeatherTestApi.Set(allowance, "Ground", Terrain(10, 9));
            WeatherTestApi.Set(allowance, "WorldKey", manager.Exploration.WorldKey);
            WeatherTestApi.Set(allowance, "PlotKey", "test-public-catchment");
            WeatherTestApi.Set(allowance, "Public", true); WeatherTestApi.Set(allowance, "PolicyVersion", 1);
            WeatherTestApi.Set(allowance, "ActivatedAt", 0);
            first = Collector(11, 10, out firstPart); second = Collector(9, 10, out secondPart);
            int rain = Enumerable.Range(0, 64).First(i => WeatherTestApi.Rain(64, BiomeType.Grovelands, i));
            Clock.AdvanceClock(rain * 600 + 10);
            RainCollectionCallback.After = null;
        }
        [TearDown] public void ClearHook() { RainCollectionCallback.After = null; }
        Entity Terrain(int x, int y) => Zone.GetCell(x, y).Objects.Single(e => e.HasTag("Terrain"));
        Entity Collector(int x, int y, out Part part)
        {
            var e = Place("CampAwning", x, y); e.RemovePart(e.GetPart<PackableFixturePart>());
            e.GetPart<PhysicsPart>().Takeable = false; e.GetPart<PhysicsPart>().Solid = true;
            part = WeatherTestApi.Part("RainCollectorPart"); e.AddPart(part);
            WeatherTestApi.Set(part, "Anchor", anchor); WeatherTestApi.Set(part, "Ground", Terrain(x, y));
            WeatherTestApi.Set(part, "Clean", true); return e;
        }
        InventoryAction[] Rows(Entity collector) => Actions(collector).Where(a => a.Command?.StartsWith(Prefix, StringComparison.Ordinal) == true).ToArray();
        InventoryAction Row(Entity collector, Entity vessel) => Rows(collector).Single(a => a.Command.Split('|')[3] == Uri.EscapeDataString(vessel.ID));
        int Spent => (int)WeatherTestApi.Get(allowance, "Spent");

        [Test] public void ExactFlaskReceivesThreeOrdinaryWaterUnitsWithoutChangingOtherVesselsOrWind()
        {
            var chosen = Flask(0); var other = Flask(0); var row = Row(first, chosen);
            StringAssert.Contains("3", row.Display); Assert.True(Act(first, row.Command));
            Assert.AreEqual(3, Units(chosen)); Assert.AreEqual("water", chosen.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(0, Units(other)); Assert.AreEqual(3, Spent);
            Assert.AreEqual(13, Zone.CurrentWindSpeed); Assert.AreEqual("NE", Zone.CurrentWindDirection);
            Assert.False(Actor.HasEffect<WetEffect>()); Assert.False(Zone.TileState.HasCoating(10, 10, "water"));
        }
        [TestCase("flask")] [TestCase("skin")]
        public void PartialCapacityLeavesTheRestForTheSecondCollector(string kind)
        {
            var small = kind == "skin" ? Skin(0, 1) : Flask(0, capacity: 1);
            Assert.True(Act(first, Row(first, small).Command)); Assert.AreEqual(1, Spent);
            var other = Flask(0); Assert.True(Act(second, Row(second, other).Command));
            Assert.AreEqual(2, Units(other)); Assert.AreEqual(3, Spent); Assert.IsEmpty(Rows(first));
        }
        [Test] public void ReadersAndMenuReopeningDoNotSpendOrRefillTheSharedAllowance()
        {
            var bottle = Flask(0); int window = (int)WeatherTestApi.Get(allowance, "LastWindow");
            for (int i = 0; i < 6; i++) Assert.AreEqual(1, Rows(first).Length);
            Assert.AreEqual(0, Spent); Assert.AreEqual(window, WeatherTestApi.Get(allowance, "LastWindow"));
            Assert.True(Act(first, Row(first, bottle).Command));
            var empty = Flask(0); Assert.IsEmpty(Rows(second)); Assert.AreEqual(0, Units(empty)); Assert.AreEqual(3, Spent);
        }
        [TestCase("missing")] [TestCase("moved")] [TestCase("replaced-ground")]
        [TestCase("private")] [TestCase("dirty")] [TestCase("unknown-policy")]
        [TestCase("duplicate-anchor")] [TestCase("interior")]
        public void InvalidAuthorityDoesNotOfferOrGrantWater(string condition)
        {
            var bottle = Flask(0); string command = Row(first, bottle).Command;
            if (condition == "missing") Zone.RemoveEntity(anchor);
            if (condition == "moved") { Zone.RemoveEntity(anchor); Zone.AddEntity(anchor, 10, 8); }
            if (condition == "replaced-ground") { var ground = Terrain(10, 9); Zone.RemoveEntity(ground); Place("Grass", 10, 9); }
            if (condition == "private") WeatherTestApi.Set(allowance, "Public", false);
            if (condition == "dirty") WeatherTestApi.Set(firstPart, "Clean", false);
            if (condition == "unknown-policy") WeatherTestApi.Set(allowance, "PolicyVersion", 0);
            if (condition == "duplicate-anchor")
            {
                var duplicate = Place("Signpost", 12, 9); var copy = WeatherTestApi.Part("RainCatchmentPart"); duplicate.AddPart(copy);
                foreach (var f in allowance.GetType().GetFields()) if (f.Name != "ParentEntity" && !f.IsStatic) f.SetValue(copy, f.GetValue(allowance));
            }
            if (condition == "interior") Zone.GetCell(11, 10).IsInterior = true;
            Assert.IsEmpty(Rows(first)); Assert.False(Act(first, command)); Assert.AreEqual(0, Units(bottle)); Assert.AreEqual(0, Spent);
        }
        [TestCase("oil")] [TestCase("full")] [TestCase("foreign")] [TestCase("duplicate-part")]
        public void IneligibleReceiversStayUnchanged(string kind)
        {
            var bottle = Flask(kind == "oil" ? 1 : kind == "full" ? 12 : 0, kind == "oil" ? "oil" : "water");
            if (kind == "foreign") { Pack.RemoveObject(bottle); Zone.AddEntity(bottle, 10, 10); }
            if (kind == "duplicate-part") bottle.AddPart(new LiquidVesselPart());
            Assert.IsEmpty(Rows(first)); Assert.AreEqual(0, Spent);
        }
        [Test] public void ANewWindowCannotRedeemAnOldQuotedCommandOrBankMissedRain()
        {
            var bottle = Flask(0); string command = Row(first, bottle).Command;
            int before = Clock.TickCount; Clock.AdvanceClock(600 * 64 * 1000);
            Assert.False(Act(first, command)); Assert.AreEqual(0, Spent); Assert.AreEqual(0, Units(bottle));
            Assert.True(Act(first, Row(first, bottle).Command)); Assert.AreEqual(3, Units(bottle));
            Assert.AreEqual(before + 600 * 64 * 1000, Clock.TickCount);
        }
        [Test] public void RemovingAndReplacingACollectorCannotRenewTheAnchorBudget()
        {
            var bottle = Flask(0, capacity: 1); Assert.True(Act(first, Row(first, bottle).Command));
            Zone.RemoveEntity(first); first = Collector(11, 10, out firstPart);
            var next = Flask(0); Assert.True(Act(first, Row(first, next).Command)); Assert.AreEqual(2, Units(next)); Assert.AreEqual(3, Spent);
        }
        [Test] public void CollectedWaterFeedsTheExistingOrdinaryCropAction()
        {
            var bottle = Flask(0); Assert.True(Act(first, Row(first, bottle).Command));
            var crop = Crop(x: 11, y: 11); Assert.True(Act(crop, "WaterCrop|" + Uri.EscapeDataString(bottle.ID)));
            Assert.AreEqual(2, Units(bottle)); Assert.AreEqual(40, crop.GetPart<CropPart>().MoistureTicks); Assert.AreEqual(3, Spent);
        }
        [Test] public void LateInvalidationReturnsOwnedWaterButKeepsIndependentInjury()
        {
            var bottle = Flask(0); string command = Row(first, bottle).Command; int hp = Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () => { Actor.GetStat("Hitpoints").BaseValue -= 7; WeatherTestApi.Set(allowance, "Public", false); };
            Assert.False(Act(first, command)); Assert.AreEqual(hp - 7, Actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, Units(bottle)); Assert.AreEqual(0, Spent);
        }
        [Test] public void LateIndependentWaterAdditionSurvivesRefusal()
        {
            var bottle = Flask(0); string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () => { bottle.GetPart<LiquidVesselPart>().Volume++; WeatherTestApi.Set(allowance, "Public", false); };
            Assert.False(Act(first, command)); Assert.AreEqual(1, Units(bottle));
            Assert.AreEqual("water", bottle.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(0, Spent);
        }
        [Test] public void LateIndependentRemovalOfPreviouslyHeldWaterIsNotRestored()
        {
            var bottle = Flask(2); string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            bool ran = false;
            RainCollectionCallback.After = () => { ran = true; bottle.GetPart<LiquidVesselPart>().Volume--; WeatherTestApi.Set(allowance, "Public", false); };
            Assert.False(Act(first, command)); Assert.True(ran); Assert.AreEqual(1, Units(bottle)); Assert.AreEqual(0, Spent);
        }
        [Test] public void IndependentRemovalToZeroKeepsTheFlaskCanonicalEmpty()
        {
            var bottle = Flask(1); string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () => { bottle.GetPart<LiquidVesselPart>().Volume--; WeatherTestApi.Set(allowance, "Public", false); };
            Assert.False(Act(first, command)); Assert.AreEqual(0, Units(bottle));
            Assert.AreEqual("", bottle.GetPart<LiquidVesselPart>().LiquidId);
        }
        [TestCase("flask")] [TestCase("skin")]
        public void MalformedNegativeCallbackQuantityNeverWrapsIntoWater(string kind)
        {
            var bottle = kind == "skin" ? Skin(0) : Flask(0); string command = Row(first, bottle).Command;
            Actor.AddPart(new RainCollectionCallback()); RainCollectionCallback.After = () =>
            {
                if (kind == "skin") bottle.GetPart<WaterskinPart>().Charges = int.MinValue;
                else bottle.GetPart<LiquidVesselPart>().Volume = int.MinValue;
            };
            Assert.False(Act(first, command)); Assert.AreEqual(0, Units(bottle)); Assert.AreEqual(0, Spent);
        }
        [Test] public void IndependentlyReplacedLiquidIsNotRewrittenDuringRefusal()
        {
            var bottle = Flask(1); string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () => { bottle.GetPart<LiquidVesselPart>().LiquidId = "oil"; };
            Assert.False(Act(first, command)); Assert.AreEqual("oil", bottle.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(4, Units(bottle), "The callback's foreign contents are outside this receipt's reversible water quantity.");
        }
        [Test] public void RehomedVesselPartIsNotMutatedByTheOldOwnersReceipt()
        {
            var bottle = Flask(0); var other = Flask(0); var captured = bottle.GetPart<LiquidVesselPart>();
            string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () =>
            {
                bottle.RemovePart(captured); other.RemovePart(other.GetPart<LiquidVesselPart>()); other.AddPart(captured);
            };
            Assert.False(Act(first, command)); Assert.AreSame(other, captured.ParentEntity); Assert.AreEqual(3, captured.Volume);
        }
        [Test] public void IndependentAllowanceRefundSurvivesTheFailedDraw()
        {
            var small = Flask(0, capacity: 1); Assert.True(Act(first, Row(first, small).Command));
            var bottle = Flask(0); string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            bool ran = false;
            RainCollectionCallback.After = () => { ran = true; WeatherTestApi.Set(allowance, "Spent", Spent - 1); WeatherTestApi.Set(allowance, "Public", false); };
            Assert.False(Act(first, command)); Assert.True(ran); Assert.AreEqual(0, Units(bottle)); Assert.AreEqual(0, Spent);
        }
        [Test] public void RehomedAllowancePartRetainsItsIndependentState()
        {
            var bottle = Flask(0); string command = Row(first, bottle).Command; var other = Place("Signpost", 13, 10);
            Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () => { anchor.RemovePart(allowance); other.AddPart(allowance); };
            Assert.False(Act(first, command)); Assert.AreSame(other, allowance.ParentEntity); Assert.AreEqual(3, Spent);
            Assert.AreEqual(0, Units(bottle));
        }
        [Test] public void LateForcedDropKeepsTheVesselOnTheGround()
        {
            var bottle = Flask(0); string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () => { Assert.True(Pack.RemoveObject(bottle)); Assert.True(Zone.AddEntity(bottle, 10, 10)); };
            Assert.False(Act(first, command)); Assert.False(Pack.Objects.Contains(bottle));
            Assert.AreSame(Zone, Zone.GetEntityCell(bottle).ParentZone); Assert.AreEqual(0, Units(bottle)); Assert.AreEqual(0, Spent);
        }
        [Test] public void AReentrantCollectionCannotSpendTheSameAllowanceTwice()
        {
            var bottle = Flask(0); var next = Flask(0); string outer = Row(first, bottle).Command;
            string inner = Row(second, next).Command; Actor.AddPart(new RainCollectionCallback());
            bool attempted = false, nested = true;
            RainCollectionCallback.After = () => { attempted = true; RainCollectionCallback.After = null; nested = Act(second, inner); };
            Assert.True(Act(first, outer)); Assert.True(attempted); Assert.False(nested);
            Assert.AreEqual(3, Units(bottle)); Assert.AreEqual(0, Units(next)); Assert.AreEqual(3, Spent);
        }
        [TestCase("capacity")] [TestCase("identity")] [TestCase("receiver-part")] [TestCase("stack")]
        public void LateReceiverChangesRefuseTheQuotedAction(string condition)
        {
            var bottle = Flask(0); string command = Row(first, bottle).Command; Actor.AddPart(new RainCollectionCallback());
            RainCollectionCallback.After = () =>
            {
                if (condition == "capacity") bottle.GetPart<LiquidVesselPart>().Capacity = 20;
                if (condition == "identity") bottle.ID += "-changed";
                if (condition == "receiver-part") { bottle.RemovePart(bottle.GetPart<LiquidVesselPart>()); bottle.AddPart(new LiquidVesselPart()); }
                if (condition == "stack") bottle.AddPart(new StackerPart { StackCount = 2, MaxStack = 10 });
            };
            Assert.False(Act(first, command)); Assert.AreEqual(0, Spent);
        }
        [Test] public void ExactAnchorAndGroundReferencesSurviveTheProductionGraphRoundTrip()
        {
            var bottle = Flask(0, capacity: 1); Assert.True(Act(first, Row(first, bottle).Command));
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(first);
            var copiedCollector = copy.Parts.Single(p => p.GetType().Name == "RainCollectorPart");
            var copiedAnchor = (Entity)WeatherTestApi.Get(copiedCollector, "Anchor");
            var saved = copiedAnchor.Parts.Single(p => p.GetType().Name == "RainCatchmentPart");
            Assert.AreEqual(anchor.ID, copiedAnchor.ID); Assert.AreEqual(1, WeatherTestApi.Get(saved, "Spent"));
            Assert.AreEqual(((Entity)WeatherTestApi.Get(allowance, "Ground")).ID, ((Entity)WeatherTestApi.Get(saved, "Ground")).ID);
        }
    }

    public sealed class RainCollectionSiteTests : FiftyWorldFixture
    {
        static bool Has(Entity e, string part) => e.Parts.Any(p => p.GetType().Name == part);
        [TearDown] public void ClearFactoryHook() { RainSiteFactoryProbe.Created = null; }
        OverworldZoneManager EmptyAllotment()
        {
            Zone = new Zone("Overworld.2.6.0");
            for (int x = 6; x <= 14; x++) for (int y = 6; y <= 14; y++) Place("Grass", x, y);
            var notice = Place("Signpost", 10, 10); notice.ID = "repair-cultivation:allotment-notice";
            notice.Properties[RepairCultivationSite.RoleKey] = "notice";
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            Factory.RegisterPartType<RainSiteFactoryProbe>("RainSiteFactoryProbe");
            Factory.Blueprints["CampAwning"].Parts["RainSiteFactoryProbe"] = new Dictionary<string, string>();
            return manager;
        }
        [Test] public void FactoryReentryIsRefusedBeforeCreatingAnotherFrame()
        {
            var manager = EmptyAllotment(); bool attempted = false, nested = true;
            RainSiteFactoryProbe.Created = _ => { if (attempted) return; attempted = true; nested = ShowerCollectionSite.TryInstall(Zone, manager); };
            Assert.True(ShowerCollectionSite.TryInstall(Zone, manager)); Assert.True(attempted); Assert.False(nested);
            Assert.AreEqual(2, Zone.GetReadOnlyEntities().Count(e => Has(e, "RainCollectorPart")));
        }
        [TestCase("anchor")] [TestCase("clean")]
        public void SecondFactoryCannotChangeThePreviouslyPreparedCollector(string field)
        {
            var manager = EmptyAllotment(); Entity first = null; int calls = 0;
            RainSiteFactoryProbe.Created = e =>
            {
                calls++; if (first == null) { first = e; return; }
                var part = first.GetPart<RainCollectorPart>();
                if (field == "anchor") part.Anchor = null; else part.Clean = false;
            };
            Assert.False(ShowerCollectionSite.TryInstall(Zone, manager)); Assert.AreEqual(2, calls);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => Has(e, "RainCatchmentPart") || Has(e, "RainCollectorPart")));
        }
        [Test] public void FreshPublicAllotmentAddsTwoFramesWithOneExactNoticeAuthority()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            var zone = manager.GetZone("Overworld.2.6.0");
            var frames = zone.GetReadOnlyEntities().Where(e => Has(e, "RainCollectorPart")).ToArray();
            Assert.AreEqual(2, frames.Length, "A fresh ordinary allotment needs the optional shared collection opportunity.");
            var root = frames[0].Parts.Single(p => p.GetType().Name == "RainCollectorPart");
            var anchor = (Entity)WeatherTestApi.Get(root, "Anchor");
            Assert.AreSame(anchor, WeatherTestApi.Get(frames[1].Parts.Single(p => p.GetType().Name == "RainCollectorPart"), "Anchor"));
            Assert.AreEqual("repair-cultivation:allotment-notice", anchor.ID);
            Assert.AreEqual(1, anchor.Parts.Count(p => p.GetType().Name == "RainCatchmentPart"));
            Assert.AreEqual(6, zone.GetReadOnlyEntities().Count(e => e.HasPart<CropPart>() && e.GetProperty(RepairCultivationSite.RoleKey) != null));
            foreach (var frame in frames) { Assert.False(frame.GetPart<PhysicsPart>().Takeable); Assert.False(frame.HasPart<PackableFixturePart>()); }
        }
        [Test] public void LoadingAnOldCachedAllotmentDoesNotRetrofitWeatherOwners()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            var cached = new Zone("Overworld.2.6.0"); var notice = Factory.CreateEntity("Signpost");
            notice.ID = "repair-cultivation:allotment-notice"; cached.AddEntity(notice, 4, 4);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { cached.ZoneID, cached } }, cached.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            Assert.AreSame(cached, manager.GetZone(cached.ZoneID));
            Assert.False(cached.GetReadOnlyEntities().Any(e => Has(e, "RainCatchmentPart") || Has(e, "RainCollectorPart")));
        }
    }

    public sealed class RainSiteFactoryProbe : Part
    {
        public static Action<Entity> Created;
        public override void Initialize() => Created?.Invoke(ParentEntity);
    }

    public sealed class RainCollectionCallback : Part
    {
        public static Action After;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "AfterInventoryAction" && e.GetStringParameter("Command")?.StartsWith("CollectRain|", StringComparison.Ordinal) == true) After?.Invoke();
            return true;
        }
    }
}
