'use strict';
(function(root){
  const DEFAULT_MAX_AGE_MS=15000;
  const STATES=Object.freeze({CLOSED:2,THROWN:4});
  function isFresh(timestamp,now,maxAge=DEFAULT_MAX_AGE_MS){
    const value=Number(timestamp);
    return Number.isFinite(value)&&value>0&&value<=now+1000&&now-value<=maxAge;
  }
  function normalizePosition(value){
    if(value&&typeof value==='object')value=value.state;
    if(value===2||value==='2'||String(value).toUpperCase()==='CLOSED'||String(value).toUpperCase()==='STRAIGHT')return'CLOSED';
    if(value===4||value==='4'||String(value).toUpperCase()==='THROWN'||String(value).toUpperCase()==='DIVERGING'||String(value).toUpperCase()==='BRANCH')return'THROWN';
    return'UNKNOWN';
  }
  function normalizeExpected(value){
    const normalized=String(value??'').trim().toUpperCase();
    if(normalized==='2'||normalized==='CLOSED'||normalized==='STRAIGHT')return'CLOSED';
    if(normalized==='4'||normalized==='THROWN'||normalized==='DIVERGING'||normalized==='BRANCH')return'THROWN';
    return'UNKNOWN';
  }
  function supervisionAriaLabel(baseLabel,status){
    const base=String(baseLabel||'Desviament').replace(/\s+·\s+supervisió\s+(?:ALIGNED|MISALIGNED|UNKNOWN)$/,'');
    return base+' · supervisió '+String(status);
  }
  function routeRequirements(route,config){
    const explicit=Array.isArray(route?.turnoutRequirements)?route.turnoutRequirements:[];
    const requirements=explicit.map(item=>({systemName:String(item.systemName||''),requiredState:normalizeExpected(item.requiredState??item.requiredPosition??item.state)}));
    const blockIds=new Set((route?.blockIds||[]).map(String));
    const tracks=(config?.levels||[]).flatMap(level=>level.tracks||[]).filter(track=>track.type==='turnout'&&track.turnoutSystemName&&blockIds.has(String(track.blockId)));
    for(const track of tracks){
      const systemName=String(track.turnoutSystemName);
      if(!requirements.some(item=>item.systemName===systemName)){
        requirements.push({systemName,requiredState:normalizeExpected(track.requiredPosition??track.requiredState)});
      }
    }
    return requirements;
  }
  function inspectRoute(route,config={},telemetry={},accessories=[],now=Date.now(),maxAge=DEFAULT_MAX_AGE_MS){
    const requirements=routeRequirements(route,config),incidents=[];
    if(!requirements.length)return{ok:true,status:'NOT_REQUIRED',requirements:[],incidents:[]};
    const connected=telemetry.connected===true&&isFresh(telemetry.lastHeartbeatAt,now,maxAge);
    if(!connected)incidents.push({severity:'error',code:'JMRI_DISCONNECTED',message:'Comunicació JMRI absent o heartbeat obsolet; no es pot validar cap desviament.'});
    for(const requirement of requirements){
      const name=requirement.systemName;
      if(!name||! /^[A-Za-z0-9_.:-]{1,80}$/.test(name)){
        incidents.push({severity:'error',code:'TURNOUT_REQUIREMENT_MISSING',message:'Itinerari '+(route?.name||route?.id||'')+': falta el desviament requerit o el seu identificador.'});continue;
      }
      if(requirement.requiredState==='UNKNOWN'){
        incidents.push({severity:'error',code:'TURNOUT_REQUIREMENT_MISSING',message:'Itinerari '+(route?.name||route?.id||'')+': manca la posició requerida per al desviament '+name+'.'});continue;
      }
      const accessory=(accessories||[]).find(item=>item.type==='TURNOUT'&&item.systemName===name);
      if(!accessory||accessory.verified!==true){
        incidents.push({severity:'error',code:'TURNOUT_UNVERIFIED',message:'Desviament '+name+' no verificat al catàleg; itinerari bloquejat.'});continue;
      }
      const raw=telemetry.turnouts?.[name],updated=telemetry.turnoutUpdatedAt?.[name];
      if(!connected||raw===undefined||!isFresh(updated,now,maxAge)){
        incidents.push({severity:'error',code:'TURNOUT_UNKNOWN',message:'Desviament '+name+' sense lectura JMRI vàlida i actualitzada.'});continue;
      }
      const actual=normalizePosition(raw);
      if(actual==='UNKNOWN'){
        incidents.push({severity:'error',code:'TURNOUT_INVALID',message:'Desviament '+name+' ha retornat una posició invàlida.'});continue;
      }
      if(actual!==requirement.requiredState){
        incidents.push({severity:'error',code:'TURNOUT_MISALIGNED',message:'Desviament '+name+': posició real '+actual+'; requerida '+requirement.requiredState+'.'});continue;
      }
    }
    const ok=incidents.length===0;
    return{ok,status:ok?'ALIGNED':incidents.some(x=>x.code==='TURNOUT_MISALIGNED')?'MISALIGNED':'UNKNOWN',requirements,incidents};
  }
  const api={DEFAULT_MAX_AGE_MS,STATES,isFresh,normalizePosition,normalizeExpected,supervisionAriaLabel,routeRequirements,inspectRoute};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  root.JMRI_TC_TurnoutSupervisionCore=api;
})(typeof window!=='undefined'?window:globalThis);
