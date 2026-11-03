namespace CavesOfOoo.Core
{
    /// <summary>Directional actions require choose, then confirm. Cursor movement
    /// is returned immediately because it has no turn cost.</summary>
    public sealed class PhoneDirectionSelection
    {
        private int _context = -1, _x, _y;
        public void SetContext(int context)
        { if (_context == context) return; _context = context; Clear(); }
        public void Clear() { _x = _y = 0; }
        public bool TrySelected(out int x, out int y)
        { x = _x; y = _y; return x != 0 || y != 0; }
        public bool Resolve(PhoneIntent intent, bool cursor, out int x, out int y)
        {
            x = y = 0;
            if (intent == null) return false;
            if (intent.kind == "step")
            {
                if (cursor) { x = intent.x; y = intent.y; return x != 0 || y != 0; }
                _x = intent.x; _y = intent.y;
                return false;
            }
            return !cursor && intent.kind == "key" && (intent.value == "Return" || intent.value == "Enter")
                && TrySelected(out x, out y);
        }
    }
}
