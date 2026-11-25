# Migració de Platinum a Avalonia UI — .NET 10

## Decisió d'arquitectura

La interfície multiplataforma nova s'implementa amb Avalonia UI 12 i C# sobre .NET 10.
Els projectes compartits `Platinum.Domain`, `Platinum.Application` i `Platinum.Infrastructure` continuen sent la base del domini i dels casos d'ús.

La interfície WPF existent es conserva temporalment per evitar perdre funcionalitats. No es duplica ni es reescriu la lògica de negoci a la capa de presentació. La migració de pantalles es fa progressivament a `Platinum.Presentation.Avalonia`.

## Estat inicial

- [x] Projecte d'escriptori Avalonia afegit a la solució.
- [x] Target framework `net10.0`.
- [x] Referències als projectes Domain, Application i Infrastructure.
- [x] Tema Fluent i tipografia Inter.
- [x] Finestra principal inicial amb navegació conceptual i estat segur.
- [x] Catàleg Märklin C vinculat com a recurs de contingut.
- [ ] Compilació i execució verificades en Windows.
- [ ] Compilació i execució verificades en Linux.
- [ ] Compilació i execució verificades en macOS.
- [ ] Migració dels editors WPF a controls Avalonia.
- [ ] Migració de les operacions i serveis de presentació.
- [ ] Proves d'interfície, accessibilitat i persistència.

## Regles de migració

1. No eliminar WPF fins que cada funció migrada tingui paritat funcional i proves.
2. No portar codi de domini ni persistència a la capa Avalonia.
3. Compartir els mateixos serveis d'aplicació i el mateix format `.platinum`.
4. Mantenir la porta de seguretat física bloquejada per defecte.
5. Distingir sempre estat real, virtual, calculat, estimat i desconegut.
6. Cap vista o ViewModel pot enviar directament trames a una central.
7. Cada connector físic passa pels serveis d'aplicació, permisos, validació de capacitats i porta de seguretat.
8. La migració no introdueix dependències de programari ferroviari extern.

## Ordre de migració

1. Shell Avalonia i serveis comuns.
2. Gestió de projectes i paquet `.platinum`.
3. Catàleg i inventari.
4. Editor 2D, geometria i topologia.
5. Blocs, sensors i itineraris.
6. Trens, locomotores i operacions.
7. Alimentació, xarxes, diagnosi i seguretat.
8. Automatització i simulació avançada.
9. Connectors Märklin, només després de validar protocol, estat i seguretat.
10. Retirada de WPF quan la paritat estigui demostrada.

## Dependències

Avalonia 12 admet .NET 10. La matriu de plataformes i els nivells de suport s'han de revisar a la documentació oficial d'Avalonia abans de cada publicació.
