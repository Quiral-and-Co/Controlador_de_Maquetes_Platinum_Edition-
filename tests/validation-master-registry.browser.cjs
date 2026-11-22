'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const http = require('node:http');
const os = require('node:os');
const path = require('node:path');
const { chromium } = require('playwright');

const root = path.resolve(__dirname, '..');
const mime = { '.html':'text/html', '.js':'text/javascript', '.css':'text/css', '.json':'application/json', '.svg':'image/svg+xml' };
const server = http.createServer((req, res) => {
  const pathname = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname);
  const file = path.resolve(root, '.' + pathname);
  if (!file.startsWith(root + path.sep) && file !== path.join(root, 'index.html')) { res.writeHead(403).end(); return; }
  fs.readFile(file, (err, data) => {
    if (err) { res.writeHead(404).end(String(err)); return; }
    res.writeHead(200, { 'content-type': mime[path.extname(file)] || 'application/octet-stream' });
    res.end(data);
  });
});

function samePersistedFields(actual, expected, label) {
  assert.equal(actual.projectId, expected.projectId, label + ': projectId');
  const fields = value => value.records.map(r => ({
    registryId:r.registryId, identityKey:r.identityKey, status:r.status,
    notes:r.notes, validation:r.validation
  }));
  assert.deepEqual(fields(actual), fields(expected), label + ': identifiers, annotations and statuses');
  assert.deepEqual(actual.audit, expected.audit, label + ': audit trail');
}

(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const port = server.address().port;
  const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'vmr-playwright-'));
  let context;
  try {
    context = await chromium.launchPersistentContext(profile, { headless:true, args:['--no-sandbox'] });
    let page = await context.newPage();
    await page.goto('http://127.0.0.1:' + port + '/index.html');
    await page.waitForFunction(() => window.JMRI_TC_DISPATCHER && window.JMRI_TC_VALIDATION_MASTER_REGISTRY_UI);

    const makeProjectFixture = async (projectId, suffix, name) => page.evaluate(({projectId,suffix,name}) => {
      const c = JSON.parse(JSON.stringify(window.JMRI_TC_DISPATCHER.getConfig()));
      c.projectId = projectId;
      c.name = name;
      c.signals = [{ id:'SIG-'+suffix, name:'Senyal '+suffix }];
      c.levels[0].blocks = [{ id:'BLK-'+suffix, name:'Bloc '+suffix, trackIds:[], sensorIds:[], lengthMm:0, verified:false }];
      c.levels[0].sensors = [{ id:'SNS-'+suffix, systemName:'LS-'+suffix, userName:'Sensor '+suffix, verified:false, invert:false }];
      c.levels[0].tracks = [{
        id:'TRK-'+suffix, type:'turnout', name:'Desviament '+suffix,
        x:100, y:100, rotation:0, levelId:1, blockId:'', sensorName:'',
        turnoutSystemName:'LT-'+suffix, geometry:{lengthMm:180,radiusMm:360,angleDeg:30}
      }];
      delete c.validationMasterRegistry;
      return c;
    }, {projectId,suffix,name});

    const importProject = async (project, filename) => {
      await page.locator('#configFile').setInputFiles({
        name:filename, mimeType:'application/json', buffer:Buffer.from(JSON.stringify(project))
      });
      await page.waitForFunction(id =>
        window.JMRI_TC_DISPATCHER.getConfig().projectId === id &&
        window.JMRI_TC_DISPATCHER.getConfig().validationMasterRegistry?.records.length === 4,
        project.projectId
      );
      return page.evaluate(() => JSON.parse(JSON.stringify(
        window.JMRI_TC_DISPATCHER.getConfig().validationMasterRegistry
      )));
    };

    const fixtureA = await makeProjectFixture('vmr-browser-project-A', 'A', 'Projecte de navegador A');
    const projectA = await importProject(fixtureA, 'project-A.json');
    assert.ok(projectA.records.every(r => r.sourceId.endsWith('-A')));

    const noteInput = page.locator('#validationMasterRegistry input[placeholder="Nota de revisió / incidència"]');
    await noteInput.fill('Anotació exclusiva del projecte A');
    await page.getByRole('button', { name:'Afegir nota' }).click();
    await page.waitForFunction(() => {
      const s = window.JMRI_TC_DISPATCHER.getConfig().validationMasterRegistry;
      return s.records.some(r => r.notes.some(n => n.text === 'Anotació exclusiva del projecte A')) &&
        s.audit.some(e => e.action === 'NOTE_ADDED');
    });

    const projectExportDownload = page.waitForEvent('download');
    await page.locator('#exportBtn').click();
    const projectDownload = await projectExportDownload;
    const projectPath = await projectDownload.path();
    assert.ok(projectPath, 'project export must create a browser download');
    const exportedProject = JSON.parse(fs.readFileSync(projectPath, 'utf8'));
    assert.equal(exportedProject.projectId, 'vmr-browser-project-A');
    assert.ok(exportedProject.validationMasterRegistry, 'project export must contain the VMR');
    const exportedA = exportedProject.validationMasterRegistry;
    samePersistedFields(exportedA, await page.evaluate(() => JSON.parse(JSON.stringify(
      window.JMRI_TC_DISPATCHER.getConfig().validationMasterRegistry
    ))), 'project A export');

    const fixtureB = await makeProjectFixture('vmr-browser-project-B', 'B', 'Projecte de navegador B');
    const projectB = await importProject(fixtureB, 'project-B.json');
    assert.notEqual(projectB.projectId, projectA.projectId);
    assert.ok(projectB.records.every(r => r.sourceId.endsWith('-B')), 'project B must contain only its own source records');
    assert.ok(projectB.records.every(r => r.notes.length === 0), 'project A annotations must not leak into project B');
    assert.ok(!projectB.audit.some(e => e.action === 'NOTE_ADDED'), 'project A audit events must not leak into project B');
    const storageKeys = await page.evaluate(() => Object.keys(localStorage).filter(k =>
      k.startsWith('jmri-tc-validation-master-registry-v1:')));
    assert.ok(storageKeys.some(k => k.endsWith('vmr-browser-project-A')));
    assert.ok(storageKeys.some(k => k.endsWith('vmr-browser-project-B')), 'projects must use separate browser storage keys');

    const importedA = await importProject(exportedProject, 'project-A-export.json');
    samePersistedFields(importedA, exportedA, 'project A re-import');

    const captureActs = async () => {
      const downloads = [];
      const onDownload = download => downloads.push(download);
      page.on('download', onDownload);
      await page.getByRole('button', { name:'Generar acta Markdown + JSON' }).click();
      await page.waitForTimeout(500);
      page.off('download', onDownload);
      const names = downloads.map(d => d.suggestedFilename());
      console.log('Captured validation downloads:', names.join(', '));
      const md = names.find(n => n.endsWith('.md'));
      const json = names.find(n => n.endsWith('.json'));
      assert.ok(md && json, 'each validation act must download Markdown and JSON');
      assert.equal(path.basename(md, '.md'), path.basename(json, '.json'), 'Markdown and JSON must refer to the same act');
      return path.basename(md, '.md');
    };
    const actOne = await captureActs();
    const actTwo = await captureActs();
    assert.notEqual(actOne, actTwo, 'two consecutive browser-generated acts must have unique IDs');

    // Close the browser context, reopen Chromium with the same profile, then import the exported project.
    await page.close();
    await context.close();
    context = await chromium.launchPersistentContext(profile, { headless:true, args:['--no-sandbox'] });
    page = await context.newPage();
    await page.goto('http://127.0.0.1:' + port + '/index.html');
    await page.waitForFunction(() => window.JMRI_TC_DISPATCHER && window.JMRI_TC_VALIDATION_MASTER_REGISTRY_UI);
    const afterRestart = await importProject(exportedProject, 'project-A-after-browser-restart.json');
    samePersistedFields(afterRestart, exportedA, 'project A after browser restart and import');

    console.log('PASS: Chromium UI project isolation, project JSON download/import, browser close/reopen, VMR IDs/status/notes/audit persistence, and unique consecutive validation acts');
  } finally {
    if (context) await context.close().catch(() => {});
    await new Promise(resolve => server.close(resolve));
    fs.rmSync(profile, { recursive:true, force:true });
  }
})().catch(err => { console.error(err); process.exitCode = 1; });
