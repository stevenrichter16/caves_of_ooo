using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    public enum AnnouncementTone { Normal, Warning, Muted, Positive }

    /// <summary>One immutable reader section. Text remains complete; pagination belongs to presentation.</summary>
    public sealed class AnnouncementSection
    {
        public string Heading { get; }
        public string Text { get; }
        public AnnouncementTone Tone { get; }
        public AnnouncementSection(string heading,string text,AnnouncementTone tone=AnnouncementTone.Normal)
        { Heading=heading??"";Text=text??"";Tone=tone; }
    }

    /// <summary>A named, already-established subject fact, not a prediction or an action.</summary>
    public sealed class AnnouncementFact
    {
        public string Label { get; }
        public string Value { get; }
        public AnnouncementFact(string label,string value) { Label=label??"";Value=value??""; }
    }

    /// <summary>Transient explicit-read snapshot shared by Examine and ordinary announcements.
    /// Copies its collections; does not execute actions, own entities, or enter the save stream.</summary>
    public sealed class AnnouncementDocument
    {
        public string Title { get; }
        public string Kind { get; }
        public string BlueprintName { get; }
        public char? Glyph { get; }
        public string GlyphColor { get; }
        public IReadOnlyList<AnnouncementSection> Sections { get; }
        public IReadOnlyList<AnnouncementFact> Facts { get; }
        // Portraits may consult current visual state but must revalidate the owner before drawing.
        // These references convey no permission to expose inventory, hidden owners or new actions.
        internal Entity Subject { get; }
        internal Zone SubjectZone { get; }
        internal Cell SubjectCell { get; }
        internal Entity SubjectViewer { get; }

        public AnnouncementDocument(string title,string kind="NOTICE",
            IEnumerable<AnnouncementSection> sections=null,IEnumerable<AnnouncementFact> facts=null,
            string blueprintName=null,char? glyph=null,string glyphColor=null,Entity subject=null,Zone subjectZone=null,
            Cell subjectCell=null,Entity subjectViewer=null)
        {
            Title=title??"";Kind=string.IsNullOrWhiteSpace(kind)?"NOTICE":kind;
            BlueprintName=blueprintName;Glyph=glyph;GlyphColor=glyphColor;
            Subject=subject;SubjectZone=subjectZone;SubjectCell=subjectCell;SubjectViewer=subjectViewer;
            var sectionCopy=new List<AnnouncementSection>();
            if(sections!=null)foreach(var section in sections)
                if(section!=null&&!string.IsNullOrWhiteSpace(section.Text))sectionCopy.Add(section);
            Sections=sectionCopy.AsReadOnly();
            var factCopy=new List<AnnouncementFact>();
            if(facts!=null)foreach(var fact in facts)
                if(fact!=null&&!string.IsNullOrWhiteSpace(fact.Label)&&!string.IsNullOrWhiteSpace(fact.Value))factCopy.Add(fact);
            Facts=factCopy.AsReadOnly();
        }

        /// <summary>Complete accessible/log fallback, including metadata. Existing queued legacy
        /// text may be retained independently when its original wording is significant.</summary>
        public string ToPlainText()
        {
            var text=new StringBuilder(Title);
            foreach(var fact in Facts)
            { if(text.Length>0)text.Append('\n');text.Append(fact.Label).Append(": ").Append(fact.Value); }
            foreach(var section in Sections)
            {
                if(text.Length>0)text.Append("\n\n");
                if(!string.IsNullOrWhiteSpace(section.Heading))text.Append(section.Heading).Append('\n');
                text.Append(section.Text);
            }
            return text.ToString();
        }
    }
}
