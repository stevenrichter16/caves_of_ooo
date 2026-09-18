using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>A per-bind, read-only bridge from native model identity to baked
    /// voxel geometry. It borrows assets and never mutates gameplay state.</summary>
    public sealed class VoxelWorldPresentation
    {
        private readonly VoxelWorldMeshCatalog catalog;
        private readonly HashSet<Mesh> generated = new HashSet<Mesh>();
        private readonly HashSet<Mesh> missing = new HashSet<Mesh>();
        public string ZoneId { get; }
        /// <summary>Source mesh uses replaced during this bind, including ground
        /// templates. Reapplying to a converted instance is a no-op.</summary>
        public int AppliedMeshCount { get; private set; }
        /// <summary>Distinct unmapped sources, excluding already baked meshes.</summary>
        public int MissingMeshCount => missing.Count;

        private VoxelWorldPresentation(string zoneId, VoxelWorldMeshCatalog catalog)
        {
            this.catalog = catalog; ZoneId = zoneId; catalog.Validate();
            foreach (var binding in catalog.Bindings) generated.Add(binding.Voxel);
            if (SpreadCompositionPlan.IsWildernessZone(zoneId) || SoddenCompositionPlan.IsWildernessZone(zoneId))
            {
                var kit=SpreadVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Spread voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
            }
            if (SoddenCompositionPlan.IsWildernessZone(zoneId))
            {
                var kit=SoddenVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Sodden voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
            }
            if (StumpCompositionPlan.IsWildernessZone(zoneId))
            {
                var kit=StumpVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Stump voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
                var rubble=BeatingVoxelLibrary.Load();
                if(rubble==null)throw new InvalidOperationException("Required shared rubble voxel kit missing.");
                for(int i=0;i<4;i++)generated.Add(rubble.Find(BeatingVoxelLibrary.ModelId("rubble",i)).Mesh);
            }
            if (BeatingCompositionPlan.IsWildernessZone(zoneId))
            {
                var kit=BeatingVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Beating voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
            }
            if (OverwritCompositionPlan.IsWildernessZone(zoneId))
            {
                var kit=OverwritVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Overwrit voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
                RegisterWreckage(false);
            }
            bool cinderhold=CinderholdCompositionPlan.IsSupportedZone(zoneId);
            bool sumphold=SumpholdCompositionPlan.IsSupportedZone(zoneId);
            bool witnessIntake=DrownedLedgerCompositionPlan.IsSupportedZone(zoneId)||MarrowstyeCompositionPlan.IsSupportedZone(zoneId);
            if(witnessIntake)
            {
                var ledger=DrownedLedgerVoxelKitLibrary.Load();var intake=MarrowstyeVoxelKitLibrary.Load();
                if(ledger==null||intake==null)throw new InvalidOperationException("Required witness/intake voxel kits missing.");
                ledger.Validate();intake.Validate();
                foreach(var entry in ledger.Entries)generated.Add(entry.Mesh);
                foreach(var entry in intake.Entries)generated.Add(entry.Mesh);
            }
            bool thresholdCamp=FirstTentCompositionPlan.IsSupportedZone(zoneId)||LastCounterCompositionPlan.IsSupportedZone(zoneId);
            if(thresholdCamp)
            {
                var first=FirstTentVoxelKitLibrary.Load();var last=LastCounterVoxelKitLibrary.Load();
                if(first==null||last==null)throw new InvalidOperationException("Required threshold camp voxel kits missing.");
                first.Validate();last.Validate();
                foreach(var entry in first.Entries)generated.Add(entry.Mesh);
                foreach(var entry in last.Entries)generated.Add(entry.Mesh);
                var shared=CinderholdVoxelKitLibrary.Load();
                if(shared==null)throw new InvalidOperationException("Required native market and quest-token art missing.");
                shared.Validate();
                foreach(var entry in shared.Entries)generated.Add(entry.Mesh);
            }
            if(cinderhold||sumphold||witnessIntake)
            {
                var post=CinderholdVoxelKitLibrary.Load();var yard=SumpholdVoxelKitLibrary.Load();
                if(post==null||yard==null)throw new InvalidOperationException("Required workshop/boatyard voxel kits missing.");
                post.Validate();yard.Validate();
                foreach(var entry in post.Entries)generated.Add(entry.Mesh);
                foreach(var entry in yard.Entries)generated.Add(entry.Mesh);
                var wet=SoddenVoxelLibrary.Load();var reeds=SpreadVoxelLibrary.Load();
                if(wet==null||reeds==null)throw new InvalidOperationException("Required native wet-margin art missing.");
                wet.Validate();reeds.Validate();
                foreach(var entry in wet.Entries)generated.Add(entry.Mesh);
                foreach(var entry in reeds.Entries)generated.Add(entry.Mesh);
            }
            bool civicQuartet=GantryCompositionPlan.IsSupportedZone(zoneId)||TineCompositionPlan.IsSupportedZone(zoneId)||QuillholdCompositionPlan.IsSupportedZone(zoneId)||TallyCompositionPlan.IsSupportedZone(zoneId);
            if(civicQuartet)
            {
                var gantry=GantryVoxelKitLibrary.Load();var tine=TineVoxelKitLibrary.Load();var quillhold=QuillholdVoxelKitLibrary.Load();var tally=TallyVoxelKitLibrary.Load();
                if(gantry==null||tine==null||quillhold==null||tally==null)throw new InvalidOperationException("Required civic quartet voxel kits missing.");
                gantry.Validate();tine.Validate();quillhold.Validate();tally.Validate();
                foreach(var e in gantry.Entries)generated.Add(e.Mesh);foreach(var e in tine.Entries)generated.Add(e.Mesh);
                foreach(var e in quillhold.Entries)generated.Add(e.Mesh);foreach(var e in tally.Entries)generated.Add(e.Mesh);
                var market=CinderholdVoxelKitLibrary.Load();var landscape=SpreadVoxelLibrary.Load();
                if(market==null||landscape==null)throw new InvalidOperationException("Required civic service and vegetation art missing.");
                market.Validate();landscape.Validate();foreach(var e in market.Entries)generated.Add(e.Mesh);foreach(var e in landscape.Entries)generated.Add(e.Mesh);
                // Native hosts can travel between the four public districts.
                {var host=FirstTentVoxelKitLibrary.Load();if(host==null)throw new InvalidOperationException("Required guest-cloth art missing.");host.Validate();foreach(var e in host.Entries)generated.Add(e.Mesh);}
                if(TineCompositionPlan.IsSupportedZone(zoneId))
                {var frames=SumpholdVoxelKitLibrary.Load();if(frames==null)throw new InvalidOperationException("Required native boat-frame art missing.");frames.Validate();foreach(var e in frames.Entries)generated.Add(e.Mesh);}
            }
            bool olderdeep=OlderdeepCompositionPlan.IsSupportedZone(zoneId);
            bool wellmeet=WellmeetCompositionPlan.IsSupportedZone(zoneId)||cinderhold||sumphold||witnessIntake||thresholdCamp||civicQuartet;
            if(olderdeep||wellmeet)
            {
                var founding=OlderdeepVoxelLibrary.Load();
                var camp=WellmeetVoxelLibrary.Load();
                if(founding==null||camp==null)throw new InvalidOperationException("Required founding/camp voxel kit missing.");
                founding.Validate();camp.Validate();
                foreach(var entry in founding.Entries)generated.Add(entry.Mesh);
                foreach(var entry in camp.Entries)generated.Add(entry.Mesh);
                // Existing native sandstone and debris can occur in the camp.
                if(wellmeet)
                {
                    var cave=GinmereVoxelLibrary.Load();
                    if(cave==null)throw new InvalidOperationException("Required shared cliff voxel kit missing.");
                    cave.Validate();foreach(var entry in cave.Entries)generated.Add(entry.Mesh);
                    RegisterWreckage(true);
                }
            }
            bool cathedral=CathedralCompositionPlan.IsSupportedZone(zoneId);
            bool stillleaf=StillleafCompositionPlan.IsSupportedZone(zoneId);
            if(cathedral||stillleaf||olderdeep)
            {
                var kit=CathedralVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Cathedral voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
            }
            if(stillleaf||cathedral||olderdeep)
            {
                var kit=StillleafVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Stillleaf voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
                var stump=StumpVoxelLibrary.Load();
                if(stump==null)throw new InvalidOperationException("Required shared Stump voxel kit missing.");
                stump.Validate();foreach(var entry in stump.Entries)generated.Add(entry.Mesh);
            }
            if (GinmereCompositionPlan.IsSupportedZone(zoneId)||cathedral||stillleaf||olderdeep)
            {
                var kit=GinmereVoxelLibrary.Load();
                if(kit==null)throw new InvalidOperationException("Required Ginmere voxel kit missing.");
                kit.Validate();foreach(var entry in kit.Entries)generated.Add(entry.Mesh);
                RegisterWreckage(true);
            }
        }
        private void RegisterWreckage(bool bones)
        {
            var kit=BeatingVoxelLibrary.Load();
            if(kit==null)throw new InvalidOperationException("Required shared wreckage voxel kit missing.");
            kit.Validate();
            for(int i=0;i<4;i++)
            {
                generated.Add(kit.Find(BeatingVoxelLibrary.ModelId("rubble",i)).Mesh);
                if(bones)generated.Add(kit.Find(BeatingVoxelLibrary.ModelId("bones",i)).Mesh);
            }
        }
        public static bool IsSupported(string zoneId)
        {
            switch (zoneId)
            {
                case "Overworld.2.6.0": case "Overworld.3.6.0":
                case "Overworld.3.7.0": case "Overworld.4.7.0": return true;
                default: return (GrovelandsCompositionPlan.IsWildernessZone(zoneId) || SpreadCompositionPlan.IsWildernessZone(zoneId) || SoddenCompositionPlan.IsWildernessZone(zoneId) || BeatingCompositionPlan.IsWildernessZone(zoneId) || StumpCompositionPlan.IsWildernessZone(zoneId) || OverwritCompositionPlan.IsWildernessZone(zoneId) || GinmereCompositionPlan.IsSupportedZone(zoneId) || CathedralCompositionPlan.IsSupportedZone(zoneId) || StillleafCompositionPlan.IsSupportedZone(zoneId) || OlderdeepCompositionPlan.IsSupportedZone(zoneId) || WellmeetCompositionPlan.IsSupportedZone(zoneId) || CinderholdCompositionPlan.IsSupportedZone(zoneId) || SumpholdCompositionPlan.IsSupportedZone(zoneId) || DrownedLedgerCompositionPlan.IsSupportedZone(zoneId) || MarrowstyeCompositionPlan.IsSupportedZone(zoneId) || FirstTentCompositionPlan.IsSupportedZone(zoneId) || LastCounterCompositionPlan.IsSupportedZone(zoneId) || GantryCompositionPlan.IsSupportedZone(zoneId) || TineCompositionPlan.IsSupportedZone(zoneId) || QuillholdCompositionPlan.IsSupportedZone(zoneId) || TallyCompositionPlan.IsSupportedZone(zoneId));
            }
        }
        public static bool IsEnabledFor(Zone zone)
            => Village3DSettings.Enabled && zone != null && IsSupported(zone.ZoneID) && AreaCompositionScope.Allows(zone);

        /// <summary>Unsupported zones do not load resources. Invalid required
        /// resources throw into the presenter's existing cleanup/fallback path.</summary>
        public static VoxelWorldPresentation ForZone(Zone zone)
        {
            if (!IsEnabledFor(zone)) return null;
            try
            {
                var library = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
                if (library == null) throw new InvalidOperationException("Required voxel mesh catalogue is unavailable.");
                var result = new VoxelWorldPresentation(zone.ZoneID, library);
                if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "VoxelPresentationBound",
                    payload: new { zoneId = zone.ZoneID, bindings = library.Bindings.Length });
                return result;
            }
            catch (Exception error)
            {
                if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "VoxelPresentationRejected",
                    payload: new { zoneId = zone.ZoneID, reason = error.Message });
                throw;
            }
        }
        /// <summary>Unknown content retains its original body and records one
        /// diagnostic per source and bind. Returned meshes are borrowed.</summary>
        public Mesh Resolve(Mesh source)
        {
            if (source == null || generated.Contains(source)) return source;
            var replacement = catalog.Resolve(source);
            if (replacement != source) { AppliedMeshCount++; return replacement; }
            if (missing.Add(source) && Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "VoxelMeshUnmapped", payload: new { zoneId = ZoneId, mesh = source.name });
            return source;
        }
        /// <summary>Call only on an owned instance, before collider creation and
        /// native material preparation. Skeletons, sockets and materials survive.</summary>
        public int Apply(GameObject ownedInstance)
        {
            if (ownedInstance == null) throw new ArgumentNullException(nameof(ownedInstance));
            int before = AppliedMeshCount;
            foreach (var filter in ownedInstance.GetComponentsInChildren<MeshFilter>(true))
                filter.sharedMesh = Resolve(filter.sharedMesh);
            foreach (var skin in ownedInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var original = skin.sharedMesh; var replacement = Resolve(original);
                if (replacement == original) continue;
                var envelope = skin.localBounds;
                skin.sharedMesh = replacement;
                envelope.Encapsulate(replacement.bounds.min); envelope.Encapsulate(replacement.bounds.max);
                skin.localBounds = envelope;
            }
            return AppliedMeshCount - before;
        }
    }
}
