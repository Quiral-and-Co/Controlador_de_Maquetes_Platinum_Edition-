'use strict';
(function(root){
  const list=v=>Array.isArray(v)?v:[];
  const id=v=>String(v??'').trim();
  function inspect(config={},signals=[],snapshot={}){
    const levels=list(config.levels),tracks=levels.flatMap(l=>list(l.tracks).map(t=>({...t,_levelId:l.id,_levelName:l.name})));
    const blocks=levels.flatMap(l=>list(l.blocks).map(b=>({...b,_levelId:l.id,_levelName:l.name})));
    const routes=list(config.dispatcher?.routes),catalogue=root.JMRI_TC_DIGITAL_CATALOGUE?.getDatabase?.().accessories||[];
    const results={};
    for(const signal of list(signals)){
      const issues=[],warnings=[],block=blocks.find(b=>id(b.id)===id(signal.protectedBlockId));
      const route=routes.find(r=>id(r.id)===id(signal.routeId));
      if(!signal.protectedBlockId||!block)issues.push('Bloc protegit absent o inexistent.');
      if(!signal.routeId||!route)issues.push('Itinerari absent o inexistent.');
      const routeBlockIds=route?list(route.blockIds).map(id):[];
      if(block&&route&&!routeBlockIds.includes(id(block.id)))issues.push('El bloc protegit no forma part de l’itinerari associat.');
      if(routeBlockIds.length===0&&route)issues.push('L’itinerari no conté cap bloc.');
      const missingRouteBlocks=routeBlockIds.filter(bid=>!blocks.some(b=>id(b.id)===bid));
      if(missingRouteBlocks.length)issues.push('Blocs de l’itinerari inexistents: '+missingRouteBlocks.join(', ')+'.');
      const blockTracks=block?list(block.trackIds).map(tid=>tracks.find(t=>id(t.id)===id(tid))).filter(Boolean):[];
      if(block&&list(block.trackIds).length===0)issues.push('El bloc no té peces de via assignades.');
      if(block&&blockTracks.length!==list(block.trackIds).length)issues.push('El bloc conté identificadors de via inexistents.');
      if(block&&blockTracks.some(t=>id(t.blockId)!==id(block.id)))issues.push('Hi ha peces del bloc amb una assignació blockId inconsistent.');
      const selectedTrack=id(signal.trackId);
      const anchorTrack=selectedTrack?tracks.find(t=>id(t.id)===selectedTrack):blockTracks[0];
      if(selectedTrack&&!anchorTrack)issues.push('La peça de via seleccionada per a la senyal no existeix.');
      if(selectedTrack&&block&&!list(block.trackIds).map(id).includes(selectedTrack))issues.push('La peça de via de la senyal no pertany al bloc protegit.');
      if(!selectedTrack&&anchorTrack)warnings.push('Punt de senyal inferit de la primera peça del bloc; convé seleccionar-lo explícitament.');
      if(!anchorTrack&&block)issues.push('No hi ha cap peça de via per ancorar visualment la senyal.');
      const routeTracks=routeBlockIds.flatMap(bid=>{const b=blocks.find(x=>id(x.id)===bid);if(!b)return[];const ids=list(b.trackIds);if(!ids.length)issues.push('El bloc '+bid+' de l’itinerari no té peces de via assignades.');const resolved=ids.map(tid=>tracks.find(t=>id(t.id)===id(tid)));if(resolved.some(t=>!t))issues.push('El bloc '+bid+' conté peces de via inexistents.');if(resolved.filter(Boolean).some(t=>id(t.blockId)!==bid))issues.push('Assignació blockId inconsistent al bloc '+bid+'.');return resolved.filter(Boolean);});
      const routeTurnouts=routeTracks.filter(t=>t.type==='turnout');
      const requirements=list(signal.turnoutRequirements);
      const reqByName=new Map(requirements.map(r=>[id(r.systemName),Number(r.expectedState)]));
      for(const turnout of routeTurnouts){
        const name=id(turnout.turnoutSystemName);
        if(!name){issues.push('Desviament '+id(turnout.id)+' de l’itinerari sense system name JMRI.');continue;}
        if(!reqByName.has(name))issues.push('Desviament '+name+' de l’itinerari sense posició requerida a la senyal.');
        const accessory=catalogue.find(a=>a.type==='TURNOUT'&&id(a.systemName)===name);
        if(!accessory||accessory.verified!==true)issues.push('Desviament '+name+' sense accessori verificat al catàleg.');
      }
      for(const req of requirements){
        if(![2,4].includes(Number(req.expectedState)))issues.push('Posició invàlida per al desviament '+id(req.systemName)+'.');
        if(!routeTurnouts.some(t=>id(t.turnoutSystemName)===id(req.systemName)))issues.push('El desviament '+id(req.systemName)+' no pertany a cap peça de l’itinerari.');
      }
      const uniqueIssues=[...new Set(issues)],uniqueWarnings=[...new Set(warnings)];
      results[id(signal.id)]={signalId:id(signal.id),blockId:block?id(block.id):'',blockName:block?.name||'',routeId:route?id(route.id):'',routeName:route?.name||'',routeBlockIds,trackIds:routeTracks.map(t=>id(t.id)),protectedTrackIds:blockTracks.map(t=>id(t.id)),turnoutNames:routeTurnouts.map(t=>id(t.turnoutSystemName)).filter(Boolean),turnoutRequirements:requirements.map(r=>({systemName:id(r.systemName),expectedState:Number(r.expectedState)})),anchorTrackId:anchorTrack?id(anchorTrack.id):'',anchorTrack,levelId:anchorTrack?anchorTrack._levelId:null,ok:uniqueIssues.length===0,issues:uniqueIssues,warnings:uniqueWarnings};
    }
    return results;
  }
  function summarize(results){
    const values=Object.values(results||{});return{total:values.length,valid:values.filter(x=>x.ok).length,invalid:values.filter(x=>!x.ok).length,warnings:values.reduce((n,x)=>n+x.warnings.length,0)};
  }
  const api={inspect,summarize};if(typeof module!=='undefined'&&module.exports)module.exports=api;root.JMRI_TC_SignallingTopologyCore=api;
})(typeof window!=='undefined'?window:globalThis);
