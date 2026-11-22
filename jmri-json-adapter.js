'use strict';

const WebSocket = require('ws');
const { EventEmitter } = require('node:events');

const TURNOUT_STATES = Object.freeze({ CLOSED: 2, THROWN: 4 });
const SENSOR_STATES = Object.freeze({ ACTIVE: 2, INACTIVE: 4, UNKNOWN: 0 });

function safeName(value) {
  const name = String(value || '').trim();
  if (!/^[A-Za-z0-9_.:-]{1,80}$/.test(name)) throw new Error('Nom JMRI invàlid.');
  return name;
}

class JmriJsonAdapter extends EventEmitter {
  constructor(options = {}) {
    super();
    this.host = String(options.host || '127.0.0.1');
    this.port = Number(options.port === undefined ? 12080 : options.port);
    this.path = String(options.path || '/json/');
    this.timeoutMs = Number(options.timeoutMs || 5000);
    if (!Number.isInteger(this.port) || this.port < 1 || this.port > 65535) throw new Error('Port JMRI invàlid.');
    if (!this.path.startsWith('/') || /[\r\n]/.test(this.path)) throw new Error('Ruta WebSocket JMRI invàlida.');
    if (!Number.isFinite(this.timeoutMs) || this.timeoutMs < 1) throw new Error('Temps d’espera invàlid.');
    this.ws = null;
    this.connected = false;
    this.pending = [];
    this._connectPromise = null;
    this._messageHandler = data => this._onMessage(data);
  }

  connect() {
    if (this.connected && this.ws?.readyState === WebSocket.OPEN) return Promise.resolve();
    if (this._connectPromise) return this._connectPromise;
    const ws = new WebSocket(`ws://${this.host}:${this.port}${this.path}`);
    this.ws = ws;
    this._connectPromise = new Promise((resolve, reject) => {
      let settled = false;
      const timer = setTimeout(() => {
        if (settled) return;
        settled = true;
        ws.terminate();
        reject(new Error('Temps d’espera connectant amb el servidor JSON de JMRI.'));
      }, this.timeoutMs);
      const cleanupTimer = () => clearTimeout(timer);
      ws.once('open', () => {
        if (settled) return;
        settled = true;
        cleanupTimer();
        this.connected = true;
        ws.on('message', this._messageHandler);
        ws.on('close', () => {
          if (this.ws === ws) this.ws = null;
          this._failPending(new Error('Connexió JMRI tancada.'));
        });
        ws.on('error', error => this._failPending(error));
        this.emit('connected');
        resolve();
      });
      ws.once('error', error => {
        cleanupTimer();
        if (!settled) {
          settled = true;
          reject(error);
        }
        if (this.ws === ws) this.ws = null;
        this.connected = false;
      });
      ws.once('close', () => {
        cleanupTimer();
        if (!settled) {
          settled = true;
          reject(new Error('Connexió JMRI tancada abans d’obrir-se.'));
        }
      });
    }).finally(() => { this._connectPromise = null; });
    return this._connectPromise;
  }

  _onMessage(raw) {
    let message;
    try { message = JSON.parse(raw.toString()); } catch { return; }
    this.emit('message', message);
    const data = message?.data || {};
    const index = this.pending.findIndex(item =>
      item.type === message.type && (!item.name || item.name === data.name));
    if (index < 0) return;
    const [pending] = this.pending.splice(index, 1);
    clearTimeout(pending.timer);
    pending.resolve(message);
  }

  _failPending(error) {
    this.connected = false;
    for (const item of this.pending.splice(0)) {
      clearTimeout(item.timer);
      item.reject(error);
    }
    this.emit('disconnected', error);
  }

  async request(type, name, method = 'get', extra = {}) {
    await this.connect();
    if (!this.connected || !this.ws || this.ws.readyState !== WebSocket.OPEN) {
      throw new Error('Connexió JMRI no disponible.');
    }
    const entityName = name ? safeName(name) : undefined;
    const payload = {
      type,
      ...(method === 'get' ? {} : { method }),
      data: { ...(entityName ? { name: entityName } : {}), ...extra }
    };
    return new Promise((resolve, reject) => {
      const pending = { type, name: entityName, resolve, reject, timer: null };
      pending.timer = setTimeout(() => {
        const index = this.pending.indexOf(pending);
        if (index >= 0) this.pending.splice(index, 1);
        reject(new Error(`JMRI no ha respost a ${type} ${entityName || ''}.`.trim()));
      }, this.timeoutMs);
      this.pending.push(pending);
      try {
        this.ws.send(JSON.stringify(payload), error => {
          if (!error) return;
          const index = this.pending.indexOf(pending);
          if (index >= 0) this.pending.splice(index, 1);
          clearTimeout(pending.timer);
          reject(error);
        });
      } catch (error) {
        const index = this.pending.indexOf(pending);
        if (index >= 0) this.pending.splice(index, 1);
        clearTimeout(pending.timer);
        reject(error);
      }
    });
  }

  async getTurnout(systemName) {
    return this._normaliseState(await this.request('turnout', systemName), 'turnout');
  }

  async getSensor(systemName) {
    return this._normaliseState(await this.request('sensor', systemName), 'sensor');
  }

  _normaliseState(message, type) {
    if (message.type !== type || !message.data ||
        !Number.isInteger(Number(message.data.state)) ||
        !message.data.name) {
      throw new Error('Resposta JMRI incompleta o no vàlida.');
    }
    return { name: safeName(message.data.name), state: Number(message.data.state), raw: message.data };
  }

  async setDccTurnout(accessory, requestedState, safety = {}) {
    if (!accessory || accessory.protocol !== 'DCC' || accessory.type !== 'TURNOUT') {
      throw new Error('Aquest adaptador només opera desviaments DCC.');
    }
    if (accessory.verified !== true) throw new Error('Desviament no verificat físicament.');
    if (safety.safetyValidated !== true) throw new Error('Validació de seguretat no completada.');
    if (safety.physicalOutputsEnabled !== true) throw new Error('Les sortides físiques continuen bloquejades.');
    if (safety.jmriConnected !== true || !this.connected) throw new Error('JMRI no està connectat.');
    if (safety.power !== 'ON') throw new Error('Alimentació de via no confirmada en ON.');
    if (safety.armed !== true) throw new Error('Control d’accessoris no armat.');
    if (safety.emergencyStopReleased !== true) throw new Error('Parada d’emergència no confirmada com a alliberada.');
    if (!Object.hasOwn(TURNOUT_STATES, requestedState)) throw new Error('Estat de desviament no vàlid.');
    const name = safeName(accessory.systemName);
    const result = this._normaliseState(
      await this.request('turnout', name, 'post', { state: TURNOUT_STATES[requestedState] }),
      'turnout'
    );
    if (result.state !== TURNOUT_STATES[requestedState]) {
      throw new Error('JMRI no ha confirmat l’estat de desviament sol·licitat.');
    }
    return result;
  }

  close() {
    const ws = this.ws;
    this.ws = null;
    this.connected = false;
    this._failPending(new Error('Adaptador JMRI tancat.'));
    if (ws && ws.readyState < WebSocket.CLOSING) ws.close();
  }
}

module.exports = { JmriJsonAdapter, TURNOUT_STATES, SENSOR_STATES, safeName };
