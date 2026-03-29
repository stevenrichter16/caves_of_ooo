using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class ProjectileMutationTests
    {
        [SetUp]
        public void Setup()
        {
            AsciiFxBus.Clear();
            MessageLog.Clear();
        }

        [Test]
        public void FlamingHands_Mutate_RegistersAdjacentAbilityMetadata()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.GetPart<MutationsPart>().AddMutation(new FlamingHandsMutation(), 1);

            ActivatedAbility ability = entity.GetPart<ActivatedAbilitiesPart>().AbilityList[0];

            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, ability.TargetingMode);
            Assert.AreEqual(1, ability.Range);
        }

        [Test]
        public void FireBolt_Mutate_RegistersDirectionalAbilityMetadata()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.GetPart<MutationsPart>().AddMutation(new FireBoltMutation(), 1);

            ActivatedAbility ability = entity.GetPart<ActivatedAbilitiesPart>().AbilityList[0];

            Assert.AreEqual("Fire Bolt", ability.DisplayName);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, ability.TargetingMode);
            Assert.AreEqual(6, ability.Range);
        }

        [Test]
        public void FrostNova_Mutate_RegistersSelfCenteredAbilityMetadata()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.GetPart<MutationsPart>().AddMutation(new FrostNovaMutation(), 1);

            ActivatedAbility ability = entity.GetPart<ActivatedAbilitiesPart>().AbilityList[0];

            Assert.AreEqual("Frost Nova", ability.DisplayName);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, ability.TargetingMode);
            Assert.AreEqual(2, ability.Range);
        }

        [Test]
        public void FireBolt_Cast_DamagesFirstCreature_AppliesBurning_AndEmitsProjectileFx()
        {
            var zone = new Zone("ProjectileZone");
            var caster = CreateCreatureWithMutationSupport();
            var firstTarget = CreateCreature("snapjaw", 20);
            var secondTarget = CreateCreature("goatfolk", 20);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(firstTarget, 7, 5);
            zone.AddEntity(secondTarget, 9, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new FireBoltMutation(), 1);
            var fireBolt = mutations.GetMutation<FireBoltMutation>();

            bool cast = fireBolt.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            Assert.Less(firstTarget.GetStatValue("Hitpoints", 20), 20);
            Assert.AreEqual(20, secondTarget.GetStatValue("Hitpoints", 20));
            Assert.IsTrue(firstTarget.HasEffect<BurningEffect>());

            var requests = AsciiFxBus.Drain();
            Assert.AreEqual(2, requests.Count);
            Assert.AreEqual(AsciiFxRequestType.Projectile, requests[0].Type);
            Assert.AreEqual(AsciiFxTheme.Fire, requests[0].Theme);
            Assert.AreEqual(2, requests[0].Path.Count);
            Assert.AreEqual(AsciiFxRequestType.AuraStart, requests[1].Type);
            Assert.AreEqual(AsciiFxTheme.Fire, requests[1].Theme);
        }

        [Test]
        public void PrismaticBeam_Cast_DamagesEveryCreatureBeforeWall_AndEmitsBeamSequence()
        {
            var zone = new Zone("BeamZone");
            var caster = CreateCreatureWithMutationSupport();
            var firstTarget = CreateCreature("snapjaw", 20);
            var secondTarget = CreateCreature("goatfolk", 20);
            var wall = CreateWall();

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(firstTarget, 7, 5);
            zone.AddEntity(secondTarget, 9, 5);
            zone.AddEntity(wall, 10, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new PrismaticBeamMutation(), 1);
            var beam = mutations.GetMutation<PrismaticBeamMutation>();

            bool cast = beam.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            Assert.Less(firstTarget.GetStatValue("Hitpoints", 20), 20);
            Assert.Less(secondTarget.GetStatValue("Hitpoints", 20), 20);

            var requests = AsciiFxBus.Drain();
            Assert.AreEqual(3, requests.Count);
            Assert.AreEqual(AsciiFxRequestType.ChargeOrbit, requests[0].Type);
            Assert.AreEqual(AsciiFxTheme.Arcane, requests[0].Theme);
            Assert.AreEqual(AsciiFxRequestType.Beam, requests[1].Type);
            Assert.AreEqual(5, requests[1].Path.Count);
            Assert.AreEqual(AsciiFxRequestType.Burst, requests[2].Type);
            Assert.AreEqual(10, requests[2].X);
            Assert.AreEqual(5, requests[2].Y);
        }

        [Test]
        public void IceShard_Cast_AppliesStun()
        {
            var zone = new Zone("ProjectileZone");
            var caster = CreateCreatureWithMutationSupport();
            var target = CreateCreature("snapjaw", 20);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new IceShardMutation(), 1);
            var iceShard = mutations.GetMutation<IceShardMutation>();

            bool cast = iceShard.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            Assert.IsTrue(target.HasEffect<StunnedEffect>());
        }

        [Test]
        public void PoisonSpit_Cast_AppliesPoison()
        {
            var zone = new Zone("ProjectileZone");
            var caster = CreateCreatureWithMutationSupport();
            var target = CreateCreature("snapjaw", 20);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new PoisonSpitMutation(), 1);
            var poisonSpit = mutations.GetMutation<PoisonSpitMutation>();

            bool cast = poisonSpit.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            Assert.IsTrue(target.HasEffect<PoisonedEffect>());
        }

        [Test]
        public void FrostNova_Cast_DamagesRadiusTwo_AndStunsOnlyRadiusOne()
        {
            var zone = new Zone("NovaZone");
            var caster = CreateCreatureWithMutationSupport();
            var nearTarget = CreateCreature("snapjaw", 20);
            var diagonalNearTarget = CreateCreature("goatfolk", 20);
            var farTarget = CreateCreature("baboon", 20);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(nearTarget, 5, 4);
            zone.AddEntity(diagonalNearTarget, 6, 6);
            zone.AddEntity(farTarget, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new FrostNovaMutation(), 1);
            var frostNova = mutations.GetMutation<FrostNovaMutation>();

            bool cast = frostNova.Cast(zone, zone.GetCell(5, 5), new Random(42));

            Assert.IsTrue(cast);
            Assert.Less(nearTarget.GetStatValue("Hitpoints", 20), 20);
            Assert.Less(diagonalNearTarget.GetStatValue("Hitpoints", 20), 20);
            Assert.Less(farTarget.GetStatValue("Hitpoints", 20), 20);
            Assert.IsTrue(nearTarget.HasEffect<StunnedEffect>());
            Assert.IsTrue(diagonalNearTarget.HasEffect<StunnedEffect>());
            Assert.IsFalse(farTarget.HasEffect<StunnedEffect>());

            var requests = AsciiFxBus.Drain();
            Assert.AreEqual(5, requests.Count);
            Assert.AreEqual(AsciiFxRequestType.ChargeOrbit, requests[0].Type);
            Assert.AreEqual(AsciiFxRequestType.RingWave, requests[1].Type);
        }

        [Test]
        public void ChainLightning_Cast_ChainsToNearestUniqueTargets()
        {
            var zone = new Zone("LightningZone");
            var caster = CreateCreatureWithMutationSupport();
            var primary = CreateCreature("snapjaw", 20);
            var firstSecondary = CreateCreature("goatfolk", 20);
            var secondSecondary = CreateCreature("baboon", 20);
            var ignored = CreateCreature("turret", 20);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(primary, 7, 5);
            zone.AddEntity(firstSecondary, 8, 6);
            zone.AddEntity(secondSecondary, 10, 6);
            zone.AddEntity(ignored, 14, 14);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ChainLightningMutation(), 1);
            var chainLightning = mutations.GetMutation<ChainLightningMutation>();

            bool cast = chainLightning.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            Assert.Less(primary.GetStatValue("Hitpoints", 20), 20);
            Assert.Less(firstSecondary.GetStatValue("Hitpoints", 20), 20);
            Assert.Less(secondSecondary.GetStatValue("Hitpoints", 20), 20);
            Assert.AreEqual(20, ignored.GetStatValue("Hitpoints", 20));
            Assert.IsTrue(primary.HasEffect<StunnedEffect>());
            Assert.IsFalse(firstSecondary.HasEffect<StunnedEffect>());

            var requests = AsciiFxBus.Drain();
            Assert.AreEqual(4, requests.Count);
            Assert.AreEqual(AsciiFxRequestType.ChainArc, requests[0].Type);
            Assert.AreEqual(4, requests[0].Path.Count);
            Assert.AreEqual(AsciiFxRequestType.Burst, requests[1].Type);
            Assert.AreEqual(AsciiFxRequestType.Burst, requests[2].Type);
            Assert.AreEqual(AsciiFxRequestType.Burst, requests[3].Type);
        }

        private static Entity CreateCreature(string name, int hp)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            return entity;
        }

        private static Entity CreateWall()
        {
            var entity = new Entity { BlueprintName = "Wall" };
            entity.Tags["Solid"] = "";
            entity.Tags["Wall"] = "";
            entity.AddPart(new RenderPart { DisplayName = "wall" });
            entity.AddPart(new PhysicsPart { Solid = true });
            return entity;
        }

        private static Entity CreateCreatureWithMutationSupport()
        {
            var entity = CreateCreature("caster", 20);
            entity.AddPart(new ActivatedAbilitiesPart());
            entity.AddPart(new MutationsPart());
            return entity;
        }
    }
}
