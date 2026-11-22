# Catàleg multiprotocol Märklin: DCC, MM2 i mfx

Aquesta branca afegeix un model de dades normalitzat per a locomotores i accessoris. No és una base de dades oficial de referències Märklin: és un registre de la maqueta que s'ha d'omplir i validar amb les dades reals de cada descodificador i la CS2 60215.

## Protocols i identitat

- **DCC:** adreça curta 1–127 o llarga 128–10239 per a locomotores; accessoris 1–2044. El control existent via WiThrottle continua limitat a DCC.
- **MM/MM2:** el registre admet adreces 1–255 per a locomotores i 1–320 per a accessoris, subjectes al descodificador i a la configuració real. No s'envien ordres MM2 pel canal WiThrottle DCC.
- **mfx:** es registra per UID de 6–16 dígits hexadecimals, no per una adreça DCC fixa. La CS2 gestiona identitats mfx; cal obtenir l'UID real de la CS2/JMRI.

## Accessoris

Tipus de registre: desviament, senyal, llum, desacoblador, relé i altres. Cada element conté protocol, adreça o UID, systemName JMRI, nivell, feedback, notes i verificació física.

Només s'ha preparat el constructor JSON de desviaments DCC cap a l'API JSON de JMRI (turnout state 2=closed, 4=thrown). L'operació requereix accessori verificat, JMRI connectat, alimentació ON i control armat. MM2, mfx, senyals i altres accessoris queden bloquejats fins que hi hagi un adaptador de protocol i un mapa d'estats provats.

## Proves

```powershell
node tests/protocols.cjs
node --check protocol-core.js
```

## Configuració CS2 60215

A JMRI, configura MARKLIN > Command Station 2 amb TCP/IP (recomanat) i la IP de la CS2. JMRI documenta suport de locomotores, desviaments, sensors i alimentació, i permet triar Märklin, DCC o mfx al throttle. La connexió CS2/MCAN no exposa totes les funcions de la web de la CS2; consulta la documentació de JMRI i verifica la versió instal·lada.

**Important:** WiThrottle documenta adreces de locomotora com DCC curta/llarga. No es pot deduir que MM2 o mfx funcionin enviant una adreça DCC. El control MM2/mfx necessita integrar-se amb la capa MARKLIN/MCAN nativa de JMRI i provar-se amb la CS2 real.

## Seguretat

- Cap UID ni adreça s'inventa o es descobreix per força bruta.
- Els elements nous no queden verificats.
- Les ordres MM2/mfx i les ordres de senyals/llum no implementades es rebutgen.
- El sistema no és un enclavament ferroviari certificat. Mantén accessible la parada física d'emergència.
