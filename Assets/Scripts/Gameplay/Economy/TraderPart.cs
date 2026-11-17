using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// TRADE STOCK FIX — blueprint-authorable shop stock for any
    /// talkable NPC.
    ///
    /// <para><b>The bug this closes.</b>
    /// <see cref="ConversationManager.RefreshVisibleChoices"/> injects a
    /// "[Let's trade.]" choice for <i>any</i> speaker with an
    /// <see cref="InventoryPart"/> — and every creature inherits one
    /// from the <c>Creature</c> blueprint. So all 31 talkable NPCs
    /// offered trade, while only the five town shopkeepers (stocked by
    /// the <c>shop:</c> structure-stamp marker at
    /// LandmarkBuilder.cs:862-885) ever had anything. Every other
    /// merchant, envoy, hermit and villager opened an empty window.</para>
    ///
    /// <para><b>How it works.</b> This part sets the
    /// <see cref="TraderRestockSystem.ShopStockTableProp"/> property and
    /// a starting purse at spawn, then rolls the opening stock. From
    /// there the EXISTING restock system takes over: it re-rolls the
    /// same table whenever the shelf drops below
    /// <see cref="TraderRestockSystem.ShelfLowWaterMark"/>. No changes
    /// to trade or restock code — this part just supplies what they
    /// were always waiting for.</para>
    ///
    /// <code>
    /// { "Name": "Trader", "Params": [
    ///     { "Key": "StockTable", "Value": "HermitStock" },
    ///     { "Key": "Drams", "Value": "60" } ]}
    /// </code>
    /// </summary>
    public class TraderPart : Part
    {
        public override string Name => "Trader";

        /// <summary>Loot table rolled for stock (and re-rolled by
        /// TraderRestockSystem when the shelf runs low).</summary>
        public string StockTable = "";

        /// <summary>Starting purse. TraderRestockSystem SKIPS any entity
        /// whose drams property is missing (&lt; 0), so a trader without
        /// a purse would never restock either.</summary>
        public int Drams = 40;

        /// <summary>Optional finite opening allocation, independent of renewable
        /// StockTable rolls. Loading an old trader never applies a new packet.</summary>
        public string OpeningStockBlueprint = "";
        public int OpeningStockUnits;
        public bool OpeningStockSupplied;

        /// <summary>Injected by GameBootstrap; null = graceful no-op
        /// (the CorpsePart.Factory convention).</summary>
        public static EntityFactory Factory;

        /// <summary>Deterministic override for tests.</summary>
        public static System.Random Rng;

        public override void OnAfterLoad(SaveReader reader)
        {
            LoadoutKeeperPart.UpgradeExistingTrader(ParentEntity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ObjectCreated")
            { Apply(); ApplyOpeningStock(); }
            return true;
        }

        private void ApplyOpeningStock()
        {
            if(OpeningStockSupplied || string.IsNullOrEmpty(OpeningStockBlueprint))return;
            // Fence before any creation/weight callbacks, including failure. This
            // is one opening opportunity, not a missing-stock retry on every visit.
            OpeningStockSupplied=true;
            if(OpeningStockUnits<1 || OpeningStockUnits>64 || Factory==null)return;
            var pack=ParentEntity?.GetPart<InventoryPart>();if(pack?.ParentEntity!=ParentEntity)return;
            var template=MaterialProcessTemplate.Capture(Factory,OpeningStockBlueprint,true);if(template==null)return;
            var products=new System.Collections.Generic.List<Entity>();
            CavesOfOoo.Core.Inventory.InventoryTransferSnapshot receipt=null;
            try
            {
                for(int n=0;n<OpeningStockUnits;n++)
                {
                    if(!template.Current())return;
                    var item=Factory.CreateEntity(OpeningStockBlueprint);
                    if(!PreparationActions.Fresh(item,OpeningStockBlueprint) || template.Snapshot(item)==null)return;
                    products.Add(item);
                }
                receipt=CavesOfOoo.Core.Inventory.InventoryTransferSnapshot.Capture(pack,products.ToArray());
                if(!receipt.Apply(()=>
                {
                    foreach(var item in products)
                        if(!pack.AddCraftedUnit(item,out var owner) || owner==null || template.Snapshot(owner)==null)return false;
                    return template.Current() && (pack.MaxWeight<0 || pack.GetCarriedWeight()<=pack.MaxWeight);
                }))receipt.Restore();
            }
            catch(System.Exception){receipt?.Restore();}
        }

        private void Apply()
        {
            if (ParentEntity == null) return;

            // Purse first: the restock system needs it even when the
            // stock roll can't run (no factory in headless contexts).
            if (TradeSystem.GetDrams(ParentEntity) <= 0)
                TradeSystem.SetDrams(ParentEntity, Drams);

            if (string.IsNullOrEmpty(StockTable)) return;
            // Entity has GetProperty but no SetProperty — string props
            // are written straight into the dict (same as
            // LandmarkBuilder.cs:871 does for shop stamps).
            ParentEntity.Properties[TraderRestockSystem.ShopStockTableProp] = StockTable;

            if (Factory == null) return;
            var inv = ParentEntity.GetPart<InventoryPart>();
            if (inv == null) return;

            // Only stock an EMPTY shelf. The five town shopkeepers are
            // already stocked by their shop: stamp before this runs in
            // some orders; double-stocking them would quietly double
            // the town economy.
            if (inv.Objects.Count > 0) return;

            var rng = Rng ?? new System.Random();
            var rolled = LootTableRegistry.Roll(StockTable, rng);
            if (rolled == null) return;
            for (int i = 0; i < rolled.Count; i++)
            {
                Entity item;
                try { item = Factory.CreateEntity(rolled[i]); }
                catch (System.Exception) { continue; }
                if (item != null) inv.AddObject(item);
            }
        }
    }
}
