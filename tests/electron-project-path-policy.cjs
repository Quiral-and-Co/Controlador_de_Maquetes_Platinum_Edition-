'use strict';

const assert = require('node:assert/strict');
const path = require('node:path');
const { createProjectPathPolicy } = require('../electron/project-path-policy.cjs');

const policy = createProjectPathPolicy();
const selected = path.resolve('fixtures/layout.json');
const unrelated = path.resolve('unrelated/important.txt');

assert.equal(policy.isAuthorized(selected), false, 'Renderer-supplied paths must not be trusted by default.');
assert.equal(policy.isAuthorized(unrelated), false);
assert.equal(policy.authorize(selected), selected);
assert.equal(policy.isAuthorized(selected), true, 'A path selected by the native open/save dialog may be reused for Save.');
assert.equal(policy.isAuthorized(unrelated), false, 'Authorizing one path must not authorize another.');
assert.equal(policy.isAuthorized(''), false);
assert.throws(() => policy.authorize(''), /ruta/);

console.log('PASS: Electron project writes are limited to paths authorized by native dialogs.');
