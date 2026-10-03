namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM7 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §4.1, pillar 3)
    /// — ink charges on a grimoire.
    ///
    /// <para><b>The third "oomph" pillar.</b> Skill spells cost only a
    /// cooldown. A rite additionally spends a charge from any carried inked
    /// grimoire, which is what stops rites from simply being
    /// better skills.</para>
    ///
    /// <para><b>Why the book still matters after you read it.</b>
    /// <see cref="GrimoirePart"/> does NOT destroy a grimoire on reading
    /// — the book stays in the inventory. That turns out to be exactly
    /// the right behaviour for this design: reading TEACHES the rite,
    /// but carrying the inked book is what lets you CAST it. A grimoire
    /// is a physical thing you keep and maintain, not a one-time unlock,
    /// and a depleted one is a real inventory problem.</para>
    ///
    /// <para><b>Rhythmic, not precious</b> (user decision, 2026-08-09):
    /// ~10 charges with cheap re-inking. The prime→detonate loop should
    /// become a habit before it becomes a ration.</para>
    /// </summary>
    public class GrimoireChargePart : Part
    {
        public override string Name => "GrimoireCharge";
        public const string ReinkCommand = "ReinkGrimoire";

        /// <summary>Ink remaining. Public field so the reflection-based
        /// save path round-trips it without special handling.</summary>
        public int Charges = 10;

        /// <summary>Ceiling for re-inking.</summary>
        public int MaxCharges = 10;

        /// <summary>Charges restored per <c>InkVial</c> consumed.</summary>
        public int ChargesPerVial = 5;

        public bool HasCharge => Charges > 0;

        /// <summary>Spends one charge. False when empty — callers must
        /// treat that as a refusal, not a free cast.</summary>
        public bool TrySpend()
        {
            if (Charges <= 0) return false;
            Charges--;
            return true;
        }

        /// <summary>Refills, clamped. Returns how many were actually
        /// added, so a caller can decline to consume a vial that would
        /// be wasted on a nearly-full book.</summary>
        public int Refill(int amount)
        {
            if (amount <= 0 || Charges < 0 || MaxCharges <= 0 || Charges >= MaxCharges) return 0;
            int before = Charges;
            Charges = (int)System.Math.Min((long)MaxCharges, (long)Charges + amount);
            return Charges - before;
        }

        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");
            if(e.ID=="GetInventoryActions")
            {
                int gain=GrimoireInkService.PreviewGain(actor,this);
                if(gain>0)e.GetParameter<InventoryActionList>("Actions")?.AddAction("Reink",
                    "re-ink (+"+gain+" charge"+(gain==1?"":"s")+"; 1 ink vial)",ReinkCommand,'i',19);
                return true;
            }
            if(e.ID!="InventoryAction"||e.GetStringParameter("Command")!=ReinkCommand)return true;
            if(e.GetParameter<bool>("GrimoireReinkAttempted"))return true;
            e.SetParameter("GrimoireReinkAttempted",true);
            if(!GrimoireInkService.TryReink(actor,this,e.GetParameter<Zone>("Zone"),
                e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction")))return true;
            e.Handled=true;return false;
        }
    }
}
