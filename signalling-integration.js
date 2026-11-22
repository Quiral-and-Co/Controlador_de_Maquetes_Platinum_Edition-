(() => {
'use strict';
const root=window,core=root.JMRI_TC_SignallingCore,bridge=root.JMRI_TC_SignallingIntegrationCore;
if(!core||!bridge)return;
const NS='http://www.w3.org/2000/svg',topology=root.JMRI_TC_SignallingTopologyCore;
const $=id=>document.getElementById(id);
let installed=false,timer=null;
function config(){return root.JMRI_TC_DISPATCHER?.getConfig?.()||{};}
function snapshot(){return root.JMRI_TC_DISPATCHER_UI?.getSnapshot?.()||{blocks:[],blockStates:{},reservations:{},routes:[]};}
function telemetry(){return root.JMRI_TC_BOARD?.getTelemetry?.()||{};}
function emergency(){return !!root.JMRI_TC_SAFETY?.state?.fault;}
function compute(){
 const c=config(),signals=Array.isArray(c.signals)?c.signals:[];
 const result=bridge.evaluateAll({config:c,signals,snapshot:snapshot(),telemetry:telemetry(),signallingCore:core,emergency:emergency()});
 const topologyResults=topology?topology.inspect(c,signals,snapshot()):{};
 signals.forEach(s=>{const r=result[s.id]||{aspect:'PARADA',reason:'Sense resultat de càlcul.'},t=topologyResults[s.id];s.computedAspect=t&&!t.ok?'PARADA':r.aspect;s.computedReason=t&&!t.ok?'Vinculació topològica incompleta: '+t.issues.join(' '):r.reason;s.computedAt=r.computedAt;});
 return result;
}
function anchor(signal,c){
 const levels=c.levels||[];
 for(const l of levels){
  const block=(l.blocks||[]).find(b=>b.id===signal.protectedBlockId);
  if(!block)continue;
  const tracks=(l.tracks||[]).filter(p=>p.blockId===block.id);
  const p=tracks.find(t=>String(t.id)===String(signal.trackId))||tracks[0];if(!p)continue;
  const length=Math.max(20,Number(p.geometry?.lengthMm)||180);
  return{levelId:l.id,x:Number(p.x)+length/2,y:Number(p.y)-42};
 }
 if(Number.isFinite(Number(signal.x))&&Number.isFinite(Number(signal.y)))return{levelId:signal.levelId??1,x:Number(signal.x),y:Number(signal.y)};
 return null;
}
function renderSvg(){
 const c=config(),svg=$('trackSvg');if(!svg)return;
 let layer=$('signalLayer');if(!layer){layer=document.createElementNS(NS,'g');layer.setAttribute('id','signalLayer');svg.append(layer);}
 layer.replaceChildren();
 const signals=Array.isArray(c.signals)?c.signals:[];
 for(const s of signals){
  const pos=anchor(s,c);if(!pos||Number(pos.levelId)!==Number(root.JMRI_TC_BOARD?.getSelectedLevel?.()??1))continue;
  const aspect=s.computedAspect||'PARADA',meta=core.ASPECTS[aspect]||{label:'Sense aspecte',color:'red'};
  const g=document.createElementNS(NS,'g');g.setAttribute('class','signal-marker signal-'+String(aspect).toLowerCase());g.setAttribute('transform',`translate(${pos.x} ${pos.y})`);g.setAttribute('role','img');g.setAttribute('aria-label',s.name+' · '+meta.label);
  const stem=document.createElementNS(NS,'path');stem.setAttribute('d','M0 8 V34');stem.setAttribute('stroke','#d6dee8');stem.setAttribute('stroke-width','4');g.append(stem);
  const lamp=document.createElementNS(NS,'circle');lamp.setAttribute('cx','0');lamp.setAttribute('cy','0');lamp.setAttribute('r','13');lamp.setAttribute('fill',({green:'#20b96b',yellow:'#f2c94c',red:'#e5484d',white:'#f5f7fa'}[meta.color]||'#7b8794'));lamp.setAttribute('stroke','#101820');lamp.setAttribute('stroke-width','4');g.append(lamp);
  const label=document.createElementNS(NS,'text');label.setAttribute('x','18');label.setAttribute('y','5');label.setAttribute('fill','#f5f7fa');label.setAttribute('font-size','22');label.textContent=s.id+' · '+meta.label;g.append(label);
  layer.append(g);
 }
}
function refresh(){
 const results=compute();renderSvg();
 const selected=$('signalId')?.value,signal=(config().signals||[]).find(s=>s.id===selected);
 if(signal&&$('signalCurrentAspect')){$('signalCurrentAspect').textContent=core.ASPECTS[signal.computedAspect||'PARADA']?.label||'Parada';$('signalCurrentAspect').dataset.aspect=signal.computedAspect||'PARADA';}
 if(signal&&$('signalMessage')&&signal.computedReason)$('signalMessage').textContent=signal.computedReason;
 return results;
}
function install(){
 if(installed||!$('signalRegistry'))return;installed=true;
 const dispatcher=root.JMRI_TC_DISPATCHER_UI;
 if(dispatcher?.refresh){const original=dispatcher.refresh;dispatcher.refresh=function(...args){const out=original.apply(this,args);refresh();return out;};}
 const signalUi=root.JMRI_TC_SIGNALLING_UI;
 if(signalUi?.refresh){const original=signalUi.refresh;signalUi.refresh=function(...args){const out=original.apply(this,args);refresh();return out;};}
 refresh();timer=setInterval(refresh,1000);
}
root.JMRI_TC_SIGNALLING_INTEGRATION={refresh,compute,renderSvg,stop(){if(timer)clearInterval(timer);timer=null;}};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();