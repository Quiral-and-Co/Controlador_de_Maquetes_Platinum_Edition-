(function(root,factory){
  'use strict';
  const api=factory();
  if(typeof module==='object'&&module.exports)module.exports=api;
  if(root)root.JMRI_TC_TRACK_JUNCTION=api;
})(typeof window!=='undefined'?window:globalThis,function(){
  'use strict';
  function finite(v,label){const n=Number(v);if(!Number.isFinite(n))throw new TypeError(label+' ha de ser finit.');return n;}
  function curvePorts(radius,angleDeg,direction){
    const r=finite(radius,'El radi'),deg=finite(angleDeg,'L’angle'),a=deg*Math.PI/180;
    if(r<=0||deg<=0||deg>=180)throw new RangeError('Geometria de corba invàlida.');
    const sign=direction==='left'?-1:1;
    return [{x:0,y:0,port:'A'},{x:r*Math.sin(a),y:sign*r*(1-Math.cos(a)),port:'B'}];
  }
  function turnoutPorts(length,branchOffset,kind){
    const len=finite(length,'La longitud'),off=finite(branchOffset,'El desplaçament de branca');
    if(len<=0||off<=0)throw new RangeError('Geometria de desviament invàlida.');
    if(kind==='double-slip')return [{x:0,y:0,port:'A'},{x:len,y:-off,port:'B'},{x:0,y:-off,port:'C'},{x:len,y:0,port:'D'}];
    return [{x:0,y:0,port:'A'},{x:len,y:0,port:'B'},{x:len*.83,y:-off,port:'C'}];
  }
  function preferredPort(piece){
    const mode=piece&&piece.connectionPoint;
    if(piece&&piece.type==='turnout'&&(piece.catalogueRef==='24624'||piece.junctionKind==='double-slip'))return ['A','B','C','D'].includes(mode)?mode:'A';
    if(piece&&piece.type==='turnout')return ['A','B','C'].includes(mode)?mode:'A';
    return null;
  }
  function catalogueSequences(catalogue,maxPieces){
    const eligible=(catalogue||[]).filter(x=>x.geometryStatus==='confirmed'&&
      ((x.type==='straight'&&Number(x.lengthMm)>0)||(x.type==='curve'&&Number(x.radiusMm)>0&&Number(x.angleDeg)>0&&Number(x.angleDeg)<180)));
    const sequences=[];
    function extend(state,depth){
      if(depth>0){
        const chord=Math.hypot(state.x,state.y);
        if(chord>0)sequences.push({items:state.items.slice(),placements:state.placements.slice(),vector:{x:state.x,y:state.y},chordLengthMm:chord});
      }
      if(depth===maxPieces)return;
      for(const item of eligible){
        const variants=item.type==='curve'?['right','left']:[null];
        for(const direction of variants){
          const heading=state.heading,cos=Math.cos(heading),sin=Math.sin(heading);
          const local=item.type==='straight'?{x:Number(item.lengthMm),y:0}:{x:curvePorts(item.radiusMm,item.angleDeg,direction)[1].x,y:curvePorts(item.radiusMm,item.angleDeg,direction)[1].y};
          const x=state.x+local.x*cos-local.y*sin,y=state.y+local.x*sin+local.y*cos;
          const placement={item,direction,x:state.x,y:state.y,rotationDeg:heading*180/Math.PI};
          const turn=item.type==='curve'?(direction==='left'?-1:1)*Number(item.angleDeg)*Math.PI/180:0;
          extend({x,y,heading:heading+turn,items:[...state.items,{item,direction}],placements:[...state.placements,placement]},depth+1);
        }
      }
    }
    extend({x:0,y:0,heading:0,items:[],placements:[]},0);
    return sequences;
  }
  function recommendGaps(openPorts,catalogue,maxGap){
    const limit=maxGap===undefined?500:finite(maxGap,'La distància màxima');
    const sequences=catalogueSequences(catalogue,3),results=[];
    for(let i=0;i<openPorts.length;i++)for(let j=i+1;j<openPorts.length;j++){
      const a=openPorts[i],b=openPorts[j];
      if(a.pieceId===b.pieceId||String(a.levelId)!==String(b.levelId))continue;
      const dx=b.x-a.x,dy=b.y-a.y,distance=Math.hypot(dx,dy);
      if(distance<40||distance>limit)continue;
      const candidates=sequences.map(seq=>{
        const rotationDeg=(Math.atan2(dy,dx)-Math.atan2(seq.vector.y,seq.vector.x))*180/Math.PI;
        return {...seq,totalLengthMm:seq.chordLengthMm,errorMm:Math.abs(seq.chordLengthMm-distance),rotationDeg};
      }).filter(c=>c.errorMm<=2).sort((x,y)=>x.errorMm-y.errorMm||x.items.length-y.items.length).slice(0,3);
      if(candidates.length)results.push({from:a,to:b,distanceMm:distance,candidates});
    }
    return results.sort((a,b)=>a.distanceMm-b.distanceMm);
  }
  return Object.freeze({curvePorts,turnoutPorts,preferredPort,recommendGaps});
});
