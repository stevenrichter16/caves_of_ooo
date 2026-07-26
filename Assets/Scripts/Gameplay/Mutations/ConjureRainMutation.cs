using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Conjure Rain — the Watering Grimoire's spell. Self-centered
    /// radius-3 cast (DryingBreezeMutation's exact ability shape): every
    /// crop within Chebyshev radius gets its moisture topped up, falling
    /// rain particles spawn above each watered crop tile, and the soil
    /// beneath darkens (via <see cref="CropPart.Water"/>'s wet-soil
    /// background) until the moisture dries out.
    /// See <c>Docs/CROPS-WATERING-GRIMOIRE.md §2.4</c>.
    ///
    /// <para><b>Convention notes:</b> the cast always succeeds and
    /// applies its cooldown even with zero crops nearby (DryingBreeze
    /// convention — the spell was cast; the message tells the player
    /// what happened). Rain FX only spawn over tiles that actually got
    /// watered, per the feature's spec ("spawns rain above the crop
    /// tiles").</para>
    /// </summary>
    public class ConjureRainMutation : BaseMutation
    {
        public const string COMMAND = "CommandConjureRain";
        public const int COOLDOWN = 5;
        public const int RADIUS = 3;

        /// <summary>Moisture granted per watering — top-up semantics via
        /// <see cref="CropPart.Water"/>, never additive.</summary>
        public const int MOISTURE_TICKS = 40;

        public override string Name => "Conjure Rain";
        public override string MutationType => "Mental";
        public override string DisplayName => "Conjure Rain";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(DisplayName, COMMAND,
                "Grimoire Spells", AbilityTargetingMode.SelfCentered, RADIUS);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != COMMAND)
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Cell sourceCell = e.GetParameter<Cell>("SourceCell");
            if (!Cast(zone, sourceCell))
                return true;

            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell)
        {
            if (zone == null || sourceCell == null || ParentEntity == null)
                return false;

            int minX = Math.Max(0, sourceCell.X - RADIUS);
            int maxX = Math.Min(Zone.Width - 1, sourceCell.X + RADIUS);
            int minY = Math.Max(0, sourceCell.Y - RADIUS);
            int maxY = Math.Min(Zone.Height - 1, sourceCell.Y + RADIUS);

            int cropsWatered = 0;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (Math.Max(Math.Abs(x - sourceCell.X), Math.Abs(y - sourceCell.Y)) > RADIUS)
                        continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null)
                        continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        var crop = cell.Objects[i].GetPart<CropPart>();
                        if (crop == null)
                            continue;

                        crop.Water(MOISTURE_TICKS);
                        EmitRainFx(zone, x, y);
                        cropsWatered++;
                        break; // one crop per cell by the planting gate
                    }
                }
            }

            MessageLog.Add(cropsWatered > 0
                ? "Rain patters down over the crops."
                : "The conjured rain finds no crops to nourish.");

            if (Diagnostics.Diag.IsChannelEnabled("crop"))
                Diagnostics.Diag.Record("crop", "RainConjured", actor: ParentEntity,
                    payload: new { cropsWatered = cropsWatered, radius = RADIUS });

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }

        /// <summary>
        /// Falling rain over one watered crop tile: three staggered
        /// drops falling from above the tile (dy:+1 — the same moving-
        /// particle mechanism floating damage numbers use upward), then
        /// a Water-theme splash on the tile itself. Pure enqueues on
        /// AsciiFxBus — EditMode-safe, renderer drains them.
        ///
        /// <para><b>Lifetime = travel budget.</b> The renderer moves a
        /// particle floor(lifetime / moveInterval) cells over its life
        /// (AsciiFxRenderer.UpdateParticles), so each drop's lifetime is
        /// sized to die ON the crop row: spawn N cells above → N moves.
        /// The original 0.45f gave every drop 4 moves — drops visibly
        /// rained 2-3 tiles THROUGH the soil below the splash (SM7d,
        /// farming audit note).</para>
        /// </summary>
        private static void EmitRainFx(Zone zone, int x, int y)
        {
            // 2 cells above → 2 moves (0.2s) + margin, dies at the crop row.
            AsciiFxBus.EmitParticle(zone, x, y - 2, '|', "&B",
                lifetime: 0.25f, dy: 1, moveInterval: 0.1f, delay: 0f);
            // 1 cell above → 1 move.
            AsciiFxBus.EmitParticle(zone, x, y - 1, '\'', "&b",
                lifetime: 0.15f, dy: 1, moveInterval: 0.1f, delay: 0.12f);
            AsciiFxBus.EmitParticle(zone, x, y - 2, '.', "&B",
                lifetime: 0.25f, dy: 1, moveInterval: 0.1f, delay: 0.24f);
            AsciiFxBus.EmitBurst(zone, x, y, AsciiFxTheme.Water,
                blocksTurnAdvance: false, delay: 0.3f);
        }
    }
}
