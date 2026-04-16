using System;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    [InitializeOnLoad]
    internal static class CodexMcpKickstart
    {
        // Temporary editor-only helper to force bridge retries while MCP transport is being stabilized.
        static CodexMcpKickstart()
        {
            EditorApplication.delayCall += () => _ = TryStartBridgeAsync();
        }

        private static async Task TryStartBridgeAsync()
        {
            const int maxAttempts = 5;

            try
            {
                if (!EditorConfigurationCache.Instance.UseHttpTransport)
                    return;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    if (MCPServiceLocator.Bridge.IsRunning)
                        return;

                    bool started = await MCPServiceLocator.Bridge.StartAsync();
                    Debug.Log($"[CodexMcpKickstart] HTTP bridge start attempted. Attempt={attempt}/{maxAttempts} Started={started}");

                    if (started)
                        return;

                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CodexMcpKickstart] Failed to start HTTP bridge: {ex.Message}");
            }
        }
    }
}
