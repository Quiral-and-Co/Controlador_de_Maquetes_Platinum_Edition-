# Migració independent a Avalonia i .NET 10

## Decisions
- .NET 10 o superior; interfície Avalonia i AXAML, sense WPF ni navegador incrustat.
- C# en capes Domain, Application i Infrastructure.
- Persistència de projectes XML versionada.
- JSON només en memòria per a protocols externs quan sigui estrictament necessari; prohibit com a fitxer de projecte, configuració, catàleg o estat persistent.
- Cap dependència, connector, adaptador, API, codi, marca, interfície ni flux de treball dels productes expressament exclosos per la direcció del projecte.
- No reutilitzar codi de tercers sense revisar-ne procedència i llicència.
- Control físic desactivat per defecte fins a completar interbloqueig, validació i proves controlades.

## Estat real d'aquesta branca
Aquesta branca introdueix una solució Avalonia separada de l'aplicació WPF, els projectes Domain/Application/Infrastructure, un model inicial de blocs i itineraris, reserva lògica sense conflictes i persistència XML atòmica. La finestra Avalonia és només un esquelet de navegació. No hi ha paritat funcional amb l'aplicació existent ni s'ha verificat encara una compilació amb SDK .NET 10.

## Pla de migració
1. Inventariar totes les funcions, regles de negoci, fluxos, recursos i formats actuals.
2. Definir especificacions originals i verificar drets sobre catàlegs, dades, imatges i recursos.
3. Migrar editor 2D, geometria, nivells, topologia, undo/redo i validació a C#.
4. Migrar catàlegs a XML o recursos compilats, comprovant la procedència de cada dada.
5. Migrar sensors, blocs, itineraris, senyalització, diagnòstic i simulació amb proves de regressió.
6. Implementar adaptadors de maquinari només sobre documentació pública i especificacions autoritzades; qualsevol missatge JSON serà transitori i mai persistit.
7. Retirar els artefactes web/Electron antics, esquemes JSON i referències als productes exclosos només quan cada funció tingui equivalència provada.
8. Afegir proves d'integració, accessibilitat, rendiment, seguretat i instal·lació.

## Compilació
~~~sh
dotnet restore Platinum.Avalonia.sln
dotnet build Platinum.Avalonia.sln -c Release
dotnet run --project src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj
~~~

## Criteri de finalització
La migració només es considerarà acabada quan tots els mòduls tinguin implementació C#, UI Avalonia, persistència no JSON, proves de regressió i documentació. Aquesta branca és una primera base tècnica, no una migració completa.