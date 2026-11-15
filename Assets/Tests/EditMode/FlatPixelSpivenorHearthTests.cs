using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelSpivenorHearthTests
    {
        const int X=25,Y=12;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelFallbackCatalog selector; FlatPixelShallowCavesCatalog art; FlatPixelGreyboxCatalog world;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope(); manager=OverworldZoneManager.CreateDetached(content.Factory,224401440,true);
            zone=new Zone("Overworld.16.4.2"); manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            selector=new FlatPixelFallbackCatalog();art=new FlatPixelShallowCavesCatalog(selector);Admit(art);world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup(){world?.Dispose();art?.Dispose();selector?.Dispose();content?.Dispose();}
        static void Admit(FlatPixelShallowCavesCatalog a)=>typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingSpivenorStone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(a,null);
        Entity Add(string name){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,X,Y));return e;}
        bool Ground()=>art.TryResolveGround(zone,X,Y,out _,out _);
        bool Back()=>art.TryResolveCellBackdrop(zone,X,Y,out _,out _);
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        [TestCase("SandstoneFloor")][TestCase("Rubble")][TestCase("none")]
        public void WholeCircleUsesOnlyItsRealSubstrateAndRetainsHearthIdentity(string substrate)
        {
            var under=substrate=="none"?null:Add(substrate);var e=Add("HearthPatch");
            var parts=e.Parts.Select(p=>JsonUtility.ToJson(p)).ToArray();int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();
            Assert.AreEqual(substrate=="SandstoneFloor",Ground());Assert.AreEqual(substrate!="SandstoneFloor",Back());
            var v=View();Assert.AreSame(e,v.Owner);Assert.True(v.BlocksMovement);Assert.False(v.NativeForeground);
            Assert.AreEqual(substrate=="SandstoneFloor",v.LocalGround);Assert.AreEqual(substrate!="SandstoneFloor",v.AmbientBackdrop);
            Assert.False(art.TryGetSubjectSprite(zone,e,X,Y,out _));
            if(substrate=="SandstoneFloor")
            {Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),v.GroundTile.sprite.texture);Assert.That(v.GroundTile.sprite.rect.x/16,Is.InRange(12,15));Assert.True(art.TryGetSubjectSprite(zone,under,X,Y,out _));}
            else Assert.AreEqual((Color)new Color32(128,106,76,255),v.GroundColor);
            Assert.AreEqual("*",e.GetPart<RenderPart>().RenderString);Assert.AreEqual("&G",e.GetPart<RenderPart>().ColorString);
            CollectionAssert.AreEqual(parts,e.Parts.Select(p=>JsonUtility.ToJson(p)).ToArray());Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [TestCase("duplicate")][TestCase("glyph")][TestCase("color")][TestCase("background")][TestCase("tile")][TestCase("variant")]
        [TestCase("light")][TestCase("duplicate-light")][TestCase("foreign-light")][TestCase("duplicate-hearth")][TestCase("foreign-role")]
        [TestCase("solid")][TestCase("material")][TestCase("gone")][TestCase("zero")][TestCase("frozen")][TestCase("burning")][TestCase("nan")]
        [TestCase("water")][TestCase("charge")][TestCase("memory")][TestCase("authority")]
        public void VisibleNoncanonicalHearthAndHazardsKeepFallback(string fault)
        {
            Add("SandstoneFloor");var e=Add("HearthPatch");Assert.True(Ground(),"positive premise");
            var r=e.GetPart<RenderPart>();var hp=e.GetPart<DestructiblePart>();var t=e.GetPart<ThermalPart>();
            switch(fault)
            {
                case "duplicate":Add("HearthPatch");break;
                case "glyph":r.RenderString="!";break;case "color":r.ColorString="&R";break;
                case "background":r.BackgroundColor="^r";break;case "tile":r.Tile="other";break;case "variant":r.GlyphVariants="*+";break;
                case "light":e.GetPart<LightSourcePart>().Radius=5;break;
                case "duplicate-light":e.AddPart(new LightSourcePart());break;
                case "foreign-light":e.GetPart<LightSourcePart>().ParentEntity=new Entity();break;
                case "duplicate-hearth":e.AddPart(new HearthPatchPart());break;
                case "foreign-role":e.AddPart(new FoundingPlumePart());break;
                case "solid":e.GetPart<PhysicsPart>().Solid=false;break;
                case "material":e.GetPart<MaterialPart>().MaterialID="Stone";break;
                case "gone":hp.Gone=true;break;case "zero":hp.HP=0;break;
                case "frozen":t.Temperature=t.FreezeTemperature;break;case "burning":t.Temperature=t.FlameTemperature;break;case "nan":t.Temperature=float.NaN;break;
                case "water":zone.TileState.WriteCoating(X,Y,"water",3);break;case "charge":zone.TileState.AddCharge(X,Y,1);break;
                case "memory":zone.GetCell(X,Y).IsVisible=false;break;case "authority":manager.WorldMap.GetPOI(16,4).Name="other";break;
            }
            Assert.False(Ground());Assert.False(Back());
        }
        [TestCase("hidden")][TestCase("custody")][TestCase("moved")]
        public void UndisclosedHearthCannotAuthorizeBackdropOrVetoRealFloor(string fault)
        {
            var e=Add("HearthPatch");Assert.True(Back(),"positive premise");
            if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;
            if(fault=="custody")e.GetPart<PhysicsPart>().InInventory=new Entity();
            if(fault=="moved")zone.RemoveEntity(e);
            Assert.False(Back());Add("SandstoneFloor");Assert.True(Ground());
        }
        [TestCase("duplicate")][TestCase("foreign-role")]
        public void FloorlessHearthWithdrawsBackingWhenItsOwnerBecomesAmbiguous(string fault)
        {
            var e=Add("HearthPatch");Assert.True(Back());Assert.True(View().AmbientBackdrop);
            if(fault=="duplicate")Add("HearthPatch");else e.AddPart(new FoundingPlumePart());
            Assert.False(Back());Assert.False(View().AmbientBackdrop);Assert.False(Ground());
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedRealFloorNeverBecomesAmbient(bool malformed)
        {
            Add("HearthPatch");var floor=Add("SandstoneFloor");Assert.True(Ground());
            if(malformed){floor.GetPart<RenderPart>().ColorString="&R";Assert.False(Ground());Assert.False(Back());}
            else using(var missing=new FlatPixelShallowCavesCatalog(null,null,Resources.Load<Texture2D>("Sprites/Environment/rubble"),null,null,null,selector))
            {Admit(missing);Assert.False(missing.TryResolveGround(zone,X,Y,out _,out _));Assert.False(missing.TryResolveCellBackdrop(zone,X,Y,out _,out _));}
        }
        [TestCase("missing")][TestCase("changed")][TestCase("duplicate")][TestCase("stalagmite")]
        public void RubbleWithHearthNeedsExactlyOneSupportedRubble(string fault)
        {
            Add("HearthPatch");var e=Add("Rubble");Assert.True(Back());
            if(fault=="changed")e.GetPart<RenderPart>().ColorString="&R";
            if(fault=="duplicate")Add("Rubble");
            if(fault=="stalagmite"){zone.RemoveEntity(e);Add("Stalagmite");}
            if(fault=="missing")using(var missing=new FlatPixelShallowCavesCatalog(null,null,null,null,null,null,selector))
            {Admit(missing);Assert.False(missing.TryResolveCellBackdrop(zone,X,Y,out _,out _));return;}
            Assert.False(Back());
        }
        [Test] public void PartialHealthAndDisabledLightArePreservedWithoutTriggeringPolitics()
        {
            var e=Add("HearthPatch");e.GetPart<DestructiblePart>().HP=17;e.GetPart<LightSourcePart>().Enabled=false;
            int rep=PlayerReputation.Get("CatacombFolk");Assert.True(Back());Assert.AreEqual(17,e.GetPart<DestructiblePart>().HP);
            Assert.False(e.GetPart<LightSourcePart>().Enabled);Assert.AreEqual(rep,PlayerReputation.Get("CatacombFolk"));
        }
        [TestCase("Overworld.16.4.1")][TestCase("Overworld.3.3.1")][TestCase("Overworld.2.4.1")]
        public void SameOwnersElsewhereDoNotGainHearthException(string id)
        {
            zone=new Zone(id);manager.SetActiveZone(zone);zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;
            Add("HearthPatch");Assert.False(Back());Add("SandstoneFloor");Assert.False(Ground());
        }
        [Test] public void CurrentAuthorityRecoversAndMemoryCannotUseLiveBacking()
        {
            Add("HearthPatch");Assert.True(Back());var p=manager.WorldMap.GetPOI(16,4);p.Name="other";Assert.False(Back());p.Name="Spivenor";Assert.True(Back());
            zone.GetCell(X,Y).IsVisible=false;Assert.False(Back());Assert.False(View().AmbientBackdrop);zone.GetCell(X,Y).IsVisible=true;Assert.True(Back());
            manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);Assert.False(Back());
        }
    }
}
