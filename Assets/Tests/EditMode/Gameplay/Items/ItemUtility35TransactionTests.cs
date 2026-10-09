using System;
using System.Reflection;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ItemUtility35TransactionTests
    {
        static void ValidateAtCommit(InventoryTransaction tx, Func<bool> predicate)
        {
            var method = typeof(InventoryTransaction).GetMethod("BeforeCommit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "deferred battlefield effects need final target validation before payment commits");
            method.Invoke(tx, new object[] { predicate });
        }
        [TestCase(true)] [TestCase(false)]
        public void FinalValidationAllowsOrRollsBackStagedPayment(bool valid)
        {
            var tx = new InventoryTransaction(); int supply = 1, effect = 0;
            tx.Do(() => supply--, () => supply++);
            ValidateAtCommit(tx, () => valid);
            typeof(InventoryTransaction).GetMethod("AfterCommit", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(tx, new object[] { (Action)(() => effect++) });
            if (valid) tx.Commit();
            else { Assert.Throws<InvalidOperationException>(() => tx.Commit()); tx.Rollback(); }
            Assert.AreEqual(valid ? 0 : 1, supply);
            Assert.AreEqual(valid ? 1 : 0, effect);
            Assert.AreEqual(valid, tx.IsCommitted);
            Assert.AreEqual(!valid, tx.IsRolledBack);
        }
        [Test] public void ExceptionInFinalValidationLeavesUndoAvailable()
        {
            var tx = new InventoryTransaction(); int supply = 1;
            tx.Do(() => supply--, () => supply++);
            ValidateAtCommit(tx, () => throw new InvalidOperationException("changed target"));
            Assert.Throws<InvalidOperationException>(() => tx.Commit()); tx.Rollback();
            Assert.AreEqual(1, supply); Assert.True(tx.IsRolledBack);
        }
        [Test] public void PredicateSeesStateAtCommitRatherThanRegistration()
        {
            var tx = new InventoryTransaction(); bool valid = true; int calls = 0;
            ValidateAtCommit(tx, () => { calls++; return valid; });
            Assert.AreEqual(0, calls); valid = false;
            Assert.Throws<InvalidOperationException>(() => tx.Commit()); tx.Rollback();
            Assert.AreEqual(1, calls);
        }
        [Test] public void RollbackDiscardsFinalValidation()
        {
            var tx = new InventoryTransaction(); int calls = 0;
            ValidateAtCommit(tx, () => { calls++; return true; });
            tx.Rollback(); tx.Commit(); Assert.AreEqual(0, calls);
        }
    }
}
