(function(root){
  'use strict';
  const MM_PER_INCH=25.4;
  const n=(v,d=0)=>Number.isFinite(Number(v))?Number(v):d;
  const esc=s=>String(s??'').replace(/\\/g,'\\\\').replace(/"/g,'\\"');
  const words=line=>{
    const out=[]; const re=/"((?:[^"\\]|\\.)*)"|(\S+)/g; let m;
    while((m=re.exec(line))) out.push(m[1]!==undefined?m[1].replace(/\\"/g,'"').replace(/\\\\/g,'\\'):m[2]);
    return out;
  };
  const baseConfig=()=>({schemaVersion:3,name:'Layout importat de XTrackCAD',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:'Nivell '+(i+1),elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]});
  function parseXtc(source,template){
    if(typeof source!=='string'||!/^\s*#?XTrackCAD|^\s*VERSION\s+/mi.test(source)) throw new Error('El fitxer no sembla un layout XTrackCAD .xtc de text.');
    const cfg=template?JSON.parse(JSON.stringify(template)):baseConfig();
    const preservedBlocks=cfg.levels.reduce((n,l)=>n+(Array.isArray(l.blocks)?l.blocks.length:0),0),preservedSensors=cfg.levels.reduce((n,l)=>n+(Array.isArray(l.sensors)?l.sensors.length:0),0),discardedConnections=Array.isArray(cfg.connections)?cfg.connections.length:0;
    cfg.levels.forEach(l=>{l.tracks=[];l.blocks=Array.isArray(l.blocks)?l.blocks:[];l.sensors=Array.isArray(l.sensors)?l.sensors:[];l.blocks.forEach(b=>{b.trackIds=[];b.sensorIds=[];});});cfg.connections=[];
    const lines=source.replace(/\r/g,'').split('\n'); let current=null,unsupported=[],imported=0;
    if(template&&preservedBlocks)unsupported.push(`S’han conservat ${preservedBlocks} bloc(s) del projecte base, però cal reassignar-hi les peces importades perquè XTrackCAD no desa aquesta relació.`);
    if(template&&preservedSensors)unsupported.push(`S’han conservat ${preservedSensors} sensor(s) JMRI del projecte base, però cal reassignar-los perquè XTrackCAD no desa aquestes relacions.`);
    if(template&&discardedConnections)unsupported.push(`S’han descartat ${discardedConnections} connexió(ns) del traçat anterior: XTrackCAD no permet mapar amb seguretat els ports a les peces importades.`);
    for(let i=0;i<lines.length;i++){
      const line=lines[i].trim(); if(!line||line.startsWith('#'))continue;
      const t=words(line); if(!t.length)continue;
      if(t[0]==='LAYERS'&&t[1]!=='CURRENT'&&t[1]!=='LINK'&&t[1]!=='SET'){
        const layer=n(t[1],0); if(layer>=0&&layer<5){const name=t.slice(10).join(' ')||('Nivell '+(layer+1));cfg.levels[layer].name=name.replace(/^"|"$/g,'')||cfg.levels[layer].name;}
      }
      if(t[0]==='ROOMSIZE'){const w=n(t[1]),h=n(t[3]);if(w>0&&h>0)cfg.room={widthMm:w*MM_PER_INCH,heightMm:h*MM_PER_INCH};}
      if(t[0]==='TITLE1')cfg.name=t.slice(1).join(' ')||cfg.name;
      if(t[0]==='STRAIGHT'||t[0]==='CURVE'){
        const kind=t[0],id=n(t[1],imported+1),rawLayer=n(t[2],0),layer=Math.max(0,Math.min(4,rawLayer));
        const data=kind==='STRAIGHT'?{x:n(t[8]),y:n(t[9])}:{cx:n(t[8]),cy:n(t[9]),radius:Math.abs(n(t[11]))};
        const endpoints=[]; let j=i+1;
        for(;j<lines.length;j++){const q=lines[j].trim();if(q.startsWith('END$SEGS'))break;if(/^[ET]\d+\s/.test(q)){const e=words(q);endpoints.push({kind:e[0][0],target:e[1]&&e[0][0]==='T'?n(e[1]):null,x:n(e[e[0][0]==='T'?2:1])*MM_PER_INCH,y:n(e[e[0][0]==='T'?3:2])*MM_PER_INCH,angle:n(e[e[0][0]==='T'?4:3])});}}
        i=j;
        if(endpoints.length<2){unsupported.push(kind+' '+id+': sense dos extrems');continue;}
        if(rawLayer>4){unsupported.push(kind+' '+id+': capa '+rawLayer+' sense nivell corresponent');continue;}
        const a=endpoints[0],b=endpoints[1],dx=b.x-a.x,dy=b.y-a.y;
        let piece;
        if(kind==='STRAIGHT'){
          piece={id:'XTC-'+id,type:'straight',name:'XTrackCAD recta '+id,x:a.x,y:a.y,rotation:Math.atan2(dy,dx)*180/Math.PI,levelId:layer+1,blockId:'',sensorName:'',geometry:{lengthMm:Math.hypot(dx,dy),radiusMm:360,angleDeg:30}};
        }else{
          const cx=data.cx*MM_PER_INCH,cy=data.cy*MM_PER_INCH,r=data.radius*MM_PER_INCH;
          const startRad=Math.atan2(a.y-cy,a.x-cx),endRad=Math.atan2(b.y-cy,b.x-cx);
          let sweep=(endRad-startRad)%(Math.PI*2);if(sweep<0)sweep+=Math.PI*2;
          if(sweep>Math.PI*1.99)sweep=0;
          const clockwise=n(t[11])>=0,angle=clockwise?sweep:((2*Math.PI-sweep)%(2*Math.PI));
          const direction=clockwise?'right':'left';
          const tangent=(startRad+(clockwise?Math.PI/2:-Math.PI/2))*180/Math.PI;
          piece={id:'XTC-'+id,type:'curve',name:'XTrackCAD corba '+id,x:a.x,y:a.y,rotation:tangent,levelId:layer+1,blockId:'',sensorName:'',geometry:{lengthMm:Math.abs(angle*r),radiusMm:r,angleDeg:Math.max(.1,Math.min(179,angle*180/Math.PI)),curveDirection:direction}};
          if(angle>Math.PI)unsupported.push('Corba '+id+': arc major importat com a angle limitat');
        }
        cfg.levels[layer].tracks.push(piece);imported++;
      }else if(['BLOCK','SENSOR','SIGNAL','SWITCHMOTOR','TURNTABLE','BEZIER','JOINT','CORNU','DRAW','TURNOUT','STRUCTURE','CAR','NOTE','CONTROL'].includes(t[0])) unsupported.push(t[0]+' '+(t[1]||''));
    }
    if(!imported)throw new Error('No s’han trobat peces STRAIGHT o CURVE compatibles al fitxer XTrackCAD.');
    return {config:cfg,report:{imported,unsupported:[...new Set(unsupported)]}};
  }
  function exportXtc(config){
    const out=['#XTrackCAD Version: 5.3.1, Date: JMRI TC Gold Custom','VERSION 12 5.3.1','TITLE1 '+String(config.name||'JMRI TC Gold Custom').replace(/[\r\n]/g,' '),'TITLE2 JMRI TC Gold Custom','MAPSCALE 1','ROOMSIZE '+(n(config.room?.widthMm,3000)/MM_PER_INCH).toFixed(6)+' x '+(n(config.room?.heightMm,1800)/MM_PER_INCH).toFixed(6),'SCALE H0'];
    for(let i=0;i<5;i++)out.push('LAYERS '+i+' 1 0 1 255 0 0 0 0 "'+esc(config.levels?.[i]?.name||('Nivell '+(i+1)))+'"');
    let id=1,unsupported=[];
    for(const level of (config.levels||[])){
      for(const p of (level.tracks||[])){
        const x=n(p.x),y=n(p.y),rot=n(p.rotation)*Math.PI/180,g=p.geometry||{};
        if(p.blockId||p.sensorName||p.turnoutSystemName)unsupported.push(`${p.name||p.id}: assignacions de bloc/sensor/desviament JMRI no representables en XTrackCAD.`);
        if(p.zMm!==undefined&&Math.abs(n(p.zMm)-n(level.elevationMm))>0.1)unsupported.push(`${p.name||p.id}: cota Z pròpia no representable com a capa XTrackCAD.`);
        if(p.type==='straight'){
          const len=n(g.lengthMm,180),ex=x+Math.cos(rot)*len,ey=y+Math.sin(rot)*len;
          out.push('STRAIGHT '+id+' '+Math.max(0,Math.min(4,n(level.id,1)-1))+' 0 0 0 H0 2 0.000000 0.000000');
          out.push('  E4 '+(x/MM_PER_INCH).toFixed(6)+' '+(y/MM_PER_INCH).toFixed(6)+' '+(((90-n(p.rotation))%360+360)%360).toFixed(6));
          out.push('  E4 '+(ex/MM_PER_INCH).toFixed(6)+' '+(ey/MM_PER_INCH).toFixed(6)+' '+(((270-n(p.rotation))%360+360)%360).toFixed(6));
          out.push('END$SEGS');id++;
        }else if(p.type==='curve'){
          const r=n(g.radiusMm,360),a=n(g.angleDeg,30)*Math.PI/180,dir=g.curveDirection==='left'?-1:1;
          const cx=x+Math.sin(rot)*dir*r,cy=y-Math.cos(rot)*dir*r;
          const endX=x+Math.cos(rot)*r*Math.sin(a)-Math.sin(rot)*dir*r*(1-Math.cos(a)),endY=y+Math.sin(rot)*r*Math.sin(a)+Math.cos(rot)*dir*r*(1-Math.cos(a));
          const bearing=(px,py)=>((Math.atan2(px-cx,-(py-cy))*180/Math.PI)%360+360)%360;
          out.push('CURVE '+id+' '+Math.max(0,Math.min(4,n(level.id,1)-1))+' 0 0 0 H0 2 '+(cx/MM_PER_INCH).toFixed(6)+' '+(cy/MM_PER_INCH).toFixed(6)+' 0 '+((dir*r)/MM_PER_INCH).toFixed(6)+' 0 0 0');
          out.push('  E4 '+(x/MM_PER_INCH).toFixed(6)+' '+(y/MM_PER_INCH).toFixed(6)+' '+bearing(x,y).toFixed(6));
          out.push('  E4 '+(endX/MM_PER_INCH).toFixed(6)+' '+(endY/MM_PER_INCH).toFixed(6)+' '+bearing(endX,endY).toFixed(6));
          out.push('END$SEGS');id++;
        }else unsupported.push((p.type||'element')+' '+(p.name||p.id||''));
      }
    }
    const blockCount=(config.levels||[]).reduce((n,l)=>n+(l.blocks||[]).length,0),sensorCount=(config.levels||[]).reduce((n,l)=>n+(l.sensors||[]).length,0);
    if(blockCount)unsupported.push(`${blockCount} registre(s) de bloc lògic no exportats: XTrackCAD no representa blocs JMRI.`);
    if(sensorCount)unsupported.push(`${sensorCount} registre(s) de sensor JMRI no exportats: XTrackCAD no representa detectors ni adreces de retroalimentació.`);
    if((config.connections||[]).length)unsupported.push(`${config.connections.length} connexió(ns) de ports del model v3 no exportades: la topologia explícita no es pot garantir en XTrackCAD.`);
    out.push('END$TRACKS');
    return {content:out.join('\n')+'\n',report:{exported:id-1,unsupported:[...new Set(unsupported)]}};
  }
  function exportJson(config){return JSON.stringify(config,null,2);}
  root.JMRI_TC_PROJECT_FILES={parseXtc,exportXtc,exportJson,baseConfig};
})(typeof window!=='undefined'?window:globalThis);
