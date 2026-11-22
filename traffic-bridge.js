'use strict';
const http=require('node:http');
const net=require('node:net');
const DeadmanCore=require('./deadman-core.js');
const fs=require('node:fs');
const path=require('node:path');
const {WebSocketServer,WebSocket}=require('ws');
const ROOT=__dirname,HOST='127.0.0.1',HTTP_PORT=Number(process.env.PORT||3000);
const DCC_ENABLED=process.env.JMRI_TC_ENABLE_DCC==='1';
const SAFETY_VALIDATED=process.env.JMRI_TC_SAFETY_VALIDATED==='1';
const MIME={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.css':'text/css; charset=utf-8','.json':'application/json; charset=utf-8','.svg':'image/svg+xml','.png':'image/png','.jpg':'image/jpeg'};
const server=http.createServer((req,res)=>{
 let pathname;
 try{pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);}catch{res.writeHead(400).end('Bad request');return;}
 const rel=pathname==='/'?'index.html':pathname.replace(/^\/+/,'');
 const file=path.resolve(ROOT,rel);
 if(rel.split('/').some(part=>part.startsWith('.'))||rel.startsWith('node_modules/')||!MIME[path.extname(file).toLowerCase()]||!file.startsWith(ROOT+path.sep)){res.writeHead(403).end('Forbidden');return;}
 fs.readFile(file,(err,data)=>{
  if(err){res.writeHead(err.code==='ENOENT'?404:500).end('Not found');return;}
  res.writeHead(200,{'Content-Type':MIME[path.extname(file).toLowerCase()],'X-Content-Type-Options':'nosniff','Cache-Control':'no-store'});
  res.end(data);
 });
});
const wss=new WebSocketServer({noServer:true,maxPayload:8192});
let activeController=null;
server.on('upgrade',(req,socket,head)=>{
 const origin=req.headers.origin||'';
 if(req.url!=='/traffic-ws'||!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(origin)){socket.write('HTTP/1.1 403 Forbidden\r\n\r\n');socket.destroy();return;}
 wss.handleUpgrade(req,socket,head,ws=>wss.emit('connection',ws,req));
});
function send(ws,obj){if(ws.readyState===WebSocket.OPEN)ws.send(JSON.stringify(obj));}
function validHost(host){return typeof host==='string'&&host.length<=253&&!/[\s/\\]/.test(host);}
wss.on('connection',ws=>{
 if(activeController&&activeController.readyState===WebSocket.OPEN){ws.close(1013,'Another local dispatcher is already connected.');return;}
 activeController=ws;
 let tcp=null,armed=false,connected=false,heartbeat=null,buffer='',acquired=new Set(),pendingAcquire=new Map(),manualLocos=new Set();
 const report=()=>send(ws,{type:'status',connected,armed,dccEnabled:DCC_ENABLED,safetyValidated:SAFETY_VALIDATED,host:tcp?.remoteAddress||null});
 const stopHeartbeat=()=>{if(heartbeat)clearInterval(heartbeat);heartbeat=null;};
 const deadman=DeadmanCore.createState();let deadmanMonitor=null,deadmanLastHeartbeat=0;
 const publishDeadman=()=>send(ws,{type:'deadmanState',phase:deadman.phase,pressed:deadman.pressed,tripped:deadman.tripped});
 const stopDeadmanMonitor=()=>{if(deadmanMonitor)clearInterval(deadmanMonitor);deadmanMonitor=null;};
 const emergencyDeadman=()=>{if(deadman.tripped)return;for(const id of manualLocos){if(connected&&tcp&&!tcp.destroyed)tcp.write('M0A'+id+'<;>X\n');}deadman.tripped=true;deadman.pressed=false;deadman.phase='TRIPPED';deadman.releasedAt=null;armed=false;report();publishDeadman();send(ws,{type:'deadmanTrip',message:'Home mort: ordre d’emergència WiThrottle enviada. Comprova físicament la parada del tren.'});};
 const monitorDeadman=()=>{if(!deadman.enabled||deadman.tripped)return;if(deadman.pressed&&Date.now()-deadmanLastHeartbeat>2000){emergencyDeadman();return;}const result=DeadmanCore.evaluate(deadman,Date.now());if(result.action==='TRIP'){emergencyDeadman();return;}if(result.phase!==deadman.phase)publishDeadman();};
 const startDeadmanMonitor=()=>{if(!deadmanMonitor)deadmanMonitor=setInterval(monitorDeadman,100);};
 const disarm=()=>{armed=false;report();};
 const clearPending=()=>{for(const timer of pendingAcquire.values())clearTimeout(timer);pendingAcquire.clear();};
 const closeTcp=()=>{
  stopHeartbeat();stopDeadmanMonitor();
  if(tcp){try{tcp.write('M0A*<;>X\nM0-*<;>r\nQ\n');}catch{}tcp.destroy();tcp=null;}
  connected=false;acquired.clear();clearPending();disarm();
 };
 const write=line=>{if(!connected||!tcp||tcp.destroyed)throw new Error('WiThrottle no està connectat.');tcp.write(line+'\n');};
 const requireArmed=()=>{if(!DCC_ENABLED||!armed)throw new Error('Control DCC bloquejat. Activa JMRI_TC_ENABLE_DCC=1 i arma el control de seguretat.');if(!connected)throw new Error('WiThrottle desconnectat.');};
 ws.on('message',raw=>{
  let m;try{m=JSON.parse(raw.toString());}catch{return send(ws,{type:'error',message:'Missatge JSON invàlid.'});}
  try{
   if(m.type==='connect'){
    if(tcp)closeTcp();
    const host=String(m.host||'127.0.0.1'),port=Number(m.port||12090);
    if(!validHost(host)||!Number.isInteger(port)||port<1||port>65535)throw new Error('Host o port WiThrottle invàlid.');
    tcp=net.createConnection({host,port});tcp.setKeepAlive(true,10000);tcp.setNoDelay(true);
    tcp.on('connect',()=>{
     connected=true;tcp.write('NJMRI-TC Gold Custom\n');tcp.write('HUJMRI-TC-'+process.pid+'\n');tcp.write('*+\n');
     heartbeat=setInterval(()=>{if(connected)tcp.write('*\n');},5000);
     send(ws,{type:'connected'});report();
    });
    tcp.on('data',chunk=>{
     buffer+=chunk.toString('utf8');let i;
     while((i=buffer.search(/[\r\n]/))>=0){
      const line=buffer.slice(0,i);buffer=buffer.slice(i+1).replace(/^\n/,'');
      if(!line)continue;
      send(ws,{type:'jmri',line});
      for(const [id,timer] of pendingAcquire){
       if(line.startsWith('M0A'+id)){clearTimeout(timer);pendingAcquire.delete(id);acquired.add(id);send(ws,{type:'acquired',locoId:id});}
      }
     }
    });
    tcp.on('error',err=>send(ws,{type:'error',message:'WiThrottle: '+err.message}));
    tcp.on('close',()=>{stopHeartbeat();connected=false;acquired.clear();clearPending();disarm();send(ws,{type:'disconnected'});});
   }else if(m.type==='arm'){
    if(!DCC_ENABLED)throw new Error('DCC desactivat per defecte. Cal iniciar el pont amb JMRI_TC_ENABLE_DCC=1.');
    if(!SAFETY_VALIDATED||m.safetyValidated!==true)throw new Error('Interbloqueig de seguretat: manca JMRI_TC_SAFETY_VALIDATED=1 i validació de la interfície.');
    if(!connected)throw new Error('Connecta primer amb el servidor WiThrottle de JMRI.');
    armed=m.enabled===true;report();send(ws,{type:'armed',armed});
   }else if(m.type==='deadmanConfigure'){const result=DeadmanCore.configure(deadman,m.enabled===true);if(!result.ok)throw new Error(result.error);publishDeadman();
   }else if(m.type==='deadmanPress'){if(!deadman.enabled)throw new Error('Home mort desactivat o no disponible.');if(!manualLocos.size)throw new Error('No hi ha cap locomotora en mode manual.');const result=DeadmanCore.press(deadman,Date.now());if(result.action==='TRIP'||deadman.tripped){emergencyDeadman();throw new Error('Temps de resposta exhaurit: frenat d’emergència activat.');}if(!result.ok)throw new Error(result.error);deadmanLastHeartbeat=Date.now();startDeadmanMonitor();publishDeadman();
   }else if(m.type==='deadmanRelease'){if(!deadman.enabled)throw new Error('Home mort no disponible.');DeadmanCore.release(deadman,Date.now());deadmanLastHeartbeat=Date.now();publishDeadman();
   }else if(m.type==='deadmanRearm'){if(!deadman.tripped)throw new Error('No cal rearmament: el watchdog no està disparat.');DeadmanCore.rearm(deadman);publishDeadman();}else if(m.type==='deadmanHeartbeat'){if(deadman.enabled&&deadman.pressed)deadmanLastHeartbeat=Date.now();
   }else if(m.type==='acquire'){
    requireArmed();
    const id=String(m.locoId||''),match=/^([SL])(\d+)$/.exec(id),n=match?Number(match[2]):NaN;
    if(!match||(match[1]==='S'&&(n<1||n>127))||(match[1]==='L'&&(n<128||n>10239)))throw new Error('Adreça DCC no vàlida.');
    if(m.manual===true){manualLocos.add(id);DeadmanCore.configure(deadman,m.deadmanEnabled!==false);}else manualLocos.delete(id);write('M0+'+id+'<;>'+id);
    const timer=setTimeout(()=>{pendingAcquire.delete(id);send(ws,{type:'acquireTimeout',locoId:id,message:'JMRI no ha confirmat l’adquisició de la locomotora.'});},5000);
    pendingAcquire.set(id,timer);send(ws,{type:'acquiring',locoId:id});
   }else if(m.type==='speed'){
    requireArmed();const id=String(m.locoId||''),speed=Number(m.speed);
    if(!acquired.has(id))throw new Error('La locomotora no està adquirida.');
    if(manualLocos.has(id)&&deadman.enabled&&speed!==0&&(!deadman.pressed||deadman.tripped))throw new Error('Home mort: mantén premuda la tecla per conduir.');
    if(!Number.isInteger(speed)||speed<0||speed>126)throw new Error('Velocitat fora del rang 0–126.');
    write('M0A'+id+'<;>V'+speed);send(ws,{type:'command',command:'speed',locoId:id,speed});
   }else if(m.type==='direction'){
    requireArmed();const id=String(m.locoId||''),direction=Number(m.direction);
    if(!acquired.has(id)||![0,1].includes(direction))throw new Error('Locomotora no adquirida o sentit invàlid.');
    if(manualLocos.has(id)&&deadman.enabled&&(!deadman.pressed||deadman.tripped))throw new Error('Home mort: mantén premuda la tecla per canviar el sentit.');
    write('M0A'+id+'R'+direction);send(ws,{type:'command',command:'direction',locoId:id,direction});
   }else if(m.type==='function'){
    requireArmed();const id=String(m.locoId||''),fn=Number(m.fn);
    if(!acquired.has(id)||!Number.isInteger(fn)||fn<0||fn>32)throw new Error('Locomotora no adquirida o funció invàlida.');
    write('M0A'+id+'<;>F'+(m.enabled?1:0)+fn);send(ws,{type:'command',command:'function',locoId:id,fn,enabled:!!m.enabled});
   }else if(m.type==='release'){
    requireArmed();const id=String(m.locoId||'');
    if(acquired.has(id)){write('M0-'+id+'<;>r');acquired.delete(id);}manualLocos.delete(id);
    send(ws,{type:'released',locoId:id});
   }else if(m.type==='emergencyStop'){
    if(connected&&tcp&&!tcp.destroyed){const id=String(m.locoId||'');if(id&&acquired.has(id))tcp.write('M0A'+id+'<;>X\n');else tcp.write('M0A*<;>X\n');}
    disarm();send(ws,{type:'emergencyStop',message:'Ordre d’emergència enviada a JMRI si la connexió era activa.'});
   }else if(m.type==='disconnect'){closeTcp();send(ws,{type:'disconnected'});}
   else throw new Error('Acció no admesa pel pont.');
  }catch(err){send(ws,{type:'error',message:err.message});}
 });
 ws.on('close',()=>{closeTcp();if(activeController===ws)activeController=null;});
 ws.on('error',closeTcp);
 report();
});
server.listen(HTTP_PORT,HOST,()=>{const address=server.address();const port=address&&typeof address==='object'?address.port:HTTP_PORT;console.log('JMRI-TC local: http://'+HOST+':'+port);console.log('WiThrottle bridge: ws://'+HOST+':'+port+'/traffic-ws');console.log('DCC commands enabled: '+DCC_ENABLED);console.log('Safety validation enabled: '+SAFETY_VALIDATED);if(typeof process.send==='function')process.send({type:'ready',port});});
process.on('SIGINT',()=>{for(const client of wss.clients)client.close();server.close(()=>process.exit(0));});
