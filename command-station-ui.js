'use strict';
(() => {
  const core=window.JMRI_TC_COMMAND_STATION_CORE;
  if(!core||!document.getElementById('commandStationPanel'))return;
  const $=id=>document.getElementById(id);
  const STORE='jmri-tc-command-stations-v1';
  let db=core.createDatabase(),editingId=null;
  try{const saved=localStorage.getItem(STORE);if(saved){const parsed=core.validateDatabase(JSON.parse(saved));if(parsed.ok)db=parsed.value;}}catch{}
  const message=value=>{$('csMessage').textContent=value;};
  const persist=()=>localStorage.setItem(STORE,JSON.stringify(db));
  const option=(value,label)=>{const o=document.createElement('option');o.value=value;o.textContent=label;return o;};
  function manufacturers(){
    const values=[...new Set(core.CATALOGUE.map(x=>x.manufacturer))];
    $('csManufacturer').replaceChildren(...values.map(x=>option(x,x)));
  }
  function models(){
    const rows=core.CATALOGUE.filter(x=>x.manufacturer===$('csManufacturer').value);
    $('csModel').replaceChildren(...rows.flatMap(x=>x.models.map(m=>option(m,m))));
    protocols();interfaces();
  }
  function protocols(){
    const row=core.CATALOGUE.find(x=>x.manufacturer===$('csManufacturer').value&&x.models.includes($('csModel').value));
    $('csProtocol').replaceChildren(...(row?.protocols||[]).map(x=>option(x,x)));
  }
  function interfaces(){
    const row=core.CATALOGUE.find(x=>x.manufacturer===$('csManufacturer').value&&x.models.includes($('csModel').value));
    $('csInterface').replaceChildren(...(row?.interfaces||[]).map(x=>option(x,x)));
    connectionFields();
  }
  function connectionFields(){
    const serial=['USB','Serial'].includes($('csInterface').value);
    $('csSerialFields').classList.toggle('hidden',!serial);
    $('csNetworkFields').classList.toggle('hidden',serial);
  }
  function render(){
    const list=$('csStationList');list.replaceChildren();
    for(const station of db.stations){
      const row=document.createElement('div');row.className='cs-station-row';
      const details=document.createElement('div');const title=document.createElement('strong');title.textContent=station.name;
      const sub=document.createElement('small');sub.textContent=station.manufacturer+' · '+station.model+' · '+station.protocol+' · '+station.interface;
      const status=document.createElement('span');status.className='cs-not-tested';status.textContent=station.catalogueVerification==='UNCONFIRMED'?'Model no verificat · confirmar referència i manual':'Configurada · connexió no provada';
      details.append(title,sub,status);
      const actions=document.createElement('div');
      const select=document.createElement('button');select.type='button';select.textContent=db.selectedStationId===station.id?'Seleccionada':'Seleccionar';select.onclick=()=>{core.selectStation(db,station.id);persist();render();};
      const edit=document.createElement('button');edit.type='button';edit.textContent='Editar';edit.onclick=()=>loadStation(station);
      const remove=document.createElement('button');remove.type='button';remove.textContent='Eliminar';remove.onclick=()=>{core.removeStation(db,station.id);persist();render();message('Central eliminada del perfil local.');};
      actions.append(select,edit,remove);row.append(details,actions);list.append(row);
    }
    $('csSelected').textContent=db.stations.find(x=>x.id===db.selectedStationId)?.name||'Cap central seleccionada';
    $('csCount').textContent=String(db.stations.length);
    $('csSafetyState').textContent='SORTIDES FÍSIQUES BLOQUEJADES · CONFIGURACIÓ NO CERTIFICADA';
  }
  function loadStation(s){
    editingId=s.id;$('csName').value=s.name;$('csManufacturer').value=s.manufacturer;models();
    $('csModel').value=s.model;protocols();$('csProtocol').value=s.protocol;interfaces();$('csInterface').value=s.interface;connectionFields();
    $('csHost').value=s.connection.host||'';$('csPort').value=s.connection.port||'';$('csSerialPort').value=s.connection.serialPort||'';$('csBaud').value=s.connection.baudRate||9600;$('csPath').value=s.connection.path||'/';$('csTimeout').value=s.connection.timeoutMs||5000;$('csNotes').value=s.notes||'';
    $('csSave').textContent='Actualitzar central';message('Editant '+s.name);
  }
  manufacturers();models();
  $('csManufacturer').onchange=models;$('csModel').onchange=()=>{protocols();interfaces();};$('csInterface').onchange=connectionFields;
  $('csSave').onclick=()=>{
    const id=editingId||'CS-'+Date.now().toString(36)+'-'+Math.random().toString(36).slice(2,7);
    const input={id,name:$('csName').value,manufacturer:$('csManufacturer').value,model:$('csModel').value,protocol:$('csProtocol').value,interface:$('csInterface').value,
      connection:{host:$('csHost').value,port:$('csPort').value,serialPort:$('csSerialPort').value,baudRate:$('csBaud').value,path:$('csPath').value,timeoutMs:$('csTimeout').value},notes:$('csNotes').value};
    const saved=core.upsertStation(db,input);if(!saved.ok){message(saved.error);return;}db=saved.value;editingId=null;$('csSave').textContent='Desar central';persist();render();message('Configuració desada localment. No s’ha obert cap connexió ni habilitat cap sortida.');
  };
  $('csNew').onclick=()=>{editingId=null;$('csName').value='';$('csHost').value='';$('csPort').value='';$('csSerialPort').value='';$('csBaud').value='9600';$('csPath').value='/';$('csTimeout').value='5000';$('csNotes').value='';$('csSave').textContent='Desar central';message('Nova configuració.');};
  $('csExport').onclick=()=>{const blob=new Blob([JSON.stringify(db,null,2)],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='jmri-tc-command-stations.json';a.click();URL.revokeObjectURL(url);};
  $('csImport').onclick=()=>$('csImportFile').click();
  $('csImportFile').onchange=async e=>{const f=e.target.files?.[0];if(!f)return;try{const parsed=core.validateDatabase(JSON.parse(await f.text()));if(!parsed.ok)throw new Error(parsed.error);db=parsed.value;persist();render();message('Perfil importat i validat. Connexions i sortides continuen desactivades.');}catch(err){message('Importació rebutjada: '+err.message);}e.target.value='';};
  render();
  window.JMRI_TC_COMMAND_STATIONS={getDatabase:()=>JSON.parse(JSON.stringify(db)),isPhysicalOutputEnabled:()=>false};
})();
