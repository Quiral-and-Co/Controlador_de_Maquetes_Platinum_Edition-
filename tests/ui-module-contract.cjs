'use strict';

const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

const root = path.resolve(__dirname, '..');
const html = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
const scripts = [...html.matchAll(/<script\\b[^>]*\\bsrc=["']([^"']+)["'][^>]*>/gi)].map(m => m[1]);
const localScripts = scripts.filter(src => !/^(?:https?:)?\\/\\//i.test(src) && !src.startsWith('data:'));

assert.ok(scripts.length > 0, 'index.html must load application scripts');
assert.equal(new Set(localScripts).size, localScripts.length, 'local scripts must not be included twice');
for (const src of localScripts) {
  const resolved = path.resolve(root, src);
  assert.ok(resolved.startsWith(root + path.sep), `script path escapes repository: ${src}`);
  assert.ok(fs.existsSync(resolved), `index.html references missing script: ${src}`);
}

const ids = [...html.matchAll(/\\bid=["']([^"']+)["']/gi)].map(m => m[1]);
const duplicates = [...new Set(ids.filter((id, i) => ids.indexOf(id) !== i))];
assert.deepEqual(duplicates, [], `duplicate DOM ids: ${duplicates.join(', ')}`);

const order = new Map(localScripts.map((src, i) => [src, i]));
for (const [dependency, consumer] of [
  ['layout-model-v3.js', 'app.js'],
  ['viewport-core.js', 'app.js'],
  ['selection-move-core.js', 'app.js'],
  ['track-junction-core.js', 'app.js'],
  ['vertical-track-core.js', 'vertical-track-ui.js'],
  ['dispatcher-core.js', 'dispatcher.js'],
  ['traffic-core.js', 'traffic.js'],
  ['safety-core.js', 'app.js'],
  ['signalling-core.js', 'signalling-ui.js'],
  ['signalling-topology-core.js', 'signalling-topology-ui.js'],
  ['physical-validation-core.js', 'physical-validation-ui.js'],
  ['validation-master-registry-core.js', 'validation-master-registry-ui.js'],
  ['command-station-core.js', 'command-station-ui.js'],
  ['locomotive-dmi-core.js', 'locomotive-dmi-ui.js'],
  ['spielewelt-core.js', 'spielewelt-ui.js'],
  ['train-protection-core.js', 'train-protection-ui.js'],
  ['train-ground-core.js', 'train-ground-ui.js']
]) {
  assert.ok(order.has(dependency), `missing dependency script: ${dependency}`);
  assert.ok(order.has(consumer), `missing consumer script: ${consumer}`);
  assert.ok(order.get(dependency) < order.get(consumer), `${dependency} must load before ${consumer}`);
}

const requiredPanels = [
  'commandStationPanel', 'dispatcherTitle', 'safetyTitle', 'trainProtectionPanel',
  'trainGroundPanel', 'locomotiveDmiPanel', 'spieleweltPanel', 'trafficTitle',
  'signalTopologyTitle', 'physicalValidationTitle', 'validationMasterRegistryTitle',
  'metgeQuiralTitle', 'blockRegistry', 'sensorRegistry'
];
for (const id of requiredPanels) assert.ok(ids.includes(id), `missing expected panel/registry anchor: ${id}`);

console.log(`UI module contract passed: ${localScripts.length} local scripts, ${ids.length} DOM ids, ${requiredPanels.length} panel anchors.`);
