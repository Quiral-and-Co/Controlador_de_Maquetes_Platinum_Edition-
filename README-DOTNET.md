# Quiral Platinum — .NET 10 i Avalonia UI

Aquesta branca incorpora una nova capa d'escriptori multiplataforma amb C# i Avalonia UI 12 sobre .NET 10.

## Arquitectura compartida

- **Platinum.Domain**: model de domini sense dependències de UI ni infraestructura.
- **Platinum.Application**: casos d'ús i contractes de serveis.
- **Platinum.Infrastructure**: persistència, integracions i adaptadors.
- **Platinum.Presentation.Avalonia**: nova interfície multiplataforma.
- **Platinum.Presentation.Wpf**: interfície Windows existent, conservada temporalment durant la migració.
- **Platinum.Infrastructure.Tests**: proves de persistència, serveis i protocols.

La interfície Avalonia reutilitza els projectes Domain, Application i Infrastructure. No duplica la lògica ferroviària dins dels ViewModels.

## Plataformes

Avalonia s'utilitza per preparar versions d'escriptori Windows, Linux i macOS. La disponibilitat i el nivell de suport de cada sistema operatiu s'han de verificar a la matriu oficial d'Avalonia i a la CI de cada release.

## Format de projecte

L'extensió nativa continua sent `.platinum`: paquet ZIP versionat amb `manifest.xml` i `layout.xml`. La migració a Avalonia no canvia el format ni invalida els projectes existents.

## Compilar i executar Avalonia

Requereix el .NET 10 SDK.

```bash
dotnet restore src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj
dotnet build src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj -c Release
dotnet run --project src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj
```

## Estat de la migració

La finestra Avalonia inicial i la seva estructura de projecte estan incorporades. Els editors WPF, el Dispatcher i les vistes operatives s'han de migrar progressivament amb proves de paritat. WPF no s'elimina fins que les funcionalitats equivalents d'Avalonia estiguin implementades i verificades.

Totes les ordres físiques romanen bloquejades per defecte. La interfície, les simulacions i les dades declaratives no equivalen a una prova física ni a un sistema ferroviari certificat.
