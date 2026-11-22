# Configuració de centrals digitals

## Objectiu
El mòdul `command-station-core.js` i la interfície `command-station-ui.js` permeten registrar diverses centrals, seleccionar fabricant/model, protocol i interfície, i guardar paràmetres de connexió.

## Catàleg inicial
Märklin CS2/CS3, ESU ECoS, Roco Z21/z21, Digitrax DCS, Lenz LZV, NCE PowerCab/Powerhouse Pro, Hornby Elite, PIKO SmartControl, DCC-EX EX-CommandStation, SPROG, Uhlenbrock Intellibox I/II/II Neo/3, Digikeijs ADJ9000 i YaMoRC. Els protocols i interfícies s'assignen per model al catàleg; no es permet una combinació arbitrària.

## Uhlenbrock, Digikeijs i YaMoRC
- Intellibox I: LocoNet via RS-232/sèrie; el perfil preveu 19200 baud com a punt de partida documentat.
- Intellibox II: LocoNet via USB; 115200 baud és el valor de fàbrica documentat.
- Intellibox II Neo i Intellibox 3: LocoNet per USB i LocoNet-over-TCP, Z21 i WiThrottle per WLAN/xarxa. L'adreça/port s'han de confirmar a la configuració de la unitat.
- YaMoRC: LocoNet-over-TCP (port 1234 segons la documentació JMRI per a YaMoRC) o LocoNet per USB/sèrie segons model/interfície.
- Digikeijs ADJ9000: entrada provisional i no verificada. No s'ha trobat documentació fiable que identifiqui aquesta referència exacta; no s'ha de confondre amb DR5000. Confirmar referència i manual abans de qualsevol connexió real.

## Connexió
- TCP/Wi-Fi/WebSocket: host, port, ruta opcional i timeout.
- USB/Serial: identificador de port i baud rate.
- La interfície valida els camps i persisteix els perfils al navegador. Permet exportar/importar JSON.
- Aquesta entrega és un gestor de perfils, no un controlador de maquinari: no obre sockets, no detecta ports sèrie i no executa cap ordre de tracció.

## Seguretat
Totes les centrals noves queden `verified:false`; l'estat de connexió és `NOT_TESTED`; `physicalOutputsEnabled:false` és invariant. La selecció d'una central no arma DCC ni canvia la porta de seguretat. DCC, MM2, mfx, accessoris i automatismes físics continuen bloquejats fins a integrar i provar adaptadors específics, validar fail-safe, i completar inspecció elèctrica i parada d'emergència física independent.

## Proves
`node tests/command-station.cjs` i suite completa `npm test`.


## Referències tècniques
- JMRI Uhlenbrock Intellibox: https://www.jmri.org/help/en/html/hardware/loconet/Uhlenbrock.shtml
- Manual Intellibox II Neo: https://uhlenbrock-shop.de/
- Manual resum Intellibox 3: https://www.uhlenbrock.de/de_DE/service/download/handbook/de/Intellibox%203%20Kurzanleitung.pdf
- JMRI Digikeijs/YaMoRC: https://www.jmri.org/help/en/html/hardware/digirails/index.shtml
