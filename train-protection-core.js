'use strict';
(function(root){
 const SYSTEMS=Object.freeze({
  ASFA_DIGITAL:{label:'ASFA Digital',family:'ASFA',levels:['Únic'],ackRequired:true,description:'Supervisió de senyals i reconeixement del maquinista.'},
  ETCS:{label:'ETCS / ERTMS',family:'ETCS',levels:['Nivell 0','Nivell 1','Nivell 2','Nivell 3'],ackRequired:true,description:'Supervisió ETCS segons nivell seleccionat; L3 en mode conceptual de bloc mòbil.'},
  LZB:{label:'LZB',family:'LZB',levels:['LZB'],ackRequired:true,description:'Supervisió contínua de velocitat i distància objectiu.'},
  PZB:{label:'PZB',family:'PZB',levels:['PZB'],ackRequired:true,description:'Supervisió puntual, avisos i reconeixement de vigilància.'}
 });
 const STATES=Object.freeze({OFF:'OFF',SUPERVISION:'SUPERVISION',ACK_REQUIRED:'ACK_REQUIRED',BRAKING:'BRAKING',TRIPPED:'TRIPPED'});
 const clamp=(v,min,max)=>Math.min(max,Math.max(min,Number(v)||0));
 function normalize(system,level){
  if(!SYSTEMS[system])return{ok:false,error:'Sistema de protecció desconegut.'};
  const chosen=level||SYSTEMS[system].levels[0];
  if(!SYSTEMS[system].levels.includes(chosen))return{ok:false,error:'Nivell ETCS/ERTMS no vàlid per al sistema seleccionat.'};
  return{ok:true,system,level:chosen};
 }
 function createSession(options={}){
  const system=options.system||'ETCS';const n=normalize(system,options.level||SYSTEMS[system]?.levels?.[0]);
  if(!n.ok)return n;
  return{ok:true,session:{system:n.system,level:n.level,state:STATES.SUPERVISION,speedKmh:0,permittedKmh:clamp(options.permittedKmh??80,0,350),targetKmh:clamp(options.targetKmh??40,0,350),targetDistanceM:clamp(options.targetDistanceM??1000,0,100000),ackPending:false,ackSecondsRemaining:0,elapsedSeconds:0,brakeDemand:false,emergency:false,lastEvent:'Supervisió manual simulada preparada.',events:[],acknowledgements:0,profile:'SIMULATION_ONLY'}};
 }
 function event(s,message){return{...s,lastEvent:message,events:[...s.events.slice(-19),{second:s.elapsedSeconds,message}]};}
 function setSpeed(s,value){if(!s||s.state===STATES.TRIPPED)return{ok:false,error:'Sessió aturada per intervenció de seguretat.'};const v=clamp(value,0,350);let n={...s,speedKmh:v};if(v>s.permittedKmh||(s.targetDistanceM<=0&&v>s.targetKmh))n={...n,brakeDemand:true,state:STATES.BRAKING};return{ok:true,session:event(n,'Velocitat manual simulada: '+v+' km/h.')};}
 function setTarget(s,{permittedKmh,targetKmh,targetDistanceM}={}){
  if(!s||s.state===STATES.TRIPPED)return{ok:false,error:'Sessió aturada per intervenció de seguretat.'};
  const n={...s,permittedKmh:clamp(permittedKmh??s.permittedKmh,0,350),targetKmh:clamp(targetKmh??s.targetKmh,0,350),targetDistanceM:clamp(targetDistanceM??s.targetDistanceM,0,100000)};
  if(n.speedKmh>n.permittedKmh||(n.targetDistanceM<=0&&n.speedKmh>n.targetKmh)) { n.brakeDemand=true; n.state=STATES.BRAKING; }
  return{ok:true,session:event(n,'Objectiu de supervisió actualitzat.')};
 }
 function requestAcknowledgement(s,reason='Avís de senyal / vigilància',windowSeconds=4){
  if(!s||s.state===STATES.TRIPPED)return{ok:false,error:'Sessió aturada per intervenció de seguretat.'};
  const n={...s,ackPending:true,ackSecondsRemaining:clamp(windowSeconds,1,30),state:STATES.ACK_REQUIRED};
  return{ok:true,session:event(n,'RECONÈIXER: '+String(reason)+'. Finestra simulada '+n.ackSecondsRemaining+' s.')};
 }
 function acknowledge(s){
  if(!s||s.state===STATES.TRIPPED)return{ok:false,error:'No es pot reconèixer: sessió aturada.'};
  if(!s.ackPending)return{ok:false,error:'No hi ha cap avís pendent de reconeixement.'};
  const n={...s,ackPending:false,ackSecondsRemaining:0,acknowledgements:s.acknowledgements+1,state:s.brakeDemand?STATES.BRAKING:STATES.SUPERVISION};
  return{ok:true,session:event(n,'Reconeixement del maquinista registrat a la simulació.')};
 }
 function trip(s,reason){return{ok:true,session:event({...s,state:STATES.TRIPPED,emergency:true,brakeDemand:true,ackPending:false,ackSecondsRemaining:0},'FRENADA D’EMERGÈNCIA SIMULADA: '+String(reason||'intervenció de protecció'))};}
 function tick(s,seconds=1){
  if(!s||s.state===STATES.TRIPPED)return{ok:false,error:'La sessió està aturada; cal reinicialitzar-la.'};
  const dt=Number(seconds);if(!Number.isInteger(dt)||dt<1||dt>60)return{ok:false,error:'L’avanç ha de ser d’1 a 60 segons enters.'};
  let n={...s,elapsedSeconds:s.elapsedSeconds+dt};
  if(n.ackPending){n.ackSecondsRemaining-=dt;if(n.ackSecondsRemaining<=0)return trip(n,'manca de reconeixement dins del temps simulat');}
  if(n.speedKmh>n.permittedKmh)return trip(n,'superació de la velocitat permesa');
  if(n.speedKmh>n.targetKmh&&n.targetDistanceM<=0)return trip(n,'incompliment de l’objectiu de velocitat');
  n.targetDistanceM=clamp(n.targetDistanceM-(n.speedKmh/3.6)*dt,0,100000);
  return{ok:true,session:event(n,'Supervisió actualitzada; '+n.targetDistanceM.toFixed(0)+' m fins a l’objectiu.')};
 }
 function reset(s){
  const fresh=createSession({system:s.system,level:s.level,permittedKmh:s.permittedKmh,targetKmh:s.targetKmh,targetDistanceM:s.targetDistanceM});
  return fresh.ok?{ok:true,session:event(fresh.session,'Sessió reinicialitzada; cal tornar a validar l’estat del tren.')}:fresh;
 }
 function snapshot(s){return{system:SYSTEMS[s.system].label,level:s.level,state:s.state,speedKmh:s.speedKmh,permittedKmh:s.permittedKmh,targetKmh:s.targetKmh,targetDistanceM:s.targetDistanceM,ackPending:s.ackPending,ackSecondsRemaining:s.ackSecondsRemaining,brakeDemand:s.brakeDemand,emergency:s.emergency,profile:'SIMULATION_ONLY'};}
 const api={SYSTEMS,STATES,normalize,createSession,setSpeed,setTarget,requestAcknowledgement,acknowledge,trip,tick,reset,snapshot};
 if(typeof module!=='undefined'&&module.exports)module.exports=api;
 if(root)root.JMRI_TC_TrainProtection=api;
})(typeof window!=='undefined'?window:globalThis);
