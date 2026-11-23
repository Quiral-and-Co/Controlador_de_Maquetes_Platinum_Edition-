# Migració de Platinum a Avalonia UI

## Objectiu
Crear una presentació d'escriptori nativa multiplataforma amb Avalonia UI i .NET 10, reutilitzant Platinum.Domain, Platinum.Application i Platinum.Infrastructure. El projecte WPF es conserva temporalment com a referència fins a assolir paritat i completar la validació.

## Estat de la branca
Branca: feature/avalonia-migration

### Portat en aquesta fase
- Projecte Platinum.Presentation.Avalonia amb Avalonia 12.1.6 i .NET 10.
- Arrencada nativa Avalonia i tema Fluent.
- Finestra principal Platinum amb tema fosc i estructura de treball.
- Reutilització del model TrackLayout, ProjectService i PlatinumPackageStore.
- Obrir i desar paquets .platinum mitjançant StorageProvider d'Avalonia.
- Catàleg Märklin C carregat des de l'asset existent.
- Editor 2D: graella, geometria recta/corba/desviament, col·locació de peces, selecció i moviment, gir, eliminació, zoom, pan, connexió per ports i historial desfer/refer.
- Ports i traçats Märklin C portats al model Avalonia, amb etiquetes d'exactitud estimada/derivada.
- Vista 3D axonomètrica: geometria de catàleg, connexions, nivells, càmera orbitable i zoom.
- Dispatcher local: blocs, ocupacions, itineraris, reserves i alliberament amb el servei d'aplicació existent.
- Vista axonomètrica inicial del model compartit amb rotació i zoom.
- Panells de simulació per automatització, senyalització, perfils de centrals, DMI, protecció, tren-terra, Món de Jocs i validació.
- Port del panell de control propi de Platinum amb RailwayControlService i LocalSimulationAdapter: velocitat virtual, funcions, accessoris i aturada d'emergència virtual; no hi ha sortides físiques.
- Prova de presència de Märklin CS2 portada a Avalonia: consulta CAN/UDP de ping, amb timeout i desconnexió explícita. No habilita ordres de tracció ni accessoris.
- Panell d'enclavaments lògics basat en RailwayAutomationEngine: blocs, reserves atòmiques, avaluació conservadora de senyals, aturada enclavada, reconeixement i auditoria.
- Consulta de versions de GitHub portable: comprova la Release més recent i obre la pàgina oficial; la instal·lació automàtica encara no s'ha portat perquè el paquet WPF actual és específic de Windows.

## Compilació
dotnet restore Platinum.sln
dotnet build src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj -c Release
dotnet run --project src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj

## Pendents abans de substituir definitivament WPF
1. Portar les eines avançades de l'editor 2D: selecció, moviment múltiple, rotació, ports, connexions, desfer/refer, zoom i pan.
2. Completar la geometria del catàleg i el dibuix de ports/connexions.
3. Completar la vista 3D amb la mateixa semàntica de càmera i geometria que l'editor WPF.
4. Completar la matriu de validació i la persistència de sensors, senyals, perfils i registres.
5. Portar els protocols JMRI, CS2 i WiThrottle com a adaptadors separats, amb proves de protocol, timeout, reconexió i supervisió.
6. Migrar l'actualització des de GitHub i la publicació/instal·lació per plataforma.
7. Afegir proves automatitzades d'interacció Avalonia i executar compilació i proves a Windows, Linux i macOS.
8. Eliminar el projecte WPF de la solució només quan la paritat funcional estigui demostrada.

## Seguretat
Tots els panells afegits en aquesta fase són locals o de simulació. No s'han habilitat ordres a locomotores, desviaments, senyals, frens ni altres sortides físiques. La parada d'emergència física independent i les proteccions elèctriques continuen sent obligatòries. Cap estat de simulació o casella de validació constitueix certificació ferroviària.
