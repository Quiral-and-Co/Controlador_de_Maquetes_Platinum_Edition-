'use strict';

// Supervised timetable planning only. This module never sends physical commands.
const MAX_SCHEDULE_SPEED = 20;
const TURNOUT_STATES = Object.freeze(['CLOSED', 'THROWN']);

function parseClock(value) {
  const match = /^(\d{2}):(\d{2})$/.exec(String(value || ''));
  if (!match) return null;
  const hours = Number(match[1]), minutes = Number(match[2]);
  return hours <= 23 && minutes <= 59 ? hours * 60 + minutes : null;
}

function dateKey(date) {
  if (!(date instanceof Date) || Number.isNaN(date.getTime())) return null;
  return [date.getFullYear(), String(date.getMonth() + 1).padStart(2, '0'), String(date.getDate()).padStart(2, '0')].join('-');
}

function validateSchedule(input, routes, locomotives) {
  if (!input || typeof input !== 'object') return { ok: false, error: 'Horari invàlid.' };
  const id = String(input.id || '').trim(), name = String(input.name || '').trim();
  if (!id || !name) return { ok: false, error: 'L’horari necessita identificador i nom.' };
  if (!Array.isArray(routes) || !routes.some(route => route.id === input.routeId)) return { ok: false, error: 'L’itinerari seleccionat no existeix.' };
  if (!Array.isArray(locomotives) || !locomotives.some(loco => loco.id === input.locomotiveId)) return { ok: false, error: 'La locomotora seleccionada no existeix.' };
  if (parseClock(input.startTime) === null) return { ok: false, error: 'L’hora ha de tenir el format HH:mm.' };
  if (!Array.isArray(input.days) || !input.days.length || input.days.some(day => !Number.isInteger(day) || day < 0 || day > 6)) return { ok: false, error: 'Selecciona almenys un dia vàlid (0 diumenge – 6 dissabte).' };
  if (new Set(input.days).size !== input.days.length) return { ok: false, error: 'No es poden repetir dies.' };
  const speed = Number(input.maxSpeed);
  if (!Number.isInteger(speed) || speed < 1 || speed > MAX_SCHEDULE_SPEED) return { ok: false, error: 'La velocitat programada ha d’estar entre 1 i 20 passos.' };
  const windowMinutes = input.windowMinutes === undefined ? 2 : Number(input.windowMinutes);
  if (!Number.isInteger(windowMinutes) || windowMinutes < 1 || windowMinutes > 15) return { ok: false, error: 'La finestra d’inici ha d’estar entre 1 i 15 minuts.' };
  const requiredTurnouts = [];
  const seenTurnouts = new Set();
  for (const turnout of (Array.isArray(input.requiredTurnouts) ? input.requiredTurnouts : [])) {
    const systemName = String(turnout?.systemName || '').trim();
    if (!systemName || seenTurnouts.has(systemName)) return { ok: false, error: 'Els desviaments requerits han de tenir identificador únic.' };
    if (!TURNOUT_STATES.includes(turnout.state) || turnout.verified !== true) return { ok: false, error: 'Cada desviament requerit ha d’estar verificat i tenir estat CLOSED o THROWN.' };
    seenTurnouts.add(systemName);
    requiredTurnouts.push({ systemName, state: turnout.state, verified: true });
  }
  return {
    ok: true,
    schedule: {
      id, name, routeId: input.routeId, locomotiveId: input.locomotiveId,
      startTime: input.startTime, days: [...input.days].sort((a, b) => a - b),
      maxSpeed: speed, windowMinutes, enabled: input.enabled !== false, requiredTurnouts
    }
  };
}

function createState() { return { schedules: [], executions: [] }; }

function addSchedule(state, input, routes, locomotives) {
  const checked = validateSchedule(input, routes, locomotives);
  if (!checked.ok) return checked;
  if (state.schedules.some(schedule => schedule.id === checked.schedule.id)) return { ok: false, error: 'Ja existeix un horari amb aquest identificador.' };
  state.schedules.push(checked.schedule);
  return { ok: true, schedule: checked.schedule };
}

function evaluateSchedule(schedule, now, safety, alreadyClaimed = false) {
  if (!schedule || !schedule.enabled) return { status: 'DISABLED', reason: 'Horari desactivat.' };
  if (!(now instanceof Date) || Number.isNaN(now.getTime())) return { status: 'BLOCKED', reason: 'Data/hora invàlida.' };
  if (alreadyClaimed) return { status: 'ALREADY_CLAIMED', reason: 'Aquest horari ja s’ha activat avui.' };
  if (!Array.isArray(schedule.days) || !schedule.days.includes(now.getDay())) return { status: 'WAIT', reason: 'Avui no és un dia programat.' };
  const target = parseClock(schedule.startTime);
  if (target === null) return { status: 'BLOCKED', reason: 'Hora programada invàlida.' };
  const current = now.getHours() * 60 + now.getMinutes(), delta = current - target;
  if (delta < 0) return { status: 'WAIT', reason: 'Encara no és l’hora programada.' };
  if (delta > schedule.windowMinutes) return { status: 'MISSED', reason: 'S’ha superat la finestra d’inici.' };
  if (!safety || safety.safetyValidated !== true) return { status: 'BLOCKED', reason: 'La validació de seguretat PR #7 no està completa.' };
  if (!safety.jmriConnected) return { status: 'BLOCKED', reason: 'JMRI no està connectat.' };
  if (safety.jmriPower !== 'ON') return { status: 'BLOCKED', reason: 'L’alimentació JMRI no està confirmada com a ON.' };
  if (!safety.bridgeConnected || !safety.dccArmed) return { status: 'BLOCKED', reason: 'WiThrottle desconnectat o DCC no armat.' };
  if (!safety.sensorDataValid) return { status: 'BLOCKED', reason: 'Les lectures de sensors no són vàlides.' };
  const route = safety.route;
  if (!route || route.id !== schedule.routeId || !Array.isArray(route.blockIds) || route.blockIds.length < 2) return { status: 'BLOCKED', reason: 'Itinerari invàlid o no coincident amb l’horari.' };
  const states = safety.blockStates || {};
  if (states[route.blockIds[0]] !== 'OCCUPIED') return { status: 'BLOCKED', reason: 'El primer bloc no està confirmat com a ocupat.' };
  for (const blockId of route.blockIds.slice(1)) {
    if (states[blockId] !== 'FREE') return { status: 'BLOCKED', reason: 'El bloc ' + blockId + ' no està confirmat com a lliure.' };
  }
  const reservations = safety.reservations || {};
  for (const blockId of route.blockIds) {
    if (reservations[blockId] && reservations[blockId] !== route.id) return { status: 'BLOCKED', reason: 'Conflicte de reserva al bloc ' + blockId + '.' };
  }
  const turnoutStates = safety.turnoutStates || {};
  for (const required of schedule.requiredTurnouts || []) {
    const actual = turnoutStates[required.systemName];
    if (!required.systemName || required.verified !== true || !TURNOUT_STATES.includes(required.state) ||
        !actual || actual.verified !== true || actual.state !== required.state) {
      return { status: 'BLOCKED', reason: 'Desviament ' + (required.systemName || '(sense nom)') + ' no verificat o no alineat.' };
    }
  }
  return { status: 'READY', reason: 'Preflight complet. Cal confirmació explícita de l’operador; no s’envia cap ordre física.' };
}

function claimExecution(state, scheduleId, now, safety) {
  const schedule = state.schedules.find(item => item.id === scheduleId);
  if (!schedule) return { ok: false, error: 'Horari inexistent.' };
  const key = dateKey(now);
  if (!key) return { ok: false, status: 'BLOCKED', error: 'Data/hora invàlida.' };
  const duplicate = state.executions.some(item => item.scheduleId === scheduleId && item.date === key);
  const decision = evaluateSchedule(schedule, now, safety, duplicate);
  if (decision.status !== 'READY') return { ok: false, status: decision.status, error: decision.reason };
  const execution = { id: 'SCH-' + scheduleId + '-' + key, scheduleId, date: key, status: 'CLAIMED', createdAt: now.getTime(), reason: decision.reason };
  state.executions.push(execution);
  return { ok: true, execution, decision };
}

function completeExecution(state, executionId) {
  const item = state.executions.find(entry => entry.id === executionId);
  if (!item || item.status !== 'CLAIMED') return { ok: false, error: 'Execució no disponible per completar.' };
  item.status = 'COMPLETED'; item.completedAt = Date.now();
  return { ok: true, execution: item };
}

function failExecution(state, executionId, reason) {
  const item = state.executions.find(entry => entry.id === executionId);
  if (!item || item.status !== 'CLAIMED') return { ok: false, error: 'Execució no disponible per marcar com a fallida.' };
  item.status = 'FAULT'; item.fault = String(reason || 'Fallada sense detall');
  return { ok: true, execution: item };
}

module.exports = { MAX_SCHEDULE_SPEED, TURNOUT_STATES, parseClock, dateKey, validateSchedule, createState, addSchedule, evaluateSchedule, claimExecution, completeExecution, failExecution };
