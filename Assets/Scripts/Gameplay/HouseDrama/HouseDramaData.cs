using System;
using System.Collections.Generic;

namespace CavesOfOoo.Data
{
    // Valid NPC role strings and canonical end-state IDs, used by Validate().
    internal static class HouseDramaSchema
    {
        internal static readonly HashSet<string> ValidRoles = new HashSet<string>
        {
            "FoundationalDead", "LostDead", "DiminishedHead",
            "RisingInheritor", "NamedAntagonist", "SilencedHelper"
        };

        internal static readonly HashSet<string> DeadRoles = new HashSet<string>
        {
            "FoundationalDead", "LostDead"
        };

        internal static readonly HashSet<string> ValidEndStateIds = new HashSet<string>
        {
            "Restored", "TransformedA", "TransformedB", "Extinct", "Corrupted"
        };
    }


    // ─────────────────────────────────────────────────────────────────────────
    // Root wrapper — matches { "Dramas": [...] } in JSON.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class HouseDramaFileData
    {
        public List<HouseDramaData> Dramas = new List<HouseDramaData>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Top-level drama. One per family/house. ID must be unique across all files.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class HouseDramaData
    {
        public string ID;
        public string Name;
        public CraftIdentityData Craft;
        public RootConflictData RootConflict;
        public List<NpcRoleData> NpcRoles = new List<NpcRoleData>();
        public WitnessMapData WitnessMap;
        public List<PressurePointData> PressurePoints = new List<PressurePointData>();
        public List<MemorialActData> MemorialActs = new List<MemorialActData>();
        public List<CrossoverEdgeData> Crossovers = new List<CrossoverEdgeData>();
        public List<EndStateData> EndStates = new List<EndStateData>();
        public CorruptionGradientData CorruptionGradient;

        public NpcRoleData GetNpc(string npcId)
        {
            if (NpcRoles == null) return null;
            foreach (var npc in NpcRoles)
                if (npc.Id == npcId) return npc;
            return null;
        }

        public PressurePointData GetPressurePoint(string pointId)
        {
            if (PressurePoints == null) return null;
            foreach (var pp in PressurePoints)
                if (pp.Id == pointId) return pp;
            return null;
        }

        public PathData GetPath(string pointId, string pathId)
        {
            var pp = GetPressurePoint(pointId);
            if (pp?.Paths == null) return null;
            foreach (var path in pp.Paths)
                if (path.Id == pathId) return path;
            return null;
        }

        /// <summary>
        /// Validates this drama definition and returns a list of error strings.
        /// An empty list means the drama is well-formed. Does not throw.
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(ID))
                errors.Add("Drama must have a non-empty ID.");

            if (PressurePoints != null)
            {
                foreach (var pp in PressurePoints)
                {
                    if (pp.Paths == null || pp.Paths.Count == 0)
                        errors.Add($"PressurePoint '{pp.Id}' has no paths.");
                }
            }

            if (NpcRoles != null)
            {
                var memorialSubjects = new HashSet<string>();
                if (MemorialActs != null)
                    foreach (var act in MemorialActs)
                        if (!string.IsNullOrEmpty(act.SubjectId))
                            memorialSubjects.Add(act.SubjectId);

                foreach (var npc in NpcRoles)
                {
                    if (string.IsNullOrEmpty(npc.Role) ||
                        !HouseDramaSchema.ValidRoles.Contains(npc.Role))
                        errors.Add($"NPC '{npc.Id}' has unknown role '{npc.Role}'.");

                    if (!string.IsNullOrEmpty(npc.Role) &&
                        HouseDramaSchema.DeadRoles.Contains(npc.Role) &&
                        !memorialSubjects.Contains(npc.Id))
                        errors.Add($"NPC '{npc.Id}' (role: {npc.Role}) has no MemorialAct.");
                }
            }

            if (EndStates != null)
            {
                foreach (var es in EndStates)
                {
                    if (!string.IsNullOrEmpty(es.Id) &&
                        !HouseDramaSchema.ValidEndStateIds.Contains(es.Id))
                        errors.Add($"EndState has invalid ID '{es.Id}'.");
                }
            }

            return errors;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Craft Identity — defines the house's practice and its continuity threshold.
    // Extinct end-state must be derivable from FailureCondition.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class CraftIdentityData
    {
        public string Name;
        public string DistinctiveProperty;  // what makes this craft non-generic
        public string MasterMarker;         // observable sign of full mastery
        public int MinimumPractitioners;    // below this, craft fails within one cycle
        public int ActivePractitioners;
        public int EmergingPractitioners;
        public string CycleDuration;
        public string FailureCondition;     // derived: active < minimum for > 1 cycle
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Root Conflict
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class RootConflictData
    {
        public string Adversary;
        public string AdversaryProject;
        public string AdversaryAlignment;
        public string NamedAntagonistId;
        public string SecretTruth;
        public string DisclosureStakes;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NPC Role — one entry per named NPC in the drama.
    // Interiority fields are on every NPC; antagonist fields only on NamedAntagonist.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class NpcRoleData
    {
        public string Id;
        public string Role;     // FoundationalDead | LostDead | DiminishedHead |
                                // RisingInheritor | NamedAntagonist | SilencedHelper
        public string Variant;  // e.g. "strategic-compromised", "idealized-origin"
        public bool Alive;
        public int Age;

        // Interiority triad — drives dialogue generation and path cost derivation
        public string Wants;
        public string Fears;
        public string SelfDeception;
        public string ChangeCondition; // what breaks the NPC's frame

        // Antagonist profile — NamedAntagonist only
        public string AntagonistMotivation;
        public string AntagonistEmotionalStyle;
        public string AntagonistVulnerability;
        public string AntagonistLineWontCross;
        public string ChangedByCondition;
        public bool ChangedByReachable;      // false = drama can't produce enough evidence
        public string ChangedByEvidenceTier;

        // Blueprint override — which entity blueprint to spawn (optional)
        public string BlueprintOverride;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Witness Map — who knows what at drama start; dynamically updated at runtime.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class WitnessMapData
    {
        public List<WitnessEntryData> Entries = new List<WitnessEntryData>();
        public List<DangerousDisclosureData> DangerousDisclosures = new List<DangerousDisclosureData>();
    }

    [Serializable]
    public class WitnessEntryData
    {
        public string SubjectId;
        public List<string> Knows = new List<string>();
        public List<string> Suspects = new List<string>();
        public List<string> DoesNotKnow = new List<string>();
    }

    [Serializable]
    public class DangerousDisclosureData
    {
        public string Condition;    // "if X is revealed to Y before Z resolves"
        public string Consequence;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pressure Points — exactly 4 per drama, one per archetype.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class PressurePointData
    {
        public string Id;
        public string Archetype;            // Wound | PreventedHelp | SuccessionHinge | HiddenEvidence
        public string Temporal;             // Past | Suspended | Imminent | Latent
        public string DominantAlignment;    // Home | CosmicA | CosmicB | Variable

        // Urgency escalation — what forces a state transition if player delays
        public string UrgencyTrigger;
        public string UrgencyEffect;
        public string UrgencyTiming;        // immediate | short | medium | long

        // Activation state transition rules
        public List<TransitionRuleData> TransitionRules = new List<TransitionRuleData>();

        // Hidden evidence definition — only populated for HiddenEvidence archetype
        public HiddenEvidenceData HiddenEvidence;

        public List<PathData> Paths = new List<PathData>();

        public PathData GetPath(string pathId)
        {
            if (Paths == null) return null;
            foreach (var p in Paths)
                if (p.Id == pathId) return p;
            return null;
        }
    }

    [Serializable]
    public class TransitionRuleData
    {
        public string FromState;
        public string ToState;
        public string Condition;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hidden Evidence — only used by PressurePoints with Archetype = HiddenEvidence.
    // Subtypes are composable: e.g. ["Living", "Gated"]
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class HiddenEvidenceData
    {
        // Composable subtypes: Passive | Active | Decaying | Living | Gated
        public List<string> Subtypes = new List<string>();
        public string CustodianId;
        public string MaintenanceCycle;     // Living: how often tending is required
        public string LossCondition;        // Living: what causes permanent loss
        public string GateCondition;        // Gated: who controls access and how
        public List<RevealMethodData> RevealMethods = new List<RevealMethodData>();

        public bool HasSubtype(string subtype)
        {
            if (Subtypes == null) return false;
            foreach (var s in Subtypes)
                if (s == subtype) return true;
            return false;
        }
    }

    [Serializable]
    public class RevealMethodData
    {
        public string Alignment;
        public string Method;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Paths — resolution options per pressure point.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class PathData
    {
        public string Id;
        public string PrimaryAlignment;
        public string SecondaryAlignment;   // optional; for paths pulling in two directions

        public List<CostData> Costs = new List<CostData>();

        // Emotional cost — used to compute the corruption gradient at runtime.
        // If computed least-resistance-to-Corrupted doesn't match intended texture,
        // fix this value — not the gradient descriptor.
        public string EmotionalCostDescription;
        public int EmotionalCostMagnitude;  // 1–5

        public int CorruptionContribution;  // 0–5; summed to find least-resistance path
        public List<EndStateContributionData> EndStateContributions = new List<EndStateContributionData>();
    }

    [Serializable]
    public class CostData
    {
        public string Type;     // SelfEdit | Ideology | Time | Trust | LineagePurity | Violence
        public int Magnitude;
        public string Timing;   // front-loaded | back-loaded | ongoing
    }

    [Serializable]
    public class EndStateContributionData
    {
        public string EndStateId;
        public int Weight;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Memorial Acts — interaction points with the drama's dead.
    // Required for every FoundationalDead and LostDead NPC.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class MemorialActData
    {
        public string SubjectId;
        public List<string> Objects = new List<string>();
        public List<MemorialInteractionData> Interactions = new List<MemorialInteractionData>();
    }

    [Serializable]
    public class MemorialInteractionData
    {
        public string Alignment;
        public string Description;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Crossover Edges — state propagation between pressure points.
    // DerivedFromWitnessMap = true means edge is derivable from knowledge state;
    // false means hand-authored and must be maintained manually.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class CrossoverEdgeData
    {
        public string Type;             // ActorDoubling | TruthPropagation | Stabilization |
                                        // Amplification | Decay | AntagonistReveal | MutualExclusion
        public string FromPointId;
        public string FromCondition;    // format: "state:resolved" or "state:active"
        public string ToPointId;
        public string ToEffect;         // format: "state:PointID:NewState" |
                                        //         "reveal:NpcID:FactID" |
                                        //         "corruption:Amount" |
                                        //         "close:PointID:PathID"
        public bool DerivedFromWitnessMap;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // End States — exactly 5: Restored, TransformedA, TransformedB, Extinct, Corrupted.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class EndStateData
    {
        public string Id;   // Restored | TransformedA | TransformedB | Extinct | Corrupted
        public string Name;
        public List<string> PathSignature = new List<string>(); // path patterns that produce this
        public string Tag;                          // player-facing achievement tag
        public List<string> Rewards = new List<string>();
        public string ExternalResonanceDescription;
        public string ExternalResonanceScope;       // house-local | regional | basin-wide | cosmic
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Corruption Gradient — earlyWarningSignal and pointOfNoReturn are authored.
    // leastResistancePath is computed at runtime from Cost + EmotionalCost sums.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class CorruptionGradientData
    {
        public string EarlyWarningSignal;   // what player observes approaching Corrupted
        public string PointOfNoReturn;      // path/event that closes Restored/Transformed
    }
}
