# JMRI TC Gold Custom — pla de desenvolupament 1.0

Estat: **en desenvolupament**. Aquest document defineix l'abast de la versió 1.0; no declara completades funcions que encara no existeixen.

## Objectiu

Convertir l'editor 0.9 en una eina de planificació i supervisió de maquetes Märklin H0 integrada amb JMRI, mantenint JMRI com a motor de maquinari i evitant ordres operatives fins que les proteccions estiguin verificades.

## Punt de partida verificat a 0.9

- Editor 2D de 3000 × 1800 mm amb cinc nivells.
- Peces gràfiques genèriques i biblioteca Märklin C amb 68 referències qualificades.
- Blocs lògics, registre de sensors i assignacions a peces.
- Connexions manuals de ports i diagnòstic topològic local.
- Importació/exportació del model JSON v3.
- Connexió JMRI JSON/WebSocket en mode només lectura.
- No hi ha encara control de locomotores, accessoris, reserves de blocs ni automatització operativa.

## Fites de la 1.0

### 1.0.0-alpha1 — estabilització del projecte
- Consolidar esquema JSON, migracions i validació d'importacions.
- Afegir proves de regressió per projectes v2/v3, referències inexistents i dades malformades.
- Separar clarament geometria dibuixada, topologia verificada i estat físic observat.
- Millorar missatges d'error i recuperació davant desconnexions de JMRI.

### 1.0.0-alpha2 — monitorització JMRI
- Descobrir i mostrar sensors configurats a JMRI.
- Mostrar estat UNKNOWN/INACTIVE/ACTIVE sense convertir valors desconeguts en estats segurs.
- Indicador de connexió, hora de l'últim missatge i registre d'esdeveniments.
- Reconnexió manual i tractament de missatges invàlids.
- Mantenir el mode només lectura durant aquesta fita.

### 1.0.0-beta1 — model operatiu supervisat
- Definir blocs amb límits, sensors d'entrada/sortida i seccions d'aturada.
- Validar que un bloc tingui topologia contínua i detecció suficient abans de marcar-lo com a candidat operatiu.
- Modelar desviaments, senyals i accessoris amb identificadors JMRI explícits.
- Simular ocupació i itineraris sense enviar ordres al maquinari.

### 1.0.0-beta2 — proves de seguretat i integració
- Proves amb JMRI en maqueta virtual o banc de proves desconnectat de la maqueta real.
- Interbloqueig de rutes incompatibles i gestió d'estats desconeguts.
- Parada d'emergència independent del navegador i del flux d'automatització.
- Registre auditable de canvis i ordres simulades.
- Revisió de recuperació després de pèrdua de xarxa o reinici.

### 1.0.0-rc1 — preparació de publicació
- Proves manuals a Windows 10/11 i navegadors compatibles.
- Validació d'importació/exportació amb còpies de seguretat.
- Guia d'instal·lació, configuració JMRI i resolució de problemes.
- Congelació d'esquema i documentació de limitacions conegudes.

## Condicions abans d'activar control real

No s'habilitarà el control de locomotores, desviaments ni rutes fins que:
1. Els identificadors DCC i JMRI s'hagin contrastat amb la central i el maquinari.
2. La topologia de la via i els blocs estigui verificada físicament.
3. Els sensors crítics hagin estat provats en tots els estats, inclòs UNKNOWN.
4. Les rutes incompatibles tinguin interbloqueig provat.
5. Hi hagi una parada d'emergència física independent i provada.
6. Les proves de fallada de comunicació i reinici hagin estat superades.

La parada d'emergència de programari, per si sola, no es considera una protecció suficient.

## Arquitectura objectiu

- **Editor:** geometria, nivells, catàleg i propietats.
- **Model de maqueta:** peces, ports, blocs, sensors, accessoris i rutes.
- **Adaptador JMRI:** protocol JSON/WebSocket aïllat de la interfície.
- **Motor de validació:** topologia, assignacions, ocupació i conflictes.
- **Dispatcher:** visualització i simulació d'itineraris; control real només després de superar les condicions de seguretat.
- **Automation:** seqüències declaratives amb simulació, registre i cancel·lació segura.

## Criteris d'acceptació de la 1.0

- Un projecte v3 vàlid es pot obrir, editar, desar i tornar a importar sense perdre identificadors ni assignacions.
- Les dades invàlides es rebutgen amb un missatge comprensible i sense substituir el projecte carregat.
- Els ports, blocs i sensors no verificats es distingeixen visualment dels verificats.
- La desconnexió de JMRI no bloqueja l'editor offline.
- Un estat de sensor desconegut mai no es tracta com a via lliure.
- Cap funció de control real s'activa per defecte.

## Relació amb TrainController

La referència a TrainController 10 Gold és funcional i d'experiència d'usuari: editor de maqueta, dispatcher, control DCC i automatitzacions. JMRI TC Gold Custom és un projecte independent i no incorpora codi, recursos ni interfície propietària de TrainController.
