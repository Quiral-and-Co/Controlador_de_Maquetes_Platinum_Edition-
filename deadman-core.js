'use strict';
(function(root){
 const WARNING_BLUE_MS=30000,WARNING_ORANGE_MS=33000,TRIP_MS=36000;
 function createState(options={}){return{enabled:options.enabled!==false,pressed:false,startedAt:null,phase:options.enabled===false?'DISABLED':'IDLE',tripped:false};}
 function configure(s,enabled){if(typeof enabled!=='boolean')return{ok:false,error:'L’opció home mort ha de ser booleana.'};if(s.tripped&&enabled)return{ok:false,error:'Cal rearmament conscient després d’un frenat d’emergència.'};s.enabled=enabled;s.pressed=false;s.startedAt=null;s.phase=enabled?'IDLE':'DISABLED';return{ok:true,state:s};}
 function trip(s){s.pressed=false;s.tripped=true;s.phase='TRIPPED';return{action:'TRIP',phase:s.phase,reason:'Home mort: no s’ha confirmat la presència del maquinista.'};}
 function press(s,now=Date.now()){
  if(!s.enabled)return{ok:true,required:false,phase:'DISABLED'};
  if(s.tripped)return{ok:false,error:'Home mort disparat; cal rearmament conscient.'};
  if(s.pressed)return{ok:true,phase:s.phase};
  const acknowledgingWarning=s.phase==='BLUE'||s.phase==='ORANGE';
  s.pressed=true;s.startedAt=now;s.phase='NORMAL';
  return{ok:true,phase:s.phase,reset:acknowledgingWarning||s.startedAt===now};
 }
 function release(s,now=Date.now()){
  if(!s.enabled)return{ok:true,phase:'DISABLED'};
  if(s.tripped)return{ok:false,error:'Home mort disparat.'};
  if(!s.pressed)return{ok:true,phase:s.phase};
  const elapsed=s.startedAt===null?0:now-s.startedAt;
  s.pressed=false;
  if(elapsed<WARNING_BLUE_MS){s.startedAt=now-WARNING_BLUE_MS;s.phase='BLUE';return{ok:true,action:'BLUE',phase:s.phase,warning:true};}
  s.phase=elapsed>=WARNING_ORANGE_MS?'ORANGE':'BLUE';
  return{ok:true,phase:s.phase,warning:true};
 }
 function evaluate(s,now=Date.now()){
  if(!s.enabled||s.tripped||s.startedAt===null)return{action:'NONE',phase:s.phase};
  const elapsed=now-s.startedAt;
  if(elapsed>=TRIP_MS)return trip(s);
  if(elapsed>=WARNING_ORANGE_MS){s.phase='ORANGE';return{action:'ORANGE',phase:s.phase};}
  if(elapsed>=WARNING_BLUE_MS){s.phase='BLUE';return{action:'BLUE',phase:s.phase};}
  if(s.pressed)s.phase='NORMAL';
  return{action:'NONE',phase:s.phase};
 }
 function rearm(s){s.pressed=false;s.startedAt=null;s.tripped=false;s.phase=s.enabled?'IDLE':'DISABLED';return{ok:true,state:s};}
 const api={WARNING_BLUE_MS,WARNING_ORANGE_MS,TRIP_MS,createState,configure,press,release,evaluate,rearm};
 if(typeof module!=='undefined'&&module.exports)module.exports=api;
 if(root)root.JMRI_TC_DeadmanCore=api;
})(typeof window!=='undefined'?window:globalThis);
