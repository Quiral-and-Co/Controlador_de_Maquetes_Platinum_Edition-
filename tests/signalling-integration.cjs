const assert=require('node:assert/strict');
const bridge=require('../signalling-integration-core.js');
const signalling=require('../signalling-core.js');
const now=100000;
const config={signallingInfrastructure:{blockSystem:'BA',vmax:200},levels:[{blocks:[{id:'B1',verified:true,sensorIds:['S1']}],sensors:[{id:'S1',systemName:'LS1',verified:true,invert:false}],tracks:[]} ]};
const signal={id:'SIG1',name:'Entrada',type:'ENTRY',blockSystem:'BA',vmax:120,aspect:'PARADA',protectedBlockId:'B1',routeId:'R1',turnoutRequirements:[]};
const snapshot={blocks:[{id:'B1',verified:true,sensorIds:['S1']}],blockStates:{B1:'FREE'},routes:[{id:'R1',blockIds:['B1'] }],reservations:{B1:'R1'}};
const telemetry={connected:true,lastHeartbeatAt:now,power:'ON',powerUpdatedAt:now,sensors:{LS1:4},sensorUpdatedAt:{LS1:now},turnouts:{},turnoutUpdatedAt:{}};
function evaluate(overrides={}){return bridge.evaluateAll({config,signals:[signal],snapshot,telemetry,signallingCore:signalling,now,...overrides}).SIG1;}
assert.equal(evaluate().aspect,'VIA_LIBRE','fresh, verified, free and reserved block permits normal aspect');
assert.equal(evaluate({telemetry:{...telemetry,connected:false}}).aspect,'PARADA','lost JMRI connection fails safe');
assert.equal(evaluate({telemetry:{...telemetry,power:'OFF'}}).aspect,'PARADA','JMRI track power off fails safe');
assert.equal(evaluate({telemetry:{...telemetry,power:'UNKNOWN'}}).aspect,'PARADA','unknown JMRI power state fails safe');
assert.equal(evaluate({telemetry:{...telemetry,powerUpdatedAt:now-16000}}).aspect,'PARADA','stale JMRI power state fails safe');
assert.equal(evaluate({telemetry:{...telemetry,lastHeartbeatAt:now-16000}}).aspect,'PARADA','stale heartbeat fails safe');
assert.equal(evaluate({telemetry:{...telemetry,sensorUpdatedAt:{LS1:now-16000}}}).blockState,'UNKNOWN','stale sensor reading becomes unknown');
assert.equal(evaluate({telemetry:{...telemetry,sensorUpdatedAt:{}}}).aspect,'PARADA','missing sensor timestamp fails safe');
assert.equal(evaluate({telemetry:{...telemetry,sensors:{LS1:'bad'}}}).blockState,'UNKNOWN','invalid sensor value becomes unknown');
assert.equal(evaluate({telemetry:{...telemetry,sensors:{LS1:2}}}).aspect,'PARADA','occupied block forces stop');
assert.equal(evaluate({snapshot:{...snapshot,reservations:{}}}).aspect,'PARADA','lost route reservation forces stop');
assert.equal(evaluate({snapshot:{...snapshot,blocks:[{id:'B1',verified:false,sensorIds:['S1']}]}}).aspect,'PARADA','unverified block forces stop');
assert.equal(evaluate({snapshot:{...snapshot,routes:[]}}).aspect,'PARADA','missing route forces stop');
const checkedTurnout=signalling.validateSignal({...signal,turnoutRequirements:[{systemName:'LT1',expectedState:2}]},config.signallingInfrastructure);assert.equal(checkedTurnout.ok,true,'turnout requirements persist through signal validation');
assert.equal(signalling.validateSignal({...signal,turnoutRequirements:[{systemName:'LT1',expectedState:3}]},config.signallingInfrastructure).ok,false,'invalid turnout positions are rejected');
const turnoutConfig={...config,levels:[{...config.levels[0],tracks:[{id:'T1',type:'turnout',blockId:'B1',turnoutSystemName:'LT1'}]}]};
assert.equal(bridge.evaluateAll({config:turnoutConfig,signals:[signal],snapshot,telemetry,signallingCore:signalling,now}).SIG1.aspect,'PARADA','unmapped turnout in protected block fails safe');
const mappedConfig={...config,levels:[{...config.levels[0],tracks:[{id:'T1',type:'turnout',blockId:'B1',turnoutSystemName:'LT1'}]}]};
const reqSignal={...signal,turnoutRequirements:[{systemName:'LT1',expectedState:2}]};
const catalogue={getDatabase:()=>({accessories:[{type:'TURNOUT',systemName:'LT1',verified:true}]})};
global.JMRI_TC_DIGITAL_CATALOGUE=catalogue;
const turnoutTelemetry={...telemetry,turnouts:{LT1:4},turnoutUpdatedAt:{LT1:now}};
assert.equal(bridge.evaluateAll({config:mappedConfig,signals:[reqSignal],snapshot,telemetry:turnoutTelemetry,signallingCore:signalling,now}).SIG1.aspect,'PARADA','misaligned turnout fails safe');
const aligned={...turnoutTelemetry,turnouts:{LT1:2}};
assert.equal(bridge.evaluateAll({config:mappedConfig,signals:[reqSignal],snapshot,telemetry:aligned,signallingCore:signalling,now}).SIG1.aspect,'VIA_LIBRE','verified aligned turnout permits aspect');
assert.equal(bridge.evaluateAll({config,signals:[signal],snapshot,telemetry,signallingCore:signalling,now,emergency:true}).SIG1.aspect,'PARADA','emergency forces stop');
const cycleA={...signal,id:'A',nextSignalId:'B'},cycleB={...signal,id:'B',nextSignalId:'A'};
const cyclic=bridge.evaluateAll({config,signals:[cycleA,cycleB],snapshot,telemetry,signallingCore:signalling,now});
assert.equal(cyclic.A.aspect,'PARADA','signal dependency cycle fails safe');
const brokenNext={...signal,nextSignalId:'MISSING'};assert.equal(bridge.evaluateAll({config,signals:[brokenNext],snapshot,telemetry,signallingCore:signalling,now}).SIG1.aspect,'PARADA','missing next signal reference fails safe');
const routeConfig={...config,levels:[{...config.levels[0],blocks:[...config.levels[0].blocks,{id:'B2',verified:true,sensorIds:['S1']}],tracks:[{id:'T1',type:'turnout',blockId:'B1',turnoutSystemName:'LT1'},{id:'T2',type:'turnout',blockId:'B2',turnoutSystemName:'LT2'}]}],dispatcher:{routes:[{id:'R1',blockIds:['B1','B2']}]}};
const routeSnapshot={...snapshot,blocks:[...snapshot.blocks,{id:'B2',verified:true,sensorIds:['S1']}],routes:[{id:'R1',blockIds:['B1','B2']}],reservations:{B1:'R1',B2:'R1'}};
global.JMRI_TC_DIGITAL_CATALOGUE={getDatabase:()=>({accessories:[{type:'TURNOUT',systemName:'LT1',verified:true},{type:'TURNOUT',systemName:'LT2',verified:true}]})};
const routeSignal={...signal,turnoutRequirements:[{systemName:'LT1',expectedState:2},{systemName:'LT2',expectedState:4}]};
const routeTelemetry={...telemetry,turnouts:{LT1:2,LT2:4},turnoutUpdatedAt:{LT1:now,LT2:now}};
assert.equal(bridge.evaluateAll({config:routeConfig,signals:[routeSignal],snapshot:routeSnapshot,telemetry:routeTelemetry,signallingCore:signalling,now}).SIG1.aspect,'VIA_LIBRE','turnout on a later route block is checked and aligned');
assert.equal(bridge.evaluateAll({config:routeConfig,signals:[routeSignal],snapshot:routeSnapshot,telemetry:{...routeTelemetry,turnouts:{LT1:2}},signallingCore:signalling,now}).SIG1.aspect,'PARADA','missing later-route turnout feedback forces stop');
console.log('PASS: signalling SVG/Dispatcher integration fail-safe core');

const sequenceConfig={...config,dispatcher:{routes:[{id:'R1',blockIds:['B1','B2','B3']}]},levels:[{...config.levels[0],blocks:[...config.levels[0].blocks,{id:'B2',verified:true,sensorIds:['S1']},{id:'B3',verified:true,sensorIds:['S1']}]}]};
const sequenceSnapshot={...snapshot,blocks:[...snapshot.blocks,{id:'B2',verified:true,sensorIds:['S1']},{id:'B3',verified:true,sensorIds:['S1']}],routes:[{id:'R1',blockIds:['B1','B2','B3']}],reservations:{B1:'R1',B2:'R1',B3:'R1'}};
const s2={...signal,id:'SIG2',protectedBlockId:'B2',nextSignal1Id:'SIG3'},s3={...signal,id:'SIG3',protectedBlockId:'B3'};const ordered=bridge.evaluateAll({config:sequenceConfig,signals:[{...signal,nextSignal1Id:'SIG2',nextSignal2Id:'SIG3'},s2,s3],snapshot:sequenceSnapshot,telemetry,signallingCore:signalling,now});assert.equal(ordered.SIG1.aspect,'VIA_LIBRE','valid ordered first and second next signals preserve normal evaluation');const badOrder=bridge.evaluateAll({config:sequenceConfig,signals:[{...signal,nextSignal1Id:'SIG2',nextSignal2Id:'SIG3'},{...s2,nextSignal1Id:''},s3],snapshot:sequenceSnapshot,telemetry,signallingCore:signalling,now});assert.equal(badOrder.SIG1.aspect,'PARADA','second next signal must follow signal 1');const missing2=bridge.evaluateAll({config:sequenceConfig,signals:[{...signal,nextSignal1Id:'SIG2',nextSignal2Id:'MISSING'},s2],snapshot:sequenceSnapshot,telemetry,signallingCore:signalling,now});assert.equal(missing2.SIG1.aspect,'PARADA','missing signal 2 fails safe');

// Recovery is a state transition: failure first, then fresh evidence and reevaluation.
const disconnected=evaluate({telemetry:{...telemetry,connected:false}});
assert.equal(disconnected.aspect,'PARADA','communication loss immediately forces stop');
const recoveryNow=now+1000;
const restoredTelemetry={...telemetry,lastHeartbeatAt:recoveryNow,sensorUpdatedAt:{LS1:recoveryNow},powerUpdatedAt:recoveryNow};
const recoveredCommunication=evaluate({now:recoveryNow,telemetry:restoredTelemetry});
assert.equal(recoveredCommunication.aspect,'VIA_LIBRE','fresh heartbeat after prior communication loss permits reevaluation');

const unknownSensor=evaluate({telemetry:{...telemetry,sensors:{LS1:'UNRECOGNISED'}}});
assert.equal(unknownSensor.blockState,'UNKNOWN','unknown sensor encoding is never interpreted as free');
assert.equal(unknownSensor.aspect,'PARADA','unknown sensor encoding forces stop');
const recoveredSensor=evaluate({now:recoveryNow,telemetry:{...restoredTelemetry,sensors:{LS1:4}}});
assert.equal(recoveredSensor.blockState,'FREE','new valid free sensor reading restores known state');
assert.equal(recoveredSensor.aspect,'VIA_LIBRE','signal recovers only after a fresh valid free sensor reading');

const failedPower=evaluate({telemetry:{...telemetry,power:'OFF'}});
assert.equal(failedPower.aspect,'PARADA','power OFF establishes failed initial state');
const recoveredPower=evaluate({now:recoveryNow,telemetry:{...restoredTelemetry,power:'ON'}});
assert.equal(recoveredPower.aspect,'VIA_LIBRE','new confirmed power ON permits reevaluation after power failure');

const missingSequence=[{...signal,nextSignal1Id:'MISSING',nextSignal2Id:''}];
const failedSequence=bridge.evaluateAll({config,signals:missingSequence,snapshot,telemetry,signallingCore:signalling,now});
assert.equal(failedSequence.SIG1.aspect,'PARADA','missing signal reference starts in restrictive state');
const repairedSequence=bridge.evaluateAll({config:sequenceConfig,signals:[{...signal,nextSignal1Id:'SIG2',nextSignal2Id:'SIG3'},s2,s3],snapshot:sequenceSnapshot,telemetry,signallingCore:signalling,now});
assert.equal(repairedSequence.SIG1.aspect,'VIA_LIBRE','repaired Senyal 1/Senyal 2 chain recovers after reevaluation');

const physicalChain=[{...signal,nextSignal1Id:'SIG2',nextSignal2Id:'SIG3'}, {...s2,protectedBlockId:'B2'}, {...s3,protectedBlockId:'B3'}];
const activeOrdered=bridge.evaluateAll({config:sequenceConfig,signals:physicalChain,snapshot:sequenceSnapshot,telemetry,signallingCore:signalling,now});
assert.equal(activeOrdered.SIG1.aspect,'VIA_LIBRE','active integration accepts physically ordered signals');
const activeWrongDirection=bridge.evaluateAll({config:sequenceConfig,signals:[{...physicalChain[0],direction:'REVERSE'},physicalChain[1],physicalChain[2]],snapshot:sequenceSnapshot,telemetry,signallingCore:signalling,now});
assert.equal(activeWrongDirection.SIG1.aspect,'PARADA','active integration rejects sequence inconsistent with travel direction');

