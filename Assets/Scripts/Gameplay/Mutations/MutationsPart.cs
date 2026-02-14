using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Container Part that manages an entity's mutations.
    /// Mirrors Qud's Mutations part: maintains a list of BaseMutation instances,
    /// handles adding/removing mutations with lifecycle callbacks,
    /// and can parse a StartingMutations string to auto-grant mutations on Initialize.
    /// </summary>
    public class MutationsPart : Part
    {
        public class RandomMutationBuyOption
        {
            public MutationDefinition Mutation;
            public bool GrantsChimericBodyPart;
        }

        public override string Name => "Mutations";
        private bool _syncing;
        private bool _restartSync;

        /// <summary>
        /// All active mutations on this entity.
        /// </summary>
        public List<BaseMutation> MutationList = new List<BaseMutation>();

        /// <summary>
        /// Temporary/permanent mutation level modifiers from effects, gear, etc.
        /// </summary>
        public List<MutationModifierTracker> MutationMods = new List<MutationModifierTracker>();

        /// <summary>
        /// Mutation-created equipment references for cleanup/remutate seams.
        /// </summary>
        public List<MutationGeneratedEquipmentTracker> MutationGeneratedEquipment =
            new List<MutationGeneratedEquipmentTracker>();

        /// <summary>
        /// Comma-separated string of starting mutations in format "ClassName:Level,ClassName:Level".
        /// Parsed during Initialize() to auto-grant mutations.
        /// Set via blueprint params.
        /// </summary>
        public string StartingMutations = "";

        /// <summary>
        /// Parse StartingMutations and grant them.
        /// Called automatically when the part is added to an entity.
        /// </summary>
        public override void Initialize()
        {
            if (string.IsNullOrEmpty(StartingMutations))
                return;

            string[] entries = StartingMutations.Split(',');
            foreach (string entry in entries)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string className = trimmed;
                int level = 1;

                int colon = trimmed.IndexOf(':');
                if (colon >= 0)
                {
                    className = trimmed.Substring(0, colon);
                    int.TryParse(trimmed.Substring(colon + 1), out level);
                    if (level < 1) level = 1;
                }

                // Create mutation instance by class name via reflection
                BaseMutation mutation = CreateMutationByName(className);
                if (mutation != null)
                {
                    AddMutation(mutation, level);
                }
            }
        }

        /// <summary>
        /// Add a mutation to this entity at the given level.
        /// Attaches the mutation as a Part, calls Mutate lifecycle.
        /// </summary>
        public bool AddMutation(BaseMutation mutation, int level = 1)
        {
            if (mutation == null) return false;
            if (level < 1) level = 1;

            // Ranked duplicates are handled as rank increases instead of duplicate parts.
            for (int i = 0; i < MutationList.Count; i++)
            {
                BaseMutation existing = MutationList[i];
                if (existing.GetType() != mutation.GetType())
                    continue;

                if (MutationRegistry.TryGetByClassName(existing.GetType().Name, out MutationDefinition definition) &&
                    definition.Ranked &&
                    existing is IRankedMutation ranked)
                {
                    ranked.AdjustRank(1);
                    existing.ChangeLevel(existing.Level);
                    SyncMutationLevels();
                    return true;
                }

                return false;
            }

            // Reuse an existing mutation part instance if one is already attached
            // (e.g. it was added temporarily by MutationMods).
            BaseMutation attached = FindAttachedMutationByClass(mutation.GetType().Name);
            if (attached != null)
            {
                mutation = attached;
                mutation.Unmutate(ParentEntity);
            }
            else
            {
                // Add as a Part on the entity (gives it ParentEntity, Initialize, HandleEvent)
                ParentEntity.AddPart(mutation);
            }

            if (!MutationList.Contains(mutation))
                MutationList.Add(mutation);

            // Call mutation lifecycle
            mutation.Mutate(ParentEntity, level);
            SyncMutationLevels();

            return true;
        }

        /// <summary>
        /// Remove a mutation from this entity.
        /// Calls Unmutate lifecycle and removes the Part.
        /// </summary>
        public bool RemoveMutation(BaseMutation mutation)
        {
            if (mutation == null) return false;

            if (!MutationList.Remove(mutation))
                return false;

            bool hasExternalLevels = MutationMods.Exists(m =>
                string.Equals(m.MutationClassName, mutation.GetType().Name, StringComparison.OrdinalIgnoreCase));

            if (hasExternalLevels)
            {
                // Mirror Qud behavior: remove inherent ownership but keep the mutation part
                // alive if temporary modifiers still reference it.
                mutation.BaseLevel = 0;
                mutation.ChangeLevel(mutation.Level);
                SyncMutationLevels();
                return true;
            }

            CleanupMutationGeneratedEquipment(mutation, force: false);

            // Call mutation lifecycle
            mutation.Unmutate(ParentEntity);

            // Remove as a Part
            ParentEntity.RemovePart(mutation);
            SyncMutationLevels();

            return true;
        }

        /// <summary>
        /// Set a mutation's base level and trigger its level-change lifecycle.
        /// </summary>
        public bool LevelMutation(BaseMutation mutation, int level)
        {
            if (mutation == null) return false;
            if (!MutationList.Contains(mutation)) return false;

            if (level < 0) level = 0;
            mutation.BaseLevel = level;
            mutation.ChangeLevel(mutation.Level);
            SyncMutationLevels();
            return true;
        }

        /// <summary>
        /// Spend 1 MP to increase a mutation's base rank by 1.
        /// Uses BaseMutation.CanIncreaseLevel() for cap and leveling eligibility checks.
        /// </summary>
        public bool SpendMPToIncreaseMutation(BaseMutation mutation, string context = "default")
        {
            if (mutation == null)
                return false;
            if (ParentEntity == null)
                return false;
            if (!MutationList.Contains(mutation))
                return false;
            if (!mutation.CanIncreaseLevel())
                return false;
            if (ParentEntity.GetStatValue("MP", 0) < 1)
                return false;
            if (!LevelMutation(mutation, mutation.BaseLevel + 1))
                return false;

            return ParentEntity.UseMP(1, context);
        }

        public bool SpendMPToIncreaseMutation(string className, string context = "default")
        {
            BaseMutation mutation = GetMutation(className);
            if (mutation == null)
                return false;

            return SpendMPToIncreaseMutation(mutation, context);
        }

        /// <summary>
        /// Register mutation-generated equipment and optionally auto-equip it.
        /// </summary>
        public Guid RegisterMutationGeneratedEquipment(
            BaseMutation mutation,
            Entity item,
            bool autoEquip = true,
            bool autoRemoveOnMutationLoss = true)
        {
            if (mutation == null || item == null || ParentEntity == null)
                return Guid.Empty;

            string className = mutation.GetType().Name;
            for (int i = 0; i < MutationGeneratedEquipment.Count; i++)
            {
                MutationGeneratedEquipmentTracker existing = MutationGeneratedEquipment[i];
                if (existing.Item == item &&
                    string.Equals(existing.MutationClassName, className, StringComparison.OrdinalIgnoreCase))
                {
                    return existing.ID;
                }
            }

            TryDetachFromCurrentOwner(item);

            MutationGeneratedEquipmentTracker tracker = new MutationGeneratedEquipmentTracker
            {
                MutationClassName = className,
                Item = item,
                AutoEquip = autoEquip,
                AutoRemoveOnMutationLoss = autoRemoveOnMutationLoss
            };
            MutationGeneratedEquipment.Add(tracker);

            InventoryPart inventory = ParentEntity.GetPart<InventoryPart>();
            if (inventory != null)
            {
                bool inInventory = inventory.Contains(item) || inventory.AddObject(item);

                if (autoEquip &&
                    inInventory &&
                    item.GetPart<EquippablePart>() != null &&
                    FindEquippedSlot(inventory, item) == null)
                {
                    InventorySystem.Equip(ParentEntity, item);
                }
            }

            return tracker.ID;
        }

        public int CleanupMutationGeneratedEquipment(BaseMutation mutation, bool force = false)
        {
            if (mutation == null)
                return 0;
            return CleanupMutationGeneratedEquipment(mutation.GetType().Name, force);
        }

        public int CleanupMutationGeneratedEquipment(string mutationClassName, bool force = false)
        {
            if (string.IsNullOrEmpty(mutationClassName) || MutationGeneratedEquipment.Count == 0)
                return 0;

            int removedItemCount = 0;
            for (int i = MutationGeneratedEquipment.Count - 1; i >= 0; i--)
            {
                MutationGeneratedEquipmentTracker tracker = MutationGeneratedEquipment[i];
                if (!string.Equals(tracker.MutationClassName, mutationClassName, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool shouldRemoveItem = force || tracker.AutoRemoveOnMutationLoss;
                if (shouldRemoveItem && TryDetachFromCurrentOwner(tracker.Item))
                    removedItemCount++;

                MutationGeneratedEquipment.RemoveAt(i);
            }

            return removedItemCount;
        }

        public bool RemoveMutationGeneratedEquipment(Guid id, bool forceItemRemoval = false)
        {
            if (id == Guid.Empty)
                return false;

            for (int i = 0; i < MutationGeneratedEquipment.Count; i++)
            {
                MutationGeneratedEquipmentTracker tracker = MutationGeneratedEquipment[i];
                if (tracker.ID != id)
                    continue;

                if ((forceItemRemoval || tracker.AutoRemoveOnMutationLoss) && tracker.Item != null)
                    TryDetachFromCurrentOwner(tracker.Item);

                MutationGeneratedEquipment.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Mutation seam used when body layout changes and body/equipment mutations must remutate.
        /// </summary>
        public bool RebuildBodyFromMutations(string reason = "BodyChanged")
        {
            if (ParentEntity == null)
                return false;

            string rebuildReason = string.IsNullOrEmpty(reason) ? "BodyChanged" : reason;
            List<BaseMutation> affected = GetAllMutationParts().Where(m =>
                m != null &&
                m.Level > 0 &&
                (m.AffectsBodyParts || m.GeneratesEquipment)).ToList();

            if (affected.Count == 0)
                return false;

            for (int i = 0; i < affected.Count; i++)
                affected[i].OnBeforeBodyRebuild(ParentEntity, rebuildReason);

            for (int i = 0; i < affected.Count; i++)
            {
                BaseMutation mutation = affected[i];
                CleanupMutationGeneratedEquipment(mutation, force: true);
                mutation.Unmutate(ParentEntity);
            }

            GameEvent rebuilding = GameEvent.New("MutationBodyRebuild");
            rebuilding.SetParameter("Actor", (object)ParentEntity);
            rebuilding.SetParameter("Reason", rebuildReason);
            rebuilding.SetParameter("MutationCount", affected.Count);
            ParentEntity.FireEvent(rebuilding);

            for (int i = 0; i < affected.Count; i++)
            {
                BaseMutation mutation = affected[i];
                mutation.Mutate(ParentEntity, mutation.BaseLevel);
                mutation.OnAfterBodyRebuild(ParentEntity, rebuildReason);
            }

            SyncMutationLevels();
            return true;
        }

        /// <summary>
        /// Build Qud-style buy-new-mutation options:
        /// - event-adjusted selection count
        /// - cost>=2 preference when trimming
        /// - morphotype filtering (Esper/Chimera)
        /// - optional chimeric extra-limb annotation hook
        /// </summary>
        public List<RandomMutationBuyOption> GetRandomBuyMutationOptions(
            int baseSelectionCount = 3,
            Random rng = null,
            Predicate<MutationDefinition> filter = null,
            bool allowMultipleDefects = false)
        {
            List<MutationDefinition> selections = GetMutatePool(
                candidate => MatchesMorphotypeRestrictions(candidate) && (filter == null || filter(candidate)),
                allowMultipleDefects);

            rng ??= new Random();
            ShuffleInPlace(selections, rng);

            int targetSelectionCount = GetAdjustedBuyAmountFromEvent("GetRandomBuyMutationCount", baseSelectionCount);
            if (targetSelectionCount < 0)
                targetSelectionCount = 0;

            if (selections.Count > targetSelectionCount)
            {
                List<MutationDefinition> weighted = new List<MutationDefinition>(targetSelectionCount);
                for (int i = 0; i < selections.Count; i++)
                {
                    MutationDefinition candidate = selections[i];
                    if (candidate.Cost >= 2)
                        weighted.Add(candidate);
                }

                if (weighted.Count < targetSelectionCount)
                {
                    for (int i = 0; i < selections.Count && weighted.Count < targetSelectionCount; i++)
                    {
                        MutationDefinition candidate = selections[i];
                        if (candidate.Cost < 2)
                            weighted.Add(candidate);
                    }
                }

                selections = weighted;
            }

            if (selections.Count > targetSelectionCount)
            {
                selections.RemoveRange(targetSelectionCount, selections.Count - targetSelectionCount);
            }

            HashSet<int> extraChimericSlots = new HashSet<int>();
            if (IsChimeraMorphotype() && targetSelectionCount > 0)
            {
                int rolls = GetAdjustedBuyAmountFromEvent("GetRandomBuyChimericBodyPartRolls", 1);
                if (rolls < 0)
                    rolls = 0;

                for (int i = 0; i < rolls; i++)
                    extraChimericSlots.Add(rng.Next(0, targetSelectionCount));
            }

            List<RandomMutationBuyOption> options = new List<RandomMutationBuyOption>(selections.Count);
            for (int i = 0; i < selections.Count; i++)
            {
                options.Add(new RandomMutationBuyOption
                {
                    Mutation = selections[i],
                    GrantsChimericBodyPart = extraChimericSlots.Contains(i)
                });
            }

            return options;
        }

        /// <summary>
        /// Headless buy-new helper.
        /// If Irritable Genome is present, bypasses choice semantics and mutates randomly.
        /// Otherwise picks the first generated option.
        /// </summary>
        public bool BuyRandomMutation(int cost = 4, int baseSelectionCount = 3, Random rng = null)
        {
            if (HasMutation("IrritableGenomeMutation") || HasMutation("IrritableGenome"))
            {
                if (ParentEntity == null)
                    return false;
                if (cost > 0)
                {
                    if (ParentEntity.GetStat("MP") == null)
                        return false;
                    if (ParentEntity.GetStatValue("MP", 0) < cost)
                        return false;
                }

                MutationDefinition random = RandomlyMutate(rng);
                if (random == null)
                    return false;

                if (cost <= 0)
                    return true;

                return ParentEntity != null && ParentEntity.UseMP(cost, "BuyNew");
            }

            List<RandomMutationBuyOption> options = GetRandomBuyMutationOptions(baseSelectionCount, rng);
            if (options.Count == 0)
                return false;

            return BuyRandomMutationOption(options[0], cost, "BuyNew");
        }

        /// <summary>
        /// Buy a selected mutation option and spend MP using the provided context.
        /// Default context matches Qud's new-mutation purchase path.
        /// </summary>
        public bool BuyRandomMutationOption(RandomMutationBuyOption option, int cost = 4, string spendContext = "BuyNew")
        {
            if (option == null || option.Mutation == null)
                return false;

            return BuyRandomMutation(option.Mutation, cost, spendContext, option.GrantsChimericBodyPart);
        }

        public bool BuyRandomMutation(
            MutationDefinition selection,
            int cost = 4,
            string spendContext = "BuyNew",
            bool grantsChimericBodyPart = false)
        {
            if (selection == null || ParentEntity == null)
                return false;
            if (!IncludedInMutatePool(selection))
                return false;
            if (!MatchesMorphotypeRestrictions(selection))
                return false;

            if (cost < 0)
                cost = 0;

            if (cost > 0)
            {
                if (ParentEntity.GetStat("MP") == null)
                    return false;
                if (ParentEntity.GetStatValue("MP", 0) < cost)
                    return false;
            }

            if (!AddMutation(selection.ClassName, 1))
                return false;

            if (cost > 0 && !ParentEntity.UseMP(cost, spendContext))
                return false;

            if (grantsChimericBodyPart)
            {
                var granted = GameEvent.New("RandomBuyChimericBodyPartGranted");
                granted.SetParameter("MutationClassName", selection.ClassName);
                granted.SetParameter("MutationName", selection.Name);
                ParentEntity.FireEvent(granted);
            }

            return true;
        }

        /// <summary>
        /// Apply one random valid mutation from the current mutate pool.
        /// Returns the granted mutation definition, or null if none could be applied.
        /// </summary>
        public MutationDefinition RandomlyMutate(
            Random rng = null,
            Predicate<MutationDefinition> filter = null,
            bool allowMultipleDefects = false)
        {
            List<MutationDefinition> pool = GetMutatePool(
                candidate => MatchesMorphotypeRestrictions(candidate) && (filter == null || filter(candidate)),
                allowMultipleDefects);

            if (pool.Count == 0)
                return null;

            rng ??= new Random();
            ShuffleInPlace(pool, rng);
            for (int i = 0; i < pool.Count; i++)
            {
                MutationDefinition candidate = pool[i];
                if (AddMutation(candidate.ClassName, 1))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Spend MP using random mutation actions (buy-new when possible, otherwise random rank-up).
        /// Returns the number of MP actually spent.
        /// </summary>
        public int RandomlySpendMutationPoints(
            int maxMPToSpend = int.MaxValue,
            Random rng = null,
            string spendContext = "RandomlySpendPoints")
        {
            if (ParentEntity == null || maxMPToSpend <= 0)
                return 0;
            if (ParentEntity.GetStat("MP") == null)
                return 0;

            rng ??= new Random();
            int spent = 0;
            int safety = 128;

            while (ParentEntity.GetStatValue("MP", 0) > 0 && spent < maxMPToSpend && safety-- > 0)
            {
                int remainingBudget = maxMPToSpend - spent;
                int currentMP = ParentEntity.GetStatValue("MP", 0);
                int before = currentMP;

                bool actionTaken = false;
                bool canBuy = remainingBudget >= 4 && currentMP >= 4;
                bool canRank = remainingBudget >= 1;

                if (canBuy && canRank)
                {
                    actionTaken = rng.Next(0, 2) == 0
                        ? TryRandomRankIncrease(rng, spendContext)
                        : TryRandomBuy(rng, spendContext);

                    if (!actionTaken)
                    {
                        actionTaken = TryRandomRankIncrease(rng, spendContext) || TryRandomBuy(rng, spendContext);
                    }
                }
                else if (canBuy)
                {
                    actionTaken = TryRandomBuy(rng, spendContext) || TryRandomRankIncrease(rng, spendContext);
                }
                else if (canRank)
                {
                    actionTaken = TryRandomRankIncrease(rng, spendContext);
                }

                int after = ParentEntity.GetStatValue("MP", 0);
                int delta = Math.Max(0, before - after);

                if (!actionTaken || delta <= 0)
                    break;

                spent += delta;
            }

            return spent;
        }

        public int GetLevelAdjustmentsForMutation(string className)
        {
            if (string.IsNullOrEmpty(className))
                return 0;

            int total = 0;
            for (int i = 0; i < MutationMods.Count; i++)
            {
                MutationModifierTracker mod = MutationMods[i];
                if (string.Equals(mod.MutationClassName, className, StringComparison.OrdinalIgnoreCase))
                    total += mod.Bonus;
            }
            return total;
        }

        public Guid AddMutationMod(
            string mutationClassName,
            int level = 1,
            MutationSourceType sourceType = MutationSourceType.Unknown,
            string sourceName = "")
        {
            if (string.IsNullOrEmpty(mutationClassName))
                return Guid.Empty;

            MutationModifierTracker tracker = new MutationModifierTracker
            {
                ID = Guid.NewGuid(),
                MutationClassName = mutationClassName,
                Bonus = level,
                SourceType = sourceType,
                SourceName = sourceName ?? ""
            };
            MutationMods.Add(tracker);

            // If this mutation isn't inherently present, create a temporary mutation part
            // so all standard lifecycle/event behavior still applies while the mod is active.
            BaseMutation existing = FindAttachedMutationByClass(mutationClassName);
            if (existing == null)
            {
                BaseMutation created = CreateMutationByName(mutationClassName);
                if (created != null)
                {
                    ParentEntity.AddPart(created);
                    created.Mutate(ParentEntity, 0);
                }
            }
            SyncMutationLevels();
            return tracker.ID;
        }

        public void RemoveMutationMod(Guid id)
        {
            if (id == Guid.Empty || MutationMods.Count == 0)
                return;

            MutationModifierTracker removed = null;
            for (int i = 0; i < MutationMods.Count; i++)
            {
                if (MutationMods[i].ID == id)
                {
                    removed = MutationMods[i];
                    MutationMods.RemoveAt(i);
                    break;
                }
            }

            if (removed == null)
                return;

            string className = removed.MutationClassName;
            bool hasOtherMods = MutationMods.Exists(m =>
                string.Equals(m.MutationClassName, className, StringComparison.OrdinalIgnoreCase));

            if (hasOtherMods)
                return;

            bool inherent = MutationList.Exists(m =>
                string.Equals(m.GetType().Name, className, StringComparison.OrdinalIgnoreCase));

            if (!inherent)
            {
                BaseMutation attached = FindAttachedMutationByClass(className);
                if (attached != null)
                {
                    CleanupMutationGeneratedEquipment(attached, force: false);
                    attached.Unmutate(ParentEntity);
                    ParentEntity.RemovePart(attached);
                }
            }
            SyncMutationLevels();
        }

        public bool SyncMutationLevels()
        {
            if (ParentEntity == null)
                return false;

            if (_syncing)
            {
                _restartSync = true;
                return false;
            }

            bool changed = false;
            _syncing = true;
            try
            {
                do
                {
                    _restartSync = false;
                    List<BaseMutation> all = GetAllMutationParts();
                    for (int i = 0; i < all.Count; i++)
                    {
                        BaseMutation mutation = all[i];
                        if (mutation == null)
                            continue;

                        int newLevel = mutation.Level;
                        int oldLevel = mutation.LastLevel;
                        if (newLevel == oldLevel)
                            continue;

                        if (newLevel <= 0 && oldLevel > 0)
                        {
                            CleanupMutationGeneratedEquipment(mutation, force: false);
                            mutation.Unmutate(ParentEntity);
                            changed = true;
                        }
                        else if (newLevel > 0 && oldLevel <= 0)
                        {
                            mutation.Mutate(ParentEntity, mutation.BaseLevel);
                            changed = true;
                        }
                        else
                        {
                            mutation.ChangeLevel(newLevel);
                            changed = true;
                        }

                        if (_restartSync)
                            break;
                    }
                }
                while (_restartSync);
            }
            finally
            {
                _syncing = false;
                _restartSync = false;
            }

            return changed;
        }

        /// <summary>
        /// Qud-style mutation pool selection with exclusion and defect-limit filtering.
        /// </summary>
        public List<MutationDefinition> GetMutatePool(
            Predicate<MutationDefinition> filter = null,
            bool allowMultipleDefects = false)
        {
            MutationRegistry.EnsureInitialized();
            List<MutationDefinition> pool = new List<MutationDefinition>();

            foreach (MutationDefinition candidate in MutationRegistry.GetAllDefinitions())
            {
                if (candidate == null)
                    continue;
                if (candidate.ExcludeFromPool)
                    continue;
                if (filter != null && !filter(candidate))
                    continue;
                if (!IncludedInMutatePool(candidate, allowMultipleDefects))
                    continue;

                pool.Add(candidate);
            }

            return pool;
        }

        public bool IncludedInMutatePool(MutationDefinition candidate, bool allowMultipleDefects = false)
        {
            if (candidate == null)
                return false;

            if (HasMutation(candidate.ClassName) && !candidate.Ranked)
                return false;

            List<MutationDefinition> current = GetCurrentMutationDefinitions();

            if (!allowMultipleDefects && candidate.Defect && current.Any(d => d.Defect))
                return false;

            // Candidate exclusions against current mutations
            for (int i = 0; i < current.Count; i++)
            {
                MutationDefinition existing = current[i];
                if (!AreMutationsCompatible(candidate, existing, allowMultipleDefects))
                    return false;
                if (!AreMutationsCompatible(existing, candidate, allowMultipleDefects))
                    return false;
            }

            return true;
        }

        private static bool AreMutationsCompatible(
            MutationDefinition source,
            MutationDefinition other,
            bool allowMultipleDefects)
        {
            if (source == null || other == null)
                return true;

            if (!allowMultipleDefects && source.Defect && other.Defect)
                return false;

            string[] exclusions = source.Exclusions ?? Array.Empty<string>();
            for (int i = 0; i < exclusions.Length; i++)
            {
                string exclusion = exclusions[i];
                if (string.IsNullOrWhiteSpace(exclusion))
                    continue;

                exclusion = exclusion.Trim();
                if (exclusion.StartsWith("*", StringComparison.Ordinal))
                {
                    string category = exclusion.Substring(1);
                    if (!string.IsNullOrEmpty(category) &&
                        string.Equals(category, other.Category, StringComparison.OrdinalIgnoreCase))
                        return false;
                    continue;
                }

                if (string.Equals(exclusion, other.Name, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (string.Equals(exclusion, other.ClassName, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private List<MutationDefinition> GetCurrentMutationDefinitions()
        {
            List<MutationDefinition> current = new List<MutationDefinition>(MutationList.Count);
            for (int i = 0; i < MutationList.Count; i++)
            {
                BaseMutation mutation = MutationList[i];
                if (mutation == null)
                    continue;
                if (MutationRegistry.TryGetByClassName(mutation.GetType().Name, out MutationDefinition definition))
                    current.Add(definition);
            }
            return current;
        }

        private int GetAdjustedBuyAmountFromEvent(string eventID, int baseAmount)
        {
            if (ParentEntity == null)
                return baseAmount;

            GameEvent e = GameEvent.New(eventID);
            e.SetParameter("Actor", (object)ParentEntity);
            e.SetParameter("BaseAmount", baseAmount);
            e.SetParameter("Amount", baseAmount);
            ParentEntity.FireEvent(e);
            return e.GetIntParameter("Amount", baseAmount);
        }

        private bool MatchesMorphotypeRestrictions(MutationDefinition definition)
        {
            if (definition == null)
                return false;

            if (IsEsperMorphotype() && !IsMentalDefinition(definition))
                return false;
            if (IsChimeraMorphotype() && !IsPhysicalDefinition(definition))
                return false;

            return true;
        }

        private bool IsEsperMorphotype()
        {
            if (ParentEntity == null)
                return false;

            if (ParentEntity.HasTag("Esper") || ParentEntity.GetProperty("Esper") != null)
                return true;
            if (HasMutation("Esper") || HasMutation("EsperMutation"))
                return true;

            string mutationLevel = ParentEntity.GetProperty("MutationLevel", ParentEntity.GetTag("MutationLevel"));
            return string.Equals(mutationLevel, "Esper", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsChimeraMorphotype()
        {
            if (ParentEntity == null)
                return false;

            if (ParentEntity.HasTag("Chimera") || ParentEntity.GetProperty("Chimera") != null)
                return true;
            if (HasMutation("Chimera") || HasMutation("ChimeraMutation"))
                return true;

            string mutationLevel = ParentEntity.GetProperty("MutationLevel", ParentEntity.GetTag("MutationLevel"));
            return string.Equals(mutationLevel, "Chimera", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMentalDefinition(MutationDefinition definition)
        {
            if (definition == null)
                return false;

            if (!string.IsNullOrEmpty(definition.Category))
            {
                if (definition.Category.IndexOf("Mental", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (definition.Category.IndexOf("Physical", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            BaseMutation instance = CreateMutationByName(definition.ClassName);
            if (instance == null || string.IsNullOrEmpty(instance.MutationType))
                return false;
            return instance.MutationType.IndexOf("Mental", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPhysicalDefinition(MutationDefinition definition)
        {
            if (definition == null)
                return false;

            if (!string.IsNullOrEmpty(definition.Category))
            {
                if (definition.Category.IndexOf("Physical", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (definition.Category.IndexOf("Mental", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            BaseMutation instance = CreateMutationByName(definition.ClassName);
            if (instance == null || string.IsNullOrEmpty(instance.MutationType))
                return false;
            return instance.MutationType.IndexOf("Physical", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ShuffleInPlace<T>(List<T> list, Random rng)
        {
            if (list == null || list.Count <= 1)
                return;
            if (rng == null)
                rng = new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private bool TryRandomRankIncrease(Random rng, string spendContext)
        {
            List<BaseMutation> levelable = new List<BaseMutation>();
            for (int i = 0; i < MutationList.Count; i++)
            {
                BaseMutation mutation = MutationList[i];
                if (mutation != null && mutation.CanIncreaseLevel())
                    levelable.Add(mutation);
            }

            if (levelable.Count == 0)
                return false;

            BaseMutation selected = levelable[rng.Next(levelable.Count)];
            return SpendMPToIncreaseMutation(selected, spendContext);
        }

        private bool TryRandomBuy(Random rng, string spendContext)
        {
            if (ParentEntity == null || ParentEntity.GetStatValue("MP", 0) < 4)
                return false;

            MutationDefinition selected = RandomlyMutate(rng);
            if (selected == null)
                return false;

            return ParentEntity.UseMP(4, spendContext);
        }

        /// <summary>
        /// Get a mutation by type.
        /// </summary>
        public T GetMutation<T>() where T : BaseMutation
        {
            for (int i = 0; i < MutationList.Count; i++)
            {
                if (MutationList[i] is T typed)
                    return typed;
            }
            return null;
        }

        /// <summary>
        /// Check if the entity has a mutation of the given type.
        /// </summary>
        public bool HasMutation<T>() where T : BaseMutation
        {
            return GetMutation<T>() != null;
        }

        public BaseMutation GetMutation(string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;

            for (int i = 0; i < MutationList.Count; i++)
            {
                BaseMutation mutation = MutationList[i];
                if (string.Equals(mutation.GetType().Name, className, StringComparison.OrdinalIgnoreCase))
                    return mutation;
            }
            return null;
        }

        public bool HasMutation(string className)
        {
            return GetMutation(className) != null;
        }

        public bool AddMutation(string className, int level = 1)
        {
            BaseMutation mutation = CreateMutationByName(className);
            if (mutation == null)
                return false;
            return AddMutation(mutation, level);
        }

        private static string FindEquippedSlot(InventoryPart inventory, Entity item)
        {
            if (inventory == null || item == null)
                return null;

            foreach (KeyValuePair<string, Entity> kvp in inventory.EquippedItems)
            {
                if (kvp.Value == item)
                    return kvp.Key;
            }

            return null;
        }

        private bool TryDetachFromCurrentOwner(Entity item)
        {
            if (item == null)
                return false;

            bool removed = false;
            PhysicsPart physics = item.GetPart<PhysicsPart>();
            Entity owner = physics?.Equipped ?? physics?.InInventory;
            if (owner != null)
                removed |= TryRemoveFromInventoryOwner(owner, item);

            if (ParentEntity != null && owner != ParentEntity)
            {
                InventoryPart parentInventory = ParentEntity.GetPart<InventoryPart>();
                if (parentInventory != null && parentInventory.Contains(item))
                    removed |= TryRemoveFromInventoryOwner(ParentEntity, item);
            }

            return removed;
        }

        private static bool TryRemoveFromInventoryOwner(Entity owner, Entity item)
        {
            if (owner == null || item == null)
                return false;

            InventoryPart inventory = owner.GetPart<InventoryPart>();
            if (inventory == null)
                return false;

            bool removed = false;
            string slot = FindEquippedSlot(inventory, item);
            if (slot != null)
            {
                if (!InventorySystem.Unequip(owner, slot))
                    removed |= inventory.Unequip(slot);
                else
                    removed = true;
            }

            if (inventory.RemoveObject(item))
                removed = true;

            return removed;
        }

        private List<BaseMutation> GetAllMutationParts()
        {
            List<BaseMutation> all = new List<BaseMutation>(MutationList.Count + 4);

            for (int i = 0; i < MutationList.Count; i++)
            {
                BaseMutation mutation = MutationList[i];
                if (mutation != null && !all.Contains(mutation))
                    all.Add(mutation);
            }

            if (ParentEntity != null)
            {
                for (int i = 0; i < ParentEntity.Parts.Count; i++)
                {
                    if (ParentEntity.Parts[i] is BaseMutation mutation && !all.Contains(mutation))
                        all.Add(mutation);
                }
            }

            return all;
        }

        private BaseMutation FindAttachedMutationByClass(string className)
        {
            if (ParentEntity == null || string.IsNullOrEmpty(className))
                return null;

            for (int i = 0; i < ParentEntity.Parts.Count; i++)
            {
                if (ParentEntity.Parts[i] is BaseMutation mutation &&
                    string.Equals(mutation.GetType().Name, className, StringComparison.OrdinalIgnoreCase))
                {
                    return mutation;
                }
            }
            return null;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "SyncMutationLevels")
            {
                SyncMutationLevels();
            }
            else if (e.ID == "BodyChanged" || e.ID == "BodyPartsChanged" || e.ID == "RebuildBodyFromMutations")
            {
                string reason = e.GetStringParameter("Reason", e.ID);
                RebuildBodyFromMutations(reason);
            }
            else if (e.ID == "StatChanged" || e.ID == "IntPropertyChanged")
            {
                SyncMutationLevels();
            }
            return true;
        }

        /// <summary>
        /// Create a mutation instance by class name using reflection.
        /// Searches the assembly for a matching BaseMutation subclass.
        /// </summary>
        private static BaseMutation CreateMutationByName(string className)
        {
            // Search all types in the assembly that contains BaseMutation
            var assembly = typeof(BaseMutation).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && typeof(BaseMutation).IsAssignableFrom(type) && type.Name == className)
                {
                    return (BaseMutation)Activator.CreateInstance(type);
                }
            }
            return null;
        }
    }
}
