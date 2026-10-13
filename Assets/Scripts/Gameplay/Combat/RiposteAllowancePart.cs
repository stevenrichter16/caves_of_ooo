using System;
namespace CavesOfOoo.Core
{
    /// <summary>Actor-owned issued-counter allowance. Skill teardown never removes it.
    /// Only the scheduled owner completion renews it; load does not run acquisition.</summary>
    public sealed class RiposteAllowancePart : Part
    {
        public bool Spent;
        [NonSerialized] internal bool Reserved;
    }
}
