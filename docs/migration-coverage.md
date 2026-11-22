# Platinum Edition — mapa de cobertura de migració

Aquest document és una matriu d'auditoria viva per evitar confondre que un panell es mostri amb que tota la seva funcionalitat estigui migrada, integrada i validada. L'abast és el repositori Platinum Edition; el repositori original queda fora de l'abast.

## Inventari de panells i mòduls detectats

| Àrea | Interfície / mòduls | Estat verificable al codi | Migració integral pendent |
|---|---|---|---|
| Projecte i editor 2D | `app.js`, `layout-model-v3.js`, `viewport-core.js`, `selection-move-core.js`, `track-junction-core.js` | Model v3 i proves de geometria/topologia a CI | Persistència completa de tots els subpanells i proves de fluxos de projecte |
| Catàleg Märklin C | `catalogue.js`, `catalogue-marklin-c.json` | Catàleg filtrable i metadades de geometria | Cobertura de peces especials, topologia real de desviaments i fixtures de round-trip |
| XTrackCAD | `project-file-interop.js` | Rectes i corbes; pèrdues reportades | Desviaments, creuaments, ponts, giratoris, capes i atribucions amb proves de fixtures |
| Blocs / sensors / JMRI | `app.js`, adaptadors JMRI, diagnòstic de dispatcher | Lectura JMRI i registres de recursos | Round-trip de tots els registres, pèrdua de connexió, dades parcials i reconciliació de noms |
| Dispatcher | `dispatcher-core.js`, `dispatcher.js` | Estat de blocs i reserva local d'itineraris | Sincronització consistent amb circulacions, senyals i canvis de projecte |
| Circulacions / DCC | `traffic-core.js`, `traffic.js`, `deadman-core.js`, `safety-core.js` | Pont WiThrottle amb bloqueig per defecte i proves de seguretat | Matriu d'integració completa; cap sortida física s'ha de considerar validada sense banc real i E-stop independent |
| Centrals digitals / protocols | `command-station-core.js`, `command-station-ui.js`, `protocol-core.js`, `protocol-catalogue-ui.js` | Perfils i catàleg de protocols | Connexió específica per fabricant, lectura/escriptura de capacitats i proves amb maquinari compatible |
| Senyalització | `signalling-*`, `jmri-turnout-supervision-*` | Motor, UI, dependències i diagnòstic | Coherència transaccional amb itineraris, desviaments i estats desconeguts |
| Validació física | `physical-validation-*`, `validation-master-registry-*`, `metge-quiral-*` | Registre, matriu i autodiagnòstic local | Persistència, auditoria d'exportació/importació i evidència instrumental separada |
| DMI / cabina | `locomotive-dmi-*` | Panell i proves unitàries | Enllaç de dades complet amb circulació i verificació de totes les ordres bloquejades/desbloquejades |
| Món de Jocs | `spielewelt-*` | Simulació local i proves | Persistència i sincronització amb registre de locomotores |
| Protecció de tren | `train-protection-*` | Simulació local ASFA/ETCS/LZB/PZB | Mantenir explícit el límit de simulació; no presentar com a sistema homologat |
| Tren-terra | `train-ground-*` | Simulació local de terminal | Persistència, cobertura d'estats i accessibilitat |
| Instal·lador / escriptori | `electron/*`, workflow NSIS | Construcció x64 verificada en CI | Proves d'instal·lació/actualització/desinstal·lació i migració de dades d'usuari |

## Regles per declarar una àrea migrada

1. El panell i tots els seus controls tenen un mòdul carregat i contracte DOM verificat.
2. El model persistent té esquema, validador, migració de versions i round-trip provat.
3. Els mòduls comparteixen identificadors i relacions sense duplicar fonts d'estat.
4. Hi ha proves unitàries, d'integració de navegador i de regressió per a errors i estats desconeguts.
5. Les funcions de maquinari continuen bloquejades per defecte; una prova de simulació no és una prova física.

## Límits que no es poden resoldre només amb codi

No es pot declarar validada la compatibilitat amb una CS2, retroalimentació S88, descodificadors, desviaments o parada d'emergència sense equips, cablejat i proves físiques. Els protocols ferroviaris de protecció i comunicació representats pels panells formatius continuen sent simulacions locals.

## Porta de contracte d'interfície

`tests/ui-module-contract.cjs` comprova que tots els scripts locals referenciats existeixen, que no hi ha IDs DOM duplicats, que les dependències carreguen abans dels seus consumidors i que els panells principals conserven ancoratges DOM. Aquesta porta detecta regressions d'estructura; no substitueix proves funcionals de cada panell.
