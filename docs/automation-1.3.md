# Planificació supervisada d’horaris — 1.3.0

## Abast

`automation-core.js` és un motor determinista per validar horaris. No envia ordres a locomotores, desviaments, senyals ni accessoris, i no inicia circulacions. Un resultat `READY` és només un preflight lògic; necessita confirmació explícita de l’operador i no és una autorització elèctrica.

## Validacions

- Hora local `HH:mm`, dies de setmana i finestra d'inici d'1 a 15 minuts.
- Velocitat programada limitada a 20 passos WiThrottle.
- Itinerari i locomotora existents, primer bloc ocupat i blocs següents lliures.
- Validació PR #7 obligatòria (`safetyValidated === true`), JMRI connectat, alimentació ON, WiThrottle connectat, DCC armat i sensors vàlids.
- Desviaments requerits limitats als estats `CLOSED` o `THROWN`, verificats al registre i a la lectura en viu.
- Detecció de conflictes de reserva i protecció contra execucions duplicades el mateix dia.

## Integració

El codi que cridi el motor ha de derivar `safetyValidated` del resultat actual de `JMRI_TC_SAFETY.canEnablePhysical(runtime)`; no s'ha de confiar en una casella de selecció o valor guardat. Si manca qualsevol lectura, el preflight queda bloquejat.

## Proves

```bash
node --check automation-core.js
node tests/automation.cjs
npm test
```

## Límits de seguretat

La parada d'emergència cablejada independent, les proteccions elèctriques i les proves de sensors/desviaments a la maqueta continuen sent obligatòries. Aquesta funció no és un enclavament ferroviari certificat i no habilita cap sortida física.
