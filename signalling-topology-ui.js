(() => {
'use strict';
const root=window,core=root.JMRI_TC_SignallingTopologyCore;
if(!core)return;
const $=id=>document.getElementById(id),NS='http://www.w3.org/2000/svg';
let timer=null,installed=false;
function config(){return root.JMRI_TC_DISPATCHER?.getConfig?.()||{};}
function selectedLevel(){return Number(root.JMRI_TC_BOARD?.getSelectedLevel?.()??1);}
function center(track){
 const length=Math.max(20,Number(track.geometry?.lengthMm)||180),angle=(Number(track.rotation)||0)*Math.PI/180;
 return{x:Number(track.x||0)+Math.cos(angle)*length/2,y:Number(track.y||0)+Math.sin(angle)*length/2};
}
function add(parent,tag,attrs,text){
 const node=document.createElementNS(NS,tag);
 Object.entries(attrs||{}).forEach(([k,v])=>node.setAttribute(k,String(v)));
 if(text!==undefined)node.textContent=text;
 parent.append(node);return node;
}
function svgLayer(svg){
 let layer=$('signalTopologyLayer');
 if(!layer){layer=document.createElementNS(NS,'g');layer.setAttribute('id','signalTopologyLayer');const signalLayer=$('signalLayer');svg.insertBefore(layer,signalLayer||null);}
 return layer;
}
function renderVisual(results){
 const svg=$('trackSvg');if(!svg)return;
 const layer=svgLayer(svg);layer.replaceChildren();
 const c=config(),levels=c.levels||[],signals=c.signals||[],level=selectedLevel();
 const tracks=levels.flatMap(l=>(l.tracks||[]).map(t=>({...t,_levelId:l.id})));
 for(const signal of signals){
  const result=results[signal.id];if(!result)continue;
  const routePoints=result.trackIds.map(id=>tracks.find(t=>String(t.id)===id)).filter(Boolean);
  const byLevel=new Map();
  routePoints.forEach(track=>{const key=String(track._levelId);if(!byLevel.has(key))byLevel.set(key,[]);byLevel.get(key).push(center(track));});
  for(const [levelId,points] of byLevel){
   if(Number(levelId)!==level||points.length<1)continue;
   const d=points.map((p,i)=>(i?'L':'M')+p.x+' '+p.y).join(' ');
   add(layer,'path',{d,class:result.ok?'signal-route-link':'signal-route-link invalid','data-signal-id':signal.id,'fill':'none','stroke':result.ok?'#36b7d8':'#e35b61','stroke-width':22,'stroke-linecap':'round','stroke-linejoin':'round','opacity':.32});
  }
  routePoints.filter(track=>track.type==='turnout'&&Number(track._levelId)===level).forEach(track=>{const p=center(track),name=String(track.turnoutSystemName||track.id),req=(result.turnoutRequirements||[]).find(r=>r.systemName===name),label=req?(req.expectedState===2?'RECTE':'BRANCA'):'SENSE POSICIÓ';const g=add(layer,'g',{class:'signal-turnout-link','data-signal-id':signal.id,transform:'translate('+p.x+' '+p.y+')'});add(g,'rect',{x:-15,y:-15,width:30,height:30,rx:5,fill:req?'#24506a':'#6b3034',stroke:'#e5edf5','stroke-width':3});add(g,'text',{x:22,y:7,class:'signal-topology-label'},name+' · '+label);});
  const anchor=result.anchorTrack;
  if(anchor&&Number(result.levelId)===level){
   const p=center(anchor),g=add(layer,'g',{class:'signal-topology-anchor','data-signal-id':signal.id,transform:'translate('+p.x+' '+p.y+')'});
   add(g,'circle',{r:30,class:'signal-protected-track-ring',fill:'none',stroke:result.ok?'#36b7d8':'#e35b61','stroke-width':7,'stroke-dasharray':'9 6'});
   add(g,'circle',{r:6,fill:'#f4f7fb'});
   const label=add(g,'text',{x:38,y:-22,class:'signal-topology-label'},signal.id+' · '+(result.blockId||'sense bloc'));
   label.setAttribute('font-size','24');
  }
 }
}
function renderDiagnostics(results){
 const host=$('signalTopologyDiagnostics');if(!host)return;
 host.replaceChildren();
 const summary=core.summarize(results),heading=document.createElement('p');
 heading.className='signal-topology-summary';
 heading.textContent='Senyals: '+summary.total+' · vinculacions correctes: '+summary.valid+' · amb incidències: '+summary.invalid+' · avisos: '+summary.warnings;
 host.append(heading);
 Object.values(results).forEach(r=>{
  const card=document.createElement('article');card.className='signal-topology-item '+(r.ok?'valid':'invalid');
  const title=document.createElement('strong');title.textContent=r.signalId+' · '+(r.ok?'Vinculació correcta':'Vinculació incompleta');card.append(title);
  const refs=document.createElement('p');refs.textContent='Bloc: '+(r.blockName||r.blockId||'—')+' · Itinerari: '+(r.routeName||r.routeId||'—')+' · Ancoratge: '+(r.anchorTrackId||'—');card.append(refs);
  const route=document.createElement('p');route.textContent='Seqüència de blocs: '+(r.routeBlockIds.join(' → ')||'—')+' · Peces: '+(r.trackIds.join(', ')||'—');card.append(route);
  const turnouts=document.createElement('p');turnouts.textContent='Desviaments de l’itinerari: '+(r.turnoutNames.join(', ')||'cap');card.append(turnouts);
  [...r.issues.map(x=>'ERROR · '+x),...r.warnings.map(x=>'AVÍS · '+x)].forEach(message=>{const p=document.createElement('p');p.className='signal-topology-issue';p.textContent=message;card.append(p);});
  host.append(card);
 });
}
function refresh(){
 const c=config(),signals=Array.isArray(c.signals)?c.signals:[],results=core.inspect(c,signals,root.JMRI_TC_DISPATCHER_UI?.getSnapshot?.()||{});
 signals.forEach(s=>{const r=results[s.id];if(r&&!r.ok){s.computedAspect='PARADA';s.computedReason='Vinculació topològica incompleta: '+r.issues.join(' ');}});
 renderVisual(results);renderDiagnostics(results);root.JMRI_TC_SIGNALLING_INTEGRATION?.renderSvg?.();return results;
}
function install(){
 if(installed||!$('signalRegistry'))return;installed=true;
 const dispatcher=root.JMRI_TC_DISPATCHER_UI;
 if(dispatcher?.refresh){const original=dispatcher.refresh;dispatcher.refresh=function(...args){const out=original.apply(this,args);refresh();return out;};}
 const signalUi=root.JMRI_TC_SIGNALLING_UI;
 if(signalUi?.refresh){const original=signalUi.refresh;signalUi.refresh=function(...args){const out=original.apply(this,args);refresh();return out;};}
 refresh();timer=setInterval(refresh,1000);
}
root.JMRI_TC_SIGNALLING_TOPOLOGY={refresh,inspect:()=>core.inspect(config(),config().signals||[],root.JMRI_TC_DISPATCHER_UI?.getSnapshot?.()||{}),stop(){if(timer)clearInterval(timer);timer=null;}};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();
