using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedStartingBuildAuditTests
    {
        [TestCase(false, true, true)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public void OnlyExplicitOwnedAuditOptInAllowsRealStartingBuildChoice(bool optIn, bool ownedRoot, bool suppressed)
        {
            var type = typeof(NativeAuditBootstrapSettings);
            var property = type.GetProperty("AllowStartingBuildChoice", BindingFlags.Static | BindingFlags.Public);
            var query = type.GetMethod("SuppressBuildChoiceForAudit", BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(property, "A new audit must opt into the real build picker without changing legacy audit starts.");
            Assert.NotNull(query, "Bootstrap and this fixture must share the current-root admission query.");
            string oldRoot = SaveGameService.SaveRootOverride;
            int oldSeed = NativeAuditBootstrapSettings.RequestedSeed;
            bool oldValue = (bool)property.GetValue(null);
            string root = ownedRoot
                ? Path.Combine(Path.GetTempPath(), "coo-native-save-audits", Guid.NewGuid().ToString("N"))
                : Path.Combine(Path.GetTempPath(), "coo-unowned-build-audit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                SaveGameService.SaveRootOverride = root; NativeAuditBootstrapSettings.RequestedSeed = 64;
                property.SetValue(null, optIn);
                Assert.AreEqual(suppressed, query.Invoke(null, null));
                NativeAuditBootstrapSettings.RequestedSeed = 0;
                Assert.True((bool)query.Invoke(null, null), "A bare flag without the seed/root audit contract cannot alter bootstrap.");
                SaveGameService.SaveRootOverride = null;
                Assert.False((bool)query.Invoke(null, null), "Ordinary starts retain the normal build picker policy.");
            }
            finally
            {
                SaveGameService.SaveRootOverride = oldRoot; NativeAuditBootstrapSettings.RequestedSeed = oldSeed;
                property.SetValue(null, oldValue); Directory.Delete(root);
            }
        }
    }
}
