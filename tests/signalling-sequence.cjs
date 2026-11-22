const assert=require('node:assert/strict');
const {inspect}=require('../signalling-sequence-core.js');
const routes=[{id:'R',blockIds:['B1','B2','B3']}];
const signals=[
 {id:'S0',routeId:'R',protectedBlockId:'B1',nextSignal1Id:'S1',nextSignal2Id:'S2'},
 {id:'S1',routeId:'R',protectedBlockId:'B2',nextSignal1Id:'S2'},
 {id:'S2',routeId:'R',protectedBlockId:'B3'}
];
let r=inspect(signals,routes);
assert.equal(r.S0.ok,true);
assert.equal(r.S0.nextSignal1Id,'S1');assert.equal(r.S0.nextSignal2Id,'S2');
r=inspect(signals.map(s=>s.id==='S0'?{...s,nextSignal1Id:'MISSING'}:s),routes);
assert.equal(r.S0.ok,false);assert.ok(r.S0.issues.some(x=>x.includes('inexistent')));
r=inspect(signals.map(s=>s.id==='S0'?{...s,nextSignal1Id:'S2'}:s),routes);
assert.equal(r.S0.ok,false);assert.ok(r.S0.issues.some(x=>x.includes('posterior')));
r=inspect(signals.map(s=>s.id==='S1'?{...s,nextSignal1Id:'S0'}:s),routes);
assert.equal(r.S0.ok,false);assert.ok(r.S0.issues.some(x=>x.includes('circular')));
r=inspect(signals.map(s=>s.id==='S0'?{...s,nextSignal2Id:'S1'}:s),routes);
assert.equal(r.S0.ok,false);assert.ok(r.S0.issues.some(x=>x.includes('duplicada')));
r=inspect(signals.map(s=>s.id==='S0'?{...s,direction:'REVERSE'}:s),routes);
assert.equal(r.S0.ok,false);
console.log('Signalling sequence validation tests: OK');
