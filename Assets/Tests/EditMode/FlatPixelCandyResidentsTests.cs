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
        const string Factor = "Sprites/Environment/concord_factor";
        Entity Candy(bool clerk = false)
        {
            zone = Attach("Overworld.7.8.0");
            var e = Add("Villager");
            e.Properties["SettlementId"] = zone.ZoneID;
            var r = e.GetPart<RenderPart>();
            r.RenderString = clerk ? "P" : "c"; r.ColorString = clerk ? "&W" : "&m";
            r.DisplayName = clerk ? "Clerk Padok" : "villager";
            e.GetPart<ConversationPart>().ConversationID = clerk ? "CandyTax_Quest" : "CandyCitizen";
            if (clerk) e.AddPart(new QuestBeaconPart { Quest = "TheCandyTax" });
            return e;
        }
        Texture2D CandyTexture(bool clerk) => Resources.Load<Texture2D>(clerk ? Factor : Cast);
        Tile CandyNative(Entity e, bool clerk)
        {
            var v = View(); Assert.AreSame(e, v.Owner); Assert.True(v.NativeForeground);
            Assert.AreSame(CandyTexture(clerk), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,clerk ? 16 : 24), v.ForegroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _));
            Assert.AreSame(v.ForegroundTile.sprite,s); return v.ForegroundTile;
        }
        void CandyRefused(Entity e, bool clerk)
        {
            using (var a = new FlatPixelSharedResidentsCatalog()) Assert.False(a.TryResolve(e,out _));
            if (world.TryResolvePresentation(zone,X,Y,out var v)) Assert.False(v.NativeForeground);
            if (world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _))
                Assert.AreNotSame(CandyTexture(clerk),s.texture);
        }
        [TestCase(false)][TestCase(true)]
        public void CandyAuthoredRolesShareWorldAndReaderWithoutSimulationMutation(bool clerk)
        {
            var e=Candy(clerk); var viewer=Add("Player",X-1,Y); var tile=CandyNative(e,clerk);
            e.Properties["candy_taxed"]="1"; e.GetPart<RenderPart>().DisplayName="Renamed resident";
            e.GetStat("Hitpoints").BaseValue=1;
            var inventory=e.GetPart<InventoryPart>().Objects.ToArray(); var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone)) using(var portraits=Portraits())
            {
                paint.Step(); Assert.AreSame(tile,paint.At(X,Y)); var count=probe.ServiceQueries;
                Assert.AreSame(tile,CandyNative(e,clerk));
                Assert.AreSame(CandyTexture(clerk),portraits.GetPortrait(Document(e,viewer),paint.Renderer).Texture);
                Assert.AreEqual(count,probe.ServiceQueries); CollectionAssert.AreEqual(inventory,e.GetPart<InventoryPart>().Objects);
                Assert.AreEqual("1",e.GetProperty("candy_taxed"));
            }
        }
        [TestCase("conversation")][TestCase("foreign-conversation")][TestCase("duplicate-conversation")]
        [TestCase("color")][TestCase("glyph")][TestCase("visual")][TestCase("settlement")]
        [TestCase("dead")][TestCase("corpse")][TestCase("inventory")][TestCase("container")]
        [TestCase("footprint")][TestCase("invisible")][TestCase("foreign-render")]
        public void CandyAliasesNeverBorrowThroughForeignOrChangedMeaning(string fault)
        {
            foreach(bool clerk in new[]{false,true})
            {
                var e=Candy(clerk);CandyNative(e,clerk);
                switch(fault)
                {
                    case "conversation":e.GetPart<ConversationPart>().ConversationID="Crunchy_Quest";break;
                    case "foreign-conversation":e.GetPart<ConversationPart>().ParentEntity=new Entity();break;
                    case "duplicate-conversation":e.AddPart(new ConversationPart{ConversationID="CandyCitizen"});break;
                    case "color":e.GetPart<RenderPart>().ColorString="&g";break;
                    case "glyph":e.GetPart<RenderPart>().RenderString="b";break;
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
                CandyRefused(e,clerk);
            }
        }
        [TestCase("missing")][TestCase("foreign")][TestCase("duplicate")][TestCase("wrong")]
        public void CandyClerkRequiresHisOwnedQuestBeacon(string fault)
        {
            var e=Candy(true);CandyNative(e,true);var b=e.GetPart<QuestBeaconPart>();
            if(fault=="missing")e.RemovePart(b);if(fault=="foreign")b.ParentEntity=new Entity();
            if(fault=="duplicate")e.AddPart(new QuestBeaconPart{Quest="TheCandyTax"});if(fault=="wrong")b.Quest="CrunchyLocket";
            CandyRefused(e,true);
        }
        [TestCase("cache")][TestCase("replaced")][TestCase("profile")][TestCase("biome")][TestCase("lair")]
        [TestCase("other-zone")][TestCase("depth")][TestCase("alias")]
        public void CandyPilotRequiresCurrentGantryGraphAndRestoresOrdinaryResidentsElsewhere(string fault)
        {
            var e=Candy();CandyNative(e,false);
            if(fault=="cache")manager.CachedZones.Remove(zone.ZoneID);
            if(fault=="replaced")manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);
            if(fault=="profile")manager.WorldMap.GetPOI(7,8).Profile="Other";
            if(fault=="biome")manager.WorldMap.Tiles[7,8]=BiomeType.Jungle;
            if(fault=="lair")
            {
                var poi=manager.WorldMap.GetPOI(7,8);
                manager.WorldMap.SetPOI(7,8,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));
                LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(7,8,poi);
            }
            if(fault=="other-zone"||fault=="depth"||fault=="alias")
            {
                zone.RemoveEntity(e);zone=Attach(fault=="other-zone"?"Overworld.6.8.0":fault=="depth"?"Overworld.7.8.1":"Overworld.07.8.0");zone.AddEntity(e,X,Y);
            }
            CandyRefused(e,false);
            zone=Attach("StandaloneResidents");NativeOwner(Add("Villager"));
        }
        [TestCase(false)][TestCase(true)]
        public void CandyHiddenCoveredAndReplacedSubjectsLoseReaderAuthority(bool clerk)
        {
            var e=Candy(clerk);CandyNative(e,clerk);var doc=Document(e);
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
        public void CandyMissingFactorNeverReloadsOrDamagesBorrowedCitizenAtlas(bool malformed)
        {
            var cast=Texture(64,24);var factor=malformed?Texture(17,17):null;
            var ctor=ArtType().GetConstructor(Enumerable.Repeat(typeof(Texture2D),8).ToArray());Assert.NotNull(ctor);
            var e=Candy(true);
            using(var a=(IDisposable)ctor.Invoke(new object[]{cast,null,null,null,null,null,null,factor}))
            {Assert.False(Resolve(a)(e,out _));e=Candy();Assert.True(Resolve(a)(e,out var tile));Assert.AreSame(cast,tile.sprite.texture);}
            Assert.True(cast!=null);if(malformed)Assert.True(factor!=null);
        }
        [Test]
        public void CandyFactorDisposesOnlyOwnedViewsAndKeepsPeerUsable()
        {
            var e=Candy(true);var cast=Texture(64,24);var factor=Texture(16,16);
            using(var a=new FlatPixelSharedResidentsCatalog(cast,null,null,null,null,null,null,factor))
            using(var peer=new FlatPixelSharedResidentsCatalog(cast,null,null,null,null,null,null,factor))
            {
                Assert.True(a.TryResolve(e,out var tile));var sprite=tile.sprite;
                a.Dispose();a.Dispose();Assert.True(tile==null);Assert.True(sprite==null);
                Assert.True(cast!=null&&factor!=null);Assert.False(a.TryResolve(e,out _));
                Assert.True(peer.TryResolve(e,out var other));Assert.AreSame(factor,other.sprite.texture);
            }
        }
        [Test]
        public void CandyCitizenRejectsAnAddedForeignQuestRoleAndRecovers()
        {
            var e=Candy();CandyNative(e,false);var beacon=new QuestBeaconPart{Quest="CrunchyLocket"};
            e.AddPart(beacon);CandyRefused(e,false);e.RemovePart(beacon);CandyNative(e,false);
        }
    }
}
