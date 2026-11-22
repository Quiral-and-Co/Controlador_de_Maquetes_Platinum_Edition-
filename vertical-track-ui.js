(() => {
'use strict';
const $=id=>document.getElementById(id),core=window.JMRI_TC_VERTICAL_TRACK;
if(!core)throw new Error('No s’ha carregat vertical-track-core.js.');
let plan=null,serial=0;
const number=id=>{const v=$(id).value.trim();return v===''?null:Number(v);};
const catalogue=()=>{const c=window.MARKLIN_C_CATALOGUE||window.JMRI_TC_CATALOGUE||{};return Array.isArray(c)?c:(c.items||[]);};
function limits(){return {greenMax:number('rampGreenMax')??2,yellowMax:number('rampYellowMax')??3,redMax:number('rampRedMax')??5};}
function mode(){return $('verticalMode').value;}
function showMode(){const helixMode=mode()==='helix';$('straightRampFields').classList.toggle('hidden',helixMode);$('helixRampFields').classList.toggle('hidden',!helixMode);$('rampLengthLabel').textContent=helixMode?'Longitud calculada per volta':'Longitud total de la rampa';}
function showResult(p){
 const c=p.classification;$('verticalRampResult').className='vertical-result status-'+c.code;
 if(p.kind==='straight')$('verticalRampResult').innerHTML='<strong>'+c.label+'</strong> · Pendent màxim '+p.gradePercent.toFixed(2)+'% · longitud '+p.lengthMm.toFixed(1)+' mm · '+p.placements.length+' rectes · desnivell '+p.riseMm.toFixed(1)+' mm · ajust '+Math.abs(p.errorMm).toFixed(1)+' mm.';
 else $('verticalRampResult').innerHTML='<strong>'+c.label+'</strong> · Pendent màxim '+p.gradePercent.toFixed(2)+'% · desnivell '+p.riseMm.toFixed(1)+' mm · radi '+p.actualRadiusMm+' mm · '+p.parallelTracks+' via/es · '+p.turns.toFixed(3)+' voltes · '+p.pieceCountPerTrack+' corbes per via.';
 $('verticalRampResult').dataset.status=c.code;
}
function calculate(){
 try{
  if(mode()==='straight'){
   plan=core.straightRamp({lengthMm:number('rampLengthMm'),riseMm:number('rampRiseMm'),gradePercent:number('rampGradePercent'),transitionLengthMm:number('rampTransitionMm')??100},catalogue(),limits());
  }else{
   const radius=number('helixRadiusMm'),curves=catalogue().filter(x=>x.type==='curve'&&x.geometryStatus==='confirmed'&&Math.abs(Number(x.radiusMm)-radius)<=2);
   if(!curves.length)throw new Error('No hi ha una corba confirmada del catàleg a ±2 mm del radi indicat.');
   const turns=number('helixTurns');
   plan=core.helix({radiusMm:radius,riseMm:number('helixRiseMm'),gradePercent:number('helixGradePercent'),parallelTracks:number('helixTrackCount'),spacingMm:number('helixSpacingMm'),turns,transitionLengthMm:number('helixTransitionMm')??100,direction:$('helixDirection').value},catalogue(),limits());
  }
  showResult(plan);$('insertVerticalRampBtn').disabled=plan.classification.code==='brown';
  return plan;
 }catch(e){plan=null;$('verticalRampResult').className='vertical-result status-brown';$('verticalRampResult').textContent='No es pot generar: '+e.message;$('insertVerticalRampBtn').disabled=true;return null;}
}
function refreshTurnPresets(){
 const radius=number('helixRadiusMm'),items=catalogue().filter(x=>x.type==='curve'&&x.geometryStatus==='confirmed'&&Math.abs(Number(x.radiusMm)-radius)<=2).sort((a,b)=>Math.abs(Number(a.radiusMm)-radius)-Math.abs(Number(b.radiusMm)-radius)||Math.abs(360/Number(a.angleDeg)-Math.round(360/Number(a.angleDeg)))-Math.abs(360/Number(b.angleDeg)-Math.round(360/Number(b.angleDeg)))||Number(a.angleDeg)-Number(b.angleDeg));
 const select=$('helixTurns'),previous=select.value;select.replaceChildren();
 if(!items.length){const o=document.createElement('option');o.value='';o.textContent='Sense geometria compatible';select.append(o);return;}
 const opts=core.suggestedTurns(Number(items[0].angleDeg),48);
 opts.forEach(x=>{const o=document.createElement('option');o.value=String(x.turns);o.textContent=x.turns.toFixed(3)+' voltes · '+x.pieces+' corbes de '+items[0].angleDeg+'°';select.append(o);});
 if([...select.options].some(x=>x.value===previous))select.value=previous;else select.value=opts.find(x=>Math.abs(x.turns-1)<.001)?.turns??String(opts[0].turns);
}
function worldPorts(piece){
 const g=piece.geometry||{},local=piece.type==='curve'?window.JMRI_TC_TRACK_JUNCTION.curvePorts(g.radiusMm,g.angleDeg,g.curveDirection||'right'):[{x:0,y:0,port:'A'},{x:g.lengthMm,y:0,port:'B'}],a=(piece.rotation||0)*Math.PI/180,c=Math.cos(a),s=Math.sin(a);
 return local.map(p=>({x:piece.x+p.x*c-p.y*s,y:piece.y+p.x*s+p.y*c,port:p.port}));
}
function insertPlan(){
 if(!plan&&!calculate())return;
 if(plan.classification.code==='brown')throw new Error('La classificació marró fosc impedeix inserir una geometria impossible segons els límits configurats.');
 const dispatcher=window.JMRI_TC_DISPATCHER,board=window.JMRI_TC_BOARD,config=dispatcher?.getConfig?.();
 if(!config||!board?.refresh)throw new Error('L’editor principal no està preparat.');
 const level=config.levels.find(x=>String(x.id)===String(board.getSelectedLevel?.()??config.levels[0].id));if(!level)throw new Error('No hi ha cap nivell seleccionat.');
 const newPieces=[],newConnections=[],baseZ=Number(level.elevationMm)||0,stamp=Date.now().toString(36);
 function addPiece(item,x,y,rotation,z,index,lane,zEnd,risk,grade){
  serial++;const isCurve=item.type==='curve',geometry=isCurve?{lengthMm:180,radiusMm:Number(item.radiusMm),angleDeg:Number(item.angleDeg),curveDirection:plan.direction||$('helixDirection').value||'right'}:{lengthMm:Number(item.lengthMm),radiusMm:360,angleDeg:30};
  const p={id:'RAMP-'+stamp+'-'+serial,type:item.type,name:(plan.kind==='helix'?'Hèlix':'Rampa')+' '+(lane?('V'+lane+' '):'')+(index+1)+' · '+item.ref,x,y,rotation,zMm:baseZ+z,levelId:level.id,blockId:'',sensorName:'',turnoutSystemName:'',catalogueRef:item.ref,geometryStatus:'confirmed',verticalRisk:risk||plan.classification.code,verticalGeometry:{zStartMm:baseZ+z,zEndMm:baseZ+(zEnd??z),gradePercent:grade??plan.gradePercent},geometry};
  newPieces.push(p);return p;
 }
 function connectSequence(sequence,closed){
  for(let i=0;i<sequence.length-(closed?0:1);i++){const a=sequence[i],b=sequence[(i+1)%sequence.length],pa=worldPorts(a).find(x=>x.port==='B'),pb=worldPorts(b).find(x=>x.port==='A');if(!pa||!pb)continue;newConnections.push({id:'RAMP-CON-'+stamp+'-'+(newConnections.length+1),levelId:level.id,from:{pieceId:a.id,port:'B'},to:{pieceId:b.id,port:'A'},verified:false,kind:'vertical-ramp'});}
 }
 if(plan.kind==='straight'){
  const x0=300,y0=700,rotation=0,total=plan.lengthMm;
  if(x0+total>2990)throw new Error('La rampa recta no cap a la superfície del tauler.');
  const seq=[];let x=x0;
  plan.placements.forEach((p,i)=>{const piece=addPiece(p.item,x,y0,rotation,p.zStartMm,i,0,p.zEndMm,plan.classification.code,plan.gradePercent);seq.push(piece);x+=p.item.lengthMm;});
  connectSequence(seq,false);
 }else{
  const centreX=1500,centreY=900,groups=new Map();
  for(const p of plan.placements){
   const laneRadius=p.radiusMm,px=centreX+p.x,py=centreY+p.y;
   if(centreX-laneRadius<10||centreX+laneRadius>2990||centreY-laneRadius<10||centreY+laneRadius>1790)throw new Error('El radi i les vies paral·leles excedeixen els límits del tauler.');
   const piece=addPiece(p.item,px,py,p.rotationDeg,p.zStartMm,p.index,p.lane+1,p.zEndMm,plan.lanes[p.lane].classification.code,plan.lanes[p.lane].gradePercent);
   if(!groups.has(p.lane))groups.set(p.lane,[]);groups.get(p.lane).push(piece);
  }
  groups.forEach(seq=>connectSequence(seq,Math.abs(plan.turns-Math.round(plan.turns))<1e-6));
 }
 if(!Array.isArray(config.verticalRoutes))config.verticalRoutes=[];
 config.verticalRoutes.push({id:'VR-'+stamp,kind:plan.kind,levelId:level.id,createdAt:new Date().toISOString(),riseMm:plan.riseMm,gradePercent:plan.gradePercent,classification:plan.classification.code,verified:false,pieceIds:newPieces.map(p=>p.id),connectionIds:newConnections.map(c=>c.id),parameters:plan.kind==='helix'?{radiusMm:plan.actualRadiusMm,parallelTracks:plan.parallelTracks,spacingMm:plan.spacingMm,turns:plan.turns}:{lengthMm:plan.lengthMm,transitionLengthMm:plan.transitionLengthMm}});
 level.tracks.push(...newPieces);config.connections.push(...newConnections);board.refresh();
 $('verticalRampResult').textContent+=' Inserides '+newPieces.length+' peces i '+newConnections.length+' connexions no verificades al '+level.name+'.';
 $('insertVerticalRampBtn').disabled=true;return newPieces.length;
}
$('verticalMode').addEventListener('change',()=>{showMode();if(mode()==='helix')refreshTurnPresets();});
$('helixRadiusMm').addEventListener('change',refreshTurnPresets);
$('calculateVerticalRampBtn').addEventListener('click',calculate);
$('insertVerticalRampBtn').addEventListener('click',()=>{try{insertPlan();}catch(e){$('verticalRampResult').textContent='Inserció bloquejada: '+e.message;}});
$('helixGradePercent').addEventListener('change',()=>{$('helixRiseMm').value='';plan=null;$('insertVerticalRampBtn').disabled=true;});
$('helixRiseMm').addEventListener('change',()=>{$('helixGradePercent').value='';plan=null;$('insertVerticalRampBtn').disabled=true;});
['rampLengthMm','rampRiseMm','rampGradePercent','rampTransitionMm','helixRiseMm','helixGradePercent','helixTrackCount','helixSpacingMm','helixTurns','helixTransitionMm','rampGreenMax','rampYellowMax','rampRedMax'].forEach(id=>$(id).addEventListener('change',()=>{plan=null;$('insertVerticalRampBtn').disabled=true;}));
showMode();refreshTurnPresets();$('insertVerticalRampBtn').disabled=true;
})();
