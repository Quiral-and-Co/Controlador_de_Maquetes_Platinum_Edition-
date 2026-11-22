# Control DMI de locomotores

La finestra DMI afegeix velocímetre circular analògic i digital en km/h, consigna circular, limitador lineal, sentit de marxa, funcions F0–F32 i supervisió de dues senyals i desviaments de l’itinerari actiu.

## Calibratge i dades

- El límit en km/h és configurable per locomotora des del comandament. La conversió lineal entre 0–126 passos DCC i km/h és una estimació de consigna, no una mesura física.
- La velocitat real només s'ha de mostrar quan hi hagi telemetria de velocitat calibrada. La lectura de distància a senyal es demana a l'adaptador opcional `JMRI_TC_TRAIN_POSITION.getDistanceToSignal(signalId)`; sense aquesta font es mostra «Sense telemetria».
- Els aspectes s'obtenen del càlcul integrat de senyalització. Les posicions d'agulles provenen de les lectures JMRI supervisades; qualsevol absència o lectura obsoleta es representa com a desconeguda.

## Seguretat

El comandament utilitza la porta de seguretat de circulacions existent, incloent-hi adquisició, DCC armat, connexió JMRI, alimentació, sensors i home mort. No afegeix cap via alternativa per enviar ordres. Les sortides físiques continuen deshabilitades per defecte i aquesta interfície no substitueix l'E-STOP independent ni una validació ferroviària.

## Comandaments verticals addicionals
- El control vertical de velocitat té zero a baix i màxim a dalt. Els botons ➖/➕ canvien un pas DCC per pulsació.
- El fre de servei té alliberat a dalt i màxim a baix. Modula la consigna de tracció (no és un fre físic independent).
- El botó EMERGÈNCIA, sota el fre, envia la petició d'emergència al pont JMRI i desarma DCC. Cal mantenir una parada d'emergència física independent.
