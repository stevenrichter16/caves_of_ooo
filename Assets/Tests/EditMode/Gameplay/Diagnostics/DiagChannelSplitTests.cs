using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Wx optimization review §3a/§3b/§3c
    /// (Docs/FELLING-WX-OPTIMIZATION-REVIEW.md) — the diag ring buffer
    /// was eating itself.
    ///
    /// <para>§3a: turn/Begin + turn/End recorded for EVERY actor on the
    /// always-on "turn" channel — 38 records per player action at 18
    /// NPCs, rotating the 8192-slot buffer in ~215 player turns and
    /// evicting all other history (Diag.cs's own buffer comment measured
    /// dropped_records: 17733 and pre-approves this split). NPC turn
    /// records now go to the off-by-default "turn-verbose" channel; the
    /// player's stay on "turn" — the Begin/End pairing survives within
    /// each channel.</para>
    ///
    /// <para>§3b: TilePropagationSystem emitted one eagerly-serialized
    /// PropagationStep per newly reached cell inside a flood that runs
    /// every player turn — a self-sustaining peat fire emitted 25-75 per
    /// turn at steady state. Each flood now emits ONE tile/
    /// PropagationWave aggregate; the per-cell steps live behind the
    /// off-by-default "tile-verbose" channel, so chains stay traceable
    /// when someone is actually tracing.</para>
    ///
    /// <para>§3c: CausticSkinPart recorded SkinContactRejected BEFORE
    /// the attacker-null gate, so source-less environmental ticks (a
    /// frog burning, a poison tick) emitted a stray rejection record per
    /// tick with a null target. The record now only fires when an
    /// external attacker actually existed to reject.</para>
    /// </summary>
    public class DiagChannelSplitTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.ResetForTests();
            var liquidJson = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                liquidJson.Add(File.ReadAllText(f));
            LiquidRegistry.InitializeFromJsonSources(liquidJson);
        }

        [TearDown]
        public void TearDown()
        {
            Diag.SetChannel("turn-verbose", false);
            Diag.SetChannel("tile-verbose", false);
            TurnManager.World = null;
            SettlementRuntime.Reset();
        }

        private static int Count(string category, string kind) =>
            DiagQuery.Apply(new DiagQuery.Filter
            { Category = category, Kind = kind, Limit = 100 }).Records.Count;

        // ════════════════════════════════════════════════════════════
        //   §3a — NPC turn records leave the default channel
        // ════════════════════════════════════════════════════════════

        [Test]
        public void NpcEndTurn_RecordsOnVerboseChannel_NotDefault()
        {
            var tm = new TurnManager();
            var npc = new Entity { ID = "npc", BlueprintName = "Bystander" };
            tm.AddEntity(npc);

            Diag.SetChannel("turn-verbose", true);
            tm.EndTurn(npc);

            Assert.AreEqual(0, Count("turn", "End"),
                "NPC turn boundaries must not rotate the always-on buffer");
            Assert.AreEqual(1, Count("turn-verbose", "End"),
                "but stay queryable when a debugger opts in");
        }

        [Test]
        public void NpcEndTurn_VerboseOff_RecordsNothing()
        {
            // The steady-state win: NPC boundaries cost zero records
            // (and zero serialization) in normal play.
            var tm = new TurnManager();
            var npc = new Entity { ID = "npc", BlueprintName = "Bystander" };
            tm.AddEntity(npc);

            tm.EndTurn(npc);

            Assert.AreEqual(0, Count("turn", "End"));
            Assert.AreEqual(0, Count("turn-verbose", "End"));
        }

        [Test]
        public void PlayerEndTurn_StaysOnDefaultTurnChannel()
        {
            // Counter-check: the player's boundary IS the round marker
            // the debugging workflow keys on — it must not move.
            var tm = new TurnManager();
            var player = new Entity { ID = "p", BlueprintName = "Player" };
            player.Tags["Player"] = "";
            tm.AddEntity(player);

            tm.EndTurn(player);

            Assert.AreEqual(1, Count("turn", "End"),
                "the player's End record anchors the round in the stream");
            Assert.AreEqual(0, Count("turn-verbose", "End"));
        }

        [Test]
        public void BeginRecords_SplitTheSameWay()
        {
            // Begin/End pairing survives within each channel: the NPC's
            // Begin joins its End on turn-verbose, the player's stays on
            // turn. NPC added first so its full turn runs before the
            // loop yields to the player.
            var tm = new TurnManager();
            var npc = new Entity { ID = "npc", BlueprintName = "Bystander" };
            var player = new Entity { ID = "p", BlueprintName = "Player" };
            player.Tags["Player"] = "";
            tm.AddEntity(npc);
            tm.AddEntity(player);

            Diag.SetChannel("turn-verbose", true);
            tm.ProcessUntilPlayerTurn();

            var verboseBegins = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "turn-verbose", Kind = "Begin", Limit = 10 }).Records;
            Assert.AreEqual(1, verboseBegins.Count, "the NPC's Begin is verbose");
            StringAssert.Contains("\"blueprintName\":\"Bystander\"",
                verboseBegins[0].PayloadJson);

            var defaultBegins = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "turn", Kind = "Begin", Limit = 10 }).Records;
            Assert.AreEqual(1, defaultBegins.Count, "the player's Begin is not");
            StringAssert.Contains("\"blueprintName\":\"Player\"",
                defaultBegins[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   §3b — one wave record per flood; steps go verbose
        // ════════════════════════════════════════════════════════════

        [Test]
        public void PropagateCharge_EmitsOneAggregateWave()
        {
            var zone = new Zone("Wave1");
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.WriteCoating(6, 5, "water", 6);
            zone.TileState.WriteCoating(7, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);
            Diag.ResetAll();

            TilePropagationSystem.PropagateCharge(zone);

            var waves = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "tile", Kind = "PropagationWave", Limit = 10 }).Records;
            Assert.AreEqual(1, waves.Count,
                "one flood, one record — however many cells it reached");
            StringAssert.Contains("\"reached\":2", waves[0].PayloadJson);
            StringAssert.Contains("\"wave\":\"charge\"", waves[0].PayloadJson);
        }

        [Test]
        public void PropagateCharge_NothingReached_EmitsNoWave()
        {
            // Counter-check: the every-turn inert case (charge with no
            // conductive neighbors) stays silent — the whole point.
            var zone = new Zone("Wave2");
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);
            Diag.ResetAll();

            TilePropagationSystem.PropagateCharge(zone);

            Assert.AreEqual(0, Count("tile", "PropagationWave"),
                "no spread, no record — inert turns cost nothing");
        }

        [Test]
        public void PropagateFire_EmitsOneAggregateWave()
        {
            var zone = new Zone("Wave3");
            for (int x = 5; x <= 7; x++) zone.TileState.WriteCoating(x, 5, "oil", 8);
            zone.TileState.WriteResidue(5, 5, "embers", 4);
            Diag.ResetAll();

            TilePropagationSystem.PropagateFire(zone);

            var waves = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "tile", Kind = "PropagationWave", Limit = 10 }).Records;
            Assert.AreEqual(1, waves.Count);
            StringAssert.Contains("\"wave\":\"fire\"", waves[0].PayloadJson);
        }

        [Test]
        public void PerCellSteps_LiveOnTheVerboseChannel()
        {
            var zone = new Zone("Wave4");
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.WriteCoating(6, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);
            Diag.ResetAll();
            Diag.SetChannel("tile-verbose", true);

            TilePropagationSystem.PropagateCharge(zone);

            Assert.AreEqual(0, Count("tile", "PropagationStep"),
                "per-cell steps must not flood the always-on channel");
            Assert.AreEqual(1, Count("tile-verbose", "PropagationStep"),
                "but the chain stays traceable when a debugger opts in");
        }

        // ════════════════════════════════════════════════════════════
        //   §3c — no rejection record without an attacker to reject
        // ════════════════════════════════════════════════════════════

        private static Entity MakeFrog(Zone zone, int x, int y)
        {
            var frog = new Entity { ID = "frog", BlueprintName = "Bandfrog" };
            frog.Tags["Creature"] = "";
            frog.Statistics["Hitpoints"] = new Stat
            { Owner = frog, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            frog.AddPart(new RenderPart { DisplayName = "bandfrog" });
            frog.AddPart(new StatusEffectsPart());
            frog.AddPart(new CausticSkinPart());
            zone.AddEntity(frog, x, y);
            return frog;
        }

        [Test]
        public void SourcelessElementalTick_EmitsNoRejectionRecord()
        {
            // A frog burning in a fire it walked into has no attacker;
            // every burn tick emitted a SkinContactRejected with a null
            // target — stream pollution on the always-on damage channel.
            var zone = new Zone("Caustic1");
            SettlementRuntime.ActiveZone = zone;
            var frog = MakeFrog(zone, 10, 10);

            var d = new Damage(3);
            d.AddAttribute("Fire");
            CombatSystem.ApplyDamage(frog, d, null, zone);

            Assert.AreEqual(0, Count("damage", "SkinContactRejected"),
                "no attacker existed — there was nothing to reject");
        }

        [Test]
        public void AttackerElementalHit_StillEmitsTheRejection()
        {
            // Counter-check: an actual arsonist's elemental hit still
            // records why the skin did not answer.
            var zone = new Zone("Caustic2");
            SettlementRuntime.ActiveZone = zone;
            var frog = MakeFrog(zone, 10, 10);
            var arsonist = new Entity { ID = "arsonist", BlueprintName = "Arsonist" };
            arsonist.Tags["Creature"] = "";
            arsonist.Statistics["Hitpoints"] = new Stat
            { Owner = arsonist, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            zone.AddEntity(arsonist, 11, 10);

            var d = new Damage(3);
            d.AddAttribute("Fire");
            CombatSystem.ApplyDamage(frog, d, arsonist, zone);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "SkinContactRejected", Limit = 10 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("elemental_not_contact", recs[0].PayloadJson);
        }
    }
}
