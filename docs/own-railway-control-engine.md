# Motor de control propi de Platinum

## Objectiu

Substituir JMRI com a motor intermediari. Platinum defineix els seus propis contractes de domini, serveis d'aplicació i adaptadors per a centrals digitals. La interfície WPF parla amb aquests serveis; no necessita navegador, WebView ni servidor JMRI.

## Implementat en aquesta fase

- Contractes independents de fabricant: ordres de velocitat, funcions, accessoris i aturada d'emergència.
- Perfil de central tipat: simulació, Märklin CS2/CS3, Z21, ESU ECoS i DCC genèric.
- Servei d'aplicació amb validació d'adreces i rangs.
- Bloqueig explícit de qualsevol ordre física fins que hi hagi validació runtime i proves de maquinari.
- Central virtual local que permet provar el flux d'ordres sense connectar-se a la maqueta.
- Adaptador natiu Märklin CS2 amb transport UDP CAN-over-Ethernet, codec de trames de 13 bytes i consulta de presència (ping).

## Seguretat

L'adaptador virtual no obre sockets ni ports sèrie. Les ordres no arriben a cap dispositiu físic. El perfil CS2 ja disposa d'un transport natiu de consulta, sense JMRI. CS3, Z21 i ECoS continuen pendents. L'adaptador CS2 encara no envia ordres físiques: només comprova la presència de la central.

Abans d'habilitar una central real cal implementar el protocol corresponent, validar-ne els paquets amb documentació fiable i proves d'integració, afegir límits de velocitat i accessoris, supervisió de connexió, parada segura i interbloqueigs físics independents. Una parada d'emergència de programari no substitueix un circuit físic d'emergència.

## Compilació

Des d'un entorn Windows amb .NET 10 SDK:

```powershell
dotnet build src/Platinum.Presentation.Wpf/Platinum.Presentation.Wpf.csproj
dotnet test
```

Aquesta fase ja implementa el handshake de xarxa natiu de CS2, però encara no migra totes les funcions de JMRI ni habilita ordres físiques. Consulta [protocol natiu CS2](native-marklin-cs2-protocol.md).
