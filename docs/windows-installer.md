# Instal·lador Windows 11

## Descarrega i instal·lació

Descarrega l'artefacte **JMRI-TC-Gold-Custom-Windows-11-NSIS** de l'execució verda del workflow **Windows 11 NSIS installer** a GitHub Actions. Extreu el ZIP i executa el fitxer `.exe`. L'assistent NSIS permet triar la carpeta d'instal·lació i crear accessos directes al menú Inici i a l'escriptori.

## Primer inici

1. Obre **JMRI TC Gold Custom** des del menú Inici o l'escriptori.
2. L'aplicació d'escriptori inicia el pont HTTP/WebSocket local en un port dinàmic vinculat a `127.0.0.1`; no cal iniciar manualment Node.js.
3. Configura el servidor JMRI i el port WiThrottle des de la interfície.
4. Tanca la finestra per aturar també el pont local.

## Seguretat

La compilació d'escriptori força `JMRI_TC_ENABLE_DCC=0` i `JMRI_TC_SAFETY_VALIDATED=0`. Per tant, el control DCC i les sortides físiques romanen desactivats per defecte. No canviïs aquests valors per fer proves inicials.

La CI verda i la generació correcta de l'instal·lador no substitueixen la inspecció individual del cablejat, les proteccions elèctriques ni la prova d'una parada d'emergència física independent. No habilitis automatització física fins que totes les comprovacions de seguretat i fail-safe estiguin completades.

## Requisits

Windows 11 x64. El paquet NSIS inclou el runtime d'Electron; no cal instal·lar Node.js separadament.
