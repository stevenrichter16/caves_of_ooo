using System;

namespace CavesOfOoo.Core
{
    public delegate void EntityMovedVisualHandler(
        Entity entity,
        Zone zone,
        int oldX,
        int oldY,
        int newX,
        int newY,
        bool forced);

    public delegate void EntityAttackVisualHandler(Entity attacker, Entity defender, Zone zone);
    public delegate void EntityCastVisualHandler(
        Entity caster, Zone zone, string spellID,
        int sourceX, int sourceY, int targetX, int targetY, float duration);
    public delegate void EntityDamageVisualHandler(Entity target, Entity source, Zone zone, int amount, bool lethal);
    public delegate void EntityDeathVisualHandler(Entity target, Entity killer, Zone zone, int x, int y);

    /// <summary>
    /// Presentation-only event surface for resolved simulation actions. Gameplay calls
    /// these null-safe hooks without knowing whether Unity visuals are present. Nothing
    /// queued here is serialized; load reconstructs views from stable entity state.
    /// </summary>
    public static class EntityVisualHooks
    {
        public static EntityMovedVisualHandler MovedCallback { get; set; }
        public static EntityAttackVisualHandler AttackCallback { get; set; }
        public static EntityCastVisualHandler CastCallback { get; set; }
        public static EntityDamageVisualHandler DamageCallback { get; set; }
        public static EntityDeathVisualHandler DeathCallback { get; set; }

        public static void EmitMoved(
            Entity entity,
            Zone zone,
            int oldX,
            int oldY,
            int newX,
            int newY,
            bool forced = false)
        {
            MovedCallback?.Invoke(entity, zone, oldX, oldY, newX, newY, forced);
        }

        public static void EmitAttack(Entity attacker, Entity defender, Zone zone)
        {
            AttackCallback?.Invoke(attacker, defender, zone);
        }

        /// <summary>
        /// Begins a resolved spell's casting pose using captured aim coordinates.
        /// The target may already have moved or died; self-casts retain their facing.
        /// Works without a renderer, including non-humanoid and CP437-only actors.
        /// </summary>
        public static void EmitCast(
            Entity caster, Zone zone, string spellID,
            int sourceX, int sourceY, int targetX, int targetY, float duration = 0.16f)
        {
            if (caster == null) return;
            RenderPart render = caster.GetPart<RenderPart>();
            int dx = targetX - sourceX;
            int dy = targetY - sourceY;
            if (render != null && (dx != 0 || dy != 0))
            {
                render.VisualFacing = Math.Abs(dx) > Math.Abs(dy)
                    ? (dx > 0 ? EntityVisualFacing.East : EntityVisualFacing.West)
                    : (dy > 0 ? EntityVisualFacing.South : EntityVisualFacing.North);
            }
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                duration = 0.16f;
            CastCallback?.Invoke(caster, zone, spellID, sourceX, sourceY, targetX, targetY, duration);
        }

        public static void EmitDamage(
            Entity target,
            Entity source,
            Zone zone,
            int amount,
            bool lethal)
        {
            DamageCallback?.Invoke(target, source, zone, amount, lethal);
        }

        public static void EmitDeath(Entity target, Entity killer, Zone zone, int x, int y)
        {
            DeathCallback?.Invoke(target, killer, zone, x, y);
        }

        public static void Reset()
        {
            MovedCallback = null;
            AttackCallback = null;
            CastCallback = null;
            DamageCallback = null;
            DeathCallback = null;
        }
    }
}
