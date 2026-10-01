using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Keyboard dispatch for the new-game "pick a build" modal. The state is a
    /// <see cref="StartingBuildMenuModel"/> (pure, unit-tested in Gameplay); this
    /// maps keys onto it and fires the chosen build exactly once.
    ///
    /// <para><b>No cancel.</b> The picker opens before the character exists in
    /// any usable sense, so there is nothing to go back to: Escape is ignored
    /// and Enter chooses. (Classic is always on the list for anyone who just
    /// wants the old start.)</para>
    /// </summary>
    public sealed class StartingBuildMenuController
    {
        public KeyCode ConfirmKey = KeyCode.Return;
        public KeyCode AltConfirmKey = KeyCode.KeypadEnter;
        public KeyCode SpaceConfirmKey = KeyCode.Space;
        public KeyCode UpKey = KeyCode.UpArrow;
        public KeyCode AltUpKey = KeyCode.W;
        public KeyCode DownKey = KeyCode.DownArrow;
        public KeyCode AltDownKey = KeyCode.S;

        private static readonly KeyCode[] DigitKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        };

        private Action<StartingBuildDef> _onChosen;

        public bool IsOpen { get; private set; }
        public StartingBuildMenuModel Model { get; private set; }
        public int SelectedIndex => Model?.SelectedIndex ?? 0;

        /// <summary>Open the modal. Does nothing (and returns false) for an empty list.</summary>
        public bool Open(IReadOnlyList<StartingBuildDef> options, Action<StartingBuildDef> onChosen)
        {
            if (options == null || options.Count == 0) return false;
            Model = new StartingBuildMenuModel(options);
            _onChosen = onChosen;
            IsOpen = true;
            return true;
        }

        /// <summary>Per-frame keys. Returns true when a key was consumed.</summary>
        public bool Tick(IInputProbe input)
        {
            if (!IsOpen || Model == null || input == null) return false;

            if (input.GetKeyDown(UpKey) || input.GetKeyDown(AltUpKey)) { Model.Move(-1); return true; }
            if (input.GetKeyDown(DownKey) || input.GetKeyDown(AltDownKey)) { Model.Move(+1); return true; }

            for (int i = 0; i < DigitKeys.Length && i < Model.Options.Count; i++)
                if (input.GetKeyDown(DigitKeys[i])) { Model.Select(i); return true; }

            if (input.GetKeyDown(ConfirmKey) || input.GetKeyDown(AltConfirmKey) || input.GetKeyDown(SpaceConfirmKey))
            {
                Confirm();
                return true;
            }
            return false;
        }

        public void HoverSelect(int index) => Model?.Select(index);

        public void ClickConfirm(int index)
        {
            if (!IsOpen || Model == null) return;
            Model.Select(index);
            Confirm();
        }

        private void Confirm()
        {
            var chosen = Model?.Selected;
            var callback = _onChosen;
            IsOpen = false;          // close first: the callback may start the game and must not see an open modal
            _onChosen = null;
            if (chosen != null) callback?.Invoke(chosen);
        }
    }
}
