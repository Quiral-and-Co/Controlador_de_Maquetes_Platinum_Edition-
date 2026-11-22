'use strict';
const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('jmriDesktop', Object.freeze({
  openProject: () => ipcRenderer.invoke('project:open'),
  saveProject: options => ipcRenderer.invoke('project:save', options),
  confirmDiscard: () => ipcRenderer.invoke('project:confirm-discard'),
  onCommand: callback => {
    if (typeof callback !== 'function') return () => {};
    const listener = (_event, command) => callback(command);
    ipcRenderer.on('app:command', listener);
    return () => ipcRenderer.removeListener('app:command', listener);
  }
}));
