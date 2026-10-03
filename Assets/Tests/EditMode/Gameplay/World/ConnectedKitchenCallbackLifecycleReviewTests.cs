using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Hypothesis: a native owner lifecycle during output creation can
    /// strand paid ingredients when the reentry guard drops its interruption.</summary>
    public sealed class ConnectedKitchenCallbackLifecycleReviewTests
    {
        public sealed class MealCreatedLifecycleProbe : Part
        {
            public static Action Callback;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ObjectCreated") Callback?.Invoke();
                return true;
            }
        }

        static int GroundUnits(ConnectedKitchenFixture f, string blueprint) => f.Zone.GetReadOnlyEntities()
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);

        [TestCase("worker", false)] [TestCase("worker", true)]
        [TestCase("pan", false)] [TestCase("pan", true)]
        [TestCase("pickup", false)] [TestCase("pickup", true)]
        public void DueOutputCreationCannotStrandIngredientsAcrossNativeOwnerInvalidation(string owner, bool invalidate)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                Entity target = owner == "worker" ? f.Worker : owner == "pan" ? f.Pan : f.Pickup;
                if (owner != "worker") target.AddPart(new DestructiblePart());
                int created = 0;
                try
                {
                    f.Factory.RegisterPartType<MealCreatedLifecycleProbe>("KitchenLifecycleReviewProbe");
                    f.Factory.Blueprints["FieldMeal"].Parts["KitchenLifecycleReviewProbe"] = new Dictionary<string, string>();
                    MealCreatedLifecycleProbe.Callback = () =>
                    {
                        created++;
                        if (!invalidate) return;
                        if (owner == "worker") CombatSystem.HandleDeath(target, null, f.Zone);
                        else Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(target, null, f.Zone, "meal-callback"));
                    };
                    Assert.True(f.Start());
                    f.Clock.AdvanceClock(KitchenBatchPart.PreparationTicks);
                    KitchenBatchPart.ReconcileZone(f.Zone);
                    Assert.AreEqual(1, created, "The fixture must cross the output callback boundary exactly once.");
                    Assert.AreEqual(!invalidate, f.Zone.GetEntityCell(target) != null);
                    Assert.IsEmpty(f.Stored.Contents, "Invalidating a service owner must settle physical custody before it can become unreachable.");
                    Assert.AreNotEqual("Working", f.State, "A paid commission cannot remain stuck behind a removed bound owner.");
                    Assert.AreEqual(8, TradeSystem.GetDrams(f.Player), "The documented service fee remains paid.");
                    Assert.AreEqual(37, TradeSystem.GetDrams(f.Worker));
                    int meals = f.Finished.Contents.Count(e => e.BlueprintName == "FieldMeal") + GroundUnits(f, "FieldMeal");
                    int grain = GroundUnits(f, "Emberwheat"), pulp = GroundUnits(f, "ClaspbeanPulp");
                    Assert.True(meals == 1 && grain == 0 && pulp == 0 || meals == 0 && grain == 2 && pulp == 1,
                        "The same paid inputs resolve into exactly one meal or exact salvage, never both or neither.");
                    if (!invalidate) Assert.AreEqual(1, meals, "Ordinary due completion still publishes its parcel.");
                    KitchenBatchPart.ReconcileZone(f.Zone);
                    Assert.AreEqual(1, created, "Later reconciliation cannot recreate a settled output.");
                    Assert.AreEqual(meals, f.Finished.Contents.Count(e => e.BlueprintName == "FieldMeal") + GroundUnits(f, "FieldMeal"));
                    Assert.AreEqual(grain, GroundUnits(f, "Emberwheat")); Assert.AreEqual(pulp, GroundUnits(f, "ClaspbeanPulp"));
                }
                finally { MealCreatedLifecycleProbe.Callback = null; }
            }
        }
    }
}
