'use strict';
(function(root){
 const VERSION=1, PREFIX='JMRI-TC-VMR';
 const TYPES=Object.freeze(['signal','block','sensor','turnout']);
 const labels={signal:'Senyal',block:'Bloc',sensor:'Sensor',turnout:'Desviament'};
 const list=v=>Array.isArray(v)?v:[];
 let actSequence=0;
 function ensureProjectId(config={},createId){if(!config.projectId){config.projectId=String(createId?createId():config.validationMasterRegistry?.projectId||('JMRI-TC-PROJECT-'+nowIso().replace(/[-:.TZ]/g,'')));}return String(config.projectId);}
 function storageKey(projectId){return 'jmri-tc-validation-master-registry-v1:'+encodeURIComponent(String(projectId));}
 const nowIso=now=>new Date(now===undefined?Date.now():now).toISOString();
 function entitySource(config={}) {
  const out=[];
  for(const s of list(config.signals)) if(s&&s.id!==undefined) out.push({type:'signal',sourceId:String(s.id),name:String(s.name||s.id)});
  for(const level of list(config.levels)) {
   for(const b of list(level.blocks)) if(b&&b.id!==undefined) out.push({type:'block',sourceId:String(b.id),name:String(b.name||b.id),levelId:level.id,levelName:level.name||('Nivell '+level.id)});
   for(const s of list(level.sensors)) if(s&&s.id!==undefined) out.push({type:'sensor',sourceId:String(s.id),name:String(s.userName||s.systemName||s.id),systemName:String(s.systemName||''),levelId:level.id,levelName:level.name||('Nivell '+level.id)});
   for(const t of list(level.tracks)) if(t&&t.type==='turnout'&&t.turnoutSystemName) out.push({type:'turnout',sourceId:String(t.turnoutSystemName),name:String(t.name||t.turnoutSystemName),trackId:t.id,levelId:level.id,levelName:level.name||('Nivell '+level.id)});
  }
  return out;
 }
 function createState(seed={}) {
  const records=Array.isArray(seed.records)?seed.records.map(r=>({...r})):[],audit=Array.isArray(seed.audit)?seed.audit.map(a=>({...a})):[];
  const counters={...(seed.counters||{})};
  // Rebuild missing/old counters from persisted IDs before allocating new IDs.
  for(const record of records) {
   const match=String(record.registryId||'').match(/^JMRI-TC-VMR-([A-Z]+)-([0-9]+)$/);
   if(!match)continue;
   const type=match[1].toLowerCase(),value=Number(match[2]);
   if(Number.isSafeInteger(value))counters[type]=Math.max(Number(counters[type])||0,value);
  }
  return {schemaVersion:VERSION,projectId:String(seed.projectId||'default'),createdAt:seed.createdAt||nowIso(),updatedAt:seed.updatedAt||nowIso(),counters,records,audit};
 }
 function nextId(state,type) {
  const n=(Number(state.counters[type])||0)+1;state.counters[type]=n;
  return PREFIX+'-'+type.toUpperCase()+'-'+String(n).padStart(5,'0');
 }
 function addAudit(state,action,record,details={},now) {
  state.audit.push({eventId:PREFIX+'-EVT-'+String(state.audit.length+1).padStart(6,'0'),action,recordId:record?.registryId||null,entityType:record?.type||null,sourceId:record?.sourceId||null,at:nowIso(now),details});
  state.updatedAt=nowIso(now);
 }
 function reconcile(state,config,now) {
  const seen=new Set(),added=[];
  for(const e of entitySource(config)) {
   const key=e.type+':'+e.sourceId;seen.add(key);
   let r=state.records.find(x=>x.identityKey===key);
   if(!r){r={registryId:nextId(state,e.type),identityKey:key,type:e.type,sourceId:e.sourceId,createdAt:nowIso(now),status:'PENDING',checks:[],notes:[]};state.records.push(r);added.push(r);addAudit(state,'REGISTERED',r,{name:e.name},now);}
   Object.assign(r,{name:e.name,levelId:e.levelId??null,levelName:e.levelName??null,systemName:e.systemName||'',trackId:e.trackId||null,present:true});
  }
  state.records.forEach(r=>{if(!seen.has(r.identityKey)&&r.present!==false){r.present=false;addAudit(state,'SOURCE_REMOVED',r,{},now);}});
  state.updatedAt=nowIso(now);return added;
 }
 function updateValidation(state,config,physicalCore,now) {
  const snapshot=physicalCore?physicalCore.inspect(config):{entries:[]};
  const byKey=new Map(snapshot.entries.map(e=>[e.type+':'+e.id,e]));
  for(const r of state.records) {
   const e=byKey.get(r.identityKey);if(!e)continue;
   const previous=r.status;
   r.status=e.status;r.failureReason=e.failureReason||'';
   r.validation={missing:[...(e.missing||[])],missingSoftware:[...(e.missingSoftware||[])],missingPhysical:[...(e.missingPhysical||[])],checks:(e.checks||[]).map(c=>({id:c.id,kind:c.kind,checked:c.checked}))};
   r.reviewedAt=nowIso(now);
   if(previous!==r.status)addAudit(state,'STATUS_CHANGED',r,{from:previous,to:r.status},now);
  }
  state.updatedAt=nowIso(now);return state;
 }
 function appendNote(state,registryId,note,author='operator',now) {
  const r=state.records.find(x=>x.registryId===registryId);if(!r)return{ok:false,error:'Identificador VMR inexistent.'};
  const value=String(note||'').trim();if(!value)return{ok:false,error:'La nota no pot estar buida.'};
  const item={noteId:PREFIX+'-NOTE-'+String(state.audit.length+1).padStart(6,'0'),text:value,author:String(author||'operator'),at:nowIso(now)};
  r.notes=list(r.notes);r.notes.push(item);addAudit(state,'NOTE_ADDED',r,{noteId:item.noteId},now);return{ok:true,note:item};
 }
 function buildAct(state,config={},options={}) {
  const entries=state.records.filter(r=>r.present!==false).map(r=>({...r})).sort((a,b)=>a.type.localeCompare(b.type)||a.registryId.localeCompare(b.registryId));
  const counts={total:entries.length,pending:0,failed:0,softwareReady:0,physicalReady:0};
  for(const r of entries){if(r.status==='FAILED')counts.failed++;else if(r.status==='PENDING_SOFTWARE'||r.status==='PENDING')counts.pending++;else if(r.status==='SOFTWARE_READY')counts.softwareReady++;else if(r.status==='PHYSICAL_READY')counts.physicalReady++;}
  const actId=PREFIX+'-ACT-'+nowIso(options.now).replace(/[-:.TZ]/g,'')+'-'+String(++actSequence).padStart(4,'0');
  const lines=['# ACTA DE VALIDACIÓ · '+(options.projectName||config.name||'JMRI TC Gold Custom'),'','- Acta: '+actId,'- Projecte: '+(options.projectId||state.projectId),'- Data: '+nowIso(options.now),'- Versió del registre: '+VERSION,'','## Resum','', '| Total | Pendents de programari | Preparats per inspecció física | Validació física registrada | No conformitats |','|---:|---:|---:|---:|---:|','| '+[counts.total,counts.pending,counts.softwareReady,counts.physicalReady,counts.failed].join(' | ')+' |','','## Registre individual','','| Identificador VMR | Tipus | Element | Estat | Comprovacions pendents |','|---|---|---|---|---|'];
  const cell=value=>String(value??'').replaceAll('|','\\|').replaceAll('\r',' ').replaceAll('\n',' ');
  entries.forEach(r=>lines.push('| '+cell(r.registryId)+' | '+cell(labels[r.type]||r.type)+' | '+cell(r.name||r.sourceId)+' ('+cell(r.sourceId)+') | '+cell(r.status)+' | '+cell((r.validation?.missing||[]).join(', '))+' |'));
  lines.push('','## Anotacions per element','');
  entries.forEach(r=>{if(list(r.notes).length){lines.push('### '+cell(r.registryId)+' · '+cell(r.name||r.sourceId));r.notes.forEach(n=>lines.push('- '+cell(n.at)+' · '+cell(n.author)+': '+cell(n.text)));lines.push('');}});
  lines.push('## Historial d’auditoria','','| Esdeveniment | Data | Acció | Element | Detall |','|---|---|---|---|---|');
  state.audit.forEach(event=>lines.push('| '+cell(event.eventId)+' | '+cell(event.at)+' | '+cell(event.action)+' | '+cell(event.recordId||'—')+' | '+cell(JSON.stringify(event.details||{}))+' |'));
  lines.push('','## Traçabilitat','','Esdeveniments registrats: '+state.audit.length+'. L’historial identifica altes, retirades de font, canvis d’estat i anotacions.','', '## Abast i reserva de seguretat','','Aquesta acta reflecteix únicament dades de programari i declaracions introduïdes al registre. No és una certificació de seguretat ni acredita mesures instrumentals. L’estat PHYSICAL_READY significa que les comprovacions físiques s’han marcat al sistema, no que s’hagin verificat independentment.','', '**DCC, sortides físiques, desviaments comandats i automatització continuen BLOQUEJATS.** Cap registre ni acta autoritza l’activació. Cal completar i revisar per separat les proves fail-safe, el cablejat i les proteccions elèctriques, i la parada d’emergència física independent.','');
  return{id:actId,createdAt:nowIso(options.now),counts,entries,audit:[...state.audit],markdown:lines.join('\n')};
 }
 function serialize(state){return JSON.stringify(state,null,2);}
 function load(storage,key='jmri-tc-validation-master-registry') {
  if(!storage||typeof storage.getItem!=='function')return createState();
  try{const raw=storage.getItem(key);return raw?createState(JSON.parse(raw)):createState();}catch(_){return createState();}
 }
 function save(state,storage,key='jmri-tc-validation-master-registry') {
  if(!storage||typeof storage.setItem!=='function')return{ok:false,error:'Persistència no disponible.'};
  try{storage.setItem(key,serialize(state));return{ok:true};}catch(e){return{ok:false,error:String(e?.message||e)};}
 }
 const api={VERSION,PREFIX,TYPES,entitySource,ensureProjectId,storageKey,createState,reconcile,updateValidation,appendNote,buildAct,serialize,load,save};
 if(typeof module!=='undefined'&&module.exports)module.exports=api;
 root.JMRI_TC_ValidationMasterRegistry=api;
})(typeof window!=='undefined'?window:globalThis);
