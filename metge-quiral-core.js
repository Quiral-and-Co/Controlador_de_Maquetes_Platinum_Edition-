'use strict';
(function(root){
  const list=v=>Array.isArray(v)?v:[];
  function inspect(config={},options={}){
    const issues=[],checks=[];
    const add=(id,label,ok,detail='')=>{checks.push({id,label,ok:!!ok,detail});if(!ok)issues.push({id,label,detail,severity:'error'});};
    const levels=list(config.levels),levelIds=new Set(),trackIds=new Set(),blockIds=new Set(),sensorNames=new Set(),connectionIds=new Set();
    add('schema','Model de projecte v3',Number(config.schemaVersion)===3,'Versió detectada: '+String(config.schemaVersion??'absent'));
    add('levels','Cinc nivells definits',levels.length===5,'Nivells detectats: '+levels.length);
    let validCoordinates=true,uniqueTracks=true,uniqueBlocks=true,uniqueSensors=true,validReferences=true;
    for(const level of levels){
      if(level.id===undefined||levelIds.has(String(level.id)))validReferences=false;levelIds.add(String(level.id));
      for(const p of list(level.tracks)){
        if(!p.id||trackIds.has(String(p.id)))uniqueTracks=false;trackIds.add(String(p.id));
        if(!Number.isFinite(Number(p.x))||!Number.isFinite(Number(p.y))||Number(p.x)<0||Number(p.x)>3000||Number(p.y)<0||Number(p.y)>1800)validCoordinates=false;
        if(p.levelId!==undefined&&String(p.levelId)!==String(level.id))validReferences=false;
        if(p.blockId&&!list(level.blocks).some(b=>String(b.id)===String(p.blockId)))validReferences=false;
        if(p.sensorName&&!list(level.sensors).some(s=>s.systemName===p.sensorName))validReferences=false;
      }
      for(const b of list(level.blocks)){if(!b.id||blockIds.has(String(b.id)))uniqueBlocks=false;blockIds.add(String(b.id));}
      for(const s of list(level.sensors)){if(!s.systemName||sensorNames.has(String(s.systemName)))uniqueSensors=false;sensorNames.add(String(s.systemName));}
    }
    add('track-ids','Identificadors de vies únics',uniqueTracks);
    add('coordinates','Coordenades dins la superfície',validCoordinates);
    add('block-ids','Identificadors de blocs únics',uniqueBlocks);
    add('sensor-ids','System names de sensors únics',uniqueSensors);
    add('references','Referències i nivells coherents',validReferences);
    let validConnections=true,unverified=0;
    for(const c of list(config.connections)){
      if(!c.id||connectionIds.has(String(c.id))||!trackIds.has(String(c.from?.pieceId))||!trackIds.has(String(c.to?.pieceId))||c.from?.pieceId===c.to?.pieceId)validConnections=false;
      connectionIds.add(String(c.id));if(c.verified!==true)unverified++;
    }
    add('connections','Connexions amb referències vàlides',validConnections);
    checks.push({id:'connection-verification',label:'Connexions verificades',ok:unverified===0,detail:unverified+' connexions no verificades',severity:'warning'});
    const physicalOutputsLocked=options.physicalOutputsEnabled!==true;
    checks.push({id:'physical-lock','label':'Sortides físiques bloquejades per defecte',ok:physicalOutputsLocked,detail:physicalOutputsLocked?'Bloqueig actiu':'Sortides declarades habilitades',severity:physicalOutputsLocked?'info':'critical'});
    const errors=issues.length,warnings=checks.filter(c=>c.severity==='warning'&&!c.ok).length,critical=checks.filter(c=>c.severity==='critical'&&!c.ok).length;
    return {status:errors||critical?'NEEDS_ATTENTION':warnings?'WARNINGS':'HEALTHY',checks,issues,counts:{total:checks.length,passed:checks.filter(c=>c.ok).length,errors,warnings,critical},physicalOutputsLocked};
  }
  function recommendations(report){
    return report.checks.filter(c=>!c.ok).map(c=>({checkId:c.id,message:c.id==='connection-verification'?'Revisa les connexions i executa les proves geomètriques i topològiques abans de marcar-les verificades.':c.id==='physical-lock'?'Desactiva les sortides físiques i revisa el bloqueig fail-safe.':'Revisa: '+c.label+'. '+(c.detail||'')}));}
  const api={inspect,recommendations};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  root.JMRI_TC_METGE_QUIRAL=api;
})(typeof window!=='undefined'?window:globalThis);
