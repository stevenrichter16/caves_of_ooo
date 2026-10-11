using CavesOfOoo.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only history, independent of the announcement/document queue.
    /// Uses the existing centered popup tilemaps and menu input context.</summary>
    public sealed class WorldLogHistoryUI : MonoBehaviour
    {
        public Tilemap Tilemap, BgTilemap;
        public Camera PopupCamera;
        const int Width=60,Height=35,BodyY=4,BodyRows=26;
        readonly WorldLogHistoryModel model=new WorldLogHistoryModel(Width-4,BodyRows);
        bool open,drawn,lastGamepad;int originX,topY;
        public bool IsOpen=>open;
        public WorldLogHistoryModel Model=>model;
        public int RenderCount {get;private set;}
        public void Open(){model.Open();open=true;Render();}
        public void Close(){ClearRegion();open=false;}
        void OnDisable(){Close();}
        public void HandleInput()
        {
            if(!open)return;
            if(InputHelper.GetKeyDown(KeyCode.Escape)){Close();return;}
            bool changed=model.Refresh();
            if((InputHelper.GetKeyDown(KeyCode.Home)||UnityEngine.InputSystem.Keyboard.current?.homeKey.wasPressedThisFrame==true)){model.Oldest();changed=true;}
            else if((InputHelper.GetKeyDown(KeyCode.End)||UnityEngine.InputSystem.Keyboard.current?.endKey.wasPressedThisFrame==true)){model.Latest();changed=true;}
            else if(InputHelper.GetKeyDown(KeyCode.LeftArrow)||InputHelper.GetKeyDown(KeyCode.PageUp)||InputHelper.GetKeyDown(KeyCode.UpArrow))changed=model.Older()||changed;
            else if(InputHelper.GetKeyDown(KeyCode.RightArrow)||InputHelper.GetKeyDown(KeyCode.PageDown)||InputHelper.GetKeyDown(KeyCode.DownArrow)||InputHelper.GetKeyDown(KeyCode.Return)||InputHelper.GetKeyDown(KeyCode.Space))changed=model.Newer()||changed;
            else if(Input.GetMouseButtonDown(0)&&CenteredPopupLayout.ScreenToGrid(PopupCamera,Tilemap,Input.mousePosition,out int x,out int y))
            {
                int localX=x-originX,localY=topY-y;
                if(localY==31&&localX>=2&&localX<Width-2)
                {
                    if(localX<20)changed=model.Older()||changed;
                    else if(localX<40)changed=model.Newer()||changed;
                    else{Close();return;}
                }
            }
            if(changed||lastGamepad!=NativeGamepadInput.IsConnected)Render();
        }
        void Render()
        {
            if(!open||Tilemap==null)return;
            ClearRegion();originX=CenteredPopupLayout.GetCenteredOriginX(Width);topY=CenteredPopupLayout.GetCenteredTopY(Height);
            var block=CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock);
            if(BgTilemap!=null&&block!=null)
                for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
                {var at=new Vector3Int(originX+x,topY-y,0);BgTilemap.SetTile(at,block);BgTilemap.SetTileFlags(at,TileFlags.None);BgTilemap.SetColor(at,Color.black);}
            for(int y=0;y<Height;y++){Char(0,y,CP437TilesetGenerator.BoxVertical,QudColorParser.Gray);Char(Width-1,y,CP437TilesetGenerator.BoxVertical,QudColorParser.Gray);}
            for(int x=0;x<Width;x++){Char(x,0,CP437TilesetGenerator.BoxHorizontal,QudColorParser.Gray);Char(x,Height-1,CP437TilesetGenerator.BoxHorizontal,QudColorParser.Gray);}
            Text(2,1,"World log",QudColorParser.White);
            string range=model.TotalEntries==0?"No messages yet.":"Entries "+(model.FirstEntryIndex+1)+"-"+model.EndEntryIndex+" of "+model.TotalEntries;
            if(model.NewerEntryCount>0)range+="  ("+model.NewerEntryCount+" newer)";
            Text(2,2,range,QudColorParser.Gray);
            var lines=model.VisibleLines;
            for(int i=0;i<lines.Count&&i<BodyRows;i++)Text(2,BodyY+i,lines[i].Text,lines[i].IsHeader?QudColorParser.BrightCyan:QudColorParser.Gray);
            Text(2,31,"< Older            Newer >             Close",QudColorParser.White);
            lastGamepad=NativeGamepadInput.IsConnected;
            Text(2,32,lastGamepad?"[LB/RB] page  [B] close":"[Arrows] page  [Home/End] ends  [Esc] close",QudColorParser.Gray);
            drawn=true;RenderCount++;
        }
        void ClearRegion()
        {
            if(!drawn)return;
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
            {var at=new Vector3Int(originX+x,topY-y,0);if(Tilemap!=null)Tilemap.SetTile(at,null);if(BgTilemap!=null)BgTilemap.SetTile(at,null);}
            drawn=false;
        }
        void Text(int x,int y,string value,Color color)
        {if(value==null)return;for(int i=0;i<value.Length&&x+i<Width-2;i++)Char(x+i,y,value[i],color);}
        void Char(int x,int y,char value,Color color)
        {
            if(Tilemap==null)return;var tile=CP437TilesetGenerator.GetUiTile(Cp437.Map(value));if(tile==null)return;
            var at=new Vector3Int(originX+x,topY-y,0);Tilemap.SetTile(at,tile);Tilemap.SetTileFlags(at,TileFlags.None);Tilemap.SetColor(at,color);
        }
    }
}
