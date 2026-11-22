# Protocol natiu Märklin CS2 a Platinum

## Objectiu

Platinum parla directament amb la Märklin CS2 mitjançant el transport CAN-over-Ethernet documentat per Märklin. No s'utilitza JMRI, el seu servidor JSON, WebSocket, Java ni WiThrottle.

## Implementat

- `MarklinCs2Adapter` obre el socket UDP local de feedback `15730` i envia datagrames al port de comandaments CS2 `15731`.
- `MarklinCs2FrameCodec` codifica i valida el paquet Ethernet de 13 bytes: ID CAN big-endian (4), DLC (1) i dades (8).
- La connexió només envia la consulta de presència (ping, comandament `0x18`, DLC 0) i espera una resposta vàlida de 8 bytes.
- `ControlStationAdapterFactory` ja crea l'adaptador natiu per a perfils `MarklinCs2`.

## Encara bloquejat

Les ordres de velocitat, funcions, accessoris, senyals, alimentació i emergència física no s'envien. `RailwayControlService` rebutja qualsevol adaptador no simulat i l'adaptador CS2 també rebutja ordres físiques. Abans d'habilitar-les cal completar:

1. Registre explícit i verificat de Loc-ID/UID per a cada locomotora i accessori; no s'infereix el protocol a partir d'una adreça genèrica.
2. Correlació i confirmació d'ordres, gestió de pèrdua de paquets, estat d'alimentació i esdeveniments S88.
3. Porta de seguretat runtime integrada amb `HardwareSafetyState` i una parada física independent.
4. Proves d'integració amb CS2 real, diferents versions de firmware i maqueta de proves, abans de permetre sortides físiques.

La CS2 ha de tenir activat el gateway CAN i configurada perquè enviï les respostes UDP a la IP del PC, port 15730. Platinum envia a la IP de la central, port 15731. El port del protocol és fix al document de Märklin.

## Proves

Les proves `MarklinCs2FrameCodecTests` comproven la mida, el fingerprint, els camps i la creació del perfil. Són proves de codec en memòria, no una prova de connexió física.

## Referència del protocol

Märklin, *CAN CS2 Protocol 2.0* (7-2-2012), seccions 1.1–1.2.7 (format i Ethernet), 3.4–3.6 (locomotores), 4.1 (accessoris), 5.2 (feedback events) i 6.1 (ping). El document adverteix que es proporciona sense garantia; cal validar el comportament amb el firmware i el maquinari reals.
