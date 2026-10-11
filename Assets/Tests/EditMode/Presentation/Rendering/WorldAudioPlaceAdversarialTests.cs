using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Observed source authority, bus isolation and bounded discovery.
    /// Synthetic clips establish routing and voice state, not audible quality.</summary>
    public sealed class WorldAudioPlaceAdversarialTests
    {
        GameObject root;
        WorldAudioLibrary library;
        WorldAudioPlayer player;
        WorldAudioSoundscape scene;
        Zone zone;
        Entity actor;
        readonly List<AudioClip> clips = new List<AudioClip>();
        float oldMusic, oldAmbience, oldSfx;
        bool oldPause;
        int nextId;

        [SetUp] public void Setup()
        {
            oldMusic = WorldAudioSettings.MusicVolume; oldAmbience = WorldAudioSettings.AmbienceVolume;
            oldSfx = WorldAudioSettings.SfxVolume; oldPause = AudioListener.pause;
            WorldAudioSettings.MusicVolume = WorldAudioSettings.AmbienceVolume = WorldAudioSettings.SfxVolume = 1;
            AudioListener.pause = false;
            root = new GameObject("place-audio-adversarial"); library = new WorldAudioLibrary();
            foreach (string id in new[] { "bird", "insect", "frog", "dog", "water-lap", "fire", "leaf-rustle","reed-rustle","wood-settle","stone-shift","water-drop","wood-creak", "stone-tick" })
                library.AddDetail(id, Clip("detail-" + id), .3f);
            foreach (string id in new[] { "field", "night-field", "marsh", "water", "cave", "dry-wind" })
                library.AddAmbience(id, Clip("bed-" + id), .2f);
            library.AddMusic("organic", Clip("music-organic"), .25f);
            library.AddMusic("sodden", library.Music("organic").Clip, .25f); // Synthetic same-score fixture; this suite tests source ownership, not regional identity.
            foreach (WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue))) library.AddSound(cue, Clip("action-" + cue), .4f);
            player = new WorldAudioPlayer(root.transform, library); scene = new WorldAudioSoundscape(player);
            zone = new Zone("Overworld.10.10.0"); actor = WorldAudioAdversarialTests.Actor("listener");
            Assert.True(zone.AddEntity(actor, 10, 10)); nextId = 0;
            foreach (var cell in zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
        }

        [TearDown] public void Cleanup()
        {
            player?.Dispose(); UnityEngine.Object.DestroyImmediate(root);
            foreach (var clip in clips) UnityEngine.Object.DestroyImmediate(clip); clips.Clear();
            WorldAudioSettings.MusicVolume = oldMusic; WorldAudioSettings.AmbienceVolume = oldAmbience;
            WorldAudioSettings.SfxVolume = oldSfx; AudioListener.pause = oldPause;
        }

        AudioClip Clip(string id)
        { var clip = AudioClip.Create(id, 48000, 1, 48000, false); clips.Add(clip); return clip; }
        Entity Add(string blueprint, int x = 11, int y = 10)
        {
            var owner = new Entity { ID = "place-" + nextId++, BlueprintName = blueprint };
            owner.AddPart(new PhysicsPart()); owner.AddPart(new RenderPart());
            Assert.True(zone.AddEntity(owner, x, y)); return owner;
        }
        Entity Animal(string blueprint = "Reedfrog")
        {
            var owner = Add(blueprint); owner.SetTag("Creature"); owner.AddPart(new BrainPart());
            owner.Statistics["Hitpoints"] = new Stat { Owner = owner, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            return owner;
        }
        Entity Water(string id = "water", bool flow = true)
        {
            var owner = Add("WaterPuddle"); owner.AddPart(new LiquidPoolPart { LiquidId = id, Volume = 20 });
            if (flow) owner.SetTag("FlowsEast"); return owner;
        }
        void Observe(BiomeType biome = BiomeType.Sodden, DayBand band = DayBand.Dawn, bool visible = true)
            => scene.Observe(zone, actor, biome, 0, band, visible);
        void StartOnlyAnimalDetail()
        {
            Observe(); Assert.AreEqual(1, scene.SourceCount); Assert.AreEqual(0, scene.HabitatDetailCount);
            scene.Tick(30, true, false); Assert.AreEqual(1, player.ActiveDetailSources);
            Assert.AreEqual("frog", player.LastDetailId);
        }
        string[] Playing() => root.GetComponentsInChildren<AudioSource>().Where(s => s.clip != null).Select(s => s.clip.name).ToArray();
        void SyncBed()
        { player.SetContext(WorldAudioContext.Select(BiomeType.Sodden, 0, false).WithAmbience(scene.AmbienceId)); player.Update(.01f); }

        [TestCase("hide", false)] [TestCase("death", false)] [TestCase("remove", false)] [TestCase("range", false)]
        [TestCase("hide", true)] [TestCase("death", true)] [TestCase("remove", true)] [TestCase("range", true)]
        public void SourceLossStopsAnAlreadyPlayingDetailOnBothIdleAndTurnRefresh(string change, bool observeAgain)
        {
            var owner = Animal(); StartOnlyAnimalDetail(); int accepted = player.AcceptedDetailCount;
            if (change == "hide") zone.GetEntityCell(owner).IsVisible = false;
            if (change == "death") owner.GetStat("Hitpoints").BaseValue = 0;
            if (change == "remove") Assert.True(zone.RemoveEntity(owner));
            if (change == "range") Assert.True(zone.MoveEntity(owner, 18, 10));
            if (observeAgain) Observe();
            scene.Tick(.01f, true, false);
            Assert.AreEqual(0, scene.SourceCount); Assert.AreEqual(0, player.ActiveDetailSources,
                "A context refresh must not discard the witness before it can retire its old voice.");
            Assert.AreEqual(accepted, player.AcceptedDetailCount);
        }

        [TestCase("water", true)] [TestCase("water", false)]
        [TestCase("fire", true)] [TestCase("fire", false)]
        [TestCase("wood", true)] [TestCase("wood", false)]
        public void DestroyedStructureCannotRetainWaterFireOrWoodAudio(string kind, bool goneFlag)
        {
            Entity owner;
            if (kind == "water") owner = Water();
            else if (kind == "wood") owner = Add("Tree");
            else
            {
                owner = Add("Campfire"); owner.AddPart(new CampfirePart());
                owner.AddPart(new FuelPart { FuelMass = 20 });
                owner.AddPart(new ThermalPart { Temperature = 600, FlameTemperature = 500 });
            }
            var structural = new DestructiblePart { HP = 20, MaxHP = 20 }; owner.AddPart(structural);
            Observe(); Assert.AreEqual(1, scene.SourceCount);
            if (goneFlag) structural.Gone = true; else structural.HP = 0;
            scene.Tick(.01f, true, false); Assert.AreEqual(0, scene.SourceCount);
            Observe(); Assert.AreEqual(0, scene.SourceCount, "A fresh scan must not re-admit the destroyed owner.");
        }

        [TestCase("render")] [TestCase("physics")] [TestCase("pool")] [TestCase("spatial-zone")] [TestCase("cell-zone")]
        public void RawOwnerMismatchRefusesThenExactRestorationRecovers(string changed)
        {
            var owner = Water(); var cell = zone.GetEntityCell(owner); Observe(); Assert.AreEqual(1, scene.SourceCount);
            var spatial = typeof(Entity).GetField("SpatialZone", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(spatial);
            if (changed == "render") owner.GetPart<RenderPart>().ParentEntity = actor;
            if (changed == "physics") owner.GetPart<PhysicsPart>().ParentEntity = actor;
            if (changed == "pool") owner.GetPart<LiquidPoolPart>().ParentEntity = actor;
            if (changed == "spatial-zone") spatial.SetValue(owner, new Zone("foreign"));
            if (changed == "cell-zone") cell.ParentZone = new Zone("foreign");
            scene.Tick(.01f, true, false); Assert.AreEqual(0, scene.SourceCount);
            owner.GetPart<RenderPart>().ParentEntity = owner; owner.GetPart<PhysicsPart>().ParentEntity = owner;
            owner.GetPart<LiquidPoolPart>().ParentEntity = owner; spatial.SetValue(owner, zone); cell.ParentZone = zone;
            Observe(); Assert.AreEqual(1, scene.SourceCount); Assert.AreEqual("water", scene.AmbienceId);
        }

        [Test] public void IdleNeverDiscoversAnAnimalWhoseHiddenCellJustBecameVisible()
        {
            var owner = Animal(); zone.GetEntityCell(owner).IsVisible = false; Observe(); int scans = scene.ScanCount;
            zone.GetEntityCell(owner).IsVisible = true;
            for (int i = 0; i < 20; i++) { scene.Tick(30, true, false); player.Update(30); }
            Assert.AreEqual(scans, scene.ScanCount); Assert.AreEqual(0, scene.SourceCount); Assert.AreEqual(0, player.AcceptedDetailCount);
            Observe(); scene.Tick(30, true, false); Assert.AreEqual(1, scene.SourceCount); Assert.AreEqual(1, player.AcceptedDetailCount);
        }

        [TestCase("missing-hitpoints")] [TestCase("missing-brain")]
        public void AnAnimalNameAndCreatureTagAreNotProofOfALivingAnimal(string missing)
        {
            var owner = Animal(); Observe(); Assert.AreEqual(1, scene.SourceCount);
            if (missing == "missing-hitpoints") owner.Statistics.Remove("Hitpoints");
            else owner.RemovePart(owner.GetPart<BrainPart>());
            scene.Tick(.01f, true, false); Assert.AreEqual(0, scene.SourceCount);
            Observe(); Assert.AreEqual(0, scene.SourceCount);
        }

        [TestCase("water")] [TestCase("brine")] [TestCase("bog-mire")]
        public void StandingWaterIsAudibleButNeverClaimsAFlowingRiver(string id)
        {
            var owner = Water(id, false); Observe(); Assert.AreEqual(1, scene.SourceCount); Assert.AreEqual("marsh", scene.AmbienceId);
            owner.SetTag("FlowsSouth"); scene.Tick(.01f, true, false);
            Assert.AreEqual(id == "water" ? "water" : "marsh", scene.AmbienceId);
            owner.Tags.Remove("FlowsSouth"); scene.Tick(.01f, true, false); Assert.AreEqual("marsh", scene.AmbienceId);
        }

        [Test] public void LosingFlowingWaterDoesNotKeepItsSourceDependentBedInACrossfadeTail()
        {
            var owner = Water(); Observe(); SyncBed(); player.Update(3);
            Assert.Contains("bed-water", Playing());
            owner.GetPart<LiquidPoolPart>().Volume = 0; scene.Tick(.01f, true, false); SyncBed();
            Assert.AreEqual("marsh", scene.AmbienceId);
            Assert.False(Playing().Contains("bed-water"), "A place fade may not retain a source that has lost authority.");
            Assert.Contains("music-organic", Playing());
        }

        [Test] public void RawInteriorChangeSuppressesExteriorTextureWithoutDiscoveringRoomSources()
        {
            Observe(BiomeType.Spread); Assert.AreEqual("field", scene.AmbienceId); int scans = scene.ScanCount;
            zone.GetEntityCell(actor).IsInterior = true; scene.Tick(.01f, true, false);
            Assert.Null(scene.AmbienceId); Assert.AreEqual(0, scene.HabitatDetailCount); Assert.AreEqual(scans, scene.ScanCount);
        }

        [Test] public void ChangingDayBandRetiresAPreviouslyAudibleDaytimeAnimal()
        {
            Animal("Magpie"); Observe(); Assert.AreEqual(0, scene.HabitatDetailCount);
            scene.Tick(30, true, false); Assert.AreEqual("bird", player.LastDetailId); Assert.AreEqual(1, player.ActiveDetailSources);
            Observe(BiomeType.Sodden, DayBand.Dark); scene.Tick(.01f, true, false);
            Assert.AreEqual(0, scene.SourceCount); Assert.AreEqual(0, player.ActiveDetailSources);
            Assert.AreEqual("night-field", scene.AmbienceId);
        }

        [Test] public void DenseVisiblePileCannotExceedBoundedOwnerBudgetOrRescanDuringIdle()
        {
            for (int i = 0; i < 600; i++) Add("unclassified rubble", 4, 4);
            Animal(); Observe(); Assert.AreEqual(512, scene.LastVisitedOwners); Assert.AreEqual(0, scene.SourceCount);
            int scans = scene.ScanCount;
            for (int i = 0; i < 30; i++) scene.Tick(.1f, true, false);
            Assert.AreEqual(scans, scene.ScanCount); Assert.AreEqual(512, scene.LastVisitedOwners); Assert.AreEqual(16, player.AllocatedSources);
        }

        [Test] public void MutingOrClearingDetailsPreservesIndependentActionsAndMusic()
        {
            player.SetContext(WorldAudioContext.Select(BiomeType.Spread, 0, false)); player.Update(3);
            Assert.True(player.TryPlay(WorldAudioCue.Pickup)); Assert.True(player.TryPlayDetail("frog")); Assert.True(player.TryPlayDetail("fire"));
            WorldAudioSettings.AmbienceVolume = 0; player.Update(.01f);
            Assert.AreEqual(0, player.ActiveDetailSources); Assert.AreEqual(1, player.ActiveSoundSources);
            Assert.Contains("action-Pickup", Playing()); Assert.Contains("music-organic", Playing());
            WorldAudioSettings.AmbienceVolume = 1; WorldAudioSettings.SfxVolume = 0;
            Assert.True(player.TryPlayDetail("dog")); player.Update(.01f);
            Assert.AreEqual(1, player.ActiveDetailSources); Assert.False(Playing().Contains("action-Pickup"));
            player.ClearDetails(); Assert.AreEqual(0, player.ActiveDetailSources); Assert.Contains("music-organic", Playing());
            Assert.AreEqual(16, player.AllocatedSources);
        }

        [Test] public void HiddenMenuRetiresDetailsAndResumptionRequiresFreshObservation()
        {
            Animal(); StartOnlyAnimalDetail(); int scans = scene.ScanCount;
            scene.Tick(.01f, false, false); Assert.AreEqual(0, player.ActiveDetailSources);
            for (int i = 0; i < 10; i++) scene.Tick(30, true, false);
            Assert.AreEqual(scans, scene.ScanCount); Assert.AreEqual(0, scene.SourceCount);
            Observe(); scene.Tick(30, true, false); Assert.AreEqual(1, scene.SourceCount); Assert.AreEqual(1, player.ActiveDetailSources);
        }
    }
}
