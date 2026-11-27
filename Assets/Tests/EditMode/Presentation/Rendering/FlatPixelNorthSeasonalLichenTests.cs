using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelNorthSeasonalLichenTests
    {
        private const string North = "Overworld.11.9.0";
        private const int Seed = 64;
        private const int DormantTick = 70;
        private const int ReadyTick = 28870;

        [Test]
        public void Seed64ShallowCrustSignalsSeasonWithoutChangingOrdinaryLichenOrSavedAppearance()
        {
            InNorth((zone, clock, catalog) =>
            {
                var seasonal = WithRole(zone, "seasonal-cold-forage");
                var ordinary = WithRole(zone, "cold-forage");
                var render = seasonal.GetPart<RenderPart>();
                string originalGlyph = render.RenderString;
                string originalColor = render.ColorString;

                Assert.That(WorldClock.GetBand(DormantTick), Is.EqualTo(WorldClock.GetBand(ReadyTick)));
                clock.AdvanceClock(DormantTick);
                var dormant = Visible(zone, catalog, seasonal);
                var ordinaryBefore = Visible(zone, catalog, ordinary);

                clock.AdvanceClock(ReadyTick - DormantTick);
                var ready = Visible(zone, catalog, seasonal);
                var ordinaryAfter = Visible(zone, catalog, ordinary);

                AssertFrostFrame(ordinaryBefore);
                AssertFrostFrame(ordinaryAfter);
                Assert.That(ordinaryAfter.ForegroundTile, Is.SameAs(ordinaryBefore.ForegroundTile));
                AssertFrostFrame(ready);
                Assert.That(dormant.NativeForeground, Is.False, "Unworkable crust should not look like a ripe frost lichen patch.");
                Assert.That(dormant.ForegroundTile, Is.Not.Null);
                Assert.That(dormant.ForegroundTile.name, Is.EqualTo("Flat world glyph 95"));
                Assert.That(dormant.ForegroundTile, Is.Not.SameAs(ready.ForegroundTile));
                Assert.That(render.RenderString, Is.EqualTo(originalGlyph));
                Assert.That(render.ColorString, Is.EqualTo(originalColor));
                Assert.That(seasonal.GetPart<HarvestablePart>().Harvested, Is.False);
            });
        }

        [Test]
        public void SeasonalLichenAppearanceDoesNotLeakThroughFogOrFollowAFormerOwner()
        {
            InNorth((zone, clock, catalog) =>
            {
                var seasonal = WithRole(zone, "seasonal-cold-forage");
                var cell = zone.GetEntityCell(seasonal);
                clock.AdvanceClock(ReadyTick);
                AssertFrostFrame(Visible(zone, catalog, seasonal));
                cell.IsVisible = false;
                Assert.That(catalog.TryResolvePresentation(zone, cell.X, cell.Y, out var memory), Is.True);
                Assert.That(memory.Owner, Is.Not.SameAs(seasonal));
                Assert.That(memory.NativeForeground, Is.False);
                cell.IsVisible = true;
                AssertFrostFrame(Visible(zone, catalog, seasonal));

                Assert.That(zone.RemoveEntity(seasonal), Is.True);
                Assert.That(catalog.TryResolvePresentation(zone, cell.X, cell.Y, out var empty), Is.True);
                Assert.That(empty.Owner, Is.Not.SameAs(seasonal));
                Assert.That(empty.NativeForeground, Is.False);
            });
        }

        private static Entity WithRole(Zone zone, string role) => zone.GetReadOnlyEntities()
            .Single(owner => owner.GetProperty(SpreadExplorationWorksites.RoleKey) == role);

        private static FlatPixelGreyboxCell Visible(Zone zone, FlatPixelGreyboxCatalog catalog, Entity owner)
        {
            var cell = zone.GetEntityCell(owner);
            cell.Explored = cell.IsVisible = true;
            Assert.That(catalog.TryResolvePresentation(zone, cell.X, cell.Y, out var view), Is.True);
            Assert.That(view.Owner, Is.SameAs(owner));
            return view;
        }

        private static void AssertFrostFrame(FlatPixelGreyboxCell view)
        {
            Assert.That(view.NativeForeground, Is.True);
            Assert.That(view.ForegroundTile?.sprite?.texture, Is.SameAs(Resources.Load<Texture2D>("Sprites/Live16x24/north-works")));
            Assert.That(view.ForegroundTile.sprite.rect.x, Is.EqualTo(3 * 16f));
        }

        private static void InNorth(Action<Zone, TurnManager, FlatPixelGreyboxCatalog> check)
        {
            using var scope = new HaulingContentScope();
            scope.Seed(unchecked(Seed ^ FormationSelector.StableIndex(North, int.MaxValue)));
            var oldClock = TurnManager.Active;
            var oldZone = SettlementRuntime.ActiveZone;
            try
            {
                var clock = new TurnManager();
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, Seed, true);
                SoddenDistrictIntegrationTests.Version(manager, 31);
                manager.SetActiveZone(North);
                var zone = manager.ActiveZone;
                Assert.That(zone, Is.Not.Null);
                Assert.That(manager.Exploration.Find(North)?.Family, Is.EqualTo(SpreadExplorationFamily.FieldAlembic));
                Assert.That(manager.Exploration.DispositionFor(North), Is.EqualTo(2));
                SettlementRuntime.ActiveZone = zone;
                using var catalog = new FlatPixelGreyboxCatalog();
                check(zone, clock, catalog);
            }
            finally
            {
                SettlementRuntime.ActiveZone = oldZone;
                typeof(TurnManager).GetField("<Active>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, oldClock);
            }
        }
    }
}
