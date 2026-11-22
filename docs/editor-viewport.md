# Navegació de l’editor 2D

## Comportament

- Vista inicial ajustada a la superfície de 3000 × 1800 mm.
- Roda del ratolí: zoom centrat en la coordenada sota el cursor.
- Botons + i −: zoom centrat al mig de la vista.
- Espai + arrossegar amb el botó principal, o arrossegar amb el botó central: desplaçament.
- «Ajusta a superfície»: restaura la vista completa.
- Rang de zoom: 10%–1000% respecte de l’amplada del tauler.

## Integritat de dades

El mòdul `viewport-core.js` només calcula el rectangle visible de l’SVG. No toca l’estat del projecte, les coordenades de les peces, les connexions, els blocs ni els sensors. El percentatge és una relació d’amplada respecte a la superfície completa, no una escala física de pantalla calibrada.

El desplaçament queda limitat a la superfície mentre el zoom és superior a l’ajust. Quan s’allunya més que la vista completa, el tauler queda centrat i es poden veure marges.

## Proves

```bash
node --check viewport-core.js
node tests/viewport.cjs
node tests/smoke.cjs
```
