using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Bounded extension of the existing residents contract. Reflection keeps the
    // missing four-texture constructor an assertion RED, not a compiler failure.
    public sealed class FlatPixelSharedSpecialistsTests
    {
        const int X = 10, Y = 10;
        const string ResourcePath = "Sprites/Live16x24/shared-specialists";
        static readonly string[] Names = { "Tinker", "WellKeeper" };
        static readonly FieldInfo ArtField = typeof(FlatPixelGreyboxCatalog).GetField("_sharedResidentsArt", BindingFlags.Instance | BindingFlags.NonPublic);
        readonly List<Texture2D> textures = new List<Texture2D>();
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Texture2D cast, merchant, smith, specialists;
        Action<string> oldMessage, oldFull;
        Action<int, int, string> oldDirty;
        EntityDamageVisualHandler oldDamage;
        EntityMovedVisualHandler oldMoved;
        EntityFactory oldCorpse;
        Action<WorldAudioCombatReceipt> oldAudio;
        int oldEquipmentVersion;

        [SetUp] public void Setup()
        {
            oldEquipmentVersion = EquipmentChangeBus.GlobalVersion;
            oldMessage = MessageLog.OnMessage; oldDirty = ZoneRenderHooks.CellDirtyCallback; oldFull = ZoneRenderHooks.FullDirtyCallback;
            oldDamage = EntityVisualHooks.DamageCallback; oldMoved = EntityVisualHooks.MovedCallback;
            oldCorpse = CorpsePart.Factory; oldAudio = WorldAudioCombat.Callback;
            MessageLog.OnMessage = null; ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            EntityVisualHooks.DamageCallback = null; EntityVisualHooks.MovedCallback = null; WorldAudioCombat.Callback = null;
            content = new DensityLootTestScope(); CorpsePart.Factory = content.Factory;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            Attach(); world = new FlatPixelGreyboxCatalog();
            cast = Texture(64, 24); merchant = Texture(16, 16); smith = Texture(16, 16); specialists = Texture(32, 24);
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                world?.Dispose(); foreach (var texture in textures) if (texture != null) Object.DestroyImmediate(texture);
                textures.Clear(); content?.Dispose();
            }
            finally
            {
                MessageLog.OnMessage = oldMessage; ZoneRenderHooks.CellDirtyCallback = oldDirty; ZoneRenderHooks.FullDirtyCallback = oldFull;
                EntityVisualHooks.DamageCallback = oldDamage; EntityVisualHooks.MovedCallback = oldMoved;
                CorpsePart.Factory = oldCorpse; WorldAudioCombat.Callback = oldAudio;
                typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null, oldEquipmentVersion);
            }
        }
        Texture2D Texture(int width, int height) { var value = new Texture2D(width, height); textures.Add(value); return value; }
        void Attach(string id = "Overworld.12.11.0")
        { zone = new Zone(id); manager.SetActiveZone(zone); foreach (var cell in zone.Cells) cell.IsVisible = cell.Explored = true; }
        Entity Owner(string name) { var owner = content.Factory.CreateEntity(name); Assert.NotNull(owner, name); return owner; }
        Entity Add(string name, int x = X, int y = Y) { var owner = Owner(name); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        static FlatPixelSharedResidentsCatalog Construct(params Texture2D[] inputs)
        {
            var ctor = typeof(FlatPixelSharedResidentsCatalog).GetConstructor(Enumerable.Repeat(typeof(Texture2D), inputs.Length).ToArray());
            Assert.NotNull(ctor, "Shared residents must expose the requested " + inputs.Length + "-texture constructor.");
            return (FlatPixelSharedResidentsCatalog)ctor.Invoke(inputs.Cast<object>().ToArray());
        }
        FlatPixelSharedResidentsCatalog Install(Texture2D atlas)
        {
            world.Dispose(); world = new FlatPixelGreyboxCatalog(null, cast);
            var art = Construct(cast, merchant, smith, atlas); Assert.NotNull(ArtField); ArtField.SetValue(world, art); return art;
        }
        static void Native(Sprite sprite, string name, Texture2D atlas)
        {
            Assert.NotNull(sprite, name); Assert.AreSame(atlas, sprite.texture);
            Assert.AreEqual(new Rect(name == "Tinker" ? 0 : 16, 0, 16, 24), sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), sprite.pivot); Assert.AreEqual(16, sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1, 1.5f), (Vector2)sprite.bounds.size);
        }
        Tile Routes(Entity owner, Texture2D atlas)
        {
            var cell = zone.GetEntityCell(owner); Assert.NotNull(cell);
            Assert.True(world.TryResolvePresentation(zone, cell.X, cell.Y, out var view));
            Assert.AreSame(owner, view.Owner); Assert.True(view.NativeForeground); Native(view.ForegroundTile.sprite, owner.BlueprintName, atlas);
            Assert.AreEqual(TileFlags.None, view.ForegroundTile.flags); Assert.AreEqual(Tile.ColliderType.None, view.ForegroundTile.colliderType);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, cell.X, cell.Y, out var sprite, out var tint));
            Native(sprite, owner.BlueprintName, atlas); Assert.AreEqual(Color.white, tint);
            if (FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone, cell.X, cell.Y, out var local)); Native(local.ForegroundTile.sprite, owner.BlueprintName, atlas);
                Assert.True(world.TryGetSubjectSprite(owner, cell.X, cell.Y, out sprite, out tint)); Native(sprite, owner.BlueprintName, atlas); Assert.AreEqual(Color.white, tint);
            }
            return view.ForegroundTile;
        }
        void Fallback(Entity owner, FlatPixelSharedResidentsCatalog art, string why)
        {
            Assert.False(art.TryResolve(owner, out var tile), why); Assert.IsNull(tile, why);
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var view)); Assert.False(view.NativeForeground, why);
            if (world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out _)) NotSpecialists(sprite.texture, why);
            Assert.True(world.TryResolve(zone, X, Y, out var local)); NotSpecialists(local.ForegroundTile?.sprite?.texture, why);
            if (world.TryGetSubjectSprite(owner, X, Y, out sprite, out _)) NotSpecialists(sprite.texture, why);
        }
        void NotSpecialists(Texture texture, string why)
        {
            Assert.AreNotSame(specialists, texture, why);
            var resource = Resources.Load<Texture2D>(ResourcePath);
            if (resource != null) Assert.AreNotSame(resource, texture, why + ": must not reload runtime art");
        }

        [Test] public void OnlyExactSpecialistRolesAreRecognizedWithoutSourceBlueprintAliases()
        {
            foreach (var name in Names) Assert.True(FlatPixelSharedResidentsCatalog.HasKnownRole(Owner(name)), name);
            Assert.False(FlatPixelSharedResidentsCatalog.HasKnownRole(null));
            foreach (var name in new[] { "Farmer", "Armorer", "Apothecary", "Player", "CreatureCorpse" })
            { var owner = Owner(name); owner.Properties["SourceBlueprint"] = "Tinker"; Assert.False(FlatPixelSharedResidentsCatalog.HasKnownRole(owner), name); }
            var renamed = Owner("Tinker"); renamed.BlueprintName = "tinker"; Assert.False(FlatPixelSharedResidentsCatalog.HasKnownRole(renamed));
        }

        [Test] public void FourthInputOwnsTwoIndependentCachedFramesAndDisposalPreservesBorrowedTexture()
        {
            using (var art = Construct(null, null, null, specialists)) using (var peer = Construct(null, null, null, specialists))
            {
                var frames = new List<Tile>(); var sprites = new List<Sprite>();
                foreach (var name in Names)
                {
                    var owner = Owner(name); Assert.True(art.TryResolve(owner, out var tile)); Native(tile.sprite, name, specialists);
                    Assert.True(art.TryResolve(owner, out var same)); Assert.AreSame(tile, same); frames.Add(tile); sprites.Add(tile.sprite);
                }
                Assert.AreNotSame(frames[0], frames[1]);
                foreach (var name in new[] { "Villager", "Merchant", "Weaponsmith" }) Assert.False(art.TryResolve(Owner(name), out _));
                art.Dispose(); art.Dispose(); foreach (var tile in frames) Assert.True(tile == null); foreach (var sprite in sprites) Assert.True(sprite == null);
                foreach (var name in Names) { Assert.False(art.TryResolve(Owner(name), out _)); Assert.True(peer.TryResolve(Owner(name), out var tile)); Native(tile.sprite, name, specialists); }
                Assert.True(specialists != null); Assert.AreEqual(FilterMode.Bilinear, specialists.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, specialists.wrapMode);
            }
        }

        [TestCase("Tinker", "&c")] [TestCase("WellKeeper", "&C")]
        public void RuntimeResourceUsesExactFrameAcrossWorldAndSubjectRoutesInsideAndOutsideOpening(string name, string color)
        {
            var atlas = Resources.Load<Texture2D>(ResourcePath); Assert.NotNull(atlas, ResourcePath);
            Assert.AreEqual(32, atlas.width); Assert.AreEqual(24, atlas.height);
            foreach (var id in new[] { "Overworld.12.11.0", "Overworld.10.10.0" })
            { Attach(id); var owner = Add(name); Assert.AreEqual(color, owner.GetPart<RenderPart>().ColorString); Routes(owner, atlas); }
        }

        [Test] public void LegacyTwoAndThreeInputConstructorsNeverLoadSpecialistsImplicitly()
        {
            foreach (var inputs in new[] { new[] { cast, merchant }, new[] { cast, merchant, smith } })
                using (var art = Construct(inputs))
                {
                    foreach (var name in Names) Assert.False(art.TryResolve(Owner(name), out _));
                    Assert.True(art.TryResolve(Owner("Villager"), out var v)); Assert.AreSame(cast, v.sprite.texture);
                    Assert.True(art.TryResolve(Owner("Merchant"), out var m)); Assert.AreSame(merchant, m.sprite.texture);
                    Assert.AreEqual(inputs.Length == 3, art.TryResolve(Owner("Weaponsmith"), out var w));
                    if (w != null) Assert.AreSame(smith, w.sprite.texture);
                }
            world.Dispose(); world = new FlatPixelGreyboxCatalog(null, cast);
            foreach (var name in Names)
            { Attach(); var owner = Add(name); Assert.True(world.TryResolvePresentation(zone, X, Y, out var view)); Assert.False(view.NativeForeground); }
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedAtlasIsAuthoritativeAcrossRoutesWhilePeersRemainNative(bool malformed)
        {
            var art = Install(malformed ? Texture(32, 16) : null);
            foreach (var name in Names) { Attach(); Fallback(Add(name), art, name); }
            foreach (var pair in new[] { ("Villager", cast), ("Merchant", merchant), ("Weaponsmith", smith) })
            {
                Attach(); var owner = Add(pair.Item1); Assert.True(art.TryResolve(owner, out var tile)); Assert.AreSame(pair.Item2, tile.sprite.texture);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var view)); Assert.True(view.NativeForeground); Assert.AreSame(pair.Item2, view.ForegroundTile.sprite.texture);
            }
        }

        [TestCase("Tinker")] [TestCase("WellKeeper")]
        public void RealPurchaseChatAndMutableHealthGearAiAndStockDoNotChangeBodyOrTriggerServices(string name)
        {
            Install(specialists); var owner = Add(name); var player = Add("Player", X - 1); var body = Routes(owner, specialists);
            var probe = new Probe(); owner.AddPart(probe); var actions = new InventoryActionList();
            var query = GameEvent.New("GetInventoryActions"); query.SetParameter("Actor", (object)player); query.SetParameter("Actions", (object)actions); owner.FireEventAndRelease(query);
            Assert.True(actions.Actions.Any(a => a.Command == "Chat"));
            var item = TradeSystem.GetTraderStock(owner).First(); int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(player), owner);
            TradeSystem.SetDrams(player, price + 10); int purse = TradeSystem.GetDrams(owner);
            Assert.True(TradeSystem.BuyFromTrader(player, owner, item, out var reason), reason);
            Assert.AreEqual(purse + price, TradeSystem.GetDrams(owner)); Assert.AreEqual(10, TradeSystem.GetDrams(player));
            owner.GetPart<RenderPart>().DisplayName = "A travelling specialist"; owner.GetPart<ConversationPart>().ConversationID = "DifferentConversation";
            owner.GetPart<TraderPart>().StockTable = "DifferentStock"; owner.GetStat("Hitpoints").BaseValue = 1;
            var brain = owner.GetPart<BrainPart>(); brain.Passive = true; brain.Staying = false; brain.Wanders = true;
            var inventory = owner.GetPart<InventoryPart>(); var sword = Owner("LongSword"); inventory.AddObject(sword); Assert.True(inventory.Equip(sword, "TestHand"));
            var stock = inventory.Objects.ToArray(); int goals = brain.GoalCount, services = probe.Services, renders = probe.Renders;
            for (int i = 0; i < 3; i++) Assert.AreSame(body, Routes(owner, specialists));
            CollectionAssert.AreEqual(stock, inventory.Objects); Assert.AreEqual(purse + price, TradeSystem.GetDrams(owner)); Assert.AreEqual(goals, brain.GoalCount);
            Assert.AreEqual(services, probe.Services); Assert.AreEqual(renders, probe.Renders); Assert.AreSame(owner, sword.GetPart<PhysicsPart>().Equipped);
        }

        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void EssentialPartsAreUniqueAndOwnedBeforeAnyOpeningFallback(string fault)
        {
            var art = Install(specialists);
            foreach (var name in Names) foreach (var type in new[] { typeof(RenderPart), typeof(PhysicsPart), typeof(Body), typeof(BrainPart), typeof(ExaminablePart), typeof(InventoryPart), typeof(TraderPart), typeof(ConversationPart) })
            {
                Attach(); var owner = Add(name); Routes(owner, specialists); var part = owner.Parts.First(p => p.GetType() == type);
                if (fault == "missing") Assert.True(owner.RemovePart(part)); else if (fault == "foreign") part.ParentEntity = new Entity(); else owner.AddPart((Part)Activator.CreateInstance(type));
                Fallback(owner, art, name + "/" + type.Name + "/" + fault);
            }
        }

        [Test] public void ReskinsAndNonCreatureTagsCannotBorrowAnExactRoleBody()
        {
            var art = Install(specialists);
            foreach (var name in Names) foreach (var fault in new[] { "glyph", "color", "layer", "visual", "variant", "variants", "background", "item", "terrain", "player", "creature" })
            {
                Attach(); var owner = Add(name); Routes(owner, specialists); var render = owner.GetPart<RenderPart>();
                switch (fault)
                {
                    case "glyph": render.RenderString = "b"; break; case "color": render.ColorString = name == "Tinker" ? "&C" : "&c"; break;
                    case "layer": render.RenderLayer = 1; break; case "visual": render.VisualID = "custom-specialist"; break;
                    case "variant": render.VisualVariant = "custom"; break; case "variants": render.GlyphVariants = "@b"; break;
                    case "background": render.BackgroundColor = "^k"; break; case "item": owner.Tags["Item"] = "true"; break;
                    case "terrain": owner.Tags["Terrain"] = "true"; break; case "player": owner.Tags["Player"] = "true"; break; case "creature": owner.Tags.Remove("Creature"); break;
                }
                Fallback(owner, art, name + "/" + fault);
            }
        }

        [Test] public void CustodyTakeableAndMulticellOwnersDoNotAcquireAWorldBody()
        {
            var art = Install(specialists);
            foreach (var name in Names) foreach (var fault in new[] { "carried", "equipped", "takeable", "multicell" })
            {
                Attach(); var owner = Add(name); Routes(owner, specialists); var physical = owner.GetPart<PhysicsPart>();
                if (fault == "carried") physical.InInventory = new Entity(); if (fault == "equipped") physical.Equipped = new Entity(); if (fault == "takeable") physical.Takeable = true;
                if (fault == "multicell") { owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" }); Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0")); }
                Fallback(owner, art, name + "/" + fault);
            }
        }

        [Test] public void LivingHealthIsMutableButMissingForeignOrDeadStatsRefuse()
        {
            var art = Install(specialists);
            foreach (var name in Names) foreach (var fault in new[] { "missing", "foreign", "dead" })
            {
                Attach(); var owner = Add(name); owner.GetStat("Hitpoints").BaseValue = 1; Routes(owner, specialists);
                if (fault == "missing") owner.Statistics.Remove("Hitpoints"); if (fault == "foreign") owner.GetStat("Hitpoints").Owner = new Entity(); if (fault == "dead") owner.GetStat("Hitpoints").BaseValue = 0;
                Fallback(owner, art, name + "/" + fault);
            }
        }

        [Test] public void ProfessionNameDoesNotAuthorizePropOrServiceParts()
        {
            var art = Install(specialists);
            foreach (var name in Names) foreach (var type in new[] { typeof(ForgePart), typeof(WellPart), typeof(WellSitePart), typeof(ContainerPart), typeof(HarvestablePart), typeof(DestructiblePart), typeof(TileStateSourcePart) })
            { Attach(); var owner = Add(name); Routes(owner, specialists); owner.AddPart((Part)Activator.CreateInstance(type)); Fallback(owner, art, name + "/" + type.Name); }
        }

        [TestCase("Tinker")] [TestCase("WellKeeper")]
        public void ActualDamagePreservesLivingFrameThenDeathRetiresItForSeparateCorpse(string name)
        {
            var art = Install(specialists); var owner = Add(name); var body = Routes(owner, specialists);
            var corpse = owner.GetPart<CorpsePart>(); Assert.NotNull(corpse); Assert.AreEqual(100, corpse.CorpseChance); corpse.TestRng = new System.Random(65);
            var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetEntityCell(owner), null); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            int hp = owner.GetStat("Hitpoints").BaseValue; CombatSystem.ApplyDamage(owner, 1, null, zone); Assert.AreEqual(hp - 1, owner.GetStat("Hitpoints").BaseValue); Assert.AreSame(body, Routes(owner, specialists));
            CombatSystem.ApplyDamage(owner, owner.GetStat("Hitpoints").BaseValue, null, zone);
            Assert.IsNull(zone.GetEntityCell(owner)); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); Assert.False(art.TryResolve(owner, out _));
            Assert.NotNull(corpse.CreatedCorpse); Assert.AreEqual("CreatureCorpse", corpse.CreatedCorpse.BlueprintName); Assert.AreEqual(name, corpse.CreatedCorpse.GetProperty("SourceBlueprint"));
            Assert.False(art.TryResolve(corpse.CreatedCorpse, out _));
            if (world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out _)) NotSpecialists(sprite.texture, "dead subject");
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var after)); NotSpecialists(after.ForegroundTile?.sprite?.texture, "corpse");
        }

        [Test] public void RealPurificationTeachingChangesSettlementCareWithoutReplacingKeeperBody()
        {
            Install(specialists); Attach(SettlementSiteDefinitions.StartingVillageZoneId);
            var keeper = Add("WellKeeper"); keeper.Properties["SettlementId"] = zone.ZoneID; var player = Add("Player", X - 1);
            var settlement = new SettlementManager(() => 0, _ => new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            settlement.GetOrCreateSettlement(zone.ZoneID, new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            var body = Routes(keeper, specialists); Assert.False(WellKeeperPurification.CanTeach(keeper, player));
            var copy = Owner("GrimoireCopy"); copy.GetPart<GrimoirePart>().KnowledgeProperty = SettlementSiteDefinitions.StartingVillageKnowledgeProperty;
            player.GetPart<InventoryPart>().AddObject(copy); Assert.True(WellKeeperPurification.CanTeach(keeper, player));
            Assert.IsNull(WellKeeperPurification.TryTeach(keeper, player, "")); Assert.False(player.GetPart<InventoryPart>().Objects.Contains(copy));
            Assert.True(settlement.HasCondition(zone.ZoneID, WellKeeperPurification.Condition)); Assert.False(WellKeeperPurification.CanTeach(keeper, player));
            Assert.AreSame(body, Routes(keeper, specialists)); Assert.False(keeper.HasPart<WellPart>()); Assert.False(keeper.HasPart<WellSitePart>());
        }

        sealed class Probe : Part
        {
            public int Renders, Services;
            public override bool HandleEvent(GameEvent e)
            { if (e.ID == "Render") Renders++; if (e.ID == "GetInventoryActions" || e.ID == "InventoryAction" || e.ID == "ObjectCreated" || e.ID == IdleQueryEvent.ID) Services++; return true; }
        }
    }
}
