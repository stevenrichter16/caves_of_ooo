using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MaterialFieldTests : FiftyWorldFixture
    {
        List<TileReactionSystem.TileReaction> previousReactions;
        bool previousInitialized;
        static readonly FieldInfo Reactions = typeof(TileReactionSystem).GetField("_reactions", BindingFlags.NonPublic | BindingFlags.Static);
        static readonly FieldInfo Initialized = typeof(TileReactionSystem).GetField("<IsInitialized>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        [SetUp] public void SetUpMaterials()
        {
            previousReactions = new List<TileReactionSystem.TileReaction>((List<TileReactionSystem.TileReaction>)Reactions.GetValue(null));
            previousInitialized = TileReactionSystem.IsInitialized;
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
            foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true;
            MessageLog.Clear(); Diag.ResetAll();
        }
        [TearDown] public void RestoreMaterials()
        {
            TileReactionSystem.ResetForTests();
            ((List<TileReactionSystem.TileReaction>)Reactions.GetValue(null)).AddRange(previousReactions);
            Initialized.SetValue(null, previousInitialized);
        }
        static void Stack(Entity item, int count)
        { if (!item.HasPart<StackerPart>()) item.AddPart(new StackerPart()); item.GetPart<StackerPart>().StackCount = count; }
        bool Offered(Entity item, string verb) => Actions(item).Any(a => a.Command.StartsWith("MaterialField|" + verb + "|", StringComparison.Ordinal));
        string Select(Entity item, string verb, Entity target = null, string layer = "", int x = 11, int y = 10)
            => Choice(item, "MaterialField|" + verb + "|", a => a.Command.Split('|')[5] == x.ToString()
                && a.Command.Split('|')[6] == y.ToString() && a.Command.Split('|')[7] == (target == null ? "" : Id(target))
                && a.Command.Split('|')[9] == Uri.EscapeDataString(layer));
        Entity HotTarget()
        {
            var target = Place("MarlbackScrabbler"); var thermal = target.GetPart<ThermalPart>();
            thermal.FlameTemperature = 400; thermal.HeatCapacity = 1.6f;
            target.ApplyEffect(new BurningEffect(1)); thermal.Temperature = 500;
            return target;
        }
        Entity FrozenTarget()
        {
            var target = Place("MarlbackScrabbler"); target.GetPart<ThermalPart>().Temperature = -10;
            Assert.True(target.ApplyEffect(new FrozenEffect(.5f))); return target;
        }

        [Test] public void FireMossKindlesRealDrySceneryThroughItsThermalSystem()
        {
            var item = Carry("FireMoss"); var barrel = Place("WoodenBarrel"); float prior = barrel.GetPart<ThermalPart>().Temperature;
            Assert.True(Act(item, Select(item, "Kindle", barrel)));
            Assert.Greater(barrel.GetPart<ThermalPart>().Temperature, prior); Assert.True(barrel.HasEffect<BurningEffect>());
            Assert.False(Pack.Objects.Contains(item));
        }
        [TestCase("StoneWall")] [TestCase("MarlbackScrabbler")]
        public void FireMossDoesNotOfferStoneOrCreatureIgnition(string name)
        { var item = Carry("FireMoss"); Place(name); Assert.False(Offered(item, "Kindle")); }
        [Test] public void WetFuelAndEmptyGroundDoNotSpendAnIgnitionSupply()
        {
            var item = Carry("FireMoss"); Assert.False(Offered(item, "Kindle"));
            var barrel = Place("WoodenBarrel"); Assert.True(Offered(item, "Kindle"));
            barrel.ApplyEffect(new WetEffect(1)); Assert.False(Offered(item, "Kindle")); Assert.Contains(item, Pack.Objects);
        }
        [Test] public void FireMossIgnitesAnActualOilFilmAndRetainsItsOrdinaryReactionRisk()
        {
            var item = Carry("FireMoss"); Zone.TileState.WriteCoating(11, 10, "oil", 8);
            Assert.True(Act(item, Select(item, "Kindle", layer:"oil")));
            Assert.False(Zone.TileState.HasCoating(11, 10, "oil")); Assert.True(Zone.TileState.HasResidue(11, 10, "embers"));
        }
        [Test] public void FrostLichenFreezesWaterAndItsOccupantButNeverConjuresIceOnDryGround()
        {
            var item = Carry("FrostLichen"); Assert.False(Offered(item, "Freeze"));
            var friend = Place("MarlbackScrabbler"); Assert.True(friend.GetPart<BrainPart>().SetPartyLeader(Actor));
            Zone.TileState.WriteCoating(11, 10, "water", 5);
            Assert.True(Act(item, Select(item, "Freeze", layer:"water")));
            Assert.True(Zone.TileState.HasCoating(11, 10, "ice")); Assert.False(Zone.TileState.HasCoating(11, 10, "water"));
            Assert.True(friend.HasEffect<FrozenEffect>()); Assert.AreEqual(4, Zone.TileState.CoatingTurns(11, 10, "ice"));
        }
        [Test] public void GlacierSaltActuallyCoolsAHotTargetAndCanCrossItsExtinguishThreshold()
        {
            var target = HotTarget(); var item = Carry("GlacierSalt");
            Assert.True(Act(item, Select(item, "Cool", target)));
            Assert.AreEqual(312.5f, target.GetPart<ThermalPart>().Temperature, .001f);
            Assert.False(target.HasEffect<BurningEffect>());
        }
        [Test] public void ColdPackRefusesAmbientTargetsAndMayFreezeAnOnlyModeratelyHotBody()
        {
            var target = Place("MarlbackScrabbler"); var item = Carry("GlacierSalt");
            Assert.False(Offered(item, "Cool")); target.GetPart<ThermalPart>().Temperature = 80;
            Assert.True(Act(item, Select(item, "Cool", target))); Assert.True(target.HasEffect<FrozenEffect>());
        }
        [Test] public void WarmPulpThawsThroughActualHeatAndDoesNotDispelUnrelatedControl()
        {
            var target = FrozenTarget(); var freeze = target.GetEffect<FrozenEffect>(); var root = new RootedEffect(3);
            Assert.True(target.ApplyEffect(root)); var item = Carry("EmberFruit");
            Assert.True(Act(item, Select(item, "Warm", target))); Assert.Less(freeze.Cold, .5f);
            Assert.Greater(target.GetPart<ThermalPart>().Temperature, -10); Assert.AreSame(root, target.GetEffect<RootedEffect>());
        }
        [Test] public void WarmPulpRefusesAnUnfrozenRecipient()
        { var item = Carry("EmberFruit"); Place("MarlbackScrabbler"); Assert.False(Offered(item, "Warm")); }

        [TestCase("GlimmerBrine", "brine")]
        [TestCase("LampOil", "oil")]
        [TestCase("SlipsedgeGel", "gel")]
        public void MaterialFilmIsFiniteSavedLocalAndNeverMintsAPool(string blueprint, string liquid)
        {
            var item = Carry(blueprint); Stack(item, 3);
            Assert.True(Act(item, Select(item, "Film", layer:liquid))); Assert.AreEqual(2, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(8, Zone.TileState.CoatingTurns(11, 10, liquid)); Assert.False(Zone.TileState.HasCoating(10, 10, liquid));
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.HasPart<LiquidPoolPart>()));
            string save = Zone.TileState.ToSaveString(); Zone.TileState.Clear(11, 10); Zone.TileState.LoadFromString(save);
            Assert.AreEqual(8, Zone.TileState.CoatingTurns(11, 10, liquid));
            for (int i = 0; i < 8; i++) Zone.TileState.Tick(); Assert.False(Zone.TileState.HasCoating(11, 10, liquid));
        }
        [Test] public void BrineAndGelConductWhileOnlyOilIsFlammable()
        {
            foreach (var pair in new[] { ("GlimmerBrine", "brine", 11, 10), ("SlipsedgeGel", "gel", 10, 11), ("LampOil", "oil", 9, 10) })
            {
                var item = Carry(pair.Item1); Assert.True(Act(item, Select(item, "Film", layer:pair.Item2, x:pair.Item3, y:pair.Item4)));
                Assert.AreEqual(pair.Item2 != "oil", TilePropagationSystem.IsConductive(Zone, pair.Item3, pair.Item4));
                Assert.AreEqual(pair.Item2 == "oil", TilePropagationSystem.IsFlammable(Zone, pair.Item3, pair.Item4));
            }
            Assert.True(LiquidRegistry.Get("gel").Slippery); Assert.False(LiquidRegistry.Get("brine").Slippery);
        }
        [Test] public void SparkRootUsesTheRealConductiveNetworkInsteadOfAnInventedRadiusAttack()
        {
            var item = Carry("SparkRoot"); Assert.False(Offered(item, "Charge"));
            Zone.TileState.WriteCoating(11, 10, "brine", 8); Zone.TileState.WriteCoating(12, 10, "water", 8);
            var target = Place("MarlbackScrabbler", 12, 10); int hp = target.GetStatValue("Hitpoints");
            var dry = Place("MarlbackScrabbler", 11, 11); int dryHp = dry.GetStatValue("Hitpoints");
            Assert.True(Act(item, Select(item, "Charge")));
            Assert.Less(target.GetStatValue("Hitpoints"), hp); Assert.True(target.HasEffect<ElectrifiedEffect>());
            Assert.AreEqual(dryHp, dry.GetStatValue("Hitpoints")); Assert.False(dry.HasEffect<ElectrifiedEffect>());
        }
        [Test] public void PithRemovesExactlyOneThinFilmAndPreservesOtherGroundHazards()
        {
            var item = Carry("PrismreedPith"); Zone.TileState.WriteCoating(11, 10, "oil", 4); Zone.TileState.WriteCoating(11, 10, "water", 6);
            Zone.TileState.WriteResidue(11, 10, "embers", 3); Zone.TileState.WriteCloud(11, 10, "smoke", 3);
            Assert.True(Act(item, Select(item, "Wick", layer:"oil")));
            Assert.False(Zone.TileState.HasCoating(11, 10, "oil")); Assert.True(Zone.TileState.HasCoating(11, 10, "water"));
            Assert.True(Zone.TileState.HasResidue(11, 10, "embers")); Assert.AreEqual("smoke", Zone.TileState.Get(11, 10).Cloud);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.HasPart<LiquidPoolPart>()));
        }
        [TestCase("pool")] [TestCase("renewing")] [TestCase("permanent")] [TestCase("ice")] [TestCase("lava")]
        public void PithCannotDrainPermanentSourcesOrAbsorbSolidIceAndLava(string blocked)
        {
            var item = Carry("PrismreedPith"); string liquid = blocked == "ice" || blocked == "lava" ? blocked : "water";
            Zone.TileState.WriteCoating(11, 10, liquid, blocked == "permanent" ? ZoneTileState.Permanent : 5);
            if (blocked == "pool") Pool(5, "water");
            if (blocked == "renewing") { var source = Place("Grass"); source.AddPart(new TileStateSourcePart { Coating = "water", CoatingTurns = 3 }); }
            Assert.False(Offered(item, "Wick")); Assert.Contains(item, Pack.Objects);
        }
        [TestCase("PitchpodResin", "pitch", 28, 4)] [TestCase("Honeycomb", "honey", 22, 2)]
        public void StickySuppliesApplyTheirRealTemporaryCombatPenaltiesAndProvokeOutsiders(string blueprint, string liquid, int amount, int drySteps)
        {
            var target = Place("MarlbackScrabbler"); int dv = target.GetStatValue("DV"), agi = target.GetStatValue("Agility");
            var item = Carry(blueprint); Assert.True(Act(item, Select(item, "Coat", target, liquid)));
            var coat = target.GetEffect<LiquidCoveredEffect>(); Assert.NotNull(coat); Assert.AreEqual(liquid, coat.LiquidId); Assert.AreEqual(amount, coat.Amount);
            Assert.AreEqual(dv - 3, target.GetStatValue("DV")); Assert.AreEqual(agi - 2, target.GetStatValue("Agility"));
            Assert.True(target.GetPart<BrainPart>().IsPersonallyHostileTo(Actor)); Assert.False(target.HasEffect<RootedEffect>());
            for (int i = 0; i < drySteps; i++) coat.OnTurnEnd(target);
            Assert.AreEqual(0, coat.Amount); target.GetPart<StatusEffectsPart>().RemoveEffect(coat);
            Assert.AreEqual(dv, target.GetStatValue("DV")); Assert.AreEqual(agi, target.GetStatValue("Agility"));
        }
        [TestCase("PitchpodResin")] [TestCase("Honeycomb")]
        public void ExistingCoatIsNeverOverwrittenByAnImprovisedSmear(string blueprint)
        {
            var target = Place("MarlbackScrabbler"); target.ApplyEffect(new LiquidCoveredEffect("water", 12));
            var item = Carry(blueprint);
            Assert.False(Actions(item).Any(a => a.Command.StartsWith("MaterialField|Coat|", StringComparison.Ordinal) && a.Command.Split('|')[7] == Id(target)));
            Assert.True(Actions(item).Any(a => a.Command.StartsWith("MaterialField|Coat|", StringComparison.Ordinal) && a.Command.Split('|')[7] == Id(Actor)), "The uncoated user remains a separate valid recipient.");
            Assert.AreEqual("water", target.GetEffect<LiquidCoveredEffect>().LiquidId);
        }

        [TestCase("FireMoss", "Kindle")] [TestCase("FrostLichen", "Freeze")]
        [TestCase("GlacierSalt", "Cool")] [TestCase("EmberFruit", "Warm")]
        [TestCase("GlimmerBrine", "Film")] [TestCase("SparkRoot", "Charge")]
        [TestCase("PrismreedPith", "Wick")] [TestCase("LampOil", "Film")]
        [TestCase("SlipsedgeGel", "Film")] [TestCase("PitchpodResin", "Coat")] [TestCase("Honeycomb", "Coat")]
        public void OuterFailureLeavesNoFreeMaterialEffectOrSpentSupply(string blueprint, string verb)
        {
            Entity target = null; string layer = "";
            if (blueprint == "FireMoss") target = Place("WoodenBarrel");
            if (blueprint == "GlacierSalt") target = HotTarget();
            if (blueprint == "EmberFruit") target = FrozenTarget();
            if (blueprint == "FrostLichen" || blueprint == "SparkRoot" || blueprint == "PrismreedPith") { Zone.TileState.WriteCoating(11, 10, "water", 6); layer = blueprint == "SparkRoot" ? "" : "water"; }
            if (blueprint == "GlimmerBrine") layer = "brine";
            if (blueprint == "LampOil") layer = "oil";
            if (blueprint == "SlipsedgeGel") layer = "gel";
            if (blueprint == "PitchpodResin" || blueprint == "Honeycomb") { target = Place("MarlbackScrabbler"); layer = blueprint == "PitchpodResin" ? "pitch" : "honey"; }
            var item = Carry(blueprint); string command = Select(item, verb, target, layer);
            string tiles = Zone.TileState.ToSaveString(); float temperature = target?.GetPart<ThermalPart>()?.Temperature ?? 0;
            var effects = target?.GetPart<StatusEffectsPart>()?.GetAllEffects().ToArray() ?? Array.Empty<Effect>();
            int dv = target?.GetStatValue("DV") ?? 0;
            FailAfter(); Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects);
            Assert.AreSame(Actor, item.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(tiles, Zone.TileState.ToSaveString());
            Assert.AreEqual(temperature, target?.GetPart<ThermalPart>()?.Temperature ?? 0);
            CollectionAssert.AreEqual(effects, target?.GetPart<StatusEffectsPart>()?.GetAllEffects().ToArray() ?? Array.Empty<Effect>());
            Assert.AreEqual(dv, target?.GetStatValue("DV") ?? 0);
            Assert.False(DiagQuery.Apply(new DiagQuery.Filter { Category="event", Kind="MaterialFieldUsed" }).Records.Any());
        }
    }
}
