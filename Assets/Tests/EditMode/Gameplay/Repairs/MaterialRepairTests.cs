using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class MaterialRepairTests
    {
        protected Zone Zone;
        protected Entity Actor, Target;
        protected RepairablePart Repair;
        [SetUp] public void SetUp()
        {
            RepairRecipeRegistry.ResetForTests(); RepairRecipeRegistry.LoadDefaults();
            Zone = new Zone("repair-test");
            Actor = Make("Player", true); Actor.SetTag("Player");
            Actor.Statistics["Hitpoints"] = new Stat { BaseValue = 20, Max = 20, Owner = Actor };
            Actor.AddPart(new InventoryPart()); Actor.AddPart(new StatusEffectsPart());
            Assert.True(Zone.AddEntity(Actor, 4, 4));
            Target = Make("RepairWell", false); Target.AddPart(new CompositionPart { MaterialsRaw = "Masonry, Wood, Fiber" });
            Repair = new RepairablePart { RecipeId = "clay-well-lining" }; Target.AddPart(Repair); Target.AddPart(new WellPart());
            Assert.True(Zone.AddEntity(Target, 5, 4));
            MessageLog.Clear();
        }
        [TearDown] public void TearDown() { RepairRecipeRegistry.ResetForTests(); }
        protected static Entity Make(string blueprint, bool takeable)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = blueprint };
            e.AddPart(new PhysicsPart { Takeable = takeable, Weight = 1 });
            e.AddPart(new RenderPart { DisplayName = blueprint }); return e;
        }
        protected Entity Supply(string blueprint = "FireClay", int count = 2)
        {
            var e = Make(blueprint, true); e.AddPart(new StackerPart { StackCount = count });
            Actor.GetPart<InventoryPart>().Objects.Add(e); e.GetPart<PhysicsPart>().InInventory = Actor; return e;
        }
        protected bool Act() => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(Target, RepairablePart.RepairCommand), Actor, Zone).Success;
        protected int Count(string blueprint) => Actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        [TestCase("clay-well-lining", "FireClay", 2)][TestCase("timber-gate-frame", "SalvagedTimber", 2)][TestCase("rope-well-line", "KnotflaxCord", 1)]
        public void ExactFaultConsumesExactSuppliesOnce(string recipe, string supply, int quantity)
        {
            Repair.RecipeId = recipe; Supply(supply, quantity + 2);
            Assert.True(Act()); Assert.True(Repair.Repaired); Assert.AreEqual(2, Count(supply));
            Assert.False(Act()); Assert.AreEqual(2, Count(supply));
        }
        [Test] public void CountsAcrossStacksAndPreservesOtherMaterials()
        {
            Supply(count: 1); Supply(count: 3); var cord = Supply("KnotflaxCord", 5);
            Assert.True(Act()); Assert.AreEqual(2, Count("FireClay")); Assert.AreEqual(5, cord.GetPart<StackerPart>().StackCount);
        }
        [Test] public void CompositionIsIndependentOfMaterialReactions()
        {
            Target.GetPart<CompositionPart>().MaterialsRaw = "Wood"; Target.AddPart(new MaterialPart { MaterialID = "Masonry", MaterialTagsRaw = "Masonry" }); Supply();
            Assert.False(Act()); Assert.False(Repair.Repaired); Assert.AreEqual(2, Count("FireClay"));
            Target.GetPart<CompositionPart>().MaterialsRaw = "Masonry"; Assert.True(Act());
        }
        [Test] public void DamagedWellRejectsDirectDrawAndRepairRestoresIt()
        {
            Assert.False(Target.GetPart<WellPart>().IsUsable);
            Actor.ApplyEffect(new ParchedEffect());
            var draw = GameEvent.New("InventoryAction"); draw.SetParameter("Command", "DrawWaterAtWell"); draw.SetParameter("Actor", (object)Actor); draw.SetParameter("Zone", (object)Zone);
            Target.FireEvent(draw); Assert.False(draw.Handled); draw.Release(); Assert.True(Actor.HasEffect<ParchedEffect>());
            Supply(); Assert.True(Act()); Assert.True(Target.GetPart<WellPart>().IsUsable);
            draw = GameEvent.New("InventoryAction"); draw.SetParameter("Command", "DrawWaterAtWell"); draw.SetParameter("Actor", (object)Actor); draw.SetParameter("Zone", (object)Zone);
            Target.FireEvent(draw); Assert.True(draw.Handled); draw.Release(); Assert.False(Actor.HasEffect<ParchedEffect>());
        }
        [Test] public void DamagedWellCannotFillWaterskinAndOrdinaryWellStillCan()
        {
            var skin = Supply("Waterskin", 1); skin.AddPart(new WaterskinPart { Charges = 0, Capacity = 4 });
            Assert.False(WaterVesselService.TryAct(Actor, skin, Zone, "FillWaterskin")); Assert.AreEqual(0, skin.GetPart<WaterskinPart>().Charges);
            Target.RemovePart(Repair); Assert.True(WaterVesselService.TryAct(Actor, skin, Zone, "FillWaterskin")); Assert.AreEqual(4, skin.GetPart<WaterskinPart>().Charges);
        }
        [Test] public void JammedOpenGateCannotCloseUntilFrameRepaired()
        {
            Target.RemovePart(Target.GetPart<WellPart>()); Target.AddPart(new DoorPart { IsOpen = true }); Repair.RecipeId = "timber-gate-frame";
            var door = Target.GetPart<DoorPart>(); Assert.False(door.CanOperate(Actor, Zone)); Assert.False(door.TrySetOpen(Actor, Zone, false)); Assert.True(door.IsOpen);
            Supply("SalvagedTimber"); Assert.True(Act()); Assert.True(door.CanOperate(Actor, Zone)); Assert.True(door.TrySetOpen(Actor, Zone, false)); Assert.True(door.TrySetOpen(Actor, Zone, true));
        }
        [Test] public void OrdinaryDoorRemainsOperable()
        {
            Target.RemovePart(Repair); Target.AddPart(new DoorPart { IsOpen = true }); Assert.True(Target.GetPart<DoorPart>().TrySetOpen(Actor, Zone, false));
        }
        [Test] public void AfterActionExceptionRollsPaymentAndFaultBack()
        {
            var a = Supply(count: 1); var b = Supply(count: 3); Actor.AddPart(new ThrowAfter());
            Assert.False(Act()); Assert.False(Repair.Repaired); Assert.AreEqual(4, Count("FireClay"));
            Assert.AreSame(Actor, a.GetPart<PhysicsPart>().InInventory); Assert.AreSame(Actor, b.GetPart<PhysicsPart>().InInventory);
        }
        public sealed class ThrowAfter : Part { public override bool HandleEvent(GameEvent e) { if(e.ID == "AfterInventoryAction") throw new InvalidOperationException("rollback probe"); return true; } }
        [Test] public void DescriptionsAdvertiseRealRequirementsAndRestoredState()
        {
            StringAssert.Contains("2", Repair.Describe()); StringAssert.Contains("fire clay", Repair.Describe());
            Supply(); Assert.True(Act()); StringAssert.Contains("repaired", Repair.Describe().ToLowerInvariant());
        }
        [TestCase("RepairLinedWell", "FireClay", 2, "water escapes before a vessel can be filled")]
        [TestCase("RepairRopeWell", "KnotflaxCord", 1, "a frayed stump of rope hangs")]
        [TestCase("RepairWoodenGate", "SalvagedTimber", 2, "has wedged this short gate open")]
        public void AuthoredExamineDescribesCurrentFaultWithoutStaleDamageAfterRepair(string blueprint, string supply, int count, string staleDamage)
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            Assert.True(Zone.RemoveEntity(Target)); Target = factory.CreateEntity(blueprint);
            Assert.True(Zone.AddEntity(Target, 5, 4)); Repair = Target.GetPart<RepairablePart>();
            var examine = Target.GetPart<ExaminablePart>(); var recipe = RepairRecipeRegistry.Get(Repair.RecipeId);
            var before = examine.BuildExamineLine();
            StringAssert.Contains(recipe.Diagnosis, before);
            StringAssert.DoesNotContain(recipe.RepairedText, before);
            Supply(supply, count); Assert.True(Act());
            var after = examine.BuildExamineLine();
            StringAssert.Contains(recipe.RepairedText, after);
            StringAssert.DoesNotContain(recipe.Diagnosis, after);
            StringAssert.DoesNotContain(staleDamage, after.ToLowerInvariant());
            StringAssert.DoesNotContain("jammed", Target.GetDisplayName().ToLowerInvariant());
        }
        [Test] public void CompositionMatchesTrimmedExactCategories()
        {
            var c = Target.GetPart<CompositionPart>(); Assert.True(c.Contains("Wood")); Assert.False(c.Contains("wood")); Assert.False(c.Contains("")); Assert.False(c.Contains(null));
        }
    }
}
