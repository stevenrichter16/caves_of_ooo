using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

// Native skills execute normally. The only fixtures are explicitly granted
// skills/reagents, inert target bodies, and read-only command/FX observers.
internal static partial class Program
{
    private static void RunTargetingContracts()
    {
        Run("ability previews and cancellation preserve resources, turns and FX", AbilityPreviewIsReadOnly);
        Run("directional targeting requires a unit heading and omits TargetCell", DirectionalAbilityContract);
        Run("directional abilities reject headings outside every local-map edge", DirectionalMapEdgeContract);
        Run("adjacent targeting rejects self and sends the exact selected cell", AdjacentAbilityContract);
        Run("self-centered activation ignores coordinates and omits TargetCell", SelfAbilityContract);
        Run("ability confirmations reject stale, cancelled and reused tokens", AbilityTokenContract);
        Run("Vault requires explicit consent before attacking a neutral target", NeutralVaultContract);
        Run("Vault confirmation rechecks target, landing, source and ownership", ChangedVaultContract);
        Run("rite readings stay free on cooldown and conceal hidden candidates", RiteReadingContract);
    }

    private static void AbilityPreviewIsReadOnly()
    {
        Game.NewGame("classic", 8128, "Preview contract");
        var player = Player(); var zone = CurrentZone(player); var ability = OwnedAbility("CommandEmberSpit");
        var observer = ObserveCommand(player, ability.Command);
        try
        {
            var before = State(); var resources = TargetingResources(player);
            using var capture = new SpellFxCapture("ContractPreviewObserver", zone, player);
            var preview = PreviewAbility(ability, dx: 1, dy: 0);
            RequireSuccess(preview, "Previewing an owned ability");
            Check(!string.IsNullOrWhiteSpace(PreviewToken(preview)), "A valid preview requires a confirmation token.");
            Check(!string.IsNullOrWhiteSpace(Text(AbilityPreviewView(preview), "details")), "A preview must include actual ability details.");
            SamePlayerState(before, preview, "Reading an ability preview");
            Check(observer.Commands == 0 && resources == TargetingResources(player), "Preview executed the skill or changed resources.");
            Check(CapturedFxEntries(capture) == 0, "Preview wrote into an active native FX capture.");
            var closed = Send("close"); SamePlayerState(before, closed, "Cancelling an ability preview");
            Check(resources == TargetingResources(player) && observer.Commands == 0, "Cancelling spent resources or dispatched the skill.");
        }
        finally { player.RemovePart(observer); }
    }

    private static void DirectionalAbilityContract()
    {
        Game.NewGame("classic", 8128, "Direction contract");
        var player = Player(); var zone = CurrentZone(player); var ability = OwnedAbility("CommandEmberSpit"); var before = State();
        var source = zone.GetEntityCell(player); var target = AddTargetingBody(player, zone, 2, "directional");
        var targetCell = zone.GetEntityCell(target); int dx = Math.Sign(targetCell.X - source.X), dy = Math.Sign(targetCell.Y - source.Y);
        var wall = new Entity { ID = "web-targeting-spell-wall", BlueprintName = "ContractSpellWall" };
        // A native Solid-only wall stops the trace without becoming an elemental target.
        wall.SetTag("Solid"); wall.SetTag("Indestructible");
        var observer = ObserveCommand(player, ability.Command); var turns = new EndTurnProbe(); player.AddPart(turns);
        try
        {
            foreach (var args in new Dictionary<string, object?>[]
            {
                new() { ["id"] = ability.ID.ToString(), ["x"] = Number(before, "x") + 3, ["y"] = Number(before, "y") + 1 },
                new() { ["id"] = ability.ID.ToString(), ["dx"] = 2, ["dy"] = 0 },
                new() { ["id"] = ability.ID.ToString(), ["dx"] = 0, ["dy"] = 0 },
                new() { ["id"] = ability.ID.ToString(), ["dx"] = int.MinValue, ["dy"] = int.MaxValue }
            })
                RejectedTargeting(before, Send("abilityPreview", args), "A missing or non-unit direction");
            RejectedTargeting(before, Send("ability", new() { ["id"] = ability.ID.ToString(), ["x"] = Number(before, "x") + 3, ["y"] = Number(before, "y") + 1 }), "Legacy off-axis direction");
            RejectedTargeting(before, Send("ability", new() { ["id"] = ability.ID.ToString(), ["x"] = int.MinValue, ["y"] = int.MaxValue }), "Extreme legacy coordinates");
            CheckControlDiagnostic(player, "ability", "BrowserActionRejected");
            Check(observer.Commands == 0, "Rejected geometry reached the native skill.");
            Check(!AbilityTargeting.IsElementalTarget(wall, player) && zone.AddEntity(wall, source.X + dx, source.Y + dy), "Could not place the non-targetable line blocker.");
            int targetHp = target.GetStatValue("Hitpoints");
            var blockedPreview = PreviewAbility(ability, dx: dx, dy: dy); RequireSuccess(blockedPreview, "Reviewing a blocked heading");
            RejectedTargeting(before, ConfirmAbility(blockedPreview), "Native blocked directional cast");
            Check(observer.Commands == 1 && turns.Count == 0 && ability.CooldownRemaining == 0 && target.GetStatValue("Hitpoints") == targetHp,
                "The blocked native cast spent a turn, cooldown or target HP.");
            CheckControlDiagnostic(player, "abilityConfirm", "BrowserActionRejected");
            zone.RemoveEntity(wall);
            var preview = PreviewAbility(ability, dx: dx, dy: dy); RequireSuccess(preview, "Choosing the clear target heading");
            var view = AbilityPreviewView(preview); var direction = view["direction"] as JsonObject;
            Check(Text(view, "mode") == "DirectionLine" && direction != null && Number(direction, "x") == dx && Number(direction, "y") == dy, "The preview changed the selected heading.");
            var fired = ConfirmAbility(preview); RequireSuccess(fired, "Confirming the native directional cast");
            Check(observer.Commands == 2 && observer.DirectionX == dx && observer.DirectionY == dy && !observer.HasTargetCell, "A DirectionLine event must contain only its selected direction, not a TargetCell.");
            Check(observer.SourceCell == CurrentZone(player).GetEntityCell(player), "The command did not use the actual player source cell.");
            Check(turns.Count == 1 && Number(fired, "tick") > Number(before, "tick") && ability.CooldownRemaining > 0 && target.GetStatValue("Hitpoints") < targetHp,
                "A clear native cast must damage the target and pay one turn and its real cooldown.");
            CheckControlDiagnostic(player, "abilityConfirm", "BrowserActionRouted");
        }
        finally { zone.RemoveEntity(wall); zone.RemoveEntity(target); player.RemovePart(observer); player.RemovePart(turns); }
    }

    private static void AdjacentAbilityContract()
    {
        Game.NewGame("breaker", 8128, "Adjacent contract");
        var player = Player(); var zone = CurrentZone(player); var ability = OwnedAbility("CommandConk");
        var target = AddTargetingBody(player, zone, 1, "adjacent"); var cell = zone.GetEntityCell(target);
        var observer = ObserveCommand(player, ability.Command); var turns = new EndTurnProbe(); player.AddPart(turns);
        try
        {
            var before = State(); var origin = zone.GetEntityCell(player);
            RejectedTargeting(before, PreviewAbility(ability, x: origin.X, y: origin.Y), "Selecting self for an adjacent strike");
            RejectedTargeting(before, PreviewAbility(ability, x: int.MinValue, y: int.MaxValue), "Extreme adjacent coordinates");
            var preview = PreviewAbility(ability, x: cell.X, y: cell.Y); RequireSuccess(preview, "Selecting the adjacent target");
            SamePlayerState(before, preview, "Previewing the adjacent strike");
            var fired = ConfirmAbility(preview); RequireSuccess(fired, "Confirming the real Conk skill");
            Check(observer.Commands == 1 && observer.HasTargetCell && ReferenceEquals(observer.TargetCell, cell), "AdjacentCell must send the exact selected native cell.");
            Check(observer.DirectionX == cell.X - origin.X && observer.DirectionY == cell.Y - origin.Y, "AdjacentCell sent a different direction from its target.");
            Check(turns.Count == 1 && ability.CooldownRemaining > 0, "Conk must resolve through its native paid skill pipeline.");
        }
        finally { zone.RemoveEntity(target); player.RemovePart(observer); player.RemovePart(turns); }
    }

    private static void DirectionalMapEdgeContract()
    {
        Game.NewGame("classic", 8128, "Direction edge contract");
        var player = Player(); var zone = CurrentZone(player); var ability = OwnedAbility("CommandJetBlast");
        var observer = ObserveCommand(player, ability.Command); var turns = new EndTurnProbe(); player.AddPart(turns);
        try
        {
            foreach (var edge in new[]
            {
                (x: 0, y: Zone.Height / 2, dx: -1, dy: 0), (x: Zone.Width - 1, y: Zone.Height / 2, dx: 1, dy: 0),
                (x: Zone.Width / 2, y: 0, dx: 0, dy: -1), (x: Zone.Width / 2, y: Zone.Height - 1, dx: 0, dy: 1)
            })
            {
                Check(zone.MoveEntity(player, edge.x, edge.y), "Could not place the player at the bounded edge fixture.");
                var before = State();
                RejectedTargeting(before, Send("ability", new() { ["id"] = ability.ID.ToString(), ["x"] = edge.x + edge.dx, ["y"] = edge.y + edge.dy }), "Direct directional action outside the zone");
                RejectedTargeting(before, PreviewAbility(ability, dx: edge.dx, dy: edge.dy), "Previewing a heading outside the zone");
                Check(observer.Commands == 0 && turns.Count == 0 && ability.CooldownRemaining == 0, "An out-of-bounds heading reached the native skill or spent resources.");
            }
        }
        finally { player.RemovePart(observer); player.RemovePart(turns); }
    }

    private static void SelfAbilityContract()
    {
        Game.NewGame("warden", 8128, "Self contract");
        var player = Player(); var ability = OwnedAbility("CommandEnGarde"); var observer = ObserveCommand(player, ability.Command);
        try
        {
            var before = State();
            var preview = PreviewAbility(ability, x: -999, y: 999, dx: 8, dy: -8); RequireSuccess(preview, "Previewing a self-centered ability");
            SamePlayerState(before, preview, "Reading a self-centered preview");
            Check(observer.Commands == 0, "Self-centered preview activated the stance.");
            var fired = ConfirmAbility(preview); RequireSuccess(fired, "Confirming En Garde");
            Check(observer.Commands == 1 && observer.DirectionX == 0 && observer.DirectionY == 0 && !observer.HasTargetCell, "SelfCentered must discard target coordinates and omit TargetCell.");
            Check(player.HasEffect<EnGardeEffect>() && ability.CooldownRemaining > 0 && Number(fired, "tick") > Number(before, "tick"), "The self-centered command did not apply its native guard and turn.");
        }
        finally { player.RemovePart(observer); }
    }

    private static void AbilityTokenContract()
    {
        Game.NewGame("warden", 8128, "Token contract");
        var player = Player(); var ability = OwnedAbility("CommandEnGarde"); var observer = ObserveCommand(player, ability.Command);
        try
        {
            var preview = PreviewAbility(ability); RequireSuccess(preview, "Creating the stale token control");
            RejectedTargeting(preview, Send("abilityConfirm", new() { ["token"] = PreviewToken(preview) }, Number(preview, "revision") - 1), "Stale confirmation revision");
            Check(observer.Commands == 0, "Stale confirmation dispatched a skill.");
            preview = PreviewAbility(ability); RequireSuccess(preview, "Creating a cancellable token");
            var token = PreviewToken(preview); var closed = Send("close");
            RejectedTargeting(closed, Send("abilityConfirm", new() { ["token"] = token }), "Cancelled confirmation token");
            preview = PreviewAbility(ability); RequireSuccess(preview, "Creating a valid confirmation");
            var fired = ConfirmAbility(preview); RequireSuccess(fired, "Using the confirmation once");
            Check(observer.Commands == 1, "A valid confirmation must dispatch exactly once.");
            RejectedTargeting(fired, ConfirmAbility(preview), "Reused confirmation token");
            Check(observer.Commands == 1, "Replaying a consumed token dispatched again.");
        }
        finally { player.RemovePart(observer); }
    }

    private static void NeutralVaultContract()
    {
        Game.NewGame("classic", 8128, "Vault consent contract");
        var player = Player(); var zone = CurrentZone(player); var ability = GrantTargetingSkill("Acrobatics_Vault", "CommandVault");
        var target = AddTargetingBody(player, zone, 2, "neutral-vault"); var cell = zone.GetEntityCell(target);
        var observer = ObserveCommand(player, ability.Command);
        try
        {
            Check(!FactionManager.IsHostile(player, target), "The consent control must be a real non-hostile creature.");
            Check(VaultTargeting.TryPlan(player, zone, cell.X, cell.Y, out var landing, out var expected, out _) && expected == target, "The consent control needs a valid native attack landing.");
            var before = State(); int hp = target.GetStatValue("Hitpoints");
            RejectedTargeting(before, Send("ability", new() { ["id"] = ability.ID.ToString(), ["x"] = cell.X, ["y"] = cell.Y }), "Direct neutral Vault without review");
            var preview = PreviewAbility(ability, x: cell.X, y: cell.Y); RequireSuccess(preview, "Reviewing a neutral Vault");
            var view = AbilityPreviewView(preview);
            Check(Text(view, "mode") == "CursorCell" && Boolean(view, "requiresNeutralConfirmation") && Text(view, "targetName") == target.GetDisplayName(), "The preview must identify the non-hostile target and require consent.");
            var shownLanding = view["landing"] as JsonObject;
            Check(shownLanding != null && Number(shownLanding, "x") == landing.X && Number(shownLanding, "y") == landing.Y, "Vault preview disagrees with the native landing plan.");
            RejectedTargeting(before, ConfirmAbility(preview), "Neutral Vault without explicit acceptance");
            Check(observer.Commands == 0 && target.GetStatValue("Hitpoints") == hp, "An unaccepted neutral Vault dealt damage or reached the skill.");
            preview = PreviewAbility(ability, x: cell.X, y: cell.Y); RequireSuccess(preview, "Revisiting the same neutral Vault");
            var fired = ConfirmAbility(preview, true); RequireSuccess(fired, "Explicitly accepting the neutral attack");
            Check(observer.Commands == 1 && observer.HasTargetCell && ReferenceEquals(observer.TargetCell, cell), "CursorCell must dispatch the exact selected target cell once.");
            Check(Number(fired, "x") == landing.X && Number(fired, "y") == landing.Y && ability.CooldownRemaining > 0, "Accepted Vault did not use its native landing and cooldown.");
        }
        finally { zone.RemoveEntity(target); player.RemovePart(observer); }
    }

    private static void ChangedVaultContract()
    {
        foreach (var change in new[] { "target", "landing", "source", "ownership" })
        {
            Game.NewGame("classic", 8128, "Vault recheck " + change);
            var player = Player(); var zone = CurrentZone(player); var ability = GrantTargetingSkill("Acrobatics_Vault", "CommandVault");
            var target = AddTargetingBody(player, zone, 2, "recheck"); var selected = zone.GetEntityCell(target);
            Check(VaultTargeting.TryPlan(player, zone, selected.X, selected.Y, out var landing, out _, out _), "The revalidation fixture needs a valid initial plan.");
            var observer = ObserveCommand(player, ability.Command); Entity? replacement = null;
            try
            {
                var preview = PreviewAbility(ability, x: selected.X, y: selected.Y); RequireSuccess(preview, "Creating a plan before " + change + " changes");
                if (change == "target")
                {
                    zone.RemoveEntity(target); replacement = NewTargetingBody("replacement");
                    Check(zone.AddEntity(replacement, selected.X, selected.Y), "Could not substitute the same-cell target fixture.");
                }
                else if (change == "landing")
                {
                    replacement = NewTargetingBody("landing-blocker");
                    Check(zone.AddEntity(replacement, landing.X, landing.Y), "Could not block the formerly valid landing.");
                }
                else if (change == "source")
                {
                    var source = zone.GetEntityCell(player);
                    var destination = TargetingCells(player, zone, 1).First(c => c != landing && c != selected);
                    Check(zone.MoveEntity(player, destination.X, destination.Y), "Could not relocate the source fixture.");
                    Check(source != zone.GetEntityCell(player), "Source relocation did not occur.");
                }
                else Check(player.GetPart<ActivatedAbilitiesPart>().RemoveAbility(ability.ID), "Could not remove the planned ability.");
                var before = State(); var refused = ConfirmAbility(preview, true);
                RejectedTargeting(before, refused, "Confirming after a changed " + change);
                Check(observer.Commands == 0, "A changed " + change + " reached the native Vault skill.");
            }
            finally { zone.RemoveEntity(target); if (replacement != null) zone.RemoveEntity(replacement); player.RemovePart(observer); }
        }
    }

    private static void RiteReadingContract()
    {
        Game.NewGame("classic", 8128, "Rite reading contract");
        var player = Player(); var zone = CurrentZone(player); var ability = GrantTargetingSkill("Rites_ShatteredRime", "CommandShatteredRime");
        var factory = typeof(CavesOfOoo.Web.BrowserGame).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(EntityFactory)).GetValue(Game) as EntityFactory;
        var book = factory!.CreateEntity("ShatteredRimeGrimoire");
        Check(player.GetPart<InventoryPart>().AddObject(book), "The charged grimoire fixture could not enter the pack.");
        ability.CooldownRemaining = 7;
        var target = AddTargetingBody(player, zone, 1, "hidden-rite-candidate"); var cell = zone.GetEntityCell(target); var origin = zone.GetEntityCell(player);
        target.GetPart<RenderPart>().Visible = false;
        var observer = ObserveCommand(player, ability.Command);
        try
        {
            var before = State(); var resources = TargetingResources(player);
            using var capture = new SpellFxCapture("ContractRiteReadObserver", zone, player);
            var read = Send("ritePreview", new() { ["id"] = ability.ID.ToString(), ["dx"] = Math.Sign(cell.X - origin.X), ["dy"] = Math.Sign(cell.Y - origin.Y) });
            RequireSuccess(read, "Reading an owned rite while it is cooling down");
            Check(Text(read, "panel") == "ritePreview" && read["ritePreview"] is JsonObject reader && !string.IsNullOrWhiteSpace(Text(reader, "text")), "The dedicated rite reader did not return its native reading.");
            Check(!read["ritePreview"]!.ToJsonString().Contains(target.GetDisplayName(), StringComparison.Ordinal), "The rite reader disclosed a render-hidden creature.");
            SamePlayerState(before, read, "Reading a rite on cooldown");
            Check(resources == TargetingResources(player) && observer.Commands == 0 && CapturedFxEntries(capture) == 0, "Reading a rite spent ink/cooldown/time, dispatched a cast, or wrote FX.");
        }
        finally { zone.RemoveEntity(target); player.RemovePart(observer); }
    }

    private static ActivatedAbility OwnedAbility(string command) => Player().GetPart<ActivatedAbilitiesPart>().AbilityList.Single(a => a.Command == command);
    private static ActivatedAbility GrantTargetingSkill(string skill, string command)
    {
        Check(Player().GetPart<SkillsPart>().AddSkill(skill, "web-contract-fixture"), "Could not grant the native targeting fixture skill.");
        return OwnedAbility(command);
    }
    private static JsonObject PreviewAbility(ActivatedAbility ability, int? x = null, int? y = null, int? dx = null, int? dy = null)
    {
        var args = new Dictionary<string, object?> { ["id"] = ability.ID.ToString() };
        if (x.HasValue) args["x"] = x.Value; if (y.HasValue) args["y"] = y.Value;
        if (dx.HasValue) args["dx"] = dx.Value; if (dy.HasValue) args["dy"] = dy.Value;
        return Send("abilityPreview", args);
    }
    private static JsonObject AbilityPreviewView(JsonObject state) => state["abilityPreview"] as JsonObject ?? throw new Exception("A valid target did not produce an ability preview.");
    private static string PreviewToken(JsonObject state) => Text(AbilityPreviewView(state), "token");
    private static JsonObject ConfirmAbility(JsonObject preview, bool accept = false) => Send("abilityConfirm", new() { ["token"] = PreviewToken(preview), ["acceptNeutralAttack"] = accept });
    private static void RejectedTargeting(JsonObject before, JsonObject after, string action)
    {
        SamePlayerState(before, after, action);
        Check(!Boolean(after, "faulted") && !string.IsNullOrWhiteSpace(Text(after, "error")), action + " must be refused explicitly without faulting.");
    }
    private static void CheckControlDiagnostic(Entity player, string action, string kind)
    {
        var entry = Diag.Snapshot(50).Last(e => e.ActorId == player.ID && e.Kind.StartsWith("BrowserAction", StringComparison.Ordinal));
        Check(entry.Kind == kind && Text(Parse(entry.PayloadJson), "action") == action,
            "The action diagnostic must report its actual routing outcome.");
    }
    private static string TargetingResources(Entity player) => JsonSerializer.Serialize(new
    {
        hp = player.GetStatValue("Hitpoints"), mp = player.GetStatValue("MP"), focus = player.GetStatValue("Focus"), inventory = InventoryIdentity(),
        abilities = player.GetPart<ActivatedAbilitiesPart>().AbilityList.Select(a => new { a.ID, a.CooldownRemaining }).ToArray(),
        ink = player.GetPart<InventoryPart>().Objects.Where(e => e.HasPart<GrimoireChargePart>()).Select(e => new { e.ID, e.GetPart<GrimoireChargePart>().Charges }).ToArray(),
        messages = MessageLog.Count, ascii = AsciiFxBus.PendingCount, spell = SpellFxBus.PendingCount
    });
    private static int CapturedFxEntries(SpellFxCapture capture) => new[] { "_path", "_cells", "_targets", "_reactions" }
        .Sum(name => ((ICollection?)typeof(SpellFxCapture).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(capture))?.Count ?? throw new Exception("Missing native FX observer field " + name));
    private static IEnumerable<Cell> TargetingCells(Entity player, Zone zone, int distance)
    {
        var source = zone.GetEntityCell(player);
        foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1) })
        {
            var cell = zone.GetCell(source.X + d.Item1 * distance, source.Y + d.Item2 * distance);
            var intermediate = zone.GetCell(source.X + d.Item1, source.Y + d.Item2);
            if (cell?.IsVisible == true && cell.Explored && !cell.BlocksMovement(player) && !cell.Occupants.Any(e => e.HasTag("Creature"))
                && intermediate != null && !intermediate.BlocksMovement(player) && !intermediate.Occupants.Any(e => e.HasTag("Creature"))) yield return cell;
        }
    }
    private static Entity AddTargetingBody(Entity player, Zone zone, int distance, string name)
    {
        var target = NewTargetingBody(name); var cell = TargetingCells(player, zone, distance).First();
        Check(zone.AddEntity(target, cell.X, cell.Y), "Could not place the bounded targeting fixture."); return target;
    }
    private static Entity NewTargetingBody(string name)
    {
        var target = new Entity { ID = "web-targeting-" + name, BlueprintName = "ContractTargetingBody" };
        target.SetTag("Creature"); target.Tags["Faction"] = "Villagers";
        target.AddPart(new PhysicsPart { Solid = true, Weight = 60 });
        target.AddPart(new RenderPart { DisplayName = "contract " + name, RenderString = "T", RenderLayer = 99 });
        target.Statistics["Hitpoints"] = new Stat { Owner = target, Name = "Hitpoints", BaseValue = 500, Max = 500 };
        return target;
    }
    private static TargetingCommandObserver ObserveCommand(Entity player, string command)
    {
        var observer = new TargetingCommandObserver { Command = command }; player.AddPart(observer);
        // Observe before SkillsPart consumes the real command; do not handle it.
        player.Parts.Remove(observer); player.Parts.Insert(0, observer); return observer;
    }
    private sealed class TargetingCommandObserver : Part
    {
        public string Command = ""; public int Commands, DirectionX, DirectionY; public bool HasTargetCell; public Cell? TargetCell, SourceCell;
        public override string Name => "WebTargetingContractObserver";
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == Command)
            {
                Commands++; DirectionX = e.GetIntParameter("DirectionX"); DirectionY = e.GetIntParameter("DirectionY");
                HasTargetCell = e.HasParameter("TargetCell"); TargetCell = e.GetParameter<Cell>("TargetCell"); SourceCell = e.GetParameter<Cell>("SourceCell");
            }
            return true;
        }
    }
}
