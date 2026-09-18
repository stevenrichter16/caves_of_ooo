using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class MultiCellSlipTests
    {
        private sealed class EastRng : System.Random
        {
            public int Rolls;
            public override int Next(int maxValue) { if(maxValue == 100) Rolls++; return 0; }
        }
        [SetUp] public void SetUp()
        {
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"ice\",\"DisplayName\":\"ice\",\"Slippery\":true,\"SlipChance\":100},{\"Id\":\"water\",\"Slippery\":false,\"SlipChance\":100}]}");
            LiquidSlipSystem.TestRng = new EastRng();
        }
        [TearDown] public void TearDown()
        {
            LiquidSlipSystem.TestRng = null;
            LiquidRegistry.ResetForTests();
        }
        private static Entity Creature(Zone z, string shape = "0,0;1,0;0,1;1,1")
        {
            var body=MultiCellSpatialTests.Body(shape);body.Tags["Creature"]="";
            Assert.IsTrue(z.AddEntity(body,10,10));return body;
        }
        [TestCase("ice",true)][TestCase("water",false)]
        public void FarFootSurfaceParticipatesInOneSlipDecision(string liquid,bool slips)
        {
            var z=new Zone();var body=Creature(z);z.TileState.WriteCoating(11,11,liquid,4);
            Assert.AreEqual(slips,LiquidSlipSystem.ResolveAfterMove(body,z,z.GetCell(10,10),LiquidSlipSystem.MAX_SLIDE_CHAIN-1));
            Assert.AreEqual((slips?11:10,10),z.GetEntityPosition(body));
            Assert.AreEqual(slips?1:0,((EastRng)LiquidSlipSystem.TestRng).Rolls);
        }
        [TestCase("0,0;1,0;1,1",10,11)][TestCase("1,0;1,1",10,10)]
        public void SlipperyBodyHoleDoesNotCountAsContact(string shape,int x,int y)
        {
            var z=new Zone();var body=Creature(z,shape);z.TileState.WriteCoating(x,y,"ice",4);
            Assert.IsFalse(LiquidSlipSystem.ResolveAfterMove(body,z,z.GetCell(10,10)));
            Assert.AreEqual(0,((EastRng)LiquidSlipSystem.TestRng).Rolls);
        }
        [TestCase(false)][TestCase(true)]
        public void SlideChecksWholeDestinationAndIgnoresItsOwnDepartingBody(bool blocked)
        {
            var z=new Zone();var body=Creature(z);z.TileState.WriteCoating(10,10,"ice",4);
            if(blocked) z.AddEntity(MultiCellSpatialTests.Body(null),12,11);
            Assert.IsTrue(LiquidSlipSystem.ResolveAfterMove(body,z,z.GetCell(10,10),LiquidSlipSystem.MAX_SLIDE_CHAIN-1));
            Assert.AreEqual((blocked?10:11,10),z.GetEntityPosition(body));
            Assert.AreEqual(1,((EastRng)LiquidSlipSystem.TestRng).Rolls);
        }
        [TestCase(false, true)] [TestCase(false, false)]
        [TestCase(true, true)] [TestCase(true, false)]
        public void SlidePreservesCreatureExclusionEvenWhenItsPhysicsIsNonSolid(bool wide, bool creature)
        {
            var z = new Zone();
            var body = Creature(z, wide ? "0,0;1,0;0,1;1,1" : "0,0");
            var occupant = MultiCellSpatialTests.Body(null, false);
            if (creature) occupant.Tags["Creature"] = "";
            Assert.IsTrue(z.AddEntity(occupant, wide ? 12 : 11, wide ? 11 : 10));
            Assert.IsFalse(occupant.GetPart<PhysicsPart>().Solid);
            z.TileState.WriteCoating(10, 10, "ice", 4);
            Assert.IsTrue(LiquidSlipSystem.ResolveAfterMove(body, z, z.GetCell(10, 10), LiquidSlipSystem.MAX_SLIDE_CHAIN - 1));
            Assert.AreEqual((creature ? 10 : 11, 10), z.GetEntityPosition(body));
            Assert.AreEqual(1, ((EastRng)LiquidSlipSystem.TestRng).Rolls,
                "A blocked displacement still made one actual slip decision.");
        }

        [Test] public void FourSlipperyBodyCellsStillRollOnlyOncePerLanding()
        {
            var z=new Zone();var body=Creature(z);
            foreach(var cell in z.GetOccupiedCells(body)) z.TileState.WriteCoating(cell.X,cell.Y,"ice",4);
            Assert.IsTrue(LiquidSlipSystem.ResolveAfterMove(body,z,z.GetCell(10,10),LiquidSlipSystem.MAX_SLIDE_CHAIN-1));
            Assert.AreEqual(1,((EastRng)LiquidSlipSystem.TestRng).Rolls);
        }
    }
}
