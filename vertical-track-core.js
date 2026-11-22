(function(root,factory){'use strict';const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;if(root)root.JMRI_TC_VERTICAL_TRACK=api;})(typeof window!=='undefined'?window:globalThis,function(){
'use strict';
const finite=(v,n)=>{const x=Number(v);if(!Number.isFinite(x))throw new TypeError(n+' ha de ser un nombre finit.');return x;};
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
function smoothIntegral(u){return u*u*u-.5*u*u*u*u;}
function profileDistance(s,total,transition){
  const x=clamp(s,0,total),t=clamp(transition,0,total/2);
  if(t<=0)return x;
  if(x<t)return t*smoothIntegral(x/t);
  if(x<=total-t)return x-t/2;
  const v=(total-x)/t;
  return total-t-t*smoothIntegral(v);
}
function elevationAt(s,total,rise,transition){const effective=total-clamp(transition,0,total/2);return rise*(effective>0?profileDistance(s,total,transition)/effective:0);}
function classifyGradient(grade,limits={}){
  const g= Math.abs(finite(grade,'El pendent'));
  const green=finite(limits.greenMax??2,'Límit verd'),yellow=finite(limits.yellowMax??3,'Límit groc'),red=finite(limits.redMax??5,'Límit vermell');
  if(!(green>=0&&green<=yellow&&yellow<=red))throw new RangeError('Els llindars han de complir 0 ≤ verd ≤ groc ≤ vermell.');
  if(g<=green)return {code:'green',label:'Correcte',gradePercent:g};
  if(g<=yellow)return {code:'yellow',label:'No recomanable',gradePercent:g};
  if(g<=red)return {code:'red',label:'Risc alt',gradePercent:g};
  return {code:'brown',label:'Impossible segons els límits configurats',gradePercent:g};
}
function confirmedStraights(catalogue){
  return (catalogue||[]).filter(x=>x&&x.type==='straight'&&x.geometryStatus==='confirmed'&&Number(x.lengthMm)>0).map(x=>({...x,lengthMm:Number(x.lengthMm)}));
}
function fitStraightPieces(length,catalogue,tolerance=1){
  const target=finite(length,'La longitud'),tol=finite(tolerance,'La tolerància');
  if(target<=0)throw new RangeError('La longitud ha de ser superior a zero.');
  const items=confirmedStraights(catalogue);if(!items.length)throw new Error('El catàleg no conté rectes amb geometria confirmada.');
  const unit=10,limit=Math.round((target+tol)*unit),dp=new Array(limit+1).fill(null);dp[0]=[];
  const coins=items.map(item=>({item,n:Math.round(item.lengthMm*unit)})).filter(x=>x.n>0);
  for(let i=0;i<=limit;i++){if(dp[i]===null)continue;for(const c of coins){const next=i+c.n;if(next<=limit&&(!dp[next]||dp[next].length>dp[i].length+1))dp[next]=dp[i].concat(c.item);}}
  let best=-1;for(let i=0;i<=limit;i++)if(dp[i]&&Math.abs(i/unit-target)<=tol&&(best<0||dp[i].length<dp[best].length||dp[i].length===dp[best].length&&Math.abs(i/unit-target)<Math.abs(best/unit-target)))best=i;
  if(best<0)throw new Error('No hi ha cap combinació de rectes confirmades dins la tolerància de '+tol+' mm.');
  return {items:dp[best],actualLengthMm:best/unit,errorMm:best/unit-target};
}
function straightRamp(input,catalogue,limits={}){
  const rise=finite(input.riseMm,'L’alçada'),transition=Math.max(0,finite(input.transitionLengthMm??100,'La transició'));
  if(rise<=0)throw new RangeError('L’alçada ha de ser superior a zero.');
  let length=input.lengthMm===undefined||input.lengthMm===null||input.lengthMm===''?null:finite(input.lengthMm,'La longitud');
  const requestedGrade=input.gradePercent===undefined||input.gradePercent===null||input.gradePercent===''?null:finite(input.gradePercent,'El pendent');
  if(length===null&&requestedGrade===null)throw new Error('Indica la longitud o el pendent per calcular la rampa.');
  if(requestedGrade!==null&&requestedGrade<=0)throw new RangeError('El pendent ha de ser superior a zero.');
  if(length===null)length=rise/(requestedGrade/100)+transition;
  if(length<=transition||transition>length/2)throw new RangeError('La longitud ha de superar la transició i deixar un tram central.');
  const fit=fitStraightPieces(length,catalogue,input.toleranceMm??1);
  length=fit.actualLengthMm;
  if(length<=transition||transition>length/2)throw new RangeError('La combinació real de vies no deixa prou longitud per als trams de transició.');
  const effectiveRun=length-transition,grade=rise/effectiveRun*100;
  if(requestedGrade!==null&&input.lengthMm!==undefined&&input.lengthMm!==null&&input.lengthMm!==''&&Math.abs(grade-requestedGrade)>0.05)throw new Error('La longitud real de les vies i el pendent introduït no coincideixen amb l’alçada i les transicions.');
  const classification=classifyGradient(grade,limits);
  let s=0;const placements=fit.items.map((item,index)=>{const p={item,index,distanceStartMm:s,distanceEndMm:s+item.lengthMm,zStartMm:elevationAt(s,length,rise,transition),zEndMm:elevationAt(s+item.lengthMm,length,rise,transition)};s+=item.lengthMm;return p;});
  return {kind:'straight',lengthMm:fit.actualLengthMm,requestedLengthMm:length,riseMm:rise,transitionLengthMm:transition,gradePercent:grade,classification,placements,errorMm:fit.errorMm};
}
function curveCatalogue(catalogue,radius){
  const items=(catalogue||[]).filter(x=>x&&x.type==='curve'&&x.geometryStatus==='confirmed'&&Number(x.radiusMm)>0&&Number(x.angleDeg)>0&&Number(x.angleDeg)<180);
  if(!items.length)throw new Error('El catàleg no conté corbes amb geometria confirmada.');
  return items.map(x=>({...x,radiusMm:Number(x.radiusMm),angleDeg:Number(x.angleDeg)})).sort((a,b)=>Math.abs(a.radiusMm-radius)-Math.abs(b.radiusMm-radius)||Math.abs(360/a.angleDeg-Math.round(360/a.angleDeg))-Math.abs(360/b.angleDeg-Math.round(360/b.angleDeg))||a.angleDeg-b.angleDeg);
}
function suggestedTurns(angleDeg,maxPieces=48){
  const a=finite(angleDeg,'L’angle');if(a<=0||a>=180)throw new RangeError('Angle de corba invàlid.');
  const out=[];for(let n=2;n<=maxPieces;n++){const turns=n*a/360;const label=Number(turns.toFixed(3));if(!out.some(x=>Math.abs(x.turns-label)<1e-6))out.push({turns:label,pieces:n,angleDeg:a});}
  return out;
}
function helix(input,catalogue,limits={}){
  const radius=finite(input.radiusMm,'El radi'),riseInput=input.riseMm===undefined||input.riseMm===null||input.riseMm===''?null:finite(input.riseMm,'L’alçada'),requestedGrade=input.gradePercent===undefined||input.gradePercent===null||input.gradePercent===''?null:finite(input.gradePercent,'El pendent'),lanes=Math.floor(finite(input.parallelTracks??1,'El nombre de vies')),spacing=finite(input.spacingMm??77,'La separació'),turns=finite(input.turns,'El nombre de voltes'),transition=Math.max(0,finite(input.transitionLengthMm??100,'La transició'));
  let rise=riseInput;
  if(radius<=0||(rise!==null&&rise<=0)||(requestedGrade!==null&&requestedGrade<=0)||(rise===null&&requestedGrade===null)||lanes<1||lanes>8||spacing<=0||turns<=0||turns>8)throw new RangeError('Radi, alçada/pendent, nombre de vies o voltes fora de rang.');
  const selected=curveCatalogue(catalogue,radius)[0],radiusError=Math.abs(selected.radiusMm-radius);
  if(radiusError>2)throw new Error('No hi ha una corba Märklin C confirmada a ±2 mm del radi sol·licitat. Radi més proper: '+selected.radiusMm+' mm.');
  const piecesExact=360*turns/selected.angleDeg,pieceCount=Math.round(piecesExact);
  if(pieceCount<2||Math.abs(piecesExact-pieceCount)>0.005)throw new Error('El nombre de voltes no és compatible amb l’angle de la via. Voltes suggerides: '+suggestedTurns(selected.angleDeg).slice(0,10).map(x=>x.turns).join(', ')+'.');
  const actualTurns=pieceCount*selected.angleDeg/360,placements=[],lanesSummary=[];
  const baseEffective=2*Math.PI*selected.radiusMm*actualTurns-transition;
  if(baseEffective<=0||transition>Math.PI*selected.radiusMm*actualTurns)throw new RangeError('La transició és massa llarga per a aquesta geometria helicoidal.');
  if(rise===null)rise=baseEffective*requestedGrade/100;
  else if(requestedGrade!==null&&Math.abs(rise/baseEffective*100-requestedGrade)>0.05)throw new Error('L’alçada i el pendent introduïts no coincideixen amb el radi, les voltes i les transicions.');
  for(let lane=0;lane<lanes;lane++){
    const r=selected.radiusMm+lane*spacing,totalLength=2*Math.PI*r*actualTurns,effective=totalLength-transition;
    if(effective<=0||transition>totalLength/2)throw new RangeError('La transició és massa llarga per a aquesta geometria helicoidal.');
    const grade=rise/effective*100,classification=classifyGradient(grade,limits),startY=(input.direction==='left'?1:-1)*r;
    const lanePlacements=[];let x=0,y=startY,heading=0;
    for(let i=0;i<pieceCount;i++){
      const p={lane,index:i,item:selected,x,y,rotationDeg:heading*180/Math.PI,radiusMm:r,direction:input.direction==='left'?'left':'right',distanceStartMm:i*totalLength/pieceCount,distanceEndMm:(i+1)*totalLength/pieceCount};
      p.zStartMm=elevationAt(p.distanceStartMm,totalLength,rise,transition);p.zEndMm=elevationAt(p.distanceEndMm,totalLength,rise,transition);
      lanePlacements.push(p);placements.push(p);
      const a=selected.angleDeg*Math.PI/180,sign=p.direction==='left'?-1:1,localX=r*Math.sin(a),localY=sign*r*(1-Math.cos(a)),rot=heading;
      x+=localX*Math.cos(rot)-localY*Math.sin(rot);y+=localX*Math.sin(rot)+localY*Math.cos(rot);heading+=sign*a;
    }
    lanesSummary.push({lane,radiusMm:r,gradePercent:grade,classification,pieceCount,actualTurns,totalLengthMm:totalLength,closureErrorMm:Math.hypot(x,y-startY)});
  }
  const rank={green:0,yellow:1,red:2,brown:3};
  const worstLane=lanesSummary.reduce((a,b)=>rank[a.classification.code]>rank[b.classification.code]?a:b);
  return {kind:'helix',requestedRadiusMm:radius,actualRadiusMm:selected.radiusMm,radiusErrorMm:radiusError,curve:selected,parallelTracks:lanes,spacingMm:spacing,riseMm:rise,transitionLengthMm:transition,turns:actualTurns,requestedTurns:turns,pieceCountPerTrack:pieceCount,placements,lanes:lanesSummary,classification:worstLane.classification,gradePercent:Math.max(...lanesSummary.map(x=>x.gradePercent))};
}
return Object.freeze({classifyGradient,fitStraightPieces,straightRamp,helix,suggestedTurns,elevationAt});
});
