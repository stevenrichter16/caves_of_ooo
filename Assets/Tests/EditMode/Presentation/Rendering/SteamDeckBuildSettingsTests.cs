using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckBuildSettingsTests
    {
        [Test] public void LinuxPlayerUsesVerifiedVulkanInsteadOfAutomaticOpenGL()
        {
            Assert.False(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64),
                "Unity automatic Linux selection chose OpenGL and reproduced a black world with a live HUD.");
            CollectionAssert.AreEqual(new[] { GraphicsDeviceType.Vulkan },
                PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneLinux64),
                "Vulkan rendered the same standalone scene. Do not silently fall back to the failing OpenGL path.");
        }
    }
}
