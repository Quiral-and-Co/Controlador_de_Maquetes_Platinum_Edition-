'use strict';
(function(root){
 const ITEMS=[
  ['software','Programari i proves automatitzades superades'],
  ['topology','Topologia i itineraris revisats'],
  ['sensors','Sensors, adreces i ocupació verificats físicament'],
  ['turnouts','Desviaments i feedback verificats o exclosos de la ruta'],
  ['power','Alimentacions, polaritats, seccions i fusibles verificats'],
  ['emergency','Parada d’emergència física independent provada'],
  ['wiring','Cablejat, aïllament i proteccions inspeccionats'],
  ['failSafe','Pèrdua de comunicació i estat segur provats']
 ];
 function createState(){return {checks:Object.fromEntries(ITEMS.map(([id])=>[id,false])),physicalConfirmed:false,fault:null,armed:false};}
 function status(s){const missing=ITEMS.filter(([id])=>s.checks[id]!==true).map(([,label])=>label);if(s.fault) return {ok:false,state:'FAULT',missing:[s.fault]};if(missing.length||s.physicalConfirmed!==true)return {ok:false,state:'LOCKED',missing:[...missing,...(s.physicalConfirmed?[]:['Confirmació física de l’operador pendent'])]};return {ok:true,state:'READY',missing:[]};}
 function setCheck(s,id,value){if(!ITEMS.some(x=>x[0]===id))return {ok:false,error:'Comprovació desconeguda.'};s.checks[id]=value===true;s.armed=false;return {ok:true,status:status(s)};}
 function confirmPhysical(s,value){s.physicalConfirmed=value===true;s.armed=false;return status(s);}
 function trip(s,reason){s.fault=String(reason||'Fallada de seguretat');s.armed=false;return status(s);}
 function reset(s){s.fault=null;s.armed=false;s.physicalConfirmed=false;s.checks=Object.fromEntries(ITEMS.map(([id])=>[id,false]));return status(s);}
 function canEnablePhysical(s,runtime){const gate=status(s);const live=runtime||{};return {ok:gate.ok&&live.jmriConnected===true&&live.jmriPower==='ON'&&live.bridgeConnected===true&&live.sensorDataValid===true&&live.emergencyCircuitVerified===true, state:gate.state,missing:[...gate.missing,...(live.jmriConnected===true?[]:['JMRI desconnectat']),...(live.jmriPower==='ON'?[]:['Alimentació JMRI no confirmada ON']),...(live.bridgeConnected===true?[]:['Pont WiThrottle desconnectat']),...(live.sensorDataValid===true?[]:['Sensors no vàlids']),...(live.emergencyCircuitVerified===true?[]:['Circuit físic d’emergència no verificat'])]};}
 const api={ITEMS,createState,status,setCheck,confirmPhysical,trip,reset,canEnablePhysical};
 if(typeof module!=='undefined'&&module.exports)module.exports=api;
 if(root){root.JMRI_TC_SafetyCore=api;const state=createState();root.JMRI_TC_SAFETY={state,refresh(runtime){return canEnablePhysical(state,runtime)},setCheck(id,v){const r=setCheck(state,id,v);root.dispatchEvent?.(new CustomEvent('jmri-tc-safety-change'));return r;},confirmPhysical(v){const r=confirmPhysical(state,v);root.dispatchEvent?.(new CustomEvent('jmri-tc-safety-change'));return r;},trip(reason){const r=trip(state,reason);root.dispatchEvent?.(new CustomEvent('jmri-tc-safety-change'));return r;},canEnablePhysical(runtime){return canEnablePhysical(state,runtime);}};}
})(typeof window!=='undefined'?window:globalThis);
