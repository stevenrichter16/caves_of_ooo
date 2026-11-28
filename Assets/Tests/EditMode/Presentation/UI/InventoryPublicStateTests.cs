using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        sealed class PublicStateRegistryScope : IDisposable
        {
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            readonly Dictionary<string, LiquidDefinition> liquids;
            readonly bool liquidLoaded, proofLoaded;
            readonly object[] proofs;
            public PublicStateRegistryScope()
            {
                liquids = new Dictionary<string, LiquidDefinition>((Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", Flags).GetValue(null));
                liquidLoaded = LiquidRegistry.IsInitialized;
                var list = (IList)typeof(ProofingRegistry).GetField("Elements", Flags).GetValue(null);
                proofs = list.Cast<object>().ToArray(); proofLoaded = (bool)typeof(ProofingRegistry).GetField("loaded", Flags).GetValue(null);
                LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"water\",\"DisplayName\":\"water\"},{\"Id\":\"bog-mire\",\"DisplayName\":\"bog mire\"}]}");
                ProofingRegistry.InitializeFromJson(Resources.Load<TextAsset>("Content/Data/Alchemy/Proofing").text);
            }
            public void Dispose()
            {
                var map = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", Flags).GetValue(null); map.Clear();
                foreach (var pair in liquids) map.Add(pair.Key, pair.Value);
                typeof(LiquidRegistry).GetField("_initialized", Flags).SetValue(null, liquidLoaded);
                var list = (IList)typeof(ProofingRegistry).GetField("Elements", Flags).GetValue(null); list.Clear(); foreach (var value in proofs) list.Add(value);
                typeof(ProofingRegistry).GetField("loaded", Flags).SetValue(null, proofLoaded);
            }
        }
        sealed class PublicStateProbe : Part, IToolMenuState, IDescribeUse
        {
            public string Key = "PublicStateProbe", Value = "public state";
            public int Reads, Actions, Descriptions; public bool Throws; public Action DuringRead;
            public int Order => 10;
            public void DescribeUse(DescribeContext context, UseGuide guide) { Descriptions++; guide.How.Add("Public tool instructions."); }
            public override string Name => Key;
            public string ToolMenuState() { Reads++; DuringRead?.Invoke(); if (Throws) throw new InvalidOperationException("SECRET_ERROR"); return Value; }
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "GetInventoryActions") Actions++;
                return true;
            }
        }
        sealed class PublicStateUnknownEnhancement : IItemEnhancement
        {
            public int DescriptionReads;
            public override string Name => "SECRET_COMPONENT";
            public override string GetEffectDescription() { DescriptionReads++; return "SECRET_LORE"; }
        }
        sealed class PublicStateDualEnhancement : IItemEnhancement, IToolMenuState
        {
            public override string Name => "DualPublicState";
            public string ToolMenuState() => "2/3 water";
            public override string GetInventoryState() => "public treatment";
        }
        static Entity PublicStateCarry(Rig r, string blueprint)
        {
            var item = r.Factory.CreateEntity(blueprint); Assert.NotNull(item); Assert.True(r.Pack.AddObject(item)); return item;
        }
        static InventoryScreenData.ItemDisplay PublicDisplay(Rig r, Entity item)
            => InventoryScreenData.Build(r.Player).Categories.SelectMany(c => c.Items).Single(d => d.Item == item);
        static string PublicText(InventoryScreenData.ItemDisplay item)
        {
            var field = item.GetType().GetField("State"); Assert.NotNull(field, "The public state must be retained in the display snapshot, not queried during paint.");
            return (string)field.GetValue(item) ?? "";
        }
        static bool PublicMatches(InventoryScreenData.ItemDisplay item, string query)
        {
            var method = typeof(InventoryBrowseQuery).GetMethod("MatchesDisplay"); Assert.NotNull(method);
            return (bool)method.Invoke(null, new object[] { item, query });
        }

        [TestCase(0, "", "empty")] [TestCase(3, "water", "3/12 water")] [TestCase(12, "bog-mire", "12/12 bog mire")]
        public void PublicStateSnapshotDistinguishesFiniteFlaskContents(int volume, string liquid, string expected)
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                var item = PublicStateCarry(r, "LiquidFlask"); var vessel = item.GetPart<LiquidVesselPart>(); vessel.Volume = volume; vessel.LiquidId = liquid;
                Assert.AreEqual(expected, PublicText(PublicDisplay(r, item))); Assert.AreEqual(volume, vessel.Volume); Assert.AreEqual(liquid, vessel.LiquidId);
            }
        }
        [TestCase(0)] [TestCase(2)] [TestCase(3)]
        public void PublicStateSnapshotShowsActualWaterCharges(int charges)
        {
            using (var r = new Rig())
            {
                var item = PublicStateCarry(r, "Waterskin"); var skin = item.GetPart<WaterskinPart>(); skin.Charges = charges;
                Assert.AreEqual(charges + "/3", PublicText(PublicDisplay(r, item))); Assert.AreEqual(charges, skin.Charges);
            }
        }
        [TestCase(-1, 12, "water")] [TestCase(13, 12, "water")] [TestCase(0, 0, "")]
        [TestCase(0, 12, "SECRET_LIQUID")] [TestCase(1, 12, "SECRET_LIQUID")] [TestCase(1, 12, "")]
        public void PublicStateSharedFlaskProviderRefusesInvalidOrUnknownContents(int volume, int capacity, string liquid)
        {
            using (var registry = new PublicStateRegistryScope())
            {
                var vessel = new LiquidVesselPart { Volume = volume, Capacity = capacity, LiquidId = liquid };
                string text = vessel.ToolMenuState(); StringAssert.Contains("unavailable", text.ToLowerInvariant()); StringAssert.DoesNotContain("SECRET", text);
                Assert.AreEqual(volume, vessel.Volume); Assert.AreEqual(capacity, vessel.Capacity); Assert.AreEqual(liquid, vessel.LiquidId);
            }
        }
        [Test] public void PublicStateUninitializedRegistryDoesNotFallBackToPrivateLiquidId()
        {
            using (var registry = new PublicStateRegistryScope())
            { LiquidRegistry.ResetForTests(); var vessel = new LiquidVesselPart { Volume = 1, LiquidId = "SECRET_LIQUID" }; StringAssert.DoesNotContain("SECRET", vessel.ToolMenuState()); StringAssert.Contains("unavailable", vessel.ToolMenuState()); }
        }
        [TestCase(-1, 3)] [TestCase(4, 3)] [TestCase(1, 0)]
        public void PublicStateSharedSkinProviderRefusesInvalidChargeState(int charges, int capacity)
        { var skin = new WaterskinPart { Charges = charges, Capacity = capacity }; StringAssert.Contains("unavailable", skin.ToolMenuState()); Assert.AreEqual(charges, skin.Charges); }

        [TestCase("heat")] [TestCase("cold")] [TestCase("acid")] [TestCase("electric")]
        public void PublicStateTreatmentLabelsAreOptInAndDistinguishThePublicElement(string kind)
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                var coat = r.Carry("LeatherArmor"); coat.AddPart(new EnhancementSteeped { Kind = kind }); coat.AddPart(new EnhancementLacquered());
                var proof = r.Carry("LeatherArmor"); proof.AddPart(new EnhancementProofed { Kind = kind });
                string label = SteepedAcidEffect.ElementLabel(kind);
                var a = PublicText(PublicDisplay(r, coat)); StringAssert.Contains(label + " coat", a); StringAssert.Contains("lacquered", a);
                StringAssert.Contains(label + " proofed", PublicText(PublicDisplay(r, proof)));
                Assert.False(a.Contains("100"), "This compact state does not promise a live remaining duration from the garment's paused field.");
            }
        }
        [TestCase(0, 20, "spent")] [TestCase(3, 0, "unavailable")] [TestCase(-1, 20, "unavailable")]
        public void PublicStateCoatsDoNotClaimAnActiveTreatmentForExhaustedOrMalformedState(int left, int amount, string expected)
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                var item = r.Carry("LeatherArmor"); item.AddPart(new EnhancementSteeped { Kind = "heat", TurnsLeft = left, Amount = amount });
                StringAssert.Contains(expected, PublicText(PublicDisplay(r, item)));
            }
        }
        [Test] public void PublicStateUnknownEnhancementDoesNotRevealTypePrivateFieldsOrDescriptions()
        {
            using (var r = new Rig())
            {
                var item = r.Carry("Dagger"); var part = new PublicStateUnknownEnhancement(); item.AddPart(part);
                var display = PublicDisplay(r, item); Assert.IsEmpty(PublicText(display)); Assert.False(PublicMatches(display, "SECRET")); Assert.Zero(part.DescriptionReads);
            }
        }
        [TestCase("hidden")] [TestCase("foreign")] [TestCase("stale-part")]
        public void PublicStateNeverQueriesUnavailableCustodyRenderOrProviderOwners(string condition)
        {
            using (var r = new Rig())
            {
                var item = r.Carry("Dagger"); var probe = new PublicStateProbe { Value = "SECRET_STATE" }; item.AddPart(probe);
                if (condition == "hidden") item.GetPart<RenderPart>().Visible = false;
                else if (condition == "foreign") item.GetPart<PhysicsPart>().InInventory = r.Factory.CreateEntity("Player");
                else probe.ParentEntity = r.Factory.CreateEntity("Dagger");
                Assert.IsEmpty(PublicText(PublicDisplay(r, item))); Assert.Zero(probe.Reads);
            }
        }
        [Test] public void PublicStateAggregatesStableSingleLineDistinctBoundedProviderSummaries()
        {
            using (var r = new Rig())
            {
                var item = r.Carry("Dagger"); var first = new PublicStateProbe { Key = "A", Value = "water\nready" }; var second = new PublicStateProbe { Key = "B", Value = "water\nready" };
                item.AddPart(first); item.AddPart(second); item.AddPart(new PublicStateProbe { Key = "C", Value = new string('z', 1000) });
                var text = PublicText(PublicDisplay(r, item)); StringAssert.Contains("water ready", text); Assert.False(text.Contains('\n')); Assert.LessOrEqual(text.Length, 256); StringAssert.Contains("...", text);
                Assert.AreEqual(1, text.Split(new[] { "water ready" }, StringSplitOptions.None).Length - 1); Assert.AreEqual(1, first.Reads); Assert.AreEqual(1, second.Reads);
            }
        }
        [Test] public void PublicStateFaultingProviderCannotBreakThePackOrExposeItsException()
        {
            using (var r = new Rig())
            { var item = r.Carry("Dagger"); item.AddPart(new PublicStateProbe { Throws = true }); var text = PublicText(PublicDisplay(r, item)); StringAssert.Contains("unavailable", text); StringAssert.DoesNotContain("SECRET", text); Assert.True(r.Pack.Contains(item)); }
        }
        [Test] public void PublicStateLiteralSearchUsesOnlyTheRetainedNameAndPublicSummary()
        {
            using (var r = new Rig())
            {
                var item = r.Carry("Dagger", "public blade"); var probe = new PublicStateProbe { Value = "water 2/3" }; item.AddPart(probe);
                var display = PublicDisplay(r, item); int reads = probe.Reads; item.GetPart<RenderPart>().DisplayName = "SECRET_RENAME"; probe.Value = "SECRET_CHANGED";
                Assert.True(PublicMatches(display, "WATER")); Assert.True(PublicMatches(display, "public blade")); Assert.False(PublicMatches(display, "SECRET")); Assert.False(PublicMatches(display, ".*")); Assert.AreEqual(reads, probe.Reads);
                Assert.True(PublicMatches(display, ""));
            }
        }
        [Test] public void PublicStateTypingAndResizeReuseModelActionsStateAndSameOwnerDescription()
        {
            using (var r = new Rig())
            {
                var item = r.Carry("Dagger"); var probe = new PublicStateProbe { Value = "public water" }; item.AddPart(probe); r.Open(); r.Tab(1);
                var state = Get(r.UI, "_state"); var document = Get(r.UI, "_ledgerSelectionDocument"); int reads = probe.Reads, actions = probe.Actions, descriptions = probe.Descriptions; Assert.Greater(descriptions, 0);
                Call(r.UI, "BeginInventorySearch"); foreach (string query in new[] { "pub", "public", "NO_MATCH", "pub", "" }) Call(r.UI, "SetInventoryQuery", query);
                r.UI.GetType().GetField("_inventorySearchActive", Members).SetValue(r.UI, false); Call(r.UI, "SetInventoryQuery", "");
                r.Camera.aspect *= 1.1f; r.UI.HandleInput(); Call(r.UI, "Render");
                Assert.AreSame(state, Get(r.UI, "_state")); Assert.AreSame(document, Get(r.UI, "_ledgerSelectionDocument"));
                Assert.AreEqual(reads, probe.Reads); Assert.AreEqual(actions, probe.Actions); Assert.AreEqual(descriptions, probe.Descriptions); Assert.AreSame(item, DenseSelected(r)); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test] public void PublicStateFindsTheCorrectSameNameFlaskAndRefreshesOnlyAfterModelRebuild()
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                var empty = PublicStateCarry(r, "LiquidFlask"); var full = PublicStateCarry(r, "LiquidFlask"); var vessel = full.GetPart<LiquidVesselPart>(); vessel.Volume = 12; vessel.LiquidId = "water";
                r.Open(); r.Tab(1); Call(r.UI, "SetInventoryQuery", "water"); CollectionAssert.AreEqual(new[] { full }, DenseOwners(r)); Assert.AreSame(full, DenseSelected(r));
                vessel.Volume = 0; vessel.LiquidId = ""; Call(r.UI, "SetInventoryQuery", "water"); CollectionAssert.AreEqual(new[] { full }, DenseOwners(r));
                Call(r.UI, "Rebuild"); Call(r.UI, "Render"); Assert.IsEmpty(DenseOwners(r)); Call(r.UI, "SetInventoryQuery", "empty");
                Assert.AreEqual(1, CountedRows(r).Length); Assert.AreEqual(2, CountedUnits(CountedRows(r)[0]));
                CountedToggle(r); CollectionAssert.AreEquivalent(new[] { empty, full }, DenseOwners(r));
                Assert.Zero(vessel.Volume); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test] public void PublicStatePaintKeepsDistinguishingBadgesInsideLargerNameCellsAcrossPages()
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                for (int i = 0; i < 42; i++) r.Carry("DriedMeat", "stock " + i);
                var flask = PublicStateCarry(r, "LiquidFlask"); var garment = r.Carry("LeatherArmor", new string('q', 90)); garment.AddPart(new EnhancementSteeped { Kind = "heat" }); garment.AddPart(new EnhancementLacquered());
                r.Open(); r.Tab(1);
                Assert.Less(r.Hits().Count(h => Kind(h) == "Item"), 44);
                Call(r.UI, "SelectLedgerHit", r.FindHit("Item", r.InventoryRow(garment))); Call(r.UI, "Render");
                string summary = PublicText(PublicDisplay(r, garment)); StringAssert.Contains(summary, (string)Get(r.UI, "_ledgerSelectionText"));
                var row = Bounds(r.FindHit("Item", r.InventoryRow(garment)));
                var name = r.Labels().Single(t => t.Content.Contains('q') && Mathf.Abs(r.BoundsOf(t).center.y - row.center.y) < .1f);
                StringAssert.Contains("...", name.Content); StringAssert.Contains("[", name.Content);
                var nameBounds = r.BoundsOf(name);
                Assert.LessOrEqual(name.Content.Length * nameBounds.height / 2, nameBounds.width + .1f);
                BrowseCategory(r, "Containers");
                AssertSelectedItemIsPainted(r, flask);
                Assert.True(r.Labels().Any(t => t.Content.Contains("liquid flask") && t.Content.Contains("empty")));
            }
        }
        [Test] public void PublicStateStrikerNumericChargeNameRemainsSearchable_Counter()
        {
            using (var r = new Rig())
            { var item = PublicStateCarry(r, "FlintStriker"); item.GetPart<FlintStrikerPart>().Strikes = 19; r.Open(); r.Tab(1); Call(r.UI, "SetInventoryQuery", "19"); CollectionAssert.AreEqual(new[] { item }, DenseOwners(r)); Assert.True(r.Labels().Any(t => t.Content.Contains("[19]"))); }
        }
        [Test] public void PublicStateReviewOnePartCanExposeBothExplicitPublicCapabilities()
        {
            using (var r = new Rig())
            { var item = r.Carry("Dagger"); item.AddPart(new PublicStateDualEnhancement()); var text = PublicText(PublicDisplay(r, item)); StringAssert.Contains("2/3 water", text); StringAssert.Contains("public treatment", text); }
        }
        [TestCase("part")] [TestCase("custody")] [TestCase("render")]
        public void PublicStateReviewProviderCannotPublishAfterInvalidatingItsOwner(string kind)
        {
            using (var r = new Rig())
            {
                var item = r.Carry("Dagger"); var probe = new PublicStateProbe { Value = "SECRET_REHOMED" }; item.AddPart(probe); var other = r.Factory.CreateEntity("Dagger");
                probe.DuringRead = () => { if (kind == "part") probe.ParentEntity = other; else if (kind == "custody") item.GetPart<PhysicsPart>().InInventory = other; else item.GetPart<RenderPart>().Visible = false; };
                Assert.IsEmpty(PublicText(PublicDisplay(r, item))); Assert.AreEqual(1, probe.Reads);
            }
        }
        [TestCase(0, 1)] [TestCase(3, 4)]
        public void PublicStateReviewMalformedCoatTotalsAreUnavailable(int total, int left)
        {
            using (var r = new Rig())
            { var item = r.Carry("LeatherArmor"); item.AddPart(new EnhancementSteeped { TurnsTotal = total, TurnsLeft = left }); StringAssert.Contains("unavailable", PublicText(PublicDisplay(r, item))); }
        }
        [TestCase("fire", "heat")] [TestCase("SECRET_KIND", "acid")]
        public void PublicStateReviewExistingCanonicalProofingPolicyIsPreserved(string kind, string label)
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                var item = r.Carry("LeatherArmor"); item.AddPart(new EnhancementSteeped { Kind = kind }); var text = PublicText(PublicDisplay(r, item));
                Assert.AreEqual(label + " coat", text); StringAssert.DoesNotContain("SECRET", text); Assert.AreEqual(kind, item.GetPart<EnhancementSteeped>().Kind);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void PublicStateReviewSharedWorldMenuKeepsValidFlasksDistinctAndInvalidIdsPrivate(bool invalid)
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                var zone = r.UI.CurrentZone; var grass = r.Factory.CreateEntity("Grass"); Assert.True(zone.AddEntity(grass, 11, 10));
                var flasks = new[] { PublicStateCarry(r, "LiquidFlask"), PublicStateCarry(r, "LiquidFlask"), PublicStateCarry(r, "LiquidFlask") };
                for (int i = 0; i < flasks.Length; i++)
                {
                    var vessel = flasks[i].GetPart<LiquidVesselPart>(); vessel.Volume = i == 0 ? 0 : 3; vessel.LiquidId = i == 0 ? "" : invalid && i == 2 ? "SECRET_LIQUID" : "water";
                    var probe = new WorldToolProbePart(); probe.PackRows.Add(new WorldToolProbePart.Row { X = 11, Y = 10, Display = "public flask work", Command = "ProbeIt|flask" }); flasks[i].AddPart(probe);
                }
                var cell = zone.GetCell(11, 10); var rows = WorldInteractionSystem.GatherCellActions(WorldInteractionSystem.ResolveTarget(cell, r.Player), cell, r.Player).Where(a => a.Name == "Probe").ToArray();
                Assert.AreEqual(invalid ? 3 : 2, rows.Length); Assert.True(rows.Any(a => a.Display.Contains("empty"))); Assert.True(rows.Any(a => a.Display.Contains("3/12 water")));
                Assert.False(rows.Any(a => a.Display.Contains("SECRET"))); if (invalid) Assert.True(rows.Any(a => a.Display.Contains("unavailable")));
                Assert.That(rows.All(a => flasks.Contains(a.Via)), Is.True);
            }
        }
    }
}
