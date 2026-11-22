# Gestió de circulacions — Märklin DCC (1.2 alpha)

## Arquitectura

La integració separa dos protocols JMRI:

- JSON WebSocket del Web Server (per defecte 12080): lectures dels sensors i estat d'alimentació.
- WiThrottle TCP (port configurable; 12090 és el valor inicial del formulari): adquisició de locomotores, velocitat, sentit i funcions DCC.

El procés local `traffic-bridge.js` exposa la interfície de circulacions a `http://127.0.0.1:3000` i un WebSocket local `/traffic-ws`. El pont escolta només a loopback, valida les ordres i no permet ordres de moviment mentre `JMRI_TC_ENABLE_DCC` no sigui `1` i el control no estigui armat a la interfície.

## Compatibilitat Märklin

JMRI documenta connexió a Märklin Central Station 2/3 mitjançant Märklin CAN per TCP/IP o UDP/IP; TCP és recomanat per a CS2. JMRI també documenta suport de throttle, sensors i alimentació per a CS2. La configuració exacta depèn de la central i del firmware.

A JMRI:
1. Obre **Edit → Preferences → Connections**.
2. Selecciona fabricant **MARKLIN** i la connexió corresponent (CS2/CS3 segons la central).
3. Per CS2, configura TCP/IP i la IP de la central; desa i reinicia JMRI si ho demana.
4. Comprova que JMRI pot controlar la locomotora des del seu throttle abans d'utilitzar JMRI-TC.
5. Configura el Web Server i el WiThrottle Server a JMRI. Anota els ports reals mostrats a les preferències; el port Web Server habitual és 12080 i el port WiThrottle es pot configurar.
6. Configura els descodificadors Märklin com a DCC a JMRI. Aquest MVP només admet adreces DCC curtes (1–127) i llargues (128–10239) al formulari; no promet control mfx/MM2 ni la gestió de consistències.

Referències oficials:
- https://www.jmri.org/help/en/html/hardware/marklin/index.shtml
- https://www.jmri.org/help/en/html/web/JsonServlet.shtml
- https://www.jmri.org/help/en/package/jmri/jmrit/withrottle/Protocol.shtml

## Instal·lació a Windows

Requisits: Windows 10/11, Node.js 20 o superior, JMRI configurat i la central Märklin connectada a JMRI.

Des d'una finestra PowerShell, a la carpeta del projecte:

```powershell
node --version
npm install
npm start
```

Obre `http://127.0.0.1:3000`. El mode segur és el predeterminat: el pont local no permet ordres DCC.

Per habilitar ordres reals, només després de validar la maqueta, els sensors, l'adreçament DCC, l'alimentació i el botó físic d'emergència:

```powershell
$env:JMRI_TC_ENABLE_DCC="1"
$env:JMRI_TC_SAFETY_VALIDATED="1"
npm start
```

Aquesta variable només afecta la finestra PowerShell actual. Reinicia el servidor per tornar al mode segur (tanca el procés amb Ctrl+C i executa `npm start` sense la variable).

## Configuració de la interfície

1. Connecta el Web Server JMRI des de la barra superior de l'editor (per defecte `ws://127.0.0.1:12080/json/`).
2. Comprova que JMRI informa alimentació **ON**.
3. Connecta el pont WiThrottle des del panell de circulacions. Introdueix la IP de l'ordinador on corre JMRI i el port indicat per les preferències WiThrottle.
4. Registra locomotores amb nom i adreça curta/llarga DCC. L'adreça ha de correspondre al roster i al protocol configurat a JMRI.
5. Crea itineraris al Dispatcher. Cada bloc de la ruta ha de tenir sensor(s) verificat(s) i bloc verificat.
6. Per iniciar una circulació, el primer bloc ha d'estar ocupat pel tren i tots els blocs següents han d'estar confirmats lliures.
7. Arma el DCC explícitament només quan la maqueta estigui preparada.

## Mode manual

- L'operador ha de comprovar físicament l'alineació dels desviaments i que la via estigui lliure abans de donar velocitat; el sistema encara no interbloqueja desviaments.
- Crea una circulació en mode **Manual** i prem **Iniciar**.
- Després que JMRI confirmi l'adquisició de la locomotora, utilitza velocitat, sentit i E-STOP.
- Si es perd la connexió JMRI/WiThrottle, l'alimentació deixa d'estar ON o un bloc passa a desconegut, el programa bloqueja noves ordres i demana E-STOP.
- La reserva es manté després d'una fallada fins que l'operador confirma que tots els blocs de la ruta són lliures.

## Mode automàtic

- Crea una circulació en mode **Automàtic**.
- El tren ha d'estar detectat al primer bloc; els blocs següents han d'estar lliures.
- El programa inicia a la velocitat màxima configurada (rang automàtic limitat a 1–20 passos WiThrottle), avança la posició lògica quan el sensor del bloc següent detecta el tren i espera que el bloc anterior quedi lliure.
- Arribada al bloc final: envia velocitat 0 i manté la reserva fins que el bloc final estigui confirmat lliure.
- Temps d'entrada excedit, bloc desconegut, pèrdua de comunicació o bloc anterior que no s'allibera: E-STOP i estat FAULT; la ruta queda bloquejada fins a inspecció manual.

**Limitació deliberada:** el mode automàtic queda bloquejat si el plànol conté qualsevol peça de tipus desviament, perquè encara no hi ha verificació d'alineació/feedback ni interbloqueig de senyals, control de cantons amb distància de frenada, perfil de frenada per locomotora, horaris ni coordinació completa de trens en rutes diferents. La velocitat automàtica està limitada a 20 passos, però això no garanteix una distància de frenada segura. Aquesta versió és alpha de proves: no utilitzis el mode automàtic per a circulació real fins a implementar i validar els elements de seguretat i frenada a la maqueta.

## Proteccions i límits

- DCC està desactivat per defecte; el servidor local exigeix explícitament `JMRI_TC_ENABLE_DCC=1`, `JMRI_TC_SAFETY_VALIDATED=1`, la validació a la interfície i l'armament de l'operador.
- El pont només escolta a `127.0.0.1`; no exposis el port a Internet ni a la xarxa local. En tancar la finestra o perdre el client, el pont intenta enviar E-STOP abans d'alliberar els throttles.
- Només s'accepten ordres WiThrottle per a velocitat, sentit, funcions, adquisició i alliberament. No hi ha ordres de desviaments.
- E-STOP es pot enviar encara que el control estigui desarmat, sempre que la connexió WiThrottle continuï activa. Si la xarxa o JMRI fallen, no es pot garantir que l'ordre arribi: mantén un botó d'emergència físic independent.
- La supervisió de blocs i les reserves del projecte no són un enclavament ferroviari certificat. La maqueta ha de tenir proteccions elèctriques i de seguretat independents.
- Aquest MVP assumeix un tren per itinerari i no garanteix separació segura entre trens simultanis.

## Proves

```powershell
node --check app.js
node --check dispatcher-core.js
node --check dispatcher.js
node --check traffic-core.js
node --check traffic.js
node --check traffic-bridge.js
node tests/safety.cjs
node tests/traffic.cjs
node tests/dispatcher.cjs
node tests/dispatcher-ui.cjs
node tests/smoke.cjs
```

Les proves automàtiques validen la màquina d'estats i els interbloqueigs lògics. No simulen una central Märklin física ni validen la latència, la frenada o els sensors de la maqueta.
