(() => {
'use strict';
const core=window.JMRI_TC_LocomotiveDmiCore;if(!core)return;
const $=id=>document.getElementById(id);
let maxKmh=160,setpoint=0,currentStep=0,direction=1,brakePercent=0,functions=new Set(),dragging=false;
const KEY_STORAGE='jmri-tc-dmi-keybindings-v1';
const KEY_ACTIONS=core.KEYBOARD_ACTIONS,defaultBindings=core.DEFAULT_KEY_BINDINGS;
let keyBindings=loadKeyBindings(),captureAction=null;
const ASPECTS=window.JMRI_TC_SignallingCore?.ASPECTS||{};
function traffic(){return window.JMRI_TC_TRAFFIC_CONTROL;}
function routeFor(snapshot){return snapshot?.activeRun?((snapshot.config?.dispatcher?.routes||[]).find(r=>r.id===snapshot.activeRun.routeId)||null):null;}
function activeLoco(snapshot){return snapshot?.locomotives?.find(l=>l.id===snapshot.activeRun?.locomotiveId)||snapshot?.locomotives?.find(l=>l.id===$('dmiLocomotive')?.value)||null;}
function loadKeyBindings(){try{return {...defaultBindings,...JSON.parse(localStorage.getItem(KEY_STORAGE)||'{}')};}catch{return {...defaultBindings};}}
function keyLabel(code){return core.keyboardLabel(code);}
function isEditableTarget(target){return !!target?.closest?.('input,textarea,select,[contenteditable="true"]');}
function renderKeyboardBindings(){
 const host=$('dmiKeyboardBindings');if(!host)return;host.replaceChildren();
 KEY_ACTIONS.forEach(action=>{const row=document.createElement('div');row.className='dmi-key-row';const label=document.createElement('label');label.textContent=action.label;const key=document.createElement('strong');key.textContent=captureAction===action.id?'Prem una tecla…':(keyLabel(keyBindings[action.id])||'Sense assignar');const button=document.createElement('button');button.type='button';button.textContent=captureAction===action.id?'Escoltant…':'Canviar';button.onclick=()=>{captureAction=action.id;renderKeyboardBindings();setStatus('Prem la tecla que vols assignar a '+action.label+'.');};const clear=document.createElement('button');clear.type='button';clear.textContent='×';clear.title='Eliminar assignació';clear.onclick=()=>{delete keyBindings[action.id];saveKeyBindings();renderKeyboardBindings();};row.append(label,key,button,clear);host.append(row);});
}
function saveKeyBindings(){try{localStorage.setItem(KEY_STORAGE,JSON.stringify(keyBindings));}catch{}}
function normalizedKey(event){return core.keyboardCode(event);}
function isDmiSafe(){const api=traffic();if(!api?.getSnapshot)return false;const s=api.getSnapshot(),r=s.activeRun;return !!r&&r.status==='RUNNING'&&r.mode==='MANUAL'&&s.bridgeConnected&&s.dccArmed&&s.deadmanHeld&&s.safetyReady;}
function toggleFunction(fn){const button=$('dmiFunctionButtons').querySelector('button[data-function="'+fn+'"]'),enabled=!functions.has(fn);if(traffic()?.setFunction?.(fn,enabled)){if(enabled)functions.add(fn);else functions.delete(fn);if(button){button.setAttribute('aria-pressed',String(enabled));button.classList.toggle('active',enabled);}}}
function handleKeyboard(event){
 if(captureAction){if(event.key==='Escape'){captureAction=null;renderKeyboardBindings();return;}if(['Tab','Shift','Control','Alt','Meta'].includes(event.key))return;event.preventDefault();const code=normalizedKey(event),conflict=core.keyboardConflict(keyBindings,captureAction,code);if(conflict){setStatus('Tecla ja assignada a '+conflict.label+'. Tria’n una altra.');return;}keyBindings[captureAction]=code;captureAction=null;saveKeyBindings();renderKeyboardBindings();setStatus('Assignació de teclat desada.');return;}
 if(event.repeat||isEditableTarget(event.target)||!$('locomotiveDmiPanel')||!isDmiSafe())return;
 const code=normalizedKey(event),action=core.keyboardAction(keyBindings,code);if(!action)return;event.preventDefault();
 if(action.id==='speedUp'){const step=Math.min(126,core.kmhToStep(setpoint,maxKmh)+1);setTarget(core.stepToKmh(step,maxKmh));}
 else if(action.id==='speedDown'){const step=Math.max(0,core.kmhToStep(setpoint,maxKmh)-1);setTarget(core.stepToKmh(step,maxKmh));}
 else if(action.id==='presetUp')setTarget(setpoint+5);
 else if(action.id==='presetDown')setTarget(setpoint-5);
 else if(action.id==='brakeUp')setBrake(brakePercent+5);
 else if(action.id==='brakeDown')setBrake(brakePercent-5);
 else if(action.id.startsWith('function'))toggleFunction(Number(action.id.slice(8)));
}
function setStatus(message){const node=$('dmiCommandStatus');if(node)node.textContent=message;}
function setDial(value){setpoint=core.clamp(value,0,maxKmh);$('dmiSetpointValue').textContent=Math.round(setpoint);$('dmiSetpointDial').setAttribute('aria-valuenow',String(Math.round(setpoint)));$('dmiSetpointDial').setAttribute('aria-valuetext',Math.round(setpoint)+' km/h');const angle=core.needleAngle(setpoint,maxKmh);$('dmiSetpointNeedle').setAttribute('transform','rotate('+angle+' 90 90)');$('dmiSetpointArc').style.strokeDasharray=(setpoint/maxKmh*240)+' 240';}
function renderSpeed(){
 const kmh=core.stepToKmh(currentStep,maxKmh),angle=core.needleAngle(kmh,maxKmh);
 $('dmiSpeedValue').textContent=String(Math.round(kmh));$('dmiSpeedScale').textContent='0 — '+maxKmh+' km/h';
 $('dmiNeedle').setAttribute('transform','rotate('+angle+' 160 150)');
 $('dmiLinearSpeed').max=String(maxKmh);$('dmiLinearSpeed').value=String(Math.round(setpoint));$('dmiLinearValue').textContent=Math.round(setpoint)+' km/h';
 setDial(setpoint);
}
function sendTractionCommand(){
 const effectiveKmh=setpoint*(1-brakePercent/100),step=core.kmhToStep(effectiveKmh,maxKmh),api=traffic();
 if(!api?.setSpeed){setStatus('Comandament no disponible: cal una circulació manual activa.');return false;}
 if(api.setSpeed(step)){currentStep=step;renderSpeed();setStatus('Tracció acceptada: '+Math.round(effectiveKmh)+' km/h equivalents · fre '+brakePercent+'%.');return true;}
 setStatus('Ordre no enviada: comprova circulació, home mort i interbloqueigs.');return false;
}
function setTarget(value,send=true){
 setpoint=core.clamp(value,0,maxKmh);$('dmiLinearSpeed').value=String(Math.round(setpoint));setDial(setpoint);
 if(send)sendTractionCommand();
}
function setBrake(value,send=true){
 brakePercent=core.clamp(value,0,100);
 $('dmiBrakeSlider').value=String(brakePercent);
 $('dmiBrakeValue').textContent=brakePercent===0?'Alliberat · 0%':brakePercent===100?'Màxim · 100%':'Frenada · '+brakePercent+'%';
 if(send)sendTractionCommand();
}
function dialValue(event){
 const rect=$('dmiSetpointDial').getBoundingClientRect(),x=event.clientX-(rect.left+rect.width/2),y=event.clientY-(rect.top+rect.height/2);
 let angle=Math.atan2(y,x)*180/Math.PI+90;if(angle<0)angle+=360;
 angle=core.clamp(angle,0,270);return angle/270*maxKmh;
}
function updateLocomotives(snapshot){
 const select=$('dmiLocomotive'),previous=select.value;select.replaceChildren();
 (snapshot?.locomotives||[]).forEach(l=>{const option=document.createElement('option');option.value=l.id;option.textContent=l.name+' · '+l.wiThrottleId;select.append(option);});
 const active=snapshot?.activeRun?.locomotiveId;
 if(active)select.value=active;else if((snapshot?.locomotives||[]).some(l=>l.id===previous))select.value=previous;
 select.disabled=!!active;
}
function renderSignals(snapshot){
 const host=$('dmiSignals');host.replaceChildren();const route=routeFor(snapshot),chain=core.signalChain(snapshot?.config?.signals,route);
 if(!chain.length){const p=document.createElement('p');p.className='dmi-muted';p.textContent='Sense senyals vinculades a l’itinerari actiu.';host.append(p);return;}
 chain.forEach((signal,index)=>{
  const card=document.createElement('article');card.className='dmi-signal-card';
  const title=document.createElement('strong');title.textContent=(index?'Següent senyal':'Senyal immediata')+' · '+(signal.name||signal.id);
  const type=document.createElement('span');type.textContent=(window.JMRI_TC_SignallingCore?.TYPES?.[signal.type]||signal.type||'Tipus no definit');
  const aspect=document.createElement('span');aspect.className='dmi-aspect';aspect.dataset.aspect=signal.computedAspect||signal.aspect||'PARADA';aspect.textContent=core.normalizeAspect(signal,ASPECTS);
  const distance=document.createElement('span');distance.className='dmi-distance';
  let meters=null;try{meters=window.JMRI_TC_TRAIN_POSITION?.getDistanceToSignal?.(signal.id);}catch{}
  distance.textContent=core.distanceLabel(meters);
  card.append(title,type,aspect,distance);host.append(card);
 });
}
function renderTurnouts(snapshot){
 const host=$('dmiTurnouts');host.replaceChildren();const route=routeFor(snapshot);
 if(!route){host.textContent='Sense itinerari actiu.';return;}
 const c=snapshot.config||{},telemetry=window.JMRI_TC_BOARD?.getTelemetry?.()||{},accessories=window.JMRI_TC_DIGITAL_CATALOGUE?.getDatabase?.().accessories||[];
 const supervisor=window.JMRI_TC_TURNOUT_SUPERVISION;
 const requirements=supervisor?.inspectRoute?.(route)?.requirements||window.JMRI_TC_TurnoutSupervisionCore?.routeRequirements(route,c)||[];
 if(!requirements.length){host.textContent='L’itinerari no declara desviaments.';return;}
 requirements.forEach(req=>{const row=document.createElement('div');row.className='dmi-turnout-row';const name=document.createElement('strong');name.textContent=req.systemName;const state=document.createElement('span');state.textContent=core.turnoutLabel(telemetry.turnouts?.[req.systemName]);const expected=document.createElement('small');expected.textContent='Requerida: '+core.turnoutLabel(req.requiredState);row.append(name,state,expected);host.append(row);});
}
function refresh(){
 const api=traffic();if(!api)return;const snapshot=api.getSnapshot();updateLocomotives(snapshot);
 const loco=activeLoco(snapshot);$('dmiLocoName').textContent=loco?.name||'Cap locomotora seleccionada';
 const run=snapshot.activeRun;
 const safe=!!run&&run.status==='RUNNING'&&run.mode==='MANUAL'&&snapshot.bridgeConnected&&snapshot.dccArmed&&snapshot.deadmanHeld&&snapshot.safetyReady;
 $('dmiSafetyState').textContent=safe?'CONDUCCIÓ SUPERVISADA':'CONTROL BLOQUEJAT · '+(run?'verifica DCC, home mort i interbloqueigs':'inicia una circulació manual');
 $('dmiSafetyState').className=safe?'dmi-safety ready':'dmi-safety locked';
 const controls=$('dmiFunctionButtons');if(controls)controls.querySelectorAll('button').forEach(b=>b.disabled=!safe);
 $('dmiDirection').disabled=!safe;$('dmiLinearSpeed').disabled=!safe;$('dmiSetpointDial').setAttribute('aria-disabled',String(!safe));$('dmiSpeedMinus').disabled=!safe;$('dmiSpeedPlus').disabled=!safe;$('dmiBrakeSlider').disabled=!safe;$('dmiEmergency').disabled=!run||!snapshot.bridgeConnected;
 renderSignals(snapshot);renderTurnouts(snapshot);
}
function init(){
 if(!$('locomotiveDmiPanel'))return;
 renderKeyboardBindings();document.addEventListener('keydown',handleKeyboard);
 $('dmiMaxSpeed').value=String(maxKmh);
 $('dmiApplyMax').onclick=()=>{const result=core.validateCalibration($('dmiMaxSpeed').value);if(!result.ok){setStatus(result.error);return;}maxKmh=result.maxKmh;setpoint=core.clamp(setpoint,0,maxKmh);currentStep=core.kmhToStep(currentStep/126*maxKmh,maxKmh);renderSpeed();setStatus('Escala DMI configurada a '+maxKmh+' km/h. Ajusta-la amb dades de calibratge reals de la locomotora.');};
 $('dmiLinearSpeed').oninput=e=>setTarget(e.target.value);
 $('dmiSpeedMinus').onclick=()=>{const step=Math.max(0,core.kmhToStep(setpoint,maxKmh)-1);setTarget(core.stepToKmh(step,maxKmh));};
 $('dmiSpeedPlus').onclick=()=>{const step=Math.min(126,core.kmhToStep(setpoint,maxKmh)+1);setTarget(core.stepToKmh(step,maxKmh));};
 $('dmiBrakeSlider').oninput=e=>setBrake(e.target.value);
 $('dmiEmergency').onclick=()=>{const api=traffic();if(api?.emergencyStop?.())setStatus('EMERGÈNCIA sol·licitada. Comprova físicament que el tren s’ha aturat.');else setStatus('No s’ha pogut enviar l’ordre d’emergència. Actua sobre la parada física independent.');};
 $('dmiDirection').onclick=()=>{direction=direction===1?0:1;const api=traffic();const ok=api?.setDirection?.(direction);if(ok){$('dmiDirection').textContent=direction===1?'▶ ENDAVANT':'◀ ENRERE';setStatus('Canvi de sentit sol·licitat.');}};
 $('dmiSetpointDial').addEventListener('pointerdown',e=>{if($('dmiSetpointDial').getAttribute('aria-disabled')==='true')return;dragging=true;$('dmiSetpointDial').setPointerCapture?.(e.pointerId);setTarget(dialValue(e));});
 $('dmiSetpointDial').addEventListener('pointermove',e=>{if(dragging)setTarget(dialValue(e));});
 $('dmiSetpointDial').addEventListener('pointerup',()=>{dragging=false;});
 $('dmiSetpointDial').addEventListener('pointercancel',()=>{dragging=false;});
 $('dmiSetpointDial').addEventListener('keydown',e=>{if(e.key==='ArrowUp'||e.key==='ArrowRight'){e.preventDefault();setTarget(setpoint+1);}else if(e.key==='ArrowDown'||e.key==='ArrowLeft'){e.preventDefault();setTarget(setpoint-1);}else if(e.key==='Home'){e.preventDefault();setTarget(0);}});
 $('dmiFunctionButtons').addEventListener('click',e=>{const button=e.target.closest('button[data-function]');if(button)toggleFunction(Number(button.dataset.function));});
 $('dmiLocomotive').onchange=()=>{const source=$('trafficLoco');if(source)source.value=$('dmiLocomotive').value;};
 renderSpeed();refresh();setInterval(refresh,1000);
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
window.JMRI_TC_LOCOMOTIVE_DMI={refresh,setTarget,getCalibration:()=>({maxKmh}),getKeyBindings:()=>({...keyBindings})};
})();