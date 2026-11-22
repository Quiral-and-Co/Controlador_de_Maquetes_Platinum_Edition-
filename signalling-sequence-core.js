'use strict';
(function(root){
 const list=v=>Array.isArray(v)?v:[];
 const id=v=>String(v??'').trim();
 function inspect(signals=[],routes=[]){
  const byId=new Map(list(signals).map(s=>[id(s.id),s]));
  const routeById=new Map(list(routes).map(r=>[id(r.id),r]));
  const result={};
  for(const signal of list(signals)){
   const sid=id(signal.id),issues=[],warnings=[];
   const first=id(signal.nextSignal1Id||signal.nextSignalId),second=id(signal.nextSignal2Id);
   const route=routeById.get(id(signal.routeId));
   const routeBlocks=route?list(route.blockIds).map(id):[];
   const direction=String(signal.direction||'FORWARD').toUpperCase();
   const pos=s=>routeBlocks.indexOf(id(s.protectedBlockId));
   if(first){
    const next=byId.get(first);
    if(!next)issues.push('Senyal 1 inexistent: '+first+'.');
    else {
     if(first===sid)issues.push('Referència circular a la mateixa senyal.');
     if(signal.routeId&&next.routeId!==signal.routeId)issues.push('Senyal 1 no pertany al mateix itinerari.');
     if(route&&pos(signal)>=0&&pos(next)>=0&&((direction==='REVERSE'&&pos(next)>=pos(signal))||(direction!=='REVERSE'&&pos(next)<=pos(signal))))issues.push('Senyal 1 no és posterior segons el sentit de circulació i l’ordre de blocs.');
    }
   }
   if(second){
    const next2=byId.get(second);
    if(!next2)issues.push('Senyal 2 inexistent: '+second+'.');
    else {
     if(second===sid||second===first)issues.push('Referència duplicada o circular a Senyal 2.');
     if(signal.routeId&&next2.routeId!==signal.routeId)issues.push('Senyal 2 no pertany al mateix itinerari.');
     if(first){
      const next1=byId.get(first);
      if(next1&&next1.nextSignal1Id&&id(next1.nextSignal1Id)!==second)issues.push('Senyal 2 no coincideix amb la continuació declarada de Senyal 1.');
      if(next1&&route&&pos(next1)>=0&&pos(next2)>=0&&((direction==='REVERSE'&&pos(next2)>=pos(next1))||(direction!=='REVERSE'&&pos(next2)<=pos(next1))))issues.push('Senyal 2 no és posterior a Senyal 1 en el sentit de circulació.');
     }
    }
   }
   if((first||second)&&!route)warnings.push('Sense itinerari verificable: no es pot certificar l’ordre físic de les senyals.');
   result[sid]={signalId:sid,nextSignal1Id:first,nextSignal2Id:second,ok:issues.length===0,issues,warnings};
  }
  // Detect cycles across Senyal 1 links, including cycles longer than two nodes.
  for(const start of list(signals)){
   const seen=new Set();let current=start;
   while(current){
    const currentId=id(current.id);
    if(seen.has(currentId)){result[id(start.id)].ok=false;result[id(start.id)].issues.push('Dependència circular detectada a la seqüència de senyals.');break;}
    seen.add(currentId);
    current=byId.get(id(current.nextSignal1Id||current.nextSignalId));
   }
  }
  return result;
 }
 const api={inspect};if(typeof module!=='undefined'&&module.exports)module.exports=api;root.JMRI_TC_SignallingSequenceCore=api;
})(typeof window!=='undefined'?window:globalThis);
