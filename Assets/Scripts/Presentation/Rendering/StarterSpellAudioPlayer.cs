using System;
using CavesOfOoo.Core;
using Unity.Profiling;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Prepared, bounded playback of the approved recorded-material starter sounds.
    /// Prefixes follow the cast; suffixes follow the chosen renderer's contact clock and copied outcomes.
    /// Audio tails never own a gameplay wait or query a live target.</summary>
    public sealed class StarterSpellAudioPlayer : IDisposable
    {
        const int Capacity = 4, LayersPerVoice = 4, SourcesPerVoice = 8;
        const float MaximumContact = 5f, HardTimeout = 16f;
        const double ScheduleLead = .015;
        const string ResourceRoot = "Audio/StarterSpells/";
        static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("COO.StarterSpellAudio.Update");

        [Serializable] sealed class Catalog
        {
            public int schemaVersion, sampleRate;
            public float commonGain;
            public Definition[] spells;
        }
        [Serializable] sealed class Definition
        {
            public string spellId;
            public Variant[] variants;
        }
        [Serializable] sealed class Variant
        {
            public int variant;
            public Layer[] layers;
        }
        [Serializable] sealed class Layer
        {
            public string name, gate;
            public Clip prefix, suffix;
        }
        [Serializable] sealed class Clip
        {
            public string resourceBase;
            public int samples;
            [NonSerialized] public AudioClip audio;
        }
        sealed class Voice
        {
            public readonly AudioSource[] Sources = new AudioSource[SourcesPerVoice];
            public SpellFxSequence Sequence;
            public Layer[] Layers;
            public Point Endpoint;
            public bool Native, Handled;
            public float Age, Wall, PrefixAge, SuffixAge, Contact, PrefixDuration, SuffixDuration;
        }
        readonly Voice[] voices = new Voice[Capacity];
        readonly Transform parent;
        Catalog catalog;
        Zone zone;
        int mixVoiceCount;
        bool disposed, loadFailed;
        public Transform Root { get; private set; }
        public int ActiveVoices { get; private set; }
        public int LastVariant { get; private set; }
        public int AcceptedCount { get; private set; }
        /// <summary>Cumulative eligible contact layers started, not number of targets or particles.</summary>
        public int ContactLayerCount { get; private set; }
        public int AllocatedSources => Root == null ? 0 : Capacity * SourcesPerVoice;
        public bool IsPrepared
        {
            get
            {
                if (disposed || Root == null || catalog == null) return false;
                for (int i = 0; i < Capacity; i++)
                    for (int k = 0; k < SourcesPerVoice; k++)
                        if (voices[i]?.Sources[k] == null) return false;
                return true;
            }
        }
        public StarterSpellAudioPlayer(Transform parent = null) { this.parent = parent; }
        public void SetZone(Zone value)
        {
            if (disposed) return;
            if (!ReferenceEquals(zone, value)) ClearAll();
            zone = value;
            if (zone != null) Prepare();
        }
        /// <summary>Load the catalog and all approved variants at zone binding; allocate the fixed pool once.</summary>
        public bool Prepare()
        {
            if (IsPrepared) return true;
            if (disposed || loadFailed || zone == null) return false;
            ClearAll(); DestroyRoot();
            if (catalog == null && !LoadCatalog()) { loadFailed = true; return false; }
            var go = new GameObject("Starter Spell Audio") { hideFlags = HideFlags.DontSave };
            Root = go.transform; Root.SetParent(parent, false);
            for (int i = 0; i < Capacity; i++)
            {
                voices[i] = new Voice();
                for (int k = 0; k < SourcesPerVoice; k++)
                {
                    var child = new GameObject("Starter " + i + " layer " + k) { hideFlags = HideFlags.DontSave };
                    child.transform.SetParent(Root, false);
                    var source = child.AddComponent<AudioSource>();
                    source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
                    source.dopplerLevel = 0; source.reverbZoneMix = 0;
                    voices[i].Sources[k] = source;
                }
            }
            return true;
        }
        bool LoadCatalog()
        {
            var text = Resources.Load<TextAsset>(ResourceRoot + "catalog");
            if (text == null) return false;
            var candidate = JsonUtility.FromJson<Catalog>(text.text);
            if (candidate == null || candidate.schemaVersion != 1 || candidate.sampleRate != 48000 ||
                !float.IsFinite(candidate.commonGain) || candidate.commonGain <= 0 || candidate.commonGain > 1 ||
                candidate.spells == null || candidate.spells.Length != 6) return false;
            for (int s = 0; s < candidate.spells.Length; s++)
            {
                var definition = candidate.spells[s];
                if (definition == null || string.IsNullOrEmpty(definition.spellId) ||
                    definition.variants == null || definition.variants.Length != 3) return false;
                for (int v = 0; v < 3; v++)
                {
                    var variant = definition.variants[v];
                    if (variant == null || variant.variant != v + 1 || variant.layers == null ||
                        variant.layers.Length < 1 || variant.layers.Length > LayersPerVoice) return false;
                    for (int k = 0; k < variant.layers.Length; k++)
                    {
                        var layer = variant.layers[k];
                        if (layer == null || !KnownGate(layer.gate) || !LoadClip(layer.prefix, true) ||
                            !LoadClip(layer.suffix, true) || (layer.prefix?.audio == null && layer.suffix?.audio == null) ||
                            (layer.gate != "always" && layer.prefix?.audio != null)) return false;
                    }
                }
            }
            catalog = candidate;
            return true;
        }
        static bool KnownGate(string gate) => gate == "always" || gate == "target" || gate == "moved" ||
            gate == "frozen" || gate == "pacified" || gate == "watered";
        static bool LoadClip(Clip clip, bool optional)
        {
            if (clip == null || string.IsNullOrEmpty(clip.resourceBase)) return optional;
            clip.audio = Resources.Load<AudioClip>(ResourceRoot + clip.resourceBase);
            return clip.audio != null && clip.audio.channels == 1 && clip.audio.frequency == 48000 &&
                clip.samples > 0 && clip.audio.samples == clip.samples &&
                (clip.audio.LoadAudioData() || clip.audio.loadState == AudioDataLoadState.Loaded);
        }
        /// <summary>Contact seconds use the selected visual backend's clock, including native hitch recovery.</summary>
        public bool Play(SpellFxSequence sequence, float contactSeconds, bool nativeClock)
        {
            if (disposed || sequence == null || zone == null || sequence.Zone != zone ||
                zone.GetCell(sequence.Source.X, sequence.Source.Y) == null || !AnyVisible(sequence) ||
                !float.IsFinite(contactSeconds) || contactSeconds < 0 || contactSeconds >= MaximumContact ||
                SpellFxSettings.Mode == SpellFxMode.Off || SpellFxSettings.SoundVolume <= 0 || AudioListener.pause ||
                !Prepare()) return false;
            Definition definition = null;
            for (int i = 0; i < catalog.spells.Length; i++)
                if (catalog.spells[i].spellId == sequence.SpellId) { definition = catalog.spells[i]; break; }
            if (definition == null || (sequence.SpellId == "Hydromancy_ConjureRain" && !HasWateredTarget(sequence, false)))
                return false;
            Voice free = null;
            for (int i = 0; i < Capacity; i++)
            {
                if (ReferenceEquals(voices[i].Sequence, sequence)) return false;
                if (voices[i].Sequence == null && free == null) free = voices[i];
            }
            if (free == null) return false;
            int variantIndex = (int)((uint)sequence.CosmeticSeed % 3);
            free.Sequence = sequence; free.Layers = definition.variants[variantIndex].layers;
            free.Endpoint = Endpoint(sequence); free.Native = nativeClock; free.Contact = contactSeconds;
            free.Handled = false; free.Age = free.Wall = free.PrefixAge = free.SuffixAge = 0;
            free.PrefixDuration = free.SuffixDuration = 0;
            LastVariant = variantIndex + 1; ActiveVoices++; AcceptedCount++;
            RefreshMix();
            double start = AudioSettings.dspTime + ScheduleLead;
            for (int k = 0; k < free.Layers.Length; k++)
            {
                var clip = free.Layers[k].prefix?.audio;
                if (clip == null) continue;
                Start(free.Sources[k], clip, start);
                free.PrefixDuration = Mathf.Max(free.PrefixDuration, clip.length);
            }
            return true;
        }
        public void Update(float wallDelta, float normalDelta, float nativeDelta)
        {
            using (UpdateMarker.Auto()) Tick(wallDelta, normalDelta, nativeDelta);
        }
        void Tick(float wallDelta, float normalDelta, float nativeDelta)
        {
            if (disposed) return;
            if (!IsPrepared || SpellFxSettings.Mode == SpellFxMode.Off || SpellFxSettings.SoundVolume <= 0 || AudioListener.pause)
            { ClearAll(); return; }
            wallDelta = WorldFxPlayback.SanitizeDelta(wallDelta);
            normalDelta = WorldFxPlayback.SanitizeDelta(normalDelta); nativeDelta = WorldFxPlayback.SanitizeDelta(nativeDelta);
            float pitch = Mathf.Min(3, SpellFxSettings.AnimationSpeed);
            for (int i = 0; i < Capacity; i++)
            {
                var voice = voices[i]; if (voice.Sequence == null) continue;
                voice.Wall += wallDelta;
                if (voice.Wall >= HardTimeout) { Stop(voice); continue; }
                voice.Age += voice.Native ? nativeDelta : normalDelta;
                voice.PrefixAge += wallDelta * pitch;
                if (voice.Handled) voice.SuffixAge += wallDelta * pitch;
                if (!voice.Handled && voice.Age >= voice.Contact)
                {
                    voice.Handled = true; voice.SuffixAge = 0;
                    double start = AudioSettings.dspTime + ScheduleLead;
                    for (int k = 0; k < voice.Layers.Length; k++)
                    {
                        var layer = voice.Layers[k];
                        if (layer.suffix?.audio == null || !Gate(voice, layer.gate)) continue;
                        Start(voice.Sources[LayersPerVoice + k], layer.suffix.audio, start);
                        voice.SuffixDuration = Mathf.Max(voice.SuffixDuration, layer.suffix.audio.length);
                        ContactLayerCount++;
                    }
                }
                if (voice.Handled && voice.PrefixAge >= voice.PrefixDuration + ScheduleLead * pitch &&
                    (voice.SuffixDuration <= 0 || voice.SuffixAge >= voice.SuffixDuration + ScheduleLead * pitch)) Stop(voice);
            }
            RefreshMix();
        }
        bool Gate(Voice voice, string gate)
        {
            var sequence = voice.Sequence;
            if (gate == "always") return AnyVisible(sequence);
            if (gate == "watered") return HasWateredTarget(sequence, true);
            for (int i = 0; i < sequence.Targets.Count; i++)
            {
                var target = sequence.Targets[i];
                if (target == null || !Visible(target.Cell)) continue;
                // The capture distinguishes selected targets from unrelated reactions resolved in the same scope.
                // Reconstructing a cone here would duplicate wall/diagonal/footprint rules and leak ambient outcomes.
                if (gate == "target" && target.IsDirectTarget && In(sequence.AffectedCells, target.Cell)) return true;
                if (gate == "moved" && target.IsDirectTarget && OnPath(sequence, target.Cell) && target.Moved && Visible(target.FinalCell)) return true;
                if (!target.Died && target.Cell.Equals(voice.Endpoint))
                {
                    if (gate == "frozen" && HasEffect(target, "FrozenEffect")) return true;
                    if (gate == "pacified" && HasEffect(target, "Pacified")) return true;
                }
            }
            if (gate == "frozen")
                for (int i = 0; i < sequence.Reactions.Count; i++)
                {
                    var reaction = sequence.Reactions[i];
                    if (reaction != null && reaction.Kind == "reaction" && reaction.Value == "freeze_water" &&
                        reaction.Amount > 0 && Visible(reaction.Cell) &&
                        (OnPath(sequence, reaction.Cell) || SelectedAnchor(voice, reaction.Cell))) return true;
                }
            return false;
        }
        bool SelectedAnchor(Voice voice, Point cell)
        {
            for (int i = 0; i < voice.Sequence.Targets.Count; i++)
            {
                var target = voice.Sequence.Targets[i];
                if (target != null && target.IsDirectTarget && target.Cell.Equals(voice.Endpoint) &&
                    Visible(target.Cell) && target.AnchorCell.Equals(cell)) return true;
            }
            return false;
        }
        bool HasWateredTarget(SpellFxSequence sequence, bool requireVisible)
        {
            for (int i = 0; i < sequence.Targets.Count; i++)
            {
                var target = sequence.Targets[i];
                // Rain capture records a crop immediately before Water(40), including idempotent top-ups.
                if (target != null && !target.Died && zone.InBounds(target.Cell.X, target.Cell.Y) &&
                    Near(sequence.Source, target.Cell, 3) && In(sequence.AffectedCells, target.Cell) &&
                    (!requireVisible || Visible(target.Cell))) return true;
            }
            return false;
        }
        static bool HasEffect(SpellFxTargetResult target, string name)
        {
            for (int i = 0; i < target.AppliedEffects.Count; i++) if (target.AppliedEffects[i] == name) return true;
            return false;
        }
        static bool Near(Point a, Point b, int distance) => Math.Abs((long)a.X - b.X) <= distance && Math.Abs((long)a.Y - b.Y) <= distance;
        static bool In(System.Collections.Generic.IReadOnlyList<Point> points, Point point)
        {
            for (int i = 0; i < points.Count; i++) if (points[i].Equals(point)) return true;
            return false;
        }
        bool OnPath(SpellFxSequence sequence, Point point) => zone.InBounds(point.X, point.Y) &&
            !point.Equals(sequence.Source) && In(sequence.Path, point);
        Point Endpoint(SpellFxSequence sequence)
        {
            var result = new Point(-1, -1);
            for (int i = 0; i < sequence.Path.Count; i++)
            {
                var point = sequence.Path[i];
                if (!zone.InBounds(point.X, point.Y) || point.Equals(sequence.Source)) continue;
                bool duplicate = false;
                for (int j = 0; j < i; j++) if (sequence.Path[j].Equals(point)) { duplicate = true; break; }
                if (!duplicate) result = point;
            }
            return result;
        }
        bool Visible(Point point)
        { var cell = zone?.GetCell(point.X, point.Y); return cell != null && cell.Explored && cell.IsVisible; }
        bool AnyVisible(SpellFxSequence sequence)
        {
            if (Visible(sequence.Source)) return true;
            for (int i = 0; i < sequence.Path.Count; i++) if (Visible(sequence.Path[i])) return true;
            for (int i = 0; i < sequence.AffectedCells.Count; i++) if (Visible(sequence.AffectedCells[i])) return true;
            for (int i = 0; i < sequence.Targets.Count; i++) if (sequence.Targets[i] != null && Visible(sequence.Targets[i].Cell)) return true;
            return false;
        }
        static void Start(AudioSource source, AudioClip clip, double start)
        { source.clip = clip; if (Application.isPlaying) source.PlayScheduled(start); }
        /// <summary>The coordinator supplies the combined active voice count, including Ember, for common headroom.</summary>
        public void SetMixVoiceCount(int total) { mixVoiceCount = Math.Max(0, total); RefreshMix(); }
        void RefreshMix()
        {
            float volume = SpellFxSettings.SoundVolume * (catalog?.commonGain ?? 1f) / Math.Max(1, Math.Max(ActiveVoices, mixVoiceCount));
            float pitch = Mathf.Min(3, SpellFxSettings.AnimationSpeed);
            for (int i = 0; i < Capacity; i++)
                if (voices[i]?.Sequence != null)
                    for (int k = 0; k < SourcesPerVoice; k++)
                        if (voices[i].Sources[k] != null) { voices[i].Sources[k].volume = volume; voices[i].Sources[k].pitch = pitch; }
        }
        void Stop(Voice voice)
        {
            if (voice?.Sequence == null) return;
            for (int k = 0; k < SourcesPerVoice; k++)
                if (voice.Sources[k] != null)
                { if (Application.isPlaying) voice.Sources[k].Stop(); voice.Sources[k].clip = null; }
            voice.Sequence = null; voice.Layers = null; ActiveVoices--;
        }
        public void ClearAll() { for (int i = 0; i < Capacity; i++) Stop(voices[i]); }
        public void CancelNative()
        { for (int i = 0; i < Capacity; i++) if (voices[i] != null && voices[i].Native) Stop(voices[i]); RefreshMix(); }
        void DestroyRoot()
        {
            if (Root != null)
            { if (Application.isPlaying) UnityEngine.Object.Destroy(Root.gameObject); else UnityEngine.Object.DestroyImmediate(Root.gameObject); }
            Root = null;
        }
        public void Dispose() { if (disposed) return; ClearAll(); DestroyRoot(); disposed = true; }
    }
}
