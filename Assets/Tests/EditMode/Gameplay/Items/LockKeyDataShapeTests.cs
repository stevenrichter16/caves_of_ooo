using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LK.2 data-shape pins for <see cref="LockPart"/> and
    /// <see cref="KeyPart"/>. Pure data — the bump-unlock event
    /// handler lands in LK.3.
    ///
    /// Why pin field defaults: a future commit might "helpfully" change
    /// LockPart.IsLocked default to false, breaking every blueprint that
    /// relies on the locked-by-default contract. These tests catch that
    /// the moment it lands.
    /// </summary>
    [TestFixture]
    public class LockKeyDataShapeTests
    {
        // ====================================================================
        // 1. LockPart default-constructed shape
        // ====================================================================

        [Test]
        public void LockPart_FreshInstance_DefaultsToLockedWithNoKeyId()
        {
            var lockPart = new LockPart();

            Assert.AreEqual("Lock", lockPart.Name,
                "LockPart.Name must be the constant 'Lock' for tag-based lookups.");
            Assert.IsTrue(lockPart.IsLocked,
                "LockPart must default to IsLocked=true. Furniture is locked " +
                "until proven otherwise.");
            Assert.AreEqual("", lockPart.KeyId,
                "LockPart must default to empty KeyId. Blueprints + tests must " +
                "set this explicitly when a key is required.");
        }

        // ====================================================================
        // 2. KeyPart default-constructed shape
        // ====================================================================

        [Test]
        public void KeyPart_FreshInstance_DefaultsToEmptyKeyId()
        {
            var keyPart = new KeyPart();

            Assert.AreEqual("Key", keyPart.Name,
                "KeyPart.Name must be the constant 'Key' for tag-based lookups.");
            Assert.AreEqual("", keyPart.KeyId,
                "KeyPart must default to empty KeyId. Blueprints + tests must " +
                "set this explicitly to match a lock's KeyId.");
        }

        // ====================================================================
        // 3. KeyId round-trips: setting + reading preserves the value
        // ====================================================================

        [Test]
        public void LockPart_KeyIdAssignment_RoundTrips()
        {
            var lockPart = new LockPart();
            lockPart.KeyId = "iron";

            Assert.AreEqual("iron", lockPart.KeyId);
        }

        [Test]
        public void KeyPart_KeyIdAssignment_RoundTrips()
        {
            var keyPart = new KeyPart();
            keyPart.KeyId = "brass";

            Assert.AreEqual("brass", keyPart.KeyId);
        }

        // ====================================================================
        // 4. IsLocked toggle round-trip — LK.3 will flip it on unlock
        // ====================================================================

        [Test]
        public void LockPart_IsLockedAssignment_RoundTrips()
        {
            var lockPart = new LockPart { IsLocked = true };
            lockPart.IsLocked = false;

            Assert.IsFalse(lockPart.IsLocked,
                "Setting IsLocked=false must stick; LK.3's unlock path " +
                "depends on this.");

            lockPart.IsLocked = true;
            Assert.IsTrue(lockPart.IsLocked);
        }

        // ====================================================================
        // 5. Counter-check: KeyPart and LockPart with the same KeyId are
        //    NOT the same object. Trivial but pins type-distinction so a
        //    refactor that accidentally aliased them via a shared base
        //    class would surface here.
        // ====================================================================

        [Test]
        public void KeyPartAndLockPart_AreDistinctTypes()
        {
            var lockPart = new LockPart { KeyId = "iron" };
            var keyPart = new KeyPart { KeyId = "iron" };

            Assert.IsInstanceOf<LockPart>(lockPart);
            Assert.IsInstanceOf<KeyPart>(keyPart);
            Assert.AreNotEqual("Lock", keyPart.Name);
            Assert.AreNotEqual("Key", lockPart.Name);
        }
    }
}
