namespace CavesOfOoo.Core
{
    /// <summary>Inspection-only hints for authored second uses. Original live
    /// equipment, food and tonic details remain in ItemExamineService. These
    /// hints do not make an action available or simulate its consequences.</summary>
    public static class ItemTacticalUseDescription
    {
        public static string Describe(Entity item)
        {
            string use;
            switch (item?.BlueprintName)
            {
                case "HealingTonic": case "KnitmossPad":
                    use = "Treat an injured adjacent companion with the healing payload above."; break;
                case "Antidote": case "SumpsievePad":
                    use = "Treat an adjacent companion's ordinary or lingering gas poison. Fresh exposure can poison them again."; break;
                case "BurnSalve": case "SootrootPulp":
                    use = "Treat an adjacent burning companion, extinguishing and cooling them. This gives no lasting fire protection."; break;
                case "Panacea":
                    use = "Treat an adjacent companion's current negative conditions while retaining positive effects."; break;
                case "KnotflaxBandage": case "ClaspbeanPulp": case "MargincressRibbon":
                    use = "Bind an adjacent companion's bleeding. This does not heal other damage or confer immunity."; break;
                case "SoddenFieldDressing":
                    use = "Treat an adjacent companion's ordinary poison and bleeding together. Gas poison and fungal infection remain."; break;
                case "AbsentmintLeaf":
                    use = "Clear an adjacent companion's confusion. Other conditions remain."; break;
                case "CookedMeat": case "ToastedEmberwheat": case "RoastedMushroom": case "RoastedHearthbulb": case "RoastedStarapple":
                    use = "Share this meal with an adjacent companion. Its preparation replaces their previous meal and expires on their turns. Resistance does not grant immunity or prevent freezing."; break;
                case "FieldMeal":
                    use = "Share with an injured or bleeding adjacent companion: heal 3d4 HP and stop one ordinary bleed. This meal grants no prepared-meal bonus."; break;
                case "FireMoss":
                    return Hint("Spend one moss to kindle nearby dry fuel that this small dose can ignite, or an oil film. Bare stone supplies no fuel. Fire can spread and hurt anyone.");
                case "FrostLichen":
                    return Hint("Spend one lichen to freeze nearby water-coated ground into slippery ice. Occupants can freeze, including companions; other liquid films and dry ground are unsuitable.");
                case "GlacierSalt":
                    return Hint("Spend one salt as a cold pack on a nearby hot target. Cooling can put out flames but can also freeze the target; using it on an outsider provokes hostility. It provides no lasting protection.");
                case "EmberFruit":
                    return Hint("Spend one fruit's warmth to help thaw a nearby frozen target. It does not dispel roots, stun or unrelated restraints.");
                case "GlimmerBrine":
                    return Hint("Spend one brine to leave an eight-turn conductive ground film. It prepares a route for a later electrical charge; it creates no charge by itself.");
                case "SparkRoot":
                    return Hint("Spend one root to discharge into a nearby conductor. The electrical route can endanger you and companions. Plain dry ground is unsuitable.");
                case "PrismreedPith":
                    return Hint("Spend one pith to wick one temporary thin liquid coating from nearby ground, or up to 20 oil, pitch or honey from yourself or a willing adjacent companion. Body wicking leaves Wet, poison and burning unchanged. Pools, permanent sources and ice remain.");
                case "LampOil":
                    return Hint("Spend one oil to spread an eight-turn flammable, slippery ground film. Anyone crossing may slip; fire remains dangerous. This competes with refueling and brewing.");
                case "SlipsedgeGel":
                    return Hint("Spend one gel to spread an eight-turn slippery, conductive film. It is not flammable. You and companions can slip or be shocked too.");
                case "PitchpodResin":
                    return Hint("Spend one resin to smear an adjacent uncoated creature with sticky pitch, lowering Agility and DV while coated and increasing fire vulnerability. Uninvited use provokes hostility; attacks and movement remain possible.");
                case "Honeycomb":
                    return Hint("Spend one comb to smear an adjacent uncoated creature with sticky honey, lowering Agility and DV while coated and increasing fire vulnerability. Uninvited use provokes hostility; this gives up its food value.");
                case "Torch":
                    return Hint("Hold a lit, dry torch and touch nearby fuel to ignite it. Costs one fuel in addition to the action. Fire can spread; an unlit or stowed torch cannot do this.");
                case "LampveinFan":
                    return Hint("Hold the fan to disperse up to five density from one nearby transient gas cloud per action. Stable clouds and the stationary sight-blocking veil remain; continued exposure is dangerous.");
                case "GroundwireScreen":
                    return Hint("Hold the screen to discharge your current electrical effect into nearby ground. The charge can endanger creatures on conductive terrain. You cannot act while stunned, and new shocks remain dangerous.");
                case "GripfrondWrap":
                    return Hint("Wear the wraps and brace beside a solid handhold against one physical shove or pull. The stance lasts three of your turns; moving ends it. Damage, spells and teleportation are not blocked.");
                case "IronshodBoots":
                    return Hint("Wear these boots and plant your feet on stable ground against one physical shove or pull. The stance lasts three of your turns; moving ends it. Slippery footing is unsuitable, and their speed penalty remains.");
                case "GlowQuartz":
                    return Hint("Spend one quartz to leave a cracked ground light for thirty world turns, freeing a hand. It cannot be picked back up and does not lure enemies. This competes with equipment infusion.");
                default: return null;
            }
            return Hint(use + " Spend one item and your own action; only a living willing party member within reach can receive it. Self-use and existing throwing remain available.");
        }
        static string Hint(string use) => "Tactical use: " + use
            + (use.Contains("action") ? "" : " Choose its inventory action while in reach; success costs one action.");
    }
}
