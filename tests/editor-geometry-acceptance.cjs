'use strict';

// Acceptance gate for the track editor. This intentionally fails while required
// editor capabilities are absent; it must never report a green result by
// testing a stand-alone mathematical substitute for the production editor.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const core = require('../selection-move-core.js');
const app = fs.readFileSync(path.join(__dirname, '..', 'app.js'), 'utf8');
const failures = [];

function check(name, fn) {
  try { fn(); console.log('PASS ' + name); }
  catch (error) { failures.push(name + ': ' + error.message); console.error('FAIL ' + name + ': ' + error.message); }
}

// X/Y are exercised against the production selection movement core.
check('moviment X/Y en mm, offsets comuns i no-mutació', () => {
  const board = { width: 3000, height: 1800 };
  const input = [{ id: 'A', x: 100, y: 100 }, { id: 'B', x: 140, y: 160 }];
  const drag = core.createDrag(input, 'A', { x: 0, y: 0 }, board, 1);
  const result = core.move(drag, { x: 25, y: -10 });
  assert.deepEqual(result.positions, [
    { id: 'A', x: 125, y: 90 },
    { id: 'B', x: 165, y: 150 }
  ]);
  assert.deepEqual(input, [{ id: 'A', x: 100, y: 100 }, { id: 'B', x: 140, y: 160 }]);
});

// These assertions are deliberately against production APIs, not test-only
// implementations. Missing capabilities remain a blocking CI failure.
check('moviment Z de la selecció', () => {
  assert.equal(typeof core.moveXYZ, 'function',
    'selection-move-core.js encara no exposa moveXYZ; el canvi de nivell no substitueix el desplaçament Z numèric.');
  const source = [{id:'A',x:100,y:100,zMm:20},{id:'B',x:150,y:160,zMm:20}];
  const moved = core.moveXYZ(source,{x:12.5,y:-7.5,z:35},{width:3000,height:1800});
  assert.deepEqual(moved.positions,[{id:'A',x:112.5,y:92.5,zMm:55},{id:'B',x:162.5,y:152.5,zMm:55}]);
  assert.deepEqual(source,[{id:'A',x:100,y:100,zMm:20},{id:'B',x:150,y:160,zMm:20}]);
  assert.throws(()=>core.moveXYZ(source,{x:-500,y:0,z:0},{width:3000,height:1800}),/fora de la superfície/);
  assert.throws(()=>core.moveXYZ(source,{x:0,y:0,z:Infinity},{width:3000,height:1800}),/nombre finit/);
});

check('generació de paral·leles a 77 mm i 64 mm', () => {
  assert.equal(typeof core.createParallel, 'function',
    'selection-move-core.js encara no exposa createParallel per generar vies paral·leles.');
  const source = [
    {id:'S1',type:'straight',x:500,y:500,rotation:0,geometry:{lengthMm:180}},
    {id:'C1',type:'curve',x:900,y:500,rotation:0,geometry:{radiusMm:360,angleDeg:30}}
  ];
  for (const distance of [77, 64]) {
    const generated = core.createParallel(source, distance, {side:'left',board:{width:3000,height:1800}});
    assert.ok(generated && Array.isArray(generated.tracks), 'createParallel ha de retornar tracks.');
    assert.equal(generated.distanceMm, distance, 'La distància entre eixos ha de ser exacta.');
    assert.equal(generated.tracks[0].y, 500-distance);
    assert.equal(generated.tracks[1].geometry.radiusMm, 360+distance, 'El radi de la corba s’ha d’ajustar a la distància entre eixos.');
    assert.equal(generated.tracks[0].parallelDistanceMm, distance);
    assert.notEqual(generated.tracks[0].id, source[0].id);
  }
  assert.throws(()=>core.createParallel(source,0),/superior a 0/);
  assert.throws(()=>core.createParallel(source,100,{side:'inside'}),/costat/);
});

// Verify that curve and connection acceptance checks exercise the editor's
// production geometry/topology routines; the full UI scenarios are in smoke.cjs.
check('geometria de corbes i ports transformats', () => {
  assert.match(app, /function localPorts\(p\)/, 'Falta el càlcul dels ports locals.');
  assert.match(app, /selectionMove\.moveXYZ\(/, 'La interfície no utilitza el moviment XYZ de producció.');
  assert.match(app, /selectionMove\.createParallel\(/, 'La interfície no utilitza el generador de paral·leles de producció.');
  assert.match(app, /function worldPorts\(p\)/, 'Falta la transformació dels ports al sistema global.');
  assert.match(app, /Math\.sin\(a\)/, 'Falta el càlcul trigonomètric de la corba.');
  assert.match(app, /Math\.cos\(a\)/, 'Falta el càlcul trigonomètric de la corba.');
});

check('connexions i auditoria de topologia', () => {
  assert.match(app, /function auditTopology\(/, "Falta l’auditoria de topologia.");
  assert.match(app, /gap>1/, 'Falta el rebuig de connexions amb separació superior a 1 mm.');
  assert.match(app, /discontinuïtat geomètrica/, 'Falta el diagnòstic de discontinuïtats geomètriques.');
  for (const id of ['moveTargetX','moveTargetY','moveTargetZ','applyXYZBtn','parallelDistance','parallelSide','createParallelBtn']) {
    assert.ok(fs.readFileSync(path.join(__dirname,'..','index.html'),'utf8').includes('id="'+id+'"'), 'Falta el control d’interfície '+id);
  }
});

check('orientació de corba, ports de desviament i suggeriments de buits integrats', () => {
  const junction = require('../track-junction-core.js');
  assert.match(app, /trackJunction\.curvePorts/);
  assert.match(app, /trackJunction\.turnoutPorts/);
  assert.match(app, /trackJunction\.recommendGaps/);
  assert.match(app, /propCurveDirection/);
  assert.match(app, /propJunctionConnection/);
  assert.match(app, /suggestGapTracksBtn/);
  assert.match(fs.readFileSync(path.join(__dirname,'..','index.html'),'utf8'), /track-junction-core\.js/);
  assert.equal(junction.preferredPort({type:'turnout',connectionPoint:'B'}),'B');
});


check('rampes verticals, transicions, hèlix i classificació visual', () => {
  const vertical = require('../vertical-track-core.js');
  assert.equal(typeof vertical.straightRamp, 'function');
  assert.equal(typeof vertical.helix, 'function');
  assert.equal(typeof vertical.suggestedTurns, 'function');
  assert.match(app, /verticalRisk/);
  const html = fs.readFileSync(path.join(__dirname,'..','index.html'),'utf8');
  assert.match(html, /vertical-track-ui\.js/);
  for (const id of ['verticalMode','rampLengthMm','rampRiseMm','rampGradePercent','rampTransitionMm','helixRadiusMm','helixTrackCount','helixTurns','helixRiseMm','helixGradePercent','helixSpacingMm','calculateVerticalRampBtn','insertVerticalRampBtn']) {
    assert.ok(html.includes('id="'+id+'"'), 'Falta el control de rampa '+id);
  }
  assert.equal(vertical.classifyGradient(1.5).code,'green');
  assert.equal(vertical.classifyGradient(2.5).code,'yellow');
  assert.equal(vertical.classifyGradient(4).code,'red');
  assert.equal(vertical.classifyGradient(6).code,'brown');
});

if (failures.length) {
  console.error('EDITOR GEOMETRY ACCEPTANCE BLOCKED: ' + failures.length + ' criteri(s) pendent(s).');
  process.exitCode = 1;
} else {
  console.log('PASS: tots els criteris d’acceptació geomètrica de l’editor.');
}
