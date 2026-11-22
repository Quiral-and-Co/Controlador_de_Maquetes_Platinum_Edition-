# Inventari de migració completa a .NET 10 / WPF

Branca de treball: `migration/complete-dotnet10-wpf`
Base: `main`

Aquest inventari és el contracte de migració. Un fitxer antic no s'elimina només perquè existeixi un menú WPF o una classe amb el mateix nom. Cal equivalència funcional, persistència, proves i acceptació.

## Arquitectura objectiu

- `Platinum.Domain`: entitats, value objects, regles, topologia, estats i invariants sense UI ni I/O.
- `Platinum.Application`: casos d'ús, serveis, validació, orquestració, ports/interfaces i esdeveniments.
- `Platinum.Infrastructure`: paquet `.platinum`, còpies/recuperació, catàlegs, adaptadors de xarxa/sèries i integracions.
- `Platinum.Presentation.Wpf`: finestres, controls, panells, MVVM, comandaments, recursos i accessibilitat.
- Projectes de proves: Domain unit tests, Application tests, Infrastructure integration tests i WPF smoke/UI tests.

Regles:
1. Els ViewModels no són la font de veritat: només projecten el model de domini.
2. Les modificacions passen per casos d'ús; una única instància de projecte i un únic servei d'estat.
3. Els adaptadors externs són opcionals i aïllats. La maqueta ha de poder obrir-se i editar-se sense JMRI ni central connectada.
4. El simulador mai no comparteix canal d'escriptura amb un adaptador físic.
5. La porta de seguretat denega per defecte; l'estat visual o una inspecció declarada no la pot obrir.
6. Els fitxers antics són només referència de comportament durant la transició. No es carreguen en temps d'execució a la versió final.

## Inventari de mòduls antics

| Mòdul/fitxer antic | Destí natiu | Estat de migració |
|---|---|---|
| `index.html`, `styles.css`, `app.js` | MainWindow, recursos WPF, shell MVVM | Pendent d'equivalència |
| `catalogue.js` | Catalogue service + WPF catalogue browser | Parcial: catàleg JSON inicial |
| `viewport-core.js` | Editor viewport domain/application + WPF canvas | Parcial: editor 2D/3D |
| `selection-move-core.js` | Selection/move commands + undo stack | Pendent |
| `track-junction-core.js` | Märklin geometry/topology services | Parcial; peces especials per validar |
| `vertical-track-core.js`, `vertical-track-ui.js` | Level/elevation editor | Pendent d'equivalència |
| `project-file-interop.js` | Import/export services | Pendent |
| `automation-core.js` | RailwayAutomationEngine | Parcial; verificar paritat |
| `dispatcher-core.js`, `dispatcher.js`, `dispatcher.css` | Dispatcher application service + WPF panel | Parcial |
| `traffic-core.js`, `traffic.js`, `traffic-bridge.js` | Traffic orchestration + infrastructure adapters | Pendent |
| `deadman-core.js` | Deadman/watchdog safety service | Pendent de proves |
| `safety-core.js` | PhysicalControlGate + safety state machine | Parcial; sortides bloquejades |
| `command-station-core.js`, `command-station-ui.js` | Control station adapters + WPF diagnostics | Parcial; CS2 presence/ping only |
| `protocol-core.js`, `protocol-catalogue-ui.js` | Protocol catalogue + typed codecs | Pendent |
| `signalling-core.js`, `signalling-ui.js`, `signalling.css` | Signalling domain + WPF panel | Pendent |
| `signalling-integration-core.js`, `signalling-integration.js` | Signal/interlocking application service | Pendent |
| `signalling-sequence-core.js` | Signal aspect sequencing engine | Pendent |
| `signalling-topology-core.js`, `signalling-topology-ui.js`, `signalling-topology.css` | Signal topology editor/validator | Pendent |
| `jmri-json-adapter.js` | Optional JMRI compatibility adapter | Pendent; no runtime dependency in final |
| `jmri-dispatcher-diagnostics-core.js`, `jmri-dispatcher-diagnostics.js` | JMRI diagnostics adapter + WPF view | Pendent |
| `jmri-turnout-supervision-core.js`, `jmri-turnout-supervision.js`, `jmri-turnout-supervision.css` | Turnout supervision service + WPF view | Pendent |
| `locomotive-dmi-core.js`, `locomotive-dmi-ui.js`, `locomotive-dmi.css` | Locomotive profile/DMI + WPF cab | Pendent |
| `train-protection-core.js`, `train-protection-ui.js`, `train-protection.css` | Train protection simulator/service | Pendent; simulation only |
| `train-ground-core.js`, `train-ground-ui.js`, `train-ground.css` | Train-ground terminal simulator | Pendent; simulation only |
| `spielewelt-core.js`, `spielewelt-ui.js`, `spielewelt.css` | Món de Jocs WPF module | Pendent |
| `physical-validation-core.js`, `physical-validation-ui.js`, `physical-validation.css` | Inspection/audit registry + WPF validation panel | Pendent; declarations are not certification |
| `validation-master-registry-core.js`, `validation-master-registry-ui.js` | Unified validation registry | Pendent |
| `metge-quiral-core.js`, `metge-quiral-ui.js` | Quiral diagnostics/assistant domain + WPF view | Pendent |
| `train-ground-core.js`, `train-protection-core.js` | Isolated deterministic simulators | Pendent |
| `electron/main.cjs`, `electron/preload.cjs`, `electron/project-path-policy.cjs` | Eliminació després de migrar actualitzador i gestió segura de fitxers | No forma part del runtime objectiu |
| `package.json`, `electron-builder` | SDK-style solution, dotnet publish, Inno Setup | Pendent de retirada final |

## Seqüència d'execució

### A. Tancar el contracte de dades
- Versionar el model `.platinum` i afegir migracions de format.
- Afegir col·leccions de senyals, locomotores, registres, accessoris, reserves, perfils de protocol i estat de simulació.
- Definir identificadors estables, referències i regles d'integritat.
- Afegir autosave, còpia de seguretat, recuperació i importació amb informe de pèrdues.

### B. Completar l'editor
- Portar geometria exacta de cada referència Märklin C, ports, transformacions, toleràncies i confiança.
- Selecció múltiple, copiar/enganxar, rotar, moure, desfer/refer i validació de connexions.
- Nivells, rampes, helix, cotes, col·lisions i accessibilitat d'edició.

### C. Migrar panells i operacions
- Dispatcher, blocs, sensors, itineraris i reserves.
- Senyalització, seqüències, interbloquejos i supervisió de desviaments.
- Cabina DMI, perfils de locomotora i simuladors.
- Diagnòstic, registre de validació i Món de Jocs.

### D. Adaptadors i seguretat
- Implementar protocols documentats com a adaptadors independents, amb tests de codec, timeout, reconnect, stale-state i watchdog.
- CS2/CS3, Z21, ECoS i DCC no s'activaran fins que cada protocol i la protecció independent s'hagin provat amb hardware.
- E-stop físic cablejat i interbloqueig independent del PC; cap botó a la UI el substitueix.
- Simulació amb bus de missatges separat i impossible de commutar accidentalment a hardware.

### E. Distribució
- Publicació self-contained win-x64, instal·lador i actualitzador signable/verificable.
- Actualització atòmica, migració de dades amb backup i rollback.
- Proves d'instal·lació, actualització i desinstal·lació en Windows net.

## Criteri de retirada de codi antic

Un mòdul antic només es pot retirar quan:
- tots els seus casos d'ús estan implementats en C#;
- la UI WPF cobreix les operacions normals i els errors;
- dades antigues es poden importar o s'ha documentat explícitament la pèrdua;
- proves de paritat/regressió passen;
- cap projecte, workflow o document d'instal·lació el necessita;
- l'acceptació queda registrada.

## Estat d'aquesta branca

Aquesta branca inicia la migració integral i no declara que els mòduls pendents estiguin completats. La versió antiga es manté intacta fins a complir els criteris anteriors.
