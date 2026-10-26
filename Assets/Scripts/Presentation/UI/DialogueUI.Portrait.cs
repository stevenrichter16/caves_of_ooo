using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class DialogueUI
    {
        // Explicit binding: dialogue may borrow only the currently presented graph.
        public ZoneRenderer WorldPresentation;
        private ZoneRenderer _portraitPresentation;
        private AnnouncementDocument _portraitDocument;
        private ConversationData _portraitConversation;
        private ExaminePortrait _portrait;
        private FlatPixelPortraitDrawer _flatPortraitDrawer;
        private bool _nativePortraitLayout;
        private bool NativePortraitLayout => UsePortraitPanel && _nativePortraitLayout;

        private void CapturePortrait()
        {
            ClearPortrait();
            _portraitPresentation = WorldPresentation;
            _portraitConversation = ConversationManager.CurrentConversation;
            var speaker = ConversationManager.Speaker;
            var listener = ConversationManager.Listener;
            if (speaker == null || listener == null || listener != PlayerEntity) return;
            var cell = CurrentZone?.GetEntityCell(speaker);
            if (cell == null) return;
            // Identity only: opening a conversation is not an implicit Examine and
            // must not execute description providers, inventory readers or actions.
            _portraitDocument = new AnnouncementDocument("", subject: speaker,
                subjectZone: CurrentZone, subjectCell: cell, subjectViewer: listener);
            RefreshPortrait();
            _nativePortraitLayout = _portrait.Texture != null;
        }

        private bool IsCurrentPortraitSession()
        {
            return _isOpen && UsePortraitPanel && ConversationManager.IsActive
                && _portraitDocument != null && WorldPresentation != null
                && WorldPresentation == _portraitPresentation && WorldPresentation.FlatPixelModeActive
                && ReferenceEquals(_portraitConversation, ConversationManager.CurrentConversation)
                && ReferenceEquals(_portraitDocument.Subject, ConversationManager.Speaker)
                && ReferenceEquals(_portraitDocument.SubjectViewer, ConversationManager.Listener)
                && ReferenceEquals(PlayerEntity, ConversationManager.Listener)
                && ReferenceEquals(CurrentZone, _portraitDocument.SubjectZone)
                && ExaminePortraitRenderer.IsCurrentPresentationSubject(_portraitDocument, WorldPresentation);
        }

        // Explicit open/refresh only. Never recapture a different owner or run a
        // model fallback after the flat-world resolver rejects a subject.
        private void RefreshPortrait()
        {
            _portrait = default;
            if (IsCurrentPortraitSession())
                WorldPresentation.TryGetFlatExaminePortrait(_portraitDocument, out _portrait);
        }

        private void ValidatePortrait()
        {
            if (_portrait.Texture != null && !IsCurrentPortraitSession()) _portrait = default;
        }

        private void ClearPortrait()
        {
            _portrait = default;
            _portraitDocument = null;
            _portraitConversation = null;
            _portraitPresentation = null;
            _nativePortraitLayout = false;
        }

        private Rect MeasurePortraitScreenRect()
        {
            if (Tilemap == null || PopupCamera == null || _portrait.Texture == null) return Rect.zero;
            var bottom = PopupCamera.WorldToScreenPoint(Tilemap.CellToWorld(
                new Vector3Int(_worldOriginX + 1, _worldTopY - 4, 0)));
            var top = PopupCamera.WorldToScreenPoint(Tilemap.CellToWorld(
                new Vector3Int(_worldOriginX + 9, _worldTopY, 0)));
            var slot = new Rect(bottom.x + 2, Screen.height - top.y + 2,
                top.x - bottom.x - 4, top.y - bottom.y - 4);
            return AnnouncementUI.MeasurePortraitRect(slot, _portrait);
        }

        private void OnGUI()
        {
            if (!_isOpen || !NativePortraitLayout || Event.current.type != EventType.Repaint) return;
            ValidatePortrait();
            if (_portrait.Texture == null || PopupCamera == null || !PopupCamera.enabled) return;
            var rect = MeasurePortraitScreenRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            int depth = GUI.depth;
            try
            {
                GUI.depth = -20;
                if (_flatPortraitDrawer == null) _flatPortraitDrawer = new FlatPixelPortraitDrawer();
                _flatPortraitDrawer.Draw(rect, _portrait);
            }
            finally { GUI.depth = depth; }
        }

        private void OnDisable()
        {
            if (_isOpen) Close();
            else ClearPortrait();
        }

        private void OnDestroy()
        {
            ClearPortrait();
            _flatPortraitDrawer?.Dispose();
            _flatPortraitDrawer = null;
        }
    }
}
