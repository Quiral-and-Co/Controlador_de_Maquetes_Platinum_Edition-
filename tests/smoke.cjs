const fs = require('fs');
const vm = require('vm');
const viewport = require('../viewport-core.js');
const selectionMove = require('../selection-move-core.js');
const trackJunction = require('../track-junction-core.js');
class El {
  constructor(id=''){this.id=id;this.value='';this.textContent='';this.innerHTML='';this.children=[];this.dataset={};this.handlers={};this.className='';this.disabled=false;this.classList={add:()=>{},toggle:()=>{},remove:()=>{}};this.style={};}
  addEventListener(n,fn){this.handlers[n]=fn;}
  append(...xs){xs.forEach(x=>{x.parent=this;this.children.push(x);});}
  prepend(x){x.parent=this;this.children.unshift(x);}
  replaceChildren(...xs){this.children=[...xs];xs.forEach(x=>{x.parent=this;});}
  setAttribute(k,v){this[k]=v;}
  getAttribute(k){return this[k]??null;}
  remove(){if(this.parent){const index=this.parent.children.indexOf(this);if(index>=0)this.parent.children.splice(index,1);}}
  getBoundingClientRect(){return {left:0,top:0,width:1000,height:600};}
  createSVGPoint(){const point={x:0,y:0,matrixTransform(){return {x:this.x,y:this.y};}};return point;}
  getScreenCTM(){return {inverse(){return {};}};}
  setPointerCapture(){}
  closest(){return null;}
}
const ids=['levelCount','pieceCount','connectionCount','blockCount','unknownCount','levelList','currentLevelTitle','currentLevelSubtitle','pieceLayer','connectionLayer','portLayer','emptyProperties','pieceProperties','selectionText','propId','propName','propType','propX','propY','propRotation','propLength','propRadius','propAngle','propBlock','propSensor','rotateBtn','deleteBtn','duplicateBtn','undoBtn','redoBtn','measureBtn','measureStatus','connectBtn2','portModeBtn','portStatus','trackSvg','cursorCoords','eventLog','modernBlocks','sensorSummary','endpointText','host','port','connectionText','connectionBadge','connectBtn','disconnectBtn','configFile','configName','exportBtn','modeClassic','modeModern','modernPanel','newBlockName','addBlockBtn','blockRegistry','newSensorSystemName','newSensorUserName','addSensorBtn','sensorRegistry','propTurnout','propLevel','moveLevelBtn','rotationQuadrant','rotationCustomAngle','rotationAngleReadout','rotationPreset0','rotationPreset30','rotationPreset45','rotationPreset60','rotationPreset90','validateTopologyBtn','topologyReport','catalogueSearch','catalogueCategory','catalogueList','zoomInBtn','zoomOutBtn','zoomFitBtn','zoomReadout'];
const els=Object.fromEntries(ids.map(x=>[x,new El(x)]));els.host.value='127.0.0.1';els.port.value='12080';
const palette=['straight','curve','turnout','block'].map(type=>{const e=new El();e.dataset.piece=type;return e;});
const shell=new El('shell');
const docHandlers={};
const document={addEventListener:(name,fn)=>{docHandlers[name]=fn;},getElementById:id=>els[id]||(els[id]=new El(id)),createElement:tag=>new El(tag),createElementNS:()=>new El('svg'),querySelectorAll:q=>q==='[data-piece]'?palette:[],querySelector:q=>shell};
const window={MARKLIN_C_CATALOGUE:JSON.parse(fs.readFileSync(__dirname+'/../catalogue-marklin-c.json','utf8')),JMRI_TC_VIEWPORT:viewport,JMRI_TC_SELECTION_MOVE:selectionMove,JMRI_TC_TRACK_JUNCTION:trackJunction,addEventListener:()=>{}};
vm.runInNewContext(fs.readFileSync(__dirname+'/../app.js','utf8'),{document,window,console,Date,Math,JSON,Number,String,Array,Set,Map,WebSocket:function(){}});
if(els.levelCount.textContent!==5)throw Error('Expected five levels');
if(els.zoomReadout.textContent!=='100%')throw Error('Viewport must start at 100% fit');
els.zoomInBtn.onclick();const zoomedView=window.JMRI_TC_VIEWPORT_UI.getViewBox();if(!(zoomedView.width<3000))throw Error('Zoom-in button did not enlarge the board');
docHandlers.keydown({code:'Space',key:' ',target:{tagName:'BODY'},preventDefault(){}});
els.trackSvg.handlers.pointerdown({button:0,pointerId:9,clientX:100,clientY:100,preventDefault(){}});
els.trackSvg.handlers.pointermove({pointerId:9,clientX:200,clientY:150});
const pannedView=window.JMRI_TC_VIEWPORT_UI.getViewBox();if(!(pannedView.x<zoomedView.x&&pannedView.y<zoomedView.y))throw Error('Space-drag did not pan the board');
els.trackSvg.handlers.pointerup({pointerId:9});docHandlers.keyup({code:'Space'});
els.zoomFitBtn.onclick();if(window.JMRI_TC_VIEWPORT_UI.getViewBox().width!==3000)throw Error('Fit button did not restore full board');
if(els.catalogueList.children.length!==68)throw Error('Expected 68 catalogue entries, got '+els.catalogueList.children.length);
const c24188=els.catalogueList.children.find(x=>x.dataset.catalogue==='24188');if(!c24188)throw Error('Catalogue reference 24188 missing');c24188.onclick();if(els.pieceCount.textContent!==1||!els.propName.value.includes('24188'))throw Error('Catalogue insertion failed');if(els.propLength.value!==188.3)throw Error('Catalogue length geometry not applied');els.undoBtn.onclick();if(els.pieceCount.textContent!==0)throw Error('Undo catalogue insertion failed');
els.validateTopologyBtn.onclick();if(!els.topologyReport.textContent.includes('0 error(s)'))throw Error('Empty topology should validate without errors');
palette[0].onclick();
if(els.pieceCount.textContent!==1)throw Error('Palette did not add a track primitive');
if(!els.propId.value.startsWith('STRAIGHT-'))throw Error('New piece not selected');
els.rotateBtn.onclick();
if(els.propRotation.value!==15)throw Error('Rotation action failed');
els.propName.value='Recta de prova';els.propName.handlers.change({target:els.propName});
if(els.pieceLayer.children.length!==1)throw Error('SVG piece not rendered');
els.propLength.value='240';els.propLength.handlers.change({target:els.propLength});
if(els.pieceLayer.children.length!==1)throw Error('Geometry edit broke SVG render');
if(els.propLength.value!==240)throw Error('Length property was not applied');

els.duplicateBtn.onclick();if(els.pieceCount.textContent!==2)throw Error('Duplicate action failed');
els.undoBtn.onclick();if(els.pieceCount.textContent!==1)throw Error('Undo failed to restore piece count');
els.redoBtn.onclick();if(els.pieceCount.textContent!==2)throw Error('Redo failed to restore duplicated piece');
// Bloc and sensor registry + assignment
els.newBlockName.value='B01';els.addBlockBtn.onclick();
if(!els.blockRegistry.innerHTML.includes('B01'))throw Error('Block registry did not create B01');
els.propBlock.value='B01';els.propBlock.handlers.change({target:els.propBlock});
if(!els.blockRegistry.innerHTML.includes('1 peça/es'))throw Error('Block assignment was not reflected in trackIds');
els.newSensorSystemName.value='LS101';els.newSensorUserName.value='Detector prova';els.addSensorBtn.onclick();
if(!els.sensorRegistry.innerHTML.includes('LS101'))throw Error('Sensor registry did not create LS101');
els.propSensor.value='LS101';els.propSensor.handlers.change({target:els.propSensor});
if(!els.sensorSummary.textContent.includes('LS101'))throw Error('Sensor assignment was not reflected in monitoring summary');
if(!window.JMRI_TC_BOARD||window.JMRI_TC_BOARD.getSensorState({sensorName:'LS101'}).code!=='UNKNOWN')throw Error('Board sensor should fail closed when reading/verification is missing');
window.JMRI_TC_BOARD.applySensorReading('LS101',4);
if(window.JMRI_TC_BOARD.getSensorState({sensorName:'LS101'}).code!=='UNKNOWN')throw Error('Unverified sensor must remain unknown on the board');
if(!els.pieceLayer.children.some(x=>x.innerHTML.includes('sensor-indicator')))throw Error('Assigned sensor indicator missing from SVG piece');
palette[2].onclick();
els.propTurnout.value='LT1';els.propTurnout.handlers.change({target:els.propTurnout});
window.JMRI_TC_BOARD.applyTurnoutReading('LT1',2);
if(window.JMRI_TC_BOARD.getTurnoutState({type:'turnout',turnoutSystemName:'LT1'}).code!=='CLOSED')throw Error('JMRI turnout CLOSED state was not mapped');
if(!els.pieceLayer.children.some(x=>x.innerHTML.includes('turnout-indicator')))throw Error('Assigned turnout indicator missing from SVG piece');
// Level move is blocked until level-local block and sensor assignments are cleared
els.propLevel.value='2';els.moveLevelBtn.onclick();if(els.currentLevelTitle.textContent!=='Nivell 1')throw Error('Unsafe move with assigned block/sensor was not blocked');
els.propBlock.value='';els.propBlock.handlers.change({target:els.propBlock});
els.propSensor.value='';els.propSensor.handlers.change({target:els.propSensor});
els.propTurnout.value='';els.propTurnout.handlers.change({target:els.propTurnout});
els.propLevel.value='2';els.moveLevelBtn.onclick();if(els.currentLevelTitle.textContent!=='Nivell 2')throw Error('Piece level reassignment failed');
(async()=>{
  const legacy={schemaVersion:2,name:'Legacy',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:`Nivell ${i+1}`,elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]};
  legacy.levels[0].tracks.push({id:'LEG-1',type:'straight',name:'Peça importada',x:100,y:100,rotation:0,levelId:1,blockId:'OLD01',sensorName:'LS77',geometry:{lengthMm:180,radiusMm:360,angleDeg:30}});
  await els.configFile.onchange({target:{files:[{name:'legacy-v2.json',text:async()=>JSON.stringify(legacy)}],value:''}});
  if(!els.blockRegistry.innerHTML.includes('OLD01')||!els.blockRegistry.innerHTML.includes('1 peça/es'))throw Error('v2 migration did not create provisional block registry');
  if(!els.sensorRegistry.innerHTML.includes('LS77')||!els.sensorSummary.textContent.includes('LS77'))throw Error('v2 migration did not create provisional sensor registry');
  els.validateTopologyBtn.onclick();if(!els.topologyReport.textContent.includes('0 error(s)'))throw Error('Topology audit should remain valid after migration');
  const topo={schemaVersion:3,name:'Topology test',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:`Nivell ${i+1}`,elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]};
  const mk=(id,x)=>({id,type:'straight',name:id,x,y:100,rotation:0,levelId:1,blockId:'',sensorName:'',geometry:{lengthMm:180,radiusMm:360,angleDeg:30}});
  topo.levels[0].tracks=[mk('T-A',100),mk('T-B',280)];
  topo.connections=[{id:'C1',levelId:1,from:{pieceId:'T-A',port:'B'},to:{pieceId:'T-B',port:'A'},verified:false},{id:'C2',levelId:1,from:{pieceId:'T-A',port:'B'},to:{pieceId:'T-B',port:'A'},verified:false}];
  await els.configFile.onchange({target:{files:[{name:'topology-duplicate.json',text:async()=>JSON.stringify(topo)}],value:''}});
  els.validateTopologyBtn.onclick();if(!els.topologyReport.textContent.includes('error(s)')||!els.topologyReport.textContent.includes('duplicada'))throw Error('Topology audit failed to detect duplicated connection/occupied ports');
  // Reset to a clean, empty five-level project for the manual-connection acceptance test.
  const clean={schemaVersion:3,name:'Manual ports',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:`Nivell ${i+1}`,elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]};
  await els.configFile.onchange({target:{files:[{name:'clean.json',text:async()=>JSON.stringify(clean)}],value:''}});
  // Manual, explicit port-to-port connection: no nearest-port guessing and no implicit piece movement.
  // Create two fresh straight primitives and align the second A port with the first B port.
  const connectionCountBefore=Number(els.connectionCount.textContent);
  palette[0].onclick();
  const firstId=els.propId.value, first=els.pieceLayer.children.find(x=>x.dataset.id===firstId);
  const firstPiece=els.portLayer.children.filter(x=>x.dataset.pieceId===firstId);
  let firstB=firstPiece.find(x=>x.dataset.port==='B');
  palette[0].onclick();
  const secondId=els.propId.value;
  if(firstId===secondId)throw Error('New track piece identifiers must be unique');
  els.propX.value='480';els.propX.handlers.change({target:els.propX});
  els.propY.value='300';els.propY.handlers.change({target:els.propY});
  els.portModeBtn.onclick();
  const findPort=(id,port)=>els.portLayer.children.find(x=>x.dataset.pieceId===id&&x.dataset.port===port);
  firstB=findPort(firstId,'B');const secondA=findPort(secondId,'A');
  if(firstB.getAttribute('role')!=='button'||firstB.getAttribute('tabindex')!=='0')throw Error('Port marker is not keyboard accessible');
  firstB.handlers.keydown({key:'Enter',currentTarget:firstB,preventDefault(){},stopPropagation(){}});
  secondA.handlers.click({currentTarget:secondA,stopPropagation(){}});
  if(!els.portStatus.textContent.includes('Connectats'))throw Error('Manual port connection failed for coincident endpoints');
  if(Number(els.connectionCount.textContent)!==connectionCountBefore+1)throw Error('Manual connection was not added to the model');
  // Self-connection must be rejected without changing the connection count.
  const firstA=findPort(firstId,'A');
  firstA.handlers.click({currentTarget:firstA,stopPropagation(){}});
  firstB.handlers.click({currentTarget:firstB,stopPropagation(){}});
  if(!els.portStatus.textContent.includes('altra peça'))throw Error('Self-connection was not rejected');
  if(Number(els.connectionCount.textContent)!==connectionCountBefore+1)throw Error('Rejected self-connection changed the model');
  // A remote endpoint (>1 mm) must be rejected, with no implicit movement.
  palette[0].onclick();const remoteId=els.propId.value;
  els.propX.value='1200';els.propX.handlers.change({target:els.propX});
  els.propY.value='700';els.propY.handlers.change({target:els.propY});
  const remoteA=findPort(remoteId,'A'),beforeRemote={x:els.propX.value,y:els.propY.value};
  firstA.handlers.click({currentTarget:firstA,stopPropagation(){}});
  remoteA.handlers.click({currentTarget:remoteA,stopPropagation(){}});
  if(!els.portStatus.textContent.includes('No connectats'))throw Error('Distant port connection was not rejected');
  if(Number(els.connectionCount.textContent)!==connectionCountBefore+1)throw Error('Rejected distant connection changed the model');
  if(els.propX.value!==beforeRemote.x||els.propY.value!==beforeRemote.y)throw Error('Rejected connection moved a piece implicitly');
  // An already occupied port must not be accepted as the first endpoint.
  firstB.handlers.click({currentTarget:firstB,stopPropagation(){}});
  if(els.portStatus.textContent.includes('Primer port:'))throw Error('Occupied port was accepted as a first endpoint');
  els.validateTopologyBtn.onclick();
  if(!els.topologyReport.textContent.includes('0 error(s)'))throw Error('Manual connection should pass topology audit');
  // Multi-selection drag: one snapped translation, preserved offsets and one undo step.
  // Keep the mock event log below its UI retention limit before exercising undo/redo.
  els.eventLog.children=[];
  const emptyGroupProject={schemaVersion:3,name:'Group move test',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:`Nivell ${i+1}`,elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]};
  await els.configFile.onchange({target:{files:[{name:'group-move.json',text:async()=>JSON.stringify(emptyGroupProject)}],value:''}});
  palette[0].onclick();const groupA=els.propId.value;
  palette[0].onclick();const groupB=els.propId.value;
  let groupAElement=els.pieceLayer.children.find(x=>x.dataset.id===groupA);
  groupAElement.handlers.pointerdown({button:0,currentTarget:groupAElement,ctrlKey:true,metaKey:false});
  groupAElement=els.pieceLayer.children.find(x=>x.dataset.id===groupA);
  groupAElement.handlers.pointerdown({button:0,pointerId:77,currentTarget:groupAElement,clientX:305,clientY:305,ctrlKey:false,metaKey:false});
  els.trackSvg.handlers.pointermove({pointerId:77,clientX:357,clientY:356});
  els.trackSvg.handlers.pointerup({pointerId:77});
  let groupTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  const groupPos=()=>Object.fromEntries(groupTracks.map(x=>[x.id,{x:x.x,y:x.y}]));
  if(groupPos()[groupA].x!==350||groupPos()[groupA].y!==350||groupPos()[groupB].x!==380||groupPos()[groupB].y!==380)throw Error('Multi-selection drag did not move all pieces by the same snapped delta');
  els.undoBtn.onclick();groupTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  if(groupTracks.find(x=>x.id===groupA).x!==300||groupTracks.find(x=>x.id===groupB).x!==330)throw Error('A single undo did not restore the whole multi-piece movement');
  els.redoBtn.onclick();groupTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  groupAElement=els.pieceLayer.children.find(x=>x.dataset.id===groupA);
  groupAElement.handlers.pointerdown({button:0,pointerId:78,currentTarget:groupAElement,clientX:355,clientY:355,ctrlKey:false,metaKey:false});
  els.trackSvg.handlers.pointermove({pointerId:78,clientX:405,clientY:405});
  els.trackSvg.handlers.pointercancel({pointerId:78});
  groupTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  if(groupTracks.find(x=>x.id===groupA).x!==350||groupTracks.find(x=>x.id===groupB).x!==380)throw Error('Pointer cancel did not restore the full selection');
  groupAElement=els.pieceLayer.children.find(x=>x.dataset.id===groupA);
  groupAElement.handlers.pointerdown({button:0,pointerId:79,currentTarget:groupAElement,clientX:355,clientY:355,ctrlKey:false,metaKey:false});
  els.trackSvg.handlers.pointermove({pointerId:79,clientX:10000,clientY:10000});
  els.trackSvg.handlers.pointerup({pointerId:79});
  groupTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  if(groupTracks.find(x=>x.id===groupB).x>3000||groupTracks.find(x=>x.id===groupB).y>1800)throw Error('Multi-selection movement escaped the board boundaries');
  els.undoBtn.onclick();
  // Joint rotation around the selection centre, including an internal and an external connection.
  const rotationProject={schemaVersion:3,name:'Group rotation topology',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:`Nivell ${i+1}`,elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]};
  const rotationPiece=(id,x)=>({id,type:'straight',name:id,x,y:100,rotation:0,levelId:1,blockId:'',sensorName:'',turnoutSystemName:'',geometry:{lengthMm:180,radiusMm:360,angleDeg:30}});
  rotationProject.levels[0].tracks=[rotationPiece('ROT-A',100),rotationPiece('ROT-B',280),rotationPiece('ROT-C',460)];
  rotationProject.connections=[{id:'ROT-INTERNAL',levelId:1,from:{pieceId:'ROT-A',port:'B'},to:{pieceId:'ROT-B',port:'A'},verified:false},{id:'ROT-EXTERNAL',levelId:1,from:{pieceId:'ROT-B',port:'B'},to:{pieceId:'ROT-C',port:'A'},verified:false}];
  await els.configFile.onchange({target:{files:[{name:'group-rotation.json',text:async()=>JSON.stringify(rotationProject)}],value:''}});
  let rotA=els.pieceLayer.children.find(x=>x.dataset.id==='ROT-A'),rotB=els.pieceLayer.children.find(x=>x.dataset.id==='ROT-B');
  rotA.handlers.pointerdown({button:0,currentTarget:rotA,ctrlKey:true,metaKey:false});
  rotB=els.pieceLayer.children.find(x=>x.dataset.id==='ROT-B');
  rotB.handlers.pointerdown({button:0,currentTarget:rotB,ctrlKey:true,metaKey:false});
  if(!els.selectionText.textContent.startsWith('2 seleccionada'))throw Error('Multi-selection was not retained for rotation');
  els.rotateBtn.onclick();
  let rotatedTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  const rotatedA=rotatedTracks.find(p=>p.id==='ROT-A'),rotatedB=rotatedTracks.find(p=>p.id==='ROT-B'),rotatedC=rotatedTracks.find(p=>p.id==='ROT-C');
  if(rotatedA.rotation!==15||rotatedB.rotation!==15||rotatedC.rotation!==0)throw Error('Joint rotation did not apply the same angle only to selected pieces');
  if(Math.abs(Math.hypot(rotatedB.x-rotatedA.x,rotatedB.y-rotatedA.y)-180)>1e-8)throw Error('Joint rotation did not preserve the relative geometric offset');
  els.validateTopologyBtn.onclick();
  if(!els.topologyReport.textContent.includes('discontinuïtat geomètrica'))throw Error('Rotation did not diagnose the external connection affected by the group rotation');
  // One undo restores all selected pieces and their topology; one redo reapplies the whole rotation.
  els.undoBtn.onclick();
  rotatedTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  if(rotatedTracks.find(p=>p.id==='ROT-A').rotation!==0||rotatedTracks.find(p=>p.id==='ROT-B').x!==280)throw Error('One undo did not restore the complete group rotation');
  els.validateTopologyBtn.onclick();
  if(!els.topologyReport.textContent.includes('0 error(s)'))throw Error('Undo did not restore valid connected topology');
  els.redoBtn.onclick();
  rotatedTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  if(rotatedTracks.find(p=>p.id==='ROT-A').rotation!==15||rotatedTracks.find(p=>p.id==='ROT-B').rotation!==15)throw Error('One redo did not reapply the complete group rotation');
  els.validateTopologyBtn.onclick();
  if(!els.topologyReport.textContent.includes('discontinuïtat geomètrica'))throw Error('Redo did not restore the affected-connection diagnostic');
  // Quadrant II + 30° selects 120°; keyboard-entered 45° in quadrant IV selects 315°.
  els.rotationQuadrant.value='1';
  els.rotationPreset30.onclick();
  rotatedTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  if(rotatedTracks.find(p=>p.id==='ROT-A').rotation!==135)throw Error('Quadrant II 30-degree mark did not apply a 120-degree group rotation');
  els.rotationQuadrant.value='3';
  els.rotationCustomAngle.value='45';
  els.rotationCustomAngle.handlers.keydown({key:'Enter',preventDefault(){}});
  rotatedTracks=window.JMRI_TC_DISPATCHER.getConfig().levels[0].tracks;
  if(rotatedTracks.find(p=>p.id==='ROT-A').rotation!==90)throw Error('Keyboard-entered 45 degrees in quadrant IV did not apply 315 degrees');

  if(!selectionMove||typeof selectionMove.createDrag!=='function')throw Error('Selection movement core is not exposed to the smoke test');
  // JSON v3 round-trip contract: catalogue-assisted connection metadata is legal,
  // while references to ports that do not exist on the piece are rejected.
  const modelApi=window.JMRI_TC_LAYOUT_MODEL;
  if(!modelApi||typeof modelApi.validateConfig!=='function')throw Error('JSON v3 validator is not exposed');
  const v3Project={schemaVersion:3,name:'JSON v3 contract',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:'Nivell '+(i+1),elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]};
  const v3Piece=(id,x)=>({id,type:'straight',name:id,x,y:100,rotation:0,levelId:1,blockId:'',sensorName:'',geometry:{lengthMm:180}});
  v3Project.levels[0].tracks=[v3Piece('V3-A',100),v3Piece('V3-B',280)];
  v3Project.connections=[{id:'V3-C1',levelId:1,from:{pieceId:'V3-A',port:'B'},to:{pieceId:'V3-B',port:'A'},verified:false,method:'catalogue-gap-suggestion'}];
  const normalizedV3=modelApi.validateConfig(JSON.parse(JSON.stringify(v3Project)));
  if(normalizedV3.schemaVersion!==3||normalizedV3.connections[0].method!=='catalogue-gap-suggestion')throw Error('Valid v3 connection metadata was not preserved');
  const badPort=JSON.parse(JSON.stringify(v3Project));badPort.connections[0].from.port='Z';
  let rejectedBadPort=false;try{modelApi.validateConfig(badPort);}catch(error){rejectedBadPort=/port inexistent/.test(error.message);}
  if(!rejectedBadPort)throw Error('JSON v3 validator accepted a connection to an unknown port');
  const badRoom=JSON.parse(JSON.stringify(v3Project));badRoom.room.widthMm=2800;
  let rejectedBadRoom=false;try{modelApi.validateConfig(badRoom);}catch(error){rejectedBadRoom=/3000/.test(error.message);}
  if(!rejectedBadRoom)throw Error('JSON v3 validator accepted an unsupported board size');
  console.log('PASS: startup, five levels, viewport zoom/pan/fit, insertion, selection, SVG, rotation, editable geometry, duplicate, undo/redo, block/sensor registries, assignment, safe level reassignment, v2 migration, topology audit, duplicate/port-conflict detection, exact manual connection, keyboard activation, multi-selection movement/undo/cancel/bounds and rejection of self/distant/occupied ports');
})().catch(err=>{console.error(err);process.exitCode=1;});
