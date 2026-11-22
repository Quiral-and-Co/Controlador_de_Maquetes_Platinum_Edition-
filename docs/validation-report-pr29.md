# Acta de validació — senyalització RFIG (PR #29)

## Identificació i abast

- Repositori: `tc-jmri-team/jmri-tc-app`
- Branca: `feature/pr29-rfig-signalling-validation`
- PR: [#29 — validació VMR i seqüència RFIG](https://github.com/tc-jmri-team/jmri-tc-app/pull/29)
- Main de referència: `3e34d909ba1659debf270425dc4d9052bd224d1f`
- SHA de codi validat per CI completa: `3960e146fdf0e3e9a7af859e851972fbf34c3a90`
- Data: 2026-11-16

## Evidència CI

| Execució | SHA provat | Workflow | Resultat |
|---|---|---|---|
| [#579](https://github.com/tc-jmri-team/jmri-tc-app/actions/runs/43953581406) | `3073ea5d989a53ae583661eddf3b80f7fee216f7` | Dispatcher and editor tests | SUCCESS |
| [#580](https://github.com/tc-jmri-team/jmri-tc-app/actions/runs/43953618305) | `3073ea5d989a53ae583661eddf3b80f7fee216f7` | Dispatcher and editor tests | SUCCESS |
| [#584](https://github.com/tc-jmri-team/jmri-tc-app/actions/runs/43954035805) | `b722e1b5269796fb2d02f8fd4b94f94701b8f393` | Dispatcher and editor tests | SUCCESS |
| [#585](https://github.com/tc-jmri-team/jmri-tc-app/actions/runs/43954048874) | `b722e1b5269796fb2d02f8fd4b94f94701b8f393` | Dispatcher and editor tests | SUCCESS |
| [#591](https://github.com/tc-jmri-team/jmri-tc-app/actions/runs/43954384152) | `d35a4d39a18ec91a52d2a59960124e07e778224e` | Dispatcher and editor tests | SUCCESS |
| [#592](https://github.com/tc-jmri-team/jmri-tc-app/actions/runs/43954451296) | `d35a4d39a18ec91a52d2a59960124e07e778224e` | Dispatcher and editor tests | SUCCESS |
| [#605](https://github.com/tc-jmri-team/jmri-tc-app/actions/runs/43955872535) | `3960e146fdf0e3e9a7af859e851972fbf34c3a90` | Dispatcher and editor tests | SUCCESS |

La CI #605 ha completat tots els passos: comprovació de sintaxi, instal·lació de Chromium, VMR de navegador real, proves de seguretat, integració de senyalització, topologia, diagnòstics JMRI, supervisió de desviaments i dues passades completes de regressió/fail-safe. Tots els passos han acabat en SUCCESS.

## Integració del validador de seqüències

- `signalling-sequence-core.js` s'invoca des de `signalling-integration-core.js`, és a dir, des del motor que calcula els aspectes actius.
- `index.html` carrega el validador abans del nucli d'integració.
- Es comproven referències de Senyal 1/Senyal 2, continuïtat, itinerari, ordre de blocs segons el sentit i cicles.
- Una seqüència invàlida o sense itinerari verificable força l'aspecte restrictiu `PARADA`.
- Les proves integrades cobreixen una seqüència vàlida i una seqüència incompatible amb el sentit de circulació.

## Proves de fallada i recuperació

A `tests/signalling-integration.cjs` les recuperacions parteixen d'un estat fallit explícit i exigeixen evidència nova abans de permetre una nova avaluació:

- Comunicació: pèrdua de connexió → `PARADA` → heartbeat nou → reavaluació.
- Sensor: codificació desconeguda → `UNKNOWN`/`PARADA` → lectura FREE nova i vàlida → reavaluació.
- Alimentació: OFF → `PARADA` → confirmació ON amb marca temporal nova → reavaluació.
- Seqüència: referència absent → `PARADA` → reparació de la cadena Senyal 1/Senyal 2 → reavaluació.

La suite també cobreix dades obsoletes, bloc ocupat, itinerari absent, desviaments no verificats o desalineats, emergència, referències inexistents i cicles.

## Estat de la VMR

- VMR-023 — seqüència Senyal 1/Senyal 2: **PASS-CI**, execució #605, SHA `3960e14`.
- VMR-024 — aspectes i transicions: **PASS-CI**, execucions #591/#592.
- VMR-025 — traçabilitat i acta: **PASS-CI**, execució #605; acta actualitzada amb les evidències i els límits.
- VMR-026 — fallada i recuperació: **PASS-CI**, execució #605.
- VMR-016 a VMR-020 i VMR-022 — comprovacions físiques i autorització: **PENDENT-FÍSIC**.
- VMR-021 — referència Digikeijs ADJ9000: **NO-VERIFICAT**.

## Resolució de les observacions tècniques

- **R1 — RESOLTA:** el validador de seqüències està integrat al flux actiu; la CI prova la validació d'ordre i sentit des del motor d'integració.
- **R2 — RESOLTA:** les proves ja no reutilitzen la mateixa configuració vàlida; cada recuperació comença amb una fallada real i exigeix reparació o telemetria nova.
- **CI completa #605 — SUCCESS:** inclou dues passades de regressió/fail-safe.

## Pendents no coberts per CI

1. Inspecció elèctrica individual, cablejat, fusibles, polaritat i parada d'emergència física independent.
2. Validació en banc dels models, protocols i firmware de les centrals reals.
3. Verificació independent de la maqueta i dels itineraris físics; les proves de simulació no certifiquen distàncies reals de frenada ni compliment ferroviari.

Aquesta acta no és una certificació ADIF/RFIG ni substitueix un enclavament ferroviari homologat.

## Decisió

**NO AUTORITZAT per a operació física.** DCC, sortides físiques, comandament de desviaments i automatització continuen bloquejats fins a completar i revisar totes les comprovacions físiques independents. La CI verda acredita únicament les proves de programari executades sobre el SHA indicat. La PR #29 es manté oberta i en esborrany; no s'ha fusionat.
