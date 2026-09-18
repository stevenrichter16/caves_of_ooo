using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Counterchecks for the transaction receipt seam introduced by the
    /// RED regional post-action failure: only committed results may reach observers.</summary>
    public sealed class RegionalReceiptTransactionTests
    {
        [SetUp] public void SetUp() { Diag.ResetAll(); Diag.SetChannel("event",true); }
        [TearDown] public void TearDown() { Diag.ResetAll(); }

        [TestCase("commit")][TestCase("rollback")][TestCase("pending")]
        public void ReceiptMatchesFinalOutcomeAndNeverRunsTwice(string outcome)
        {
            var tx=new InventoryTransaction();int calls=0;
            Observe(tx,()=>{Assert.IsTrue(tx.IsCommitted);Assert.IsFalse(tx.IsRolledBack);calls++;});
            Assert.AreEqual(0,calls);
            if(outcome=="commit"){tx.Commit();tx.Commit();tx.Rollback();}
            if(outcome=="rollback"){tx.Rollback();tx.Rollback();tx.Commit();}
            Assert.AreEqual(outcome=="commit"?1:0,calls);
        }

        [TestCase(false)][TestCase(true)]
        public void ReceiptSeesPaidWalletAndOverflowPublishesNothing(bool overflow)
        {
            var actor=new Entity();actor.IntProperties[TradeSystem.CURRENCY_PROP]=overflow?int.MaxValue:10;
            var tx=new InventoryTransaction();int calls=0,observed=-1;
            Credit(tx,actor,1);Observe(tx,()=>{calls++;observed=TradeSystem.GetDrams(actor);});
            if(overflow){Assert.Throws<InvalidOperationException>(()=>tx.Commit());tx.Rollback();}
            else tx.Commit();
            Assert.AreEqual(overflow?0:1,calls);Assert.AreEqual(overflow?-1:11,observed);
            Assert.AreEqual(overflow?int.MaxValue:11,TradeSystem.GetDrams(actor));
        }

        [TestCase(false)][TestCase(true)]
        public void BrokenReceiptCannotUndoCommitOrSuppressNextReceipt(bool broken)
        {
            var tx=new InventoryTransaction();int calls=0;
            Observe(tx,()=>{if(broken)throw new InvalidOperationException("receipt test");});
            Observe(tx,()=>calls++);
            Assert.DoesNotThrow(()=>tx.Commit());Assert.IsTrue(tx.IsCommitted);Assert.AreEqual(1,calls);
            Assert.AreEqual(broken?1:0,DiagQuery.Count(new DiagQuery.Filter{Kind="InventoryCommitObserverFailed"}).Count);
        }

        [Test] public void ReentrantReceiptCannotReopenTransactionOrRepeatPayment()
        {
            var tx=new InventoryTransaction();int calls=0;
            Observe(tx,()=>{calls++;tx.Commit();tx.Rollback();
                Assert.Throws<InvalidOperationException>(()=>Observe(tx,()=>calls++));});
            tx.Commit();Assert.AreEqual(1,calls);Assert.IsTrue(tx.IsCommitted);
        }
        private static void Observe(InventoryTransaction tx,Action callback)=>Invoke(tx,"AfterCommit",callback);
        private static void Credit(InventoryTransaction tx,Entity actor,int value)=>Invoke(tx,"DeferCurrencyCredit",actor,value);
        private static void Invoke(InventoryTransaction tx,string name,params object[] args)
        {
            try { typeof(InventoryTransaction).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tx,args); }
            catch(TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw; }
        }
    }
}
