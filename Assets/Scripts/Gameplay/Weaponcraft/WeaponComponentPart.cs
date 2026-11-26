using System.Linq;
namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an item as a weapon-forging component and carries its
    /// contribution to the assembled weapon (Docs/CRAFTING-ALCHEMY-SYSTEM.md
    /// §7.1 Layer 1). Blueprint-authorable like ReagentPart: part name
    /// "WeaponComponent" resolves via EntityFactory's name+"Part" convention.
    ///
    /// Slot vocabulary: "Blade" (a blade or head; drives damage dice,
    /// damage class and weapon family), "Haft" (drives strength cap + handling), "Binding"
    /// (drives pen/hit bonuses + quirk). One component of each slot forges
    /// one weapon; contributions are combined by
    /// WeaponForgingService.PreviewForge.
    /// </summary>
    public class WeaponComponentPart : Part, IMaterialProcessPermission, IInventoryActionReceiptOwner, IDescribeUse, IDescribeUseRows
    {
        public override string Name => "WeaponComponent";

        /// <summary>"Blade" | "Haft" | "Binding".</summary>
        public string Slot = "";

        /// <summary>Dice contributed to MeleeWeaponPart.BaseDamage. Only the Blade's is used.</summary>
        public string BaseDamage = "";

        /// <summary>Added to MeleeWeaponPart.PenBonus (summed across components).</summary>
        public int PenBonus = 0;

        /// <summary>Added to MeleeWeaponPart.HitBonus (summed across components).</summary>
        public int HitBonus = 0;

        /// <summary>
        /// Contribution to MeleeWeaponPart.MaxStrengthBonus — the MAX across
        /// components wins (a better haft raises the cap; a worse blade
        /// doesn't drag it down). -1 = contributes nothing.
        /// </summary>
        public int MaxStrengthBonus = -1;

        /// <summary>Space-delimited weapon attributes to union in (e.g. "Cutting LongBlades").</summary>
        public string Attributes = "";

        /// <summary>
        /// Optional on-hit quirk in MeleeWeaponPart.OnHitEffectsRaw grammar
        /// ("EffectName,ChancePercent,DamageDice,DurationTurns,Magnitude").
        /// Quirks from all components are joined with ';'.
        /// </summary>
        public string OnHitEffectSpec = "";

        /// <summary>
        /// Fragment used to build the forged weapon's display name.
        /// Assembly order: haft fragment, binding fragment, blade fragment
        /// (e.g. "long-hafted" + "serrated" + "steel blade").
        /// </summary>
        public string NameFragment = "";

        bool IMaterialProcessPermission.Allows(MaterialProcessDefinition definition)=>definition!=null
            && ParentEntity?.GetPart<WeaponComponentPart>()==this && ParentEntity.BlueprintName==definition.SourceBlueprint
            && definition.SourceRecipeId.Length==0 && definition.Mode=="Process" && definition.WorkKind=="ForgeCold";
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<WeaponComponentPart>()==this && (MaterialProcessInputPart.IsCommand(command) || DoorBraceActions.IsCommand(command));
        public override bool HandleEvent(GameEvent e)=>ParentEntity?.GetPart<WeaponComponentPart>()!=this
            || (ParentEntity.HasPart<LodgedSpikePart>() || DoorBraceActions.Handle(ParentEntity,e)) && (ParentEntity.HasPart<MaterialProcessInputPart>() || MaterialProcessActions.Handle(ParentEntity,e,MaterialProcessRegistry.ForSource(ParentEntity)
                .Where(d=>d.Mode=="Process" && d.WorkKind=="ForgeCold" && d.SourceRecipeId.Length==0).Select(d=>d.Id)));
        public int Order=>40;
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)
        {
            DoorBraceActions.AddUseRows(ParentEntity,actor,zone,rows);
            if(ParentEntity?.HasPart<MaterialProcessInputPart>()!=false)return;
            foreach(var d in MaterialProcessRegistry.ForSource(ParentEntity))
                if(d.Mode=="Process" && d.WorkKind=="ForgeCold" && MaterialProcessService.TryQuote(MaterialProcessPlan.TryCreate(actor,ParentEntity,d.Id,zone,ForgePart.Factory))!=null)
                    rows.AddAction(d.Id,d.ActionText,"description-only",'\0',19);
        }
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {
            if(DoorBraceActions.IsBraceStock(ParentEntity))
            {
                string material = DoorBraceActions.BraceName(ParentEntity);
                guide.How.Add("Use one plain carried " + material + " beside an ordinary closed unlocked door to brace it.");
                guide.Limits.Add("One action blocks opening from both sides. Remove the same piece to reopen. The door keeps its ordinary strength.");
            }
            var processes=MaterialProcessRegistry.ForSource(ParentEntity).Where(d=>d.Mode=="Process" && d.WorkKind=="ForgeCold" && d.SourceRecipeId.Length==0).ToArray();
            if(processes.Length==0 || ParentEntity.HasPart<MaterialProcessInputPart>())return;
            guide.How.Add("At a forge, select this component in your pack to make: "+string.Join(", ",processes.Select(d=>d.ActionText))+".");
            guide.Limits.Add("One action; the selected component and each listed extra material are used up. Plain unworn inputs only; the finished tool goes into your pack.");
        }

        public override void OnAfterLoad(SaveReader reader)
        {
            SteelBladeFamilyCompatibility.RestoreComponent(this);
        }
    }
}
