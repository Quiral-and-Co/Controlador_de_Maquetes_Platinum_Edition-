# Aplicació d'escriptori nativa · .NET 10 WPF

> **Branca de migració nativa:** l'aplicació d'escriptori en desenvolupament és C# + WPF + XAML. No carrega HTML, CSS, JavaScript, WebView ni necessita iniciar cap servidor o navegador per executar el projecte WPF. Els fitxers web descrits més avall es mantenen com a implementació de referència mentre es completa la paritat funcional; no són dependències del projecte WPF.

Per compilar i executar la versió nativa a Windows:

```powershell
dotnet restore Platinum.sln
dotnet build Platinum.sln -c Release
dotnet run --project src/Platinum.Presentation.Wpf/Platinum.Presentation.Wpf.csproj
```

Consulta [la guia de migració WPF](docs/native-wpf-migration.md) per veure què ja s'ha portat i què queda pendent.

### Dispatcher natiu WPF (.NET 10)

La branca `main` incorpora el panell Dispatcher WPF connectat al `TrackLayout` actiu: consulta de blocs per nivell, estats lògics Desconegut/Lliure/Ocupat, creació d'itineraris amb blocs seleccionats, reserva atòmica sense conflictes i alliberament de reserves quan un bloc deixa d'estar lliure. Les ocupacions i reserves es desen al paquet `.platinum`. Aquesta interfície és de simulació local: no envia ordres a locomotores, desviaments, senyals, JMRI ni centrals digitals. Proves de domini: `dotnet test Platinum.sln -c Release` (inclou `DispatcherServiceTests`). Les pantalles noves de simulació són locals i no controlen maquinari real.

## Instal·lació i actualitzacions (WPF)

El setup Inno Setup conserva un `AppId` estable. Quan detecta una instal·lació anterior de Quiral Platinum, executa el desinstal·lador antic en mode silenciós abans de copiar els fitxers nous. Aquesta operació es fa després que l'usuari confirmi la instal·lació; si la desinstal·lació falla, el setup s'atura i no deixa una instal·lació parcial. Es conserva la carpeta d'instal·lació anterior i es tornen a crear les dreceres.

La barra superior de Platinum inclou **Actualitzar des de GitHub**. L'aplicació consulta l'última GitHub Release, compara la versió de l'assembly, descarrega l'instal·lador oficial .exe, verifica el manifest SHA-256 i només després de la confirmació de l'usuari inicia l'instal·lador elevat de Windows. L'aplicació es tanca perquè Inno Setup pugui substituir la versió instal·lada.

- [Platinum WPF setup build](.github/workflows/wpf-setup-build.yml) compila WPF self-contained per a Windows x64 i publica un artefacte descarregable a GitHub Actions.
- [Platinum WPF Windows release](.github/workflows/wpf-release.yml) genera el mateix instal·lador i el publica com a GitHub Release amb el seu manifest SHA-256.
- Per publicar una release estable, executa el workflow de release indicant una versió semàntica (per exemple `0.2.0`) o crea un tag `vMAJOR.MINOR.PATCH`.
- Fins que no hi hagi una GitHub Release amb l'EXE i el fitxer `.sha256`, el botó d'actualització només pot informar de l'estat; no hi ha cap actualització instal·lable.

La verificació SHA-256 detecta fitxers alterats o corruptes, però no substitueix la signatura Authenticode del fabricant. Abans de distribuir públicament, és recomanable signar l'instal·lador amb un certificat de codi.

**Motor de control propi:** Platinum ja disposa de serveis C# i d'un adaptador de xarxa natiu per a Märklin CS2: CAN-over-Ethernet UDP, ports 15731/15730 i consulta de presència, sense JMRI/Java/WebSocket. Les ordres físiques continuen bloquejades fins a completar el mapa d'identificadors, la supervisió de seguretat i les proves amb maquinari. CS3, Z21 i ECoS continuen pendents. Vegeu [motor de control propi](docs/own-railway-control-engine.md) i [protocol natiu CS2](docs/native-marklin-cs2-protocol.md).

---

# JMRI TC Gold Custom — 1.4.0

Aplicació d'escriptori i eines de planificació, diagnòstic i validació per a maquetes Märklin H0. La integració amb JMRI i el pont de circulacions tenen modes i límits de seguretat documentats; les funcions simulades no equivalen a protocols ferroviaris certificats ni a control físic validat.

## Funcions disponibles

### Edició 2D (0.3–0.4)
- Tauler de 3000 × 1800 mm, graella de 10 mm i cinc nivells amb cotes previstes de 0, 110, 220, 330 i 440 mm.
- Primitives gràfiques genèriques de recta, corba i desviament; selecció, moviment, rotació i geometria editable. No són referències de catàleg Märklin C.
- Connexions geomètriques entre ports A/B/C amb `verified:false`; no validen continuïtat elèctrica.
- Historial Desfer/Refer (fins a 100 estats), duplicació i mesura euclidiana en mm.

### Blocs, sensors i nivells (0.6)
- Crear blocs lògics al nivell actual i assignar-los a peces. `trackIds` i `sensorIds` es recalculen des de les assignacions de les peces.
- Registrar sensors amb el `systemName` JMRI i nom visible opcional; assignar-los a peces. Tots els sensors nous queden `verified:false`.
- Canviar una peça de nivell només si no té bloc, sensor ni connexions. No es crea cap enllaç vertical.
- Importar configuracions v2. Les assignacions textuals antigues es migren a registres provisionals explícits, no verificats.
- Exportació JSON en model v3 i comprovacions d'IDs, referències i connexions dins del mateix nivell.

## Execució i proves

Obre `index.html` en un navegador modern. No cal servidor per editar offline. Per monitoritzar JMRI, activa el Web Server de JMRI i configura l'adreça i el port a la interfície (habitualment 12080).

Executa:

```bash
node --check app.js
node tests/smoke.cjs
python3 -m json.tool config/layout.example.json
```

El fitxer `config/layout.example.json` ha de validar contra `schema/layout-v3.schema.json` (JSON Schema Draft 2020-12).


## Dispatcher · despatxador (1.1)

El panell Dispatcher permet:
- Consultar els blocs lògics creats al projecte i el nivell al qual pertanyen.
- Calcular automàticament l'ocupació dels blocs a partir de sensors JMRI verificats.
- Simular els estats **Desconegut**, **Lliure** i **Ocupat** només als blocs sense sensors assignats.
- Definir itineraris com una seqüència de blocs (identificadors separats per comes).
- Reservar i alliberar itineraris de manera atòmica dins del simulador.
- Detectar conflictes quan dos itineraris comparteixen blocs.
- Alliberar automàticament les reserves afectades quan un bloc passa a ocupat o desconegut.

Les definicions dels itineraris es desen a `dispatcher.routes` dins del projecte JSON. Els estats simulats i les reserves són temporals i es reinicien en tornar a carregar la pàgina.

**Integració JMRI:** els sensors es llegeixen pel WebSocket JSON en mode només lectura. ACTIVE (2) significa ocupat i INACTIVE (4) significa lliure. Cal marcar sensor i bloc com a verificats físicament des dels registres de recursos. Si falta una lectura, el valor és invàlid o es perd el heartbeat/connexió, el bloc passa a desconegut; els blocs amb sensors no accepten canvis manuals. No s'envien ordres a locomotores, desviaments ni rutes. La reserva continua sent local i no és un interbloqueig ferroviari certificat. Consulta [docs/dispatcher.md](docs/dispatcher.md).

Proves del motor i de la integració de sensors:
```bash
node --check app.js
node --check dispatcher-core.js
node --check dispatcher.js
node tests/dispatcher.cjs
node tests/dispatcher-ui.cjs
node tests/smoke.cjs
```

## Context de maquetisme i límits de seguretat

- Märklin H0, via C, cinc nivells, superfície 3000 × 1800 mm; separació vertical prevista de 110 mm. Les cotes s'han de contrastar amb l'estructura física.
- Central Märklin CS2 60215. Retroalimentació prevista S88 amb mòduls 60881/60882; la interfície física, el cablejat i la configuració real s'han de verificar abans de donar per operatiu el bus.
- Les peces poden provenir de la biblioteca de catàleg o ser primitives geomètriques lliures. No s'ha importat ni inferit cap traçat real des de XTrackCAD.
- Un bloc és només una agrupació de disseny; no implica cantó validat, reserva, frenada ni automatització. Un sensor registrat no confirma existència, adreça ni funcionament físic.
- No s'envien ordres de locomotores, desviaments, rutes ni automatismes. Aquesta restricció es manté fins que traçat, detecció, reserves i parada d'emergència estiguin verificats.

## Validació topològica (0.6)

Prem **Validar topologia** per revisar totes les connexions definides. La comprovació detecta referències a ports inexistents, nivells inconsistents, duplicats, ports compartits i extrems separats més d'1 mm. Els ports oberts només es mostren com a avisos mentre el traçat no estigui complet. Aquesta eina és de diagnòstic local i no valida el circuit elèctric ni activa cap equip.

Per executar les proves automatitzades amb Node.js:

```bash
node --check app.js
node tests/smoke.cjs
```


## Connexió manual de ports (0.7)

Activa **Activar connexió manual de ports** al panell de propietats i fes clic a dos ports (A/B/C) visibles al tauler. Els ports es mostren amb marcadors grocs i el primer seleccionat queda ressaltat. La connexió només es crea si els dos extrems coincideixen a una distància màxima d'1 mm; l'eina no mou ni gira cap peça automàticament. Els ports ja ocupats es rebutgen. La connexió queda amb `verified:false` i només representa topologia geomètrica.

La funció **Connexió ràpida** de versions anteriors es conserva com a eina auxiliar; per treballar amb precisió, prioritza la connexió manual.


## Accessibilitat i proves de connexió (0.7.1)

Els marcadors de ports accepten focus de teclat i es poden activar amb **Enter** o **barra espaiadora**. Cada marcador exposa una etiqueta accessible amb el nom de la peça i el port. Les proves automatitzades cobreixen connexions exactes, activació per teclat, autoconnexions, ports ocupats i extrems separats més d'1 mm. Una connexió rebutjada no mou peces ni modifica el model.

## Biblioteca Märklin C (0.9)

La paleta ara és una biblioteca filtrable per categoria i cercable per referència o nom. Les dades viuen a `catalogue-marklin-c.json` i es carreguen des de `catalogue.js`; afegir referències no requereix modificar el motor. La càrrega inicial inclou 68 referències trobades en documentació pública Märklin (rectes, corbes R1–R5, desviaments, creuaments i accessoris). Les dimensions confirmades i les geometries parcials/no verificades estan diferenciades explícitament. No es declara encara exhaustivitat del catàleg històric o comercial complet. Consulta `docs/catalogue-c-track.md` abans d'utilitzar la geometria per construir o validar encaixos.

El tipus `special` s'afegeix al model v3 i es manté compatible amb projectes antics. La importació/exportació JSON conserva les metadades de referència. La geometria esquemàtica de desviaments i peces especials no valida encaixos físics.


## Gestió de circulacions Märklin DCC (1.2 alpha)

El panell de circulacions incorpora registre de locomotores DCC, itineraris, mode manual i mode automàtic supervisat. La lectura de sensors/alimentació continua al JSON WebSocket JMRI; les ordres de locomotora passen pel pont local WiThrottle TCP. Consulta [docs/traffic-management.md](docs/traffic-management.md) per instal·lació, configuració Märklin, ports, seguretat i limitacions.

**Seguretat per defecte:** el pont DCC està desactivat. Per provar només la interfície, executa `npm install` i `npm start` i obre `http://127.0.0.1:3000`. Les ordres reals exigeixen `JMRI_TC_ENABLE_DCC=1` i armar el control. L'automàtic rebutja rutes amb desviaments no verificats i atura davant lectures desconegudes. No és un enclavament ferroviari certificat.

Proves addicionals: `node tests/traffic.cjs`.


## Catàleg digital Märklin DCC / MM2 / mfx

La branca `feature/marklin-mm-mfx-accessory-catalogue` incorpora un registre local de locomotores i accessoris amb exportació/importació JSON. Admet fitxes DCC, MM2 i mfx per UID; tipus d'accessori: desviaments, senyals, llums, desacobladors, relés i altres. Consulta [docs/marklin-multiprotocol.md](docs/marklin-multiprotocol.md).

**Límit funcional:** el registre no és una importació automàtica de la base interna de la CS2 ni una base oficial completa de models. El control MM2/mfx i els mapes d'aspectes de senyals/llums encara no estan connectats. Les ordres MM2/mfx es bloquegen deliberadament; només hi ha un constructor per a una ordre de desviament DCC via JMRI JSON, pendent d'integració i prova física. No s'ha provat amb una CS2 real.


## Validació i bloqueig de seguretat (1.2.1 · en revisió)

El mòdul `safety-core.js` manté l'estat BLOQUEJAT per defecte i exigeix una llista explícita de validacions, confirmació física de l'operador, JMRI connectat, alimentació ON, pont WiThrottle connectat i sensors vàlids abans de permetre armar el control DCC. La interfície de circulacions, les ordres manuals i l'inici de circulacions passen per aquesta porta. El pont local exigeix també `JMRI_TC_SAFETY_VALIDATED=1` a més de `JMRI_TC_ENABLE_DCC=1`.

La confirmació és una declaració de l'operador, no una certificació ni una lectura del circuit d'emergència. Cal mantenir una parada física independent que talli la tracció; el programari no substitueix proteccions elèctriques ni un enclavament ferroviari certificat. Consulta `docs/safety-validation.md`.

Prova específica: `node tests/safety.cjs`. La suite completa inclou aquesta prova amb `npm test`.


## Planificació supervisada d’horaris (1.3.0)

El motor `automation-core.js` valida horaris, dies, finestres d’inici i velocitats limitades a 20 passos. El preflight exigeix explícitament la validació de seguretat de PR #7, JMRI i WiThrottle connectats, alimentació ON, DCC armat, sensors vàlids, primer bloc ocupat, blocs següents lliures, absència de conflictes de reserva i feedback verificat dels desviaments requerits (`CLOSED` o `THROWN`). La protecció contra execucions duplicades impedeix reclamar el mateix horari dues vegades el mateix dia.

**Límit deliberat:** el planificador només valida i registra; no envia ordres físiques ni inicia trens. `safetyValidated` s’ha de derivar de l’estat actual de `JMRI_TC_SAFETY.canEnablePhysical(runtime)`, mai d’un valor persistent. La parada d’emergència física independent i les proteccions elèctriques continuen sent obligatòries. Consulta `docs/automation-1.3.md`.

Proves: `node tests/automation.cjs` i `npm test`.


## Diagnòstic de senyals i matriu de validació física (1.4.0)

La selecció d'una senyal al registre ressalta al tauler les peces de via relacionades amb el bloc protegit i l'itinerari complet. El diagnòstic resumeix incidències per nivell. La matriu individual manté comprovacions separades per senyal, bloc, sensor i desviament, distingint revisió de programari i inspecció física. Les anotacions s'exporten amb el projecte JSON a `validationMatrix`.

**Límit de seguretat:** els estats de la matriu són registres declaratius de l'operador, no mesures instrumentals. Cap casella habilita sortides. DCC i automatització física continuen bloquejats fins a completar proves de programari, inspecció del cablejat i proteccions, i prova d'una parada d'emergència física independent. Consulta [docs/signalling-diagnostics-validation.md](docs/signalling-diagnostics-validation.md).

Prova específica: `node tests/physical-validation.cjs`; suite completa: `npm test`.


## Navegació del tauler 2D (0.10)

- **Zoom:** roda del ratolí centrada al cursor o botons +/−; l’indicador mostra el percentatge respecte de la vista completa.
- **Desplaçament:** mantén Espai i arrossega amb el botó principal, o arrossega amb el botó central.
- **Ajustar:** «Ajusta a superfície» retorna la vista completa de 3000 × 1800 mm.
- El zoom està limitat entre 10% i 1000%; el desplaçament no perd l’àrea de la maqueta.
- La navegació canvia exclusivament el `viewBox` SVG: no modifica coordenades en mm, geometries, connexions, blocs, sensors ni el JSON exportat.
- Proves: `node --check viewport-core.js`, `node tests/viewport.cjs` i `node tests/smoke.cjs`.


## Moviment conjunt de peces seleccionades (0.11)

- Selecciona diverses peces amb **Ctrl + clic** (o **Cmd + clic** a macOS) i arrossega qualsevol peça de la selecció.
- Totes les peces seleccionades es traslladen amb el mateix desplaçament, ajustat a la graella de 10 mm; es mantenen els offsets relatius i les rotacions.
- Les coordenades d’origen de totes les peces seleccionades queden dins de la superfície 3000 × 1800 mm. Si es cancel·la l’arrossegament, es restauren totes les coordenades.
- Cada moviment conjunt crea un únic pas a **Desfer/Refer**. Les connexions entre peces mogudes juntes conserven la geometria; qualsevol connexió amb peces no seleccionades s’ha de tornar a revisar amb «Validar topologia».
- El motor de moviment és independent (`selection-move-core.js`) i no envia ordres a JMRI ni modifica estats de sensors, desviaments, blocs o sortides físiques.
- Proves: `node --check selection-move-core.js`, `node tests/selection-move.cjs` i `node tests/smoke.cjs`.


## Món de Jocs Märklin universal

El mòdul `spielewelt-core.js` i la interfície `spielewelt-ui.js` ofereixen cabines virtuals per a locomotores mfx+, mfx, DCC, MM2/Motorola i altres descodificadors. Inclou perfils de vapor, dièsel i elèctric, modes sense consum, amb consum simulat i repostatge virtual al dipòsit. El motor diferencia explícitament la capacitat mfx+ declarada de la simulació pròpia del programa: en locomotores no mfx+ els recursos no es llegeixen ni s'escriuen al descodificador.

**Límit de seguretat:** aquest mòdul és una simulació local i no envia ordres a la CS2, al bus de tracció, a JMRI ni a sortides físiques. El repostatge exigeix confirmació de dipòsit i només modifica l'estat simulat. No és una implementació del protocol propietari mfx+ ni substitueix la cabina nativa de Märklin.

Proves: `node --check spielewelt-core.js`, `node --check spielewelt-ui.js` i `node tests/spielewelt.cjs`; també incorporades a `npm test`.


## Protecció ferroviària i conducció manual simulada

El panell `train-protection-core.js` / `train-protection-ui.js` permet practicar procediments de reconeixement i supervisió conceptual amb ASFA Digital, ETCS dins d'ERTMS (nivells 0, 1, 2 i 3), LZB i PZB. Inclou velocitat actual, velocitat permesa, objectiu, distància, avís pendent, botó de reconeixement, rellotge de simulació pas a pas, registre d'esdeveniments i frenada d'emergència virtual per manca de reconeixement o superació de límits.

**Limitació essencial:** és una simulació educativa de lògica d'interfície, no una implementació certificada ni una rèplica exacta de cap sistema ATP. No implementa protocols de balises, EuroRadio, Eurobalise, LZB/PZB reals ni corbes de frenada homologades; no controla actuadors ni frens reals. El nivell ETCS 3 és només conceptual. No s'ha d'utilitzar per protegir circulacions reals. La porta de seguretat existent i les sortides físiques romanen independents i bloquejades.

Proves: `node --check train-protection-core.js`, `node --check train-protection-ui.js` i `node tests/train-protection.cjs`; incorporades a `npm test`.


## Terminal tren-terra analògic i GSM-R · RENFE sèrie 252 (simulació)

El panell train-ground incorpora una interfície de cabina inspirada en terminals Alcatel/AEG de la sèrie 252: mode analògic per canals i mode GSM-R amb registre de xarxa simulat, funcions ferroviàries, trucades entrants/sortints, teclat numèric, PTT, missatges, alliberament i registre d'esdeveniments. Tots els botons tenen resposta local i les operacions es poden provar sense maquinari.

**Límit essencial:** no és una rèplica exacta del terminal Alcatel/AEG ni implementa ràdio, SIM, autenticació, GSM-R real, EuroRadio o comunicacions amb infraestructura ferroviària. El botó d'emergència només crea un esdeveniment simulat. No controla la CS2, JMRI, DCC, tracció, frens ni sortides físiques. No s'ha d'utilitzar per comunicacions reals. Consulta [docs/train-ground-terminal.md](docs/train-ground-terminal.md).

Proves: `node --check train-ground-core.js`, `node --check train-ground-ui.js` i `node tests/train-ground.cjs`; incorporades a `npm test`.


## Platinum Edition 1.4.0

Aquest repositori manté una línia de publicació independent de la branca principal del projecte original. Les compilacions Windows x64 es publiquen des de les GitHub Actions d'aquest repositori.
