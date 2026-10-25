using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CavesOfOoo.Rendering
{
    /// <summary>Retained UI mesh using the game's own CP437 font atlas. One graphic per
    /// label, point filtering, no OS font dependency and no per-frame text generation.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SkillScreenText : MaskableGraphic
    {
        string content="";
        public float GlyphHeight=20;
        public string Content { get=>content; set { value=value??""; if(content==value)return; content=value; SetVerticesDirty(); } }
        public override Texture mainTexture => CP437TilesetGenerator.GetUiTile('A').sprite.texture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect=rectTransform.rect;float w=GlyphHeight*.5f, x=rect.xMin,y=rect.yMax;
            foreach(char c in content)
            {
                if(c=='\n') { x=rect.xMin;y-=GlyphHeight+4;continue; }
                if(y-GlyphHeight<rect.yMin) break;
                if(c!=' ' && x+w<=rect.xMax+.1f)
                {
                    var sprite=CP437TilesetGenerator.GetUiTile(c).sprite;
                    Rect uv=sprite.textureRect;float tw=sprite.texture.width,th=sprite.texture.height;
                    int n=vh.currentVertCount; Color32 tint=color;
                    vh.AddVert(new Vector3(x,y-GlyphHeight),tint,new Vector2(uv.xMin/tw,uv.yMin/th));
                    vh.AddVert(new Vector3(x,y),tint,new Vector2(uv.xMin/tw,uv.yMax/th));
                    vh.AddVert(new Vector3(x+w,y),tint,new Vector2(uv.xMax/tw,uv.yMax/th));
                    vh.AddVert(new Vector3(x+w,y-GlyphHeight),tint,new Vector2(uv.xMax/tw,uv.yMin/th));
                    vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+2,n+3,n);
                }
                x+=w;
            }
        }
        public static List<string> Wrap(string text,int width)
        {
            var lines=new List<string>(); width=Mathf.Max(1,width);
            text=(text??"").Replace("\r","").Replace('—','-').Replace('–','-').Replace('’','\'').Replace('“','"').Replace('”','"').Replace("≥",">=").Replace('−','-').Replace("°"," degrees").Replace("→","->").Replace('•','-');
            foreach(var paragraph in text.Split('\n'))
            {
                string remaining=paragraph;
                while(remaining.Length>width)
                {
                    int space=remaining.LastIndexOf(' ',width-1,width);
                    int end=space>0?space:width;
                    lines.Add(remaining.Substring(0,end));remaining=remaining.Substring(end).TrimStart();
                }
                lines.Add(remaining);
            }
            return lines;
        }
        /// <summary>Short selectable labels announce truncation; the details pane keeps all text.</summary>
        public static string Fit(string text,int columns,int rows)
        {
            var lines=Wrap(text,columns);
            if(lines.Count>rows) lines[rows-1]=lines[rows-1].Substring(0,Mathf.Min(lines[rows-1].Length,columns-3))+"...";
            return Page(lines,0,rows);
        }
        public static string Page(List<string> lines,int page,int size)
        {
            var b=new StringBuilder(); int start=page*size;
            for(int i=start;i<Mathf.Min(lines.Count,start+size);i++) { if(i>start)b.Append('\n');b.Append(lines[i]); }
            return b.ToString();
        }
    }
}
