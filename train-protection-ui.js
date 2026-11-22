(()=>{'use strict';
const p=window.JMRI_TC_TrainProtection;if(!p)return;let session=null;
const $=id=>document.getElementById(id);
function status(message){$('tpStatus').textContent=message;}
function render(){
 if(!session)return;
 const snap=p.snapshot(session);
 $('tpState').textContent=snap.state;$('tpState').className='tp-state '+(snap.state==='SUPERVISION'?'ready':snap.state==='TRIPPED'?'tripped':'warning');
 $('tpSpeedReadout').textContent=snap.speedKmh+' km/h';
 $('tpPermittedReadout').textContent=snap.permittedKmh+' km/h';
 $('tpTargetReadout').textContent=snap.targetKmh+' km/h';
 $('tpDistanceReadout').textContent=Math.round(snap.targetDistanceM)+' m';
 $('tpAck').disabled=!snap.ackPending||snap.emergency;
 $('tpAck').textContent=snap.ackPending?'RECONÈIXER AVÍS · '+snap.ackSecondsRemaining+' s':'RECONÈIXER';
 $('tpTick').disabled=snap.emergency;$('tpEvent').textContent=snap.lastEvent;
 $('tpEvents').replaceChildren();snap.events.slice().reverse().forEach(e=>{const li=document.createElement('li');li.textContent=e.second+' s · '+e.message;$('tpEvents').append(li);});
}
function init(){
 if(!$('trainProtectionPanel'))return;
 function levels(){const system=$('tpSystem').value;const select=$('tpLevel');select.replaceChildren();p.SYSTEMS[system].levels.forEach(level=>{const o=document.createElement('option');o.value=level;o.textContent=level;select.append(o);});select.disabled=system!=='ETCS';}
 $('tpSystem').onchange=levels;levels();
 $('tpStart').onclick=()=>{const result=p.createSession({system:$('tpSystem').value,level:$('tpLevel').value,permittedKmh:$('tpPermitted').value,targetKmh:$('tpTarget').value,targetDistanceM:$('tpDistance').value});if(!result.ok){status(result.error);return;}session=result.session;status('Sessió de protecció manual en mode exclusivament simulat.');render();};
 $('tpSpeed').oninput=()=>{if(!session)return;$('tpSpeedInput').textContent=$('tpSpeed').value+' km/h';};
 $('tpSetSpeed').onclick=()=>{if(!session)return;const r=p.setSpeed(session,$('tpSpeed').value);if(r.ok)session=r.session;else status(r.error);render();};
 $('tpSetTarget').onclick=()=>{if(!session)return;const r=p.setTarget(session,{permittedKmh:$('tpPermitted').value,targetKmh:$('tpTarget').value,targetDistanceM:$('tpDistance').value});if(r.ok)session=r.session;else status(r.error);render();};
 $('tpWarning').onclick=()=>{if(!session)return;const r=p.requestAcknowledgement(session,$('tpReason').value,Number($('tpAckWindow').value));if(r.ok)session=r.session;else status(r.error);render();};
 $('tpAck').onclick=()=>{const r=p.acknowledge(session);if(r.ok)session=r.session;else status(r.error);render();};
 $('tpTick').onclick=()=>{const r=p.tick(session,1);if(r.ok)session=r.session;else status(r.error);render();};
 $('tpEmergency').onclick=()=>{if(!session)return;session=p.trip(session,'polsador d’emergència del panell').session;render();};
 $('tpReset').onclick=()=>{if(!session)return;if(session.emergency&&!window.confirm('Confirmes el reinici de la simulació? No rearma cap sortida física.'))return;const r=p.reset(session);if(r.ok)session=r.session;render();};
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();
