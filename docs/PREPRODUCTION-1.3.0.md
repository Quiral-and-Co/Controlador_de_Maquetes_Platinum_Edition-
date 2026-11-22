# Pla de preproducció — JMRI TC Gold Custom 1.3.0

**Estat:** candidat de preproducció documental; no és una autorització de servei físic ni una publicació estable.  
**Branca base:** `main`  
**Commit base:** `a550753c8637a8c33d092f21768c35be94d6e8ce`  
**Versió declarada a `package.json`:** `1.3.0`  
**Branca de treball:** `release/preproduction-1.3.0`

## 1. Objectiu

Preparar una versió reproduïble de preproducció per a Windows 11 x64, amb regressió completa, validació geomètrica, comprovació de l'instal·lador NSIS, documentació coherent i evidències traçables.

Aquesta fase no habilita automàticament DCC, sortides físiques, automatismes ni boosters. La posada en servei de la maqueta continua sent una autorització separada, presencial i supervisada.

## 2. Estat inicial conegut

- El repositori té `main` com a branca principal i `package.json` declara `1.3.0`.
- El paquet requereix Node.js >=20; el workflow NSIS configura Node.js 22.
- El paquet defineix `npm test`, `npm run test:browser` i `npm run dist:win`.
- El workflow `.github/workflows/windows-installer.yml` executa `npm install --no-package-lock` (el repositori no conté lockfile), `npm test`, les proves reals de navegador amb Playwright i `node tests/editor-geometry-acceptance.cjs`; després genera l'instal·lador NSIS x64, verifica que l'EXE no és buit i calcula el SHA-256 en un manifest adjunt.
- La CI de regressió #940 ha acabat correctament sobre el SHA `0553de422c01d8c9a69e3aa5b5411bc63be6034e`; això no substitueix l'execució Windows NSIS, les proves d'instal·lació ni la CI sobre el `main` final.
- El `README.md` ja s'ha actualitzat per identificar el producte com a 1.3.0 i descriure els límits de seguretat; cal mantenir-lo coherent amb `package.json` i les notes de versió.
- La documentació de circulacions identifica explícitament limitacions del MVP: DCC no equival a suport físic mfx/MM2, el mode automàtic no és apte per a circulació real i el sistema no és un enclavament ferroviari certificat.
- El mòdul de seguretat és fail-closed i les confirmacions de l'operador no són mesures instrumentals ni certificació.

## 3. Porta A — Identitat i netedat del candidat

- [ ] Confirmar que la branca candidata parteix de l'últim `main` aprovat.
- [ ] Confirmar versió única i coherent a `package.json`, aplicació, instal·lador i notes de versió.
- [ ] Revisar dependències, fitxers inclosos a l'asar i exclusió de proves/documentació no necessàries.
- [ ] Revisar que no hi hagi secrets, IPs privades, credencials ni dades locals als fitxers distribuïts.
- [ ] Actualitzar README i notes de versió perquè descriguin el producte 1.3.0 real, no l'editor 0.9.
- [ ] Documentar funcionalitats simulades, funcions només de lectura i funcions amb ordres físiques separadament.
- [ ] Revisar instruccions d'instal·lació, configuració, recuperació i desinstal·lació.

**PASS:** identitat coherent, documentació actualitzada i cap secret o dada local.  
**FAIL:** qualsevol discrepància de versió, instrucció enganyosa o dada sensible inclosa.

## 4. Porta B — Regressió i seguretat del programari

Executar en entorn net amb Node.js 22:

```bash
npm ci
npm test
npm run test:browser
node tests/editor-geometry-acceptance.cjs
```

- [ ] Suite completa sense errors.
- [ ] Proves browser sense errors.
- [ ] Moviments XYZ i límits de superfície.
- [ ] Vies paral·leles a 77 mm i 64 mm.
- [ ] Geometria de corbes, unions i topologia.
- [ ] Importació/exportació i compatibilitat de projectes.
- [ ] Mòduls de diagnòstic, senyalització i protecció.
- [ ] Fail-safe, pèrdua de comunicació, dead-man i parada d'emergència lògica.
- [ ] Bloqueig per defecte de les sortides físiques i del pont DCC.
- [ ] Cap regressió que permeti saltar-se la porta de seguretat.

**PASS:** totes les proves requerides acaben amb codi 0 i les evidències queden associades al SHA candidat.  
**FAIL:** qualsevol prova crítica fallida, omesa o sense evidència.

## 5. Porta C — Instal·lador Windows 11 x64

```bash
npm run dist:win
```

- [ ] Workflow Windows 11 completat amb èxit sobre el SHA candidat.
- [ ] `npm install --no-package-lock`, `npm test`, `npm run test:browser` i la porta geomètrica completats amb èxit al workflow Windows.
- [ ] EXE NSIS generat i no buit.
- [ ] Nom i versió de l'artefacte coherents amb 1.3.0.
- [ ] SHA-256 calculat, verificat de nou i publicat amb l'artefacte.
- [ ] Instal·lació neta en una màquina Windows 11 x64 de prova, amb evidència externa al build.
- [ ] Arrencada i tancament de l'aplicació en Windows 11.
- [ ] Confirmar que DCC i sortides continuen deshabilitats després d'instal·lar.
- [ ] Actualització des de la versió anterior, preservant les dades d'usuari, en una màquina Windows 11 de prova.
- [ ] Desinstal·lació sense esborrar dades d'usuari de manera inesperada.
- [ ] Recollir logs i resultat de les proves d'instal·lació.

**PASS:** instal·lador reproduïble, verificat i provat en Windows 11.  
**FAIL:** EXE absent, corrupte, versió incorrecta, pèrdua de dades o arrencada insegura.

## 6. Porta D — Validació de CI i traçabilitat

- [ ] Regressió completa verda sobre el commit exacte candidat.
- [ ] Workflow NSIS verd sobre el mateix commit.
- [ ] Validació browser i geometria registrada.
- [ ] No hi ha comprovacions requerides pendents, cancel·lades o ignorades.
- [ ] Registrar URLs de les execucions, SHA del commit i hash de l'EXE.
- [ ] Revisar els logs de qualsevol advertiment o retry.
- [ ] Repetir la CI sobre el commit final després de qualsevol correcció.

**PASS:** totes les execucions exigides són verdes i corresponen al mateix SHA.  
**FAIL:** execució absent, incompleta, fallida o associada a un altre commit.

## 7. Porta E — Límits operatius i seguretat física

- [ ] Arrencada segura sense armar DCC.
- [ ] Variables `JMRI_TC_ENABLE_DCC` i `JMRI_TC_SAFETY_VALIDATED` absents per defecte.
- [ ] Sortides físiques bloquejades fins a autorització independent.
- [ ] Parada d'emergència física independent disponible i verificada presencialment.
- [ ] Proteccions, polaritat, alimentació, seccions i cablejat revisats amb instruments adequats.
- [ ] Pèrdua de comunicació amb CS2/JMRI/WiThrottle tractada com a estat de fallada.
- [ ] Cap prova automàtica presentada com a certificació elèctrica o ferroviària.

**PASS:** mode segur confirmat i evidència de les proteccions físiques separada de les proves de programari.  
**FAIL:** qualsevol protecció absent, incerta o substituïda només per una casella de confirmació.

## 8. Criteris de publicació

No crear ni publicar una versió estable fins que les portes A–E siguin PASS.

Seqüència proposada:

1. Actualitzar documentació i notes de versió a la branca candidata.
2. Obrir PR en esborrany contra `main`.
3. Executar tota la CI requerida sobre el SHA de la PR.
4. Revisar els resultats i corregir qualsevol FAIL.
5. Aprovar i fusionar només amb totes les comprovacions verdes.
6. Repetir regressió i NSIS sobre el nou `main`.
7. Publicar primer un candidat `v1.3.0-rc.1` amb EXE i SHA-256.
8. Després de l'acceptació del candidat, publicar `v1.3.0` com a estable.

No reutilitzar un artefacte d'una execució anterior com si fos el resultat del commit nou.

## 9. Autorització

La publicació del programari i l'autorització de la maqueta són decisions diferents. Aquest pla no autoritza circulació, activació de sortides, DCC, automatismes ni connexió de boosters Märklin 60175. Aquestes proves requereixen el protocol de posada en servei supervisada i autorització expressa de l'operador.

## 10. Registre d'evidències

| Evidència | Valor |
|---|---|
| SHA candidat | Pendent |
| CI regressió | Pendent |
| CI NSIS | Pendent d'execució sobre SHA candidat actualitzat |
| CI browser/geometria | Pendent |
| Nom de l'EXE | Pendent |
| Mida de l'EXE | Pendent |
| SHA-256 de l'EXE | Pendent |
| Prova instal·lació Windows 11 | Pendent |
| Responsable i data d'acceptació | Pendent |
