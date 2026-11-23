# Migració de WPF a Avalonia

## Objectiu

Substituir progressivament la presentació WPF per Avalonia sense duplicar el domini, els casos d'ús ni la infraestructura. La migració és incremental i manté WPF fins que hi hagi paritat funcional demostrada.

## Arquitectura

- `Platinum.Domain`: model i regles compartits, target `net10.0`.
- `Platinum.Application`: casos d'ús i contractes compartits, target `net10.0`.
- `Platinum.Infrastructure`: persistència i adaptadors compartits, target `net10.0`.
- `Platinum.Presentation.Wpf`: interfície Windows existent, temporal.
- `Platinum.Presentation.Avalonia`: nova interfície multiplataforma, target `net10.0`.

## Fases

1. **Shell i build multiplataforma** — projecte Avalonia, tema Fluent, entrada d'aplicació i CI en Windows, Linux i macOS.
2. **MVVM i navegació** — contenidor DI, navegació, serveis de diàleg i gestió del cicle de vida.
3. **Editor de maqueta** — canvas 2D, zoom/pan, selecció i edició sobre els casos d'ús existents.
4. **Mòduls operatius** — catàleg, dispatcher, senyalització, locomotores i diagnòstic.
5. **Paritat i retirada WPF** — proves de regressió, accessibilitat, persistència i acceptació abans d'eliminar WPF.

## Criteris de seguretat

- No duplicar ni relaxar `PhysicalControlGate`.
- Les ordres físiques romanen deshabilitades per defecte.
- La simulació no comparteix canal d'escriptura amb cap adaptador físic.
- No declarar paritat ni retirar WPF només perquè existeixi una vista amb el mateix nom.

## Estat

- Fase 1 en curs: shell inicial i workflow multiplataforma.
- Els botons de navegació són placeholders; encara no representen mòduls migrats.
- WPF continua sent l'aplicació Windows de referència fins a completar les fases posteriors.
