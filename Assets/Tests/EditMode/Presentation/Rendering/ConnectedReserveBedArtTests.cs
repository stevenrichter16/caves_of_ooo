using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class ConnectedReserveBedArtTests : ConnectedReserveTestBase
    {
        static string Resolve(Zone zone, Entity terrain)
        {
            var method = typeof(ConnectedSpread3DLibrary).GetMethod("ResolveReserveBed", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Bound reserve soil needs persistent physical cord after harvest.");
            return (string)method.Invoke(null, new object[] { zone, terrain });
        }
        [Test] public void ExactBoundBedsKeepCordAfterCropRemovalButPublicSoilDoesNotBorrowIt()
        {
            SoilA.Properties["ConnectedSpread.ReserveBed"] = Keeper.ID; SoilB.Properties["ConnectedSpread.ReserveBed"] = Keeper.ID;
            var crop = Crop(); Assert.AreEqual("connected-spread-reserve-bed", Resolve(Reserve, SoilA));
            Reserve.RemoveEntity(crop); Assert.AreEqual("connected-spread-reserve-bed", Resolve(Reserve, SoilA));
            Assert.AreEqual("connected-spread-reserve-bed", Resolve(Reserve, SoilB));
            var publicSoil = Soil(Reserve, 7, 6); publicSoil.Properties["ConnectedSpread.ReserveBed"] = Keeper.ID;
            Assert.Null(Resolve(Reserve, publicSoil));
        }
        [TestCase("unmarked")][TestCase("wrong-keeper")][TestCase("moved")][TestCase("removed-marker-part")][TestCase("detached-keeper")]
        public void BrokenOrForgedBoundSoilCannotBorrowReserveCord(string fault)
        {
            SoilA.Properties["ConnectedSpread.ReserveBed"] = Keeper.ID;
            Assert.AreEqual("connected-spread-reserve-bed", Resolve(Reserve, SoilA));
            if (fault == "unmarked") SoilA.Properties["ConnectedSpread.ReserveBed"] = "";
            if (fault == "wrong-keeper") SoilA.Properties["ConnectedSpread.ReserveBed"] = Player.ID;
            if (fault == "moved") Reserve.MoveEntity(SoilA, 8, 6);
            if (fault == "removed-marker-part") SoilA.RemovePart(SoilA.GetPart<CultivatedSoilPart>());
            if (fault == "detached-keeper") Reserve.RemoveEntity(Keeper);
            Assert.Null(Resolve(Reserve, SoilA));
        }
    }
}
