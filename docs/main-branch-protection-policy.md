# Política de protecció de la branca `main`

## Estat observat el 2026-11-16

La consulta de GitHub va retornar `protected: false` per a `main`. La connexió autoritzada per aquesta sessió informa `admin: false`, `maintain: false`, `push: true`; per tant, no pot activar branch protection ni rulesets. Aquesta política és una proposta preparada per aplicar per un administrador, no una protecció ja activa.

## Regles recomanades

1. Exigir que els canvis arribin mitjançant pull request; prohibir pushes directes a `main`.
2. Exigir que el check del workflow `Dispatcher and editor tests` (job `test`) passi abans de fusionar.
3. Exigir que la branca de la PR estigui actualitzada amb `main` abans de fusionar.
4. Prohibir force-push i eliminació de `main`.
5. Aplicar les regles també als administradors; qualsevol excepció ha de quedar explícita i auditada.
6. Revisió humana obligatòria abans de fusionar canvis que afectin seguretat, interbloqueigs, adaptadors de maquinari o sortides físiques.
7. No permetre que una CI verda substitueixi les inspeccions físiques ni autoritzi DCC/automatismes.

## Aplicació

Un administrador del repositori ha d'activar aquestes regles a Settings → Branches (branch protection rule) o Rulesets, associades exactament a `refs/heads/main`. Després cal tornar a consultar l'API de la branca i verificar `protected: true`, els checks obligatoris i la prohibició de force-push/eliminació.

Fins que aquesta verificació no retorni els valors esperats, `main` s'ha de considerar no protegida.
