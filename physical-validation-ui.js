(() => {
'use strict';
const root=window,core=root.JMRI_TC_PhysicalValidationCore,topology=root.JMRI_TC_SignallingTopologyCore;
if(!core)return;
const $=id=>document.getElementById(id);
let selectedSignalId=null,installed=false,timer=null,lastFingerprint='';
function config(){return root.JMRI_TC_DISPATCHER?.getConfig?.()||{};}
function topologyResults(c){return topology?topology.inspect(c,c.signals||[],root.JMRI_TC_DISPATCHER_UI?.getSnapshot?.()||{}):{};}
function selectSignal(id){selectedSignalId=id;refresh();}
function renderLevelDiagnostics(c,t){
 const host=$('signalLevelDiagnostics');if(!host)return;host.replaceChildren();
 core.levelDiagnostics(c,t).forEach(level=>{
  const card=document.createElement('article');card.className='signal-level-diagnostic';
  const title=document.createElement('strong');title.textContent=level.levelName+' · '+level.entities+' elements';card.append(title);
  const summary=document.createElement('p');summary.textContent='Senyals vinculades: '+level.signals+' · incidències topològiques: '+level.topologyIssues+' · pendents programari: '+level.pending+' · llestes per validació física: '+level.softwareReady+' · verificades: '+level.physicalReady+' · fallades: '+level.failed;card.append(summary);
  host.append(card);
 });
}
function renderMatrix(c){
 const host=$('physicalValidationMatrix');if(!host)return;host.replaceChildren();
 const result=core.inspect(c),summary=document.createElement('p');summary.className='validation-matrix-summary';
 summary.textContent='Elements: '+result.counts.total+' · pendents programari: '+result.counts.pendingSoftware+' · preparats per inspecció física: '+result.counts.softwareReady+' · verificació física completa: '+result.counts.physicalReady+' · incidències: '+result.counts.failed;host.append(summary);
 [['signal','Senyals'],['block','Blocs'],['sensor','Sensors'],['turnout','Desviaments']].forEach(([type,label])=>{
  const entries=result.entries.filter(e=>e.type===type);if(!entries.length)return;
  const section=document.createElement('section');section.className='validation-group';
  const heading=document.createElement('h4');heading.textContent=label+' ('+entries.length+')';section.append(heading);
  entries.forEach(entry=>{
   const card=document.createElement('article');card.className='validation-entry '+entry.status.toLowerCase().replaceAll('_','-');
   const title=document.createElement('strong');title.textContent=entry.name+' · '+entry.id+' · '+entry.status;card.append(title);
   if(entry.levelName){const meta=document.createElement('p');meta.textContent=entry.levelName;card.append(meta);}
   entry.checks.forEach(check=>{
    const row=document.createElement('label');row.className='validation-check';
    const input=document.createElement('input');input.type='checkbox';input.checked=check.checked;input.addEventListener('change',()=>{core.setCheck(c,entry.key,check.id,input.checked);refresh();});
    const text=document.createElement('span');text.textContent=(check.kind==='software'?'PROGRAMARI · ':'FÍSIC · ')+check.label;row.append(input,text);card.append(row);
   });
   const failure=document.createElement('label');failure.className='validation-failure';
   const failInput=document.createElement('input');failInput.type='checkbox';failInput.checked=entry.failed;
   failInput.addEventListener('change',()=>{core.setFailure(c,entry.key,failInput.checked,failInput.checked?'Incidència marcada per l’operador':'');refresh();});
   failure.append(failInput,document.createTextNode('Incidència / no conforme'));card.append(failure);
   if(entry.failed&&entry.failureReason){const reason=document.createElement('p');reason.className='validation-error';reason.textContent=entry.failureReason;card.append(reason);}
   section.append(card);
  });
  host.append(section);
 });
}
function highlight(c,t){
 const svg=$('trackSvg');if(!svg)return;
 svg.querySelectorAll('.track-piece').forEach(n=>n.classList.remove('signal-related-highlight'));
 svg.querySelectorAll('[data-signal-id]').forEach(n=>n.classList.remove('signal-selected-highlight'));
 if(!selectedSignalId)return;
 const result=t[selectedSignalId];if(!result)return;
 const ids=new Set([...(result.trackIds||[]),...(result.protectedTrackIds||[])].map(String));
 ids.forEach(id=>svg.querySelectorAll('.track-piece').forEach(n=>{if(n.dataset.id===id)n.classList.add('signal-related-highlight');}));
 svg.querySelectorAll('[data-signal-id]').forEach(n=>{if(n.dataset.signalId===selectedSignalId)n.classList.add('signal-selected-highlight');});
}
function renderSelection(c,t){
 const host=$('selectedSignalRelations');if(!host)return;host.replaceChildren();
 const signal=(c.signals||[]).find(s=>s.id===selectedSignalId);
 if(!signal){host.textContent='Selecciona una senyal al registre per ressaltar les seves dependències.';return;}
 const r=t[selectedSignalId];if(!r){host.textContent='No s’han pogut calcular les dependències d’aquesta senyal.';return;}
 const p=document.createElement('p');p.textContent=signal.name+' · Bloc: '+(r.blockName||r.blockId||'—')+' · Itinerari: '+(r.routeName||r.routeId||'—');host.append(p);
 const details=document.createElement('p');details.textContent='Blocs: '+(r.routeBlockIds||[]).join(' → ')+' · Peces: '+(r.trackIds||[]).join(', ')+' · Desviaments: '+(r.turnoutNames||[]).join(', ');host.append(details);
}
function refresh(){const c=config(),t=topologyResults(c),fingerprint=JSON.stringify({selectedSignalId,signals:c.signals||[],levels:(c.levels||[]).map(l=>({id:l.id,blocks:l.blocks||[],sensors:l.sensors||[],tracks:(l.tracks||[]).filter(p=>p.type==='turnout'||p.blockId)})),matrix:c.validationMatrix||{},topology:t});if(fingerprint!==lastFingerprint){lastFingerprint=fingerprint;renderLevelDiagnostics(c,t);renderMatrix(c);renderSelection(c,t);highlight(c,t);}return{levels:core.levelDiagnostics(c,t),matrix:core.inspect(c)};}
function install(){
 if(installed||!$('signalRegistry'))return;installed=true;
 $('signalRegistry').addEventListener('click',e=>{const row=e.target.closest('button.signal-row');if(!row)return;const id=row.dataset.signalId;if(id)selectSignal(id);});
 refresh();timer=setInterval(refresh,1000);
}
root.JMRI_TC_PHYSICAL_VALIDATION_UI={refresh,selectSignal,stop(){if(timer)clearInterval(timer);timer=null;}};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();
