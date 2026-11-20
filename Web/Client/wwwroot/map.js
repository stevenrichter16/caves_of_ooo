const palette={k:'#0b1310',K:'#55634f',y:'#afba9d',Y:'#e8e5c8',w:'#97885c',W:'#dfc37b',g:'#669455',G:'#a7c77a',r:'#ad6855',R:'#e18b68',b:'#547781',B:'#92b5be',c:'#66a29a',C:'#a5c9b2',m:'#947d99',M:'#c0a0b8'};
const atlases=new Map();
const atlasNames=['base','cast','anchors','states','items','cellar','ground','motion'].map(n=>`opening-${n}`).concat(['sill-environment','beating-materials','grovelands-ground','stump-materials','overwrit-materials','sodden-flood-water']);
export const imagesReady=Promise.all(atlasNames.map(name=>new Promise(resolve=>{const image=new Image();image.src=`art/${name}.png`;image.onload=()=>{atlases.set(name,image);resolve();};image.onerror=resolve;})));
export function color(code){return palette[(code||'&y').replace(/[^a-zA-Z]/g,'').slice(-1)]||palette.y;}
export class WorldMapView {
 constructor(canvas){this.canvas=canvas;this.ctx=canvas.getContext('2d');this.scale=Math.min(window.innerWidth,window.innerHeight)<650?1.35:1.7;this.scales={local:this.scale,world:1};this.cx=40;this.cy=12;this.follow=true;this.target=null;this.data=null;this.bounds=null;this.observer=new ResizeObserver(()=>this.draw());this.observer.observe(canvas);}
 set(data){const changed=this.data?.zone!==data.zone,oldMode=this.data?.isWorldMap?'world':'local',mode=data.isWorldMap?'world':'local';if(mode!==oldMode){this.scales[oldMode]=this.scale;this.scale=this.scales[mode];}this.data=data;this.canvas.dataset.mode=mode;if(this.follow||changed){this.cx=data.x;this.cy=data.y;this.follow=true;}this.draw();}
 center(){if(this.data){this.follow=true;this.cx=this.data.x;this.cy=this.data.y;this.draw();}}
 zoom(change){this.scale=Math.min(3,Math.max(.65,this.scale+change));this.draw();}
 tileAt(x,y){if(!this.bounds)return null;const tile={x:Math.floor((x-this.bounds.ox)/this.bounds.tw),y:Math.floor((y-this.bounds.oy)/this.bounds.th)},area=this.data?.mapBounds||{x:0,y:0,width:80,height:25};return tile.x>=area.x&&tile.x<area.x+area.width&&tile.y>=area.y&&tile.y<area.y+area.height?tile:null;}
 pan(dx,dy){if(!this.bounds)return;this.follow=false;this.cx-=dx/this.bounds.tw;this.cy-=dy/this.bounds.th;const area=this.data.mapBounds||{x:0,y:0,width:80,height:25};this.cx=Math.min(area.x+area.width-1,Math.max(area.x,this.cx));this.cy=Math.min(area.y+area.height-1,Math.max(area.y,this.cy));this.draw();}
 draw(){
  if(!this.data)return;
  const rect=this.canvas.getBoundingClientRect();if(!rect.width||!rect.height)return;
  const dpr=Math.min(devicePixelRatio||1,2),W=rect.width,H=rect.height;
  if(this.canvas.width!==Math.round(W*dpr)||this.canvas.height!==Math.round(H*dpr)){this.canvas.width=Math.round(W*dpr);this.canvas.height=Math.round(H*dpr);}
  const c=this.ctx;c.setTransform(dpr,0,0,dpr,0,0);c.imageSmoothingEnabled=false;c.fillStyle='#0a120d';c.fillRect(0,0,W,H);
  const world=!!this.data.isWorldMap,area=this.data.mapBounds||{x:0,y:0,width:80,height:25};
  const landscape=world&&matchMedia('(orientation:landscape) and (max-height:600px)').matches;
  let top=world?(landscape?36:8):0,bottom=world?(landscape?48:24):0;
  if(world&&this.canvas.id==='world-canvas'){
   const toolbar=document.querySelector('.map-tools')?.getBoundingClientRect();
   if(toolbar&&toolbar.width>0&&toolbar.left<rect.right&&toolbar.right>rect.left&&toolbar.top<rect.bottom&&toolbar.bottom>rect.top){
    if(toolbar.top-rect.top<H/2)top=Math.max(top,toolbar.bottom-rect.top+8);
    else bottom=Math.max(bottom,rect.bottom-toolbar.top+8);
   }
  }
  const usableHeight=H-top-bottom;
  const fit=world?Math.min((W-16)/(area.width*16),usableHeight/(area.height*24)):1;
  const tw=Math.max(1,Math.round(16*this.scale*fit)),th=Math.max(1,world?Math.floor(24*this.scale*fit):Math.round(24*this.scale));
  let ox=Math.round(W/2-(this.cx+.5)*tw),oy=Math.round(top+usableHeight/2-(this.cy+.5)*th);
  if(W>=area.width*tw)ox=Math.floor((W-area.width*tw)/2)-area.x*tw;else ox=Math.min(-area.x*tw,Math.max(W-(area.x+area.width)*tw,ox));
  if(usableHeight>=area.height*th)oy=top+Math.floor((usableHeight-area.height*th)/2)-area.y*th;else oy=Math.min(top-area.y*th,Math.max(H-bottom-(area.y+area.height)*th,oy));
  this.bounds={ox,oy,tw,th,W,H};
  const underground=!world&&this.data.zone?.split('.').at(-1)!=='0';
  for(const t of this.data.cells){
   const x=ox+t.x*tw,y=oy+t.y*th;if(x+tw<0||x>W||y+th<0||y>H)continue;
   c.globalAlpha=t.v?1:.32;
   if(world){this.drawMapCell(t,x,y,tw,th);continue;}
   const noise=((t.x*17+t.y*37)%7)/7;
   c.fillStyle=underground?`rgb(${22+noise*6},${26+noise*7},${23+noise*5})`:`rgb(${23+noise*6},${36+noise*7},${24+noise*5})`;
   if(t.bg&&/\^[bBcC]/.test(t.bg))c.fillStyle='#214440';
   c.fillRect(x,y,tw,th);
   const environment=atlases.get('sill-environment'),ground=atlases.get('opening-ground');
   const variation=(t.x*13+t.y*7)%4;
   if(environment)c.drawImage(environment,(underground?4+variation:variation)*16,0,16,24,x,y,tw,th);
   if(ground&&(t.ground==='RoadStone'||t.g==='='))c.drawImage(ground,(16+((t.x+t.y)&1))*16,0,16,24,x,y,tw,th);
   if(ground&&(t.wet||t.ground==='Water'))c.drawImage(ground,0,0,16,24,x,y,tw,th);
   if(t.v&&!t.solid&&!underground){c.fillStyle='#8b9f5b15';c.fillRect(x+((t.x*7+t.y)%13)/16*tw,y+((t.x+t.y*7)%20)/24*th,1,2);}
   let drawn=false;
   if(t.art){const [key,frame]=t.art.split(':'),image=atlases.get(key);if(image){c.drawImage(image,Number(frame)*16,0,16,24,x,y,tw,th);drawn=true;}}
   if(!drawn&&t.g&&t.g!=='.'&&t.g!==' '&&t.g!=='='&&t.g!==','){c.fillStyle=color(t.c);c.font=`${Math.max(12,Math.round(th*.68))}px ui-monospace,SFMono-Regular,Menlo,monospace`;c.textAlign='center';c.textBaseline='middle';c.fillText(t.g==='#'?'▓':t.g,x+tw/2,y+th*.53,tw);}
   if(t.hostile){c.fillStyle='#cc765a';c.fillRect(x+tw*.3,y+th-2,tw*.4,2);}
  }
  c.globalAlpha=1;
  const px=ox+this.data.x*tw,py=oy+this.data.y*th;
  c.strokeStyle='#e4cd8b';c.lineWidth=1;c.strokeRect(px+1,py+1,tw-2,th-2);
  if(this.aim){
   const a=this.aim,d=this.data;c.strokeStyle='#f2d38499';c.lineWidth=1;c.setLineDash([3,4]);c.beginPath();c.moveTo(ox+(d.x+.5)*tw,oy+(d.y+.5)*th);
   let x=d.x,y=d.y;for(let i=1;i<=a.range;i++){const nx=d.x+a.dx*i,ny=d.y+a.dy*i;if(!d.cells.some(t=>t.x===nx&&t.y===ny&&t.v))break;x=nx;y=ny;}
   c.lineTo(ox+(x+.5)*tw,oy+(y+.5)*th);c.stroke();c.setLineDash([]);
  }
  if(this.target){const tx=ox+this.target.x*tw,ty=oy+this.target.y*th;c.strokeStyle='#f2d384';c.lineWidth=2;c.strokeRect(tx+1,ty+1,tw-2,th-2);c.fillStyle='#e7c47b33';c.fillRect(tx,ty,tw,th);}
 }
 drawMapCell(t,x,y,tw,th){
  const c=this.ctx;
  c.fillStyle=color(t.mapColor);c.fillRect(x,y,tw,th);this.drawSprite(t.mapGround,x,y,tw,th);
  c.fillStyle=`rgba(0,0,0,${1-(t.mapTint??.65)})`;c.fillRect(x,y,tw,th);
  if(t.landmark){c.fillStyle=color(t.mapColor);c.font=`bold ${Math.max(8,Math.round(th*.78))}px ui-monospace,SFMono-Regular,Menlo,monospace`;c.textAlign='center';c.textBaseline='middle';c.fillText(t.mapGlyph,x+tw/2,y+th*.52,tw);}
  if(t.visited){c.fillStyle='#e4cd8b';c.fillRect(x+1,y+th-2,2,1);}
  this.drawSprite(t.art,x,y,tw,th);
 }
 drawSprite(key,x,y,tw,th){if(!key)return;const [name,frame]=key.split(':'),image=atlases.get(name);if(image)this.ctx.drawImage(image,Number(frame)*16,0,16,24,x,y,tw,th);}
}
export async function drawOpening(canvas){try{const data=await(await fetch('opening-view.json')).json();await imagesReady;const map=new WorldMapView(canvas);map.scale=1.15;map.follow=false;map.cx=39;map.cy=12;map.data=data;map.draw();return map;}catch{return null;}}
