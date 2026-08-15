using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The M0 substrate contracts for the mutations→skills migration
    /// (<c>Docs/MUTATIONS-TO-SKILLS-MIGRATION.md</c> §2, S1-S6).
    ///
    /// <para>Every one of these was a confirmed blocker from the
    /// adversarial critique: without them, a ported power either charges
    /// the player for refusals (S1), cannot read its own targeting input
    /// (S2), lets monsters act mid-animation (S3), vanishes from the
    /// grimoire picker (S4), can be double-registered into an
    /// infinite-cast exploit (S5), or lets a JSON row that omits Cost
    /// grant +999 SP per purchase (S6).</para>
    ///
    /// <para><b>These tests dispatch through FireEvent, not by calling
    /// OnCommand directly.</b> 2,487 lines of rite tests stayed green
    /// while six rites were dead from the keybind, because every test
    /// called Cast() and bypassed HandleEvent — the event path is the
    /// thing that breaks, so the event path is what these exercise.</para>
    /// </summary>
    public class SkillSubstrateTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        // ── Fixture ──────────────────────────────────────────────────

        /// <summary>A skill whose OnCommand outcome the test controls,
        /// and which records everything the ctx carried.</summary>
        private sealed class ProbeSkill : BaseSkillPart
        {
            public override string Name => nameof(ProbeSkill);
            public bool NextResult = true;
            public bool EmitFx = false;
            public SkillEventContext Seen;
            public int Casts;

            public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
            {
                return new ActivatedAbilitySpec
                {
                    DisplayName = "Probe",
                    Command = "CommandProbe",
                    Class = "Skills",
                    TargetingMode = AbilityTargetingMode.AdjacentCell,
                    Range = 3,
                    Cooldown = 7,
                };
            }

            public override bool OnCommand(SkillEventContext ctx)
            {
                Casts++;
                Seen = ctx;
                if (EmitFx) ctx.BlocksTurnAdvance = true;
                return NextResult;
            }
        }

        private static (Entity actor, ProbeSkill skill, ActivatedAbilitiesPart abilities)
            Rig()
        {
            var actor = new Entity { ID = "caster", BlueprintName = "Player" };
            actor.Tags["Creature"] = "";
            var abilities = new ActivatedAbilitiesPart();
            actor.AddPart(abilities);
            var skills = new SkillsPart();
            actor.AddPart(skills);
            var probe = new ProbeSkill();
            Assert.IsTrue(skills.AddSkill(probe), "fixture: probe must attach");
            return (actor, probe, abilities);
        }

        /// <summary>Fire the command exactly as the InputHandler does —
        /// same parameter names, same overloads, same dictionaries.</summary>
        private static (bool handled, bool blocks) FireProbe(
            Entity actor, Zone zone, Cell source, Cell target, int range = 3)
        {
            var cmd = GameEvent.New("CommandProbe");
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new System.Random(7));
            if (source != null) cmd.SetParameter("SourceCell", (object)source);
            cmd.SetParameter("DirectionX", 1);
            cmd.SetParameter("DirectionY", 0);
            cmd.SetParameter("Range", range);
            if (target != null) cmd.SetParameter("TargetCell", (object)target);
            actor.FireEvent(cmd);
            bool handled = cmd.Handled;
            bool blocks = cmd.GetParameter<bool>("BlocksTurnAdvance");
            cmd.Release();
            return (handled, blocks);
        }

        // ── S1: a refused cast is free ───────────────────────────────

        [Test]
        public void S1_ARefusedCastCostsNothing()
        {
            var (actor, probe, abilities) = Rig();
            probe.NextResult = false;
            var zone = new Zone("Z");
            zone.AddEntity(actor, 5, 5);

            var (handled, _) = FireProbe(actor, zone, zone.GetCell(5, 5), zone.GetCell(6, 5));

            var ability = abilities.GetAbility(probe.ActivatedAbilityID);
            Assert.AreEqual(0, ability.CooldownRemaining,
                "a refusal must not charge the cooldown");
            Assert.IsFalse(handled,
                "and must leave the event unhandled so the InputHandler " +
                "neither ends the turn nor suppresses the failure line");
        }

        [Test]
        public void S1_ASuccessfulCastChargesTheCooldown()
        {
            // Counter-check — without it, "never apply the cooldown"
            // would pass the test above.
            var (actor, probe, abilities) = Rig();
            probe.NextResult = true;
            var zone = new Zone("Z");
            zone.AddEntity(actor, 5, 5);

            var (handled, _) = FireProbe(actor, zone, zone.GetCell(5, 5), zone.GetCell(6, 5));

            Assert.AreEqual(7, abilities.GetAbility(probe.ActivatedAbilityID).CooldownRemaining);
            Assert.IsTrue(handled);
        }

        [Test]
        public void S1_TheDiagDispatchContractSurvives()
        {
            // Pinned adversarial contract: reaching the skill emits
            // CommandRouted whether or not it refuses; the refusal's own
            // SkillRejected carries the reason. A refusal must not
            // silently disappear from the dispatch trace.
            var (actor, probe, _) = Rig();
            probe.NextResult = false;
            var zone = new Zone("Z");
            zone.AddEntity(actor, 5, 5);
            FireProbe(actor, zone, zone.GetCell(5, 5), null);

            var routed = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "CommandRouted", Limit = 5 }).Records;
            Assert.AreEqual(1, routed.Count, "refusals still leave a dispatch trace");
        }

        // ── S2: the ctx carries the targeting inputs ─────────────────

        [Test]
        public void S2_TheContextCarriesCellsAndRange()
        {
            var (actor, probe, _) = Rig();
            var zone = new Zone("Z");
            zone.AddEntity(actor, 5, 5);
            var src = zone.GetCell(5, 5);
            var tgt = zone.GetCell(6, 5);

            FireProbe(actor, zone, src, tgt, range: 3);

            Assert.AreSame(src, probe.Seen.SourceCell, "SourceCell (object dict)");
            Assert.AreSame(tgt, probe.Seen.TargetCell,
                "TargetCell — the exact cell the player picked, not an auto-target");
            Assert.AreEqual(3, probe.Seen.Range, "Range (int dict — the split-dictionary trap)");
            Assert.AreEqual(1, probe.Seen.DirectionX);
            Assert.IsNotNull(probe.Seen.Zone);
            Assert.IsNotNull(probe.Seen.Rng);
        }

        [Test]
        public void S2_AMissingTargetCellArrivesNullNotCrashing()
        {
            // Directional/self-centered powers get no TargetCell — null
            // is a targeting-mode fact, not an error.
            var (actor, probe, _) = Rig();
            var zone = new Zone("Z");
            zone.AddEntity(actor, 5, 5);

            FireProbe(actor, zone, zone.GetCell(5, 5), target: null);

            Assert.IsNull(probe.Seen.TargetCell);
        }

        // ── S3: the FX/turn interlock ────────────────────────────────

        [Test]
        public void S3_BlocksTurnAdvancePropagatesToTheEvent()
        {
            var (actor, probe, _) = Rig();
            probe.EmitFx = true;
            var zone = new Zone("Z");
            zone.AddEntity(actor, 5, 5);

            var (_, blocks) = FireProbe(actor, zone, zone.GetCell(5, 5), null);

            Assert.IsTrue(blocks,
                "the InputHandler reads this to hold the turn until FX drain");
        }

        [Test]
        public void S3_NoFxMeansNoBlock()
        {
            var (actor, probe, _) = Rig();
            probe.EmitFx = false;
            var zone = new Zone("Z");
            zone.AddEntity(actor, 5, 5);

            var (_, blocks) = FireProbe(actor, zone, zone.GetCell(5, 5), null);

            Assert.IsFalse(blocks);
        }

        // ── S4: hotbar/picker identity ───────────────────────────────

        [Test]
        public void S4_ASkillAbilityCarriesItsPowerClass()
        {
            var (_, probe, abilities) = Rig();
            var ability = abilities.GetAbility(probe.ActivatedAbilityID);

            Assert.AreEqual(nameof(ProbeSkill), ability.SourcePowerClass,
                "the tooltip/picker key — empty means invisible in the " +
                "grimoire slot picker and colourless on the hotbar");
        }

        // ── S5: duplicate commands are rejected ──────────────────────

        [Test]
        public void S5_TwoAbilitiesCannotShareACommand()
        {
            var (_, _, abilities) = Rig();

            // The guard logs an error by design — expected, not a failure.
            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Error,
                "[ActivatedAbilities] duplicate Command 'CommandProbe' " +
                "(existing: 'Probe', rejected: 'Probe Impostor') " +
                "— two powers may not share a command string.");

            var second = abilities.AddAbility(
                displayName: "Probe Impostor",
                command: "CommandProbe",
                abilityClass: "Skills");

            Assert.AreEqual(System.Guid.Empty, second,
                "dispatch is keyed on the command string and FireEvent " +
                "short-circuits — a duplicate is an infinite-cast exploit");
            int count = 0;
            for (int i = 0; i < abilities.AbilityList.Count; i++)
                if (abilities.AbilityList[i].Command == "CommandProbe") count++;
            Assert.AreEqual(1, count, "exactly one registration survives");
        }

        // ── S6: negative cost is not an SP faucet ────────────────────

        [Test]
        public void S6_ANegativeCostRowCannotBeBought()
        {
            SkillRegistry.ResetForTests();
            SkillRegistry.InitializeFromJson(@"{
                ""Skills"": [{
                    ""Name"": ""Testcraft"", ""Class"": ""SpellcraftSkill"",
                    ""Cost"": 1, ""Description"": ""d"",
                    ""Powers"": [{
                        ""Name"": ""Faucet"", ""Class"": ""Spellcraft_LeyTap"",
                        ""Cost"": -1, ""Description"": ""d""
                    }]
                }]
            }");

            var actor = new Entity { ID = "buyer", BlueprintName = "Player" };
            actor.AddPart(new SkillsPart());
            actor.AddPart(new ActivatedAbilitiesPart());
            actor.Statistics["SP"] = new Stat
            { Owner = actor, Name = "SP", BaseValue = 5, Min = 0, Max = 999 };

            var result = BuySkillAction.Execute(actor, "Spellcraft_LeyTap");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.NotPurchasable, result.Reason);
            Assert.AreEqual(5, actor.GetStatValue("SP", 0),
                "SP must be untouched — the old gate ADDED SP on negative-cost purchases");
        }
    }
}
