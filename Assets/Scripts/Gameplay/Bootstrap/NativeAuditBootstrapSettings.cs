using System;
using System.IO;

namespace CavesOfOoo.Core
{
    /// <summary>Optional seed for an editor native audit's ordinary new-game bootstrap.
    /// An existing, GUID-named private audit root is required. Normal games and
    /// player builds retain seed 0, which keeps ZoneManager's usual seed selection.
    /// The audit launcher owns restoring RequestedSeed across Play/domain reloads.</summary>
    public static class NativeAuditBootstrapSettings
    {
#if UNITY_EDITOR
        public static int RequestedSeed { get; set; }
#endif
        public static int ResolveSeed()
        {
#if UNITY_EDITOR
            string root = SaveGameService.SaveRootOverride;
            if (RequestedSeed == 0 || string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root)) return 0;
            try
            {
                string full = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!Guid.TryParseExact(Path.GetFileName(full), "N", out _)) return 0;
                string parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "coo-native-save-audits"))
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(Path.GetDirectoryName(full), parent, StringComparison.Ordinal) && Directory.Exists(full)
                    ? RequestedSeed : 0;
            }
            catch (ArgumentException) { return 0; }
            catch (NotSupportedException) { return 0; }
            catch (IOException) { return 0; }
#else
            return 0;
#endif
        }
    }
}
