# Migració nativa a Avalonia i .NET 10+

## Arquitectura

Platinum utilitza Avalonia UI per a la presentació i C#/.NET 10 per a Domain, Application i Infrastructure. La interfície i les regles de negoci són natives; no s'executen pàgines web, scripts de navegador ni serveis Java.

- UI: controls Avalonia i XAML compilat.
- Model i serveis: C# tipat, contractes propis i dependències explícites.
- Projectes: contenidor ZIP amb manifest i model XML validat; no depèn de serialització JSON.
- Catàleg Märklin C: dades tipades i compilades dins de Platinum.Presentation.Avalonia.
- Control: contracte IControlStationAdapter propi. La simulació és local. Märklin CS2 conserva la consulta nativa de presència CAN/UDP. Altres centrals només s'activaran quan hi hagi un protocol del fabricant implementat i validat directament en C#.

## Dependències eliminades

- Adaptador JMRI HTTP/JSON i les seves proves.
- Adaptador WiThrottle TCP i les seves proves.
- Tipus d'estació JMRI/WiThrottle i registre a la fàbrica d'adaptadors.
- Carregador i fitxer extern de catàleg JSON; ara és un catàleg C# tipat.
- Consulta d'actualitzacions de GitHub que consumia JSON; retirada de la UI per eliminar aquest protocol de runtime.

## Límits explícits

No es declara compatibilitat amb JMRI, WiThrottle, Z21, ESU ECoS ni Märklin CS3 sense un adaptador natiu provat amb la central física. El protocol Märklin CS2 implementat és només una consulta de presència; les ordres físiques continuen bloquejades. No s'ha inventat cap protocol substitutiu.

## Compilació i proves

```sh
dotnet restore Platinum.sln
dotnet build src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj -c Release
dotnet test Platinum.sln -c Release
```

WPF continua a la solució fins que la matriu de paritat funcional estigui completada i revisada.
