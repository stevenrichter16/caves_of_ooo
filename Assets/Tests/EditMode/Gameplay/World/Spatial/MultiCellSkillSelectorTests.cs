using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    /// <summary>Selector migration by player-visible pattern, with physical
    /// edge/hole controls and safe continuation after world mutation.</summary>
    public sealed class MultiCellSkillSelectorTests
    {
        private static EntityFactory _factory;
        private EntityFactory _oldMaterialFactory, _oldWallFactory;
        [OneTimeSetUp] public void LoadNativeBlueprints()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            MessageLog.Clear(); Diag.ResetAll();
            _oldMaterialFactory = MaterialReactionResolver.Factory;
            _oldWallFactory = Cryomancy_GlacialWall.Factory;
            MaterialReactionResolver.Factory = _factory;
            Cryomancy_GlacialWall.Factory = _factory;
        }
        [TearDown] public void Restore()
        {
            MaterialReactionResolver.Factory = _oldMaterialFactory;
            Cryomancy_GlacialWall.Factory = _oldWallFactory;
        }
        private static BaseSkillPart Skill(string name)
            => (BaseSkillPart)Activator.CreateInstance(typeof(BaseSkillPart).Assembly.GetType("CavesOfOoo.Skills." + name, true));
        private static MultiCellAbilityProbePart Probe(Entity e) => e.GetPart<MultiCellAbilityProbePart>();
        private static IEnumerable<TestCaseData> AdjacentCases()
        {
            foreach (var skill in new[] { "Cryomancy_Frostbind", "Axe_RendArmor", "Axe_HookAndDrag",
                "ShortBlades_Flurry", "ShortBlades_Shank", "Cudgel_Disarm", "Cudgel_Slam", "ShortBlades_Backstab" })
                foreach (var mode in new[] { "target-body", "caster-body", "ordinary-distant" })
                    yield return new TestCaseData(skill, mode);
        }
        [TestCaseSource(nameof(AdjacentCases))]
        public void AdjacentActive_UsesPhysicalContactWithoutChangingWeaponGate(string skill, string mode)
        {
            var z = new Zone(); bool sourceBody = mode == "caster-body";
            var actor = F.ArmedActor("Piercing Axe Cudgel", sourceBody ? "0,0;1,0;2,0" : null);
            var target = F.ArmedActor("Piercing", mode == "target-body" ? "0,0;1,0;2,0" : null);
            F.Place(z, actor, sourceBody ? 7 : 10, 10); F.Place(z, target, sourceBody ? 10 : 7, 10);
            Assert.AreEqual(mode != "ordinary-distant", Skill(skill).OnCommand(F.Context(z, actor)));
            if (mode == "ordinary-distant")
            {
                Assert.AreEqual(1000, target.GetStatValue("Hitpoints"));
                Assert.IsFalse(target.HasEffect<RootedEffect>() || target.HasEffect<HookedEffect>()
                    || target.HasEffect<ShatterArmorEffect>() || target.HasEffect<StunnedEffect>());
            }
            else if (skill == "Cryomancy_Frostbind") Assert.IsTrue(target.HasEffect<RootedEffect>());
            else if (skill == "Axe_RendArmor") Assert.IsTrue(target.HasEffect<ShatterArmorEffect>());
            else if (skill == "Axe_HookAndDrag") Assert.IsTrue(target.HasEffect<HookedEffect>());
            else if (skill == "Cudgel_Disarm") Assert.IsNull(target.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>());
            else if (skill == "Cudgel_Slam") Assert.IsTrue(target.HasEffect<StunnedEffect>());
            else Assert.Greater(Probe(target).DamageCalls, 0);
        }

        [TestCase("Pyromancy_FlamingHands", true)] [TestCase("Pyromancy_FlamingHands", false)]
        [TestCase("Pyromancy_KindleFlame", true)] [TestCase("Pyromancy_KindleFlame", false)]
        [TestCase("Pyromancy_Hearthwarm", true)] [TestCase("Pyromancy_Hearthwarm", false)]
        public void ChosenCellSpell_ActsAtRemoteBodyAndAuraActuallyPulses(string spell, bool body)
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var target = F.Owner(body ? "0,0;1,0" : null);
            target.AddPart(new ThermalPart { FlameTemperature = 100 }); F.Place(z, target, 8, 10);
            var context = F.Context(z, actor); context.TargetCell = z.GetCell(9, 10);
            Assert.AreEqual(body || spell == "Pyromancy_FlamingHands", Skill(spell).OnCommand(context));
            if (spell == "Pyromancy_Hearthwarm" && body)
            {
                var pulse = GameEvent.New("BeginTakeAction"); pulse.SetParameter("Zone", (object)z);
                try { actor.GetEffect<HearthAuraEffect>().OnTurnStart(actor, pulse); }
                finally { pulse.Release(); }
            }
            Assert.AreEqual(body ? 1 : 0, Probe(target).DirectHeatCalls);
        }

        [TestCase("Pyromancy_FlamingHands")] [TestCase("Pyromancy_KindleFlame")] [TestCase("Pyromancy_Hearthwarm")]
        public void ChosenCellSpell_EmptyAnchorHoleDoesNotSupplyThermalTarget(string spell)
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var target = F.Owner("-1,0"); target.AddPart(new ThermalPart { FlameTemperature = 100 }); F.Place(z, target, 9, 10);
            var context = F.Context(z, actor); context.TargetCell = z.GetCell(9, 10);
            Assert.AreEqual(spell == "Pyromancy_FlamingHands", Skill(spell).OnCommand(context));
            Assert.AreEqual(0, Probe(target).DirectHeatCalls);
            Assert.IsFalse(actor.HasEffect<HearthAuraEffect>());
        }

        [Test] public void Kindle_SnapshotDoesNotLoseOrRepeatOwnersWhenFirstHeatMovesTheStack()
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var later = F.Owner(); later.AddPart(new ThermalPart { FlameTemperature = 100 }); F.Place(z, later, 11, 10);
            var first = F.Owner(); first.AddPart(new ThermalPart { FlameTemperature = 100 }); F.Place(z, first, 11, 10);
            Probe(first).OnHeat = () => { z.RemoveEntity(first); z.MoveEntity(later, 15, 10); };
            var context = F.Context(z, actor); context.TargetCell = z.GetCell(11, 10);
            Assert.IsTrue(new Pyromancy_KindleFlame().OnCommand(context));
            Assert.AreEqual(1, Probe(first).DirectHeatCalls); Assert.AreEqual(1, Probe(later).DirectHeatCalls);
        }

        [TestCase(true)] [TestCase(false)]
        public void ConjureWater_PrioritizesBurningPhysicalEdgeOverFurtherEmptyCell(bool body)
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var target = F.Owner(body ? "0,0;0,1" : null, true); F.Place(z, target, 11, 9);
            target.ApplyEffect(new BurningEffect(rng: new Random(1)), actor, z);
            Assert.IsTrue(new Hydromancy_ConjureWater().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(body, target.HasEffect<WetEffect>());
            Assert.IsTrue(z.GetCell(body ? 11 : 12, 10).Objects.Any(e => e.BlueprintName == "WaterPuddle"));
        }

        [TestCase(true)] [TestCase(false)]
        public void DrenchLob_StopsAtFirstPhysicalBodyInsteadOfFlyingPastItsSurface(bool body)
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 5, 10);
            var target = F.Owner(body ? "0,0;0,1" : null, true); F.Place(z, target, 8, 9);
            var witness = F.Owner(null, true); F.Place(z, witness, 6, 11);
            Assert.AreEqual(body, new Hydromancy_DrenchLob().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(body, target.HasEffect<WetEffect>()); Assert.AreEqual(body, witness.HasEffect<WetEffect>());
        }

        [Test] public void BacklashCoil_HitsOneLargeOwnerOnceAndRetainsItsTwoCellPush()
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var target = F.Owner("0,0;1,0;1,1", true); F.Place(z, target, 8, 9);
            Assert.IsTrue(new Galvanism_BacklashCoil().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(996, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, Probe(target).DamageCalls);
            Assert.Greater(SpatialQuery.Distance(z, actor, target), 1);
            Assert.IsFalse(target.HasEffect<ElectrifiedEffect>());
        }

        [TestCase(true)] [TestCase(false)]
        public void Rain_WatersOnePhysicalCropOwnerAndReportsOneOwnerPerCast(bool body)
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var crop = F.Owner(body ? "0,0;1,0;2,0;3,0" : null); crop.AddPart(new CropPart()); F.Place(z, crop, 6, 10);
            Assert.IsTrue(new Hydromancy_ConjureRain().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(body ? 40 : 0, crop.GetPart<CropPart>().MoistureTicks);
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "crop", Kind = "CropWatered", Limit = 50 }).Records;
            Assert.AreEqual(body ? 1 : 0, records.Count(e => e.TargetId == crop.ID));
            Assert.IsTrue(new Hydromancy_ConjureRain().OnCommand(F.Context(z, actor)));
            records = DiagQuery.Apply(new DiagQuery.Filter { Category = "crop", Kind = "CropWatered", Limit = 50 }).Records;
            Assert.AreEqual(body ? 2 : 0, records.Count(e => e.TargetId == crop.ID));
        }

        [TestCase(true)] [TestCase(false)]
        public void GlacialWall_DoesNotBuryCreatureBodyOutsideItsAnchor(bool body)
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var target = F.Owner(body ? "0,0;0,1" : null, true); F.Place(z, target, 11, 9);
            Assert.IsTrue(new Cryomancy_GlacialWall().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(!body, z.GetCell(11, 10).Objects.Any(e => e.BlueprintName == "IceWall"));
            Assert.IsTrue(z.GetCell(12, 10).Objects.Any(e => e.BlueprintName == "IceWall"));
        }

        [TestCase("Acrobatics_Vault")] [TestCase("ShortBlades_Disengage")] [TestCase("Cudgel_ChargingStrike")]
        public void Mobility_DoesNotReportOrAdvancePastRejectedFarCorner(string skill)
        {
            var z = new Zone(); var actor = F.ArmedActor("Piercing Cudgel", "0,0;0,1"); F.Place(z, actor, 10, 10);
            int blockX = skill == "Acrobatics_Vault" ? 12 : 11;
            var blocker = F.Owner(); blocker.GetPart<PhysicsPart>().Solid = true; F.Place(z, blocker, blockX, 11);
            var farEnemy = F.Owner(null, true); F.Place(z, farEnemy, 13, 10);
            bool result = Skill(skill).OnCommand(F.Context(z, actor));
            Assert.AreEqual((10, 10), z.GetEntityPosition(actor));
            Assert.AreEqual(0, Probe(farEnemy).DamageCalls);
            if (skill == "Acrobatics_Vault") Assert.IsFalse(result);
            Assert.IsFalse(MessageLog.GetRecent(20).Any(s => s.Contains("vaults forward") || s.Contains("disengages 3")));
        }

        [TestCase("Acrobatics_Vault", true)] [TestCase("Acrobatics_Vault", false)]
        [TestCase("ShortBlades_Disengage", true)] [TestCase("ShortBlades_Disengage", false)]
        public void Mobility_RecognizesRemoteCreatureAtLandingOrNextCell(string skill, bool body)
        {
            var z = new Zone(); var actor = F.ArmedActor("Piercing"); F.Place(z, actor, 10, 10);
            int targetX = skill == "Acrobatics_Vault" ? 12 : 11;
            var target = F.Owner(body ? "0,0;0,1" : null, true); F.Place(z, target, targetX, 9);
            bool cast = Skill(skill).OnCommand(F.Context(z, actor));
            int expectedX = body ? 10 : skill == "Acrobatics_Vault" ? 12 : 13;
            Assert.AreEqual((expectedX, 10), z.GetEntityPosition(actor));
            if (skill == "Acrobatics_Vault") Assert.AreEqual(!body, cast);
        }

        [TestCase(true)] [TestCase(false)]
        public void ChargingStrike_AttacksFirstRemoteBodyAndStopsAtRealContact(bool body)
        {
            var z = new Zone(); var actor = F.ArmedActor("Cudgel"); F.Place(z, actor, 10, 10);
            var target = F.Owner(body ? "0,0;0,1" : null, true); F.Place(z, target, 12, 9);
            Assert.AreEqual(body, new Cudgel_ChargingStrike().OnCommand(F.Context(z, actor)));
            Assert.AreEqual((body ? 11 : 13, 10), z.GetEntityPosition(actor));
            Assert.AreEqual(body, Probe(target).DamageCalls > 0);
        }

        [TestCase(true)] [TestCase(false)]
        public void HookedPull_StopsAtPhysicalAdjacencyInsteadOfPullingBodyAcrossHooker(bool body)
        {
            var z = new Zone(); var actor = F.Owner(null, true); F.Place(z, actor, 10, 10);
            var target = F.Owner(body ? "0,0;1,0;2,0" : null, true); F.Place(z, target, 7, 10);
            var effect = new HookedEffect(hooker: actor, saveTarget: 100, rng: new Random(17));
            var pulse = GameEvent.New("EndTurn"); pulse.SetParameter("Zone", (object)z);
            try { effect.OnTurnEnd(target, pulse); } finally { pulse.Release(); }
            Assert.AreEqual((body ? 7 : 8, 10), z.GetEntityPosition(target));
        }

        [TestCase(true)] [TestCase(false)]
        public void Disarm_RefusesUnplaceableFootprintWeaponBeforeChangingEquipment(bool body)
        {
            var z = new Zone(); var actor = F.ArmedActor("Cudgel"); F.Place(z, actor, 10, 10);
            var target = F.ArmedActor("Piercing"); target.GetPart<PhysicsPart>().Solid = true; F.Place(z, target, 11, 10);
            var weapon = target.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>();
            if (body) weapon.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            Assert.AreEqual(!body, new Cudgel_Disarm().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(body ? weapon : null, target.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>());
            Assert.AreEqual(body, z.GetEntityCell(weapon) == null);
        }

        [TestCase(true)] [TestCase(false)]
        public void Backstab_FlankerMustStandBeyondTheWholeTargetBody(bool flankerBody)
        {
            var z = new Zone(); var actor = F.ArmedActor("Piercing"); F.Place(z, actor, 10, 10);
            var target = F.Owner("0,0;1,0;2,0", true); F.Place(z, target, 11, 10);
            var flanker = F.Owner(flankerBody ? "0,0;0,1" : null, true); F.Place(z, flanker, 14, 9);
            Assert.IsTrue(new ShortBlades_Backstab().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(flankerBody ? 2 : 1, Probe(target).DamageCalls,
                "A second target body cell is not a flanker; a remote allied body edge beyond it is.");
        }

        [Test] public void Flurry_StopsWhenFirstStrikeRemovesTheLivingTarget()
        {
            var z = new Zone(); var actor = F.ArmedActor("Piercing"); F.Place(z, actor, 10, 10);
            var target = F.Owner(null, true); F.Place(z, target, 11, 10);
            Probe(target).OnDamage = () => z.RemoveEntity(target);
            Assert.IsTrue(new ShortBlades_Flurry().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(1, Probe(target).DamageCalls);
        }

        [Test] public void Flurry_RetainsThreeIntentionalStrikesAgainstOneFootprintOwner()
        {
            var z = new Zone(); var actor = F.ArmedActor("Piercing"); F.Place(z, actor, 10, 10);
            var target = F.Owner("0,0;1,0;2,0", true); F.Place(z, target, 7, 10);
            Assert.IsTrue(new ShortBlades_Flurry().OnCommand(F.Context(z, actor)));
            Assert.AreEqual(3, Probe(target).DamageCalls);
        }
    }
}
