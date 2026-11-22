'use strict';

const { app, BrowserWindow, dialog, ipcMain, Menu } = require('electron');
const fs = require('node:fs/promises');
const { spawn } = require('node:child_process');
const path = require('node:path');
const { createProjectPathPolicy } = require('./project-path-policy.cjs');

let bridge = null;
let quitting = false;
let mainWindow = null;
const approvedProjectPaths = createProjectPathPolicy();
const MAX_PROJECT_BYTES = 25 * 1024 * 1024;

const projectFilters = [
  { name: 'Projectes JMRI TC', extensions: ['json'] },
  { name: 'XTrackCAD Layout', extensions: ['xtc', 'xti', 'xtm'] },
  { name: 'Tots els fitxers', extensions: ['*'] }
];

function installProjectHandlers() {
  ipcMain.handle('project:open', async () => {
    const result = await dialog.showOpenDialog(mainWindow, { properties: ['openFile'], filters: projectFilters });
    if (result.canceled || !result.filePaths[0]) return { canceled: true };
    const filePath = result.filePaths[0];
    const content = await fs.readFile(filePath, 'utf8');
    approvedProjectPaths.authorize(filePath);
    return { canceled: false, filePath, fileName: path.basename(filePath), content };
  });
  ipcMain.handle('project:save', async (_event, options = {}) => {
    let filePath = '';
    if (options.saveAs === true || typeof options.filePath !== 'string' || !options.filePath) {
      const result = await dialog.showSaveDialog(mainWindow, {
        defaultPath: path.basename(typeof options.defaultPath === 'string' ? options.defaultPath : 'JMRI_TC_Gold_Custom_layout.json'),
        filters: projectFilters.slice(0, 2)
      });
      if (result.canceled || !result.filePath) return { canceled: true };
      filePath = result.filePath;
      approvedProjectPaths.authorize(filePath);
    } else {
      filePath = options.filePath;
      if (!approvedProjectPaths.isAuthorized(filePath)) {
        throw new Error('Ruta de desat no autoritzada. Utilitza el diàleg natiu de desat.');
      }
    }
    const content = String(options.content ?? '');
    if (Buffer.byteLength(content, 'utf8') > MAX_PROJECT_BYTES) {
      throw new Error('El projecte supera la mida màxima permesa de 25 MB.');
    }
    await fs.writeFile(filePath, content, { encoding: 'utf8', flag: 'w' });
    return { canceled: false, filePath, fileName: path.basename(filePath) };
  });
  ipcMain.handle('project:confirm-discard', async () => {
    const result = await dialog.showMessageBox(mainWindow, {
      type: 'warning', buttons: ['Cancel·lar', 'Descartar canvis'], defaultId: 0, cancelId: 0,
      title: 'Canvis sense desar', message: 'Hi ha canvis sense desar. Vols descartar-los?'
    });
    return result.response === 1;
  });
}

function installApplicationMenu() {
  const send = command => mainWindow?.webContents.send('app:command', command);
  const template = [{ label: 'Fitxer', submenu: [
    { label: 'Nou', accelerator: 'CmdOrCtrl+N', click: () => send('new') },
    { label: 'Obrir…', accelerator: 'CmdOrCtrl+O', click: () => send('open') },
    { type: 'separator' },
    { label: 'Guardar', accelerator: 'CmdOrCtrl+S', click: () => send('save') },
    { label: 'Guardar com…', accelerator: 'CmdOrCtrl+Shift+S', click: () => send('saveAs') },
    { type: 'separator' },
    { label: 'Importar XTrackCAD…', click: () => send('importXtc') },
    { label: 'Exportar XTrackCAD…', click: () => send('exportXtc') },
    { type: 'separator' },
    { role: 'quit', label: 'Sortir' }
  ] }, { label: 'Edició', submenu: [{ role: 'undo' }, { role: 'redo' }, { type: 'separator' }, { role: 'cut' }, { role: 'copy' }, { role: 'paste' }] }, { label: 'Ajuda', submenu: [{ role: 'about' }] }];
  Menu.setApplicationMenu(Menu.buildFromTemplate(template));
}

function startBridge() {
  return new Promise((resolve, reject) => {
    const bridgePath = path.join(app.getAppPath(), 'traffic-bridge.js');
    bridge = spawn(process.execPath, [bridgePath], {
      env: {
        ...process.env,
        ELECTRON_RUN_AS_NODE: '1',
        PORT: '0',
        // A desktop install must always start in the fail-safe state.
        JMRI_TC_ENABLE_DCC: '0',
        JMRI_TC_SAFETY_VALIDATED: '0'
      },
      stdio: ['ignore', 'pipe', 'pipe', 'ipc'],
      windowsHide: true
    });

    let settled = false;
    const timeout = setTimeout(() => finish(new Error('El pont local no ha iniciat en 15 segons.')), 15000);
    const finish = error => {
      if (settled) return;
      settled = true;
      clearTimeout(timeout);
      if (error) reject(error);
    };

    bridge.on('message', message => {
      if (message && message.type === 'ready' && Number.isInteger(message.port)) {
        if (settled) return;
        settled = true;
        clearTimeout(timeout);
        resolve(message.port);
      }
    });
    bridge.once('error', error => finish(error));
    bridge.once('exit', (code, signal) => {
      if (!quitting) finish(new Error(`El pont local s'ha tancat (codi ${code ?? signal}).`));
    });
    bridge.stdout.on('data', chunk => console.log('[JMRI bridge]', chunk.toString().trim()));
    bridge.stderr.on('data', chunk => console.error('[JMRI bridge]', chunk.toString().trim()));
  });
}

async function createWindow() {
  const port = await startBridge();
  const window = new BrowserWindow({
    width: 1500,
    height: 950,
    minWidth: 1024,
    minHeight: 700,
    show: false,
    autoHideMenuBar: true,
    backgroundColor: '#101b28',
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      preload: path.join(__dirname, 'preload.cjs')
    }
  });
  mainWindow = window;
  window.once('ready-to-show', () => window.show());
  await window.loadURL(`http://127.0.0.1:${port}`);
  window.on('closed', () => {
    mainWindow = null;
    if (app.quit) app.quit();
  });
}

app.whenReady().then(() => { installProjectHandlers(); installApplicationMenu(); return createWindow(); }).catch(async error => {
  console.error(error);
  await dialog.showMessageBox({
    type: 'error',
    title: 'JMRI TC Gold Custom',
    message: 'No s’ha pogut iniciar el pont local.',
    detail: error.message
  });
  app.quit();
});

app.on('before-quit', () => {
  quitting = true;
  if (bridge && !bridge.killed) bridge.kill();
});

app.on('window-all-closed', () => app.quit());
