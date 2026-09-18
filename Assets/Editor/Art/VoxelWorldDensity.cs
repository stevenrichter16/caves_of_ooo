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
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
#endif
