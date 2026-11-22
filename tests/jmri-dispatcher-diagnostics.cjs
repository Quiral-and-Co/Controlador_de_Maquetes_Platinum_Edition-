'use strict';
const assert=require('node:assert/strict');
const core=require('../jmri-dispatcher-diagnostics-core.js');
const now=100000;
const config={levels:[{id:1,name:'Nivell 1',sensors:[
{id:'S1',systemName:'LS101',verified:true},{id:'S2',systemName:'LS102',verified:false}
],blocks:[
{id:'B01',verified:true,sensorIds:['S1']},{id:'B02',verified:true,sensorIds:['S2']},{id:'B03',verified:true,sensorIds:['MISSING']}
],tracks:[{id:'T1',type:'turnout',turnoutSystemName:'LT1'}]}],dispatcher:{routes:[{id:'R1',name:'Ruta nord',blockIds:['B01','B02']}]}};
const telemetry={connected:true,lastHeartbeatAt:now,sensors:{LS101:4,LS102:4},sensorUpdatedAt:{LS101:now,LS102:now},turnouts:{LT1:2},turnoutUpdatedAt:{LT1:now}};
const accessories=[{type:'TURNOUT',systemName:'LT1',verified:true}];
let result=core.inspect(config,telemetry,{routes:[],blockStates:{B01:'FREE',B02:'UNKNOWN',B03:'UNKNOWN'}},now,undefined,accessories);
assert.equal(result.connected,true);
assert.equal(result.validSensorCount,1);
assert.equal(result.unknownBlocks,2);
assert.ok(result.incidents.some(x=>x.code==='SENSOR_UNVERIFIED'));
assert.equal(result.incidents.some(x=>x.code==='TURNOUT_UNKNOWN'),false);
result=core.inspect(config,{...telemetry,sensorUpdatedAt:{LS101:now-20000,LS102:now}}, {routes:[],blockStates:{B01:'UNKNOWN',B02:'UNKNOWN',B03:'UNKNOWN'}},now,undefined,accessories);
assert.equal(result.validSensorCount,0);
assert.ok(result.incidents.some(x=>x.code==='SENSOR_STALE'));
result=core.inspect(config,{...telemetry,connected:false},{routes:[],blockStates:{B01:'UNKNOWN',B02:'UNKNOWN',B03:'UNKNOWN'}},now,undefined,accessories);
assert.equal(result.connected,false);
assert.ok(result.incidents.some(x=>x.code==='JMRI_DISCONNECTED'));
assert.ok(result.incidents.some(x=>x.code==='TURNOUT_UNKNOWN'));
result=core.inspect(config,telemetry,{routes:[{id:'R1',name:'Ruta nord',blockIds:['B01','B02']}],blockStates:{B01:'FREE',B02:'UNKNOWN',B03:'UNKNOWN'}},now,undefined,accessories);
assert.ok(result.incidents.some(x=>x.code==='ROUTE_NOT_CLEAR'));
assert.equal(core.isFresh(now,now),true);
assert.equal(core.isFresh(now-20000,now),false);
console.log('PASS: JMRI live diagnostics, stale-data rejection, turnout status and route incident reporting');

const unverified=core.inspect(config,telemetry,{routes:[],blockStates:{}},now,undefined,[]);
assert.ok(unverified.incidents.some(x=>x.code==='TURNOUT_UNVERIFIED'));
