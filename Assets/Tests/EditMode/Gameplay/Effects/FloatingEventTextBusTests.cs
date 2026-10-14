using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FloatingEventTextBusTests
    {
        Zone zone; Entity actor;
        [SetUp] public void Setup()
        {
            AsciiFxBus.Clear(); zone = new Zone("EventText"); actor = new Entity();
            actor.AddPart(new RenderPart()); zone.AddEntity(actor, 10, 10);
            zone.GetCell(10, 10).IsVisible = zone.GetCell(10, 10).Explored = true;
        }
        [TearDown] public void Cleanup() => AsciiFxBus.Clear();
        [Test] public void VisibleReadoutIsOneNonBlockingGroupWithCapturedOrigin()
        {
            AsciiFxBus.EmitEventText(zone, actor, "Parried", "&C");
            Assert.False(AsciiFxBus.HasPendingBlocking);
            var requests = AsciiFxBus.Drain(); Assert.AreEqual(1, requests.Count);
            var r = requests[0]; Assert.AreEqual(AsciiFxRequestType.EventText, r.Type);
            Assert.AreEqual("Parried", r.Text); Assert.AreSame(actor, r.Anchor);
            Assert.AreEqual(10,r.X); Assert.AreEqual(10,r.Y); Assert.AreEqual("&C",r.ColorString);
            Assert.Greater(r.Lifetime, 0.5f); Assert.False(r.BlocksTurnAdvance);
            AsciiFxBus.Release(r);
        }
        [TestCase("fog")] [TestCase("unexplored")] [TestCase("hidden")]
        [TestCase("removed")] [TestCase("wrong-zone")] [TestCase("no-render")]
        public void HiddenOrNonCurrentOriginCannotLeakText(string reason)
        {
            if(reason=="fog") zone.GetCell(10,10).IsVisible=false;
            if(reason=="unexplored") zone.GetCell(10,10).Explored=false;
            if(reason=="hidden") actor.GetPart<RenderPart>().Visible=false;
            if(reason=="no-render") actor.RemovePart(actor.GetPart<RenderPart>());
            if(reason=="removed") zone.RemoveEntity(actor);
            AsciiFxBus.EmitEventText(reason=="wrong-zone"?new Zone("Other"):zone,actor,"Parried","&C");
            Assert.Zero(AsciiFxBus.PendingCount);
        }
        [Test] public void DuplicatePendingLabelIsQuietButDistinctOutcomesRemain()
        {
            AsciiFxBus.EmitEventText(zone,actor,"Poisoned","&R");
            AsciiFxBus.EmitEventText(zone,actor,"Poisoned","&R");
            AsciiFxBus.EmitEventText(zone,actor,"Stunned","&R");
            var r=AsciiFxBus.Drain(); Assert.AreEqual(2,r.Count);
            CollectionAssert.AreEqual(new[]{"Poisoned","Stunned"},r.Select(x=>x.Text));
            foreach(var x in r)AsciiFxBus.Release(x);
        }
        [Test] public void EventBurstIsBoundedWithoutBlockingAndPoolDoesNotLeakText()
        {
            for(int i=0;i<200;i++)AsciiFxBus.EmitEventText(zone,actor,"Event "+i,"&C");
            Assert.LessOrEqual(AsciiFxBus.PendingCount,64);Assert.Greater(AsciiFxBus.PendingCount,0);
            AsciiFxBus.Clear(); AsciiFxBus.EmitBurst(zone,10,10,AsciiFxTheme.Fire,false);
            var r=AsciiFxBus.Drain();Assert.AreEqual(1,r.Count);Assert.IsNull(r[0].Text);
            Assert.IsNull(r[0].Anchor);AsciiFxBus.Release(r[0]);
        }
        [TestCase(null)] [TestCase("")] [TestCase("   ")] [TestCase("bad\ntext")]
        public void EmptyOrControlTextDoesNotCreateReadout(string text)
        {
            AsciiFxBus.EmitEventText(zone,actor,text,"&C");Assert.Zero(AsciiFxBus.PendingCount);
        }
        [Test] public void LongTextHasBoundedWidth()
        {
            AsciiFxBus.EmitEventText(zone,actor,new string('x',100),"&C");
            var r=AsciiFxBus.Drain();Assert.AreEqual(1,r.Count);Assert.AreEqual(24,r[0].Text.Length);
            AsciiFxBus.Release(r[0]);
        }
    }
}
