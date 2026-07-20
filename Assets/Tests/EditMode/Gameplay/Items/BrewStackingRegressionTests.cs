using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests.Gameplay.Items
{
    /// <summary>
    /// Live-play bug (2026-07-19): "I brewed fire moss + lamp oil, the log
    /// said it made a flask, and it never appeared in my inventory."
    /// Root cause: StackerPart.CanStackWith compared BlueprintName ONLY,
    /// and every brew is blueprint "BrewedTonic" (with Stacker inherited
    /// from the Item base) — so a NEW brew merged into any older brew's
    /// stack, keeping the OLD item's name and payload. The new flask was
    /// consumed as a +1 on something else. These tests run against
    /// PRODUCTION blueprints + rules, which is exactly why the original
    /// suites missed it (their fixtures' BrewedTonic had no Stacker).
    /// </summary>
    public class BrewStackingRegressionTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.EnsureInitialized();
        }

        [TearDown]
        public void TearDown()
        {
            BrewRuleRegistry.ResetForTests();
        }

        private Entity CreateBrewer()
        {
            var brewer = new Entity { ID = "brewer", BlueprintName = "Player" };
            brewer.Statistics["Hitpoints"] = new Stat
            {
                Owner = brewer, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20
            };
            brewer.AddPart(new RenderPart { DisplayName = "brewer" });
            brewer.AddPart(new InventoryPart());
            return brewer;
        }

        private Entity Give(Entity brewer, string blueprint)
        {
            Entity item = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"production blueprint '{blueprint}' must exist");
            Assert.IsTrue(brewer.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        // ════════════════ CanStackWith contract ════════════════

        [Test]
        public void CanStackWith_SameBlueprintDifferentDisplayName_False()
        {
            var a = _factory.CreateEntity("BrewedTonic");
            var b = _factory.CreateEntity("BrewedTonic");
            a.GetPart<RenderPart>().DisplayName = "burning coating";
            b.GetPart<RenderPart>().DisplayName = "burning and wet flask";

            Assert.IsFalse(a.GetPart<StackerPart>().CanStackWith(b),
                "different display names = different items; merging destroys identity");
        }

        [Test]
        public void CanStackWith_SameBlueprintSameDisplayName_True()
        {
            // Counter-check: identical brews still stack (that IS the
            // batch-brewing convenience).
            var a = _factory.CreateEntity("BrewedTonic");
            var b = _factory.CreateEntity("BrewedTonic");
            a.GetPart<RenderPart>().DisplayName = "burning coating";
            b.GetPart<RenderPart>().DisplayName = "burning coating";

            Assert.IsTrue(a.GetPart<StackerPart>().CanStackWith(b));
        }

        // ════════════════ The user's exact scenario ════════════════

        [Test]
        public void TwoDifferentBrews_BothAppearAsDistinctItems()
        {
            var brewer = CreateBrewer();
            var moss = Give(brewer, "FireMoss");
            var oil = Give(brewer, "LampOil");
            var brine = Give(brewer, "GlimmerBrine");

            Assert.IsTrue(BrewingService.TryBrew(
                brewer, _factory, new List<Entity> { moss, oil },
                out Entity first, out _, out string r1), r1);
            Assert.IsNotNull(first);
            string firstName = first.GetDisplayName();

            Assert.IsTrue(BrewingService.TryBrew(
                brewer, _factory, new List<Entity> { brine },
                out Entity second, out _, out string r2), r2);
            Assert.IsNotNull(second);

            var carried = brewer.GetPart<InventoryPart>().Objects;
            Assert.IsTrue(carried.Contains(first), "first brew still a distinct item");
            Assert.IsTrue(carried.Contains(second),
                "the SECOND brew must exist in inventory as its own item — " +
                "this is the live bug: it merged into the first stack and vanished");
            Assert.AreEqual(firstName, first.GetDisplayName(),
                "first brew's identity untouched by the second");
            Assert.AreNotEqual(first.GetDisplayName(), second.GetDisplayName(),
                "fixture sanity: the two recipes produce differently-named brews");
        }
    }
}
