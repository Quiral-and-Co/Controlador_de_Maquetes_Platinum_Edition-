'use strict';
const assert=require('node:assert/strict');
const c=require('../command-station-core');
const db=c.createDatabase();
const base={id:'CS-1',name:'Central principal',manufacturer:'Märklin',model:'Central Station 2',protocol:'MFX',interface:'TCP',connection:{host:'192.168.1.50',port:15731,timeoutMs:5000}};
assert.equal(c.validateStation(base).ok,true);
assert.equal(c.validateStation({...base,protocol:'LocoNet'}).ok,false,'protocol must be supported by selected model');
assert.equal(c.validateStation({...base,interface:'Serial'}).ok,false,'interface must be supported by selected model');
assert.equal(c.validateStation({...base,connection:{host:'bad host',port:70000}}).ok,false,'network settings must be validated');
assert.equal(c.validateStation({...base,connection:{host:'192.168.1.50',port:15731,timeoutMs:100}}).ok,false,'timeout bounds must be enforced');
assert.equal(c.validateStation({...base,manufacturer:'Unknown'}).ok,false,'unknown manufacturer/model rejected');
assert.equal(c.upsertStation(db,base).ok,true);
assert.equal(c.upsertStation(db,{...base,id:'CS-2',name:'Z21',manufacturer:'Roco',model:'Z21',protocol:'DCC',interface:'Wi-Fi',connection:{host:'192.168.1.51',port:21105}}).ok,true);
assert.equal(db.stations.length,2);
assert.equal(c.selectStation(db,'CS-2').ok,true);
assert.equal(db.selectedStationId,'CS-2');
assert.equal(c.validateDatabase({...db,stations:[base,base]}).ok,false,'duplicate IDs rejected');
assert.equal(c.validateDatabase({...db,selectedStationId:'missing'}).ok,false,'dangling selection rejected');
assert.equal(c.removeStation(db,'CS-2').ok,true);
assert.equal(db.selectedStationId,'CS-1','selection falls back to remaining station');
assert.equal(db.physicalOutputsEnabled,false);
assert.equal(c.validateStation({...base,manufacturer:'DCC-EX',model:'EX-CommandStation',protocol:'DCC-EX Native',interface:'Wi-Fi',connection:{host:'192.168.1.60',port:2560}}).ok,true);
assert.equal(c.validateStation({...base,manufacturer:'Digitrax',model:'DCS52',protocol:'LocoNet',interface:'USB',connection:{serialPort:'COM4',baudRate:19200}}).ok,true);
const uhlenbrockCases=[
  ['Intellibox','LocoNet','Serial',{serialPort:'COM5',baudRate:19200}],
  ['Intellibox II','LocoNet','USB',{serialPort:'COM6',baudRate:115200}],
  ['Intellibox II Neo','LocoNet over TCP','Wi-Fi',{host:'192.168.0.111',port:1234}],
  ['Intellibox 3','Z21 LAN','Wi-Fi',{host:'192.168.0.111',port:21105}]
];
for(const [model,protocol,iface,connection] of uhlenbrockCases){
  assert.equal(c.validateStation({...base,id:'IB-'+model,name:model,manufacturer:'Uhlenbrock',model,protocol,interface:iface,connection}).ok,true,model+' supported connection');
}
assert.equal(c.validateStation({...base,manufacturer:'Uhlenbrock',model:'Intellibox II Neo',protocol:'WiThrottle',interface:'USB',connection:{serialPort:'COM7',baudRate:115200}}).ok,false,'Neo must reject WiThrottle over USB');
const digikeijs=c.validateStation({...base,manufacturer:'Digikeijs',model:'ADJ9000 (referència pendent de confirmar)',protocol:'LocoNet over TCP',interface:'TCP',connection:{host:'192.168.1.80',port:5550}});
assert.equal(digikeijs.ok,true);
assert.equal(digikeijs.value.catalogueVerification,'UNCONFIRMED','ADJ9000 must remain explicitly unverified');
assert.equal(c.validateStation({...base,manufacturer:'YaMoRC',model:'YaMoRC (model segons unitat)',protocol:'LocoNet over TCP',interface:'TCP',connection:{host:'192.168.1.90',port:1234}}).ok,true);
assert.equal(c.validateStation({...base,manufacturer:'YaMoRC',model:'YaMoRC (model segons unitat)',protocol:'LocoNet',interface:'Wi-Fi',connection:{host:'192.168.1.90',port:1234}}).ok,false,'YaMoRC LocoNet serial mode must reject Wi-Fi');

console.log('Command station configuration tests passed.');
