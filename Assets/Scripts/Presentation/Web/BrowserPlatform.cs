using System;
using System.Runtime.InteropServices;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Owns browser callbacks across scene changes. The browser path
    /// cannot bootstrap a new campaign until its durable checkpoints hydrate.</summary>
    public sealed class BrowserPlatform : MonoBehaviour, IBrowserSaveStore
    {
        private const string Root = "/coo-browser-saves";
        private static BrowserPlatform _instance;
        private Action _start;
        private Action<Exception> _saveCompleted;
        private int _operation;
        public bool IsReady { get; private set; }

        [Serializable] private sealed class Reply { public int operation; public string error; public bool unavailable; }

        public static void StartWhenReady(Action start)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_instance != null)
            {
                if (_instance.IsReady) start();
                else _instance._start += start;
                return;
            }
            WebGLInput.captureAllKeyboardInput = false;
            var owner = new GameObject("CooBrowser");
            DontDestroyOnLoad(owner);
            _instance = owner.AddComponent<BrowserPlatform>();
            _instance._start = start;
            CooStorageInitialize(owner.name, Root);
#else
            start();
#endif
        }

        // Called by the JS plugin. Operation zero is hydration, never an empty-save fallback.
        public void StorageReply(string json)
        {
            var reply = JsonUtility.FromJson<Reply>(json);
            if (reply == null) return;
            if (reply.operation == 0)
            {
                if (IsReady || _start == null) return;
                if (!string.IsNullOrEmpty(reply.error)) { Debug.LogError("[Web] " + reply.error); return; }
                IsReady = true;
                SaveGameService.ConfigureBrowserStore(this, Root);
                var start = _start; _start = null;
                start();
                return;
            }
            if (reply.operation != _operation || _saveCompleted == null) return;
            if (reply.unavailable) IsReady = false;
            var completed = _saveCompleted; _saveCompleted = null;
            completed(string.IsNullOrEmpty(reply.error) ? null : new InvalidOperationException(reply.error));
        }

        public void PhoneIntent(string json) => PhoneInput.Receive(json);
        public void Overlay(string value)
        {
            var input = UnityEngine.Object.FindFirstObjectByType<InputHandler>();
            if (input != null) input.SetPhoneOverlay(value);
        }
        public void Visibility(string value)
        {
            var input = UnityEngine.Object.FindFirstObjectByType<InputHandler>();
            if (input != null) input.SetPhoneVisibility(value);
        }

        public void StorageUnavailable(string reason) { IsReady = false; Debug.LogError("[Web] " + reason); }

        public void Commit(string gameID, string slot, byte[] compressed, string metadata, Action<Exception> completed)
        {
            if (!IsReady || _saveCompleted != null) throw new InvalidOperationException("Browser storage is not ready.");
            _saveCompleted = completed;
#if UNITY_WEBGL && !UNITY_EDITOR
            CooStorageCommit(gameObject.name, ++_operation, gameID, slot, compressed, compressed.Length, metadata);
#else
            _saveCompleted = null;
            throw new PlatformNotSupportedException("Browser storage requires a Web build.");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void CooStorageInitialize(string receiver, string root);
        [DllImport("__Internal")] private static extern void CooStorageCommit(string receiver, int operation,
            string gameID, string slot, byte[] payload, int length, string metadata);
#endif
    }
}
