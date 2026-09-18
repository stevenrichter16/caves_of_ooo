using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops SM7 — regression pins for the 2026-07-25 farming audit
    /// (Docs/CROPS-WATERING-GRIMOIRE.md §5 SM7). Each section pins one
    /// confirmed finding's fix; test names carry the finding ID.
    ///
    /// SM7a — the critical ground-seed exploit: the world-action menu
    /// fires GetInventoryActions / InventoryAction directly on
    /// zone-resident items (WorldInteractionSystem.GatherActions →
    /// InputHandler.ExecuteWorldActionSelection), so a seed lying on
    /// the ground offered "Plant", planted at the PLAYER's cell, and
    /// was never consumed (InventoryPart.RemoveObject no-ops for items
    /// not in the inventory) — infinite crops from one dropped seed.
    /// The fix gates both the action row and DoPlant on possession.
    /// </summary>
    [TestFixture]
    public class FarmingAuditFixTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(path));
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            SeedPart.Factory = _factory;
        }

        [TearDown]
        public void TearDown()
        {
            SeedPart.Factory = null;
        }

        // ── Helpers ──────────────────────────────────────────────

        private static Entity MakeActor(Zone zone, int x, int y)
        {
            var actor = new Entity { ID = "farmer-" + x + "-" + y, BlueprintName = "TestActor" };
            actor.Tags["Creature"] = "";
            actor.AddPart(new RenderPart { DisplayName = "farmer" });
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.AddPart(new InventoryPart { MaxWeight = 150 });
            zone.AddEntity(actor, x, y);
            return actor;
        }

        private void PlaceTerrain(Zone zone, string blueprint, int x, int y)
        {
            var terrain = _factory.CreateEntity(blueprint);
            zone.AddEntity(terrain, x, y);
        }

        /// <summary>Mirror of InputHandler.ExecuteWorldActionSelection's
        /// generic dispatch (InputHandler.cs:2413-2419): the world-action
        /// menu fires InventoryAction DIRECTLY on the target entity,
        /// bypassing PerformInventoryActionCommand.</summary>
        private static void FireWorldMenuPlant(Entity target, Entity actor, Zone zone)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "PlantSeed");
            e.SetParameter("Actor", (object)actor);
            e.SetParameter("Zone", (object)zone);
            target.FireEventAndRelease(e);
        }

        private static bool CellHasCrop(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && cell.HasObjectWithPart<CropPart>();
        }

        // ════════════════════════════════════════════════════════════
        //   SM7a — ground-seed exploit (audit finding F1 critical,
        //   F2 yellow: remote plant-at-player)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Sm7a_GroundSeed_WorldMenuDispatch_RejectsNotCarried_NoCropNoConsume()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            PlaceTerrain(zone, "Grass", 6, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            zone.AddEntity(seed, 6, 5); // seed lies on the GROUND, not carried

            FireWorldMenuPlant(seed, actor, zone);

            Assert.IsFalse(CellHasCrop(zone, 5, 5),
                "no crop at the actor's cell — the exploit planted here");
            Assert.IsFalse(CellHasCrop(zone, 6, 5), "no crop at the seed's cell either");
            Assert.IsNotNull(zone.GetEntityCell(seed), "ground seed untouched");

            var rejects = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejects.Count, "exactly one PlantRejected record");
            StringAssert.Contains("\"reason\":\"not_carried\"", rejects[0].PayloadJson);

            var planted = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "CropPlanted", Limit = 5 }).Records;
            Assert.AreEqual(0, planted.Count, "no CropPlanted success record");
        }

        [Test]
        public void Sm7a_GroundSeedStack_NotDecrementedByRemotePlant()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            var stacker = seed.GetPart<StackerPart>();
            Assert.IsNotNull(stacker, "sanity: seeds stack");
            stacker.StackCount = 3;
            zone.AddEntity(seed, 8, 8);

            FireWorldMenuPlant(seed, actor, zone);

            Assert.AreEqual(3, stacker.StackCount,
                "a ground stack must not be decremented by a rejected remote plant");
        }

        [Test]
        public void Sm7a_GroundSeed_WorldMenu_OmitsPlantRow()
        {
            var zone = new Zone("z");
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            zone.AddEntity(seed, 6, 5);

            var actions = WorldInteractionSystem.GatherActions(seed, actor);

            for (int i = 0; i < actions.Count; i++)
                Assert.AreNotEqual("PlantSeed", actions[i].Command,
                    "a ground seed must not offer Plant in the world-action menu");
        }

        [Test]
        public void Sm7a_CarriedSeed_SameRawDispatch_StillPlants()
        {
            // Counter-check: the possession gate keys on POSSESSION, not
            // on which dispatch path fired the event. A carried seed via
            // the same raw world-menu event shape plants normally.
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            actor.GetPart<InventoryPart>().AddObject(seed);

            FireWorldMenuPlant(seed, actor, zone);

            Assert.IsTrue(CellHasCrop(zone, 5, 5), "carried seed plants at the actor's cell");
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "single carried seed is consumed");

            var planted = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "CropPlanted", Limit = 5 }).Records;
            Assert.AreEqual(1, planted.Count);
        }

        [Test]
        public void Sm7a_CarriedSeed_InventoryUi_StillOffersPlantRow()
        {
            // Counter-check for the action-row gate: the inventory-screen
            // path (InventorySystem.GetActions) must keep offering Plant
            // for a carried seed.
            var zone = new Zone("z");
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            actor.GetPart<InventoryPart>().AddObject(seed);

            var actions = InventorySystem.GetActions(actor, seed);

            bool hasPlant = false;
            for (int i = 0; i < actions.Count; i++)
                if (actions[i].Command == "PlantSeed") hasPlant = true;
            Assert.IsTrue(hasPlant, "carried seeds keep the Plant action");
        }

        // ════════════════════════════════════════════════════════════
        //   SM7c — load-path farming access (audit finding F4 yellow:
        //   pre-farming saves had NO source of seeds or the grimoire —
        //   the kit was new-game-only and no trade/drop source exists,
        //   so the feature was permanently inaccessible on persistent
        //   characters, contradicting the RPG identity)
        // ════════════════════════════════════════════════════════════

        private static Entity MakeLoadedPlayer()
        {
            var player = new Entity { ID = "player", BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new InventoryPart { MaxWeight = 500 });
            player.AddPart(new ActivatedAbilitiesPart());
            player.AddPart(new CavesOfOoo.Skills.SkillsPart());
            return player;
        }

        [Test]
        public void Sm7c_FreshPreFarmingPlayer_ShouldGrantOnLoad()
        {
            var player = MakeLoadedPlayer();
            Assert.IsTrue(FarmingAccessGrant.ShouldGrantOnLoad(player),
                "a pre-farming save's player (no kit, no pin) gets the one-shot grant");
        }

        [Test]
        public void Sm7c_MarkedPlayer_ShouldNotGrantAgain()
        {
            var player = MakeLoadedPlayer();
            FarmingAccessGrant.MarkGranted(player);
            Assert.IsFalse(FarmingAccessGrant.ShouldGrantOnLoad(player),
                "the pin makes the grant one-shot — discarding the kit later never re-grants");
        }

        [Test]
        public void Sm7c_PlayerCarryingGrimoire_ShouldNotGrant()
        {
            // A post-SM4 save made BEFORE this fix existed: has the kit
            // but no pin. Must not double-grant.
            var player = MakeLoadedPlayer();
            var grimoire = _factory.CreateEntity("WateringGrimoire");
            player.GetPart<InventoryPart>().AddObject(grimoire);
            Assert.IsFalse(FarmingAccessGrant.ShouldGrantOnLoad(player));
        }

        [Test]
        public void Sm7c_PlayerCarryingSeeds_ShouldNotGrant()
        {
            var player = MakeLoadedPlayer();
            var seed = _factory.CreateEntity("EmberwheatSeed");
            player.GetPart<InventoryPart>().AddObject(seed);
            Assert.IsFalse(FarmingAccessGrant.ShouldGrantOnLoad(player));
        }

        [Test]
        public void Sm7c_PlayerKnowingConjureRain_ShouldNotGrant()
        {
            // Kit consumed/discarded but the spell already learned —
            // farming access exists (rain + any future seed source), and
            // re-granting a free grimoire would dupe its trade value.
            var player = MakeLoadedPlayer();
            Assert.IsTrue(player.GetPart<CavesOfOoo.Skills.SkillsPart>()
                .AddSkill("Hydromancy_ConjureRain"));
            Assert.IsFalse(FarmingAccessGrant.ShouldGrantOnLoad(player));
        }

        [Test]
        public void Sm7c_NullPlayer_ShouldNotGrant_NoCrash()
        {
            Assert.IsFalse(FarmingAccessGrant.ShouldGrantOnLoad(null));
            Assert.DoesNotThrow(() => FarmingAccessGrant.MarkGranted(null));
        }

        [Test]
        public void Sm7c_GrantedPin_SurvivesSaveRoundTrip()
        {
            var player = MakeLoadedPlayer();
            FarmingAccessGrant.MarkGranted(player);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);

            Assert.IsFalse(FarmingAccessGrant.ShouldGrantOnLoad(loaded),
                "the one-shot pin must survive save/load or every load re-grants the kit");
        }

        // ════════════════════════════════════════════════════════════
        //   SM7d — content/UX polish (F3 picker entry, F8 grimoire
        //   content + copy fix, blue/note diag + FX items)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Sm7d_ConjureRain_HasGrimoirePickerEntry()
        {
            // F3: the grimoire picker filters by IsGrimoirePower — a
            // missing row made Conjure Rain invisible and strandable
            // (reassign its hotbar slot once and only the M-key ability
            // manager could re-bind it).
            Assert.IsTrue(GrimoireTooltipData.IsGrimoirePower("Hydromancy_ConjureRain"),
                "Conjure Rain must be visible to the grimoire picker");
            Assert.IsTrue(GrimoireTooltipData.TryGet("Hydromancy_ConjureRain", out var tip));
            Assert.AreEqual("Conjure Rain", tip.DisplayName);
            Assert.IsFalse(string.IsNullOrEmpty(tip.Mechanics));
            Assert.IsFalse(string.IsNullOrEmpty(tip.ColorCode));
            // Counter-check: the filter still rejects unknown classes.
            Assert.IsFalse(GrimoireTooltipData.IsGrimoirePower("NoSuchMutation"));
        }

        [Test]
        public void Sm7d_WateringGrimoire_MatchesSiblingGrimoireContract()
        {
            // F8: every sibling grimoire ships Category=Books + the
            // Grimoire/Tier tags + a bespoke AlreadyKnownMessage; the
            // WateringGrimoire had none — miscategorized in the
            // inventory and invisible to the scribe's tag-gated
            // grimoire-copy service.
            var grimoire = _factory.CreateEntity("WateringGrimoire");
            Assert.IsTrue(grimoire.HasTag("Grimoire"),
                "the scribe copy service selects by HasTag(\"Grimoire\")");
            Assert.IsTrue(grimoire.HasTag("Tier"));
            Assert.AreEqual("Books", grimoire.GetPart<PhysicsPart>().Category,
                "files under Books with every other grimoire");
            Assert.AreEqual("The rite of rain is already yours.",
                grimoire.GetPart<GrimoirePart>().AlreadyKnownMessage);
        }

        [Test]
        public void Sm7d_CopyGrimoire_CopiesMutationTeachingFields()
        {
            // F8 rider: CopyGrimoire copied only the knowledge-property
            // fields — a copy of a spell-teaching grimoire read as
            // "blank pages". Latent until the Grimoire tag fix exposed
            // the WateringGrimoire to the copy service.
            ConversationActions.Reset();
            ConversationActions.Factory = _factory;
            try
            {
                var player = MakeLoadedPlayer();
                var grimoire = _factory.CreateEntity("WateringGrimoire");
                player.GetPart<InventoryPart>().AddObject(grimoire);

                ConversationActions.Execute("CopyGrimoire", null, player, null);

                Entity copy = null;
                var inv = player.GetPart<InventoryPart>();
                for (int i = 0; i < inv.Objects.Count; i++)
                    if (inv.Objects[i].BlueprintName == "GrimoireCopy") copy = inv.Objects[i];
                Assert.IsNotNull(copy, "the scribe produced a copy");
                var copyPart = copy.GetPart<GrimoirePart>();
                Assert.AreEqual("Hydromancy_ConjureRain", copyPart.SkillClassName,
                    "the copy must teach the same spell, not read as blank pages");
            }
            finally
            {
                ConversationActions.Factory = null;
                ConversationActions.Reset();
            }
        }

        [Test]
        public void Sm7d_ZeroYieldCount_MatureBlocked_WithDistinctReason()
        {
            // Blue finding: YieldCount<=0 fell into the spawned==0 branch
            // and was misattributed as unknown_yield_blueprint, sending a
            // debugger hunting a blueprint that resolves fine.
            var zone = new Zone("z");
            var cropEntity = _factory.CreateEntity("CandyCarrotCrop");
            zone.AddEntity(cropEntity, 5, 5);
            var crop = cropEntity.GetPart<CropPart>();
            crop.GrowthStage = 1;
            crop.TicksInStage = crop.TicksPerStage;
            crop.MoistureTicks = 5;
            crop.YieldCount = 0;

            CropSystem.Factory = _factory;
            try { CropSystem.OnTickEnd(zone); }
            finally { CropSystem.Factory = null; }

            Assert.IsNotNull(zone.GetEntityCell(cropEntity), "crop held, not deleted");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "MatureBlocked", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"reason\":\"no_yield_count\"", recs[0].PayloadJson);
        }

        [Test]
        public void Sm7d_StageAdvanced_PayloadUsesCropBlueprintField()
        {
            // Note finding: CropPlanted/PlantRejected carry the crop's
            // blueprint as `cropBlueprint`; StageAdvanced called the same
            // referent `blueprint`, so a lifecycle grep silently missed
            // stage advances. One name per referent (Q2 convention).
            var zone = new Zone("z");
            var cropEntity = _factory.CreateEntity("CandyCarrotCrop");
            zone.AddEntity(cropEntity, 5, 5);
            var crop = cropEntity.GetPart<CropPart>();
            crop.TicksInStage = crop.TicksPerStage - 1;
            crop.MoistureTicks = 5;

            CropSystem.OnTickEnd(zone);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "StageAdvanced", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"cropBlueprint\":\"CandyCarrotCrop\"", recs[0].PayloadJson);
        }

        [Test]
        public void Sm7d_RainSequence_EndsAtTheCropRow()
        {
            // Blue finding: lifetime 0.45 / moveInterval 0.1 gave every
            // drop 4 moves — drops rained 2-3 tiles THROUGH the soil
            // after the splash landed. Lifetime is the travel budget:
            // spawn N cells above the crop → at most N moves.
            AsciiFxBus.Clear();
            SpellFxBus.Clear();
            var zone = new Zone("z");
            var caster = new Entity { ID = "caster", BlueprintName = "TestCaster" };
            caster.Tags["Creature"] = "";
            caster.AddPart(new RenderPart { DisplayName = "caster" });
            caster.AddPart(new PhysicsPart { Solid = true });
            caster.AddPart(new ActivatedAbilitiesPart());
            caster.AddPart(new CavesOfOoo.Skills.SkillsPart());
            zone.AddEntity(caster, 10, 10);
            // Migration M4: Conjure Rain is a skill — cast through the dispatcher.
            caster.GetPart<CavesOfOoo.Skills.SkillsPart>()
                .AddSkill(new CavesOfOoo.Skills.Hydromancy_ConjureRain());
            int cropY = 10;
            var cropEntity = _factory.CreateEntity("CandyCarrotCrop");
            zone.AddEntity(cropEntity, 12, cropY);

            var cmd = GameEvent.New("CommandConjureRain");
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("SourceCell", (object)zone.GetCell(10, 10));
            caster.FireEvent(cmd);
            cmd.Release();

            var sequences = SpellFxBus.Drain();
            Assert.AreEqual(1, sequences.Count);
            Assert.AreEqual(1, sequences[0].AffectedCells.Count);
            Assert.AreEqual(cropY, sequences[0].AffectedCells[0].Y,
                "the gameplay endpoint is the crop row; renderer travel may not change it");
        }

        [Test]
        public void Sm7a_PlantReject_ActorNotInZone_DistinctReason()
        {
            // Audit note: three failure modes shared reason "no_zone".
            // The actor-missing-from-zone path now reports its own reason
            // so a diag query can tell "no active zone" from "actor not
            // registered in this zone".
            var zone = new Zone("z");
            var actor = new Entity { ID = "ghost", BlueprintName = "TestActor" };
            actor.AddPart(new RenderPart { DisplayName = "ghost" });
            actor.AddPart(new InventoryPart { MaxWeight = 150 });
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            actor.GetPart<InventoryPart>().AddObject(seed);
            // actor deliberately NOT added to the zone

            var result = InventorySystem.ExecuteCommand(
                new CavesOfOoo.Core.Inventory.Commands.PerformInventoryActionCommand(seed, "PlantSeed"),
                actor, zone);

            var rejects = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejects.Count);
            StringAssert.Contains("\"reason\":\"actor_not_in_zone\"", rejects[0].PayloadJson);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "seed not consumed on reject");
        }
    }
}
