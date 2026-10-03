using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SnakeVenomSaveTests
    {
        const string Old = "Poisoned,75,1d6,8,0";
        const string Current = "Poisoned,75,1d2,4,0";
        static BodyPart Hand(Entity actor) => actor.GetPart<Body>().GetPartsByType("Hand")[0];
        static Entity OldFang(Entity actor)
        {
            var fang = Hand(actor)._DefaultBehavior;
            // Fresh in-code defaults have no ID until save hydration. Supply
            // the stable ID of an already-saved fang for the identity witness.
            fang.ID = Guid.NewGuid().ToString("N");
            fang.GetPart<MeleeWeaponPart>().OnHitEffectsRaw = Old;
            return fang;
        }

        [TestCase("Viper")]
        [TestCase("SpreadLatchcoil")]
        public void StockSavedFangsUpgradeInPlaceAndStayFixedAfterAnotherLoad(string blueprint)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create(blueprint); var fang = OldFang(actor); string id = fang.ID;
                var before = fang.GetPart<MeleeWeaponPart>().OnHitEffectsCachedSpecs;
                Assert.AreEqual("1d6", before.Single().DamageDice);
                actor.GetPart<Body>().OnAfterLoad(null);
                Assert.AreSame(fang, Hand(actor)._DefaultBehavior);
                Assert.AreEqual(Current, fang.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                Assert.AreEqual("1d2", fang.GetPart<MeleeWeaponPart>().OnHitEffectsCachedSpecs.Single().DamageDice);
                var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
                Assert.AreEqual(id, Hand(loaded)._DefaultBehavior.ID);
                Assert.AreEqual(Current, Hand(loaded)._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                loaded.GetPart<Body>().OnAfterLoad(null);
                Assert.AreEqual(Current, Hand(loaded)._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            }
        }

        [TestCase("Viper")]
        [TestCase("SpreadLatchcoil")]
        public void ActualOldPayloadRoundTripUsesTheBodyLoadHook(string blueprint)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create(blueprint); var fang = OldFang(actor);
                var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
                Assert.AreEqual(fang.ID, Hand(loaded)._DefaultBehavior.ID);
                Assert.AreNotSame(fang, Hand(loaded)._DefaultBehavior);
                Assert.AreEqual(Current, Hand(loaded)._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                Assert.AreEqual(Old, fang.GetPart<MeleeWeaponPart>().OnHitEffectsRaw,"Source save graph stays unchanged.");
            }
        }

        [TestCase("raw")][TestCase("base")][TestCase("penetration")][TestCase("hit")]
        [TestCase("strength")][TestCase("stat")][TestCase("attributes")][TestCase("gas")]
        [TestCase("name")][TestCase("color")][TestCase("tag")][TestCase("recipe")][TestCase("actor")]
        [TestCase("extra-part")]
        public void CustomizedOrUnrelatedFangsRemainLiteral(string change)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("Viper"); var fang = OldFang(actor); var weapon = fang.GetPart<MeleeWeaponPart>();
                switch(change)
                {
                    case "raw": weapon.OnHitEffectsRaw = "Poisoned,75,2d6,8,0"; break;
                    case "base": weapon.BaseDamage = "2d3"; break;
                    case "penetration": weapon.PenBonus = 2; break;
                    case "hit": weapon.HitBonus = 1; break;
                    case "strength": weapon.MaxStrengthBonus = 2; break;
                    case "stat": weapon.Stat = "Agility"; break;
                    case "attributes": weapon.Attributes = "Piercing Animal Fire"; break;
                    case "gas": weapon.EmitGasOnHitRaw = "poison-vapor,25"; break;
                    case "name": fang.GetPart<RenderPart>().DisplayName = "custom fangs"; break;
                    case "color": fang.GetPart<RenderPart>().ColorString = "&r"; break;
                    case "tag": fang.Tags.Remove("Natural"); break;
                    case "recipe": Hand(actor).DefaultBehaviorBlueprint = "CustomFangs"; break;
                    case "actor": actor.BlueprintName = "CustomSnake"; break;
                    case "extra-part": fang.AddPart(new ExaminablePart()); break;
                }
                string raw=weapon.OnHitEffectsRaw;
                actor.GetPart<Body>().OnAfterLoad(null);
                Assert.AreSame(fang,Hand(actor)._DefaultBehavior);
                Assert.AreEqual(raw,weapon.OnHitEffectsRaw);
                var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
                Assert.AreEqual(raw,Hand(loaded)._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            }
        }

        [Test]
        public void PreexistingActivePoisonAndOtherSpeciesStayLiteral()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor=f.Create("Viper"); OldFang(actor);
                actor.ApplyEffect(new PoisonedEffect(17,"1d6",new Random(7)));
                var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
                Assert.AreEqual(Current,Hand(loaded)._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                Assert.AreEqual(17,loaded.GetEffect<PoisonedEffect>().Duration);
                Assert.AreEqual("1d6",loaded.GetEffect<PoisonedEffect>().DamageDice);
                var scorpion=f.Create("Scorpion"); string raw=Hand(scorpion)._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw;
                var other=PartRoundTripHelper.RoundTripEntityViaTokenGraph(scorpion);
                Assert.AreEqual(raw,Hand(other)._DefaultBehavior.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            }
        }
    }
}
