using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A finite fire-starter (Docs/MACHINE-INVENTED-USES.md, "Spark"). It adds exactly two things and invents no fire rules of its own:
    /// <list type="bullet">
    /// <item>It answers the shared "is there a flame in reach" lookup (<see cref="IgnitionSources"/>) when a carried striker has a strike left and is dry, so a torch or a finite
    /// cooking fire lights from it through their ordinary verbs; a burning fire in reach is always used first and costs nothing.</item>
    /// <item>It offers "strike a spark" toward an adjacent cell only where one point of heat would light something (<see cref="TileReactionSystem.HeatWouldIgnite"/>), and writes that
    /// heat through <see cref="ZoneTileStateSystem.ApplyFireToTile"/>, the routine every fire ability uses, so an oil slick burns by the shared reactions and whatever the world already does for those fires happens here too. No law, damage or spread
    /// rule is written or bypassed in this class.</item>
    /// </list>
    /// Strikes are finite and are renewed only by spending an iron spike (<see cref="KnapStrikes"/> strikes per spike). Wet or frozen, it will not spark.
    /// </summary>
    public sealed class FlintStrikerPart : Part, IDescribeState
    {
        public override string Name => "FlintStriker";

        public int Strikes = 20;
        public int MaxStrikes = 20;

        public const string SparkPrefix = "StrikeSpark|", KnapPrefix = "KnapStriker|", KnapBlueprint = "IronSpikeComponent";
        public const int KnapStrikes = 10;
        const float WetLimit = 0.35f;

        static readonly (int x, int y, string label)[] Directions =
        {
            (0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),
            (0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };

        // ── what the shared ignition lookup asks ──

        /// <summary>A striker that can make a flame for this actor right now: carried by them, a strike left, and neither wet nor frozen.</summary>
        internal static bool Usable(Entity actor, Entity item)
        {
            var part = item?.GetPart<FlintStrikerPart>();
            return part != null && part.ParentEntity == item && part.Strikes > 0 && WorldResourceActions.Carried(actor, item, false) && Dry(item);
        }

        static bool Dry(Entity item) => !(item.GetEffect<WetEffect>()?.Moisture > WetLimit) && !(item.GetEffect<FrozenEffect>()?.Cold > 0);

        /// <summary>Spends one strike inside the caller's transaction, undone with it. The message and diag run only on commit.</summary>
        internal bool Spend(Entity actor, InventoryTransaction tx, string use)
        {
            if (tx == null || !Usable(actor, ParentEntity)) return false;
            int before = Strikes; var owner = ParentEntity;
            tx.Do(() => Strikes = before - 1, () => Strikes = before);
            tx.AfterCommit(() => { MessageLog.Add("You strike the flint."); Diag.Record("event", "StrikerLit", actor, owner, new { use, remaining = Strikes }); });
            return true;
        }

        // ── the reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;
        public string DescribeState(DescribeContext context)
        {
            if (Strikes <= 0) return "The flint is worn smooth. Knapping a new edge needs an iron spike.";
            return Strikes + (Strikes == 1 ? " strike left." : " strikes left.");
        }

        // ── rows and verbs ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                AddActions(e.GetParameter<Entity>("Actor"), e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == null) return true;
                bool spark = command.StartsWith(SparkPrefix, StringComparison.Ordinal), knap = command.StartsWith(KnapPrefix, StringComparison.Ordinal);
                if (!spark && !knap) return true;
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone"); var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                if (spark ? TrySpark(actor, zone, command, tx) : TryKnap(actor, zone, command, tx)) { e.Handled = true; return false; }
            }
            return true;
        }

        void AddActions(Entity actor, InventoryActionList actions)
        {
            var zone = actor?.SpatialZone;
            if (actions == null || zone == null || !WorldResourceActions.Carried(actor, ParentEntity, false)) return;
            var origin = zone.GetEntityCell(actor);
            if (origin != null && Usable(actor, ParentEntity))
            {
                foreach (var d in Directions)
                {
                    int x = origin.X + d.x, y = origin.Y + d.y;
                    if (zone.GetCell(x, y) == null || !TileReactionSystem.HeatWouldIgnite(zone, x, y)) continue;
                    // A slick you are standing in burns toward you: say so on the row, before the strike is spent.
                    string warning = TileReactionSystem.HeatWouldIgnite(zone, origin.X, origin.Y) ? " (you are standing in it)" : "";
                    actions.AddAction("StrikeSpark", "strike a spark " + d.label + warning, SparkCommand(zone, origin, x, y), '\0', 16, false, ActionTime.OneAction);
                }
            }
            if (Strikes >= MaxStrikes) return;
            if (Spike(actor) != null)
                actions.AddAction("KnapStriker", "knap a new edge (1 iron spike, " + KnapStrikes + " strikes)", KnapPrefix + ParentEntity.ID, '\0', 15, false, ActionTime.OneAction);
            else
                actions.AddDisabled("KnapStriker", "knap a new edge", "no-spike", "Knapping a new edge needs an iron spike.", '\0', 15);
        }

        string SparkCommand(Zone zone, Cell origin, int x, int y)
            => SparkPrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + N(x) + "|" + N(y) + "|" + ParentEntity.ID;

        static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        static bool Int(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        static Entity Spike(Entity actor)
            => actor?.GetPart<InventoryPart>()?.Objects.FirstOrDefault(o => o != null && o.BlueprintName == KnapBlueprint && WorldResourceActions.Carried(actor, o, false));

        bool TrySpark(Entity actor, Zone zone, string command, InventoryTransaction transaction)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, "actor_unavailable", "You cannot strike a light right now.");
            if (f.Length != 7 || !Int(f[2], out int ox) || !Int(f[3], out int oy) || !Int(f[4], out int x) || !Int(f[5], out int y)) return Reject(actor, "malformed_selection", "That is not a spark you can strike.");
            var origin = zone.GetEntityCell(actor);
            if (Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[6] != ParentEntity.ID || origin == null || origin.X != ox || origin.Y != oy || !WorldResourceActions.Carried(actor, ParentEntity, false))
                return Reject(actor, "stale_selection", "You have moved since you chose that.");
            if (Math.Abs((long)x - origin.X) > 1 || Math.Abs((long)y - origin.Y) > 1 || (x == origin.X && y == origin.Y) || zone.GetCell(x, y) == null)
                return Reject(actor, "not_adjacent", "You can only strike a spark at the cell beside you.");
            if (!Usable(actor, ParentEntity)) return Reject(actor, "striker_unusable", Strikes <= 0 ? "The flint is worn smooth." : "The flint will not spark while it is wet or frozen.");
            if (!TileReactionSystem.HeatWouldIgnite(zone, x, y)) return Reject(actor, "nothing_to_catch", "A spark there would catch on nothing.");
            bool own = transaction == null; transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(ParentEntity, actor, command)) return Reject(actor, "in_progress", "The flint is already in use.");
                int before = Strikes; var owner = ParentEntity;
                transaction.Do(() => Strikes = before - 1, () => Strikes = before);
                // The world write happens once, after the action commits: tile heat cannot be taken back, so an outer failure must never leave a fire with the strike returned.
                transaction.AfterCommit(() =>
                {
                    MessageLog.Add("You strike a spark.");
                    ZoneTileStateSystem.ApplyFireToTile(zone, x, y, actor, "FlintStriker");
                    ZoneTileStateSystem.ResolveAfterAbility(zone, actor);
                    Diag.Record("event", "StrikerSpark", actor, owner, new { x, y, remaining = Strikes });
                });
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        bool TryKnap(Entity actor, Zone zone, string command, InventoryTransaction transaction)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, "actor_unavailable", "You cannot knap the flint right now.");
            if (f.Length != 2 || f[1] != ParentEntity.ID || !WorldResourceActions.Carried(actor, ParentEntity, false)) return Reject(actor, "stale_selection", "You are no longer holding that.");
            if (Strikes >= MaxStrikes) return Reject(actor, "already_keen", "The flint already has its full edge.");
            var spike = Spike(actor); if (spike == null) return Reject(actor, "no_spike", "Knapping a new edge needs an iron spike.");
            bool own = transaction == null; transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(ParentEntity, actor, command) || !transaction.TryClaim(spike, actor, command))
                    return Reject(actor, "in_progress", "The flint is already in use.");
                var pack = actor.GetPart<InventoryPart>(); var receipt = InventoryTransferSnapshot.Capture(pack); int before = Strikes; var owner = ParentEntity;
                transaction.Do(null, () => { receipt.Restore(); Strikes = before; });
                if (!receipt.Apply(() => pack.TryConsumeOne(spike)) || !receipt.ClaimChanges(transaction, actor, command)) return Reject(actor, "spike_unavailable", "The spike could not be spent.");
                Strikes = Math.Min(MaxStrikes, Math.Max(0, before) + KnapStrikes); // a corrupt negative count is treated as worn, not as a debt
                transaction.AfterCommit(() =>
                {
                    MessageLog.Add("You knap a fresh edge on the flint with an iron spike.");
                    Diag.Record("event", "StrikerKnapped", actor, owner, new { strikes = Strikes });
                });
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        bool Reject(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            Diag.Record("event", "StrikerRejected", actor, ParentEntity, new { reason });
            return false;
        }
    }
}
