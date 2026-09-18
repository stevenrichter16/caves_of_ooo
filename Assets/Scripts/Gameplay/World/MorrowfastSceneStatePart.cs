using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Durable semantic state. No Unity objects or derived collider caches are serialized.</summary>
    public sealed class MorrowfastSceneStatePart : Part
    {
        public override string Name=>"MorrowfastSceneState";
        public int Revision=1;
        public string RemovedIds="", OpenDoorIds="", LiftedRoofIds="";
        [NonSerialized] internal Dictionary<string,Entity> Owners;
        [NonSerialized] internal int LastOwnerScan=-1;
        [NonSerialized] internal List<MorrowfastSceneDefinition.OwnerSpec>[] Footprints;
        [NonSerialized] internal int FootprintVersion=-1;
        [NonSerialized] private string previousRemoved,previousOpen,previousRoof;
        [NonSerialized] private HashSet<string> removed,open,roof;
        private static HashSet<string> Read(string value)
        {var result=new HashSet<string>(StringComparer.Ordinal);if(!string.IsNullOrEmpty(value))foreach(string id in value.Split('|'))if(MorrowfastSceneDefinition.ValidId(id))result.Add(id);return result;}
        public bool WasRemoved(string id){if(removed==null||previousRemoved!=RemovedIds){removed=Read(RemovedIds);previousRemoved=RemovedIds;}return id!=null&&removed.Contains(id);}
        public bool DoorIsOpen(string id){if(open==null||previousOpen!=OpenDoorIds){open=Read(OpenDoorIds);previousOpen=OpenDoorIds;}return id!=null&&open.Contains(id);}
        public bool RoofIsLifted(string id){if(roof==null||previousRoof!=LiftedRoofIds){roof=Read(LiftedRoofIds);previousRoof=LiftedRoofIds;}return id!=null&&roof.Contains(id);}
        internal void RemoveOwner(string id){WasRemoved(id);if(removed.Add(id)){RemovedIds=string.Join("|",removed);previousRemoved=RemovedIds;}}
        internal void SetDoor(string id,bool value){DoorIsOpen(id);if(value)open.Add(id);else open.Remove(id);OpenDoorIds=string.Join("|",open);previousOpen=OpenDoorIds;}
        internal void SetRoof(string id,bool value){RoofIsLifted(id);if(value)roof.Add(id);else roof.Remove(id);LiftedRoofIds=string.Join("|",roof);previousRoof=LiftedRoofIds;}
    }
}
