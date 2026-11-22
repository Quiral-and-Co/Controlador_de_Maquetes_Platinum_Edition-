# Senyalització ADIF

Mòdul de modelatge i simulació dels deu aspectes fonamentals del Catàleg Oficial de Senyals de Circulació Ferroviària (BOE-A-2017-556): via lliure, via lliure condicional, anunci de precaució, preanunci de parada, anunci de parada, anunci de parada immediata, parada selectiva, parada, rebasament autoritzat i moviment autoritzat.

El model separa la funció de la senyal (preavançada, avançada, intermèdia, protecció, entrada, interior/destí, sortida, alliberament o maniobres) del disseny físic (alta, baixa, mono cap o múltiples caps). El nombre d'aspectes depèn de la funció i del context d'infraestructura; no totes les senyals poden mostrar els deu.

La matriu de compatibilitat segueix l'apartat 1.5 del Catàleg: via lliure condicional només en BA/BLA i Vmax >160 km/h; parada selectiva només en línia d'alta velocitat amb ERTMS BCA; anunci de precaució, anunci de parada i parada immediata condicionats a funció, agulles i senyal següent. Rebase i moviment autoritzat exigeixen autorització explícita; mai es dedueixen d'un itinerari lliure.

Les senyals es desen a `config.signals` del projecte. El simulador aplica criteri restrictiu: emergència, ocupació, estat desconegut, reserva absent o desviaments no verificats/alineats impedeixen via lliure.

**Límit de seguretat:** és un mòdul de maqueta i simulació, no un enclavament homologat ni un sistema ferroviari de seguretat. No habilita sortides físiques ni substitueix l'E-stop cablejat independent. Manteniu DCC i automatització física bloquejats fins a validar programari, cablejat i proteccions amb proves reals.

Referències: [Catàleg Oficial de Senyals, BOE-A-2017-556](https://www.boe.es/eli/es/o/2016/12/30/fom2015/con), apartat 1.5 i capítol 2; [Reglament de Circulació Ferroviària, BOE-A-2015-8042](https://www.boe.es/eli/es/rd/2015/07/17/664/con).

## Validació de seqüència i aspectes

El camp `nextSignal1Id` identifica la primera senyal posterior i `nextSignal2Id` la següent després de Senyal 1; `nextSignalId` es conserva com a àlies de compatibilitat de Senyal 1. El validador de seqüència comprova existència, itinerari, ordre dels blocs segons `direction` (`FORWARD` per defecte o `REVERSE`), continuïtat declarada i dependències cícliques. Si no hi ha itinerari verificable, emet un avís i no afirma haver comprovat l'ordre físic.

Els aspectes es calculen segons la funció de la senyal i les condicions d'infraestructura. Les proves automatitzades cobreixen compatibilitat, transicions condicionades per la senyal següent, ocupació, estat desconegut i autoritzacions explícites. Aquestes proves són de simulació; no validen distàncies de frenada reals ni constitueixen una certificació RFIG.
