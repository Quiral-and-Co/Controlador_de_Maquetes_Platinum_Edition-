(() => {
'use strict';
const root=window,core=root.JMRI_TC_ValidationMasterRegistry,physical=root.JMRI_TC_PhysicalValidationCore;
if(!core||!physical)return;
const $=id=>document.getElementById(id);let timer=null,installed=false;
const LEGACY_STORAGE_KEY='jmri-tc-validation-master-registry-v1';
function createProjectId(){return 'JMRI-TC-PROJECT-'+(root.crypto?.randomUUID?.()||Date.now().toString(36)+'-'+Math.random().toString(36).slice(2));}
function state(){const c=root.JMRI_TC_DISPATCHER?.getConfig?.()||{};const projectId=String(c.projectId||c.validationMasterRegistry?.projectId||createProjectId());c.projectId=projectId;const storageKey=core.storageKey(projectId);let saved=c.validationMasterRegistry;if(!saved){saved=core.load(root.localStorage,storageKey);if(saved.projectId!==projectId){const legacy=core.load(root.localStorage,LEGACY_STORAGE_KEY);saved=legacy.projectId===projectId?legacy:{projectId};}}const s=core.createState(saved);s.projectId=projectId;core.reconcile(s,c);core.updateValidation(s,c,physical);c.validationMasterRegistry=s;const persistence=core.save(s,root.localStorage,storageKey);return{c,s,persistence,storageKey};}
function download(name,content,type){const blob=new Blob([content],{type}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
function render(){const host=$('validationMasterRegistry');if(!host)return;const {c,s,persistence,storageKey}=state();host.replaceChildren();
 const summary=document.createElement('p');summary.textContent='Registres: '+s.records.filter(r=>r.present!==false).length+' · esdeveniments: '+s.audit.length+' · projecte JSON: actualitzat en memòria · navegador: '+(persistence.ok?'desat':'ERROR: '+persistence.error);host.append(summary);
 const table=document.createElement('div');table.className='validation-registry-list';
 s.records.filter(r=>r.present!==false).forEach(r=>{const row=document.createElement('article');row.className='validation-entry '+String(r.status).toLowerCase().replaceAll('_','-');const title=document.createElement('strong');title.textContent=r.registryId+' · '+r.name+' · '+r.status;row.append(title);const meta=document.createElement('p');meta.textContent=r.type+' · ID font: '+r.sourceId+(r.levelName?' · '+r.levelName:'');row.append(meta);table.append(row);});host.append(table);
 const controls=document.createElement('div');controls.className='signal-controls';
 const act=document.createElement('button');act.type='button';act.textContent='Generar acta Markdown + JSON';act.addEventListener('click',()=>{const fresh=state(),a=core.buildAct(fresh.s,fresh.c);download(a.id+'.md',a.markdown,'text/markdown;charset=utf-8');download(a.id+'.json',JSON.stringify(a,null,2),'application/json');});controls.append(act);
 const json=document.createElement('button');json.type='button';json.textContent='Exportar registre JSON';json.addEventListener('click',()=>download('validation-master-registry.json',core.serialize(s),'application/json'));controls.append(json);
 const refresh=document.createElement('button');refresh.type='button';refresh.textContent='Actualitzar registre';refresh.addEventListener('click',render);controls.append(refresh);host.append(controls);
 const note=document.createElement('label');note.textContent='Anotació de traçabilitat ';const input=document.createElement('input');input.type='text';input.placeholder='Nota de revisió / incidència';note.append(input);const add=document.createElement('button');add.type='button';add.textContent='Afegir nota';add.addEventListener('click',()=>{const id=s.records.find(r=>r.present!==false)?.registryId;if(!id){alert('No hi ha elements al registre.');return;}const result=core.appendNote(s,id,input.value);if(!result.ok)alert(result.error);else{core.save(s,root.localStorage,storageKey);input.value='';render();}});note.append(add);host.append(note);
}
function install(){if(installed||!$('validationMasterRegistry'))return;installed=true;render();timer=setInterval(render,2500);}
root.JMRI_TC_VALIDATION_MASTER_REGISTRY_UI={refresh:render,stop(){if(timer)clearInterval(timer);timer=null;}};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();
