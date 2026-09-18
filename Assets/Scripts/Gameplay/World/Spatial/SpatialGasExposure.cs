using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>A large body takes one dose of each gas behavior/family per
    /// exposure pass. The strongest contacted pool represents that family;
    /// different families and later exposure passes remain independent.</summary>
    internal static class SpatialGasExposure
    {
        internal struct Dose
        {
            internal Entity Target;
            internal IObjectGasBehaviorPart Behavior;
            internal Cell Cell;
        }
        internal static bool SameFamily(IObjectGasBehaviorPart a,IObjectGasBehaviorPart b)
            => a!=null && b!=null && a.GetType()==b.GetType()
                && a.BaseGas?.GasType==b.BaseGas?.GasType;
        internal static bool Stronger(IObjectGasBehaviorPart a,IObjectGasBehaviorPart b)
        {
            int al=a.BaseGas?.Level ?? 0, bl=b.BaseGas?.Level ?? 0;
            if(al!=bl) return al>bl;
            return a.GasDensity()>b.GasDensity();
        }
        internal static void Add(List<Dose> doses,Entity target,IObjectGasBehaviorPart behavior,Cell cell)
        {
            for(int i=0;i<doses.Count;i++)
            {
                var old=doses[i];
                if(old.Target!=target || !SameFamily(old.Behavior,behavior)) continue;
                if(Stronger(behavior,old.Behavior)) doses[i]=new Dose {Target=target,Behavior=behavior,Cell=cell};
                return;
            }
            doses.Add(new Dose {Target=target,Behavior=behavior,Cell=cell});
        }
        internal static void Apply(List<Dose> doses,Zone zone)
        {
            foreach(var dose in doses)
            {
                var source=dose.Behavior?.ParentEntity;
                if(source==null || !source.Parts.Contains(dose.Behavior)) continue;
                if(dose.Cell.Occupants.Contains(dose.Target)
                    && zone.GetEntityCell(source)==dose.Cell)
                    dose.Behavior.ApplyGas(dose.Target,zone);
            }
        }
    }
}
