using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Emits death splatter FX when a creature dies.
    /// 10 variations selected randomly with special-case overrides
    /// (ghost fade for undead, creature-typed blood color, overkill scaling).
    /// All output goes through AsciiFxBus.EmitParticle.
    /// </summary>
    public static class DeathSplatterFx
    {
        private static readonly char[] BloodGlyphs = { '.', ',', '\'', '`', '~', ';' };
        private static readonly char[] GibGlyphs = { '%', '*', '~', ',', '.' };
        private static readonly char[] GhostGlyphs = { '.', '\'', '`', ' ' };

        /// <summary>
        /// Emit a death splatter at the target's position.
        /// Called from CombatSystem.HandleDeath before entity removal.
        /// </summary>
        public static void Emit(Entity target, Entity killer, Zone zone)
        {
            if (target == null || zone == null) return;

            Cell cell = zone.GetEntityCell(target);
            if (cell == null) return;

            int x = cell.X;
            int y = cell.Y;

            var rng = new Random();

            // Ghost fade for undead creatures
            if (target.HasTag("GhostDeath"))
            {
                EmitGhostFade(zone, x, y, rng);
                return;
            }

            // Resolve blood color: creature-typed override or default red
            string bloodColor = target.GetTag("BloodColor", "&r");

            // Calculate overkill multiplier
            int hp = target.GetStatValue("Hitpoints", 0);
            int maxHp = target.GetStat("Hitpoints")?.Max ?? 1;
            float overkillRatio = maxHp > 0 ? (float)Math.Abs(hp) / maxHp : 0f;
            int multiplier = 1 + (int)(overkillRatio * 2);
            if (multiplier > 3) multiplier = 3;

            // Resolve direction from killer for directional variations
            int dx = 0, dy = 0;
            if (killer != null)
            {
                Cell killerCell = zone.GetEntityCell(killer);
                if (killerCell != null)
                {
                    dx = Math.Sign(x - killerCell.X);
                    dy = Math.Sign(y - killerCell.Y);
                }
            }

            // Pick a random variation (weighted toward simpler ones)
            int roll = rng.Next(100);
            if (roll < 20)
                EmitDirectionalSpray(zone, x, y, dx, dy, bloodColor, multiplier, rng);
            else if (roll < 40)
                EmitRadialBurst(zone, x, y, bloodColor, multiplier, rng);
            else if (roll < 55)
                EmitPoolingDrip(zone, x, y, bloodColor, multiplier, rng);
            else if (roll < 65)
                EmitGibletScatter(zone, x, y, bloodColor, multiplier, rng);
            else if (roll < 75)
                EmitVerticalSplash(zone, x, y, bloodColor, multiplier, rng);
            else if (roll < 82)
                EmitLingeringStain(zone, x, y, bloodColor, rng);
            else if (roll < 89)
                EmitColorFadeCascade(zone, x, y, multiplier, rng);
            else
                EmitRadialBurst(zone, x, y, bloodColor, multiplier, rng);
        }

        /// <summary>
        /// 1. Directional Spray — blood particles fly away from the killer.
        /// </summary>
        private static void EmitDirectionalSpray(Zone zone, int x, int y,
            int dx, int dy, string color, int multiplier, Random rng)
        {
            if (dx == 0 && dy == 0) { dx = 1; dy = 0; }

            int count = 3 * multiplier;
            for (int i = 0; i < count; i++)
            {
                int px = x + dx * (1 + rng.Next(2));
                int py = y + dy * (1 + rng.Next(2));
                // Slight lateral scatter
                px += rng.Next(3) - 1;
                py += rng.Next(3) - 1;
                if (!zone.InBounds(px, py)) continue;

                char glyph = BloodGlyphs[rng.Next(BloodGlyphs.Length)];
                float delay = i * 0.03f;
                float lifetime = 0.4f + (float)rng.NextDouble() * 0.3f;
                AsciiFxBus.EmitParticle(zone, px, py, glyph, color, lifetime, delay: delay);
            }
        }

        /// <summary>
        /// 2. Radial Burst — blood splatters in all directions from death point.
        /// </summary>
        private static void EmitRadialBurst(Zone zone, int x, int y,
            string color, int multiplier, Random rng)
        {
            int count = 4 * multiplier;
            for (int i = 0; i < count; i++)
            {
                int px = x + rng.Next(3) - 1;
                int py = y + rng.Next(3) - 1;
                if (!zone.InBounds(px, py)) continue;

                char glyph = BloodGlyphs[rng.Next(BloodGlyphs.Length)];
                float delay = (float)rng.NextDouble() * 0.08f;
                float lifetime = 0.3f + (float)rng.NextDouble() * 0.4f;
                AsciiFxBus.EmitParticle(zone, px, py, glyph, color, lifetime, delay: delay);
            }
        }

        /// <summary>
        /// 3. Pooling Drip — particles appear at center then slowly spread downward.
        /// </summary>
        private static void EmitPoolingDrip(Zone zone, int x, int y,
            string color, int multiplier, Random rng)
        {
            // Center stain
            AsciiFxBus.EmitParticle(zone, x, y, '.', color, 0.8f);

            int drips = 2 * multiplier;
            for (int i = 0; i < drips; i++)
            {
                int px = x + rng.Next(3) - 1;
                int py = y + 1; // drips downward
                if (!zone.InBounds(px, py)) continue;

                char glyph = BloodGlyphs[rng.Next(3)]; // smaller glyphs
                float delay = 0.1f + i * 0.08f;
                AsciiFxBus.EmitParticle(zone, px, py, glyph, color, 0.6f,
                    dy: 1, moveInterval: 0.2f, delay: delay);
            }
        }

        /// <summary>
        /// 4. Giblet Scatter — chunky debris flies outward.
        /// </summary>
        private static void EmitGibletScatter(Zone zone, int x, int y,
            string color, int multiplier, Random rng)
        {
            int count = 3 * multiplier;
            for (int i = 0; i < count; i++)
            {
                int px = x + rng.Next(5) - 2;
                int py = y + rng.Next(5) - 2;
                if (!zone.InBounds(px, py)) continue;

                char glyph = GibGlyphs[rng.Next(GibGlyphs.Length)];
                float delay = i * 0.02f;
                float lifetime = 0.3f + (float)rng.NextDouble() * 0.3f;
                AsciiFxBus.EmitParticle(zone, px, py, glyph, color, lifetime, delay: delay);
            }
        }

        /// <summary>
        /// 5. Vertical Splash — blood rises upward briefly then fades.
        /// </summary>
        private static void EmitVerticalSplash(Zone zone, int x, int y,
            string color, int multiplier, Random rng)
        {
            int count = 2 * multiplier;
            for (int i = 0; i < count; i++)
            {
                int px = x + rng.Next(3) - 1;
                if (!zone.InBounds(px, y)) continue;

                char glyph = BloodGlyphs[rng.Next(BloodGlyphs.Length)];
                float delay = i * 0.04f;
                AsciiFxBus.EmitParticle(zone, px, y, glyph, color, 0.5f,
                    dy: -1, moveInterval: 0.12f, delay: delay);
            }
        }

        /// <summary>
        /// 6. Lingering Stain — a long-lived blood mark at the death spot.
        /// </summary>
        private static void EmitLingeringStain(Zone zone, int x, int y,
            string color, Random rng)
        {
            char[] stainGlyphs = { '.', ',', ';' };
            // Place 1-3 long-lived stain particles
            int count = 1 + rng.Next(3);
            for (int i = 0; i < count; i++)
            {
                int px = x + rng.Next(3) - 1;
                int py = y + rng.Next(3) - 1;
                if (!zone.InBounds(px, py)) continue;

                char glyph = stainGlyphs[rng.Next(stainGlyphs.Length)];
                AsciiFxBus.EmitParticle(zone, px, py, glyph, color, 3.0f);
            }
        }

        /// <summary>
        /// 7. Color Fade Cascade — blood starts bright red, shifts to dark.
        /// Three waves with progressively darker color codes.
        /// </summary>
        private static void EmitColorFadeCascade(Zone zone, int x, int y,
            int multiplier, Random rng)
        {
            string[] fadeColors = { "&R", "&r", "&K" };
            int countPerWave = 2 * multiplier;

            for (int wave = 0; wave < fadeColors.Length; wave++)
            {
                for (int i = 0; i < countPerWave; i++)
                {
                    int px = x + rng.Next(3) - 1;
                    int py = y + rng.Next(3) - 1;
                    if (!zone.InBounds(px, py)) continue;

                    char glyph = BloodGlyphs[rng.Next(BloodGlyphs.Length)];
                    float delay = wave * 0.15f + i * 0.03f;
                    float lifetime = 0.25f + (float)rng.NextDouble() * 0.2f;
                    AsciiFxBus.EmitParticle(zone, px, py, glyph, fadeColors[wave],
                        lifetime, delay: delay);
                }
            }
        }

        /// <summary>
        /// 10. Ghost Fade — ethereal wisps rise and vanish (for undead/spectral creatures).
        /// </summary>
        private static void EmitGhostFade(Zone zone, int x, int y, Random rng)
        {
            string[] ghostColors = { "&C", "&B", "&K" };
            int count = 4 + rng.Next(3);
            for (int i = 0; i < count; i++)
            {
                int px = x + rng.Next(3) - 1;
                if (!zone.InBounds(px, y)) continue;

                char glyph = GhostGlyphs[rng.Next(GhostGlyphs.Length)];
                string color = ghostColors[rng.Next(ghostColors.Length)];
                float delay = i * 0.06f;
                AsciiFxBus.EmitParticle(zone, px, y, glyph, color, 0.6f,
                    dy: -1, moveInterval: 0.1f, delay: delay);
            }
        }
    }
}
