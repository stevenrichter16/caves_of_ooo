using System;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pure dispatch logic for the pause menu — a centered modal
    /// shown when the player presses Tab during normal gameplay.
    /// Two items: <see cref="SaveIndex"/> (Save) and
    /// <see cref="LoadIndex"/> (Load). Per
    /// <c>Docs/QUD-PARITY.md §2.1</c> (TDD).
    ///
    /// <para><b>Default key choice.</b> Tab — Esc was originally
    /// considered but conflicts with the project-wide convention
    /// that Esc closes the currently-active modal (PickupUI,
    /// ContainerPickerUI, WorldActionMenuUI, etc. all use Esc to
    /// dismiss). Tab is unbound elsewhere and reads naturally as a
    /// "switch context" gesture.</para>
    ///
    /// <para><b>Lifecycle.</b> Closed on construct. Pressing
    /// <see cref="OpenCloseKey"/> (default Tab) opens; while open,
    /// arrow keys navigate, Enter (or <see cref="ClickSelect"/> for
    /// mouse) confirms, and pressing <see cref="OpenCloseKey"/>
    /// again closes without dispatch. Visual rendering lives in
    /// <c>PauseMenuUI</c> (MonoBehaviour) — this class is pure
    /// logic and 100% unit-tested.</para>
    ///
    /// <para><b>Suppression.</b> When <see cref="IsOpen"/> is true,
    /// the host (InputHandler) suppresses ALL other input. The
    /// modal is fully blocking — turns don't advance, look mode
    /// can't open, etc.</para>
    /// </summary>
    public sealed class PauseMenuController
    {
        public const int SaveIndex = 0;
        public const int LoadIndex = 1;
        public const int ItemCount = 2;

        public KeyCode OpenCloseKey = KeyCode.Tab;
        public KeyCode ConfirmKey = KeyCode.Return;
        public KeyCode UpKey = KeyCode.UpArrow;
        public KeyCode DownKey = KeyCode.DownArrow;

        public bool IsOpen { get; private set; }
        public int SelectedIndex { get; private set; }

        public void Open()
        {
            IsOpen = true;
            SelectedIndex = SaveIndex;
        }

        public void Close()
        {
            IsOpen = false;
        }

        /// <summary>
        /// Test-only setter for the selection — production code uses
        /// keyboard navigation or <see cref="HoverSelect"/>. Public
        /// (rather than internal) because the test assembly doesn't
        /// have InternalsVisibleTo configured.
        /// </summary>
        public void MoveSelectionForTest(int index)
        {
            if (index >= 0 && index < ItemCount)
                SelectedIndex = index;
        }

        /// <summary>
        /// Set selection without dispatching — for mouse hover. No-op
        /// when closed or when the index is out of range.
        /// </summary>
        public void HoverSelect(int index)
        {
            if (!IsOpen) return;
            if (index < 0 || index >= ItemCount) return;
            SelectedIndex = index;
        }

        /// <summary>
        /// Confirm a selection at <paramref name="index"/> — used by
        /// the UI layer for mouse clicks. Same dispatch path as Tick's
        /// Enter handler.
        /// </summary>
        public void ClickSelect(int index, ISaveLoadService service, Action<string> log)
        {
            if (!IsOpen) return;
            if (index < 0 || index >= ItemCount) return;
            SelectedIndex = index;
            DispatchSelection(service, log);
        }

        /// <summary>
        /// Run one polling tick. Returns true when input was consumed
        /// (host should short-circuit subsequent bindings).
        /// </summary>
        public bool Tick(IInputProbe input, ISaveLoadService service, Action<string> log)
        {
            // Closed → only the open/close key (default Tab) is meaningful.
            if (!IsOpen)
            {
                if (input.GetKeyDown(OpenCloseKey))
                {
                    Open();
                    return true;
                }
                return false;
            }

            // Open → handle close key (close), arrows (navigate), Enter (confirm).
            if (input.GetKeyDown(OpenCloseKey))
            {
                Close();
                return true;
            }

            if (input.GetKeyDown(UpKey))
            {
                if (SelectedIndex > 0) SelectedIndex--;
                return true;
            }

            if (input.GetKeyDown(DownKey))
            {
                if (SelectedIndex < ItemCount - 1) SelectedIndex++;
                return true;
            }

            if (input.GetKeyDown(ConfirmKey))
            {
                DispatchSelection(service, log);
                return true;
            }

            return false;
        }

        // ---- Dispatch ----

        private void DispatchSelection(ISaveLoadService service, Action<string> log)
        {
            switch (SelectedIndex)
            {
                case SaveIndex:
                    bool saveOk = service.QuickSave();
                    log?.Invoke(saveOk ? "Game saved." : "Save failed — see console.");
                    Close();
                    break;

                case LoadIndex:
                    if (!service.HasQuickSave())
                    {
                        log?.Invoke("No save to load.");
                        // Stay open so the player can pick Save instead.
                        return;
                    }
                    bool loadOk = service.QuickLoad();
                    log?.Invoke(loadOk ? "Game loaded." : "Load failed — save may be corrupted.");
                    Close();
                    break;
            }
        }
    }
}
