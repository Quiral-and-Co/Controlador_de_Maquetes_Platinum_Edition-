'use strict';
(function(root){
  const CHECKS=Object.freeze({
    signal:[
      {id:'software-link',label:'Referències senyal, bloc, itinerari i aspectes revisades',kind:'software'},
      {id:'software-failsafe',label:'PARADA per dades absents, obsoletes o inconsistents',kind:'software'},
      {id:'physical-lamp',label:'Cablejat i correspondència de llums inspeccionats',kind:'physical'},
      {id:'physical-aspects',label:'Tots els aspectes permesos contrastats individualment',kind:'physical'}
    ],
    block:[
      {id:'software-track',label:'Peces assignades i topologia consistents',kind:'software'},
      {id:'software-sensor',label:'Cobertura de detecció i itineraris revisats',kind:'software'},
      {id:'physical-detection',label:'Ocupació i alliberament provats al tram real',kind:'physical'},
      {id:'physical-isolation',label:'Aïllaments, alimentació i retorn verificats',kind:'physical'}
    ],
    sensor:[
      {id:'software-identity',label:'Identificador JMRI únic i assignació coherent',kind:'software'},
      {id:'software-freshness',label:'Lectures, valors i temps d’actualització validats',kind:'software'},
      {id:'physical-wiring',label:'Cablejat, polaritat i connexions inspeccionats',kind:'physical'},
      {id:'physical-states',label:'Estats ocupat / lliure provats físicament',kind:'physical'}
    ],
    turnout:[
      {id:'software-identity',label:'Desviament i posició requerida vinculats a la ruta',kind:'software'},
      {id:'software-feedback',label:'Feedback i estat desconegut tractats com a fallada',kind:'software'},
      {id:'physical-straight',label:'Posició recta i feedback verificats',kind:'physical'},
      {id:'physical-branch',label:'Posició desviada i feedback verificats',kind:'physical'},
      {id:'physical-protection',label:'Motor, alimentació i protecció elèctrica inspeccionats',kind:'physical'}
    ]
  });
  const list=v=>Array.isArray(v)?v:[];
  const key=(type,id)=>type+':'+String(id??'').trim();
  function entities(config={}){
    const out=[];
    for(const signal of list(config.signals))out.push({key:key('signal',signal.id),type:'signal',id:String(signal.id),name:signal.name||signal.id});
    for(const level of list(config.levels)){
      for(const block of list(level.blocks))out.push({key:key('block',block.id),type:'block',id:String(block.id),name:block.name||block.id,levelId:level.id,levelName:level.name||('Nivell '+level.id)});
      for(const sensor of list(level.sensors))out.push({key:key('sensor',sensor.id),type:'sensor',id:String(sensor.id),name:sensor.userName||sensor.systemName||sensor.id,systemName:sensor.systemName||'',levelId:level.id,levelName:level.name||('Nivell '+level.id)});
      for(const track of list(level.tracks).filter(t=>t.type==='turnout'&&t.turnoutSystemName))out.push({key:key('turnout',track.turnoutSystemName),type:'turnout',id:String(track.turnoutSystemName),name:track.name||track.turnoutSystemName,trackId:track.id,levelId:level.id,levelName:level.name||('Nivell '+level.id)});
    }
    return out;
  }
  function record(config,entityKey){
    const matrix=config.validationMatrix&&typeof config.validationMatrix==='object'?config.validationMatrix:{};
    const r=matrix[entityKey];return r&&typeof r==='object'?r:{checks:{},failed:false,updatedAt:null};
  }
  function evaluateEntry(entity,r){
    const checks=CHECKS[entity.type]||[],state=r.checks||{},failed=r.failed===true;
    const missing=checks.filter(c=>state[c.id]!==true).map(c=>c.id);
    const missingSoftware=checks.filter(c=>c.kind==='software'&&state[c.id]!==true).map(c=>c.id);
    const missingPhysical=checks.filter(c=>c.kind==='physical'&&state[c.id]!==true).map(c=>c.id);
    const status=failed?'FAILED':missingSoftware.length?'PENDING_SOFTWARE':missingPhysical.length?'SOFTWARE_READY':'PHYSICAL_READY';
    return{...entity,status,failed,failureReason:r.failureReason||'',missing,missingSoftware,missingPhysical,checks:checks.map(c=>({...c,checked:state[c.id]===true}))};
  }
  function inspect(config={}){
    const entries=entities(config).map(e=>evaluateEntry(e,record(config,e.key)));
    const counts={total:entries.length,failed:0,pendingSoftware:0,softwareReady:0,physicalReady:0};
    entries.forEach(e=>{if(e.status==='FAILED')counts.failed++;else if(e.status==='PENDING_SOFTWARE')counts.pendingSoftware++;else if(e.status==='SOFTWARE_READY')counts.softwareReady++;else counts.physicalReady++;});
    return{entries,counts};
  }
  function setCheck(config,entityKey,checkId,value,now=Date.now()){
    const entity=entities(config).find(e=>e.key===entityKey);if(!entity)return{ok:false,error:'Element de validació inexistent.'};
    const check=(CHECKS[entity.type]||[]).find(c=>c.id===checkId);if(!check)return{ok:false,error:'Comprovació desconeguda.'};
    config.validationMatrix=config.validationMatrix&&typeof config.validationMatrix==='object'?config.validationMatrix:{};
    const current=record(config,entityKey),checks={...(current.checks||{}),[checkId]:value===true};
    config.validationMatrix[entityKey]={...current,checks,failed:false,updatedAt:now};
    return{ok:true,result:evaluateEntry(entity,config.validationMatrix[entityKey])};
  }
  function setFailure(config,entityKey,failed,reason='',now=Date.now()){
    const entity=entities(config).find(e=>e.key===entityKey);if(!entity)return{ok:false,error:'Element de validació inexistent.'};
    config.validationMatrix=config.validationMatrix&&typeof config.validationMatrix==='object'?config.validationMatrix:{};
    const current=record(config,entityKey);
    config.validationMatrix[entityKey]={...current,failed:failed===true,failureReason:failed?String(reason||'Incidència declarada'):'',updatedAt:now};
    return{ok:true,result:evaluateEntry(entity,config.validationMatrix[entityKey])};
  }
  function levelDiagnostics(config={},topologyResults={}){
    const all=entities(config);
    return list(config.levels).map(level=>{
      const entries=all.filter(e=>e.levelId===level.id);
      const signals=list(config.signals).filter(s=>{const t=topologyResults[s.id];return t&&Number(t.levelId)===Number(level.id);});
      const issues=signals.reduce((n,s)=>n+(topologyResults[s.id]?.issues?.length||0),0);
      const validation=entries.map(e=>evaluateEntry(e,record(config,e.key)));
      return{levelId:level.id,levelName:level.name||('Nivell '+level.id),entities:entries.length,signals:signals.length,topologyIssues:issues,pending:validation.filter(e=>e.status==='PENDING_SOFTWARE').length,softwareReady:validation.filter(e=>e.status==='SOFTWARE_READY').length,physicalReady:validation.filter(e=>e.status==='PHYSICAL_READY').length,failed:validation.filter(e=>e.status==='FAILED').length};
    });
  }
  const api={CHECKS,key,entities,inspect,setCheck,setFailure,levelDiagnostics};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  root.JMRI_TC_PhysicalValidationCore=api;
})(typeof window!=='undefined'?window:globalThis);
