using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelLampwellSundewTests
    {
        const int X=31,Y=12;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelFallbackCatalog selector; FlatPixelShallowCavesCatalog art; FlatPixelGreyboxCatalog world;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope(); manager=OverworldZoneManager.CreateDetached(content.Factory,224401440,true);
            zone=new Zone("Overworld.12.3.2"); manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            selector=new FlatPixelFallbackCatalog();art=new FlatPixelShallowCavesCatalog(selector);Admit(art);world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup(){world?.Dispose();art?.Dispose();selector?.Dispose();content?.Dispose();}
        static void Admit(FlatPixelShallowCavesCatalog a)=>typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingLampwellStone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(a,null);
        Entity Add(string name){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,X,Y));return e;}
        bool Ground()=>art.TryResolveGround(zone,X,Y,out _,out _);
        bool Back()=>art.TryResolveCellBackdrop(zone,X,Y,out _,out _);
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        [TestCase("SandstoneFloor")][TestCase("Rubble")][TestCase("none")]
        public void ThresholdKeepsItsGlyphAndGreetingOnItsRealSubstrate(string substrate)
        {
            var under=substrate=="none"?null:Add(substrate);var e=Add("PebbleSundewThreshold");
            var parts=e.Parts.Select(p=>JsonUtility.ToJson(p)).ToArray();int version=zone.EntityVersion,rep=PlayerReputation.Get("CatacombFolk");string state=zone.TileState.ToSaveString();
            Assert.AreEqual(substrate=="SandstoneFloor",Ground());Assert.AreEqual(substrate!="SandstoneFloor",Back());
            var v=View();Assert.AreSame(e,v.Owner);Assert.False(v.BlocksMovement);Assert.False(v.NativeForeground);
            Assert.AreEqual(substrate=="SandstoneFloor",v.LocalGround);Assert.AreEqual(substrate!="SandstoneFloor",v.AmbientBackdrop);
            Assert.False(art.TryGetSubjectSprite(zone,e,X,Y,out _));
            if(substrate=="SandstoneFloor")
            {Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),v.GroundTile.sprite.texture);Assert.That(v.GroundTile.sprite.rect.x/16,Is.InRange(12,15));Assert.True(art.TryGetSubjectSprite(zone,under,X,Y,out _));}
            else Assert.AreEqual((Color)new Color32(128,106,76,255),v.GroundColor);
            Assert.AreEqual(":",e.GetPart<RenderPart>().RenderString);Assert.AreEqual("&g",e.GetPart<RenderPart>().ColorString);
            CollectionAssert.AreEqual(parts,e.Parts.Select(p=>JsonUtility.ToJson(p)).ToArray());Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(rep,PlayerReputation.Get("CatacombFolk"));Assert.False(e.Properties.ContainsKey(PebbleSundewThresholdPart.GreetedProperty));
        }
        [TestCase("duplicate")][TestCase("glyph")][TestCase("color")][TestCase("background")][TestCase("tile")][TestCase("variant")]
        [TestCase("trigger-faction")][TestCase("consumed")][TestCase("duplicate-trigger")][TestCase("foreign-trigger")][TestCase("other-trigger")]
        [TestCase("missing-trigger")][TestCase("layer")][TestCase("tile-color")][TestCase("detail")][TestCase("visual")][TestCase("wall-tag")][TestCase("null-part")]
        [TestCase("solid")][TestCase("material")][TestCase("light")][TestCase("hearth")][TestCase("foreign-role")]
        [TestCase("water")][TestCase("charge")][TestCase("memory")][TestCase("authority")]
        public void NoncanonicalThresholdOrHazardKeepsFallback(string fault)
        {
            Add("SandstoneFloor");var e=Add("PebbleSundewThreshold");Assert.True(Ground(),"positive premise");var r=e.GetPart<RenderPart>();
            switch(fault)
            {
                case "duplicate":Add("PebbleSundewThreshold");break;
                case "glyph":r.RenderString="!";break;case "color":r.ColorString="&R";break;
                case "background":r.BackgroundColor="^r";break;case "tile":r.Tile="other";break;case "variant":r.GlyphVariants=":;";break;
                case "trigger-faction":e.GetPart<PebbleSundewThresholdPart>().TriggerFaction="other";break;
                case "consumed":e.GetPart<PebbleSundewThresholdPart>().ConsumeOnTrigger=true;break;
                case "duplicate-trigger":e.AddPart(new PebbleSundewThresholdPart());break;
                case "foreign-trigger":e.GetPart<PebbleSundewThresholdPart>().ParentEntity=new Entity();break;
                case "other-trigger":e.AddPart(new GreatdewSnarePart());break;
                case "missing-trigger":e.RemovePart(e.GetPart<PebbleSundewThresholdPart>());break;
                case "layer":r.RenderLayer=3;break;case "tile-color":r.TileColor="&R";break;case "detail":r.DetailColor="&R";break;
                case "visual":r.VisualID="other";break;case "wall-tag":e.Tags.Add("Wall", "");break;
                case "null-part":e.Parts.Add(null);break;
                case "solid":e.GetPart<PhysicsPart>().Solid=true;break;case "material":e.AddPart(new MaterialPart());break;
                case "light":e.AddPart(new LightSourcePart());break;case "hearth":Add("HearthPatch");break;
                case "foreign-role":e.AddPart(new FoundingPlumePart());break;
                case "water":zone.TileState.WriteCoating(X,Y,"water",3);break;case "charge":zone.TileState.AddCharge(X,Y,1);break;
                case "memory":zone.GetCell(X,Y).IsVisible=false;break;case "authority":manager.WorldMap.GetPOI(12,3).Name="other";break;
            }
            Assert.False(Ground());Assert.False(Back());
        }
        [TestCase("hidden")][TestCase("custody")][TestCase("moved")][TestCase("empty-stack")]
        public void UndisclosedThresholdCannotAuthorizeBackdropOrVetoRealFloor(string fault)
        {
            var e=Add("PebbleSundewThreshold");Assert.True(Back(),"positive premise");
            if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;
            if(fault=="custody")e.GetPart<PhysicsPart>().InInventory=new Entity();
            if(fault=="moved")zone.RemoveEntity(e);
            if(fault=="empty-stack")e.AddPart(new StackerPart{StackCount=0});
            Assert.False(Back());Add("SandstoneFloor");Assert.True(Ground());
        }
        [TestCase("duplicate")][TestCase("other-trigger")][TestCase("hearth")]
        public void BareThresholdWithdrawsAmbientWhenAmbiguous(string fault)
        {
            var e=Add("PebbleSundewThreshold");Assert.True(Back());
            if(fault=="duplicate")Add("PebbleSundewThreshold");else if(fault=="hearth")Add("HearthPatch");else e.AddPart(new GreatdewSnarePart());
            Assert.False(Back());Assert.False(View().AmbientBackdrop);Assert.False(Ground());
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedRealFloorNeverBecomesAmbient(bool malformed)
        {
            Add("PebbleSundewThreshold");var floor=Add("SandstoneFloor");Assert.True(Ground());
            if(malformed){floor.GetPart<RenderPart>().ColorString="&R";Assert.False(Ground());Assert.False(Back());}
            else using(var missing=new FlatPixelShallowCavesCatalog(null,null,Resources.Load<Texture2D>("Sprites/Environment/rubble"),null,null,null,selector))
            {Admit(missing);Assert.False(missing.TryResolveGround(zone,X,Y,out _,out _));Assert.False(missing.TryResolveCellBackdrop(zone,X,Y,out _,out _));}
        }
        [TestCase("missing")][TestCase("changed")][TestCase("duplicate")][TestCase("stalagmite")]
        public void RubbleThresholdRequiresOneSupportedRubble(string fault)
        {
            Add("PebbleSundewThreshold");var e=Add("Rubble");Assert.True(Back());
            if(fault=="changed")e.GetPart<RenderPart>().ColorString="&R";
            if(fault=="duplicate")Add("Rubble");
            if(fault=="stalagmite"){zone.RemoveEntity(e);Add("Stalagmite");}
            if(fault=="missing")using(var missing=new FlatPixelShallowCavesCatalog(null,null,null,null,null,null,selector))
            {Admit(missing);Assert.False(missing.TryResolveCellBackdrop(zone,X,Y,out _,out _));return;}
            Assert.False(Back());
        }
        [TestCase("Overworld.12.3.1")][TestCase("Overworld.16.4.2")][TestCase("Overworld.3.3.1")]
        public void OtherChambersDoNotGainThresholdException(string id)
        {
            zone=new Zone(id);manager.SetActiveZone(zone);zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;
            Add("PebbleSundewThreshold");Assert.False(Back());Add("SandstoneFloor");Assert.False(Ground());
        }
        [Test] public void AuthorityRecoversAndMemoryCannotBorrowLiveBacking()
        {
            Add("PebbleSundewThreshold");Assert.True(Back());var p=manager.WorldMap.GetPOI(12,3);p.Name="other";Assert.False(Back());p.Name="Lampwell";Assert.True(Back());
            zone.GetCell(X,Y).IsVisible=false;Assert.False(Back());Assert.False(View().AmbientBackdrop);zone.GetCell(X,Y).IsVisible=true;Assert.True(Back());
            manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);Assert.False(Back());
        }
        [Test] public void DefaultAndSpivenorCatalogsCannotBorrowLampwellThresholdException()
        {
            Add("PebbleSundewThreshold");Add("SandstoneFloor");Assert.True(Ground());
            using(var other=new FlatPixelShallowCavesCatalog(selector))
            {Assert.False(other.TryResolveGround(zone,X,Y,out _,out _));typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingSpivenorStone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(other,null);Assert.False(other.TryResolveGround(zone,X,Y,out _,out _));}
        }
    }
}
