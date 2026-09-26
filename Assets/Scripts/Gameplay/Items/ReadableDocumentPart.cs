using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A repeatable, non-consuming reading copy. Only the stable catalog
    /// ID is saved; the full canonical text is loaded from shipped Resources.</summary>
    public sealed class ReadableDocumentPart : Part
    {
        public override string Name => "ReadableDocument";
        public string DocumentId = "";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
                e.GetParameter<InventoryActionList>("Actions")?.AddAction("Read", "read", "ReadDocument", 'r', 20);
            else if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == "ReadDocument")
            {
                bool read = TryRead(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone"));
                e.Handled = read;
                return !read;
            }
            return true;
        }

        /// <summary>Read through an actual carried/equipped reference or a nearby
        /// entity in the actor's zone. Returns false with a visible refusal when
        /// stale/inaccessible. Success queues complete text; pagination is UI-owned.
        /// Does not consume items, turns, health, RNG, quests or knowledge flags.</summary>
        public bool TryRead(Entity actor, Zone zone)
        {
            if (!CanReach(actor, zone))
                return Reject(actor, "inaccessible-document", "That document is no longer within reach.");
            var document = ReadableDocumentCatalog.Get(DocumentId);
            if (document == null)
                return Reject(actor, "missing-document", "The writing cannot be read.");
            MessageLog.AddAnnouncement(document.Title + "\n\n" + document.Text);
            Diag.Record("event", "DocumentRead", actor: actor, target: ParentEntity,
                payload: new { documentId = DocumentId, source = document.Source });
            return true;
        }

        private bool CanReach(Entity actor, Zone zone)
        {
            if (actor == null || ParentEntity == null || CombatSystem.IsDeathHandled(actor)) return false;
            if (actor.Statistics.ContainsKey("Hitpoints") && actor.GetStatValue("Hitpoints") <= 0) return false;
            if (actor.GetPart<InventoryPart>()?.Contains(ParentEntity) == true) return true;
            zone = zone ?? actor.SpatialZone;
            if (zone == null || !ReferenceEquals(actor.SpatialZone, zone)
                || !ReferenceEquals(ParentEntity.SpatialZone, zone)) return false;
            var from = zone.GetEntityCell(actor); var to = zone.GetEntityCell(ParentEntity);
            return from != null && to != null && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;
        }

        private bool Reject(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            Diag.Record("event", "DocumentReadRejected", actor: actor, target: ParentEntity,
                payload: new { documentId = DocumentId, reason });
            return false;
        }
    }
}
