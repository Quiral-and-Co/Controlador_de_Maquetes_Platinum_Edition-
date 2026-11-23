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
- Servei de topologia compartit a Platinum.Application: valida peces existents, nivell comú, tolerància geomètrica dels ports, ports ja ocupats i evita connexions duplicades; l'editor Avalonia ja el fa servir.
- Proves xUnit per a connexions vàlides, tolerància, ports ocupats, nivells diferents i auto connexió.
- Domain, Application, Infrastructure i proves comparteixen ara target .NET 10 multiplataforma; WPF continua explícitament a net10.0-windows.
- CI de l'aplicació Avalonia ampliada a Windows, Linux i macOS, incloent execució de les proves .NET.
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
1. Completar eines avançades de topologia: ajustament automàtic de ports, orientació/compatibilitat de via, ramificacions i validació de xarxa; ja hi ha validació de distància, ocupació de port i nivell.
2. Completar la geometria del catàleg i el dibuix de ports/connexions.
3. Substituir la vista axonomètrica dibuixada en 2D per un motor 3D real integrat amb Avalonia; encara no està fet.
4. Migrar sensors, diagnòstic en viu i matriu de validació amb persistència, proves i traçabilitat; actualment els panells són parcials/simulats.
5. Completar la desvinculació de JMRI i WiThrottle i migrar els adaptadors de protocol independents; només hi ha prova de presència CS2 per ping, sense control físic.
6. Migrar l'actualitzador amb descàrrega segura, verificació d'integritat/signatura, rollback i instal·ladors reals per Windows, Linux i macOS; ara només es consulta GitHub i s'obre la Release.
7. Afegir proves automatitzades d'interacció Avalonia (headless/visuals); la CI multiplataforma compila i executa proves de domini/infrastructure, però encara no prova interaccions visuals Avalonia.
8. Eliminar el projecte WPF de la solució només quan la paritat funcional estigui demostrada.

## Seguretat
Tots els panells afegits en aquesta fase són locals o de simulació. No s'han habilitat ordres a locomotores, desviaments, senyals, frens ni altres sortides físiques. La parada d'emergència física independent i les proteccions elèctriques continuen sent obligatòries. Cap estat de simulació o casella de validació constitueix certificació ferroviària.


## Implementació de protocols, distribució i proves (fase actual)

- JMRI: adaptador propi HTTP/JSON amb timeout, validació de la resposta, desconnexió i reconnexió. En aquesta fase només verifica disponibilitat i estructura JSON; no envia ordres físiques.
- WiThrottle: adaptador TCP natiu amb handshake de versió, timeout, gestió de resposta invàlida i reconnexió. Les ordres de tracció continuen bloquejades.
- Märklin CS2: adaptador UDP CAN existent amb port de feedback configurable per proves; proves de ping, timeout, reconnexió i trames invàlides.
- Actualitzador: selecciona assets de GitHub Releases per RID, descarrega ZIP i manifest SHA-256, verifica el hash, extreu en staging, promou el directori i conserva/restaura backup davant una interrupció. La verificació SHA-256 comprova integritat respecte al manifest publicat, però no substitueix signatura criptogràfica del publicador.
- Windows: instal·lador Inno Setup x64.
- Linux: paquet DEB amd64.
- macOS: DMG per arm64 i x64.
- Els tres sistemes també publiquen ZIP self-contained amb manifest SHA-256, utilitzat per l'actualitzador.
- Proves: nou projecte Avalonia.Headless.XUnit per controls Avalonia i proves d'integració de protocols i actualitzador.
- CI: matriu Windows/Linux/macOS per compilar i executar proves; matriu de publicació per Windows x64, Linux x64 i macOS arm64/x64.

## Gate de retirada de WPF

La matriu `docs/avalonia-parity-status.json` continua inicialitzada amb tots els criteris a `false`. El workflow manual `.github/workflows/wpf-retirement-gate.yml` i el script `eng/verify-avalonia-parity.sh` bloquegen la retirada mentre no hi hagi paritat completa i absència de dependències WPF. El workflow no elimina el projecte: exigeix revisió humana i una PR separada.

## Pendents de seguretat i publicació

- Integrar JMRI/WiThrottle/CS2 amb el gate de seguretat físic abans d'habilitar cap ordre real.
- Signar executables i instal·ladors Windows, notaritzar el DMG de macOS i publicar manifests signats.
- Ampliar les proves headless a fluxos complets de l'editor, operacions i diagnòstic.
- Executar i revisar els workflows de tots els sistemes operatius; la creació dels workflows no equival a una validació completada.
- No retirar WPF fins que la matriu funcional sigui completada i revisada.
