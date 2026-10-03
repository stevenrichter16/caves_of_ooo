using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A factory exception may block work, but it must not erase an
    /// irreversible owner death which occurred before that exception.</summary>
    public sealed class ConnectedKitchenCallbackExceptionReviewTests
    {
        public sealed class MealCreatedExceptionProbe : Part
        {
            public static Action Callback;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ObjectCreated") Callback?.Invoke();
                return true;
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void FactoryFailureOnlyPreservesWorkWhileItsWorkerStillExists(bool killWorker)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                int created = 0;
                try
                {
                    f.Factory.RegisterPartType<MealCreatedExceptionProbe>("KitchenExceptionReviewProbe");
                    f.Factory.Blueprints["FieldMeal"].Parts["KitchenExceptionReviewProbe"] = new Dictionary<string, string>();
                    MealCreatedExceptionProbe.Callback = () =>
                    {
                        created++;
                        if (killWorker) CombatSystem.HandleDeath(f.Worker, null, f.Zone);
                        throw new InvalidOperationException("Synthetic meal initializer failure.");
                    };
                    Assert.True(f.Start()); f.Clock.AdvanceClock(KitchenBatchPart.PreparationTicks);
                    Assert.DoesNotThrow(() => KitchenBatchPart.ReconcileZone(f.Zone));
                    Assert.AreEqual(1, created);
                    Assert.IsEmpty(f.Finished.Contents);
                    Assert.AreEqual(killWorker ? "Idle" : "Working", f.State);
                    Assert.AreEqual(killWorker ? 0 : 2, f.Stored.Contents.Count);
                    Assert.AreEqual(killWorker ? 2 : 0, f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Emberwheat").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                    Assert.AreEqual(killWorker ? 1 : 0, f.Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "ClaspbeanPulp"));
                    Assert.AreEqual(8, TradeSystem.GetDrams(f.Player));
                    MealCreatedExceptionProbe.Callback = () => created++;
                    KitchenBatchPart.ReconcileZone(f.Zone);
                    Assert.AreEqual(killWorker ? 1 : 2, created);
                    Assert.AreEqual(killWorker ? 0 : 1, f.Finished.Contents.Count);
                    Assert.IsEmpty(f.Stored.Contents);
                }
                finally { MealCreatedExceptionProbe.Callback = null; }
            }
        }
    }
}
