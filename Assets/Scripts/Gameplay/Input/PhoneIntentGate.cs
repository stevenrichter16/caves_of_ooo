using System;

namespace CavesOfOoo.Core
{
    [Serializable]
    public sealed class PhoneIntent
    {
        public int revision;
        public string kind, value;
        public int x, y;
        public float screenX, screenY;
    }

    /// <summary>A bounded mailbox, not a key-hold emulator. Every accepted tap
    /// is scoped to the visible UI revision; old taps cannot leak through modals.</summary>
    public sealed class PhoneIntentGate
    {
        private PhoneIntent _pending;
        private int _context = -1;
        public int Revision { get; private set; } = 1;
        public bool Suspended { get; private set; }
        public bool HasPending => _pending != null;
        public void SetContext(int context)
        { if (_context == context) return; _context = context; Quarantine(); }
        public void Quarantine() { _pending = null; Revision++; }
        public void Suspend() { Suspended = true; Quarantine(); }
        public void Resume() { Suspended = false; Quarantine(); }
        public bool Offer(PhoneIntent intent)
        {
            if (intent == null || Suspended || _pending != null || intent.revision != Revision
                || Math.Abs((long)intent.x) > 1 || Math.Abs((long)intent.y) > 1
                || (intent.value != null && intent.value.Length > 80)) return false;
            switch (intent.kind)
            {
                case "step": case "wait": case "command": case "key": case "inspect": case "text": break;
                default: return false;
            }
            if (intent.kind == "inspect" && (float.IsNaN(intent.screenX) || float.IsNaN(intent.screenY)
                || intent.screenX < 0 || intent.screenX > 1 || intent.screenY < 0 || intent.screenY > 1)) return false;
            _pending = intent; return true;
        }
        public PhoneIntent Take() { var result = _pending; _pending = null; return result; }
    }
}
