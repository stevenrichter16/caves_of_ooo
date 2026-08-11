using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// C1 — the preview services behind the Crafting panel's RESULT box.
    ///
    /// <para>The load-bearing invariant is <b>preview == reality</b>. A
    /// panel that promises "1d6+2" and then forges a 1d4 weapon is worse
    /// than no panel at all, because the player stops trusting the one
    /// thing the feature exists to provide. These tests forge and brew
    /// for real and compare the product against what the preview
    /// promised, field by field.</para>
    /// </summary>
    public class CraftPreviewTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.EnsureInitialized();
        }

        [TearDown]
        public void TearDown() => BrewRuleRegistry.ResetForTests();

        private static WeaponComponentPart Component(
            string slot, string dice = "", int pen = 0, int hit = 0,
            int maxStr = -1, string attrs = "", string quirk = "", string frag = "")
        {
            return new WeaponComponentPart
            {
                Slot = slot,
                BaseDamage = dice,
                PenBonus = pen,
                HitBonus = hit,
                MaxStrengthBonus = maxStr,
                Attributes = attrs,
                OnHitEffectSpec = quirk,
                NameFragment = frag,
            };
        }

        private Entity Crafter(params Entity[] carried)
        {
            var e = new Entity { ID = "crafter", BlueprintName = "crafter" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "the smith" });
            var inv = new InventoryPart { MaxWeight = 500 };
            e.AddPart(inv);
            foreach (var c in carried) inv.AddObject(c);
            return e;
        }

        private Entity ComponentItem(string id, WeaponComponentPart part)
        {
            var e = new Entity { ID = id, BlueprintName = "IronBlade" };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(part);
            return e;
        }

        // ════════════════════════════════════════════════════════
        // FORGE — preview must equal what forging actually produces
        // ════════════════════════════════════════════════════════

        [Test]
        public void PreviewForge_MatchesTheWeaponForgingActuallyProduces()
        {
            var blade = Component("Blade", dice: "1d6", pen: 2, attrs: "Cutting",
                quirk: "Bleeding:3", frag: "sword");
            var haft = Component("Haft", hit: 1, maxStr: 4, frag: "oak");
            var binding = Component("Binding", pen: 1, attrs: "Cutting Piercing",
                maxStr: 2, frag: "bound");

            var preview = WeaponForgingService.PreviewForge(blade, haft, binding);

            var crafter = Crafter(
                ComponentItem("blade", blade),
                ComponentItem("haft", haft),
                ComponentItem("binding", binding));

            bool ok = WeaponForgingService.TryForge(
                crafter, _factory,
                crafter.GetPart<InventoryPart>().Objects[0],
                crafter.GetPart<InventoryPart>().Objects[1],
                crafter.GetPart<InventoryPart>().Objects[2],
                out Entity weapon, out string reason);

            Assert.IsTrue(ok, reason);
            var melee = weapon.GetPart<MeleeWeaponPart>();

            Assert.AreEqual(preview.BaseDamage, melee.BaseDamage, "damage dice");
            Assert.AreEqual(preview.PenBonus, melee.PenBonus, "penetration");
            Assert.AreEqual(preview.HitBonus, melee.HitBonus, "to-hit");
            Assert.AreEqual(preview.MaxStrengthBonus, melee.MaxStrengthBonus, "str cap");
            Assert.AreEqual(preview.Attributes, melee.Attributes, "attributes");
            Assert.AreEqual(preview.OnHitEffectsRaw, melee.OnHitEffectsRaw, "on-hit quirks");
            Assert.AreEqual(preview.DisplayName, weapon.GetDisplayName(), "display name");
        }

        [Test]
        public void PreviewForge_IsPure_ConsumesNothingAndCreatesNothing()
        {
            // Counter-check to the above: if the preview forged a real
            // weapon to inspect it, the panel would consume the player's
            // components every time they moved the cursor.
            var blade = Component("Blade", dice: "1d8", frag: "axe");
            var haft = Component("Haft", frag: "iron");
            var binding = Component("Binding", frag: "riveted");

            var crafter = Crafter(
                ComponentItem("blade", blade),
                ComponentItem("haft", haft),
                ComponentItem("binding", binding));
            int before = crafter.GetPart<InventoryPart>().Objects.Count;

            for (int i = 0; i < 5; i++)
                WeaponForgingService.PreviewForge(blade, haft, binding);

            Assert.AreEqual(before, crafter.GetPart<InventoryPart>().Objects.Count,
                "previewing must not touch the pack");
        }

        [Test]
        public void PreviewForge_WithAMissingComponent_ReportsIncompleteRatherThanThrowing()
        {
            // The panel previews on every cursor move, including while
            // the selection is half-built.
            var blade = Component("Blade", dice: "1d6", frag: "sword");

            ForgePreview preview = default;
            Assert.DoesNotThrow(() => preview = WeaponForgingService.PreviewForge(blade, null, null));
            Assert.IsFalse(preview.IsComplete, "a partial kit is not a weapon");
            Assert.IsNotNull(preview.Missing, "and it says what is missing");
            Assert.IsTrue(preview.Missing.Contains("haft"), "names the haft: " + preview.Missing);
            Assert.IsTrue(preview.Missing.Contains("binding"), "names the binding: " + preview.Missing);
        }

        [Test]
        public void PreviewForge_WithAFullKit_IsComplete()
        {
            var preview = WeaponForgingService.PreviewForge(
                Component("Blade", dice: "1d6", frag: "sword"),
                Component("Haft", frag: "oak"),
                Component("Binding", frag: "bound"));

            Assert.IsTrue(preview.IsComplete);
            Assert.AreEqual("oak bound sword", preview.DisplayName);
        }

        // ════════════════════════════════════════════════════════
        // BREW — preview must equal what brewing actually produces
        // ════════════════════════════════════════════════════════

        [Test]
        public void PreviewBrew_MatchesTheBrewBrewingActuallyProduces()
        {
            var reagents = new List<Entity>
            {
                Reagent("firemoss", "heat:2, volatile:1"),
                Reagent("lampoil", "combustible:3, viscous:1"),
            };
            var crafter = Crafter(reagents.ToArray());

            var preview = BrewingService.PreviewBrew(reagents);
            Assert.IsTrue(preview.IsValid, "two real reagents make a brew");

            bool ok = BrewingService.TryBrew(
                crafter, _factory, reagents, out Entity brew,
                out BrewResult _, out string reason);
            Assert.IsTrue(ok, reason);

            var item = brew.GetPart<BrewItemPart>();
            Assert.IsNotNull(item);
            Assert.AreEqual(preview.DisplayName, brew.GetDisplayName(), "name");
            var predicted = new List<string>();
            foreach (var e in preview.Effects) predicted.Add(e.Property + ":" + e.Potency);
            var actual = new List<string>();
            foreach (var e in item.GetEffects()) actual.Add(e.Property + ":" + e.Potency);
            predicted.Sort(); actual.Sort();
            CollectionAssert.AreEqual(predicted, actual,
                "effects — compared by value; BrewPropertyAmount is a class "
                + "with no value equality, so comparing the objects directly "
                + "could never pass");
        }

        [Test]
        public void PreviewBrew_WithNoReagents_IsNotValid_AndDoesNotThrow()
        {
            BrewPreview preview = default;
            Assert.DoesNotThrow(() => preview = BrewingService.PreviewBrew(new List<Entity>()));
            Assert.IsFalse(preview.IsValid, "an empty mix is not a brew");
        }

        [Test]
        public void PreviewBrew_ChangesWhenTheMixChanges()
        {
            // The whole point of the RESULT box: adding a reagent must
            // visibly change the prediction, or the panel is decoration.
            var one = new List<Entity> { Reagent("frostlichen", "cold:2") };
            var two = new List<Entity>
            {
                Reagent("frostlichen", "cold:2"),
                Reagent("glimmerbrine", "corrosive:2, conductive:2"),
            };

            var a = BrewingService.PreviewBrew(one);
            var b = BrewingService.PreviewBrew(two);

            Assert.AreNotEqual(a.DisplayName + a.Properties, b.DisplayName + b.Properties,
                "adding a reagent must change the prediction");
        }

        private Entity Reagent(string id, string properties)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new ReagentPart { PropertiesRaw = properties });
            return e;
        }
    }
}
