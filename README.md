# Aplicació d'escriptori nativa · .NET 10 WPF

> **Branca de migració nativa:** l'aplicació d'escriptori en desenvolupament és C# + WPF + XAML. No carrega HTML, CSS, JavaScript, WebView ni necessita iniciar cap servidor o navegador per executar el projecte WPF. Els fitxers web descrits més avall es mantenen com a implementació de referència mentre es completa la paritat funcional; no són dependències del projecte WPF.

Per compilar i executar la versió nativa a Windows:

```powershell
dotnet restore Platinum.sln
dotnet build Platinum.sln -c Release
dotnet run --project src/Platinum.Presentation.Wpf/Platinum.Presentation.Wpf.csproj
```

Consulta [la guia de migració WPF](docs/native-wpf-migration.md) per veure què ja s'ha portat i què queda pendent. Les pantalles noves de simulació són locals i no controlen maquinari real.

## Instal·lació i actualitzacions (WPF)

El setup Inno Setup utilitza un `AppId` estable. Quan detecta una instal·lació anterior de Quiral Platinum, executa el desinstal·lador antic en mode silenciós abans de copiar els fitxers nous. Aquesta operació es fa quan l'usuari ja ha confirmat la instal·lació; si la desinstal·lació falla, el setup s'atura i no continua amb una instal·lació parcial. Es conserva la carpeta d'instal·lació anterior i es tornen a crear les dreceres.

La barra superior de Platinum inclou **Actualitzar des de GitHub**. L'aplicació consulta l'última GitHub Release, compara la versió de l'assembly, descarrega l'instal·lador oficial .exe, verifica el manifest SHA-256 i només després de la confirmació de l'usuari inicia l'instal·lador elevat de Windows. L'aplicació es tanca perquè Inno Setup pugui substituir la versió instal·lada.

### Generar el setup

- El workflow [Platinum WPF setup build](.github/workflows/wpf-setup-build.yml) compila WPF self-contained per a Windows x64 i publica un artefacte descarregable a GitHub Actions.
- El workflow [Platinum WPF Windows release](.github/workflows/wpf-release.yml) genera el mateix instal·lador i el publica com a GitHub Release amb el seu manifest SHA-256.
- Per publicar una release estable, executa el workflow de release indicant una versió semàntica (per exemple `0.2.0`) o crea un tag `vMAJOR.MINOR.PATCH`.
- Fins que no hi hagi una GitHub Release amb l'EXE i el fitxer `.sha256`, el botó d'actualització només pot informar de l'estat; no hi ha cap actualització instal·lable.

La verificació SHA-256 detecta fitxers alterats o corruptes, però no substitueix la signatura Authenticode del fabricant. Abans de distribuir públicament, és recomanable signar l'instal·lador amb un certificat de codi.

**Motor de control propi:** Platinum ja disposa de serveis C# i d'un adaptador de xarxa natiu per a Märklin CS2: CAN-over-Ethernet UDP, ports 15731/15730 i consulta de presència, sense JMRI/Java/WebSocket. Les ordres físiques continuen bloquejades fins a completar el mapa d'identificadors, la supervisió de seguretat i les proves amb maquinari. CS3, Z21 i ECoS continuen pendents. Vegeu [motor de control propi](docs/own-railway-control-engine.md) i [protocol natiu CS2](docs/native-marklin-cs2-protocol.md).

---

# Codi heretat (referència de migració) — JMRI TC Gold Custom 1.4.0

La secció següent descriu la implementació web/Electron anterior i es conserva com a referència funcional durant la migració. No forma part de l'executable WPF natiu i no s'ha d'utilitzar com a instrucció per configurar la nova aplicació. Les funcions pendents s'estan reimplementant en C#.

## Funcions disponibles

### Edició 2D (0.3–0.4)
- Tauler de 3000 × 1800 mm, graella de 10 mm i cinc nivells amb cotes previstes de 0, 110, 220, 330 i 440 mm.
- Primitives gràfiques genèriques de recta, corba i desviament; selecció, moviment, rotació i geometria editable. No són referències de catàleg Märklin C.
- Connexions geomètriques entre ports A/B/C amb `verified:false`; no validen continuïtat elèctrica.
- Historial Desfer/Refer (fins a 100 estats), duplicació i mesura euclidiana en mm.

### Blocs, sensors i nivells (0.6)
- Crear blocs lògics al nivell actual i assignar-los a peces. `trackIds` i `sensorIds` es recalculen des de les assignacions de les peces.
- Registrar sensors amb el `systemName` JMRI i nom visible opcional; assignar-los a peces. Tots els sensors nous queden `verified:false`.
- Canviar una peça de nivell només si no té bloc, sensor ni connexions. No es crea cap enllaç vertical.
- Importar configuracions v2. Les assignacions textuals antigues es migren a registres provisionals explícits, no verificats.
- Exportació JSON en model v3 i comprovacions d'IDs, referències i connexions dins del mateix nivell.

## Execució i proves

Obre `index.html` en un navegador modern. No cal servidor per editar offline. Per monitoritzar JMRI, activa el Web Server de JMRI i configura l'adreça i el port a la interfície (habitualment 12080).

Executa:

```bash
node --check app.js
node tests/smoke.cjs
python3 -m json.tool config/layout.example.json
```

El fitxer `config/layout.example.json` ha de validar contra `schema/layout-v3.schema.json` (JSON Schema Draft 2020-12).

La resta de documentació heretada i especificacions funcionals del repositori es mantenen sense canvis durant aquesta migració.
