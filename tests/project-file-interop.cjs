'use strict';
const assert = require('node:assert/strict');
require('../project-file-interop.js');
const io = globalThis.JMRI_TC_PROJECT_FILES;
const config = io.baseConfig();
config.name = 'Round trip XTC';
config.levels[0].tracks.push(
  { id:'S1', type:'straight', name:'Recta', x:100, y:200, rotation:0, levelId:1, geometry:{lengthMm:180} },
  { id:'C1', type:'curve', name:'Corba', x:500, y:300, rotation:30, levelId:1, geometry:{radiusMm:360, angleDeg:30, curveDirection:'right'} }
);
config.levels[1].tracks.push({id:'S2',type:'straight',name:'Nivell 2',x:250,y:400,rotation:90,levelId:2,geometry:{lengthMm:90}});
const saved = io.exportXtc(config);
assert.equal(saved.report.exported, 3);
assert.match(saved.content, /^#XTrackCAD/m);
assert.match(saved.content, /END\$TRACKS/);
const loaded = io.parseXtc(saved.content);
assert.equal(loaded.report.imported, 3);
assert.equal(loaded.config.levels[0].tracks.length, 2);
assert.equal(loaded.config.levels[1].tracks.length, 1);
assert.ok(Math.abs(loaded.config.levels[0].tracks[0].geometry.lengthMm - 180) < 0.1);
assert.ok(Math.abs(loaded.config.levels[0].tracks[1].geometry.radiusMm - 360) < 0.1);
assert.throws(() => io.parseXtc('this is not an XTC file'), /no sembla/);
const unsupported = io.exportXtc({...config, levels: config.levels.map((l,i)=>({...l,tracks:i===0?[...l.tracks,{id:'T1',type:'turnout',name:'Desviament'}]:l.tracks}))});
assert.ok(unsupported.report.unsupported.some(x=>x.includes('turnout')));

const metadataConfig = JSON.parse(JSON.stringify(config));
metadataConfig.levels[0].blocks.push({id:'B01',name:'Bloc 01',trackIds:['S1'],lengthMm:180,sensorIds:['LS1'],verified:false});
metadataConfig.levels[0].sensors.push({id:'LS1',systemName:'LS101',userName:'Detector 101',invert:false,verified:false});
metadataConfig.levels[0].tracks[0].blockId='B01';
metadataConfig.levels[0].tracks[0].sensorName='LS101';
metadataConfig.connections.push({id:'CONN-1',levelId:1,from:{pieceId:'S1',port:'A'},to:{pieceId:'C1',port:'B'},verified:false,method:'manual'});
const metadataExport=io.exportXtc(metadataConfig);
assert.ok(metadataExport.report.unsupported.some(x=>x.includes('assignacions de bloc/sensor')));
assert.ok(metadataExport.report.unsupported.some(x=>x.includes('registre(s) de bloc')));
assert.ok(metadataExport.report.unsupported.some(x=>x.includes('registre(s) de sensor')));
assert.ok(metadataExport.report.unsupported.some(x=>x.includes('connexió(ns) de ports')));
const metadataImport=io.parseXtc(saved.content,metadataConfig);
assert.equal(metadataImport.config.levels[0].blocks.length,1,'XTC import must preserve project block registry');
assert.equal(metadataImport.config.levels[0].sensors.length,1,'XTC import must preserve project sensor registry');
assert.deepEqual(metadataImport.config.levels[0].blocks[0].trackIds,[],'XTC import must clear stale piece assignments');
assert.ok(metadataImport.report.unsupported.some(x=>x.includes('S’han conservat 1 bloc')));
assert.ok(metadataImport.report.unsupported.some(x=>x.includes('S’han conservat 1 sensor')));
assert.ok(metadataImport.report.unsupported.some(x=>x.includes('S’han descartat 1 connexió')));

console.log('PASS: XTrackCAD XTC parser/writer round-trip for straight, curve and levels; unsupported elements are reported.');
