using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M1.a — the Mortal Start. Pins the real Level-1 loadout
    /// (dagger + two healing tonics + empty BitLocker) and the debug-grant
    /// gate's shipping default. Counter-checks assert the showcase grants
    /// do NOT leak into the mortal start.
    /// </summary>
    public class StartingLoadoutTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        private Entity CreateBlueprintPlayer()
        {
            var player = _harness.Factory.CreateEntity("Player");
            Assert.IsNotNull(player, "Player blueprint must exist.");
            return player;
        }

        private static int CountUnits(InventoryPart inventory, string blueprintName)
        {
            return inventory.Objects
                .Where(o => o.BlueprintName == blueprintName)
                .Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        }

        // ====================================================================
        // The shipping default: debug grants are OFF.
        // ====================================================================

        [Test]
        public void DebugGrantsEnabled_ShipsFalse()
        {
            // The debug loadout inverted every reward loop in the game
            // (fun-gap analysis 2026-07-12). If this test starts failing,
            // someone flipped the shipping default — that is a release
            // decision, not a code cleanup.
            Assert.IsFalse(StartingLoadout.DebugGrantsEnabled,
                "StartingLoadout.DebugGrantsEnabled must default to false in play builds.");
        }

        // ====================================================================
        // The starter kit.
        // ====================================================================

        [Test]
        public void ApplyStarterKit_GrantsDaggerAndTwoHealingTonics()
        {
            var player = CreateBlueprintPlayer();

            StartingLoadout.ApplyStarterKit(player, _harness.Factory);

            var inventory = player.GetPart<InventoryPart>();
            Assert.IsNotNull(inventory, "Player blueprint must carry InventoryPart.");
            Assert.AreEqual(1, CountUnits(inventory, "Dagger"),
                "Starter kit grants exactly one Dagger.");
            Assert.AreEqual(2, CountUnits(inventory, "HealingTonic"),
                "Starter kit grants exactly two HealingTonics.");
        }

        [Test]
        public void ApplyStarterKit_AttachesEmptyBitLocker()
        {
            // Verification sweep C2: InitializePlayerStartingTinkering was the
            // ONLY thing attaching BitLockerPart; gating it would strip the part
            // and silently break the Tinker UI + ItemEnhancementShowcase.
            // The kit must attach the part EMPTY — recipes/bits are world rewards.
            var player = CreateBlueprintPlayer();

            StartingLoadout.ApplyStarterKit(player, _harness.Factory);

            var bitLocker = player.GetPart<BitLockerPart>();
            Assert.IsNotNull(bitLocker,
                "Starter kit must attach BitLockerPart (Tinker UI requires it).");
            Assert.AreEqual(0, bitLocker.GetKnownRecipes().Count,
                "Mortal start knows zero tinker recipes.");
            Assert.IsTrue(bitLocker.GetBitsSnapshot().All(kv => kv.Value == 0),
                "Mortal start holds zero bits of every type.");
        }

        [Test]
        public void ApplyStarterKit_SecondCall_DoesNotDoubleGrant()
        {
            // Idempotency guard: the load path re-runs bootstrap wiring; a
            // second ApplyStarterKit must not stack a second kit.
            var player = CreateBlueprintPlayer();

            StartingLoadout.ApplyStarterKit(player, _harness.Factory);
            StartingLoadout.ApplyStarterKit(player, _harness.Factory);

            var inventory = player.GetPart<InventoryPart>();
            Assert.AreEqual(1, CountUnits(inventory, "Dagger"),
                "Second ApplyStarterKit call must be a no-op (dagger).");
            Assert.AreEqual(2, CountUnits(inventory, "HealingTonic"),
                "Second ApplyStarterKit call must be a no-op (tonics).");
        }

        // ====================================================================
        // Counter-checks: the mortal start does NOT include showcase grants.
        // ====================================================================

        [Test]
        public void ApplyStarterKit_DoesNotGrantShowcaseMutations()
        {
            var player = CreateBlueprintPlayer();

            StartingLoadout.ApplyStarterKit(player, _harness.Factory);

            var mutations = player.GetPart<MutationsPart>();
            Assert.IsNotNull(mutations, "Player blueprint carries MutationsPart.");
            Assert.IsFalse(mutations.HasMutation("FireBoltMutation"),
                "Mortal start must not include the showcase FireBolt grant — " +
                "attack spells are learned from grimoires in the world.");
            Assert.IsFalse(mutations.HasMutation("ChainLightningMutation"),
                "Mortal start must not include ChainLightning.");
        }

        [Test]
        public void ApplyStarterKit_DoesNotGrantTonicPile()
        {
            // The debug loadout granted 8 assorted tonics. The kit grants
            // exactly 2 HealingTonics and nothing else from that pile.
            var player = CreateBlueprintPlayer();

            StartingLoadout.ApplyStarterKit(player, _harness.Factory);

            var inventory = player.GetPart<InventoryPart>();
            Assert.AreEqual(0, CountUnits(inventory, "Panacea"),
                "Mortal start has no Panacea.");
            Assert.AreEqual(0, CountUnits(inventory, "SpeedTonic"),
                "Mortal start has no SpeedTonic.");
        }

        // ====================================================================
        // The mortal statline (blueprint contract pins).
        // ====================================================================

        [Test]
        public void PlayerBlueprint_MortalHitpoints()
        {
            var player = CreateBlueprintPlayer();
            var hp = player.GetStat("Hitpoints");
            Assert.IsNotNull(hp);
            Assert.AreEqual(40, hp.BaseValue,
                "Mortal start: 40 HP. The 500 HP debug pool made every threat " +
                "response system in the game unreachable.");
            Assert.AreEqual(40, hp.Max,
                "Hitpoints Max must match (sweep C3: both fields on the stat).");
        }

        [Test]
        public void PlayerBlueprint_MortalAttributes()
        {
            var player = CreateBlueprintPlayer();
            Assert.AreEqual(16, player.GetStatValue("Strength"), "Mortal Strength 16.");
            Assert.AreEqual(16, player.GetStatValue("Agility"), "Mortal Agility 16.");
            Assert.AreEqual(16, player.GetStatValue("Toughness"), "Mortal Toughness 16.");
        }

        [Test]
        public void PlayerBlueprint_MortalPurse()
        {
            var player = CreateBlueprintPlayer();
            Assert.AreEqual(10, player.GetIntProperty("Drams", 0),
                "Mortal start: 10 drams — breakfast money, not a war chest.");
            Assert.AreEqual(50, player.GetIntProperty("Ink", 0),
                "Ink stays at 50 so the rental loop remains reachable before " +
                "the P1 ink faucet ships.");
        }
    }
}
