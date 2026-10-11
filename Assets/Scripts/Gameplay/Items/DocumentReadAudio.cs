namespace CavesOfOoo.Core
{
    /// <summary>Transient physical reading provenance. Never serialized or
    /// recovered from text equality; old saved announcements remain quiet.</summary>
    internal sealed class DocumentReadAudio
    {
        readonly Entity actor,document;readonly Zone zone;readonly Cell origin;
        readonly ReadableDocumentPart part;readonly string id;bool armed;
        internal DocumentReadAudio(Entity actor,Zone zone,ReadableDocumentPart part)
        {this.actor=actor;this.zone=zone??actor?.SpatialZone;this.part=part;document=part?.ParentEntity;id=part?.DocumentId;origin=this.zone?.GetEntityCell(actor);}
        // A queued legacy announcement may outlive a failed inventory command.
        // Only the owning transaction can authorize physical paper feedback.
        internal void Arm()=>armed=true;
        bool Current()
        {
            if(!armed||actor==null||document==null||zone==null||origin==null||zone.GetEntityCell(actor)!=origin
                ||document.GetPart<ReadableDocumentPart>()!=part||part.ParentEntity!=document||part.DocumentId!=id
                ||document.GetPart<RenderPart>()?.Visible!=true||!part.CanReach(actor,zone))return false;
            if(document.SpatialZone==zone)
            {var at=zone.GetEntityCell(document);return at!=null&&at.IsVisible&&at.Explored;}
            return actor.GetPart<InventoryPart>()?.Contains(document)==true;
        }
        internal void TurnPage()
        {
            // Each actual visible page change is a fresh optional presentation;
            // keeping the immutable origin prevents an old reader crossing a load.
            if(!Current())return;
            WorldAudioUtility.TakeActor(actor,zone,WorldAudioCue.Page,Current)?.Publish();
        }
    }
}
