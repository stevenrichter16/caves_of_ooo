namespace CavesOfOoo.Core
{
    /// <summary>
    /// Pure C# cursor state for world-space inspection/targeting.
    /// This stores intent only; Unity input and rendering stay elsewhere.
    /// </summary>
    public sealed class WorldCursorState
    {
        public bool Active { get; private set; }
        public WorldCursorMode Mode { get; private set; }
        public Zone Zone { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int AnchorX { get; private set; }
        public int AnchorY { get; private set; }
        public int? MaxRange { get; private set; }
        public bool FollowMouse { get; private set; }

        public void Activate(
            WorldCursorMode mode,
            Zone zone,
            int x,
            int y,
            int anchorX,
            int anchorY,
            int? maxRange = null,
            bool followMouse = true)
        {
            Active = true;
            Mode = mode;
            Zone = zone;
            X = x;
            Y = y;
            AnchorX = anchorX;
            AnchorY = anchorY;
            MaxRange = maxRange;
            FollowMouse = followMouse;
            ClampToZone();
        }

        public void Deactivate()
        {
            Active = false;
            Zone = null;
            X = 0;
            Y = 0;
            AnchorX = 0;
            AnchorY = 0;
            MaxRange = null;
            FollowMouse = false;
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
            ClampToZone();
        }

        public void MoveBy(int dx, int dy)
        {
            SetPosition(X + dx, Y + dy);
        }

        public void ClampToZone()
        {
            if (Zone == null)
                return;

            X = X < 0 ? 0 : (X >= Zone.Width ? Zone.Width - 1 : X);
            Y = Y < 0 ? 0 : (Y >= Zone.Height ? Zone.Height - 1 : Y);
        }

        public int DistanceFromAnchor()
        {
            int dx = X - AnchorX;
            if (dx < 0)
                dx = -dx;

            int dy = Y - AnchorY;
            if (dy < 0)
                dy = -dy;

            return dx > dy ? dx : dy;
        }

        public bool CanOccupy(int x, int y)
        {
            return Zone != null && Zone.InBounds(x, y);
        }
    }
}
