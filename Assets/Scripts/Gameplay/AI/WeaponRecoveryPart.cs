using CavesOfOoo.Core.Inventory.Commands;

namespace CavesOfOoo.Core
{
    /// <summary>Remember an actual disarm, then pay separate ordinary actions for approach, pickup and equip.</summary>
    public sealed class WeaponRecoveryPart : Part
    {
        public override string Name=>"WeaponRecovery";
        public Entity RecoveryWeapon;
        public override bool HandleEvent(GameEvent e)
        {
            if(e?.ID=="WeaponDisarmed")
            {
                var weapon=e.GetParameter<Entity>("Weapon");var zone=ParentEntity?.SpatialZone;
                if(zone!=null&&weapon?.SpatialZone==zone&&zone.GetEntityCell(weapon)==zone.GetEntityCell(ParentEntity)
                    &&weapon.HasPart<MeleeWeaponPart>())RecoveryWeapon=weapon;
            }
            return true;
        }
        public bool TryRecover(Zone zone)
        {
            var actor=ParentEntity;var weapon=RecoveryWeapon;var brain=actor?.GetPart<BrainPart>();
            if(actor==null||weapon==null||zone==null||actor.SpatialZone!=zone||actor.HasTag("Player")
                ||actor.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(actor)||brain?.CurrentZone!=zone
                ||brain.HasGoal<NoFightGoal>()||actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true)return false;
            if(InventorySystem.IsEquipped(actor,weapon)){RecoveryWeapon=null;return false;}
            var physical=weapon.GetPart<PhysicsPart>();var inventory=actor.GetPart<InventoryPart>();
            if(physical?.InInventory==actor&&inventory?.Contains(weapon)==true)
            {
                bool equipped=InventorySystem.Equip(actor,weapon);
                if(equipped)RecoveryWeapon=null;
                return equipped;
            }
            if(physical==null||physical.InInventory!=null||physical.Equipped!=null||weapon.SpatialZone!=zone
                ||zone.GetEntityCell(weapon)==null||!physical.Takeable||!weapon.HasPart<MeleeWeaponPart>())
            {RecoveryWeapon=null;return false;}
            var at=zone.GetEntityCell(actor);var item=zone.GetEntityCell(weapon);
            int distance=AIHelpers.ChebyshevDistance(at.X,at.Y,item.X,item.Y);
            if(distance>2){RecoveryWeapon=null;return false;}
            if(distance==0)return InventorySystem.ExecuteCommand(new PickupCommand(weapon, autoEquip: false),actor,zone).Success;
            return AIHelpers.TryStepToward(actor,zone,at.X,at.Y,item.X,item.Y);
        }
    }
}
