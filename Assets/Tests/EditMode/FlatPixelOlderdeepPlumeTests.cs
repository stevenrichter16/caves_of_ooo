using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelOlderdeepPlumeTests
    {
        const int X=55,Y=12;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelFallbackCatalog selector; FlatPixelShallowCavesCatalog art; FlatPixelGreyboxCatalog world;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            zone=new Zone("Overworld.4.6.2");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            selector=new FlatPixelFallbackCatalog();art=Make(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath));world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup(){world?.Dispose();art?.Dispose();selector?.Dispose();content?.Dispose();}
        static void Admit(FlatPixelShallowCavesCatalog a)=>typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingOlderdeepDescent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(a,null);
        FlatPixelShallowCavesCatalog Make(Texture2D stone)
        {
            var a=new FlatPixelShallowCavesCatalog(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),null,null,null,null,selector,stone);Admit(a);return a;
        }
        Entity Add(string name){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,X,Y));return e;}
        bool Ground()=>art.TryResolveGround(zone,X,Y,out _,out _);
        bool Back()=>art.TryResolveCellBackdrop(zone,X,Y,out _,out _);
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        [TestCase(false)][TestCase(true)] public void RealStoneBacksPlumeWithoutChangingOwnerOrRest(bool covered)
        {
            var floor=Add("StoneFloor");var e=Add("FoundingPlume");if(covered)Add("Player");
            var parts=e.Parts.Select(p=>JsonUtility.ToJson(p)).ToArray();int version=zone.EntityVersion,rep=PlayerReputation.Get("CatacombFolk");string state=zone.TileState.ToSaveString();
            Assert.True(Ground());Assert.False(Back());var v=View();Assert.True(v.LocalGround);Assert.False(v.AmbientBackdrop);if(!covered)Assert.False(v.BlocksMovement);
            if(!covered){Assert.AreSame(e,v.Owner);Assert.False(v.NativeForeground);}
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath),v.GroundTile.sprite.texture);Assert.That(v.GroundTile.sprite.rect.x/16,Is.InRange(4,7));
            Assert.False(art.TryGetSubjectSprite(zone,e,X,Y,out _));Assert.True(art.TryGetSubjectSprite(zone,floor,X,Y,out _));
            CollectionAssert.AreEqual(parts,e.Parts.Select(p=>JsonUtility.ToJson(p)).ToArray());Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(rep,PlayerReputation.Get("CatacombFolk"));
        }
        [TestCase("duplicate")][TestCase("glyph")][TestCase("color")][TestCase("background")][TestCase("tile")][TestCase("variant")]
        [TestCase("layer")][TestCase("tile-color")][TestCase("detail")][TestCase("visual")][TestCase("facing")][TestCase("wall-tag")]
        [TestCase("null-part")][TestCase("solid")][TestCase("takeable")][TestCase("foreign-role")][TestCase("missing-plume")][TestCase("duplicate-plume-part")][TestCase("foreign-plume-part")]
        [TestCase("material")][TestCase("combustibility")][TestCase("burning")][TestCase("frozen")][TestCase("nan-temperature")]
        [TestCase("dead")][TestCase("gone")][TestCase("maxhp")][TestCase("hardness")][TestCase("indestructible")][TestCase("wreckage")]
        [TestCase("light-radius")][TestCase("light-color")][TestCase("light-intensity")][TestCase("light-disabled")][TestCase("war-rep")]
        [TestCase("hearth")][TestCase("sundew")][TestCase("water")][TestCase("charge")][TestCase("memory")][TestCase("unexplored")]
        [TestCase("duplicate-floor")][TestCase("changed-floor")][TestCase("authority")]
        public void ChangedOwnerOrHazardRetainsFallback(string fault)
        {
            var f=Add("StoneFloor");var e=Add("FoundingPlume");Assert.True(Ground(),"positive premise");var r=e.GetPart<RenderPart>();
            switch(fault)
            {
                case "duplicate":Add("FoundingPlume");break;case "glyph":r.RenderString="!";break;case "color":r.ColorString="&R";break;
                case "background":r.BackgroundColor="^r";break;case "tile":r.Tile="other";break;case "variant":r.GlyphVariants="*:";break;
                case "layer":r.RenderLayer=3;break;case "tile-color":r.TileColor="&R";break;case "detail":r.DetailColor="&R";break;case "visual":r.VisualID="other";break;case "facing":r.VisualFacing=(EntityVisualFacing)1;break;
                case "wall-tag":e.Tags.Add("Wall","");break;case "null-part":e.Parts.Add(null);break;
                case "solid":e.GetPart<PhysicsPart>().Solid=true;break;case "takeable":e.GetPart<PhysicsPart>().Takeable=true;break;case "foreign-role":e.AddPart(new GreatdewSnarePart());break;
                case "missing-plume":e.RemovePart(e.GetPart<FoundingPlumePart>());break;case "duplicate-plume-part":e.AddPart(new FoundingPlumePart());break;case "foreign-plume-part":e.GetPart<FoundingPlumePart>().ParentEntity=new Entity();break;
                case "material":e.GetPart<MaterialPart>().MaterialID="Stone";break;case "combustibility":e.GetPart<MaterialPart>().Combustibility=0;break;
                case "burning":e.GetPart<ThermalPart>().Temperature=420;break;case "frozen":e.GetPart<ThermalPart>().Temperature=0;break;case "nan-temperature":e.GetPart<ThermalPart>().Temperature=float.NaN;break;
                case "dead":e.GetPart<DestructiblePart>().HP=0;break;case "gone":e.GetPart<DestructiblePart>().Gone=true;break;case "maxhp":e.GetPart<DestructiblePart>().MaxHP=41;break;
                case "hardness":e.GetPart<DestructiblePart>().Hardness=1;break;case "indestructible":e.GetPart<DestructiblePart>().Indestructible=true;break;case "wreckage":e.GetPart<DestructiblePart>().WreckageBlueprint="Rubble";break;
                case "light-radius":e.GetPart<LightSourcePart>().Radius=4;break;case "light-color":e.GetPart<LightSourcePart>().LightColor="&R";break;case "light-intensity":e.GetPart<LightSourcePart>().Intensity=1;break;case "light-disabled":e.GetPart<LightSourcePart>().Enabled=false;break;
                case "war-rep":e.GetPart<HearthPatchPart>().WarRep=0;break;case "hearth":Add("HearthPatch");break;case "sundew":Add("PebbleSundewThreshold");break;
                case "water":zone.TileState.WriteCoating(X,Y,"water",3);break;case "charge":zone.TileState.AddCharge(X,Y,1);break;
                case "memory":zone.GetCell(X,Y).IsVisible=false;break;case "unexplored":zone.GetCell(X,Y).Explored=false;break;
                case "duplicate-floor":Add("StoneFloor");break;case "changed-floor":f.GetPart<RenderPart>().ColorString="&R";break;case "authority":manager.WorldMap.GetPOI(4,6).Profile="other";break;
            }
            Assert.False(Ground());Assert.False(Back());
        }
        [TestCase("none")][TestCase("Rubble")][TestCase("SandstoneFloor")]
        public void UnobservedSubstratesDoNotBorrowBacking(string name)
        {if(name!="none")Add(name);Add("FoundingPlume");Assert.False(Ground());Assert.False(Back());Assert.False(View().LocalGround);Assert.False(View().AmbientBackdrop);}
        [TestCase("hidden")][TestCase("custody")][TestCase("equipped")][TestCase("removed")][TestCase("empty-stack")]
        public void UndisclosedPlumeCannotVetoRealFloor(string fault)
        {
            Add("StoneFloor");var e=Add("FoundingPlume");Assert.True(Ground());
            if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;if(fault=="custody")e.GetPart<PhysicsPart>().InInventory=new Entity();if(fault=="equipped")e.GetPart<PhysicsPart>().Equipped=new Entity();if(fault=="removed")zone.RemoveEntity(e);if(fault=="empty-stack")e.AddPart(new StackerPart{StackCount=0});
            Assert.True(Ground());Assert.False(Back());
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedStoneGroupCannotBorrowOtherArt(bool malformed)
        {
            Add("StoneFloor");Add("FoundingPlume");Assert.True(Ground());var bad=malformed?new Texture2D(16,24):null;
            try{using(var missing=Make(bad)){Assert.False(missing.TryResolveGround(zone,X,Y,out _,out _));Assert.False(missing.TryResolveCellBackdrop(zone,X,Y,out _,out _));}}
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [TestCase("Overworld.4.6.1")][TestCase("Overworld.04.6.2")][TestCase("Overworld.12.3.2")]
        public void ExactInhabitedChamberIsRequired(string id)
        {zone=new Zone(id);manager.SetActiveZone(zone);zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;Add("StoneFloor");Add("FoundingPlume");Assert.False(Ground());Assert.False(Back());}
        [Test] public void StoryRenameAndPartialDamageRemainValidButStaleGraphDoesNot()
        {
            Add("StoneFloor");var e=Add("FoundingPlume");Assert.True(Ground());manager.WorldMap.GetPOI(4,6).Name="renamed site";e.GetPart<DestructiblePart>().HP=21;Assert.True(Ground());
            manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);Assert.False(Ground());
        }
        [Test] public void OtherCatalogInstancesNeverGainPlumeException()
        {
            Add("StoneFloor");Add("FoundingPlume");Assert.True(Ground());
            using(var other=new FlatPixelShallowCavesCatalog(null,Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),null,null,null,null,selector,Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath))){Assert.False(other.TryResolveGround(zone,X,Y,out _,out _));typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingLampwellStone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(other,null);Assert.False(other.TryResolveGround(zone,X,Y,out _,out _));}
        }
    }
}
