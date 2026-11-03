using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class BrowserSaveQueueTests
    {
        [Test]
        public void CommitIsPendingUntilItsCallbackAndPump_AndNeverBlocks()
        {
            Action<Exception> finish = null;
            var published = new List<int>();
            var queue = new BrowserSaveQueue<int>((value, done) => finish = done);
            queue.Enqueue(1);
            Assert.That(queue.HasPending, Is.True);
            queue.Pump((value, error) => published.Add(value));
            Assert.That(finish, Is.Not.Null);
            Assert.That(published, Is.Empty);
            finish(null);
            Assert.That(published, Is.Empty, "Browser callback must not re-enter gameplay.");
            queue.Pump((value, error) => published.Add(value));
            Assert.That(published, Is.EqualTo(new[] { 1 }));
            Assert.That(queue.HasPending, Is.False);
        }

        [Test]
        public void ActiveCommitAndNewestPendingSurvive_WithoutOverlappingWrites()
        {
            var writes = new List<int>();
            var completions = new List<Action<Exception>>();
            var published = new List<int>();
            var queue = new BrowserSaveQueue<int>((value, done) => { writes.Add(value); completions.Add(done); });
            Action<int, Exception> publish = (value, error) => published.Add(value);
            queue.Enqueue(1); queue.Pump(publish);
            queue.Enqueue(2); queue.Enqueue(3);
            queue.Pump(publish);
            Assert.That(writes, Is.EqualTo(new[] { 1 }));
            completions[0](null);
            queue.Pump(publish); queue.Pump(publish);
            Assert.That(writes, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(published, Is.EqualTo(new[] { 1 }));
            completions[1](null); queue.Pump(publish);
            Assert.That(published, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void DuplicateAndStaleCallbacksCannotCompleteANewerSave()
        {
            var callbacks = new List<Action<Exception>>();
            var published = new List<int>();
            var queue = new BrowserSaveQueue<int>((value, done) => callbacks.Add(done));
            Action<int, Exception> publish = (value, error) => published.Add(value);
            queue.Enqueue(1); queue.Pump(publish);
            callbacks[0](null); callbacks[0](new Exception("duplicate")); queue.Pump(publish);
            queue.Enqueue(2); queue.Pump(publish);
            callbacks[0](null); queue.Pump(publish);
            Assert.That(published, Is.EqualTo(new[] { 1 }));
            Assert.That(queue.HasPending, Is.True);
            callbacks[1](null); queue.Pump(publish);
            Assert.That(published, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void FailureIsPublishedOnce_AndNextSaveCanSucceed()
        {
            var failure = new InvalidOperationException("quota");
            var errors = new List<Exception>();
            var queue = new BrowserSaveQueue<int>((value, done) => { if (value == 1) throw failure; done(null); });
            Action<int, Exception> publish = (value, error) => errors.Add(error);
            queue.Enqueue(1); queue.Pump(publish); queue.Pump(publish);
            queue.Enqueue(2); queue.Pump(publish); queue.Pump(publish);
            Assert.That(errors, Is.EqualTo(new Exception[] { failure, null }));
            Assert.That(queue.HasPending, Is.False);
        }
    }
}
