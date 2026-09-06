using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A45: actual paid modifications survive rebuilding a weapon's components.</summary>
    public abstract class ReforgeModsFixture : StackIdentityFixture
    {
        protected Entity GiveLive(Entity actor, string bp)
        {
            var made = Give(actor, bp); var inv = actor.GetPart<InventoryPart>();
            return inv.Objects.Contains(made) ? made : inv.Objects.Single(i => i.BlueprintName == bp);
        }
        protected Entity Forge(Entity actor)
        {
            var blade = GiveLive(actor, "SteelBladeComponent"); var haft = GiveLive(actor, "OakHaftComponent"); var binding = GiveLive(actor, "LeatherBindingComponent");
            Assert.IsTrue(WeaponForgingService.TryForge(actor, Factory, blade, haft, binding, out var weapon, out var reason), reason);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(weapon)); Assert.AreEqual(1, weapon.GetPart<StackerPart>().StackCount);
            return weapon;
        }
        protected void Pay(Entity actor, Entity weapon, string recipeId)
        {
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe(recipeId, out var recipe)); var bits = actor.GetPart<BitLockerPart>();
            bits.LearnRecipe(recipeId); bits.AddBits(recipe.Cost);
            int ingredientBefore = 0;
            if (!string.IsNullOrEmpty(recipe.Ingredient))
            {
                GiveLive(actor, recipe.Ingredient);
                ingredientBefore = actor.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == recipe.Ingredient).Sum(i => i.GetPart<StackerPart>().StackCount);
            }
            int b = bits.GetBitCount('B'), c = bits.GetBitCount('C');
            Assert.IsTrue(TinkeringService.TryApplyModification(actor, recipeId, weapon, out var why), why);
            if (!string.IsNullOrEmpty(recipe.Ingredient))
                Assert.AreEqual(ingredientBefore - 1, actor.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == recipe.Ingredient).Sum(i => i.GetPart<StackerPart>().StackCount), "actual mineral paid exactly once");
            if (recipeId == "mod_sharp_melee") { Assert.AreEqual(b - 1, bits.GetBitCount('B')); Assert.AreEqual(c - 1, bits.GetBitCount('C')); }
            else { Assert.AreEqual(b, bits.GetBitCount('B')); Assert.AreEqual(c, bits.GetBitCount('C')); }
        }
        protected void Reforge(Entity actor, Entity weapon, string replacement = "OakHaftComponent")
        {
            var component = GiveLive(actor, replacement); int quantity = component.GetPart<StackerPart>().StackCount;
            var bits = actor.GetPart<BitLockerPart>().GetBitsSnapshot(); int modifications = weapon.GetIntProperty("ModificationCount");
            Assert.IsTrue(WeaponForgingService.TryReforge(actor, Factory, weapon, component, out var returned, out var why), why);
            Assert.NotNull(returned); Assert.AreEqual(modifications, weapon.GetIntProperty("ModificationCount"));
            CollectionAssert.AreEquivalent(bits, actor.GetPart<BitLockerPart>().GetBitsSnapshot(), "reforge never pays for permanent mods again");
            if (replacement != returned.BlueprintName)
                Assert.AreEqual(quantity - 1, actor.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == replacement).Sum(i => i.GetPart<StackerPart>().StackCount));
        }
        protected ForgePreview BasePreview(Entity weapon)
        {
            var a = weapon.GetPart<WeaponAssemblyPart>();
            return WeaponForgingService.PreviewForge(Item(a.BladeBlueprint).GetPart<WeaponComponentPart>(),
                Item(a.HaftBlueprint).GetPart<WeaponComponentPart>(), Item(a.BindingBlueprint).GetPart<WeaponComponentPart>());
        }
        protected void CheckSharp(Entity weapon, bool sharp)
        {
            Assert.AreEqual(BasePreview(weapon).PenBonus + (sharp ? 1 : 0), weapon.GetPart<MeleeWeaponPart>().PenBonus);
            Assert.AreEqual(sharp, weapon.HasTag("ModSharp"));
            Assert.AreEqual(sharp ? 1 : 0, Name(weapon).Split(' ').Count(s => s == "sharp"));
        }
    }
    public class GameAuditReforgeModsTests : ReforgeModsFixture
    {
        [TestCase("OakHaftComponent", false)] [TestCase("OakHaftComponent", true)]
        [TestCase("IronSpikeComponent", false)] [TestCase("IronSpikeComponent", true)]
        [TestCase("SerratedEdgeComponent", false)] [TestCase("SerratedEdgeComponent", true)]
        public void ComponentReplacementRetainsOnlyPaidSharpContribution(string component, bool sharp)
        {
            var actor = Actor(); var weapon = Forge(actor); if (sharp) Pay(actor, weapon, "mod_sharp_melee");
            Reforge(actor, weapon, component); CheckSharp(weapon, sharp);
            Assert.AreEqual(BasePreview(weapon).BaseDamage, weapon.GetPart<MeleeWeaponPart>().BaseDamage);
            Assert.AreEqual(BasePreview(weapon).OnHitEffectsRaw, weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
        }
        [TestCase(false)] [TestCase(true)] public void RepeatedComponentCyclesDoNotAccumulateBonusOrLabels(bool sharp)
        {
            var actor = Actor(); var weapon = Forge(actor); if (sharp) Pay(actor, weapon, "mod_sharp_melee");
            foreach (string component in new[] { "WillowHaftComponent", "OakHaftComponent", "WillowHaftComponent", "OakHaftComponent" })
            { Reforge(actor, weapon, component); CheckSharp(weapon, sharp); }
            Assert.AreEqual(sharp ? 1 : 0, weapon.GetIntProperty("ModificationCount"));
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void ReforgeMeltsTemperButRetainsPermanentPaidSharp(bool sharp, bool temper)
        {
            var actor = Actor(); var weapon = Forge(actor); if (sharp) Pay(actor, weapon, "mod_sharp_melee");
            int hpMax = weapon.GetStat("Hitpoints").Max;
            if (temper)
            {
                var tonic = Brew(actor, false, true);
                Assert.IsTrue(WeaponTemperingService.TryTemper(actor, weapon, tonic, out var why), why);
                Assert.Greater(weapon.GetPart<WeaponTemperPart>().HpPenaltyTotal, 0);
                StringAssert.Contains("acid-quenched", Name(weapon));
            }
            Reforge(actor, weapon); CheckSharp(weapon, sharp);
            Assert.AreEqual(hpMax, weapon.GetStat("Hitpoints").Max);
            Assert.AreEqual(0, weapon.GetPart<WeaponTemperPart>()?.TemperCount ?? 0);
            Assert.AreEqual(BasePreview(weapon).OnHitEffectsRaw, weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            StringAssert.DoesNotContain("quenched", Name(weapon));
        }
        [TestCase("palesalt", "pale-salt-edged", false)] [TestCase("palesalt", "pale-salt-edged", true)]
        [TestCase("choiriron", "choir-iron-edged", false)] [TestCase("choiriron", "choir-iron-edged", true)]
        [TestCase("glowquartz", "glow-quartz-tipped", false)] [TestCase("glowquartz", "glow-quartz-tipped", true)]
        public void ActualPaidMineralRetainsPartAndLabelWithoutReapplyingEquipment(string mineral, string label, bool equipped)
        {
            var actor = Actor(); var weapon = Forge(actor);
            if (equipped) Assert.IsTrue(InventorySystem.Equip(actor, weapon));
            Pay(actor, weapon, "mod_" + mineral + "_infuse");
            var effects = weapon.Parts.OfType<IItemEnhancement>().ToArray(); Assert.AreEqual(1, effects.Length);
            int tier = effects[0].Tier; var light = weapon.GetPart<LightSourcePart>(); int radius = light?.Radius ?? 0;
            var slots = actor.GetPart<Body>().GetParts().Where(p => p._Equipped == weapon).ToArray();
            Reforge(actor, weapon, "WillowHaftComponent");
            StringAssert.Contains(label, Name(weapon)); Assert.AreEqual(1, Name(weapon).Split(' ').Count(w => w == label));
            CollectionAssert.AreEqual(effects, weapon.Parts.OfType<IItemEnhancement>()); Assert.AreEqual(tier, effects[0].Tier);
            Assert.AreEqual(radius, weapon.GetPart<LightSourcePart>()?.Radius ?? 0); Assert.AreSame(light, weapon.GetPart<LightSourcePart>());
            CollectionAssert.AreEqual(slots, actor.GetPart<Body>().GetParts().Where(p => p._Equipped == weapon));
            Assert.AreEqual(equipped, InventorySystem.IsEquipped(actor, weapon)); Assert.AreEqual(1, weapon.GetIntProperty("ModificationCount"));
        }
        [TestCase(false)] [TestCase(true)] public void SharpApplicationAfterReforgeChargesOnlyIfNotAlreadyPaid(bool previouslySharp)
        {
            var actor = Actor(); var weapon = Forge(actor); if (previouslySharp) Pay(actor, weapon, "mod_sharp_melee");
            Reforge(actor, weapon); var bits = actor.GetPart<BitLockerPart>(); bits.LearnRecipe("mod_sharp_melee"); bits.AddBits("BC");
            int b = bits.GetBitCount('B'), c = bits.GetBitCount('C');
            Assert.AreEqual(!previouslySharp, TinkeringService.TryApplyModification(actor, "mod_sharp_melee", weapon, out var why));
            if (previouslySharp) StringAssert.Contains("already sharp", why);
            Assert.AreEqual(b - (previouslySharp ? 0 : 1), bits.GetBitCount('B')); Assert.AreEqual(c - (previouslySharp ? 0 : 1), bits.GetBitCount('C'));
            CheckSharp(weapon, true); Assert.AreEqual(1, weapon.GetIntProperty("ModificationCount"));
        }
        [TestCase(false, 1)] [TestCase(false, 2)] [TestCase(true, 1)] [TestCase(true, 2)]
        public void SavedDuplicateMineralsRetainEveryEffectAndOneLabel(bool saved, int count)
        {
            var actor = Actor(); var weapon = Forge(actor); Pay(actor, weapon, "mod_sharp_melee");
            for (int i = 0; i < count; i++) Pay(actor, weapon, "mod_palesalt_infuse");
            if (saved) { actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor); weapon = actor.GetPart<InventoryPart>().Objects.Single(i => i.HasPart<WeaponAssemblyPart>()); }
            var parts = weapon.Parts.OfType<EnhancementPaleSalt>().ToArray();
            Reforge(actor, weapon); CheckSharp(weapon, true);
            CollectionAssert.AreEqual(parts, weapon.Parts.OfType<EnhancementPaleSalt>());
            Assert.AreEqual(count, parts.Length); Assert.IsTrue(parts.All(p => p.Tier == 2 && p.BonusDamage == 4));
            Assert.AreEqual(1, Name(weapon).Split(' ').Count(w => w == "pale-salt-edged"));
            Assert.AreEqual(count + 1, weapon.GetIntProperty("ModificationCount"));
        }
    }
}
