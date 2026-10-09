using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual current native owners, submitted meshes and borrowed art.
    /// Reflection permits a behavioral RED before the additive library exists.</summary>
    public sealed class SoddenNativeStyleIntegrationTests
    {
        static Type RequiredType(string name)
        {
            var type = typeof(SpawnRing3DPresenter).Assembly.GetType("CavesOfOoo.Rendering." + name);
            Assert.NotNull(type, "The additive native Sodden art contract is required.");
            return type;
        }
        static bool Active(Zone zone)
            => (bool)RequiredType("SoddenPresentationScope").GetMethod("IsActive").Invoke(null, new object[] { zone });
        static UnityEngine.Object Art()
        {
            var art = Resources.Load("SoddenNativeArt3D/Library", RequiredType("SoddenNativeArtLibrary"));
            Assert.NotNull(art, "Import the actual original regional kit before integration GREEN.");
            art.GetType().GetMethod("Validate").Invoke(art, null);
            return art;
        }
        static object Entry(UnityEngine.Object art, string id)
        {
            var entry = art.GetType().GetMethod("Find").Invoke(art, new object[] { id });
            Assert.NotNull(entry, "The effective native recipe needs an exact art entry: " + id);
            return entry;
        }
        static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name).GetValue(owner);

        static bool RegionalStructure(string id)
        {
            if (id == null) return false;
            return id.StartsWith("spread-environment-paving-", StringComparison.Ordinal)
                || id.StartsWith("wellmeet-floor-", StringComparison.Ordinal)
                || id.StartsWith("sumphold-wall-", StringComparison.Ordinal)
                || id.StartsWith("sumphold-water-", StringComparison.Ordinal)
                || id.StartsWith("drownedledger-boards-", StringComparison.Ordinal)
                || id.StartsWith("wellmeet-tent-", StringComparison.Ordinal)
                || id.StartsWith("wellmeet-corner-", StringComparison.Ordinal)
                || id.StartsWith("drownedledger-preserved-", StringComparison.Ordinal)
                || id.StartsWith("drownedledger-stake-", StringComparison.Ordinal)
                || id.StartsWith("drownedledger-table-", StringComparison.Ordinal)
                || id == SoddenDistrictArtLibrary.BrokenBench || id == SoddenDistrictArtLibrary.WorkingBench
                || id == SoddenDistrictArtLibrary.Locker || id == SoddenDistrictArtLibrary.Salvage
                || id == SoddenDistrictArtLibrary.Notice;
        }

        [TestCase("Overworld.15.6.0")][TestCase("Overworld.17.5.0")]
        [TestCase("Overworld.15.7.0")][TestCase("Overworld.17.7.0")]
        public void ActualNamedPlacesSubmitRegionalArchitectureAndUsefulProps(string id)
        {
            using (var f = new SpawnRing3DIntegrationFixture(id))
            {
                var art = Art(); var presenter = (SpawnRing3DPresenter)f.Presenter;
                int owners = 0, version = f.Zone.EntityVersion; string tiles = f.Zone.TileState.ToSaveString();
                foreach (var owner in f.Zone.GetReadOnlyEntities())
                {
                    var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
                    if (!RegionalStructure(recipe.ModelId)) continue;
                    var expected = Entry(art, recipe.ModelId);
                    Assert.True(presenter.TryGetApprovedStyle(owner, out var evidence), owner.BlueprintName + ": " + evidence.Failure);
                    Assert.AreSame(Field<Mesh>(expected, "Mesh"), evidence.ExpectedMesh);
                    Assert.AreSame(Field<Material>(art, "Material"), evidence.ExpectedMaterial);
                    Assert.AreNotSame(f.Library.FindModel(recipe.ModelId).GetComponent<MeshFilter>()?.sharedMesh, evidence.ExpectedMesh,
                        "An approval label must not disguise unchanged original architecture: " + recipe.ModelId);
                    Assert.AreEqual(recipe.Batched, evidence.Batched);
                    owners++;
                }
                Assert.Greater(owners, 20, "Actual native generation must exercise the regional architecture.");
                Assert.AreEqual(version, f.Zone.EntityVersion);
                Assert.AreEqual(tiles, f.Zone.TileState.ToSaveString());
            }
        }

        [Test] public void RegionalBenchArtFollowsRealRepairStateAndKeepsLiteralOwner()
        {
            using (var f = new SpawnRing3DIntegrationFixture(SoddenDistrictPlan.StopZoneID))
            {
                var owner = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SoddenDressingBench");
                var repair = owner.GetPart<RepairablePart>(); var parts = owner.Parts.ToArray();
                Assert.False(repair.Repaired); var art = Art(); var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out var broken), broken.Failure);
                Assert.AreSame(Field<Mesh>(Entry(art, SoddenDistrictArtLibrary.BrokenBench), "Mesh"), broken.ExpectedMesh);
                repair.Repaired = true; f.Refresh();
                Assert.True(presenter.TryGetApprovedStyle(owner, out var working), working.Failure);
                Assert.AreSame(Field<Mesh>(Entry(art, SoddenDistrictArtLibrary.WorkingBench), "Mesh"), working.ExpectedMesh);
                Assert.AreNotSame(broken.ExpectedMesh, working.ExpectedMesh);
                CollectionAssert.AreEqual(parts, owner.Parts);
                owner.GetPart<RenderPart>().RenderString = "X"; f.Refresh();
                Assert.False(presenter.TryGetApprovedStyle(owner, out _));
            }
        }

        [Test] public void SpentActualWorksSalvageCannotKeepItsNewUsefulStockAppearance()
        {
            using (var f = new SpawnRing3DIntegrationFixture(SoddenDistrictPlan.WorksZoneID))
            {
                var owner = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SoddenWorksSalvage");
                var presenter = (SpawnRing3DPresenter)f.Presenter; var art = Art();
                Assert.True(presenter.TryGetApprovedStyle(owner, out var evidence), evidence.Failure);
                Assert.AreSame(Field<Mesh>(Entry(art, SoddenDistrictArtLibrary.Salvage), "Mesh"), evidence.ExpectedMesh);
                owner.GetPart<HarvestablePart>().Harvested = true;
                Assert.False(presenter.TryGetApprovedStyle(owner, out _));
                f.Refresh(); Assert.False(f.Rendered(owner));
            }
        }

        [Test] public void SharedPavingRetainsItsOtherBiomePalette()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("Floor"); f.Refresh(); var art = Art();
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out var evidence), evidence.Failure);
                Assert.That(evidence.ModelId, Does.StartWith("spread-environment-paving-"));
                var regional = Entry(art, evidence.ModelId);
                Assert.AreNotSame(Field<Mesh>(regional, "Mesh"), evidence.ExpectedMesh);
                Assert.AreNotSame(Field<Material>(art, "Material"), evidence.ExpectedMaterial);
            }
        }

        [Test] public void RegionalPaletteApprovalRejectsAnActualSubmittedColorOverride()
        {
            using (var f = new SpawnRing3DIntegrationFixture(SumpholdCompositionPlan.ZoneID))
            {
                var owner = f.Zone.GetReadOnlyEntities().First(e => e.BlueprintName == "Floor");
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out var before), before.Failure);
                var renderer = f.View(owner).GetComponentsInChildren<MeshRenderer>(true)
                    .Single(r => r.GetComponent<MeshFilter>().sharedMesh == before.SubmittedMesh);
                var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original);
                var changed = new MaterialPropertyBlock(); renderer.GetPropertyBlock(changed);
                try
                {
                    changed.SetColor("_BaseColor", Color.magenta); renderer.SetPropertyBlock(changed);
                    Assert.False(presenter.TryGetApprovedStyle(owner, out _));
                    Assert.AreEqual(Color.white, before.ExpectedMaterial.GetColor("_BaseColor"));
                }
                finally { renderer.SetPropertyBlock(original); }
                Assert.True(presenter.TryGetApprovedStyle(owner, out _));
            }
        }

        [TestCase("VineWall", "ring-vine-wall-")]
        [TestCase("BrinePool", "ring-brine-pool-")]
        [TestCase("AcidPool", "density-pool-acid")]
        [TestCase("SteamVent", "ring-steam-vent")]
        public void RemainingNativeHazardsUseRegionalGeometryWithoutChangingTheirRealState(string blueprint, string modelPrefix)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.8.0"))
            {
                Assert.True(Active(f.Zone)); var art = Art();
                var owner = f.Add(blueprint); var parts = owner.Parts.ToArray();
                var pool = owner.GetPart<LiquidPoolPart>(); string liquid = pool?.LiquidId; int volume = pool?.Volume ?? 0;
                var thermal = owner.GetPart<ThermalPart>(); float temperature = thermal?.Temperature ?? 0;
                var source = owner.GetPart<TileStateSourcePart>();
                var sourceState = source == null ? null : new object[] { source.Coating, source.CoatingTurns, source.Heat,
                    source.Cold, source.Charge, source.Residue, source.ResidueTurns, source.RadiatesEnergy };
                var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
                Assert.NotNull(recipe.ModelId, blueprint + ": native recipe policy: " + recipe.Failure);
                Assert.That(recipe.ModelId, Does.StartWith(modelPrefix), "Keep the existing native identity before selecting regional art.");
                var oldPrefab = f.Library.FindModel(recipe.ModelId); Assert.NotNull(oldPrefab);
                var oldMesh = oldPrefab.GetComponentInChildren<MeshFilter>().sharedMesh; Assert.NotNull(oldMesh);
                int version = f.Zone.EntityVersion; string tiles = f.Zone.TileState.ToSaveString();
                f.Refresh(); var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out var evidence), blueprint + ": regional proof: " + evidence.Failure);
                var expected = Entry(art, recipe.ModelId);
                Assert.AreSame(Field<Mesh>(expected, "Mesh"), evidence.ExpectedMesh);
                Assert.AreSame(Field<Material>(art, "Material"), evidence.ExpectedMaterial);
                Assert.AreNotSame(oldMesh, evidence.ExpectedMesh);
                Assert.Greater(evidence.SubmittedMesh.vertexCount, 0); Assert.Greater(evidence.PieceCount, 0);
                Assert.AreEqual(recipe.ModelId, evidence.ModelId); Assert.AreEqual(recipe.Batched, evidence.Batched);
                CollectionAssert.AreEqual(parts, owner.Parts); Assert.AreEqual(version, f.Zone.EntityVersion);
                Assert.AreEqual(tiles, f.Zone.TileState.ToSaveString());
                Assert.AreEqual(liquid, pool?.LiquidId); Assert.AreEqual(volume, pool?.Volume ?? 0);
                Assert.AreEqual(temperature, thermal?.Temperature ?? 0);
                if (source != null) CollectionAssert.AreEqual(sourceState, new object[] { source.Coating, source.CoatingTurns,
                    source.Heat, source.Cold, source.Charge, source.Residue, source.ResidueTurns, source.RadiatesEnergy });
                // Removal is an established native ownership counter. Do not
                // invent a stricter glyph policy for existing native aliases.
                Assert.True(f.Zone.RemoveEntity(owner));
                Assert.False(presenter.TryGetApprovedStyle(owner, out _));
                f.Refresh(); Assert.False(f.Rendered(owner));
            }
        }

        [TestCase("Overworld.15.7.0")]
        [TestCase("Overworld.16.7.0")]
        [TestCase("Overworld.17.5.0")]
        public void ExistingNativeTerrainUsesTheExactNewGeometryAndPalette(string zoneId)
        {
            using (var f = new SpawnRing3DIntegrationFixture(zoneId))
            {
                Assert.True(Active(f.Zone)); var art = Art();
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                foreach (string blueprint in new[] { "Grass", "MirePool", "PeatBank", "DeadTree", "Duckboard", "BogTakenBody", "Reeds" })
                {
                    var owner = f.Add(blueprint); var parts = owner.Parts.ToArray();
                    var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
                    Assert.NotNull(recipe.ModelId, blueprint + ": " + recipe.Failure);
                    int version = f.Zone.EntityVersion; string tiles = f.Zone.TileState.ToSaveString();
                    f.Refresh();
                    Assert.True(presenter.TryGetApprovedStyle(owner, out var evidence), blueprint + ": " + evidence.Failure);
                    var expected = Entry(art, recipe.ModelId);
                    Assert.AreSame(Field<Mesh>(expected, "Mesh"), evidence.ExpectedMesh);
                    Assert.AreSame(Field<Material>(art, "Material"), evidence.ExpectedMaterial);
                    Assert.Greater(evidence.SubmittedMesh.vertexCount, 0);
                    Assert.AreEqual(recipe.ModelId, evidence.ModelId);
                    Assert.AreEqual(recipe.Batched, evidence.Batched);
                    Assert.AreEqual(version, f.Zone.EntityVersion);
                    Assert.AreEqual(tiles, f.Zone.TileState.ToSaveString());
                    CollectionAssert.AreEqual(parts, owner.Parts);
                }
            }
        }

        [Test] public void NewStyleRequiresExactManagedOwnerRatherThanPlausibleAddress()
        {
            Assert.False(Active(null));
            Assert.False(Active(new Zone("Overworld.15.7.0")));
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                Assert.True(Active(f.Zone));
                var sameAddress = new Zone(f.Zone.ZoneID);
                Assert.False(Active(sameAddress));
                f.Manager.CachedZones[f.Zone.ZoneID] = sameAddress;
                try { Assert.False(Active(f.Zone), "A replaced graph cannot retain new-style authority."); }
                finally { f.Manager.CachedZones[f.Zone.ZoneID] = f.Zone; }
                Assert.True(Active(f.Zone));
            }
        }

        [Test] public void EveryActualSoddenSurfaceIncludingNamedPlacesReceivesRegionalAuthority()
        {
            using (var content = new DensityLootTestScope())
            {
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64);
                int checkedSurfaces = 0, outsideWilderness = 0;
                for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
                {
                    if (manager.WorldMap.GetBiome(x, y) != BiomeType.Sodden) continue;
                    string id = WorldMap.ToZoneID(x, y);
                    var zone = new Zone(id); manager.SetActiveZone(zone);
                    Assert.True(Active(zone), "Actual Sodden surfaces include named sites and lair mouths: " + id);
                    checkedSurfaces++;
                    if (!SoddenCompositionPlan.IsWildernessZone(id)) outsideWilderness++;
                }
                Assert.Greater(checkedSurfaces, 50);
                Assert.Greater(outsideWilderness, 0, "The counterexample must exercise more than the old finite wilderness index.");
            }
        }

        [Test] public void AuthenticatedSoddenBeyondOldAddressCatalogCanBindItsActualOwner()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                const string id = "Overworld.0.0.0";
                Assert.False(SoddenCompositionPlan.IsWildernessZone(id));
                f.Manager.WorldMap.Tiles[0, 0] = BiomeType.Sodden;
                f.Manager.WorldMap.SetPOI(0, 0, null);
                var zone = new Zone(id); f.Manager.SetActiveZone(zone);
                var owner = f.Factory.CreateEntity("Reeds"); Assert.True(zone.AddEntity(owner, 20, 10));
                zone.GetCell(20, 10).Explored = zone.GetCell(20, 10).IsVisible = true;
                f.Zone = zone; f.Bind(zone); f.Refresh();
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.IsReady, presenter.Failure);
                Assert.True(presenter.TryGetApprovedStyle(owner, out var evidence), evidence.Failure);
                Assert.True(presenter.VoxelPresentationActive);
            }
        }

        [Test] public void ExistingBareCellFallbackUsesTheRegionalSubstratePalette()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var sparse = new Zone(f.Zone.ZoneID); f.Manager.SetActiveZone(sparse); f.Zone = sparse;
                var owner = f.Add("Reeds", 20, 10); f.Reveal(); f.Bind(sparse); f.Refresh();
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out var evidence), evidence.Failure);
                var patch = f.View(owner);
                var renderers = patch.GetComponentsInChildren<MeshRenderer>(true);
                Assert.Greater(renderers.Length, 0);
                Assert.True(renderers.All(r => r.sharedMaterial.GetTexture("_BaseMap")
                    == evidence.ExpectedMaterial.GetTexture("_BaseMap")),
                    "The existing bare-cell fallback must not leave an old olive plane between new bog models.");
                Assert.AreEqual(1, sparse.EntityCount, "The substrate remains presentation fallback, never new native terrain.");
            }
        }

        [Test] public void RegionAuthorityChangeImmediatelyRelinquishesStyleAndRefreshRebinds()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var owner = f.Add("Reeds"); f.Refresh(); var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out _));
                var before = f.Manager.WorldMap.Tiles[15, 7];
                try
                {
                    f.Manager.WorldMap.Tiles[15, 7] = BiomeType.Beating;
                    Assert.False(Active(f.Zone));
                    Assert.False(presenter.TryGetApprovedStyle(owner, out _), "No stale style evidence before the next frame.");
                    f.Refresh(); Assert.False(presenter.TryGetApprovedStyle(owner, out _));
                }
                finally { f.Manager.WorldMap.Tiles[15, 7] = before; }
                f.Refresh(); Assert.True(presenter.TryGetApprovedStyle(owner, out _));
            }
        }

        [Test] public void SumpholdIsAnExactAuthenticatedTownException()
        {
            using (var f = new SpawnRing3DIntegrationFixture(SumpholdCompositionPlan.ZoneID))
            {
                Assert.AreEqual(BiomeType.Spread, f.Manager.WorldMap.GetBiome(15, 6), "The native gameplay biome must remain unchanged.");
                Assert.True(Active(f.Zone));
                var poi = f.Manager.WorldMap.GetPOI(15, 6); Assert.NotNull(poi);
                string profile = poi.Profile;
                try { poi.Profile = "TentCamp"; Assert.False(Active(f.Zone)); }
                finally { poi.Profile = profile; }
                Assert.True(Active(f.Zone));
            }
        }

        [Test] public void StartingSpreadKeepsItsExistingApprovedArt()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Assert.False(Active(f.Zone));
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(f.Player, out var evidence), evidence.Failure);
                Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material, evidence.ExpectedMaterial);
            }
        }

        [TestCase("hidden")][TestCase("removed")]
        public void NativeOwnerRefusalCannotBeRepairedByTheNewArt(string change)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var owner = f.Add("Reeds"); f.Refresh(); var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out _));
                if (change == "hidden") owner.GetPart<RenderPart>().Visible = false;
                else Assert.True(f.Zone.RemoveEntity(owner));
                Assert.Null(SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition).ModelId);
                Assert.False(presenter.TryGetApprovedStyle(owner, out _));
                f.Refresh(); Assert.False(presenter.IsRenderedEntity(owner));
            }
        }

        [Test] public void GroundMemoryAndRefreshReuseTheCommittedPatch()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var owner = f.Add("Reeds"); f.Refresh(); var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out var before), before.Failure);
                int builds = presenter.GroundBuildCount; var cell = f.Zone.GetEntityCell(owner);
                cell.IsVisible = false; f.Refresh();
                Assert.True(presenter.TryGetApprovedStyle(owner, out var remembered), remembered.Failure);
                Assert.AreSame(before.SubmittedMesh, remembered.SubmittedMesh);
                Assert.AreEqual(builds, presenter.GroundBuildCount, "FOV updates do not rebuild static geometry.");
                cell.Explored = false; f.Refresh();
                Assert.False(presenter.TryGetApprovedStyle(owner, out _));
                Assert.AreEqual(builds, presenter.GroundBuildCount);
            }
        }

        [Test] public void SavedNativeOwnerRetainsItsLiteralIdentityAndGetsScopedArtOnLoad()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var owner = f.Add("Duckboard"); string id = owner.ID; f.Refresh();
                var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out var before), before.Failure);
                f.BindLoaded(f.RoundTrip());
                var restored = f.Zone.GetReadOnlyEntities().Single(e => e.ID == id);
                Assert.True(Active(f.Zone));
                Assert.AreEqual(recipe.ModelId, SpawnRing3DRecipes.Resolve(f.Zone, restored, f.Library.Definition).ModelId);
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(restored, out var after), after.Failure);
                Assert.AreSame(before.ExpectedMesh, after.ExpectedMesh);
            }
        }
    }
}
