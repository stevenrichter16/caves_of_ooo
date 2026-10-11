using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public abstract class WorldAudioWorkFixture : FiftyWorldFixture
    {
        protected readonly List<WorldAudioWorkReceipt> Receipts = new List<WorldAudioWorkReceipt>();
        protected WorldAudioPlayer Audio;
        protected WorldAudioEventRouter Router;
        protected GameObject AudioRoot;
        Action<WorldAudioWorkReceipt> oldWork;
        readonly List<AudioClip> clips = new List<AudioClip>();
        float oldSfx; bool oldPause;
        [SetUp] public void SetupWorkAudio()
        {
            WithoutClock(); RepairRecipeRegistry.ResetForTests(); RepairRecipeRegistry.LoadDefaults();
            foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
            oldWork = WorldAudioWork.Callback; WorldAudioWork.Callback = Receipts.Add; Receipts.Clear();
            oldSfx = WorldAudioSettings.SfxVolume; oldPause = AudioListener.pause;
            WorldAudioSettings.SfxVolume = 1; AudioListener.pause = false;
            AudioRoot = new GameObject("committed work audio test"); var library = new WorldAudioLibrary();
            foreach (WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))
                for (int take = 0; take < 2; take++)
                {
                    var clip = AudioClip.Create(cue + " take " + take, 24000, 1, 48000, false);
                    clips.Add(clip); library.AddSound(cue, clip, .3f);
                }
            Audio = new WorldAudioPlayer(AudioRoot.transform, library);
            Router = new WorldAudioEventRouter(Audio); Router.SetWorld(Zone, Actor, true);
        }
        [TearDown] public void CleanupWorkAudio()
        {
            Router?.Dispose(); Audio?.Dispose(); UnityEngine.Object.DestroyImmediate(AudioRoot);
            foreach (var clip in clips) UnityEngine.Object.DestroyImmediate(clip); clips.Clear();
            WorldAudioWork.Callback = oldWork; WorldAudioSettings.SfxVolume = oldSfx; AudioListener.pause = oldPause;
            RepairRecipeRegistry.ResetForTests();
        }
        protected string[] Assigned() => AudioRoot.GetComponentsInChildren<AudioSource>().Where(s => s.clip != null).Select(s => s.clip.name).ToArray();
        protected Entity RepairTarget(string blueprint = "RepairLinedWell") => Place(blueprint);
        protected void PayRepair(Entity target)
        {
            var recipe = RepairRecipeRegistry.Get(target.GetPart<RepairablePart>().RecipeId);
            Assert.NotNull(recipe);
            for (int i = 0; i < recipe.Quantity; i++) Carry(recipe.MaterialBlueprint);
        }
        protected bool Repair(Entity target) => Act(target, RepairablePart.RepairCommand);
        protected ForgeWeaponCommand ForgeCommand(int count = 1)
        {
            Entity blade = Carry("SteelBladeComponent"), haft = Carry("OakHaftComponent"), binding = Carry("LeatherBindingComponent");
            if (count > 1)
                foreach (var item in new[] { blade, haft, binding })
                {
                    var stack = item.GetPart<StackerPart>();
                    if (stack == null) item.AddPart(new StackerPart { StackCount = count }); else stack.StackCount = count;
                }
            return new ForgeWeaponCommand(blade, haft, binding, Factory, count);
        }
        protected static void Stage(Entity actor, Entity target, Zone zone, string command, InventoryTransaction tx)
        {
            var e = GameEvent.New("InventoryAction");
            try
            {
                e.SetParameter("Actor", actor); e.SetParameter("Zone", zone); e.SetParameter("Command", command);
                e.SetParameter("InventoryTransaction", tx); target.FireEvent(e); Assert.True(e.Handled);
            }
            finally { e.Release(); }
        }
    }

    public sealed class WorldAudioWorkTests : WorldAudioWorkFixture
    {
        [Test] public void ActualResourceCatalogBindsTwoDifferentShortMonoTakesForEveryWorkCue()
        {
            var resources=WorldAudioLibrary.LoadResources();
            foreach(var cue in new[] { WorldAudioCue.WorkForge,WorldAudioCue.WorkRepairWood,WorldAudioCue.WorkRepairStone,
                WorldAudioCue.WorkRepairMetal,WorldAudioCue.HarvestPlant })
            {
                var first=resources.Sound(cue,0);var second=resources.Sound(cue,1);
                Assert.NotNull(first,cue.ToString());Assert.NotNull(second,cue.ToString());Assert.AreNotSame(first.Clip,second.Clip);
                foreach(var take in new[] {first,second})
                {
                    Assert.NotNull(take.Clip);Assert.That(take.Clip.length,Is.GreaterThan(0).And.LessThan(2));
                    Assert.AreEqual(1,take.Clip.channels);Assert.AreEqual(AudioClipLoadType.DecompressOnLoad,take.Clip.loadType);
                }
                Assert.AreSame(first.Clip,resources.Sound(cue,2).Clip,"exact paired takes repeat after both recordings");
            }
        }
        [TestCase("RepairLinedWell", WorldAudioCue.WorkRepairStone)]
        [TestCase("RepairWoodenGate", WorldAudioCue.WorkRepairWood)]
        [TestCase("RepairRopeWell", WorldAudioCue.Use)]
        public void ActualAuthoredRepairPaysAndPublishesExactlyOneMaterialCue(string blueprint, WorldAudioCue cue)
        {
            var target = RepairTarget(blueprint); PayRepair(target); var recipe = RepairRecipeRegistry.Get(target.GetPart<RepairablePart>().RecipeId);
            int units = Count(recipe.MaterialBlueprint); Assert.True(Repair(target));
            Assert.True(target.GetPart<RepairablePart>().Repaired); Assert.AreEqual(units - recipe.Quantity, Count(recipe.MaterialBlueprint));
            Assert.AreEqual(1, Receipts.Count); Assert.AreEqual(cue, Receipts[0].Cue);
            Assert.AreSame(Actor, Receipts[0].Actor); Assert.AreSame(target, Receipts[0].Source); Assert.AreSame(Zone, Receipts[0].Zone);
            Assert.AreEqual(new Point(11, 10), Receipts[0].Contact); Assert.False(Receipts[0].SourceConsumed);
            CollectionAssert.AreEqual(new[] { cue + " take 0" }, Assigned());
            Audio.Update(.3f); Assert.False(Repair(target)); Assert.AreEqual(1, Receipts.Count); Assert.AreEqual(1, Audio.AcceptedSoundCount);
        }
        [TestCase("metal-equipment-brace", "Metal", WorldAudioCue.WorkRepairMetal)]
        [TestCase("wood-equipment-splint", "Wood", WorldAudioCue.WorkRepairWood)]
        [TestCase("leather-equipment-stitch", "Leather", WorldAudioCue.Use)]
        public void ActualPortableRepairUsesActorOriginAndExactRecipe(string recipe, string material, WorldAudioCue cue)
        {
            var item = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "WorkTestEquipment" };
            item.AddPart(new PhysicsPart { Takeable = true }); item.AddPart(new RenderPart());
            item.AddPart(new EquippablePart { Slot = "Hand" }); item.AddPart(new MeleeWeaponPart());
            item.AddPart(new CompositionPart { MaterialsRaw = material });
            item.AddPart(new RepairablePart { PortableEquipment = true, RecipeId = recipe });
            item.AddPart(new StatusEffectsPart()); Assert.True(Pack.AddObject(item)); Assert.True(item.ApplyEffect(new BrokenEffect()));
            PayRepair(item); Assert.True(Repair(item)); Assert.False(item.HasEffect<BrokenEffect>());
            Assert.AreEqual(1, Receipts.Count); Assert.AreEqual(cue, Receipts[0].Cue);
            Assert.AreEqual(new Point(10, 10), Receipts[0].Contact); Assert.AreSame(item, Receipts[0].Source);
            Assert.AreEqual(1, Audio.AcceptedSoundCount); Assert.False(Repair(item)); Assert.AreEqual(1, Receipts.Count);
        }
        [TestCase(false)][TestCase(true)]
        public void StagedRepairDoesNotSoundUntilTheParentActuallyCommits(bool commit)
        {
            var target = RepairTarget(); PayRepair(target); var tx = new InventoryTransaction();
            Stage(Actor, target, Zone, RepairablePart.RepairCommand, tx);
            Assert.True(target.GetPart<RepairablePart>().Repaired); Assert.IsEmpty(Receipts); Assert.IsEmpty(Assigned());
            if (commit) tx.Commit(); else tx.Rollback();
            Assert.AreEqual(commit, target.GetPart<RepairablePart>().Repaired); Assert.AreEqual(commit ? 1 : 0, Receipts.Count);
            tx.Commit(); Assert.AreEqual(commit ? 1 : 0, Receipts.Count, "completed transaction cannot replay observers");
        }
        [TestCase(1)][TestCase(3)]
        public void ActualFieldHarvestSoundsOnceForTheActionNotPerYieldUnit(int units)
        {
            var row = Place("RipeCropRow"); var harvest = row.GetPart<FieldHarvestPart>(); harvest.YieldCount = units;
            Assert.True(Act(row, "Harvest")); Assert.True(harvest.Harvested); Assert.AreEqual(units, Count(harvest.YieldBlueprint));
            Assert.AreEqual(1, Receipts.Count); Assert.AreEqual(WorldAudioCue.HarvestPlant, Receipts[0].Cue); Assert.False(Receipts[0].SourceConsumed);
            Assert.AreEqual(1, Audio.AcceptedSoundCount); Assert.False(Act(row, "Harvest")); Assert.AreEqual(1, Receipts.Count);
        }
        [TestCase("HarvestCultivatedCrop")][TestCase("GatherCrop")][TestCase("HarvestCropSeeds")]
        public void ActualCultivatedHarvestKeepsItsCommittedContactWhenItConsumesTheCrop(string command)
        {
            var crop = Crop(2); Assert.True(Act(crop, command)); Assert.IsNull(Zone.GetEntityCell(crop));
            Assert.AreEqual(command=="HarvestCropSeeds"?0:2, Count("CandyCarrot"));
            Assert.AreEqual(command=="HarvestCropSeeds"?3:1, Count("CandyCarrotSeed"));
            Assert.AreEqual(1, Receipts.Count); Assert.AreSame(crop, Receipts[0].Source); Assert.True(Receipts[0].SourceConsumed);
            Assert.AreEqual(new Point(11, 10), Receipts[0].Contact); Assert.AreEqual(WorldAudioCue.HarvestPlant, Receipts[0].Cue);
            Assert.AreEqual(1, Audio.AcceptedSoundCount); Assert.False(Act(crop, command)); Assert.AreEqual(1, Receipts.Count);
        }
        [Test] public void AutomaticMaturityHasNoHandHarvestSound()
        {
            var crop = Crop(1, 2); var growing=crop.GetPart<CropPart>(); growing.HarvestAtMaturity = false;
            growing.TicksInStage=growing.TicksPerStage-1;
            CropSystem.OnTickEnd(Zone); Assert.IsNull(Zone.GetEntityCell(crop));
            Assert.Greater(Count("CandyCarrot"), 0); Assert.IsEmpty(Receipts); Assert.IsEmpty(Assigned());
        }
        [TestCase(1)][TestCase(2)]
        public void ActualForgeCommandPublishesOneCuePerCommittedBatch(int count)
        {
            var forge = Place("TinkersForge"); var command = ForgeCommand(count);
            Assert.True(InventorySystem.ExecuteCommand(command, Actor, Zone).Success); Assert.AreEqual(count, command.ForgedWeapons.Count);
            Assert.AreEqual(1, Receipts.Count); Assert.AreEqual(WorldAudioCue.WorkForge, Receipts[0].Cue); Assert.AreSame(forge, Receipts[0].Source);
            Assert.AreEqual(1, Audio.AcceptedSoundCount); Assert.AreEqual(16, Audio.AllocatedSources);
        }
        [Test] public void LegitimateSinglePackAssemblyCannotInventAnAnvil()
        {
            var command = ForgeCommand(); Assert.True(InventorySystem.ExecuteCommand(command, Actor, Zone).Success);
            Assert.AreEqual(1, command.ForgedWeapons.Count); Assert.IsEmpty(Receipts); Assert.IsEmpty(Assigned());
        }
    }
}
