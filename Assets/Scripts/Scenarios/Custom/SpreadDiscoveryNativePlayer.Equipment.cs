using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        string _soddenForgedWeaponId;

        // Native UI only: the parts are taken from the actual generated works locker.
        // Reflection reads the cursor/selection; it never assigns one or grants items.
        IEnumerator EquipmentDiscoveryMallet()
        {
            var inventory = Player.GetPart<InventoryPart>();
            var head = inventory.Objects.Single(e => e.BlueprintName == "PeatMalletHeadComponent");
            var haft = inventory.Objects.Single(e => e.BlueprintName == "OakHaftComponent");
            var binding = inventory.Objects.Single(e => e.BlueprintName == "LeatherBindingComponent");
            var screen = inventory.Objects.Single(e => e.BlueprintName == "GroundwireScreen");
            var oldWeapons = inventory.Objects.Where(e => e.HasPart<WeaponAssemblyPart>()).Select(e => e.ID).ToArray();
            Require(!Player.HasPart<BitLockerPart>(), "ordinary player has no developer crafting grant");
            yield return Tap(Key.I);
            for (int n = 0; (int)Field(_input.InventoryUI, "_panel") != 4; n++)
            { Require(n < 6, "bounded actual crafting panel"); yield return Tap(Key.Tab); }
            yield return Tap(Key.F); yield return Tap(Key.C);
            foreach (var component in new[] { head, haft, binding })
            {
                var rows = (IList)Field(_input.InventoryUI, "_craftRows"); int row = -1;
                for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Field(rows[i], "Item"), component)) row = i;
                Require(row >= 0, "earned component in actual crafting rows");
                for (int n = 0; (int)Field(_input.InventoryUI, "_craftCursorIndex") != row; n++)
                { Require(n < 80, "bounded component cursor"); yield return Tap((int)Field(_input.InventoryUI, "_craftCursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
                yield return Tap(Key.Space);
                Require(CraftingMarkPart.IsMarked(component), "actual keyboard selected earned component");
            }
            var preview = (ForgePreview)Field(_input.InventoryUI, "_forgePreview");
            Check("sodden_equipment_preview", preview.IsComplete && preview.Attributes == "Bludgeoning Cudgel"
                && preview.BaseDamage == "1d4" && preview.PenBonus == -1 && preview.HitBonus == 1);
            yield return Capture("equipment-01-earned-mallet-preview");
            // Pack crafting uses the existing free inventory-command path.
            int craftTick = Tick, craftEnergy = Energy;
            yield return Tap(Key.Enter);
            yield return CloseNormal();
            var forged = inventory.Objects.Single(e => e.HasPart<WeaponAssemblyPart>() && !oldWeapons.Contains(e.ID));
            Check("sodden_equipment_forged", forged.GetPart<WeaponAssemblyPart>().BladeBlueprint == head.BlueprintName
                && forged.GetPart<MeleeWeaponPart>().Attributes == preview.Attributes
                && !inventory.Contains(head) && !inventory.Contains(haft) && !inventory.Contains(binding)
                && Tick == craftTick && Energy == craftEnergy);
            _soddenForgedWeaponId = forged.ID;
            // Both Duelist hands start occupied. Explicitly stow that loadout
            // through the UI; successive auto replacements target the same hand.
            var startingHands = inventory.GetAllEquipped()
                .Where(e => e.GetPart<EquippablePart>()?.GetEffectiveSlots() == "Hand").ToArray();
            Require(startingHands.Length == 2, "ordinary Duelist's original two occupied hands");
            foreach (var item in startingHands)
            {
                yield return ItemAction(item, "unequip");
                yield return CloseNormal();
                Require(!InventorySystem.IsEquipped(Player, item) && inventory.Contains(item),
                    "original hand gear stowed through the actual inventory UI");
            }
            foreach (var item in new[] { forged, screen })
            {
                yield return ItemAction(item, "equip_auto");
                if (Field(_input.InventoryUI, "_displaceConfirm") != null) yield return Tap(Key.Enter);
                yield return CloseNormal();
                Require(InventorySystem.IsEquipped(Player, item), "actual acquired gear equipped through inventory");
            }
            Check("sodden_equipment_equipped", InventorySystem.IsEquipped(Player, forged)
                && InventorySystem.IsEquipped(Player, screen) && Player.GetStatValue("ElectricResistance") == 50
                && !Player.GetPart<CavesOfOoo.Skills.SkillsPart>().HasSkill("Cudgel_Conk"));
            yield return Settled();
            var presenter = _input.ZoneRenderer.SpawnRing3D;
            foreach (var item in new[] { forged, screen })
            {
                var style = default(SpreadBiomeStyleEvidence);
                bool approved = presenter != null && presenter.TryGetApprovedEquipmentStyle(Player, item, out style);
                _observations.Add(new { phase="equipment-earned-model", item=item.ID,
                    model=style.ModelId, pieces=style.PieceCount, zone=Zone.ZoneID, approved, failure=style.Failure });
                Require(approved, "earned equipment uses its exact submitted mesh, palette and attachment: "
                    + item.BlueprintName + "/" + style.Failure);
            }
            _observations.Add(new { phase="equipment-earned-assembly", head=head.ID, haft=haft.ID, binding=binding.ID,
                weapon=forged.ID, screen=screen.ID, family=preview.Attributes, electricResistance=Player.GetStatValue("ElectricResistance"),
                bound="Native acquisition, crafting and equip; controlled typed-damage and learned-skill benefit tested separately in EditMode." });
            yield return Capture("equipment-02-earned-mallet-and-screen");
        }
    }
}
