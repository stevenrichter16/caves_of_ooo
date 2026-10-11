using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Material-specific authority and active-phrase transitions.
    /// Synthetic voices establish routing, not subjective acoustic quality.</summary>
    public sealed class WorldAudioMaterialAdversarialTests
    {
        GameObject root;
        WorldAudioPlayer player;
        WorldAudioSoundscape scene;
        Zone zone;
        Entity actor;
        readonly List<AudioClip> clips = new List<AudioClip>();
        float music, ambience, sfx;
        bool paused;
        int nextId;

        [SetUp] public void Setup()
        {
            music = WorldAudioSettings.MusicVolume; ambience = WorldAudioSettings.AmbienceVolume;
            sfx = WorldAudioSettings.SfxVolume; paused = AudioListener.pause;
            WorldAudioSettings.MusicVolume = WorldAudioSettings.AmbienceVolume = WorldAudioSettings.SfxVolume = 1;
            AudioListener.pause = false;
            root = new GameObject("material-audio-adversarial");
            var library = new WorldAudioLibrary();
            foreach (string id in new[] { "leaf-rustle", "reed-rustle", "wood-settle", "stone-shift", "water-drop", "water-lap" })
            {
                var clip = AudioClip.Create(id, 48000, 1, 48000, false);
                clips.Add(clip); library.AddDetail(id, clip, .3f);
            }
            player = new WorldAudioPlayer(root.transform, library); scene = new WorldAudioSoundscape(player);
            zone = new Zone("Overworld.10.10.0"); actor = WorldAudioAdversarialTests.Actor("material-listener");
            Assert.True(zone.AddEntity(actor, 10, 10)); nextId = 0;
            foreach (var cell in zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
        }

        [TearDown] public void Cleanup()
        {
            player?.Dispose(); Object.DestroyImmediate(root);
            foreach (var clip in clips) Object.DestroyImmediate(clip); clips.Clear();
            WorldAudioSettings.MusicVolume = music; WorldAudioSettings.AmbienceVolume = ambience;
            WorldAudioSettings.SfxVolume = sfx; AudioListener.pause = paused;
        }

        Entity Add(string blueprint)
        {
            var owner = new Entity { ID = "material-source-" + nextId++, BlueprintName = blueprint };
            owner.AddPart(new PhysicsPart()); owner.AddPart(new RenderPart());
            Assert.True(zone.AddEntity(owner, 11, 10)); return owner;
        }
        Entity Wall(string material)
        {
            var owner = Add("test fixed structure"); owner.SetTag("Wall");
            owner.AddPart(new MaterialPart { MaterialID = material }); return owner;
        }
        void Shelter(bool inside)
        { zone.GetCell(10, 10).IsInterior = zone.GetCell(11, 10).IsInterior = inside; }
        void Observe() => scene.Observe(zone, actor, BiomeType.Sodden, 0, DayBand.Dawn, true);
        void Start(string expected)
        {
            int before = player.AcceptedDetailCount;
            Observe(); Assert.AreEqual(1, scene.SourceCount);
            player.Update(30); scene.Tick(30, true, false);
            Assert.AreEqual(before + 1, player.AcceptedDetailCount);
            Assert.AreEqual(expected, player.LastDetailId); Assert.AreEqual(1, player.ActiveDetailSources);
        }
        void RefusedAfterMutation()
        {
            int accepted = player.AcceptedDetailCount, scans = scene.ScanCount;
            scene.Tick(.01f, true, false);
            Assert.AreEqual(scans, scene.ScanCount); Assert.AreEqual(0, scene.SourceCount);
            Assert.AreEqual(0, player.ActiveDetailSources); Assert.AreEqual(accepted, player.AcceptedDetailCount);
            Observe(); Assert.AreEqual(0, scene.SourceCount);
        }

        [TestCase("portable")] [TestCase("foreign-material")] [TestCase("missing-material")]
        [TestCase("steel")] [TestCase("no-structure")]
        public void SettlingTimberNeedsItsCurrentOwnedFixedMaterial(string mutation)
        {
            Shelter(true); var owner = Wall("Wood"); var material = owner.GetPart<MaterialPart>(); Start("wood-settle");
            if (mutation == "portable") owner.GetPart<PhysicsPart>().Takeable = true;
            if (mutation == "foreign-material") material.ParentEntity = actor;
            if (mutation == "missing-material") owner.RemovePart(material);
            if (mutation == "steel") material.MaterialID = "Steel";
            if (mutation == "no-structure") owner.Tags.Remove("Wall");
            RefusedAfterMutation();
            owner.GetPart<PhysicsPart>().Takeable = false; owner.SetTag("Wall"); material.MaterialID = "Wood";
            if (owner.GetPart<MaterialPart>() == null) owner.AddPart(material); else material.ParentEntity = owner;
            Start("wood-settle");
        }

        [TestCase("Tree", "leaf-rustle")] [TestCase("Bush", "leaf-rustle")] [TestCase("Reeds", "reed-rustle")]
        public void PortableFoliageIsNotAStandingLandscapeSource(string blueprint, string cue)
        {
            var owner = Add(blueprint); Start(cue);
            owner.GetPart<PhysicsPart>().Takeable = true; RefusedAfterMutation();
            owner.GetPart<PhysicsPart>().Takeable = false; Start(cue);
        }

        [TestCase("Reeds", true)] [TestCase("Reeds", false)]
        [TestCase("stone", true)] [TestCase("stone", false)]
        public void NewTextureBranchesRetireDestroyedOwnersBeforeRediscovery(string blueprint, bool gone)
        {
            bool stone = blueprint == "stone"; Shelter(stone);
            var owner = stone ? Wall("Masonry") : Add(blueprint);
            var structural = new DestructiblePart { HP = 20, MaxHP = 20 }; owner.AddPart(structural);
            Start(stone ? "stone-shift" : "reed-rustle");
            if (gone) structural.Gone = true; else structural.HP = 0;
            RefusedAfterMutation(); structural.Gone = false; structural.HP = 20;
            Start(stone ? "stone-shift" : "reed-rustle");
        }

        [Test] public void SameVisibleTreeStopsRustlingWhenTheListenerAndSourceBecomeSheltered()
        {
            Add("Tree"); Start("leaf-rustle"); Shelter(true); RefusedAfterMutation();
            int scans = scene.ScanCount; Shelter(false); scene.Tick(.01f, true, false);
            Assert.AreEqual(0, scene.SourceCount); Assert.AreEqual(scans, scene.ScanCount,
                "Returning outside does not discover fresh foliage until the context boundary.");
            Start("leaf-rustle");
        }

        [Test] public void ChangingVisibleVegetationIdentityRetiresTheOldPhraseBeforeSelectingTheNewOne()
        {
            var owner = Add("Tree"); Start("leaf-rustle"); owner.BlueprintName = "Reeds";
            scene.Tick(.01f, true, false); Assert.AreEqual(0, scene.SourceCount); Assert.AreEqual(0, player.ActiveDetailSources);
            Start("reed-rustle"); Assert.AreEqual(16, player.AllocatedSources);
        }

        [TestCase("oil")] [TestCase("acid")] [TestCase("drained")]
        public void EnclosedWaterDropsStopWhenTheActualLiquidCeasesToQualify(string mutation)
        {
            Shelter(true); var owner = Add("test pool"); var pool = new LiquidPoolPart { LiquidId = "water", Volume = 3 };
            owner.AddPart(pool); Start("water-drop");
            if (mutation == "drained") pool.Volume = 0; else pool.LiquidId = mutation;
            RefusedAfterMutation(); pool.Volume = 3; pool.LiquidId = "water"; Start("water-drop");
        }

        [Test] public void DryStoneDoesNotBecomeADripWithoutAnActualPositiveWaterPart()
        {
            Shelter(true); var owner = Wall("Stone"); Start("stone-shift");
            var pool = new LiquidPoolPart { LiquidId = "water", Volume = 0 }; owner.AddPart(pool);
            scene.Tick(.01f, true, false); Assert.AreEqual(1, scene.SourceCount); Assert.AreEqual("stone-shift", player.LastDetailId);
            pool.Volume = 1; scene.Tick(.01f, true, false); Assert.AreEqual(0, player.ActiveDetailSources);
            Start("water-drop"); owner.RemovePart(pool); scene.Tick(.01f, true, false);
            Assert.AreEqual(0, player.ActiveDetailSources); Start("stone-shift");
        }

        [Test] public void WoodenDoorRequiresItsOwnDoorPartNotAStaleForeignReference()
        {
            Shelter(true); var owner = Add("test door"); var door = new DoorPart(); owner.AddPart(door);
            owner.AddPart(new MaterialPart { MaterialID = "Wood" }); Start("wood-settle");
            door.ParentEntity = actor; RefusedAfterMutation(); door.ParentEntity = owner; Start("wood-settle");
        }

        [Test] public void HiddenNewFoliageDoesNotBorrowAnotherVisibleMaterialsAdmission()
        {
            var owner = Add("Tree"); zone.GetEntityCell(owner).IsVisible = false; Observe(); Assert.AreEqual(0, scene.SourceCount);
            zone.GetEntityCell(owner).IsVisible = true; scene.Tick(30, true, false);
            Assert.AreEqual(0, player.AcceptedDetailCount); Start("leaf-rustle");
            zone.GetEntityCell(owner).IsVisible = false; RefusedAfterMutation();
        }

        [Test] public void ReachableCreditsKeepEveryExpansionAndItsSourceAuthors()
        {
            using (var isolation = new HotbarSaveFixture(false, false))
            {
                MessageLog.Clear(); var menu = new PauseMenuController(); menu.Open();
                menu.ClickSelect(PauseMenuController.AudioIndex, null, null);
                menu.ClickSelect(PauseMenuController.AudioCreditsIndex, null, null);
                string text = MessageLog.ConsumeAnnouncement();
                foreach (string id in new[] { "CREDITS", "PLACES-CREDITS", "ATMOSPHERIC-CREDITS" })
                {
                    var credits = Resources.Load<TextAsset>("Audio/World/" + id); Assert.NotNull(credits, id);
                    StringAssert.Contains(credits.text, text, id);
                }
                StringAssert.Contains("Vehicle", text); StringAssert.Contains("Vishwa", text);
            }
        }
    }
}
