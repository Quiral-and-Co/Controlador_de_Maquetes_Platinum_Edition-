(() => {
'use strict';
const game=window.JMRI_TC_Spielewelt;if(!game)return;
const $=id=>document.getElementById(id);let session=null;
function traffic(){return window.JMRI_TC_TRAFFIC_CONTROL;}
function locomotives(){const live=traffic()?.getSnapshot?.()?.locomotives||[];const catalogue=window.JMRI_TC_DIGITAL_CATALOGUE?.getDatabase?.()?.locomotives||[];const byId=new Map();[...catalogue,...live].forEach(l=>{if(l?.id!==undefined&&l?.id!==null)byId.set(String(l.id),l);});return [...byId.values()];}
function renderLocomotives(){
 const select=$('spieleweltLocomotive'),previous=select.value;select.replaceChildren();
 locomotives().forEach(l=>{const o=document.createElement('option');o.value=l.id;o.textContent=l.name+' · '+(l.decoderProtocol||l.protocol||'protocol no indicat');select.append(o);});
 if(locomotives().some(l=>String(l.id)===previous))select.value=previous;
}
function selectedLoco(){return locomotives().find(l=>String(l.id)===$('spieleweltLocomotive').value)||null;}
function status(message){$('spieleweltStatus').textContent=message;}
function render(){
 if(!session)return;
 const profile=game.PROFILES[session.profile],caps=session.capabilities;
 $('spieleweltCapability').textContent=caps.nativeMfxPlus?'mfx+ detectat · simulació local':'Descodificador '+caps.protocol.toUpperCase()+' · cabina virtual';
 $('spieleweltCapability').className='dmi-safety '+(caps.nativeMfxPlus?'ready':'locked');
 const instruments=$('spieleweltInstruments');instruments.replaceChildren();
 profile.instruments.forEach(label=>{const card=document.createElement('div');card.className='spielewelt-instrument';const strong=document.createElement('strong');strong.textContent=label;const value=document.createElement('p');value.textContent=label==='Velocímetre'?Math.round(session.throttle*1.6)+' km/h virtuals':'Instrument virtual';card.append(strong,value);instruments.append(card);});
 const resources=$('spieleweltResources');resources.replaceChildren();
 game.resourceStatus(session).forEach(item=>{const card=document.createElement('div');card.className='spielewelt-resource';const label=document.createElement('strong');label.textContent=item.label+' · '+Math.round(item.value)+'%';const bar=document.createElement('progress');bar.max=100;bar.value=item.value;bar.setAttribute('aria-label',item.label);card.append(label,bar);resources.append(card);});
 $('spieleweltAdvance').disabled=!session.active||![game.MODES.WITH_CONSUMPTION,game.MODES.DEPOT_REFUEL].includes(session.mode);
 $('spieleweltRefill').disabled=session.mode!==game.MODES.DEPOT_REFUEL;
 $('spieleweltThrottle').value=String(session.throttle);$('spieleweltThrottleValue').textContent=Math.round(session.throttle)+'%';
 status(session.lastEvent+' Temps virtual: '+session.elapsedMinutes+' min. '+(caps.consumptionSource==='software-simulation'?'Consum: simulació del TC Gold Custom.':'Capacitat mfx+ identificada; aquest mòdul continua sense escriure al descodificador.'));
}
function init(){
 if(!$('spieleweltPanel'))return;
 renderLocomotives();
 $('spieleweltStart').onclick=()=>{
  const loco=selectedLoco();if(!loco){status('No hi ha cap locomotora al registre de circulacions.');return;}
  session=game.createSession(loco,{profile:$('spieleweltProfile').value,mode:$('spieleweltMode').value});
  render();
 };
 $('spieleweltMode').onchange=()=>{if(!session)return;const result=game.setMode(session,$('spieleweltMode').value);if(result.ok){session=result.session;render();}};
 $('spieleweltProfile').onchange=()=>{if(!session)return;const loco=selectedLoco();session=game.createSession(loco,{profile:$('spieleweltProfile').value,mode:$('spieleweltMode').value});render();};
 $('spieleweltThrottle').oninput=e=>{if(!session)return;session=game.setThrottle(session,e.target.value);render();};
 $('spieleweltAdvance').onclick=()=>{const result=game.advance(session,10);if(result.ok){session=result.session;render();}else status(result.error);};
 $('spieleweltRefill').onclick=()=>{const result=game.refill(session,{atDepot:window.confirm('Confirma que la locomotora és al dipòsit de màquines? Aquesta acció només modifica la simulació local.')});if(result.ok){session=result.session;render();}else status(result.error);};
 const observer=()=>{renderLocomotives();if(session){const current=locomotives().find(l=>String(l.id)===session.locomotiveId);if(current){session.capabilities=game.capabilities(current);render();}}};
 window.addEventListener('jmri-tc-traffic-updated',observer);
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();
