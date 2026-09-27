#if UNITY_EDITOR
using System;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Offline art resolution in metres, independent of placement scale.
    /// Static scenery divides the one-metre gameplay grid; skins retain finer
    /// joint detail, and tiny/thin objects keep their prior readable resolution.</summary>
    public static class VoxelWorldDensity
    {
        public const float SceneryPitch=.25f;
        public const float CharacterPitch=.1875f;
        public const float SmallObjectExtent=.55f;
        public const float ThinCreatureExtent=.15f;
        public const float ThinRigidCrossSection=.5f;
        public const float RigidElongation=4f;
        public static float SelectWorldPitch(Vector3 localBoundsSize,float prefabScale,bool skinned,bool equipment)
        {
            if(!Finite(prefabScale)||prefabScale<=0)throw new ArgumentException("Prefab scale must be positive and finite.");
            for(int axis=0;axis<3;axis++)
                if(!Finite(localBoundsSize[axis])||localBoundsSize[axis]<0)throw new ArgumentException("Bounds must be finite and nonnegative.");
            var world=localBoundsSize*prefabScale;float diagonal=world.magnitude;
            if(!Finite(diagonal)||diagonal<=0)throw new ArgumentException("World bounds must have a positive finite extent.");
            float previousPitch=diagonal>=3f?.2f:.125f;
            float longest=Mathf.Max(world.x,Mathf.Max(world.y,world.z));
            float shortest=Mathf.Min(world.x,Mathf.Min(world.y,world.z));
            if(equipment||longest<SmallObjectExtent||(skinned&&shortest<ThinCreatureExtent))return previousPitch;
            // Two narrow axes identify rods/pipes, unlike a thin broad floor.
            // Enlarging their cross-section can fill native selection gaps.
            float middle=Mathf.Max(Mathf.Min(world.x,world.y),Mathf.Min(Mathf.Max(world.x,world.y),world.z));
            if(!skinned&&middle<ThinRigidCrossSection&&longest>=RigidElongation*middle)return previousPitch;
            return skinned?Mathf.Max(previousPitch,CharacterPitch):SceneryPitch;
        }
        /// <summary>The one original thin-winged moth needs enough cells to
        /// retain four lobes and its dark body. Exact native source identity and
        /// a real skin are required; all generic density and validation stay intact.</summary>
        public static float SelectNativeWorldPitch(string sourceAssetPath,Vector3 localBoundsSize,float prefabScale,bool skinned,bool equipment)
        {
            float ordinary=SelectWorldPitch(localBoundsSize,prefabScale,skinned,equipment);
            // These four thin authored basins contain separate .02m water and
            // .16m rim rocks. The ordinary .25m cell merges both into one slab.
            // Keep exact source identities and ordinary validation/capability gates.
            const float sprayBasinPitch=.0625f;
            if(!skinned&&!equipment&&IsSprayBasin(sourceAssetPath))return sprayBasinPitch;
            return sourceAssetPath=="Assets/Art3D/SpawnRing/Models/ring-grove-lantern-moth.fbx"&&skinned&&!equipment
                ?.0625f:ordinary;
        }
        static bool IsSprayBasin(string sourceAssetPath)
        {
            switch(sourceAssetPath)
            {
                case "Assets/Art3D/SpawnRing/Models/ring-spray-pool-0.fbx":
                case "Assets/Art3D/SpawnRing/Models/ring-spray-pool-1.fbx":
                case "Assets/Art3D/SpawnRing/Models/ring-spray-pool-2.fbx":
                case "Assets/Art3D/SpawnRing/Models/ring-spray-pool-3.fbx": return true;
                default: return false;
            }
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
#endif
