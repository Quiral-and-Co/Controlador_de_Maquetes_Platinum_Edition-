'use strict';

const path = require('node:path');

function createProjectPathPolicy() {
  const approved = new Set();
  const key = value => {
    if (typeof value !== 'string' || !value.trim()) throw new TypeError('La ruta del projecte no és vàlida.');
    return path.resolve(value);
  };
  return Object.freeze({
    authorize(value) {
      const resolved = key(value);
      approved.add(resolved);
      return resolved;
    },
    isAuthorized(value) {
      try { return approved.has(key(value)); }
      catch { return false; }
    }
  });
}

module.exports = { createProjectPathPolicy };
