# Migració de WPF a Avalonia

## Objectiu

Substituir progressivament la presentació WPF per Avalonia sense duplicar el domini, els casos d'ús ni la infraestructura. La migració és incremental i manté WPF fins que hi hagi paritat funcional demostrada.

## Arquitectura

- `Platinum.Domain`: model i regles compartits, target `net10.0`.
- `Platinum.Application`: casos d'ús i contractes compartits, target `net10.0`.
- `Platinum.Infrastructure`: persistència i adaptadors compartits, target `net10.0`.
- `Platinum.Presentation.Wpf`: interfície Windows existent, temporal.
- `Platinum.Presentation.Avalonia`: nova interfície multiplataforma, target `net10.0`.

La capa Avalonia aplica MVVM amb `INotifyPropertyChanged`, ViewModels per secció i ordres ICommand. El contenidor `Microsoft.Extensions.DependencyInjection` registra els ViewModels, `IDialogService` i el motor d'edició compartit. La finestra principal mostra la pàgina seleccionada amb DataTemplates.

La geometria del catàleg Märklin C s'ha extret de la presentació WPF a `Platinum.Domain.Catalogue`. Els registres `CatalogueItem`, `CataloguePort` i `CatalogueTrackPath` i els càlculs dels ports/traçats són ara compartits. La lectura JSON resideix a `Platinum.Infrastructure.Catalogue.MarklinCCatalogue`.

## Motor d'edició compartit

`Platinum.Domain.Layout.TrackEditorEngine` és independent de WPF i Avalonia. Manté el document de segments, peces i connexions i implementa:
- creació de rectes, corbes quadràtiques i desviaments amb la mateixa fórmula de punt de control que l'editor WPF;
- moviment de segments traslladant inici, final i punt de control;
- moviment i rotació de peces, normalitzant l'angle a [0, 360);
- encaix a graella en mil·límetres amb arrodoniment midpoint-to-even;
- selecció per hit-test amb toleràncies de 115 mm per al centre de peça, 55 mm per al traçat i 22 mm per als ports;
- transformació de ports locals al sistema global amb la rotació de la peça;
- connexions lògiques per ID de port i historial desfer/refer, incloent-hi una única transacció d'historial per arrossegament.

L'editor WPF continua amb el seu codi visual propi en aquesta fase, però ara utilitza els mateixos models de geometria del domini. La següent tasca de paritat serà substituir els seus handlers per crides a `TrackEditorEngine`; no s'ha eliminat ni reescrit la interfície WPF.

## Editor Avalonia

- Canvas 2D interactiu: dibuix de rectes, corbes i desviaments; selecció, moviment, zoom i encaix a graella.
- Col·locació de peces del catàleg compartit, traçat dels seus camins centrals i visualització dels ports.
- Connexió entre ports del catàleg amb distinció de ports derivats i estimats.
- Vista 3D projectada amb orbitació i zoom; representa els mateixos segments, peces, ports, connexions i cotes de nivell.
- Les connexions es mantenen com a lògiques i sempre es creen amb `PhysicallyVerified = false`.

La vista 3D Avalonia actual és una projecció interactiva 3D, no encara una escena de malla/il·luminació equivalent al Viewport3D WPF. La vista Avalonia també parteix d'un nivell base buit; la integració amb càrrega/desament de `TrackLayout`, nivells reals i cicle de vida de projectes queda pendent.

## Proves de paritat

El projecte `tests/Platinum.LayoutEditor.Tests` és multiplataforma i verifica:
- fórmula de control de corbes de l'editor WPF;
- moviment de segments i preservació del punt de control;
- rotació i normalització d'angles;
- encaix a graella;
- toleràncies de selecció;
- transformació i hit-test de ports;
- connexions lògiques, duplicats, desfer/refer;
- col·locació de peces i coherència entre ports i camins del catàleg.

La CI executa aquestes proves a Linux i compila l'aplicació Avalonia a Windows, Linux i macOS.

## Criteris pendents abans de declarar paritat

- Adaptar WPF a l'engine compartit i executar els mateixos casos de prova contra ambdues vistes.
- Integrar càrrega/desament de `TrackLayout` a Avalonia sense alterar els esquemes JSON v2/v3 ni els paquets Platinum.
- Afegir proves d'encaix i connexions sobre els ports reals del catàleg, incloent-hi casos `estimated`.
- Completar la vista 3D amb la mateixa representació de rails, travesses i detalls de peces que WPF.
- Afegir proves d'acceptació d'usuari per selecció, moviment, gir, zoom, connexió, desfer i refer.

## Criteris de seguretat

- No duplicar ni relaxar `PhysicalControlGate`.
- Les ordres físiques romanen deshabilitades per defecte.
- Les connexions creades per l'editor són lògiques; no es marquen com a verificades físicament.
- La simulació no comparteix canal d'escriptura amb cap adaptador físic.
- No declarar paritat ni retirar WPF només perquè existeixi una vista amb el mateix nom.

## Estat

- Shell Avalonia, MVVM, navegació, DI i servei de diàleg implementats.
- Motor d'edició i geometria de catàleg compartits al domini.
- Canvas 2D funcional per a traçats, peces de catàleg, ports, connexions, selecció, moviment, rotació, zoom, graella i desfer/refer.
- Vista 3D projectada funcional per inspecció, encara no equivalent al render 3D WPF.
- Càrrega/desament de projectes i nivells de projectes encara pendents a Avalonia.
- WPF continua com a referència i no s'ha retirat.
