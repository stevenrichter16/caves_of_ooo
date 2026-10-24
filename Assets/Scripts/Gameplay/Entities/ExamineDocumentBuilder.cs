using System;
using System.Collections.Generic;
using System.Globalization;

namespace CavesOfOoo.Core
{
    /// <summary>Truthful explicit-read snapshots. Uses the same live description providers as
    /// ordinary Examine; does not infer composition, simulate actions, or reveal unseen owners.</summary>
    public static class ExamineDocumentBuilder
    {
        /// <summary>Only a current visible occupied cell may identify its subject. Footprint cells
        /// are supported; they need not be the subject's anchor cell.</summary>
        public static AnnouncementDocument BuildWorld(Entity target,Zone zone,Cell cell,Entity viewer=null)
        {
            if(!VisibleOwner(target,zone,cell))return null;
            var examinable=target.GetPart<ExaminablePart>();
            var document=Build(target,zone,cell,viewer);
            return VisibleOwner(target,zone,cell)&&ReferenceEquals(examinable,target.GetPart<ExaminablePart>())?document:null;
        }

        /// <summary>Requires the actual carried/equipped subject and its current owning viewer.
        /// A bare entity reference is not permission to inspect somebody else's inventory.</summary>
        public static AnnouncementDocument BuildInventory(Entity target,Entity viewer=null)
        {
            if(!Owned(target,viewer))return null;
            var examinable=target.GetPart<ExaminablePart>();
            var document=Build(target,null,null,viewer);
            return Owned(target,viewer)&&ReferenceEquals(examinable,target.GetPart<ExaminablePart>())?document:null;
        }

        /// <summary>Open-time authority for a current subject portrait. Rechecks only physical
        /// ownership and the captured visible cell/viewer; never calls description providers or
        /// refreshes the document's prose. An unrelated replacement owner or zone grants no access.</summary>
        public static bool IsCurrentSubject(AnnouncementDocument document)
        {
            if(document?.Subject==null)return false;
            if(document.SubjectZone==null)
                return document.SubjectCell==null&&Owned(document.Subject,document.SubjectViewer);
            if(document.SubjectViewer!=null&&!PresentViewer(document.SubjectViewer,document.SubjectZone))return false;
            return VisibleOwner(document.Subject,document.SubjectZone,document.SubjectCell);
        }

        static bool PresentViewer(Entity viewer,Zone zone)
        {
            if(viewer==null||zone==null)return false;
            var cell=zone.GetEntityCell(viewer);var physical=viewer.GetPart<PhysicsPart>();
            return viewer.SpatialZone==zone&&cell?.ParentZone==zone
                &&zone.GetCell(cell.X,cell.Y)==cell&&cell.Objects.Contains(viewer)
                &&physical?.ParentEntity==viewer&&physical.InInventory==null&&physical.Equipped==null;
        }

        static bool VisibleOwner(Entity target,Zone zone,Cell cell)
        {
            if(target==null||zone==null||cell==null||string.IsNullOrEmpty(target.ID)
                ||cell.ParentZone!=zone||zone.GetCell(cell.X,cell.Y)!=cell||!cell.Explored||!cell.IsVisible
                ||target.SpatialZone!=zone||!cell.Occupants.Contains(target))return false;
            var anchor=zone.GetEntityCell(target);var render=target.GetPart<RenderPart>();var physical=target.GetPart<PhysicsPart>();
            return anchor?.ParentZone==zone&&zone.GetCell(anchor.X,anchor.Y)==anchor&&anchor.Objects.Contains(target)
                &&render?.ParentEntity==target&&render.Visible&&physical?.ParentEntity==target
                &&physical.InInventory==null&&physical.Equipped==null
                &&target.GetPart<ExaminablePart>()?.ParentEntity==target&&ValidStack(target);
        }

        static bool Owned(Entity target,Entity viewer)
        {
            if(target==null||viewer==null||target.SpatialZone!=null||!ValidStack(target))return false;
            var inventory=viewer.GetPart<InventoryPart>();var physical=target.GetPart<PhysicsPart>();var render=target.GetPart<RenderPart>();
            if(inventory?.ParentEntity!=viewer||physical?.ParentEntity!=target||render?.ParentEntity!=target||!render.Visible)return false;
            var examinable=target.GetPart<ExaminablePart>();
            if(examinable!=null&&examinable.ParentEntity!=target)return false;
            if(physical.InInventory==viewer&&physical.Equipped==null)return inventory.Objects.Contains(target);
            return physical.Equipped==viewer&&physical.InInventory==null&&inventory.EquippedItems.ContainsValue(target);
        }

        static bool ValidStack(Entity target)
        {var stack=target.GetPart<StackerPart>();return stack==null||(stack.ParentEntity==target&&stack.StackCount>0);}

        static AnnouncementDocument Build(Entity owner,Zone zone,Cell cell,Entity viewer)
        {
            var sections=new List<AnnouncementSection>();var facts=new List<AnnouncementFact>();
            bool inWorld=zone!=null&&cell!=null;
            string warning=inWorld?LocalGatheringClaims.WarningFor(viewer,owner,zone):null;
            Add(sections,"Attention",warning,AnnouncementTone.Warning);

            // Public commitments, current effects and visible ground must precede long flavor.
            var state=new List<string>();
            Add(state,CombatIntentReadout.ActorLine(owner,zone));
            if(inWorld)Add(state,CombatIntentReadout.ThreatLine(zone,cell));
            var effects=owner.GetPart<StatusEffectsPart>()?.GetAllEffects();
            if(effects!=null)for(int i=0;i<effects.Count;i++)
                if(effects[i]!=null&&effects[i].Owner==owner&&effects[i].Duration!=0)Add(state,EffectDescriber.Describe(effects[i]));
            if(inWorld)Add(state,CellStatusReadout.GroundLine(zone,cell));
            Add(state,owner.GetPart<KitchenBatchPart>()?.Describe());
            Add(state,owner.GetPart<SoddenPreparationPart>()?.Describe());
            Add(state,owner.GetPart<RepairablePart>()?.Describe());
            Add(state,owner.GetPart<TrapJammingPart>()?.DescribeJamming());
            if(inWorld)
            {
                Add(state,owner.GetPart<CampfirePart>()?.DescribeFiniteCooking(zone,cell));
                Add(state,owner.GetPart<CropPart>()?.DescribeGrowth(zone,cell));
            }
            Add(sections,"Current state",string.Join("\n",state));

            var examine=owner.GetPart<ExaminablePart>();
            string flavor=examine?.GetDescriptionText();
            Add(sections,"Description",!string.IsNullOrWhiteSpace(flavor)
                ?flavor.Trim():"You see "+Article(owner.GetDisplayName())+owner.GetDisplayName()+".");

            var uses=new List<string>();
            if(owner.HasPart<CultivatedSoilPart>())Add(uses,"Tilled growing bed. Plant a carried seed here when the bed is empty. Conjure Rain waters planted crops; watered crops keep growing while you travel and rest, but dry soil stops growth.");
            Add(uses,owner.GetPart<LegendaryIdentityPart>()?.DescribeCarriedReward());
            Add(uses,owner.GetPart<TacticalSupplyPart>()?.Describe());
            Add(uses,owner.GetPart<FieldMedicinePart>()?.Describe());
            if(owner.GetPart<MeleeTraumaProfilePart>()?.AllowIncidentalDismemberment==false)
                Add(uses,"Its ordinary melee attacks can wound and kill, but do not cause incidental limb loss. This does not protect against other hazards.");
            if(ItemExamineService.TryDescribeDetails(owner,out string mechanics))Add(uses,mechanics);
            Add(sections,"Use & mechanics",string.Join("\n",uses));

            // The carried-material provider checks consumable ownership and guide possession itself.
            // Passing no factory uses its canonical authored guide names, without loading more content.
            if(viewer!=null&&MaterialUseDescription.TryDescribe(viewer,owner,null,out string materialUses))
                Add(sections,"Field uses",materialUses);

            var enhancements=new List<string>();
            var parts=new List<Part>(owner.Parts);
            foreach(var part in parts)if(part.ParentEntity==owner&&part is IItemEnhancement enhancement)
                Add(enhancements,enhancement.GetEffectDescription());
            Add(sections,"Enhancements",string.Join("\n",enhancements));
            Add(sections,"Directions",owner.GetPart<RegionalSignpostPart>()?.GetDirectionsText());
            Add(sections,"Local observations",SpreadExplorationReadout.Describe(owner));

            AddFacts(owner,facts);
            var render=owner.GetPart<RenderPart>();
            if(render?.ParentEntity!=owner||!render.Visible)return null;
            char? glyph=inWorld?render.ResolveGlyph(cell.X,cell.Y):string.IsNullOrEmpty(render.RenderString)?(char?)null:render.RenderString[0];
            return new AnnouncementDocument(owner.GetDisplayName(),"EXAMINE",sections,facts,
                owner.BlueprintName,glyph,render.ColorString,owner,zone,cell,viewer);
        }

        static void AddFacts(Entity owner,List<AnnouncementFact> facts)
        {
            var physical=owner.GetPart<PhysicsPart>();
            string type=owner.HasTag("Creature")?"Creature":owner.GetPart<CropPart>()!=null?"Crop"
                :owner.HasTag("Terrain")?"Terrain":!string.IsNullOrWhiteSpace(physical?.Category)?physical.Category
                :owner.HasTag("Item")?"Item":"Object";
            facts.Add(new AnnouncementFact("Type",type));
            var material=owner.GetPart<MaterialPart>();
            if(material?.ParentEntity==owner&&!string.IsNullOrWhiteSpace(material.MaterialID)
                &&!string.Equals(material.MaterialID.Trim(),"Generic",StringComparison.OrdinalIgnoreCase))
                facts.Add(new AnnouncementFact("Material",material.MaterialID.Trim()));
            var composition=owner.GetPart<CompositionPart>();
            if(composition?.ParentEntity==owner&&!string.IsNullOrWhiteSpace(composition.MaterialsRaw))
            {
                var values=new List<string>();
                foreach(string raw in composition.MaterialsRaw.Split(','))
                {string value=raw.Trim();if(value.Length>0&&!values.Contains(value))values.Add(value);}
                if(values.Count>0)facts.Add(new AnnouncementFact("Composition",string.Join(", ",values)));
            }
            AddRepairFact(owner,facts);
            if(physical?.Takeable==true)
            {
                int count=owner.GetPart<StackerPart>()?.StackCount??1,weight=HandlingService.GetWeight(owner);
                if(weight>=0)
                {
                    facts.Add(new AnnouncementFact("Weight",weight.ToString(CultureInfo.InvariantCulture)+" lb each"));
                    if(count>1)facts.Add(new AnnouncementFact("Stack",count.ToString(CultureInfo.InvariantCulture)+"; "+((long)weight*count).ToString(CultureInfo.InvariantCulture)+" lb total"));
                }
            }
            var structure=owner.GetPart<DestructiblePart>();
            if(structure?.ParentEntity==owner&&structure.MaxHP>0)
                facts.Add(new AnnouncementFact("Structure",structure.HP.ToString(CultureInfo.InvariantCulture)+" / "+structure.MaxHP.ToString(CultureInfo.InvariantCulture)));
        }

        static void AddRepairFact(Entity owner,List<AnnouncementFact> facts)
        {
            var repair=owner.GetPart<RepairablePart>();var composition=owner.GetPart<CompositionPart>();
            if(repair?.ParentEntity!=owner||composition?.ParentEntity!=owner)return;
            if(repair.PortableEquipment?!owner.HasEffect<BrokenEffect>():repair.Repaired)return;
            int repairs=0,compositions=0;
            foreach(var part in owner.Parts)
            { if(part is RepairablePart)repairs++;if(part is CompositionPart)compositions++; }
            if(repairs!=1||compositions!=1)return;
            var recipe=RepairRecipeRegistry.Get(repair.RecipeId);
            if(recipe==null||!composition.Contains(recipe.Composition))return;
            // A requirement, not a claim that the viewer has supplies, is in reach, or can act.
            facts.Add(new AnnouncementFact("Repair",recipe.Quantity.ToString(CultureInfo.InvariantCulture)+" "+recipe.MaterialName));
        }

        static void Add(List<string> values,string text) { if(!string.IsNullOrWhiteSpace(text))values.Add(text); }
        static void Add(List<AnnouncementSection> values,string heading,string text,AnnouncementTone tone=AnnouncementTone.Normal)
        { if(!string.IsNullOrWhiteSpace(text))values.Add(new AnnouncementSection(heading,text,tone)); }
        static string Article(string name)
        {
            if(string.IsNullOrEmpty(name)||char.IsUpper(name[0]))return "";
            string lower=name.ToLowerInvariant();
            foreach(string prefix in new[]{"a ","an ","the ","some ","your ","his ","her ","their "})
                if(lower.StartsWith(prefix,StringComparison.Ordinal))return "";
            return "aeiou".IndexOf(lower[0])>=0?"an ":"a ";
        }
    }
}
