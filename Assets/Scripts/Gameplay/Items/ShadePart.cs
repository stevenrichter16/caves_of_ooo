using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Shade as a capability (Docs/MACHINE-INVENTED-USES.md 3o). The Beating's glare (<see cref="BeatingGlareSystem"/>) asks one question of a traveller's cell: is there shade here? Until now only a
    /// roof (an interior cell) or something worn on the head answered it. A thing that stands in the world and carries this Part also does: it shades its own cell and the cells within
    /// <see cref="Radius"/> of it (at most <see cref="MaxRadius"/>), for anyone who stands there, while it stands. A kit that is carried, dropped or taken up shades nothing. Any future awning, canopy
    /// or cloth carries the same Part and gets the same rule, with no edit to the glare system.
    /// </summary>
    public sealed class ShadePart : Part, IDescribeState
    {
        public override string Name => "Shade";
        public const int MaxRadius = 2;

        /// <summary>Cells around it that it covers: 1 is its own cell and the ring around it.</summary>
        public int Radius = 1;

        /// <summary>True when this one is standing in the world (set down and installed), so it is giving shade now.</summary>
        public bool Shading => ParentEntity?.SpatialZone != null && !PackableFixturePart.IsInertKit(ParentEntity);

        /// <summary>Whether any standing shade covers the cell. Read-only; looks only at the few cells around it.</summary>
        public static bool Covers(Zone zone, Cell cell)
        {
            if (zone == null || cell == null) return false;
            for (int dx = -MaxRadius; dx <= MaxRadius; dx++)
                for (int dy = -MaxRadius; dy <= MaxRadius; dy++)
                {
                    var at = zone.GetCell(cell.X + dx, cell.Y + dy); if (at == null) continue;
                    int reach = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    for (int i = 0; i < at.Objects.Count; i++)
                    {
                        var shade = at.Objects[i]?.GetPart<ShadePart>();
                        if (shade != null && shade.ParentEntity == at.Objects[i] && shade.Shading && Math.Min(shade.Radius, MaxRadius) >= reach) return true;
                    }
                }
            return false;
        }

        // ── reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 45;

        public string DescribeState(DescribeContext context)
            => Shading
                ? "It shades the ground " + (Math.Min(Radius, MaxRadius) == 1 ? "under it and the cells around it" : "within " + Math.Min(Radius, MaxRadius) + " cells of it") + " from the glare, for whoever stands there."
                : "Cloth on collapsible poles. Set down on open ground, it shades the cells around it from the glare. Carried, it shades nothing.";
    }
}
