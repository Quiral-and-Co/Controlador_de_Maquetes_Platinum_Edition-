'use strict';
(function(root){
  const MAX_AGE_MS=15000, FUTURE_TOLERANCE_MS=1000;
  const sequenceCore=root.JMRI_TC_SignallingSequenceCore||
    (typeof module!=='undefined'&&module.exports?require('./signalling-sequence-core.js'):null);
  const finiteTime=v=>Number.isFinite(Number(v))?Number(v):null;
  function fresh(timestamp,now,maxAge=MAX_AGE_MS){
    const t=finiteTime(timestamp);
    return t!==null&&t<=now+FUTURE_TOLERANCE_MS&&now-t<=maxAge;
  }
  function sensorBlockState(block,config,telemetry,now,maxAge){
    if(!block||block.verified!==true||!Array.isArray(block.sensorIds)||!block.sensorIds.length)return'UNKNOWN';
    const sensors=(config.levels||[]).flatMap(l=>l.sensors||[]);
    const defs=block.sensorIds.map(id=>sensors.find(s=>s.id===id));
    if(defs.some(s=>!s||s.verified!==true||!s.systemName||!fresh(telemetry.sensorUpdatedAt?.[s.systemName],now,maxAge)))return'UNKNOWN';
    const values=defs.map(s=>{
      let v=telemetry.sensors?.[s.systemName];
      if(v&&typeof v==='object')v=v.state;
      let status='UNKNOWN';
      if(v===2||v==='2'||['ACTIVE','OCCUPIED','ON','TRUE'].includes(String(v).trim().toUpperCase()))status='OCCUPIED';
      else if(v===4||v==='4'||['INACTIVE','FREE','CLEAR','OFF','FALSE'].includes(String(v).trim().toUpperCase()))status='FREE';
      if(s.invert&&status!=='UNKNOWN')status=status==='FREE'?'OCCUPIED':'FREE';
      return status;
    });
    if(values.includes('OCCUPIED'))return'OCCUPIED';
    return values.length&&values.every(v=>v==='FREE')?'FREE':'UNKNOWN';
  }
  function turnoutChecks(signal,config,telemetry,now,maxAge){
    const requirements=Array.isArray(signal.turnoutRequirements)?signal.turnoutRequirements:[];
    const allTracks=(config.levels||[]).flatMap(l=>l.tracks||[]),route=(config.dispatcher?.routes||[]).find(r=>r.id===signal.routeId);
    const routeBlocks=new Set(route?.blockIds||[signal.protectedBlockId]);
    const blockTurnouts=allTracks.filter(p=>routeBlocks.has(p.blockId)&&p.type==='turnout');
    if(blockTurnouts.length&&!requirements.length)return{verified:false,aligned:false,reverse:false,reason:'Hi ha desviaments al bloc protegit però la senyal no té posicions requerides configurades.'};
    const mappedNames=new Set(blockTurnouts.map(p=>p.turnoutSystemName));
    if(requirements.some(r=>!mappedNames.has(r.systemName))||blockTurnouts.some(p=>!requirements.some(r=>r.systemName===p.turnoutSystemName)))return{verified:false,aligned:false,reverse:false,reason:'Les posicions requerides no coincideixen exactament amb els desviaments assignats al bloc protegit.'};
    const catalogue=root.JMRI_TC_DIGITAL_CATALOGUE?.getDatabase?.().accessories||[];
    for(const req of requirements){
      const accessory=catalogue.find(a=>a.type==='TURNOUT'&&a.systemName===req.systemName);
      if(!accessory||accessory.verified!==true||!fresh(telemetry.turnoutUpdatedAt?.[req.systemName],now,maxAge))return{verified:false,aligned:false,reverse:false,reason:'Desviament no verificat o lectura obsoleta: '+String(req.systemName||'desconegut')};
      const actual=Number(telemetry.turnouts?.[req.systemName]);
      if(actual!==2&&actual!==4)return{verified:false,aligned:false,reverse:false,reason:'Estat de desviament desconegut: '+req.systemName};
      if(Number(req.expectedState)!==2&&Number(req.expectedState)!==4)return{verified:false,aligned:false,reverse:false,reason:'Posició requerida no configurada: '+req.systemName};
      if(actual!==Number(req.expectedState))return{verified:true,aligned:false,reverse:false,reason:'Desviament fora de la posició requerida: '+req.systemName};
    }
    return{verified:true,aligned:true,reverse:requirements.some(r=>Number(r.expectedState)===4),reason:''};
  }
  function evaluateAll(input){
    const now=Number.isFinite(input.now)?input.now:Date.now(),maxAge=Number.isFinite(input.maxAgeMs)?input.maxAgeMs:MAX_AGE_MS;
    const config=input.config||{},signals=Array.isArray(input.signals)?input.signals:[],snapshot=input.snapshot||{},telemetry=input.telemetry||{};
    const core=input.signallingCore;
    if(!core)throw new Error('Motor de senyalització no disponible.');
    const routeDefinitions=(snapshot.routes||[]).length?snapshot.routes:(config.dispatcher?.routes||[]);
    const sequenceResults=sequenceCore?sequenceCore.inspect(signals,routeDefinitions):null;
    const communication=telemetry.connected===true&&fresh(telemetry.lastHeartbeatAt,now,maxAge);
    const results={},visiting=new Set();
    function calculate(signal){
      if(!signal)return{aspect:'PARADA',reason:'Senyal següent inexistent: criteri restrictiu.'};
      if(results[signal.id])return results[signal.id];
      if(visiting.has(signal.id))return{aspect:'PARADA',reason:'Cicle de dependències entre senyals: criteri restrictiu.',cycle:true};
      visiting.add(signal.id);
      const blockId=signal.protectedBlockId||'',block=(snapshot.blocks||[]).find(b=>b.id===blockId);
      const blockState=communication?sensorBlockState(block,config,telemetry,now,maxAge):'UNKNOWN';
      const route=(snapshot.routes||[]).find(r=>r.id===signal.routeId);
      const routeReserved=!!route&&route.blockIds?.length>0&&route.blockIds.includes(blockId)&&route.blockIds.every(id=>snapshot.reservations?.[id]===route.id);
      const turnouts=communication?turnoutChecks(signal,config,telemetry,now,maxAge):{verified:false,aligned:false,reverse:false,reason:'Comunicació JMRI absent o heartbeat obsolet.'};
      const next1Id=signal.nextSignal1Id||signal.nextSignalId||'';
      const next2Id=signal.nextSignal2Id||'';
      const next=signals.find(s=>s.id===next1Id),next2=signals.find(s=>s.id===next2Id);
      const missingNext=!!next1Id&&!next;
      const missingNext2=!!next2Id&&!next2;
      const invalidNextOrder=!!next2Id&&(!next||!next2||String(next.nextSignal1Id||next.nextSignalId||'')!==next2Id);
      const nextResult=next?calculate(next):null;
      const next2Result=next2?calculate(next2):null;
      const conditions={
        blockState,occupied:blockState==='OCCUPIED',routeReserved,
        turnoutsVerified:turnouts.verified,turnoutsAligned:turnouts.aligned,
        emergency:input.emergency===true||telemetry.power!=='ON'||!fresh(telemetry.powerUpdatedAt,now,maxAge),nextAspect:next?(nextResult?.aspect||'PARADA'):'',
        turnoutReverse:turnouts.reverse,shortBrakingDistance:signal.shortBrakingDistance===true,
        authorisation:signal.authorisation||'',explicitAuthorisation:signal.explicitAuthorisation===true,
        protectionChecks:signal.protectionChecks===true
      };
      const derived=core.deriveAspect(signal,conditions,config.signallingInfrastructure||{});
      const cycle=nextResult?.cycle===true||next2Result?.cycle===true;
      const sequenceCheck=sequenceResults?.[signal.id];
      const sequenceUnverified=!!sequenceCheck&&sequenceCheck.warnings.length>0;
      const sequenceInvalid=!!sequenceCheck&&(!sequenceCheck.ok||sequenceUnverified);
      const brokenSequence=missingNext||missingNext2||invalidNextOrder||sequenceInvalid;
      const result={aspect:cycle||brokenSequence?'PARADA':derived.aspect,cycle:cycle||brokenSequence,reason:cycle?'Cicle de dependències entre senyals: criteri restrictiu.':brokenSequence?(sequenceCheck?.issues?.join(' ')||sequenceCheck?.warnings?.join(' ')||'Relació Senyal 1 / Senyal 2 absent, inexistent o fora d’ordre: criteri restrictiu.'):!communication?'Comunicació JMRI perduda o dades obsoletes: '+derived.reason:(blockState!=='FREE'?'Bloc no confirmat com a lliure: '+derived.reason:(!routeReserved?'Itinerari no reservat íntegrament: '+derived.reason:(!turnouts.verified||!turnouts.aligned?turnouts.reason||derived.reason:derived.reason))),blockState,routeReserved,turnoutsVerified:turnouts.verified,turnoutsAligned:turnouts.aligned,computedAt:now};
      visiting.delete(signal.id);results[signal.id]=result;return result;
    }
    for(const signal of signals)calculate(signal);
    return results;
  }
  const api={MAX_AGE_MS,fresh,sensorBlockState,turnoutChecks,evaluateAll};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  root.JMRI_TC_SignallingIntegrationCore=api;
})(typeof window!=='undefined'?window:globalThis);