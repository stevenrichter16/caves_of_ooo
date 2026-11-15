using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SaltVaultApproachTests
    {
        [TestCase(64,true)] [TestCase(1729,false)] [TestCase(224401440,true)]
        public void FreshArchiveUsesOnlyItsOwnSurvivingDoorAndPreservesItsFile(int seed,bool hasIntactDoor)
        {
            using (var scope = new HotbarSaveFixture(false, false))
            {
                var m = OverworldZoneManager.CreateDetached(GrovelandsCompositionTests.Factory(), seed);
                var z = m.GetZone(StillleafSaltVault.ZoneId);
                var paving = z.GetAllEntities().Where(e => e.BlueprintName == "StoneFloor" && !z.GetEntityCell(e).IsInterior).ToArray();
                if(hasIntactDoor)Assert.That(paving.Length, Is.InRange(6, 9));
                else
                {
                    Assert.AreEqual(0,paving.Length,"Do not borrow another house's doorway when this archive has no intact one.");
                    var at=z.GetEntityCell(StillleafSaltVault.FindCabinet(z));var room=new HashSet<(int,int)>{(at.X,at.Y)};var todo=new Queue<(int x,int y)>();todo.Enqueue((at.X,at.Y));
                    while(todo.Count>0){var c=todo.Dequeue();foreach(var d in new[]{(0,-1),(-1,0),(1,0),(0,1)}){var n=(c.x+d.Item1,c.y+d.Item2);var cell=z.GetCell(n.Item1,n.Item2);if(cell?.IsInterior==true&&room.Add(n))todo.Enqueue(n);}}
                    foreach(var door in z.GetAllEntities().Where(e=>e.BlueprintName=="VillageDoor"))
                    {var c=z.GetEntityCell(door);Assert.False(room.Any(p=>System.Math.Abs(c.X-p.Item1)+System.Math.Abs(c.Y-p.Item2)==1),"This negative seed must actually lack a door into its archive room.");}
                }
                Assert.AreEqual(1, z.GetAllEntities().Count(e => e.ID == StillleafSaltVault.IndexerId));
                var file = StillleafSaltVault.FindCabinet(z); Assert.NotNull(file);
                Assert.True(file.GetPart<ContainerPart>().Locked);
                Assert.AreEqual(1, file.GetPart<ContainerPart>().Contents.Count);
                Assert.AreEqual(StillleafSaltVault.KeyId, file.GetPart<ContainerPart>().Contents[0].ID);
                foreach (var e in paving) { var c = z.GetEntityCell(e); Assert.False(z.GenReservedCells.Contains((c.X,c.Y))); Assert.AreEqual(1,c.Occupants.Count); }
                Assert.AreSame(z,m.GetZone(z.ZoneID));
                Assert.AreEqual(paving.Length,z.GetAllEntities().Count(e => e.BlueprintName == "StoneFloor" && !z.GetEntityCell(e).IsInterior));
            }
        }
    }
}
