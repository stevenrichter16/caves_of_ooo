using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class PhoneIntentGateTests
    {
        [Test]
        public void DirectionSelectionRequiresConfirmationAndCannotSurviveContextChange()
        {
            var direction = new PhoneDirectionSelection();
            direction.SetContext(10);
            Assert.That(direction.Resolve(new PhoneIntent { kind = "step", x = -1, y = 1 }, false, out _, out _), Is.False);
            Assert.That(direction.Resolve(new PhoneIntent { kind = "key", value = "Return" }, false, out int x, out int y), Is.True);
            Assert.That((x, y), Is.EqualTo((-1, 1)));
            direction.SetContext(11);
            Assert.That(direction.Resolve(new PhoneIntent { kind = "key", value = "Return" }, false, out _, out _), Is.False);
            Assert.That(direction.Resolve(new PhoneIntent { kind = "step", x = 1 }, true, out x, out y), Is.True);
            Assert.That((x, y), Is.EqualTo((1, 0)), "Cursor movement remains free; it never confirms the target.");
        }
        [Test]
        public void OneTapIsConsumedOnce_AndBacklogCannotAccumulate()
        {
            var gate = new PhoneIntentGate(); gate.SetContext(1);
            Assert.That(gate.Offer(new PhoneIntent { revision = gate.Revision, kind = "step", x = 1 }), Is.True);
            Assert.That(gate.Offer(new PhoneIntent { revision = gate.Revision, kind = "wait" }), Is.False);
            Assert.That(gate.Take().x, Is.EqualTo(1));
            Assert.That(gate.Take(), Is.Null);
        }
        [Test]
        public void ContextChangeAndSuspensionRejectOldTouches()
        {
            var gate = new PhoneIntentGate(); gate.SetContext(1);
            int old = gate.Revision;
            gate.Offer(new PhoneIntent { revision = old, kind = "wait" });
            gate.SetContext(2);
            Assert.That(gate.Take(), Is.Null);
            Assert.That(gate.Offer(new PhoneIntent { revision = old, kind = "wait" }), Is.False);
            gate.Suspend();
            Assert.That(gate.Offer(new PhoneIntent { revision = gate.Revision, kind = "wait" }), Is.False);
            gate.Resume();
            Assert.That(gate.Offer(new PhoneIntent { revision = gate.Revision, kind = "wait" }), Is.True);
        }
        [Test]
        public void InvalidKindsAndDirectionsAreRejectedBeforeGameplay()
        {
            var gate = new PhoneIntentGate();
            Assert.That(gate.Offer(new PhoneIntent { revision = gate.Revision, kind = "step", x = 999 }), Is.False);
            Assert.That(gate.Offer(new PhoneIntent { revision = gate.Revision, kind = "execute-code" }), Is.False);
            Assert.That(gate.Offer(new PhoneIntent { revision = gate.Revision, kind = "text", value = new string('x', 1000) }), Is.False);
        }
    }
}
