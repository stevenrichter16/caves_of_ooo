namespace CavesOfOoo.Core
{
    /// <summary>
    /// One exact, player-readable line per <see cref="Effect"/> instance,
    /// read from the LIVE fields — so clamps, defaults, and remaining
    /// durations are reported as they truly are. Shared by the tonic
    /// Examine popup (constructed-effect preview), the look-mode FOCUS
    /// panel, and entity Examine (live afflictions). Keep the wording
    /// CP437-safe: plain ASCII only.
    /// </summary>
    public static class EffectDescriber
    {
        public static string Describe(Effect effect)
        {
            switch (effect)
            {
                case TonicStatSurgeEffect surge:
                    return surge.StatName + " surge: +" + surge.Amount + " for " + surge.Duration + " turns; repeat doses refresh, not stack.";
                case BrokenEffect broken:
                    return broken.PenaltyApplied
                        ? "Broken: " + (broken.HitPenalty > 0 ? "−" + broken.HitPenalty + " melee accuracy. " : "")
                            + (broken.ArmorPenalty > 0 ? "−" + broken.ArmorPenalty + " armor. " : "")
                            + "Unequip and repair with the listed material."
                        : "Broken: damaged condition.";
                case PreparedMealEffect meal:
                    return "Prepared meal - " + meal.Describe();
                case LeyTapEffect ley:
                    return "Ley Tap - 1 damaging cast: +" + ley.BonusDamage
                        + " damage per target; " + ley.Duration + " of your turns left.";
                case HeartFlameEffect heart:
                    return "Heart Flame - " + heart.ChargesRemaining
                        + " Fire/Heat damage casts at +100% base damage; "
                        + heart.Duration + " of your turns left.";
                case PoisonedEffect p:
                    return "Poisoned - " + p.DamageDice + " damage a turn for "
                        + p.Duration + " turns.";
                case BurningEffect b:
                    return "Set ablaze - burning at intensity "
                        + b.Intensity.ToString("0.#") + " until doused.";
                case AcidicEffect a:
                {
                    int perTurn = 1 + (int)System.Math.Floor(a.Corrosion * 4f);
                    return "Corroding acid - " + perTurn
                        + " damage a turn while the coating lasts.";
                }
                case ElectrifiedEffect el:
                    return "Electrified - charge " + el.Charge.ToString("0.#")
                        + " for " + el.Duration + " turns.";
                case SteamEffect steam:
                    return SteamContact.SourceRejectionReason(steam.Owner, steam) == null
                        ? "Hot steam - scalds creatures on or beside its source for 2 Heat damage per pulse (Heat resistance applies)."
                        : "Steam - cools and lightly wets neighboring objects as it fades.";
                case WetEffect w:
                    return "Soaked - " + (w.Moisture * 100f).ToString("0")
                        + "% drenched (douses flame, conducts shock).";
                case FrozenEffect fz:
                    return "Frozen over - " + (fz.Cold * 100f).ToString("0")
                        + "% iced; thaws in about " + fz.TurnsToThaw
                        + (fz.TurnsToThaw == 1 ? " turn" : " turns") + " (fire thaws it faster).";
                case StoneskinEffect s:
                    return "Stoneskin - incoming damage reduced by " + s.Reduction
                        + " for " + s.Duration + " turns.";
                case BleedingEffect bl:
                    return "Bleeding - " + bl.DamageDice
                        + " damage a turn until staunched (save " + bl.SaveTarget + ").";
                case CharredEffect _:
                    return "Charred - scorched, and far less combustible.";

                // --- world / material afflictions (live-play finding:
                //     "drenched" is LiquidCoveredEffect, not WetEffect) ---
                case LiquidCoveredEffect lc:
                    return "Drenched in "
                        + (string.IsNullOrEmpty(lc.LiquidId) ? "liquid" : lc.LiquidId)
                        + " (" + lc.Amount + ").";
                case CoatedInPlasmaEffect _:
                    return "Coated in plasma - heat and cold bite far deeper.";
                case PoisonedByGasEffect pg:
                    return "Choking - " + pg.DamagePerTurn + " damage a turn from "
                        + pg.GasTypeKey.ToLowerInvariant() + " gas" + ForTurns(pg.Duration) + ".";
                case AsleepByGasEffect asleep:
                    return "Asleep - helpless until damaged or it wears off"
                        + ForTurns(asleep.Duration) + ".";
                case FungalInfectionEffect fi:
                    return "Fungal infection - spreading (" + fi.TurnsInfected + " turns in).";
                case SmolderingEffect sm:
                    return "Smoldering - about to catch fire" + ForTurns(sm.Duration) + ".";

                // --- control / combat afflictions ---
                case StunnedEffect st:
                    return st.SaveTarget > 0
                        ? "Stunned - can't act" + ForTurns(st.Duration)
                            + " (Toughness may shake it off, save " + st.SaveTarget + ")."
                        : "Stunned - can't act" + ForTurns(st.Duration) + ".";
                case ParalyzedEffect pz:
                    return "Paralyzed - can't move or act" + ForTurns(pz.Duration) + ".";
                case ConfusedEffect cf:
                    return "Confused - staggers unpredictably" + ForTurns(cf.Duration) + ".";
                case RootedEffect rt:
                    return "Rooted - held in place" + ForTurns(rt.Duration) + ".";
                case HobbledEffect hb:
                    return "Hobbled - -3 DV (easier to hit)" + ForTurns(hb.Duration) + ".";
                case WeakenedEffect wk:
                    return "Weakened - -" + wk.StrPenalty + " Strength" + ForTurns(wk.Duration) + ".";
                case HookedEffect hk:
                    return "Hooked - dragged and held (save " + hk.SaveTarget + ").";
                case ShatterArmorEffect sa:
                    return "Armor shattered x" + sa.StackCount + " - protection reduced.";
                case BerserkEffect bz:
                    return "Berserk - lashing out at anything near" + ForTurns(bz.Duration) + ".";

                case ParchedEffect pa:
                    return "Parched x" + pa.Stacks + " - -" + pa.Stacks
                        + " Strength and Agility until watered.";
                case UnderTheClothEffect oath:
                    return "Under the cloth - guest-right holds"
                        + " (" + System.Math.Max(0, oath.ExpiryTick - WorldClock.CurrentTick)
                        + " ticks remain).";

                case null:
                    return "Nothing.";
                default:
                {
                    // Presentable fallback for effects without bespoke
                    // wording: cleaned type name + remaining duration.
                    string label = CleanTypeName(effect);
                    if (effect.Duration > 0)
                        return label + " - " + effect.Duration + " turns left.";
                    return label + ".";
                }
            }
        }

        /// <summary>" for N turns" when finite; empty for indefinite.</summary>
        private static string ForTurns(int duration)
        {
            return duration > 0 ? " for " + duration + " turns" : "";
        }

        /// <summary>"PoisonedEffect" -> "Poisoned" for unknown effect types.</summary>
        private static string CleanTypeName(Effect effect)
        {
            string name = effect.GetType().Name;
            const string suffix = "Effect";
            if (name.EndsWith(suffix) && name.Length > suffix.Length)
                name = name.Substring(0, name.Length - suffix.Length);
            return name;
        }
    }
}
