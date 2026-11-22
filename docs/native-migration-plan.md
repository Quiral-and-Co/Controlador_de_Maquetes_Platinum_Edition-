# Pla de reconstrucció nativa — .NET 10 / WPF

## Principis no negociables

1. Windows natiu: C# i WPF/XAML; cap runtime Electron, navegador incrustat o lògica de negoci JavaScript.
2. Arquitectura per capes: Domain, Application, Infrastructure i Presentation.
3. Estat únic de domini; els panells consumeixen casos d'ús i no mantenen còpies independents del model.
4. Persistència pròpia `.platinum`, versionada, transaccional i validada. JSON v3 no és el format de projecte.
5. Seguretat per defecte: cap ordre física fins que totes les condicions runtime s'hagin verificat. Una casella declarativa no equival a prova física.
6. Cap funció es considera migrada perquè només existeixi al menú: cal implementar fluxos, persistència, errors, proves i accessibilitat.

## Fases

### Fase 0 — Fonaments (iniciada)
- Solució .NET 10 i WPF.
- Model de domini inicial de maqueta.
- Contractes d'aplicació i persistència.
- Paquet ZIP `.platinum` amb manifest XML i model XML.
- Porta central de seguretat i primeres proves de persistència.

### Fase 1 — Projecte i editor
- CRUD de projectes, còpia de seguretat, autosave i recuperació.
- Editor vectorial 2D, zoom, pan, graella, selecció múltiple, propietats, desfer/refer.
- Nivells, cotes, connexions/ports i validador topològic.
- Catàleg Märklin C amb geometria i nivell de confiança per dada.

### Fase 2 — Dades i intercanvi
- Blocs, sensors, registres i assignacions.
- Importadors/exportadors XTrackCAD amb informe explícit de pèrdues.
- Catàlegs de locomotores, accessoris i protocols.

### Fase 3 — Operació i integracions
- Adaptadors JMRI desacoblats, reconnectivitat i dades obsoletes/desconegudes.
- Dispatcher, itineraris, reserves i sincronització transaccional.
- Supervisió de desviaments, senyalització i diagnòstic.
- WiThrottle/DCC darrere de la porta de seguretat; simulació separada del maquinari real.

### Fase 4 — Panells avançats
- Matriu de validació física i registre d'auditoria.
- DMI de locomotora i cabina.
- Món de Jocs.
- Simuladors de protecció de tren i terminal tren-terra, sempre identificats com a simulacions no certificades.

### Fase 5 — Distribució i acceptació
- Instal·lador Windows x64, actualització, desinstal·lació i recuperació de dades.
- Proves de rendiment, accessibilitat, regressió i compatibilitat.
- Proves físiques només en banc controlat amb parada d'emergència independent.

## Criteri de finalització

Cada mòdul necessita: especificació de domini, UI WPF funcional, persistència nativa, validació, tractament de fallades, proves unitàries i d'integració, i evidència d'acceptació. Les funcions que depenen de maquinari no es declararan validades sense proves reals.
