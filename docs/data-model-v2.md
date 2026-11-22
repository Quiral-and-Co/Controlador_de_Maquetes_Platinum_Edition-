# Model de dades v2 — JMRI TC Gold Custom

El model separa geometria, topologia i control. Una connexió dibuixada no es considera validada elèctricament ni operativament.

## Entitats

- **Layout**: `schemaVersion`, escala, sistema de via, dimensions de la superfície, separació vertical, nivells i connexions.
- **Level**: `id`, `name`, `elevationMm`, `tracks[]`, `blocks[]`, `sensors[]`. Les cinc cotes inicials són 0, 110, 220, 330 i 440 mm; són valors de projecte pendents de mesura física.
- **TrackPiece**: `id`, `type` (`straight`, `curve`, `turnout`, `block`), `name`, `x`, `y`, `rotation`, `levelId`, `blockId`, `sensorName` i `geometry`. `x/y` són coordenades en mm respecte de l'angle superior esquerre del nivell. La rotació és en graus.
- **Ports**: identificadors locals d'extrem (`A`, `B`, `C`). Recta: A/B; corba: A/B; desviament: A/B/C. Són ports geomètrics, no connexions elèctriques.
- **Connection**: `id`, `levelId`, `from {pieceId,port}`, `to {pieceId,port}`, `verified`. L'editor crea una connexió si els ports estan a 20 mm o menys; `verified` queda sempre `false` en aquesta versió.
- **Block**: `id`, `name`, `trackIds[]`, `lengthMm`, `sensorIds[]`. No es generen blocs reals automàticament.
- **Sensor**: `id`, `systemName`, `userName`, `invert`, `verified`. `systemName` ha de correspondre a un sensor existent a JMRI; `verified` és fals fins a una prova física.

## Regles de seguretat

1. Cap geometria de paleta equival a una peça de catàleg Märklin C. Cal introduir més endavant les geometries exactes a partir de les referències reals.
2. La connexió geomètrica no valida continuïtat elèctrica, polaritat, cantons, galibos ni accessibilitat.
3. No hi ha cap ordre de comandament de locomotores, desviaments, rutes ni automatismes en l'editor 0.2.
4. Els sensors només es llegeixen del WebSocket JSON de JMRI; la vinculació s'ha de verificar amb l'estat real de cada detector.
5. Les connexions entre nivells requeriran un tipus explícit d'enllaç vertical en una versió posterior; no es connecten automàticament plantes diferents.
