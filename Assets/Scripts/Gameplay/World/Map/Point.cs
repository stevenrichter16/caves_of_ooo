namespace CavesOfOoo.Core
{
    /// <summary>
    /// Lightweight integer grid coordinate used by projectile and FX systems.
    /// </summary>
    public struct Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }
}
