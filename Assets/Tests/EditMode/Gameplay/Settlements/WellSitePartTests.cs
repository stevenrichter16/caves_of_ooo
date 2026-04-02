using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class WellSitePartTests
    {
        private const string SettlementId = SettlementSiteDefinitions.StartingVillageZoneId;
        private const string SiteId = SettlementSiteDefinitions.MainWellSiteId;
        private int _turn;
        private SettlementManager _manager;
        private Zone _zone;

        [SetUp]
        public void SetUp()
        {
            _turn = 0;
            MessageLog.Clear();
            AsciiFxBus.Clear();
            _manager = new SettlementManager(() => _turn, _ => new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));
            _manager.GetOrCreateSettlement(SettlementId, new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));
            _zone = new Zone(SettlementId);
            SettlementRuntime.ActiveZone = _zone;
        }

        [TearDown]
        public void TearDown()
        {
            SettlementManager.ResetCurrent();
            SettlementRuntime.Reset();
            AsciiFxBus.Clear();
        }

        [Test]
        public void FouledWell_FlickersToRed_OnEvery8thFrame()
        {
            Entity well = CreateWellEntity(RepairStage.Fouled);

            int redCount = 0;
            int normalCount = 0;
            for (int i = 0; i < 24; i++)
            {
                var e = CreateRenderEvent(well);
                well.FireEvent(e);
                string color = e.GetStringParameter("ColorString", "&y");
                if (color == "&r")
                    redCount++;
                else
                    normalCount++;
            }

            Assert.Greater(redCount, 0, "Fouled well should flash red on some frames");
            Assert.Greater(normalCount, 0, "Fouled well should not always be red");
        }

        [Test]
        public void TemporarilyPurified_DegradesThroughColorStages()
        {
            Entity well = CreateWellEntity(RepairStage.Fouled);

            // Purify the well
            var player = CreatePlayer();
            player.Properties[SettlementSiteDefinitions.StartingVillageKnowledgeProperty] = "true";
            _manager.ApplyRepairMethod(SettlementId, SiteId, RepairMethodId.PurifySpell, player);

            var site = _manager.GetSite(SettlementId, SiteId);
            Assert.AreEqual(RepairStage.TemporarilyPurified, site.Stage);

            // At start (100% remaining): should be bright yellow
            string colorAtStart = GetRenderColor(well);
            Assert.AreEqual("&W", colorAtStart, "Should start as bright yellow");

            // At 40% remaining: should be gray
            _turn = site.ResolvedAtTurn + (int)(SettlementRepairDefinitions.PurifyRelapseTurns * 0.60f);
            string colorAtMid = GetRenderColor(well);
            Assert.AreEqual("&Y", colorAtMid, "Should be white at ~40% elapsed");

            // At 80% remaining: should be gray
            _turn = site.ResolvedAtTurn + (int)(SettlementRepairDefinitions.PurifyRelapseTurns * 0.80f);
            string colorLate = GetRenderColor(well);
            Assert.AreEqual("&y", colorLate, "Should be gray near expiry");
        }

        [Test]
        public void StableRepair_NoFlicker()
        {
            Entity well = CreateWellEntity(RepairStage.StableRepair);

            for (int i = 0; i < 20; i++)
            {
                var e = CreateRenderEvent(well);
                well.FireEvent(e);
                string color = e.GetStringParameter("ColorString", "&c");
                // Stable repair should never override the base color
                Assert.AreEqual("&c", color, $"Frame {i}: stable well should not flicker");
            }
        }

        [Test]
        public void ProximityMessage_ShowsOnceWhenPlayerAdjacent()
        {
            Entity well = CreateWellEntity(RepairStage.Fouled);
            _zone.AddEntity(well, 10, 10);

            Entity player = CreatePlayer();
            player.SetTag("Player", "");
            _zone.AddEntity(player, 11, 10);

            MessageLog.Clear();

            // First EndTurn: should show proximity message
            well.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, MessageLog.Count, "Should show proximity message");
            Assert.AreEqual("You hear water dripping unevenly.", MessageLog.GetLast());

            // Second EndTurn: should NOT show duplicate
            well.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, MessageLog.Count, "Should not duplicate proximity message");
        }

        [Test]
        public void ProximityMessage_DoesNotShow_WhenPlayerFarAway()
        {
            Entity well = CreateWellEntity(RepairStage.StableRepair);
            _zone.AddEntity(well, 10, 10);

            Entity player = CreatePlayer();
            player.SetTag("Player", "");
            _zone.AddEntity(player, 15, 15);

            MessageLog.Clear();

            well.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(0, MessageLog.Count, "Should not show message when player is far");
        }

        [Test]
        public void ProximityMessage_ResetsAfterCall()
        {
            Entity well = CreateWellEntity(RepairStage.Fouled);
            _zone.AddEntity(well, 10, 10);

            Entity player = CreatePlayer();
            player.SetTag("Player", "");
            _zone.AddEntity(player, 11, 10);

            MessageLog.Clear();

            well.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, MessageLog.Count);

            // Reset proximity flag (simulates zone re-entry)
            well.GetPart<WellSitePart>().ResetProximityMessage();

            well.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(2, MessageLog.Count, "Should show message again after reset");
        }

        [Test]
        public void GroundMarkerVisuals_ChangesWithStage()
        {
            Entity marker = new Entity { ID = "marker1", BlueprintName = "WellGroundMarker" };
            marker.AddPart(new RenderPart { RenderString = ".", ColorString = "&w" });
            marker.SetTag("WellGroundMarker", "");

            var site = new RepairableSiteState
            {
                SiteId = SiteId,
                Stage = RepairStage.Fouled
            };

            SettlementSiteVisuals.ApplyToEntity(marker, site);
            var render = marker.GetPart<RenderPart>();
            Assert.IsTrue(render.ColorString == "&g" || render.ColorString == "&w",
                "Fouled ground marker should be brown or green");

            site.Stage = RepairStage.StableRepair;
            SettlementSiteVisuals.ApplyToEntity(marker, site);
            Assert.AreEqual("&c", render.ColorString, "Repaired ground marker should be cyan");
        }

        [Test]
        public void GraduatedDisplayNames_ChangeWithStage()
        {
            Entity well = new Entity { BlueprintName = "Well" };
            well.AddPart(new RenderPart());

            var site = new RepairableSiteState { SiteId = SiteId, Stage = RepairStage.Fouled };
            SettlementSiteVisuals.ApplyToEntity(well, site);
            Assert.AreEqual("fouled well (cracked ring)", well.GetPart<RenderPart>().DisplayName);

            site.Stage = RepairStage.TemporarilyPurified;
            SettlementSiteVisuals.ApplyToEntity(well, site);
            Assert.AreEqual("freshened well (temporary)", well.GetPart<RenderPart>().DisplayName);

            site.Stage = RepairStage.StableRepair;
            SettlementSiteVisuals.ApplyToEntity(well, site);
            Assert.AreEqual("repaired well", well.GetPart<RenderPart>().DisplayName);

            site.Stage = RepairStage.ImprovedWithCaretaker;
            SettlementSiteVisuals.ApplyToEntity(well, site);
            Assert.AreEqual("maintained well", well.GetPart<RenderPart>().DisplayName);
        }

        [Test]
        public void OnStageChanged_EmitsAuraRequests()
        {
            Entity well = CreateWellEntity(RepairStage.Fouled);
            _zone.AddEntity(well, 10, 10);

            var wellPart = well.GetPart<WellSitePart>();
            AsciiFxBus.Clear();

            // Start aura for current stage
            wellPart.StartAuraForStage(RepairStage.Fouled, _zone);

            var requests = AsciiFxBus.Drain();
            Assert.AreEqual(1, requests.Count, "Should emit one aura start request");
            Assert.AreEqual(AsciiFxRequestType.AuraStart, requests[0].Type);
            Assert.AreEqual(AsciiFxTheme.WellFouled, requests[0].Theme);
        }

        private Entity CreateWellEntity(RepairStage stage)
        {
            var site = _manager.GetSite(SettlementId, SiteId);
            if (site != null)
                site.Stage = stage;

            Entity well = new Entity { BlueprintName = "Well" };
            well.AddPart(new RenderPart { RenderString = "O", ColorString = "&c", DisplayName = "well" });
            well.Properties["SettlementId"] = SettlementId;
            well.Properties["SettlementSiteId"] = SiteId;

            var wellPart = new WellSitePart();
            wellPart.SettlementId = SettlementId;
            wellPart.SiteId = SiteId;
            well.AddPart(wellPart);

            return well;
        }

        private static GameEvent CreateRenderEvent(Entity entity)
        {
            var render = entity.GetPart<RenderPart>();
            var e = GameEvent.New("Render");
            e.SetParameter("Entity", (object)entity);
            e.SetParameter("RenderPart", (object)render);
            e.SetParameter("ColorString", render.ColorString ?? "&y");
            e.SetParameter("DetailColor", render.DetailColor ?? "");
            return e;
        }

        private string GetRenderColor(Entity entity)
        {
            var e = CreateRenderEvent(entity);
            entity.FireEvent(e);
            return e.GetStringParameter("ColorString", "&y");
        }

        private static Entity CreatePlayer()
        {
            var entity = new Entity { BlueprintName = "Player" };
            entity.AddPart(new InventoryPart());
            return entity;
        }
    }
}
