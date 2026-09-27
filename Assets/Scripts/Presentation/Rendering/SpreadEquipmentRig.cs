using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Owned-instance attachment anchors reconstructed from the actual
    /// imported bindposes. Current animation pose cannot shift the reference fit.</summary>
    internal sealed class SpreadEquipmentRig : IDisposable
    {
        private static readonly string[] Names = { "Root", "Spine", "Head", "Arm.L", "Arm.R", "Hand.L", "Hand.R", "Leg.L", "Leg.R" };
        private readonly Transform root;
        private readonly SkinnedMeshRenderer skin;
        private readonly Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> original = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly bool marlback;
        private readonly Bounds torsoBounds, headBounds;
        // Shared fitted forms are authored at these measured bind-space extents.
        private const float TorsoFormWidth = .5f, TorsoFormLength = .5725f;
        private const float TorsoFrontPlane = .1835f, HeadFormWidth = .347f, HeadFormDepth = .349f;
        private const float BodySideClearance = .08f, BodyLengthClearance = .09f, BodyTopClearance = .035f;
        private const float LowBodyDepthScale = 1.8f, LowHeadHeightScale = .65f;
        public bool Supported { get; }
        public SpreadEquipmentRig(Entity actor, GameObject instance)
        {
            root = instance.transform; marlback = actor.BlueprintName?.StartsWith("Marlback", StringComparison.Ordinal) == true
                || SpreadRareMarlbackLibrary.IsBlueprint(actor.BlueprintName);
            foreach (var candidate in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (candidate.sharedMesh == null || candidate.bones.Length != Names.Length || candidate.sharedMesh.bindposeCount != Names.Length) continue;
                var found = new Dictionary<string, int>(StringComparer.Ordinal); bool valid = true;
                for (int i = 0; i < candidate.bones.Length; i++)
                {
                    var bone = candidate.bones[i];
                    if (bone == null || !bone.IsChildOf(root) || Array.IndexOf(Names, bone.name) < 0 || found.ContainsKey(bone.name)) { valid = false; break; }
                    found.Add(bone.name, i);
                }
                if (!valid) continue;
                skin = candidate; foreach (var pair in found) indices.Add(pair.Key, pair.Value); break;
            }
            if (skin == null) return;
            foreach (var transform in instance.GetComponentsInChildren<Transform>(true))
            {
                string name = transform.name;
                if (name != "Equipment.Head" && name != "Equipment.Hand.L" && name != "Equipment.Hand.R" && name != "Equipment.Back") continue;
                if (original.ContainsKey(name)) return;
                original.Add(name, transform);
            }
            if (original.Count != 4) return;
            if (marlback && (!TryBindBounds("Spine", out torsoBounds) || !TryBindBounds("Head", out headBounds))) return;
            Supported = true;
        }
        private Matrix4x4 BoneToRoot(int index)
            => root.worldToLocalMatrix * skin.transform.localToWorldMatrix * skin.sharedMesh.bindposes[index].inverse;
        private Vector3 SocketRest(string socket, string bone)
        {
            int index = indices[bone];
            return (BoneToRoot(index) * skin.bones[index].worldToLocalMatrix * original[socket].localToWorldMatrix).MultiplyPoint3x4(Vector3.zero);
        }
        private bool TryBindBounds(string bone, out Bounds bounds)
        {
            bounds = default; bool found = false;
            var vertices = skin.sharedMesh.vertices; var weights = skin.sharedMesh.boneWeights;
            if (vertices.Length != weights.Length) return false;
            var toRoot = root.worldToLocalMatrix * skin.transform.localToWorldMatrix;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (weights[i].boneIndex0 != indices[bone]) continue;
                var point = toRoot.MultiplyPoint3x4(vertices[i]);
                if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
            return found && bounds.size.x > 0 && bounds.size.y > 0 && bounds.size.z > 0;
        }
        private Transform Anchor(string key, string bone, Vector3 at, Vector3 scale)
            => Anchor(key, bone, at, scale, Quaternion.identity);
        private Transform Anchor(string key, string bone, Vector3 at, Vector3 scale, Quaternion rotation)
        {
            if (anchors.TryGetValue(key, out var cached)) return cached;
            int index = indices[bone];
            var local = BoneToRoot(index).inverse * Matrix4x4.TRS(at, rotation, scale);
            if (!Finite(local.determinant) || local.determinant <= .000001f) return null;
            var go = new GameObject("SpreadEquipment." + key); go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(skin.bones[index], false); go.transform.localPosition = local.GetColumn(3);
            go.transform.localRotation = local.rotation; go.transform.localScale = local.lossyScale;
            anchors.Add(key, go.transform); return go.transform;
        }
        public Transform Held(string side)
            => !Supported ? null : Anchor("Held." + side, "Hand." + side, SocketRest("Equipment.Hand." + side, "Hand." + side), Vector3.one);
        public Transform Worn(string slot, string side)
        {
            if (!Supported) return null;
            switch (slot)
            {
                case "Head":
                    var head = SocketRest("Equipment.Head", "Head");
                    if (!marlback) return Anchor("Head", "Head", head, Vector3.one);
                    // The broad low head has its own upper rim; the original
                    // socket retains vertical face clearance while width/depth
                    // follow the actual imported anatomy, not a humanoid scale.
                    head.x = headBounds.center.x; head.z = headBounds.center.z;
                    return Anchor("Head", "Head", head,
                        new Vector3((headBounds.size.x + BodySideClearance) / HeadFormWidth,
                            LowHeadHeightScale, (headBounds.size.z + .025f) / HeadFormDepth));
                case "Back": return Anchor("Back", "Spine", SocketRest("Equipment.Back", "Spine"), marlback ? new Vector3(1.65f,.55f,1) : Vector3.one);
                case "Body":
                    if (!marlback) return Anchor("Body", "Spine", new Vector3(0,.99f,0), Vector3.one);
                    // Rotate the shared open-neck cuirass onto the horizontal
                    // shale torso. Its decorated front becomes the upper plate;
                    // the lower body, muzzle and feet remain outside the shell.
                    var center = torsoBounds.center;
                    center.y = torsoBounds.max.y + BodyTopClearance - TorsoFrontPlane * LowBodyDepthScale;
                    return Anchor("Body", "Spine", center,
                        new Vector3((torsoBounds.size.x + BodySideClearance) / TorsoFormWidth,
                            (torsoBounds.size.z + BodyLengthClearance) / TorsoFormLength, LowBodyDepthScale),
                        Quaternion.Euler(90,0,0));
                case "Feet":
                    float sign = side == "L" ? -1 : 1;
                    return Anchor("Feet." + side, "Leg." + side, marlback ? new Vector3(sign*.29f,.08f,.13f) : new Vector3(sign*.17f,.10f,-.045f), Vector3.one);
                case "Handwear": return Anchor("Handwear." + side, "Hand." + side, SocketRest("Equipment.Hand." + side,"Hand." + side), Vector3.one);
                default: return null;
            }
        }
        public static bool HasHand(IReadOnlyList<BodyPart> parts, string side)
        {
            int flag = side == "L" ? Laterality.LEFT : Laterality.RIGHT;
            foreach (var part in parts) if (part.Type == "Hand" && (part.GetLaterality() & flag) != 0) return true;
            return false;
        }
        public bool Owns(Transform target)
        {
            if (!Supported || target == null || !target.IsChildOf(root)) return false;
            foreach (var anchor in anchors.Values) if (ReferenceEquals(anchor,target)) return true;
            return false;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public void Dispose()
        {
            foreach (var anchor in anchors.Values)
                if (anchor != null) { anchor.gameObject.SetActive(false); if (Application.isPlaying) Object.Destroy(anchor.gameObject); else Object.DestroyImmediate(anchor.gameObject); }
            anchors.Clear();
        }
    }
}
