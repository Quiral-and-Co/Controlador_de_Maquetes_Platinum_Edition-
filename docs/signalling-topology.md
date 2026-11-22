# Vinculació visual de senyals amb la topologia

Cada senyal pot tenir un `trackId` explícit que determina el seu punt d'ancoratge SVG. El bloc protegit aporta les peces de via protegides i l'itinerari associat aporta l'ordre dels blocs i les peces de via del recorregut.

La validació comprova referències a bloc, peça, itinerari i desviaments de tots els blocs de la ruta. Cada desviament ha de tenir system name, accessori verificat al catàleg i una posició requerida (2 recte o 4 branca). Les associacions incompletes es mostren com a incidències i mantenen l'aspecte en PARADA.

La vista SVG dibuixa el recorregut i el bloc protegit, marca el punt d'ancoratge de cada senyal i mostra un diagnòstic per senyal. Si no s'ha triat `trackId`, es pot mostrar provisionalment la primera peça del bloc però s'adverteix que l'ancoratge no és explícit.

## Límits de seguretat

Aquesta fase és només de modelatge, visualització i validació de dades. No envia ordres a desviaments, senyals ni sortides físiques. Les sortides continuen deshabilitades; les dades del catàleg i les proves de programari no certifiquen el cablejat ni la instal·lació real. Cal mantenir una parada d'emergència física independent i validar proteccions abans de qualsevol automatització.
