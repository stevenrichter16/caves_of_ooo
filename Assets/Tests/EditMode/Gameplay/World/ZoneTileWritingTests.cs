using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// PALIMPSEST P2 (Docs/PALIMPSEST-PROTOTYPE-PLAN.md §4) — abilities
    /// write to the ground.
    ///
    /// <para>P1 built an inert tile layer. P2 is the first phase where a
    /// player action leaves something behind that outlives it. The POC
    /// question this is all in service of — <i>does leaving marks change
    /// where you choose to fight?</i> — can only be answered in play, but
    /// these pin that the marks are actually written, in the right
    /// places, and that they decay on the player's clock.</para>
    /// </summary>
    public class ZoneTileWritingTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        private static Entity Caster(string name = "atk")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 300, Min = 0, Max = 300 };
            e.Statistics["ElectricResistance"] = new Stat { Owner = e, Name = "ElectricResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        private static SkillEventContext Ctx(Entity actor, Zone zone, int dx, int dy)
            => new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone,
                Rng = new Random(0), DirectionX = dx, DirectionY = dy,
            };

        private static T Fix<T>(Entity actor) where T : BaseSkillPart, new()
        {
            var s = new T();
            actor.GetPart<SkillsPart>().AddSkill(s, source: "test");
            return s;
        }

        // ── A zone owns its layer ────────────────────────────────

        [Test]
        public void EveryZone_HasATileStateLayer()
        {
            var zone = new Zone();
            Assert.IsNotNull(zone.TileState);
            Assert.AreEqual(0, zone.TileState.WrittenCount,
                "and a fresh zone costs nothing");
        }

        // ── Oilmark: the coating nothing could write before ──────

        [Test]
        public void Oilmark_LaysOilAlongTheLine()
        {
            // The sweep found `oil` fully defined as a liquid
            // (Combustibility 90) with NOTHING in the game able to apply
            // it. This is the skill that closes that.
            var atk = Caster();
            var skill = Fix<Pyromancy_Oilmark>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            for (int i = 1; i <= Pyromancy_Oilmark.OILMARK_RANGE; i++)
                Assert.IsTrue(zone.TileState.HasCoating(5 + i, 5, "oil"),
                    $"cell {5 + i},5 should be oiled");
        }

        [Test]
        public void Oilmark_DealsNoDamage()
        {
            // Oil is setup. The damage belongs to whatever ignites it —
            // that separation is the grammar this prototype is testing.
            var atk = Caster();
            var skill = Fix<Pyromancy_Oilmark>(atk);
            var zone = new Zone();
            var victim = Caster("victim");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(victim, 6, 5);

            int hp = victim.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(hp, victim.GetStatValue("Hitpoints"));
        }

        [Test]
        public void Oilmark_AlsoCoatsWhoeverIsStandingInIt()
        {
            // The ground and the creature standing on it must not
            // disagree about whether there is oil there.
            var atk = Caster();
            var skill = Fix<Pyromancy_Oilmark>(atk);
            var zone = new Zone();
            var victim = Caster("victim");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(victim, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(zone.TileState.HasCoating(6, 5, "oil"), "the ground is oiled");
            Assert.IsTrue(victim.GetPart<StatusEffectsPart>().HasEffect<LiquidCoveredEffect>(),
                "and so is the creature standing in it");
        }

        [Test]
        public void Oilmark_OilOutlastsWater()
        {
            // Design §6: water 6 turns, oil 8. Oil does not evaporate.
            Assert.Greater(Pyromancy_Oilmark.OilTurns,
                Hydromancy_JetBlast.GroundTurns);
        }

        // ── Existing skills now leave marks ──────────────────────

        [Test]
        public void JetBlast_WetsTheGroundItSpraysOver()
        {
            var atk = Caster();
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(zone.TileState.HasCoating(6, 5, "water"),
                "a jet blast at empty ground still leaves water — "
                + "that is the whole point of writing to the world");
        }

        [Test]
        public void JetBlast_WetsWhereTheTargetLANDS_NotWhereItStarted()
        {
            // The push moves them first; the water goes where they end
            // up. Reading the position back rather than computing it is
            // what makes this correct when a push is refused.
            var atk = Caster();
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            var target = Caster("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            var pos = zone.GetEntityPosition(target);
            Assert.AreEqual(7, pos.x, "precondition: it was pushed");
            Assert.IsTrue(zone.TileState.HasCoating(pos.x, pos.y, "water"),
                "the landing cell is wet");
        }

        [Test]
        public void EmberSpit_LeavesEmbersOnTheGround()
        {
            var atk = Caster();
            var skill = Fix<Pyromancy_EmberSpit>(atk);
            var zone = new Zone();
            var target = Caster("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(zone.TileState.HasResidue(6, 5, "embers"));
        }

        [Test]
        public void EmberSpit_LeavesEmbersEvenWhenItKills()
        {
            // Written from the pre-damage cell on purpose: a target that
            // dies should still leave a scorched tile behind it.
            var atk = Caster();
            var skill = Fix<Pyromancy_EmberSpit>(atk);
            var zone = new Zone();
            var frail = Caster("frail");
            frail.Statistics["Hitpoints"].BaseValue = 1;
            frail.Statistics["Hitpoints"].Value = 1;
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(frail, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.LessOrEqual(frail.GetStatValue("Hitpoints"), 0, "precondition: lethal");
            Assert.IsTrue(zone.TileState.HasResidue(6, 5, "embers"),
                "the ground is still scorched");
        }

        [Test]
        public void GroundSurge_ChargesTheGroundItRollsAcross()
        {
            // Including cells nobody was standing in — a charged puddle
            // with no one in it is exactly the setup this system exists
            // to allow.
            var atk = Caster();
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Greater(zone.TileState.Charge(6, 5), 0);
            Assert.Greater(zone.TileState.Charge(7, 5), 0,
                "the whole line is charged, not just occupied cells");
        }

        [Test]
        public void AWriteStopsAtStone()
        {
            // Counter-check on the shared cell walk: marks must not
            // appear through a wall.
            var atk = Caster();
            var skill = Fix<Pyromancy_Oilmark>(atk);
            var zone = new Zone();
            var wall = new Entity { ID = "w", BlueprintName = "Wall" };
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall" });
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(wall, 7, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(zone.TileState.HasCoating(6, 5, "oil"), "up to the wall");
            Assert.IsFalse(zone.TileState.HasCoating(8, 5, "oil"), "and not past it");
        }

        // ── Decay runs on the PLAYER's clock ────────────────────

        [Test]
        public void MarksDecayOncePerPlayerTurn()
        {
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 2);

            ZoneTileStateSystem.OnPlayerTurnEnd(zone);
            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "water"));

            ZoneTileStateSystem.OnPlayerTurnEnd(zone);
            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "water"));
        }

        [Test]
        public void TickingANullZone_IsGraceful()
        {
            Assert.DoesNotThrow(() => ZoneTileStateSystem.OnPlayerTurnEnd(null));
        }

        // ── Observability ────────────────────────────────────────

        [Test]
        public void EveryWrite_EmitsATileWrittenRecord()
        {
            // Routed through ZoneTileStateSystem precisely so this fires
            // uniformly — emitting from each ability would drift the
            // first time a fifth one is added.
            var zone = new Zone();
            Diag.ResetAll();

            ZoneTileStateSystem.WriteCoating(zone, 5, 5, "oil", 8, null, "TestAbility");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "tile", Kind = "TileWritten", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1,
                "the `tile` category must be registered or records vanish silently");
            StringAssert.Contains("oil", recs[0].PayloadJson);
            StringAssert.Contains("TestAbility", recs[0].PayloadJson);
        }

        // ── Reachability ─────────────────────────────────────────

        [Test]
        public void Oilmark_IsPurchasableAndFitsTheSkillsScreen()
        {
            var json = System.IO.File.ReadAllText(System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "Resources/Content/Data/Skills/Pyromancy.json"));
            SkillRegistry.LoadFromJson(json, "test:Pyromancy");

            Assert.IsTrue(SkillRegistry.TryGetPowerByClass("Pyromancy_Oilmark", out var power),
                "a skill nobody can buy does not exist");
            Assert.Greater(power.Cost, 0);
            Assert.LessOrEqual(power.Description.Length, 55);
        }
    }
}
