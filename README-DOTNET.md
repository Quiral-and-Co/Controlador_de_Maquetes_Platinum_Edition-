# Quiral Platinum — aplicació principal Avalonia · .NET 10

Aquesta branca estableix **Avalonia UI com a interfície d'escriptori principal** de Quiral Platinum. El projecte WPF es conserva temporalment al repositori com a referència de migració, però ja no forma part de `Platinum.sln` ni és necessari per compilar o executar l'aplicació principal.

## Arquitectura

- **Platinum.Domain**: model de domini sense dependències de UI ni infraestructura.
- **Platinum.Application**: casos d'ús i contractes de serveis.
- **Platinum.Infrastructure**: persistència, integracions i adaptadors externs.
- **Platinum.Presentation.Avalonia**: aplicació d'escriptori principal amb Avalonia XAML.
- **Platinum.Presentation.Avalonia.Tests**: proves de la interfície Avalonia.
- **Platinum.Infrastructure.Tests**: proves de la infraestructura.

`Platinum.Presentation.Wpf` queda fora de la solució. No s'ha eliminat el codi font ni s'han esborrat fitxers: es podrà consultar durant la migració i retirar-lo en una fase posterior quan s'hagi confirmat que no cal recuperar cap funcionalitat.

## Requisits

- .NET 10 SDK.
- Windows per executar la versió d'escriptori actual.
- NuGet per restaurar les dependències d'Avalonia.

## Compilar, provar i executar

Des de l'arrel del repositori:

```powershell
dotnet restore Platinum.sln
dotnet build Platinum.sln -c Release
dotnet test Platinum.sln -c Release
dotnet run --project src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj
```

Per publicar una versió Windows x64 autocontinguda:

```powershell
dotnet publish src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj -c Release -r win-x64 --self-contained true
```

## Estat de la migració

La finestra principal Avalonia ja integra editor 2D/3D, catàleg Märklin C, dispatcher, operacions i gestió de projectes `.platinum`. Cal executar la compilació i les proves a cada canvi; la presència d'un panell a la interfície no garanteix per si sola que totes les seves funcions estiguin completades.

Les ordres físiques continuen bloquejades per defecte. Les simulacions, les lectures de sensors i les validacions de programari no equivalen a una prova física ni a un sistema ferroviari certificat. La connexió amb centrals digitals només s'ha d'habilitar després de completar les proves de seguretat corresponents.

## Format de projecte

L'extensió és `.platinum`, un paquet ZIP versionat amb manifest i dades de maqueta. La compatibilitat del format s'ha de mantenir a través de `Platinum.Domain`, `Platinum.Application` i `Platinum.Infrastructure`, sense dependre de la tecnologia de la interfície gràfica.
