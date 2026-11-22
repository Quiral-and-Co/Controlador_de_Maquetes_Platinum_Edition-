'use strict';
const assert=require('node:assert/strict');
const model=require('../layout-model-v3.js');
const ports=p=>p.type==='turnout'&&p.junctionKind==='double-slip'
  ?[{port:'A'},{port:'B'},{port:'C'},{port:'D'}]
  :p.type==='turnout'?[{port:'A'},{port:'B'},{port:'C'}]:[{port:'A'},{port:'B'}];
const sync=l=>{for(const b of l.blocks)b.trackIds=[];for(const p of l.tracks){const b=l.blocks.find(x=>x.id===p.blockId);if(b)b.trackIds.push(p.id);}};
const base=()=>({schemaVersion:3,name:'Contract test',scale:'H0',trackSystem:'Märklin C',room:{widthMm:3000,heightMm:1800},levelSpacingMm:110,levels:Array.from({length:5},(_,i)=>({id:i+1,name:'Nivell '+(i+1),elevationMm:i*110,tracks:[],blocks:[],sensors:[]})),connections:[]});
const adapters={worldPorts:ports,syncRelations:sync};
const valid=base();valid.levels[0].tracks.push({id:'DS',type:'turnout',name:'Double slip',x:100,y:100,rotation:0,levelId:1,junctionKind:'double-slip',blockId:'',sensorName:'',geometry:{lengthMm:180,branchOffsetMm:60}});valid.levels[0].tracks.push({id:'S',type:'straight',name:'Straight',x:280,y:100,rotation:0,levelId:1,blockId:'',sensorName:'',geometry:{lengthMm:180}});valid.connections.push({id:'C1',levelId:1,from:{pieceId:'DS',port:'D'},to:{pieceId:'S',port:'A'},verified:false,method:'manual'});
assert.equal(model.validateConfig(valid,adapters).schemaVersion,3);
for(const mutate of [
 c=>{c.connections[0].from.port='Z';},
 c=>{c.connections.push({id:'C2',levelId:1,from:{pieceId:'DS',port:'D'},to:{pieceId:'S',port:'B'},verified:false});},
 c=>{c.levels[0].tracks[0].geometry.lengthMm=-1;},
 c=>{c.levels[0].tracks[0].zMm=Infinity;},
 c=>{c.connections[0].method='guess';}
]){const c=base();c.levels[0].tracks.push({id:'DS',type:'turnout',name:'Double slip',x:100,y:100,rotation:0,levelId:1,junctionKind:'double-slip',blockId:'',sensorName:'',geometry:{lengthMm:180,branchOffsetMm:60}});c.levels[0].tracks.push({id:'S',type:'straight',name:'Straight',x:280,y:100,rotation:0,levelId:1,blockId:'',sensorName:'',geometry:{lengthMm:180}});c.connections.push({id:'C1',levelId:1,from:{pieceId:'DS',port:'D'},to:{pieceId:'S',port:'A'},verified:false,method:'manual'});mutate(c);assert.throws(()=>model.validateConfig(c,adapters));}
const legacy=base();legacy.schemaVersion=2;legacy.levels[0].tracks.push({id:'L1',type:'straight',name:'Legacy',x:10,y:10,rotation:0,levelId:1,blockId:'B-OLD',sensorName:'LS-OLD',geometry:{lengthMm:180}});const migrated=model.validateConfig(legacy,adapters);assert.equal(migrated.schemaVersion,3);assert.equal(migrated.levels[0].blocks[0].verified,false);assert.equal(migrated.levels[0].sensors[0].verified,false);
console.log('PASS: model JSON v3 pur, ports doble-slip, integritat de connexions, geometria i migració segura v2→v3.');
