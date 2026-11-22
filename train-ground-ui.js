(function(){
'use strict';
const $=id=>document.getElementById(id),core=window.JMRI_TC_TrainGround;
let state;
function apply(result){if(result&&result.ok){state=result.state;render();}else if(result)status(result.error);}
function status(v){$('tgStatus').textContent=v;}
function render(){if(!state)return;
$('tgNetwork').textContent=state.network;$('tgCallState').textContent=({IDLE:'EN ESPERA',CALLING:'TRUCANT…',RINGING:'TRUCADA ENTRANT',CONNECTED:'COMUNICACIÓ OBERTA'})[state.callState];
$('tgRegistration').textContent=state.mode==='GSMR'?(state.registered?'REGISTRAT':'NO REGISTRAT'):'ANALÒGIC';
$('tgRegistration').className='tg-pill '+(state.mode==='GSMR'&&state.registered?'ready':'');
$('tgMode').value=state.mode;$('tgChannel').value=state.channel;$('tgTrainId').value=state.trainId;$('tgRadioId').value=state.radioId;$('tgPeer').value=state.peer;$('tgFunction').value=state.selectedFunction;
$('tgRegister').disabled=state.mode!=='GSMR'||state.registered;$('tgCall').disabled=state.callState!=='IDLE'||(state.mode==='GSMR'&&!state.registered);$('tgAnswer').disabled=!['CALLING','RINGING'].includes(state.callState);$('tgRelease').disabled=state.callState==='IDLE';$('tgTransmit').disabled=state.callState!=='CONNECTED';$('tgPttOn').disabled=state.callState!=='CONNECTED';$('tgPttOff').disabled=!state.ptt;
$('tgPttState').textContent=state.ptt?'PTT · PREMUTAT':'PTT · EN REPÒS';
$('tgMessages').replaceChildren();state.messages.slice().reverse().forEach(m=>{const li=document.createElement('li');li.textContent=m.mode+' · '+m.from+' → '+m.to+': '+m.text;$('tgMessages').append(li);});
$('tgEvents').replaceChildren();state.events.slice().reverse().forEach(e=>{const li=document.createElement('li');li.textContent=e.type+' · '+e.message;$('tgEvents').append(li);});
}
function init(){if(!$('trainGroundPanel'))return;state=core.createState();
$('tgMode').onchange=()=>apply(core.setMode(state,$('tgMode').value));
$('tgRegister').onclick=()=>apply(core.register(state));
$('tgSave').onclick=()=>apply(core.configure(state,{channel:$('tgChannel').value,trainId:$('tgTrainId').value,radioId:$('tgRadioId').value,peer:$('tgPeer').value,selectedFunction:$('tgFunction').value}));
$('tgCall').onclick=()=>{const c=core.configure(state,{peer:$('tgPeer').value,selectedFunction:$('tgFunction').value});if(c.ok)state=c.state;apply(core.call(state,$('tgPeer').value));};
$('tgAnswer').onclick=()=>apply(core.answer(state));
$('tgIncoming').onclick=()=>apply(core.incoming(state,$('tgPeer').value||'Regulació'));
$('tgRelease').onclick=()=>apply(core.release(state));
$('tgTransmit').onclick=()=>apply(core.transmit(state,$('tgText').value));
$('tgPttOn').onclick=()=>apply(core.press(state,'PTT_ON'));$('tgPttOff').onclick=()=>apply(core.press(state,'PTT_OFF'));
$('tgEmergency').onclick=()=>apply(core.press(state,'EMERGENCY'));$('tgClear').onclick=()=>apply(core.press(state,'CLEAR'));
$('tgDialKeys').addEventListener('click',e=>{const b=e.target.closest('button[data-digit]');if(!b)return;$('tgPeer').value+=$('tgPeer').value.length<40?b.dataset.digit:'';});
$('tgMessagesClear').onclick=()=>{state.messages=[];render();};
render();status('Terminal preparat en mode local de simulació.');}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();
