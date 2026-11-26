using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        enum StorageLabelField { Mode, Color, Detail, Save, Clear, Cancel }

        static readonly StorageLabelMode[] StorageLabelModes =
            { StorageLabelMode.ColorMark, StorageLabelMode.Icon, StorageLabelMode.Text };
        static readonly string[] StorageLabelModeNames = { "Color mark", "Icon", "Text" };
        static readonly Color[] StorageLabelColors =
        {
            new Color(.93f, .93f, .82f),
            new Color(.99f, .77f, .32f),
            new Color(1f, .55f, .49f),
            new Color(.54f, .88f, .55f),
            new Color(.41f, .88f, .87f),
            new Color(.54f, .71f, 1f),
            new Color(.80f, .65f, .98f)
        };

        const int StorageIconPixels = 12;
        // Keep this order aligned with the persisted choices in StorageLabels.IconNames.
        static readonly string[][] StorageIconMasks =
        {
            new[] // Tools
            {
                "............", "............", "....#####...", "...#######..",
                "...#######..", "......##....", ".....##.....", "....##......",
                "...##.......", "..##........", "..#.........", "............",
            },
            new[] // Food
            {
                "............", ".......##...", "......###...", ".....#......",
                "...##.###...", "..########..", "..########..", "..########..",
                "...######...", "...######...", "....#..#....", "............",
            },
            new[] // Materials
            {
                "............", "....####....", "...######...", "...#....#...",
                "...######...", "............", ".####..####.", "######.#####",
                "#....#.#...#", "######.#####", ".####..####.", "............",
            },
            new[] // Potions
            {
                "............", "....####....", "....#..#....", ".....##.....",
                ".....##.....", "....####....", "...##..##...", "...#....#...",
                "...######...", "...######...", "....####....", "............",
            },
            new[] // Clothing
            {
                "............", "...##..##...", "..###..###..", ".##########.",
                ".##########.", ".##.####.##.", "....####....", "....####....",
                "....####....", "....####....", "....####....", "............",
            },
            new[] // Trade
            {
                "............", ".......#....", ".......##...", "..########..",
                "..#########.", ".......##...", "...##.......", ".#########..",
                "..########..", "...##.......", "....#.......", "............",
            },
            new[] // Important
            {
                "............", ".....##.....", ".....##.....", ".....##.....",
                ".##########.", "..########..", "...######...", "....####....",
                "...######...", "...##..##...", "..##....##..", "............",
            },
            new[] // Travel
            {
                "............", "...####.....", "...####.....", "...####.....",
                "...####.....", "...#####....", "...######...", "...#######..",
                "...########.", "...########.", "...##...###.", "............",
            },
        };
        static Texture2D _storageLabelIconAtlas;
        static Sprite[] _storageLabelIcons;

        StorageLabelEdit _storageLabelEdit;
        StorageLabelField _storageLabelFocus;
        StorageLabelMode _storageLabelMode;
        int _storageLabelColor, _storageLabelIcon;
        string _storageLabelText;

        bool StorageLabelEditorOpen => _storageLabelEdit != null;

        void OpenStorageLabelEditor(Entity item)
        {
            var edit = StorageLabels.Begin(PlayerEntity, item);
            if (edit == null)
            {
                SetActionFailure("That item is no longer available to label.");
                Render();
                return;
            }

            _storageLabelEdit = edit;
            _itemActionPopup = null;
            _storageLabelFocus = StorageLabelField.Mode;
            var original = edit.Original;
            _storageLabelMode = original.Mode == StorageLabelMode.None ? StorageLabelMode.ColorMark : original.Mode;
            _storageLabelColor = original.ColorIndex;
            _storageLabelIcon = original.IconIndex;
            _storageLabelText = original.Text ?? "";
            Render();
        }

        bool SelectStorageLabelField(StorageLabelField field)
        {
            if (!StorageLabelEditorOpen || _storageLabelFocus == field) return false;
            _storageLabelFocus = field;
            return true;
        }

        void MoveStorageLabelFocus(int delta)
        {
            int next = Mathf.Clamp((int)_storageLabelFocus + delta, 0, (int)StorageLabelField.Cancel);
            if (next == (int)StorageLabelField.Detail && _storageLabelMode == StorageLabelMode.ColorMark)
                next += delta;
            _storageLabelFocus = (StorageLabelField)next;
        }

        static int StepLabelChoice(int index, int delta, int count) => (index + delta + count) % count;

        void ChangeStorageLabelField(int delta)
        {
            switch (_storageLabelFocus)
            {
                case StorageLabelField.Mode:
                    _storageLabelMode = StorageLabelModes[StepLabelChoice(Array.IndexOf(StorageLabelModes, _storageLabelMode), delta, StorageLabelModes.Length)];
                    break;
                case StorageLabelField.Color:
                    _storageLabelColor = StepLabelChoice(_storageLabelColor, delta, StorageLabels.ColorNames.Count);
                    break;
                case StorageLabelField.Detail when _storageLabelMode == StorageLabelMode.Icon:
                    _storageLabelIcon = StepLabelChoice(_storageLabelIcon, delta, StorageLabels.IconNames.Count);
                    break;
            }
            Render();
        }

        void ActivateStorageLabelField()
        {
            switch (_storageLabelFocus)
            {
                case StorageLabelField.Save: FinishStorageLabelEdit(_storageLabelMode); return;
                case StorageLabelField.Clear: FinishStorageLabelEdit(StorageLabelMode.None); return;
                case StorageLabelField.Cancel: CancelLedgerPopup(); return;
                case StorageLabelField.Detail when _storageLabelMode == StorageLabelMode.Text:
                    Render();
                    return;
                default: ChangeStorageLabelField(1); return;
            }
        }

        void FinishStorageLabelEdit(StorageLabelMode mode)
        {
            if (!StorageLabelEditorOpen) return;
            if (mode == StorageLabelMode.Text && string.IsNullOrWhiteSpace(_storageLabelText))
            {
                _storageLabelFocus = StorageLabelField.Detail;
                SetActionFailure("Type a label or choose Clear label.");
                Render();
                return;
            }

            bool saved = StorageLabels.CanEdit(PlayerEntity, _storageLabelEdit.Owner)
                && _storageLabelEdit.Commit(mode, _storageLabelColor, _storageLabelIcon, _storageLabelText);
            _storageLabelEdit = null;
            if (saved) SetActionSuccess(mode == StorageLabelMode.None ? "Item label cleared." : "Item label saved.");
            else SetActionFailure("That item or its label changed while you were editing.");
            Rebuild();
            Render();
        }

        void HandleStorageLabelInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape)) { CancelLedgerPopup(); return; }
            if (InputHelper.GetKeyDown(KeyCode.UpArrow)) { MoveStorageLabelFocus(-1); Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.DownArrow)) { MoveStorageLabelFocus(1); Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.LeftArrow)) { ChangeStorageLabelField(-1); return; }
            if (InputHelper.GetKeyDown(KeyCode.RightArrow)) { ChangeStorageLabelField(1); return; }
            if (InputHelper.GetKeyDown(KeyCode.Return))
            {
                if (_storageLabelFocus == StorageLabelField.Detail && _storageLabelMode == StorageLabelMode.Text)
                {
                    _storageLabelFocus = StorageLabelField.Save;
                    Render();
                }
                else ActivateStorageLabelField();
                return;
            }
            if (_storageLabelMode != StorageLabelMode.Text || _storageLabelFocus != StorageLabelField.Detail) return;

            if (InputHelper.GetKeyboardKeyDown(KeyCode.Backspace) && _storageLabelText.Length > 0)
            {
                _storageLabelText = _storageLabelText.Substring(0, _storageLabelText.Length - 1);
                ClearActionStatus();
                Render();
                return;
            }

            string input = Input.inputString;
            AppendStorageLabelText(string.IsNullOrEmpty(input) ? StorageLabelKeyboardText() : input);
        }

        static string StorageLabelKeyboardText()
        {
            // A key-state event can reach the native menu without a text event,
            // including keyboards supplied by the Input System.
            bool shift = InputHelper.GetKeyboardKey(KeyCode.LeftShift) || InputHelper.GetKeyboardKey(KeyCode.RightShift);
            for (int i = 0; i < 26; i++)
                if (InputHelper.GetKeyboardKeyDown(KeyCode.A + i)) return ((char)((shift ? 'A' : 'a') + i)).ToString();
            for (int i = 0; i < 10; i++)
                if (InputHelper.GetKeyboardKeyDown(KeyCode.Alpha0 + i)) return (shift ? ")!@#$%^&*("[i] : (char)('0' + i)).ToString();
            if (InputHelper.GetKeyboardKeyDown(KeyCode.Space)) return " ";
            if (InputHelper.GetKeyboardKeyDown(KeyCode.Minus)) return shift ? "_" : "-";
            if (InputHelper.GetKeyboardKeyDown(KeyCode.Quote)) return shift ? "\"" : "'";
            return "";
        }

        void AppendStorageLabelText(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            int length = _storageLabelText.Length;
            foreach (char character in input)
            {
                if (_storageLabelText.Length >= StorageLabels.MaxTextLength) break;
                if (character >= ' ' && character <= '~' && character != '&')
                    _storageLabelText += character;
            }
            if (_storageLabelText.Length == length) return;
            ClearActionStatus();
            Render();
        }

        void RenderStorageLabelEditor()
        {
            LLabel(new Rect(274, 164, 732, 30), "LABEL ITEM", LGold, 24);
            LLabel(new Rect(274, 198, 732, 27), SkillScreenText.Fit(_storageLabelEdit.Owner.GetDisplayName(), 66, 1), LInk, 22);
            StorageLabelChoice(StorageLabelField.Mode, 242, "Mode", StorageLabelModeNames[Array.IndexOf(StorageLabelModes, _storageLabelMode)]);
            StorageLabelChoice(StorageLabelField.Color, 301, "Color", StorageLabels.ColorNames[_storageLabelColor], StorageLabelColors[_storageLabelColor]);

            string detail = _storageLabelMode == StorageLabelMode.Icon ? StorageLabels.IconNames[_storageLabelIcon]
                : _storageLabelText + (_storageLabelFocus == StorageLabelField.Detail ? "_" : "");
            if (_storageLabelMode != StorageLabelMode.ColorMark)
                StorageLabelChoice(StorageLabelField.Detail, 360, _storageLabelMode == StorageLabelMode.Icon ? "Icon" : "Text",
                    detail, canCycle: _storageLabelMode == StorageLabelMode.Icon);

            LLabel(new Rect(274, 439, 114, 24), "Preview", LMuted, 20);
            var previewBounds = new Rect(393, 435, 613, 30);
            var tint = StorageLabelColors[_storageLabelColor];
            switch (_storageLabelMode)
            {
                case StorageLabelMode.ColorMark: DrawStorageLabelSwatch(previewBounds, tint); break;
                case StorageLabelMode.Icon: DrawStorageLabelIcon(previewBounds, 24, _storageLabelIcon, tint); break;
                case StorageLabelMode.Text: LLabel(previewBounds, _storageLabelText.Trim(), tint, 24); break;
            }

            StorageLabelButton(StorageLabelField.Save, new Rect(274, 559, 226, 44), "Save label");
            StorageLabelButton(StorageLabelField.Clear, new Rect(527, 559, 226, 44), "Clear label");
            StorageLabelButton(StorageLabelField.Cancel, new Rect(780, 559, 226, 44), "Cancel");
            string controls = NativeGamepadInput.IsConnected
                ? "D-pad: browse/change   A: select   B: cancel"
                : "Arrows: browse/change   Enter: select   Esc: cancel";
            LLabel(new Rect(274, 620, 732, 24), controls, LMuted, 18);
            if (_storageLabelMode == StorageLabelMode.Text)
                LLabel(new Rect(274, 647, 732, 24), "Type on the Text row. Up to " + StorageLabels.MaxTextLength + " characters.", LMuted, 18);
        }

        void StorageLabelChoice(StorageLabelField field, float y, string name, string value, Color? valueTint = null, bool canCycle = true)
        {
            bool selected = _storageLabelFocus == field;
            var row = new Rect(274, y, 732, 48);
            LFrame(row, selected ? LGold : LBorder, selected ? LSelected : LPanel);
            LLabel(new Rect(288, y + 12, 92, 24), name, selected ? LGold : LMuted);
            LLabel(new Rect(402, y + 12, 490, 24), value, valueTint ?? LInk);
            LHit(row, LedgerHitKind.StorageLabelField, (int)field);
            if (!canCycle) return;
            StorageLabelArrow(field, new Rect(902, y + 4, 40, 40), "<", LedgerHitKind.StorageLabelDecrease);
            StorageLabelArrow(field, new Rect(958, y + 4, 40, 40), ">", LedgerHitKind.StorageLabelIncrease);
        }

        void StorageLabelArrow(StorageLabelField field, Rect bounds, string text, LedgerHitKind kind)
        {
            LFrame(bounds, LBorder, LBackground);
            LLabel(new Rect(bounds.x + 15, bounds.y + 9, 20, 24), text, LGold);
            LHit(bounds, kind, (int)field);
        }

        void StorageLabelButton(StorageLabelField field, Rect bounds, string text)
        {
            bool selected = _storageLabelFocus == field;
            LFrame(bounds, selected ? LGold : LBorder, selected ? LSelected : LPanel);
            LLabel(new Rect(bounds.x + 12, bounds.y + 12, bounds.width - 24, 22), text, selected ? LGold : LInk);
            LHit(bounds, LedgerHitKind.StorageLabelField, (int)field);
        }

        void DrawStorageLabelSwatch(Rect bounds, Color tint)
        {
            const float size = 11;
            LBox(new Rect(bounds.x, bounds.y + (bounds.height - size) / 2, size, size), tint);
        }

        void DrawStorageLabelIcon(Rect bounds, float size, int index, Color tint)
        {
            if (_storageLabelIcons == null || _storageLabelIconAtlas == null) BuildStorageLabelIcons();
            var iconBounds = new Rect(bounds.x, bounds.y + (bounds.height - size) / 2, size, size);
            LBox(iconBounds, tint, _storageLabelIcons[index]);
        }

        static void BuildStorageLabelIcons()
        {
            ReleaseStorageLabelIcons();
            int size = StorageIconPixels;
            int width = StorageIconMasks.Length * size;
            var pixels = new Color32[width * size];
            var ink = new Color32(255, 255, 255, 255);
            for (int index = 0; index < StorageIconMasks.Length; index++)
            {
                var mask = StorageIconMasks[index];
                for (int row = 0; row < size; row++)
                    for (int column = 0; column < size; column++)
                        if (mask[row][column] == '#')
                            pixels[(size - row - 1) * width + index * size + column] = ink;
            }

            _storageLabelIconAtlas = new Texture2D(width, size, TextureFormat.RGBA32, false)
            {
                name = "Storage Label Icons",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            _storageLabelIconAtlas.SetPixels32(pixels);
            _storageLabelIconAtlas.Apply(false, false);
            _storageLabelIcons = new Sprite[StorageIconMasks.Length];
            for (int index = 0; index < _storageLabelIcons.Length; index++)
            {
                var bounds = new Rect(index * size, 0, size, size);
                var icon = Sprite.Create(_storageLabelIconAtlas, bounds, new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect);
                icon.name = "Storage label icon - " + StorageLabels.IconNames[index];
                icon.hideFlags = HideFlags.HideAndDontSave;
                _storageLabelIcons[index] = icon;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReleaseStorageLabelIcons()
        {
            if (_storageLabelIcons != null)
                foreach (var icon in _storageLabelIcons) DestroyStorageLabelArt(icon);
            DestroyStorageLabelArt(_storageLabelIconAtlas);
            _storageLabelIcons = null;
            _storageLabelIconAtlas = null;
        }

        static void DestroyStorageLabelArt(UnityEngine.Object art)
        {
            if (art == null) return;
            if (Application.isPlaying) Destroy(art);
            else DestroyImmediate(art);
        }

        float DrawStorageLabel(Rect bounds, Entity item)
        {
            var label = StorageLabels.Read(item);
            const float glyphWidth = 9, gap = 9;
            if (label.Mode == StorageLabelMode.None || bounds.width < 60) return 0;
            var tint = StorageLabelColors[label.ColorIndex];
            if (label.Mode == StorageLabelMode.ColorMark)
            {
                DrawStorageLabelSwatch(bounds, tint);
                return 11 + gap;
            }
            if (label.Mode == StorageLabelMode.Icon)
            {
                DrawStorageLabelIcon(bounds, 18, label.IconIndex, tint);
                return 18 + gap;
            }
            int columns = Math.Max(3, (int)(bounds.width / 2 / glyphWidth));
            string text = SkillScreenText.Fit(label.Display, columns, 1);
            float textWidth = text.Length * glyphWidth;
            LLabel(new Rect(bounds.x, bounds.y, textWidth, bounds.height), text, tint, 18);
            return textWidth + gap;
        }
    }
}
