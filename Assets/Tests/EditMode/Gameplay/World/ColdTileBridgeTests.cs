using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/COLD-TILE-BRIDGE.md — cold spells reach the ground. Before
    /// this, the tile universe owned a complete freeze mechanic
    /// (freeze_water: water coating + tile cold → ice + Frozen on the
    /// occupant; melt_ice: the reverse) that NO spell could trigger:
    /// there was no ApplyColdToTile twin of ApplyFireToTile, and zero
    /// Cryomancy skills wrote tile cold. Rime Grip at a Jet Blast puddle
    /// "closed on nothing" — the reported repro (test 2).
    ///
    /// <para>All casts dispatch through FireEvent with the
    /// InputHandler's parameter conventions.</para>
    /// </summary>
    public class ColdTileBridgeTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            AsciiFxBus.Clear();
            LiquidRegistry.ResetForTests();
            var liquidJson = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                liquidJson.Add(File.ReadAllText(f));
            LiquidRegistry.InitializeFromJsonSources(liquidJson);
            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }

        [TearDown]
        public void TearDown()
        {
            TileReactionSystem.ResetForTests();
            LiquidRegistry.ResetForTests();
        }

        // ── Fixtures ─────────────────────────────────────────────────

        private static Entity Caster(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "caster", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static T Learn<T>(Entity caster) where T : BaseSkillPart, new()
        {
            var s = new T();
            Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(s));
            return s;
        }

        private static Entity Snapjaw(Zone zone, int x, int y, int hp = 40)
        {
            var e = new Entity { ID = "snapjaw" + x + "_" + y, BlueprintName = "Snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "snapjaw" });
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static (bool handled, bool blocks) Cast(Entity caster, Zone zone,
            string command, int dx = 0, int dy = 0, int range = 5)
        {
            var cmd = GameEvent.New(command);
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new System.Random(11));
            cmd.SetParameter("SourceCell", (object)zone.GetEntityCell(caster));
            cmd.SetParameter("DirectionX", dx);
            cmd.SetParameter("DirectionY", dy);
            cmd.SetParameter("Range", range);
            caster.FireEvent(cmd);
            bool handled = cmd.Handled;
            bool blocks = cmd.GetParameter<bool>("BlocksTurnAdvance");
            cmd.Release();
            return (handled, blocks);
        }

        private static ActivatedAbility Ability(Entity caster, string command)
        {
            var abilities = caster.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < abilities.AbilityList.Count; i++)
                if (abilities.AbilityList[i].Command == command) return abilities.AbilityList[i];
            return null;
        }

        private static bool HasCoating(Zone zone, int x, int y, string id)
            => zone.TileState.HasCoating(x, y, id);

        // ── 1. The substrate twin ────────────────────────────────────

        [Test]
        public void ApplyColdToTile_WritesColdAndEmitsDiag()
        {
            var zone = new Zone("Z");
            ZoneTileStateSystem.ApplyColdToTile(zone, 5, 5, null, "test");

            Assert.AreEqual(1, zone.TileState.Cold(5, 5));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "tile", Kind = "TileWritten", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("cold", recs[0].PayloadJson);
        }

        // ── 2. THE REPORTED REPRO ────────────────────────────────────

        [Test]
        public void RimeGrip_AtAnEmptyWetTile_FreezesTheWater()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeGrip>(caster);
            zone.TileState.WriteCoating(7, 5, "water", 6);   // Jet Blast's kind of water

            var (handled, _) = Cast(caster, zone, "CommandRimeGrip", dx: 1);

            Assert.IsTrue(handled, "gripping a wet tile is a real cast");
            Assert.IsTrue(HasCoating(zone, 7, 5, "ice"), "the water became ice");
            Assert.IsFalse(HasCoating(zone, 7, 5, "water"), "and the water is gone");
            Assert.Greater(Ability(caster, "CommandRimeGrip").CooldownRemaining, 0);
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("freezes the ground")));
        }

        // ── 3. Counter-check: dry ground stays a free refusal ────────

        [Test]
        public void RimeGrip_AtDryGround_IsStillAFreeRefusal()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeGrip>(caster);

            var (handled, _) = Cast(caster, zone, "CommandRimeGrip", dx: 1);

            Assert.IsFalse(handled, "nothing to grip, nothing to freeze — no cast");
            Assert.AreEqual(0, Ability(caster, "CommandRimeGrip").CooldownRemaining,
                "the refusal is free");
            Assert.AreEqual(0, zone.TileState.Cold(6, 5),
                "and dry ground is not even chilled — cold is written only where water was");
        }

        // ── 4. Hit path: creature frozen AND the ground under it ─────

        [Test]
        public void RimeGrip_HittingACreatureOnWater_FreezesBoth()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeGrip>(caster);
            zone.TileState.WriteCoating(7, 5, "water", 6);
            var target = Snapjaw(zone, 7, 5);

            var (handled, _) = Cast(caster, zone, "CommandRimeGrip", dx: 1);

            Assert.IsTrue(handled);
            Assert.IsTrue(target.HasEffect<FrozenEffect>(), "the grip's own freeze (existing)");
            Assert.IsTrue(HasCoating(zone, 7, 5, "ice"), "and the ground under the target iced (new)");
        }

        // ── 4b. LIVE FIND: shattering a prop on water still ices it ──

        [Test]
        public void RimeGrip_ShatteringAPropOnWater_StillFreezesTheGround()
        {
            // Live probe on the real world: a CHEST stood on the puddle,
            // Rime Grip took the hit path, RouteDamage shattered it, and
            // the early "shatters" return skipped the ground pass — the
            // water stayed water. Every hit branch ices the ground now,
            // and the position is captured BEFORE damage (a shattered
            // target may already be gone from the zone).
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeGrip>(caster);
            zone.TileState.WriteCoating(7, 5, "water", 6);
            var chest = new Entity { ID = "chest", BlueprintName = "Chest" };
            chest.AddPart(new RenderPart { DisplayName = "chest" });
            chest.AddPart(new PhysicsPart { Solid = false });
            chest.AddPart(new DestructiblePart { HP = 1, MaxHP = 1 });   // one grip breaks it
            chest.AddPart(new MaterialPart { MaterialTagsRaw = "Wood" });
            zone.AddEntity(chest, 7, 5);

            var (handled, _) = Cast(caster, zone, "CommandRimeGrip", dx: 1);

            Assert.IsTrue(handled);
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("shatters")),
                "precondition: the chest shattered on the hit path");
            Assert.IsTrue(HasCoating(zone, 7, 5, "ice"),
                "the ground under a shattered target still freezes");
        }

        // ── 5. Rime Nova ices its whole wet radius ───────────────────

        [Test]
        public void RimeNova_FreezesEveryWetCellInRadius()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 10, 10);
            Learn<Cryomancy_RimeNova>(caster);
            zone.TileState.WriteCoating(8, 10, "water", 6);   // radius-2 edge
            zone.TileState.WriteCoating(11, 9, "water", 6);   // diagonal
            zone.TileState.WriteCoating(13, 10, "water", 6);  // radius 3 — outside

            var (handled, _) = Cast(caster, zone, "CommandRimeNova");

            Assert.IsTrue(handled);
            Assert.IsTrue(HasCoating(zone, 8, 10, "ice"));
            Assert.IsTrue(HasCoating(zone, 11, 9, "ice"));
            Assert.IsTrue(HasCoating(zone, 13, 10, "water"), "counter-check: outside the radius stays water");
        }

        // ── 6. The pair, end to end: freeze then melt via spells ─────

        [Test]
        public void FreezeThenMelt_RoundTripsThroughTheSpells()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeGrip>(caster);
            Learn<Pyromancy_FlamingHands>(caster);
            zone.TileState.WriteCoating(6, 5, "water", 6);

            Cast(caster, zone, "CommandRimeGrip", dx: 1);
            Assert.IsTrue(HasCoating(zone, 6, 5, "ice"), "step 1: frozen");

            // A SEPARATE action (melt_ice's priority sorts after
            // freeze_water; within one action the pair is guarded).
            var fh = GameEvent.New("CommandFlamingHands");
            fh.SetParameter("Zone", (object)zone);
            fh.SetParameter("RNG", (object)new System.Random(1));
            fh.SetParameter("SourceCell", (object)zone.GetEntityCell(caster));
            fh.SetParameter("TargetCell", (object)zone.GetCell(6, 5));
            caster.FireEvent(fh);
            fh.Release();

            Assert.IsTrue(HasCoating(zone, 6, 5, "water"), "step 2: melted back to water");
            Assert.IsFalse(HasCoating(zone, 6, 5, "ice"));
        }

        // ── 7. Ground freeze is real CC through the reaction ─────────

        [Test]
        public void RimeGrip_GroundFreeze_FreezesTheOccupantViaTheReaction()
        {
            // The grip's own Frozen(0.5) lands on the entity it hits; the
            // REACTION's OccupantEffect Frozen(0.6) lands on whoever stands
            // on the tile that froze. Both are real. Here the creature is
            // BEHIND a bush... no — simpler: two cells of water, creature
            // on the SECOND; the grip takes the nearest entity (the
            // creature) but the ground cold goes on ITS cell, so the
            // reaction fires under it too. This pins that ground-freeze is
            // a real CC path — via a creature that receives BOTH.
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeGrip>(caster);
            zone.TileState.WriteCoating(7, 5, "water", 6);
            var target = Snapjaw(zone, 7, 5);

            Cast(caster, zone, "CommandRimeGrip", dx: 1);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "tile", Kind = "ReactionFired", Limit = 5 }).Records;
            Assert.IsTrue(recs.Any(r => r.PayloadJson.Contains("freeze_water")
                    && r.PayloadJson.Contains("\"occupantsHit\":1")),
                "freeze_water fired on the target's cell and hit its occupant: "
                + string.Join(" | ", recs.Select(r => r.PayloadJson)));
        }

        // ── 8. Player-visible: the readout says ice ──────────────────

        [Test]
        public void AfterTheFreeze_LookModeSaysIce()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeGrip>(caster);
            zone.TileState.WriteCoating(7, 5, "water", 6);
            var cell = zone.GetCell(7, 5);
            cell.Explored = true; cell.IsVisible = true;

            Cast(caster, zone, "CommandRimeGrip", dx: 1);

            Assert.AreEqual("On the ground: ice (4 turns)",
                CellStatusReadout.GroundLine(zone, cell));
        }

        // ── 9. Ice Lance ices its landing cell ───────────────────────

        [Test]
        public void IceLance_FreezesTheLandingCell()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_IceLance>(caster);
            zone.TileState.WriteCoating(8, 5, "water", 6);
            var target = Snapjaw(zone, 8, 5);

            var (handled, _) = Cast(caster, zone, "CommandIceLance", dx: 1, range: 6);

            Assert.IsTrue(handled);
            Assert.IsTrue(HasCoating(zone, 8, 5, "ice"), "the lance ices the ground where it lands");
        }
    }
}
