'use strict';
(function(root){
  const MODES=Object.freeze({OFF:'off',WITHOUT_CONSUMPTION:'without-consumption',WITH_CONSUMPTION:'with-consumption',DEPOT_REFUEL:'depot-refuel'});
  const PROFILES=Object.freeze({
    steam:{label:'Vapor',instruments:['Velocímetre','Pressió de caldera','Nivell d’aigua','Reserva de carbó','Sorra'],resources:{water:{label:'Aigua',unit:'%',rate:0.55},coal:{label:'Carbó',unit:'%',rate:0.32},sand:{label:'Sorra',unit:'%',rate:0.04},oil:{label:'Oli',unit:'%',rate:0.02}}},
    diesel:{label:'Dièsel',instruments:['Velocímetre','Revolucions del motor','Temperatura','Pressió d’oli','Combustible','Sorra'],resources:{fuel:{label:'Gasoil',unit:'%',rate:0.38},sand:{label:'Sorra',unit:'%',rate:0.04},oil:{label:'Oli',unit:'%',rate:0.015}}},
    electric:{label:'Elèctrica',instruments:['Velocímetre','Intensitat de tracció','Tensió','Temperatura del motor','Sorra'],resources:{sand:{label:'Sorra',unit:'%',rate:0.035},energy:{label:'Energia',unit:'%',rate:0.12}}}
  });
  const SUPPORTED_PROTOCOLS=Object.freeze(['mfx+','mfx','dcc','mm2','motorola','other']);
  const clamp=(v,min=0,max=100)=>Math.min(max,Math.max(min,Number(v)||0));
  function normalizeProtocol(loco){
    const raw=String(loco?.decoderProtocol||loco?.protocol||loco?.format||'other').toLowerCase().replace(/\s/g,'');
    if(raw==='mm'||raw==='motorola2'||raw==='mm2')return'mm2';
    return SUPPORTED_PROTOCOLS.includes(raw)?raw:'other';
  }
  function capabilities(loco){
    const protocol=normalizeProtocol(loco);
    const nativeMfxPlus=protocol==='mfx+'||loco?.mfxPlus===true;
    return {protocol,universalCab:true,virtualInstruments:true,virtualResourceSimulation:true,nativeMfxPlus,consumptionSource:nativeMfxPlus?'decoder-capability':'software-simulation'};
  }
  function profileFor(value){return PROFILES[value]?value:'diesel';}
  function createSession(loco,options={}){
    const profile=profileFor(options.profile||loco?.vehicleType||loco?.tractionType||'diesel');
    const mode=Object.values(MODES).includes(options.mode)?options.mode:MODES.WITHOUT_CONSUMPTION;
    const resources={};Object.entries(PROFILES[profile].resources).forEach(([key])=>resources[key]=100);
    return {locomotiveId:String(loco?.id??''),locomotiveName:String(loco?.name||'Locomotora'),profile,mode,throttle:0,resources,elapsedMinutes:0,active:mode!==MODES.OFF,capabilities:capabilities(loco),lastEvent:'Sessió virtual preparada.'};
  }
  function setMode(session,mode){
    if(!Object.values(MODES).includes(mode))return{ok:false,error:'Mode de joc no vàlid.'};
    return{ok:true,session:{...session,mode,active:mode!==MODES.OFF,lastEvent:'Mode virtual: '+mode+'.'}};
  }
  function setThrottle(session,value){return{...session,throttle:clamp(value),lastEvent:'Consigna virtual: '+Math.round(clamp(value))+'%.'};}
  function advance(session,minutes=1){
    const duration=Number(minutes);
    if(!Number.isFinite(duration)||duration<=0||duration>240)return{ok:false,error:'L’interval ha de ser entre 0 i 240 minuts.'};
    if(!session?.active)return{ok:false,error:'La sessió del Món de Jocs està aturada.'};
    if(session.mode===MODES.WITHOUT_CONSUMPTION||session.mode===MODES.OFF)return{ok:true,session:{...session,elapsedMinutes:session.elapsedMinutes+duration,lastEvent:'Temps de simulació avançat sense consum.'}};
    const resources={...session.resources},spec=PROFILES[profileFor(session.profile)].resources,used=[];
    Object.entries(spec).forEach(([key,resource])=>{const amount=resource.rate*(duration/60)*(session.throttle/100);resources[key]=clamp(resources[key]-amount);if(amount>0)used.push(resource.label);});
    const depleted=Object.keys(resources).filter(key=>resources[key]<=0);
    return{ok:true,session:{...session,resources,elapsedMinutes:session.elapsedMinutes+duration,lastEvent:depleted.length?'Recurs esgotat: '+depleted.join(', ')+'.':'Consum virtual actualitzat: '+(used.join(', ')||'cap recurs')+'.',depleted}};
  }
  function refill(session,{atDepot=false}={}){
    if(session.mode!==MODES.DEPOT_REFUEL)return{ok:false,error:'El repostatge només està disponible en el mode «Repostatge al dipòsit».'};
    if(atDepot!==true)return{ok:false,error:'Cal confirmar que la locomotora és al dipòsit de màquines.'};
    const resources={};Object.keys(PROFILES[profileFor(session.profile)].resources).forEach(key=>resources[key]=100);
    return{ok:true,session:{...session,resources,lastEvent:'Recursos de la simulació virtual reposats al dipòsit.'}};
  }
  function resourceStatus(session){
    return Object.entries(session?.resources||{}).map(([key,value])=>({key,label:PROFILES[profileFor(session.profile)].resources[key]?.label||key,value:clamp(value),status:value<=10?'critical':value<=25?'low':'ok'}));
  }
  const api={MODES,PROFILES,SUPPORTED_PROTOCOLS,normalizeProtocol,capabilities,createSession,setMode,setThrottle,advance,refill,resourceStatus};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;
  root.JMRI_TC_Spielewelt=api;
})(typeof window!=='undefined'?window:globalThis);
