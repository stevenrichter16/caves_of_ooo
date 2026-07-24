using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.7b — per-weapon on-hit gas emission. Reads
    /// <see cref="MeleeWeaponPart.EmitGasOnHitRaw"/>, parses it into
    /// <see cref="EmitGasOnHitSpec"/> entries via the cached
    /// <see cref="MeleeWeaponPart.EmitGasOnHitCachedSpecs"/>, rolls
    /// each spec's ChancePercent independently, and spawns a 3×3 gas
    /// cloud at the defender's cell on success.
    ///
    /// <para>Called from <c>CombatSystem.PerformSingleAttack</c>
    /// immediately after <see cref="OnHitWeaponEffects.Apply"/>, inside
    /// the same <c>if (hpAfter &gt; 0)</c> survival block. So gas
    /// emission ONLY fires when the defender survives — Qud's
    /// <c>EmitGasOnHit</c> fires regardless of kill, but G.7b ships
    /// with the conservative gate. Tracked as 🟡 in self-review for a
    /// potential follow-up (gas-on-killing-blow is the same hit-cell
    /// dispatch, just outside the survival gate).</para>
    ///
    /// <para>Direct port of Qud
    /// <c>XRL.World.Parts.EmitGasOnHit.EmitGas</c>
    /// (EmitGasOnHit.cs:132-188): center cell at CellDensity, 8
    /// adjacents at AdjacentDensity each, attacker credited as
    /// Creator on every spawned cloud.</para>
    ///
    /// <para>Docs/COMBAT-SYSTEM-AUDIT-2026-07.md: takes <c>damage</c> +
    /// <c>actualDamage</c> and gates on <c>actualDamage &lt;= 0</c>,
    /// mirroring <see cref="OnHitClassEffects.Apply"/> and
    /// <see cref="OnHitWeaponEffects.Apply"/>'s "no damage = no on-hit"
    /// contract — a fully-resisted/vetoed 0-damage hit must not spawn a
    /// gas cloud, same as it must not stun/bleed/confuse or proc a
    /// weapon effect. Currently unreachable in shipped content (no
    /// blueprint sets <c>EmitGasOnHitRaw</c> yet), but fixed now while
    /// touching this pipeline so a future gas-weapon blueprint doesn't
    /// inherit the gap.</para>
    /// </summary>
    public static class OnHitGasEmit
    {
        public static void Apply(MeleeWeaponPart weapon, Damage damage, int actualDamage,
            Entity defender, Entity attacker, Zone zone, Random rng)
        {
            if (weapon == null || defender == null || zone == null || rng == null) return;
            if (actualDamage <= 0) return;
            if (string.IsNullOrWhiteSpace(weapon.EmitGasOnHitRaw)) return;

            // The impact cell is the defender's current cell — that's
            // where the gas pools.
            var center = zone.GetEntityCell(defender);
            if (center == null) return;

            List<EmitGasOnHitSpec> specs = weapon.EmitGasOnHitCachedSpecs;
            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                if (rng.Next(100) >= spec.ChancePercent) continue;

                int spawnedCenter = 0;
                int spawnedAdjacent = 0;

                // Center cell at spec.CellDensity.
                var centerSpawn = GasFactory.SpawnGas(zone, center.X, center.Y,
                    spec.GasId, density: spec.CellDensity, level: spec.GasLevel,
                    creator: attacker);
                if (centerSpawn != null) spawnedCenter++;

                // 8 adjacents at spec.AdjacentDensity.
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // skip center (already done)
                        var adj = GasFactory.SpawnGas(zone, center.X + dx, center.Y + dy,
                            spec.GasId, density: spec.AdjacentDensity, level: spec.GasLevel,
                            creator: attacker);
                        if (adj != null) spawnedAdjacent++;
                    }
                }

                if (Diag.IsChannelEnabled("gas"))
                    Diag.Record("gas", "EmitOnHit", attacker, defender,
                        new
                        {
                            gasId = spec.GasId,
                            chance = spec.ChancePercent,
                            cellDensity = spec.CellDensity,
                            adjacentDensity = spec.AdjacentDensity,
                            gasLevel = spec.GasLevel,
                            spawnedCenter,
                            spawnedAdjacent,
                            totalSpawned = spawnedCenter + spawnedAdjacent,
                        });
            }
        }
    }
}
