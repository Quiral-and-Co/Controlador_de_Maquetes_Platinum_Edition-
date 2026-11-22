# Validation Master Registry

## Funció
El Validation Master Registry (VMR) crea un registre mestre versionat per projecte per a senyals, blocs, sensors i desviaments. Cada entitat rep un identificador immutable `JMRI-TC-VMR-TYPE-00001` separat del seu identificador d'origen. La reconciliació és idempotent: una segona sincronització no duplica registres. Els elements que desapareixen de la configuració es conserven com a retirats, sense esborrar-ne la traçabilitat.

## Persistència
L'estat es manté a `validationMasterRegistry` dins del projecte i es desa també al magatzem local del navegador, associant la recuperació local a l'identificador del projecte. La importació/exportació JSON permet conservar còpies externes i traslladar el registre. En restaurar un registre antic sense comptadors, aquests es reconstrueixen a partir dels identificadors existents per evitar col·lisions després de recarregar el projecte. Si s'importa un projecte que ja conté el VMR, aquest estat preval sobre la còpia local.

## Identificadors i historial
- `registryId`: identificador únic assignat pel VMR.
- `identityKey`: tipus + identificador d'origen, emprat per evitar duplicats.
- `audit`: esdeveniments d'alta, retirada i canvis d'estat.
- Les anotacions s'associen a l'element individual i queden auditades.

## Actes de validació
L'acta Markdown i la seva exportació JSON inclouen identificadors, estat, comprovacions pendents, recompte, anotacions per element i historial d'auditoria. Diferencia pendents de programari, preparats per inspecció física, registres marcats com a físicament complets i no conformitats.

## Reserva de seguretat obligatòria
L'acta és un registre de traçabilitat, no una certificació. `PHYSICAL_READY` reflecteix caselles declarades per l'operador i no mesures independents. No es genera cap autorització operativa. DCC, sortides físiques, comandament de desviaments i automatització resten deshabilitats fins a completar i revisar les proves de programari/fail-safe, la inspecció de cablejat i proteccions elèctriques i la parada d'emergència física independent.
