using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class CombatInventoryThrowTests : FiftyWorldFixture
    {
        static readonly FieldInfo Registry = typeof(GasRegistry).GetField("_byId", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo Initialized = typeof(GasRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<string, GasDefinition> saved; bool initialized;
        [SetUp] public void SaveGases()
        {
            saved = new Dictionary<string, GasDefinition>((Dictionary<string, GasDefinition>)Registry.GetValue(null));
            initialized = GasRegistry.IsInitialized;
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/GasDefinitions"), "*.json").Select(File.ReadAllText));
        }
        [TearDown] public void RestoreGases()
        {
            var current = (Dictionary<string, GasDefinition>)Registry.GetValue(null);
            current.Clear(); foreach (var pair in saved) current[pair.Key] = pair.Value;
            Initialized.SetValue(null, initialized);
        }
        [TestCase(true)] [TestCase(false)]
        public void ClosedDoorAndOrdinaryWallBothBurstOnApproachSide(bool door)
        {
            if (door) Place("VillageDoor", 12, 10).GetPart<DoorPart>().IsOpen = false;
            else { var wall = new Entity(); wall.SetTag("Solid"); Assert.True(Zone.AddEntity(wall, 12, 10)); }
            Assert.True(Zone.GetCell(12, 10).IsSolid(), "fixture must start with closed solid geometry");
            var pod = Carry("VeilpuffBladder");
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(pod, 15, 10, new System.Random(1)), Actor, Zone);
            Assert.True(result.Success, result.ErrorMessage); Assert.False(Pack.Contains(pod));
            Assert.True(Zone.TileState.ObscuresSight(11, 10), "a solid impact must leave the paid screen on the approach side");
            Assert.False(Zone.TileState.ObscuresSight(12, 10), "no cover in solid geometry");
            Assert.False(Zone.TileState.ObscuresSight(13, 10), "no cover behind the obstacle");
        }
        [Test] public void NonSolidCreatureImpactStillCentersTheRealColdPayloadOnThatCreature()
        {
            var target = Place("Player", 13, 10); var pod = Carry("VeilpuffBladder");
            Assert.False(Zone.GetCell(13, 10).IsSolid());
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(pod, 15, 10, new System.Random(1)), Actor, Zone);
            Assert.True(result.Success, result.ErrorMessage); Assert.False(Pack.Contains(pod));
            Assert.True(Zone.TileState.ObscuresSight(14, 10)); Assert.False(Zone.TileState.ObscuresSight(11, 10));
            Assert.True(Zone.GetEntityCell(target).Objects.Any(e => e.HasPart<GasCryoPart>()));
        }
    }
}
