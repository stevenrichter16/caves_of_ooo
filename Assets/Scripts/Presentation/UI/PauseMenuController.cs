using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pause-menu dispatch and its graphics submenu. The host suppresses gameplay
    /// input while IsOpen. Return/A confirms; arrows/Dpad navigate; Escape/B backs
    /// out one page; Tab/Start closes. Presentation choices never consume a turn.
    /// Rendering and mouse hit-testing use the existing PauseMenuUI popup camera.
    /// </summary>
    public sealed class PauseMenuController
    {
        public const int SaveIndex = 0;
        public const int LoadIndex = 1;
        public const int ControlsIndex = 2;
        public const int GraphicsIndex = 3;
        public const int AudioIndex = 4;
        public const int QuitIndex = 5;
        public const int ItemCount = 6;
        public const int MusicVolumeIndex = 0, AmbienceVolumeIndex = 1, SfxVolumeIndex = 2, AudioCreditsIndex = 3, AudioBackIndex = 4;
        public const int AudioItemCount = 5;
        public const int HandheldPresetIndex = 0;
        public const int FullPresetIndex = 1;
        public const int WorldResolutionIndex = 2;
        public const int WorldShadowsIndex = 3;
        public const int WorldDetailIndex = 4;
        public const int SpellEffectsIndex = 5;
        public const int GraphicsBackIndex = 6;
        public const int GraphicsItemCount = 7;
        private static readonly float[] ResolutionChoices = { 1f, .75f, .5f };
        private static readonly string[] MainLabels = { "Save game", "Load game", "Controls", "Graphics", "Audio", "Quit" };

        /// <summary>Invoked by the Controls entry (host wires this to
        /// the ControlsReference help dump).</summary>
        public Action ShowControls;

        /// <summary>Invoked by the Quit entry (host wires this to
        /// Application.Quit; no-ops in the editor by Unity contract).</summary>
        public Action RequestQuit;

        public KeyCode OpenCloseKey = KeyCode.Tab;
        /// <summary>ALPHA onboarding: Escape aliases open/close. This
        /// RESPECTS the project's Esc-closes-the-active-modal
        /// convention (see class docstring) and adds the industry-
        /// standard Esc-opens-the-menu gesture in normal play.</summary>
        public KeyCode AltOpenCloseKey = KeyCode.Escape;
        public KeyCode ConfirmKey = KeyCode.Return;
        public KeyCode UpKey = KeyCode.UpArrow;
        public KeyCode DownKey = KeyCode.DownArrow;

        public bool IsOpen { get; private set; }
        public int SelectedIndex { get; private set; }
        /// <summary>True only while the graphics page of the open menu is active.</summary>
        public bool IsGraphicsOpen { get; private set; }
        public bool IsAudioOpen { get; private set; }
        /// <summary>Navigation and rendering bounds for the active menu page.</summary>
        public int VisibleItemCount => IsGraphicsOpen ? GraphicsItemCount : IsAudioOpen ? AudioItemCount : ItemCount;
        /// <summary>Changes when the page, open state or a chosen preference changes.
        /// Selection changes are exposed separately through SelectedIndex.</summary>
        public int ViewRevision { get; private set; }

        public void Open()
        {
            IsOpen = true;
            IsGraphicsOpen = false;
            IsAudioOpen = false;
            SelectedIndex = SaveIndex;
            ViewRevision++;
        }

        public void Close()
        {
            IsOpen = false;
            IsGraphicsOpen = false;
            IsAudioOpen = false;
            ViewRevision++;
        }

        /// <summary>Escape/B returns from graphics to its main-menu entry;
        /// on the main menu it closes. No preference or gameplay action occurs.</summary>
        public void Back()
        {
            if (!IsOpen) return;
            if (IsAudioOpen)
            {IsAudioOpen=false;SelectedIndex=AudioIndex;ViewRevision++;return;}
            if (!IsGraphicsOpen) { Close(); return; }
            IsGraphicsOpen = false;
            SelectedIndex = GraphicsIndex;
            ViewRevision++;
        }

        /// <summary>Current row text including the effective preference value;
        /// returns an empty string for indices outside the active page.</summary>
        public string GetItemLabel(int index)
        {
            if (index < 0 || index >= VisibleItemCount) return string.Empty;
            if (IsAudioOpen)
            {
                switch(index)
                {
                    case MusicVolumeIndex:return "Music: "+Mathf.RoundToInt(WorldAudioSettings.MusicVolume*100)+"%";
                    case AmbienceVolumeIndex:return "Ambience: "+Mathf.RoundToInt(WorldAudioSettings.AmbienceVolume*100)+"%";
                    case SfxVolumeIndex:return "Sound effects: "+Mathf.RoundToInt(WorldAudioSettings.SfxVolume*100)+"%";
                    case AudioCreditsIndex:return "Audio credits";
                    default:return "Back";
                }
            }
            if (!IsGraphicsOpen) return MainLabels[index];
            switch (index)
            {
                case HandheldPresetIndex: return "Handheld preset";
                case FullPresetIndex: return "Full preset";
                case WorldResolutionIndex: return "World resolution: " + Mathf.RoundToInt(Village3DSettings.WorldResolutionScale * 100) + "%";
                case WorldShadowsIndex: return "World shadows: " + (Village3DSettings.ShadowsEnabled ? "On" : "Off");
                case WorldDetailIndex: return "World detail: " + (Village3DSettings.LowDetail ? "Low" : "Full");
                case SpellEffectsIndex: return "Spell effects: " + SpellFxSettings.Mode;
                default: return "Back";
            }
        }

        /// <summary>
        /// Test-only setter for the selection — production code uses
        /// keyboard navigation or <see cref="HoverSelect"/>. Public
        /// (rather than internal) because the test assembly doesn't
        /// have InternalsVisibleTo configured.
        /// </summary>
        public void MoveSelectionForTest(int index)
        {
            if (index >= 0 && index < VisibleItemCount)
                SelectedIndex = index;
        }

        /// <summary>
        /// Set selection without dispatching — for mouse hover. No-op
        /// when closed or when the index is out of range.
        /// </summary>
        public void HoverSelect(int index)
        {
            if (!IsOpen) return;
            if (index < 0 || index >= VisibleItemCount) return;
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
            if (index < 0 || index >= VisibleItemCount) return;
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
                if (input.GetKeyDown(OpenCloseKey) || input.GetKeyDown(AltOpenCloseKey))
                {
                    Open();
                    return true;
                }
                return false;
            }

            if (input.GetKeyDown(OpenCloseKey))
            {
                Close();
                return true;
            }
            if (input.GetKeyDown(AltOpenCloseKey))
            {
                Back();
                return true;
            }

            if (input.GetKeyDown(UpKey))
            {
                if (SelectedIndex > 0) SelectedIndex--;
                return true;
            }

            if (input.GetKeyDown(DownKey))
            {
                if (SelectedIndex < VisibleItemCount - 1) SelectedIndex++;
                return true;
            }

            if (IsGraphicsOpen && SelectedIndex >= WorldResolutionIndex && SelectedIndex <= SpellEffectsIndex)
            {
                if (input.GetKeyDown(KeyCode.LeftArrow)) { ChangeGraphics(-1); return true; }
                if (input.GetKeyDown(KeyCode.RightArrow)) { ChangeGraphics(1); return true; }
            }
            if(IsAudioOpen&&SelectedIndex<=SfxVolumeIndex)
            {
                if(input.GetKeyDown(KeyCode.LeftArrow)){ChangeAudio(-1);return true;}
                if(input.GetKeyDown(KeyCode.RightArrow)){ChangeAudio(1);return true;}
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
            if(IsAudioOpen)
            {
                if(SelectedIndex==AudioBackIndex)Back();
                else if(SelectedIndex==AudioCreditsIndex)
                {
                    var credits=Resources.Load<TextAsset>("Audio/World/CREDITS");
                    var places=Resources.Load<TextAsset>("Audio/World/PLACES-CREDITS");
                    string text=credits!=null?credits.text:"";
                    if(places!=null)text+=(text.Length>0?"\n\n":"")+places.text;
                    MessageLog.AddAnnouncement(text.Length>0?text:"Audio credits are unavailable.");
                    Close();
                }
                else ChangeAudio(1);
                return;
            }
            if (IsGraphicsOpen)
            {
                if (SelectedIndex == GraphicsBackIndex) Back();
                else ChangeGraphics(1);
                return;
            }
            switch (SelectedIndex)
            {
                case AudioIndex:
                    IsAudioOpen=true;SelectedIndex=MusicVolumeIndex;ViewRevision++;break;
                case GraphicsIndex:
                    IsGraphicsOpen = true;
                    SelectedIndex = HandheldPresetIndex;
                    ViewRevision++;
                    break;

                case ControlsIndex:
                    Close();
                    ShowControls?.Invoke();
                    break;

                case QuitIndex:
                    Close();
                    RequestQuit?.Invoke();
                    break;

                case SaveIndex:
                    bool saveOk = service != null && service.QuickSave();
                    log?.Invoke(saveOk ? "Game saved." : "Save failed — see console.");
                    Close();
                    break;

                case LoadIndex:
                    if (service == null || !service.HasQuickSave())
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

        private void ChangeAudio(int direction)
        {
            switch(SelectedIndex)
            {
                case MusicVolumeIndex:WorldAudioSettings.MusicVolume=Mathf.Round((WorldAudioSettings.MusicVolume+direction*.1f)*100)/100;break;
                case AmbienceVolumeIndex:WorldAudioSettings.AmbienceVolume=Mathf.Round((WorldAudioSettings.AmbienceVolume+direction*.1f)*100)/100;break;
                case SfxVolumeIndex:WorldAudioSettings.SfxVolume=Mathf.Round((WorldAudioSettings.SfxVolume+direction*.1f)*100)/100;break;
                default:return;
            }
            WorldAudioSettings.Save();ViewRevision++;
        }

        private void ChangeGraphics(int direction)
        {
            switch (SelectedIndex)
            {
                case HandheldPresetIndex:
                case FullPresetIndex:
                    bool handheld = SelectedIndex == HandheldPresetIndex;
                    Village3DSettings.LowDetail = handheld;
                    Village3DSettings.WorldResolutionScale = handheld ? .75f : 1f;
                    Village3DSettings.ShadowsEnabled = !handheld;
                    SpellFxSettings.Mode = handheld ? SpellFxMode.Reduced : SpellFxMode.Full;
                    break;
                case WorldResolutionIndex:
                    int current = 0;
                    for (int i = 0; i < ResolutionChoices.Length; i++)
                        if (Mathf.Approximately(Village3DSettings.WorldResolutionScale, ResolutionChoices[i]))
                            current = i;
                    Village3DSettings.WorldResolutionScale = ResolutionChoices[(current + direction + ResolutionChoices.Length) % ResolutionChoices.Length];
                    break;
                case WorldShadowsIndex:
                    Village3DSettings.ShadowsEnabled = !Village3DSettings.ShadowsEnabled;
                    break;
                case WorldDetailIndex:
                    // The menu's detail choice is independent even for legacy
                    // preferences that previously linked resolution and shadows.
                    float resolution = Village3DSettings.WorldResolutionScale;
                    bool shadows = Village3DSettings.ShadowsEnabled;
                    Village3DSettings.LowDetail = !Village3DSettings.LowDetail;
                    Village3DSettings.WorldResolutionScale = resolution;
                    Village3DSettings.ShadowsEnabled = shadows;
                    break;
                case SpellEffectsIndex:
                    int mode = Mathf.Clamp((int)SpellFxSettings.Mode, 0, 2);
                    SpellFxSettings.Mode = (SpellFxMode)((mode - direction + 3) % 3);
                    break;
                default:
                    return;
            }
            Village3DSettings.Save();
            SpellFxSettings.Save();
            ViewRevision++;
        }
    }
}
