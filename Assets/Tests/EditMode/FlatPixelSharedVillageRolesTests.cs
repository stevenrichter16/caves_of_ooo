using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Farmer remains in its existing incidental catalog. Elder/child extend the
    // resident inputs; reflection leaves the absent six-input API assertion RED.
    public sealed class FlatPixelSharedVillageRolesTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Names = { "Farmer", "Elder", "VillageChild" };
        readonly List<Action> restore = new List<Action>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        FlatPixelIncidentalActorCatalog incidental;
        FlatPixelSharedResidentsCatalog residents;
        Texture2D farmer, grazer, cindercaller;
        Texture2D[] inputs;

        [SetUp] public void Setup()
        {
            foreach (var type in new[] { typeof(MessageLog), typeof(EquipmentChangeBus), typeof(WorldAudioHooks), typeof(WorldAudioUtility), typeof(WorldAudioCombat), typeof(EntityVisualHooks) })
                foreach (var field in type.GetFields(S)) if (!field.IsLiteral) Snapshot(field);
            Snapshot(typeof(AnatomyFactory).GetField("_types", S)); Snapshot(typeof(BodyPart).GetField("_nextID", S));
            foreach (string name in new[] { "_buffer", "_writeIndex", "_filledCount", "_droppedCount" }) Snapshot(typeof(Diag).GetField(name, S));
            content = new DensityLootTestScope(); CorpsePart.Factory = content.Factory; MessageLog.OnMessage = null;
            WorldAudioHooks.Callback = null; WorldAudioUtility.Callback = null; WorldAudioCombat.Callback = null; EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true); Attach(); world = new FlatPixelGreyboxCatalog();
            farmer = Texture(16, 24); grazer = Texture(16, 24); cindercaller = Texture(16, 24);
            inputs = new[] { Texture(64, 24), Texture(16, 16), Texture(16, 16), Texture(32, 24), Texture(16, 16), Texture(16, 16) };
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); incidental?.Dispose(); residents?.Dispose(); foreach (var t in textures) if (t != null) Object.DestroyImmediate(t); textures.Clear(); content?.Dispose(); }
            finally { for (int i = restore.Count - 1; i >= 0; i--) restore[i](); restore.Clear(); }
        }
        Texture2D Texture(int w, int h) { var t = new Texture2D(w, h); textures.Add(t); return t; }
        void Attach(string id = "Overworld.10.10.0")
        { zone = new Zone(id); manager.SetActiveZone(zone); foreach (var cell in zone.Cells) cell.IsVisible = cell.Explored = true; }
        Entity Owner(string name) { var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); return e; }
        Entity Add(string name, int x = 10, int y = 10) { var e = Owner(name); Assert.True(zone.AddEntity(e, x, y)); return e; }
        static string Resource(string name) => name == "Farmer" ? "Sprites/Live16x24/farmer" : "Sprites/Environment/" + (name == "Elder" ? "elder" : "village_child");
        Texture2D TextureFor(string name) => name == "Farmer" ? farmer : inputs[name == "Elder" ? 4 : 5];
        static FlatPixelSharedResidentsCatalog Construct(params Texture2D[] values)
        {
            var ctor = typeof(FlatPixelSharedResidentsCatalog).GetConstructor(Enumerable.Repeat(typeof(Texture2D), values.Length).ToArray());
            Assert.NotNull(ctor, "Missing independent " + values.Length + "-texture resident constructor.");
            return (FlatPixelSharedResidentsCatalog)ctor.Invoke(values.Cast<object>().ToArray());
        }
        static void Set(object target, string name, object value) { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        void Install(Texture2D farmerInput, Texture2D[] residentInputs)
        {
            world.Dispose(); incidental?.Dispose(); residents?.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            incidental = new FlatPixelIncidentalActorCatalog(farmerInput, grazer, cindercaller); residents = Construct(residentInputs);
            Set(world, "_incidentalActorArt", incidental); Set(world, "_sharedResidentsArt", residents);
        }
        void Install() => Install(farmer, inputs);
        bool Query(Entity owner, out Tile tile) => owner.BlueprintName == "Farmer" ? incidental.TryResolve(owner, out tile) : residents.TryResolve(owner, out tile);
        static void Frame(Tile tile, string name, Texture2D texture)
        {
            int h = name == "Farmer" ? 24 : 16; Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, h), tile.sprite.rect); Assert.AreEqual(new Vector2(8, h / 2f), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(1, h / 16f), (Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        FlatPixelGreyboxCell View(int x = 10, int y = 10) { Assert.True(world.TryResolvePresentation(zone, x, y, out var v)); return v; }
        Tile Routes(Entity owner, Texture2D texture)
        {
            var cell = zone.GetEntityCell(owner); Assert.NotNull(cell); var v = View(cell.X, cell.Y); Assert.AreSame(owner, v.Owner); Assert.True(v.NativeForeground, owner.BlueprintName);
            Frame(v.ForegroundTile, owner.BlueprintName, texture); Assert.AreEqual(Color.white, v.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, cell.X, cell.Y, out var sprite, out var tint)); Assert.AreSame(v.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            if (owner.BlueprintName != "Farmer" && FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone, cell.X, cell.Y, out var old)); Frame(old.ForegroundTile, owner.BlueprintName, texture);
                Assert.True(world.TryGetSubjectSprite(owner, cell.X, cell.Y, out sprite, out tint)); Assert.AreSame(v.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            }
            return v.ForegroundTile;
        }
        static void Portrait(Reader reader, AnnouncementDocument doc, Texture2D texture)
        {
            Assert.NotNull(doc); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc)); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(doc, out var p));
            Assert.AreSame(texture, p.Texture); Assert.AreEqual(new Rect(0, 0, 1, 1), p.Uv); Assert.AreEqual(Color.white, p.Tint); Assert.False(p.IsModel);
        }
        static void Rejected(Reader reader, AnnouncementDocument doc)
        { Assert.NotNull(doc); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(doc, out var p)); Assert.IsNull(p.Texture); }
        void NotNative(Texture texture, string name)
        { Assert.AreNotSame(TextureFor(name), texture); var imported = Resources.Load<Texture2D>(Resource(name)); Assert.NotNull(imported); Assert.AreNotSame(imported, texture); }
        void Denied(Entity owner, string why)
        {
            Assert.False(Query(owner, out var tile), why); Assert.IsNull(tile); Assert.False(View().NativeForeground, why);
            if (world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out _)) NotNative(sprite.texture, owner.BlueprintName);
            if (owner.BlueprintName != "Farmer" && FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone, 10, 10, out var old)); NotNative(old.ForegroundTile?.sprite?.texture, owner.BlueprintName);
                if (world.TryGetSubjectSprite(owner, 10, 10, out sprite, out _)) NotNative(sprite.texture, owner.BlueprintName);
            }
        }

        [TestCase("Farmer", "@", "&Y")] [TestCase("Elder", "@", "&M")] [TestCase("VillageChild", "c", "&Y")]
        public void ActualCanonicalRoleUsesFullResourceInWorldAndAuthorizedReaderInsideAndOutsideOpening(string name, string glyph, string color)
        {
            foreach (var id in new[] { "Overworld.10.10.0", "Overworld.12.11.0" })
            {
                Attach(id); var owner = Add(name); Assert.AreEqual(glyph, owner.GetPart<RenderPart>().RenderString); Assert.AreEqual(color, owner.GetPart<RenderPart>().ColorString);
                Assert.True(View().NativeForeground, name + " still has glyph fallback."); var texture = Resources.Load<Texture2D>(Resource(name)); Assert.NotNull(texture); Routes(owner, texture);
                using (var reader = new Reader(world, zone, Owner("Player"))) Portrait(reader, ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer), texture);
            }
        }

        [Test] public void ChildNeedsItsActualLivingCoreButNeverInventsTradeOrConversationServices()
        {
            Install(); var child = Add("VillageChild"); Assert.IsNull(child.GetPart<TraderPart>()); Assert.IsNull(child.GetPart<ConversationPart>());
            Assert.NotNull(child.GetPart<InventoryPart>()); Assert.NotNull(child.GetPart<ArmorPart>()); Assert.NotNull(child.GetPart<CorpsePart>()); Routes(child, inputs[5]);
            Assert.True(FlatPixelSharedResidentsCatalog.HasKnownRole(child)); Assert.False(FlatPixelSharedResidentsCatalog.HasKnownRole(Owner("Farmer")));
            foreach (var name in new[] { "EncasedElder", "CreatureCorpse", "Player" })
            { var owner = Owner(name); owner.Properties["SourceBlueprint"] = "Elder"; Assert.False(residents.TryResolve(owner, out _)); Assert.False(incidental.TryResolve(owner, out _)); }
            var frazzled = Owner("Villager"); frazzled.GetPart<RenderPart>().RenderString = "f"; frazzled.GetPart<RenderPart>().ColorString = "&w";
            frazzled.Properties["SourceBlueprint"] = "Farmer"; Assert.False(incidental.TryResolve(frazzled, out _));
        }

        [Test] public void OldResidentInputsKeepNewRolesAbsentAndFarmerRetainsIndependentIncidentalInjection()
        {
            foreach (int count in new[] { 2, 3, 4 }) using (var old = Construct(inputs.Take(count).ToArray()))
            {
                Assert.False(old.TryResolve(Owner("Elder"), out _)); Assert.False(old.TryResolve(Owner("VillageChild"), out _));
                Assert.True(old.TryResolve(Owner("Villager"), out var tile)); Assert.AreSame(inputs[0], tile.sprite.texture);
                Assert.True(old.TryResolve(Owner("Merchant"), out _)); if (count >= 3) Assert.True(old.TryResolve(Owner("Weaponsmith"), out _));
                if (count == 4) { Assert.True(old.TryResolve(Owner("Tinker"), out _)); Assert.True(old.TryResolve(Owner("WellKeeper"), out _)); }
            }
            using (var old = new FlatPixelIncidentalActorCatalog(farmer, grazer))
            { Assert.True(old.TryResolve(Owner("Farmer"), out var tile)); Frame(tile, "Farmer", farmer); Assert.True(old.TryResolve(Owner("ReedbackGrazer"), out _)); Assert.False(old.TryResolve(Owner("MarlbackCindercaller"), out _)); }
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            foreach (var name in Names) { Attach(); Add(name); Assert.False(View().NativeForeground); }
        }

        [TestCase("Farmer")] [TestCase("Elder")] [TestCase("VillageChild")]
        public void MissingOrMalformedInputIsAuthoritativeAndKeepsOtherRolesAvailable(string missing)
        {
            foreach (var bad in new[] { null, Texture(16, missing == "Farmer" ? 16 : 24) })
            {
                var residentInputs = (Texture2D[])inputs.Clone(); if (missing != "Farmer") residentInputs[missing == "Elder" ? 4 : 5] = bad;
                Install(missing == "Farmer" ? bad : farmer, residentInputs);
                foreach (var name in Names)
                {
                    Attach("Overworld.12.11.0"); var owner = Add(name);
                    if (name != missing) { Routes(owner, TextureFor(name)); continue; }
                    Denied(owner, "missing or malformed " + name);
                    using (var reader = new Reader(world, zone, Owner("Player")))
                    { var doc = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(doc, out var p)); NotNative(p.Texture, name); }
                }
                Assert.True(residents.TryResolve(Owner("Merchant"), out var merchant)); Assert.AreSame(inputs[1], merchant.sprite.texture);
                Assert.True(incidental.TryResolve(Owner("ReedbackGrazer"), out var g)); Assert.AreSame(grazer, g.sprite.texture);
            }
        }

        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void EveryCorePartAndAdultServiceMustBeUniqueAndOwned(string fault)
        {
            Install();
            foreach (var name in Names)
            {
                var core = new[] { typeof(RenderPart), typeof(PhysicsPart), typeof(Body), typeof(BrainPart), typeof(InventoryPart), typeof(ExaminablePart) };
                var required = name == "VillageChild" ? core : core.Concat(new[] { typeof(TraderPart), typeof(ConversationPart) }).ToArray();
                foreach (var type in required)
                {
                    Attach("Overworld.12.11.0"); var owner = Add(name); Routes(owner, TextureFor(name)); var part = owner.Parts.First(p => p.GetType() == type);
                    if (fault == "missing") Assert.True(owner.RemovePart(part)); else if (fault == "foreign") part.ParentEntity = new Entity(); else owner.AddPart((Part)Activator.CreateInstance(type));
                    Denied(owner, name + "/" + type.Name + "/" + fault);
                }
            }
        }

        [Test] public void ReskinsAndForeignTagsCannotFallThroughToOpeningArt()
        {
            Install();
            foreach (var name in Names) foreach (var fault in new[] { "glyph", "color", "layer", "background", "visual", "variant", "variants", "player", "item", "terrain", "creature" })
            {
                Attach("Overworld.12.11.0"); var owner = Add(name); Routes(owner, TextureFor(name)); var r = owner.GetPart<RenderPart>();
                switch (fault)
                {
                    case "glyph": r.RenderString = name == "VillageChild" ? "@" : "c"; break; case "color": r.ColorString = "&w"; break; case "layer": r.RenderLayer = 1; break;
                    case "background": r.BackgroundColor = "^k"; break; case "visual": r.VisualID = "custom-resident"; break; case "variant": r.VisualVariant = "custom"; break; case "variants": r.GlyphVariants = "@c"; break;
                    case "player": owner.SetTag("Player"); break; case "item": owner.SetTag("Item"); break; case "terrain": owner.SetTag("Terrain"); break; case "creature": owner.Tags.Remove("Creature"); break;
                }
                Denied(owner, name + "/" + fault);
            }
        }

        [Test] public void NonActorPropAndServiceMeaningsRefuseWithoutRejectingRealCorpseSpawners()
        {
            Install();
            foreach (var name in Names) foreach (var type in new[] { typeof(ForgePart), typeof(WellPart), typeof(WellSitePart), typeof(ContainerPart), typeof(CropPart), typeof(HarvestablePart), typeof(DestructiblePart), typeof(TileStateSourcePart) })
            { Attach("Overworld.12.11.0"); var owner = Add(name); Assert.NotNull(owner.GetPart<CorpsePart>()); Routes(owner, TextureFor(name)); owner.AddPart((Part)Activator.CreateInstance(type)); Denied(owner, name + "/" + type.Name); }
        }

        [Test] public void CarriedEquippedTakeableAndMulticellActorsCannotKeepAWorldBody()
        {
            Install();
            foreach (var name in Names) foreach (var fault in new[] { "carried", "equipped", "takeable", "multicell" })
            {
                Attach("Overworld.12.11.0"); var owner = Add(name); Routes(owner, TextureFor(name)); var p = owner.GetPart<PhysicsPart>();
                if (fault == "carried") p.InInventory = new Entity(); if (fault == "equipped") p.Equipped = new Entity(); if (fault == "takeable") p.Takeable = true;
                if (fault == "multicell") { owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" }); Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0")); }
                Denied(owner, name + "/" + fault);
            }
        }

        [Test] public void LivingHpCanChangeButMissingForeignOrDeadStatsCannotBorrowTheFrame()
        {
            Install();
            foreach (var name in Names) foreach (var fault in new[] { "missing", "foreign", "dead" })
            {
                Attach("Overworld.12.11.0"); var owner = Add(name); owner.GetStat("Hitpoints").BaseValue = 1; Routes(owner, TextureFor(name));
                if (fault == "missing") owner.Statistics.Remove("Hitpoints"); if (fault == "foreign") owner.GetStat("Hitpoints").Owner = new Entity(); if (fault == "dead") owner.GetStat("Hitpoints").BaseValue = 0;
                Denied(owner, name + "/" + fault);
            }
        }

        [TestCase("Farmer")] [TestCase("Elder")] [TestCase("VillageChild")]
        public void RealAdultTradeAndMutableNamesHealthAiEquipmentAndServicesDoNotChangeTheBody(string name)
        {
            Install(); var owner = Add(name); var player = Add("Player", 9); var tile = Routes(owner, TextureFor(name));
            if (name != "VillageChild")
            {
                var stock = TradeSystem.GetTraderStock(owner); Assert.IsNotEmpty(stock); var item = stock.First();
                int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(player), owner); TradeSystem.SetDrams(player, price + 10); int purse = TradeSystem.GetDrams(owner);
                Assert.True(TradeSystem.BuyFromTrader(player, owner, item, out var reason), reason); Assert.AreEqual(10, TradeSystem.GetDrams(player)); Assert.AreEqual(purse + price, TradeSystem.GetDrams(owner));
                owner.GetPart<TraderPart>().StockTable = "DifferentStock"; owner.GetPart<ConversationPart>().ConversationID = "DifferentConversation";
            }
            var inventory = owner.GetPart<InventoryPart>(); var sword = Owner("LongSword"); Assert.True(inventory.AddObject(sword)); Assert.True(inventory.Equip(sword, "TestHand"));
            owner.GetStat("Hitpoints").BaseValue = 1; owner.GetPart<RenderPart>().DisplayName = "A named villager";
            var brain = owner.GetPart<BrainPart>(); brain.Passive = true; brain.Staying = false; brain.Wanders = true;
            var probe = new Probe(); owner.AddPart(probe); var items = inventory.Objects.ToArray(); int goals = brain.GoalCount, drams = TradeSystem.GetDrams(owner);
            for (int i = 0; i < 3; i++) Assert.AreSame(tile, Routes(owner, TextureFor(name)));
            CollectionAssert.AreEqual(items, inventory.Objects); Assert.AreEqual(goals, brain.GoalCount); Assert.AreEqual(drams, TradeSystem.GetDrams(owner)); Assert.Zero(probe.Queries);
            Assert.AreSame(owner, sword.GetPart<PhysicsPart>().Equipped); if (name == "VillageChild") { Assert.IsNull(owner.GetPart<TraderPart>()); Assert.IsNull(owner.GetPart<ConversationPart>()); }
        }

        [Test] public void ActualDamageKeepsLivingIdentityAndDeathRetiresReaderForSeparateCorpse()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); var owner = Add(name); var tile = Routes(owner, TextureFor(name)); var spawner = owner.GetPart<CorpsePart>(); Assert.AreEqual(100, spawner.CorpseChance); spawner.TestRng = new System.Random(65);
                using (var reader = new Reader(world, zone, Owner("Player")))
                {
                    var doc = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer); Portrait(reader, doc, TextureFor(name));
                    int hp = owner.GetStatValue("Hitpoints"); CombatSystem.ApplyDamage(owner, 1, null, zone); Assert.AreEqual(hp - 1, owner.GetStatValue("Hitpoints")); Assert.AreSame(tile, Routes(owner, TextureFor(name)));
                    CombatSystem.ApplyDamage(owner, owner.GetStatValue("Hitpoints"), null, zone); Assert.IsNull(zone.GetEntityCell(owner)); Rejected(reader, doc); Assert.False(Query(owner, out _));
                    Assert.NotNull(spawner.CreatedCorpse); Assert.AreEqual(name, spawner.CreatedCorpse.GetProperty("SourceBlueprint")); Assert.False(incidental.TryResolve(spawner.CreatedCorpse, out _)); Assert.False(residents.TryResolve(spawner.CreatedCorpse, out _));
                }
            }
        }

        [Test] public void DisclosureCoverMovementRemovalAndReplacedGraphRetireOnlyCurrentAuthority()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); var owner = Add(name); var tile = Routes(owner, TextureFor(name));
                using (var reader = new Reader(world, zone, Owner("Player")))
                {
                    var doc = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer); Portrait(reader, doc, TextureFor(name));
                    zone.GetCell(10, 10).IsVisible = false; Assert.False(View().NativeForeground); Rejected(reader, doc);
                    zone.GetCell(10, 10).Explored = false; Assert.False(world.TryResolvePresentation(zone, 10, 10, out _)); zone.GetCell(10, 10).IsVisible = zone.GetCell(10, 10).Explored = true;
                    owner.GetPart<RenderPart>().Visible = false; Assert.False(View().NativeForeground); Rejected(reader, doc); owner.GetPart<RenderPart>().Visible = true;
                    var cover = Add("Player"); Assert.AreSame(cover, View().Owner); Assert.AreNotSame(tile, View().ForegroundTile); Assert.True(zone.RemoveEntity(cover)); Assert.AreSame(tile, Routes(owner, TextureFor(name)));
                    Assert.True(zone.MoveEntity(owner, 11, 10)); Rejected(reader, doc); Assert.AreNotSame(tile, View().ForegroundTile); Assert.AreSame(tile, Routes(owner, TextureFor(name)));
                    var moved = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(11, 10), reader.Viewer); Portrait(reader, moved, TextureFor(name));
                    manager.SetActiveZone(new Zone(zone.ZoneID)); Assert.False(world.TryResolvePresentation(zone, 11, 10, out _)); Rejected(reader, moved);
                    Assert.True(zone.RemoveEntity(owner)); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(moved));
                }
            }
        }

        [Test] public void StalePlacedFootprintCannotRegainNativeReaderUntilCanonicalSpatialRepair()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); var owner = Add(name); var footprint = new SpatialFootprintPart { CellsRaw = "0,0" }; owner.AddPart(footprint); var tile = Routes(owner, TextureFor(name));
                using (var reader = new Reader(world, zone, Owner("Player")))
                {
                    var doc = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer); Portrait(reader, doc, TextureFor(name));
                    Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0")); Assert.False(Query(owner, out _)); footprint.CellsRaw = "0,0";
                    Assert.IsNull(View().Owner); Assert.False(Query(owner, out _)); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(doc, out var p)); NotNative(p.Texture, name);
                    Assert.True(zone.TryChangeFootprint(owner, "0,0")); Assert.AreSame(tile, Routes(owner, TextureFor(name))); Portrait(reader, doc, TextureFor(name));
                }
            }
        }

        [Test] public void FarmerPortabilityDoesNotPromoteGrazerOrCindercallerOutsideOpening()
        {
            Install();
            foreach (var pair in new[] { ("ReedbackGrazer", grazer), ("MarlbackCindercaller", cindercaller) })
            {
                Attach("Overworld.12.11.0"); var owner = Add(pair.Item1); Assert.True(View().NativeForeground); Assert.AreSame(pair.Item2, View().ForegroundTile.sprite.texture);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out _)); Assert.AreSame(pair.Item2, sprite.texture);
                Assert.True(zone.RemoveEntity(owner)); Attach(); Assert.True(zone.AddEntity(owner, 10, 10)); Assert.False(View().NativeForeground);
                if (world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out sprite, out _)) Assert.AreNotSame(pair.Item2, sprite.texture);
                Assert.True(incidental.TryResolve(owner, out var tile)); Assert.AreSame(pair.Item2, tile.sprite.texture, "Only caller locality is restricted.");
            }
        }

        [Test] public void ExistingResidentsRemainIndependentAndDisposalKeepsBorrowedInputsAndPeerAlive()
        {
            Install(); var peers = new[] { ("Villager", 0), ("Merchant", 1), ("Weaponsmith", 2), ("Tinker", 3), ("WellKeeper", 3) };
            foreach (var pair in peers) { Assert.True(residents.TryResolve(Owner(pair.Item1), out var tile)); Assert.AreSame(inputs[pair.Item2], tile.sprite.texture); }
            var frames = new List<Tile>(); foreach (var name in Names) { Assert.True(Query(Owner(name), out var tile)); frames.Add(tile); Assert.True(Query(Owner(name), out var again)); Assert.AreSame(tile, again); }
            var sprites = frames.Select(t => t.sprite).ToArray();
            using (var peer = Construct(inputs)) using (var farmerPeer = new FlatPixelIncidentalActorCatalog(farmer, grazer, cindercaller))
            {
                world.Dispose(); world.Dispose(); foreach (var tile in frames) Assert.True(tile == null); foreach (var sprite in sprites) Assert.True(sprite == null);
                foreach (var name in Names) { var owner = Owner(name); Assert.False(Query(owner, out _)); Tile tile; Assert.True(name == "Farmer" ? farmerPeer.TryResolve(owner, out tile) : peer.TryResolve(owner, out tile)); Frame(tile, name, TextureFor(name)); Assert.True(TextureFor(name) != null); Assert.AreEqual(FilterMode.Bilinear, TextureFor(name).filterMode); Assert.AreEqual(TextureWrapMode.Repeat, TextureFor(name).wrapMode); }
            }
        }

        [Test] public void PortableFarmerPainterPreservesActualDamageAndWetCuesWithoutReaderConsumption()
        {
            Install(); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "Exercise portable Farmer outside the opening.");
            var owner = Add("Farmer"); var body = Routes(owner, farmer); var probe = new FarmerRenderProbe(); owner.AddPart(probe);
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                // Invoke the real per-cell painter: it dispatches Render, records
                // the environment receipt and submits the actual tile/color.
                var map = reader.Presentation.GetComponent<Tilemap>(); Assert.NotNull(map); Set(reader.Presentation, "_tilemap", map);
                var environment = (EnvironmentSpriteRenderer)typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).GetValue(reader.Presentation);
                environment.RenderingEnabled = true;
                var paint = typeof(ZoneRenderer).GetMethod("RenderFlatCellCore", I); Assert.NotNull(paint);
                var position = new Vector3Int(10, Zone.Height - 1 - 10, 0);
                Action<Color> submit = expected =>
                {
                    int before = probe.Renders;
                    paint.Invoke(reader.Presentation, new object[] { 10, 10, position, zone.GetCell(10, 10) });
                    Assert.AreSame(body, map.GetTile<Tile>(position)); Assert.AreEqual(expected, map.GetColor(position));
                    Assert.AreEqual(before + 1, probe.Renders, "Only the selected owner's submitted cell dispatches Render.");
                };
                var doc = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                submit(Color.white);
                int hp = owner.GetStatValue("Hitpoints"); CombatSystem.ApplyDamage(owner, 1, null, zone); Assert.AreEqual(hp - 1, owner.GetStatValue("Hitpoints"));
                for (int frame = 0; frame < 3; frame++)
                {
                    int before = probe.Renders; Assert.AreSame(body, Routes(owner, farmer)); Portrait(reader, doc, farmer); Assert.AreEqual(before, probe.Renders);
                    submit(QudColorParser.Parse("&R"));
                }
                submit(Color.white);
                var wet = new WetEffect(); Assert.True(owner.ForceApplyEffect(wet, null, zone));
                Assert.AreEqual("&B", owner.GetPart<StatusEffectsPart>().GetRenderColorOverride()); submit(QudColorParser.Parse("&B"));
                int wetRenders = probe.Renders; Assert.AreSame(body, Routes(owner, farmer)); Portrait(reader, doc, farmer); Assert.AreEqual(wetRenders, probe.Renders);
                Assert.True(owner.GetPart<StatusEffectsPart>().RemoveEffect(wet)); submit(Color.white);
            }
        }

        sealed class FarmerRenderProbe : Part
        {
            public int Renders;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "Render") Renders++; return true; }
        }

        sealed class Probe : Part
        {
            public int Queries;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "Render" || e.ID == "GetInventoryActions" || e.ID == "InventoryAction" || e.ID == "ObjectCreated") Queries++; return true; }
        }
        sealed class Reader : IDisposable
        {
            readonly GameObject root; readonly EnvironmentSpriteRenderer environment;
            public readonly Entity Viewer; public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog, Zone zone, Entity viewer)
            {
                Viewer = viewer; Assert.True(zone.AddEntity(viewer, 9, 10)); root = new GameObject("Village role reader"); root.SetActive(false);
                Presentation = root.AddComponent<ZoneRenderer>(); environment = root.AddComponent<EnvironmentSpriteRenderer>();
                Set(environment, "_flatPixelGreybox", catalog); environment.FlatPixelGreyboxEnabled = true; Set(Presentation, "_envSpriteRenderer", environment); Set(Presentation, "_flatPixelModeEnabled", true);
                typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone); Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose() { if (environment != null) Set(environment, "_flatPixelGreybox", null); if (root != null) Object.DestroyImmediate(root); }
        }
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field); object value = field.GetValue(null);
            if (value is Array array) { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary) { var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry entry in dictionary) copy.Add(entry); restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); }); }
            else if (value is IList list) { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            else if (value is IEnumerable items && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
            { var copy = items.Cast<object>().ToArray(); restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null); foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); }); }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
