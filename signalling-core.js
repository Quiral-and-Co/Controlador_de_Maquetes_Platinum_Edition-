'use strict';
(function (root) {
  const ASPECTS = Object.freeze({
    VIA_LIBRE: { id:'VIA_LIBRE', label:'Via lliure', code:'V', color:'green', order:'Circular normalment si res no s’oposa.' },
    VIA_LIBRE_CONDICIONAL: { id:'VIA_LIBRE_CONDICIONAL', label:'Via lliure condicional', code:'V*', color:'green', order:'No excedir 160 km/h en passar per la senyal següent, llevat que aquesta ordeni via lliure.' },
    ANUNCIO_PRECAUCION: { id:'ANUNCIO_PRECAUCION', label:'Anunci de precaució', code:'V/A', color:'yellow', order:'Respectar la velocitat indicada per a les agulles protegides per la senyal següent.' },
    PREANUNCIO_PARADA: { id:'PREANUNCIO_PARADA', label:'Preanunci de parada', code:'P', color:'yellow', order:'No excedir la velocitat indicada en passar per la senyal següent, llevat de les excepcions reglamentàries.' },
    ANUNCIO_PARADA: { id:'ANUNCIO_PARADA', label:'Anunci de parada', code:'A', color:'yellow', order:'Preveure parada davant la senyal següent.' },
    ANUNCIO_PARADA_INMEDIATA: { id:'ANUNCIO_PARADA_INMEDIATA', label:'Anunci de parada immediata', code:'A*', color:'yellow', order:'Preveure parada davant la senyal següent a distància inferior a la de frenada.' },
    PARADA_SELECTIVA: { id:'PARADA_SELECTIVA', label:'Parada selectiva', code:'R/B*', color:'red', order:'Parada segons el sistema ERTMS en BCA i les condicions reglamentàries aplicables.' },
    PARADA: { id:'PARADA', label:'Parada', code:'R', color:'red', order:'Aturar-se davant la senyal i no ultrapassar-la.' },
    REBASE_AUTORIZADO: { id:'REBASE_AUTORIZADO', label:'Rebasament autoritzat', code:'R/B', color:'white', order:'Rebasar només amb les condicions i marxa prescrites; no equival a via lliure.' },
    MOVIMIENTO_AUTORIZADO: { id:'MOVIMIENTO_AUTORIZADO', label:'Moviment autoritzat', code:'M', color:'white', order:'Autorització limitada segons la funció de la senyal interior; no autoritza circular fins a l’estació següent.' }
  });
  const TYPES = Object.freeze({
    PREADVANCED:'Preavançada', ADVANCED:'Avançada', INTERMEDIATE:'Intermèdia / plena via', PROTECTION:'Protecció', ENTRY:'Entrada', ENTRY_INTERIOR:'Entrada interior', INTERIOR:'Interior', EXIT_INTERIOR:'Sortida interior', DESTINATION:'Destí / interior (denominació funcional)', EXIT:'Sortida', RETROCESS:'Retrocés', MANEUVER:'Maniobres', RELEASE:'Alliberament', LEVEL_CROSSING:'Protecció de pas a nivell', ROUTE_INDICATOR:'Indicadora d’itinerari'
  });
  const BASE = {
    PREADVANCED: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL'],
    ADVANCED: ['VIA_LIBRE','ANUNCIO_PRECAUCION','PREANUNCIO_PARADA','ANUNCIO_PARADA','PARADA'],
    INTERMEDIATE: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL','PREANUNCIO_PARADA','ANUNCIO_PARADA','PARADA'],
    PROTECTION: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL','ANUNCIO_PRECAUCION','PREANUNCIO_PARADA','ANUNCIO_PARADA','PARADA','REBASE_AUTORIZADO'],
    ENTRY: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL','ANUNCIO_PRECAUCION','PREANUNCIO_PARADA','ANUNCIO_PARADA','ANUNCIO_PARADA_INMEDIATA','PARADA','PARADA_SELECTIVA','REBASE_AUTORIZADO'],
    INTERIOR: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL','PREANUNCIO_PARADA','ANUNCIO_PARADA','ANUNCIO_PARADA_INMEDIATA','PARADA','PARADA_SELECTIVA','REBASE_AUTORIZADO','MOVIMIENTO_AUTORIZADO'],
    DESTINATION: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL','PREANUNCIO_PARADA','ANUNCIO_PARADA','ANUNCIO_PARADA_INMEDIATA','PARADA','PARADA_SELECTIVA','REBASE_AUTORIZADO','MOVIMIENTO_AUTORIZADO'],
    EXIT: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL','ANUNCIO_PRECAUCION','PREANUNCIO_PARADA','ANUNCIO_PARADA','ANUNCIO_PARADA_INMEDIATA','PARADA','PARADA_SELECTIVA','REBASE_AUTORIZADO'],
    RELEASE: ['VIA_LIBRE','VIA_LIBRE_CONDICIONAL','PARADA'],
    MANEUVER: ['PARADA','REBASE_AUTORIZADO','MOVIMIENTO_AUTORIZADO'],
    ENTRY_INTERIOR: ['VIA_LIBRE','PREANUNCIO_PARADA','ANUNCIO_PARADA','ANUNCIO_PARADA_INMEDIATA','PARADA','REBASE_AUTORIZADO','MOVIMIENTO_AUTORIZADO'],
    EXIT_INTERIOR: ['VIA_LIBRE','PREANUNCIO_PARADA','ANUNCIO_PARADA','ANUNCIO_PARADA_INMEDIATA','PARADA','REBASE_AUTORIZADO','MOVIMIENTO_AUTORIZADO'],
    RETROCESS: ['PARADA','REBASE_AUTORIZADO','MOVIMIENTO_AUTORIZADO'],
    LEVEL_CROSSING: ['PARADA','VIA_LIBRE'],
    ROUTE_INDICATOR: ['PARADA','MOVIMIENTO_AUTORIZADO']
  };
  const copy = x => JSON.parse(JSON.stringify(x));
  function allowedAspects(signal, infrastructure={}) {
    if (!signal || !BASE[signal.type]) return [];
    let ids = [...BASE[signal.type]];
    const vmax = Number(signal.vmax ?? infrastructure.vmax ?? 0);
    const block = String(signal.blockSystem || infrastructure.blockSystem || 'BA').toUpperCase();
    if (ids.includes('VIA_LIBRE_CONDICIONAL')) {
      let valid = vmax > 160 && block === 'BA' && signal.type !== 'PREADVANCED';
      if (block === 'BLA') valid = vmax > 160 && (
        signal.type === 'PREADVANCED' || signal.type === 'ADVANCED' ||
        (signal.type === 'ENTRY' && infrastructure.alsoPreadvancedOfNextStation === true) ||
        (signal.type === 'EXIT' && infrastructure.alsoAdvancedOfNextStation === true)
      );
      if (!valid) ids = ids.filter(x => x !== 'VIA_LIBRE_CONDICIONAL');
    }
    if (signal.type === 'PARADA_SELECTIVA' || ids.includes('PARADA_SELECTIVA')) {
      if (!(infrastructure.ertms === true && String(infrastructure.ertmsLevel || '') === 'BCA' && infrastructure.highSpeed === true)) ids = ids.filter(x => x !== 'PARADA_SELECTIVA');
    }
    if (signal.type === 'ADVANCED' && infrastructure.followingEntryHasReverseTurnout !== true) ids = ids.filter(x => x !== 'ANUNCIO_PRECAUCION');
    if (signal.type === 'PROTECTION' && (signal.alsoAdvanced !== true || infrastructure.followingEntryHasReverseTurnout !== true)) ids = ids.filter(x => x !== 'ANUNCIO_PRECAUCION');
    if (signal.type === 'ENTRY' && infrastructure.departureTurnoutReverse !== true) ids = ids.filter(x => x !== 'ANUNCIO_PRECAUCION');
    if (signal.type === 'EXIT') {
      const bab = block === 'BAB' && infrastructure.fromMainTrack === true && infrastructure.departureTurnoutReverse === true;
      const bsl = block === 'BSL' && infrastructure.releaseSignalEquipped === true;
      const advancedForNext = infrastructure.alsoAdvancedOfNextStation === true && infrastructure.nextSignalCanStop === true;
      if (!bab && !bsl && !advancedForNext) ids = ids.filter(x => x !== 'ANUNCIO_PARADA');
      if (infrastructure.alsoAdvancedOfNextStation !== true || infrastructure.followingEntryHasReverseTurnout !== true) ids = ids.filter(x => x !== 'ANUNCIO_PRECAUCION');
    }
    if (signal.type === 'ADVANCED' && (infrastructure.precededByStopCapableSignal !== true || block !== 'BA')) ids = ids.filter(x => x !== 'PARADA');
    if (signal.type === 'INTERMEDIATE' && block !== 'BA') ids = ids.filter(x => x !== 'PARADA');
    if (['INTERMEDIATE','PROTECTION','ADVANCED','INTERIOR','DESTINATION'].includes(signal.type) && infrastructure.nextSignalCanStop !== true) ids = ids.filter(x => x !== 'ANUNCIO_PARADA');
    if (signal.type === 'ENTRY' && infrastructure.nextSignalCanStop !== true && infrastructure.departureTurnoutReverse !== true) ids = ids.filter(x => x !== 'ANUNCIO_PARADA');
    if (['ENTRY','INTERIOR','DESTINATION','EXIT'].includes(signal.type) && infrastructure.nextSignalCanStop !== true) ids = ids.filter(x => x !== 'ANUNCIO_PARADA_INMEDIATA');
    return ids;
  }
  function validateSignal(input, infrastructure={}) {
    if (!input || typeof input !== 'object') return {ok:false,error:'Senyal invàlida.'};
    const id=String(input.id||'').trim(), name=String(input.name||'').trim(), type=String(input.type||'');
    if (!id || !name) return {ok:false,error:'La senyal necessita identificador i nom.'};
    if (!BASE[type]) return {ok:false,error:'Tipus de senyal desconegut.'};
    if(input.turnoutRequirements!==undefined&&!Array.isArray(input.turnoutRequirements))return{ok:false,error:'La configuració de desviaments ha de ser una llista.'};
    const turnoutRequirements=Array.isArray(input.turnoutRequirements)?input.turnoutRequirements.map(r=>({systemName:String(r?.systemName||'').trim(),expectedState:Number(r?.expectedState)})):[];if(turnoutRequirements.some(r=>!r.systemName||![2,4].includes(r.expectedState)))return{ok:false,error:'Cada desviament requer un system name i una posició esperada 2 (recte) o 4 (branca).'};if(new Set(turnoutRequirements.map(r=>r.systemName)).size!==turnoutRequirements.length)return{ok:false,error:'No es pot repetir el mateix desviament a la configuració de la senyal.'};
    const nextSignal1Id=String(input.nextSignal1Id||input.nextSignalId||'').trim(),nextSignal2Id=String(input.nextSignal2Id||'').trim();
    if(nextSignal1Id===id||nextSignal2Id===id)return{ok:false,error:'Una senyal no pot apuntar-se a si mateixa.'};
    if(nextSignal1Id&&nextSignal2Id&&nextSignal1Id===nextSignal2Id)return{ok:false,error:'Senyal 1 i Senyal 2 han de ser diferents.'};
    const signal={id,name,type,blockSystem:String(input.blockSystem||infrastructure.blockSystem||'BA').toUpperCase(),vmax:Number(input.vmax||infrastructure.vmax||120),alsoAdvanced:input.alsoAdvanced===true,head:String(input.head||'HIGH'),aspect:String(input.aspect||'PARADA'),blockIds:Array.isArray(input.blockIds)?[...new Set(input.blockIds.map(String))]:[],protectedBlockId:String(input.protectedBlockId||''),trackId:String(input.trackId||''),routeId:String(input.routeId||''),nextSignal1Id,nextSignal2Id,nextSignalId:nextSignal1Id,turnoutRequirements,enabled:input.enabled!==false};
    const allowed=allowedAspects(signal,infrastructure);
    if (!allowed.includes(signal.aspect)) signal.aspect=allowed.includes('PARADA')?'PARADA':(allowed[0]||'NO_ASPECT');
    return {ok:true,signal,allowedAspects:allowed};
  }
  function deriveAspect(signal, conditions={}, infrastructure={}) {
    const allowed=allowedAspects(signal,infrastructure);
    const stop=()=>({aspect:allowed.includes('PARADA')?'PARADA':'NO_ASPECT',reason:'Estat desconegut, conflicte o protecció no confirmada: criteri restrictiu. La senyal queda sense indicació permissiva.'});
    if (!signal || signal.enabled===false) return stop();
    if (conditions.emergency===true || conditions.occupied===true || conditions.blockState!=='FREE' || conditions.routeReserved!==true || conditions.turnoutsVerified!==true || conditions.turnoutsAligned!==true) return stop();
    if (conditions.authorisation==='REBASE' && allowed.includes('REBASE_AUTORIZADO') && conditions.explicitAuthorisation===true && conditions.protectionChecks===true) return {aspect:'REBASE_AUTORIZADO',reason:'Autorització explícita i comprovacions de protecció confirmades.'};
    if (conditions.authorisation==='MOVEMENT' && allowed.includes('MOVIMIENTO_AUTORIZADO') && conditions.explicitAuthorisation===true) return {aspect:'MOVIMIENTO_AUTORIZADO',reason:'Autorització explícita de moviment interior.'};
    if (conditions.nextAspect==='PARADA' && allowed.includes('ANUNCIO_PARADA_INMEDIATA') && conditions.shortBrakingDistance===true) return {aspect:'ANUNCIO_PARADA_INMEDIATA',reason:'La següent senyal ordena parada i la distància de frenada és insuficient.'};
    if (conditions.nextAspect==='PARADA' && allowed.includes('ANUNCIO_PARADA')) return {aspect:'ANUNCIO_PARADA',reason:'La següent senyal pot ordenar parada.'};
    if (conditions.turnoutReverse===true && allowed.includes('ANUNCIO_PRECAUCION')) return {aspect:'ANUNCIO_PRECAUCION',reason:'Agulles en posició desviada protegides per la següent senyal.'};
    if (conditions.nextAspect==='PREANUNCIO_PARADA' && allowed.includes('PREANUNCIO_PARADA')) return {aspect:'PREANUNCIO_PARADA',reason:'Seqüència de frenada configurada.'};
    if (conditions.nextAspect==='VIA_LIBRE_CONDICIONAL' && allowed.includes('VIA_LIBRE_CONDICIONAL')) return {aspect:'VIA_LIBRE_CONDICIONAL',reason:'Seqüència de distància de frenada condicional.'};
    if (allowed.includes('VIA_LIBRE')) return {aspect:'VIA_LIBRE',reason:'Itinerari lliure, reservat i desviaments verificats.'};
    return stop();
  }
  function createState(){return {signals:[],lastEvaluation:null};}
  function addSignal(state,input,infrastructure={}) {
    const checked=validateSignal(input,infrastructure); if(!checked.ok)return checked;
    if(state.signals.some(s=>s.id===checked.signal.id))return {ok:false,error:'Ja existeix una senyal amb aquest identificador.'};
    state.signals.push(checked.signal);return {ok:true,signal:checked.signal};
  }
  function setAspect(signal,aspect,infrastructure={}) {
    if(!allowedAspects(signal,infrastructure).includes(aspect))return {ok:false,error:'Aquest aspecte no és admissible per a aquest tipus de senyal o configuració de línia.'};
    signal.aspect=aspect;return {ok:true,signal};
  }
  const api={ASPECTS,TYPES,BASE,createState,allowedAspects,validateSignal,deriveAspect,addSignal,setAspect};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  root.JMRI_TC_SignallingCore=api;
})(typeof window!=='undefined'?window:globalThis);
