# Gestió de projectes .platinum a Avalonia

## Abast

La primera pantalla Avalonia reutilitza directament:

- `ProjectService` per carregar i desar.
- `PlatinumPackageStore` com a implementació de `IProjectStore`.
- `TrackLayout` com a model de domini.

No crea un segon format de projecte. El paquet continua sent un ZIP amb `manifest.xml` i `layout.xml`.

## Funcionalitats

- Crear un projecte nou amb cinc nivells inicials.
- Obrir un fitxer `.platinum` mitjançant el selector de fitxers del sistema.
- Desar al camí actual o utilitzar «Desar com».
- Validar el model executant el mateix cicle de serialització que el desament, en un fitxer temporal que s'elimina en acabar.
- Advertir abans de descartar canvis pendents.
- Mostrar comptadors bàsics i el camí del projecte.

## Limitacions d'aquesta primera iteració

- La pantalla edita les metadades bàsiques del `TrackLayout`; no substitueix encara l'editor gràfic 2D/3D.
- Els elements ferroviaris carregats es conserven perquè el servei treballa sobre l'objecte `TrackLayout` complet.
- La validació comprova les regles actuals de `PlatinumPackageStore`; no afirma validar geometria física, compatibilitat elèctrica o seguretat d'una maqueta real.
- No s'envien ordres a cap central ni dispositiu físic.

## Execució

```bash
dotnet restore src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj
dotnet run --project src/Platinum.Presentation.Avalonia/Platinum.Presentation.Avalonia.csproj
```
