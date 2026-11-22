'use strict';
const assert=require('node:assert/strict');
const junction=require('../track-junction-core.js');
const right=junction.curvePorts(360,30,'right');
assert.equal(right[0].port,'A');assert.equal(right[1].port,'B');assert.ok(right[1].y>0);
const left=junction.curvePorts(360,30,'left');assert.ok(left[1].y<0);
assert.equal(junction.turnoutPorts(188.3,60,'normal').length,3);
assert.deepEqual(junction.turnoutPorts(188.3,60,'double-slip').map(p=>p.port),['A','B','C','D']);
assert.equal(junction.preferredPort({type:'turnout',connectionPoint:'C'}),'C');
assert.equal(junction.preferredPort({type:'turnout',catalogueRef:'24624',connectionPoint:'D'}),'D');
const cat=[
 {ref:'24077',type:'straight',lengthMm:77.5,geometryStatus:'confirmed'},
 {ref:'24094',type:'straight',lengthMm:94.2,geometryStatus:'confirmed'},
 {ref:'24188',type:'straight',lengthMm:188.3,geometryStatus:'confirmed'},
 {ref:'24130',type:'curve',radiusMm:360,angleDeg:30,geometryStatus:'confirmed'}
];
const gaps=junction.recommendGaps([
 {pieceId:'A',levelId:1,x:0,y:0},
 {pieceId:'B',levelId:1,x:188.3,y:0},
 {pieceId:'C',levelId:1,x:0,y:100}
],cat,300);
assert.equal(gaps.length,1);
assert.ok(gaps[0].candidates.some(c=>c.items.length===1&&c.items[0].item.ref==='24188'));
const curveGap=junction.recommendGaps([
 {pieceId:'X',levelId:1,x:0,y:0},
 {pieceId:'Y',levelId:1,x:179.99999999999997,y:48.230854637602064}
],cat,300);
assert.ok(curveGap.length===1&&curveGap[0].candidates.some(c=>c.items[0].item.type==='curve'));
assert.throws(()=>junction.curvePorts(0,30,'right'),/invàlida/);
console.log('PASS track junction geometry, connection choices and straight/curve gap recommendations');
