using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SoddenTransientArtTests
    {
        Dictionary<string,GasDefinition> prior;
        bool initialized;
        Dictionary<string,LiquidDefinition> priorLiquids;
        bool liquidsInitialized;
        static FieldInfo Liquids => typeof(LiquidRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static);
        static FieldInfo LiquidsInitialized => typeof(LiquidRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
        static FieldInfo Registry => typeof(GasRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static);
        static FieldInfo Initialized => typeof(GasRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
        [SetUp] public void SetupGasDefinitions()
        {
            prior=new Dictionary<string,GasDefinition>((Dictionary<string,GasDefinition>)Registry.GetValue(null));
            initialized=(bool)Initialized.GetValue(null);
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/GasDefinitions"),"*.json").Select(File.ReadAllText));
            priorLiquids=new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Liquids.GetValue(null));
            liquidsInitialized=(bool)LiquidsInitialized.GetValue(null);
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
        }
        [TearDown] public void RestoreGasDefinitions()
        {
            var map=(Dictionary<string,GasDefinition>)Registry.GetValue(null);map.Clear();
            foreach(var pair in prior)map[pair.Key]=pair.Value;
            Initialized.SetValue(null,initialized);
            var liquids=(Dictionary<string,LiquidDefinition>)Liquids.GetValue(null);liquids.Clear();
            foreach(var pair in priorLiquids)liquids[pair.Key]=pair.Value;
            LiquidsInitialized.SetValue(null,liquidsInitialized);
        }
        [TestCase("poison-vapor")][TestCase("marsh-gas")][TestCase("cryo-mist")]
        public void ActualBogGasHasCurrentThreeDimensionalVolumeAndExpiresWithItsSource(string id)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var c=f.FreeCell(); f.Zone.GetCell(c.x,c.y).IsVisible=true;f.Zone.GetCell(c.x,c.y).Explored=true;
                var gas=GasFactory.SpawnGas(f.Zone,c.x,c.y,id,80);Assert.NotNull(gas);
                Assert.True(SpreadTransientSource.TryGas(f.Zone,gas,out var before));
                f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetGasVolume(gas,out var root,out var sample));
                Assert.AreSame(gas,sample.Owner);Assert.AreEqual(before.Amount,sample.Amount);Assert.True(root.activeSelf);
                f.Zone.GetCell(c.x,c.y).IsVisible=false;f.Refresh();Assert.False(presenter.TryGetGasVolume(gas,out _,out _));
                f.Zone.GetCell(c.x,c.y).IsVisible=true;gas.GetPart<GasPoolPart>().Density=0;f.Refresh();
                Assert.False(presenter.TryGetGasVolume(gas,out _,out _));
            }
        }
        [TestCase("heat")][TestCase("cold")][TestCase("charge")][TestCase("veil-mist")][TestCase("steam")][TestCase("smoke")]
        public void BogSurfaceEffectsKeepTheirExactLifetimeAndVisibleOnlyPolicy(string kind)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var c=f.FreeCell(); var cell=f.Zone.GetCell(c.x,c.y);cell.IsVisible=true;cell.Explored=true;
                if(kind=="heat")f.Zone.TileState.AddHeat(c.x,c.y,2);
                else if(kind=="cold")f.Zone.TileState.AddCold(c.x,c.y,2);
                else if(kind=="charge")f.Zone.TileState.AddCharge(c.x,c.y,2);
                else f.Zone.TileState.WriteCloud(c.x,c.y,kind,2);
                Assert.True(SpreadTransientSource.TryElement(f.Zone,c.x,c.y,out var sample));Assert.AreEqual(kind,sample.Kind);
                f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetElementVolume(c.x,c.y,out _,out sample));Assert.AreEqual(kind,sample.Kind);
                cell.IsVisible=false;f.Refresh();Assert.False(presenter.TryGetElementVolume(c.x,c.y,out _,out _));
                cell.IsVisible=true;f.Zone.TileState.Clear(c.x,c.y);f.Refresh();Assert.False(presenter.TryGetElementVolume(c.x,c.y,out _,out _));
            }
        }
        [TestCase("oil")][TestCase("ice")][TestCase("water")]
        public void RealFiniteFilmsHaveOwnedSurfaceMarksWithoutChangingTheirTurns(string liquid)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                var c=f.FreeCell(); var cell=f.Zone.GetCell(c.x,c.y);cell.IsVisible=true;cell.Explored=true;
                f.Zone.TileState.WriteCoating(c.x,c.y,liquid,5);
                Assert.True(SpreadTransientSource.TrySurfaceMark(f.Zone,c.x,c.y,out var sample));Assert.AreEqual(5,sample.Amount);
                f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetElementVolume(c.x,c.y,out _,out sample));Assert.AreEqual("coating:"+liquid,sample.Kind);
                f.Manager.WorldMap.Tiles[15,7]=BiomeType.Beating;f.Refresh();Assert.False(presenter.TryGetElementVolume(c.x,c.y,out _,out _));
                Assert.AreEqual(5,f.Zone.TileState.Get(c.x,c.y).Coatings[0].Turns);
            }
        }
    }
}
