using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Save-backed scene revision and removals. Runtime owner cache
    /// is reconstructed from saved stable identities, never Unity objects.</summary>
    public sealed class FellingSceneStatePart : Part
    {
        public override string Name=>"FellingSceneState";
        public int Revision=1;
        public string RemovedIds="";
        public int PopulationRevision;
        public int DressingRevision;
        public bool PreserveMissingSeventh;
        [NonSerialized] internal Dictionary<string,Entity> Owners;
        [NonSerialized] internal Dictionary<string,Entity> DressingOwners;
        [NonSerialized] internal int DressingOwnerVersion = -1;
        [NonSerialized] private string parsedRemovals;
        [NonSerialized] private HashSet<string> removed;
        public bool WasRemoved(string id)
        {
            if(string.IsNullOrEmpty(id))return false;
            if(removed==null||parsedRemovals!=RemovedIds)
            {
                removed=new HashSet<string>(StringComparer.Ordinal);
                if(!string.IsNullOrEmpty(RemovedIds))foreach(var part in RemovedIds.Split('|'))if(FellingSceneDefinition.ValidId(part))removed.Add(part);
                parsedRemovals=RemovedIds;
            }
            return removed.Contains(id);
        }
        internal void RecordRemoval(string id)
        {
            if(!FellingSceneDefinition.ValidId(id)||WasRemoved(id))return;
            RemovedIds=string.IsNullOrEmpty(RemovedIds)?id:RemovedIds+"|"+id;
            removed.Add(id);parsedRemovals=RemovedIds;
        }
    }
}
