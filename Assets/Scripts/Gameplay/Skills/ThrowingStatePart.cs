using System;

namespace CavesOfOoo.Core
{
    /// <summary>Actor-owned throwing state. Removing a skill never removes its outstanding debt.</summary>
    public sealed class ThrowingStatePart : Part
    {
        public int MeasuredCooldown, DrivingCooldown, RecoveryCooldown;
        public int ThrowSerial, MovementRevision, PreparationSerial;
        public bool HasPreparation, PreparationClosingTurn;
        public Entity PreparedSource, PreparedTarget;
        public string PreparedZone = "", PreparedSourcePlace = "", PreparedBlueprint = "";
        public int PreparedX, PreparedY, PreparedQuantity, PreparedRevision;
        [NonSerialized] internal Action RelocationCommitted;
        public bool Prepared => HasPreparation && ThrowingRules.PreparationCurrent(this);

        public void ClearPreparation()
        {
            HasPreparation = PreparationClosingTurn = false;
            PreparedSource = PreparedTarget = null;
            PreparedZone = PreparedSourcePlace = PreparedBlueprint = "";
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "AfterMove") ClearPreparation();
            if (e.ID == "EffectApplied")
            {
                var effect = e.GetParameter<Effect>("Effect");
                if (effect?.Owner == ParentEntity && effect.Duration != 0
                    && (effect is StunnedEffect || effect is FrozenEffect || effect is ParalyzedEffect))
                    ClearPreparation();
            }
            return true;
        }

        public override void FinalizeLoad(SaveReader reader)
        {
            MeasuredCooldown = Math.Max(0, Math.Min(6, MeasuredCooldown));
            DrivingCooldown = Math.Max(0, Math.Min(12, DrivingCooldown));
            RecoveryCooldown = Math.Max(0, Math.Min(6, RecoveryCooldown));
            if (!Prepared) ClearPreparation();
            ThrowingRules.SynchronizeAbilityDebt(ParentEntity);
        }
    }

    /// <summary>Exact ground-unit receipt and mutation generation. Never grants ownership permission.</summary>
    public sealed class ThrownRecoveryPart : Part
    {
        public string ThrowerId = "", ItemId = "", Blueprint = "", ZoneId = "";
        public int Serial, X, Y, Revision;
        public void Invalidate()
        {
            ThrowerId = ItemId = Blueprint = ZoneId = "";
            Serial = 0;
            // Overflow fails closed for old preparation signatures.
            Revision = Revision == int.MaxValue ? int.MinValue : Revision + 1;
        }
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Taken" || e.ID == "Destroyed") Invalidate();
            return true;
        }
    }
}
