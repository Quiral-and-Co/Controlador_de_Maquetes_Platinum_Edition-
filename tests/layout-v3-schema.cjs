'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const schema = JSON.parse(fs.readFileSync(require.resolve('../schema/layout-v3.schema.json'), 'utf8'));
const track = schema.$defs.track;
const connection = schema.$defs.connection;
assert.ok(track.properties.zMm, 'v3 ha de declarar la coordenada Z.');
for (const key of ['turnoutSystemName','catalogueRef','catalogueBrand','catalogueSystem','catalogueNotes','geometryStatus','junctionKind','connectionPoint','verticalRisk']) {
  assert.ok(track.properties[key], 'Falta la propietat de peça '+key);
}
assert.deepEqual(schema.$defs.portRef.properties.port.enum, ['A','B','C','D']);
assert.deepEqual(connection.properties.method.enum, ['manual','catalogue-gap-suggestion','legacy-import']);
assert.ok(track.properties.geometry.properties.branchOffsetMm);
assert.deepEqual(track.properties.geometry.properties.curveDirection.enum, ['left','right']);
console.log('PASS: JSON v3 schema cobreix propietats reals de l’editor, cota Z i ports A/B/C/D.');
