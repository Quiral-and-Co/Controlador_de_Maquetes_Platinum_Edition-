const assert = require('node:assert/strict');

class Element {
  constructor(id = '') {
    this.id = id; this.value = ''; this.textContent = ''; this.className = '';
    this.children = []; this.handlers = Object.create(null); this.disabled = false;
  }
  addEventListener(name, fn) { this.handlers[name] = fn; }
  append(...nodes) { this.children.push(...nodes); }
  replaceChildren(...nodes) { this.children = [...nodes]; }
  setAttribute() {}
  fire(name) { if (this.handlers[name]) this.handlers[name]({ target: this, currentTarget: this }); }
  click() { if (!this.disabled) this.fire('click'); }
}

const elements = new Map();
const document = {
  readyState: 'complete',
  getElementById(id) {
    if (!elements.has(id)) elements.set(id, new Element(id));
    return elements.get(id);
  },
  createElement(tag) { return new Element(tag); },
  addEventListener() {}
};
const config = {
  levels: [{ id: 1, name: 'Nivell 1', blocks: [
    { id: 'B01', name: 'B01', trackIds: ['T1'], sensorIds: [] },
    { id: 'B02', name: 'B02', trackIds: ['T2'], sensorIds: [] },
    { id: 'B03', name: 'B03', trackIds: ['T3'], sensorIds: ['S1'], verified: true }
  ], sensors: [{ id: 'S1', systemName: 'LS101', verified: false, invert: false }] }],
  dispatcher: { routes: [] }
};
let sensorReadings = {};
global.document = document;
global.window = {
  JMRI_TC_DispatcherCore: require('../dispatcher-core.js'),
  JMRI_TC_DISPATCHER: { getConfig: () => config, getSensorStates: () => ({ ...sensorReadings }), isConnected: () => true, log: () => {} }
};
require('../dispatcher.js');

const originalNow = Date.now;
Date.now = () => 123456789;

const $ = id => document.getElementById(id);
const blockRows = () => $('dispatcherBlocks').children;
function setBlock(id, status) {
  const row = blockRows().find(item => item.children[0].children[0].textContent === id);
  assert.ok(row, 'block row exists: ' + id);
  const select = row.children.find(item => item.id === 'select' || item.handlers.change);
  assert.ok(select, 'block state selector exists: ' + id);
  select.value = status;
  select.fire('change');
}
function createRoute(name, ids) {
  $('dispatcherRouteName').value = name;
  $('dispatcherRouteBlocks').value = ids;
  $('dispatcherCreateRoute').click();
}
function routeRow(name) {
  return $('dispatcherRoutes').children.find(row => row.children[0].children[0].textContent === name);
}

assert.equal($('dispatcherUnknownCount').textContent, 3);
for (const id of ['B01', 'B02']) setBlock(id, 'FREE');
assert.equal($('dispatcherFreeCount').textContent, 2);

createRoute('Ruta nord', 'B01, B02');
assert.equal(config.dispatcher.routes.length, 1);
assert.equal(routeRow('Ruta nord').children[1].children[0].disabled, false);
routeRow('Ruta nord').children[1].children[0].click();
assert.equal($('dispatcherReservedCount').textContent, 2);

createRoute('Ruta sud', 'B02, B03');
Date.now = originalNow;
assert.equal(config.dispatcher.routes.length, 2);
assert.notEqual(config.dispatcher.routes[0].id, config.dispatcher.routes[1].id, 'route IDs must remain unique within the same millisecond');
assert.equal(routeRow('Ruta sud').children[1].children[0].disabled, true, 'overlapping route must be blocked');

setBlock('B02', 'OCCUPIED');
assert.equal($('dispatcherOccupiedCount').textContent, 1);
assert.equal($('dispatcherReservedCount').textContent, 0, 'occupied block releases complete route reservation');
assert.equal(routeRow('Ruta sud').children[1].children[0].disabled, true);

// Sensor-driven blocks are read-only and fail closed until both sensor and block are verified.
let b03 = blockRows().find(row => row.children[0].children[0].textContent === 'B03');
assert.equal(b03.children.find(item => item.handlers.change).disabled, true);
assert.equal(b03.children[0].children[2].textContent, 'Desconegut');
config.levels[0].sensors[0].verified = true;
sensorReadings = { LS101: 4 };
window.JMRI_TC_DISPATCHER_UI.refresh();
b03 = blockRows().find(row => row.children[0].children[0].textContent === 'B03');
assert.equal(b03.children[0].children[2].textContent, 'Lliure');
createRoute('Ruta sensor', 'B03');
routeRow('Ruta sensor').children[1].children[0].click();
assert.equal($('dispatcherReservedCount').textContent, 1);
sensorReadings = { LS101: 2 };
window.JMRI_TC_DISPATCHER_UI.refresh();
b03 = blockRows().find(row => row.children[0].children[0].textContent === 'B03');
assert.equal(b03.children[0].children[2].textContent, 'Ocupat');
assert.equal($('dispatcherReservedCount').textContent, 0, 'active sensor releases the route reservation');
sensorReadings = {};
window.JMRI_TC_DISPATCHER_UI.refresh();
b03 = blockRows().find(row => row.children[0].children[0].textContent === 'B03');
assert.equal(b03.children[0].children[2].textContent, 'Desconegut', 'lost sensor reading fails closed');

console.log('PASS: dispatcher UI integration, route persistence in project model, block state changes, reservation actions and overlapping-route protection');
