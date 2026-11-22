# Motor de control propi de Platinum

## Objectiu

Substituir JMRI com a motor intermediari. Platinum defineix els seus propis contractes de domini, serveis d'aplicació i adaptadors per a centrals digitals. La interfície WPF parla amb aquests serveis; no necessita navegador, WebView ni servidor JMRI.

## Implementat en aquesta fase

- Contractes independents de fabricant: ordres de velocitat, funcions, accessoris i aturada d'emergència.
- Perfil de central tipat: simulació, Märklin CS2/CS3, Z21, ESU ECoS i DCC genèric.
- Servei d'aplicació amb validació d'adreces i rangs.
- Bloqueig explícit de qualsevol adaptador que no sigui de simulació.
- Central virtual local que permet provar el flux d'ordres sense connectar-se a la maqueta.

## Seguretat

L'adaptador virtual no obre sockets ni ports sèrie. Les ordres no arriben a cap dispositiu físic. Els perfils CS2/CS3, Z21 i ECoS són només descriptors; no impliquen que el protocol estigui implementat.

Abans d'habilitar una central real cal implementar el protocol corresponent, validar-ne els paquets amb documentació fiable i proves d'integració, afegir límits de velocitat i accessoris, supervisió de connexió, parada segura i interbloqueigs físics independents. Una parada d'emergència de programari no substitueix un circuit físic d'emergència.

## Compilació

Des d'un entorn Windows amb .NET 10 SDK:

```powershell
dotnet build src/Platinum.Presentation.Wpf/Platinum.Presentation.Wpf.csproj
dotnet test
```

Aquesta fase encara no connecta CS2/CS3 ni migra totes les funcions que abans poguessin dependre de JMRI.
