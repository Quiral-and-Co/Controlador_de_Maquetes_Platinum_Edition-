'use strict';
const assert=require('node:assert/strict');
const core=require('../vertical-track-core.js');
const catalogue=[
 {ref:'24172',type:'straight',lengthMm:171.7,geometryStatus:'confirmed'},
 {ref:'24180',type:'straight',lengthMm:180,geometryStatus:'confirmed'},
 {ref:'24200',type:'straight',lengthMm:200,geometryStatus:'confirmed'},
 {ref:'24188',type:'straight',lengthMm:188.3,geometryStatus:'confirmed'},
 {ref:'24189',type:'straight',lengthMm:188.3,geometryStatus:'unverified'},
 {ref:'24230',type:'curve',radiusMm:360,angleDeg:30,geometryStatus:'confirmed'},
 {ref:'24224',type:'curve',radiusMm:437.5,angleDeg:24.3,geometryStatus:'confirmed'},
 {ref:'24230',type:'curve',radiusMm:437.5,angleDeg:30,geometryStatus:'confirmed'}
];
assert.equal(core.classifyGradient(1.5).code,'green');
assert.equal(core.classifyGradient(2.5).code,'yellow');
assert.equal(core.classifyGradient(4).code,'red');
assert.equal(core.classifyGradient(6).code,'brown');
assert.throws(()=>core.classifyGradient(2,{greenMax:4,yellowMax:3,redMax:5}),/llindars/);
const fit=core.fitStraightPieces(360,catalogue,1);
assert.equal(fit.actualLengthMm,360);
assert.equal(fit.items.length,2);
assert.ok(fit.items.every(x=>x.geometryStatus==='confirmed'));
const ramp=core.straightRamp({lengthMm:600,riseMm:10,transitionLengthMm:100},catalogue);
assert.equal(ramp.kind,'straight');
assert.equal(ramp.placements.length,3);
assert.ok(Math.abs(ramp.gradePercent-2)<1e-9);
assert.equal(ramp.placements[0].zStartMm,0);
assert.ok(Math.abs(ramp.placements.at(-1).zEndMm-10)<1e-8);
assert.ok(ramp.placements.every((p,i)=>i===0||p.zStartMm>=ramp.placements[i-1].zStartMm));
const byGrade=core.straightRamp({riseMm:20,gradePercent:2,transitionLengthMm:100},catalogue);
assert.equal(byGrade.requestedLengthMm,1100);
assert.equal(core.suggestedTurns(30).find(x=>x.turns===1).pieces,12);
const helix=core.helix({radiusMm:360,riseMm:90,parallelTracks:2,spacingMm:77,turns:1,transitionLengthMm:100},catalogue);
assert.equal(helix.pieceCountPerTrack,12);
const helixByGrade=core.helix({radiusMm:360,gradePercent:2,parallelTracks:1,spacingMm:77,turns:1,transitionLengthMm:100},catalogue);
assert.ok(Math.abs(helixByGrade.gradePercent-2)<1e-8);
assert.ok(helixByGrade.riseMm>0);
assert.equal(helix.placements.length,24);
assert.equal(helix.lanes[1].radiusMm,437);
const r2Helix=core.helix({radiusMm:437.5,riseMm:50,parallelTracks:1,spacingMm:77,turns:1,transitionLengthMm:100},catalogue);
assert.equal(r2Helix.curve.angleDeg,30);
assert.equal(r2Helix.pieceCountPerTrack,12);
assert.equal(helix.lanes[0].classification.code,'red');
assert.ok(helix.lanes.every(x=>x.closureErrorMm<1e-6));
assert.throws(()=>core.helix({radiusMm:400,riseMm:100,parallelTracks:1,spacingMm:77,turns:1,transitionLengthMm:100},catalogue),/±2 mm/);
assert.throws(()=>core.helix({radiusMm:360,riseMm:100,parallelTracks:1,spacingMm:77,turns:0.9,transitionLengthMm:100},catalogue),/compatible/);
console.log('PASS: vertical ramp, transition profile, gradient classification, catalogue fit, helical geometry and decimal-turn compatibility.');
