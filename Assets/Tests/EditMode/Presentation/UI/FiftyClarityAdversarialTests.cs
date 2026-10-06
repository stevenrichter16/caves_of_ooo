using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Reflection permits all teams' test-first additions to compile together.
    // Every presence guard is followed by a player-visible behavioral assertion.
    public sealed class FiftyClarityAdversarialTests : FiftyClarityFixture
    {
        [TestCase("")] [TestCase("missing")]
        public void SearchEmptyAndNoMatchDoNotInventRows(string query)
        {
            var one = Loose("short sword"); var list = new List<Entity> { one };
            var found = ((IEnumerable)Query("InventoryBrowseQuery", "Filter", list, query)).Cast<Entity>().ToArray();
            Assert.AreEqual(query == "" ? 1 : 0, found.Length);
        }

        [TestCase("fog")] [TestCase("hidden")] [TestCase("foreign")]
        public void LootReaderRefusesUnavailableSource(string denial)
        {
            var item = Loose("SECRET_LOOT"); zone.AddEntity(item, 10, 10);
            if (denial == "fog") zone.GetCell(10, 10).IsVisible = false;
            if (denial == "hidden") item.GetPart<RenderPart>().Visible = false;
            if (denial == "foreign") { zone.RemoveEntity(item); Pack.AddObject(item); }
            string text = Detail("Loot", actor, zone, item, null) ?? "";
            StringAssert.DoesNotContain("SECRET_LOOT", text);
        }

        [TestCase(false)] [TestCase(true)]
        public void HaulingCueRequiresTwoSidedCurrentLinkAndReadsAppliedPenalty(bool stale)
        {
            var load = Loose("oak beam", 60); zone.AddEntity(load, 11, 10);
            var grip = new DragPart { Dragged = load, SpeedPenalty = 99, AppliedPenalty = 7 }; actor.AddPart(grip);
            if (!stale) load.AddPart(new DraggedPart { Dragger = actor });
            string text = Detail("Haul", actor, zone) ?? "";
            if (stale) StringAssert.DoesNotContain("oak beam", text);
            else { StringAssert.Contains("oak beam", text); StringAssert.Contains("-7", text); StringAssert.DoesNotContain("99", text); }
            Assert.AreSame(grip, actor.GetPart<DragPart>());
        }

        [Test] public void StatusReaderDoesNotCallVirtualActionBlockers()
        {
            var counter = new ReadCounterEffect(); actor.ApplyEffect(counter);
            string text = Detail("Status", actor); Assert.IsNotEmpty(text);
            Assert.Zero(counter.Reads);
        }

        [Test] public void UnknownModificationDoesNotPromiseGuessedNumericChanges()
        {
            var item = Carry("Dagger");
            var recipe = new TinkerRecipe { Blueprint = "third_party_mod", Type = "Mod", Description = "A custom enhancement." };
            string text = Detail("Modification", item, recipe);
            StringAssert.Contains(recipe.Description, text); StringAssert.Contains("unavailable", text.ToLowerInvariant());
        }

        [TestCase(false)] [TestCase(true)]
        public void ContainerInspectionDoesNotRevealLockedContents(bool locked)
        {
            var box = Loose("chest"); box.AddPart(new ContainerPart { MaxItems = 4, Locked = locked }); zone.AddEntity(box, 11, 10);
            var carried = Carry("Dagger");
            string text = Detail("Container", actor, zone, box, carried);
            if (locked) { StringAssert.Contains("locked", text); StringAssert.DoesNotContain("entries:", text); }
            else StringAssert.Contains("0/4", text);
        }

        [Test] public void AbsentOrForeignHandlingStateDoesNotLeakOrThrow()
        {
            Assert.IsNull(Detail("Haul", actor, zone)); Assert.IsNull(Detail("Handling", actor, null));
            var other = factory.CreateEntity("Player"); var item = Loose("FOREIGN_GOODS"); other.GetPart<InventoryPart>().AddObject(item);
            Assert.IsNull(Detail("Item", actor, item)); Assert.IsNull(Detail("Trade", actor, actor, item, true));
            Assert.IsNull(Detail("Food", actor, item)); Assert.IsNull(Detail("Handling", actor, item));
        }

        sealed class ReadCounterEffect : Effect
        {
            public int Reads;
            public override string DisplayName => "read counter";
            public ReadCounterEffect() { Duration = 5; }
            public override bool AllowAction(Entity owner) { Reads++; return true; }
        }

        [TestCase(49, false)] [TestCase(50, true)]
        public void SurfaceAmplificationUsesTheActualCoatThreshold(int amount, bool amplifies)
        {
            LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"clarity_threshold\",\"DisplayName\":\"threshold coat\",\"Conductivity\":" + amount + ",\"Combustibility\":" + amount + "}]}");
            zone.TileState.WriteCoating(11, 10, "clarity_threshold", 4);
            string text = Detail("Surface", zone, zone.GetCell(11, 10));
            Assert.AreEqual(amplifies, text.Contains("amplifies electrical"));
            Assert.AreEqual(amplifies, text.Contains("fire more dangerous"));
            Assert.AreEqual(4, zone.TileState.Get(11, 10).Coatings[0].Turns);
        }
    }
}
