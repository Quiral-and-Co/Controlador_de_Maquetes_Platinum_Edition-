# Adaptador JMRI JSON per a accessoris DCC

Aquest mòdul proporciona una connexió WebSocket configurable al servidor JSON de JMRI. JMRI ha d'estar connectat prèviament a la Märklin CS2 60215; aquest adaptador no parla directament amb la central.

## Funcions

- Connexió a `ws://HOST:12080/json/` (host, port, ruta i temps d'espera configurables).
- Lectura d'estat de desviaments i sensors per `systemName` JMRI.
- Ordres de desviament DCC amb estats JMRI CLOSED (2) i THROWN (4).
- Rebuig explícit de protocols diferents de DCC, accessoris no verificats i respostes incompletes o discrepants.
- Correlació de respostes, temps d'espera i fallada de peticions pendents quan es perd la connexió.

## Bloqueig de seguretat

Les sortides físiques continuen bloquejades per defecte. Una ordre només es pot tramitar si el sistema invocador proporciona simultàniament:

- `safetyValidated: true`, derivat de la validació de seguretat actual (no d'un valor desat).
- `physicalOutputsEnabled: true`, habilitació explícita posterior a la validació.
- JMRI connectat, alimentació confirmada ON i control d'accessoris armat.
- Desviament DCC verificat i confirmació que la parada d'emergència física independent està alliberada.

Aquest mòdul no habilita cap sortida en inicialitzar-se i no substitueix l'enclavament ni la parada d'emergència cablejada. No s'ha provat amb una CS2 física. Mantén les sortides deshabilitades fins a completar la validació elèctrica i física de la instal·lació.

## Proves

```bash
node --check jmri-json-adapter.js
node tests/jmri-json-adapter.cjs
npm test
```

Les proves utilitzen un servidor WebSocket local simulat; no constitueixen una prova de maquinari.
