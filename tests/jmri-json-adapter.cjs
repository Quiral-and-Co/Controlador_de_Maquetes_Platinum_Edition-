'use strict';

const assert = require('node:assert/strict');
const { WebSocketServer } = require('ws');
const { JmriJsonAdapter, TURNOUT_STATES, SENSOR_STATES, safeName } = require('../jmri-json-adapter');

(async () => {
  assert.equal(TURNOUT_STATES.CLOSED, 2);
  assert.equal(TURNOUT_STATES.THROWN, 4);
  assert.equal(SENSOR_STATES.ACTIVE, 2);
  assert.equal(SENSOR_STATES.INACTIVE, 4);
  assert.equal(SENSOR_STATES.UNKNOWN, 0);
  assert.throws(() => safeName('../bad'), /invàlid/);
  assert.throws(() => safeName(''), /invàlid/);

  const server = new WebSocketServer({ host: '127.0.0.1', port: 0, path: '/json/' });
  await new Promise(resolve => server.once('listening', resolve));
  const port = server.address().port;
  const received = [];
  server.on('connection', socket => socket.on('message', raw => {
    const command = JSON.parse(raw.toString());
    received.push(command);
    const state = command.data.state ?? (command.type === 'sensor' ? SENSOR_STATES.ACTIVE : TURNOUT_STATES.THROWN);
    socket.send(JSON.stringify({ type: command.type, data: { name: command.data.name, state } }));
  }));

  const adapter = new JmriJsonAdapter({ host: '127.0.0.1', port, timeoutMs: 1000 });
  try {
    await adapter.connect();
    const turnout = await adapter.getTurnout('LT1');
    assert.equal(turnout.name, 'LT1');
    assert.equal(turnout.state, TURNOUT_STATES.THROWN);
    const sensor = await adapter.getSensor('IS1');
    assert.equal(sensor.state, SENSOR_STATES.ACTIVE);

    const accessory = { protocol: 'DCC', type: 'TURNOUT', systemName: 'LT1', verified: true };
    const safe = {
      safetyValidated: true, physicalOutputsEnabled: true, jmriConnected: true,
      power: 'ON', armed: true, emergencyStopReleased: true
    };
    const before = received.length;
    await assert.rejects(adapter.setDccTurnout(accessory, 'CLOSED', {
      ...safe, physicalOutputsEnabled: false
    }), /continuen bloquejades/);
    await assert.rejects(adapter.setDccTurnout(accessory, 'CLOSED', {
      ...safe, safetyValidated: false
    }), /Validació de seguretat/);
    await assert.rejects(adapter.setDccTurnout(accessory, 'CLOSED', {
      ...safe, emergencyStopReleased: false
    }), /Parada d’emergència/);
    await assert.rejects(adapter.setDccTurnout(accessory, 'CLOSED', {
      ...safe, armed: false
    }), /no armat/);
    assert.equal(received.length, before, 'blocked commands must never reach the WebSocket');

    await adapter.setDccTurnout(accessory, 'CLOSED', safe);
    assert.equal(received.at(-1).method, 'post');
    assert.equal(received.at(-1).data.state, TURNOUT_STATES.CLOSED);
    await assert.rejects(adapter.setDccTurnout({ ...accessory, protocol: 'MM2' }, 'CLOSED', safe),
      /només opera desviaments DCC/);
    await assert.rejects(adapter.setDccTurnout({ ...accessory, verified: false }, 'CLOSED', safe),
      /no verificat físicament/);
    await assert.rejects(adapter.setDccTurnout(accessory, 'UNKNOWN', safe),
      /Estat de desviament no vàlid/);
  } finally {
    adapter.close();
    await new Promise(resolve => server.close(resolve));
  }
  process.stdout.write('PASS: JMRI JSON adapter readback, DCC allowlist and fail-safe physical-output gates\n');
})().catch(error => { console.error(error); process.exitCode = 1; });
