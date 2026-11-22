# Migració nativa .NET 10 WPF

## Objectiu
L'executable d'escriptori s'inicia amb WPF i no necessita navegador, servidor web, WebView, Node.js ni cap runtime HTML/CSS/JavaScript. La interfície nativa es defineix amb XAML i controls WPF; la lògica de presentació i domini s'escriu en C#.

## Estat d'aquesta fase
- L'editor 2D existent continua sent un control WPF natiu (`TrackEditorView`), amb catàleg Märklin C, peces, ports, connexions i geometria dibuixada amb `PathGeometry`.
- S'afegeix una vista nativa WPF per a Dispatcher, senyalització, perfils de centrals, DMI, protecció ferroviària, terminal tren-terra, Món de Jocs i matriu de validació.
- El Dispatcher natiu ja disposa de gestió de blocs i ocupació lògica, creació d'itineraris, reserves atòmiques sense conflictes i alliberament quan un bloc deixa d'estar lliure. El model d'ocupació i les reserves formen part del `TrackLayout` i es serialitzen al paquet `.platinum`. La interfície és només de simulació i no té cap adaptador físic.
- Els panells nous implementen operacions locals de planificació i simulació; no obren connexions de xarxa ni envien ordres a JMRI, CS2/CS3, DCC o maquinari.
- Els mòduls web antics es conserven al repositori com a referència de funcionalitat durant la migració. No formen part del projecte WPF ni es carreguen en temps d'execució. No s'esborren fins que cada funció tingui equivalència nativa i proves.

## Compilar i executar a Windows

```powershell
dotnet restore Platinum.sln
dotnet build Platinum.sln -c Release
dotnet run --project src/Platinum.Presentation.Wpf/Platinum.Presentation.Wpf.csproj
```

## Fases pendents abans de declarar paritat funcional
1. Portar els esquemes JSON v3 i l'importador/exportador XTrackCAD al model de domini C#.
2. Migrar els motors de topologia, rampes/hèlix, suggeriment de buits i historial desfer/refer a serveis C# amb proves.
3. Completar la persistència dels sensors, senyals, perfils i registres en el paquet `.platinum`; els blocs, itineraris, ocupacions i reserves lògiques ja formen part del `TrackLayout` natiu.
4. Portar els protocols JMRI/CS2 i WiThrottle a adaptadors C# només després d'afegir proves de protocol, timeout, reconexió i interbloqueig independent. La sortida física continua bloquejada.
5. Executar les proves del Dispatcher i afegir proves WPF d'interacció; validar la compilació completa en Windows .NET 10.

No s'ha de suprimir la versió web de referència fins que aquests punts estiguin completats i revisats.
