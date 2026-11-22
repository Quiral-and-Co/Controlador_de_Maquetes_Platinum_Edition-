'use strict';
(function(root){
  const MAX_DCC_STEP=126,MIN_MAX_KMH=1,MAX_MAX_KMH=500;
  function clamp(value,min,max){return Math.min(max,Math.max(min,Number(value)||0));}
  function validateCalibration(maxKmh){
    const value=Number(maxKmh);
    if(!Number.isFinite(value)||value<MIN_MAX_KMH||value>MAX_MAX_KMH)return{ok:false,error:'La velocitat màxima ha d’estar entre 1 i 500 km/h.'};
    return{ok:true,maxKmh:value};
  }
  function stepToKmh(step,maxKmh){
    const calibration=validateCalibration(maxKmh);if(!calibration.ok)return 0;
    return clamp(step,0,MAX_DCC_STEP)/MAX_DCC_STEP*calibration.maxKmh;
  }
  function kmhToStep(kmh,maxKmh){
    const calibration=validateCalibration(maxKmh);if(!calibration.ok)return 0;
    return Math.round(clamp(kmh,0,calibration.maxKmh)/calibration.maxKmh*MAX_DCC_STEP);
  }
  function needleAngle(kmh,maxKmh){
    const calibration=validateCalibration(maxKmh);if(!calibration.ok)return-135;
    return-135+clamp(kmh,0,calibration.maxKmh)/calibration.maxKmh*270;
  }
  function normalizeAspect(signal,aspects={}){
    const key=signal?.computedAspect||signal?.aspect||'PARADA';
    return aspects[key]?.label||key.replaceAll('_',' ').toLowerCase();
  }
  function signalChain(signals,route){
    const list=Array.isArray(signals)?signals:[];
    const blocks=route?.blockIds||[];
    const eligible=list.filter(s=>!s.protectedBlockId||blocks.includes(s.protectedBlockId));
    if(!eligible.length)return[];
    const byId=new Map(eligible.map(s=>[s.id,s]));
    let first=eligible.find(s=>!eligible.some(other=>other.nextSignalId===s.id))||eligible[0];
    const chain=[],seen=new Set();
    while(first&&!seen.has(first.id)&&chain.length<2){chain.push(first);seen.add(first.id);first=byId.get(first.nextSignalId);}
    if(chain.length<2){for(const signal of eligible){if(!seen.has(signal.id)){chain.push(signal);break;}}}
    return chain.slice(0,2);
  }
  function distanceLabel(value){
    if(!Number.isFinite(Number(value))||value===null||value===undefined)return'Sense telemetria';
    const meters=Number(value);return meters>=1000?(meters/1000).toFixed(2)+' km':Math.round(meters)+' m';
  }
  function turnoutLabel(value){
    const state=String(value??'').toUpperCase();
    if([2,'2','CLOSED','STRAIGHT'].includes(value)||state==='CLOSED'||state==='STRAIGHT')return'Directa';
    if([4,'4','THROWN','DIVERGING','BRANCH'].includes(value)||['THROWN','DIVERGING','BRANCH'].includes(state))return'Desviada';
    return'Desconeguda';
  }
  function functionLabel(fn){return fn===0?'F0 · Llums':'F'+fn;}
  const DEFAULT_KEY_BINDINGS=Object.freeze({speedUp:'BracketRight',speedDown:'BracketLeft',presetUp:'Shift+BracketRight',presetDown:'Shift+BracketLeft',brakeUp:'PageDown',brakeDown:'PageUp'});
  const KEYBOARD_ACTIONS=Object.freeze([{id:'speedUp',label:'Velocitat · augmentar (1 pas DCC)'},{id:'speedDown',label:'Velocitat · reduir (1 pas DCC)'},{id:'presetUp',label:'Consigna prefixada · augmentar (5 km/h)'},{id:'presetDown',label:'Consigna prefixada · reduir (5 km/h)'},{id:'brakeUp',label:'Fre · aplicar (5%)'},{id:'brakeDown',label:'Fre · alliberar (5%)'},...Array.from({length:33},(_,fn)=>({id:'function'+fn,label:'Funció auxiliar F'+fn}))]);
  function keyboardCode(event){return (event.shiftKey?'Shift+':'')+(event.ctrlKey?'Ctrl+':'')+(event.altKey?'Alt+':'')+(event.metaKey?'Meta+':'')+event.code;}
  function keyboardLabel(code){return String(code||'').replace(/^Key/,'').replace(/^Digit/,'').replace('BracketRight',']').replace('BracketLeft','[').replace('PageUp','Re Pàg').replace('PageDown','Av Pàg');}
  function keyboardConflict(bindings,action,code){return KEYBOARD_ACTIONS.find(item=>item.id!==action&&bindings[item.id]===code)||null;}
  function keyboardAction(bindings,code){return KEYBOARD_ACTIONS.find(item=>bindings[item.id]===code)||null;}
  const api={MAX_DCC_STEP,MIN_MAX_KMH,MAX_MAX_KMH,clamp,validateCalibration,stepToKmh,kmhToStep,needleAngle,normalizeAspect,signalChain,distanceLabel,turnoutLabel,functionLabel,DEFAULT_KEY_BINDINGS,KEYBOARD_ACTIONS,keyboardCode,keyboardLabel,keyboardConflict,keyboardAction};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  root.JMRI_TC_LocomotiveDmiCore=api;
})(typeof window!=='undefined'?window:globalThis);
