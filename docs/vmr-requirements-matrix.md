# Matriu mestra de requisits i verificació (VMR)

## Convencions

- **PASS-CI**: prova automatitzada identificada executada amb èxit a CI. No equival a inspecció física.
- **PENDENT-FÍSIC**: requereix mesura, inspecció o prova presencial; no es pot tancar amb CI.
- **NO-VERIFICAT**: manca evidència fiable o identificació inequívoca.
- Els IDs següents són IDs de requisit/traçabilitat. Els elements de maqueta continuen rebent IDs individuals `JMRI-TC-VMR-TYPE-00001` del registre per tipus.
- Evidència de referència de programari: [CI main #508](https://github.com/terrsola/jmri-tc/actions/runs/43865601085), SHA `713cac3c25a24afa4cbecb2ff02353595700b9df`.

## Matriu de requisits

| ID | Requisit / objecte | Prova i evidència | Criteri d'acceptació | Estat |
|---|---|---|---|---|
| VMR-001 | ID de projecte persistent i únic | `tests/validation-master-registry.cjs`; `tests/validation-master-registry.browser.cjs`; pas CI VMR real-browser | Dos projectes no comparteixen ID ni clau d'emmagatzematge | PASS-CI |
| VMR-002 | IDs individuals estables i sense duplicats | `tests/validation-master-registry.cjs` | Reconciliació repetida idempotent; cada registre té un `registryId` únic i estable | PASS-CI |
| VMR-003 | Recuperació de comptadors i absència de col·lisions | `tests/validation-master-registry.cjs` | Restaurar registre antic sense comptadors i assignar el següent ID disponible | PASS-CI |
| VMR-004 | Persistència de notes, estats i auditoria | `tests/validation-master-registry.cjs`; prova Chromium | Exportar, tancar/reobrir navegador i importar conserva IDs, notes, validacions i auditoria | PASS-CI |
| VMR-005 | Aïllament entre projectes | prova Chromium VMR | Projecte B no hereta notes ni auditoria del projecte A | PASS-CI |
| VMR-006 | Retirada traçable d'elements eliminats | `tests/validation-master-registry.cjs` | Element absent queda marcat com retirat; no esborra historial ni ID | PASS-CI |
| VMR-007 | Actes de validació Markdown + JSON | proves VMR unitàries i Chromium | Acta conté entrades, recomptes, notes i auditoria; MD/JSON comparteixen ID | PASS-CI |
| VMR-008 | Unicitat d'actes consecutives | proves VMR unitàries i Chromium | Dues actes consecutives, fins i tot amb mateix timestamp, tenen IDs diferents | PASS-CI |
| VMR-009 | Porta de seguretat fail-closed | `tests/safety.cjs`; `tests/safety-bridge.cjs` | Estat inicial bloquejat; cap habilitació sense totes les comprovacions i telemetria en viu | PASS-CI |
| VMR-010 | Pèrdua de comunicació / telemetria caducada | `tests/signalling-integration.cjs`; `tests/jmri-turnout-supervision.cjs`; `tests/safety-bridge.cjs` | Connexió o heartbeat perdut, dades caducades o invàlides força estat segur i bloqueig | PASS-CI |
| VMR-011 | Interbloqueig de blocs i itineraris | `tests/dispatcher.cjs`; `tests/traffic.cjs` | Estat UNKNOWN, ocupació o conflicte impedeix reserva; fallada allibera proteccions segons regla | PASS-CI |
| VMR-012 | Senyals i topologia integrada | `tests/signalling-integration.cjs`; `tests/signalling-topology.cjs`; proves SVG integrades | Senyal en parada davant d'itinerari absent, bloc ocupat, topologia invàlida o dependència cíclica | PASS-CI |
| VMR-013 | Supervisió de desviaments i recuperació | `tests/jmri-turnout-supervision.cjs` | Feedback absent, caducat, no verificat o desalineat bloqueja; recuperació només reconeix estat alineat, no mou el desviament | PASS-CI |
| VMR-014 | Configuració de centrals digitals | `tests/command-station.cjs` | Rebutja fabricant/model/protocol/interfície incompatibles i paràmetres de connexió invàlids | PASS-CI |
| VMR-015 | Sortides físiques deshabilitades per defecte | `tests/command-station.cjs`; `tests/safety-bridge.cjs` | Seleccionar o desar perfil no habilita DCC ni sortides físiques | PASS-CI |
| VMR-016 | Inspecció elèctrica individual per element | `tests/physical-validation.cjs`; registre VMR | Cada element físic ha de tenir comprovacions individuals, evidència mesurada, inspector i data | PENDENT-FÍSIC |
| VMR-017 | Alimentacions, polaritat, seccions i fusibles | llista de comprovació de `physical-validation-core.js`; acta d'inspecció | Mesures i identificació de circuit documentades i revisades per persona competent | PENDENT-FÍSIC |
| VMR-018 | Cablejat, aïllament i proteccions | llista de comprovació física i acta per circuit | Inspecció i mesures reals registrades per tram/circuit; cap no-conformitat oberta | PENDENT-FÍSIC |
| VMR-019 | Parada d'emergència física independent | checklist de seguretat i inspecció presencial | Tall físic independent provat; no depèn del navegador, JMRI ni del pont de programari | PENDENT-FÍSIC |
| VMR-020 | Validació real de protocols i centrals | proves de banc per model, interfície i firmware | Connexió real, lectura/diagnòstic i comportament de desconnexió documentats per cada model | PENDENT-FÍSIC |
| VMR-021 | Identificació Digikeijs ADJ9000 | `tests/command-station.cjs`; `docs/command-stations.md` | Confirmar referència exacta i manual del fabricant abans de declarar compatibilitat | NO-VERIFICAT |
| VMR-022 | Autorització final d'operació | revisió conjunta VMR, CI, inspecció elèctrica i parada física | Cap no-conformitat oberta; revisió signada; proves fail-safe satisfactòries; autorització separada | PENDENT-FÍSIC |
| VMR-023 | Seqüència Senyal 1 / Senyal 2 | `tests/signalling-sequence.cjs`; `tests/signalling-integration.cjs`; CI #591/#592 (SHA `d35a4d3`) | Referències existents, ordre segons sentit i blocs de ruta, continuïtat Senyal 1→Senyal 2 i absència de cicles | PARCIAL-CI: manca integrar el validador d'ordre físic al flux actiu |\n| VMR-024 | Aspectes i transicions de senyalització | `tests/signalling.cjs`; `signalling-core.js`; CI #591/#592 (SHA `d35a4d3`) | Aspectes admissibles segons funció/context; parada per ocupació, estat desconegut o protecció absent; autoritzacions explícites | PASS-CI |\n| VMR-025 | Traçabilitat de senyalització RFIG | aquesta matriu + `docs/validation-report-pr29.md`; CI #591/#592 (SHA `d35a4d3`) | Cada requisit de senyalització enllaça prova, evidència CI, criteri d'acceptació i estat; cap prova CI certifica compliment ferroviari | PARCIAL: acta pendent d'actualitzar després de revisió |\n| VMR-026 | Fallada i recuperació de senyalització | `tests/signalling-integration.cjs`; `tests/safety.cjs`; `tests/safety-bridge.cjs`; CI #591/#592 (SHA `d35a4d3`) | Pèrdua/restauració de comunicació, dades desconegudes o obsoletes, ocupació, seqüència inconsistent i recuperació només amb telemetria vàlida; estat segur per defecte | PARCIAL-CI: falta prova de transició de fallada a recuperació i restauració de referència absent |

## Registre d'evidències CI

La run #508 va completar amb èxit aquests grups de proves: VMR real-browser, safety core, safety bridge, Dispatcher, regressió editor, topologia, validació física de programari, integració SVG/Dispatcher/senyalització, diagnòstic JMRI, supervisió de desviaments, centrals digitals i dues execucions completes de regressió/fail-safe.

La run és evidència del SHA congelat, no dels commits documentals posteriors ni d'una certificació física.

## Regles per tancar requisits

1. Cada requisit ha de conservar ID estable, responsable/inspector, versió, data, evidència i resultat.
2. Una prova automatitzada només pot tancar criteris de programari.
3. Els requisits físics exigeixen evidència individual per circuit/element i revisió competent.
4. Qualsevol fallada, dada desconeguda o telemetria caducada manté el requisit o l'operació bloquejats.
5. Les centrals o protocols amb referència no confirmada romanen explícitament no verificats.
6. No s'habilitaran DCC, desviaments físics ni automatismes per haver completat aquesta matriu documental.

## Dependències de seguretat

La VMR es complementa amb `docs/safety-validation.md`, `docs/validation-master-registry.md` i `docs/command-stations.md`. La inspecció física i la parada d'emergència independent són portes obligatòries abans de qualsevol activació.
