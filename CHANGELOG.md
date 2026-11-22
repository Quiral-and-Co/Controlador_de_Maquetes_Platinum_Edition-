# Changelog

## 1.5.0 — JSON v3 model contract hardening

- Alineat l'esquema JSON v3 amb les dades que desa l'editor: cota Z, vinculació JMRI de desviaments, metadades de catàleg, estat de geometria, tipus de junction i riscos verticals.
- Incorporats els quatre ports A/B/C/D dels desviaments doble-slip al contracte del projecte.
- La validació rebutja ports reutilitzats, connexions duplicades, mètodes desconeguts i geometries numèricament invàlides.
- Extret el validador JSON v3 a un mòdul pur compartit entre aplicació i proves.
- La importació XTC conserva els registres de blocs i sensors, neteja les assignacions obsoletes i avisa de les connexions descartades; l'exportació informa de les metadades JMRI i Z no representables.
- Afegides proves de regressió per al port D, conflictes de ports, esquema v3 i preservació d'informació XTC.
- Workflow d'instal·lador i artefactes actualitzats a la versió 1.5.0.

# Changelog

## 1.5.0 — JSON v3 model contract hardening

- Alineat l'esquema JSON v3 amb les dades que desa l'editor: cota Z, vinculació JMRI de desviaments, metadades de catàleg, estat de geometria, tipus de junction i riscos verticals.
- Incorporats els quatre ports A/B/C/D dels desviaments doble-slip al contracte del projecte.
- La validació rebutja ports reutilitzats, connexions duplicades, mètodes desconeguts i geometries numèricament invàlides.
- Afegides proves de regressió per al port D, conflictes de ports i esquema de dades.
- Workflow d'instal·lador i artefactes actualitzats a la versió 1.5.0.

# Canvis

## 1.4.0 — editor de maquetes i contracte JSON v3
- Reforçada la validació d'importació: objecte arrel, metadades H0/Märklin C, superfície, cotes dels cinc nivells, identificadors i referències.
- Les connexions JSON només poden referenciar ports existents de les peces; les assignacions v2 continuen migrant-se com a registres provisionals no verificats.
- Alineat l'esquema JSON Schema v3 amb les metadades de connexió generades per l'editor (method) i els ports A/B/C.
- Afegides proves de regressió per preservar metadades de connexió, rebutjar ports inexistents i impedir dimensions de maqueta incompatibles.
- Preparada la compilació Windows NSIS versionada 1.4.0; el fitxer executable es publicarà com a artefacte després de superar CI.

## 1.0 alpha — Dispatcher / despatxador
- Afegit panell de supervisió de blocs amb estats simulats desconegut, lliure i ocupat.
- Afegida definició d'itineraris per seqüències de blocs, reserves i alliberaments.
- Bloqueig de reserves amb blocs desconeguts o ocupats i detecció de conflictes entre itineraris.
- Alliberament automàtic de les reserves afectades quan un bloc deixa d'estar lliure.
- Les rutes es desen al projecte; els estats simulats i les reserves no persisteixen.
- Integració estrictament de simulació: cap ordre operativa s'envia a JMRI.
- Afegides proves unitàries del motor Dispatcher.

## 1.0.0 — en desenvolupament
- Obert el full de ruta 1.0 amb fites alpha, beta i RC.
- Documentats els criteris d'acceptació, l'arquitectura objectiu i les condicions de seguretat per al futur control real.
- JMRI continua en mode només lectura fins a superar les proves d'integració i seguretat.

## 0.7.1 — reforç de la connexió manual
- Els ports SVG ara són accessibles amb teclat: focus, Enter i barra espaiadora.
- Etiquetes accessibles per identificar el port i la peça.
- Proves ampliades: activació per teclat, rebuig d’autoconnexions, ports ocupats i separacions superiors a 1 mm.
- Verificat que una connexió rebutjada no desplaça peces ni altera el model.

## 0.7
- Marcadors SVG interactius per als ports A/B/C de cada peça.
- Mode de connexió manual: l'usuari tria explícitament els dos ports; no s'escull automàticament la parella més propera.
- Rebutja ports ja ocupats, auto connexions i extrems separats més d'1 mm.
- No desplaça ni gira peces de manera implícita; la connexió queda no verificada elèctricament.
- Conserva la connexió ràpida antiga com a funció auxiliar.
- Prova automatitzada de connexió manual exacta i auditoria topològica posterior.


## 0.6
- Auditoria topològica de només lectura sobre tots els cinc nivells.
- Detecta referències a ports inexistents, connexions duplicades, ports compartits, nivells inconsistents, auto connexions i discontinuïtats geomètriques superiors a 1 mm.
- Informa separadament dels ports oberts com a avisos; no són errors en un traçat encara no definit.
- La validació no modifica el projecte ni envia ordres a JMRI.


## 0.5 — 2026-11-15
- Model de dades v3 amb registres explícits de blocs i sensors per nivell.
- Alta de blocs lògics i sensors JMRI; assignació a peces mitjançant selectors.
- Relacions inverses `trackIds` i `sensorIds` recalculades a partir de les peces.
- Canvi de nivell restringit a peces sense assignacions ni connexions; sense enllaços verticals automàtics.
- Migració de configuracions v2 amb assignacions textuals a registres provisionals no verificats.
- Validació reforçada d'IDs, referències i connexions dins d'un mateix nivell.

## 0.4
- Historial Desfer/Refer, duplicació de peces i eina de mesura en mm.

## 0.3
- Geometria editable i alineació de ports per translació.

## 0.9 — Biblioteca Märklin C basada en dades

- Afegida biblioteca externa `catalogue-marklin-c.json` amb 68 referències identificades: rectes, corbes R1–R5, desviaments, creuaments i complements.
- Cerca per referència/nom i filtre per categoria; inserció d'elements de catàleg amb metadades de referència.
- Dimensions conegudes de rectes i corbes carregades com a dades; geometria de desviaments/creuaments marcada parcial o no verificada quan no hi ha prou fitxa tècnica.
- Afegit tipus esquemàtic `special` al model v3, mantenint la compatibilitat amb projectes 0.7.x.
- Conservades les primitives lliures per a peces encara no incorporades al catàleg.
- Documentada cobertura inicial i procediment d'ampliació. No es declara exhaustivitat del catàleg complet fins a contrastar totes les referències i fitxes tècniques.
- JMRI continua exclusivament en mode monitorització/només lectura; cap ordre de locomotora, desviament o ruta.


## 1.3.0 — planificació supervisada i preflight
- Afegit `automation-core.js` per validar horaris, dies, finestres i velocitat màxima de 20 passos.
- Integrada com a requisit la porta de seguretat PR #7 (`safetyValidated`) abans de donar estat READY.
- Preflight reforçat amb estat dels blocs, conflictes de reserva i feedback verificat dels desviaments `CLOSED`/`THROWN`.
- Afegida protecció contra execucions duplicades per horari i dia.
- Afegides proves unitàries i execució a GitHub Actions.
- Sense ordres físiques ni inici automàtic de trens; continua pendent la validació real de la maqueta.
\n## Home mort · conducció manual\n- Watchdog de presència activat per defecte per a locomotores adquirides en mode manual.\n- Control de conducció amb manteniment premut; en deixar-lo anar, s'envia velocitat zero.\n- El pont local dispara una ordre d'emergència i desarma el DCC si no rep heartbeat en 2 segons.\n- Opció explícita per desactivar només el watchdog; la porta general de seguretat continua activa.\n- Proves unitàries de timeout, rearmament, desactivació i límits de configuració.\n
- Home mort amb tecla de teclat configurable per l'usuari; tecla per defecte Espai, configuració desada localment i alliberament automàtic en perdre el focus de la finestra.


## 0.10 — Navegació del tauler 2D
- Zoom progressiu amb roda del ratolí centrat al cursor i controls accessibles +/−.
- Desplaçament amb Espai + arrossegar o botó central, limitat a l’àrea de la maqueta.
- Ajust a la superfície completa de 3000 × 1800 mm i indicador de percentatge.
- La navegació només modifica el viewBox SVG; no altera coordenades, peces, connexions ni dades del projecte.
- Proves unitàries de límits, ancoratge del zoom i proves d’integració de la UI.
