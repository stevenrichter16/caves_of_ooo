using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
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

    public sealed class SeasonalSeedFrameTests : FiftyWorldFixture
    {
        const string Patch = "LateKnotflaxStand";
        const string Bundle = "KnotflaxSeedHeads";
        const string Chaff = "DryKnotflaxChaff";
        const string Work = "MaterialProcess|winnow_knotflax_seed|";
        const int LateGrowth = 16 * 600;
        EntityFactory oldForge;

        [SetUp] public void BindForge() { oldForge = ForgePart.Factory; ForgePart.Factory = Factory; }
        [TearDown] public void RestoreForge() { ForgePart.Factory = oldForge; }

        OverworldZoneManager EnterAllotment(bool missingProduct = false)
        {
            if (missingProduct) Factory.Blueprints.Remove(Bundle);
            Assert.True(Zone.RemoveEntity(Actor));
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            Zone = manager.GetZone("Overworld.2.6.0");
            manager.SetActiveZone(Zone); SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, 40, 12));
            Zone.ForEachCell((c, x, y) => { c.IsVisible = true; c.Explored = true; });
            return manager;
        }

        void StandBeside(Entity target)
        {
            var at = Zone.GetEntityCell(target);
            var next = new[] { (at.X, at.Y + 1), (at.X - 1, at.Y), (at.X + 1, at.Y), (at.X, at.Y - 1) }
                .First(p => Zone.GetCell(p.Item1, p.Item2)?.BlocksMovement() == false);
            Assert.True(Zone.MoveEntity(Actor, next.Item1, next.Item2));
        }

        int FirstFairWindowBeforeRain()
        {
            return Enumerable.Range(0, RegionalWeather.CycleWindows).First(window =>
                !RegionalWeather.Current(Zone, window * RegionalWeather.WindowTicks, RegionalWeather.PolicyVersion).Raining
                && RegionalWeather.Current(Zone, (window + 1) * RegionalWeather.WindowTicks, RegionalWeather.PolicyVersion).Raining);
        }

        static bool CordHarvestRow(InventoryAction row)
        {
            var text = row.Display + " " + row.Hint;
            return !row.IsDisabled && text.IndexOf("cord", StringComparison.OrdinalIgnoreCase) >= 0
                && (text.IndexOf("stem", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("cut", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("trim", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Test] public void EarlyKnotflaxOffersOneCordWithARealCutterAndSpendsTheStand()
        {
            var manager = EnterAllotment();
            var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Patch);
            string sourceId = source.ID;
            StandBeside(source);
            var cell = Zone.GetEntityCell(source);
            Assert.That(RegionalWeather.Current(Zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion).Season, Is.Zero);

            Carry("Spear");
            Assert.False(CellMenu(cell.X, cell.Y).Any(CordHarvestRow));
            var cutter = Carry("Dagger");
            var cutterState = cutter.GetPart<CuttingToolPart>();
            int tick = Clock.TickCount;
            var menu = CellMenu(cell.X, cell.Y);
            var harvest = menu.Single(CordHarvestRow);
            Assert.False(menu.Any(a => a.Command == "Harvest"));
            Assert.That(Clock.TickCount, Is.EqualTo(tick));
            Assert.False(source.GetPart<HarvestablePart>().Harvested);

            Assert.True(ChooseRow(harvest, cell.X, cell.Y));
            Assert.True(source.GetPart<HarvestablePart>().Harvested);
            Assert.Null(Zone.GetEntityCell(source));
            Assert.False(Act(source, harvest.Command));
            Assert.That(Count(Bundle), Is.Zero);
            Assert.That(Count("KnotflaxCord"), Is.EqualTo(1));
            Assert.That(Pack.Objects, Does.Contain(cutter));
            Assert.That(cutter.GetPart<CuttingToolPart>(), Is.SameAs(cutterState));
            Assert.False(cutter.HasEffect<BrokenEffect>());
            Assert.That(cutter.GetPart<StackerPart>()?.StackCount ?? 1, Is.EqualTo(1));

            var cord = Pack.Objects.Single(e => e.BlueprintName == "KnotflaxCord");
            Assert.True(Act(cord, Choice(cord, "PrepareRecipe|bandage")));
            Assert.That(Count("KnotflaxCord"), Is.Zero);
            Assert.That(Count("KnotflaxBandage"), Is.EqualTo(1));

            Clock.AdvanceClock(LateGrowth - Clock.TickCount);
            var loaded = HotbarSaveFixture.RoundTrip(
                GameSessionState.Capture("early-knotflax", "spent before seed", manager, Clock, Actor));
            var returned = loaded.ZoneManager.GetZone(Zone.ZoneID);
            Assert.That(loaded.TurnManager.TickCount, Is.EqualTo(LateGrowth));
            Assert.False(returned.GetReadOnlyEntities().Any(e => e.ID == sourceId || e.BlueprintName == Patch || e.BlueprintName == Bundle));
            Assert.That(loaded.Player.GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == "KnotflaxBandage"), Is.EqualTo(1));
        }

        [Test] public void AnEarlyCordOfferCannotSpendTheStandAfterPhaseChanges()
        {
            EnterAllotment();
            var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Patch);
            StandBeside(source);
            var cell = Zone.GetEntityCell(source);
            Carry("Dagger");
            Clock.AdvanceClock(LateGrowth - 1);
            var early = CellMenu(cell.X, cell.Y).Single(CordHarvestRow);
            Clock.AdvanceClock(1);

            Assert.False(ChooseRow(early, cell.X, cell.Y));
            Assert.That(Count("KnotflaxCord"), Is.Zero);
            Assert.False(source.GetPart<HarvestablePart>().Harvested);
            Assert.That(Zone.GetEntityCell(source), Is.SameAs(cell));
            var menu = CellMenu(cell.X, cell.Y);
            var late = menu.Single(a => a.Command == "Harvest");
            Assert.That(late.Display, Is.EqualTo("gather ripe seed heads (1 bundle)"));
            Assert.False(menu.Any(CordHarvestRow));
            Assert.True(ChooseRow(late, cell.X, cell.Y));
            Assert.That(Count(Bundle), Is.EqualTo(1));
            Assert.Null(Zone.GetEntityCell(source));

            var frame = Zone.GetReadOnlyEntities().First(e => e.HasPart<SeedFramePart>());
            StandBeside(frame);
            var frameCell = Zone.GetEntityCell(frame);
            var work = CellMenu(frameCell.X, frameCell.Y).Single(a => a.Command.StartsWith(Work, StringComparison.Ordinal));
            Assert.That(work.Command.Split('|')[2], Is.EqualTo(Uri.EscapeDataString(frame.ID)));
            Assert.True(ChooseRow(work, frameCell.X, frameCell.Y));
            Assert.That(Count(Bundle), Is.Zero);
            Assert.That(Count("KnotflaxSeed"), Is.EqualTo(1));
        }

        [Test] public void AnUnsafeCordTemplateCannotPublishOutputOrSpendTheSeasonalStand()
        {
            EnterAllotment();
            var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Patch);
            StandBeside(source);
            var cell = Zone.GetEntityCell(source);
            Carry("Dagger");
            var quote = CellMenu(cell.X, cell.Y).Single(CordHarvestRow);
            var template = Factory.Blueprints["KnotflaxCord"];
            Factory.RegisterPartType<FiftyWorldCreationProbe>();
            template.Parts.Add(nameof(FiftyWorldCreationProbe), new Dictionary<string, string>());
            int callbacks = 0;
            FiftyWorldCreationProbe.Created = cord => { callbacks++; Pack.AddObject(cord); };
            try
            {
                Assert.False(ChooseRow(quote, cell.X, cell.Y));
                Assert.That(callbacks, Is.Zero);
                Assert.That(Count("KnotflaxCord"), Is.Zero);
                Assert.False(CellMenu(cell.X, cell.Y).Any(CordHarvestRow));
                Assert.That(Zone.GetEntityCell(source), Is.SameAs(cell));
                Assert.False(source.GetPart<HarvestablePart>().Harvested);

                Clock.AdvanceClock(LateGrowth);
                Assert.True(ChooseRow(CellMenu(cell.X, cell.Y).Single(a => a.Command == "Harvest"), cell.X, cell.Y));
                Assert.That(Count(Bundle), Is.EqualTo(1));
            }
            finally
            {
                FiftyWorldCreationProbe.Created = null;
                template.Parts.Remove(nameof(FiftyWorldCreationProbe));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnavailableCutterReceiptLeavesTheExactEarlyStand(bool competingClaim)
        {
            EnterAllotment();
            var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Patch);
            StandBeside(source);
            var cell = Zone.GetEntityCell(source);
            var cutter = Carry("Dagger");
            var row = CellMenu(cell.X, cell.Y).Single(CordHarvestRow);
            int tick = Clock.TickCount;
            var independent = new CavesOfOoo.Core.Inventory.InventoryTransaction();
            try
            {
                if (competingClaim)
                {
                    var claim = typeof(CavesOfOoo.Core.Inventory.InventoryTransaction).GetMethod("TryClaim",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    Assert.True((bool)claim.Invoke(independent, new object[] { cutter, Actor, "seasonal-cutter-test" }));
                }
                else Assert.True(cutter.ApplyEffect(new BrokenEffect()));

                Assert.False(ChooseRow(row, cell.X, cell.Y));
                Assert.That(Clock.TickCount, Is.EqualTo(tick));
                Assert.That(Zone.GetEntityCell(source), Is.SameAs(cell));
                Assert.False(source.GetPart<HarvestablePart>().Harvested);
                Assert.That(Count("KnotflaxCord"), Is.Zero);
                Assert.That(Count(Bundle), Is.Zero);
                Assert.That(Pack.Objects, Does.Contain(cutter));
                Assert.That(cutter.HasEffect<BrokenEffect>(), Is.EqualTo(!competingClaim));
            }
            finally
            {
                independent.Rollback();
                if (!competingClaim) cutter.RemoveEffect<BrokenEffect>();
            }

            Assert.True(ChooseRow(CellMenu(cell.X, cell.Y).Single(CordHarvestRow), cell.X, cell.Y));
            Assert.That(Count("KnotflaxCord"), Is.EqualTo(1));
            Assert.Null(Zone.GetEntityCell(source));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void OnlyFairDryWorkRetainsOnePoundOfChaff(bool raining, bool wetGround)
        {
            EnterAllotment();
            int tick = (FirstFairWindowBeforeRain() + (raining ? 1 : 0)) * RegionalWeather.WindowTicks;
            Clock.AdvanceClock(tick);
            var weather = RegionalWeather.Current(Zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion);
            Assert.True(weather.Valid);
            Assert.That(weather.Raining, Is.EqualTo(raining));
            var frame = Zone.GetReadOnlyEntities().First(e => e.HasPart<RainCollectorPart>());
            StandBeside(frame);
            var cell = Zone.GetEntityCell(frame);
            if (wetGround) Zone.TileState.WriteCoating(cell.X, cell.Y, "water", ZoneTileState.Permanent);
            var packet = Carry(Bundle);

            bool retained = !raining && !wetGround;
            var row = CellMenu(cell.X, cell.Y).Single(a => a.Command.StartsWith(Work));
            Assert.That(row.Hint, Does.Contain(retained ? "retain dry chaff" : "discard dry chaff"));
            var fromPack = Actions(packet).Single(a => a.Command.StartsWith(Work));
            Assert.That(fromPack.Command, Is.EqualTo(row.Command));
            Assert.That(fromPack.Hint, Is.EqualTo(row.Hint));
            Assert.That(WorldClock.CurrentTick, Is.EqualTo(tick));
            Assert.True(ChooseRow(row, cell.X, cell.Y));
            Assert.That(Count(Bundle), Is.Zero);
            Assert.That(Count("KnotflaxSeed"), Is.EqualTo(1));
            Assert.That(Count(Chaff), Is.EqualTo(retained ? 1 : 0));
            if (retained)
            {
                var chaff = Pack.Objects.Single(e => e.BlueprintName == Chaff);
                Assert.That(chaff.GetPart<PhysicsPart>().Weight, Is.EqualTo(1));
                Assert.That(chaff.GetPart<ReagentPart>().PropertiesRaw, Is.EqualTo("combustible:1"));
            }
            Assert.That(frame.GetPart<RainCollectorPart>().Anchor.GetPart<RainCatchmentPart>().Spent, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AQuotedChaffBonusExpiresWhenRainStartsBeforePayment(bool duringCallback)
        {
            EnterAllotment();
            int rainOpens = (FirstFairWindowBeforeRain() + 1) * RegionalWeather.WindowTicks;
            Clock.AdvanceClock(rainOpens - 1);
            var frame = Zone.GetReadOnlyEntities().First(e => e.HasPart<RainCollectorPart>());
            StandBeside(frame);
            Carry(Bundle);
            var cell = Zone.GetEntityCell(frame);
            var dryRow = CellMenu(cell.X, cell.Y).Single(a => a.Command.StartsWith(Work));
            var probe = new MachineMaterialProcessActionTests.BeforeProcessProbe { Callback = () => Clock.AdvanceClock(1) };
            if (duringCallback) Actor.AddPart(probe);
            else Clock.AdvanceClock(1);

            Assert.False(ChooseRow(dryRow, cell.X, cell.Y));
            if (duringCallback) Actor.RemovePart(probe);
            Assert.That(Count(Bundle), Is.EqualTo(1));
            Assert.That(Count("KnotflaxSeed"), Is.Zero);
            Assert.That(Count(Chaff), Is.Zero);
            var wetRow = CellMenu(cell.X, cell.Y).Single(a => a.Command.StartsWith(Work));
            Assert.True(ChooseRow(wetRow, cell.X, cell.Y));
            Assert.That(Count(Bundle), Is.Zero);
            Assert.That(Count("KnotflaxSeed"), Is.EqualTo(1));
            Assert.That(Count(Chaff), Is.Zero);
        }

        [TestCase("missing")]
        [TestCase("weight")]
        [TestCase("reagent")]
        [TestCase("carried")]
        [TestCase("seed weight")]
        public void UnavailableBonusLeavesTheOrdinarySeedWork(string reason)
        {
            EnterAllotment();
            Clock.AdvanceClock(FirstFairWindowBeforeRain() * RegionalWeather.WindowTicks);
            Entity altered = null;
            if (reason == "missing") Factory.Blueprints.Remove(Chaff);
            if (reason == "weight") Factory.Blueprints[Chaff].Parts["Physics"]["Weight"] = "0";
            if (reason == "reagent") Factory.Blueprints[Chaff].Parts["Reagent"]["PropertiesRaw"] = "combustible:2";
            if (reason == "seed weight") Factory.Blueprints["KnotflaxSeed"].Parts["Handling"] = new Dictionary<string, string> { ["Weight"] = "1" };
            if (reason == "carried")
            {
                altered = Carry(Chaff);
                altered.GetPart<ReagentPart>().PropertiesRaw = "combustible:2";
                var fresh = Factory.CreateEntity(Chaff);
                Assert.False(altered.GetPart<StackerPart>().CanStackWith(fresh));
            }

            var frame = Zone.GetReadOnlyEntities().First(e => e.HasPart<RainCollectorPart>());
            StandBeside(frame);
            var packet = Carry(Bundle);
            var row = Actions(packet).Single(a => a.Command.StartsWith(Work));
            Assert.That(row.Hint, Does.Contain("discard dry chaff"));
            Assert.True(Act(packet, row.Command));
            Assert.That(Count("KnotflaxSeed"), Is.EqualTo(1));
            Assert.That(Count(Chaff), Is.EqualTo(altered == null ? 0 : 1));
            if (altered != null) Assert.That(altered.GetPart<ReagentPart>().PropertiesRaw, Is.EqualTo("combustible:2"));
        }

        [Test] public void RetainedChaffKeepsItsIdentityOnLoadAndFeedsAnOrdinaryFire()
        {
            var manager = EnterAllotment();
            Clock.AdvanceClock(FirstFairWindowBeforeRain() * RegionalWeather.WindowTicks);
            var frame = Zone.GetReadOnlyEntities().First(e => e.HasPart<RainCollectorPart>());
            StandBeside(frame);
            for (int i = 0; i < 2; i++)
            {
                var packet = Carry(Bundle);
                Assert.True(Act(packet, Choice(packet, Work)));
            }
            var recovered = Pack.Objects.Single(e => e.BlueprintName == Chaff);
            Assert.That(recovered.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            var saved = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("fair-chaff", "two retained units", manager, Clock, Actor));
            var loadedChaff = saved.ZoneManager.GetZone(Zone.ZoneID).GetReadOnlyEntities().Single(e => e.ID == Actor.ID)
                .GetPart<InventoryPart>().Objects.Single(e => e.ID == recovered.ID);
            Assert.That(loadedChaff.GetPart<ReagentPart>().PropertiesRaw, Is.EqualTo("combustible:1"));
            Assert.That(loadedChaff.GetPart<StackerPart>().StackCount, Is.EqualTo(2));

            Assert.True(Zone.MoveEntity(Actor, 40, 12));
            var fire = Place("CampHearth", 41, 12);
            fire.GetPart<PhysicsPart>().Takeable = false;
            fire.GetPart<CampfirePart>().FiniteCooking = true;
            var fuel = fire.GetPart<FuelPart>();
            fuel.FuelMass = 0;
            float temperature = fire.GetPart<ThermalPart>().Temperature;
            var feed = CellMenu(41, 12).Single(a => a.Command.StartsWith("FeedReagentFuel|"));
            Assert.That(feed.Display, Does.Contain("dry knotflax chaff").And.Contain("+5 fuel"));
            Assert.True(ChooseRow(feed, 41, 12));
            Assert.That(fuel.FuelMass, Is.EqualTo(5));
            Assert.That(fire.GetPart<ThermalPart>().Temperature, Is.EqualTo(temperature));
            Assert.False(fire.GetPart<LightSourcePart>().Enabled);
            Assert.That(recovered.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
        }

        [Test] public void OneActuallyGeneratedSeasonalBundleCanBecomeAPlantedSeedAfterTheSeason()
        {
            var manager = EnterAllotment();
            var sources = Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == Patch).ToArray();
            Assert.That(sources, Has.Length.EqualTo(1));
            var source = sources[0]; StandBeside(source);
            Assert.False(Actions(source).Any(a => a.Command == "Harvest"));
            Assert.False(Act(source, "Harvest"));
            Assert.That(Count(Bundle), Is.Zero);
            Clock.AdvanceClock(LateGrowth);
            Assert.That(Actions(source).Single(a => a.Command == "Harvest").Display, Is.EqualTo("gather ripe seed heads (1 bundle)"));
            Assert.True(Act(source, "Harvest"));
            Assert.False(Act(source, "Harvest"));
            Assert.That(Count(Bundle), Is.EqualTo(1));
            Assert.That(Count("KnotflaxSeed"), Is.Zero);
            var packet = Pack.Objects.Single(e => e.BlueprintName == Bundle);
            Clock.AdvanceClock(LateGrowth);
            for (int i = 0; i < RegionalWeather.CycleWindows; i++)
            {
                if (RegionalWeather.Current(Zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion).Raining) break;
                Clock.AdvanceClock(RegionalWeather.WindowTicks);
            }
            Assert.True(RegionalWeather.Current(Zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion).Raining);
            var frame = Zone.GetReadOnlyEntities().First(e => e.HasPart<RainCollectorPart>());
            StandBeside(frame);
            var cell = Zone.GetEntityCell(frame);
            var row = CellMenu(cell.X, cell.Y).Single(a => a.Command.StartsWith(Work));
            Assert.That(row.Via, Is.SameAs(packet));
            Assert.That(row.Display, Does.Contain("1 knotflax seed-head bundle"));
            Assert.That(row.Hint, Does.Contain("discard dry chaff").And.Contain("One action"));
            Assert.True(ChooseRow(row, cell.X, cell.Y));
            Assert.False(ChooseRow(row, cell.X, cell.Y));
            Assert.That(Count(Bundle), Is.Zero);
            var seed = Pack.Objects.Single(e => e.BlueprintName == "KnotflaxSeed");
            Assert.That(seed.GetPart<SeedPart>().CropBlueprint, Is.EqualTo("KnotflaxCrop"));
            Assert.True(seed.GetPart<SeedPart>().RequireCultivatedSoil);
            Assert.That(frame.GetPart<RainCollectorPart>().Anchor.GetPart<RainCatchmentPart>().Spent, Is.Zero);

            var empty = Zone.GetCell(40, 12);
            var terrain = empty.Objects.Single(e => e.HasTag("Terrain"));
            terrain.SetTag("Plantable"); terrain.AddPart(new CultivatedSoilPart());
            Assert.True(Zone.MoveEntity(Actor, empty.X, empty.Y));
            Assert.True(Act(seed, "PlantSeed"));
            Assert.That(empty.Objects.Count(e => e.BlueprintName == "KnotflaxCrop"), Is.EqualTo(1));

            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("late-knotflax", "spent stand", manager, Clock, Actor));
            var returned = loaded.ZoneManager.GetZone(Zone.ZoneID);
            Assert.False(returned.GetReadOnlyEntities().Any(e => e.BlueprintName == Patch || e.BlueprintName == Bundle));
            Assert.That(returned.GetCell(empty.X, empty.Y).Objects.Count(e => e.BlueprintName == "KnotflaxCrop"), Is.EqualTo(1));
        }

        [Test] public void BothOriginalRainFramesCanOfferTheNewWorkWithoutBorrowingRainAllowance()
        {
            EnterAllotment();
            Assert.True(Factory.Blueprints.ContainsKey(Bundle));
            var packet = Carry(Bundle);
            var frames = Zone.GetReadOnlyEntities().Where(e => e.HasPart<RainCollectorPart>()).ToArray();
            Assert.That(frames, Has.Length.EqualTo(2));
            foreach (var frame in frames)
            {
                StandBeside(frame);
                var cell = Zone.GetEntityCell(frame);
                var row = CellMenu(cell.X, cell.Y).Single(a => a.Command.StartsWith(Work));
                Assert.That(row.Command.Split('|')[2], Is.EqualTo(Uri.EscapeDataString(frame.ID)));
                Assert.That(row.Via, Is.SameAs(packet));
            }
            Assert.That(Count(Bundle), Is.EqualTo(1));
            Assert.That(Count("KnotflaxSeed"), Is.Zero);
            Assert.That(frames[0].GetPart<RainCollectorPart>().Anchor.GetPart<RainCatchmentPart>().Spent, Is.Zero);
        }

        [Test] public void MissingBundleLeavesPreviouslyWorkingBedsAndRainFrames()
        {
            EnterAllotment(true);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == Patch));
            Assert.That(Zone.GetReadOnlyEntities().Count(e => e.HasPart<RainCollectorPart>()), Is.EqualTo(2));
            Assert.That(Zone.GetReadOnlyEntities().Count(e => e.HasPart<CropPart>() && e.GetProperty(RepairCultivationSite.RoleKey) != null), Is.EqualTo(6));
            Carry("HandSieve");
            var stock = Carry("DryClaySpoil");
            Assert.That(Actions(stock).Count(a => a.Command.StartsWith("MaterialProcess|screen_size_grades|")), Is.EqualTo(1));
        }

        [Test] public void AReadyStandCannotBeGatheredByAStaleOutOfSeasonAction()
        {
            EnterAllotment();
            var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Patch);
            StandBeside(source);
            Clock.AdvanceClock(2 * LateGrowth - 1);
            string action = Choice(source, "Harvest");
            Clock.AdvanceClock(1);
            Assert.False(Act(source, action));
            Assert.That(Count(Bundle), Is.Zero);
            Assert.That(Zone.GetEntityCell(source), Is.Not.Null);
        }

        [Test] public void SelectedFrameLossDoesNotSwitchTheQuoteToTheOtherFrame()
        {
            EnterAllotment();
            var packet = Carry(Bundle);
            var frames = Zone.GetReadOnlyEntities().Where(e => e.HasPart<RainCollectorPart>()).ToArray();
            StandBeside(frames[0]);
            string command = Choice(packet, Work);
            Assert.True(Zone.RemoveEntity(frames[0]));
            Assert.False(Act(packet, command));
            Assert.That(Pack.Objects, Does.Contain(packet));
            Assert.That(Count("KnotflaxSeed"), Is.Zero);
            StandBeside(frames[1]);
            Assert.True(Act(packet, Choice(packet, Work)));
            Assert.That(Count("KnotflaxSeed"), Is.EqualTo(1));
        }

        [Test] public void AStunnedActorCannotGatherThroughDirectDispatch()
        {
            EnterAllotment();
            var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Patch);
            StandBeside(source);
            Clock.AdvanceClock(LateGrowth);
            Actor.ApplyEffect(new StunnedEffect { Duration = 3 });
            Assert.False(Actions(source).Any(a => a.Command == "Harvest"));
            Assert.False(Act(source, "Harvest"));
            Assert.That(Count(Bundle), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FatalNotificationCannotDuplicateTheSourceOrReturnStockToADeadPack(bool processing)
        {
            EnterAllotment();
            var target = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Patch);
            string command = "Harvest";
            if (processing)
            {
                StandBeside(Zone.GetReadOnlyEntities().First(e => e.HasPart<RainCollectorPart>()));
                target = Carry(Bundle);
                command = Choice(target, Work);
            }
            else
            {
                StandBeside(target);
                Clock.AdvanceClock(LateGrowth);
            }
            Actor.AddPart(new SeasonalFatalAfterPart { Zone = Zone });
            Assert.True(Act(target, command));
            Assert.True(CombatSystem.IsDeathHandled(Actor));
            Assert.That(Pack.Objects.Where(e => e.BlueprintName == Bundle || e.BlueprintName == "KnotflaxSeed"), Is.Empty);
            string product = processing ? "KnotflaxSeed" : Bundle;
            Assert.That(Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == product), Is.EqualTo(1));
            if (processing) Assert.That(Count(Bundle), Is.Zero);
            else Assert.Null(Zone.GetEntityCell(target));
        }
    }

    public sealed class SeasonalFatalAfterPart : Part
    {
        public Zone Zone;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "AfterInventoryAction") return true;
            CombatSystem.HandleDeath(ParentEntity, null, Zone);
            throw new InvalidOperationException("seasonal committed notification probe");
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
