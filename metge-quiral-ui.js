(() => {
'use strict';
const $=id=>document.getElementById(id);
function run(){
 const host=$('metgeQuiralResults'),status=$('metgeQuiralStatus'),summary=$('metgeQuiralSummary');
 if(!host||!window.JMRI_TC_METGE_QUIRAL)return;
 const config=window.JMRI_TC_DISPATCHER?.getConfig?.()||{};
 const runtime=window.JMRI_TC_TRAFFIC_CONTROL?.getSnapshot?.()||{};\n const report=window.JMRI_TC_METGE_QUIRAL.inspect(config,{physicalOutputsEnabled:runtime.physicalOutputsEnabled===true});
 status.textContent=report.status==='HEALTHY'?'SALUTABLE':report.status==='WARNINGS'?'AVISOS':'REVISIÓ NECESSÀRIA';
 status.dataset.status=report.status;
 summary.textContent=report.counts.passed+'/'+report.counts.total+' comprovacions correctes · '+report.counts.errors+' errors · '+report.counts.warnings+' avisos · sortides físiques '+(report.physicalOutputsLocked?'BLOQUEJADES':'NO BLOQUEJADES');
 host.replaceChildren();
 report.checks.forEach(check=>{
  const row=document.createElement('article');row.className='quiral-check '+(check.ok?'ok':check.severity==='warning'?'warning':'error');
  const title=document.createElement('strong');title.textContent=(check.ok?'✓ ':'! ')+check.label;row.append(title);
  if(check.detail){const detail=document.createElement('p');detail.textContent=check.detail;row.append(detail);}
  host.append(row);
 });
 const recommendations=window.JMRI_TC_METGE_QUIRAL.recommendations(report);
 if(recommendations.length){const h=document.createElement('h4');h.textContent='Recomanacions';host.append(h);recommendations.forEach(item=>{const p=document.createElement('p');p.className='small';p.textContent='• '+item.message;host.append(p);});}
 return report;
}
$('metgeQuiralRun')?.addEventListener('click',run);
window.JMRI_TC_METGE_QUIRAL_UI={run};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',run);else run();
})();
