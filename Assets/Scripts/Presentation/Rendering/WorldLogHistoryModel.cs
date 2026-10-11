using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>A raw-entry/character identity, separate from viewport row numbers.</summary>
    public readonly struct WorldLogHistoryLine
    {
        public readonly string Text;
        public readonly int EntryIndex, Serial, Tick, CharacterOffset;
        public readonly bool IsHeader;
        public WorldLogHistoryLine(string text,int index,int serial,int tick,int offset,bool header)
        { Text=text;EntryIndex=index;Serial=serial;Tick=tick;CharacterOffset=offset;IsHeader=header; }
    }

    /// <summary>On-demand chronological history in bounded raw-entry windows.
    /// It never edits, consumes, classifies or paraphrases a logged message.
    /// Wrapping is proportional to the text of at most16 selected entries;
    /// unchanged/append-only refresh never gathers or rewraps that window.</summary>
    public sealed class WorldLogHistoryModel
    {
        public const int WindowEntries=16;
        static readonly char[] Newlines={'\r','\n'};
        readonly List<MessageLog.Entry> entries=new List<MessageLog.Entry>(WindowEntries);
        readonly List<WorldLogHistoryLine> lines=new List<WorldLogHistoryLine>();
        IReadOnlyList<WorldLogHistoryLine> visible=Array.Empty<WorldLogHistoryLine>();
        int width,rows,top,start,end,total;
        long revision,generation;
        bool opened;
        public WorldLogHistoryModel(int width=52,int pageRows=26){this.width=Clamp(width,8,72);rows=Clamp(pageRows,1,40);}
        public IReadOnlyList<WorldLogHistoryLine> VisibleLines=>visible;
        public int FirstEntryIndex=>start;
        public int EndEntryIndex=>end;
        public int TotalEntries=>total;
        public int NewerEntryCount=>Math.Max(0,total-end);
        public int TopEntryIndex=>visible.Count>0?visible[0].EntryIndex:-1;
        public int TopCharacterOffset=>visible.Count>0?visible[0].CharacterOffset:0;
        public int BuildCount {get;private set;}
        public int LastReadCount {get;private set;}
        public bool HasOlder=>top>0||start>0;
        public bool HasNewer=>top+rows<lines.Count||end<total;
        public void Open(){opened=true;Latest();}
        public void Oldest(){opened=true;Load(0,false);}
        public void Latest(){opened=true;Load(Math.Max(0,MessageLog.Count-WindowEntries),true);}
        public bool Refresh()
        {
            if(!opened)return false;
            if(generation!=MessageLog.HistoryGeneration){Latest();return true;}
            if(revision==MessageLog.Revision)return false;
            revision=MessageLog.Revision;int count=MessageLog.Count;bool changed=count!=total;total=count;return changed;
        }
        public bool Older()
        {
            Refresh();
            if(top>0){top=Math.Max(0,top-rows);SelectVisible();return true;}
            if(start<=0)return false;
            Load(Math.Max(0,start-WindowEntries),true,start);return true;
        }
        public bool Newer()
        {
            Refresh();
            if(top+rows<lines.Count){top+=rows;SelectVisible();return true;}
            if(end>=total)return false;
            Load(end,false);return true;
        }
        public void Resize(int newWidth,int newRows)
        {
            Refresh();newWidth=Clamp(newWidth,8,72);newRows=Clamp(newRows,1,40);
            if(width==newWidth&&rows==newRows)return;
            int anchor=TopEntryIndex,character=TopCharacterOffset;bool header=visible.Count>0&&visible[0].IsHeader;
            width=newWidth;rows=newRows;if(!opened)return;Wrap();
            top=0;
            for(int i=0;i<lines.Count;i++)
            {
                var line=lines[i];if(line.EntryIndex!=anchor)continue;
                if(header&&line.IsHeader){top=i;break;}
                if(!line.IsHeader&&line.CharacterOffset<=character)top=i;
            }
            SelectVisible();
        }
        void Load(int requested,bool last,int? limit=null)
        {
            total=MessageLog.Count;start=Clamp(requested,0,total);
            int count=Math.Min(WindowEntries,(limit??total)-start);
            LastReadCount=MessageLog.CopyEntryRange(start,Math.Max(0,count),entries);end=start+entries.Count;
            revision=MessageLog.Revision;generation=MessageLog.HistoryGeneration;Wrap();
            top=last&&lines.Count>0?((lines.Count-1)/rows)*rows:0;SelectVisible();
        }
        void Wrap()
        {
            BuildCount++;lines.Clear();
            for(int i=0;i<entries.Count;i++)
            {
                var entry=entries[i];int index=start+i;
                if(i==0||entries[i-1].Tick!=entry.Tick)
                {
                    string label="Tick "+entry.Tick;
                    for(int offset=0;offset<label.Length;offset+=width)
                        lines.Add(new WorldLogHistoryLine(label.Substring(offset,Math.Min(width,label.Length-offset)),index,entry.Serial,entry.Tick,0,true));
                }
                string text=entry.Text??string.Empty;int paragraph=0;
                while(true)
                {
                    int end=text.IndexOfAny(Newlines,paragraph);if(end<0)end=text.Length;
                    if(paragraph==end)AddLine("",paragraph,index,entry);
                    for(int position=paragraph;position<end;)
                    {
                        int length=Math.Min(width,end-position);
                        if(position+length<end)
                        {int space=text.LastIndexOf(' ',position+length-1,length);if(space>position)length=space-position+1;}
                        AddLine(text.Substring(position,length),position,index,entry);position+=length;
                    }
                    if(end==text.Length)break;
                    paragraph=end+1;if(text[end]=='\r'&&paragraph<text.Length&&text[paragraph]=='\n')paragraph++;
                }
            }
        }
        void AddLine(string text,int offset,int index,MessageLog.Entry entry)
        {lines.Add(new WorldLogHistoryLine(text,index,entry.Serial,entry.Tick,offset,false));}
        void SelectVisible()
        {
            int length=Math.Min(rows,Math.Max(0,lines.Count-top));var copy=new WorldLogHistoryLine[length];
            for(int i=0;i<length;i++)copy[i]=lines[top+i];visible=Array.AsReadOnly(copy);
        }
        static int Clamp(int value,int minimum,int maximum)=>Math.Max(minimum,Math.Min(maximum,value));
    }
}
