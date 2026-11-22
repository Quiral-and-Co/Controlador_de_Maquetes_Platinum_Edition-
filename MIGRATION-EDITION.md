# Platinum Edition — repositori independent

Aquesta edició parteix del codi de la branca `migration/model-editor-json-v3` del repositori original i incorpora el contracte de validació JSON v3, els tests de regressió, l'esquema v3 i les notes de versió 1.4.0.

Aquest repositori és una còpia independent: el seu codi, les execucions de GitHub Actions i els artefactes de Windows es publiquen aquí, sense fusionar el PR #69 ni modificar `main` del repositori original.

## Versió

- Aplicació: `1.4.0`
- Model de projecte: JSON v3
- Instal·lador: Windows x64 (NSIS)
- Workflow: `.github/workflows/windows-installer.yml`

## Compilació local

```bash
npm install
npm test
npm run test:browser
npm run dist:win
```

L'instal·lador i el manifest SHA-256 es publiquen com a artefacte de la GitHub Action d'aquest repositori.
