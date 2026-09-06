using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditReforgeModsAdversarialTests : ReforgeModsFixture
    {
        private sealed class HitRandom : Random
        {
            public override int Next(int min, int max) => max == 21 ? 10 : min;
            public override int Next(int max) => 0;
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void AnonymousComponentsUseStableBaseNameWithoutOldTemperOrDuplicateMods(bool blank, bool sharp)
        {
            var actor = Actor(); var weapon = Forge(actor); if (sharp) Pay(actor, weapon, "mod_sharp_melee");
            var tonic = Brew(actor, false, true); Assert.IsTrue(WeaponTemperingService.TryTemper(actor, weapon, tonic, out _));
            string fallback = Name(Item("ForgedWeapon"));
            string[] bps = { "SteelBladeComponent", "OakHaftComponent", "LeatherBindingComponent" };
            var original = bps.Select(bp => Factory.Blueprints[bp].Parts["WeaponComponent"]["NameFragment"]).ToArray();
            try
            {
                if (blank) foreach (string bp in bps) Factory.Blueprints[bp].Parts["WeaponComponent"]["NameFragment"] = "";
                for (int i = 0; i < 3; i++)
                {
                    Reforge(actor, weapon); CheckSharp(weapon, sharp);
                    Assert.AreEqual((sharp ? "sharp " : "") + (blank ? fallback : BasePreview(weapon).DisplayName), Name(weapon));
                    Assert.AreEqual(0, weapon.GetPart<WeaponTemperPart>().TemperCount); StringAssert.DoesNotContain("quenched", Name(weapon));
                }
            }
            finally { for (int i = 0; i < bps.Length; i++) Factory.Blueprints[bps[i]].Parts["WeaponComponent"]["NameFragment"] = original[i]; }
        }
        [TestCase(false, 0)] [TestCase(false, 3)] [TestCase(true, 0)] [TestCase(true, 3)]
        public void OnlySharpMarkerRestoresBonusAndOldArbitraryValuesAreReplaced(bool sharp, int unrelatedCount)
        {
            var actor = Actor(); var weapon = Forge(actor); if (sharp) Pay(actor, weapon, "mod_sharp_melee");
            weapon.SetIntProperty("ModificationCount", unrelatedCount); weapon.GetPart<MeleeWeaponPart>().PenBonus += 77;
            weapon.GetPart<RenderPart>().DisplayName = "stale arbitrary component name";
            Reforge(actor, weapon, "IronSpikeComponent"); CheckSharp(weapon, sharp);
            Assert.AreEqual(unrelatedCount, weapon.GetIntProperty("ModificationCount")); StringAssert.DoesNotContain("stale", Name(weapon));
        }
        [TestCase("palesalt", "choiriron", false)] [TestCase("palesalt", "choiriron", true)]
        [TestCase("palesalt", "glowquartz", false)] [TestCase("palesalt", "glowquartz", true)]
        [TestCase("choiriron", "glowquartz", false)] [TestCase("choiriron", "glowquartz", true)]
        public void CanonicalNamesNeverReorderPaidPartsOrEraseStackIdentity(string first, string second, bool reverse)
        {
            var actorA = Actor(); var actorB = Actor(); var a = Forge(actorA); var b = Forge(actorB);
            Pay(actorA, a, "mod_" + first + "_infuse"); Pay(actorA, a, "mod_" + second + "_infuse");
            Pay(actorB, b, "mod_" + (reverse ? second : first) + "_infuse"); Pay(actorB, b, "mod_" + (reverse ? first : second) + "_infuse");
            var partsA = a.Parts.OfType<IItemEnhancement>().ToArray(); var partsB = b.Parts.OfType<IItemEnhancement>().ToArray();
            Reforge(actorA, a); Reforge(actorB, b);
            Assert.AreEqual(Name(a), Name(b)); CollectionAssert.AreEqual(partsA, a.Parts.OfType<IItemEnhancement>());
            CollectionAssert.AreEqual(partsB, b.Parts.OfType<IItemEnhancement>());
            Assert.AreEqual(!reverse, a.GetPart<StackerPart>().CanStackWith(b));
            Assert.AreEqual(2, a.GetIntProperty("ModificationCount")); Assert.AreEqual(2, b.GetIntProperty("ModificationCount"));
        }
        [TestCase(false, 1)] [TestCase(false, 2)] [TestCase(true, 1)] [TestCase(true, 2)]
        public void GlowStateSurvivesReforgeAndRealUnequipReequip(bool saved, int count)
        {
            var actor = Actor(); var weapon = Forge(actor); Assert.IsTrue(InventorySystem.Equip(actor, weapon));
            for (int i = 0; i < count; i++) Pay(actor, weapon, "mod_glowquartz_infuse");
            if (saved) { actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor); weapon = actor.GetPart<InventoryPart>().GetAllEquipped().Single(); }
            var parts = weapon.Parts.OfType<EnhancementGlowQuartz>().ToArray(); Assert.AreEqual(count, parts.Length);
            Assert.IsTrue(parts.All(p => p.AppliedBonus && p.RadiusBonus == 2)); Assert.AreEqual(count * 2, weapon.GetPart<LightSourcePart>().Radius);
            Reforge(actor, weapon); CollectionAssert.AreEqual(parts, weapon.Parts.OfType<EnhancementGlowQuartz>());
            Assert.IsTrue(parts.All(p => p.AppliedBonus && p.RadiusBonus == 2)); Assert.AreEqual(count * 2, weapon.GetPart<LightSourcePart>().Radius);
            Assert.IsTrue(InventorySystem.UnequipItem(actor, weapon)); Assert.IsTrue(parts.All(p => !p.AppliedBonus)); Assert.AreEqual(0, weapon.GetPart<LightSourcePart>().Radius);
            Assert.IsTrue(InventorySystem.Equip(actor, weapon)); Assert.IsTrue(parts.All(p => p.AppliedBonus)); Assert.AreEqual(count * 2, weapon.GetPart<LightSourcePart>().Radius);
        }
        [TestCase(false)] [TestCase(true)] public void SavedCustomMineralValuesAreNeverTierConfiguredAgain(bool saved)
        {
            var actor = Actor(); var weapon = Forge(actor); Pay(actor, weapon, "mod_palesalt_infuse");
            var part = weapon.GetPart<EnhancementPaleSalt>(); part.Tier = 7; part.BonusDamage = 13;
            if (saved) { actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor); weapon = actor.GetPart<InventoryPart>().Objects.Single(i => i.HasPart<WeaponAssemblyPart>()); part = weapon.GetPart<EnhancementPaleSalt>(); }
            Reforge(actor, weapon); Assert.AreSame(part, weapon.GetPart<EnhancementPaleSalt>()); Assert.AreEqual(7, part.Tier); Assert.AreEqual(13, part.BonusDamage);
        }
        [TestCase(false)] [TestCase(true)] public void MissingRenderStillRestoresSharpMechanic(bool sharp)
        {
            var actor = Actor(); var weapon = Forge(actor); if (sharp) Pay(actor, weapon, "mod_sharp_melee");
            weapon.RemovePart(weapon.GetPart<RenderPart>()); Reforge(actor, weapon);
            Assert.IsNull(weapon.GetPart<RenderPart>()); Assert.AreEqual(BasePreview(weapon).PenBonus + (sharp ? 1 : 0), weapon.GetPart<MeleeWeaponPart>().PenBonus);
        }
        [TestCase(false)] [TestCase(true)] public void ActualMeleeDispatchReadsRetainedPermanentContribution(bool sharp)
        {
            var actor = Actor(); var weapon = Forge(actor); if (sharp) Pay(actor, weapon, "mod_sharp_melee"); Reforge(actor, weapon);
            var target = new Entity(); target.AddPart(new PhysicsPart());
            target.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100, Max = 100 };
            // Combat derives DV from agility/body; use a controlled noncritical hit roll.
            var zone = new Zone("ReforgeCombat"); Assert.IsTrue(zone.AddEntity(actor, 10, 10)); Assert.IsTrue(zone.AddEntity(target, 11, 10));
            Diag.ResetAll(); Diag.SetChannel("damage", true);
            CombatSystem.PerformSingleAttack(actor, target, weapon.GetPart<MeleeWeaponPart>(), true, zone, new HitRandom());
            var hit = DiagQuery.Apply(new DiagQuery.Filter { Kind = "HitRoll", Actor = actor.ID, Target = target.ID }).Records.Single();
            StringAssert.Contains("\"landed\":true", hit.PayloadJson);
            var penetration = DiagQuery.Apply(new DiagQuery.Filter { Kind = "Penetration", Actor = actor.ID, Target = target.ID }).Records.Single();
            StringAssert.Contains("\"weaponPenBonus\":" + (BasePreview(weapon).PenBonus + (sharp ? 1 : 0)), penetration.PayloadJson);
        }
        [TestCase("owned")] [TestCase("foreign_component")] [TestCase("foreign_weapon")] [TestCase("missing")]
        public void ServiceRefusalKeepsPaidStateAndPayments(string condition)
        {
            var actor = Actor(); var owner = condition == "foreign_weapon" ? Actor() : actor; var weapon = Forge(owner);
            Pay(owner, weapon, "mod_sharp_melee"); Pay(owner, weapon, "mod_palesalt_infuse");
            var component = condition == "missing" ? null : GiveLive(condition == "foreign_component" ? Actor() : actor, "WillowHaftComponent");
            var componentOwner = component?.GetPart<PhysicsPart>()?.InInventory;
            var effects = weapon.Parts.OfType<IItemEnhancement>().ToArray(); string name = Name(weapon); int pen = weapon.GetPart<MeleeWeaponPart>().PenBonus;
            Assert.AreEqual(condition == "owned", WeaponForgingService.TryReforge(actor, Factory, weapon, component, out _, out _));
            if (condition != "owned") { Assert.AreEqual(name, Name(weapon)); Assert.AreEqual(pen, weapon.GetPart<MeleeWeaponPart>().PenBonus); if (component != null) { Assert.AreEqual(1, component.GetPart<StackerPart>().StackCount);
                Assert.AreSame(componentOwner, component.GetPart<PhysicsPart>().InInventory); Assert.IsTrue(componentOwner.GetPart<InventoryPart>().Objects.Contains(component)); } }
            else { Assert.AreEqual("WillowHaftComponent", weapon.GetPart<WeaponAssemblyPart>().HaftBlueprint); Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(component)); }
            Assert.IsTrue(owner.GetPart<InventoryPart>().Contains(weapon));
            CheckSharp(weapon, true); CollectionAssert.AreEqual(effects, weapon.Parts.OfType<IItemEnhancement>()); Assert.AreEqual(2, weapon.GetIntProperty("ModificationCount"));
        }
        [TestCase(false)] [TestCase(true)] public void ActualCommandKeepsStationGateAndRefusalState(bool near)
        {
            var actor = Actor(); var weapon = Forge(actor); Pay(actor, weapon, "mod_sharp_melee");
            var replacement = GiveLive(actor, "WillowHaftComponent"); var zone = new Zone("ReforgeStation");
            Assert.IsTrue(zone.AddEntity(actor, 10, 10)); Assert.IsTrue(zone.AddEntity(Item("TinkersForge"), near ? 11 : 20, 10));
            string name = Name(weapon);
            Assert.AreEqual(near, InventorySystem.ExecuteCommand(new ReforgeWeaponCommand(weapon, replacement, Factory), actor, zone).Success);
            if (!near) { Assert.AreEqual(name, Name(weapon)); Assert.AreEqual(1, replacement.GetPart<StackerPart>().StackCount);
                Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(replacement)); Assert.AreSame(actor, replacement.GetPart<PhysicsPart>().InInventory); }
            else { Assert.AreEqual("WillowHaftComponent", weapon.GetPart<WeaponAssemblyPart>().HaftBlueprint); Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(replacement)); }
            CheckSharp(weapon, true);
        }
        [TestCase(false)] [TestCase(true)] public void ReforgeDoesNotFreePaidMineralSlots(bool full)
        {
            var actor = Actor(); var weapon = Forge(actor); Pay(actor, weapon, "mod_palesalt_infuse"); if (full) Pay(actor, weapon, "mod_palesalt_infuse");
            Reforge(actor, weapon); var salt = GiveLive(actor, "PaleSalt");
            Assert.AreEqual(!full, TinkeringService.TryApplyModification(actor, "mod_palesalt_infuse", weapon, out _));
            Assert.AreEqual(full, actor.GetPart<InventoryPart>().Objects.Contains(salt));
            Assert.AreEqual(2, weapon.Parts.OfType<EnhancementPaleSalt>().Count()); Assert.AreEqual(2, weapon.GetIntProperty("ModificationCount"));
        }
    }
}
