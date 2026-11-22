# Supervisió de desviaments i itineraris (PR17)

## Model
Els itineraris poden declarar `turnoutRequirements`, una llista de `{ systemName, requiredState }`. Els estats admesos són `CLOSED` (JMRI 2, recte) i `THROWN` (JMRI 4, branca). També es pot introduir al Dispatcher com `LT1:2, LT2:4`.

Si una peça de tipus desviament està associada a un dels blocs de l'itinerari però no hi ha posició requerida explícita, la ruta queda bloquejada.

## Supervisió fail-safe
- Requereix heartbeat JMRI actual, lectura de desviament actual i accessori verificat.
- Una posició invàlida, absent, obsoleta, desconeguda o desalineada bloqueja la reserva.
- La pèrdua de comunicació no es tracta com a alineació.
- La recuperació només actualitza el diagnòstic; no mou desviaments ni reprèn circulacions.

## Integració
El Dispatcher comprova els desviaments abans de reservar itineraris, incloses les reserves demanades pel motor de circulacions. El panell mostra les incidències i el SVG marca desviaments alineats, desalineats o desconeguts.

## Seguretat
Aquest mòdul és de supervisió de lectura. No emet ordres JMRI, DCC, MM2 o mfx i no habilita sortides físiques. La validació física, l'E-stop independent i les proves elèctriques continuen sent requisits separats.
