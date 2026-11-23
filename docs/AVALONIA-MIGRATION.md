# Migració de WPF a Avalonia

## Objectiu

Substituir progressivament la presentació WPF per Avalonia sense duplicar el domini, els casos d'ús ni la infraestructura. La migració és incremental i manté WPF fins que hi hagi paritat funcional demostrada.

## Arquitectura

- `Platinum.Domain`: model i regles compartits, target `net10.0`.
- `Platinum.Application`: casos d'ús i contractes compartits, target `net10.0`.
- `Platinum.Infrastructure`: persistència i adaptadors compartits, target `net10.0`.
- `Platinum.Presentation.Wpf`: interfície Windows existent, temporal.
- `Platinum.Presentation.Avalonia`: nova interfície multiplataforma, target `net10.0`.

La capa Avalonia aplica MVVM amb `INotifyPropertyChanged`, ViewModels per secció i ordres ICommand. El contenidor `Microsoft.Extensions.DependencyInjection` registra els ViewModels i `IDialogService`; l'Application crea i allibera el contenidor al cicle de vida d'escriptori. La finestra principal mostra la pàgina seleccionada amb DataTemplates, i els botons de navegació canvien de ViewModel sense acoblar les vistes entre elles.

`IDialogService` encapsula missatges, confirmacions i selectors de fitxer d'obertura/desament mitjançant el proveïdor d'emmagatzematge d'Avalonia. Els mòduls no han de cridar directament APIs de diàleg de la plataforma.

## Fases

1. **Shell i build multiplataforma** — projecte Avalonia, tema Fluent, entrada d'aplicació i CI en Windows, Linux i macOS.
2. **MVVM, DI i navegació** — completat a nivell de shell: contenidor DI, pàgines navegables, cicle de vida i servei de diàleg.
3. **Editor de maqueta** — canvas 2D, zoom/pan, selecció i edició sobre els casos d'ús existents. La implementació funcional encara no s'ha iniciat.
4. **Mòduls operatius** — catàleg, dispatcher, senyalització, locomotores i diagnòstic.
5. **Paritat i retirada WPF** — proves de regressió, accessibilitat, persistència i acceptació abans d'eliminar WPF.

## Criteris de migració de l'editor 2D

- Preservar el model de geometria existent: unitats, coordenades, escala, orientació, connexions i toleràncies.
- Mantenir les operacions d'edició com a casos d'ús/model compartit; la vista Avalonia només ha de traduir gestos en aquestes operacions.
- Afegir proves de paritat per càrrega/desament, selecció, moviment, rotació, encaix i undo/redo abans de substituir la vista WPF.
- No introduir conversió de coordenades ni arrodoniments implícits sense proves que demostrin equivalència.

## Criteris de seguretat

- No duplicar ni relaxar `PhysicalControlGate`.
- Les ordres físiques romanen deshabilitades per defecte.
- La simulació no comparteix canal d'escriptura amb cap adaptador físic.
- No declarar paritat ni retirar WPF només perquè existeixi una vista amb el mateix nom.

## Estat

- Shell Avalonia, MVVM, navegació, DI i servei de diàleg implementats.
- Les pàgines de catàleg i operacions són encara punts d'entrada; no equivalen a mòduls migrats.
- L'editor 2D té una pàgina inicial informativa, no un canvas funcional.
- WPF continua sent la referència fins a completar la paritat.
