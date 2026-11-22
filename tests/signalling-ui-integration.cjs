const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

const signalling = require('../signalling-core.js');
const integrationCore = require('../signalling-integration-core.js');
const topologyCore = require('../signalling-topology-core.js');

class Element {
  constructor(document, tag = 'div', id = '') {
    this.document = document;
    this.tagName = tag;
    this.id = id;
    this.value = '';
    this.textContent = '';
    this.dataset = {};
    this.attributes = {};
    this.children = [];
    this.parentNode = null;
    this.className = '';
  }
  setAttribute(name, value) {
    this.attributes[name] = String(value);
    if (name === 'id') {
      this.id = String(value);
      this.document.elements.set(this.id, this);
    }
    if (name === 'class') this.className = String(value);
  }
  append(...nodes) {
    for (const node of nodes) {
      node.parentNode = this;
      this.children.push(node);
    }
  }
  replaceChildren(...nodes) {
    this.children = [];
    this.append(...nodes);
  }
}

const document = {
  readyState: 'complete',
  elements: new Map(),
  getElementById(id) { return this.elements.get(id) || null; },
  createElementNS(_namespace, tag) { return new Element(this, tag); },
  addEventListener() {}
};
for (const id of ['signalRegistry', 'signalId', 'signalCurrentAspect', 'signalMessage', 'trackSvg']) {
  document.elements.set(id, new Element(document, id === 'trackSvg' ? 'svg' : 'div', id));
}
document.getElementById('signalId').value = 'SIG1';

const now = Date.now();
const config = {
  signallingInfrastructure: { blockSystem: 'BA', vmax: 200 },
  levels: [{
    id: 1, name: 'Nivell 1',
    blocks: [{ id: 'B1', name: 'Bloc 1', verified: true, sensorIds: ['S1'], trackIds: ['T1'] }],
    sensors: [{ id: 'S1', systemName: 'LS1', verified: true, invert: false }],
    tracks: [{ id: 'T1', type: 'straight', blockId: 'B1', x: 100, y: 200 }]
  }],
  dispatcher: { routes: [{ id: 'R1', name: 'Ruta 1', blockIds: ['B1'] }] },
  signals: [{
    id: 'SIG1', name: 'Entrada', type: 'ENTRY', blockSystem: 'BA', vmax: 120,
    aspect: 'PARADA', protectedBlockId: 'B1', routeId: 'R1', trackId: 'T1',
    turnoutRequirements: []
  }]
};
const snapshot = {
  blocks: [{ id: 'B1', verified: true, sensorIds: ['S1'] }],
  blockStates: { B1: 'FREE' },
  routes: [{ id: 'R1', blockIds: ['B1'] }],
  reservations: { B1: 'R1' }
};
let telemetry = {
  connected: true, lastHeartbeatAt: now, power: 'ON', powerUpdatedAt: now,
  sensors: { LS1: 4 }, sensorUpdatedAt: { LS1: now }, turnouts: {}, turnoutUpdatedAt: {}
};
let dispatcherRefreshes = 0;
let signalRefreshes = 0;
const window = {
  JMRI_TC_SignallingCore: signalling,
  JMRI_TC_SignallingIntegrationCore: integrationCore,
  JMRI_TC_SignallingTopologyCore: topologyCore,
  JMRI_TC_DISPATCHER: { getConfig: () => config },
  JMRI_TC_DISPATCHER_UI: {
    getSnapshot: () => snapshot,
    refresh() { dispatcherRefreshes++; return 'dispatcher'; }
  },
  JMRI_TC_SIGNALLING_UI: { refresh() { signalRefreshes++; return 'signals'; } },
  JMRI_TC_BOARD: { getTelemetry: () => telemetry, getSelectedLevel: () => 1 },
  JMRI_TC_SAFETY: { state: { fault: false } }
};
let intervalCallback = null;
const context = {
  window, document,
  setInterval(fn) { intervalCallback = fn; return 1; },
  clearInterval() {},
  console
};
vm.runInNewContext(fs.readFileSync(require.resolve('../signalling-integration.js'), 'utf8'), context);

const integration = window.JMRI_TC_SIGNALLING_INTEGRATION;
assert.ok(integration, 'SVG signalling integration is installed');
assert.equal(config.signals[0].computedAspect, 'VIA_LIBRE', 'valid Dispatcher snapshot and fresh JMRI telemetry calculate a permissive aspect');
assert.equal(document.getElementById('signalCurrentAspect').dataset.aspect, 'VIA_LIBRE');
const signalLayer = document.getElementById('signalLayer');
assert.ok(signalLayer, 'signal SVG layer is created');
assert.equal(signalLayer.children.length, 1, 'signal marker is rendered on the SVG board');
assert.equal(signalLayer.children[0].attributes['aria-label'], 'Entrada · Via lliure');
assert.equal(typeof intervalCallback, 'function', 'periodic refresh is installed');

assert.equal(window.JMRI_TC_DISPATCHER_UI.refresh(), 'dispatcher');
assert.equal(dispatcherRefreshes, 1);
assert.equal(signalRefreshes, 0);
assert.equal(config.signals[0].computedAspect, 'VIA_LIBRE', 'Dispatcher refresh triggers integrated signal recomputation');

telemetry = { ...telemetry, connected: false };
intervalCallback();
assert.equal(config.signals[0].computedAspect, 'PARADA', 'loss of JMRI communication forces PARADA');
assert.equal(signalLayer.children[0].attributes.class, 'signal-marker signal-parada');

telemetry = { ...telemetry, connected: true, lastHeartbeatAt: now };
config.levels[0].blocks[0].trackIds = ['MISSING'];
window.JMRI_TC_SIGNALLING_UI.refresh();
assert.equal(signalRefreshes, 1);
assert.equal(config.signals[0].computedAspect, 'PARADA', 'broken SVG/topology linkage overrides a permissive calculated aspect');
assert.match(config.signals[0].computedReason, /Vinculació topològica incompleta/);

console.log('PASS: integrated SVG board, Dispatcher refresh, signalling engine and topology fail-safe UI');
