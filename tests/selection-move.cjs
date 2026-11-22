'use strict';
const assert = require('node:assert/strict');
const selectionMove = require('../selection-move-core.js');
const board = { width: 3000, height: 1800 };
const pieces = [{ id: 'A', x: 100, y: 200 }, { id: 'B', x: 140, y: 240 }, { id: 'C', x: 2800, y: 1700 }];
const plan = selectionMove.createDrag(pieces.slice(0, 2), 'A', { x: 105, y: 205 }, board, 10);
const moved = selectionMove.move(plan, { x: 157, y: 256 });
assert.deepEqual(moved, { dx: 50, dy: 50, positions: [{ id: 'A', x: 150, y: 250 }, { id: 'B', x: 190, y: 290 }] });
assert.deepEqual(pieces[0], { id: 'A', x: 100, y: 200 }, 'El càlcul no ha de mutar les peces originals.');
const rightBottom = selectionMove.move(plan, { x: 10000, y: 10000 });
assert.deepEqual(rightBottom.positions, [{ id: 'A', x: 2960, y: 1760 }, { id: 'B', x: 3000, y: 1800 }]);
const leftTop = selectionMove.move(plan, { x: -10000, y: -10000 });
assert.deepEqual(leftTop.positions, [{ id: 'A', x: 0, y: 0 }, { id: 'B', x: 40, y: 40 }]);
const fractional = selectionMove.createDrag([{ id: 'F1', x: 100.5, y: 100.5 }, { id: 'F2', x: 140.5, y: 140.5 }], 'F1', { x: 105, y: 105 }, board, 10);
const fractionalMove = selectionMove.move(fractional, { x: 128, y: 132 });
assert.equal(fractionalMove.dx, 20);
assert.equal(fractionalMove.dy, 30);
assert.equal(fractionalMove.positions[1].x - fractionalMove.positions[0].x, 40);
assert.equal(fractionalMove.positions[1].y - fractionalMove.positions[0].y, 40);
assert.throws(() => selectionMove.createDrag([], 'A', { x: 0, y: 0 }, board, 10), /almenys una peça/);
assert.throws(() => selectionMove.createDrag(pieces.slice(0, 1), 'missing', { x: 0, y: 0 }, board, 10), /ancoratge/);
assert.throws(() => selectionMove.createDrag([{ id: 'D', x: -1, y: 0 }], 'D', { x: 0, y: 0 }, board, 10), /fora de la superfície/);
assert.throws(() => selectionMove.move(plan, { x: Infinity, y: 0 }), /nombre finit/);
const rotationInput = [{ id: 'R1', x: 100, y: 100, rotation: 30 }, { id: 'R2', x: 200, y: 100, rotation: 90 }];
const rotated = selectionMove.rotate(rotationInput, 90, board);
assert.deepEqual(rotated.center, { x: 150, y: 100 }, 'El pivot ha de ser el centre de la caixa de selecció.');
assert.ok(Math.abs(rotated.positions[0].x - 150) < 1e-9 && Math.abs(rotated.positions[0].y - 50) < 1e-9);
assert.ok(Math.abs(rotated.positions[1].x - 150) < 1e-9 && Math.abs(rotated.positions[1].y - 150) < 1e-9);
assert.equal(rotated.positions[0].rotation, 120);
assert.equal(rotated.positions[1].rotation, 180);
assert.ok(Math.abs(Math.hypot(rotated.positions[1].x-rotated.positions[0].x,rotated.positions[1].y-rotated.positions[0].y)-100)<1e-9,'La rotació ha de conservar els offsets geomètrics.');
assert.deepEqual(rotationInput,[{id:'R1',x:100,y:100,rotation:30},{id:'R2',x:200,y:100,rotation:90}], 'La planificació de rotació no ha de mutar les peces originals.');
assert.throws(()=>selectionMove.rotate([{id:'EDGE',x:0,y:0,rotation:0},{id:'EDGE2',x:3000,y:1800,rotation:0}],45,board),/fora de la superfície/);
assert.throws(()=>selectionMove.rotate([{id:'DUP',x:100,y:100},{id:'DUP',x:200,y:200}],15,board),/identificadors duplicats/);
assert.throws(()=>selectionMove.rotate([{id:'BAD',x:NaN,y:100}],15,board),/nombre finit/);
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],0,board);
 assert.equal(result.positions[0].rotation,0,'Angle de quadrants 0°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 0°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],30,board);
 assert.equal(result.positions[0].rotation,30,'Angle de quadrants 30°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 30°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],45,board);
 assert.equal(result.positions[0].rotation,45,'Angle de quadrants 45°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 45°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],60,board);
 assert.equal(result.positions[0].rotation,60,'Angle de quadrants 60°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 60°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],90,board);
 assert.equal(result.positions[0].rotation,90,'Angle de quadrants 90°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 90°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],120,board);
 assert.equal(result.positions[0].rotation,120,'Angle de quadrants 120°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 120°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],135,board);
 assert.equal(result.positions[0].rotation,135,'Angle de quadrants 135°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 135°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],150,board);
 assert.equal(result.positions[0].rotation,150,'Angle de quadrants 150°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 150°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],180,board);
 assert.equal(result.positions[0].rotation,180,'Angle de quadrants 180°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 180°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],210,board);
 assert.equal(result.positions[0].rotation,210,'Angle de quadrants 210°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 210°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],225,board);
 assert.equal(result.positions[0].rotation,225,'Angle de quadrants 225°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 225°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],240,board);
 assert.equal(result.positions[0].rotation,240,'Angle de quadrants 240°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 240°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],270,board);
 assert.equal(result.positions[0].rotation,270,'Angle de quadrants 270°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 270°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],300,board);
 assert.equal(result.positions[0].rotation,300,'Angle de quadrants 300°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 300°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],315,board);
 assert.equal(result.positions[0].rotation,315,'Angle de quadrants 315°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 315°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],330,board);
 assert.equal(result.positions[0].rotation,330,'Angle de quadrants 330°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 330°');
}
{
 const result=selectionMove.rotate([{id:'A',x:100,y:100,rotation:0},{id:'B',x:200,y:100,rotation:0}],360,board);
 assert.equal(result.positions[0].rotation,0,'Angle de quadrants 360°');
 assert.ok(Math.abs(Math.hypot(result.positions[1].x-result.positions[0].x,result.positions[1].y-result.positions[0].y)-100)<1e-8,'Offsets conservats à 360°');
}
console.log('PASS: moviment conjunt, graella de 10 mm, offsets relatius, límits i validació d’entrada');
