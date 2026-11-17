using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MachineContainedOilFlatTests
    {
        [TestCase(0)] [TestCase(6)] [TestCase(12)]
        public void WorldAndReaderUseSameApprovedSealedClayPanWithoutChangingWithContents(int amount)
        {
            using(var scope=new DensityLootTestScope())using(var art=new FlatPixelGreyboxCatalog())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);manager.SetActiveZone(zone);
                foreach(var c in zone.Cells)c.IsVisible=c.Explored=true;
                var tray=zone.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName=="CinderholdOilRecoveryTray");Assert.NotNull(tray);
                tray.GetPart<ContainedLiquidSourcePart>().Volume=amount;var at=zone.GetEntityPosition(tray);
                Assert.True(art.TryResolvePresentation(zone,at.x,at.y,out var visual));Assert.AreSame(tray,visual.Owner);Assert.True(visual.NativeForeground);
                Assert.AreSame(Resources.Load<Texture2D>("Sprites/Live16x24/east-kitchen"),visual.ForegroundTile.sprite.texture);
                Assert.AreEqual(new Rect(80,0,16,24),visual.ForegroundTile.sprite.rect);
                Assert.True(art.TryGetPresentationSubjectSprite(zone,tray,at.x,at.y,out var portrait,out var tint));
                Assert.AreSame(visual.ForegroundTile.sprite,portrait);Assert.AreEqual(Color.white,tint);
            }
        }
    }
}
