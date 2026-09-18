using System;
using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>The paired profile seed is opt-in and confined to a live private
    /// audit root. These are RED-first seed-selection pins; the native N/F6 audit
    /// separately proves that ordinary bootstrap/save loading use the selected seed.</summary>
    public sealed class NativeAuditBootstrapSettingsTests
    {
        private string _oldRoot, _root;
        private int _oldSeed;
        [SetUp] public void SetUp()
        {
            _oldRoot = SaveGameService.SaveRootOverride;
            _oldSeed = NativeAuditBootstrapSettings.RequestedSeed;
            _root = Path.Combine(Path.GetTempPath(), "coo-native-save-audits", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            SaveGameService.SaveRootOverride = _root;
            NativeAuditBootstrapSettings.RequestedSeed = 729490642;
        }
        [TearDown] public void TearDown()
        {
            SaveGameService.SaveRootOverride = _oldRoot;
            NativeAuditBootstrapSettings.RequestedSeed = _oldSeed;
            if (_root != null && Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        [Test] public void ExistingPrivateRoot_UsesExplicitPairedSeed()
            => Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.EqualTo(729490642));
        [Test] public void ExistingPrivateRoot_ZeroRequest_KeepsOrdinarySeedSelection()
        { NativeAuditBootstrapSettings.RequestedSeed = 0; Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void NoIsolation_IgnoresRequest(string root)
        { SaveGameService.SaveRootOverride = root; Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [Test] public void NormalSaveRoot_IgnoresRequest()
        { SaveGameService.SaveRootOverride = Path.Combine(Path.GetTempPath(), "ordinary-user-saves"); Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [Test] public void PrefixSibling_IsNotAnOwnedAuditRoot()
        { SaveGameService.SaveRootOverride = Path.Combine(Path.GetTempPath(), "coo-native-save-audits-other", Path.GetFileName(_root)); Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [Test] public void PrivateParentWithoutGuid_IsNotAnOwnedAuditRoot()
        { SaveGameService.SaveRootOverride = Path.GetDirectoryName(_root); Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [Test] public void NestedSaveDirectory_IsNotAnOwnedAuditRoot()
        { SaveGameService.SaveRootOverride = Path.Combine(_root, Guid.NewGuid().ToString("N")); Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [Test] public void ValidShapeButAbsentPrivateDirectory_IgnoresRequest()
        { Directory.Delete(_root); Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [Test] public void TrailingSeparator_StillRecognizesSamePrivateRoot()
        { SaveGameService.SaveRootOverride = _root + Path.DirectorySeparatorChar; Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.EqualTo(729490642)); }
        [Test] public void RelativePathToExistingPrivateRoot_DoesNotEnableOverride()
        { SaveGameService.SaveRootOverride = Path.GetRelativePath(Environment.CurrentDirectory, _root); Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
        [Test] public void ExistingHyphenatedGuidDirectory_IsNotTheNativeIsolationFormat()
        {
            string other = Path.Combine(Path.GetDirectoryName(_root), Guid.NewGuid().ToString("D"));
            Directory.CreateDirectory(other);
            try { SaveGameService.SaveRootOverride = other; Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.Zero); }
            finally { Directory.Delete(other); }
        }
        [Test] public void NonzeroNegativeSeed_IsRetainedWithoutRewritingSettings()
        {
            NativeAuditBootstrapSettings.RequestedSeed = -23;
            Assert.That(NativeAuditBootstrapSettings.ResolveSeed(), Is.EqualTo(-23));
            Assert.That(NativeAuditBootstrapSettings.RequestedSeed, Is.EqualTo(-23));
            Assert.That(SaveGameService.SaveRootOverride, Is.EqualTo(_root));
        }
    }
}
