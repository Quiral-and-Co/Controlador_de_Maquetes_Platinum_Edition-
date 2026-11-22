'use strict';

(function(root,factory){
  const api=factory();
  if(typeof module==='object'&&module.exports) module.exports=api;
  if(root) root.JMRI_TC_COMMAND_STATION_CORE=api;
})(typeof globalThis!=='undefined'?globalThis:this,function(){
  const CATALOGUE=Object.freeze([
    {id:'marklin-cs2',manufacturer:'Märklin',models:['Central Station 2'],protocols:['MFX','MM2','DCC'],interfaces:['TCP']},
    {id:'marklin-cs3',manufacturer:'Märklin',models:['Central Station 3','Central Station 3+'],protocols:['MFX','MM2','DCC'],interfaces:['TCP']},
    {id:'esu-ecos',manufacturer:'ESU',models:['ECoS 50200','ECoS 50210'],protocols:['DCC','MM2','MFX'],interfaces:['TCP']},
    {id:'roco-z21',manufacturer:'Roco',models:['Z21','z21','z21 start'],protocols:['DCC','MM2','XpressNet','Z21 LAN'],interfaces:['TCP','Wi-Fi']},
    {id:'digitrax-dcs',manufacturer:'Digitrax',models:['DCS52','DCS100','DCS200','DCS210','DCS240'],protocols:['DCC','LocoNet'],interfaces:['USB','Serial','TCP']},
    {id:'lenz-lzv',manufacturer:'Lenz',models:['LZV100','LZV200'],protocols:['DCC','XpressNet'],interfaces:['USB','Serial','TCP']},
    {id:'nce-powercab',manufacturer:'NCE',models:['PowerCab','Powerhouse Pro'],protocols:['DCC'],interfaces:['USB','Serial','TCP']},
    {id:'hornby-elite',manufacturer:'Hornby',models:['Elite'],protocols:['DCC'],interfaces:['USB','Serial']},
    {id:'piko-smartcontrol',manufacturer:'PIKO',models:['SmartControl','SmartControl WLAN'],protocols:['DCC'],interfaces:['TCP','Wi-Fi']},
    {id:'dcc-ex',manufacturer:'DCC-EX',models:['EX-CommandStation'],protocols:['DCC-EX Native','DCC'],interfaces:['USB','Serial','TCP','Wi-Fi']},
    {id:'sprog',manufacturer:'SPROG',models:['SPROG 3','SPROG 4','SPROG 5'],protocols:['DCC'],interfaces:['USB','Serial']},
    {id:'uhlenbrock-ib1',manufacturer:'Uhlenbrock',models:['Intellibox','Intellibox I'],protocols:['LocoNet'],interfaces:['Serial']},
    {id:'uhlenbrock-ib2',manufacturer:'Uhlenbrock',models:['Intellibox II'],protocols:['LocoNet'],interfaces:['USB']},
    {id:'uhlenbrock-ib2neo',manufacturer:'Uhlenbrock',models:['Intellibox II Neo'],protocols:['LocoNet','LocoNet over TCP','Z21 LAN','WiThrottle'],interfaces:['USB','Wi-Fi','TCP'],modes:[{protocol:'LocoNet',interfaces:['USB']},{protocol:'LocoNet over TCP',interfaces:['Wi-Fi','TCP']},{protocol:'Z21 LAN',interfaces:['Wi-Fi','TCP']},{protocol:'WiThrottle',interfaces:['Wi-Fi','TCP']}]},
    {id:'uhlenbrock-ib3',manufacturer:'Uhlenbrock',models:['Intellibox 3'],protocols:['LocoNet','LocoNet over TCP','Z21 LAN','WiThrottle'],interfaces:['USB','Wi-Fi','TCP'],modes:[{protocol:'LocoNet',interfaces:['USB']},{protocol:'LocoNet over TCP',interfaces:['Wi-Fi','TCP']},{protocol:'Z21 LAN',interfaces:['Wi-Fi','TCP']},{protocol:'WiThrottle',interfaces:['Wi-Fi','TCP']}]},
    {id:'digikeijs-adj9000',manufacturer:'Digikeijs',models:['ADJ9000 (referència pendent de confirmar)'],protocols:['LocoNet over TCP'],interfaces:['TCP','USB'],catalogueVerification:'UNCONFIRMED'},
    {id:'yamorc-command-station',manufacturer:'YaMoRC',models:['YaMoRC (model segons unitat)'],protocols:['LocoNet over TCP','LocoNet'],interfaces:['TCP','Wi-Fi','USB','Serial'],modes:[{protocol:'LocoNet over TCP',interfaces:['TCP','Wi-Fi']},{protocol:'LocoNet',interfaces:['USB','Serial']}]}
  ]);
  const PROTOCOLS=Object.freeze(['DCC','MM2','MFX','LocoNet','XpressNet','Z21 LAN','DCC-EX Native','SRCP','CAN/CBUS','BiDiB']);
  const INTERFACES=Object.freeze(['TCP','Wi-Fi','USB','Serial','WebSocket']);
  const DEFAULTS=Object.freeze({schemaVersion:1,stations:[],selectedStationId:null,physicalOutputsEnabled:false});
  const clone=value=>JSON.parse(JSON.stringify(value));
  function result(ok,value,error){return ok?{ok:true,value}:{ok:false,error};}
  function findModel(manufacturer,model){return CATALOGUE.find(x=>x.manufacturer===manufacturer&&x.models.includes(model));}
  function validateStation(input){
    if(!input||typeof input!=='object'||Array.isArray(input))return result(false,null,'Configuració de central invàlida.');
    const name=String(input.name||'').trim(),manufacturer=String(input.manufacturer||''),model=String(input.model||''),protocol=String(input.protocol||''),connection=input.connection||{};
    if(!name||name.length>80)return result(false,null,'El nom és obligatori (màxim 80 caràcters).');
    const entry=findModel(manufacturer,model);
    if(!entry)return result(false,null,'Fabricant i model no inclosos al catàleg suportat.');
    if(!entry.protocols.includes(protocol))return result(false,null,'El protocol no està declarat per a aquest model.');
    const type=String(input.interface||connection.type||'');
    if(!entry.interfaces.includes(type))return result(false,null,'La interfície no està declarada per a aquest model.');
    const mode=entry.modes?.find(x=>x.protocol===protocol);
    if(mode&&!mode.interfaces.includes(type))return result(false,null,'La combinació de protocol i interfície no està suportada per a aquest model.');
    const host=String(connection.host||'').trim();
    const port=connection.port===''||connection.port==null?null:Number(connection.port);
    const baud=connection.baudRate===''||connection.baudRate==null?null:Number(connection.baudRate);
    if(['TCP','Wi-Fi','WebSocket'].includes(type)){
      if(!host||host.length>253||/[\s\r\n]/.test(host))return result(false,null,'Cal una adreça IP o host vàlid.');
      if(!Number.isInteger(port)||port<1||port>65535)return result(false,null,'El port TCP ha d’estar entre 1 i 65535.');
    }
    if(['USB','Serial'].includes(type)){
      const serialPort=String(connection.serialPort||'').trim();
      if(!serialPort||serialPort.length>120)return result(false,null,'Cal seleccionar el port USB/sèrie.');
      if(!Number.isInteger(baud)||baud<300||baud>4000000)return result(false,null,'La velocitat sèrie no és vàlida.');
    }
    const timeoutMs=Number(connection.timeoutMs??5000);
    if(!Number.isInteger(timeoutMs)||timeoutMs<250||timeoutMs>60000)return result(false,null,'El timeout ha d’estar entre 250 i 60000 ms.');
    const id=String(input.id||'').trim();
    if(!id||id.length>100)return result(false,null,'Identificador de central invàlid.');
    return result(true,{
      id,name,manufacturer,model,protocol,interface:type,
      connection:{host,port,serialPort:String(connection.serialPort||''),baudRate:baud,path:String(connection.path||'/'),timeoutMs},
      notes:String(input.notes||'').slice(0,1000),
      verified:false,catalogueVerification:entry.catalogueVerification||'DOCUMENTED',connectionStatus:'NOT_TESTED',physicalOutputsEnabled:false
    });
  }
  function validateDatabase(db){
    if(!db||typeof db!=='object'||db.schemaVersion!==1||!Array.isArray(db.stations))return result(false,null,'Esquema de configuració de centrals no vàlid.');
    const ids=new Set();
    const stations=[];
    for(const raw of db.stations){
      const checked=validateStation(raw);
      if(!checked.ok)return checked;
      if(ids.has(checked.value.id))return result(false,null,'Identificador de central duplicat.');
      ids.add(checked.value.id);stations.push(checked.value);
    }
    const selected=db.selectedStationId==null?null:String(db.selectedStationId);
    if(selected!==null&&!ids.has(selected))return result(false,null,'La central seleccionada no existeix.');
    return result(true,{schemaVersion:1,stations,selectedStationId:selected,physicalOutputsEnabled:false});
  }
  function createDatabase(){return clone(DEFAULTS);}
  function upsertStation(db,input){
    const checked=validateStation(input);if(!checked.ok)return checked;
    const index=db.stations.findIndex(x=>x.id===checked.value.id);
    if(index<0)db.stations.push(checked.value);else db.stations[index]=checked.value;
    if(!db.selectedStationId)db.selectedStationId=checked.value.id;
    return validateDatabase(db);
  }
  function selectStation(db,id){
    if(!db.stations.some(x=>x.id===id))return result(false,null,'Central inexistent.');
    db.selectedStationId=id;return result(true,db.selectedStationId);
  }
  function removeStation(db,id){
    db.stations=db.stations.filter(x=>x.id!==id);
    if(db.selectedStationId===id)db.selectedStationId=db.stations[0]?.id||null;
    return validateDatabase(db);
  }
  function getCatalogue(){return clone(CATALOGUE);}
  return {CATALOGUE,PROTOCOLS,INTERFACES,createDatabase,validateStation,validateDatabase,upsertStation,selectStation,removeStation,getCatalogue};
});
