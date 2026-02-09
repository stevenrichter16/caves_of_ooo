using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A named statistic on an entity (HP, Strength, Speed, etc.).
    /// Mirrors Qud's Statistic class with base value, bonus/penalty modifiers, and boost.
    /// The computed Value property = BaseValue + Bonus - Penalty + Boost.
    /// </summary>
    [Serializable]
    public class Stat
    {
        public Entity Owner;
        public string Name = "";
        public string sValue = "";

        public int BaseValue;
        public int Bonus;
        public int Penalty;
        public int Boost;
        public int Min = 0;
        public int Max = 30;

        /// <summary>
        /// Computed current value: base + bonus - penalty + boost, clamped to [Min, Max].
        /// </summary>
        public int Value
        {
            get
            {
                int v = BaseValue + Bonus - Penalty + Boost;
                if (v < Min) v = Min;
                if (v > Max) v = Max;
                return v;
            }
            set
            {
                BaseValue = value;
            }
        }

        public Stat() { }

        public Stat(Stat other)
        {
            Name = other.Name;
            sValue = other.sValue;
            BaseValue = other.BaseValue;
            Bonus = other.Bonus;
            Penalty = other.Penalty;
            Boost = other.Boost;
            Min = other.Min;
            Max = other.Max;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(sValue))
                return sValue;
            return Value.ToString();
        }
    }
}
