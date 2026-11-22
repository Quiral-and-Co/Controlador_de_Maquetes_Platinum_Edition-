# Diagnòstic i matriu de validació física

## Abast

La fase afegeix tres eines de preparació:
1. Selecció d'una senyal al registre i ressaltat dels trams/peces que formen la seva dependència topològica (bloc protegit, blocs de l'itinerari i desviaments).
2. Resum d'incidències i estat de vinculacions per nivell.
3. Matriu individual de comprovacions per senyal, bloc, sensor i desviament.

La matriu es desa al projecte JSON com a `validationMatrix`. Les caselles són registres d'inspecció introduïts per l'operador; no són lectures instrumentals ni certificats.

## Estats

- `PENDING_SOFTWARE`: falten comprovacions de programari.
- `SOFTWARE_READY`: comprovacions lògiques completades; queda pendent la inspecció física.
- `PHYSICAL_READY`: totes les comprovacions de la matriu han estat marcades.
- `FAILED`: incidència declarada; cal corregir-la i retirar l'estat de no conformitat.

## Comprovacions per element

- **Senyal:** referències i aspectes, comportament restrictiu, cablejat dels llums i comprovació individual dels aspectes.
- **Bloc:** assignació de peces, cobertura de sensors, detecció d'ocupació/alliberament i aïllaments.
- **Sensor:** identificador JMRI, actualització de lectures, cablejat/polaritat i proves ocupat/lliure.
- **Desviament:** vincle i posició requerida, tractament del feedback desconegut, proves recta/branca i protecció del motor.

## Seguretat

Aquesta matriu és una eina de diagnòstic i traçabilitat. **No arma DCC, no envia ordres JMRI i no habilita cap sortida física.** L'estat `PHYSICAL_READY` només indica que l'operador ha marcat totes les caselles de l'element; no demostra que s'hagin mesurat el cablejat, les proteccions o el circuit d'emergència.

Abans de qualsevol prova a la maqueta cal:
- validar la suite de programari i els casos fail-safe;
- inspeccionar cablejat, polaritats, seccions, alimentacions, fusibles i proteccions;
- provar una parada d'emergència física, independent del PC/JMRI i capaç de tallar la tracció;
- documentar cada sensor, desviament i senyal en les seves comprovacions físiques.

Les sortides físiques i l'automatització DCC romanen deshabilitades fins que totes les verificacions globals i independents hagin estat completades i revisades.
