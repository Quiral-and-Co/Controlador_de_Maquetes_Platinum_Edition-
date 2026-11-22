# Registre de congelació de versió validada

## Identificació de la línia base

- Projecte: JMRI TC Gold Custom
- Repositori: https://github.com/terrsola/jmri-tc
- Branca examinada: `main`
- SHA exacte congelat: `713cac3c25a24afa4cbecb2ff02353595700b9df`
- Commit: merge de la PR #22, «Add multi-vendor digital command station configuration»
- Data del commit: 2026-11-16 12:19:56 UTC
- Verificació de signatura del commit: GitHub indica `verified: true`.

Aquesta línia base identifica el codi provat. Els commits posteriors que afegeixin documentació o canvis de codi no alteren retroactivament aquest SHA ni hereten automàticament la seva validació.

## Evidència de CI

- Workflow: **Dispatcher and editor tests**
- Run: [#508](https://github.com/terrsola/jmri-tc/actions/runs/43865601085)
- Run ID: `43865601085`
- Event: `push`
- Branca: `main`
- SHA executat: `713cac3c25a24afa4cbecb2ff02353595700b9df`
- Estat: `completed`
- Conclusió: `success`
- Job `test`: `success`.

Tots els passos del job van acabar amb `success`: sintaxi JavaScript, instal·lació de dependències i Chromium, proves VMR reals amb navegador, nucli i pont de seguretat, horaris supervisats, màquina d'estats de trànsit, vigilància dead-man, Dispatcher, editor, JSON d'exemple, topologia de senyals, matriu d'inspecció física, integració SVG/Dispatcher/senyalització, diagnòstic JMRI, supervisió de desviaments i itineraris, configuració de centrals digitals i les dues execucions de regressió/fail-safe.

## Resultat de regressió

| Execució | Pas CI | Resultat |
|---|---|---|
| 1 | Full regression and fail-safe suite — run 1 | PASS |
| 2 | Full regression and fail-safe suite — run 2 | PASS |

També consten com a PASS les proves específiques de VMR amb Chromium, seguretat, pont, desviaments/itineraris i centrals digitals.

## Abast i exclusions

Aquesta congelació certifica únicament que el commit identificat ha superat els passos automatitzats del run indicat. No certifica el cablejat, les alimentacions, els fusibles, la parada d'emergència física, les centrals connectades ni el comportament elèctric real de la maqueta.

La configuració de la branca consultada durant aquesta tasca indicava `protected: false`. No s'ha pogut activar la protecció perquè la connexió GitHub utilitzada no té permisos d'administració. Cal que un administrador del repositori configuri les regles descrites a la guia de protecció.

## Criteri de canvi de línia base

Qualsevol nova línia base requereix SHA nou de `main`, execució CI associada exactament a aquell SHA, conclusió verda i regressió completa satisfactòria. Cal conservar aquest registre com a evidència històrica, no substituir-ne silenciosament els valors.
