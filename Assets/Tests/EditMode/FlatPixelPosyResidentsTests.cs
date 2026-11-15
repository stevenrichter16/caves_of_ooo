using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelSharedResidentsTests
    {
        const string PosyCook = "Sprites/Live16x24/east-kitchen-cook";
        Entity Posy(bool baker = false)
        {
            zone = Attach("Overworld.5.9.0");
            var e = Add("Villager");
            e.Properties["SettlementId"] = zone.ZoneID;
            var r = e.GetPart<RenderPart>();
            r.RenderString = baker ? "b" : "h"; r.ColorString = baker ? "&w" : "&K";
            r.DisplayName = baker ? "worried baker" : "the hermit";
            e.GetPart<ConversationPart>().ConversationID = baker ? "Baker_Quest" : "Hermit_Quest";
            if (baker) e.AddPart(new QuestBeaconPart { Quest = "MessageForHermit" });
            return e;
        }
        Texture2D PosyTexture(bool baker) => Resources.Load<Texture2D>(baker ? PosyCook : Cast);
        Tile PosyNative(Entity e, bool baker)
        {
            var v = View(); Assert.AreSame(e, v.Owner); Assert.True(v.NativeForeground);
            Assert.AreSame(PosyTexture(baker), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,24), v.ForegroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _));
            Assert.AreSame(v.ForegroundTile.sprite,s); return v.ForegroundTile;
        }
        void PosyRefused(Entity e, bool baker)
        {
            using (var a = new FlatPixelSharedResidentsCatalog()) Assert.False(a.TryResolve(e,out _));
            if (world.TryResolvePresentation(zone,X,Y,out var v)) Assert.False(v.NativeForeground);
            if (world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _))
                Assert.AreNotSame(PosyTexture(baker),s.texture);
        }
        [TestCase(false)][TestCase(true)]
        public void PosyAuthoredRolesShareWorldAndReaderWithoutSimulationMutation(bool baker)
        {
            var e=Posy(baker); var viewer=Add("Player",X-1,Y); var tile=PosyNative(e,baker);
            e.Properties["message_appearance_test"]="1"; e.GetPart<RenderPart>().DisplayName="Renamed resident";
            e.GetStat("Hitpoints").BaseValue=1;
            var inventory=e.GetPart<InventoryPart>().Objects.ToArray(); var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone)) using(var portraits=Portraits())
            {
                paint.Step(); Assert.AreSame(tile,paint.At(X,Y)); var count=probe.ServiceQueries;
                Assert.AreSame(tile,PosyNative(e,baker));
                Assert.AreSame(PosyTexture(baker),portraits.GetPortrait(Document(e,viewer),paint.Renderer).Texture);
                Assert.AreEqual(count,probe.ServiceQueries); CollectionAssert.AreEqual(inventory,e.GetPart<InventoryPart>().Objects);
                Assert.AreEqual("1",e.GetProperty("message_appearance_test"));
            }
        }
        [TestCase("conversation")][TestCase("foreign-conversation")][TestCase("duplicate-conversation")]
        [TestCase("color")][TestCase("glyph")][TestCase("visual")][TestCase("settlement")]
        [TestCase("dead")][TestCase("corpse")][TestCase("inventory")][TestCase("container")]
        [TestCase("footprint")][TestCase("invisible")][TestCase("foreign-render")]
        public void PosyAliasesNeverBorrowThroughForeignOrChangedMeaning(string fault)
        {
            foreach(bool baker in new[]{false,true})
            {
                var e=Posy(baker);PosyNative(e,baker);
                switch(fault)
                {
                    case "conversation":e.GetPart<ConversationPart>().ConversationID="Crunchy_Quest";break;
                    case "foreign-conversation":e.GetPart<ConversationPart>().ParentEntity=new Entity();break;
                    case "duplicate-conversation":e.AddPart(new ConversationPart{ConversationID="Hermit_Quest"});break;
                    case "color":e.GetPart<RenderPart>().ColorString="&g";break;
                    case "glyph":e.GetPart<RenderPart>().RenderString="z";break;
                    case "visual":e.GetPart<RenderPart>().VisualID="another-role";break;
                    case "settlement":e.Properties["SettlementId"]="Overworld.13.7.0";break;
                    case "dead":e.GetStat("Hitpoints").BaseValue=0;break;
                    case "corpse":e.Tags["Corpse"]="";break;
                    case "inventory":e.GetPart<PhysicsPart>().InInventory=new Entity();break;
                    case "container":e.AddPart(new ContainerPart());break;
                    case "footprint":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});break;
                    case "invisible":e.GetPart<RenderPart>().Visible=false;break;
                    case "foreign-render":e.GetPart<RenderPart>().ParentEntity=new Entity();break;
                }
                PosyRefused(e,baker);
            }
        }
        [TestCase("missing")][TestCase("foreign")][TestCase("duplicate")][TestCase("wrong")]
        public void PosyBakerRequiresHisOwnedQuestBeacon(string fault)
        {
            var e=Posy(true);PosyNative(e,true);var b=e.GetPart<QuestBeaconPart>();
            if(fault=="missing")e.RemovePart(b);if(fault=="foreign")b.ParentEntity=new Entity();
            if(fault=="duplicate")e.AddPart(new QuestBeaconPart{Quest="MessageForHermit"});if(fault=="wrong")b.Quest="CrunchyLocket";
            PosyRefused(e,true);
        }
        [TestCase("cache")][TestCase("replaced")][TestCase("profile")][TestCase("biome")][TestCase("lair")]
        [TestCase("other-zone")][TestCase("depth")][TestCase("alias")]
        public void PosyPilotRequiresCurrentPosyGraphAndRestoresOrdinaryResidentsElsewhere(string fault)
        {
            var e=Posy();PosyNative(e,false);
            if(fault=="cache")manager.CachedZones.Remove(zone.ZoneID);
            if(fault=="replaced")manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);
            if(fault=="profile")manager.WorldMap.GetPOI(5,9).Profile="Other";
            if(fault=="biome")manager.WorldMap.Tiles[5,9]=BiomeType.Jungle;
            if(fault=="lair")
            {
                var poi=manager.WorldMap.GetPOI(5,9);
                manager.WorldMap.SetPOI(5,9,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));
                LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(5,9,poi);
            }
            if(fault=="other-zone"||fault=="depth"||fault=="alias")
            {
                zone.RemoveEntity(e);zone=Attach(fault=="other-zone"?"Overworld.6.8.0":fault=="depth"?"Overworld.5.9.1":"Overworld.05.9.0");zone.AddEntity(e,X,Y);
            }
            PosyRefused(e,false);
            zone=Attach("StandaloneResidents");NativeOwner(Add("Villager"));
        }
        [TestCase(false)][TestCase(true)]
        public void PosyHiddenCoveredAndReplacedSubjectsLoseReaderAuthority(bool baker)
        {
            var e=Posy(baker);PosyNative(e,baker);var doc=Document(e);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var tile=paint.At(X,Y);zone.GetCell(X,Y).IsVisible=false;
                paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                zone.GetCell(X,Y).IsVisible=true;var player=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreNotSame(tile,paint.At(X,Y));
                zone.RemoveEntity(player);paint.Dirty(X,Y);paint.Step();Assert.AreSame(tile,paint.At(X,Y));
                Attach(zone.ZoneID);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
            }
        }
        [TestCase(false)][TestCase(true)]
        public void PosyMissingCookNeverReloadsOrDamagesBorrowedCitizenAtlas(bool malformed)
        {
            var cast=Texture(64,24);var cook=malformed?Texture(17,17):null;
            var ctor=ArtType().GetConstructor(Enumerable.Repeat(typeof(Texture2D),9).ToArray());Assert.NotNull(ctor);
            var e=Posy(true);
            using(var a=(IDisposable)ctor.Invoke(new object[]{cast,null,null,null,null,null,null,null,cook}))
            {Assert.False(Resolve(a)(e,out _));e=Posy();Assert.True(Resolve(a)(e,out var tile));Assert.AreSame(cast,tile.sprite.texture);}
            Assert.True(cast!=null);if(malformed)Assert.True(cook!=null);
        }
        [Test]
        public void PosyCookDisposesOnlyOwnedViewsAndKeepsPeerUsable()
        {
            var e=Posy(true);var cast=Texture(64,24);var cook=Texture(16,24);
            var ctor=ArtType().GetConstructor(Enumerable.Repeat(typeof(Texture2D),9).ToArray());Assert.NotNull(ctor);
            using(var a=(FlatPixelSharedResidentsCatalog)ctor.Invoke(new object[]{cast,null,null,null,null,null,null,null,cook}))
            using(var peer=(FlatPixelSharedResidentsCatalog)ctor.Invoke(new object[]{cast,null,null,null,null,null,null,null,cook}))
            {
                Assert.True(a.TryResolve(e,out var tile));var sprite=tile.sprite;
                a.Dispose();a.Dispose();Assert.True(tile==null);Assert.True(sprite==null);
                Assert.True(cast!=null&&cook!=null);Assert.False(a.TryResolve(e,out _));
                Assert.True(peer.TryResolve(e,out var other));Assert.AreSame(cook,other.sprite.texture);
            }
        }
        [Test]
        public void PosyHermitRejectsAnAddedForeignQuestRoleAndRecovers()
        {
            var e=Posy();PosyNative(e,false);var beacon=new QuestBeaconPart{Quest="CrunchyLocket"};
            e.AddPart(beacon);PosyRefused(e,false);e.RemovePart(beacon);PosyNative(e,false);
        }
        [TestCase(false)][TestCase(true)]
        public void PosyQuestPhasesChangeCueWithoutChangingNativeBody(bool baker)
        {
            var old=StoryletPart.Current;var oldPlayer=StoryletPart.LocalPlayer;
            using(var registry=new QuestCueRegistryFixture())
            try
            {
                var e=Posy(baker);var player=Add("Player",X-1,Y);StoryletPart.LocalPlayer=player;
                StoryletPart.Current=new StoryletPart();var tile=PosyNative(e,baker);
                Assert.AreEqual(baker?QuestCueState.Available:QuestCueState.None,QuestCueStateQuery.Evaluate(e,zone,player));
                StoryletPart.Current.StartQuest(new QuestState{QuestId="MessageForHermit"});
                Assert.AreSame(tile,PosyNative(e,baker));
                Assert.AreEqual(baker?QuestCueState.Active:QuestCueState.None,QuestCueStateQuery.Evaluate(e,zone,player));
                StoryletPart.Current.MarkQuestCompleted("MessageForHermit");
                Assert.AreSame(tile,PosyNative(e,baker));
                Assert.AreEqual(QuestCueState.None,QuestCueStateQuery.Evaluate(e,zone,player));
            }
            finally {StoryletPart.Current=old;StoryletPart.LocalPlayer=oldPlayer;}
        }

    }
}
