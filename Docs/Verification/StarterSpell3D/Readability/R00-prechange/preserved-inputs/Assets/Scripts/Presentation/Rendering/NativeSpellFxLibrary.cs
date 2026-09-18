using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public enum NativeSpellFxRole { SourceGather, ProjectileHead, ProjectileTrail, TargetImpact, GroundCell, ConeCell, CropCell, ReactionCell }
    public enum NativeSpellFxAnchor { Source, ProjectileCarrier, Target, Cell }
    public enum NativeSpellFxCondition { Always, FrozenApplied, PacifiedApplied, Rejected, Resisted, Died, FreezeWater }

    [Serializable] public struct NativeSpellFxPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    [Serializable] public sealed class NativeSpellFxPiece
    {
        public string Id;
        public Mesh Mesh;
        public Color Color;
        public NativeSpellFxRole Role;
        public NativeSpellFxAnchor Anchor;
        public NativeSpellFxCondition Condition;
        public int ForwardCell, LateralCell, Variant;
        public bool ReducedEssential = true;
        public NativeSpellFxPose[] Poses;
    }

    [Serializable] public sealed class NativeSpellCastBinding
    {
        public string RigPath;
        public AnimationClip Clip;
    }

    [Serializable] public sealed class NativeSpellFxEntry
    {
        public string SpellId;
        public AnimationClip CastClip;
        public NativeSpellCastBinding[] CastBindings;
        public float CastDuration = .66f;
        public float SampleRate = 100f;
        public float ReleaseFrame = 22f;
        public float StudyContactFrame;
        public float StudyClearFrame;
        public float AuthoredDistanceCells;
        public float LaunchDistance = .36f;
        public float[] ProjectileProgress;
        public NativeSpellFxPiece[] Pieces;

        /// <summary>The source motion is baked against each actual native rig path.
        /// Unrigged props and animals keep their existing presentation fallback.</summary>
        public AnimationClip FindCastClip(Animator animator)
        {
            if (animator == null) return null;
            if (CastBindings != null)
                for (int i = 0; i < CastBindings.Length; i++)
                {
                    var binding = CastBindings[i];
                    if (binding != null && !string.IsNullOrEmpty(binding.RigPath) && binding.Clip != null
                        && animator.transform.Find(binding.RigPath) != null) return binding.Clip;
                }
            return animator.transform.Find("character-teal/character-teal__Rig") != null ? CastClip : null;
        }
    }

    /// <summary>Borrowed, immutable Blender mesh and transform tracks. Validation is
    /// performed once before lookup is published; playback never mutates these assets.</summary>
    [CreateAssetMenu(menuName = "Caves of Ooo/Native Spell FX Library")]
    public sealed class NativeSpellFxLibrary : ScriptableObject
    {
        public const string ResourcePath = "SpellFx3D/Library";
        public const int SampleCount = 111;
        public Material Material;
        public NativeSpellFxEntry[] Entries;
        private Dictionary<string, NativeSpellFxEntry> _entries;
        internal bool IsValidated => _entries != null;

        public void Validate()
        {
            _entries = null;
            Require(Material != null && Material.shader != null && Material.shader.name == "CavesOfOoo/Village3D/Palette"
                && Material.HasProperty("_FogLight") && Material.HasProperty("_Transient")
                && Material.HasProperty("_BaseColor"), "A fog-aware palette material is required.");
            Require(Entries != null && Entries.Length > 0, "No authored spell entries.");
            var entries = new Dictionary<string, NativeSpellFxEntry>(StringComparer.Ordinal);
            var meshes = new HashSet<Mesh>();
            foreach (var entry in Entries)
            {
                Require(entry != null && !string.IsNullOrWhiteSpace(entry.SpellId), "A spell ID is required.");
                Require(!entries.ContainsKey(entry.SpellId), "Duplicate spell: " + entry.SpellId);
                Require(entry.CastClip != null && Finite(entry.CastDuration) && entry.CastDuration > 0
                    && entry.CastDuration <= WorldFxPlayback.HardTimeoutSeconds && entry.SampleRate == 100f,
                    "Invalid cast/100-fps clock: " + entry.SpellId);
                Require(Finite(entry.ReleaseFrame) && Finite(entry.StudyContactFrame) && Finite(entry.StudyClearFrame)
                    && entry.ReleaseFrame >= 0 && entry.StudyContactFrame >= entry.ReleaseFrame
                    && entry.StudyClearFrame > entry.StudyContactFrame && entry.StudyClearFrame < SampleCount,
                    "Invalid phase markers: " + entry.SpellId);
                Require(Finite(entry.AuthoredDistanceCells) && entry.AuthoredDistanceCells >= 0
                    && Finite(entry.LaunchDistance) && entry.LaunchDistance >= 0, "Invalid authored range.");
                Require(entry.Pieces != null && entry.Pieces.Length > 0, "Missing authored pieces.");
                if (entry.ProjectileProgress != null && entry.ProjectileProgress.Length > 0)
                {
                    Require(entry.ProjectileProgress.Length == SampleCount, "Invalid carrier sample count.");
                    foreach (float value in entry.ProjectileProgress) Require(Finite(value), "Invalid carrier progress.");
                }
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var piece in entry.Pieces)
                {
                    Require(piece != null && !string.IsNullOrWhiteSpace(piece.Id) && ids.Add(piece.Id), "Missing/duplicate piece ID.");
                    Require(piece.Mesh != null && piece.Mesh.vertexCount > 0, "Missing authored mesh: " + piece.Id);
                    Require(Finite(piece.Color.r) && Finite(piece.Color.g) && Finite(piece.Color.b)
                        && Finite(piece.Color.a) && piece.Color.a > 0, "Invalid color: " + piece.Id);
                    Require(Enum.IsDefined(typeof(NativeSpellFxRole), piece.Role)
                        && Enum.IsDefined(typeof(NativeSpellFxAnchor), piece.Anchor)
                        && Enum.IsDefined(typeof(NativeSpellFxCondition), piece.Condition), "Unknown piece semantics.");
                    Require(piece.Anchor == AnchorFor(piece.Role), "Incompatible role/anchor: " + piece.Id);
                    Require(piece.Poses != null && piece.Poses.Length == SampleCount, "Invalid pose sample count: " + piece.Id);
                    foreach (var pose in piece.Poses)
                    {
                        float norm = Quaternion.Dot(pose.Rotation, pose.Rotation);
                        Require(Finite(pose.Position) && Finite(pose.Scale) && pose.Scale.x >= 0 && pose.Scale.y >= 0
                            && pose.Scale.z >= 0 && Finite(norm) && Mathf.Abs(norm - 1f) < .01f,
                            "Invalid sampled transform: " + piece.Id);
                    }
                    if (piece.Role == NativeSpellFxRole.ProjectileHead)
                        Require(entry.AuthoredDistanceCells > 0 && entry.ProjectileProgress != null
                            && entry.ProjectileProgress.Length == SampleCount, "Missing carrier track: " + piece.Id);
                    if (piece.Role == NativeSpellFxRole.ProjectileTrail)
                        Require(entry.AuthoredDistanceCells > 0, "Missing trail range: " + piece.Id);
                    if (meshes.Add(piece.Mesh))
                    {
                        Require(piece.Mesh.isReadable, "Authored mesh is not readable: " + piece.Id);
                        foreach (var vertex in piece.Mesh.vertices) Require(Finite(vertex), "Nonfinite mesh vertex.");
                        var triangles = piece.Mesh.triangles;
                        Require(triangles.Length >= 3 && triangles.Length % 3 == 0, "Missing mesh triangles.");
                        foreach (int index in triangles) Require(index >= 0 && index < piece.Mesh.vertexCount, "Invalid triangle index.");
                    }
                }
                entries.Add(entry.SpellId, entry);
            }
            _entries = entries;
        }

        public void InvalidateCaches() => _entries = null;
        private void OnValidate() => InvalidateCaches();
        public NativeSpellFxEntry Find(string spellId)
        {
            if (_entries == null) Validate();
            return spellId != null && _entries.TryGetValue(spellId, out var entry) ? entry : null;
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static NativeSpellFxAnchor AnchorFor(NativeSpellFxRole role)
        {
            switch (role)
            {
                case NativeSpellFxRole.SourceGather:
                case NativeSpellFxRole.ProjectileTrail: return NativeSpellFxAnchor.Source;
                case NativeSpellFxRole.ProjectileHead: return NativeSpellFxAnchor.ProjectileCarrier;
                case NativeSpellFxRole.TargetImpact: return NativeSpellFxAnchor.Target;
                default: return NativeSpellFxAnchor.Cell;
            }
        }
        private static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException("Native spell library: " + reason); }
    }
}
