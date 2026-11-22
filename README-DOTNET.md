# Quiral Platinum — migració nativa .NET 10

Aquesta branca inicia una reconstrucció nativa Windows amb C# i WPF. No utilitza Electron, HTML, JavaScript ni JSON v3 com a arquitectura o format de persistència.

## Arquitectura

- **Platinum.Domain**: model de domini sense dependències de UI ni infraestructura.
- **Platinum.Application**: casos d'ús i contractes de serveis.
- **Platinum.Infrastructure**: persistència, integracions i adaptadors externs.
- **Platinum.Presentation.Wpf**: interfície nativa WPF/XAML.

## Format de projecte nou

L'extensió és `.platinum`. És un paquet ZIP versionat amb:
- `manifest.xml`: identificació i versió del format.
- `layout.xml`: model estructurat de la maqueta.
- `assets/`: espai reservat per a recursos adjunts en versions posteriors.

El format té versió pròpia, validació de referències, límit de mida de les entrades i desament atòmic. No és JSON v3 ni una simple exportació d'aquest.

## Compilar

Requereix Windows i .NET 10 SDK:

```powershell
dotnet restore Platinum.sln
dotnet build Platinum.sln -c Release
dotnet run --project src/Platinum.Presentation.Wpf
```

## Estat

Aquesta primera entrega estableix la solució, el model base, el contracte de persistència, el paquet natiu i l'esquelet WPF. Els panells funcionals s'han de migrar per fases; la presència d'un panell al menú no significa que la seva lògica estigui implementada.

Totes les ordres físiques romanen bloquejades per defecte. Les simulacions, lectures de JMRI i validacions de software no equivalen a una prova física ni a un sistema ferroviari certificat.
