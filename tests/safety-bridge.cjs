'use strict';
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const net = require('node:net');
const WebSocket = require('ws');

async function freePort() {
  const server = net.createServer();
  await new Promise((resolve, reject) => server.listen(0, '127.0.0.1', resolve).once('error', reject));
  const port = server.address().port;
  await new Promise(resolve => server.close(resolve));
  return port;
}

async function runCase({ dcc, safety, expected }) {
  const port = await freePort();
  const env = { ...process.env, PORT: String(port) };
  delete env.JMRI_TC_ENABLE_DCC;
  delete env.JMRI_TC_SAFETY_VALIDATED;
  if (dcc) env.JMRI_TC_ENABLE_DCC = '1';
  if (safety) env.JMRI_TC_SAFETY_VALIDATED = '1';

  const child = spawn(process.execPath, ['traffic-bridge.js'], {
    cwd: process.cwd(), env, stdio: ['ignore', 'pipe', 'pipe']
  });
  let output = '';
  child.stdout.on('data', chunk => { output += chunk.toString(); });
  child.stderr.on('data', chunk => { output += chunk.toString(); });

  let ws;
  try {
    await new Promise((resolve, reject) => {
      const deadline = setTimeout(() => reject(new Error('Bridge startup timeout: ' + output)), 5000);
      const poll = () => {
        if (output.includes('JMRI-TC local:')) return clearTimeout(deadline), resolve();
        if (child.exitCode !== null) return clearTimeout(deadline), reject(new Error('Bridge exited: ' + output));
        setTimeout(poll, 25);
      };
      poll();
    });
    ws = new WebSocket('ws://127.0.0.1:' + port + '/traffic-ws', { origin: 'http://127.0.0.1:' + port });
    const messages = [];
    ws.on('message', raw => messages.push(JSON.parse(raw.toString())));
    await new Promise((resolve, reject) => {
      ws.once('open', resolve);
      ws.once('error', reject);
    });
    const waitFor = async predicate => {
      const deadline = Date.now() + 3000;
      while (Date.now() < deadline) {
        const found = messages.find(predicate);
        if (found) return found;
        await new Promise(resolve => setTimeout(resolve, 10));
      }
      throw new Error('Timed out waiting for bridge message; got: ' + JSON.stringify(messages));
    };
    const status = await waitFor(m => m.type === 'status');
    assert.equal(status.dccEnabled, dcc);
    assert.equal(status.safetyValidated, safety);
    ws.send(JSON.stringify({ type: 'arm', enabled: true, safetyValidated: true }));
    const error = await waitFor(m => m.type === 'error');
    assert.match(error.message, expected);
  } finally {
    if (ws && ws.readyState === WebSocket.OPEN) ws.close();
    child.kill('SIGTERM');
    await new Promise(resolve => child.once('exit', resolve));
  }
}

(async () => {
  await runCase({
    dcc: false, safety: false,
    expected: /DCC desactivat per defecte/
  });
  await runCase({
    dcc: true, safety: false,
    expected: /Interbloqueig de seguretat/
  });
  await runCase({
    dcc: true, safety: true,
    expected: /Connecta primer/
  });
  console.log('PASS: bridge refuses DCC by default and enforces both server-side safety flags and live JMRI connection');
})().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
