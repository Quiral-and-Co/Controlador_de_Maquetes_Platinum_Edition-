(() => {
'use strict';
const core=window.JMRI_TC_TurnoutSupervisionCore;
if(!core)return;
const $=id=>document.getElementById(id);
function config(){return window.JMRI_TC_DISPATCHER?.getConfig?.()||{};}
function telemetry(){return window.JMRI_TC_BOARD?.getTelemetry?.()||{};}
function accessories(){return window.JMRI_TC_DIGITAL_CATALOGUE?.getDatabase?.().accessories||[];}
function snapshot(){return window.JMRI_TC_DISPATCHER_UI?.getSnapshot?.()||{routes:[],reservations:{},blockStates:{}};}
function ensurePanel(){
  const host=document.querySelector('.dispatcher-diagnostics');
  if(!host||$('turnoutSupervisionPanel'))return;
  const section=document.createElement('section');section.id='turnoutSupervisionPanel';section.className='turnout-supervision';
  const heading=document.createElement('h3');heading.textContent='Supervisió de desviaments i itineraris';
  const metrics=document.createElement('div');metrics.className='turnout-supervision-metrics';
  for(const [id,label] of [['turnoutSupervisionAligned','Alineats'],['turnoutSupervisionBlocked','Bloquejats'],['turnoutSupervisionUnknown','Desconeguts']]){
    const span=document.createElement('span');span.textContent=label+': ';const strong=document.createElement('strong');strong.id=id;strong.textContent='0';span.append(strong);metrics.append(span);
  }
  const list=document.createElement('ul');list.id='turnoutSupervisionList';list.className='dispatcher-diagnostic-list';
  section.append(heading,metrics,list);host.append(section);
}
function refresh(){
  ensurePanel();
  const c=config(),t=telemetry(),a=accessories(),s=snapshot(),routes=s.routes||[],now=Date.now();
  let aligned=0,blocked=0,unknown=0;const incidents=[];
  for(const route of routes){
    const result=core.inspectRoute(route,c,t,a,now);
    if(result.status==='NOT_REQUIRED')continue;
    if(result.ok)aligned++;else{blocked++;unknown+=result.status==='UNKNOWN'?1:0;for(const incident of result.incidents)incidents.push({route:route.name,...incident});}
  }
  if($('turnoutSupervisionAligned'))$('turnoutSupervisionAligned').textContent=String(aligned);
  if($('turnoutSupervisionBlocked'))$('turnoutSupervisionBlocked').textContent=String(blocked);
  if($('turnoutSupervisionUnknown'))$('turnoutSupervisionUnknown').textContent=String(unknown);
  const list=$('turnoutSupervisionList');
  if(list){list.replaceChildren();if(!incidents.length){const item=document.createElement('li');item.textContent='Sense incidències de desviaments requerits.';list.append(item);}for(const incident of incidents){const item=document.createElement('li');item.className='incident-error';item.textContent=incident.route+': '+incident.message;list.append(item);}}
  const activeRouteIds=new Set(Object.values(s.reservations||{}));
  const activeRoutes=routes.filter(route=>activeRouteIds.has(route.id));
  document.querySelectorAll('#trackSvg [data-turnout]').forEach(node=>{
    const name=node.getAttribute('data-turnout');
    const candidates=(activeRoutes.length?activeRoutes:routes).filter(route=>core.routeRequirements(route,c).some(req=>req.systemName===name));
    const expectedStates=new Set(candidates.flatMap(route=>core.routeRequirements(route,c).filter(req=>req.systemName===name).map(req=>req.requiredState)));
    const relevant=expectedStates.size===1?candidates.map(route=>({route,result:core.inspectRoute(route,c,t,a,now)})):[];
    node.classList.remove('supervision-aligned','supervision-misaligned','supervision-unknown');
    if(!relevant.length){node.dataset.supervision=candidates.length?'AMBIGUOUS':'UNCOMMITTED';return;}
    const failed=relevant.flatMap(x=>x.result.incidents);
    const status=failed.some(x=>x.code==='TURNOUT_MISALIGNED')?'MISALIGNED':failed.length?'UNKNOWN':'ALIGNED';
    node.dataset.supervision=status;node.classList.add('supervision-'+status.toLowerCase());
    if(!node.dataset.supervisionBaseLabel)node.dataset.supervisionBaseLabel=node.getAttribute('aria-label')||name;
    node.setAttribute('aria-label',core.supervisionAriaLabel(node.dataset.supervisionBaseLabel,status));
  });
  return{aligned,blocked,unknown,incidents};
}
let timer=null;
function install(){
  if(timer)return;
  const dispatcher=window.JMRI_TC_DISPATCHER_UI;
  if(dispatcher?.refresh){const original=dispatcher.refresh;dispatcher.refresh=function(...args){const result=original.apply(this,args);refresh();return result;};}
  refresh();timer=setInterval(refresh,1000);
}
window.JMRI_TC_TURNOUT_SUPERVISION={refresh,stop(){if(timer)clearInterval(timer);timer=null;},inspectRoute(route){return core.inspectRoute(route,config(),telemetry(),accessories(),Date.now());}};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();
