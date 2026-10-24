using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class EventTextRenderingTests
    {
        GameObject root;
        Tilemap tiles;
        AsciiFxRenderer ascii;
        WorldFxCoordinator world;
        Zone zone;
        Entity actor;
        SpellFxMode previousMode;
        float previousSpeed,previousSound;

        [SetUp] public void Setup()
        {
            previousMode=SpellFxSettings.Mode;previousSpeed=SpellFxSettings.AnimationSpeed;
            previousSound=SpellFxSettings.SoundVolume;
            AsciiFxBus.Clear();SpellFxBus.Clear();
            SpellFxSettings.Mode=SpellFxMode.Full;SpellFxSettings.AnimationSpeed=1;SpellFxSettings.SoundVolume=0;
            root=new GameObject("Event text fixture",typeof(Grid));
            var map=new GameObject("Event text tiles",typeof(Tilemap),typeof(TilemapRenderer));
            map.transform.SetParent(root.transform);tiles=map.GetComponent<Tilemap>();
            ascii=new AsciiFxRenderer(tiles);world=new WorldFxCoordinator(ascii,root.transform);
            zone=VisibleZone("EventText");world.SetZone(zone);actor=Actor(zone,20,10);
        }
        [TearDown] public void Cleanup()
        {
            world?.Dispose();UnityEngine.Object.DestroyImmediate(root);
            AsciiFxBus.Clear();SpellFxBus.Clear();SpellFxSettings.Mode=previousMode;
            SpellFxSettings.AnimationSpeed=previousSpeed;SpellFxSettings.SoundVolume=previousSound;
        }
        static Zone VisibleZone(string id)
        {
            var result=new Zone(id);
            foreach(var cell in result.Cells){cell.IsVisible=true;cell.Explored=true;}
            return result;
        }
        static Entity Actor(Zone at,int x,int y)
        {
            var result=new Entity{ID=Guid.NewGuid().ToString("N")};result.SetTag("Creature");
            result.AddPart(new PhysicsPart());result.AddPart(new RenderPart{Visible=true,DisplayName="reader"});
            result.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40,Max=40,Owner=result};
            Assert.True(at.AddEntity(result,x,y));return result;
        }
        Vector3Int Position(int x,int y)=>new Vector3Int(x,Zone.Height-1-y,0);
        void Word(string value,int x,int y)
        {
            for(int i=0;i<value.Length;i++)
                Assert.AreSame(CP437TilesetGenerator.GetUiTile(value[i]),tiles.GetTile(Position(x+i,y)),value+" letter "+i);
        }
        void Emit(string label="Parried")=>AsciiFxBus.EmitEventText(zone,actor,label,"&C");

        [TestCase(SpellFxMode.Full)] [TestCase(SpellFxMode.Reduced)] [TestCase(SpellFxMode.Off)]
        public void SemanticWordsRemainReadableWithoutBlockingInEveryFxMode(SpellFxMode mode)
        {
            SpellFxSettings.Mode=mode;world.Update(0);
            int version=zone.EntityVersion,parts=actor.Parts.Count;
            Emit();Assert.False(AsciiFxBus.HasPendingBlocking);world.Update(0);
            Assert.AreEqual(1,ascii.ActiveEventTextCount);Word("Parried",17,9);
            Assert.False(world.HasBlockingFx);Assert.AreEqual(0,world.ActiveSequenceCount);
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(parts,actor.Parts.Count);
            Assert.AreEqual(40,actor.GetStatValue("Hitpoints"));
            world.Update(.6f);Word("Parried",17,8);
            Assert.Null(tiles.GetTile(Position(17,9)));
            world.Update(.51f);Assert.Zero(ascii.ActiveEventTextCount);
            Assert.Null(tiles.GetTile(Position(17,8)));
        }
        [Test] public void DuplicateActiveMessageDoesNotRenewItsLife()
        {
            Emit();world.Update(0);world.Update(.6f);Emit();world.Update(0);
            Assert.AreEqual(1,ascii.ActiveEventTextCount);
            world.Update(.51f);Assert.Zero(ascii.ActiveEventTextCount);
        }
        [TestCase(.25f)] [TestCase(4f)]
        public void ReadingTimeDoesNotScaleWithDecorativeAnimationSpeed(float speed)
        {
            SpellFxSettings.AnimationSpeed=speed;Emit();world.Update(0);
            Assert.AreEqual(1,ascii.ActiveEventTextCount);
            world.Update(.6f);Assert.AreEqual(1,ascii.ActiveEventTextCount);Word("Parried",17,8);
            world.Update(.51f);Assert.Zero(ascii.ActiveEventTextCount);
        }
        [Test] public void ActorLanesStayDistinctAndDoNotShiftWhenAnOlderLabelExpires()
        {
            Emit("A");world.Update(0);world.Update(.3f);
            Emit("B");Emit("C");Emit("D");world.Update(0);
            Assert.AreEqual(3,ascii.ActiveEventTextCount);Word("A",20,9);Word("B",20,7);Word("C",20,5);
            world.Update(.81f);Assert.AreEqual(2,ascii.ActiveEventTextCount);
            Word("B",20,6);Word("C",20,4);
            Emit("D");world.Update(0);Word("D",20,9);Word("B",20,6);Word("C",20,4);
        }
        [Test] public void StaggeredLabelsCannotRiseIntoEachOther()
        {
            Emit("A");world.Update(0);world.Update(.3f);Emit("B");world.Update(0);world.Update(.3f);
            Assert.AreEqual(2,ascii.ActiveEventTextCount);
            Assert.AreEqual(2,tiles.GetUsedTilesCount(),"Both different labels must remain drawn when only the older one rises.");
            Word("A",20,8);Word("B",20,7);
        }
        [Test] public void ActiveReadoutsAreBoundedAcrossManyActors()
        {
            // Spread complete words out so this proves the shared capacity limit, not collision rejection.
            for(int i=0;i<40;i++)AsciiFxBus.EmitEventText(zone,Actor(zone,3+8*(i%10),6+6*(i/10)),"Guarded","&C");
            world.Update(0);Assert.AreEqual(32,ascii.ActiveEventTextCount);
            world.Update(1.2f);Assert.Zero(ascii.ActiveEventTextCount);
            Emit();world.Update(0);Assert.AreEqual(1,ascii.ActiveEventTextCount);
        }
        [TestCase(0,0,0,5)] [TestCase(79,0,73,5)]
        [TestCase(0,24,0,23)] [TestCase(79,24,73,23)]
        public void CompleteWordsClampAtEachMapCorner(int x,int y,int left,int row)
        {
            Assert.True(zone.MoveEntity(actor,x,y));Emit();world.Update(0);Word("Parried",left,row);
            world.Update(.6f);Word("Parried",left,row-1);
        }
        [TestCase("origin-hidden")] [TestCase("origin-unexplored")] [TestCase("actor-hidden")]
        [TestCase("moved-hidden")] [TestCase("removed")] [TestCase("other-zone")]
        public void OriginAndActorVisibilityAreRecheckedEvenWithGlobalReveal(string change)
        {
            Emit();world.Update(0);Assert.AreEqual(1,ascii.ActiveEventTextCount);
            ascii.ClipToFieldOfView=false;
            switch(change)
            {
                case "origin-hidden":zone.GetCell(20,10).IsVisible=false;break;
                case "origin-unexplored":zone.GetCell(20,10).Explored=false;break;
                case "actor-hidden":actor.GetPart<RenderPart>().Visible=false;break;
                case "moved-hidden":Assert.True(zone.MoveEntity(actor,21,10));zone.GetCell(21,10).IsVisible=false;break;
                case "removed":zone.RemoveEntity(actor);break;
                case "other-zone":zone.RemoveEntity(actor);Assert.True(VisibleZone("Elsewhere").AddEntity(actor,20,10));break;
            }
            world.Update(0);Assert.Zero(ascii.ActiveEventTextCount);Assert.Null(tiles.GetTile(Position(17,9)));
            zone.GetCell(20,10).IsVisible=true;zone.GetCell(20,10).Explored=true;
            world.Update(0);Assert.Zero(ascii.ActiveEventTextCount,"Hidden labels must not reappear later.");
        }
        [Test] public void CapturedOriginIsCheckedAgainBeforeQueuedAdmission()
        {
            Emit();Assert.AreEqual(1,AsciiFxBus.PendingCount);zone.GetCell(20,10).IsVisible=false;
            world.Update(0);Assert.Zero(ascii.ActiveEventTextCount);Assert.Null(tiles.GetTile(Position(17,9)));
        }
        [TestCase(false)] [TestCase(true)]
        public void RemovingRenderPartHidesQueuedAndActiveWords(bool admitted)
        {
            Emit();
            if(admitted){world.Update(0);Assert.AreEqual(1,ascii.ActiveEventTextCount);}
            Assert.True(actor.RemovePart(actor.GetPart<RenderPart>()));
            world.Update(0);Assert.Zero(ascii.ActiveEventTextCount);Assert.Null(tiles.GetTile(Position(17,9)));
        }
        [Test] public void MovementKeepsSharedActorLanesDistinctAndStillHonorsEachCapturedOrigin()
        {
            Emit("A");world.Update(0);
            Assert.True(zone.MoveEntity(actor,20,11));Emit("B");world.Update(0);
            Assert.AreEqual(2,ascii.ActiveEventTextCount);Word("A",20,10);Word("B",20,8);
            zone.GetCell(20,10).IsVisible=false;world.Update(0);
            Assert.AreEqual(1,ascii.ActiveEventTextCount);Word("B",20,8);
            Assert.Null(tiles.GetTile(Position(20,10)),"A cannot follow its owner out of its hidden origin.");
        }
        [TestCase("clear")] [TestCase("zone")] [TestCase("hidden")]
        public void LifecycleChangesClearActiveAndQueuedWords(string change)
        {
            Emit();world.Update(0);Emit("Guarded");
            switch(change)
            {
                case "clear":AsciiFxBus.Clear();world.Update(0);break;
                case "zone":world.SetZone(VisibleZone("NewZone"));break;
                default:world.Update(0,true,false);break;
            }
            Assert.Zero(ascii.ActiveEventTextCount);Assert.Zero(AsciiFxBus.PendingCount);
            Assert.False(world.HasBlockingFx);Assert.Null(tiles.GetTile(Position(17,9)));
        }
        [Test] public void WordsDrawAfterDecorativeParticlesAndBursts()
        {
            Emit();AsciiFxBus.EmitParticle(zone,17,9,'*',"&R",1);
            AsciiFxBus.EmitBurst(zone,17,9,AsciiFxTheme.Fire,false);world.Update(0);
            Word("Parried",17,9);Assert.AreEqual(QudColorParser.Parse("&C"),tiles.GetColor(Position(17,9)));
            Assert.AreEqual(Matrix4x4.identity,tiles.GetTransformMatrix(Position(17,9)));
        }
        [Test] public void StatusLettersUsePureFontInsteadOfCreatureAndTerrainGlyphs()
        {
            Emit("Resisted");AsciiFxBus.EmitParticle(zone,20,12,'s',"&R",1);
            world.Update(0);Word("Resisted",17,9);
            Assert.AreNotSame(CP437TilesetGenerator.GetTile('s'),tiles.GetTile(Position(19,9)));
            Assert.AreSame(CP437TilesetGenerator.GetTile('s'),tiles.GetTile(Position(20,12)),"Decorative particles retain their gameplay glyphs.");
        }
        [Test] public void UnsupportedUnicodeUsesCachedFontFallbackWithoutRebuildingEitherAtlas()
        {
            var gameQuestion=CP437TilesetGenerator.GetTile('?');
            var fontQuestion=CP437TilesetGenerator.GetUiTile('?');
            Emit("\u03B1");world.Update(0);
            Assert.AreSame(gameQuestion,CP437TilesetGenerator.GetTile('?'),"Unknown text must not rebuild the gameplay atlas.");
            Assert.AreSame(fontQuestion,tiles.GetTile(Position(20,9)));
            world.Update(.1f);
            Assert.AreSame(fontQuestion,CP437TilesetGenerator.GetUiTile('?'));
            Assert.AreSame(gameQuestion,CP437TilesetGenerator.GetTile('?'));
        }
        [Test] public void NativeSceneKeepsLettersInsteadOfConvertingThemIntoSparks()
        {
            using(var map=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                world.SetZone(map.Zone);world.SetNativeSurface(((SpawnRing3DPresenter)map.Presenter).ActiveSurface);
                var visibleActor=Actor(map.Zone,20,10);
                AsciiFxBus.EmitParticle(map.Zone,17,9,'*',"&R",1);world.Update(0);
                Assert.True(world.TryGetNativeParticle(17,9,out _),"Control: decoration really uses native geometry.");
                AsciiFxBus.EmitEventText(map.Zone,visibleActor,"Parried","&C");world.Update(0);
                Word("Parried",17,9);Assert.False(world.TryGetNativeParticle(17,9,out _));
                world.CancelAll();world.SetNativeSurface(null);
            }
        }
    }
}
