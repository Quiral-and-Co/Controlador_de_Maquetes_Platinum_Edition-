# Mòdul de validació i bloqueig de seguretat

## Estat
El mòdul `safety-core.js` és una barrera de programari fail-closed. Comença sempre BLOQUEJAT. Les comprovacions i la confirmació física són declaracions de l'operador, no mesures certificades per l'ordinador.

## Comprovacions
- Programari i proves automatitzades.
- Topologia i itineraris.
- Sensors, adreces i ocupació.
- Desviaments/feedback o exclusió de la ruta.
- Alimentacions, polaritats, seccions i fusibles.
- Parada d'emergència física independent.
- Cablejat, aïllament i proteccions.
- Pèrdua de comunicació i estat segur.

## Porta d'autorització
Per permetre l'activació, totes les comprovacions han d'estar marcades, l'operador ha de confirmar la inspecció física i l'estat en viu ha de confirmar JMRI connectat, alimentació ON, pont WiThrottle connectat, sensors vàlids i circuit d'emergència verificat. Qualsevol fallada ha de tornar a bloquejar.

El bloqueig de programari no substitueix un tall físic d'alimentació, un relé de seguretat ni una parada d'emergència cablejada independentment. No s'ha d'utilitzar aquest mòdul com a enclavament ferroviari certificat. Les proves automàtiques no validen el cablejat ni el comportament real de la maqueta.

## Proves
`node tests/safety.cjs` i `node tests/safety-bridge.cjs`
