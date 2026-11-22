(() => {
'use strict';
const core=window.JMRI_TC_JmriDiagnosticsCore;
if(!core)return;
const $=id=>document.getElementById(id);
function refresh(){
  const config=window.JMRI_TC_DISPATCHER?.getConfig?.()||{};
  const telemetry=window.JMRI_TC_BOARD?.getTelemetry?.()||{};
  const snapshot=window.JMRI_TC_DISPATCHER_UI?.getSnapshot?.()||{routes:[],blockStates:{}};
  const accessories=window.JMRI_TC_DIGITAL_CATALOGUE?.getDatabase?.().accessories||[];
  const result=core.inspect(config,telemetry,snapshot,Date.now(),undefined,accessories);
  const valid=$('jmriDiagnosticValid'),count=$('jmriDiagnosticIncidents'),unknown=$('jmriDiagnosticUnknown'),list=$('jmriDiagnosticList');
  if(!valid||!count||!unknown||!list)return result;
  valid.textContent=String(result.validSensorCount);
  count.textContent=String(result.incidentCount);
  unknown.textContent=String(result.unknownBlocks);
  list.replaceChildren();
  if(!result.incidents.length){const item=document.createElement('li');item.textContent='Sense incidències de lectura detectades.';list.append(item);}
  result.incidents.forEach(incident=>{const item=document.createElement('li');item.className=incident.severity==='error'?'incident-error':'incident-warning';item.textContent=incident.message;list.append(item);});
  return result;
}
let timer=null;
function install(){
  if(timer)return;
  const dispatcher=window.JMRI_TC_DISPATCHER_UI;
  if(dispatcher?.refresh){const original=dispatcher.refresh;dispatcher.refresh=function(...args){const result=original.apply(this,args);refresh();return result;};}
  refresh();
  timer=setInterval(refresh,1000);
}
window.JMRI_TC_JMRI_DIAGNOSTICS={refresh,stop(){if(timer)clearInterval(timer);timer=null;}};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();
